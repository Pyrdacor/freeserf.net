using System;
using System.Collections.Generic;
using System.IO;

namespace Freeserf
{
    public static class StringExtensions
    {
        public static string ReplaceInvalidPathChars(this string str, char replacement)
        {
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

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
