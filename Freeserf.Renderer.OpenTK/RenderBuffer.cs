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

        readonly VertexArrayObject vertexArrayObject = null;
        readonly PositionBuffer positionBuffer = null;
        readonly PositionBuffer textureAtlasOffsetBuffer = null;
        readonly PositionBuffer maskTextureAtlasOffsetBuffer = null; // is null for normal sprites
        readonly BaseLineBuffer baseLineBuffer = null;

        public RenderBuffer(Shape shape, bool masked)
        {
            Shape = shape;
            Masked = masked;

            if (masked)
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

            if (shape == Shape.Triangle)
            {
                // map rendering will change the texture offsets often to change the terrain so use non-static buffers
                textureAtlasOffsetBuffer = new PositionBuffer(false);
            }
            else
            {
                // TODO: static buffer? is this the case? animations are everywhere for serfs and some buildings (but ui etc are very static)
                // most sprites won't change their appearances much so use static buffer
                textureAtlasOffsetBuffer = new PositionBuffer(true);

                // base line only for rectangular sprites
                baseLineBuffer = new BaseLineBuffer(false);

                vertexArrayObject.AddBuffer(TextureShader.DefaultBaseLineName, baseLineBuffer);
            }

            if (masked)
            {
                maskTextureAtlasOffsetBuffer = new PositionBuffer(false);

                vertexArrayObject.AddBuffer(MaskedTextureShader.DefaultMaskTexCoordName, maskTextureAtlasOffsetBuffer);
            }

            vertexArrayObject.AddBuffer(TextureShader.DefaultPositionName, positionBuffer);
            vertexArrayObject.AddBuffer(TextureShader.DefaultTexCoordName, textureAtlasOffsetBuffer);
        }

        public int GetDrawIndex(Render.ISprite sprite, Position maskSpriteTextureAtlasOffset = null)
        {
            int index = positionBuffer.Add((short)sprite.X, (short)sprite.Y);
            textureAtlasOffsetBuffer.Add((short)sprite.TextureAtlasOffset.X, (short)sprite.TextureAtlasOffset.Y);

            positionBuffer.Add((short)(sprite.X + sprite.Width), (short)sprite.Y);
            textureAtlasOffsetBuffer.Add((short)(sprite.TextureAtlasOffset.X + sprite.Width), (short)sprite.TextureAtlasOffset.Y);

            positionBuffer.Add((short)(sprite.X + sprite.Width), (short)(sprite.Y + sprite.Height));
            textureAtlasOffsetBuffer.Add((short)(sprite.TextureAtlasOffset.X + sprite.Width), (short)(sprite.TextureAtlasOffset.Y + sprite.Height));

            positionBuffer.Add((short)sprite.X, (short)(sprite.Y + sprite.Height));
            textureAtlasOffsetBuffer.Add((short)sprite.TextureAtlasOffset.X, (short)(sprite.TextureAtlasOffset.Y + sprite.Height));

            if (Masked && maskSpriteTextureAtlasOffset != null)
            {
                maskTextureAtlasOffsetBuffer.Add((short)maskSpriteTextureAtlasOffset.X, (short)maskSpriteTextureAtlasOffset.Y);
                maskTextureAtlasOffsetBuffer.Add((short)(maskSpriteTextureAtlasOffset.X + sprite.Width), (short)maskSpriteTextureAtlasOffset.Y);
                maskTextureAtlasOffsetBuffer.Add((short)(maskSpriteTextureAtlasOffset.X + sprite.Width), (short)(maskSpriteTextureAtlasOffset.Y + sprite.Height));
                maskTextureAtlasOffsetBuffer.Add((short)maskSpriteTextureAtlasOffset.X, (short)(maskSpriteTextureAtlasOffset.Y + sprite.Height));
            }

            if (Shape != Shape.Triangle && baseLineBuffer != null)
            {
                ushort baseLine = (ushort)(sprite.Y + sprite.Height);

                baseLineBuffer.Add(baseLine);
                baseLineBuffer.Add(baseLine);
                baseLineBuffer.Add(baseLine);
                baseLineBuffer.Add(baseLine);
            }

            return index;
        }

        public void UpdatePosition(int index, Render.ISprite sprite)
        {
            positionBuffer.Update(index, (short)sprite.X, (short)sprite.Y);
            positionBuffer.Update(index + 1, (short)(sprite.X + sprite.Width), (short)sprite.Y);
            positionBuffer.Update(index + 2, (short)(sprite.X + sprite.Width), (short)(sprite.Y + sprite.Height));
            positionBuffer.Update(index + 3, (short)sprite.X, (short)(sprite.Y + sprite.Height));

            if (Shape != Shape.Triangle && baseLineBuffer != null)
            {
                ushort baseLine = (ushort)(sprite.Y + sprite.Height);

                baseLineBuffer.Update(index, baseLine);
                baseLineBuffer.Update(index + 1, baseLine);
                baseLineBuffer.Update(index + 2, baseLine);
                baseLineBuffer.Update(index + 3, baseLine);
            }
        }

        public void UpdateTextureAtlasOffset(int index, Render.ISprite sprite, Position maskSpriteTextureAtlasOffset = null)
        {
            textureAtlasOffsetBuffer.Update(index, (short)sprite.TextureAtlasOffset.X, (short)sprite.TextureAtlasOffset.Y);
            textureAtlasOffsetBuffer.Update(index + 1, (short)(sprite.TextureAtlasOffset.X + sprite.Width), (short)sprite.TextureAtlasOffset.Y);
            textureAtlasOffsetBuffer.Update(index + 2, (short)(sprite.TextureAtlasOffset.X + sprite.Width), (short)(sprite.TextureAtlasOffset.Y + sprite.Height));
            textureAtlasOffsetBuffer.Update(index + 3, (short)sprite.TextureAtlasOffset.X, (short)(sprite.TextureAtlasOffset.Y + sprite.Height));

            if (Shape == Shape.Triangle && maskSpriteTextureAtlasOffset != null)
            {
                maskTextureAtlasOffsetBuffer.Update(index, (short)maskSpriteTextureAtlasOffset.X, (short)maskSpriteTextureAtlasOffset.Y);
                maskTextureAtlasOffsetBuffer.Update(index + 1, (short)(maskSpriteTextureAtlasOffset.X + sprite.Width), (short)maskSpriteTextureAtlasOffset.Y);
                maskTextureAtlasOffsetBuffer.Update(index + 2, (short)(maskSpriteTextureAtlasOffset.X + sprite.Width), (short)(maskSpriteTextureAtlasOffset.Y + sprite.Height));
                maskTextureAtlasOffsetBuffer.Update(index + 3, (short)maskSpriteTextureAtlasOffset.X, (short)(maskSpriteTextureAtlasOffset.Y + sprite.Height));
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

            positionBuffer.Remove(index);
            textureAtlasOffsetBuffer.Remove(index);

            if (maskTextureAtlasOffsetBuffer != null)
                maskTextureAtlasOffsetBuffer.Remove(index);

            if (baseLineBuffer != null)
                baseLineBuffer.Remove(index);

            if (newSize != -1)
            {
                positionBuffer.ReduceSizeTo(newSize);
                textureAtlasOffsetBuffer.ReduceSizeTo(newSize);

                if (maskTextureAtlasOffsetBuffer != null)
                    maskTextureAtlasOffsetBuffer.ReduceSizeTo(newSize);

                if (baseLineBuffer != null)
                    baseLineBuffer.ReduceSizeTo(newSize);
            }
        }

        public void Render()
        {
            vertexArrayObject.Bind();

            GL.DrawArrays(PrimitiveType.Quads, 0, positionBuffer.Size / 2);
        }
    }
}
