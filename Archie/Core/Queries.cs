﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Archie
{
    [CreateQueries]
    partial class World
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Query(Archie.ComponentMask mask, System.Action<ArraySegment<Entity>> action)
        {
            var filter = GetFilter(mask);
            for (int i = 0; i < filter.MatchCount; i++)
            {
                var arch = filter.MatchingArchetypesBuffer[i];
                action.Invoke(new ArraySegment<Entity>(arch.EntitiesBuffer, 0, arch.InternalEntityCount));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<Archetype> GetMatchingArchetypes(ComponentMask mask)
        {
            return GetFilter(mask).MatchingArchetypes;
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
            var filter = GetFilter(mask);
            for (int i = 0; i < filter.MatchCount; i++)
            {
                var arch = filter.MatchingArchetypesBuffer[i];
                int count = (int)arch.InternalEntityCount;
                ref var current1 = ref MemoryMarshal.GetArrayDataReference(arch.DangerousGetPool<T1>());
                ref var last1 = ref Unsafe.Add(ref current1, count);

                while (Unsafe.IsAddressLessThan(ref current1, ref last1))
                {
                    forEach.Process(ref current1);
                    current1 = ref Unsafe.Add(ref current1, 1);
                }
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
            var filter = GetFilter(mask);
            for (int i = 0; i < filter.MatchCount; i++)
            {
                var arch = filter.MatchingArchetypesBuffer[i];
                int count = (int)arch.InternalEntityCount;
                ref var current1 = ref MemoryMarshal.GetArrayDataReference(arch.DangerousGetPool<T1>());
                ref var current2 = ref MemoryMarshal.GetArrayDataReference(arch.DangerousGetPool<T2>());
                ref var last1 = ref Unsafe.Add(ref current1, count);
                while (Unsafe.IsAddressLessThan(ref current1, ref last1))
                {
                    forEach.Process(ref current1, ref current2);
                    current1 = ref Unsafe.Add(ref current1, 1);
                    current2 = ref Unsafe.Add(ref current2, 1);
                }
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
            var filter = GetFilter(mask);
            for (int i = 0; i < filter.MatchCount; i++)
            {
                var arch = filter.MatchingArchetypesBuffer[i];
                int count = (int)arch.InternalEntityCount;
                ref var current1 = ref MemoryMarshal.GetArrayDataReference(arch.DangerousGetPool<T1>());
                ref var current2 = ref MemoryMarshal.GetArrayDataReference(arch.DangerousGetPool<T2>());
                ref var current3 = ref MemoryMarshal.GetArrayDataReference(arch.DangerousGetPool<T3>());
                ref var last1 = ref Unsafe.Add(ref current1, count);
                while (Unsafe.IsAddressLessThan(ref current1, ref last1))
                {
                    forEach.Process(ref current1, ref current2, ref current3);
                    current1 = ref Unsafe.Add(ref current1, 1);
                    current2 = ref Unsafe.Add(ref current2, 1);
                    current3 = ref Unsafe.Add(ref current3, 1);
                }
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
            var filter = GetFilter(mask);
            for (int i = 0; i < filter.MatchCount; i++)
            {
                var arch = filter.MatchingArchetypesBuffer[i];
                int count = (int)arch.InternalEntityCount;
                ref var current1 = ref MemoryMarshal.GetArrayDataReference(arch.DangerousGetPool<T1>());
                ref var current2 = ref MemoryMarshal.GetArrayDataReference(arch.DangerousGetPool<T2>());
                ref var current3 = ref MemoryMarshal.GetArrayDataReference(arch.DangerousGetPool<T3>());
                ref var current4 = ref MemoryMarshal.GetArrayDataReference(arch.DangerousGetPool<T4>());
                ref var last1 = ref Unsafe.Add(ref current1, count);
                while (Unsafe.IsAddressLessThan(ref current1, ref last1))
                {
                    forEach.Process(ref current1, ref current2, ref current3, ref current4);
                    current1 = ref Unsafe.Add(ref current1, 1);
                    current2 = ref Unsafe.Add(ref current2, 1);
                    current3 = ref Unsafe.Add(ref current3, 1);
                    current4 = ref Unsafe.Add(ref current4, 1);
                }
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
            var filter = GetFilter(mask);
            for (int i = 0; i < filter.MatchCount; i++)
            {
                var arch = filter.MatchingArchetypesBuffer[i];
                int count = (int)arch.InternalEntityCount;
                ref var current1 = ref MemoryMarshal.GetArrayDataReference(arch.DangerousGetPool<T1>());
                ref var current2 = ref MemoryMarshal.GetArrayDataReference(arch.DangerousGetPool<T2>());
                ref var current3 = ref MemoryMarshal.GetArrayDataReference(arch.DangerousGetPool<T3>());
                ref var current4 = ref MemoryMarshal.GetArrayDataReference(arch.DangerousGetPool<T4>());
                ref var current5 = ref MemoryMarshal.GetArrayDataReference(arch.DangerousGetPool<T5>());
                ref var last1 = ref Unsafe.Add(ref current1, count);
                while (Unsafe.IsAddressLessThan(ref current1, ref last1))
                {
                    forEach.Process(ref current1, ref current2, ref current3, ref current4, ref current5);
                    current1 = ref Unsafe.Add(ref current1, 1);
                    current2 = ref Unsafe.Add(ref current2, 1);
                    current3 = ref Unsafe.Add(ref current3, 1);
                    current4 = ref Unsafe.Add(ref current4, 1);
                    current5 = ref Unsafe.Add(ref current5, 1);
                }
            }
        }
    }
}
