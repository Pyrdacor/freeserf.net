/*
 * Color.cs - Basic color (used mainly for player colors)
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

using System.Collections.Generic;

namespace Freeserf
{
    using Serialize;

    [DataClass]
    internal class Color : IState
    {
        public byte Red;
        public byte Green;
        public byte Blue;

        public bool Dirty => false; // Will never change during game.
        public IReadOnlyList<string> DirtyProperties { get; } = new List<string>();

        public void ResetDirtyFlag() { }
    }
}
