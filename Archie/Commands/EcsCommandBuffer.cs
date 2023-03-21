using Archie.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Commands
{

    internal class EcsCommandBuffer
    {
        internal struct EcsCommand
        {
            public int CommandType;
            public int CommandParamA;
            public int CommandParamB;
            public int CommandParamC;

            public EcsCommand(int commandType, int commandParamA = 0, int commandParamB = 0, int commandParamC = 0)
            {
                CommandType = commandType;
                CommandParamA = commandParamA;
                CommandParamB = commandParamB;
                CommandParamC = commandParamC;
            }

            public static EcsCommand CreateEntity()
            {
                return new EcsCommand(CreateEntityCommandType);
            }

            public static EcsCommand DestroyEntity(int e)
            {
                return new EcsCommand(CreateEntityCommandType, e);
            }
        }

        internal const int CreateEntityCommandType = 0;
        internal const int DestroyEntityCommandType = 1;
        internal const int AddComponentCommandType = 2;
        internal const int AddComponentValueCommandType = 3;
        internal const int RemoveComponentCommandType = 4;
        internal const int SetComponentCommandType = 5;
        internal const int SetComponentValueCommandType = 6;
        internal const int UnsetComponentCommandType = 7;

        int entityCount;
        EcsCommand[] Commands;

        int commandCount;
        int componentCount;

        public EntityId CreateEntity()
        {
            int ent = entityCount++;
            AddCommand(EcsCommand.CreateEntity());
            return new EntityId(ent);
        }

        public void DestroyEntity(int entity)
        {

        }


        void AddCommand(in EcsCommand cmd)
        {
            Commands = Commands.GrowIfNeeded(commandCount, 1);
            Commands[commandCount++] = cmd;
        }

        void Process()
        {
            for (int i = 0; i < Commands.Length; i++)
            {
                var cmd = Commands[i];

                switch (cmd.CommandType)
                {
                    case CreateEntityCommandType:


                        break;
                    case DestroyEntityCommandType:


                        break;
                    default:
                        break;
                }
            }
        }
    }
}
