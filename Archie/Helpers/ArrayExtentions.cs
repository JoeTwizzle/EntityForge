using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Helpers
{
    internal static class ArrayExtentions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static T[] GrowIfNeeded<T>(this T[] array, uint filled, uint added)
        {
            uint sum = filled + added;
            int length = array.Length;
            if (length < sum)
            {
                //Grow by 2x
                //Keep doubling size if we grow by a large amount
                do
                {
                    length *= 2;

                } while (length < sum);
               
                if (length > Array.MaxLength) length = Array.MaxLength;

                Array.Resize(ref array, length);
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static Array GrowIfNeeded(this Array array, Type elementType, uint filled, uint added)
        {
            uint sum = filled + added;
            uint length = (uint)array.Length;
            if (length < sum)
            {
                var old = array;
                //Grow by 2x
                uint newCapacity = length * 2;
                //Keep doubling size if we grow by a large amount
                while (newCapacity < sum)
                {
                    newCapacity *= 2u;
                }

                if (newCapacity > Array.MaxLength) newCapacity = (uint)Array.MaxLength;

                var newPool = Array.CreateInstance(elementType, newCapacity);
                array = newPool;
                //move existing entities
                Array.Copy(old, 0, newPool, 0, filled);
            }
            return array;
        }
    }
}
