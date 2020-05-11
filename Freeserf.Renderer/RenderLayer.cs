/*
 * RenderLayer.cs - Render layer implementation
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
using Silk.NET.OpenGL;
using System;

namespace Freeserf.Renderer
{
    public class RenderLayer : IRenderLayer, IDisposable
    {
        public Layer Layer { get; } = Layer.None;

        public Render.Color ColorKey
        {
            get;
            set;
        } = null;

        public Render.Color ColorOverlay
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
        readonly Texture texture = null;
        readonly int layerIndex = 0;
        bool disposed = false;

        public RenderLayer(Layer layer, Texture texture, bool supportColoredRects = false, Render.Color colorKey = null, Render.Color colorOverlay = null)
        {
            var shape = (layer == Layer.Landscape || layer == Layer.Waves) ? Shape.Triangle : Shape.Rect;
            bool masked = layer == Layer.Landscape || layer == Layer.Waves || layer == Layer.Buildings || layer == Layer.Paths; // we need the mask for slope display and drawing of building progress
            bool supportAnimations = layer != Layer.Gui && layer != Layer.GuiBuildings && layer != Layer.Cursor && layer != Layer.GuiFont; // gui is mostly static

            renderBuffer = new RenderBuffer(shape, masked, supportAnimations, layer == Layer.Gui || layer == Layer.GuiBuildings || layer == Layer.GuiFont);

            if (supportColoredRects)
                renderBufferColorRects = new RenderBuffer(Shape.Rect, false, supportAnimations, true, true);

            Layer = layer;
            this.texture = texture;
            ColorKey = colorKey;
            ColorOverlay = colorOverlay;
            layerIndex = Misc.Round(Math.Log((int)layer, 2.0));
        }

        bool SupportZoom =>
            Layer != Layer.Gui &&
            Layer != Layer.GuiBuildings &&
            Layer != Layer.GuiFont &&
            Layer != Layer.Minimap &&
            Layer != Layer.Cursor;

        public void Render()
        {
            if (!Visible || texture == null)
                return;

            if (renderBufferColorRects != null)
            {
                var colorShader = ColorShader.Instance;

                colorShader.UpdateMatrices(SupportZoom);
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

            shader.UpdateMatrices(SupportZoom);

            shader.SetSampler(0); // we use texture unit 0 -> see Gl.ActiveTexture below
            State.Gl.ActiveTexture(GLEnum.Texture0);
            (texture as Texture).Bind();

            shader.SetAtlasSize((uint)texture.Width, (uint)texture.Height);

            shader.SetZ(Global.LayerBaseZ[layerIndex]);

            if (ColorKey == null)
                shader.SetColorKey(1.0f, 0.0f, 1.0f);
            else
                shader.SetColorKey(ColorKey.R / 255.0f, ColorKey.G / 255.0f, ColorKey.B / 255.0f);

            if (ColorOverlay == null)
                shader.SetColorOverlay(1.0f, 1.0f, 1.0f, 1.0f);
            else
                shader.SetColorOverlay(ColorOverlay.R / 255.0f, ColorOverlay.G / 255.0f, ColorOverlay.B / 255.0f, ColorOverlay.A / 255.0f);

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
            renderBuffer.UpdatePosition(index, sprite, sprite.BaseLineOffset, PositionTransformation, SizeTransformation);
        }

        public void UpdateTextureAtlasOffset(int index, ISprite sprite, Position maskSpriteTextureAtlasOffset = null)
        {
            renderBuffer.UpdateTextureAtlasOffset(index, sprite, maskSpriteTextureAtlasOffset);
        }

        public void UpdateDisplayLayer(int index, byte displayLayer)
        {
            renderBuffer.UpdateDisplayLayer(index, displayLayer);
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
            renderBufferColorRects.UpdatePosition(index, coloredRect, 0, PositionTransformation, SizeTransformation);
        }

        public void UpdateColoredRectColor(int index, Render.Color color)
        {
            renderBufferColorRects.UpdateColor(index, color);
        }

        public void UpdateColoredRectDisplayLayer(int index, byte displayLayer)
        {
            renderBufferColorRects.UpdateDisplayLayer(index, displayLayer);
        }

        public void TestNode(IRenderNode node)
        {
            if (!(node is Node))
                throw new ExceptionFreeserf(ErrorSystemType.Render, "The given render node is not valid for this renderer.");

            if ((node as Node).Shape != renderBuffer.Shape)
                throw new ExceptionFreeserf(ErrorSystemType.Render, $"Only nodes with shape {Enum.GetName(typeof(Shape), renderBuffer.Shape)} are allowed for this layer.");

            if (node is ColoredRect && renderBufferColorRects == null)
                throw new ExceptionFreeserf(ErrorSystemType.Render, "This layer does not support colored rects.");
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    renderBuffer?.Dispose();
                    renderBufferColorRects?.Dispose();
                    texture?.Dispose();
                    Visible = false;

                    disposed = true;
                }
            }
        }
    }

    public class RenderLayerFactory : IRenderLayerFactory
    {
        public IRenderLayer Create(Layer layer, Render.Texture texture, bool supportColoredRects = false, Render.Color colorKey = null, Render.Color colorOverlay = null)
        {
            if (!(texture is Texture))
                throw new ExceptionFreeserf(ErrorSystemType.Render, "The given texture is not valid for this renderer.");

            switch (layer)
            {
                case Layer.None:
                    throw new ExceptionFreeserf(ErrorSystemType.Render, $"Cannot create render layer for layer {Enum.GetName(typeof(Layer), layer)}");
                default:
                    return new RenderLayer(layer, texture as Texture, supportColoredRects, colorKey, colorOverlay);
            }
        }
    }
}
