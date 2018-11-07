/*
 * Shader.cs - GLSL shader handling
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
        public Int32 ShaderIndex { get; private set; } = 0;

        public Shader(Type type, string code)
        {
        	ShaderType = type;
            this.code = code;

            Create();
        }

        void Create()
        {
            ShaderIndex = GL.CreateShader((ShaderType == Type.Fragment) ?
                global::OpenTK.Graphics.OpenGL.ShaderType.FragmentShader :
                global::OpenTK.Graphics.OpenGL.ShaderType.VertexShader);

            GL.ShaderSource(ShaderIndex, 1, new string[] { code }, new Int32[] { code.Length });
            GL.CompileShader(ShaderIndex);

            // Auf Fehler prüfen
            string infoLog = GL.GetShaderInfoLog(ShaderIndex);

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
                    	GL.DeleteShader(ShaderIndex);
                        ShaderIndex = 0;
                    }

                    disposed = true;
                }
            }
        }
   	}
}