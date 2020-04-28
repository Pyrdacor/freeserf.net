/*
 * StateSerializer.cs - Serializer for state objects
 *
 * Copyright (C) 2019-2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

        public delegate object CustomTypeCreator(object parent);

        private static readonly BindingFlags propertyFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly Dictionary<Type, CustomTypeCreator> customTypeCreators = new Dictionary<Type, CustomTypeCreator>();
        private static readonly Dictionary<Type, PropertyMap> propertyMapCache = new Dictionary<Type, PropertyMap>();
        private static readonly Dictionary<Type, List<KeyValuePair<string, bool>>> typeSerializablePropertyCache = new Dictionary<Type, List<KeyValuePair<string, bool>>>();
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

        public static void RegisterCustomTypeCreator(Type type, CustomTypeCreator customTypeCreator)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (customTypeCreator == null)
                customTypeCreators.Remove(type);
            else
                customTypeCreators[type] = customTypeCreator;
        }

        private static bool IsPropertySerializable(PropertyInfo property)
        {
            if (property.GetGetMethod() != null)
                return true;

            // Internal properties are serializable too
            if (property.GetGetMethod(true) != null)
            {
                if (property.GetGetMethod(true).IsAssembly)
                    return true;
            }

            return false;
        }

        private static bool IsPropertyDeserializable(PropertyInfo property)
        {
            // We will deserialize into such objects.
            if (typeof(IState).IsAssignableFrom(property.PropertyType) ||
                typeof(IDirtyArray).IsAssignableFrom(property.PropertyType) ||
                typeof(IDirtyMap).IsAssignableFrom(property.PropertyType))
                return true;
            else
                // Everything else is deserializable with a pulic setter.
                return property.GetSetMethod() != null;
        }

        /// <summary>
        /// If a class has the DataClass attribute, each public property and field
        /// of the class is serializable. Except for those which have the Ignore attribute.
        /// Properties without public getter or setter are not serializable as well.
        /// 
        /// Otherwise single properties and fields may have the Data attribute.
        /// </summary>
        private static IEnumerable<KeyValuePair<string, bool>> GetSerializableProperties(Type stateType)
        {
            if (typeSerializablePropertyCache.ContainsKey(stateType))
                return typeSerializablePropertyCache[stateType];

            var properties = stateType.GetProperties(propertyFlags)
                .Where(property =>
                    property.GetCustomAttribute(typeof(DataAttribute)) != null &&
                    IsPropertySerializable(property) &&
                    IsPropertyDeserializable(property)
                )
                .Select(property => new { property.Name, Property = true, Order = ReadOrder(property) });
            var fields = stateType.GetFields(propertyFlags)
                .Where(field =>
                    field.GetCustomAttribute(typeof(DataAttribute)) != null
                )
                .Select(field => new { field.Name, Property = false, Order = ReadOrder(field) });
            var resultWithOrder = Enumerable.Concat(properties, fields).ToList();
            resultWithOrder.Sort((a, b) => a.Order.CompareTo(b.Order));
            var result = resultWithOrder.Select(r => new KeyValuePair<string, bool>(r.Name, r.Property)).ToList();
            typeSerializablePropertyCache[stateType] = result;
            return result;
        }

        private static int ReadOrder(MemberInfo memberInfo)
        {
            return (memberInfo.GetCustomAttribute(typeof(DataAttribute)) as DataAttribute).Order;
        }

        /// <summary>
        /// Public fields and properties are understand as "Properties" here.
        /// 
        /// The results are key-value-pairs where the key is the property or field name
        /// and the value is a boolean that is true for real properties and false for
        /// fields.
        /// </summary>
        private static IEnumerable<KeyValuePair<string, bool>> GetSerializableProperties(IState state, bool onlyDirtyProperties)
        {
            if (onlyDirtyProperties && state.DirtyProperties.Count == 0)
                return new KeyValuePair<string, bool>[0];

            var allProperties = GetSerializableProperties(state.GetType());

            if (onlyDirtyProperties)
                return allProperties.Where(property => state.DirtyProperties.Contains(property.Key));

            return allProperties;
        }

        public static void Serialize(Stream stream, IState state, bool full, bool leaveOpen = false)
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen);
            // "FS" -> Freeserf State
            writer.Write(new byte[] { (byte)'F', (byte)'S', DATA_MAJOR_VERSION, DATA_MINOR_VERSION });

            SerializeWithoutHeader(writer, state, full);
        }

        public static void SerializeWithoutHeader(Stream stream, IState state, bool full)
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

            SerializeWithoutHeader(writer, state, full);
        }

        private static void SerializeWithoutHeader(BinaryWriter writer, IState state, bool full)
        {
            if (!full && !state.Dirty)
            {
                SerializePropertyValue(writer, (byte)0, false);
                return;
            }

            PropertyMap propertyMap;

            if (propertyMapCache.ContainsKey(state.GetType()))
                propertyMap = propertyMapCache[state.GetType()];
            else
            {
                propertyMap = PropertyMap.Create(state.GetType());
                propertyMapCache.Add(state.GetType(), propertyMap);
            }

            var properties = GetSerializableProperties(state, !full);

            foreach (var property in properties)
            {
                propertyMap.SerializePropertyName(writer, property.Key);
                object propertyValue;
                Type propertyType;

                if (state is IVirtualDataProvider vdp)
                {
                    // If the property has changed we store 'true' otherwise 'false'.
                    writer.Write(vdp.ChangeVirtualDataMembers.Contains(property.Key));
                }

                if (property.Value) // real property
                {
                    var propertyInfo = state.GetType().GetProperty(property.Key, propertyFlags);
                    propertyValue = propertyInfo.GetValue(state);
                    propertyType = propertyInfo.PropertyType;
                }
                else // field
                {
                    var fieldInfo = state.GetType().GetField(property.Key, propertyFlags);
                    propertyValue = fieldInfo.GetValue(state);
                    propertyType = fieldInfo.FieldType;
                }

                if (propertyValue == null)
                    SerializePropertyNullValue(writer, propertyType);
                else
                    SerializePropertyValue(writer, propertyValue, full);
            }

            // end of state data marker
            SerializePropertyValue(writer, (byte)0, false);
        }

        public static void Deserialize<T>(T targetObject, Stream stream, bool leaveOpen = false) where T : IState
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen);
            byte[] header = reader.ReadBytes(4);

            if (header[0] != 'F' || header[1] != 'S')
                throw new ExceptionFreeserf("Invalid state data");

            if (header[2] != DATA_MAJOR_VERSION ||
                header[3] != DATA_MINOR_VERSION)
                throw new ExceptionFreeserf("Wrong state data version");

            DeserializeWithoutHeaderInto(targetObject, reader);
        }

        public static void DeserializeWithoutHeader<T>(T targetObject, Stream stream) where T : IState
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);

            DeserializeWithoutHeaderInto(targetObject, reader);
        }

        private static void DeserializeWithoutHeaderInto(object targetObject, BinaryReader reader)
        {
            var type = targetObject.GetType();
            PropertyMap propertyMap;

            if (propertyMapCache.ContainsKey(type))
                propertyMap = propertyMapCache[type];
            else
            {
                propertyMap = PropertyMap.Create(type);
                propertyMapCache.Add(type, propertyMap);
            }

            while (true)
            {
                var propertyName = propertyMap.DeserializePropertyName(reader);

                if (propertyName == null)
                    break;

                // We perform a reset (set value to null) when a virtual data provider
                // notifies about a virtual type change.
                bool reset = targetObject is IVirtualDataProvider vdp && reader.ReadBoolean();
                var property = type.GetProperty(propertyName, propertyFlags);

                if (property != null)
                {
                    if (reset)
                        property.SetValue(targetObject, null);

                    DeserializePropertyValueInto(targetObject, reader, property);
                }
                else // possibly a field
                {
                    var field = type.GetField(propertyName, propertyFlags);
                    var value = reset ? null : field.GetValue(targetObject);

                    if (value == null && customTypeCreators.ContainsKey(field.FieldType))
                        value = customTypeCreators[field.FieldType]?.Invoke(targetObject);

                    field.SetValue(targetObject, DeserializePropertyValue(reader, field.FieldType, value));
                }
            }
        }

        private static void DeserializePropertyValueInto(object targetObject, BinaryReader reader, PropertyInfo property)
        {
            if (typeof(IState).IsAssignableFrom(property.PropertyType) ||
                typeof(IDirtyArray).IsAssignableFrom(property.PropertyType) ||
                typeof(IDirtyMap).IsAssignableFrom(property.PropertyType))
            {
                var value = property.GetValue(targetObject);
                bool wasNull = value == null;

                if (wasNull && property.GetSetMethod(true) == null)
                    throw new ExceptionFreeserf(ErrorSystemType.Network, "Null-Property without setter.");

                if (wasNull && customTypeCreators.ContainsKey(property.PropertyType))
                {
                    value = customTypeCreators[property.PropertyType]?.Invoke(targetObject);

                    if (value != null)
                        value = DeserializePropertyValue(reader, property.PropertyType, value);
                }
                else
                    value = DeserializePropertyValue(reader, property.PropertyType, value);

                if (wasNull)
                    property.SetValue(targetObject, value);
            }
            else
            {
                property.SetValue(targetObject, DeserializePropertyValue(reader, property.PropertyType, property.GetValue(targetObject)));
            }
        }

        private static object DeserializePropertyValue(BinaryReader reader, Type type, object propertyValue)
        {
            if (typeof(IState).IsAssignableFrom(type))
            {
                if (Object.ReferenceEquals(propertyValue, null))
                    propertyValue = Activator.CreateInstance(type);

                DeserializeWithoutHeaderInto(propertyValue, reader);

                return propertyValue;
            }
            else if (type == typeof(dword))
            {
                return reader.ReadUInt32();
            }
            else if (type == typeof(int))
            {
                return reader.ReadInt32();
            }
            else if (type == typeof(word))
            {
                return reader.ReadUInt16();
            }
            else if (type == typeof(byte))
            {
                return reader.ReadByte();
            }            
            else if (type == typeof(bool))
            {
                return reader.ReadByte() != 0;
            }
            else if (type == typeof(string))
            {
                return reader.ReadString();
            }
            else if (type.IsEnum)
            {
                var baseType = Enum.GetUnderlyingType(type);
                var value = DeserializePropertyValue(reader, baseType, null);

                return Enum.ToObject(type, value);
            }
            else if (typeof(IDirtyArray).IsAssignableFrom(type))
            {
                // Note: Dirty arrays have to provide the element type as the last generic argument to make this work!
                var elementType = type.IsGenericType ? type.GetGenericArguments()[^1] : typeof(object);

                if (typeof(IDirtyArray).IsAssignableFrom(elementType) ||
                    typeof(IDirtyMap).IsAssignableFrom(elementType))
                    throw new ExceptionFreeserf("DirtyArray and DirtyMap are not supported as elements of a DirtyArray.");

                uint length = ReadCount(reader.BaseStream);
                uint serializedLength = ReadCount(reader.BaseStream);

                if (serializedLength > length)
                    throw new ExceptionFreeserf("Invalid dirty array data.");

                bool full = length == serializedLength;

                if (!(propertyValue is IDirtyArray dirtyArray))
                    dirtyArray = (IDirtyArray)Activator.CreateInstance(type, (int)length);
                else if (dirtyArray.Length != length)
                    throw new ExceptionFreeserf("Invalid dirty array data.");

                for (int i = 0; i < serializedLength; ++i)
                {
                    int index = full ? i : (int)ReadCount(reader.BaseStream);
                    dirtyArray.Set(index, DeserializePropertyValue(reader, elementType, dirtyArray.Get(index)));
                }

                return dirtyArray;
            }
            else if (typeof(IDirtyMap).IsAssignableFrom(type))
            {
                // TODO: adjust serialization like for DirtyArray
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

                for (int i = 0; i < count; ++i)
                {
                    var key = DeserializePropertyValue(reader, keyType, null);
                    var value = DeserializePropertyValue(reader, valueType, null); // TODO: use previous value instead of null
                    map.SetValue(new KeyValuePair<object, object>(key, value), i);
                }

                if (!(propertyValue is IDirtyMap dirtyMap))
                    dirtyMap = (IDirtyMap)Activator.CreateInstance(type);

                dirtyMap.Initialize(map);

                return dirtyMap;
            }
            else if (type == typeof(char))
            {
                return reader.ReadChar();
            }            
            else if (type == typeof(sbyte))
            {
                return reader.ReadSByte();
            }
            else if (type == typeof(short))
            {
                return reader.ReadInt16();
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
            else
            {
                throw new ExceptionFreeserf("Unsupport data type for state deserialization: " + type.Name);
            }
        }

        private static void SerializePropertyNullValue(BinaryWriter writer, Type propertyType)
        {
            if (typeof(IState).IsAssignableFrom(propertyType))
            {
                SerializePropertyValue(writer, (byte)0, false);
            }
            else if (propertyType == typeof(string))
            {
                writer.Write("");
            }
            else if (typeof(IDirtyArray).IsAssignableFrom(propertyType))
            {
                WriteCount(writer.BaseStream, 0); // length of zero
                WriteCount(writer.BaseStream, 0); // serialized length of zero
            }
            else if (typeof(IDirtyMap).IsAssignableFrom(propertyType))
            {
                writer.Write(0); // count of zero
            }
            else if (propertyType == typeof(Type))
            {
                writer.Write("");
            }
            else
            {
                throw new ExceptionFreeserf("Unsupport data type for state serialization: " + propertyType.Name);
            }
        }

        private static void SerializePropertyValue(BinaryWriter writer, object value, bool full)
        {
            if (value is IState)
            {
                SerializeWithoutHeader(writer, value as IState, full);
            }
            else if (value is dword dw)
            {
                writer.Write(dw);
            }
            else if (value is int n)
            {
                writer.Write(n);
            }
            else if (value is word w)
            {
                writer.Write(w);
            }
            else if (value is byte by)
            {
                writer.Write(by);
            }
            else if (value is bool b)
            {
                writer.Write((byte)(b ? 1 : 0));
            }
            else if (value is string str)
            {
                writer.Write(str);
            }
            else if (value.GetType().IsEnum)
            {
                var baseType = Enum.GetUnderlyingType(value.GetType());

                SerializePropertyValue(writer, Convert.ChangeType(value, baseType), false);
            }
            else if (value is IDirtyArray)
            {
                // First the lenght of the array.
                // Then the number of serialized elements (0 - array length).
                // Then each element is serialized:
                // - If not all elements are serialized, the index of the element is serialized.
                // - Then the element itself is serialized.

                var array = value as IDirtyArray;

                WriteCount(writer.BaseStream, (uint)array.Length);

                if (full)
                {
                    WriteCount(writer.BaseStream, (uint)array.Length);

                    foreach (var element in array)
                        SerializePropertyValue(writer, element, full);
                }
                else
                {
                    var dirtyIndices = array.DirtyIndices;
                    bool allDirty = dirtyIndices.Count == array.Length;

                    WriteCount(writer.BaseStream, (uint)dirtyIndices.Count);

                    if (!allDirty)
                    {
                        for (int i = 0; i < dirtyIndices.Count; ++i)
                        {
                            WriteCount(writer.BaseStream, (uint)dirtyIndices[i]);
                            SerializePropertyValue(writer, array.Get(dirtyIndices[i]), full);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < array.Length; ++i)
                        {
                            SerializePropertyValue(writer, array.Get(i), full);
                        }
                    }
                }
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
            else if (value is char c)
            {
                writer.Write(c);
            }            
            else if (value is sbyte sb)
            {
                writer.Write(sb);
            }
            else if (value is short s)
            {
                writer.Write(s);
            }
            else if (value is long l)
            {
                writer.Write(l);
            }
            else if (value is qword qw)
            {
                writer.Write(qw);
            }
            else if (value is float f)
            {
                writer.Write(f);
            }
            else if (value is double d)
            {
                writer.Write(d);
            }
            else if (value is decimal dec)
            {
                writer.Write(dec);
            }
            else
            {
                throw new ExceptionFreeserf("Unsupport data type for state serialization: " + value.GetType().Name);
            }
        }

        public static uint ReadCount(Stream stream)
        {
            ulong count = 0;
            int shift = 0;

            while (true)
            {
                int b = stream.ReadByte();

                if (b == -1)
                    throw new ExceptionFreeserf(ErrorSystemType.Data, "Invalid game state data.");

                count |= (((ulong)b & 0x7ful) << shift);

                if (count > uint.MaxValue)
                    throw new ExceptionFreeserf(ErrorSystemType.Data, "Invalid game state data.");

                if ((b & 0x80) == 0)
                    break;

                shift += 7;
            }

            return (uint)count;
        }

        public static void WriteCount(Stream stream, uint count)
        {
            do
            {
                if (count < 0x80)
                {
                    stream.WriteByte((byte)count);
                    break;
                }
                else
                {
                    stream.WriteByte((byte)(0x80 | (count & 0x7f)));
                    count >>= 7;
                }
            } while (count != 0);
        }
    }
}