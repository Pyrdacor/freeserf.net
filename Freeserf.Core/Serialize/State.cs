/*
 * State.cs - Basic state interfaces
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

using System.Collections.Generic;

namespace Freeserf.Serialize
{
    internal interface IState
    {
        bool Dirty { get; }
        IReadOnlyList<string> DirtyProperties { get; }

        void ResetDirtyFlag();
    }


    internal abstract class State : IState, System.IComparable
    {
        private readonly List<string> dirtyProperties = new List<string>();
        protected object dirtyLock = new object();

        public static IReadOnlyList<string> DirtyState(string stateName, bool dirty) => dirty ? new List<string>() { stateName } : new List<string>();

        public bool Dirty
        {
            get;
            private set;
        }

        public IReadOnlyList<string> DirtyProperties => dirtyProperties.AsReadOnly();

        protected virtual void MarkPropertyAsDirty(string name)
        {
            lock (dirtyLock)
            {
                if (!dirtyProperties.Contains(name))
                    dirtyProperties.Add(name);
                Dirty = true;
            }
        }

        public virtual void ResetDirtyFlag()
        {
            lock (dirtyLock)
            {
                ResetDirtyFlagUnlocked();
            }
        }

        protected void ResetDirtyFlagUnlocked()
        {
            dirtyProperties.Clear();
            Dirty = false;
        }

        public virtual int CompareTo(object obj)
        {
            if (obj == null)
                return 1;

            // we only support reference comparison here
            // its only purpose is to be useable in DirtyArray and DirtyMap values
            if (this == obj)
                return 0;

            var hashCodeResult = GetHashCode().CompareTo(obj.GetHashCode());

            if (hashCodeResult == 0) // not equal but same hash code
            {
                Log.Error.Write(ErrorSystemType.Application, "Different object with same hash code.");
                return -1; // hopefully it works
            }

            return hashCodeResult;
        }
    }
}
