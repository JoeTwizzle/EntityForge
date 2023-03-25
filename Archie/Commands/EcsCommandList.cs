using Archie.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Commands
{
    readonly struct CommandListEntity
    {
        public readonly int Id;

        public CommandListEntity(int id)
        {
            Id = id;
        }
    }
    internal sealed class EcsCommandList
    {
        ArchetypeBuilder[] builders;
        int componentCount;
        int entityCount;
        World World;
        EntityId[] Entities;
        (int Count, (int Variant, object Component)[] Data)[] componentData;

        public EcsCommandList(World world)
        {
            Entities = Array.Empty<EntityId>();
            World = world;
            builders = new ArchetypeBuilder[2];
            componentData = Array.Empty<(int Count, (int Variant, object Component)[] Data)>();
        }

        public void Begin()
        {
            componentCount = 0;
            entityCount = 0;
        }

        public void End()
        {
            if (Entities.Length < entityCount)
            {
                int newSize = Entities.Length * 2;

                do
                {
                    newSize *= 2;
                }
                while (newSize < entityCount);

                Array.Resize(ref Entities, newSize);
            }

        }

        public CommandListEntity CreateEntity()
        {
            return CreateEntity(ArchetypeDefinition.Empty);
        }

        public CommandListEntity CreateEntity(ArchetypeDefinition definition)
        {
            componentData = componentData.GrowIfNeeded(entityCount, 1);
            componentData[entityCount] = (0, Array.Empty<(int, object)>());
            builders = builders.GrowIfNeeded(entityCount, 1);
            builders[entityCount] = new ArchetypeBuilder(definition);
            int ent = entityCount++;
            return new CommandListEntity(ent);
        }

        public void AddComponent<T>(CommandListEntity entity, int variant = 0) where T : struct, IComponent<T>
        {
            builders[entity.Id] = builders[entity.Id].Inc<T>(variant);
        }

        public void AddComponent<T>(CommandListEntity entity, T value, int variant = 0) where T : struct, IComponent<T>
        {
            builders[entity.Id] = builders[entity.Id].Inc<T>(variant);
            var components = componentData[entity.Id];
            components.Data = components.Data.GrowIfNeeded(components.Count, 1);
            components.Data[components.Count++] = (variant, value);
            componentData[entity.Id] = components;
        }


        ReadOnlySpan<EntityId> Execute()
        {
            for (int i = 0; i < entityCount; i++)
            {
                var ent = World.CreateEntity(builders[i].End());
                Entities[i] = ent;
                var components = componentData[i];
                for (int j = 0; j < components.Count; j++)
                {
                    var info = components.Data[j];
                    World.SetComponent(ent, info.Component, info.Variant);
                }
            }
            return new ReadOnlySpan<EntityId>(Entities, 0, entityCount);
        }
    }
}
