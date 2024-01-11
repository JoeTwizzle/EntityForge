using System.Runtime.CompilerServices;

namespace EntityForge
{
    public readonly struct ComponentInfo : IEquatable<int>, IEquatable<ComponentInfo>
    {
        public readonly bool IsUnmanaged => UnmanagedSize != 0;

        public readonly int TypeId;
        public readonly int UnmanagedSize;
        public readonly Type Type;

        public ComponentInfo(int typeId, Type type)
        {
            TypeId = typeId;
            UnmanagedSize = 0;
            Type = type;
        }

        public ComponentInfo(int typeId, int unmanagedSize, Type type)
        {
            TypeId = typeId;
            UnmanagedSize = unmanagedSize;
            Type = type;
        }

        public override bool Equals(object? obj)
        {
            return obj is int c && Equals(c);
        }

        
        public override int GetHashCode()
        {
            return TypeId;
        }

        public static bool operator ==(ComponentInfo left, int right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentInfo left, int right)
        {
            return !(left == right);
        }

        public static bool operator ==(int left, ComponentInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(int left, ComponentInfo right)
        {
            return !(left == right);
        }

        public bool Equals(int other)
        {
            return TypeId == other;
        }

        public static bool operator ==(ComponentInfo left, ComponentInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentInfo left, ComponentInfo right)
        {
            return !(left == right);
        }

        public bool Equals(ComponentInfo other)
        {
            return TypeId == other.TypeId;
        }

        public static void SortTypes(Span<ComponentInfo> componentTypes)
        {
            componentTypes.Sort((x, y) =>
            {
                return x.TypeId > y.TypeId ? 1 : (x.TypeId < y.TypeId ? -1 : 0);
            });
        }

        public static int GetComponentHash(Span<ComponentInfo> componentTypes)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < componentTypes.Length; i++)
                {
                    hash = hash * 486187739 + componentTypes[i].TypeId;
                }
                return hash;
            }
        }

        public static ComponentInfo[] RemoveDuplicates(ComponentInfo[] types)
        {
            int head = 0;
            Span<int> indices = types.Length < 32 ? stackalloc int[32] : new int[types.Length];
            if (types.Length > 0)
            {
                ComponentInfo prevType = types[0];
                indices[head++] = 0;
                for (int i = 1; i < types.Length; i++)
                {
                    //This only works if the sparseArray is sorted
                    if (prevType == types[i])
                    {
                        continue;
                    }
                    indices[head++] = i;
                    prevType = types[i];
                }
            }
            //Contained no duplicates
            if (head == types.Length)
            {
                return types;
            }
            var deDup = new ComponentInfo[head];
            for (int i = 0; i < deDup.Length; i++)
            {
                deDup[i] = types[indices[--head]];
            }
            return deDup;
        }
    }
}
