using Freeserf.Serialize;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Freeserf.Test.Freeserf.Core.Serialize
{
    [DataClass]
    class InvalidTestState_UnsupportedPropertyType : State
    {
        public int Property1 { get; set; }
        public List<int> Property2 { get; set; } // should not be supported by StateSerializer
    }

    [DataClass]
    class InvalidTestState_InvalidPropertyName : State
    {
        public int Property1 { get; set; }
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

    [DataClass]
    class InvalidTestState_UnsupportedArrayElementType : State
    {
        public int Property1 { get; set; }
        public DirtyArray<InvalidDirtyArray> Property2 { get; set; } // should not be supported by StateSerializer
    }
}
