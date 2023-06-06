using System.Runtime.CompilerServices;

namespace EntityForge.Systems
{
    public abstract class EcsSystem
    {
        public bool IsEnabled;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        internal EcsSystemPipeline pipeline
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetSingleton<T>() where T : class
        {
            return pipeline.GetSingleton<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetShared<T>(string identifier) where T : class
        {
            return pipeline.GetShared<T>(identifier);
        }

        public abstract void Execute();
    }
}
