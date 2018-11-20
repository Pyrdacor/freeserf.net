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

        public Sprite(int width, int height, int textureAtlasX, int textureAtlasY, Rect virtualScreen)
            : base(Shape.Rect, width, height, virtualScreen)
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

                textureAtlasOffset = new Position(value);

                UpdateTextureAtlasOffset();
            }
        }

        protected override void AddToLayer()
        {
            drawIndex = (Layer as RenderLayer).GetDrawIndex(this);
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
                (Layer as RenderLayer).UpdateTextureAtlasOffset(drawIndex, this);
        }

        public override void Resize(int width, int height)
        {
            if (Width == width && Height == height)
                return;

            base.Resize(width, height);

            UpdatePosition();
            UpdateTextureAtlasOffset();
        }
    }

    public class MaskedSprite : Node, IMaskedSprite
    {
        protected int drawIndex = -1;
        Position textureAtlasOffset = null;
        Position maskTextureAtlasOffset = null;

        public MaskedSprite(int width, int height, int textureAtlasX, int textureAtlasY, Rect virtualScreen)
            : base(Shape.Rect, width, height, virtualScreen)
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

        public override void Resize(int width, int height)
        {
            if (Width == width && Height == height)
                return;

            base.Resize(width, height);

            UpdatePosition();
            UpdateTextureAtlasOffset();
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

    public class LayerSprite : Sprite, ILayerSprite
    {
        byte displayLayer = 0;

        public LayerSprite(int width, int height, int textureAtlasX, int textureAtlasY, byte displayLayer, Rect virtualScreen)
            : base(width, height, textureAtlasX, textureAtlasY, virtualScreen)
        {
            this.displayLayer = displayLayer;
        }

        public byte DisplayLayer
        {
            get => displayLayer;
            set
            {
                if (displayLayer == value)
                    return;

                displayLayer = value;

                UpdateDisplayLayer();
            }
        }

        protected virtual void UpdateDisplayLayer()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateDisplayLayer(drawIndex, displayLayer);
        }
    }

    public class MaskedLayerSprite : MaskedSprite, IMaskedLayerSprite
    {
        byte displayLayer = 0;

        public MaskedLayerSprite(int width, int height, int textureAtlasX, int textureAtlasY, byte displayLayer, Rect virtualScreen)
            : base(width, height, textureAtlasX, textureAtlasY, virtualScreen)
        {
            this.displayLayer = displayLayer;
        }

        public byte DisplayLayer
        {
            get => displayLayer;
            set
            {
                if (displayLayer == value)
                    return;

                displayLayer = value;

                UpdateDisplayLayer();
            }
        }

        protected virtual void UpdateDisplayLayer()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateDisplayLayer(drawIndex, displayLayer);
        }
    }

    public class SpriteFactory : ISpriteFactory
    {
        readonly Rect virtualScreen = null;

        public SpriteFactory(Rect virtualScreen)
        {
            this.virtualScreen = virtualScreen;
        }

        public ISprite Create(int width, int height, int textureAtlasX, int textureAtlasY, bool masked, bool layered, byte displayLayer = 0)
        {
            if (masked)
            {
                if (layered)
                    return new MaskedLayerSprite(width, height, textureAtlasX, textureAtlasY, displayLayer, virtualScreen);
                else
                    return new MaskedSprite(width, height, textureAtlasX, textureAtlasY, virtualScreen);
            }
            else
            {
                if (layered)
                    return new LayerSprite(width, height, textureAtlasX, textureAtlasY, displayLayer, virtualScreen);
                else
                    return new Sprite(width, height, textureAtlasX, textureAtlasY, virtualScreen);
            }
        }
    }
}
