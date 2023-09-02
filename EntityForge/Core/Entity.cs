using EntityForge.Tags;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EntityForge
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct Entity : IEquatable<Entity>, IEquatable<EntityId>
    {
        [FieldOffset(0)]
        public readonly EntityId EntityId;
        [FieldOffset(4)]
        public readonly short WorldId;
        [FieldOffset(6)]
        public readonly short Version;

        public Entity(int id, short version, short worldId)
        {
            EntityId = new(id);
            Version = version;
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
                return World.Worlds[WorldId].IsAlive(this);
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
        public void Delete()
        {
            World.DeleteEntity(EntityId);
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
        public ref T GetComponentOrNullRef<T>() where T : struct, IComponent<T>
        {
            return ref World.GetComponentOrNullRef<T>(EntityId);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTag<T>() where T : struct, ITag<T>
        {
            World.AddTag<T>(EntityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTag<T>() where T : struct, ITag<T>
        {
            World.SetTag<T>(EntityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsetTag<T>() where T : struct, ITag<T>
        {
            World.UnsetTag<T>(EntityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveTag<T>() where T : struct, ITag<T>
        {
            World.RemoveTag<T>(EntityId);
        }

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
            hash = hash * 486187739 + Version;
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
            return WorldId == other.WorldId && EntityId == other.EntityId && Version == other.Version;
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
