using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Helpers
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNullRefrenceException()
        {
            throw new NullReferenceException();
        }
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNullRefrenceException(string? msg)
        {
            throw new NullReferenceException(msg);
        }
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException()
        {
            throw new InvalidOperationException();
        }
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException(string? msg)
        {
            throw new InvalidOperationException(msg);
        }
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentException(string? msg)
        {
            throw new ArgumentException(msg);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowDuplicateComponentException(string? msg)
        {
            throw new DuplicateComponentException(msg);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowMissingComponentException(string? msg)
        {
            throw new MissingComponentException(msg);
        }
    }
}
