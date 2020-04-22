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
    public sealed class Pointer<T> where T : class
    {
        public T Value = null;
    }

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

        public static uint BitU(int n)
        {
            return (1u << n);
        }

        public static bool BitTest(int x, int n)
        {
            return (x & Bit(n)) != 0;
        }

        public static bool BitTest(uint x, int n)
        {
            return (x & BitU(n)) != 0u;
        }

        public static void BitInvert(ref int x, int n)
        {
            x = BitInvert(x, n);
        }

        public static int BitInvert(int x, int n)
        {
            return x ^ Bit(n);
        }

        public static void SetBit(ref int value, int bit, bool set)
        {
            if (set)
                value |= Bit(bit);
            else
                value &= ~Bit(bit);
        }

        public static void SetBit(ref uint value, int bit, bool set)
        {
            if (set)
                value |= BitU(bit);
            else
                value &= ~BitU(bit);
        }

        public static void Clamp(int low, ref int value, int high)
        {
            value = Clamp(low, value, high);
        }

        public static int Clamp(int low, int value, int high)
        {
            return Math.Max(low, Math.Min(value, high));
        }

        public static void Clamp(uint low, ref uint value, uint high)
        {
            value = Clamp(low, value, high);
        }

        public static uint Clamp(uint low, uint value, uint high)
        {
            return Math.Max(low, Math.Min(value, high));
        }

        public static void Clamp(float low, ref float value, float high)
        {
            value = Clamp(low, value, high);
        }

        public static float Clamp(float low, float value, float high)
        {
            return Math.Max(low, Math.Min(value, high));
        }

        public static bool FloatEqual(float f1, float f2)
        {
            return Math.Abs(f1 - f2) < 0.00001f;
        }

        public static int Round(float f)
        {
            return (int)Math.Round(f);
        }

        public static int Round(double d)
        {
            return (int)Math.Round(d);
        }

        public static float Min(float firstValue, float secondValue, params float[] values)
        {
            float min = Math.Min(firstValue, secondValue);

            foreach (var value in values)
            {
                if (value < min)
                    min = value;
            }

            return min;
        }

        public static float Max(float firstValue, float secondValue, params float[] values)
        {
            float max = Math.Max(firstValue, secondValue);

            foreach (var value in values)
            {
                if (value > max)
                    max = value;
            }

            return max;
        }

        public static int Min(int firstValue, int secondValue, params int[] values)
        {
            int min = Math.Min(firstValue, secondValue);

            foreach (var value in values)
            {
                if (value < min)
                    min = value;
            }

            return min;
        }

        public static int Max(int firstValue, int secondValue, params int[] values)
        {
            int max = Math.Max(firstValue, secondValue);

            foreach (var value in values)
            {
                if (value > max)
                    max = value;
            }

            return max;
        }

        public static string SecondsToTime(uint seconds)
        {
            var hours = seconds / 3600;
            seconds -= hours * 3600;
            var minutes = seconds / 60;
            seconds -= minutes * 60;

            return $"{hours:00}:{minutes:00}:{seconds:00}";
        }
    }
}
