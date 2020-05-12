/*
 * Extensions.cs - Some useful class extensions
 *
 * Copyright (C) 2018  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Freeserf
{
    public static class StringExtensions
    {
        public static string ReplaceInvalidPathChars(this string str, char replacement)
        {
            string invalid = "";
            var invalidFileNameChars = new List<char>(Path.GetInvalidFileNameChars());

            foreach (char c in Path.GetInvalidPathChars())
            {
                if (invalidFileNameChars.Contains(c))
                    invalid += c;
            }

            return ReplaceChars(str, invalid, replacement);
        }

        public static string ReplaceChars(this string str, string charsToReplace, char replacement)
        {
            string result = str;

            foreach (char c in charsToReplace)
            {
                result = result.Replace(c, replacement);
            }

            return result;
        }

        public static T GetValue<T>(this string value)
        {
            if (typeof(T).IsEnum)
                return (T)Enum.Parse(typeof(T), value);

            if (typeof(T) == typeof(bool))
                return (T)(object)(value == "1" || value.ToLower() == "true");

            return (T)Convert.ChangeType(value, typeof(T));
        }
    }

    public static class ArrayExtensions
    {
        public static IEnumerable<T> SliceRow<T>(this T[,] array, int row)
        {
            for (var i = 0; i < array.GetLength(1); ++i)
            {
                yield return array[row, i];
            }
        }
    }
}
