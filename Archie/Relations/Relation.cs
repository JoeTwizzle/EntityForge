using Archie.Helpers;
using System.Collections.Generic;

namespace Archie.Relations
{
    //E.g. Child has one parent
    //ChildOf(E1,E2,E3)
    internal struct OneToOneRelation<T> : IComponent<OneToOneRelation<T>> where T : struct, IComponent<T>
    {
        public T RelationData;
        public Entity TargetEntity;

        public OneToOneRelation(T relationType, Entity targetEntity)
        {
            RelationData = relationType;
            TargetEntity = targetEntity;
        }
    }

    //E.g. Parent has many Children
    //ParentOf(E1,E2,E3)
    internal struct OneToManyRelation<T> : IComponent<OneToManyRelation<T>> where T : struct, IComponent<T>
    {
        public T RelationData;
        public int Length;
        public Entity[] TargetEntities;
        public Dictionary<Entity, int> EntityIndexMap;

        public OneToManyRelation(T relationType, Entity targetEntity)
        {
            EntityIndexMap = new();
            RelationData = relationType;
            EntityIndexMap.Add(targetEntity, Length);
            TargetEntities = new Entity[1] { targetEntity };
            Length = 1;
        }

        public void Add(Entity entity)
        {
            EntityIndexMap.Add(entity, Length);
            TargetEntities = TargetEntities.GrowIfNeeded(Length, 1);
            TargetEntities[Length++] = entity;
        }

        public void Remove(Entity entity)
        {
            Remove(EntityIndexMap[entity]);
        }

        public void Remove(int index)
        {
            EntityIndexMap.Remove(TargetEntities[index]);
            TargetEntities[index] = TargetEntities[Length - 1];
        }

        public ReadOnlySpan<Entity> TargetedEntities => new ReadOnlySpan<Entity>(TargetEntities, 0, Length);
    }

    //E.g. Player has many friends and player gives Friend a nickname
    //FriendOf(E1 Klaus, E2 Galea, E3 - no nickname)
    internal struct ManyToManyRelation<T> : IComponent<ManyToManyRelation<T>> where T : struct, IComponent<T>
    {
        public int Length;
        public T[] RelationData;
        public Entity[] TargetEntities;
        public Dictionary<Entity, int> EntityIndexMap;

        public ManyToManyRelation(T relationType, Entity targetEntity)
        {
            EntityIndexMap = new();
            EntityIndexMap.Add(targetEntity, Length);
            RelationData = new T[1] { relationType };
            TargetEntities = new Entity[1] { targetEntity };
            Length = 1;
        }

        public void Add(Entity entity, T value)
        {
            EntityIndexMap.Add(entity, Length);
            TargetEntities = TargetEntities.GrowIfNeeded(Length, 1);
            RelationData = RelationData.GrowIfNeeded(Length, 1);
            RelationData[Length] = value;
            TargetEntities[Length++] = entity;
        }

        public void Remove(Entity entity)
        {
            Remove(EntityIndexMap[entity]);
        }

        public void Remove(int index)
        {
            EntityIndexMap.Remove(TargetEntities[index]);
            TargetEntities[index] = TargetEntities[--Length];
            RelationData[index] = RelationData[Length];
        }

        public ReadOnlySpan<Entity> TargetedEntities => new ReadOnlySpan<Entity>(TargetEntities, 0, Length);
        public Span<T> RelationValues => new Span<T>(RelationData, 0, Length);
    }

    //E.g. Player has many friends and player gives Friend a nickname
    //FriendOf(E1 Klaus, E2 Galea, E3 - no nickname)
    internal struct DiscriminatingOneToOneRelation<T> : IComponent<DiscriminatingOneToOneRelation<T>> where T : struct, IComponent<T>
    {
        public T RelationData;
        public Entity Entity;

        public DiscriminatingOneToOneRelation(T relationType, Entity entity)
        {
            RelationData = relationType;
            Entity = entity;
        }
    }
}
