using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge.Collections
{
    public sealed class EntityCollection : IEnumerable<Entity>
    {
        public readonly World World;
        public readonly BitMask PresentEntities = new();

        public EntityCollection(World world)
        {
            World = world;
        }

        public void Add(int entity)
        {
            PresentEntities.SetBit(entity);
        }

        public void Remove(int entity)
        {
            PresentEntities.ClearBit(entity);
        }

        public void AddRange(int first, int count)
        {
            PresentEntities.SetRange(first, count);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        //This should be a moveonly struct once possible
        public struct Enumerator : IEnumerator<Entity>
        {
            EntityCollection _entityCollection;
            int entityId;
            int idx;
            long bitItem;
            public Enumerator(EntityCollection entityCollection)
            {
                bitItem = 0;
                idx = 0;
                entityId = 0;
                _entityCollection = entityCollection;
            }

            public Entity Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return _entityCollection.World.GetEntity(new EntityId(entityId));
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                var bits = _entityCollection.PresentEntities.Bits;
                if (bitItem == 0 && idx < bits.Length)
                {
                    bitItem = bits[idx];
                    idx++;
                }
                if (bitItem != 0)
                {
                    entityId = idx * sizeof(ulong) * 8 + BitOperations.TrailingZeroCount(bitItem);
                    bitItem ^= bitItem & -bitItem;
                    return true;
                }
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}
