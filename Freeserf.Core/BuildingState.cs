using System;

namespace Freeserf
{
    using Serialize;
    using MapPos = UInt32;
    using word = UInt16;

    [Flags]
    public enum BuildingStateFlags : byte
    {
        None = 0x00,
        /// <summary>
        /// The building is under construction
        /// </summary>
        Constructing = 0x01,
        /// <summary>
        /// A sound effect is playing
        /// </summary>
        PlayingSfx = 0x02,
        /// <summary>
        /// The Building is active
        /// </summary>
        Active = 0x04,
        /// <summary>
        /// The building is burning
        /// </summary>
        Burning = 0x08,
        /// <summary>
        /// The building has a holder
        /// </summary>
        Holder = 0x10,
        /// <summary>
        /// Serf was requested
        /// </summary>
        SerfRequested = 0x20,
        /// <summary>
        /// Failed to request a serf
        /// </summary>
        SerfRequestFailed = 0x40,
        /// <summary>
        /// A sword has been crafted and the next
        /// shield can be crafted without resources
        /// Only for weapon makers!
        /// </summary>
        FreeShieldPossible = 0x80,
    }

    [DataClass]
    internal class BuildingState : State
    {
        private Building.Type type = Building.Type.None;
        private MapPos position = Global.INVALID_MAPPOS;
        private word flag = 0;
        private byte player = 0;
        private BuildingStateFlags flags = BuildingStateFlags.Constructing; // new buildings are unfinished
        private byte threatLevel = 0;
        private word progress = 0;
        private word firstKnight = 0;
        private word inventoryOrTickOrLevel = 0;

        public BuildingState()
        {
            Stock.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(Stock)); };
        }

        public override void ResetDirtyFlag()
        {
            lock (dirtyLock)
            {
                Stock.ResetDirtyFlag();
                ResetDirtyFlagUnlocked();
            }
        }

        /// <summary>
        /// Type of this building
        /// </summary>
        public Building.Type Type
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
        /// Position of this building
        /// </summary>
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
        /// Index of flag connected to this building
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
        /// Owner of this building
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
        public BuildingStateFlags Flags
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
        /// <summary>
        /// Threat level of military building
        /// </summary>
        public byte ThreatLevel
        {
            get => threatLevel;
            set
            {
                if (threatLevel != value)
                {
                    threatLevel = value;
                    MarkPropertyAsDirty(nameof(ThreatLevel));
                }
            }
        }
        /// <summary>
        /// Construction progress
        /// </summary>
        public word Progress
        {
            get => progress;
            set
            {
                if (progress != value)
                {
                    progress = value;
                    MarkPropertyAsDirty(nameof(Progress));
                }
            }
        }
        /// <summary>
        /// Index of first knight in military building
        /// </summary>
        public word FirstKnight
        {
            get => firstKnight;
            set
            {
                if (firstKnight != value)
                {
                    firstKnight = value;
                    MarkPropertyAsDirty(nameof(FirstKnight));
                }
            }
        }
        /// <summary>
        /// Can be one of 3 things:
        /// - Inventory index (for stocks and castle)
        /// - Tick (for burning buildings)
        /// - Level (for new large buildings that need terrain leveling)
        /// </summary>
        public word InventoryOrTickOrLevel
        {
            get => inventoryOrTickOrLevel;
            set
            {
                if (inventoryOrTickOrLevel != value)
                {
                    inventoryOrTickOrLevel = value;
                    MarkPropertyAsDirty(nameof(InventoryOrTickOrLevel));
                }
            }
        }
        /// <summary>
        /// Stocks of this building
        /// </summary>
        public DirtyArray<Building.Stock> Stock { get; } = new DirtyArray<Building.Stock>
        (
            new Building.Stock(), new Building.Stock()
        );

        /// <summary>
        /// Inventory
        /// </summary>
        [Ignore]
        public word Inventory
        {
            get => InventoryOrTickOrLevel;
            set => InventoryOrTickOrLevel = value;
        }
        /// <summary>
        /// Tick
        /// </summary>
        [Ignore]
        public word Tick
        {
            get => InventoryOrTickOrLevel;
            set => InventoryOrTickOrLevel = value;
        }
        /// <summary>
        /// Level
        /// </summary>
        [Ignore]
        public word Level
        {
            get => InventoryOrTickOrLevel;
            set => InventoryOrTickOrLevel = value;
        }

        [Ignore]
        public bool Constructing
        {
            get => Flags.HasFlag(BuildingStateFlags.Constructing);
            set
            {
                if (value)
                    Flags |= BuildingStateFlags.Constructing;
                else
                    Flags &= ~BuildingStateFlags.Constructing;
            }
        }
        [Ignore]
        public bool PlayingSfx // Note: This is used for weapon smiths as a flag for free shields. :(
        {
            get => Flags.HasFlag(BuildingStateFlags.PlayingSfx);
            set
            {
                if (value)
                    Flags |= BuildingStateFlags.PlayingSfx;
                else
                    Flags &= ~BuildingStateFlags.PlayingSfx;
            }
        }
        [Ignore]
        public bool Active
        {
            get => Flags.HasFlag(BuildingStateFlags.Active);
            set
            {
                if (value)
                    Flags |= BuildingStateFlags.Active;
                else
                    Flags &= ~BuildingStateFlags.Active;
            }
        }
        [Ignore]
        public bool Burning
        {
            get => Flags.HasFlag(BuildingStateFlags.Burning);
            set
            {
                if (value)
                    Flags |= BuildingStateFlags.Burning;
                else
                    Flags &= ~BuildingStateFlags.Burning;
            }
        }
        [Ignore]
        public bool Holder
        {
            get => Flags.HasFlag(BuildingStateFlags.Holder);
            set
            {
                if (value)
                    Flags |= BuildingStateFlags.Holder;
                else
                    Flags &= ~BuildingStateFlags.Holder;
            }
        }
        [Ignore]
        public bool SerfRequested
        {
            get => Flags.HasFlag(BuildingStateFlags.SerfRequested);
            set
            {
                if (value)
                    Flags |= BuildingStateFlags.SerfRequested;
                else
                    Flags &= ~BuildingStateFlags.SerfRequested;
            }
        }
        [Ignore]
        public bool SerfRequestFailed
        {
            get => Flags.HasFlag(BuildingStateFlags.SerfRequestFailed);
            set
            {
                if (value)
                    Flags |= BuildingStateFlags.SerfRequestFailed;
                else
                    Flags &= ~BuildingStateFlags.SerfRequestFailed;
            }
        }
        [Ignore]
        public bool FreeShieldPossible
        {
            get => Flags.HasFlag(BuildingStateFlags.FreeShieldPossible);
            set
            {
                if (value)
                    Flags |= BuildingStateFlags.FreeShieldPossible;
                else
                    Flags &= ~BuildingStateFlags.FreeShieldPossible;
            }
        }
    }
}
