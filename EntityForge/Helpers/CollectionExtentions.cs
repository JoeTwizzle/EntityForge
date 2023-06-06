using System.Runtime.InteropServices;

namespace EntityForge.Helpers
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
