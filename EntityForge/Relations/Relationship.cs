//using EntityForge.Collections;
//using EntityForge.Relations;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace EntityForge
//{
//    partial class World
//    {
//        readonly ArchetypeDefinition singleTarget = ArchetypeDefinition.Create().Inc<RelationShipBearer>().End();

//        public void AddRelationship(EntityId self, EntityId target)
//        {
//            CreateEntity(singleTarget);
//        }
//    }
//}

//namespace EntityForge.Relations
//{
//    internal struct RelationShipBearer : IComponent<RelationShipBearer>
//    {
//        public EntityId Target;
//    }

//    internal struct RelationShipBearer2 : IComponent<RelationShipBearer2>
//    {
//        public UnsafeList<EntityId> Targets;
//    }

//    internal struct RelationShipBearer3<T> : IComponent<RelationShipBearer3<T>> where T : struct, IComponent<T>
//    {
//        public EntityId Target;
//    }

//    internal struct RelationShipBearer4<T> : IComponent<RelationShipBearer4<T>> where T : struct, IComponent<T>
//    {
//        public UnsafeList<EntityId> Targets;
//    }



//    public readonly struct Relationship : IEquatable<Relationship>
//    {
//        public readonly Entity Entity;

//        public override bool Equals(object? obj)
//        {
//            return obj is Relationship r && Equals(r);
//        }

//        public override int GetHashCode()
//        {
//            return Entity.GetHashCode();
//        }

//        public static bool operator ==(Relationship left, Relationship right)
//        {
//            return left.Equals(right);
//        }

//        public static bool operator !=(Relationship left, Relationship right)
//        {
//            return !(left == right);
//        }

//        public bool Equals(Relationship other)
//        {
//            return Entity.Equals(other.Entity);
//        }
//    }

//    public readonly struct RelationshipId : IEquatable<RelationshipId>
//    {
//        public readonly EntityId EntityId;

//        public override bool Equals(object? obj)
//        {
//            return obj is RelationshipId r && Equals(r);
//        }

//        public override int GetHashCode()
//        {
//            return EntityId.GetHashCode();
//        }

//        public static bool operator ==(RelationshipId left, RelationshipId right)
//        {
//            return left.Equals(right);
//        }

//        public static bool operator !=(RelationshipId left, RelationshipId right)
//        {
//            return !(left == right);
//        }

//        public bool Equals(RelationshipId other)
//        {
//            return EntityId.Equals(other.EntityId);
//        }
//    }
//}
