using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Render
{
    public interface ITextureAtlas
    {
        void AddSprite(uint spriteIndex, int width, int height, byte[] data);
        Position GetOffset(uint spriteIndex);
    }
}
