using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf.Render
{
    public interface ISprite : IRenderNode
    {
        Position TextureAtlasOffset
        {
            get;
        }
    }

    public interface ISpriteFactory
    {
        ISprite Create(int width, int height, int textureAtlasX, int textureAtlasY);
    }
}
