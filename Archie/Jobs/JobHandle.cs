namespace Archie.Jobs
{
    public readonly struct JobHandle : IEquatable<JobHandle>
    {
        public bool IsCompleted
        {
            get
            {
                var scheduler = JobThreadScheduler.Schedulers[schedulerId];
                if (scheduler.maxCompletedJobId >= id)
                {
                    return true;
                }
                else if (scheduler.jobCounter > id)
                {
                    return false;
                }
                scheduler.rwLock.EnterReadLock();
                var status = scheduler.activeJobs[(id - scheduler.maxCompletedJobId) - 1];
                scheduler.rwLock.ExitReadLock();
                return status;
            }
        }

        private readonly int schedulerId;
        private readonly int id;

        public JobHandle(int schedulerId, int id)
        {
            this.schedulerId = schedulerId;
            this.id = id;
        }

        public void WaitForCompletion()
        {
            var sw = new SpinWait();
            while (!IsCompleted)
            {
                sw.SpinOnce();
            }
        }

        public override bool Equals(object? obj)
        {
            return obj is JobHandle h && Equals(h);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 486187739 + schedulerId;
            hash = hash * 486187739 + id;
            return hash;
        }

        public static bool operator ==(JobHandle left, JobHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(JobHandle left, JobHandle right)
        {
            return !(left == right);
        }

        public bool Equals(JobHandle other)
        {
            return id == other.id && schedulerId == other.schedulerId;
        }
    }
}
