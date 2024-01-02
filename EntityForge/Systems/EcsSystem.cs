using System.Runtime.CompilerServices;

namespace EntityForge.Systems
{
    public abstract class EcsSystem
    {
        public bool IsEnabled;
        private EcsSystemPipeline _pipeline = null!;

        internal EcsSystemPipeline pipeline
        {
            
            get => _pipeline;
            set => _pipeline = value;
        }

        
        public T GetSingleton<T>() where T : class
        {
            return pipeline.GetSingleton<T>();
        }

        
        public T GetShared<T>(string identifier) where T : class
        {
            return pipeline.GetShared<T>(identifier);
        }

        public abstract void Execute();
    }
}
