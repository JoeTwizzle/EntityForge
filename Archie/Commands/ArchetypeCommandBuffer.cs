﻿using Archie.Collections;
using Archie.Collections.Generic;
using Archie.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Commands
{
    internal class ArchetypeCommandBuffer : IDisposable
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
            internal Archetype? archetype;
        }
        private int added;
        private readonly object accessLock = new();
        public readonly UnsafeList<Command> Commands;
        public readonly MultiComponentList ComponentList;
        public ArchetypeCommandBuffer()
        {
            ComponentList = new();
            Commands = new();
        }

        public int Create(int slot, EntityId id)
        {
            lock (accessLock)
            {
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

        public bool Destroy(int slot)
        {
            lock (accessLock)
            {
                ref var cmd = ref Commands.GetOrAdd(slot);
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
                ref var cmd = ref Commands.GetOrAdd(slot);
                cmd.entityId = id;
                cmd.archetype = dest;
                if (cmd.commandType == CommandType.Create)
                {
                    cmd.commandType = CommandType.CreateMove;
                }
            }
        }

        public Archetype? GetArchetype(int slot)
        {
            lock (accessLock)
            {
                if (Commands.Length > slot)
                {
                    return Commands.GetOrAdd(slot).archetype;
                }
                return null;
            }
        }

        public void SetValue<T>(int slot, T value) where T : struct, IComponent<T>
        {
            lock (accessLock)
            {
                ComponentList.Add(slot, value);
            }
        }

        public void UnsetValue<T>(int slot) where T : struct, IComponent<T>
        {
            lock (accessLock)
            {
                ComponentList.Remove<T>(slot);
            }
        }

        public void UnsetValue(int slot, ComponentInfo info)
        {
            lock (accessLock)
            {
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
            archetype.GrowBy(added);
            lock (accessLock)
            {
                
                added = 0;
                var cmds = Commands.GetData();
                for (int i = 0; i < cmds.Length; i++)
                {
                    var cmd = cmds[i];
                    switch (cmd.commandType)
                    {
                        case CommandType.Create:
                            archetype.AddEntityInternal(new Entity(cmd.entityId.Id, world.WorldId));
                            break;
                        case CommandType.CreateMove:
                            archetype.AddEntityInternal(new Entity(cmd.entityId.Id, world.WorldId));
                            world.MoveEntity(archetype, cmd.archetype!, cmd.entityId);
                            break;
                        case CommandType.Move:
                            world.MoveEntity(archetype, cmd.archetype!, cmd.entityId);
                            break;
                        case CommandType.Destroy:
                            world.DeleteEntityInternal(archetype, i);
                            break;
                        default:
                            break;
                    }
                    world.SetValues(cmd.entityId, ComponentList.valuesSet);
                    cmds[i] = default;
                }
            }
        }


    }
}