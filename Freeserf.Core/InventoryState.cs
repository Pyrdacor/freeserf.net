using System;

namespace Freeserf
{
    using Serialize;
    using dword = UInt32;
    using ResourceMap = Serialize.DirtyMap<Resource.Type, UInt32>;
    using SerfMap = Serialize.DirtyMap<Serf.Type, UInt32>;
    using word = UInt16;

    [DataClass]
    internal class InventoryState : State
    {
        private byte player = 0;
        private word flag = 0;
        private word building = 0;
        private dword genericCount = 0;
        private byte resourceDir = 0;

        public InventoryState()
        {
            Resources.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(Resources)); };
            Serfs.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(Serfs)); };
            OutQueue.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(OutQueue)); };
        }

        public override void ResetDirtyFlag()
        {
            lock (dirtyLock)
            {
                Resources.Dirty = false;
                Serfs.Dirty = false;
                OutQueue.Dirty = false;

                ResetDirtyFlagUnlocked();
            }
        }

        /// <summary>
        /// Owner of this inventory
        /// </summary>
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
        /// Index of flag connected to this inventory
        /// </summary>
        public word Flag
        {
            get => flag;
            set
            {
                if (flag != value)
                {
                    flag = value;
                    MarkPropertyAsDirty(nameof(Flag));
                }
            }
        }
        /// <summary>
        /// Index of building containing this inventory
        /// </summary>
        public word Building
        {
            get => building;
            set
            {
                if (building != value)
                {
                    building = value;
                    MarkPropertyAsDirty(nameof(Building));
                }
            }
        }
        /// <summary>
        /// Count of generic serfs
        /// </summary>
        public dword GenericCount
        {
            get => genericCount;
            set
            {
                if (genericCount != value)
                {
                    genericCount = value;
                    MarkPropertyAsDirty(nameof(GenericCount));
                }
            }
        }
        /// <summary>
        /// Count of resources
        /// </summary>
        public ResourceMap Resources { get; } = new ResourceMap();
        /// <summary>
        /// Indices to serfs of each type
        /// </summary>
        public SerfMap Serfs { get; } = new SerfMap();
        /// <summary>
        /// Resources waiting to be moved out
        /// </summary>
        public DirtyArray<Inventory.OutQueue> OutQueue { get; } = new DirtyArray<Inventory.OutQueue>
        (
            new Inventory.OutQueue(), new Inventory.OutQueue()
        );
        /// <summary>
        /// Directions for resources and serfs
        /// Bit 0-1: Resource direction
        /// Bit 2-3: Serf direction
        /// </summary>
        public byte ResourceDirection
        {
            get => resourceDir;
            set
            {
                if (resourceDir != value)
                {
                    resourceDir = value;
                    MarkPropertyAsDirty(nameof(ResourceDirection));
                }
            }
        }

        [Ignore]
        public Inventory.Mode ResourceMode
        {
            get => (Inventory.Mode)(ResourceDirection & 0x03);
            set => ResourceDirection = (byte)((ResourceDirection & 0xFC) | (byte)value);
        }
        [Ignore]
        public Inventory.Mode SerfMode
        {
            get => (Inventory.Mode)((ResourceDirection >> 2) & 0x03);
            set => ResourceDirection = (byte)((ResourceDirection & 0xF3) | ((byte)value << 2));
        }

        public bool HasAnyOutMode()
        {
            return (ResourceDirection & 0x0A) != 0;
        }
    }
}
