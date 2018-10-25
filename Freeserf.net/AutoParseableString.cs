using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Freeserf
{
    /// <summary>
    ///  Replacement of std::stringstream in CommandLine
    /// </summary>
    public class AutoParseableString
    {
        string content = "";
        static readonly Regex NoDigitRegex = new Regex("[^0-9]", RegexOptions.Compiled);

        public AutoParseableString(string content)
        {
            this.content = content;
        }

        public void Skip(int num)
        {
            num = Math.Min(num, content.Length);

            content = content.Substring(num);
        }

        public void Retrieve(out uint val)
        {
            Retrieve(out int temp);

            val = (uint)temp;
        }


        public void Retrieve(out int val)
        {
            var match = NoDigitRegex.Match(content);

            if (!match.Success || match.Index == 0)
            {
                val = -1;
                return;
            }

            string valueString = content.Substring(0, match.Index);

            content = content.Substring(match.Index);

            if (!int.TryParse(valueString, out val))
                val = -1;
        }

        public void Retrieve(out string val)
        {
            int end = content.IndexOfAny(new char[] { ' ', '\t' });

            if (end == -1)
            {
                val = content;
                content = "";
            }
            else
            {
                val = content.Substring(0, end);
                content = content.Substring(end + 1);
            }
        }

        public string ReadLine()
        {
            var temp = content;

            content = "";

            return temp;
        }
    }
}