using Freeserf.Serialize;
using System;
using System.Collections;

namespace Freeserf.Test.Freeserf.Core.Serialize
{
    class InvalidTestState_UnsupportedPropertyType : State
    {
        public int Property1 { get; set; }
        public sbyte Property2 { get; set; } // should not be supported by StateSerializer
    }

    class InvalidTestState_InvalidPropertyName : State
    {
        public int Property1 { get; set; }
        public int ÜProperty2 { get; set; } // should not be supported by StateSerializer
    }

    class InvalidDirtyArray : IDirtyArray, IComparable
    {
        public bool Dirty => throw new NotImplementedException();

        public int Length => 0;

        public event EventHandler GotDirty;

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
        public int Property1 { get; set; }
        public DirtyArray<InvalidDirtyArray> Property2 { get; set; } // should not be supported by StateSerializer
    }
}
