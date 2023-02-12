using Archie.Helpers;

namespace Archie.Relations
{
    //E.g. Child has one parent
    //ChildOf(E1,E2,E3)
    internal struct OneToOneRelation<T> : IComponent<OneToOneRelation<T>> where T : struct, IComponent<T>
    {
        public T RelationData;
        public EntityId TargetEntity;

        public OneToOneRelation(T relationType, EntityId targetEntity)
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
        public EntityId[] TargetEntities;
        Dictionary<EntityId, int> EntityIndexMap;

        public OneToManyRelation(T relationType, EntityId[] targetEntities)
        {
            EntityIndexMap = new();
            RelationData = relationType;
            TargetEntities = targetEntities;
        }

        public void Add(EntityId entity)
        {
            TargetEntities = TargetEntities.GrowIfNeeded(Length, 1);
            TargetEntities[Length++] = entity;
        }

        public void Remove(EntityId entity)
        {
            Remove(EntityIndexMap[entity]);
        }

        public void Remove(int index)
        {
            TargetEntities[index] = TargetEntities[--Length];
        }

        public Span<EntityId> TargetedEntities => new Span<EntityId>(TargetEntities, 0, Length);
    }

    //E.g. Player has many friends and player gives Friend a nickname
    //FriendOf(E1 Klaus, E2 Galea, E3 - no nickname)
    internal struct ManyToManyRelation<T> : IComponent<ManyToManyRelation<T>> where T : struct, IComponent<T>
    {
        public int Length;
        public T[] RelationData;
        public EntityId[] TargetEntities;
        Dictionary<EntityId, int> EntityIndexMap;

        public ManyToManyRelation(T[] relationTypes, EntityId[] targetEntities) : this()
        {
            EntityIndexMap = new();
            RelationData = relationTypes;
            TargetEntities = targetEntities;
        }

        public void Add(EntityId entity, T value)
        {
            EntityIndexMap.Add(entity, Length);
            TargetEntities = TargetEntities.GrowIfNeeded(Length, 1);
            RelationData = RelationData.GrowIfNeeded(Length, 1);
            RelationData[Length] = value;
            TargetEntities[Length++] = entity;
        }

        public void Remove(EntityId entity)
        {
            Remove(EntityIndexMap[entity]);
        }

        public void Remove(int index)
        {
            TargetEntities[index] = TargetEntities[--Length];
            RelationData[index] = RelationData[Length];
        }

        public Span<EntityId> TargetedEntities => new Span<EntityId>(TargetEntities, 0, Length);
        public Span<T> RelationValues => new Span<T>(RelationData, 0, Length);
    }

    //E.g. Player has many friends and player gives Friend a nickname
    //FriendOf(E1 Klaus, E2 Galea, E3 - no nickname)
    internal struct DiscriminatingOneToOneRelation<T> : IComponent<DiscriminatingOneToOneRelation<T>> where T : struct, IComponent<T>
    {
        public T RelationType;

        public DiscriminatingOneToOneRelation(T relationType)
        {
            RelationType = relationType;
        }
    }
}
