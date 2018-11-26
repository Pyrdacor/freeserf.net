/*
 * DataSourceLegacy.cs - Legacy game resources file functions
 *
 * Copyright (C) 2017  Wicked_Digger <wicked_digger@mail.ru>
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
using System.Collections.Generic;

namespace Freeserf
{
    public abstract class DataSourceLegacy : DataSource
    {
        public DataSourceLegacy(string path)
            : base(path)
        {

        }

        unsafe protected bool LoadAnimationTable(Buffer data)
        {
            if (data == null)
            {
                return false;
            }

            // The serf animation table is stored in big endian order in the data file.
            data.SetEndianess(Endian.Endianess.Big);

            // * Starts with 200 uint32s that are offsets from the start
            // of this table to an animation table (one for each animation).
            // * The animation tables are of varying lengths.
            // Each entry in the animation table is three bytes long.
            // First byte is used to determine the serf body sprite.
            // Second byte is a signed horizontal sprite offset.
            // Third byte is a signed vertical offset.

            uint* animationBlock = (uint*)data.Data;

            // Endianess convert from big endian.
            for (uint i = 0; i < 200; ++i)
            {
                animationBlock[i] = Endian.Betoh(animationBlock[i]);
            }

            uint[] sizes = new uint[200];

            for (uint i = 0; i < 200; ++i)
            {
                uint a = animationBlock[i];
                uint next = data.Size;

                for (int j = 0; j < 200; ++j)
                {
                    uint b = animationBlock[j];

                    if (b > a)
                    {
                        next = Math.Min(next, b);
                    }
                }

                sizes[i] = (next - a) / 3;
            }

            for (uint i = 0; i < 200; ++i)
            {
                uint offset = animationBlock[i];

                byte* anims = (byte*)animationBlock + offset;
                List<Animation> animations = new List<Animation>();

                for (uint j = 0; j < sizes[i]; ++j)
                {
                    Animation a = new Animation();

                    a.Sprite = anims[j * 3 + 0];
                    a.X = (sbyte)anims[j * 3 + 1];
                    a.Y = (sbyte)anims[j * 3 + 2];

                    animations.Add(a);
                }

                animationTable.Add(animations);
            }

            return true;
        }
    }
}
