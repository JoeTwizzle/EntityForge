// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Archie.Collections
{
    internal sealed class DictionaryKeyCollectionDebugView<TKey, TValue>
    {
        private readonly ICollection<TKey> _collection;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TKey[] Items
        {
            get
            {
                TKey[] array = new TKey[_collection.Count];
                _collection.CopyTo(array, 0);
                return array;
            }
        }

        public DictionaryKeyCollectionDebugView(ICollection<TKey> collection)
        {
            _collection = collection ?? throw new ArgumentNullException("collection");
        }
    }
}