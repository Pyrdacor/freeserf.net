﻿/*
 * RenderBuffer.cs - Renders several buffered objects
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

using Silk.NET.OpenGL;

namespace Freeserf.Renderer
{
    public enum Shape
    {
        Rect,
        Triangle
    }

    // Note: If we have different sprites per layer (e.g. those with masked tex coords and those without)
    // the indices will not fit inside the different buffers. We can't use different sprite types per layer!
    public class RenderBuffer
    {
        public Shape Shape { get; } = Shape.Rect;
        public bool Masked { get; } = false;
        bool supportAnimations = false;

        readonly VertexArrayObject vertexArrayObject = null;
        readonly PositionBuffer positionBuffer = null;
        readonly PositionBuffer textureAtlasOffsetBuffer = null;
        readonly PositionBuffer maskTextureAtlasOffsetBuffer = null; // is null for normal sprites
        readonly BaseLineBuffer baseLineBuffer = null;
        readonly ColorBuffer colorBuffer = null;
        readonly LayerBuffer layerBuffer = null;
        readonly IndexBuffer indexBuffer = null;

        public RenderBuffer(Shape shape, bool masked, bool supportAnimations, bool layered, bool noTexture = false)
        {
            Shape = shape;
            Masked = masked;
            this.supportAnimations = supportAnimations;

            if (noTexture)
            {
                vertexArrayObject = new VertexArrayObject(ColorShader.Instance.ShaderProgram);
            }
            else if (masked)
            {
                if (shape == Shape.Triangle)
                    vertexArrayObject = new VertexArrayObject(MaskedTriangleShader.Instance.ShaderProgram);
                else
                    vertexArrayObject = new VertexArrayObject(MaskedTextureShader.Instance.ShaderProgram);
            }
            else
            {
                vertexArrayObject = new VertexArrayObject(TextureShader.Instance.ShaderProgram);
            }

            positionBuffer = new PositionBuffer(false);
            indexBuffer = new IndexBuffer();

            if (noTexture)
            {
                colorBuffer = new ColorBuffer(true);
                layerBuffer = new LayerBuffer(true);

                vertexArrayObject.AddBuffer(ColorShader.DefaultColorName, colorBuffer);
                vertexArrayObject.AddBuffer(ColorShader.DefaultLayerName, layerBuffer);
            }
            else if (shape == Shape.Triangle)
            {
                // map rendering will change the texture offsets often to change the terrain so use non-static buffers
                textureAtlasOffsetBuffer = new PositionBuffer(false);
            }
            else
            {
                textureAtlasOffsetBuffer = new PositionBuffer(!supportAnimations);

                if (layered)
                {
                    layerBuffer = new LayerBuffer(true);

                    vertexArrayObject.AddBuffer(ColorShader.DefaultLayerName, layerBuffer);
                }
                else
                {
                    baseLineBuffer = new BaseLineBuffer(false);

                    vertexArrayObject.AddBuffer(ColorShader.DefaultLayerName, baseLineBuffer);
                }
            }

            if (masked && !noTexture)
            {
                maskTextureAtlasOffsetBuffer = new PositionBuffer(shape != Shape.Triangle && !supportAnimations);

                vertexArrayObject.AddBuffer(MaskedTextureShader.DefaultMaskTexCoordName, maskTextureAtlasOffsetBuffer);
            }

            vertexArrayObject.AddBuffer(ColorShader.DefaultPositionName, positionBuffer);
            vertexArrayObject.AddBuffer("index", indexBuffer);

            if (!noTexture)
                vertexArrayObject.AddBuffer(TextureShader.DefaultTexCoordName, textureAtlasOffsetBuffer);
        }

        public int GetDrawIndex(Render.IColoredRect coloredRect, 
            Render.PositionTransformation positionTransformation, 
            Render.SizeTransformation sizeTransformation)
        {
            var position = new Position(coloredRect.X, coloredRect.Y);
            var size = new Size(coloredRect.Width, coloredRect.Height);

            if (positionTransformation != null)
                position = positionTransformation(position);

            if (sizeTransformation != null)
                size = sizeTransformation(size);

            int index = positionBuffer.Add((short)position.X, (short)position.Y);
            positionBuffer.Add((short)(position.X + size.Width), (short)position.Y, index + 1);
            positionBuffer.Add((short)(position.X + size.Width), (short)(position.Y + size.Height), index + 2);
            positionBuffer.Add((short)position.X, (short)(position.Y + size.Height), index + 3);

            indexBuffer.InsertQuad(index / 4);

            if (layerBuffer != null)
            {
                int layerBufferIndex = layerBuffer.Add(coloredRect.DisplayLayer);

                if (layerBufferIndex != index)
                    throw new System.Exception("Invalid index");

                layerBuffer.Add(coloredRect.DisplayLayer, layerBufferIndex + 1);
                layerBuffer.Add(coloredRect.DisplayLayer, layerBufferIndex + 2);
                layerBuffer.Add(coloredRect.DisplayLayer, layerBufferIndex + 3);
            }

            if (colorBuffer != null)
            {
                var color = coloredRect.Color;

                int colorBufferIndex = colorBuffer.Add(color);

                if (colorBufferIndex != index)
                    throw new System.Exception("Invalid index");

                colorBuffer.Add(color, colorBufferIndex + 1);
                colorBuffer.Add(color, colorBufferIndex + 2);
                colorBuffer.Add(color, colorBufferIndex + 3);
            }

            return index;
        }

        public int GetDrawIndex(Render.ISprite sprite, Render.PositionTransformation positionTransformation,
            Render.SizeTransformation sizeTransformation, Position maskSpriteTextureAtlasOffset = null)
        {
            var position = new Position(sprite.X, sprite.Y);
            var size = new Size(sprite.Width, sprite.Height);

            if (positionTransformation != null)
                position = positionTransformation(position);

            if (sizeTransformation != null)
                size = sizeTransformation(size);

            int index = positionBuffer.Add((short)position.X, (short)position.Y);
            positionBuffer.Add((short)(position.X + size.Width), (short)position.Y, index + 1);
            positionBuffer.Add((short)(position.X + size.Width), (short)(position.Y + size.Height), index + 2);
            positionBuffer.Add((short)position.X, (short)(position.Y + size.Height), index + 3);

            indexBuffer.InsertQuad(index / 4);

            if (textureAtlasOffsetBuffer != null)
            {
                int textureAtlasOffsetBufferIndex = textureAtlasOffsetBuffer.Add((short)sprite.TextureAtlasOffset.X, (short)sprite.TextureAtlasOffset.Y);

                if (textureAtlasOffsetBufferIndex != index)
                    throw new System.Exception("Invalid index");

                textureAtlasOffsetBuffer.Add((short)(sprite.TextureAtlasOffset.X + sprite.Width), (short)sprite.TextureAtlasOffset.Y, textureAtlasOffsetBufferIndex + 1);
                textureAtlasOffsetBuffer.Add((short)(sprite.TextureAtlasOffset.X + sprite.Width), (short)(sprite.TextureAtlasOffset.Y + sprite.Height), textureAtlasOffsetBufferIndex + 2);
                textureAtlasOffsetBuffer.Add((short)sprite.TextureAtlasOffset.X, (short)(sprite.TextureAtlasOffset.Y + sprite.Height), textureAtlasOffsetBufferIndex + 3);
            }

            if (Masked && maskSpriteTextureAtlasOffset != null)
            {
                int maskTextureAtlasOffsetBufferIndex = maskTextureAtlasOffsetBuffer.Add((short)maskSpriteTextureAtlasOffset.X, (short)maskSpriteTextureAtlasOffset.Y);

                if (maskTextureAtlasOffsetBufferIndex != index)
                    throw new System.Exception("Invalid index");

                maskTextureAtlasOffsetBuffer.Add((short)(maskSpriteTextureAtlasOffset.X + sprite.Width), (short)maskSpriteTextureAtlasOffset.Y, maskTextureAtlasOffsetBufferIndex + 1);
                maskTextureAtlasOffsetBuffer.Add((short)(maskSpriteTextureAtlasOffset.X + sprite.Width), (short)(maskSpriteTextureAtlasOffset.Y + sprite.Height), maskTextureAtlasOffsetBufferIndex + 2);
                maskTextureAtlasOffsetBuffer.Add((short)maskSpriteTextureAtlasOffset.X, (short)(maskSpriteTextureAtlasOffset.Y + sprite.Height), maskTextureAtlasOffsetBufferIndex + 3);
            }

            if (Shape != Shape.Triangle && baseLineBuffer != null)
            {
                ushort baseLine = (ushort)(position.Y + size.Height + sprite.BaseLineOffset);

                int baseLineBufferIndex = baseLineBuffer.Add(baseLine);

                if (baseLineBufferIndex != index)
                    throw new System.Exception("Invalid index");

                baseLineBuffer.Add(baseLine, baseLineBufferIndex + 1);
                baseLineBuffer.Add(baseLine, baseLineBufferIndex + 2);
                baseLineBuffer.Add(baseLine, baseLineBufferIndex + 3);
            }

            if (layerBuffer != null)
            {
                byte layer = (sprite is Render.ILayerSprite) ? (sprite as Render.ILayerSprite).DisplayLayer : (byte)0;

                int layerBufferIndex = layerBuffer.Add(layer);

                if (layerBufferIndex != index)
                    throw new System.Exception("Invalid index");

                layerBuffer.Add(layer, layerBufferIndex + 1);
                layerBuffer.Add(layer, layerBufferIndex + 2);
                layerBuffer.Add(layer, layerBufferIndex + 3);
            }

            return index;
        }

        public void UpdatePosition(int index, Render.IRenderNode renderNode, int baseLineOffset,
            Render.PositionTransformation positionTransformation, Render.SizeTransformation sizeTransformation)
        {
            var position = new Position(renderNode.X, renderNode.Y);
            var size = new Size(renderNode.Width, renderNode.Height);

            if (positionTransformation != null)
                position = positionTransformation(position);

            if (sizeTransformation != null)
                size = sizeTransformation(size);

            positionBuffer.Update(index, (short)position.X, (short)position.Y);
            positionBuffer.Update(index + 1, (short)(position.X + size.Width), (short)position.Y);
            positionBuffer.Update(index + 2, (short)(position.X + size.Width), (short)(position.Y + size.Height));
            positionBuffer.Update(index + 3, (short)position.X, (short)(position.Y + size.Height));

            if (Shape != Shape.Triangle && baseLineBuffer != null)
            {
                ushort baseLine = (ushort)(position.Y + size.Height + baseLineOffset);

                baseLineBuffer.Update(index, baseLine);
                baseLineBuffer.Update(index + 1, baseLine);
                baseLineBuffer.Update(index + 2, baseLine);
                baseLineBuffer.Update(index + 3, baseLine);
            }
        }

        public void UpdateTextureAtlasOffset(int index, Render.ISprite sprite, Position maskSpriteTextureAtlasOffset = null)
        {
            if (textureAtlasOffsetBuffer == null)
                return;

            textureAtlasOffsetBuffer.Update(index, (short)sprite.TextureAtlasOffset.X, (short)sprite.TextureAtlasOffset.Y);
            textureAtlasOffsetBuffer.Update(index + 1, (short)(sprite.TextureAtlasOffset.X + sprite.Width), (short)sprite.TextureAtlasOffset.Y);
            textureAtlasOffsetBuffer.Update(index + 2, (short)(sprite.TextureAtlasOffset.X + sprite.Width), (short)(sprite.TextureAtlasOffset.Y + sprite.Height));
            textureAtlasOffsetBuffer.Update(index + 3, (short)sprite.TextureAtlasOffset.X, (short)(sprite.TextureAtlasOffset.Y + sprite.Height));

            if (Masked && maskSpriteTextureAtlasOffset != null)
            {
                maskTextureAtlasOffsetBuffer.Update(index, (short)maskSpriteTextureAtlasOffset.X, (short)maskSpriteTextureAtlasOffset.Y);
                maskTextureAtlasOffsetBuffer.Update(index + 1, (short)(maskSpriteTextureAtlasOffset.X + sprite.Width), (short)maskSpriteTextureAtlasOffset.Y);
                maskTextureAtlasOffsetBuffer.Update(index + 2, (short)(maskSpriteTextureAtlasOffset.X + sprite.Width), (short)(maskSpriteTextureAtlasOffset.Y + sprite.Height));
                maskTextureAtlasOffsetBuffer.Update(index + 3, (short)maskSpriteTextureAtlasOffset.X, (short)(maskSpriteTextureAtlasOffset.Y + sprite.Height));
            }
        }

        public void UpdateColor(int index, Render.Color color)
        {
            if (colorBuffer != null)
            {
                colorBuffer.Update(index, color);
                colorBuffer.Update(index + 1, color);
                colorBuffer.Update(index + 2, color);
                colorBuffer.Update(index + 3, color);
            }
        }

        public void UpdateDisplayLayer(int index, byte displayLayer)
        {
            if (layerBuffer != null)
            {
                layerBuffer.Update(index, displayLayer);
                layerBuffer.Update(index + 1, displayLayer);
                layerBuffer.Update(index + 2, displayLayer);
                layerBuffer.Update(index + 3, displayLayer);
            }
        }

        public void FreeDrawIndex(int index)
        {
            /*int newSize = -1;

            if (index == (positionBuffer.Size - 8) / 8)
            {
                int i = (index - 1) * 4;
                newSize = positionBuffer.Size - 8;

                while (i >= 0 && !positionBuffer.IsPositionValid(i))
                {
                    i -= 4;
                    newSize -= 8;
                }
            }*/

            for (int i = 0; i < 4; ++i)
            {
                positionBuffer.Update(index + i, short.MaxValue, short.MaxValue); // ensure it is not visible
                positionBuffer.Remove(index + i);
            }

            if (textureAtlasOffsetBuffer != null)
            {
                textureAtlasOffsetBuffer.Remove(index);
                textureAtlasOffsetBuffer.Remove(index + 1);
                textureAtlasOffsetBuffer.Remove(index + 2);
                textureAtlasOffsetBuffer.Remove(index + 3);
            }

            if (maskTextureAtlasOffsetBuffer != null)
            {
                maskTextureAtlasOffsetBuffer.Remove(index);
                maskTextureAtlasOffsetBuffer.Remove(index + 1);
                maskTextureAtlasOffsetBuffer.Remove(index + 2);
                maskTextureAtlasOffsetBuffer.Remove(index + 3);
            }

            if (baseLineBuffer != null)
            {
                baseLineBuffer.Remove(index);
                baseLineBuffer.Remove(index + 1);
                baseLineBuffer.Remove(index + 2);
                baseLineBuffer.Remove(index + 3);
            }

            if (colorBuffer != null)
            {
                colorBuffer.Remove(index);
                colorBuffer.Remove(index + 1);
                colorBuffer.Remove(index + 2);
                colorBuffer.Remove(index + 3);
            }

            if (layerBuffer != null)
            {
                layerBuffer.Remove(index);
                layerBuffer.Remove(index + 1);
                layerBuffer.Remove(index + 2);
                layerBuffer.Remove(index + 3);
            }

            // TODO: this code causes problems. commented out for now
            /*if (newSize != -1)
            {
                positionBuffer.ReduceSizeTo(newSize);

                if (textureAtlasOffsetBuffer != null)
                    textureAtlasOffsetBuffer.ReduceSizeTo(newSize);

                if (maskTextureAtlasOffsetBuffer != null)
                    maskTextureAtlasOffsetBuffer.ReduceSizeTo(newSize);

                if (baseLineBuffer != null)
                    baseLineBuffer.ReduceSizeTo(newSize / 2);

                if (colorBuffer != null)
                    colorBuffer.ReduceSizeTo(newSize * 2);

                if (layerBuffer != null)
                    layerBuffer.ReduceSizeTo(newSize / 2);
            }*/
        }

        public void Render()
        {
            vertexArrayObject.Bind();

            unsafe
            {
                vertexArrayObject.Lock();

                try
                {
                    State.Gl.DrawElements(PrimitiveType.Triangles, (uint)positionBuffer.Size / 4 * 3, DrawElementsType.UnsignedInt, (void*)0);
                }
                catch
                {
                    // ignore for now
                }
                finally
                {
                    vertexArrayObject.Unlock();
                }
            }
        }
    }
}
