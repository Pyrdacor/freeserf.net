using Freeserf.Serialize;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Freeserf.Test.Freeserf.Core.Serialize
{
    using dword = UInt32;
    using qword = UInt64;
    using word = UInt16;

    [DataClass]
    class TestState : State, IEquatable<TestState>
    {
        [DataClass]
        public class InnerState : State, IComparable
        {
            public qword InnerTestProperty { get; set; }

            public override int CompareTo(object obj)
            {
                if (obj == null || !(obj is InnerState))
                    return 1;

                return InnerTestProperty.CompareTo((obj as InnerState).InnerTestProperty);
            }
        }

        public string TestProperty1 { get; set; }
        public int TestProperty2 { get; set; }
        public byte TestProperty3 { get; set; }
        public word PropertyTest1 { get; set; }
        public double PropertyTest2 { get; set; }
        public DirtyArray<dword> ArrayTestProperty { get; set; }
        public DirtyMap<float, InnerState> MapTestProperty { get; set; }

        public bool Equals([AllowNull] TestState other)
        {
            if (other == null)
                return false;

            return
                StringsEqual(TestProperty1, other.TestProperty1) &&
                TestProperty2 == other.TestProperty2 &&
                TestProperty3 == other.TestProperty3 &&
                PropertyTest1 == other.PropertyTest1 &&
                PropertyTest2 == other.PropertyTest2 &&
                ArraysEqual(ArrayTestProperty, other.ArrayTestProperty) &&
                MapsEqual(MapTestProperty, other.MapTestProperty);
        }

        private static bool StringsEqual(string a, string b)
        {
            if (a == b)
                return true;

            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
                return true;

            return false;
        }

        private static bool ArraysEqual<T>(DirtyArray<T> a, DirtyArray<T> b) where T : IComparable
        {
            if (a == b)
                return true;

            if (a == null && b.Length == 0) // b can not be null here because of check above
                return true;

            if (b == null && a.Length == 0) // a can not be null here because of check above
                return true;

            if (a == null || b == null)
                return false;

            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; ++i)
            {
                if (a[i].CompareTo(b[i]) != 0)
                    return false;
            }

            return true;
        }

        private static bool MapsEqual<TKey, TValue>(DirtyMap<TKey, TValue> a, DirtyMap<TKey, TValue> b) where TKey : IComparable where TValue : IComparable
        {
            if (a == b)
                return true;

            if (a == null && b.Count == 0) // b can not be null here because of check above
                return true;

            if (b == null && a.Count == 0) // a can not be null here because of check above
                return true;

            if (a == null || b == null)
                return false;

            if (a.Count != b.Count)
                return false;

            foreach (var entry in a)
            {
                if (!b.ContainsKey(entry.Key) || entry.Value.CompareTo(b[entry.Key]) != 0)
                    return false;
            }

            return true;
        }
    }

    class TestState_DataAttribute : State
    {
        [Data]
        public int Serialized = 0;

        public int NotSerialized = 0;
    }

    [DataClass]
    class TestState_IgnoreAttribute : State
    {
        public int Serialized = 0;

        [Ignore]
        public int NotSerialized = 0;
    }
}
