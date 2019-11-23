using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.Serialize
{
    public class DirtyArray<T> : IEnumerable<T> where T : IComparable
    {
        T[] array = null;

        public DirtyArray(int size)
        {
            array = new T[size];
        }

        public DirtyArray(params T[] values)
        {
            array = values;
        }

        public T this[int index]
        {
            get => array[index];
            set
            {
                if ((array[index] == null && value != null) || array[index].CompareTo(value) != 0)
                {
                    array[index] = value;

                    if (!Dirty)
                    {
                        Dirty = true;
                        GotDirty?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        public T this[uint index]
        {
            get => array[index];
            set => this[(int)index] = value;
        }

        public bool Dirty
        {
            get;
            set;
        } = false;

        public int Length => array.Length;

        public event EventHandler GotDirty;

        public void RemoveGotDirtyHandlers()
        {
            GotDirty = null;
        }

        public static implicit operator T[](DirtyArray<T> array)
        {
            // TODO make this readonly/a copy?
            return array.array;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return array.Cast<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}