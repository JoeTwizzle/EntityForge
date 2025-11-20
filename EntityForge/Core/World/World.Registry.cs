using EntityForge.Tags;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge;

public sealed partial class World
{
    /// <summary>
    /// Stores the ComponentInfo for a componentId
    /// </summary>
    private static readonly Dictionary<Type, ComponentInfo> s_TypeMap = new();
    /// <summary>
    /// Stores the System.Type a componentId designates
    /// </summary>
    private static readonly ReaderWriterLockSlim s_createTypeRWLock = new();
    private static readonly Dictionary<int, Type> s_typeMapReverse = new();
    private static readonly Dictionary<RuntimeTypeHandle, int> s_typeRegistry = new();
    private static readonly Queue<int> s_recycledIdQueue = new();
    private static readonly Dictionary<RuntimeTypeHandle, int> s_tagTypeRegistry = new();
    private static readonly Queue<int> s_recycledTagIdQueue = new();
    private static int s_componentCount;
    private static int s_tagCount;


    public static void ClearRegistry()
    {
        s_createTypeRWLock.EnterWriteLock();
        s_componentCount = 0;
        s_tagCount = 0;
        s_TypeMap.Clear();
        s_recycledIdQueue.Clear();
        s_recycledTagIdQueue.Clear();
        s_typeRegistry.Clear();
        s_typeMapReverse.Clear();
        s_createTypeRWLock.ExitWriteLock();
    }

    public static int GetTypeId(Type type)
    {
        s_createTypeRWLock.EnterReadLock();
        var id = s_TypeMap[type].TypeId;
        s_createTypeRWLock.ExitReadLock();
        return id;
    }

    public static ComponentInfo GetComponentInfo(Type type)
    {
        s_createTypeRWLock.EnterReadLock();
        var meta = s_TypeMap[type];
        s_createTypeRWLock.ExitReadLock();
        return meta;
    }

    public static ComponentInfo GetComponentInfo(int typeId)
    {
        s_createTypeRWLock.EnterReadLock();
        var meta = new ComponentInfo(typeId, s_typeMapReverse[typeId]);
        s_createTypeRWLock.ExitReadLock();
        return meta;
    }

    public static int GetOrCreateTagId<T>() where T : struct, ITag
    {
        if (!s_tagTypeRegistry.TryGetValue(typeof(T).TypeHandle, out var bitIndex))
        {
            bitIndex = CreateTagId<T>();
        }
        return bitIndex;
    }

    private static int CreateTagId<T>()
    {
        if (!s_recycledTagIdQueue.TryDequeue(out int bitIndex))
        {
            bitIndex = ++s_tagCount;
        }
        s_tagTypeRegistry.Add(typeof(T).TypeHandle, bitIndex);
        return bitIndex;
    }

    public static int GetOrCreateComponentId<T>() where T : struct, IComponent
    {
        if (!s_typeRegistry.TryGetValue(typeof(T).TypeHandle, out var result))
        {
            result = CreateComponentId<T>();
        }
        return result;
    }

    private static int CreateComponentId<T>() where T : struct, IComponent
    {
        s_createTypeRWLock.EnterWriteLock();
        int nativeSize = RuntimeHelpers.IsReferenceOrContainsReferences<T>() ? 0 : Unsafe.SizeOf<T>();
        if (!s_recycledIdQueue.TryDequeue(out int id))
        {
            id = ++s_componentCount;
        }
        s_typeRegistry.Add(typeof(T).TypeHandle, id);
        s_typeMapReverse.Add(id, typeof(T));
        s_TypeMap.Add(typeof(T), new ComponentInfo(id, nativeSize, typeof(T)));
        s_createTypeRWLock.ExitWriteLock();
        return id;
    }

    public static ComponentInfo GetOrCreateComponentInfo<T>() where T : struct, IComponent
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            return new ComponentInfo(GetOrCreateComponentId<T>(), typeof(T));
        }
        else
        {
            return new ComponentInfo(GetOrCreateComponentId<T>(), Unsafe.SizeOf<T>(), typeof(T));
        }
    }
}
