using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace Freeserf.Renderer.OpenTK
{
    public enum Shape
    {
        Rect,
        TriangleUp,
        TriangleDown
    }

    public class StaticRenderBuffer
    {
        public Shape Shape { get; } = Shape.Rect;
        public int NumVerticesPerObject { get; } = 4;

        readonly VertexArrayObject vertexArrayObject = null;
        readonly PositionBuffer positionBuffer = null;
        readonly SizeBuffer sizeBuffer = null;
        readonly PositionBuffer textureAtlasOffsetBuffer = null;

        public StaticRenderBuffer(Shape shape)
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
