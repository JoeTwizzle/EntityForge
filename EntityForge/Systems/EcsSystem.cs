using System.Runtime.CompilerServices;

namespace EntityForge.Systems
{
    public abstract class EcsSystem
    {
        public bool IsEnabled;

        internal EcsSystemPipeline pipeline
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
