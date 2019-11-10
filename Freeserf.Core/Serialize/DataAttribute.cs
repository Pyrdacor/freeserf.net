/*
 * DataAttribute.cs - C# attributes for data classes
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

namespace Freeserf.Serialize
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    internal class DataAttribute : Attribute
    {
        /// <summary>
        /// Mark a member as serializable data.
        /// </summary>
        /// <param name="dataName">The serialized name</param>
        public DataAttribute(string dataName = null)
        {
            DataName = dataName;
        }

        public string DataName
        {
            get;
            private set;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    internal class DataClassAttribute : Attribute
    {
        /// <summary>
        /// Mark a class or struct as serializable.
        /// Each public field or property is marked as serializable data
        /// as long as it is not marked with the Ignore attribute.
        /// </summary>
        public DataClassAttribute()
        {

        }
    }
}
