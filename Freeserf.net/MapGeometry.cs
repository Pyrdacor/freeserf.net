/*
 * MapGeometry.cs - Map geometry functions
 *
 * Copyright (C) 2013-2016  Jon Lund Steffensen <jonlst@gmail.com>
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    // MapPos is a compact composition of col and row values that
    // uniquely identifies a vertex in the map space. It is also used
    // directly as index to map data arrays.
    using MapPos = UInt32;

    public static partial class Global
    {
        public const MapPos BadMapPos = MapPos.MaxValue;
    }

    // Map directions
    //
    //    A ______ B
    //     /\    /
    //    /  \  /
    // C /____\/ D
    //
    // Six standard directions:
    // RIGHT: A to B
    // DOWN_RIGHT: A to D
    // DOWN: A to C
    // LEFT: D to C
    // UP_LEFT: D to A
    // UP: D to B
    //
    // Non-standard directions:
    // UP_RIGHT: C to B
    // DOWN_LEFT: B to C
    public enum Direction
    {
        None = -1,

        Right = 0,
        DownRight,
        Down,
        Left,
        UpLeft,
        Up
    }

    // Return the given direction turned clockwise a number of times.
    //
    // Return the resulting direction from turning the given direction
    // clockwise in 60 degree increment the specified number of times.
    // If times is a negative number the direction will be turned counter
    // clockwise.

    // Cycle direction (clockwise or counter-clockwise)
    public enum Cycle
    {
        CW,
        CCW
    }    

    public class MapGeometry
    {
    }
}
