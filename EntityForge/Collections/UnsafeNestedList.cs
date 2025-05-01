using EntityForge.Collections.Generic;
using System.Runtime.CompilerServices;

namespace EntityForge.Collections
{
    internal sealed class UnsafeNestedList : IDisposable
    {
        UnsafeSparseSet<UnsafeList> lists = new();

        public int Add<T>(int key, T item)
        {
            ref var list = ref lists.GetOrAdd(key);
            if (list == null)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                list = new UnsafeList(typeof(T), 1);
#pragma warning restore CA2000 // Dispose objects before losing scope
            }

            list.Add(item);
            return list.Count - 1;
        }

        public void Remove<T>(int key, T item)
        {
            ref var list = ref lists.GetRefOrNullRef(key);
            if (Unsafe.IsNullRef(ref list))
            {
                return;
            }

            list.Remove(item);
        }

        public void RemoveAt<T>(int key, int entryLocation)
        {
            ref var list = ref lists.GetRefOrNullRef(key);
            if (Unsafe.IsNullRef(ref list))
            {
                return;
            }

            list.RemoveAt<T>(entryLocation);
        }

        public void RemoveEntry(int key)
        {
            lists.RemoveAt(key);
        }

        public ref UnsafeList GetListOrNullRef(int key)
        {
            return ref lists.GetRefOrNullRef(key);
        }

        public void Dispose()
        {
            lists.Dispose();
        }
    }
}
