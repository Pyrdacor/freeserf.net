/*
 * DataSource.cs - Game resources file functions
 *
 * Copyright (C) 2015-2017  Wicked_Digger <wicked_digger@mail.ru>
 * Copyright (C) 2018       Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Freeserf.Data
{
    [Flags]
    public enum DataLoadResult
    {
        NothingLoaded = 0x00,
        GraphicsLoaded = 0x01,
        MusicLoaded = 0x02,
        SoundLoaded = 0x04,
        AllLoaded = GraphicsLoaded | MusicLoaded | SoundLoaded
    }

    public class SpriteInfo
    {
        public int DeltaX;
        public int DeltaY;
        public int OffsetX;
        public int OffsetY;
        public int Width;
        public int Height;
    }

    // Sprite object.
    // Contains BGRA data.
    public class Sprite : IDisposable
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Color
        {
            public byte Blue;
            public byte Green;
            public byte Red;
            public byte Alpha;

            public static readonly Color Transparent = new Color() { Blue = 0, Green = 0, Red = 0, Alpha = 0 };
        }

        protected int deltaX = 0;
        protected int deltaY = 0;
        protected int offsetX = 0;
        protected int offsetY = 0;
        protected uint width = 0u;
        protected uint height = 0u;
        protected byte[] data = null;

        public Sprite()
        {

        }

        public Sprite(Sprite other)
        {
            deltaX = other.DeltaX;
            deltaY = other.DeltaY;
            offsetX = other.OffsetX;
            offsetY = other.OffsetY;

            Create(other.Width, other.Height);
        }

        public Sprite(uint width, uint height)
        {
            Create(width, height);
        }

        public static Sprite CreateFromFile(string filename, Color? colorkey = null, KeyValuePair<Color, Color>? colorReplacing = null)
        {
            using (var stream = File.OpenRead(filename))
            {
                return CreateFromStream(stream, colorkey, colorReplacing);
            }
        }

        public static Sprite CreateFromStream(Stream stream, Color? colorkey = null, KeyValuePair<Color, Color>? colorReplacing = null)
        {
            var image = new Bitmap(stream);
            var sprite = new Sprite();

            if (image.PixelFormat != PixelFormat.Format32bppArgb &&
                image.PixelFormat != PixelFormat.Format24bppRgb &&
                image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new NotSupportedException("Unsupported image format: " + image.PixelFormat.ToString());

            sprite.width = (uint)image.Width;
            sprite.height = (uint)image.Height;
            sprite.data = GetImageData(image, colorkey, colorReplacing);

            return sprite;
        }

        private static byte[] GetImageData(Bitmap image, Color? colorkey, KeyValuePair<Color, Color>? colorReplacing)
        {
            Rectangle area = new Rectangle(0, 0, image.Width, image.Height);
            var imageData = image.LockBits(area, ImageLockMode.ReadOnly, image.PixelFormat);
            int bytesPerPixel = image.PixelFormat switch
            {
                PixelFormat.Format32bppArgb => 4,
                PixelFormat.Format24bppRgb => 3,
                PixelFormat.Format8bppIndexed => 1,
                _ => throw new NotSupportedException("Unsupported image format: " + image.PixelFormat.ToString())
            };
            byte[] data = new byte[image.Width * image.Height * bytesPerPixel];

            try
            {
                IntPtr dataPointer = imageData.Scan0;
                Marshal.Copy(dataPointer, data, 0, data.Length);
            }
            finally
            {
                image.UnlockBits(imageData);
            }

            if (bytesPerPixel == 1)
            {
                if (image.Palette == null)
                    throw new InvalidDataException("No palette provided.");

                byte[] temp = new byte[image.Width * image.Height * 4];

                for (int i = 0; i < data.Length; ++i)
                {
                    var color = image.Palette.Entries[data[i]];
                    temp[i * 4 + 0] = color.B;
                    temp[i * 4 + 1] = color.G;
                    temp[i * 4 + 2] = color.R;
                    temp[i * 4 + 3] = color.A;
                }

                data = temp;
            }
            else if (bytesPerPixel == 3)
            {
                byte[] temp = new byte[image.Width * image.Height * 4];

                for (int i = 0; i < data.Length / 3; ++i)
                {
                    if (colorkey != null &&
                        data[i * 3 + 0] == colorkey.Value.Blue &&
                        data[i * 3 + 1] == colorkey.Value.Green &&
                        data[i * 3 + 2] == colorkey.Value.Red)
                    {
                        temp[i * 4 + 0] = 0;
                        temp[i * 4 + 1] = 0;
                        temp[i * 4 + 2] = 0;
                        temp[i * 4 + 3] = 0;
                    }
                    else if (colorReplacing != null &&
                        data[i * 3 + 0] == colorReplacing.Value.Key.Blue &&
                        data[i * 3 + 1] == colorReplacing.Value.Key.Green &&
                        data[i * 3 + 2] == colorReplacing.Value.Key.Red)
                    {
                        temp[i * 4 + 0] = colorReplacing.Value.Value.Blue;
                        temp[i * 4 + 1] = colorReplacing.Value.Value.Green;
                        temp[i * 4 + 2] = colorReplacing.Value.Value.Red;
                        temp[i * 4 + 3] = 255;
                    }
                    else
                    {
                        temp[i * 4 + 0] = data[i * 3 + 0];
                        temp[i * 4 + 1] = data[i * 3 + 1];
                        temp[i * 4 + 2] = data[i * 3 + 2];
                        temp[i * 4 + 3] = 255;
                    }
                }

                data = temp;
            }
            else if (colorkey != null)
            {
                for (int i = 0; i < data.Length / 4; ++i)
                {
                    if (colorkey != null &&
                        data[i * 4 + 0] == colorkey.Value.Blue &&
                        data[i * 4 + 1] == colorkey.Value.Green &&
                        data[i * 4 + 2] == colorkey.Value.Red)
                    {
                        data[i * 4 + 0] = 0;
                        data[i * 4 + 1] = 0;
                        data[i * 4 + 2] = 0;
                        data[i * 4 + 3] = 0;
                    }
                    else if (colorReplacing != null &&
                        data[i * 4 + 0] == colorReplacing.Value.Key.Blue &&
                        data[i * 4 + 1] == colorReplacing.Value.Key.Green &&
                        data[i * 4 + 2] == colorReplacing.Value.Key.Red &&
                        data[i * 4 + 3] == colorReplacing.Value.Key.Alpha)
                    {
                        data[i * 4 + 0] = colorReplacing.Value.Value.Blue;
                        data[i * 4 + 1] = colorReplacing.Value.Value.Green;
                        data[i * 4 + 2] = colorReplacing.Value.Value.Red;
                        data[i * 4 + 3] = colorReplacing.Value.Value.Alpha;
                    }
                }
            }

            return data;
        }

        public virtual byte[] GetData()
        {
            return data;
        }

        public virtual uint Width => width;
        public virtual uint Height => height;
        public virtual int DeltaX => deltaX;
        public virtual int DeltaY => deltaY;
        public virtual int OffsetX => offsetX;
        public virtual int OffsetY => offsetY;

        // Enlarges a sprite in height by adding pixel rows from the beginning
        public Sprite RepeatTo(int height)
        {
            if (height < Height)
                throw new ExceptionFreeserf(ErrorSystemType.Data, "Height must be greater or equal to previous height.");

            if (height == Height)
                return this;

            Sprite sprite = new Sprite(Width, (uint)height);

            sprite.deltaX = DeltaX;
            sprite.deltaY = DeltaY;
            sprite.offsetX = OffsetX;
            sprite.offsetY = OffsetY;

            // copy original sprite data
            System.Buffer.BlockCopy(data, 0, sprite.data, 0, (int)(Width * Height * 4u));

            int additionalHeight = height - (int)Height;

            for (int i = 0; i < additionalHeight; ++i)
            {
                int destRow = (int)Height + i;
                int sourceRow = i % (int)Height;

                System.Buffer.BlockCopy(data, sourceRow * (int)Width * 4, sprite.data,
                    destRow * (int)Width * 4, (int)Width * 4);
            }

            return sprite;
        }

        // Enlarges a sprite in height by adding full transparent pixel rows
        public Sprite ClearTo(int height)
        {
            if (height < Height)
                throw new ExceptionFreeserf(ErrorSystemType.Data, "Height must be greater or equal to previous height.");

            if (height == Height)
                return this;

            Sprite sprite = new Sprite(Width, (uint)height);

            sprite.deltaX = DeltaX;
            sprite.deltaY = DeltaY;
            sprite.offsetX = OffsetX;
            sprite.offsetY = OffsetY;

            // copy original sprite data
            System.Buffer.BlockCopy(data, 0, sprite.data, 0, (int)(Width * Height * 4u));

            // the rest is already filled with zeros cause of array initialization, so nothing to do anymore

            return sprite;
        }

        // Enlarges a sprite in width and height by adding full transparent pixel rows
        public Sprite ClearTo(int width, int height)
        {
            if (width < Width)
                throw new ExceptionFreeserf(ErrorSystemType.Data, "Width must be greater or equal to previous width.");

            if (height < Height)
                throw new ExceptionFreeserf(ErrorSystemType.Data, "Height must be greater or equal to previous height.");

            if (width == Width && height == Height)
                return this;

            Sprite sprite = new Sprite((uint)width, (uint)height);

            sprite.deltaX = DeltaX;
            sprite.deltaY = DeltaY;
            sprite.offsetX = OffsetX;
            sprite.offsetY = OffsetY;

            // copy original sprite data
            for (int i = 0; i < Height; ++i)
                System.Buffer.BlockCopy(data, i * (int)Width * 4, sprite.data, i * width * 4, (int)Width * 4);

            // the rest is already filled with zeros cause of array initialization, so nothing to do anymore

            return sprite;
        }

        public static Sprite CreateHalfMask(uint width, uint height, bool secondHalfFilled)
        {
            var sprite = new Sprite(width, height);

            int halfSize = (int)width * (int)height * 2; // * 2 because 4 bytes per pixel and height * 4 / 2 = height * 2

            int offset = secondHalfFilled ? halfSize : 0;

            System.Buffer.BlockCopy(Enumerable.Repeat((byte)0xFF, halfSize).ToArray(), 0, sprite.data, offset, halfSize);

            return sprite;
        }

        public static Sprite CreateFullMask(uint width, uint height)
        {
            var sprite = new Sprite(width, height);
            int size = (int)width * (int)height * 4;

            System.Buffer.BlockCopy(Enumerable.Repeat((byte)0xFF, size).ToArray(), 0, sprite.data, 0, size);

            return sprite;
        }

        // Apply mask to map tile sprite
        // The resulting sprite will be extended to the height of the mask
        // by repeating lines from the top of the sprite. The width of the
        // mask and the sprite must be identical.
        unsafe public virtual Sprite GetMasked(Sprite mask)
        {
            if (mask.Width > width)
            {
                throw new ExceptionFreeserf(ErrorSystemType.Data, "Failed to apply mask to sprite");
            }

            Sprite masked = new Sprite(mask);

            fixed (byte* pointer = masked.GetData())
            fixed (byte* dataPointer = data)
            fixed (byte* maskDataPointer = mask.GetData())
            {
                uint* pos = (uint*)pointer;

                uint* sBeg = (uint*)dataPointer;
                uint* sPos = sBeg;
                uint* sEnd = sBeg + (width * height);
                uint sDelta = width - masked.Width;

                uint* mPos = (uint*)maskDataPointer;

                for (uint y = 0; y < masked.Height; y++)
                {
                    for (uint x = 0; x < masked.Width; x++)
                    {
                        if (sPos >= sEnd)
                        {
                            sPos = sBeg;
                        }

                        *pos++ = *sPos++ & *mPos++;
                    }

                    sPos += sDelta;
                }
            }

            return masked;
        }

        unsafe public virtual Sprite CreateMask(Sprite other)
        {
            if ((width != other.Width) || (height != other.Height))
            {
                return null;
            }

            Sprite result = new Sprite(this);

            fixed (byte* src1Pointer = data)
            fixed (byte* src2Pointer = other.GetData())
            fixed (byte* resPointer = result.GetData())
            {
                uint* src1 = (uint*)src1Pointer;
                uint* src2 = (uint*)src2Pointer;
                uint* res = (uint*)resPointer;

                for (uint i = 0; i < width * height; ++i)
                {
                    if (*src1++ == *src2++)
                    {
                        *res++ = 0x00000000;
                    }
                    else
                    {
                        *res++ = 0xFFFFFFFF;
                    }
                }
            }

            return result;
        }

        unsafe public virtual void Fill(Sprite.Color color)
        {
            fixed (byte* pointer = data)
            {
                Color* c = (Color*)pointer;

                for (uint i = 0; i < (width * height); ++i)
                {
                    *c++ = color;
                }
            }
        }

        unsafe public virtual void FillMasked(Sprite.Color color)
        {
            fixed (byte* pointer = data)
            {
                Color* res = (Color*)pointer;

                for (uint i = 0; i < width * height; ++i)
                {
                    if ((res->Alpha & 0xFF) != 0x00)
                    {
                        *res = color;
                    }

                    res++;
                }
            }
        }

        unsafe public virtual void Add(int x, int y, Sprite other)
        {
            if (x < 0 || y < 0)
                throw new ExceptionFreeserf(ErrorSystemType.Data, "Offset is negative.");

            if (width < x + other.Width || height < y + other.Height)
                throw new ExceptionFreeserf(ErrorSystemType.Data, "Sprite can not be added at this position.");

            fixed (byte* srcPointer = other.GetData())
            fixed (byte* resPointer = data)
            {
                byte* src = srcPointer;
                byte* dst = resPointer + (x + y * width) * 4;

                for (int r = 0; r < other.height; ++r)
                {
                    System.Buffer.MemoryCopy(src, dst, other.width * 4, other.width * 4);
                    src += other.width * 4;
                    dst += width * 4;
                }
            }
        }

        unsafe public virtual void Add(Sprite other)
        {
            if (width != other.Width || height != other.Height)
            {
                return;
            }

            fixed (byte* srcPointer = other.GetData())
            fixed (byte* resPointer = data)
            {
                uint* src = (uint*)srcPointer;
                uint* res = (uint*)resPointer;

                for (uint i = 0; i < width * height; ++i)
                {
                    *res++ += *src++;
                }
            }
        }

        unsafe public virtual void Delete(Sprite other)
        {
            if (width != other.Width || height != other.Height)
            {
                return;
            }

            fixed (byte* srcPointer = other.GetData())
            fixed (byte* resPointer = data)
            {
                uint* src = (uint*)srcPointer;
                uint* res = (uint*)resPointer;

                for (uint i = 0; i < width * height; ++i)
                {
                    if (*src++ == 0xFFFFFFFF)
                    {
                        *res = 0x00000000;
                    }

                    ++res;
                }
            }
        }

        static byte Unmultiply(byte colorComponent, uint alpha)
        {
            return (byte)(0xFF * colorComponent / alpha);
        }

        static byte Blend(byte back, byte front, uint alpha)
        {
            return (byte)(((front * alpha) + (back * (0xFF - alpha))) / 0xFF);
        }

        unsafe public virtual void Blend(Sprite other)
        {
            if (width != other.Width || height != other.Height)
            {
                return;
            }

            fixed (byte* dataPointer = data)
            fixed (byte* otherDataPointer = other.GetData())
            {
                Color* c = (Color*)dataPointer;
                Color* o = (Color*)otherDataPointer;

                for (uint i = 0; i < width * height; ++i)
                {
                    uint alpha = o->Alpha;

                    if (alpha == 0x00)
                    {
                        ++c;
                        ++o;
                        continue;
                    }

                    if (alpha == 0xFF)
                    {
                        *c++ = *o++;
                        continue;
                    }

                    byte backR = c->Red;
                    byte backG = c->Green;
                    byte backB = c->Blue;

                    byte frontR = Unmultiply(o->Red, alpha);
                    byte frontG = Unmultiply(o->Green, alpha);
                    byte frontB = Unmultiply(o->Blue, alpha);

                    uint R = Blend(backR, frontR, alpha);
                    uint G = Blend(backG, frontG, alpha);
                    uint B = Blend(backB, frontB, alpha);

                    *c++ = new Color { Blue = (byte)B, Green = (byte)G, Red = (byte)R, Alpha = 0xFF };
                    ++o;
                }
            }

            deltaX = other.DeltaX;
            deltaY = other.DeltaY;
        }

        unsafe public virtual void MakeAlphaMask()
        {
            byte min = 0xFF;

            fixed (byte* pointer = data)
            {
                Color* c = (Color*)pointer;

                for (uint i = 0; i < width * height; ++i)
                {
                    if (c->Alpha != 0x00)
                    {
                        c->Alpha = (byte)(0xFF - (int)((0.21 * c->Red) + (0.72 * c->Green) + (0.07 * c->Blue)));
                        c->Red = 0;
                        c->Green = 0;
                        c->Blue = 0;
                        min = Math.Min(min, c->Alpha);
                    }

                    ++c;
                }
            }

            fixed (byte* pointer = data)
            {
                Color* c = (Color*)pointer;

                for (uint i = 0; i < width * height; ++i)
                {
                    if (c->Alpha != 0x00)
                    {
                        c->Alpha = (byte)(c->Alpha - min);
                    }

                    ++c;
                }
            }
        }

        unsafe public virtual void Stick(Sprite sticker, uint dx, uint dy)
        {
            fixed (byte* pointer = data)
            fixed (byte* stickerPointer = sticker.GetData())
            {
                Color* baseColor = (Color*)pointer;
                Color* stickerColor = (Color*)stickerPointer;
                uint w = Math.Min(width, sticker.Width);
                uint h = Math.Min(height, sticker.Height);

                baseColor += dy * width;

                for (uint y = 0; y < w; ++y)
                {
                    baseColor += dx;

                    for (uint x = 0; x < h; ++x)
                    {
                        Color pixel = *stickerColor++;

                        if ((pixel.Alpha & 0xFF) != 0x00)
                        {
                            *baseColor = pixel;
                        }

                        ++baseColor;
                    }
                }
            }

            deltaX = sticker.DeltaX;
            deltaY = sticker.DeltaY;
        }

        // Calculate hash of sprite identifier.
        public static ulong CreateID(ulong resource, ulong index,
                          ulong maskResource, ulong maskIndex,
                          Color color)
        {
            // 0xFF00000000000000
            ulong result = (resource & 0xFF) << 56;
            // 0x00FFF00000000000
            result |= (index & 0xFFF) << 44;
            // 0x00000FF000000000
            result |= (maskResource & 0xFF) << 36;
            // 0x0000000FFF000000
            result |= (maskIndex & 0xFFF) << 24;
            // 0x0000000000FF0000
            result |= (ulong)(color.Red & 0xFF) << 16;
            // 0x000000000000FF00
            result |= (ulong)(color.Green & 0xFF) << 8;
            // 0x00000000000000FF
            result |= (ulong)(color.Blue & 0xFF) << 0;

            return result;
        }

        protected void Create(uint width, uint height)
        {
            this.width = width;
            this.height = height;
            data = new byte[width * height * 4];
        }


        #region IDisposable Support

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    data = null;
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

    public class Animation
    {
        public byte Sprite;
        public int X;
        public int Y;
    }

    public abstract class DataSource
    {
        protected string path;
        protected bool loaded;
        protected List<List<Animation>> animationTable = new List<List<Animation>>();
        readonly Dictionary<Data.Resource, Dictionary<uint, SpriteInfo>> spriteInfoCache = new Dictionary<Data.Resource, Dictionary<uint, SpriteInfo>>();

        public DataSource(string path)
        {
            this.path = path;
            loaded = false;
        }

        public static bool DosGraphics(DataSource source)
        {
            if (source is DataSourceDos)
                return true;

            if (source is DataSourceMixed)
                return (source as DataSourceMixed).UseDosGraphics;

            return false;
        }

        public static bool DosSounds(DataSource source)
        {
            if (source is DataSourceDos)
                return true;

            if (source is DataSourceMixed)
                return (source as DataSourceMixed).UseDosSounds;

            return false;
        }

        public static bool DosMusic(DataSource source)
        {
            if (source is DataSourceDos)
                return true;

            if (source is DataSourceMixed)
                return (source as DataSourceMixed).UseDosMusic;

            return false;
        }

        public abstract string Name { get; }
        public virtual string Path => path;
        public virtual bool IsLoaded => loaded;
        public virtual uint Scale => 0;
        public virtual uint BPP => 0;

        public abstract bool Check();
        public abstract bool CheckMusic();
        public abstract bool CheckSound();
        public abstract bool CheckGraphics();
        public abstract DataLoadResult Load();

        public SpriteInfo GetSpriteInfo(Data.Resource resource, uint index)
        {
            if (spriteInfoCache.ContainsKey(resource) && spriteInfoCache[resource] != null && spriteInfoCache[resource].ContainsKey(index))
            {
                return spriteInfoCache[resource][index];
            }

            var sprite = GetSprite(resource, index, Sprite.Color.Transparent);

            SpriteInfo spriteInfo = null;

            if (sprite != null)
            {
                spriteInfo = new SpriteInfo()
                {
                    Width = (int)sprite.Width,
                    Height = (int)sprite.Height,
                    OffsetX = sprite.OffsetX,
                    OffsetY = sprite.OffsetY,
                    DeltaX = sprite.DeltaX,
                    DeltaY = sprite.DeltaY
                };
            }

            if (!spriteInfoCache.ContainsKey(resource))
                spriteInfoCache.Add(resource, new Dictionary<uint, SpriteInfo>() { { index, spriteInfo } });
            else
                spriteInfoCache[resource].Add(index, spriteInfo);

            return spriteInfo;
        }

        public virtual Sprite GetSprite(Data.Resource resource, uint index, Sprite.Color color)
        {
            if (index >= Data.GetResourceCount(resource))
            {
                return null;
            }

            Tuple<Sprite, Sprite> ms = GetSpriteParts(resource, index);
            Sprite mask = ms.Item1;
            Sprite image = ms.Item2;

            if (mask != null)
            {
                mask.FillMasked(color);

                if (image != null)
                {
                    mask.Blend(image);
                }

                return mask;
            }

            return image;
        }

        public abstract Tuple<Sprite, Sprite> GetSpriteParts(Data.Resource resource, uint index);

        public virtual int GetAnimationPhaseCount(int animation)
        {
            if (animation >= animationTable.Count)
            {
                Log.Error.Write(ErrorSystemType.Data, $"Failed to get phase count for animation #{animation} (got only {animationTable.Count} animations)");

                return 0;
            }

            if (animationTable[animation] == null)
                throw new ExceptionFreeserf(ErrorSystemType.Data, $"Animation table entry at index {animation} not initialized.");

            return animationTable[animation].Count;
        }

        public virtual Animation GetAnimation(int animation, int phase)
        {
            phase >>= 3;

            if (animation >= animationTable.Count || animationTable[animation] == null ||
                phase >= animationTable[animation].Count)
            {
                if (animationTable[animation] == null)
                    throw new ExceptionFreeserf(ErrorSystemType.Data, $"Animation table entry at index {animation} not initialized.");

                Log.Error.Write(ErrorSystemType.Data, $"Failed to get animation #{animation} phase #{phase} (got only {animationTable[animation].Count} phases)");

                return new Animation { Sprite = 0, X = 0, Y = 0 };
            }

            return animationTable[animation][phase];
        }

        public abstract Buffer GetSound(uint index);
        public abstract Buffer GetMusic(uint index);

        public bool CheckFile(string path)
        {
            return File.Exists(path) && !Directory.Exists(path);
        }

        protected Tuple<Sprite, Sprite> SeparateSprites(Sprite s1, Sprite s2)
        {
            if (s1 == null || s2 == null)
            {
                return Tuple.Create<Sprite, Sprite>(null, null);
            }

            Sprite filled = s1.CreateMask(s2);
            Sprite masked = s1.GetMasked(filled);
            masked.MakeAlphaMask();
            s1.Delete(filled);
            s1.Stick(masked, 0, 0);

            return Tuple.Create(filled, s1);
        }
    }
}
