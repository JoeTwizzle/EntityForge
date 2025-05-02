
using EntityForge.Collections;
using EntityForge.Tags;
using System.Numerics;
using static EntityForge.Commands.OperationBuffer;

namespace EntityForge
{
    public readonly struct ArchetypeDefinition : IEquatable<ArchetypeDefinition>
    {
        public static ArchetypeDefinition Empty => World.s_emptyArchetypeDefinition;
        public readonly int HashCode;
        public readonly ReadOnlyMemory<ComponentInfo> ComponentInfos;

        internal ArchetypeDefinition(int hashCode, ReadOnlyMemory<ComponentInfo> componentInfos)
        {
            HashCode = hashCode;
            ComponentInfos = componentInfos;
        }

        internal static ArchetypeDefinition FromMask(BitMask mask)
        {
            int componentCount = 0;
            var bits = mask.Bits;
            for (int i = 0; i < bits.Length; i++)
            {
                componentCount += BitOperations.PopCount(bits[i]);
            }
            var components = new ComponentInfo[componentCount];
            int index = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                long tagBitItem = (long)bits[i];
                while (tagBitItem != 0)
                {
                    int bitIndex = i * (sizeof(ulong) * 8) + BitOperations.TrailingZeroCount(tagBitItem);

                    components[index++] = World.GetComponentInfo(bitIndex);

                    tagBitItem ^= tagBitItem & -tagBitItem;
                }
            }
            ComponentInfo.SortTypes(components);
            return new ArchetypeDefinition(ComponentInfo.GetComponentHash(components), components);
        }

        public static ArchetypeBuilder Create()
        {
            return new ArchetypeBuilder(Array.Empty<ComponentInfo>());
        }

        public override bool Equals(object? obj)
        {
            return obj is ArchetypeDefinition a && Equals(a);
        }

        public override int GetHashCode()
        {
            return HashCode;
        }

        public static bool operator ==(ArchetypeDefinition left, ArchetypeDefinition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ArchetypeDefinition left, ArchetypeDefinition right)
        {
            return !(left == right);
        }

        public bool Equals(ArchetypeDefinition other)
        {
            return other.HashCode == HashCode && ComponentInfos.Span.SequenceEqual(other.ComponentInfos.Span);
        }
    }
}
