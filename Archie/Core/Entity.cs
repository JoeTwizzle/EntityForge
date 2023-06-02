using System.Runtime.CompilerServices;

namespace Archie
{
    public readonly struct Entity : IEquatable<Entity>, IEquatable<EntityId>
    {
        public readonly EntityId EntityId;
        public readonly int WorldId;

        public Entity(int id, int worldId)
        {
            EntityId = new(id);
            WorldId = worldId;
        }

        public World World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return World.Worlds[WorldId];
            }
        }

        public bool IsAlive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return World.Worlds[WorldId].IsAlive(EntityId);
            }
        }

        public Archetype Archetype
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return World.Worlds[WorldId].GetArchetype(EntityId);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Destroy()
        {
            World.DestroyEntity(EntityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>() where T : struct, IComponent<T>
        {
            World.AddComponent<T>(EntityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(T value) where T : struct, IComponent<T>
        {
            World.AddComponent<T>(EntityId, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>() where T : struct, IComponent<T>
        {
            World.RemoveComponent<T>(EntityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent<T>() where T : struct, IComponent<T>
        {
            World.SetComponent<T>(EntityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent<T>(T value) where T : struct, IComponent<T>
        {
            World.SetComponent<T>(EntityId, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsetComponent<T>() where T : struct, IComponent<T>
        {
            World.UnsetComponent<T>(EntityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>() where T : struct, IComponent<T>
        {
            return ref World.GetComponent<T>(EntityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>() where T : struct, IComponent<T>
        {
            return World.HasComponent<T>(EntityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(int typeId)
        {
            return World.HasComponent(EntityId, typeId);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool HasRelation<T>() where T : struct, IComponent<T>, IComponent<T>
        //{
        //    return World.HasRelation<T>(Id);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool HasRelation<T>(EntityId targetInternal) where T : struct, IComponent<T>, IComponent<T>
        //{
        //    return World.HasRelation<T>(Id, targetInternal);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void AddRelationTarget<T>(EntityId targetInternal) where T : struct, IComponent<T>, IComponent<T>
        //{
        //    World.AddRelationTarget<T>(Id, targetInternal);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void AddRelationTarget<T>(EntityId targetInternal, T value) where T : struct, IComponent<T>, IComponent<T>
        //{
        //    World.AddRelationTarget<T>(Id, targetInternal, value);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void RemoveRelationTarget<T>(EntityId targetInternal) where T : struct, IComponent<T>, IComponent<T>
        //{
        //    World.RemoveRelationTarget<T>(Id, targetInternal);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public ref T GetTreeRelationData<T>(EntityId targetInternal) where T : struct, IComponent<T>, IComponent<T>
        //{
        //    return ref World.GetTreeRelationData<T>(Id, targetInternal);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public Span<T> GetTreeRelationData<T>() where T : struct, IComponent<T>, IComponent<T>
        //{
        //    return World.GetTreeRelationData<T>(Id);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public Entity GetRelationTarget<T>() where T : struct, IComponent<T>, IComponent<T>
        //{
        //    return World.GetRelationTarget<T>(Id);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public ReadOnlySpan<Entity> GetRelationTargets<T>() where T : struct, IComponent<T>, IComponent<T>
        //{
        //    return World.GetRelationTargets<T>(Id);
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj)
        {
            return obj is Entity e && Equals(e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 486187739 + EntityId.Id;
            hash = hash * 486187739 + WorldId;
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EntityId(Entity e)
        {
            return e.EntityId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Entity left, Entity right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Entity left, Entity right)
        {
            return !(left == right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Entity other)
        {
            return EntityId == other.EntityId && WorldId == other.WorldId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(EntityId other)
        {
            return other.Id == EntityId.Id;
        }

        public EntityId ToEntityId()
        {
            return EntityId;
        }
    }
}
