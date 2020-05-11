/*
 * Shader.cs - GLSL shader handling
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
    internal class Shader : IDisposable
    {
        public enum Type
        {
            Fragment,
            Vertex
        }

        string code = "";
        bool disposed = false;

        public Type ShaderType { get; } = Type.Fragment;
        public uint ShaderIndex { get; private set; } = 0;

        public Shader(Type type, string code)
        {
            ShaderType = type;
            this.code = code;

            Create();
        }

        void Create()
        {
            ShaderIndex = State.Gl.CreateShader((ShaderType == Type.Fragment) ?
                GLEnum.FragmentShader :
                GLEnum.VertexShader);

            State.Gl.ShaderSource(ShaderIndex, 1, new string[] { code }, new Int32[] { code.Length });
            State.Gl.CompileShader(ShaderIndex);

            // Auf Fehler pr�fen
            string infoLog = State.Gl.GetShaderInfoLog(ShaderIndex);

            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                throw new Exception(infoLog.Trim()); // TODO: throw specialized exception?
            }
        }

        public void AttachToProgram(ShaderProgram program)
        {
            program.AttachShader(this);
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
                    if (ShaderIndex != 0)
                    {
                        State.Gl.DeleteShader(ShaderIndex);
                        ShaderIndex = 0;
                    }

                    disposed = true;
                }
            }
        }
    }
}