using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Freeserf
{
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
        }

        protected int deltaX;
        protected int deltaY;
        protected int offsetX;
        protected int offsetY;
        protected uint width;
        protected uint height;
        protected byte[] data;

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

        // Apply mask to map tile sprite
        // The resulting sprite will be extended to the height of the mask
        // by repeating lines from the top of the sprite. The width of the
        // mask and the sprite must be identical.
        unsafe public virtual Sprite GetMasked(Sprite mask)
        {
            if (mask.Width > width)
            {
                throw new ExceptionFreeserf("Failed to apply mask to sprite");
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

                    *c++ = new Color { Blue = (byte)B, Green = (byte)G, Red = (byte)R, Alpha = 0xFF};
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

        public DataSource(string path)
        {
            this.path = path;
            loaded = false;
        }

        public abstract string Name { get; }
        public virtual string Path => path;
        public virtual bool IsLoaded => loaded;
        public virtual uint Scale => 0;
        public virtual uint BPP => 0;

        public abstract bool Check();
        public abstract bool Load();

        public virtual Sprite GetSprite(Data.Resource resource, int index, Sprite.Color color)
        {
            if (index >= Data.GetAssetCount(resource))
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

        public abstract Tuple<Sprite, Sprite> GetSpriteParts(Data.Resource resource, int index);

        public virtual int GetAnimationPhaseCount(int animation)
        {
            if (animation >= animationTable.Count)
            {
                Log.Error.Write("data", $"Failed to get phase count for animation #{animation} (got only {animationTable.Count} animations)");

                return 0;
            }

            if (animationTable[animation] == null)
                throw new ExceptionFreeserf($"Animation table entry at index {animation} not initialized.");

            return animationTable[animation].Count;
        }

        public virtual Animation GetAnimation(int animation, int phase)
        {
            phase >>= 3;

            if (animation >= animationTable.Count || animationTable[animation] == null ||
                phase >= animationTable[animation].Count)
            {
                if (animationTable[animation] == null)
                    throw new ExceptionFreeserf($"Animation table entry at index {animation} not initialized.");

                Log.Error.Write("data", $"Failed to get animation #{animation} phase #{phase} (got only {animationTable[animation].Count} phases)");

                return new Animation { Sprite = 0, X = 0, Y = 0 };
            }

            return animationTable[animation][phase];
        }

        public abstract Buffer GetSound(int index);
        public abstract Buffer GetMusic(int index);

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
