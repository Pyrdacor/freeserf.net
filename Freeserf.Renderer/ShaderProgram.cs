/*
 * ShaderProgram.cs - GLSL shader program handling
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

using System;

namespace Freeserf.Renderer
{
    // TODO: for OGL version < 4.1 we should use Gl.UniformX instead of Gl.ProgramUniformX
    internal class ShaderProgram : IDisposable
    {
        Shader fragmentShader = null;
        Shader vertexShader = null;
        bool disposed = false;
        string fragmentColorOutputName = "color";

        public uint ProgramIndex { get; private set; } = 0;
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
            ProgramIndex = State.Gl.CreateProgram();
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
                    State.Gl.DetachShader(ProgramIndex, fragmentShader.ShaderIndex);

                fragmentShader = shader;
                State.Gl.AttachShader(ProgramIndex, shader.ShaderIndex);
            }
            else if (shader.ShaderType == Shader.Type.Vertex)
            {
                if (vertexShader == shader)
                    return;

                if (vertexShader != null)
                    State.Gl.DetachShader(ProgramIndex, vertexShader.ShaderIndex);

                vertexShader = shader;
                State.Gl.AttachShader(ProgramIndex, shader.ShaderIndex);
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

                State.Gl.LinkProgram(ProgramIndex);

                // Auf Fehler pr�fen
                string infoLog = State.Gl.GetProgramInfoLog(ProgramIndex);

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
                    State.Gl.DetachShader(ProgramIndex, fragmentShader.ShaderIndex);
                    fragmentShader = null;
                }

                if (vertexShader != null)
                {
                    State.Gl.DetachShader(ProgramIndex, vertexShader.ShaderIndex);
                    vertexShader = null;
                }

                Loaded = false;
            }
        }

        public void Use()
        {
            if (!Linked)
                throw new InvalidOperationException("ShaderProgram.Use: Shader program was not linked.");

            State.Gl.UseProgram(ProgramIndex);
            ActiveProgram = this;

            //Gl.BindFragDataLocation(ProgramIndex, 0, fragmentColorOutputName);
        }

        public static void Use(ShaderProgram program)
        {
            if (program != null)
                program.Use();
            else
            {
                State.Gl.UseProgram(0);
                ActiveProgram = null;
            }
        }

        public uint BindInputBuffer<T>(string name, BufferObject<T> buffer)
        {
            if (ActiveProgram != this)
                throw new InvalidOperationException("ShaderProgram.SetInputBuffer: Shader program is not active.");

            var location = GetLocation(name, true);

            buffer.Bind();

            State.Gl.EnableVertexAttribArray(location);

            unsafe
            {
                State.Gl.VertexAttribIPointer(location, buffer.Dimension, buffer.Type, 0, (void*)0);
            }

            return location;
        }

        public void UnbindInputBuffer(uint location)
        {
            State.Gl.DisableVertexAttribArray(location);
        }

        uint GetLocation(string name, bool preferAttribute = false)
        {
            if (preferAttribute)
                return (uint)State.Gl.GetAttribLocation(ProgramIndex, name);

            return (uint)State.Gl.GetUniformLocation(ProgramIndex, name);
        }

        public void SetInputMatrix(string name, float[] matrix, bool transpose)
        {
            var location = GetLocation(name);

            switch (matrix.Length)
            {
                case 4: // 2x2
                    State.Gl.ProgramUniformMatrix2(ProgramIndex, (int)location, transpose, matrix);
                    break;
                case 9: // 3x3
                    State.Gl.ProgramUniformMatrix3(ProgramIndex, (int)location, transpose, matrix);
                    break;
                case 16: // 4x4
                    State.Gl.ProgramUniformMatrix4(ProgramIndex, (int)location, transpose, matrix);
                    break;
                default:
                    throw new InvalidOperationException("ShaderProgram.SetInputMatrix: Unsupported matrix dimensions. Valid are 2x2, 3x3 or 4x4.");
            }
        }

        public void SetInput(string name, bool value)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform1(ProgramIndex, (int)location, (value) ? 1 : 0);
        }

        public void SetInput(string name, float value)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform1(ProgramIndex, (int)location, value);
        }

        public void SetInput(string name, double value)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform1(ProgramIndex, (int)location, (float)value);
        }

        public void SetInput(string name, int value)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform1(ProgramIndex, (int)location, value);
        }

        public void SetInput(string name, uint value)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform1(ProgramIndex, (int)location, value);
        }

        public void SetInputVector2(string name, float x, float y)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform2(ProgramIndex, (int)location, x, y);
        }

        public void SetInputVector2(string name, int x, int y)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform2(ProgramIndex, (int)location, x, y);
        }

        public void SetInputVector2(string name, uint x, uint y)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform2(ProgramIndex, (int)location, x, y);
        }

        public void SetInputVector3(string name, float x, float y, float z)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform3(ProgramIndex, (int)location, x, y, z);
        }

        public void SetInputVector3(string name, int x, int y, int z)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform3(ProgramIndex, (int)location, x, y, z);
        }

        public void SetInputVector3(string name, uint x, uint y, uint z)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform3(ProgramIndex, (int)location, x, y, z);
        }

        public void SetInputVector4(string name, float x, float y, float z, float w)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform4(ProgramIndex, (int)location, x, y, z, w);
        }

        public void SetInputVector4(string name, int x, int y, int z, int w)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform4(ProgramIndex, (int)location, x, y, z, w);
        }

        public void SetInputVector4(string name, uint x, uint y, uint z, uint w)
        {
            var location = GetLocation(name);

            State.Gl.ProgramUniform4(ProgramIndex, (int)location, x, y, z, w);
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
                            State.Gl.UseProgram(0);
                            ActiveProgram = null;
                        }

                        State.Gl.DeleteProgram(ProgramIndex);
                        ProgramIndex = 0;
                    }

                    disposed = true;
                }
            }
        }
    }
}