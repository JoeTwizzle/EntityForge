using EntityForge.Collections;
using EntityForge.Collections.Generic;
using EntityForge.Helpers;
using EntityForge.Tags;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EntityOperationKind = EntityForge.Commands.OperationBuffer.EntityOperationKind;

namespace EntityForge.Commands
{
    internal sealed class EcsCommandBuffer : IDisposable
    {
        readonly World _world;
        readonly Archetype _archetype;

        readonly object _lock = new();
        readonly BitMask _knownEntityMask = new();
        readonly BitMask _createdEntityMask = new();
        readonly BitMask _moveIntoEntityMask = new();
        readonly BitMask _destroyedEntityMask = new();
        //used for temporary operations
        readonly BitMask _scratchTagAddMask = new();
        readonly BitMask _scratchTagRemoveMask = new();
        readonly OperationBuffer _operationBuffer = new();
        readonly UnsafeSparseSet<UnsafeSparseSet> _virtualComponentStore = new(); //T.id, entityId
        readonly UnsafeSparseSet<BitMask> _varyingComponentsMasks = new(); //T.id, entityId

        private int _createdEntities;
        private int _reservedEntities;
        private int _movedEntities;

        public EcsCommandBuffer(Archetype archetype)
        {
            _world = archetype.World;
            _archetype = archetype;
        }

        public bool HasComponent(EntityId entity, int typeId)
        {
            lock (_lock)
            {
                bool initial = _archetype.HasComponent(typeId);
                if (_varyingComponentsMasks.TryGetValue(typeId, out var mask) && mask.IsSet(entity.Id))
                {
                    initial = !initial;
                }
                return initial;
            }
        }

        public ref T GetComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            lock (_lock)
            {
                ref var pool = ref _virtualComponentStore.GetRefOrNullRef(World.GetOrCreateTypeId<T>());
                if (!Unsafe.IsNullRef<UnsafeSparseSet>(ref pool))
                {
                    return ref pool.GetRef<T>(entity.Id);
                }
                ThrowHelper.ThrowMissingComponentException("The entity does not have this component");
                throw null;
            }
        }

        public ref T GetComponentOrNullRef<T>(EntityId entity) where T : struct, IComponent<T>
        {
            lock (_lock)
            {
                ref var pool = ref _virtualComponentStore.GetRefOrNullRef(World.GetOrCreateTypeId<T>());
                if (!Unsafe.IsNullRef<UnsafeSparseSet>(ref pool))
                {
                    return ref pool.GetRefOrNullRef<T>(entity.Id);
                }
                return ref Unsafe.NullRef<T>();
            }
        }

        public int Create(EntityId entity)
        {
            lock (_lock)
            {
                _knownEntityMask.SetBit(entity.Id);
                _createdEntityMask.SetBit(entity.Id);
                return _archetype.ElementCount + _movedEntities + _createdEntities++;
            }
        }

        public int CreateMany(int idStart, int count)
        {
            lock (_lock)
            {
                _knownEntityMask.SetRange(idStart, count);
                _createdEntityMask.SetRange(idStart, count);
                _createdEntities += count;
                return _archetype.ElementCount + _movedEntities + _createdEntities - count;
            }
        }

        public void Destroy(EntityId entity)
        {
            lock (_lock)
            {
                _knownEntityMask.SetBit(entity.Id);
                _destroyedEntityMask.SetBit(entity.Id);
            }
        }

        public void Reserve(int count)
        {
            lock (_lock)
            {
                _reservedEntities += count;
            }
        }

        public void Add(EntityId entity, ComponentInfo info)
        {
            lock (_lock)
            {
                _knownEntityMask.SetBit(entity.Id);
                _operationBuffer.Add(entity.Id, EntityOperationKind.Add, info);
                ref var mask = ref _varyingComponentsMasks.GetOrAdd(info.TypeId);
                if (mask == null)
                {
                    mask = new();
                }
                mask.FlipBit(entity.Id);
            }
        }

        public void Remove(EntityId entity, ComponentInfo info)
        {
            lock (_lock)
            {
                _knownEntityMask.SetBit(entity.Id);
                _operationBuffer.Add(entity.Id, EntityOperationKind.Remove, info);
                ref var mask = ref _varyingComponentsMasks.GetOrAdd(info.TypeId);
                if (mask == null)
                {
                    mask = new();
                }
                mask.FlipBit(entity.Id);
                ref var pool = ref _virtualComponentStore.GetRefOrNullRef(info.TypeId);
                if (!Unsafe.IsNullRef<UnsafeSparseSet>(ref pool) && pool.Has(entity.Id))
                {
                    pool.RemoveAt(entity.Id, info);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddWithValue<T>(EntityId entity, T value) where T : struct, IComponent<T>
        {
            lock (_lock)
            {
                var info = World.GetOrCreateComponentInfo<T>();
                if (_archetype.HasComponent(info.TypeId))
                {
                    _operationBuffer.Add(entity.Id, EntityOperationKind.Add, info);
                    _archetype.GetComponent<T>(_world.GetEntityIndexRecord(entity).ArchetypeColumn, info.TypeId) = value;
                    if (_varyingComponentsMasks.TryGetValue(info.TypeId, out var mask))
                    {
                        mask.ClearBit(entity.Id);
                    }
                    return;
                }
                Add(entity, info);
                ref var pool = ref _virtualComponentStore.GetOrAdd(info.TypeId);
                if (pool is null)
                {
#pragma warning disable CA2000 // Dispose objects before losing scope
                    pool = UnsafeSparseSet.CreateForComponent(info, 5);
#pragma warning restore CA2000 // Dispose objects before losing scope
                }
                pool.Add(entity.Id, value);
            }
        }

        public void AddTag(EntityId entity, int tagId)
        {
            lock (_lock)
            {
                _knownEntityMask.SetBit(entity.Id);
                _operationBuffer.Add(entity.Id, EntityOperationKind.AddTag, new ComponentInfo(tagId, null!));
            }
        }

        public void RemoveTag(EntityId entity, int tagId)
        {
            lock (_lock)
            {
                _knownEntityMask.SetBit(entity.Id);
                _operationBuffer.Add(entity.Id, EntityOperationKind.RemoveTag, new ComponentInfo(tagId, null!));
            }
        }

        private int MoveInto(EntityId entity, EcsCommandBuffer srcCmdBuf, ReadOnlySpan<OperationBuffer.Entry> opSpan)
        {
            var src = srcCmdBuf._archetype;
            //_archetype.EntitiesPool.GetRefAt(_archetype.ElementCount) = _world.GetEntity(entity);
            int newIndex = _archetype.ElementCount + _createdEntities + _movedEntities++;

            ref var record = ref _world.GetEntityIndexRecord(entity);
            int oldIndex = record.ArchetypeColumn;
            //Copy Pool to new Arrays
            StoreComponents(entity, oldIndex, srcCmdBuf);

            //Fill hole in old Arrays
            src.FillHole(oldIndex);

            //Update index of entityId filling the hole
            ref EntityIndexRecord rec = ref _world.GetEntityIndexRecord(src.Entities[src.ElementCount - 1]);
            rec.ArchetypeColumn = oldIndex;
            //Update index of moved entityId
            record.ArchetypeColumn = newIndex;
            record.Archetype = _archetype;
            //Finish removing entityId from source
            src.ElementCount--;


            //add remaining commands
            _knownEntityMask.SetBit(entity.Id);
            _moveIntoEntityMask.SetBit(entity.Id);

            for (int i = 0; i < opSpan.Length; i++)
            {
                var op = opSpan[i];

                switch (op.Kind)
                {
                    //dont add these, since they have already been executed
                    case EntityOperationKind.Add:
                    case EntityOperationKind.Remove:
                        break;
                    default:
                        _operationBuffer.Add(entity.Id, op);
                        break;
                }
            }

            return newIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void StoreComponents(EntityId entity, int srcIndex, EcsCommandBuffer srcCmdBuf)
        {
            var src = srcCmdBuf._archetype;
            var infos = _archetype.ComponentInfo.Span;
            for (int i = 0; i < _archetype.ComponentInfo.Length; i++)
            {
                ref readonly var info = ref infos[i];
                if (src.ComponentIdsMap.TryGetValue(info.TypeId, out var index))
                {
                    ref var pool = ref _virtualComponentStore.GetOrAdd(info.TypeId);
                    if (pool is null)
                    {
#pragma warning disable CA2000 // Dispose objects before losing scope
                        pool = UnsafeSparseSet.CreateForComponent(info, 5);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    }
                    var destIndex = pool.Add(entity.Id, info);
                    if (info.IsUnmanaged)
                    {
                        src.ComponentPools[index].CopyToUnmanaged(srcIndex, pool.denseArray.UnmanagedData, destIndex, info.UnmanagedSize);
                    }
                    else
                    {
                        src.ComponentPools[index].CopyToManaged(srcIndex, pool.denseArray.ManagedData!, destIndex, 1);
                    }
                }

                if (srcCmdBuf._virtualComponentStore.TryGetValue(info.TypeId, out var srcPool) && srcPool.TryGetIndex(entity.Id, out int srcDenseIndex))
                {
                    ref var pool = ref _virtualComponentStore.GetOrAdd(info.TypeId);
                    if (pool is null)
                    {
#pragma warning disable CA2000 // Dispose objects before losing scope
                        pool = UnsafeSparseSet.CreateForComponent(info, 5);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    }
                    var destIndex = pool.Add(entity.Id, info);

                    if (info.IsUnmanaged)
                    {
                        srcPool.denseArray.CopyToUnmanaged(srcDenseIndex, pool.denseArray.UnmanagedData, destIndex, info.UnmanagedSize);
                    }
                    else
                    {
                        srcPool.denseArray.CopyToManaged(srcDenseIndex, pool.denseArray.ManagedData!, destIndex, 1);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteTagChanges(ReadOnlySpan<OperationBuffer.Entry> opSpan, EntityId entity, TagBearer tag)
        {
            for (int i = 0; i < opSpan.Length; i++)
            {
                var op = opSpan[i];
                switch (op.Kind)
                {
                    case EntityOperationKind.AddTag:
                        _scratchTagAddMask.SetBit(op.Info.TypeId);
                        _scratchTagRemoveMask.ClearBit(op.Info.TypeId);
                        break;
                    case EntityOperationKind.RemoveTag:
                        _scratchTagAddMask.ClearBit(op.Info.TypeId);
                        _scratchTagRemoveMask.SetBit(op.Info.TypeId);
                        break;
                    default:
                        break;
                }
            }

            var scratchBits = _scratchTagAddMask.Bits;

            for (int i = 0; i < scratchBits.Length; i++)
            {
                long tagBitItem = scratchBits[i];
                while (tagBitItem != 0)
                {
                    int bitIndex = i * (sizeof(ulong) * 8) + BitOperations.TrailingZeroCount(tagBitItem);

                    tag.SetTag(bitIndex);
                    _world.InvokeTagAddEvent(entity, bitIndex);

                    tagBitItem ^= tagBitItem & -tagBitItem;
                }
            }
            _scratchTagAddMask.ClearAll();

            var scratchRemoveBits = _scratchTagRemoveMask.Bits;
            var tagBits = tag.mask.Bits;

            int len = Math.Min(tagBits.Length, scratchRemoveBits.Length);

            for (int i = 0; i < len; i++)
            {
                long tagBitItem = scratchRemoveBits[i] & tagBits[i];
                while (tagBitItem != 0)
                {
                    int bitIndex = i * (sizeof(ulong) * 8) + BitOperations.TrailingZeroCount(tagBitItem);

                    tag.UnsetTag(bitIndex);
                    _world.InvokeTagRemoveEvent(entity, bitIndex);

                    tagBitItem ^= tagBitItem & -tagBitItem;
                }
            }

            _scratchTagRemoveMask.ClearAll();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void OnUnlock()
        {
            lock (_lock)
            {
                _archetype.GrowBy(_reservedEntities);
                _reservedEntities = 0;

                //See: https://lemire.me/blog/2018/02/21/iterating-over-set-bits-quickly/

                var bits = _knownEntityMask.Bits;
                for (int idx = 0; idx < bits.Length; idx++)
                {
                    long bitItem = bits[idx];
                    while (bitItem != 0)
                    {
                        int id = idx * (sizeof(ulong) * 8) + BitOperations.TrailingZeroCount(bitItem);
                        bitItem ^= bitItem & -bitItem;
                        var opList = _operationBuffer.GetEntries(id);
                        ReadOnlySpan<OperationBuffer.Entry> opSpan;
                        if (opList is null)
                        {
                            opSpan = ReadOnlySpan<OperationBuffer.Entry>.Empty;
                        }
                        else
                        {
                            opSpan = CollectionsMarshal.AsSpan(opList);
                        }

                        EntityId entity = new EntityId(id);
                        int destIndex;
                        var arch = _archetype;
                        if (!_moveIntoEntityMask.IsSet(id))
                        {
                            //fold add/remove
                            for (int i = 0; i < opSpan.Length; i++)
                            {
                                var op = opSpan[i];

                                switch (op.Kind)
                                {
                                    case EntityOperationKind.Add:
                                        arch = _world.GetOrCreateArchetypeVariantAdd(arch, op.Info);
                                        break;
                                    case EntityOperationKind.Remove:
                                        arch = _world.GetOrCreateArchetypeVariantRemove(arch, op.Info.TypeId);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            if (_createdEntityMask.IsSet(id))
                            {
                                _archetype.AddEntityInternal(_world.GetEntity(entity));
                                _world.InvokeCreateEntityEvent(entity);
                            }
                            if (arch != _archetype)
                            {
                                if (!arch.IsLocked)
                                {
                                    _world.MoveEntity(_archetype, arch, entity);
                                }
                                else
                                {
                                    arch.CommandBuffer.MoveInto(entity, this, opSpan);
                                    opList?.Clear();
                                    continue;
                                }
                            }

                            destIndex = _world.GetEntityIndexRecord(entity).ArchetypeColumn;
                        }
                        else
                        {
                            destIndex = _archetype.ElementCount;
                            _archetype.AddEntityInternal(_world.GetEntity(entity));
                            ref var rec = ref _world.GetEntityIndexRecord(entity);
                            rec.Archetype = _archetype;
                            rec.ArchetypeColumn = destIndex;
                        }

                        var infos = arch.ComponentInfo.Span;

                        if (opSpan.Length > 0)
                        {
                            for (int i = 0; i < arch.ComponentInfo.Length; i++)
                            {
                                ref readonly var info = ref infos[i];
                                ref var pool = ref _virtualComponentStore.GetRefOrNullRef(info.TypeId);
                                if (Unsafe.IsNullRef<UnsafeSparseSet>(ref pool))
                                {
                                    continue;
                                }
                                if (pool.TryGetIndex(entity.Id, out int denseIndex))
                                {
                                    if (info.IsUnmanaged)
                                    {
                                        pool.denseArray.CopyToUnmanaged(denseIndex, arch.ComponentPools[i].UnmanagedData, destIndex, info.UnmanagedSize);
                                    }
                                    else
                                    {
                                        pool.denseArray.CopyToManaged(denseIndex, arch.ComponentPools[i].ManagedData!, destIndex, 1);
                                    }
                                    pool.RemoveAt(entity.Id, info);
                                }
                            }
                        }

                        if (_world.IsAlive(entity))
                        {
                            ref var tag = ref _world.GetComponentOrNullRef<TagBearer>(entity);
                            if (!Unsafe.IsNullRef<TagBearer>(ref tag))
                            {
                                ExecuteTagChanges(opSpan, entity, tag);
                            }
                        }

                        if (_destroyedEntityMask.IsSet(id))
                        {
                            _world.DeleteEntityInternal(_archetype, _world.GetEntityIndexRecord(entity).ArchetypeColumn);
                            _world.InvokeDeleteEntityEvent(entity);
                        }

                        opList?.Clear();
                    }
                }
                _createdEntityMask.ClearAll();
                _destroyedEntityMask.ClearAll();
                _moveIntoEntityMask.ClearAll();
                _knownEntityMask.ClearAll();
                _operationBuffer.ClearAll();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                var compStores = _virtualComponentStore.GetDenseData();
                for (int i = 0; i < compStores.Length; i++)
                {
                    compStores[i].Dispose();
                }
                _virtualComponentStore.Dispose();
                _varyingComponentsMasks.Dispose();
            }
        }
    }
}
