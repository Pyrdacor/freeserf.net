using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;
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
