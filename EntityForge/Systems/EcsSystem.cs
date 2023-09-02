using System.Runtime.CompilerServices;

namespace EntityForge.Systems
{
    public abstract class EcsSystem
    {
        public bool IsEnabled;
        private EcsSystemPipeline _pipeline = null!;

        internal EcsSystemPipeline pipeline
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _pipeline;
            set => _pipeline = value;
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
