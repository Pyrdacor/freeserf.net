/*
 * PositionBuffer.cs - Buffer for shader position data
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
using OpenTK.Graphics.OpenGL;

namespace Freeserf.Renderer.OpenTK
{
    internal class PositionBuffer : BufferObject<short>
    {
        int index = 0;
        bool disposed = false;
        short[] buffer = null;
        int size; // count of x,y pairs
        readonly IndexPool indices = new IndexPool();
        bool changedSinceLastCreation = true;
        readonly BufferUsageHint usageHint = BufferUsageHint.DynamicDraw;

        public override int Size => size;

        public override VertexAttribIntegerType Type => VertexAttribIntegerType.Short;

        public override int Dimension => 2;

        public PositionBuffer(bool staticData)
            : base(false)
        {
            index = GL.GenBuffer();

            if (staticData)
                usageHint = BufferUsageHint.StaticDraw;
        }

        public bool IsPositionValid(int index)
        {
            return buffer[index * 2] != short.MaxValue;
        }

        public int Add(short x, short y)
        {
            int index = indices.AssignNextFreeIndex();

            if (buffer == null)
            {
                buffer = new short[128];
                buffer[0] = x;
                buffer[1] = y;
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

                if (buffer[index * 2 + 0] != x ||
                    buffer[index * 2 + 1] != y)
                {
                    buffer[index * 2 + 0] = x;
                    buffer[index * 2 + 1] = y;
                    changedSinceLastCreation = true;
                }
            }

            return index;
        }

        public void Update(int index, short x, short y)
        {
            buffer[index * 2 + 0] = x;
            buffer[index * 2 + 1] = y;
            changedSinceLastCreation = true;
        }

        public void Remove(int index)
        {
            indices.UnassignIndex(index);
            buffer[index * 2] = short.MaxValue; // not displayed anymore
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
                GL.BufferData(BufferTarget.ArrayBuffer, Size * sizeof(short),
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
                GL.BufferData(BufferTarget.ArrayBuffer, Size * sizeof(short),
                    buffer, usageHint);
            }

            changedSinceLastCreation = false;

            return true;
        }
    }
}
