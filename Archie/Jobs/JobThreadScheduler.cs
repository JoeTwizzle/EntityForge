using Archie.Helpers;
using System.Collections.Concurrent;

namespace Archie.Jobs
{

    public sealed class JobThreadScheduler : IDisposable
    {
        static readonly object sLock = new();

        internal static JobThreadScheduler[] Schedulers { get; private set; } = Array.Empty<JobThreadScheduler>();

        private static int schedulerCounter;

        private static readonly Queue<int> recycledIds = new();

        public readonly int Id;

        private readonly Thread[] workerThreads;

        private readonly CancellationTokenSource source;

        private readonly ConcurrentQueue<(Action job, int jobId)> jobs;

        private readonly ReaderWriterLockSlim rwLock = new();

        internal int maxCompletedJobId;

        internal int jobCounter;

        //To deal with int overflow reset counter to 0 after ushort.MaxValue
        bool idReset;

        //Sliding window of active/completed jobs
        //index = id - maxCompletedJobId
        internal bool[] activeJobs = Array.Empty<bool>();

        public JobThreadScheduler()
        {
            jobs = new();
            source = new();
            workerThreads = new Thread[Math.Max(1, Environment.ProcessorCount - 1)];
            for (int i = 0; i < workerThreads.Length; i++)
            {
                workerThreads[i] = new Thread(() => WorkerLoop(source.Token)) { IsBackground = true, Name = $"Worker {i + 1}" };
            }
            lock (sLock)
            {
                int id;
                if (!recycledIds.TryDequeue(out id))
                {
                    id = Interlocked.Increment(ref schedulerCounter);
                }
                Id = id;
                Schedulers.EnsureContains(id);
                Schedulers[id] = this;
            }
        }

        void WorkerLoop(CancellationToken token)
        {
            var sw = new SpinWait();
            while (!token.IsCancellationRequested)
            {
                if (jobs.TryDequeue(out var action))
                {
                    action.job();
                    rwLock.EnterUpgradeableReadLock();
                    int neededSize = jobCounter - maxCompletedJobId;
                    int index = action.jobId - maxCompletedJobId;
                    activeJobs.EnsureContains(neededSize);
                    bool allDone = true;
                    for (int i = 0; i < index; i++)
                    {
                        allDone &= activeJobs[i];
                    }
                    rwLock.EnterWriteLock();
                    if (allDone)
                    {
                        for (int i = index; i < neededSize; i++)
                        {
                            activeJobs[i - index] = activeJobs[i];
                        }
                        maxCompletedJobId = index;
                    }
                    else
                    {
                        activeJobs[index] = true;
                    }
                    rwLock.ExitWriteLock();
                    rwLock.ExitUpgradeableReadLock();
                }
                else
                {
                    sw.SpinOnce();
                }
                if (sw.NextSpinWillYield)
                {
                    sw.SpinOnce();
                    sw.Reset();
                }
            }
        }

        public JobHandle Schedule(Action job)
        {
            while (idReset) ;
            var jobId = Interlocked.Increment(ref jobCounter);
            if (jobId > ushort.MaxValue)
            {
                idReset = true;
                WaitForAllCompletion();
                jobId = 1;
                jobCounter = 1;
                maxCompletedJobId = 0;
                idReset = false;
            }
            var handle = new JobHandle(Id, jobId);
            jobs.Enqueue((job, jobId));
            return handle;
        }

        void WaitForAllCompletion()
        {
            var sw = new SpinWait();
            while (maxCompletedJobId != jobCounter)
            {
                sw.SpinOnce();
            }
        }

        public void Dispose()
        {
            jobs.Clear();
            source.Cancel();
            WaitForAllCompletion();
            for (int i = 0; i < workerThreads.Length; i++)
            {
                workerThreads[i].Join();
            }
            source.Dispose();
            rwLock.Dispose();
            lock (sLock)
            {
                recycledIds.Enqueue(Id);
            }
        }
    }
}
