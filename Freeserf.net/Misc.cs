using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    public static class Misc
    {
        unsafe public static void CopyByte(byte* pointer, byte value, uint count)
        {
            while (count > 0)
            {
                *pointer++ = value;
                --count;
            }
        }

        unsafe public static void CopyWord(byte* pointer, ushort value, uint count)
        {
            ushort* p = (ushort*)pointer;

            while (count > 0)
            {
                *p++ = value;
                --count;
            }
        }

        unsafe public static void CopyDWord(byte* pointer, uint value, uint count)
        {
            uint* p = (uint*)pointer;

            while (count > 0)
            {
                *p++ = value;
                --count;
            }
        }

        unsafe public static void CopyQWord(byte* pointer, ulong value, uint count)
        {
            ulong* p = (ulong*)pointer;

            while (count > 0)
            {
                *p++ = value;
                --count;
            }
        }

        public static int Bit(int n)
        {
            return (1 << n);
        }

        public static bool BitTest(int x, int n)
        {
            return (x & Bit(n)) != 0;
        }

        public static void BitInvert(ref int x, int n)
        {
            x = BitInvert(x, n);
        }

        public static int BitInvert(int x, int n)
        {
            return x ^ Bit(n);
        }

        public static void Clamp(int low, ref int value, int high)
        {
            value = Clamp(low, value, high);
        }

        public static int Clamp(int low, int value, int high)
        {
            return Math.Max(low, Math.Min(value, high));
        }
    }
}
