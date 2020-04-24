using System;

namespace Freeserf
{
    using Serialize;
    using MapPos = UInt32;
    using word = UInt16;

    [Flags]
    public enum SerfStateFlags : byte
    {
        None = 0x00,
        /// <summary>
        /// A sound effect is playing
        /// </summary>
        PlayingSfx = 0x01,
    }

    internal class SerfState : State
    {
        private Serf.Type type = Serf.Type.None;
        private byte player = 0;
        private byte animation = 0;
        private int counter = 0;
        private MapPos position = Global.INVALID_MAPPOS;
        private word tick = 0;
        private SerfStateFlags flags = SerfStateFlags.None;
        private byte stateIndex = 0;

        /// <summary>
        /// Type of this building
        /// </summary>
        [Data]
        public Serf.Type Type
        {
            get => type;
            set
            {
                if (type != value)
                {
                    type = value;
                    MarkPropertyAsDirty(nameof(Type));
                }
            }
        }

        /// <summary>
        /// Owner of this building
        /// </summary>
        [Data]
        public byte Player
        {
            get => player;
            set
            {
                if (player != value)
                {
                    player = value;
                    MarkPropertyAsDirty(nameof(Player));
                }
            }
        }

        /// <summary>
        /// Current animation index
        /// </summary>
        [Data]
        public byte Animation
        {
            get => animation;
            set
            {
                if (animation != value)
                {
                    animation = value;
                    MarkPropertyAsDirty(nameof(Animation));
                }
            }
        }

        /// <summary>
        /// Current animation counter
        /// </summary>
        [Data]
        public int Counter
        {
            get => counter;
            set
            {
                if (counter != value)
                {
                    counter = value;
                    MarkPropertyAsDirty(nameof(Counter));
                }
            }
        }

        /// <summary>
        /// Position of this building
        /// </summary>
        [Data]
        public MapPos Position
        {
            get => position;
            set
            {
                if (position != value)
                {
                    position = value;
                    MarkPropertyAsDirty(nameof(Position));
                }
            }
        }

        /// <summary>
        /// Current tick
        /// </summary>
        [Data]
        public word Tick
        {
            get => tick;
            set
            {
                if (tick != value)
                {
                    tick = value;
                    MarkPropertyAsDirty(nameof(Tick));
                }
            }
        }

        [Data]
        public SerfStateFlags Flags
        {
            get => flags;
            set
            {
                if (flags != value)
                {
                    flags = value;
                    MarkPropertyAsDirty(nameof(Flags));
                }
            }
        }

        [Data]
        public byte StateIndex
        {
            get => stateIndex;
            set
            {
                if (stateIndex != value)
                {
                    stateIndex = value;
                    MarkPropertyAsDirty(nameof(StateIndex));
                }
            }
        }

        public Serf.State State
        {
            get => (Serf.State)StateIndex;
            set => StateIndex = (byte)value;
        }

        public bool PlayingSfx
        {
            get => Flags.HasFlag(SerfStateFlags.PlayingSfx);
            set
            {
                if (value)
                    Flags |= SerfStateFlags.PlayingSfx;
                else
                    Flags &= ~SerfStateFlags.PlayingSfx;
            }
        }
    }
}
