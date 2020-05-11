/*
 * Triangle.cs - Textured triangle
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

using Freeserf.Render;

namespace Freeserf.Renderer
{
    public class Triangle : Node, ITriangle
    {
        protected int drawIndex = -1;
        Position textureAtlasOffset = null;
        Position maskTextureAtlasOffset = null;

        public Triangle(int width, int height, int textureAtlasX, int textureAtlasY, Rect virtualScreen)
            : base(Shape.Triangle, width, height, virtualScreen)
        {
            textureAtlasOffset = new Position(textureAtlasX, textureAtlasY);
            maskTextureAtlasOffset = new Position(textureAtlasX, textureAtlasY);
        }

        public Position TextureAtlasOffset
        {
            get => textureAtlasOffset;
            set
            {
                if (textureAtlasOffset == value)
                    return;

                textureAtlasOffset = new Position(value);

                UpdateTextureAtlasOffset();
            }
        }

        public int BaseLineOffset
        {
            get => 0;
            set
            {
                // do nothing
            }
        }

        public Position MaskTextureAtlasOffset
        {
            get => maskTextureAtlasOffset;
            set
            {
                if (maskTextureAtlasOffset == value)
                    return;

                maskTextureAtlasOffset = new Position(value);

                UpdateTextureAtlasOffset();
            }
        }

        protected override void AddToLayer()
        {
            drawIndex = (Layer as RenderLayer).GetDrawIndex(this, maskTextureAtlasOffset);
        }

        protected override void RemoveFromLayer()
        {
            if (drawIndex != -1)
            {
                (Layer as RenderLayer).FreeDrawIndex(drawIndex);
                drawIndex = -1;
            }
        }

        protected override void UpdatePosition()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdatePosition(drawIndex, this);
        }

        protected virtual void UpdateTextureAtlasOffset()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateTextureAtlasOffset(drawIndex, this, maskTextureAtlasOffset);
        }
    }

    public class TriangleFactory : ITriangleFactory
    {
        readonly Rect virtualScreen = null;

        public TriangleFactory(Rect virtualScreen)
        {
            this.virtualScreen = virtualScreen;
        }

        public ITriangle Create(int width, int height, int textureAtlasX, int textureAtlasY)
        {
            return new Triangle(width, height, textureAtlasX, textureAtlasY, virtualScreen);
        }
    }
}
