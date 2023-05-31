using Archie.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Archie.Collections
{
    internal sealed class MultiComponentList : IDisposable
    {
        internal readonly UnsafeSparseSet<UnsafeSparseSet> valuesSet;

        public MultiComponentList()
        {
            valuesSet = new();
        }

        public void Add<T>(int entity, T value) where T : struct, IComponent<T>
        {
            ref var componentSet = ref valuesSet.GetOrAdd(T.Id);
            if (componentSet == null)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    componentSet = new UnsafeSparseSet(typeof(T));
                }
                else
                {
                    componentSet = new UnsafeSparseSet(Unsafe.SizeOf<T>());
                }
#pragma warning restore CA2000 // Dispose objects before losing scope
            }

            componentSet.Add(entity, value);
        }

        public void Remove<T>(int entity) where T : struct, IComponent<T>
        {
            if (valuesSet.Has(T.Id))
            {
                valuesSet.Get(T.Id).RemoveAt<T>(entity);
            }
        }

        public void Remove(int entity, ComponentInfo info)
        {
            if (valuesSet.Has(info.ComponentId.TypeId))
            {
                valuesSet.Get(info.ComponentId.TypeId).RemoveAt(entity, info.UnmanagedSize);
            }
        }

        public void Dispose()
        {
            var arrays = valuesSet.GetDenseData();
            foreach (var array in arrays)
            {
                array.Dispose();
            }
            valuesSet.Dispose();
        }
    }
}
