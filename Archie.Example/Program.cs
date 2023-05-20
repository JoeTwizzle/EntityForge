
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Archie.Example
{
    internal class Program
    {
        static World world;
        static ArchetypeDefinition archetypeC0 = ArchetypeBuilder.Create().End();
        static ArchetypeDefinition archetypeC1 = ArchetypeBuilder.Create().Inc<Component1>().End();
        static ArchetypeDefinition archetypeC2 = ArchetypeBuilder.Create().Inc<Component2>().End();
        static ArchetypeDefinition archetypeC3 = ArchetypeBuilder.Create().Inc<Component3>().End();
        static ArchetypeDefinition archetypeC1C2 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component2>().End();
        static ArchetypeDefinition archetypeC1C3 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component3>().End();
        static ArchetypeDefinition archetypeC2C3 = ArchetypeBuilder.Create().Inc<Component2>().Inc<Component3>().End();
        static ArchetypeDefinition archetypeC1C2C3 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component2>().Inc<Component3>().End();
        static void Main(string[] args)
        {
            AddManyTest();
        }
        struct Component1 : IComponent<Component1>
        {
            public int Value;
        }

        public struct Component2 : IComponent<Component2>
        {
            public int Value;
        }

        public struct Component3 : IComponent<Component3>
        {
            public int Value;
        }

        static EntityId[] InitMany(int count)
        {
            EntityId[] entites;
            entites = new EntityId[iterations];
            world = new World();
            //world.ReserveEntities(archetypeC1C2, iterations);
            for (int i = 0; i < iterations; i++)
            {
                entites[i] = world.CreateEntity(archetypeC1C2);
            }
            return entites;
        }

        static int iterations = 100000;

        public static void AddManyTest()
        {
            var ents = InitMany(iterations);

            for (int i = 0; i < iterations; i++)
            {
                world.AddComponent<Component3>(ents[i]);
            }
        }
    }
}