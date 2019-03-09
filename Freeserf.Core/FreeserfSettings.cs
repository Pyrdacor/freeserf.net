/*
 * FreeserfSetting.cs - Endianness conversions, Encoding, etc
 *
 * Copyright (C) 2007-2012  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018       Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Text;
using Freeserf.Data;

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
            if (TypeSize<T>.Size == 1)
                return x;

            var ul = (ulong)Convert.ChangeType(x, typeof(ulong));

            ul = SwapBytes(ul);

            switch (TypeSize<T>.Size)
            {
                case 2:
                    ul >>= 48;
                    break;
                case 4:
                    ul >>= 32;
                    break;
                case 8:
                    break;
                default:
                    throw new ExceptionFreeserf("data", "Data type");
            }

            return (T)Convert.ChangeType(ul, typeof(T));
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
