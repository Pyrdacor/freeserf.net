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
        public int NumVerticesPerObject { get; } = 4;

        readonly VertexArrayObject vertexArrayObject = null;
        readonly PositionBuffer positionBuffer = null;
        readonly SizeBuffer sizeBuffer = null;
        readonly PositionBuffer textureAtlasOffsetBuffer = null;

        public RenderBuffer(Shape shape)
        {
            Shape = shape;

            if (shape != Shape.Rect)
                NumVerticesPerObject = 3;

            vertexArrayObject = new VertexArrayObject(TextureShader.Instance.ShaderProgram);
            positionBuffer = new PositionBuffer(false);
            sizeBuffer = new SizeBuffer(true);
            textureAtlasOffsetBuffer = new PositionBuffer(true);

            vertexArrayObject.AddBuffer(TextureShader.DefaultPositionName, positionBuffer);
            vertexArrayObject.AddBuffer(TextureShader.DefaultSizeName, sizeBuffer);
            vertexArrayObject.AddBuffer(TextureShader.DefaultTexCoordName, textureAtlasOffsetBuffer);
        }

        public int GetDrawIndex(Sprite sprite)
        {
            int index = positionBuffer.Add((short)sprite.X, (short)sprite.Y);
            textureAtlasOffsetBuffer.Add((short)sprite.TextureAtlasOffset.X, (short)sprite.TextureAtlasOffset.Y);
            sizeBuffer.Add((ushort)sprite.Width, (ushort)sprite.Height);

            return index;
        }

        public void UpdatePosition(int index, Sprite sprite)
        {
            positionBuffer.Update(index, (short)sprite.X, (short)sprite.Y);
        }

        public void UpdateTextureAtlasOffset(int index, Sprite sprite)
        {
            textureAtlasOffsetBuffer.Update(index, (short)sprite.TextureAtlasOffset.X, (short)sprite.TextureAtlasOffset.Y);
        }

        public void FreeDrawIndex(int index)
        {
            int newSize = -1;

            if (index == (positionBuffer.Size - 2) / 2)
            {
                int i = index - 1;
                newSize = positionBuffer.Size - 2;

                while (i >= 0 && !positionBuffer.IsPositionValid(i))
                {
                    --i;
                    newSize -= 2;
                }
            }

            positionBuffer.Remove(index);
            textureAtlasOffsetBuffer.Remove(index);
            sizeBuffer.Remove(index);

            if (newSize != -1)
            {
                positionBuffer.ReduceSizeTo(newSize);
                textureAtlasOffsetBuffer.ReduceSizeTo(newSize);
                sizeBuffer.ReduceSizeTo(newSize);
            }
        }

        public void Render()
        {
            vertexArrayObject.Bind();

            GL.DrawArraysInstanced((Shape == Shape.Rect) ? PrimitiveType.Quads : PrimitiveType.Triangles, 0, positionBuffer.Size / 2, positionBuffer.Size / 2);
        }
    }
}
