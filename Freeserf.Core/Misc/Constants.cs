/*
 * Constants.cs - Some basic constants
 *
 * Copyright (C) 2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
    using MapPos = UInt32;
    public static class Constants
    {
        public const int NUM_RESOURCE_TYPES = 26;
        public const int NUM_TOOL_TYPES = 9;
        public const int NUM_SERF_TYPES = 27;
        public const int NUM_BUILDING_TYPES = 24;
        public const int NUM_TREATMENT_LEVEL_TYPES = 4;
        public const MapPos INVALID_MAPPOS = MapPos.MaxValue;
    }
}
