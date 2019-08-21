/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Contexts.DataFlowAnalysis {
    [Serializable]
    public class DataFlowSet : IReadOnlyDictionary<IDataFlowFact, IDataFlowValue>, IDictionary<IDataFlowFact, IDataFlowValue> {
        readonly Dictionary<IDataFlowFact, IDataFlowValue> underlying = new Dictionary<IDataFlowFact, IDataFlowValue>();

        public IDataFlowValue this[IDataFlowFact key] {
            get {
                if (underlying.TryGetValue(key, out var val)) {
                    return val;
                }
                return DataFlowValue.Bottom;
            }
            set {
                if (value.IsBottom())
                    underlying.Remove(key);
                else
                    underlying[key] = value;
            }
        }

        public IEnumerable<IDataFlowFact> Keys => underlying.Keys;
        public IEnumerable<IDataFlowValue> Values => underlying.Values;
        public int Count => underlying.Count;
        public bool IsReadOnly => (underlying as ICollection<KeyValuePair<IDataFlowFact, IDataFlowValue>>).IsReadOnly;
        ICollection<IDataFlowFact> IDictionary<IDataFlowFact, IDataFlowValue>.Keys => underlying.Keys;
        ICollection<IDataFlowValue> IDictionary<IDataFlowFact, IDataFlowValue>.Values => underlying.Values;

        public void Add(IDataFlowFact key, IDataFlowValue value) => underlying.Add(key, value);
        public void Add(KeyValuePair<IDataFlowFact, IDataFlowValue> item) => underlying.Add(item.Key, item.Value);
        public void Clear() => underlying.Clear();
        public bool Contains(KeyValuePair<IDataFlowFact, IDataFlowValue> item) => item.Value.IsBottom() || underlying.TryGetValue(item.Key, out var val) && Equals(val, item.Value);
        public bool ContainsKey(IDataFlowFact key) => true;
        public void CopyTo(KeyValuePair<IDataFlowFact, IDataFlowValue>[] array, int arrayIndex)
            => (underlying as ICollection<KeyValuePair<IDataFlowFact, IDataFlowValue>>).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<IDataFlowFact, IDataFlowValue>> GetEnumerator() => underlying.GetEnumerator();
        public bool Remove(IDataFlowFact key) => underlying.Remove(key);
        public bool Remove(KeyValuePair<IDataFlowFact, IDataFlowValue> item) => (underlying as ICollection<KeyValuePair<IDataFlowFact, IDataFlowValue>>).Remove(item);
        public bool TryGetValue(IDataFlowFact key, out IDataFlowValue value) {
            if (!underlying.TryGetValue(key, out value))
                value = DataFlowValue.Bottom;
            return true;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
