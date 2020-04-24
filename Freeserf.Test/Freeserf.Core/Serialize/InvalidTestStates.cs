using Freeserf.Serialize;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Freeserf.Test.Freeserf.Core.Serialize
{
    class InvalidTestState_UnsupportedPropertyType : State
    {
        [Data]
        public int Property1 { get; set; }
        [Data]
        public List<int> Property2 { get; set; } // should not be supported by StateSerializer
    }

    class InvalidTestState_InvalidPropertyName : State
    {
        [Data]
        public int Property1 { get; set; }
        [Data]
        public int ÜProperty2 { get; set; } // should not be supported by StateSerializer
    }

    class InvalidDirtyArray : IDirtyArray, IComparable
    {
        public bool Dirty => throw new NotImplementedException();

        public int Length => 0;

#pragma warning disable CS0067
        public event EventHandler GotDirty;
#pragma warning restore CS0067

        public IReadOnlyList<int> DirtyIndices => throw new NotImplementedException();

        public object Get(int index) => throw new NotImplementedException();

        public void Set(int index, object value) => throw new NotImplementedException();

        public int CompareTo(object obj)
        {
            if (obj == null || !(obj is InvalidDirtyArray))
                return 1;

            return 0;
        }

        public IEnumerator GetEnumerator()
        {
            return new int[0].GetEnumerator();
        }

        public void Initialize(Array values)
        {
            // do nothing
        }
    }

    class InvalidTestState_UnsupportedArrayElementType : State
    {
        [Data]
        public int Property1 { get; set; }
        [Data]
        public DirtyArray<InvalidDirtyArray> Property2 { get; set; } // should not be supported by StateSerializer
    }
}
