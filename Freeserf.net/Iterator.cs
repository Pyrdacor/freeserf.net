/*
 * Iterator.cs - Basic iterator interface
 *
 * Copyright (C) 2018   Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Collections.Generic;

namespace Freeserf
{
    public abstract class Iterator<T> : IEquatable<Iterator<T>>, IEqualityComparer<Iterator<T>>
    {
        public abstract T Current { get; }

        public abstract bool Equals(Iterator<T> other);

        protected abstract void Increment();

        public static Iterator<T> operator ++(Iterator<T> iterator)
        {
            iterator.Increment();

            return iterator;
        }

        public static bool operator ==(Iterator<T> self, Iterator<T> other)
        {
#pragma warning disable IDE0041
            if (ReferenceEquals(self, null))
                return ReferenceEquals(other, null);

            if (ReferenceEquals(other, null))
                return false;
#pragma warning restore IDE0041

            return self.Equals(other);
        }

        public static bool operator !=(Iterator<T> self, Iterator<T> other)
        {
            return !self.Equals(other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Iterator<T>);
        }

        public override int GetHashCode()
        {
            return Current.GetHashCode();
        }

        public bool Equals(Iterator<T> x, Iterator<T> y)
        {
            return x == y;
        }

        public int GetHashCode(Iterator<T> obj)
        {
            return (obj == null) ? 0 : obj.GetHashCode();
        }
    }
}
