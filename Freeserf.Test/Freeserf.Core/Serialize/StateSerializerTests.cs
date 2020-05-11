using Freeserf.Serialize;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace Freeserf.Test.Freeserf.Core.Serialize
{
    [TestClass]
    public class StateSerializerTests
    {
        [TestMethod]
        public void EmptyState_ShouldSerializeAndDeserializeBackCorrectly()
        {
            using var stream = new MemoryStream();
            var state = new TestState();

            StateSerializer.Serialize(stream, state, true, true);
            stream.Position = 0;
            var resultingState = StateSerializer.Deserialize<TestState>(stream);

            Assert.IsTrue(state.Equals(resultingState), "Deserialized state does not match previously serialized state.");
        }

        [TestMethod]
        public void TestState_ShouldSerializeAndDeserializeBackCorrectly()
        {
            using var stream = new MemoryStream();
            var state = new TestState();
            var innerState = new TestState.InnerState();

            state.TestProperty1 = "Test";
            state.TestProperty2 = 5;
            state.TestProperty3 = 0xff;
            state.PropertyTest1 = 0xabcd;
            state.PropertyTest2 = 1.23;
            state.ArrayTestProperty = new DirtyArray<uint>(3);
            state.ArrayTestProperty[0] = 3;
            state.ArrayTestProperty[1] = 2;
            state.ArrayTestProperty[2] = 1;
            state.MapTestProperty = new DirtyMap<float, TestState.InnerState>();
            innerState.InnerTestProperty = 5000ul;
            state.MapTestProperty.Add(3.5f, innerState);

            StateSerializer.Serialize(stream, state, true, true);
            stream.Position = 0;
            var resultingState = StateSerializer.Deserialize<TestState>(stream);

            Assert.IsTrue(state.Equals(resultingState), "Deserialized state does not match previously serialized state.");
        }

        [TestMethod]
        public void ManipulatedSerializedStateHeader_ShouldThrowException()
        {
            using var stream = new MemoryStream();
            var state = new TestState();

            StateSerializer.Serialize(stream, state, true, true);
            stream.Position = 0;
            stream.Write(new byte[1] { 0 }, 0, 1); // overwrite 'F' with 0

            Assert.ThrowsException<ExceptionFreeserf>(() => StateSerializer.Deserialize<TestState>(stream),
                "Manipulated serialized state header does not throw exception on deserialization.");
        }

        [TestMethod]
        public void EmptyState_ShouldDeserializeNullValuesAsEmptyValues()
        {
            using var stream = new MemoryStream();
            var state = new TestState();

            StateSerializer.Serialize(stream, state, true, true);
            stream.Position = 0;
            var resultingState = StateSerializer.Deserialize<TestState>(stream);

            Assert.IsTrue(
                state.TestProperty1 == null && resultingState.TestProperty1 == "" &&
                state.ArrayTestProperty == null && resultingState.ArrayTestProperty != null && resultingState.ArrayTestProperty.Length == 0 &&
                state.MapTestProperty == null && resultingState.MapTestProperty != null && resultingState.MapTestProperty.Count == 0,
                "Deserialized state does not convert null values to empty values.");
        }

        [TestMethod]
        public void InvalidTestState_UnsupportedPropertyType_ShouldThrowExceptionOnSerialization()
        {
            using var stream = new MemoryStream();
            var state = new InvalidTestState_UnsupportedPropertyType();

            Assert.ThrowsException<ExceptionFreeserf>(() => StateSerializer.Serialize(stream, state, true, true),
                "Invalid state does not throw exception on serialization.");
        }

        [TestMethod]
        public void InvalidTestState_InvalidPropertyName_ShouldThrowExceptionOnSerialization()
        {
            using var stream = new MemoryStream();
            var state = new InvalidTestState_InvalidPropertyName();

            Assert.ThrowsException<ExceptionFreeserf>(() => StateSerializer.Serialize(stream, state, true, true),
                "Invalid state does not throw exception on serialization.");
        }

        [TestMethod]
        public void InvalidTestState_UnsupportedArrayElementType_ShouldThrowExceptionOnSerialization()
        {
            using var stream = new MemoryStream();
            var state = new InvalidTestState_UnsupportedArrayElementType();
            state.Property2 = new DirtyArray<InvalidDirtyArray>();

            StateSerializer.Serialize(stream, state, true, true);
            stream.Position = 0;

            Assert.ThrowsException<ExceptionFreeserf>(() => StateSerializer.Deserialize<InvalidTestState_UnsupportedArrayElementType>(stream),
                "Invalid state does not throw exception on deserialization.");
        }

        [TestMethod]
        public void FlagState_SerializeAndDeserialize_ShouldNotThrowException()
        {
            using var stream = new MemoryStream();
            var state = new FlagState();

            try
            {
                StateSerializer.Serialize(stream, state, true, true);
                stream.Position = 0;
                StateSerializer.Deserialize<FlagState>(stream);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Exception of type ({ex.GetType().Name}) thrown: {ex.Message}");
            }
        }

        [TestMethod]
        public void TestState_DataAttribute_ShouldBeSerialized()
        {
            using var stream = new MemoryStream();
            var state = new TestState_DataAttribute();
            state.Serialized = 100;
            state.NotSerialized = 100;

            StateSerializer.Serialize(stream, state, true, true);
            stream.Position = 0;
            var resultingState = StateSerializer.Deserialize<TestState_DataAttribute>(stream);

            Assert.IsTrue(state.Serialized == resultingState.Serialized,
                "Public field with Data attribute is not serialized.");
            Assert.IsTrue(resultingState.NotSerialized == 0,
                "Public field without Data attribute is serialized.");
        }

        [TestMethod]
        public void TestState_IgnoreAttribute_ShouldNotBeSerialized()
        {
            using var stream = new MemoryStream();
            var state = new TestState_IgnoreAttribute();
            state.Serialized = 100;
            state.NotSerialized = 100;

            StateSerializer.Serialize(stream, state, true, true);
            stream.Position = 0;
            var resultingState = StateSerializer.Deserialize<TestState_IgnoreAttribute>(stream);

            Assert.IsTrue(state.Serialized == resultingState.Serialized,
                "Public field in DataClass without Ignore attribute is not serialized.");
            Assert.IsTrue(resultingState.NotSerialized == 0,
                "Public field in DataClass with Ignore attribute is serialized.");
        }
    }
}
