/*
 * Sprite.cs - Textured sprite
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
        Position textureAtlasOffset = null;

        protected Sprite(Shape shape, int width, int height, int textureAtlasX, int textureAtlasY)
            : base(shape, width, height)
        {
            textureAtlasOffset = new Position(textureAtlasX, textureAtlasY);
        }

        public Sprite(int width, int height, int textureAtlasX, int textureAtlasY)
            : base(Shape.Rect, width, height)
        {
            textureAtlasOffset = new Position(textureAtlasX, textureAtlasY);
        }

        public Position TextureAtlasOffset
        {
            get => textureAtlasOffset;
            set
            {
                if (textureAtlasOffset == value)
                    return;

                textureAtlasOffset = value;

                UpdateTextureAtlasOffset();
            }
        }

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

        protected virtual void UpdateTextureAtlasOffset()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateTextureAtlasOffset(drawIndex, this);
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
