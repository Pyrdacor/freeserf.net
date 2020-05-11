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
    // -------------
    // Landscape
    // Water waves
    // Paths
    // Objects (MapObjects, Flags), Resources on Flags, Shadows of the object
    // Buildings (like Objects but masked for build progress)
    // Serfs
    // Builds
    // Gui
    // Gui Buildings
    // Gui Font
    // Minimap
    // Cursor

    public enum Layer
    {
        None = 0,
        Landscape = 1 << 0,
        Waves = 1 << 1,
        Paths = 1 << 2,
        Objects = 1 << 3,
        Serfs = 1 << 4,
        Buildings = 1 << 5,
        Builds = 1 << 6,
        Gui = 1 << 7,
        GuiBuildings = 1 << 8, // we have to display buildings inside some windows (e.g. build menu)
        GuiFont = 1 << 9, // new UI fonts
        Minimap = 1 << 10,
        Cursor = 1 << 11
    }

    public partial class Global
    {
        // Objects (inlcuding Flags), Buildings and Serfs share a base Z value range from 0.05 to 0.95.
        // Higher values mean drawing over lower valued sprites.
        public static readonly float[] LayerBaseZ = new float[]
        {
            0.01f,  // Landscape
            0.02f,  // Waves
            0.03f,  // Paths
            0.05f,  // Objects
            0.05f,  // Serfs
            0.05f,  // Buildings
            0.96f,  // Builds
            0.97f,  // Gui
            0.97f,  // Gui Buildings
            0.97f,  // Gui Font
            0.97f,  // Minimap
            0.98f   // Cursor
        };
    }
}
