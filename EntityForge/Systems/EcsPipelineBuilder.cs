using EntityForge.Helpers;
using System.Runtime.CompilerServices;

namespace EntityForge.Systems
{
    internal class EcsPipelineBuilder
    {
        int systemCount;
        EcsSystem[] systems;
        Dictionary<Type, object> singletons;
        Dictionary<string, object> shared;

        public EcsPipelineBuilder()
        {
            systemCount = 0;
            systems = new EcsSystem[2];
            singletons = new Dictionary<Type, object>();
            shared = new Dictionary<string, object>();
        }

        public EcsPipelineBuilder InjectSingleton<T>(T data) where T : class
        {
            singletons.Add(typeof(T), data);
            return this;
        }

        public EcsPipelineBuilder InjectShared<T>(string name, T data) where T : class
        {
            shared.Add(name, data);
            return this;
        }

        public EcsPipelineBuilder AddSystem<T>() where T : EcsSystem, new()
        {
            systems = systems.GrowIfNeeded(systemCount, 1);
            systems[systemCount++] = (EcsSystem)RuntimeHelpers.GetUninitializedObject(typeof(T));
            return this;
        }

        public EcsSystemPipeline End()
        {
            var pipeline = new EcsSystemPipeline(new ArraySegment<EcsSystem>(this.systems, 0, systemCount).ToArray(), singletons, shared);
            for (int i = 0; i < systemCount; i++)
            {
                var system = systems[i];
                systems[i].pipeline = pipeline;
                //Call constructor of the system
                system.GetType().GetConstructor(Array.Empty<Type>())!.Invoke(null);
            }
            return pipeline;
        }
    }
}
