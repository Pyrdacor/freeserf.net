/*
 * MutableTexture.cs - OpenGL texture creation
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

namespace Freeserf.Renderer.OpenTK
{
    internal class MutableTexture : Texture
    {
        byte[] data = null;

        public MutableTexture(int width, int height)
            : base(width, height)
        {
            data = new byte[width * height * 4]; // initialized with zeros so non-occupied areas will be transparent
        }

        public void AddSprite(Position position, byte[] data, int width, int height)
        {
            for (int y = 0; y < height; ++y)
            {
                System.Buffer.BlockCopy(data, y * width * 4, this.data, (position.X + (position.Y + y) * Width) * 4, width * 4);
            }
        }

        public void Finish(int numMipMapLevels)
        {
            Create(PixelFormat.BGRA8, data, numMipMapLevels);

            data = null;
        }
    }
}
