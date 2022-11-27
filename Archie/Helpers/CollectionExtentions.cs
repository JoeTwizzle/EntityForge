using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Helpers
{
    internal static class CollectionExtentions
    {
        public static ref TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TKey : notnull
        {
            return ref CollectionsMarshal.GetValueRefOrNullRef(dict, key);
        }

        public static ref T Get<T>(this T[] arr, int i)
        {
            return ref arr[i];
        }
    }
}
