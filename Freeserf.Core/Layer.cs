/*
 * Layer.cs - Layer definition
 *
 * Copyright (C) 2013  Jon Lund Steffensen <jonlst@gmail.com>
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


namespace Freeserf
{
    // Drawing order
    // Landscape(including WaterWaves)
    // Grid
    // Paths
    // SerfsBehind
    // Objects(MapObjects, Flags, Buildings), Resources on Flags, Shadows of the object
    // Serfs
    // Cursor

    public enum Layer
    {
        None = 0,
        Landscape = 1 << 0,
        Paths = 1 << 1,
        Objects = 1 << 2,
        Serfs = 1 << 3,
        Cursor = 1 << 4,
        Grid = 1 << 5,
        Builds = 1 << 6,
        All = Landscape | Paths | Objects | Serfs | Cursor
    }
}
