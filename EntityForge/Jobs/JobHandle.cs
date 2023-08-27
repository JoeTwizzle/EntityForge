namespace EntityForge.Jobs
{
    public readonly struct JobHandle : IEquatable<JobHandle>
    {
        public bool IsCompleted
        {
            get
            {
                //check if our scheduler has been disposed and is oob

                JobThreadScheduler.SchedulersArrayLock.EnterReadLock();
                var scheduler = JobThreadScheduler.Schedulers[schedulerId];
                JobThreadScheduler.SchedulersArrayLock.ExitReadLock();
                if (scheduler == null)
                {
                    return true;
                }
                if (scheduler.Version == version)
                {
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
                return scheduler.Version > version;
            }
        }

        private readonly short schedulerId;
        private readonly short version;
        private readonly int id;

        public JobHandle(short schedulerId, short version, int id)
        {
            this.schedulerId = schedulerId;
            this.version = version;
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
