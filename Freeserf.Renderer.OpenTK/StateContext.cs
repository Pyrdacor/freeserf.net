using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Freeserf.Renderer.OpenTK
{
    internal class StateContext : IDisposable
    {
        bool disposed = false;
        ShaderProgram preProgram;
        VertexArrayObject preVAO;
        readonly Matrix4 preProjectionMatrix;
        readonly Matrix4 preModelViewMatrix;

        public StateContext()
        {
            preProgram = ShaderProgram.ActiveProgram;
            preVAO = VertexArrayObject.ActiveVAO;
            preProjectionMatrix = State.CurrentProjectionMatrix;
            preModelViewMatrix = State.CurrentModelViewMatrix;
        }

        void Release()
        {
            if (preProgram != ShaderProgram.ActiveProgram && preProgram != null)
                preProgram.Use();
            if (preVAO != VertexArrayObject.ActiveVAO && preVAO != null)
                preVAO.Bind();
            if (preProjectionMatrix != State.CurrentProjectionMatrix && preProjectionMatrix != null)
                State.RestoreProjectionMatrix(preProjectionMatrix);
            if (preModelViewMatrix != State.CurrentModelViewMatrix && preModelViewMatrix != null)
                State.RestoreProjectionMatrix(preModelViewMatrix);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    Release();

                    disposed = true;
                }
            }
        }
    }
}