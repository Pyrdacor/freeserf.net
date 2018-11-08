using System;
using System.Collections.Generic;
using System.Text;

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
            Create(PixelFormat.RGBA8, data, numMipMapLevels);
            data = null;
        }
    }
}
