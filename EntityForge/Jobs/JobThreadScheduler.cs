using EntityForge.Helpers;
using System.Collections.Concurrent;
using System.Numerics;

namespace EntityForge.Jobs
{
    public sealed class JobThreadScheduler : IDisposable
    {
        internal static readonly ReaderWriterLockSlim SchedulersArrayLock = new();

        internal static JobThreadScheduler[] Schedulers { get; private set; } = new JobThreadScheduler[1];

        private static short schedulerCounter;

        private static readonly Queue<short> recycledIds = new();

        private readonly short id;
        internal short Version;


        private readonly Thread[] workerThreads;

        private readonly CancellationTokenSource cancellationTokenSource;

        private readonly ConcurrentQueue<(Action job, int jobId)> jobs;

        internal readonly ReaderWriterLockSlim rwLock = new();

        internal int maxCompletedJobId;

        internal int jobCounter;


        private SpinLock spinLock;
        //Sliding window of active/completed jobs
        //index = id - maxCompletedJobId
        internal bool[] activeJobs = Array.Empty<bool>();

        public JobThreadScheduler()
        {
            jobs = new();
            cancellationTokenSource = new();
            workerThreads = new Thread[Math.Max(1, Environment.ProcessorCount - 1)];
            for (int i = 0; i < workerThreads.Length; i++)
            {
                workerThreads[i] = new Thread(() => WorkerLoop(cancellationTokenSource.Token)) { IsBackground = true, Name = $"Worker {i + 1}" };
            }
            //register in static array
            SchedulersArrayLock.EnterWriteLock();
            if (!recycledIds.TryDequeue(out short id))
            {
                id = schedulerCounter++;
            }
            this.id = id;

            Schedulers = Schedulers.EnsureContains(id);
            Schedulers[id] = this;
            SchedulersArrayLock.ExitWriteLock();
            //start all worker threads
            for (int i = 0; i < workerThreads.Length; i++)
            {
                workerThreads[i].Start();
            }
        }

        void WorkerLoop(CancellationToken token)
        {
            var sw = new SpinWait();
            while (!token.IsCancellationRequested)
            {
                //Get job if available
                if (jobs.TryDequeue(out var action))
                {
                    //execute job
                    action.job();
                    //read jobCounter
                    rwLock.EnterUpgradeableReadLock();
                    int maxJob = maxCompletedJobId;
                    int requiredSlidingWindowSize = jobCounter - maxJob;
                    int activeJobsIndex = (action.jobId - maxJob) - 1;

                    //check if we are the first item in this array and the array has items
                    if (activeJobsIndex == 0)
                    {
                        rwLock.EnterWriteLock();
                        //Scan for last job that was completed from left to right of array
                        int afterLastCompletedJobIndex = 1;
                        for (; afterLastCompletedJobIndex < activeJobs.Length && activeJobs[afterLastCompletedJobIndex]; afterLastCompletedJobIndex++) ;
                        //move all uncompleted items to the left
                        if (activeJobs.Length > 0)
                        {
                            Array.Copy(activeJobs, afterLastCompletedJobIndex, activeJobs, 0, activeJobs.Length - afterLastCompletedJobIndex);
                        }
                        rwLock.ExitWriteLock();
                        Interlocked.Add(ref maxCompletedJobId, afterLastCompletedJobIndex);
                    }
                    else //we are somewhere in the middle of the array
                    {
                        //check if we need to grow to fit in the array
                        if (activeJobs.Length <= requiredSlidingWindowSize)
                        {
                            rwLock.EnterWriteLock();
                            Array.Resize(ref activeJobs, (int)BitOperations.RoundUpToPowerOf2((uint)requiredSlidingWindowSize + 1));
                            rwLock.ExitWriteLock();
                        }
                        // mark job as completed
                        activeJobs[activeJobsIndex] = true;
                    }
                    rwLock.ExitUpgradeableReadLock();
                }
                else //no job was available
                {
                    //wait a bit
                    sw.SpinOnce();
                }
                // check if we will switch context next spin
                if (sw.NextSpinWillYield)
                {
                    //Spin and switch contexts
                    sw.SpinOnce();
                    //reset so we do not switch contexts too often.
                    //this does waste a few cpu cycles for lower latency.
                    sw.Reset();
                }
            }
        }


        public JobHandle Schedule(Action job)
        {
            var jobId = Interlocked.Increment(ref jobCounter);
            //To deal with int overflow reset counter to 0
            bool idReset = false;
            spinLock.Enter(ref idReset);
            if (jobId == int.MaxValue - 20000)
            {
                WaitForActiveJobsCompletion();
                jobCounter = 1;
                jobId = jobCounter;
                maxCompletedJobId = 0;
                Version++;
            }
            if (idReset)
            {
                spinLock.Exit();
            }
            var handle = new JobHandle(id, Version, jobId);
            jobs.Enqueue((job, jobId));
            return handle;
        }

        void WaitForActiveJobsCompletion()
        {
            var sw = new SpinWait();
            while (maxCompletedJobId != jobCounter)
            {
                sw.SpinOnce();
            }
        }

        public void Dispose()
        {
            //Signal threads to exit after current iteration
            cancellationTokenSource.Cancel();
            WaitForActiveJobsCompletion();
            //discard all pending jobs
            jobs.Clear();
            //make sure all threads have finished their work
            for (int i = 0; i < workerThreads.Length; i++)
            {
                workerThreads[i].Join();
            }
            //unregister from static array
            SchedulersArrayLock.EnterWriteLock();
            Schedulers[id] = null!;
            recycledIds.Enqueue(id);
            SchedulersArrayLock.ExitWriteLock();
            //dispose resources
            cancellationTokenSource.Dispose();
            rwLock.Dispose();
        }
    }
}
