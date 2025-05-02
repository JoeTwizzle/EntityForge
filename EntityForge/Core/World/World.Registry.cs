using EntityForge.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge;

public sealed partial class World
{
	/// <summary>
	/// Stores the meta item id has
	/// </summary>
	private static readonly Dictionary<Type, ComponentInfo> s_TypeMap = new();
	/// <summary>
	/// Stores the type an meta has
	/// </summary>
	private static readonly Dictionary<int, Type> s_TypeMapReverse = new();
	private static readonly ReaderWriterLockSlim s_createTypeRWLock = new();
	private static int s_componentCount;
	private static int s_tagCount;

	public static void ClearRegistry()
	{
		s_createTypeRWLock.EnterWriteLock();
		s_componentCount = 0;
		s_tagCount = 0;
		s_TypeMap.Clear();
		s_TypeMapReverse.Clear();
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
		var meta = new ComponentInfo(typeId, s_TypeMapReverse[typeId]);
		s_createTypeRWLock.ExitReadLock();
		return meta;
	}

	public static int GetOrCreateTagId<T>() where T : struct, ITag<T>
	{
		if (T.BitIndex == 0)
		{
			T.BitIndex = Interlocked.Increment(ref s_tagCount);
		}
		return T.BitIndex;
	}

	public static int GetOrCreateComponentId<T>() where T : struct, IComponent<T>
	{
		if (!T.Registered)
		{
			CreateComponentId<T>();
		}
		return T.Id;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void CreateComponentId<T>() where T : struct, IComponent<T>
	{
		s_createTypeRWLock.EnterWriteLock();
		ref var info = ref CollectionsMarshal.GetValueRefOrAddDefault(s_TypeMap, typeof(T), out var exists);
		if (!exists)
		{
			int size = RuntimeHelpers.IsReferenceOrContainsReferences<T>() ? 0 : Unsafe.SizeOf<T>();
			int id = ++s_componentCount;
			s_TypeMapReverse.Add(id, typeof(T));
			info = new ComponentInfo(id, size, typeof(T));
			T.Id = id;
			T.Registered = true;
		}
		s_createTypeRWLock.ExitWriteLock();
	}

	public static ComponentInfo GetOrCreateComponentInfo<T>() where T : struct, IComponent<T>
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
