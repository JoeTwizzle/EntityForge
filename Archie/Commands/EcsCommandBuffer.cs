using Archie.Collections;
using Archie.Collections.Generic;
using Archie.Helpers;

namespace Archie.Commands
{
    [Flags]
    enum CommandKind : int
    {
        None = 0,
        Create = 1,
        Move = 2,
        Destroy = 4,
        NoOp = 8
    }

    internal struct Command
    {
        public CommandKind CommandKind;
        public Archetype? Src;
        public Archetype? Dest;
    }

    public sealed class EcsCommandBuffer : IDisposable
    {
        //indexed by entity Id
        readonly UnsafeSparseSet<Command> Commands;

        readonly MultiComponentList ComponentList;

        public EcsCommandBuffer()
        {
            Commands = new();
            ComponentList = new();
        }

        public void Create(Archetype dest, EntityId entity)
        {
            ref var command = ref Commands.Add(entity.Id);

            if (command.CommandKind != CommandKind.None)
            {
                ThrowHelper.ThrowInvalidOperationException("Cannot create already existing Entity");
            }
            command.Dest = dest;
            command.CommandKind = CommandKind.Create;
        }

        public void Add(Archetype src, Archetype dest, EntityId entity)
        {
            ref var command = ref Commands.Get(entity.Id);

            if (command.CommandKind == CommandKind.NoOp || command.CommandKind == CommandKind.Destroy)
            {
                ThrowHelper.ThrowInvalidOperationException("Cannot access already Destroyed Entity");
            }

            if (command.CommandKind != CommandKind.Create)
            {
                command.CommandKind = CommandKind.Move;
                command.Src = src;
            }

            command.Dest = dest;
        }

        public void Add<T>(Archetype src, Archetype dest, EntityId entity, T value) where T : struct, IComponent<T>
        {
            ref var command = ref Commands.Get(entity.Id);

            if (command.CommandKind == CommandKind.NoOp || command.CommandKind == CommandKind.Destroy)
            {
                ThrowHelper.ThrowInvalidOperationException("Cannot access already Destroyed Entity");
            }

            if (command.CommandKind != CommandKind.Create)
            {
                command.CommandKind = CommandKind.Move;
                command.Src = src;
            }

            command.Dest = dest;

            ComponentList.Add(entity.Id, value);
        }

        public void Set<T>(EntityId entity, T value) where T : struct, IComponent<T>
        {
            ComponentList.Add(entity.Id, value);
        }

        public void Remove<T>(Archetype src, Archetype dest, EntityId entity) where T : struct, IComponent<T>
        {
            ref var command = ref Commands.Get(entity.Id);

            if (command.CommandKind == CommandKind.NoOp || command.CommandKind == CommandKind.Destroy)
            {
                ThrowHelper.ThrowInvalidOperationException("Cannot access already Destroyed Entity");
            }

            if (command.CommandKind != CommandKind.Create)
            {
                command.CommandKind = CommandKind.Move;
                command.Src = src;
            }

            command.Dest = dest;

            ComponentList.Remove<T>(entity.Id);
        }

        public void Remove(Archetype src, Archetype dest, EntityId entity, ComponentInfo info)
        {
            ref var command = ref Commands.Get(entity.Id);

            if (command.CommandKind == CommandKind.NoOp || command.CommandKind == CommandKind.Destroy)
            {
                ThrowHelper.ThrowInvalidOperationException("Cannot access already Destroyed Entity");
            }

            if (command.CommandKind != CommandKind.Create)
            {
                command.CommandKind = CommandKind.Move;
                command.Src = src;
            }

            command.Dest = dest;

            ComponentList.Remove(entity.Id, info);
        }

        public void Destroy(Archetype src, EntityId entity)
        {
            ref var command = ref Commands.Get(entity.Id);

            if (command.CommandKind == CommandKind.NoOp)
            {
                ThrowHelper.ThrowInvalidOperationException("Cannot access already Destroyed Entity");
            }

            if (command.CommandKind == CommandKind.Create)
            {
                command.CommandKind = CommandKind.NoOp;
                command.Dest = null;
            }
        }

        public void Execute(World world)
        {
            var data = Commands.GetDenseData();
            var entities = Commands.GetIndexData();
            for (int i = 0; i < data.Length; i++)
            {
                ref var cmd = ref data[i];
                if ((cmd.CommandKind & (CommandKind.Move | CommandKind.Destroy | CommandKind.Create)) != 0)
                {
                    if ((!cmd.Src?.IsLocked ?? true) && (!cmd.Dest?.IsLocked ?? true))
                    {
                        switch (cmd.CommandKind)
                        {
                            case CommandKind.Create:
                                cmd.Dest!.AddEntityInternal(new Entity(entities[i], world.WorldId));
                                break;
                            case CommandKind.Move:
                                world.MoveEntity(cmd.Src!, cmd.Dest!, new EntityId(entities[i]));
                                world.SetValues(new EntityId(entities[i]), ComponentList.valuesSet);
                                break;
                            case CommandKind.Destroy:
                                ref var entityIndex = ref world.GetEntityIndexRecord(new EntityId(entities[i]));
                                world.DeleteEntityInternal(cmd.Src!, entityIndex.ArchetypeColumn);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        public void Clear()
        {

        }

        public void Dispose()
        {
            Commands.Dispose();
            ComponentList.Dispose();
        }
    }
}
