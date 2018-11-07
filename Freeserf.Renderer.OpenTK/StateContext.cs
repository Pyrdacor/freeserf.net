/*
 * StateContext.cs - OpenGL state switch helper
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