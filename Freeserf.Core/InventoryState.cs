using System;

namespace Freeserf
{
    using Serialize;
    using word = UInt16;
    using dword = UInt32;
    using SerfMap = Serialize.DirtyMap<Serf.Type, UInt32>;
    using ResourceMap = Serialize.DirtyMap<Resource.Type, UInt32>;

    [DataClass]
    internal class InventoryState : State
    {
        private byte player = 0;
        private word flag = 0;
        private word building = 0;
        private dword genericCount = 0;
        private readonly ResourceMap resources = new ResourceMap();
        private readonly SerfMap serfs = new SerfMap();
        private readonly DirtyArray<Inventory.OutQueue> outQueue = new DirtyArray<Inventory.OutQueue>
        (
            new Inventory.OutQueue(), new Inventory.OutQueue()
        );
        private byte resourceDir = 0;

        public InventoryState()
        {
            resources.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(Resources)); };
            serfs.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(Serfs)); };
            outQueue.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(OutQueue)); };
        }

        public override void ResetDirtyFlag()
        {
            lock (dirtyLock)
            {
                resources.Dirty = false;
                serfs.Dirty = false;
                outQueue.Dirty = false;

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
        public ResourceMap Resources => resources;
        /// <summary>
        /// Indices to serfs of each type
        /// </summary>
        public SerfMap Serfs => serfs;
        /// <summary>
        /// Resources waiting to be moved out
        /// </summary>
        public DirtyArray<Inventory.OutQueue> OutQueue => outQueue;   
        /// <summary>
        /// Directions for resources and serfs
        /// Bit 0-1: Resource direction
        /// Bit 2-3: Serf direction
        /// </summary>
        public byte ResourceDir
        {
            get => resourceDir;
            set
            {
                if (resourceDir != value)
                {
                    resourceDir = value;
                    MarkPropertyAsDirty(nameof(ResourceDir));
                }
            }
        }

        [Ignore]
        public Inventory.Mode ResourceMode
        {
            get => (Inventory.Mode)(ResourceDir & 0x03);
            set => ResourceDir = (byte)((ResourceDir & 0xFC) | (byte)value);
        }
        [Ignore]
        public Inventory.Mode SerfMode
        {
            get => (Inventory.Mode)((ResourceDir >> 2) & 0x03);
            set => ResourceDir = (byte)((ResourceDir & 0xF3) | ((byte)value << 2));
        }

        public bool HaveAnyOutMode()
        {
            return (ResourceDir & 0x0A) != 0;
        }
    }
}
