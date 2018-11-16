/*
 * RenderBuffer.cs - Renders several buffered objects
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

using OpenTK.Graphics.OpenGL;

namespace Freeserf.Renderer.OpenTK
{
    public enum Shape
    {
        Rect,
        Triangle
    }

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

        public RenderBuffer(Shape shape, bool masked, bool supportAnimations, bool noTexture = false)
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

            if (noTexture)
            {
                colorBuffer = new ColorBuffer(true);
                baseLineBuffer = new BaseLineBuffer(false);

                vertexArrayObject.AddBuffer(ColorShader.DefaultColorName, colorBuffer);
                vertexArrayObject.AddBuffer(TextureShader.DefaultBaseLineName, baseLineBuffer);
            }
            else if (shape == Shape.Triangle)
            {
                // map rendering will change the texture offsets often to change the terrain so use non-static buffers
                textureAtlasOffsetBuffer = new PositionBuffer(false);
            }
            else
            {
                textureAtlasOffsetBuffer = new PositionBuffer(!supportAnimations);

                // base line only for rectangular sprites
                baseLineBuffer = new BaseLineBuffer(false);

                vertexArrayObject.AddBuffer(TextureShader.DefaultBaseLineName, baseLineBuffer);
            }

            if (masked && !noTexture)
            {
                maskTextureAtlasOffsetBuffer = new PositionBuffer(shape != Shape.Triangle && !supportAnimations);

                vertexArrayObject.AddBuffer(MaskedTextureShader.DefaultMaskTexCoordName, maskTextureAtlasOffsetBuffer);
            }

            vertexArrayObject.AddBuffer(TextureShader.DefaultPositionName, positionBuffer);

            if (!noTexture)
                vertexArrayObject.AddBuffer(TextureShader.DefaultTexCoordName, textureAtlasOffsetBuffer);
        }

        public int GetDrawIndex(Render.IColoredRect coloredRect)
        {
            var scaledRect = (coloredRect as ColoredRect).ScaledRect;

            int index = positionBuffer.Add((short)scaledRect.Position.X, (short)scaledRect.Position.Y);
            positionBuffer.Add((short)(scaledRect.Position.X + scaledRect.Size.Width), (short)scaledRect.Position.Y);
            positionBuffer.Add((short)(scaledRect.Position.X + scaledRect.Size.Width), (short)(scaledRect.Position.Y + scaledRect.Size.Height));
            positionBuffer.Add((short)scaledRect.Position.X, (short)(scaledRect.Position.Y + scaledRect.Size.Height));

            if (Shape != Shape.Triangle && baseLineBuffer != null)
            {
                ushort baseLine = (ushort)(scaledRect.Position.Y + scaledRect.Size.Height);

                baseLineBuffer.Add(baseLine);
                baseLineBuffer.Add(baseLine);
                baseLineBuffer.Add(baseLine);
                baseLineBuffer.Add(baseLine);
            }

            if (colorBuffer != null)
            {
                // initialize with black
                var color = coloredRect.Color;

                colorBuffer.Add(color);
                colorBuffer.Add(color);
                colorBuffer.Add(color);
                colorBuffer.Add(color);
            }

            return index;
        }

        public int GetDrawIndex(Render.ISprite sprite, Position maskSpriteTextureAtlasOffset = null)
        {
            var scaledRect = (sprite as Sprite).ScaledRect;

            int index = positionBuffer.Add((short)scaledRect.Position.X, (short)scaledRect.Position.Y);
            positionBuffer.Add((short)(scaledRect.Position.X + scaledRect.Size.Width), (short)scaledRect.Position.Y);
            positionBuffer.Add((short)(scaledRect.Position.X + scaledRect.Size.Width), (short)(scaledRect.Position.Y + scaledRect.Size.Height));
            positionBuffer.Add((short)scaledRect.Position.X, (short)(scaledRect.Position.Y + scaledRect.Size.Height));

            if (textureAtlasOffsetBuffer != null)
            {
                textureAtlasOffsetBuffer.Add((short)sprite.TextureAtlasOffset.X, (short)sprite.TextureAtlasOffset.Y);
                textureAtlasOffsetBuffer.Add((short)(sprite.TextureAtlasOffset.X + sprite.Width), (short)sprite.TextureAtlasOffset.Y);
                textureAtlasOffsetBuffer.Add((short)(sprite.TextureAtlasOffset.X + sprite.Width), (short)(sprite.TextureAtlasOffset.Y + sprite.Height));
                textureAtlasOffsetBuffer.Add((short)sprite.TextureAtlasOffset.X, (short)(sprite.TextureAtlasOffset.Y + sprite.Height));
            }

            if (Masked && maskSpriteTextureAtlasOffset != null)
            {
                maskTextureAtlasOffsetBuffer.Add((short)maskSpriteTextureAtlasOffset.X, (short)maskSpriteTextureAtlasOffset.Y);
                maskTextureAtlasOffsetBuffer.Add((short)(maskSpriteTextureAtlasOffset.X + sprite.Width), (short)maskSpriteTextureAtlasOffset.Y);
                maskTextureAtlasOffsetBuffer.Add((short)(maskSpriteTextureAtlasOffset.X + sprite.Width), (short)(maskSpriteTextureAtlasOffset.Y + sprite.Height));
                maskTextureAtlasOffsetBuffer.Add((short)maskSpriteTextureAtlasOffset.X, (short)(maskSpriteTextureAtlasOffset.Y + sprite.Height));
            }

            if (Shape != Shape.Triangle && baseLineBuffer != null)
            {
                ushort baseLine = (ushort)(scaledRect.Position.Y + scaledRect.Size.Height);

                baseLineBuffer.Add(baseLine);
                baseLineBuffer.Add(baseLine);
                baseLineBuffer.Add(baseLine);
                baseLineBuffer.Add(baseLine);
            }

            return index;
        }

        public void UpdatePosition(int index, Render.IRenderNode renderNode)
        {
            var scaledRect = (renderNode as Node).ScaledRect;

            positionBuffer.Update(index, (short)scaledRect.Position.X, (short)scaledRect.Position.Y);
            positionBuffer.Update(index + 1, (short)(scaledRect.Position.X + scaledRect.Size.Width), (short)scaledRect.Position.Y);
            positionBuffer.Update(index + 2, (short)(scaledRect.Position.X + scaledRect.Size.Width), (short)(scaledRect.Position.Y + scaledRect.Size.Height));
            positionBuffer.Update(index + 3, (short)scaledRect.Position.X, (short)(scaledRect.Position.Y + scaledRect.Size.Height));

            if (Shape != Shape.Triangle && baseLineBuffer != null)
            {
                ushort baseLine = (ushort)(scaledRect.Position.Y + scaledRect.Size.Height);

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

        public void FreeDrawIndex(int index)
        {
            int newSize = -1;

            if (index == (positionBuffer.Size - 8) / 8)
            {
                int i = index - 1;
                newSize = positionBuffer.Size - 8;

                while (i >= 0 && !positionBuffer.IsPositionValid(i))
                {
                    --i;
                    newSize -= 8;
                }
            }

            for (int i = 0; i < 4; ++i)
            {
                positionBuffer.Update(index + i, short.MaxValue, short.MaxValue); // ensure it is not visible
            }

            positionBuffer.Remove(index);

            if (textureAtlasOffsetBuffer != null)
                textureAtlasOffsetBuffer.Remove(index);

            if (maskTextureAtlasOffsetBuffer != null)
                maskTextureAtlasOffsetBuffer.Remove(index);

            if (baseLineBuffer != null)
                baseLineBuffer.Remove(index);

            if (colorBuffer != null)
                colorBuffer.Remove(index);

            if (newSize != -1)
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
            }
        }

        public void Render()
        {
            vertexArrayObject.Bind();

            GL.DrawArrays(PrimitiveType.Quads, 0, positionBuffer.Size / 2);
        }
    }
}
