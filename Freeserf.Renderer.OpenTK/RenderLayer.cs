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
    // TODO: scaling
    // TODO: set tex coords for shader
    public class RenderLayer : IRenderLayer
    {
        public Layer Layer { get; } = Layer.None;

        public Color ColorKey
        {
            get;
            set;
        } = null;

        readonly RenderBuffer renderBuffer = null;
        readonly Dictionary<Size, List<IRenderNode>> nodes = new Dictionary<Size, List<IRenderNode>>();
        readonly Texture texture = null;

        public RenderLayer(Layer layer, Shape shape, Texture texture, Color colorKey = null)
        {
            renderBuffer = new RenderBuffer(shape);

            Layer = layer;
            this.texture = texture;
            ColorKey = colorKey;
        }

        public void Render()
        {
            if (texture == null)
                return;

            var shader = TextureShader.Instance;

            shader.UpdateMatrices(); // TODO: maybe do this in game view

            shader.SetSampler(0); // we use texture unit 0 -> see GL.ActiveTexture below
            GL.ActiveTexture(TextureUnit.Texture0);
            (texture as Texture).Bind();

            shader.SetAtlasSize((uint)texture.Width, (uint)texture.Height);

            shader.SetZ(0.0f); // TODO: set dependent of layer?

            if (ColorKey == null)
                shader.SetColorKey(1.0f, 0.0f, 1.0f);
            else
                shader.SetColorKey(ColorKey.R, ColorKey.G, ColorKey.B);

            renderBuffer.Render();
         }

        public int GetDrawIndex(Sprite sprite)
        {
            return renderBuffer.GetDrawIndex(sprite);
        }

        public void FreeDrawIndex(int index)
        {
            renderBuffer.FreeDrawIndex(index);
        }

        public void UpdatePosition(int index, Sprite sprite)
        {
            renderBuffer.UpdatePosition(index, sprite);
        }

        public void AddNode(IRenderNode node)
        {
            if (!(node is Node))
                throw new InvalidCastException("The given render node is not valid for this renderer.");

            if ((node as Node).Shape != renderBuffer.Shape)
                throw new InvalidOperationException($"Only nodes with shape {Enum.GetName(typeof(Shape), renderBuffer.Shape)} are allowed for this layer.");

            var size = new Size(node.Width, node.Height);

            if (!nodes.ContainsKey(size))
                nodes[size] = new List<IRenderNode>() { node };
            else if (!nodes[size].Contains(node))
            {
                nodes[size].Add(node);
            }
        }

        public void RemoveNode(IRenderNode node)
        {
            var size = new Size(node.Width, node.Height);

            if (nodes.ContainsKey(size) && nodes[size].Contains(node))
            {
                nodes[size].Remove(node);
            }
        }
    }

    public class MapRenderLayer : IRenderLayer
    {
        readonly RenderLayer triangleUpLayer = null;
        readonly RenderLayer triangleDownLayer = null;

        // TODO
        public Color ColorKey
        {
            get;
            set;
        } = null;

        public MapRenderLayer(Layer layer, Texture texture, Color colorKey = null)
        {
            triangleUpLayer = new RenderLayer(layer, Shape.TriangleUp, texture, colorKey);
            triangleDownLayer = new RenderLayer(layer, Shape.TriangleDown, texture, colorKey);

            Layer = layer;
        }

        public Layer Layer { get; }

        public void AddNode(IRenderNode node)
        {
            if (!(node is Triangle))
                throw new InvalidCastException("The given render node is not valid for this renderer.");

            var triangle = node as Triangle;

            if (triangle.Up)
                triangleUpLayer.AddNode(triangle);
            else
                triangleDownLayer.AddNode(triangle);
        }

        public void RemoveNode(IRenderNode node)
        {
            if (!(node is Triangle))
                throw new InvalidCastException("The given render node is not valid for this renderer.");

            var triangle = node as Triangle;

            if (triangle.Up)
                triangleUpLayer.RemoveNode(triangle);
            else
                triangleDownLayer.RemoveNode(triangle);
        }

        public void Render()
        {
            triangleUpLayer.Render();
            triangleDownLayer.Render();
        }
    }

    public class RenderLayerFactory : IRenderLayerFactory
    {
        public IRenderLayer Create(Layer layer, Render.Texture texture, Color colorKey = null)
        {
            if (!(texture is Texture))
                throw new InvalidCastException("The given texture is not valid for this renderer.");

            switch (layer)
            {
                case Layer.Landscape:
                    return new MapRenderLayer(layer, texture as Texture, colorKey);
                case Layer.All:
                case Layer.None:
                    throw new InvalidOperationException($"Cannot create render layer for layer {Enum.GetName(typeof(Layer), layer)}");
                default:
                    return new RenderLayer(layer, Shape.Rect, texture as Texture, colorKey);
            }
        }
    }
}
