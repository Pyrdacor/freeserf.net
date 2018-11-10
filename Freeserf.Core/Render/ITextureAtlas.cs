using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Render
{
    public interface ITextureAtlas
    {
        Texture Texture
        {
            get;
        }

        Position GetOffset(uint spriteIndex);
    }

    public interface ITextureAtlasBuilder
    {
        void AddSprite(uint spriteIndex, Sprite sprite);
        ITextureAtlas Create(int numMipMapLevels = 0);
    }

    public interface ITextureAtlasBuilderFactory
    {
        ITextureAtlasBuilder Create();
    }
}
