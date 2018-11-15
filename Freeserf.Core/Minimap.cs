/*
 * Minimap.cs - Minimap GUI component
 *
 * Copyright (C) 2013   Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018   Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
    internal class Minimap : GuiObject
    {
        public Minimap(Interface interf)
        {

        }

        protected override void InternalDraw()
        {
            throw new NotImplementedException();
        }
    }

    internal class MinimapGame : Minimap
    {
        public MinimapGame(Interface interf, Game game)
            : base(interf)
        {

        }
    }
}
