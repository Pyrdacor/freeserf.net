using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Freeserf.Serialize;

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
                "Manipulated serialized state header did not throw exception on deserialization.");
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
                "Deserialized state does not match previously serialized state.");
        }
    }
}
