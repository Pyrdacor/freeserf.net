using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace Freeserf.Renderer.OpenTK
{
    internal abstract class BufferObject<T> : IDisposable
    {
        public abstract int Dimension { get; }
        public bool Normalized { get; protected set; } = false;
        public abstract int Size { get; }
        public bool PerVertex { get; }
        public abstract VertexAttribIntegerType Type { get; }

        public BufferObject(bool perVertex = true)
        {
            PerVertex = perVertex;
        }

        public abstract void Dispose();

        public abstract void Bind();

        internal abstract bool RecreateUnbound();
    }
}
