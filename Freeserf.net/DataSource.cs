using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    // Sprite object.
    // Contains BGRA data.
    public class Sprite
    {
        public struct Color
        {
            byte blue;
            byte green;
            byte red;
            byte alpha;
        }

        protected int deltaX;
        protected int deltaY;
        protected int offsetX;
        protected int offsetY;
        protected int width;
        protected int height;
        protected byte[] data;

        public Sprite()
        {

        }

        public Sprite(uint w, uint h)
        {

        }

        public virtual byte[] GetData()
        {
            return data;
        }

        public virtual int Width => width;
        public virtual int Height => height;
        public virtual int DeltaX => deltaX;
        public virtual int DeltaY => deltaY;
        public virtual int OffsetX => offsetX;
        public virtual int OffsetY => offsetY;

        public virtual Sprite GetMasked(Sprite mask)
        {

        }

        public virtual Sprite CreateMask(Sprite other)
        {

        }

        public virtual void fill(Sprite::Color color)
        {

        }

        public virtual void fill_masked(Sprite::Color color)
        {

        }

        public virtual void add(PSprite other)
        {

        }

        public virtual void del(PSprite other)
        {

        }

        public virtual void blend(PSprite other)
        {

        }

        public virtual void make_alpha_mask()
        {

        }

        public virtual void Stick(Sprite sticker, uint x, uint y)
        {

        }

        public static ulong CreateID(ulong resource, ulong index,
                          ulong maskResource, ulong maskIndex,
                          Color color)
        {
        }

        protected void Create(int width, int height)
        {

        }
    }

    public class Animation
    {
        public byte Sprite;
        public int X;
        public int Y;
    }

    public abstract class DataSource
    {
        public:
      typedef std::tuple<PSprite, PSprite> MaskImage;

        protected string path;
        protected bool loaded;
        protected List<List<Animation>> animationTable = new List<List<Animation>>();

        public DataSource(string path)
        {

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

        }

        public abstract Tuple<Sprite, Sprite> GetSpriteParts(Data.Resource resource, int index);

        public virtual int GetAnimationPhaseCount(int animation)
        {

        }

        public virtual Animation GetAnimation(int animation, int phase)
        {

        }

        public abstract Buffer GetSound(int index);
        public abstract Buffer GetMusic(int index);

        public bool CheckFile(string path)
        {

        }

        protected Tuple<Sprite, Sprite> separate_sprites(PSprite s1, PSprite s2);
    }
}
