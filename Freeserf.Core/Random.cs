/*
 * Random.cs - Random number generator
 *
 * Copyright (C) 2012       Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018-2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
    using Serialize;
    using word = UInt16;

    internal class Random : State
    {
        public DirtyArray<word> State { get; } = new DirtyArray<word>(3);

        public Random()
        {
            var internalRandom = new System.Random(DateTime.Now.Millisecond);

            ushort RandomInt()
            {
                return (ushort)internalRandom.Next(0, 65536);
            }

            State[0] = RandomInt();
            State[1] = RandomInt();
            State[2] = RandomInt();

            Next();
            Init();
        }

        public Random(ushort value)
        {
            State[0] = value;
            State[1] = value;
            State[2] = value;

            Init();
        }

        public Random(Random randomState)
        {
            State[0] = randomState.State[0];
            State[1] = randomState.State[1];
            State[2] = randomState.State[2];

            Init();
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

            State[0] = (ushort)(tmp & 0xFFFF);
            tmp >>= 16;
            State[1] = (ushort)(tmp & 0xFFFF);
            tmp >>= 16;
            State[2] = (ushort)(tmp & 0xFFFF);

            Init();
        }

        public Random(ushort base0, ushort base1, ushort base2)
        {
            State[0] = base0;
            State[1] = base1;
            State[2] = base2;

            Init();
        }

        private void Init()
        {
            State.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(State)); };
        }

        public override void ResetDirtyFlag()
        {
            lock (dirtyLock)
            {
                State.Dirty = false;

                ResetDirtyFlagUnlocked();
            }
        }

        public ushort Next()
        {
            ushort[] random = State;
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
            ulong tmp0 = State[0];
            ulong tmp1 = State[1];
            ulong tmp2 = State[2];

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
            ushort s0 = (ushort)(left.State[0] ^ right.State[0]);
            ushort s1 = (ushort)(left.State[1] ^ right.State[1]);
            ushort s2 = (ushort)(left.State[2] ^ right.State[2]);

            return new Random(s0, s1, s2);
        }
    }
}
