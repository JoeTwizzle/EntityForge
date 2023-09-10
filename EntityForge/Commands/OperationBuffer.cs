using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge.Commands
{
    internal sealed class OperationBuffer
    {
        internal enum EntityOperationKind
        {
            Add,
            Remove,
            AddTag,
            RemoveTag
        }

        internal readonly struct Entry
        {
            public readonly EntityOperationKind Kind;
            public readonly ComponentInfo Info;

            public Entry(EntityOperationKind kind, ComponentInfo info)
            {
                Kind = kind;
                Info = info;
            }
        }

        readonly Dictionary<int, List<Entry>> operations = new();
        public void Add(int entity, EntityOperationKind kind, ComponentInfo info)
        {
            ref var opList = ref CollectionsMarshal.GetValueRefOrAddDefault(operations, entity, out _);
            if (opList == null)
            {
                opList = new List<Entry>();
            }
            opList.Add(new Entry(kind, info));
        }

        public void Add(int entity, Entry entry)
        {
            ref var opList = ref CollectionsMarshal.GetValueRefOrAddDefault(operations, entity, out _);
            if (opList == null)
            {
                opList = new List<Entry>();
            }
            opList.Add(entry);
        }

        public List<Entry>? GetEntries(int entity)
        {
            return operations.GetValueOrDefault(entity);
        }

        public void ClearAll()
        {
            operations.Clear();
        }
    }
}
