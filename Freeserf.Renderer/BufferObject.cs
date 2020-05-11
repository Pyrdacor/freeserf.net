/*
 * BufferObject.cs - Base class for integer based data buffers
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
using System;

namespace Freeserf.Renderer
{
    internal abstract class BufferObject<T> : IDisposable
    {
        public abstract int Dimension { get; }
        public bool Normalized { get; protected set; } = false;
        public abstract int Size { get; }
        public abstract VertexAttribPointerType Type { get; }

        public abstract void Dispose();

        public abstract void Bind();

        internal abstract bool RecreateUnbound();

        protected static T[] EnsureBufferSize(T[] buffer, int size, out bool changed)
        {
            changed = false;

            if (buffer == null)
            {
                changed = true;

                // first we just use a 256B buffer
                return new T[256];
            }
            else if (buffer.Length <= size) // we need to recreate the buffer
            {
                changed = true;

                if (buffer.Length < 0xffff) // double size up to 64K
                    Array.Resize(ref buffer, buffer.Length << 1);
                else // increase by 1K after 64K reached
                    Array.Resize(ref buffer, buffer.Length + 1024);
            }

            return buffer;
        }
    }
}
