/*
 * State.cs - OpenGL state
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
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Freeserf.Renderer
{
    public static class State
    {
        public static readonly int OpenGLVersionMajor = 0;
        public static readonly int OpenGLVersionMinor = 0;
        public static readonly int GLSLVersionMajor = 0;
        public static readonly int GLSLVersionMinor = 0;
        public static readonly GL Gl = null;

        static State()
        {
            Gl = GL.GetApi();

            var openGLVersion = Gl.GetString(StringName.Version).TrimStart();

            Regex versionRegex = new Regex(@"([0-9]+)\.([0-9]+)", RegexOptions.Compiled);

            var match = versionRegex.Match(openGLVersion);

            if (!match.Success || match.Index != 0 || match.Groups.Count < 3)
            {
                throw new Exception("OpenGL is not supported or the version could not be determined.");
            }

            OpenGLVersionMajor = int.Parse(match.Groups[1].Value);
            OpenGLVersionMinor = int.Parse(match.Groups[2].Value);

            if (OpenGLVersionMajor >= 2) // glsl is supported since OpenGL 2.0
            {
                var glslVersion = Gl.GetString(StringName.ShadingLanguageVersion);

                match = versionRegex.Match(glslVersion);

                if (match.Success && match.Index == 0 && match.Groups.Count >= 3)
                {
                    GLSLVersionMajor = int.Parse(match.Groups[1].Value);
                    GLSLVersionMinor = int.Parse(match.Groups[2].Value);
                }
            }
        }

        public static bool ShadersAvailable => OpenGLVersionMajor >= 2 && GLSLVersionMajor > 0;

        static Stack<Matrix4> projectionMatrixStack = new Stack<Matrix4>();
        static Stack<Matrix4> modelViewMatrixStack = new Stack<Matrix4>();
        static Stack<Matrix4> unzoomedModelViewMatrixStack = new Stack<Matrix4>();

        public static void PushProjectionMatrix(Matrix4 matrix)
        {
            projectionMatrixStack.Push(matrix);
        }

        public static void PushModelViewMatrix(Matrix4 matrix)
        {
            modelViewMatrixStack.Push(matrix);
        }

        public static void PushUnzoomedModelViewMatrix(Matrix4 matrix)
        {
            unzoomedModelViewMatrixStack.Push(matrix);
        }

        public static Matrix4 PopProjectionMatrix()
        {
            return projectionMatrixStack.Pop();
        }

        public static Matrix4 PopModelViewMatrix()
        {
            return modelViewMatrixStack.Pop();
        }

        public static Matrix4 PopUnzoomedModelViewMatrix()
        {
            return unzoomedModelViewMatrixStack.Pop();
        }

        public static void RestoreProjectionMatrix(Matrix4 matrix)
        {
            if (projectionMatrixStack.Contains(matrix))
            {
                while (CurrentProjectionMatrix != matrix)
                    projectionMatrixStack.Pop();
            }
            else
                PushProjectionMatrix(matrix);
        }

        public static void RestoreModelViewMatrix(Matrix4 matrix)
        {
            if (modelViewMatrixStack.Contains(matrix))
            {
                while (CurrentModelViewMatrix != matrix)
                    modelViewMatrixStack.Pop();
            }
            else
                PushModelViewMatrix(matrix);
        }

        public static void RestoreUnzoomedModelViewMatrix(Matrix4 matrix)
        {
            if (unzoomedModelViewMatrixStack.Contains(matrix))
            {
                while (CurrentUnzoomedModelViewMatrix != matrix)
                    unzoomedModelViewMatrixStack.Pop();
            }
            else
                PushUnzoomedModelViewMatrix(matrix);
        }

        public static void ClearMatrices()
        {
            projectionMatrixStack.Clear();
            modelViewMatrixStack.Clear();
        }

        public static Matrix4 CurrentProjectionMatrix => (projectionMatrixStack.Count == 0) ? null : projectionMatrixStack.Peek();
        public static Matrix4 CurrentModelViewMatrix => (modelViewMatrixStack.Count == 0) ? null : modelViewMatrixStack.Peek();
        public static Matrix4 CurrentUnzoomedModelViewMatrix => (unzoomedModelViewMatrixStack.Count == 0) ? null : unzoomedModelViewMatrixStack.Peek();
    }
}
