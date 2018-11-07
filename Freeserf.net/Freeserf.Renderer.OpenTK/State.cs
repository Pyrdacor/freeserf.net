using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using OpenTK.Graphics.OpenGL;

namespace Freeserf.Renderer.OpenTK
{
    internal static class State
    {
        public static readonly int OpenGLVersionMajor = 0;
        public static readonly int OpenGLVersionMinor = 0;
        public static readonly int GLSLVersionMajor = 0;
        public static readonly int GLSLVersionMinor = 0;

        static State()
        {
            var openGLVersion = GL.GetString(StringName.Version).TrimStart();

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
                var glslVersion = GL.GetString(StringName.ShadingLanguageVersion);

                match = versionRegex.Match(openGLVersion);

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

        public static void PushProjectionMatrix(Matrix4 matrix)
        {
            projectionMatrixStack.Push(matrix);
        }

        public static void PushModelViewMatrix(Matrix4 matrix)
        {
            modelViewMatrixStack.Push(matrix);
        }

        public static Matrix4 PopProjectionMatrix()
        {
            return projectionMatrixStack.Pop();
        }

        public static Matrix4 PopModelViewMatrix()
        {
            return modelViewMatrixStack.Pop();
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

        public static void ClearMatrices()
        {
            projectionMatrixStack.Clear();
            modelViewMatrixStack.Clear();
        }

        public static Matrix4 CurrentProjectionMatrix => (projectionMatrixStack.Count == 0) ? null : projectionMatrixStack.Peek();
        public static Matrix4 CurrentModelViewMatrix => (modelViewMatrixStack.Count == 0) ? null : modelViewMatrixStack.Peek();
    }
}
