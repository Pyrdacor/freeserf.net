using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace Freeserf.Renderer.OpenTK
{
    internal class Context
    {
        int width = -1;
        int height = -1;
        Rotation rotation = Rotation.None;
        Matrix4 modelViewMatrix = Matrix4.Identity;

        public Context(int width, int height)
        {
            // We need at least OpenGL 3.1 for instancing and shaders
            if (State.OpenGLVersionMajor < 3 || (State.OpenGLVersionMajor == 3 && State.OpenGLVersionMinor < 1))
                throw new Exception($"OpenGL version 3.1 is required for rendering. Your version is {State.OpenGLVersionMajor}.{State.OpenGLVersionMinor}.");

            GL.ClearColor(0.0f, 1.0f, 0.0f, 1.0f);

            GL.Enable(EnableCap.Blend);
            GL.BlendEquationSeparate(BlendEquationMode.FuncAdd, BlendEquationMode.FuncAdd);
            GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.Zero);

            Resize(width, height);
        }

        public void Resize(int width, int height)
        {
            //if (this.width == width && this.height == height)
            //    return;

            State.ClearMatrices();
            State.PushModelViewMatrix(Matrix4.Identity);
            State.PushProjectionMatrix(Matrix4.CreateOrtho2D(0, width, 0, height));

            this.width = width;
            this.height = height;

            SetRotation(rotation, true);
        }

        public void SetRotation(Rotation rotation, bool forceUpdate = false)
        {
            if (forceUpdate || rotation != this.rotation)
            {
                this.rotation = rotation;

                ApplyRotationMatrix();
            }
        }

        void ApplyRotationMatrix()
        {
            State.RestoreModelViewMatrix(modelViewMatrix);
            State.PopModelViewMatrix();

            if (rotation == Rotation.None)
                modelViewMatrix = Matrix4.Identity;
            else
            {
                var rotationDegree = 0.0f;

                switch (rotation)
                {
                    case Rotation.Deg90:
                        rotationDegree = 90.0f;
                        break;
                    case Rotation.Deg180:
                        rotationDegree = 180.0f;
                        break;
                    case Rotation.Deg270:
                        rotationDegree = 270.0f;
                        break;
                    default:
                        break;
                }

                var x = 0.5f * width;
                var y = 0.5f * height;

                if (rotation != Rotation.Deg180) // 90° or 270°
                {
                    float factor = (float)height / (float)width;

                    modelViewMatrix =
                        Matrix4.CreateTranslationMatrix(x, y) *
                        Matrix4.CreateRotationMatrix(rotationDegree) *
                        Matrix4.CreateScalingMatrix(factor, 1.0f / factor) *
                        Matrix4.CreateTranslationMatrix(-x, -y);
                }
                else // 180°
                {
                    modelViewMatrix =
                        Matrix4.CreateTranslationMatrix(x, y) *
                        Matrix4.CreateRotationMatrix(rotationDegree) *
                        Matrix4.CreateTranslationMatrix(-x, -y);
                }
            }

            State.PushModelViewMatrix(modelViewMatrix);
        }
    }
}
