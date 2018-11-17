/*
 * RenderLayer.cs - Render layer implementation
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

using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using Freeserf.Render;

namespace Freeserf.Renderer.OpenTK
{
    public class RenderLayer : IRenderLayer
    {
        public Layer Layer { get; } = Layer.None;

        public Color ColorKey
        {
            get;
            set;
        } = null;

        public bool Visible
        {
            get;
            set;
        }

        public PositionTransformation PositionTransformation
        {
            get;
            set;
        } = null;

        public SizeTransformation SizeTransformation
        {
            get;
            set;
        } = null;

        readonly RenderBuffer renderBuffer = null;
        readonly RenderBuffer renderBufferColorRects = null;
        readonly Dictionary<Size, List<IRenderNode>> nodes = new Dictionary<Size, List<IRenderNode>>();
        readonly Texture texture = null;
        readonly int layerIndex = 0;

        public RenderLayer(Layer layer, Texture texture, bool supportColoredRects = false, Color colorKey = null)
        {
            var shape = (layer == Layer.Landscape || layer == Layer.Grid) ? Shape.Triangle : Shape.Rect;
            bool masked = layer == Layer.Landscape || layer == Layer.Buildings || layer == Layer.Paths; // we need the mask for slope display and drawing of building progress
            bool supportAnimations = layer != Layer.Gui; // gui is mostly static

            renderBuffer = new RenderBuffer(shape, masked, supportAnimations);

            if (supportColoredRects)
                renderBufferColorRects = new RenderBuffer(Shape.Rect, false, supportAnimations, true);

            Layer = layer;
            this.texture = texture;
            ColorKey = colorKey;
            layerIndex = Misc.Round(Math.Log((int)layer, 2.0));
        }

        public void Render()
        {
            if (!Visible || texture == null)
                return;
            if (renderBufferColorRects != null)
            {
                var colorShader = ColorShader.Instance;

                colorShader.UpdateMatrices();
                colorShader.SetZ(Global.LayerBaseZ[layerIndex]);

                renderBufferColorRects.Render();
            }

            TextureShader shader;

            if (renderBuffer.Masked)
            {
                if (renderBuffer.Shape == Shape.Triangle)
                    shader = MaskedTriangleShader.Instance;
                else
                    shader = MaskedTextureShader.Instance;
            }
            else
            {
                shader = TextureShader.Instance;
            }

            shader.UpdateMatrices(); // TODO: maybe do this in game view

            shader.SetSampler(0); // we use texture unit 0 -> see GL.ActiveTexture below
            GL.ActiveTexture(TextureUnit.Texture0);
            (texture as Texture).Bind();

            shader.SetAtlasSize((uint)texture.Width, (uint)texture.Height);

            shader.SetZ(Global.LayerBaseZ[layerIndex]);

            if (ColorKey == null)
                shader.SetColorKey(1.0f, 0.0f, 1.0f);
            else
                shader.SetColorKey(ColorKey.R, ColorKey.G, ColorKey.B);

            renderBuffer.Render();
        }

        public int GetDrawIndex(ISprite sprite, Position maskSpriteTextureAtlasOffset = null)
        {
            return renderBuffer.GetDrawIndex(sprite, PositionTransformation, SizeTransformation, maskSpriteTextureAtlasOffset);
        }

        public void FreeDrawIndex(int index)
        {
            renderBuffer.FreeDrawIndex(index);
        }

        public void UpdatePosition(int index, ISprite sprite)
        {
            renderBuffer.UpdatePosition(index, sprite, PositionTransformation, SizeTransformation);
        }

        public void UpdateTextureAtlasOffset(int index, ISprite sprite, Position maskSpriteTextureAtlasOffset = null)
        {
            renderBuffer.UpdateTextureAtlasOffset(index, sprite, maskSpriteTextureAtlasOffset);
        }

        public int GetColoredRectDrawIndex(ColoredRect coloredRect)
        {
            return renderBufferColorRects.GetDrawIndex(coloredRect, PositionTransformation, SizeTransformation);
        }

        public void FreeColoredRectDrawIndex(int index)
        {
            renderBufferColorRects.FreeDrawIndex(index);
        }

        public void UpdateColoredRectPosition(int index, ColoredRect coloredRect)
        {
            renderBufferColorRects.UpdatePosition(index, coloredRect, PositionTransformation, SizeTransformation);
        }

        public void UpdateColoredRectColor(int index, Color color)
        {
            renderBufferColorRects.UpdateColor(index, color);
        }

        public void TestNode(IRenderNode node)
        {
            if (!(node is Node))
                throw new InvalidCastException("The given render node is not valid for this renderer.");

            if ((node as Node).Shape != renderBuffer.Shape)
                throw new InvalidOperationException($"Only nodes with shape {Enum.GetName(typeof(Shape), renderBuffer.Shape)} are allowed for this layer.");

            if (node is ColoredRect && renderBufferColorRects == null)
                throw new ExceptionFreeserf("This layer does not support colored rects.");
        }
    }

    public class RenderLayerFactory : IRenderLayerFactory
    {
        public IRenderLayer Create(Layer layer, Render.Texture texture, bool supportColoredRects = false, Color colorKey = null)
        {
            if (!(texture is Texture))
                throw new InvalidCastException("The given texture is not valid for this renderer.");

            switch (layer)
            {
                case Layer.All:
                case Layer.None:
                    throw new InvalidOperationException($"Cannot create render layer for layer {Enum.GetName(typeof(Layer), layer)}");
                default:
                    return new RenderLayer(layer, texture as Texture, supportColoredRects, colorKey);
            }
        }
    }
}
