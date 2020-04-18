/*
 * DirtyArray.cs - Array implementation which tracks the dirty state
 *
 * Copyright (C) 2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net. freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with freeserf.net. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.Serialize
{
    internal interface IDirtyArray : IEnumerable
    {
        bool Dirty { get; }
        IReadOnlyList<int> DirtyIndices { get; }
        int Length { get; }
        event EventHandler GotDirty;
        object Get(int index);
        void Set(int index, object value);
    }

    public class DirtyArray<T> : IDirtyArray, IEnumerable<T> where T : IComparable
    {
        T[] array = null;
        readonly List<int> dirtyIndices = new List<int>();
  
        public DirtyArray()
        {
            array = new T[0];
        }

        public DirtyArray(int size)
        {
            array = new T[size];
        }

        public DirtyArray(params T[] values)
        {
            array = values;
        }

        public object Get(int index) => this[index];

        public void Set(int index, object value) => this[index] = (T)value;

        public T this[int index]
        {
            get => array[index];
            set
            {
                if ((array[index] == null && value != null) || array[index].CompareTo(value) != 0)
                {
                    array[index] = value;

                    bool wasDirty = Dirty;

                    if (!dirtyIndices.Contains(index))
                        dirtyIndices.Add(index);

                    if (!wasDirty)
                        GotDirty?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public T this[uint index]
        {
            get => array[index];
            set => this[(int)index] = value;
        }

        public bool Dirty => dirtyIndices.Count != 0;

        public IReadOnlyList<int> DirtyIndices => dirtyIndices.AsReadOnly();

        public void ResetDirtyFlag()
        {
            dirtyIndices.Clear();
        }

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