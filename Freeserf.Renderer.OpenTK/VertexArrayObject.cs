/*
 * VertexArrayBuffer.cs - OpenGL VAO handling
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
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace Freeserf.Renderer.OpenTK
{
	// VAO
    internal class VertexArrayObject : IDisposable
    {
    	Int32 index = 0;
        readonly Dictionary<string, PositionBuffer> positionBuffers = new Dictionary<string, PositionBuffer>();
        readonly Dictionary<string, BaseLineBuffer> baseLineBuffers = new Dictionary<string, BaseLineBuffer>();
        readonly Dictionary<string, int> bufferLocations = new Dictionary<string, int>();
        bool disposed = false;
        bool buffersAreBound = false;
        ShaderProgram program = null;

        public static VertexArrayObject ActiveVAO { get; private set; } = null;

        public VertexArrayObject(ShaderProgram program)
        {
            this.program = program;

        	Create();
        }

        void Create()
        {
            index = GL.GenVertexArray();
        }

        public void AddBuffer(string name, PositionBuffer buffer)
        {
            positionBuffers.Add(name, buffer);
        }

        public void AddBuffer(string name, BaseLineBuffer buffer)
        {
            baseLineBuffers.Add(name, buffer);
        }

        public void BindBuffers()
        {
            if (buffersAreBound)
                return;

            program.Use();
            InternalBind(true);

            foreach (var buffer in positionBuffers)
            {
                bufferLocations[buffer.Key] = program.BindInputBuffer(buffer.Key, buffer.Value);
            }

            foreach (var buffer in baseLineBuffers)
            {
                bufferLocations[buffer.Key] = program.BindInputBuffer(buffer.Key, buffer.Value);
            }

            buffersAreBound = true;
        }

        public void UnbindBuffers()
        {
            if (!buffersAreBound)
                return;

            program.Use();
            InternalBind(true);            

            foreach (var buffer in positionBuffers)
            {
                program.UnbindInputBuffer(bufferLocations[buffer.Key]);
                bufferLocations[buffer.Key] = -1;
            }

            foreach (var buffer in baseLineBuffers)
            {
                program.UnbindInputBuffer(bufferLocations[buffer.Key]);
                bufferLocations[buffer.Key] = -1;
            }

            buffersAreBound = false;
        }

        public void Bind()
        {
            InternalBind(false);
        }

        void InternalBind(bool bindOnly)
        {
            if (ActiveVAO != this)
            {
                GL.BindVertexArray(index);
                program.Use();
            }

            if (!bindOnly)
            {
                bool buffersChanged = false;

                // ensure that all buffers are up to date
                foreach (var buffer in positionBuffers)
                {
                    if (buffer.Value.RecreateUnbound())
                        buffersChanged = true;
                }

                foreach (var buffer in baseLineBuffers)
                {
                    if (buffer.Value.RecreateUnbound())
                        buffersChanged = true;
                }

                if (buffersChanged)
                {
                    UnbindBuffers();
                    BindBuffers();
                }
            }

            ActiveVAO = this;
        }

        public static void Bind(VertexArrayObject vao)
        {
            if (vao != null)
            	vao.Bind();
            else
            	Unbind();
        }

        public static void Unbind()
        {
        	GL.BindVertexArray(0);
            ActiveVAO = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (index != 0)
                    {
                    	if (ActiveVAO == this)
                    		Unbind();

                    	GL.DeleteVertexArray(index);
                        index = 0;
                    }

                    disposed = true;
                }
            }
        }
   	}
}