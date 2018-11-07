/*
 * ShaderProgram.cs - GLSL shader program handling
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
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace Freeserf.Renderer.OpenTK
{
    // TODO: for OGL version < 4.1 we should use GL.UniformX instead of GL.ProgramUniformX
    internal class ShaderProgram : IDisposable
    {
        Shader fragmentShader = null;
        Shader vertexShader = null;
        bool disposed = false;
        string fragmentColorOutputName = "color";
        readonly Dictionary<string, int> uniformLocations = new Dictionary<string, int>();
        readonly Dictionary<string, int> attributeLocations = new Dictionary<string, int>();

        public Int32 ProgramIndex { get; private set; } = 0;
        public bool Loaded { get; private set; } = false;
        public bool Linked { get; private set; } = false;
        public static ShaderProgram ActiveProgram { get; private set; } = null;

        public ShaderProgram()
        {
            Create();
        }

        public ShaderProgram(Shader fragmentShader, Shader vertexShader)
        {
        	Create();

            AttachShader(fragmentShader);
            AttachShader(vertexShader);

            Link(false);
        }

        void Create()
        {
            ProgramIndex = GL.CreateProgram();
        }

        public bool SetFragmentColorOutputName(string name)
        {
        	if (!string.IsNullOrWhiteSpace(name))
            {
            	fragmentColorOutputName = name;
                return true;
            }

            return false;
        }

        public void AttachShader(Shader shader)
        {
        	if (shader == null)
            	return;

            if (shader.ShaderType == Shader.Type.Fragment)
            {
            	if (fragmentShader == shader)
                	return;

            	if (fragmentShader != null)
                	GL.DetachShader(ProgramIndex, fragmentShader.ShaderIndex);

                fragmentShader = shader;
                GL.AttachShader(ProgramIndex, shader.ShaderIndex);
            }
            else if (shader.ShaderType == Shader.Type.Vertex)
            {
            	if (vertexShader == shader)
                	return;

            	if (vertexShader != null)
                	GL.DetachShader(ProgramIndex, vertexShader.ShaderIndex);

                vertexShader = shader;
                GL.AttachShader(ProgramIndex, shader.ShaderIndex);
            }

            Linked = false;
            Loaded = fragmentShader != null && vertexShader != null;
        }

        public void Link(bool detachShaders)
        {
            if (!Linked)
            {
            	if (!Loaded)
            		throw new InvalidOperationException("ShaderProgram.Link: Shader program was not loaded.");

                GL.LinkProgram(ProgramIndex);

                // Auf Fehler prüfen
                string infoLog = GL.GetProgramInfoLog(ProgramIndex);

                if (!string.IsNullOrWhiteSpace(infoLog))
                {
                    throw new Exception(infoLog.Trim()); // TODO: throw specialized exception?
                }

                Linked = true;
            }

            if (detachShaders)
            {
            	if (fragmentShader != null)
                {
                	GL.DetachShader(ProgramIndex, fragmentShader.ShaderIndex);
                    fragmentShader = null;
                }

                if (vertexShader != null)
                {
                	GL.DetachShader(ProgramIndex, vertexShader.ShaderIndex);
                    vertexShader = null;
                }

                Loaded = false;
            }
        }

        public void Use()
        {
        	if (!Linked)
            	throw new InvalidOperationException("ShaderProgram.Use: Shader program was not linked.");

            GL.UseProgram(ProgramIndex);
            ActiveProgram = this;

            //GL.BindFragDataLocation(ProgramIndex, 0, fragmentColorOutputName);
        }

        public static void Use(ShaderProgram program)
        {
            if (program != null)
                program.Use();
            else
            {
                GL.UseProgram(0);
                ActiveProgram = null;
            }
        }

        public int BindInputBuffer<T>(string name, BufferObject<T> buffer)
        {
        	if (ActiveProgram != this)
            	throw new InvalidOperationException("ShaderProgram.SetInputBuffer: Shader program is not active.");

            Int32 location = GetLocation(name, true);

            buffer.Bind();

            GL.EnableVertexAttribArray(location);
            GL.VertexAttribDivisor(location, (buffer.PerVertex) ? 0 : 1);

            GL.VertexAttribIPointer(location, buffer.Dimension, buffer.Type, 0, IntPtr.Zero);

            return location;
        }

        public void UnbindInputBuffer(int location)
        {
            GL.DisableVertexAttribArray(location);
        }

        Int32 GetLocation(string name, bool preferAttribute = false)
        {
            if (preferAttribute)
                return GL.GetAttribLocation(ProgramIndex, name);

            return GL.GetUniformLocation(ProgramIndex, name);
        }

        public void SetInputMatrix(string name, float[] matrix, bool transpose)
        {
            Int32 location = GetLocation(name);

            switch (matrix.Length)
            {
                case 4: // 2x2
                    GL.ProgramUniformMatrix2(ProgramIndex, location, 1, transpose, matrix);
                    break;
                case 9: // 3x3
                	GL.ProgramUniformMatrix3(ProgramIndex, location, 1, transpose, matrix);
                    break;
                case 16: // 4x4
                	GL.ProgramUniformMatrix4(ProgramIndex, location, 1, transpose, matrix);
                    break;
                default:
                	throw new InvalidOperationException("ShaderProgram.SetInputMatrix: Unsupported matrix dimensions. Valid are 2x2, 3x3 or 4x4.");
            }
        }

		public void SetInput(string name, bool value)
        {
            Int32 location = GetLocation(name);

            GL.ProgramUniform1(ProgramIndex, location, (value) ? 1 : 0);
        }

        public void SetInput(string name, float value)
        {
            Int32 location = GetLocation(name);

            GL.ProgramUniform1(ProgramIndex, location, value);
        }

        public void SetInput(string name, double value)
        {
            Int32 location = GetLocation(name);

            GL.ProgramUniform1(ProgramIndex, location, value);
        }

        public void SetInput(string name, int value)
        {
            Int32 location = GetLocation(name);

            GL.ProgramUniform1(ProgramIndex, location, value);
        }

        public void SetInput(string name, uint value)
        {
            Int32 location = GetLocation(name);

            // Note: for the uint version the program index also must be uint
            GL.ProgramUniform1((uint)ProgramIndex, location, value);
        }

        public void SetInputVector2(string name, float x, float y)
        {
            Int32 location = GetLocation(name);

            GL.ProgramUniform2(ProgramIndex, location, x, y);
        }

        public void SetInputVector2(string name, int x, int y)
        {
            Int32 location = GetLocation(name);

            GL.ProgramUniform2(ProgramIndex, location, x, y);
        }

        public void SetInputVector2(string name, uint x, uint y)
        {
            Int32 location = GetLocation(name);

            // Note: for the uint version the program index also must be uint
            GL.ProgramUniform2((uint)ProgramIndex, location, x, y);
        }

        public void SetInputVector3(string name, float x, float y, float z)
        {
            Int32 location = GetLocation(name);

            GL.ProgramUniform3(ProgramIndex, location, x, y, z);
        }

        public void SetInputVector3(string name, int x, int y, int z)
        {
            Int32 location = GetLocation(name);

            GL.ProgramUniform3(ProgramIndex, location, x, y, z);
        }

        public void SetInputVector3(string name, uint x, uint y, uint z)
        {
            Int32 location = GetLocation(name);

            // Note: for the uint version the program index also must be uint
            GL.ProgramUniform3((uint)ProgramIndex, location, x, y, z);
        }

        public void SetInputVector4(string name, float x, float y, float z, float w)
        {
            Int32 location = GetLocation(name);

            GL.ProgramUniform4(ProgramIndex, location, x, y, z, w);
        }

        public void SetInputVector4(string name, int x, int y, int z, int w)
        {
            Int32 location = GetLocation(name);

            GL.ProgramUniform4(ProgramIndex, location, x, y, z, w);
        }

        public void SetInputVector4(string name, uint x, uint y, uint z, uint w)
        {
            Int32 location = GetLocation(name);

            // Note: for the uint version the program index also must be uint
            GL.ProgramUniform4((uint)ProgramIndex, location, x, y, z, w);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (ProgramIndex != 0)
                    {
                    	if (ActiveProgram == this)
                        {
                    		GL.UseProgram(0);
                            ActiveProgram = null;
                        }

                    	GL.DeleteProgram(ProgramIndex);
                        ProgramIndex = 0;
                    }

                    disposed = true;
                }
            }
        }
   	}
}