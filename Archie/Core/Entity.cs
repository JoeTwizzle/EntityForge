﻿using System.Runtime.CompilerServices;

namespace Archie
{
    public readonly struct Entity : IEquatable<Entity>, IEquatable<EntityId>
    {
        public readonly int Id;
        public readonly int WorldId;

        [SkipLocalsInit]
        public Entity(int id, int worldId)
        {
            Id = id;
            WorldId = worldId;
        }

        public World World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return World.worlds[WorldId];
            }
        }

        public bool IsAlive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return World.worlds[WorldId].IsAlive(ToEntityId());
            }
        }

        public Archetype Archetype
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return World.worlds[WorldId].GetArchetype(ToEntityId());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>() where T : struct, IComponent<T>
        {
            World.AddComponent<T>(ToEntityId());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(T value) where T : struct, IComponent<T>
        {
            World.AddComponent<T>(ToEntityId(), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>() where T : struct, IComponent<T>
        {
            World.RemoveComponent<T>(ToEntityId());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent<T>() where T : struct, IComponent<T>
        {
            World.SetComponent<T>(ToEntityId());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent<T>(T value) where T : struct, IComponent<T>
        {
            World.SetComponent<T>(ToEntityId(), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsetComponent<T>() where T : struct, IComponent<T>
        {
            World.UnsetComponent<T>(ToEntityId());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>() where T : struct, IComponent<T>
        {
            return ref World.GetComponent<T>(ToEntityId());
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool HasRelation<T>() where T : struct, IComponent<T>, ITreeRelation<T>
        //{
        //    return World.HasRelation<T>(ToEntityId());
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool HasRelation<T>(EntityId targetInternal) where T : struct, IComponent<T>, ITreeRelation<T>
        //{
        //    return World.HasRelation<T>(ToEntityId(), targetInternal);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void AddRelationTarget<T>(EntityId targetInternal) where T : struct, IComponent<T>, ITreeRelation<T>
        //{
        //    World.AddRelationTarget<T>(ToEntityId(), targetInternal);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void AddRelationTarget<T>(EntityId targetInternal, T value) where T : struct, IComponent<T>, ITreeRelation<T>
        //{
        //    World.AddRelationTarget<T>(ToEntityId(), targetInternal, value);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void RemoveRelationTarget<T>(EntityId targetInternal) where T : struct, IComponent<T>, ITreeRelation<T>
        //{
        //    World.RemoveRelationTarget<T>(ToEntityId(), targetInternal);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public ref T GetTreeRelationData<T>(EntityId targetInternal) where T : struct, IComponent<T>, ITreeRelation<T>
        //{
        //    return ref World.GetTreeRelationData<T>(ToEntityId(), targetInternal);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public Span<T> GetTreeRelationData<T>() where T : struct, IComponent<T>, ITreeRelation<T>
        //{
        //    return World.GetTreeRelationData<T>(ToEntityId());
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public Entity GetRelationTarget<T>() where T : struct, IComponent<T>, ITreeRelation<T>
        //{
        //    return World.GetRelationTarget<T>(ToEntityId());
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public ReadOnlySpan<Entity> GetRelationTargets<T>() where T : struct, IComponent<T>, ITreeRelation<T>
        //{
        //    return World.GetRelationTargets<T>(ToEntityId());
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
            hash = hash * 486187739 + Id;
            hash = hash * 486187739 + WorldId;
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EntityId(Entity e)
        {
            return new EntityId(e.Id);
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
            return Id == other.Id && WorldId == other.WorldId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityId ToEntityId()
        {
            return new EntityId(Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(EntityId other)
        {
            return other.Id == Id;
        }
    }
}
