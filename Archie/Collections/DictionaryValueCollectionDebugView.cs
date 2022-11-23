﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Archie.Collections
{
    internal sealed class DictionaryValueCollectionDebugView<TKey, TValue>
    {
        private readonly ICollection<TValue> _collection;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TValue[] Items
        {
            get
            {
                TValue[] array = new TValue[_collection.Count];
                _collection.CopyTo(array, 0);
                return array;
            }
        }

        public DictionaryValueCollectionDebugView(ICollection<TValue> collection)
        {
            _collection = collection ?? throw new ArgumentNullException("collection");
        }
    }
}