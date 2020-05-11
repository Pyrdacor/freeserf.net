/*
 * Random.cs - Random number generator
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
using System.Text;

namespace Freeserf
{
    public class Random
    {
        System.Random internalRandom;
        ushort[] state = new ushort[3];

        ushort RandomInt()
        {
            return (ushort)internalRandom.Next(0, 65536);
        }

        public Random()
        {
            internalRandom = new System.Random(DateTime.Now.Millisecond);

            state[0] = RandomInt();
            state[1] = RandomInt();
            state[2] = RandomInt();

            Next();
        }

        public Random(ushort value)
        {
            state[0] = value;
            state[1] = value;
            state[2] = value;
        }

        public Random(Random randomState)
        {
            state[0] = randomState.state[0];
            state[1] = randomState.state[1];
            state[2] = randomState.state[2];
        }

        public Random(string str)
        {
            var bytes = Encoding.ASCII.GetBytes(str);
            ulong tmp = 0;

            for (int i = 15; i >= 0; --i)
            {
                tmp <<= 3;
                byte c = (byte)(bytes[i] - (byte)'0' - 1);
                tmp |= c;
            }

            state[0] = (ushort)(tmp & 0xFFFF);
            tmp >>= 16;
            state[1] = (ushort)(tmp & 0xFFFF);
            tmp >>= 16;
            state[2] = (ushort)(tmp & 0xFFFF);
        }

        public Random(ushort base0, ushort base1, ushort base2)
        {
            state[0] = base0;
            state[1] = base1;
            state[2] = base2;
        }

        public ushort Next()
        {
            ushort[] random = state;
            ushort result = (ushort)((random[0] + random[1]) ^ random[2]);
            random[2] += random[1];
            random[1] ^= random[2];
            random[1] = (ushort)((random[1] >> 1) | (random[1] << 15));
            random[2] = (ushort)((random[2] >> 1) | (random[2] << 15));
            random[0] = result;

            return result;
        }

        public override string ToString()
        {
            ulong tmp0 = state[0];
            ulong tmp1 = state[1];
            ulong tmp2 = state[2];

            string str = "";

            ulong tmp = tmp0;
            tmp |= tmp1 << 16;
            tmp |= tmp2 << 32;

            for (int i = 0; i < 16; ++i)
            {
                byte c = (byte)(tmp & 0x07);
                c += (byte)'1';
                str += (char)c;
                tmp >>= 3;
            }

            return str;
        }

        public static Random operator ^(Random left, Random right)
        {
            ushort s0 = (ushort)(left.state[0] ^ right.state[0]);
            ushort s1 = (ushort)(left.state[1] ^ right.state[1]);
            ushort s2 = (ushort)(left.state[2] ^ right.state[2]);

            return new Random(s0, s1, s2);
        }
    }
}
