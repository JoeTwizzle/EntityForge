using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    partial class World
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query<T, T1>(ComponentMask mask, ref T forEach) where T : struct, IComponentQuery<T1> where T1 : struct, IComponent<T1>
        {
            var filter = Filter(mask);
            for (int i = 0; i < archetypeCount; i++)
            {
                var arch = AllArchetypes[i];
                if (filter.Matches(arch.BitMask))
                {
                    int count = (int)arch.entityCount;
                    var items1 = new Span<T1>((T1[])arch.PropertyPool[ComponentIndex[typeof(T1)][arch.Index].ComponentTypeIndex], 0, count);
                    for (int j = 0; j < count; j++)
                    {
                        forEach.Process(ref items1[j]);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query<T, T1, T2>(ComponentMask mask, ref T forEach) where T : struct, IComponentQuery<T1, T2> where T1 : struct, IComponent<T1> where T2 : struct, IComponent<T2>
        {
            var filter = Filter(mask);
            for (int i = 0; i < archetypeCount; i++)
            {
                var arch = AllArchetypes[i];
                if (filter.Matches(arch.BitMask))
                {
                    int count = (int)arch.entityCount;
                    var items1 = new Span<T1>((T1[])arch.PropertyPool[ComponentIndex[typeof(T1)][arch.Index].ComponentTypeIndex], 0, count);
                    var items2 = new Span<T2>((T2[])arch.PropertyPool[ComponentIndex[typeof(T2)][arch.Index].ComponentTypeIndex], 0, count);
                    for (int j = 0; j < count; j++)
                    {
                        forEach.Process(ref items1[j], ref items2[j]);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query<T, T1, T2, T3>(ComponentMask mask, ref T forEach) where T : struct, IComponentQuery<T1, T2, T3> where T1 : struct, IComponent<T1> where T2 : struct, IComponent<T2> where T3 : struct, IComponent<T3>
        {
            var filter = Filter(mask);
            for (int i = 0; i < archetypeCount; i++)
            {
                var arch = AllArchetypes[i];
                if (filter.Matches(arch.BitMask))
                {
                    int count = (int)arch.entityCount;
                    var items1 = new Span<T1>((T1[])arch.PropertyPool[ComponentIndex[typeof(T1)][arch.Index].ComponentTypeIndex], 0, count);
                    var items2 = new Span<T2>((T2[])arch.PropertyPool[ComponentIndex[typeof(T2)][arch.Index].ComponentTypeIndex], 0, count);
                    var items3 = new Span<T3>((T3[])arch.PropertyPool[ComponentIndex[typeof(T3)][arch.Index].ComponentTypeIndex], 0, count);
                    for (int j = 0; j < count; j++)
                    {
                        forEach.Process(ref items1[j], ref items2[j], ref items3[j]);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query<T, T1, T2, T3, T4>(ComponentMask mask, ref T forEach) where T : struct, IComponentQuery<T1, T2, T3, T4> where T1 : struct, IComponent<T1> where T2 : struct, IComponent<T2> where T3 : struct, IComponent<T3> where T4 : struct, IComponent<T4>
        {
            var filter = Filter(mask);
            for (int i = 0; i < archetypeCount; i++)
            {
                var arch = AllArchetypes[i];
                if (filter.Matches(arch.BitMask))
                {
                    int count = (int)arch.entityCount;
                    var items1 = new Span<T1>((T1[])arch.PropertyPool[ComponentIndex[typeof(T1)][arch.Index].ComponentTypeIndex], 0, count);
                    var items2 = new Span<T2>((T2[])arch.PropertyPool[ComponentIndex[typeof(T2)][arch.Index].ComponentTypeIndex], 0, count);
                    var items3 = new Span<T3>((T3[])arch.PropertyPool[ComponentIndex[typeof(T3)][arch.Index].ComponentTypeIndex], 0, count);
                    var items4 = new Span<T4>((T4[])arch.PropertyPool[ComponentIndex[typeof(T4)][arch.Index].ComponentTypeIndex], 0, count);
                    for (int j = 0; j < count; j++)
                    {
                        forEach.Process(ref items1[j], ref items2[j], ref items3[j], ref items4[j]);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Query<T, T1, T2, T3, T4, T5>(ComponentMask mask, ref T forEach) where T : struct, IComponentQuery<T1, T2, T3, T4, T5> where T1 : struct, IComponent<T1> where T2 : struct, IComponent<T2> where T3 : struct, IComponent<T3> where T4 : struct, IComponent<T4> where T5 : struct, IComponent<T5>
        {
            var filter = Filter(mask);
            for (int i = 0; i < archetypeCount; i++)
            {
                var arch = AllArchetypes[i];
                if (filter.Matches(arch.BitMask))
                {
                    int count = (int)arch.entityCount;
                    var items1 = new Span<T1>((T1[])arch.PropertyPool[ComponentIndex[typeof(T1)][arch.Index].ComponentTypeIndex], 0, count);
                    var items2 = new Span<T2>((T2[])arch.PropertyPool[ComponentIndex[typeof(T2)][arch.Index].ComponentTypeIndex], 0, count);
                    var items3 = new Span<T3>((T3[])arch.PropertyPool[ComponentIndex[typeof(T3)][arch.Index].ComponentTypeIndex], 0, count);
                    var items4 = new Span<T4>((T4[])arch.PropertyPool[ComponentIndex[typeof(T4)][arch.Index].ComponentTypeIndex], 0, count);
                    var items5 = new Span<T5>((T5[])arch.PropertyPool[ComponentIndex[typeof(T5)][arch.Index].ComponentTypeIndex], 0, count);
                    for (int j = 0; j < count; j++)
                    {
                        forEach.Process(ref items1[j], ref items2[j], ref items3[j], ref items4[j], ref items5[j]);
                    }
                }
            }
        }
    }
}
