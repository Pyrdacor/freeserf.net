using System;
using System.Collections;
using System.Collections.Generic;

namespace Freeserf.Serialize
{
    public class DirtyMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : IComparable where TValue : IComparable
    {
        Dictionary<TKey, TValue> map = null;

        public DirtyMap()
        {
            map = new Dictionary<TKey, TValue>();
        }

        public DirtyMap(int capacity)
        {
            map = new Dictionary<TKey, TValue>(capacity);
        }

        public TValue this[TKey key]
        {
            get => map[key];
            set
            {
                if (map[key].CompareTo(value) != 0)
                {
                    map[key] = value;

                    if (!Dirty)
                    {
                        Dirty = true;
                        GotDirty?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        public bool Dirty
        {
            get;
            set;
        } = false;

        public int Count => map.Count;

        public event EventHandler GotDirty;

        public void Add(TKey key, TValue value)
        {
            map.Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return map.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            return map.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return map.TryGetValue(key, out value);
        }

        public static implicit operator Dictionary<TKey, TValue>(DirtyMap<TKey, TValue> map)
        {
            // TODO make this readonly/a copy?
            return map;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return map.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}