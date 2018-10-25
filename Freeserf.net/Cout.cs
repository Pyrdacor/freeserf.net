using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    // TODO: Change output if it is not a console application!
    public static class Cout
    {
        public static void Write(string text)
        {
            Console.Write(text);
        }

        public static void WriteLine(string text = "")
        {
            Console.WriteLine(text);
        }
    }
}
