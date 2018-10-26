/*
 * Misc.cs - Various definitions of general usefulness
 *
 * Copyright (C) 2012  Jon Lund Steffensen <jonlst@gmail.com>
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
