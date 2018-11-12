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
    // Objects(MapObjects, Flags), Resources on Flags, Shadows of the object
    // Buildings (like Objects but masked for build progress)
    // Serfs
    // Builds
    // Cursor

    public enum Layer
    {
        None = 0,
        Landscape = 1 << 0,
        Grid = 1 << 1,
        Paths = 1 << 2,
        Objects = 1 << 3,
        Buildings = 1 << 4,
        Serfs = 1 << 5,
        Builds = 1 << 6,
        Cursor = 1 << 7,        
        All = Landscape | Paths | Objects | Buildings | Serfs | Cursor
    }

    public partial class Global
    {
        // Objects, Buildings and Serfs share a base Z value range from 0.05 to 0.95.
        // Higher values mean drawing over lower valued sprites.
        public static readonly float[] LayerBaseZ = new float[]
        {
            0.00f,  // None
            0.00f,  // Landscape
            0.01f,  // Grid
            0.02f,  // Paths
            0.05f,  // Objects
            0.05f,  // Buildings
            0.05f,  // Serfs
            0.96f,  // Builds
            0.97f   // Cursor
        };
    }
}
