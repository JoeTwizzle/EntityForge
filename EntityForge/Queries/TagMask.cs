using EntityForge.Collections;
using EntityForge.Tags;
using System.Diagnostics.CodeAnalysis;

namespace EntityForge.Queries
{
    public sealed class TagMask
    {
        public readonly BitMask[] SomeTags;
        public readonly BitMask[] NotAllTags;
        public readonly BitMask HasTags;
        public readonly BitMask NoTags;

        internal TagMask(BitMask[] someTags, BitMask[] notAllTags, BitMask hasTags, BitMask noTags)
        {
            SomeTags = someTags;
            NotAllTags = notAllTags;
            HasTags = hasTags;
            NoTags = noTags;
        }

        public void Match(Archetype archetype, BitMask mask)
        {
            if (archetype.TryGetComponentIndex<TagBearer>(out int index))
            {
                var tags = archetype.GetPool<TagBearer>(index);
                for (int i = 0; i < archetype.ElementCount; i++)
                {
                    bool candidate = HasTags.AllMatch(tags[i].mask) && !NoTags.AnyMatch(tags[i].mask);
                    if (!candidate)
                    {
                        continue;
                    }
                    for (int j = 0; j < SomeTags.Length && candidate; j++)
                    {
                        candidate &= SomeTags[j].AnyMatch(tags[i].mask);
                    }
                    for (int j = 0; j < NotAllTags.Length && candidate; j++)
                    {
                        candidate &= !NotAllTags[j].AllMatch(tags[i].mask);
                    }

                    if (candidate)
                    {
                        mask.SetBit(i);
                    }
                    else
                    {
                        mask.ClearBit(i);
                    }
                }
            }
            else
            {
                bool candidate = HasTags.IsAllZeros();
                for (int i = 0; i < SomeTags.Length; i++)
                {
                    candidate &= SomeTags[i].IsAllZeros();
                }
                if (candidate)
                {
                    mask.SetRange(0, archetype.ElementCount);
                }
            }
        }

        public override string? ToString()
        {
            return $"""
            Has: {HasTags.ToString()}
            Has not: {NoTags.ToString()}
            Some: {string.Join<BitMask>(" - ", SomeTags)}
            Not all: {string.Join<BitMask>(" - ", NotAllTags)}
            """;
        }

        public static TagMaskBuilder Create()
        {
            return new TagMaskBuilder(new List<BitMask>(), new List<BitMask>(), new(), new());
        }

#pragma warning disable CA1034 // Nested types should not be visible
        public struct TagMaskBuilder : IEquatable<TagMaskBuilder>
        {
            internal readonly List<BitMask> SomeMasks;
            internal readonly List<BitMask> NoneMasks;
            internal readonly BitMask HasMask;
            internal readonly BitMask ExcludeMask;

            internal TagMaskBuilder(List<BitMask> someMasks, List<BitMask> noneMasks, BitMask hasMask, BitMask excludeMask)
            {
                SomeMasks = someMasks;
                NoneMasks = noneMasks;
                HasMask = hasMask;
                ExcludeMask = excludeMask;
            }

            [UnscopedRef]
            public ref TagMaskBuilder Has<T>() where T : struct, ITag<T>
            {
                HasMask.SetBit(World.GetOrCreateTagId<T>());
                return ref this;
            }

            [UnscopedRef]
            public ref TagMaskBuilder HasNot<T>() where T : struct, ITag<T>
            {
                ExcludeMask.SetBit(World.GetOrCreateTagId<T>());
                return ref this;
            }

            public SomeMaskBuilder Some()
            {
                return new SomeMaskBuilder(this, new(), new());
            }

            public NotAllMaskBuilder NotAll()
            {
                return new NotAllMaskBuilder(this, new());
            }

            public TagMask End()
            {
                return new TagMask(SomeMasks.ToArray(), NoneMasks.ToArray(), HasMask, ExcludeMask); //TODO
            }

            public override bool Equals(object? obj)
            {
                return obj is TagMaskBuilder builder && Equals(builder);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(SomeMasks, NoneMasks, HasMask, ExcludeMask);
            }

            public bool Equals(TagMaskBuilder other)
            {
                bool hasSome = SomeMasks.Count == other.SomeMasks.Count;
                if (hasSome)
                {
                    for (int i = 0; i < SomeMasks.Count; i++)
                    {
                        hasSome &= SomeMasks[i].Equals(other.SomeMasks[i]);
                    }
                }
                return hasSome && other.HasMask.Equals(HasMask) && other.ExcludeMask.Equals(ExcludeMask);
            }

            public static bool operator ==(TagMaskBuilder left, TagMaskBuilder right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(TagMaskBuilder left, TagMaskBuilder right)
            {
                return !(left == right);
            }
        }

        public struct SomeMaskBuilder : IEquatable<SomeMaskBuilder>
        {
            internal readonly BitMask SomeMask;
            internal readonly BitMask WriteMask;
            private readonly TagMaskBuilder parent;

            public SomeMaskBuilder(TagMaskBuilder parent, BitMask someMask, BitMask accessMask)
            {
                this.parent = parent;
                SomeMask = someMask;
                WriteMask = accessMask;
            }

            [UnscopedRef]
            public ref SomeMaskBuilder Has<T>() where T : struct, ITag<T>
            {
                SomeMask.SetBit(World.GetOrCreateTagId<T>());
                return ref this;
            }

            public TagMaskBuilder EndSome()
            {
                parent.SomeMasks.Add(SomeMask);
                return parent;
            }

            public override bool Equals(object? obj)
            {
                return obj is SomeMaskBuilder other && Equals(other);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override string? ToString()
            {
                return base.ToString();
            }

            public static bool operator ==(SomeMaskBuilder left, SomeMaskBuilder right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(SomeMaskBuilder left, SomeMaskBuilder right)
            {
                return !(left == right);
            }

            public bool Equals(SomeMaskBuilder other)
            {
                return SomeMask.Equals(other.SomeMask) && WriteMask.Equals(other.WriteMask);
            }
        }

        public struct NotAllMaskBuilder : IEquatable<NotAllMaskBuilder>
        {
            internal readonly BitMask NoneMask;
            private readonly TagMaskBuilder parent;

            public NotAllMaskBuilder(TagMaskBuilder parent, BitMask noneMask)
            {
                this.parent = parent;
                NoneMask = noneMask;
            }

            [UnscopedRef]
            public ref NotAllMaskBuilder HasNot<T>() where T : struct, ITag<T>
            {
                NoneMask.SetBit(World.GetOrCreateTagId<T>());
                return ref this;
            }

            public TagMaskBuilder EndSome()
            {
                parent.NoneMasks.Add(NoneMask);
                return parent;
            }

            public override bool Equals(object? obj)
            {
                return obj is NotAllMaskBuilder other && Equals(other);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override string? ToString()
            {
                return base.ToString();
            }

            public static bool operator ==(NotAllMaskBuilder left, NotAllMaskBuilder right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(NotAllMaskBuilder left, NotAllMaskBuilder right)
            {
                return !(left == right);
            }

            public bool Equals(NotAllMaskBuilder other)
            {
                return NoneMask.Equals(other.NoneMask);
            }
        }
    }
}
