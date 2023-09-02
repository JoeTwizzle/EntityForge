using EntityForge.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge.Collections
{
    internal sealed class UnsafeNestedList:IDisposable
    {
        UnsafeSparseSet<UnsafeList> lists = new();

        public void Add<T>(int key, T item)
        {
            ref var list = ref lists.GetOrAdd(key);
            if (list == null)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                list = new UnsafeList(typeof(T), 1);
#pragma warning restore CA2000 // Dispose objects before losing scope
            }

            list.Add(item);
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
