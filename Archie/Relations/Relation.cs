using Archie.Helpers;
using System.Buffers;
using System.Collections.Generic;

namespace Archie.Relations
{
    struct Relation : IDisposable
    {
        Dictionary<int, int> EntityMap;
        public int Length { get; private set; }
        public Span<int> Targets => new Span<int>(targets, 0, Length);
        int[] targets;

        public void AddTarget(int targetEnt)
        {
            if (targets == null)
            {
                EntityMap = new();
                targets = ArrayPool<int>.Shared.Rent(1);
            }
            targets = targets.GrowIfNeededPooled(Length, 1);
            EntityMap.Add(targetEnt, Length);
            targets[Length++] = targetEnt;
        }

        public void RemoveTarget(int targetEnt)
        {
            int lastEnt = targets[--Length];
            int currentIdx = EntityMap[targetEnt];
            targets[currentIdx] = lastEnt;
            EntityMap.Remove(targetEnt);
            EntityMap[lastEnt] = currentIdx;
        }

        public void Dispose()
        {
            ArrayPool<int>.Shared.Return(targets);
        }
    }
}
