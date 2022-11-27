using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public sealed class ComponentMask : IDisposable
    {
        readonly World world;
        internal Type[] Included;
        internal Type[] Excluded;
        int includedCount;
        int excludedCount;

        internal ComponentMask(World world)
        {
            this.world = world;
            this.Included = ArrayPool<Type>.Shared.Rent(8);
            this.Excluded = ArrayPool<Type>.Shared.Rent(8);
        }

        public ComponentMask Inc<T>() where T : struct, IComponent<T>
        {
            Included[includedCount++] = typeof(T);
            ResizeIncIfNeeded();
            return this;
        }

        public ComponentMask Exc<T>() where T : struct, IComponent<T>
        {
            Excluded[excludedCount++] = typeof(T);
            ResizeExcIfNeeded();
            return this;
        }

        void ResizeIncIfNeeded()
        {
            if (Included.Length >= includedCount)
            {
                ArrayPool<Type>.Shared.Return(Included);
                Included = ArrayPool<Type>.Shared.Rent(includedCount * 2);
            }
        }

        void ResizeExcIfNeeded()
        {
            if (Excluded.Length >= excludedCount)
            {
                ArrayPool<Type>.Shared.Return(Excluded);
                Excluded = ArrayPool<Type>.Shared.Rent(excludedCount * 2);
            }
        }

        public EntityFilter End()
        {
            var inc = Included.AsSpan(0, includedCount).ToArray();
            var exc = Included.AsSpan(0, excludedCount).ToArray();
            ArrayPool<Type>.Shared.Return(Included);
            ArrayPool<Type>.Shared.Return(Excluded);
            Included = inc;
            Excluded = exc;
            return new EntityFilter(world, this);
        }


        public void Dispose()
        {
            ArrayPool<Type>.Shared.Return(Included);
            ArrayPool<Type>.Shared.Return(Excluded);
        }
    }
}
