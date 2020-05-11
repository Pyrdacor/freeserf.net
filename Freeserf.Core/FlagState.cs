/*
 * FlagState.cs - Flag state
 *
 * Copyright (C) 2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Freeserf
{
    using Serialize;
    using FlagPath = Flag.FlagPath;
    using MapPos = UInt32;
    using OtherEndpointPath = Flag.OtherEndpointPath;
    using ResourceSlot = Flag.ResourceSlot;
    using word = UInt16;

    [Flags]
    public enum PathConnectionFlags : byte
    {
        None = 0x00,
        /// <summary>
        /// Has connected road right
        /// </summary>
        ConnectedRight = 0x01,
        /// <summary>
        /// Has connected road down right
        /// </summary>
        ConnectedDownRight = 0x02,
        /// <summary>
        /// Has connected road down
        /// </summary>
        ConnectedDown = 0x04,
        /// <summary>
        /// Has connected road left
        /// </summary>
        ConnectedLeft = 0x08,
        /// <summary>
        /// Has connected road up left
        /// </summary>
        ConnectedUpLeft = 0x10,
        /// <summary>
        /// Has connected road up
        /// </summary>
        ConnectedUp = 0x20,
        // Note: Bit 6-7 of the byte are reserved for owner index (0-3)
    }

    [Flags]
    public enum EndPointFlags : byte
    {
        None = 0x00,
        /// <summary>
        /// Is land road right (otherwise water)
        /// </summary>
        IsLandRight = 0x01,
        /// <summary>
        /// Is land road down right (otherwise water)
        /// </summary>
        IsLandDownRight = 0x02,
        /// <summary>
        /// Is land road down (otherwise water)
        /// </summary>
        IsLandDown = 0x04,
        /// <summary>
        /// Is land road left (otherwise water)
        /// </summary>
        IsLandLeft = 0x08,
        /// <summary>
        /// Is land road up left (otherwise water)
        /// </summary>
        IsLandUpLeft = 0x10,
        /// <summary>
        /// Is land road up (otherwise water)
        /// </summary>
        IsLandUp = 0x20,
        /// <summary>
        // Has connected building
        /// </summary>
        HasConnectedBuilding = 0x40,
        /// <summary>
        // Has unscheduled resources
        /// </summary>
        HasUnscheduledResources = 0x80,
    }

    [Flags]
    public enum TransporterFlags : byte
    {
        None = 0x00,
        /// <summary>
        /// Has transporter at road right
        /// </summary>
        IsLandRight = 0x01,
        /// <summary>
        /// Has transporter at road down right
        /// </summary>
        IsLandDownRight = 0x02,
        /// <summary>
        /// Has transporter at road down
        /// </summary>
        IsLandDown = 0x04,
        /// <summary>
        /// Has transporter at road left
        /// </summary>
        IsLandLeft = 0x08,
        /// <summary>
        /// Has transporter at road up left
        /// </summary>
        IsLandUpLeft = 0x10,
        /// <summary>
        /// Has transporter at road up
        /// </summary>
        IsLandUp = 0x20,
        /// <summary>
        // Reserve (unused at the moment)
        /// </summary>
        Reserve = 0x40,
        /// <summary>
        // Serf request failed
        /// </summary>
        SerfRequestFailed = 0x80,
    }

    [Flags]
    public enum FlagBuildingFlags : byte
    {
        None = 0x00,
        /// <summary>
        /// Flag has an associated inventory building
        /// </summary>
        HasInventory = 0x01,
        /// <summary>
        /// Associated invertory building accepts serfs
        /// </summary>
        InventoryAcceptsSerfs = 0x02,
        /// <summary>
        /// Associated invertory building accepts resources
        /// </summary>
        InventoryAcceptsResources = 0x04,
        // Rest is unused at the moment
    }

    internal static class FlagDirectionExtensions
    {
        public static PathConnectionFlags ToPathConnectionFlag(this Direction direction)
        {
            return (PathConnectionFlags)Misc.Bit((int)direction);
        }

        public static EndPointFlags ToEndPointFlag(this Direction direction)
        {
            return (EndPointFlags)Misc.Bit((int)direction);
        }

        public static TransporterFlags ToTransporterFlag(this Direction direction)
        {
            return (TransporterFlags)Misc.Bit((int)direction);
        }
    }

    [DataClass]
    internal class FlagState : State
    {
        private MapPos position = Global.INVALID_MAPPOS;
        private Direction searchDirection = Direction.None;
        private word searchNumber = 0;
        private byte pathConnections = 0;
        private EndPointFlags endPointFlags = EndPointFlags.None;
        private TransporterFlags transporterFlags = TransporterFlags.None;
        private FlagBuildingFlags flagBuildingFlags = FlagBuildingFlags.None;

        public FlagState()
        {
            FlagPaths.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(FlagPaths)); };
            OtherEndpoints.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(OtherEndpoints)); };
            OtherEndpointPaths.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(OtherEndpointPaths)); };
            Slots.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(Slots)); };

            for (int i = 0; i < Global.FLAG_MAX_RES_COUNT; ++i)
                Slots[i] = new ResourceSlot();
        }

        public override void ResetDirtyFlag()
        {
            lock (dirtyLock)
            {
                FlagPaths.Dirty = false;
                OtherEndpoints.Dirty = false;
                OtherEndpointPaths.Dirty = false;
                // incompleteBuildingCount.Dirty = false;

                ResetDirtyFlagUnlocked();
            }
        }

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
        public Direction SearchDirection
        {
            get => searchDirection;
            set
            {
                if (searchDirection != value)
                {
                    searchDirection = value;
                    MarkPropertyAsDirty(nameof(SearchDirection));
                }
            }
        }
        public word SearchNumber
        {
            get => searchNumber;
            set
            {
                if (searchNumber != value)
                {
                    searchNumber = value;
                    MarkPropertyAsDirty(nameof(SearchNumber));
                }
            }
        }
        public byte PathConnections
        {
            get => pathConnections;
            set
            {
                if (pathConnections != value)
                {
                    pathConnections = value;
                    MarkPropertyAsDirty(nameof(PathConnections));
                }
            }
        }
        public EndPointFlags EndPointFlags
        {
            get => endPointFlags;
            set
            {
                if (endPointFlags != value)
                {
                    endPointFlags = value;
                    MarkPropertyAsDirty(nameof(EndPointFlags));
                }
            }
        }
        public TransporterFlags TransporterFlags
        {
            get => transporterFlags;
            set
            {
                if (transporterFlags != value)
                {
                    transporterFlags = value;
                    MarkPropertyAsDirty(nameof(TransporterFlags));
                }
            }
        }
        public FlagBuildingFlags FlagBuildingFlags
        {
            get => flagBuildingFlags;
            set
            {
                if (flagBuildingFlags != value)
                {
                    flagBuildingFlags = value;
                    MarkPropertyAsDirty(nameof(FlagBuildingFlags));
                }
            }
        }
        public DirtyArray<byte> FlagPaths { get; } = new DirtyArray<byte>(6);
        public DirtyArray<word> OtherEndpoints { get; } = new DirtyArray<word>(6);
        public DirtyArray<byte> OtherEndpointPaths { get; } = new DirtyArray<byte>(6);
        public DirtyArray<ResourceSlot> Slots { get; } = new DirtyArray<ResourceSlot>(Global.FLAG_MAX_RES_COUNT);

        [Ignore]
        public PathConnectionFlags PathConnectionFlags
        {
            get => (PathConnectionFlags)(PathConnections & 0x3f);
            set => PathConnections = (byte)((PathConnections & 0xC0) | ((byte)value & 0x3f));
        }
        [Ignore]
        public byte OwnerIndex
        {
            get => (byte)((PathConnections >> 6) & 0x03);
            set => PathConnections = (byte)((PathConnections & 0x3f) | ((value & 0x03) << 6));
        }

        public OtherEndpointPath GetOtherEndpointPath(Direction direction)
        {
            return ToEndpointPath(OtherEndpointPaths[(int)direction]);
        }

        public void SetOtherEndpointSlot(Direction direction, byte scheduledSlot)
        {
            OtherEndpointPaths[(int)direction] = (byte)
                ((scheduledSlot & 0x07) |
                (OtherEndpointPaths[(int)direction] & 0xf8));
        }

        public void ScheduleOtherEndpoint(Direction direction, byte scheduledSlot)
        {
            OtherEndpointPaths[(int)direction] = (byte)
                ((scheduledSlot & 0x07) | 0x80 |
                (OtherEndpointPaths[(int)direction] & 0x78));
        }

        public void SetOtherEndpointDirection(Direction direction, Direction otherEndpointDirection)
        {
            OtherEndpointPaths[(int)direction] = (byte)
                ((OtherEndpointPaths[(int)direction] & 0xc7) |
                (((byte)otherEndpointDirection << 3) & 0x38));
        }

        public FlagPath GetFlagPath(Direction direction)
        {
            return ToFlagPath(FlagPaths[(int)direction]);
        }

        public void SetSerfRequested(Direction direction, bool requested)
        {
            byte result = FlagPaths[(int)direction];

            if (requested)
                result |= 0x80;
            else
                result &= 0x7f;

            FlagPaths[(int)direction] = result;
        }

        public void ResetRoadLength(Direction direction, byte length)
        {
            FlagPaths[(int)direction] = (byte)(length << 4);
        }

        public void SetRoadLength(Direction direction, byte length)
        {
            FlagPaths[(int)direction] = (byte)
                ((FlagPaths[(int)direction] & 0x80) |
                ((length << 4) & 0x70));
        }

        public void SetFreeTransporters(Direction direction, byte count)
        {
            FlagPaths[(int)direction] = (byte)
                ((FlagPaths[(int)direction] & 0xf0) |
                (count & 0x0f));
        }

        public void IncreaseFreeTransporters(Direction direction, byte count = 1)
        {
            // TODO safety check for > 15?
            FlagPaths[(int)direction] += count;
        }


        #region Helpers

        private static OtherEndpointPath ToEndpointPath(byte value)
        {
            return new OtherEndpointPath()
            {
                ScheduledResourceSlotIndex = (byte)(value & 0x07),
                LeadingBackDirection = (Direction)((value >> 3) & 0x07),
                ResourcePickupScheduled = ((value >> 7) & 0x01) != 0
            };
        }

        private static FlagPath ToFlagPath(byte value)
        {
            return new FlagPath()
            {
                FreeTransporters = (byte)(value & 0x0f),
                LengthCategory = (byte)((value >> 4) & 0x07),
                SerfRequested = ((value >> 7) & 0x01) != 0
            };
        }

        private static byte FromFlagPath(FlagPath value)
        {
            byte result = 0;

            result |= (byte)(value.FreeTransporters & 0x0f);
            result |= (byte)(((byte)value.LengthCategory << 4) & 0x70);

            if (value.SerfRequested)
                result |= 0x80;

            return result;
        }

        #endregion

    }
}