using EntityForge.Collections.Generic;
using System.Runtime.CompilerServices;

namespace EntityForge.Collections
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

        public bool Has<T>(int entity) where T : struct, IComponent<T>
        {
            if (valuesSet.TryGetValue(T.Id, out var componentsSet))
            {
                return componentsSet.Has(entity);
            }
            return false;
        }

        public bool Has(int entity, int typeId)
        {
            if (valuesSet.TryGetValue(typeId, out var componentsSet))
            {
                return componentsSet.Has(entity);
            }
            return false;
        }

        public void Remove<T>(int entity) where T : struct, IComponent<T>
        {
            if (valuesSet.TryGetValue(T.Id, out var componentsSet))
            {
                componentsSet.RemoveAt<T>(entity);
            }
        }

        public void Remove(int entity, ComponentInfo info)
        {
            if (valuesSet.TryGetValue(info.TypeId, out var componentsSet))
            {
                componentsSet.RemoveAt(entity, info.UnmanagedSize);
            }
        }

        public void ClearValues()
        {
            var sets = valuesSet.GetDenseData();
            for (int i = 0; i < sets.Length; i++)
            {
                sets[i]?.ClearManaged();
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
