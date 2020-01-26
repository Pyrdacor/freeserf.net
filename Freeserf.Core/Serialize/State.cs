using System.Collections.Generic;

namespace Freeserf.Serialize
{
    internal interface IState
    {
        bool Dirty { get; }

        void ResetDirtyFlag();
    }


    internal abstract class State : IState
    {
        private readonly List<string> dirtyProperties = new List<string>();
        protected object dirtyLock = new object();

        [Ignore]
        public bool Dirty
        {
            get;
            private set;
        }

        [Ignore]
        public IReadOnlyList<string> DirtyProperties => dirtyProperties.AsReadOnly();

        protected void MarkPropertyAsDirty(string name)
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
    }
}
