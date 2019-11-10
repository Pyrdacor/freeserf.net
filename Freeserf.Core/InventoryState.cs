using System;
using System.Collections.Generic;

namespace Freeserf
{
    using Serialize;
    using word = UInt16;
    using dword = UInt32;
    using SerfMap = Dictionary<Serf.Type, UInt32>;
    using ResourceMap = Dictionary<Resource.Type, UInt32>;

    [DataClass]
    internal class InventoryState
    {
        [Ignore]
        public bool Dirty
        {
            get;
            internal set;
        }

        /// <summary>
        /// Owner of this inventory
        /// </summary>
        public byte Player { get; set; } = 0;
        /// <summary>
        /// Index of flag connected to this inventory
        /// </summary>
        public word Flag { get; set; } = 0;
        /* Index of building containing this inventory */
        /// <summary>
        /// Index of building containing this inventory
        /// </summary>
        public word Building { get; set; } = 0;
        /// <summary>
        /// Count of generic serfs
        /// </summary>
        public dword GenericCount { get; set; } = 0;
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
        public Inventory.OutQueue[] OutQueue { get; } = new Inventory.OutQueue[2]
        {
            new Inventory.OutQueue(), new Inventory.OutQueue()
        };        
        /// <summary>
        /// Directions for resources and serfs
        /// Bit 0-1: Resource direction
        /// Bit 2-3: Serf direction
        /// </summary>
        public byte ResourceDir { get; set; } = 0;

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
