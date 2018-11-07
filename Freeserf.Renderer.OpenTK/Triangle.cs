/*
 * Triangle.cs - Textured triangle
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

using Freeserf.Render;

namespace Freeserf.Renderer.OpenTK
{
    public class Triangle : Sprite, ITriangle
    {
        public Triangle(bool up, int width, int height, int textureAtlasX, int textureAtlasY)
            : base(width, height, textureAtlasX, textureAtlasY)
        {
            Up = up;
        }

        public bool Up { get; }
    }

    public class TriangleFactory : ITriangleFactory
    {
        public ITriangle Create(bool up, int width, int height, int textureAtlasX, int textureAtlasY)
        {
            return new Triangle(up, width, height, textureAtlasX, textureAtlasY);
        }
    }
}
