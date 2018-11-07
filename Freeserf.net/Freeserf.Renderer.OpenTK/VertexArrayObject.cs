using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace Freeserf.Renderer.OpenTK
{
	// VAO
    internal class VertexArrayObject : IDisposable
    {
    	Int32 index = 0;
        readonly Dictionary<string, PositionBuffer> positionBuffers = new Dictionary<string, PositionBuffer>();
        readonly Dictionary<string, SizeBuffer> sizeBuffers = new Dictionary<string, SizeBuffer>();
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

        public void AddBuffer(string name, SizeBuffer buffer)
        {
            sizeBuffers.Add(name, buffer);
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

            foreach (var buffer in sizeBuffers)
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

            foreach (var buffer in sizeBuffers)
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
                GL.BindVertexArray(index);

            if (!bindOnly)
            {
                bool buffersChanged = false;

                // ensure that all buffers are up to date
                foreach (var buffer in positionBuffers)
                {
                    if (buffer.Value.RecreateUnbound())
                        buffersChanged = true;
                }
                foreach (var buffer in sizeBuffers)
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