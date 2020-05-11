/*
 * StateSerializer.cs - Serializer for state objects
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Freeserf.Serialize
{
    using dword = UInt32;
    using qword = UInt64;
    using word = UInt16;

    internal static class StateSerializer
    {
        /**
         * The property map is used to compress property names for serialization.
         * Instead of transferring the whole name (which can become quiet long)
         * only the first letter plus a 8-bit number is transferred. This number
         * represents the index of the sorted properties starting with this letter.
         * 
         * Example:
         * 
         * Property names are: foo, bar and baz
         * 
         * The name "foo" is transferred as 'f' and 0.
         * The name "bar" is transferred as 'b' and 0.
         * The name "baz" is transferred as 'b' and 1.
         * 
         * So any property name can be compressed to 2 bytes regardless of its length.
         * We limit the property names to ASCII (7 bit) for simplicity.
         * 
         * There are a few limitations though:
         * - As mentioned we only support ASCII in property names (hence 1 byte per letter).
         * - The number of properties with the same starting letter is limited to 256.
         * - Each side (server and client) need the same version of the data classes.
         * 
         * The first limitation isn't a hard one. Property names should be ASCII anyway.
         * 
         * The second limit should not matter as this rare case won't happen and if so
         * there is an exception thrown in that case, so the mechanism could be adjusted
         * later (e.g. increasing the number range to a 16bit value -> 65536 possibilities).
         * 
         * The third limit is secured by transferring a data version which must match
         * on both sides. Otherwise an exception is thrown. Clients and servers have to
         * use the same data version to be able to communicate properly.
         */
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
                var properties = GetSerializableProperties(stateType);

                foreach (var property in properties)
                {
                    map.AddProperty(property.Key);
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
        /// <summary>
        /// Major data version.
        /// This is part of the data version a communication partner uses.
        /// </summary>
        private const byte DATA_MAJOR_VERSION = 0;
        /// <summary>
        /// Minor data version.
        /// This is part of the data version a communication partner uses.
        /// </summary>
        private const byte DATA_MINOR_VERSION = 0;

        /// <summary>
        /// If a class has the DataClass attribute, each public property and field
        /// of the class is serializable. Except for those which have the Ignore attribute.
        /// Properties without public getter or setter are not serializable as well.
        /// 
        /// Otherwise single properties and fields may have the Data attribute.
        /// </summary>
        /// <param name="stateType"></param>
        /// <returns></returns>
        private static IEnumerable<KeyValuePair<string, bool>> GetSerializableProperties(Type stateType)
        {
            if (stateType.GetCustomAttribute(typeof(DataClassAttribute)) != null)
            {
                var properties = stateType.GetProperties()
                    .Where(property =>
                        property.GetCustomAttribute(typeof(IgnoreAttribute)) == null &&
                        property.GetGetMethod() != null &&
                        property.GetSetMethod() != null
                    )
                    .Select(property => new KeyValuePair<string, bool>(property.Name, true));
                var fields = stateType.GetFields()
                    .Where(field =>
                        field.GetCustomAttribute(typeof(IgnoreAttribute)) == null
                    )
                    .Select(field => new KeyValuePair<string, bool>(field.Name, false));
                return Enumerable.Concat(properties, fields);
            }
            else
            {
                var properties = stateType.GetProperties()
                    .Where(property =>
                        property.GetCustomAttribute(typeof(DataAttribute)) != null &&
                        property.GetGetMethod() != null &&
                        property.GetSetMethod() != null
                    )
                    .Select(property => new KeyValuePair<string, bool>(property.Name, true));
                var fields = stateType.GetFields()
                    .Where(field =>
                        field.GetCustomAttribute(typeof(DataAttribute)) != null
                    )
                    .Select(field => new KeyValuePair<string, bool>(field.Name, false));
                return Enumerable.Concat(properties, fields);
            }
        }

        /// <summary>
        /// Public fields and properties are understand as "Properties" here.
        /// 
        /// The results are key-value-pairs where the key is the property or field name
        /// and the value is a boolean that is true for real properties and false for
        /// fields.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="onlyDirtyProperties"></param>
        /// <returns></returns>
        private static IEnumerable<KeyValuePair<string, bool>> GetSerializableProperties(State state, bool onlyDirtyProperties)
        {
            if (onlyDirtyProperties && state.DirtyProperties.Count == 0)
                return new KeyValuePair<string, bool>[0];

            var allProperties = GetSerializableProperties(state.GetType());

            if (onlyDirtyProperties)
                return allProperties.Where(property => state.DirtyProperties.Contains(property.Key));

            return allProperties;
        }

        public static void Serialize(Stream stream, State state, bool full, bool leaveOpen = false)
        {
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen))
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
            var properties = GetSerializableProperties(state, !full);

            foreach (var property in properties)
            {
                propertyMap.SerializePropertyName(writer, property.Key);
                object propertyValue;
                Type propertyType;

                if (property.Value) // real property
                {
                    var propertyInfo = state.GetType().GetProperty(property.Key);
                    propertyValue = propertyInfo.GetValue(state);
                    propertyType = propertyInfo.PropertyType;
                }
                else // field
                {
                    var fieldInfo = state.GetType().GetField(property.Key);
                    propertyValue = fieldInfo.GetValue(state);
                    propertyType = fieldInfo.FieldType;
                }

                if (propertyValue == null)
                    SerializePropertyNullValue(writer, propertyType);
                else
                    SerializePropertyValue(writer, propertyValue, full);
            }

            // end of state data marker
            SerializePropertyValue(writer, 0, false);
        }

        public static T Deserialize<T>(Stream stream, bool leaveOpen = false) where T : State, new()
        {
            using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen))
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

                if (property != null)
                {
                    property.SetValue(obj, DeserializePropertyValue(reader, property.PropertyType, property.GetValue(obj)));
                }
                else // possibly a field
                {
                    var field = type.GetField(propertyName);

                    field.SetValue(obj, DeserializePropertyValue(reader, field.FieldType, field.GetValue(obj)));
                }
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
            else if (type == typeof(sbyte))
            {
                return reader.ReadSByte();
            }
            else if (type == typeof(byte))
            {
                return reader.ReadByte();
            }
            else if (type == typeof(short))
            {
                return reader.ReadInt16();
            }
            else if (type == typeof(word))
            {
                return reader.ReadUInt16();
            }
            else if (type == typeof(dword))
            {
                return reader.ReadUInt32();
            }
            else if (type == typeof(long))
            {
                return reader.ReadInt64();
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
            else if (type == typeof(decimal))
            {
                return reader.ReadDecimal();
            }
            else if (type.IsEnum)
            {
                var baseType = Enum.GetUnderlyingType(type);
                var value = DeserializePropertyValue(reader, baseType, null);

                return Enum.ToObject(type, value);
            }
            else if (typeof(IDirtyArray).IsAssignableFrom(type))
            {
                var elementType = type.IsGenericType ? type.GetGenericArguments()[0] : typeof(object);

                if (typeof(IDirtyArray).IsAssignableFrom(elementType) ||
                    typeof(IDirtyMap).IsAssignableFrom(elementType))
                    throw new ExceptionFreeserf("DirtyArray and DirtyMap are not supported as elements of a DirtyArray.");

                int length = reader.ReadInt32();
                var array = Array.CreateInstance(elementType, length);
                var dirtyArray = propertyValue as IDirtyArray;

                for (int i = 0; i < length; ++i)
                    array.SetValue(DeserializePropertyValue(reader, elementType, null), i);

                if (dirtyArray == null)
                    dirtyArray = (IDirtyArray)Activator.CreateInstance(type);

                dirtyArray.Initialize(array);

                return dirtyArray;
            }
            else if (typeof(IDirtyMap).IsAssignableFrom(type))
            {
                var keyType = type.IsGenericType ? type.GetGenericArguments()[0] : typeof(object);
                var valueType = type.IsGenericType ? type.GetGenericArguments()[1] : typeof(object);

                if (typeof(IDirtyArray).IsAssignableFrom(keyType) ||
                    typeof(IDirtyMap).IsAssignableFrom(keyType))
                    throw new ExceptionFreeserf("DirtyArray and DirtyMap are not supported as keys of a DirtyMap.");
                if (typeof(IDirtyArray).IsAssignableFrom(valueType) ||
                    typeof(IDirtyMap).IsAssignableFrom(valueType))
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

                if (dirtyMap == null)
                    dirtyMap = (IDirtyMap)Activator.CreateInstance(type);

                dirtyMap.Initialize(map);

                return dirtyMap;
            }
            else
            {
                throw new ExceptionFreeserf("Unsupport data type for state deserialization: " + type.Name);
            }
        }

        private static void SerializePropertyNullValue(BinaryWriter writer, Type propertyType)
        {
            if (propertyType.IsSubclassOf(typeof(State)))
            {
                SerializePropertyValue(writer, 0, false);
            }
            else if (propertyType == typeof(string))
            {
                writer.Write("");
            }
            else if (typeof(IDirtyArray).IsAssignableFrom(propertyType))
            {
                writer.Write(0); // length of zero
            }
            else if (typeof(IDirtyMap).IsAssignableFrom(propertyType))
            {
                writer.Write(0); // count of zero
            }
            else
            {
                throw new ExceptionFreeserf("Unsupport data type for state serialization: " + propertyType.Name);
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
            else if (value is sbyte)
            {
                writer.Write((sbyte)value);
            }
            else if (value is byte)
            {
                writer.Write((byte)value);
            }
            else if (value is short)
            {
                writer.Write((short)value);
            }
            else if (value is word)
            {
                writer.Write((word)value);
            }
            else if (value is dword)
            {
                writer.Write((dword)value);
            }
            else if (value is long)
            {
                writer.Write((long)value);
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
            else if (value is decimal)
            {
                writer.Write((decimal)value);
            }
            else if (value.GetType().IsEnum)
            {
                var baseType = Enum.GetUnderlyingType(value.GetType());

                SerializePropertyValue(writer, Convert.ChangeType(value, baseType), false);
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

                    var elementKey = element.GetType().GetProperty("Key").GetValue(element);
                    var elementValue = element.GetType().GetProperty("Value").GetValue(element);
                    SerializePropertyValue(writer, elementKey, full);
                    SerializePropertyValue(writer, elementValue, full);
                }
            }
            else
            {
                throw new ExceptionFreeserf("Unsupport data type for state serialization: " + value.GetType().Name);
            }
        }
    }
}