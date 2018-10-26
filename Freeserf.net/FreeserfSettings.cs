using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    public static class Endian
    {
        public enum Endianess
        {
            Big,
            Little,
            Default
        }

        public static bool IsBigEndian => !BitConverter.IsLittleEndian;
        public static Endianess HostEndianess = IsBigEndian ? Endianess.Big : Endianess.Little;

        static ulong SwapBytes(ulong x)
        {
            // swap adjacent 32-bit blocks
            x = (x >> 32) | (x << 32);
            // swap adjacent 16-bit blocks
            x = ((x & 0xFFFF0000FFFF0000) >> 16) | ((x & 0x0000FFFF0000FFFF) << 16);
            // swap adjacent 8-bit blocks
            return ((x & 0xFF00FF00FF00FF00) >> 8) | ((x & 0x00FF00FF00FF00FF) << 8);
        }

        public static T ByteSwap<T>(T x)
        {
            var ul = (ulong)Convert.ChangeType(x, typeof(ulong));

            return (T)Convert.ChangeType(SwapBytes(ul), typeof(T));
        }

        public static T Betoh<T>(T x)
        {
            return IsBigEndian ? x : ByteSwap(x);
        }

        public static T Letoh<T>(T x)
        {
            return IsBigEndian ? ByteSwap(x) : x;
        }

        public static T Htobe<T>(T x)
        {
            return Betoh(x);
        }

        public static T Htole<T>(T x)
        {
            return Letoh(x);
        }
    }

    public static class Settings
    {
        public static Encoding Encoding = Encoding.ASCII;
    }
}
