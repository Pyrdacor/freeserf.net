/*
 * MutableTexture.cs - OpenGL texture creation
 *
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Freeserf.Renderer
{
    internal class MutableTexture : Texture
    {
        int width = 0;
        int height = 0;
        byte[] data = null;

        public MutableTexture(int width, int height)
            : base(width, height)
        {
            this.width = width;
            this.height = height;
            data = new byte[width * height * 4]; // initialized with zeros so non-occupied areas will be transparent
        }

        public override int Width => width;
        public override int Height => height;

        public void AddSprite(Position position, byte[] data, int width, int height)
        {
            for (int y = 0; y < height; ++y)
            {
                System.Buffer.BlockCopy(data, y * width * 4, this.data, (position.X + (position.Y + y) * Width) * 4, width * 4);
            }
        }

        public void SetPixel(int x, int y, byte r, byte g, byte b, byte a = 255)
        {
            int index = y * Width + x;

            data[index * 4 + 0] = b;
            data[index * 4 + 1] = g;
            data[index * 4 + 2] = r;
            data[index * 4 + 3] = a;
        }

        public void SetPixels(byte[] pixelData)
        {
            if (pixelData == null)
                throw new ExceptionFreeserf(ErrorSystemType.Textures, "Pixel data was null.");

            if (pixelData.Length != data.Length)
                throw new ExceptionFreeserf(ErrorSystemType.Textures, "Pixel data size does not match texture data size.");

            System.Buffer.BlockCopy(pixelData, 0, data, 0, pixelData.Length);
        }

        public void Finish(int numMipMapLevels)
        {
            Create(PixelFormat.BGRA8, data, numMipMapLevels);

            data = null;
        }

        public void Resize(int width, int height)
        {
            if (data != null && this.width == width && this.height == height)
                return;

            this.width = width;
            this.height = height;
            data = new byte[width * height * 4]; // initialized with zeros so non-occupied areas will be transparent
        }
    }

    public class MinimapTextureFactory : Render.IMinimapTextureFactory
    {
        static MutableTexture minimap = null;

        public Render.Texture GetMinimapTexture()
        {
            minimap ??= new MutableTexture(128, 128);

            return minimap;
        }

        public void FillMinimapTexture(byte[] colorData)
        {
            if (minimap is not MutableTexture)
                throw new ExceptionFreeserf(ErrorSystemType.Textures, "The given minimap texture is no mutable texture known by this renderer.");

            minimap.SetPixels(colorData);
            minimap.Finish(0);
        }

        public void ResizeMinimapTexture(int width, int height)
        {
            if (minimap is not MutableTexture)
                throw new ExceptionFreeserf(ErrorSystemType.Textures, "The given minimap texture is no mutable texture known by this renderer.");

            minimap.Resize(width, height);
        }
    }
}
