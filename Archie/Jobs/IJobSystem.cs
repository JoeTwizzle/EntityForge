namespace Archie.Jobs
{
    public interface IJobSystem<T> where T : struct, IJobSystem<T>
    {
        void Execute(Archetype archetype);
    }
}
