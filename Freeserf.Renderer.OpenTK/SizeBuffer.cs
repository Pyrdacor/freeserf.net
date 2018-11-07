using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace Freeserf.Renderer.OpenTK
{
    internal class SizeBuffer : BufferObject<ushort>
    {
        int index = 0;
        bool disposed = false;
        ushort[] buffer = null;
        int size; // count of x,y pairs
        readonly IndexPool indices = new IndexPool();
        bool changedSinceLastCreation = true;
        readonly BufferUsageHint usageHint = BufferUsageHint.DynamicDraw;

        public override int Size => size;

        public override VertexAttribIntegerType Type => VertexAttribIntegerType.UnsignedShort;

        public override int Dimension => 2;

        public SizeBuffer(bool staticData)
            : base(false)
        {
            index = GL.GenBuffer();

            if (staticData)
                usageHint = BufferUsageHint.StaticDraw;
        }

        public int Add(ushort width, ushort height)
        {
            int index = indices.AssignNextFreeIndex();

            if (buffer == null)
            {
                buffer = new ushort[128];
                buffer[0] = width;
                buffer[1] = height;
                changedSinceLastCreation = true;
                size = 2;
            }
            else
            {
                if (index == buffer.Length / 2) // we need to recreate the buffer
                {
                    Array.Resize(ref buffer, buffer.Length + 128);
                }

                size += 2;

                if (buffer[index * 2 + 0] != width ||
                    buffer[index * 2 + 1] != height)
                {
                    buffer[index * 2 + 0] = width;
                    buffer[index * 2 + 1] = height;
                    changedSinceLastCreation = true;
                }
            }

            return index;
        }

        public void Update(int index, ushort width, ushort height)
        {
            buffer[index * 2 + 0] = width;
            buffer[index * 2 + 1] = height;
            changedSinceLastCreation = true;
        }

        public void Remove(int index)
        {
            indices.UnassignIndex(index);
        }

        public void ReduceSizeTo(int size)
        {
            this.size = size;
        }

        public override void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                    if (index != 0)
                    {
                        GL.DeleteBuffer(index);

                        lock (buffer)
                        {
                            buffer = null;
                        }

                        size = 0;
                        index = 0;
                    }

                    disposed = true;
                }
            }
        }

        public override void Bind()
        {
            if (disposed)
                throw new Exception("Tried to bind a disposed buffer.");

            GL.BindBuffer(BufferTarget.ArrayBuffer, index);

            Recreate(); // ensure that the data is up to date
        }

        void Recreate() // is only called when the buffer is bound (see Bind())
        {
            if (!changedSinceLastCreation)
                return;

            lock (buffer)
            {
                GL.BufferData(BufferTarget.ArrayBuffer, Size * sizeof(ushort),
                    buffer, usageHint);
            }

            changedSinceLastCreation = false;
        }

        internal override bool RecreateUnbound()
        {
            if (!changedSinceLastCreation)
                return false;

            if (disposed)
                throw new Exception("Tried to recreate a disposed buffer.");

            GL.BindBuffer(BufferTarget.ArrayBuffer, index);

            lock (buffer)
            {
                GL.BufferData(BufferTarget.ArrayBuffer, Size * sizeof(ushort),
                    buffer, usageHint);
            }

            changedSinceLastCreation = false;

            return true;
        }
    }
}
