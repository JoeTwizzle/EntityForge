using EntityForge.Collections;
using EntityForge;
using EntityForge.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityForge.Collections.Generic;
using System.Buffers;
using EntityForge.Helpers;
using CommunityToolkit.HighPerformance;
using System.ComponentModel;
using System.Runtime.InteropServices;
using static EntityForge.Commands.OperationBuffer;


namespace EntityForge.Commands
{
    public readonly struct CommandBufferItem : IEquatable<CommandBufferItem>
    {
        public readonly int Id;

        public readonly int Count;

        public CommandBufferItem(int id)
        {
            Id = id;
            Count = 1;
        }

        public CommandBufferItem(int id, int count)
        {
            Id = id;
            Count = count;
        }

        public override bool Equals(object? obj)
        {
            return obj is CommandBufferItem e && Equals(e);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public static bool operator ==(CommandBufferItem left, CommandBufferItem right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CommandBufferItem left, CommandBufferItem right)
        {
            return !(left == right);
        }

        public bool Equals(CommandBufferItem other)
        {
            return Id == other.Id;
        }
    }

    public sealed class EcsCommandBuffer
    {
        enum MaskFlags : byte
        {
            None = 0,
            Create = 1 << 0,
            Captured = 1 << 1,
            Destroy = 1 << 2,
            AddedComponent = 1 << 3,
            RemovedComponent = 1 << 4,
            AddedTag = 1 << 5,
            RemovedTag = 1 << 6,
        }

        struct CommandBufferRecord
        {
            public MaskFlags Mask;
            public CommandBufferItem Item;
            public BitMask ComponentsAdded;
            public BitMask? ComponentsRemoved;
            public BitMask TagsAdded;
            public BitMask? TagsRemoved;
            public EntityId[]? Entities;
            public MultiComponentList? ComponentValuesAdded;

            public CommandBufferRecord(MaskFlags mask,
                                       CommandBufferItem item,
                                       BitMask componentsAdded,
                                       BitMask? componentsRemoved,
                                       BitMask tagsAdded,
                                       BitMask? tagsRemoved,
                                       EntityId[]? entities,
                                       MultiComponentList? componentValuesAdded)
            {
                Mask = mask;
                Item = item;
                ComponentsAdded = componentsAdded;
                ComponentsRemoved = componentsRemoved;
                TagsAdded = tagsAdded;
                TagsRemoved = tagsRemoved;
                Entities = entities;
                ComponentValuesAdded = componentValuesAdded;
            }
        }

        private readonly World _world;
        private readonly List<CommandBufferRecord> _records;
        private bool _recordingStarted;

        public EcsCommandBuffer(World world)
        {
            _world = world;
            _records = new();
        }

        public void Begin()
        {
            if (_recordingStarted) throw new InvalidOperationException("Recording already in progress");
            _recordingStarted = true;
            _records.Clear();
        }

        public void End()
        {
            CheckRecordingStarted();
            _recordingStarted = false;
        }

        public CommandBufferItem Create()
        {
            return Create(1);
        }

        public CommandBufferItem Create(int count)
        {
            CheckRecordingStarted();
            int index = _records.Count;
            var item = new CommandBufferItem(index, count);
            var record = new CommandBufferRecord(MaskFlags.Create,
                item,
                new BitMask(),
                null,
                new BitMask(),
                null,
                null,
                null);

            _records.Add(record);

            return item;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity">Entity to capture</param>
        /// <returns></returns>
        public CommandBufferItem Capture(EntityId entity)
        {
            return Capture([entity]);
        }

        /// <summary>
        /// Captures entites to be operated on in this command buffer
        /// </summary>
        /// <param name="entities">Entites to capture</param>
        /// <returns>The item representing the collection of entites in the command buffer</returns>
        /// <exception cref="ArgumentException">Thrown the span is empty</exception>
        public CommandBufferItem Capture(ReadOnlySpan<EntityId> entities)
        {
            CheckRecordingStarted();
            if (entities.Length == 0)
            {
                throw new ArgumentException("span must not be empty", nameof(entities), null);
            }

            int index = _records.Count;
            var item = new CommandBufferItem(index, entities.Length);
            var record = new CommandBufferRecord(MaskFlags.Captured,
                item,
                new BitMask(),
                new BitMask(),
                new BitMask(),
                new BitMask(),
                ArrayPool<EntityId>.Shared.Rent(entities.Length),
                null);

            _records.Add(record);

            return item;
        }

        public void Destroy(CommandBufferItem item)
        {
            CheckRecordingStarted();
            GetEntityRecord(item).Mask |= MaskFlags.Destroy;
        }

        public void AddComponent<T>(CommandBufferItem item) where T : struct, IComponent<T>
        {
            CheckRecordingStarted();
            ref var record = ref GetEntityRecord(item);
            record.Mask |= MaskFlags.AddedComponent;
            int id = World.GetOrCreateComponentId<T>();

            //Was it already added?
            if (record.ComponentsAdded.IsSet(id))
            {
                throw new DuplicateComponentException();
            }
            //Was it captured?
            if (record.Mask.HasFlag(MaskFlags.Captured))
            {
                record.ComponentsRemoved!.ClearBit(id);
            }
            record.ComponentsAdded.SetBit(id);
        }

        public void AddComponent<T>(CommandBufferItem item, T component) where T : struct, IComponent<T>
        {
            CheckRecordingStarted();
            ref var record = ref GetEntityRecord(item);
            record.Mask |= MaskFlags.AddedComponent;
            int id = World.GetOrCreateComponentId<T>();
            if (record.ComponentsAdded.IsSet(id))
            {
                throw new DuplicateComponentException();
            }
            if (record.Mask.HasFlag(MaskFlags.Captured))
            {
                record.ComponentsRemoved!.ClearBit(id);
            }
            if (record.ComponentValuesAdded == null)
            {
                record.ComponentValuesAdded = new();
            }
            record.ComponentsAdded.SetBit(id);
            record.ComponentValuesAdded.Add(item.Id, component);
        }

        public void RemoveComponent<T>(CommandBufferItem item) where T : struct, IComponent<T>
        {
            CheckRecordingStarted();
            ref var record = ref GetEntityRecord(item);
            int id = World.GetOrCreateComponentId<T>();

            //Was it captured?
            if (record.Mask.HasFlag(MaskFlags.Captured))
            {
                //NOTE: Ideally we would check if the component is present on the entity,
                //however the entity may not yet have the component when this function is called.
                //Thus we defer validation to the actual World.RemoveComponent call in execute
                record.Mask |= MaskFlags.RemovedComponent;
                //Was Component already removed?
                if (record.ComponentsRemoved!.IsSet(id))
                {
                    throw new MissingComponentException();
                }
                record.ComponentsRemoved!.SetBit(id);
            }
            else
            {
                //NOTE: This is a new entity, so we know all components present deterministically.
                //This allows us to perfome validation directly in place

                //Component was not added before
                if (!record.ComponentsAdded.IsSet(id))
                {
                    throw new MissingComponentException();
                }
            }
            record.ComponentsAdded.ClearBit(id);
            if (record.ComponentValuesAdded != null)
            {
                record.ComponentValuesAdded.Remove<T>(item.Id);
            }
        }

        public void AddTag<T>(CommandBufferItem item) where T : struct, ITag<T>
        {
            CheckRecordingStarted();
            ref var record = ref GetEntityRecord(item);
            record.Mask |= MaskFlags.AddedTag;
            int id = World.GetOrCreateTagId<T>();
            if (record.TagsAdded.IsSet(id))
            {
                throw new DuplicateTagException();
            }
            if (record.Mask.HasFlag(MaskFlags.Captured))
            {
                record.TagsRemoved!.ClearBit(id);
            }
            record.TagsAdded.SetBit(id);
        }

        public void RemoveTag<T>(CommandBufferItem item) where T : struct, ITag<T>
        {
            CheckRecordingStarted();
            ref var record = ref GetEntityRecord(item);
            int id = World.GetOrCreateTagId<T>();
            if (record.Mask.HasFlag(MaskFlags.Captured))
            {
                record.Mask |= MaskFlags.RemovedTag;
                if (record.TagsRemoved!.IsSet(id))
                {
                    throw new MissingComponentException();
                }
                record.TagsRemoved!.SetBit(id);
            }
            else
            {
                if (!record.TagsAdded.IsSet(id))
                {
                    throw new MissingComponentException();
                }
            }
            record.TagsAdded.ClearBit(id);
        }

        public void Execute()
        {
            if (_recordingStarted) throw new InvalidOperationException("Must call End before executing commands");

            var items = _records.AsSpan();
            for (int i = 0; i < items.Length; i++)
            {
                ref var record = ref items[i];

                if (record.Mask.HasFlag(MaskFlags.Create))
                {
                    ProcessRecordCreated(ref record);
                }
                else if (record.Mask.HasFlag(MaskFlags.Captured))
                {
                    ProcessRecordCaptured(ref record);
                }
            }
        }

        private void ProcessRecordCaptured(ref CommandBufferRecord record)
        {
            //NOTE: record.Entities must be returned to array pool
            //NOTE: this method can throw
            for (int i = 0; i < record.Entities!.Length; i++)
            {
                var ent = record.Entities[i];
                var srcArch = _world.GetArchetype(ent);
                bool hasTags = srcArch.HasComponent<TagBearer>();
                if (!hasTags && record.TagsAdded.HasAnySet())
                {
                    if (record.TagsRemoved!.HasAnySet())
                    {
                        throw new MissingTagException($"A component was not present on the entity: {_world.GetEntity(ent)}");
                    }
                    record.ComponentsAdded.SetBit(World.GetOrCreateComponentId<TagBearer>());
                    hasTags = true;
                }

                _world.MoveArchetypeInternal(ent, srcArch, record.ComponentsAdded, record.ComponentsRemoved!);
                if (record.ComponentValuesAdded != null)
                {
                    _world.SetValues(ent, record.ComponentValuesAdded.valuesSet);
                }
                _world.InvokeComponentsRemoveEvent(ent, record.ComponentsRemoved!);
                _world.InvokeComponentsAddEvent(ent, record.ComponentsAdded);
                if (hasTags)
                {
                    ref var tagBearer = ref _world.GetComponent<TagBearer>(ent);
                    //Check if the components that we want to remove exist!
                    if (!tagBearer.mask.AreSet(record.ComponentsRemoved!))
                    {
                        throw new MissingTagException($"A component was not present on the entity: {_world.GetEntity(ent)}");
                    }

                    //Check if the components that we want to add don't exist!
                    if (tagBearer.mask.AreSet(record.TagsAdded!))
                    {
                        throw new DuplicateTagException($"A component already present on the entity: {_world.GetEntity(ent)}");
                    }
                    tagBearer.mask.ClearBits(record.TagsRemoved!);
                    tagBearer.mask.OrBits(record.TagsAdded!);
                    _world.InvokeTagsRemoveEvent(ent, record.TagsRemoved!);
                    _world.InvokeTagsAddEvent(ent, record.TagsAdded);
                }
            }


            ArrayPool<EntityId>.Shared.Return(record.Entities!);
        }

        private void ProcessRecordCreated(ref CommandBufferRecord record)
        {
            //Our user is an idiot, do nothing
            if (record.Mask.HasFlag(MaskFlags.Destroy)) return;

            var tagId = World.GetOrCreateComponentId<TagBearer>();

            bool hasTags = record.TagsAdded.HasAnySet();
            //If we have any tags encode that in the mask
            if (hasTags) { record.ComponentsAdded.SetBit(tagId); }

            //Create definition from mask
            var componentsArchDef = ArchetypeDefinition.FromMask(record.ComponentsAdded);
            var entities = _world.CreateEntities(componentsArchDef, record.Item.Count);
            var arch = _world.GetArchetype(componentsArchDef)!;
            if (hasTags)
            {
                arch.Lock();
                var index = arch.GetComponentIndex(tagId);
                var tagBearersPool = arch.componentPools[index];
                var firstIndex = _world.GetEntityIndexRecord(new EntityId(entities.Start)).ArchetypeColumn;
                Span<TagBearer> tagBearers = MemoryMarshal.CreateSpan(ref tagBearersPool.GetRefAt<TagBearer>(firstIndex), entities.Count);
                for (int i = 0; i < tagBearers.Length; i++)
                {
                    tagBearers[i].mask.OverrideUL(record.TagsAdded);
                }
                _world.InvokeTagsSequenceAddEvent(entities, record.TagsAdded);

                arch.Unlock();
            }

            if (record.ComponentValuesAdded != null)
            {
                for (int i = 0; i < entities.Count; i++)
                {
                    _world.SetValues(new EntityId(entities.Start + i), record.ComponentValuesAdded.valuesSet);
                }
            }
        }

        private void CheckRecordingStarted()
        {
            if (!_recordingStarted) throw new InvalidOperationException("Must call Begin before recording commands");
        }

        private ref CommandBufferRecord GetEntityRecord(CommandBufferItem item)
        {
            ref CommandBufferRecord record = ref _records.AsSpan()[item.Id];
#if DEBUG || ACCESS_CHECKS
            if ((record.Mask & (MaskFlags.Create | MaskFlags.Captured)) == 0)
            {
                throw new ArgumentException($"Entity with id {item.Id} is not created or captured");
            }
#endif
            return ref record;
        }
    }
}
