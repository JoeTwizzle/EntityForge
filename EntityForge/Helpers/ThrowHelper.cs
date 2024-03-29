﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace EntityForge.Helpers
{
    internal static class ThrowHelper
    {
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
        internal static void ThrowArrayTypeMismatchException()
        {
            throw new ArrayTypeMismatchException();
        }

        internal static void ThrowArgumentOutOfRangeException()
        {
            throw new ArgumentOutOfRangeException();
        }

        internal static void ThrowArgumentException_DestinationTooShort()
        {
            throw new ArgumentException("Destination too short.");
        }

        internal static void ThrowArgumentException_OverlapAlignmentMismatch()
        {
            throw new ArgumentException("Overlap alignment mismatch.");
        }

        internal static void ThrowArgumentOutOfRange_IndexException()
        {
            throw GetArgumentOutOfRangeException(ExceptionArgument.index,
                                                    ExceptionResource.ArgumentOutOfRange_Index);
        }

        internal static void ThrowIndexArgumentOutOfRange_NeedNonNegNumException()
        {
            throw GetArgumentOutOfRangeException(ExceptionArgument.index,
                                                    ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
        }

        internal static void ThrowValueArgumentOutOfRange_NeedNonNegNumException()
        {
            throw GetArgumentOutOfRangeException(ExceptionArgument.value,
                                                    ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
        }

        internal static void ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum()
        {
            throw GetArgumentOutOfRangeException(ExceptionArgument.length,
                                                    ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
        }

        internal static void ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index()
        {
            throw GetArgumentOutOfRangeException(ExceptionArgument.startIndex,
                                                    ExceptionResource.ArgumentOutOfRange_Index);
        }

        internal static void ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count()
        {
            throw GetArgumentOutOfRangeException(ExceptionArgument.count,
                                                    ExceptionResource.ArgumentOutOfRange_Count);
        }

        internal static void ThrowWrongKeyTypeArgumentException<T>(T key, Type targetType)
        {
            // Generic key to move the boxing to the right hand side of throw
            throw GetWrongKeyTypeArgumentException((object?)key, targetType);
        }

        internal static void ThrowWrongValueTypeArgumentException<T>(T value, Type targetType)
        {
            // Generic key to move the boxing to the right hand side of throw
            throw GetWrongValueTypeArgumentException((object?)value, targetType);
        }

        private static ArgumentException GetAddingDuplicateWithKeyArgumentException(object? key)
        {
            return new ArgumentException($"Error adding duplicate with key: {key}.");
        }

        internal static void ThrowAddingDuplicateWithKeyArgumentException<T>(T key)
        {
            // Generic key to move the boxing to the right hand side of throw
            throw GetAddingDuplicateWithKeyArgumentException((object?)key);
        }

        internal static void ThrowKeyNotFoundException<T>(T? key)
        {
            // Generic key to move the boxing to the right hand side of throw
            throw GetKeyNotFoundException((object?)key);
        }

        internal static void ThrowArgumentException(ExceptionResource resource)
        {
            throw GetArgumentException(resource);
        }

        internal static void ThrowArgumentException(ExceptionResource resource, ExceptionArgument argument)
        {
            throw GetArgumentException(resource, argument);
        }

        private static ArgumentNullException GetArgumentNullException(ExceptionArgument argument)
        {
            return new ArgumentNullException(GetArgumentName(argument));
        }
        [DoesNotReturn]
        internal static void ThrowArgumentNullException(ExceptionArgument argument)
        {
            throw GetArgumentNullException(argument);
        }

        internal static void ThrowArgumentNullException(ExceptionResource resource)
        {
            throw new ArgumentNullException(GetResourceString(resource));
        }

        internal static void ThrowArgumentNullException(ExceptionArgument argument, ExceptionResource resource)
        {
            throw new ArgumentNullException(GetArgumentName(argument), GetResourceString(resource));
        }

        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument)
        {
            throw new ArgumentOutOfRangeException(GetArgumentName(argument));
        }

        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument, ExceptionResource resource)
        {
            throw GetArgumentOutOfRangeException(argument, resource);
        }

        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument, int paramNumber, ExceptionResource resource)
        {
            throw GetArgumentOutOfRangeException(argument, paramNumber, resource);
        }

        internal static void ThrowInvalidOperationException(ExceptionResource resource)
        {
            throw GetInvalidOperationException(resource);
        }

        internal static void ThrowInvalidOperationException(ExceptionResource resource, Exception e)
        {
            throw new InvalidOperationException(GetResourceString(resource), e);
        }

        internal static void ThrowSerializationException(ExceptionResource resource)
        {
            throw new SerializationException(GetResourceString(resource));
        }

        internal static void ThrowSecurityException(ExceptionResource resource)
        {
            throw new System.Security.SecurityException(GetResourceString(resource));
        }

        internal static void ThrowRankException(ExceptionResource resource)
        {
            throw new RankException(GetResourceString(resource));
        }

        internal static void ThrowNotSupportedException(ExceptionResource resource)
        {
            throw new NotSupportedException(GetResourceString(resource));
        }

        internal static void ThrowUnauthorizedAccessException(ExceptionResource resource)
        {
            throw new UnauthorizedAccessException(GetResourceString(resource));
        }

        internal static void ThrowObjectDisposedException(string objectName, ExceptionResource resource)
        {
            throw new ObjectDisposedException(objectName, GetResourceString(resource));
        }

        internal static void ThrowObjectDisposedException(ExceptionResource resource)
        {
            throw new ObjectDisposedException(null, GetResourceString(resource));
        }

        internal static void ThrowNotSupportedException()
        {
            throw new NotSupportedException();
        }

        internal static void ThrowAggregateException(List<Exception> exceptions)
        {
            throw new AggregateException(exceptions);
        }

        internal static void ThrowArgumentException_Argument_InvalidArrayType()
        {
            throw new ArgumentException("Invalid sparseArray type.");
        }

        internal static void ThrowInvalidOperationException_InvalidOperation_EnumNotStarted()
        {
            throw new InvalidOperationException("Enumeration has not started.");
        }

        internal static void ThrowInvalidOperationException_InvalidOperation_EnumEnded()
        {
            throw new InvalidOperationException("Enumeration has ended.");
        }

        internal static void ThrowInvalidOperationException_EnumCurrent(int index)
        {
            throw GetInvalidOperationException_EnumCurrent(index);
        }

        internal static void ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion()
        {
            throw new InvalidOperationException("Collection was modified during enumeration.");
        }

        internal static void ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen()
        {
            throw new InvalidOperationException("Invalid enumerator state: enumeration cannot proceed.");
        }

        internal static void ThrowInvalidOperationException_InvalidOperation_NoValue()
        {
            throw new InvalidOperationException("No value provided.");
        }

        internal static void ThrowInvalidOperationException_ConcurrentOperationsNotSupported()
        {
            throw new InvalidOperationException("Concurrent operations are not supported.");
        }

        internal static void ThrowInvalidOperationException_HandleIsNotInitialized()
        {
            throw new InvalidOperationException("Handle is not initialized.");
        }

        internal static void ThrowFormatException_BadFormatSpecifier()
        {
            throw new FormatException("Bad format specifier.");
        }

        private static ArgumentException GetArgumentException(ExceptionResource resource)
        {
            return new ArgumentException(GetResourceString(resource));
        }

        private static InvalidOperationException GetInvalidOperationException(ExceptionResource resource)
        {
            return new InvalidOperationException(GetResourceString(resource));
        }

        private static ArgumentException GetWrongKeyTypeArgumentException(object? key, Type targetType)
        {
            return new ArgumentException($"Wrong key type. Expected {targetType}, got: '{key}'.", nameof(key));
        }

        private static ArgumentException GetWrongValueTypeArgumentException(object? value, Type targetType)
        {
            return new ArgumentException($"Wrong value type. Expected {targetType}, got: '{value}'.", nameof(value));
        }

        private static KeyNotFoundException GetKeyNotFoundException(object? key)
        {
            return new KeyNotFoundException($"Key not found: {key}");
        }

        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument, ExceptionResource resource)
        {
            return new ArgumentOutOfRangeException(GetArgumentName(argument), GetResourceString(resource));
        }

        private static ArgumentException GetArgumentException(ExceptionResource resource, ExceptionArgument argument)
        {
            return new ArgumentException(GetResourceString(resource), GetArgumentName(argument));
        }

        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument, int paramNumber, ExceptionResource resource)
        {
            return new ArgumentOutOfRangeException($"{GetArgumentName(argument)}[{paramNumber}]{GetResourceString(resource)}");
        }

        private static InvalidOperationException GetInvalidOperationException_EnumCurrent(int index)
        {
            return new InvalidOperationException(
                index < 0 ?
                "Enumeration has not started" :
                "Enumeration has ended");
        }

        // Allow nulls for reference types and Nullable<U>, but not for value types.
        // Aggressively inline so the jit evaluates the if in place and either drops the call altogether
        // Or just leaves null test and call to the Non-returning ThrowHelper.ThrowArgumentNullException
        
        internal static void IfNullAndNullsAreIllegalThenThrow<T>(object? value, ExceptionArgument argName)
        {
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
            if (!(default(T) == null) && value == null)
                ThrowHelper.ThrowArgumentNullException(argName);
        }

        
        internal static void ThrowForUnsupportedVectorBaseType<T>() where T : struct
        {
            if (typeof(T) != typeof(byte) && typeof(T) != typeof(sbyte) &&
                typeof(T) != typeof(short) && typeof(T) != typeof(ushort) &&
                typeof(T) != typeof(int) && typeof(T) != typeof(uint) &&
                typeof(T) != typeof(long) && typeof(T) != typeof(ulong) &&
                typeof(T) != typeof(float) && typeof(T) != typeof(double))
            {
                ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
            }
        }

        private static string GetArgumentName(ExceptionArgument argument)
        {
            switch (argument)
            {
                case ExceptionArgument.obj:
                    return "obj";
                case ExceptionArgument.dictionary:
                    return "dictionary";
                case ExceptionArgument.array:
                    return "sparseArray";
                case ExceptionArgument.info:
                    return "info";
                case ExceptionArgument.key:
                    return "key";
                case ExceptionArgument.text:
                    return "text";
                case ExceptionArgument.values:
                    return "values";
                case ExceptionArgument.value:
                    return "value";
                case ExceptionArgument.startIndex:
                    return "startIndex";
                case ExceptionArgument.task:
                    return "task";
                case ExceptionArgument.ch:
                    return "ch";
                case ExceptionArgument.s:
                    return "s";
                case ExceptionArgument.input:
                    return "input";
                case ExceptionArgument.list:
                    return "list";
                case ExceptionArgument.index:
                    return "index";
                case ExceptionArgument.capacity:
                    return "capacity";
                case ExceptionArgument.collection:
                    return "collection";
                case ExceptionArgument.item:
                    return "item";
                case ExceptionArgument.converter:
                    return "converter";
                case ExceptionArgument.match:
                    return "match";
                case ExceptionArgument.count:
                    return "count";
                case ExceptionArgument.action:
                    return "action";
                case ExceptionArgument.comparison:
                    return "comparison";
                case ExceptionArgument.exceptions:
                    return "exceptions";
                case ExceptionArgument.exception:
                    return "exception";
                case ExceptionArgument.enumerable:
                    return "enumerable";
                case ExceptionArgument.start:
                    return "start";
                case ExceptionArgument.format:
                    return "format";
                case ExceptionArgument.culture:
                    return "culture";
                case ExceptionArgument.comparer:
                    return "comparer";
                case ExceptionArgument.comparable:
                    return "comparable";
                case ExceptionArgument.source:
                    return "source";
                case ExceptionArgument.state:
                    return "state";
                case ExceptionArgument.length:
                    return "Length";
                case ExceptionArgument.comparisonType:
                    return "comparisonType";
                case ExceptionArgument.manager:
                    return "manager";
                case ExceptionArgument.sourceBytesToCopy:
                    return "sourceBytesToCopy";
                case ExceptionArgument.callBack:
                    return "callBack";
                case ExceptionArgument.creationOptions:
                    return "creationOptions";
                case ExceptionArgument.function:
                    return "function";
                case ExceptionArgument.delay:
                    return "delay";
                case ExceptionArgument.millisecondsDelay:
                    return "millisecondsDelay";
                case ExceptionArgument.millisecondsTimeout:
                    return "millisecondsTimeout";
                case ExceptionArgument.timeout:
                    return "timeout";
                case ExceptionArgument.type:
                    return "type";
                case ExceptionArgument.sourceIndex:
                    return "sourceIndex";
                case ExceptionArgument.sourceArray:
                    return "sourceArray";
                case ExceptionArgument.destinationIndex:
                    return "destinationIndex";
                case ExceptionArgument.destinationArray:
                    return "destinationArray";
                case ExceptionArgument.other:
                    return "other";
                case ExceptionArgument.newSize:
                    return "newSize";
                case ExceptionArgument.lowerBounds:
                    return "lowerBounds";
                case ExceptionArgument.lengths:
                    return "lengths";
                case ExceptionArgument.len:
                    return "len";
                case ExceptionArgument.keys:
                    return "keys";
                case ExceptionArgument.indices:
                    return "indices";
                case ExceptionArgument.endIndex:
                    return "endIndex";
                case ExceptionArgument.elementType:
                    return "elementType";
                case ExceptionArgument.arrayIndex:
                    return "arrayIndex";
                default:
                    Debug.Fail("The enum value is not defined, please check the ExceptionArgument Enum.");
                    return argument.ToString();
            }
        }

#if false // Reflection-based implementation does not work for CoreRT/ProjectN
        // This function will convert an ExceptionResource enum value to the resource string.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetResourceString(ExceptionResource resource)
        {
            Debug.Assert(Enum.IsDefined(typeof(ExceptionResource), resource),
                "The enum value is not defined, please check the ExceptionResource Enum.");

            return SR.GetResourceString(resource.ToString());
        }
#endif

        private static string GetResourceString(ExceptionResource resource)
        {
            switch (resource)
            {
                case ExceptionResource.ArgumentOutOfRange_Index:
                    return "Argument 'index' was out of the range of valid values.";
                case ExceptionResource.ArgumentOutOfRange_Count:
                    return "Argument 'count' was out of the range of valid values.";
                case ExceptionResource.Arg_ArrayPlusOffTooSmall:
                    return "Array plus offset too small.";
                case ExceptionResource.NotSupported_ReadOnlyCollection:
                    return "This operation is not supported on a read-only collection.";
                case ExceptionResource.Arg_RankMultiDimNotSupported:
                    return "Multi-dimensional arrays are not supported.";
                case ExceptionResource.Arg_NonZeroLowerBound:
                    return "Arrays with a non-zero lower bound are not supported.";
                case ExceptionResource.ArgumentOutOfRange_ListInsert:
                    return "Insertion index was out of the range of valid values.";
                case ExceptionResource.ArgumentOutOfRange_NeedNonNegNum:
                    return "The number must be non-negative.";
                case ExceptionResource.ArgumentOutOfRange_SmallCapacity:
                    return "The capacity cannot be set below the current Count.";
                case ExceptionResource.Argument_InvalidOffLen:
                    return "Invalid offset Length.";
                case ExceptionResource.ArgumentOutOfRange_BiggerThanCollection:
                    return "The given value was larger than the Capacity of the collection.";
                case ExceptionResource.Serialization_MissingKeys:
                    return "Serialization error: missing keys.";
                case ExceptionResource.Serialization_NullKey:
                    return "Serialization error: null key.";
                case ExceptionResource.NotSupported_KeyCollectionSet:
                    return "The KeyCollection does not support modification.";
                case ExceptionResource.NotSupported_ValueCollectionSet:
                    return "The ValueCollection does not support modification.";
                case ExceptionResource.InvalidOperation_NullArray:
                    return "Null arrays are not supported.";
                case ExceptionResource.InvalidOperation_HSCapacityOverflow:
                    return "Set hash capacity overflow. Cannot increase Capacity.";
                case ExceptionResource.NotSupported_StringComparison:
                    return "String comparison not supported.";
                case ExceptionResource.ConcurrentCollection_SyncRoot_NotSupported:
                    return "SyncRoot not supported.";
                case ExceptionResource.ArgumentException_OtherNotArrayOfCorrectLength:
                    return "The other sparseArray is not of the correct Length.";
                case ExceptionResource.ArgumentOutOfRange_EndIndexStartIndex:
                    return "The end index does not come after the start index.";
                case ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported:
                    return "Huge arrays are not supported.";
                case ExceptionResource.Argument_AddingDuplicate:
                    return "Duplicate item added.";
                case ExceptionResource.Argument_InvalidArgumentForComparison:
                    return "Invalid argument for comparison.";
                case ExceptionResource.Arg_LowerBoundsMustMatch:
                    return "Array lower bounds must match.";
                case ExceptionResource.Arg_MustBeType:
                    return "Argument must be of type: ";
                case ExceptionResource.InvalidOperation_IComparerFailed:
                    return "IComparer failed.";
                case ExceptionResource.NotSupported_FixedSizeCollection:
                    return "This operation is not suppored on a fixed-Capacity collection.";
                case ExceptionResource.Rank_MultiDimNotSupported:
                    return "Multi-dimensional arrays are not supported.";
                case ExceptionResource.Arg_TypeNotSupported:
                    return "Type not supported.";
                default:
                    Debug.Assert(false,
                        "The enum value is not defined, please check the ExceptionResource Enum.");
                    return resource.ToString();
            }
        }
    }

    //
    // The convention for this enum is using the argument name as the enum name
    //
    internal enum ExceptionArgument
    {
        obj,
        dictionary,
        array,
        info,
        key,
        text,
        values,
        value,
        startIndex,
        task,
        ch,
        s,
        input,
        list,
        index,
        capacity,
        collection,
        item,
        converter,
        match,
        count,
        action,
        comparison,
        exceptions,
        exception,
        enumerable,
        start,
        format,
        culture,
        comparer,
        comparable,
        source,
        state,
        length,
        comparisonType,
        manager,
        sourceBytesToCopy,
        callBack,
        creationOptions,
        function,
        delay,
        millisecondsDelay,
        millisecondsTimeout,
        timeout,
        type,
        sourceIndex,
        sourceArray,
        destinationIndex,
        destinationArray,
        other,
        newSize,
        lowerBounds,
        lengths,
        len,
        keys,
        indices,
        endIndex,
        elementType,
        arrayIndex
    }

    //
    // The convention for this enum is using the resource name as the enum name
    //
    internal enum ExceptionResource
    {
        ArgumentOutOfRange_Index,
        ArgumentOutOfRange_Count,
        Arg_ArrayPlusOffTooSmall,
        NotSupported_ReadOnlyCollection,
        Arg_RankMultiDimNotSupported,
        Arg_NonZeroLowerBound,
        ArgumentOutOfRange_ListInsert,
        ArgumentOutOfRange_NeedNonNegNum,
        ArgumentOutOfRange_SmallCapacity,
        Argument_InvalidOffLen,
        ArgumentOutOfRange_BiggerThanCollection,
        Serialization_MissingKeys,
        Serialization_NullKey,
        NotSupported_KeyCollectionSet,
        NotSupported_ValueCollectionSet,
        InvalidOperation_NullArray,
        InvalidOperation_HSCapacityOverflow,
        NotSupported_StringComparison,
        ConcurrentCollection_SyncRoot_NotSupported,
        ArgumentException_OtherNotArrayOfCorrectLength,
        ArgumentOutOfRange_EndIndexStartIndex,
        ArgumentOutOfRange_HugeArrayNotSupported,
        Argument_AddingDuplicate,
        Argument_InvalidArgumentForComparison,
        Arg_LowerBoundsMustMatch,
        Arg_MustBeType,
        InvalidOperation_IComparerFailed,
        NotSupported_FixedSizeCollection,
        Rank_MultiDimNotSupported,
        Arg_TypeNotSupported,
    }
}
