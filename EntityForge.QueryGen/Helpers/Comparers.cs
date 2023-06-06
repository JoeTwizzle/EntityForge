using EntityForge.QueryGen.Helpers;
using System;
using System.Collections.Generic;

namespace EntityForge.QueryGen
{
    partial class QueryGen
    {
        private readonly record struct RefStructDefContext(string? ShortNamespace, string ShortName, string FullName, ParentClass? Parent);

        private sealed class RefStructDefContextEqualityComparer : IEqualityComparer<RefStructDefContext>
        {
            private RefStructDefContextEqualityComparer() { }

            public static RefStructDefContextEqualityComparer Instance { get; } = new RefStructDefContextEqualityComparer();

            public bool Equals(RefStructDefContext x, RefStructDefContext y)
            {
                return x.FullName == y.FullName;
            }

            public int GetHashCode(RefStructDefContext obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
