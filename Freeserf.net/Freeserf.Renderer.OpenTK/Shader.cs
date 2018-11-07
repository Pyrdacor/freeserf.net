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