using Archie.Helpers;
using System.Runtime.CompilerServices;

namespace Archie.Systems
{
    public sealed class EcsSystemPipeline
    {
        public readonly EcsSystem[] Systems;
        private Dictionary<Type, object> singletons;
        private Dictionary<string, object> shared;

        public EcsSystemPipeline(EcsSystem[] systems, Dictionary<Type, object> singletons, Dictionary<string, object> shared)
        {
            Systems = systems;
            this.singletons = singletons;
            this.shared = shared;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetSingleton<T>() where T : class
        {
            return (T)singletons[typeof(T)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetShared<T>(string identifier) where T : class
        {
            return (T)shared[identifier];
        }

        public void Execute()
        {
            for (int i = 0; i < Systems.Length; i++)
            {
                var system = Systems[i];
                if (system.IsEnabled)
                {
                    Systems[i].Execute();
                }
            }
        }
    }
}
