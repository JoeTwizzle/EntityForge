using System.Runtime.CompilerServices;

namespace EntityForge
{
    public readonly struct EntityId : IEquatable<EntityId>, IEquatable<Entity>
    {
        public readonly int Id;

        
        public EntityId(int id)
        {
            Id = id;
        }

        public override bool Equals(object? obj)
        {
            return obj is EntityId e && Equals(e);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public static bool operator ==(EntityId left, EntityId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityId left, EntityId right)
        {
            return !(left == right);
        }

        public bool Equals(EntityId other)
        {
            return Id == other.Id;
        }

        public static EntityId ToEntityId(int id)
        {
            return new EntityId(id);
        }

        public bool Equals(Entity other)
        {
            return Id == other.EntityId.Id;
        }
    }
}
