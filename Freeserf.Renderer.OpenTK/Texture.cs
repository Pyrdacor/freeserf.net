/*
 * Texture.cs - OpenGL texture handling
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
using System.IO;
using OpenTK.Graphics.OpenGL;

namespace Freeserf.Renderer.OpenTK
{
    public class Texture : Render.Texture, IDisposable
    {
        public static Texture ActiveTexture { get; private set; } = null;

        public virtual int Index { get; private set; } = 0;
        public override int Width { get; } = 0;
        public override int Height { get; } = 0;
        public virtual int SizeInBytes { get; private set; } = 0;

        protected Texture(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public Texture(int width, int height, PixelFormat format, Stream pixelDataStream, int numMipMapLevels = 0)
        {
            int size = width * height * (int)BytesPerPixel[(int)format];

            if ((pixelDataStream.Length - pixelDataStream.Position) < size)
                throw new Exception("Pixel data stream does not contain enough data.");

            if (!pixelDataStream.CanRead)
                throw new Exception("Pixel data stream does not support reading.");

            byte[] pixelData = new byte[size];

            pixelDataStream.Read(pixelData, 0, size);

            Index = GL.GenTexture();
            Width = width;
            Height = height;

            Create(format, pixelData, numMipMapLevels);

            pixelData = null;
        }

        public Texture(int width, int height, PixelFormat format, byte[] pixelData, int numMipMapLevels = 0)
        {
            if (width * height * BytesPerPixel[(int)format] != pixelData.Length)
                throw new Exception("Invalid texture data size.");

            Index = GL.GenTexture();
            Width = width;
            Height = height;

            Create(format, pixelData, numMipMapLevels);
        }

        static global::OpenTK.Graphics.OpenGL.PixelFormat ToOpenGLPixelFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.RGBA8:
                    return global::OpenTK.Graphics.OpenGL.PixelFormat.Rgba;
                case PixelFormat.BGRA8:
                    return global::OpenTK.Graphics.OpenGL.PixelFormat.Bgra;
                case PixelFormat.RGB8:
                    return global::OpenTK.Graphics.OpenGL.PixelFormat.Rgb;
                case PixelFormat.BGR8:
                    return global::OpenTK.Graphics.OpenGL.PixelFormat.Bgr;
                case PixelFormat.Alpha:
                    // Note: for the supported image format GL_RED means one channel data, GL_ALPHA is only used for texture storage on the gpu, so we don't use it
                    // We always use RGBA8 as texture storage on the gpu
                    return global::OpenTK.Graphics.OpenGL.PixelFormat.Red;
                default:
                    throw new Exception("Invalid pixel format.");
            }
        }

        void Create(PixelFormat format, byte[] pixelData, int numMipMapLevels)
        {
            if (format >= PixelFormat.RGB5A1)
            {
                pixelData = ConvertPixelData(pixelData, ref format);
            }

            Bind();

            var minMode = (numMipMapLevels > 0) ? TextureMinFilter.LinearMipmapLinear : TextureMinFilter.Nearest;

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minMode);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            GL.TexStorage2D(TextureTarget2d.Texture2D, 1 + numMipMapLevels, SizedInternalFormat.Rgba8, Width, Height);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Width, Height, ToOpenGLPixelFormat(format), PixelType.UnsignedByte, pixelData);

            if (numMipMapLevels > 0)
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            SizeInBytes = pixelData.Length;
        }

        public virtual void Bind()
        {
            if (disposed)
                throw new Exception("Tried to bind a disposed texture.");

            if (ActiveTexture == this)
                return;

            GL.BindTexture(TextureTarget.Texture2D, Index);
            ActiveTexture = this;
        }

        public static void Unbind()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
            ActiveTexture = null;
        }


        #region IDisposable Support

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (ActiveTexture == this)
                        Unbind();

                    if (Index != -1)
                    {
                        GL.DeleteTexture(Index);
                        Index = -1;
                    }
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);

        }

        #endregion

    }
}
