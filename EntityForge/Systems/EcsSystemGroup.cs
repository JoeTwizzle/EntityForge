using EntityForge.Helpers;
using System.Runtime.CompilerServices;

namespace EntityForge.Systems
{
    public sealed class EcsSystemGroup
    {
        int systemCount;
        EcsSystem[] systems;

        public EcsSystemGroup()
        {
            systemCount = 0;
            systems = new EcsSystem[2];
        }

        public EcsSystemGroup AddSystem<T>() where T : EcsSystem, new()
        {
            systems = systems.GrowIfNeeded(systemCount, 1);
            systems[systemCount++] = (EcsSystem)RuntimeHelpers.GetUninitializedObject(typeof(T));
            return this;
        }

        internal void InitSystems(EcsSystemPipeline pipeline)
        {
            for (int i = 0; i < systemCount; i++)
            {
                var system = systems[i];
                systems[i].pipeline = pipeline;
                //Call constructor of the system
                system.GetType().GetConstructor(Array.Empty<Type>())!.Invoke(null);
            }
        }
    }
}
