using EntityForge.Collections;
using EntityForge.Collections.Generic;
using EntityForge.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge.Commands
{
    internal sealed class ArchetypeCommandBuffer : IDisposable
    {
        internal enum CommandType
        {
            None,
            Create,
            CreateMove,
            Move,
            Destroy,
            NoOp
        }

        internal struct Command
        {
            internal CommandType commandType;
            internal EntityId entityId;
            internal int archetype;
        }
        private int added;
        private int reserved;
        private readonly object accessLock = new();
        public readonly UnsafeList<Command> Commands;
        public readonly MultiComponentList ComponentList;
        public int cmdCount;

        public ArchetypeCommandBuffer()
        {
            ComponentList = new();
            Commands = new();
        }

        public void Reserve(int count)
        {
            reserved = count;
        }

        public int Create(int slot, EntityId id)
        {
            lock (accessLock)
            {
                cmdCount++;
                slot += added++;
                ref var cmd = ref Commands.GetOrAdd(slot);
                cmd.entityId = id;
                if (cmd.commandType == CommandType.Create)
                {
                    ThrowHelper.ThrowArgumentException($"Entity at slot {slot} was already created");
                }
                cmd.commandType = CommandType.Create;
                return slot;
            }
        }

        public bool Destroy(int slot, EntityId id)
        {
            lock (accessLock)
            {
                cmdCount++; 
                ref var cmd = ref Commands.GetOrAdd(slot);
                cmd.entityId = id;
                if (cmd.commandType == CommandType.Create)
                {
                    cmd.commandType = CommandType.NoOp;
                    return true;
                }
                else
                {
                    cmd.commandType = CommandType.Destroy;
                    return false;
                }
            }
        }

        public void Move(int slot, Archetype dest, EntityId id)
        {
            lock (accessLock)
            {
                cmdCount++;
                ref var cmd = ref Commands.GetOrAdd(slot);
                cmd.entityId = id;
                cmd.archetype = dest.Index;

                if (cmd.commandType == CommandType.Create)
                {
                    cmd.commandType = CommandType.CreateMove;
                }
                else
                {
                    cmd.commandType = CommandType.Move;
                }
            }
        }

        public Archetype? GetArchetype(World world, int slot)
        {
            lock (accessLock)
            {
                if (Commands.Count > slot)
                {
                    return world.GetArchetypeById(Commands.GetOrAdd(slot).archetype);
                }
                return null;
            }
        }

        public void SetValue<T>(int slot, T value) where T : struct, IComponent<T>
        {
            lock (accessLock)
            {
                cmdCount++;
                ComponentList.Add(slot, value);
            }
        }

        public void UnsetValue<T>(int slot) where T : struct, IComponent<T>
        {
            lock (accessLock)
            {
                cmdCount++;
                ComponentList.Remove<T>(slot);
            }
        }

        public void UnsetValue(int slot, ComponentInfo info)
        {
            lock (accessLock)
            {
                cmdCount++;
                ComponentList.Remove(slot, info);
            }
        }

        public void Dispose()
        {
            lock (accessLock)
            {
                Commands.Dispose();
                ComponentList.Dispose();
            }
        }

        public void Execute(World world, Archetype archetype)
        {
            archetype.GrowBy(Math.Max(reserved, added));
            lock (accessLock)
            {
                cmdCount = 0;
                added = 0;
                reserved = 0;
                var cmds = Commands.GetData();
                world.worldArchetypesRWLock.EnterReadLock();
                for (int i = 0; i < cmds.Length; i++)
                {
                    var cmd = cmds[i];
                    switch (cmd.commandType)
                    {
                        case CommandType.Create:
                            archetype.AddEntityInternal(new Entity(cmd.entityId.Id, world.WorldId));
                            world.InvokeCreateEntityEvent(cmd.entityId);
                            break;
                        case CommandType.CreateMove:
                            archetype.AddEntityInternal(new Entity(cmd.entityId.Id, world.WorldId));
                            world.InvokeCreateEntityEvent(cmd.entityId);
                            if (archetype.Index != cmd.archetype)
                            {
                                world.MoveEntity(archetype, world.AllArchetypes[cmd.archetype], cmd.entityId);
                            }
                            break;
                        case CommandType.Move:
                            if (archetype.Index != cmd.archetype)
                            {
                                world.MoveEntity(archetype, world.AllArchetypes[cmd.archetype], cmd.entityId);
                            }
                            break;
                        case CommandType.Destroy:
                            world.DeleteEntityInternal(archetype, i);
                            world.InvokeDeleteEntityEvent(cmd.entityId);
                            break;
                        default:
                            break;
                    }
                    world.SetValues(cmd.entityId, ComponentList.valuesSet);
                }
                world.worldArchetypesRWLock.ExitReadLock();
                cmds.Clear();
                ComponentList.ClearValues();
            }
        }
    }
}
