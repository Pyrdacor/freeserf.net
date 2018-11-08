using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Render
{
    public interface ITextureAtlas
    {
        Position GetOffset(uint spriteIndex);
    }

    public interface ITextureAtlasBuilder
    {
        void AddSprite(uint spriteIndex, Sprite sprite);
        ITextureAtlas Create();
    }

    public interface ITextureAtlasBuilderFactory
    {
        ITextureAtlasBuilder Create();
    }
}
