using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf.Render
{
    public interface ITriangle : ISprite
    {
        bool Up { get; }
    }

    public interface ITriangleFactory
    {
        ITriangle Create(bool up, int width, int height, int textureAtlasX, int textureAtlasY);
    }
}
