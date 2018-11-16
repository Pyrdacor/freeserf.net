/*
 * IRenderLayer.cs - Render layer interface
 *
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

namespace Freeserf.Render
{
    public delegate Position PositionTransformation(Position position);
    public delegate Size SizeTransformation(Size size);

    public interface IRenderLayer
    {
        Layer Layer { get; }
        Color ColorKey
        {
            get;
            set;
        }
        bool Visible
        {
            get;
            set;
        }
        PositionTransformation PositionTransformation
        {
            get;
            set;
        }
        SizeTransformation SizeTransformation
        {
            get;
            set;
        }

        void Render();
    }

    public interface IRenderLayerFactory
    {
        IRenderLayer Create(Layer layer, Texture texture, bool supportColoredRects = false, Color colorKey = null);
    }
}
