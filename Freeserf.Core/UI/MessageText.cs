using System;
using System.Collections.Generic;

namespace Freeserf.UI
{
    internal static class MessageText
    {
        public const int Columns = 16;
        public const int LineHeight = 10;

        public static string[] Wrap(string text, int columns, int maxLines, string context)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLines);

            if (text.Length == 0)
                return Array.Empty<string>();

            var lines = new List<string>();
            foreach (string paragraph in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string line = "";
                foreach (string word in paragraph.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.Length != 0 && line.Length + 1 + word.Length > columns)
                    {
                        lines.Add(line);
                        line = "";
                    }

                    int offset = 0;
                    while (word.Length - offset > columns)
                    {
                        lines.Add(word.Substring(offset, columns));
                        offset += columns;
                    }

                    string remainder = word.Substring(offset);
                    line += (line.Length == 0 ? "" : " ") + remainder;
                }

                lines.Add(line);
            }

            if (lines.Count > maxLines)
                ReportLayoutError(context, $"needs {lines.Count} lines but only {maxLines} fit at {columns} columns");

            return lines.ToArray();
        }

        internal static void ReportLayoutError(string context, string detail)
        {
            string message = $"Message layout '{context}' {detail}. An explicit layout change is required.";
            Log.Error.Write(ErrorSystemType.UI, message);
            throw new ExceptionFreeserf(ErrorSystemType.UI, message);
        }
    }
}
