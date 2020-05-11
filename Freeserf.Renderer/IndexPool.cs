/*
 * IndexPool.cs - Pool of indices which handles index reusing
 *
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using System.Collections.Generic;

namespace Freeserf.Renderer
{
    internal class IndexPool
    {
        readonly List<int> releasedIndices = new List<int>();
        int firstFree = 0;

        public int AssignNextFreeIndex(out bool reused)
        {
            if (releasedIndices.Count > 0)
            {
                reused = true;

                int index = releasedIndices[0];

                releasedIndices.RemoveAt(0);

                return index;
            }

            reused = false;

            if (firstFree == int.MaxValue)
            {
                throw new ExceptionFreeserf(ErrorSystemType.Render, "No free index available.");
            }

            return firstFree++;
        }

        public void UnassignIndex(int index)
        {
            releasedIndices.Add(index);
        }

        public bool AssignIndex(int index)
        {
            if (releasedIndices.Contains(index))
            {
                releasedIndices.Remove(index);
                return true;
            }

            if (index == firstFree)
                ++firstFree;

            return false;
        }
    }
}
