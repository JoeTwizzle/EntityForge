using Archie.SourceGen.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Archie.SourceGen
{
    partial class GroupGen
    {
        private readonly record struct RefStructDefContext(string? ShortNamespace, string ShortName, string FullName, ParentClass? Parent, MemberContext MemberContext);
        private readonly record struct MemberContext(ImmutableArray<string> MemberNames, ImmutableArray<string> MemberTypes);

        private sealed class RefStructDefContextEqualityComparer : IEqualityComparer<RefStructDefContext>
        {
            private RefStructDefContextEqualityComparer() { }

            public static RefStructDefContextEqualityComparer Instance { get; } = new RefStructDefContextEqualityComparer();

            public bool Equals(RefStructDefContext x, RefStructDefContext y)
            {
                return x.FullName == y.FullName
                    && CompareTypeContextEqualityComparer.Instance.Equals(x.MemberContext, y.MemberContext);
            }

            public int GetHashCode(RefStructDefContext obj)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class CompareTypeContextEqualityComparer : IEqualityComparer<MemberContext>
        {
            private CompareTypeContextEqualityComparer() { }

            public static CompareTypeContextEqualityComparer Instance { get; } = new CompareTypeContextEqualityComparer();

            public bool Equals(MemberContext x, MemberContext y)
            {
                return x.MemberNames.SequenceEqual(y.MemberNames) && x.MemberTypes.SequenceEqual(y.MemberTypes);
            }

            public int GetHashCode(MemberContext obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
