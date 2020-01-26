using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Freeserf.Serialize
{
    using word = UInt16;
    using dword = UInt32;
    using qword = UInt64;

    internal static class StateSerializer
    {
        private class PropertyMap
        {
            private readonly Dictionary<char, List<string>> map = new Dictionary<char, List<string>>();

            private PropertyMap()
            {

            }

            private void AddProperty(string name)
            {
                if (string.IsNullOrEmpty(name))
                    throw new ExceptionFreeserf(ErrorSystemType.Application, "Property name was null or empty");

                if (name[0] > 127)
                    throw new ExceptionFreeserf(ErrorSystemType.Application, "Property must start with an ASCII character.");

                if (!map.ContainsKey(name[0]))
                    map[name[0]] = new List<string>() { name };
                else
                {
                    if (map[name[0]].Count == 255)
                        throw new ExceptionFreeserf(ErrorSystemType.Application, "Max properties with same starting letter exceeded");

                    map[name[0]].Add(name);
                }
            }

            private void Sort()
            {
                foreach (var entry in map)
                    entry.Value.Sort();
            }

            public static PropertyMap Create(Type stateType)
            {
                var map = new PropertyMap();
                var properties = stateType.GetProperties()
                    .Where(property => property.GetCustomAttribute(typeof(IgnoreAttribute)) != null);

                foreach (var property in properties)
                {
                    map.AddProperty(property.Name);
                }

                map.Sort();

                return map;
            }

            public void SerializePropertyName(BinaryWriter writer, string property)
            {
                if (string.IsNullOrEmpty(property))
                    throw new ExceptionFreeserf(ErrorSystemType.Application, "Property name was null or empty");

                writer.Write((byte)property[0]);
                writer.Write((byte)map[property[0]].IndexOf(property));
            }

            public string DeserializePropertyName(BinaryReader reader)
            {
                if (reader.BaseStream.Position == reader.BaseStream.Length)
                    return null;
                char firstCharacter = (char)reader.ReadByte();

                if (firstCharacter == 0)
                    return null;

                if (reader.BaseStream.Position == reader.BaseStream.Length)
                    throw new ExceptionFreeserf("Invalid state data");

                int index = reader.ReadByte();

                return map[firstCharacter][index];
            }
        }

        private static readonly Dictionary<State, PropertyMap> propertyMapCache = new Dictionary<State, PropertyMap>();
        private const byte DATA_MAJOR_VERSION = 0;
        private const byte DATA_MINOR_VERSION = 0;

        public static void Serialize(Stream stream, State state, bool full)
        {
            using (var writer = new BinaryWriter(stream))
            {
                // "FS" -> Freeserf State
                writer.Write(new byte[] { (byte)'F', (byte)'S', DATA_MAJOR_VERSION, DATA_MINOR_VERSION });

                SerializeWithoutHeader(writer, state, full);
            }
        }

        private static void SerializeWithoutHeader(BinaryWriter writer, State state, bool full)
        {
            if (!full && !state.Dirty)
            {
                SerializePropertyValue(writer, 0, false);
                return;
            }

            var propertyMap = PropertyMap.Create(state.GetType());
            var properties = full ? state.GetType().GetProperties()
                .Where(property => property.GetCustomAttribute(typeof(IgnoreAttribute)) != null)
                .Select(property => property.Name) : state.DirtyProperties;

            foreach (var property in properties)
            {
                propertyMap.SerializePropertyName(writer, property);
                SerializePropertyValue(writer, state.GetType().GetProperty(property).GetValue(state), full);
            }

            // end of state data marker
            SerializePropertyValue(writer, 0, false);
        }

        public static T Deserialize<T>(Stream stream) where T : State, new()
        {
            using (var reader = new BinaryReader(stream))
            {
                byte[] header = reader.ReadBytes(4);

                if (header[0] != 'F' || header[1] != 'S')
                    throw new ExceptionFreeserf("Invalid state data");

                if (header[2] != DATA_MAJOR_VERSION ||
                    header[3] != DATA_MINOR_VERSION)
                    throw new ExceptionFreeserf("Wrong state data version");

                return DeserializeWithoutHeader<T>(reader);
            }
        }

        private static object DeserializeWithoutHeader(BinaryReader reader, Type type)
        {
            var propertyMap = PropertyMap.Create(type);
            var obj = Activator.CreateInstance(type);

            while (true)
            {
                var propertyName = propertyMap.DeserializePropertyName(reader);

                if (propertyName == null)
                    break;

                var property = type.GetProperty(propertyName);

                property.SetValue(obj, DeserializePropertyValue(reader, property.PropertyType, property.GetValue(obj)));
            }

            return obj;
        }

        private static T DeserializeWithoutHeader<T>(BinaryReader reader) where T : State, new()
        {
            return (T)DeserializeWithoutHeader(reader, typeof(T));
        }

        private static object DeserializePropertyValue(BinaryReader reader, Type type, object propertyValue)
        {
            if (type.IsSubclassOf(typeof(State)))
            {
                return DeserializeWithoutHeader(reader, type);
            }
            else if (type == typeof(string))
            {
                return reader.ReadString();
            }
            else if (type == typeof(char))
            {
                return reader.ReadChar();
            }
            else if (type == typeof(int))
            {
                return reader.ReadInt32();
            }
            else if (type == typeof(byte))
            {
                return reader.ReadByte();
            }
            else if (type == typeof(word))
            {
                return reader.ReadUInt16();
            }
            else if (type == typeof(dword))
            {
                return reader.ReadUInt32();
            }
            else if (type == typeof(qword))
            {
                return reader.ReadUInt64();
            }
            else if (type == typeof(float))
            {
                return reader.ReadSingle();
            }
            else if (type == typeof(double))
            {
                return reader.ReadDouble();
            }
            else if (type.IsSubclassOf(typeof(IDirtyArray)))
            {
                var elementType = type.IsGenericType ? type.GetGenericArguments()[0] : typeof(object);

                if (elementType.IsSubclassOf(typeof(IDirtyArray)) ||
                    elementType.IsSubclassOf(typeof(IDirtyMap)))
                    throw new ExceptionFreeserf("DirtyArray and DirtyMap are not supported as elements of a DirtyArray.");

                int length = reader.ReadInt32();
                var array = Array.CreateInstance(elementType, length);
                var dirtyArray = propertyValue as IDirtyArray;

                for (int i = 0; i < length; ++i)
                    array.SetValue(DeserializePropertyValue(reader, elementType, null), i);

                dirtyArray.Initialize(array);

                return dirtyArray;
            }
            else if (type.IsSubclassOf(typeof(IDirtyMap)))
            {
                var keyType = type.IsGenericType ? type.GetGenericArguments()[0] : typeof(object);
                var valueType = type.IsGenericType ? type.GetGenericArguments()[1] : typeof(object);

                if (keyType.IsSubclassOf(typeof(IDirtyArray)) ||
                    keyType.IsSubclassOf(typeof(IDirtyMap)))
                    throw new ExceptionFreeserf("DirtyArray and DirtyMap are not supported as keys of a DirtyMap.");
                if (valueType.IsSubclassOf(typeof(IDirtyArray)) ||
                    valueType.IsSubclassOf(typeof(IDirtyMap)))
                    throw new ExceptionFreeserf("DirtyArray and DirtyMap are not supported as values of a DirtyMap.");

                int count = reader.ReadInt32();
                var map = Array.CreateInstance(typeof(KeyValuePair<object, object>), count);
                var dirtyMap = propertyValue as IDirtyMap;

                for (int i = 0; i < count; ++i)
                {
                    var key = DeserializePropertyValue(reader, keyType, null);
                    var value = DeserializePropertyValue(reader, valueType, null);
                    map.SetValue(new KeyValuePair<object, object>(key, value), i);
                }

                dirtyMap.Initialize(map);

                return dirtyMap;
            }
            else
            {
                throw new ExceptionFreeserf("Unsupport data type for state deserialization: " + type.Name);
            }
        }

        private static void SerializePropertyValue(BinaryWriter writer, object value, bool full)
        {
            if (value is State)
            {
                SerializeWithoutHeader(writer, value as State, full);
            }
            else if (value is string)
            {
                writer.Write(value as string);
            }
            else if (value is char)
            {
                writer.Write((char)value);
            }
            else if (value is int)
            {
                writer.Write((int)value);
            }
            else if (value is byte)
            {
                writer.Write((byte)value);
            }
            else if (value is word)
            {
                writer.Write((word)value);
            }
            else if (value is dword)
            {
                writer.Write((dword)value);
            }
            else if (value is qword)
            {
                writer.Write((qword)value);
            }
            else if (value is float)
            {
                writer.Write((float)value);
            }
            else if (value is double)
            {
                writer.Write((double)value);
            }
            else if (value is IDirtyArray)
            {
                var array = value as IDirtyArray;

                writer.Write(array.Length);

                foreach (var element in array)
                    SerializePropertyValue(writer, element, full);
            }
            else if (value is IDirtyMap)
            {
                var map = value as IDirtyMap;

                writer.Write(map.Count);

                foreach (var element in map)
                {
                    if (!element.GetType().IsGenericType ||
                        element.GetType().GetGenericTypeDefinition() != typeof(KeyValuePair<,>))
                        throw new ExceptionFreeserf("Invalid dirty map class");

                    dynamic entry = element;
                    SerializePropertyValue(writer, entry.Key, full);
                    SerializePropertyValue(writer, entry.Value, full);
                }
            }
            else
            {
                throw new ExceptionFreeserf("Unsupport data type for state serialization: " + value.GetType().Name);
            }
        }
    }
}