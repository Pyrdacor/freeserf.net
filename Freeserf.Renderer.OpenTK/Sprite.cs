using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;
using Freeserf.Render;

namespace Freeserf.Renderer.OpenTK
{
    /// <summary>
    /// A sprite has a fixed size and an offset into the layer's texture atlas.
    /// The layer will sort sprites by size and then by the texture atlas offset.
    /// </summary>
    public class Sprite : Node, ISprite
    {
        protected int drawIndex = -1;

        public Sprite(int width, int height, int textureAtlasX, int textureAtlasY)
            : base(Shape.Rect, width, height)
        {
            TextureAtlasOffset = new Position(textureAtlasX, textureAtlasY);
        }

        public Position TextureAtlasOffset { get; } = null;

        protected override void AddToLayer()
        {
            base.AddToLayer();

            drawIndex = (Layer as RenderLayer).GetDrawIndex(this);
        }

        protected override void RemoveFromLayer()
        {
            base.RemoveFromLayer();

            (Layer as RenderLayer).FreeDrawIndex(drawIndex);
            drawIndex = -1;
        }

        protected override void UpdatePosition()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdatePosition(drawIndex, this);
        }
    }

    public class SpriteFactory : ISpriteFactory
    {
        public ISprite Create(int width, int height, int textureAtlasX, int textureAtlasY)
        {
            return new Sprite(width, height, textureAtlasX, textureAtlasY);
        }
    }
}
