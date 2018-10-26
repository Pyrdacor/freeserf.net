using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    public abstract class DataSourceLegacy : DataSource
    {
        protected struct AnimationPhase
        {
            public byte Sprite;
            public sbyte X;
            public sbyte Y;
        }

        public DataSourceLegacy(string path)
            : base(path)
        {

        }

        unsafe protected bool LoadAnimationTable(Buffer data)
        {
            if (data == null)
            {
                return false;
            }

            // The serf animation table is stored in big endian order in the data file.
            data.SetEndianess(Endian.Endianess.Big);

            // * Starts with 200 uint32s that are offsets from the start
            // of this table to an animation table (one for each animation).
            // * The animation tables are of varying lengths.
            // Each entry in the animation table is three bytes long.
            // First byte is used to determine the serf body sprite.
            // Second byte is a signed horizontal sprite offset.
            // Third byte is a signed vertical offset.

            fixed (byte* pointer = data.Data)
            {
                uint* animationBlock = (uint*)pointer;

                // Endianess convert from big endian.
                for (uint i = 0; i < 200; ++i)
                {
                    animationBlock[i] = Endian.Betoh(animationBlock[i]);
                }

                uint[] sizes = new uint[200];

                for (uint i = 0; i < 200; ++i)
                {
                    uint a = animationBlock[i];
                    uint next = data.Size;

                    for (int j = 0; j < 200; ++j)
                    {
                        uint b = animationBlock[j];

                        if (b > a)
                        {
                            next = Math.Min(next, b);
                        }
                    }

                    sizes[i] = (next - a) / 3;
                }

                for (uint i = 0; i < 200; ++i)
                {
                    uint offset = animationBlock[i];

                    AnimationPhase* anims = (AnimationPhase*)(byte*)animationBlock + offset;
                    List<Animation> animations = new List<Animation>();

                    for (uint j = 0; j < sizes[i]; ++j)
                    {
                        Animation a = new Animation();

                        a.Sprite = anims[j].Sprite;
                        a.X = anims[j].X;
                        a.Y = anims[j].Y;

                        animations.Add(a);
                    }

                    animationTable.Add(animations);
                }
            }

            return true;
        }
    }
}
