/*
 * IndexBuffer.cs - Dynamic buffer for vertex indices
 *
 * Copyright (C) 2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System;

namespace Freeserf.Renderer
{
    internal class IndexBuffer : BufferObject<uint>
    {
        uint index = 0;
        bool disposed = false;
        readonly object bufferLock = new object();
        private uint[] buffer = null;
        bool changedSinceLastCreation = true;
        int size = 0;

        public override int Size => size;

        public override VertexAttribPointerType Type => VertexAttribPointerType.UnsignedInt;

        public override int Dimension => 6;

        public IndexBuffer()
        {
            index = State.Gl.GenBuffer();
        }

        public override void Bind()
        {
            if (disposed)
                throw new Exception("Tried to bind a disposed buffer.");

            State.Gl.BindBuffer(GLEnum.ElementArrayBuffer, index);

            Recreate(); // ensure that the data is up to date
        }

        public void Unbind()
        {
            if (disposed)
                return;

            State.Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
        }

        void Recreate() // is only called when the buffer is bound (see Bind())
        {
            if (!changedSinceLastCreation || buffer == null)
                return;

            lock (bufferLock)
            {
                unsafe
                {
                    fixed (uint* ptr = &buffer[0])
                    {
                        State.Gl.BufferData(GLEnum.ElementArrayBuffer, (uint)(Size * sizeof(uint)),
                            ptr, GLEnum.StaticDraw);
                    }
                }
            }

            changedSinceLastCreation = false;
        }

        internal override bool RecreateUnbound()
        {
            if (!changedSinceLastCreation || buffer == null)
                return false;

            if (disposed)
                throw new Exception("Tried to recreate a disposed buffer.");

            State.Gl.BindBuffer(GLEnum.ArrayBuffer, index);

            lock (bufferLock)
            {
                unsafe
                {
                    fixed (uint* ptr = &buffer[0])
                    {
                        State.Gl.BufferData(GLEnum.ArrayBuffer, (uint)(Size * sizeof(uint)),
                            ptr, GLEnum.StaticDraw);
                    }
                }
            }

            changedSinceLastCreation = false;

            return true;
        }

        public void InsertQuad(int quadIndex)
        {
            if (quadIndex >= int.MaxValue / 6)
                throw new OutOfMemoryException("Too many polygons to render.");

            int arrayIndex = quadIndex * 6; // 2 triangles with 3 vertices each
            uint vertexIndex = (uint)(quadIndex * 4); // 4 different vertices form a quad

            if (size <= arrayIndex + 6)
            {
                buffer = EnsureBufferSize(buffer, arrayIndex + 6, out _);

                buffer[arrayIndex++] = vertexIndex + 0;
                buffer[arrayIndex++] = vertexIndex + 1;
                buffer[arrayIndex++] = vertexIndex + 2;
                buffer[arrayIndex++] = vertexIndex + 3;
                buffer[arrayIndex++] = vertexIndex + 0;
                buffer[arrayIndex++] = vertexIndex + 2;

                size = arrayIndex;
                changedSinceLastCreation = true;
            }
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
                    State.Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);

                    if (index != 0)
                    {
                        State.Gl.DeleteBuffer(index);

                        if (buffer != null)
                        {
                            lock (bufferLock)
                            {
                                buffer = null;
                            }
                        }

                        size = 0;
                        index = 0;
                    }

                    disposed = true;
                }
            }
        }
    }
}
