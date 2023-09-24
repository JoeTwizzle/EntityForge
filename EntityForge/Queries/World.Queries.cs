using EntityForge.Collections;
using EntityForge.Queries;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EntityForge
{
    partial class World
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<Archetype> GetMatchingArchetypes(ComponentMask mask)
        {
            return GetArchetypeFilter(mask).MatchingArchetypes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query<T, T1>(ComponentMask mask) where T : struct, IComponentQuery<T1> where T1 : struct, IComponent<T1>
        {
            var forEach = new T();
            Query<T, T1>(mask, ref forEach);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query<T, T1>(ComponentMask mask, ref T forEach) where T : struct, IComponentQuery<T1> where T1 : struct, IComponent<T1>
        {
            var filter = GetArchetypeFilter(mask);
            var archetypes = filter.MatchingArchetypes;
            for (int i = 0; i < archetypes.Length; i++)
            {
                var arch = archetypes[i];
                arch.Lock();
                arch.GetAccess(mask);
                ref var current1 = ref arch.GetRef<T1>(0);
                ref var last1 = ref arch.GetRef<T1>(arch.ElementCount);
                while (Unsafe.IsAddressLessThan(ref current1, ref last1))
                {
                    forEach.Process(ref current1);
                    current1 = ref Unsafe.Add(ref current1, 1);
                }
                arch.ReleaseAccess(mask);
                arch.Unlock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query<T, T1, T2>(ComponentMask mask) where T : struct, IComponentQuery<T1, T2> where T1 : struct, IComponent<T1> where T2 : struct, IComponent<T2>
        {
            var forEach = new T();
            Query<T, T1, T2>(mask, ref forEach);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query<T, T1, T2>(ComponentMask mask, ref T forEach) where T : struct, IComponentQuery<T1, T2> where T1 : struct, IComponent<T1> where T2 : struct, IComponent<T2>
        {
            var filter = GetArchetypeFilter(mask);
            var archetypes = filter.MatchingArchetypes;
            for (int i = 0; i < archetypes.Length; i++)
            {
                var arch = archetypes[i];
                arch.Lock();
                arch.GetAccess(mask);
                int count = (int)arch.ElementCount;
                ref var current1 = ref arch.GetRef<T1>(0);
                ref var current2 = ref arch.GetRef<T2>(0);
                ref var last1 = ref arch.GetRef<T1>(arch.ElementCount);
                while (Unsafe.IsAddressLessThan(ref current1, ref last1))
                {
                    forEach.Process(ref current1, ref current2);
                    current1 = ref Unsafe.Add(ref current1, 1);
                    current2 = ref Unsafe.Add(ref current2, 1);
                }
                arch.ReleaseAccess(mask);
                arch.Unlock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query<T, T1, T2, T3>(ComponentMask mask) where T : struct, IComponentQuery<T1, T2, T3> where T1 : struct, IComponent<T1> where T2 : struct, IComponent<T2> where T3 : struct, IComponent<T3>
        {
            var forEach = new T();
            Query<T, T1, T2, T3>(mask, ref forEach);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query<T, T1, T2, T3>(ComponentMask mask, ref T forEach) where T : struct, IComponentQuery<T1, T2, T3> where T1 : struct, IComponent<T1> where T2 : struct, IComponent<T2> where T3 : struct, IComponent<T3>
        {
            var filter = GetArchetypeFilter(mask);
            var archetypes = filter.MatchingArchetypes;
            for (int i = 0; i < archetypes.Length; i++)
            {
                var arch = archetypes[i];
                arch.Lock();
                arch.GetAccess(mask);
                int count = (int)arch.ElementCount;
                ref var current1 = ref arch.GetRef<T1>(0);
                ref var current2 = ref arch.GetRef<T2>(0);
                ref var current3 = ref arch.GetRef<T3>(0);
                ref var last1 = ref arch.GetRef<T1>(arch.ElementCount);
                while (Unsafe.IsAddressLessThan(ref current1, ref last1))
                {
                    forEach.Process(ref current1, ref current2, ref current3);
                    current1 = ref Unsafe.Add(ref current1, 1);
                    current2 = ref Unsafe.Add(ref current2, 1);
                    current3 = ref Unsafe.Add(ref current3, 1);
                }
                arch.ReleaseAccess(mask);
                arch.Unlock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query<T, T1, T2, T3, T4>(ComponentMask mask) where T : struct, IComponentQuery<T1, T2, T3, T4> where T1 : struct, IComponent<T1> where T2 : struct, IComponent<T2> where T3 : struct, IComponent<T3> where T4 : struct, IComponent<T4>
        {
            var forEach = new T();
            Query<T, T1, T2, T3, T4>(mask, ref forEach);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query<T, T1, T2, T3, T4>(ComponentMask mask, ref T forEach) where T : struct, IComponentQuery<T1, T2, T3, T4> where T1 : struct, IComponent<T1> where T2 : struct, IComponent<T2> where T3 : struct, IComponent<T3> where T4 : struct, IComponent<T4>
        {
            var filter = GetArchetypeFilter(mask);
            var archetypes = filter.MatchingArchetypes;
            for (int i = 0; i < archetypes.Length; i++)
            {
                var arch = archetypes[i];
                arch.Lock();
                arch.GetAccess(mask);
                int count = (int)arch.ElementCount;
                ref var current1 = ref arch.GetRef<T1>(0);
                ref var current2 = ref arch.GetRef<T2>(0);
                ref var current3 = ref arch.GetRef<T3>(0);
                ref var current4 = ref arch.GetRef<T4>(0);
                ref var last1 = ref arch.GetRef<T1>(arch.ElementCount);
                while (Unsafe.IsAddressLessThan(ref current1, ref last1))
                {
                    forEach.Process(ref current1, ref current2, ref current3, ref current4);
                    current1 = ref Unsafe.Add(ref current1, 1);
                    current2 = ref Unsafe.Add(ref current2, 1);
                    current3 = ref Unsafe.Add(ref current3, 1);
                    current4 = ref Unsafe.Add(ref current4, 1);
                }
                arch.ReleaseAccess(mask);
                arch.Unlock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query<T, T1, T2, T3, T4, T5>(ComponentMask mask) where T : struct, IComponentQuery<T1, T2, T3, T4, T5> where T1 : struct, IComponent<T1> where T2 : struct, IComponent<T2> where T3 : struct, IComponent<T3> where T4 : struct, IComponent<T4> where T5 : struct, IComponent<T5>
        {
            var forEach = new T();
            Query<T, T1, T2, T3, T4, T5>(mask, ref forEach);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query<T, T1, T2, T3, T4, T5>(ComponentMask mask, ref T forEach) where T : struct, IComponentQuery<T1, T2, T3, T4, T5> where T1 : struct, IComponent<T1> where T2 : struct, IComponent<T2> where T3 : struct, IComponent<T3> where T4 : struct, IComponent<T4> where T5 : struct, IComponent<T5>
        {
            var filter = GetArchetypeFilter(mask);
            var archetypes = filter.MatchingArchetypes;
            for (int i = 0; i < archetypes.Length; i++)
            {
                var arch = archetypes[i];
                arch.Lock();
                arch.GetAccess(mask);
                int count = (int)arch.ElementCount;
                ref var current1 = ref arch.GetRef<T1>(0);
                ref var current2 = ref arch.GetRef<T2>(0);
                ref var current3 = ref arch.GetRef<T3>(0);
                ref var current4 = ref arch.GetRef<T4>(0);
                ref var current5 = ref arch.GetRef<T5>(0);
                ref var last1 = ref arch.GetRef<T1>(arch.ElementCount);
                while (Unsafe.IsAddressLessThan(ref current1, ref last1))
                {
                    forEach.Process(ref current1, ref current2, ref current3, ref current4, ref current5);
                    current1 = ref Unsafe.Add(ref current1, 1);
                    current2 = ref Unsafe.Add(ref current2, 1);
                    current3 = ref Unsafe.Add(ref current3, 1);
                    current4 = ref Unsafe.Add(ref current4, 1);
                    current5 = ref Unsafe.Add(ref current5, 1);
                }
                arch.ReleaseAccess(mask);
                arch.Unlock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query(ComponentMask mask, Action<ArchetypeView> action)
        {
            var filter = GetArchetypeFilter(mask);
            var archetypes = filter.MatchingArchetypes;
            for (int i = 0; i < archetypes.Length; i++)
            {
                var arch = archetypes[i];
                arch.Lock();
                arch.GetAccess(mask);
                action.Invoke(new ArchetypeView(arch, mask.WriteMask));
                arch.ReleaseAccess(mask);
                arch.Unlock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void QueryParallel(ComponentMask mask, Action<ArchetypeView> action)
        {
            var filter = GetArchetypeFilter(mask);
            Parallel.For(0, filter.MatchCount, i =>
            {
                var archetypes = filter.MatchingArchetypes;
                var arch = archetypes[i];
                arch.Lock();
                arch.GetAccess(mask);
                action.Invoke(new ArchetypeView(arch, mask.WriteMask));
                arch.ReleaseAccess(mask);
                arch.Unlock();
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void QueryFiltered(ComponentMask mask, TagMask tagMask, BitMask filterMask, Action<FilteredArchetypeView> action)
        {
            var filter = GetArchetypeFilter(mask);
            var entityFilter = new EntityFilter(filter, tagMask, filterMask);
            var archetypes = filter.MatchingArchetypes;
            for (int i = 0; i < archetypes.Length; i++)
            {
                var arch = archetypes[i];
                arch.Lock();
                arch.GetAccess(filter.componentMask);
                entityFilter.TagMask.Match(arch, entityFilter._filterMask);
                action.Invoke(new FilteredArchetypeView(arch, filter.componentMask.WriteMask, entityFilter._filterMask, arch.ElementCount));
                arch.ReleaseAccess(filter.componentMask);
                arch.Unlock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void QueryFilteredParallel(ComponentMask mask, TagMask tagMask, BitMask filterMask, Action<FilteredArchetypeView> action)
        {
            var filter = GetArchetypeFilter(mask);
            var entityFilter = new EntityFilter(filter, tagMask, filterMask);
            Parallel.For(0, filter.MatchCount, i =>
            {
                var archetypes = filter.MatchingArchetypes;
                var arch = archetypes[i];
                arch.Lock();
                arch.GetAccess(filter.componentMask);
                entityFilter.TagMask.Match(arch, entityFilter._filterMask);
                action.Invoke(new FilteredArchetypeView(arch, filter.componentMask.WriteMask, entityFilter._filterMask, arch.ElementCount));
                arch.ReleaseAccess(filter.componentMask);
                arch.Unlock();
            });
        }
    }
}
