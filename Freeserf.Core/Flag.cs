/*
 * Flag.cs - Flag related functions.
 *
 * Copyright (C) 2013       Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Linq;

namespace Freeserf
{
    using Serialize;
    using MapPos = UInt32;
    using word = UInt16;

    public class SerfPathInfo
    {
        public int PathLength;
        public int SerfCount;
        public int FlagIndex;
        public Direction FlagDirection;
        public int[] Serfs; // int[16]
    }

    public class Flag : GameObject, IState
    {
        internal class ResourceSlot : State, IComparable
        {
            public Resource.Type Type { get; set; } = Resource.Type.None;
            public Direction Direction { get; set; }
            public word DestinationObjectIndex { get; set; }

            public override int CompareTo(object other)
            {
                if (other is ResourceSlot)
                {
                    var otherSlot = other as ResourceSlot;

                    if (Type == otherSlot.Type)
                    {
                        if (Direction == otherSlot.Direction)
                            return DestinationObjectIndex.CompareTo(otherSlot.DestinationObjectIndex);
                        else
                            return Direction.CompareTo(otherSlot.Direction);
                    }

                    return Type.CompareTo(otherSlot.Type);
                }

                return 1;
            }
        }

        internal struct OtherEndpoint
        {
            private object obj;
            public Building Building { get { return obj as Building; } set { obj = value; } }
            public Flag Flag { get { return obj as Flag; } set { obj = value; } }
            public object Object { get { return obj; } set { obj = value; } }
            public Road Road { get; set; }
        }

        internal struct OtherEndpointPath
        {
            public byte ScheduledResourceSlotIndex;
            public Direction LeadingBackDirection;
            public bool ResourcePickupScheduled;
        }

        internal struct FlagPath
        {
            public byte FreeTransporters;
            public byte LengthCategory;
            public bool SerfRequested;
        }

        static readonly int[] MaxTransporters = new[] { 1, 2, 3, 4, 6, 8, 11, 15 };

        [Data]
        private FlagState state = new FlagState();
        public object Tag { get; set; } = null; // General purpose tagged object (used in Game.UpdateInventories)

        internal OtherEndpoint[] OtherEndPoints { get; } = new OtherEndpoint[6];

        public Flag(Game game, uint index)
            : base(game, index)
        {
            state.Position = 0u;
            state.SearchNumber = 0;
            state.SearchDirection = Direction.Right;
            state.PathConnections = 0;
            state.EndPointFlags = EndPointFlags.None;
            state.TransporterFlags = TransporterFlags.None;
            state.FlagBuildingFlags = FlagBuildingFlags.None;

            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                OtherEndPoints[(int)direction].Object = null;
                OtherEndPoints[(int)direction].Road = null;
            }
        }

        public bool Dirty => state.Dirty;

        internal MapPos Position
        {
            get => state.Position;
            set => state.Position = value;
        }
        internal uint SearchNumber
        {
            get => state.SearchNumber;
            set => state.SearchNumber = (word)value;
        }
        internal Direction SearchDirection
        {
            get => state.SearchDirection;
            set => state.SearchDirection = value;
        }
        public int ResourceCount => state.Slots.Count(s => s.Type != Resource.Type.None);

        public void ResetDirtyFlag()
        {
            state.ResetDirtyFlag();
        }

        public uint GetCostToNearestInventory(bool inventorySupportsResIn, bool inventorySupportsResOut)
        {
            var player = Game.GetPlayer(Player);
            uint bestCost = uint.MaxValue;

            foreach (var inventory in Game.GetPlayerInventories(player).ToList())
            {
                if (inventorySupportsResIn && inventory.ResourceMode != Inventory.Mode.In)
                    continue;

                if (inventorySupportsResOut && inventory.ResourceMode == Inventory.Mode.Out)
                    continue;

                Pathfinder.FindShortestRoad(Game.Map, this, Game.GetFlag(inventory.Flag), out uint cost);

                if (cost < bestCost)
                    bestCost = cost;
            }

            return bestCost;
        }

        public bool HasDirectConnectionTo(Flag flag)
        {
            for (int i = 0; i < 6; ++i)
            {
                if (OtherEndPoints[i].Flag == flag)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Bitmap of all directions with outgoing paths.
        /// </summary>
        public PathConnectionFlags Paths()
        {
            return state.PathConnectionFlags;
        }

        public void AddPath(Direction direction, bool water)
        {
            state.PathConnectionFlags |= direction.ToPathConnectionFlag();

            if (water)
            {
                state.EndPointFlags &= ~direction.ToEndPointFlag();
            }
            else
            {
                state.EndPointFlags |= direction.ToEndPointFlag();
            }

            state.TransporterFlags &= ~direction.ToTransporterFlag();
        }

        public void DeletePath(Direction direction)
        {
            state.PathConnectionFlags &= ~direction.ToPathConnectionFlag();
            state.EndPointFlags &= ~direction.ToEndPointFlag();
            state.TransporterFlags &= ~direction.ToTransporterFlag();

            if (SerfRequested(direction))
            {
                CancelSerfRequest(direction);

                var destination = Game.Map.GetObjectIndex(state.Position);

                foreach (Serf serf in Game.GetSerfsRelatedTo(destination, direction))
                {
                    serf.PathDeleted(destination, direction);
                }
            }

            state.OtherEndpointPaths[(int)direction] &= 0x78;
            OtherEndPoints[(int)direction].Flag = null;
            OtherEndPoints[(int)direction].Road = null;

            // Mark resource path for recalculation if they would
            // have followed the removed path.
            InvalidateResourcePath(direction);
        }

        // Whether a path exists in a given direction.
        public bool HasPath(Direction direction)
        {
            return state.PathConnectionFlags.HasFlag(direction.ToPathConnectionFlag());
        }

        public void PrioritizePickup(Direction direction, Player player)
        {
            int resourceNext = -1;
            int resourcePriority = -1;

            for (int i = 0; i < Global.FLAG_MAX_RES_COUNT; ++i)
            {
                if (state.Slots[i].Type != Resource.Type.None &&
                    state.Slots[i].Direction == direction)
                {
                    var resourceType = state.Slots[i].Type;
                    var flagPriority = player.GetFlagPriority(resourceType);

                    if (flagPriority > resourcePriority)
                    {
                        resourceNext = i;
                        resourcePriority = flagPriority;
                    }
                }
            }

            if (resourceNext > -1)
                state.ScheduleOtherEndpoint(direction, (byte)resourceNext);
            else
                state.OtherEndpointPaths[(int)direction] &= 0x78;
        }

        // Owner of this flag.
        public uint Player
        {
            get => state.OwnerIndex;
            set => state.OwnerIndex = (byte)value;
        }

        // Bitmap showing whether the outgoing paths are land paths.
        public int LandPaths => (int)state.EndPointFlags & 0x3f;

        // Whether the path in the given direction is a water path.
        public bool IsWaterPath(Direction direction)
        {
            return !state.EndPointFlags.HasFlag(direction.ToEndPointFlag());
        }

        // Whether a building is connected to this flag. If so, the pointer to
        // the other endpoint is a valid building pointer. (Always at UP LEFT direction).
        public bool HasBuilding => state.EndPointFlags.HasFlag(EndPointFlags.HasConnectedBuilding);

        // Whether resources exist that are not yet scheduled.
        public bool HasResources => state.EndPointFlags.HasFlag(EndPointFlags.HasUnscheduledResources);

        // Bitmap showing whether the outgoing paths have transporters
        // servicing them.
        public int Transporters => (int)state.TransporterFlags & 0x3f;

        // Whether the path in the given direction has a transporter
        // serving it.
        public bool HasTransporter(Direction direction)
        {
            return state.TransporterFlags.HasFlag(direction.ToTransporterFlag());
        }

        // Whether this flag has tried to request a transporter without success.
        public bool SerfRequestFail => state.TransporterFlags.HasFlag(TransporterFlags.SerfRequestFailed);

        public void SerfRequestClear()
        {
            state.TransporterFlags &= ~TransporterFlags.SerfRequestFailed;
        }

        // Current number of transporters on path.
        public uint FreeTransporterCount(Direction direction)
        {
            return state.FlagPaths[(int)direction] & 0xfu;
        }

        public void TransporterToServe(Direction direction)
        {
            --state.FlagPaths[(int)direction];
        }

        // Length category of path determining max number of transporters.
        public uint LengthCategory(Direction direction)
        {
            return (uint)(state.FlagPaths[(int)direction] >> 4) & 7u;
        }

        // Whether a transporter serf was successfully requested for this path.
        public bool SerfRequested(Direction direction)
        {
            return state.GetFlagPath(direction).SerfRequested;
        }

        public void CancelSerfRequest(Direction direction)
        {
            state.SetSerfRequested(direction, false);
        }

        public void CompleteSerfRequest(Direction direction)
        {
            state.SetSerfRequested(direction, false);
            state.IncreaseFreeTransporters(direction);
        }

        // The slot that is scheduled for pickup by the given path.
        public uint ScheduledSlot(Direction direction)
        {
            return state.GetOtherEndpointPath(direction).ScheduledResourceSlotIndex;
        }

        // The direction from the other endpoint leading back to this flag.
        public Direction GetOtherEndDirection(Direction direction)
        {
            return state.GetOtherEndpointPath(direction).LeadingBackDirection;
        }

        public Flag GetOtherEndFlag(Direction direction)
        {
            return OtherEndPoints[(int)direction].Flag;
        }

        public Road GetRoad(Direction direction)
        {
            return OtherEndPoints[(int)direction].Road;
        }

        // Whether the given direction has a resource pickup scheduled.
        public bool IsScheduled(Direction direction)
        {
            return state.GetOtherEndpointPath(direction).ResourcePickupScheduled;
        }

        public bool PickUpResource(uint fromSlot, ref Resource.Type resource, ref uint destination)
        {
            if (fromSlot >= Global.FLAG_MAX_RES_COUNT)
            {
                throw new ExceptionFreeserf(Game, ErrorSystemType.Flag, "Wrong flag slot index.");
            }

            if (state.Slots[fromSlot].Type == Resource.Type.None)
            {
                return false;
            }

            resource = state.Slots[fromSlot].Type;
            destination = state.Slots[fromSlot].DestinationObjectIndex;
            state.Slots[fromSlot].Type = Resource.Type.None;
            state.Slots[fromSlot].DestinationObjectIndex = 0;
            state.Slots[fromSlot].Direction = Direction.None;

            FixScheduled();

            return true;
        }

        public bool DropResource(Resource.Type resource, uint destination)
        {
            if (resource < Resource.Type.MinValue || resource > Resource.Type.MaxValue)
            {
                throw new ExceptionFreeserf(Game, ErrorSystemType.Flag, "Wrong resource type.");
            }

            for (int i = 0; i < Global.FLAG_MAX_RES_COUNT; ++i)
            {
                if (state.Slots[i].Type == Resource.Type.None)
                {
                    state.Slots[i].Type = resource;
                    state.Slots[i].DestinationObjectIndex = (word)destination;
                    state.Slots[i].Direction = Direction.None;
                    state.EndPointFlags |= EndPointFlags.HasUnscheduledResources;

                    return true;
                }
            }

            return false;
        }

        public bool HasEmptySlot()
        {
            return state.Slots.Any(slot => slot.Type == Resource.Type.None);
        }

        public void RemoveAllResources()
        {
            for (int i = 0; i < Global.FLAG_MAX_RES_COUNT; ++i)
            {
                var resource = state.Slots[i].Type;

                if (resource != Resource.Type.None)
                {
                    Game.CancelTransportedResource(resource, state.Slots[i].DestinationObjectIndex);
                    Game.LoseResource(resource);
                }
            }
        }

        public Resource.Type GetResourceAtSlot(int slot)
        {
            return state.Slots[slot].Type;
        }

        // Whether this flag has an inventory building.
        public bool HasInventory()
        {
            return state.FlagBuildingFlags.HasFlag(FlagBuildingFlags.HasInventory);
        }

        // Whether this inventory accepts resources.
        public bool AcceptsResources()
        {
            return state.FlagBuildingFlags.HasFlag(FlagBuildingFlags.InventoryAcceptsResources);
        }

        // Whether this inventory accepts serfs.
        public bool AcceptsSerfs()
        {
            return state.FlagBuildingFlags.HasFlag(FlagBuildingFlags.InventoryAcceptsSerfs);
        }

        public void SetHasInventory()
        {
            state.FlagBuildingFlags |= FlagBuildingFlags.HasInventory;
        }

        public void SetAcceptsResources(bool accepts)
        {
            if (accepts)
                state.FlagBuildingFlags |= FlagBuildingFlags.InventoryAcceptsResources;
            else
                state.FlagBuildingFlags &= ~FlagBuildingFlags.InventoryAcceptsResources;
        }

        public void SetAcceptsSerfs(bool accepts)
        {
            if (accepts)
                state.FlagBuildingFlags |= FlagBuildingFlags.InventoryAcceptsSerfs;
            else
                state.FlagBuildingFlags &= ~FlagBuildingFlags.InventoryAcceptsSerfs;
        }

        public void ClearFlags()
        {
            state.FlagBuildingFlags = FlagBuildingFlags.None;
        }

        /// <summary>
        /// Read legacy savegame.
        /// </summary>
        /// <param name="reader"></param>
        public void ReadFrom(SaveReaderBinary reader)
        {
            state.Position = 0; // Set correctly later. 
            state.SearchNumber = reader.ReadWord(); // 0
            state.SearchDirection = (Direction)reader.ReadByte(); // 2
            state.PathConnections = reader.ReadByte(); // 3
            state.EndPointFlags = (EndPointFlags)reader.ReadByte(); // 4
            state.TransporterFlags = (TransporterFlags)reader.ReadByte(); // 5

            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                state.FlagPaths[(int)direction] = reader.ReadByte(); // 6 + direction
            }

            for (int j = 0; j < 8; ++j)
            {
                byte value = reader.ReadByte(); // 12 + j

                state.Slots[j].Type = (Resource.Type)((value & 0x1f) - 1);
                state.Slots[j].Direction = (Direction)(((value >> 5) & 7) - 1);
            }

            for (int j = 0; j < 8; ++j)
            {
                state.Slots[j].DestinationObjectIndex = reader.ReadWord(); // 20 + j*2
            }

            // base + 36
            cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction j in cycle)
            {
                int offset = (int)reader.ReadDWord();

                // Other endpoint could be a building in direction up left. 
                if (j == Direction.UpLeft && HasBuilding)
                {
                    OtherEndPoints[(int)j].Building = Game.CreateBuilding(offset / 18);
                    OtherEndPoints[(int)j].Road = Road.CreateRoadFromMapPath(Game.Map, state.Position, j);
                }
                else
                {
                    if (offset < 0)
                    {
                        OtherEndPoints[(int)j].Flag = null;
                        OtherEndPoints[(int)j].Road = null;
                    }
                    else
                    {
                        OtherEndPoints[(int)j].Flag = Game.CreateFlag(offset / 70);
                        // Re-use the road from the other flag to this flag if possible
                        var newRoad = Road.CreateRoadFromMapPath(Game.Map, state.Position, j);
                        var otherFlagOutDir = newRoad.Last.Reverse();
                        var otherFlagOutRoad = OtherEndPoints[(int)j].Flag.GetRoad(otherFlagOutDir);
                        OtherEndPoints[(int)j].Road = otherFlagOutRoad != null ? otherFlagOutRoad : newRoad;
                    }
                }
            }

            // base + 60
            cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                state.OtherEndpointPaths[(int)direction] = reader.ReadByte();
            }

            // Bit 0-5: Unused?
            // Bit 6: Flag has an associated inventory building
            // Bit 7: Associated invertory building accepts serfs
            byte buildingFlags = reader.ReadByte(); // 66

            byte priority = reader.ReadByte(); // 67

            if (HasBuilding)
            {
                OtherEndPoints[(int)Direction.UpLeft].Building.SetPriorityInStock(0, priority);
            }

            // Bit 0-6: Unused?
            // Bit 7: Associated invertory building accepts resources
            byte buildingFlags2 = reader.ReadByte(); // 68

            priority = reader.ReadByte(); // 69

            if (HasBuilding)
            {
                OtherEndPoints[(int)Direction.UpLeft].Building.SetPriorityInStock(1, priority);
            }

            state.FlagBuildingFlags = FlagBuildingFlags.None;
            if ((buildingFlags & 0x40) != 0)
                state.FlagBuildingFlags |= FlagBuildingFlags.HasInventory;
            if ((buildingFlags & 0x80) != 0)
                state.FlagBuildingFlags |= FlagBuildingFlags.InventoryAcceptsSerfs;
            if ((buildingFlags2 & 0x80) != 0)
                state.FlagBuildingFlags |= FlagBuildingFlags.InventoryAcceptsResources;
        }

        /// <summary>
        /// Read savegames from freeserf project.
        /// </summary>
        public void ReadFrom(SaveReaderText reader)
        {
            uint x = reader.Value("pos")[0].ReadUInt();
            uint y = reader.Value("pos")[1].ReadUInt();
            state.Position = Game.Map.Position(x, y);
            state.SearchNumber = (word)reader.Value("search_num").ReadInt();
            state.SearchDirection = reader.Value("search_dir").ReadDirection();
            state.PathConnections = (byte)reader.Value("path_con").ReadInt();
            state.EndPointFlags = (EndPointFlags)reader.Value("endpoints").ReadInt();
            state.TransporterFlags = (TransporterFlags)reader.Value("transporter").ReadInt();

            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                state.FlagPaths[(int)direction] = (byte)reader.Value("length")[(int)direction].ReadUInt();
                int objectIndex = reader.Value("other_endpoint")[(int)direction].ReadInt();

                if (direction == Direction.UpLeft && HasBuilding)
                {
                    OtherEndPoints[(int)direction].Building = Game.CreateBuilding(objectIndex);
                    OtherEndPoints[(int)direction].Road = Road.CreateRoadFromMapPath(Game.Map, state.Position, direction);
                }
                else
                {
                    Flag otherFlag = null;

                    if (objectIndex != 0)
                    {
                        otherFlag = Game.CreateFlag(objectIndex);
                    }

                    OtherEndPoints[(int)direction].Flag = otherFlag;
                    if (otherFlag != null)
                    {
                        // Re-use the road from the other flag to this flag if possible
                        var newRoad = Road.CreateRoadFromMapPath(Game.Map, state.Position, direction);
                        var otherFlagOutDir = newRoad.Last.Reverse();
                        var otherFlagOutRoad = OtherEndPoints[(int)direction].Flag.GetRoad(otherFlagOutDir);
                        OtherEndPoints[(int)direction].Road = otherFlagOutRoad != null ? otherFlagOutRoad : newRoad;
                    }
                    else
                    {
                        OtherEndPoints[(int)direction].Road = null;
                    }
                }

                state.OtherEndpointPaths[(int)direction] = (byte)reader.Value("other_end_dir")[(int)direction].ReadInt();
            }

            for (int i = 0; i < Global.FLAG_MAX_RES_COUNT; ++i)
            {
                state.Slots[i].Type = reader.Value("slot.type")[i].ReadResource();
                state.Slots[i].Direction = reader.Value("slot.dir")[i].ReadDirection();
                state.Slots[i].DestinationObjectIndex = (word)reader.Value("slot.dest")[i].ReadUInt();
            }

            uint buildingFlags = reader.Value("bld_flags").ReadUInt();
            uint buildingFlags2 = reader.Value("bld2_flags").ReadUInt();

            state.FlagBuildingFlags = FlagBuildingFlags.None;
            if ((buildingFlags & 0x40) != 0)
                state.FlagBuildingFlags |= FlagBuildingFlags.HasInventory;
            if ((buildingFlags & 0x80) != 0)
                state.FlagBuildingFlags |= FlagBuildingFlags.InventoryAcceptsSerfs;
            if ((buildingFlags2 & 0x80) != 0)
                state.FlagBuildingFlags |= FlagBuildingFlags.InventoryAcceptsResources;
        }

        /// <summary>
        /// Write savegames for freeserf project.
        /// </summary>
        public void WriteTo(SaveWriterText writer)
        {
            writer.Value("pos").Write(Game.Map.PositionColumn(state.Position));
            writer.Value("pos").Write(Game.Map.PositionRow(state.Position));
            writer.Value("search_num").Write(state.SearchNumber);
            writer.Value("search_dir").Write((int)state.SearchDirection);
            writer.Value("path_con").Write((int)state.PathConnections);
            writer.Value("endpoints").Write((int)state.EndPointFlags);
            writer.Value("transporter").Write((int)state.TransporterFlags);

            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                writer.Value("length").Write((uint)state.FlagPaths[(int)direction]);

                if (direction == Direction.UpLeft && HasBuilding)
                {
                    writer.Value("other_endpoint").Write(OtherEndPoints[(int)direction].Building.Index);
                }
                else
                {
                    if (HasPath(direction))
                    {
                        writer.Value("other_endpoint").Write(OtherEndPoints[(int)direction].Flag.Index);
                    }
                    else
                    {
                        writer.Value("other_endpoint").Write(0u);
                    }
                }

                writer.Value("other_end_dir").Write((int)state.OtherEndpointPaths[(int)direction]);
            }

            for (int i = 0; i < Global.FLAG_MAX_RES_COUNT; ++i)
            {
                writer.Value("slot.type").Write((int)state.Slots[i].Type);
                writer.Value("slot.dir").Write((int)state.Slots[i].Direction);
                writer.Value("slot.dest").Write((uint)state.Slots[i].DestinationObjectIndex);
            }

            byte buildingFlags = 0;
            byte buildingFlags2 = 0;

            if (state.FlagBuildingFlags.HasFlag(FlagBuildingFlags.HasInventory))
                buildingFlags |= 0x40;
            if (state.FlagBuildingFlags.HasFlag(FlagBuildingFlags.InventoryAcceptsSerfs))
                buildingFlags |= 0x80;
            if (state.FlagBuildingFlags.HasFlag(FlagBuildingFlags.InventoryAcceptsResources))
                buildingFlags2 |= 0x80;

            writer.Value("bld_flags").Write(buildingFlags);
            writer.Value("bld2_flags").Write(buildingFlags2);
        }

        public void ResetTransport(Flag other)
        {
            for (int slot = 0; slot < Global.FLAG_MAX_RES_COUNT; ++slot)
            {
                if (other.state.Slots[slot].Type != Resource.Type.None &&
                    other.state.Slots[slot].DestinationObjectIndex == Index)
                {
                    other.state.Slots[slot].DestinationObjectIndex = 0;
                    other.state.EndPointFlags |= EndPointFlags.HasUnscheduledResources;

                    if (other.state.Slots[slot].Direction != Direction.None)
                    {
                        var direction = other.state.Slots[slot].Direction;
                        var otherPlayer = Game.GetPlayer(other.Player);

                        other.PrioritizePickup(direction, otherPlayer);
                    }
                }
            }
        }

        public void ResetDestinationOfStolenResources()
        {
            for (int i = 0; i < Global.FLAG_MAX_RES_COUNT; ++i)
            {
                if (state.Slots[i].Type != Resource.Type.None)
                {
                    var resource = state.Slots[i].Type;
                    Game.CancelTransportedResource(resource, state.Slots[i].DestinationObjectIndex);
                    state.Slots[i].DestinationObjectIndex = 0;
                }
            }
        }

        public void LinkBuilding(Building building)
        {
            OtherEndPoints[(int)Direction.UpLeft].Building = building;
            OtherEndPoints[(int)Direction.UpLeft].Road = Road.CreateBuildingRoad(Game.Map, Game.Map.MoveDownRight(building.Position));
            state.EndPointFlags |= EndPointFlags.HasConnectedBuilding;
        }

        public void UnlinkBuilding()
        {
            OtherEndPoints[(int)Direction.UpLeft].Building = null;
            OtherEndPoints[(int)Direction.UpLeft].Road = null;
            state.EndPointFlags &= ~EndPointFlags.HasConnectedBuilding;
            ClearFlags();
        }

        public Building Building => OtherEndPoints[(int)Direction.UpLeft].Building;

        public void InvalidateResourcePath(Direction direction)
        {
            for (int i = 0; i < Global.FLAG_MAX_RES_COUNT; ++i)
            {
                if (state.Slots[i].Type != Resource.Type.None && state.Slots[i].Direction == direction)
                {
                    state.Slots[i].Direction = Direction.None;
                    state.EndPointFlags |= EndPointFlags.HasUnscheduledResources;
                }
            }
        }

        class IntPointerHelper
        {
            public int Value = 0;
        }

        class FlagPointerHelper
        {
            public Flag Value = null;
        }

        static bool FindNearestInventorySearchCB(Flag flag, object data)
        {
            var destination = data as FlagPointerHelper;

            if (flag.AcceptsResources())
            {
                destination.Value = flag;
                return true;
            }

            return false;
        }

        static bool FlagSearchInventorySearchCB(Flag flag, object data)
        {
            var destinationIndex = data as IntPointerHelper;

            if (flag.AcceptsSerfs())
            {
                var building = flag.Building;

                destinationIndex.Value = (int)building.FlagIndex;

                return true;
            }

            return false;
        }

        public int FindNearestInventoryForResource()
        {
            var destination = new FlagPointerHelper()
            {
                Value = null
            };

            FlagSearch.Single(this, FindNearestInventorySearchCB, false, true, destination);

            if (destination.Value != null)
                return (int)destination.Value.Index;

            return -1;
        }

        public int FindNearestInventoryForSerf()
        {
            var destinationIndex = new IntPointerHelper()
            {
                Value = -1
            };

            FlagSearch.Single(this, FlagSearchInventorySearchCB, true, false, destinationIndex);

            return destinationIndex.Value;
        }

        public void LinkWithFlag(Flag destinationFlag, bool waterPath,
            Direction inDirection, Direction outDirection, Road road)
        {
            destinationFlag.AddPath(inDirection, waterPath);
            AddPath(outDirection, waterPath);

            destinationFlag.state.SetOtherEndpointDirection(inDirection, outDirection);
            this.state.SetOtherEndpointDirection(outDirection, inDirection);

            uint roadLength = GetRoadLengthValue(road.Length);

            destinationFlag.state.ResetRoadLength(inDirection, (byte)roadLength);
            this.state.ResetRoadLength(outDirection, (byte)roadLength);

            destinationFlag.OtherEndPoints[(int)inDirection].Flag = this;
            OtherEndPoints[(int)outDirection].Flag = destinationFlag;

            destinationFlag.OtherEndPoints[(int)inDirection].Road = road;
            OtherEndPoints[(int)outDirection].Road = road;
        }

        public void Update()
        {
            try
            {
                // Count and store in bitfield which directions
                // have strictly more than 0,1,2,3 slots waiting.
                var resourcesWaiting = new int[4] { 0, 0, 0, 0 };

                for (int j = 0; j < Global.FLAG_MAX_RES_COUNT; ++j)
                {
                    if (state.Slots[j].Type != Resource.Type.None && state.Slots[j].Direction != Direction.None)
                    {
                        var resourceDirection = state.Slots[j].Direction;

                        for (int k = 0; k < 4; ++k)
                        {
                            if (!Misc.BitTest(resourcesWaiting[k], (int)resourceDirection))
                            {
                                resourcesWaiting[k] |= Misc.Bit((int)resourceDirection);
                                break;
                            }
                        }
                    }
                }

                // Count of total resources waiting at flag
                int waitingCount = 0;

                if (HasResources)
                {
                    state.EndPointFlags &= ~EndPointFlags.HasUnscheduledResources;

                    for (int slot = 0; slot < Global.FLAG_MAX_RES_COUNT; ++slot)
                    {
                        if (state.Slots[slot].Type != Resource.Type.None)
                        {
                            ++waitingCount;

                            // Only schedule the slot if it has not already
                            // been scheduled for fetch.
                            int resourceDirection = (int)state.Slots[slot].Direction;

                            if (resourceDirection < 0)
                            {
                                if (state.Slots[slot].DestinationObjectIndex != 0)
                                {
                                    // Destination is known
                                    ScheduleSlotToKnownDestination(slot, resourcesWaiting);
                                }
                                else
                                {
                                    // Destination is not known
                                    ScheduleSlotToUnknownDest(slot);
                                }
                            }
                        }
                    }
                }

                // Update transporter flags, decide if serf needs to be sent to road
                var cycle = DirectionCycleCCW.CreateDefault();

                foreach (var direction in cycle)
                {
                    if (HasPath(direction))
                    {
                        if (SerfRequested(direction))
                        {
                            if (Misc.BitTest(resourcesWaiting[2], (int)direction))
                            {
                                if (waitingCount >= 7)
                                {
                                    state.TransporterFlags &= direction.ToTransporterFlag();
                                }
                            }
                            else if (FreeTransporterCount(direction) != 0)
                            {
                                state.TransporterFlags |= direction.ToTransporterFlag();
                            }
                        }
                        else if (FreeTransporterCount(direction) == 0 || Misc.BitTest(resourcesWaiting[2], (int)direction))
                        {
                            int maxTransporters = MaxTransporters[LengthCategory(direction)];

                            if (FreeTransporterCount(direction) < (uint)maxTransporters && !SerfRequestFail)
                            {
                                if (!CallTransporter(direction, IsWaterPath(direction)))
                                    state.TransporterFlags |= TransporterFlags.SerfRequestFailed;
                            }

                            if (waitingCount >= 7 && Misc.BitTest(resourcesWaiting[2], (int)direction))
                            {
                                state.TransporterFlags &= direction.ToTransporterFlag();
                            }
                        }
                        else
                        {
                            state.TransporterFlags |= direction.ToTransporterFlag();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExceptionFreeserf(Game, ErrorSystemType.Flag, ex);
            }
        }

        // Get road length category value for real length.
        // Determines number of serfs servicing the path segment.
        public static uint GetRoadLengthValue(uint roadLength)
        {
            if (roadLength >= 24) return 7;
            else if (roadLength >= 18) return 6;
            else if (roadLength >= 13) return 5;
            else if (roadLength >= 10) return 4;
            else if (roadLength >= 7) return 3;
            else if (roadLength >= 6) return 2;
            else if (roadLength >= 4) return 1;
            return 0;
        }

        public void RestorePathSerfInfo(Direction direction, SerfPathInfo data)
        {
            var maxPathSerfs = new int[] { 1, 2, 3, 4, 6, 8, 11, 15 };
            var otherFlag = Game.GetFlag((uint)data.FlagIndex);
            var otherDirection = data.FlagDirection;

            AddPath(direction, otherFlag.IsWaterPath(otherDirection));

            otherFlag.state.TransporterFlags &= ~otherDirection.ToTransporterFlag();

            uint roadLength = Flag.GetRoadLengthValue((uint)data.PathLength);

            this.state.ResetRoadLength(direction, (byte)roadLength);
            otherFlag.state.SetRoadLength(otherDirection, (byte)roadLength);

            this.state.SetSerfRequested(direction, otherFlag.SerfRequested(otherDirection));

            this.state.SetOtherEndpointDirection(direction, otherDirection);
            otherFlag.state.SetOtherEndpointDirection(otherDirection, direction);

            OtherEndPoints[(int)direction].Flag = otherFlag;
            otherFlag.OtherEndPoints[(int)otherDirection].Flag = this;

            OtherEndPoints[(int)direction].Road = Road.CreateRoadFromMapPath(Game.Map, state.Position, direction);
            otherFlag.OtherEndPoints[(int)otherDirection].Road = OtherEndPoints[(int)direction].Road;

            int maxSerfs = maxPathSerfs[roadLength];

            if (SerfRequested(direction))
                --maxSerfs;

            if (data.SerfCount > maxSerfs)
            {
                for (int i = 0; i < data.SerfCount - maxSerfs; ++i)
                {
                    var serf = Game.GetSerf((uint)data.Serfs[i]);
                    serf.RestorePathSerfInfo();
                }
            }

            int minSerfs = Math.Min(data.SerfCount, maxSerfs);

            if (minSerfs > 0)
            {
                // There are still transporters on the paths.
                this.state.TransporterFlags |= direction.ToTransporterFlag();
                otherFlag.state.TransporterFlags |= otherDirection.ToTransporterFlag();

                this.state.SetFreeTransporters(direction, (byte)minSerfs);
                otherFlag.state.SetFreeTransporters(otherDirection, (byte)minSerfs);
            }
        }

        public void ClearSearchId()
        {
            state.SearchNumber = 0;
        }

        public bool CanDemolish()
        {
            int connected = 0;
            object otherEndObject = null;
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                if (HasPath(direction))
                {
                    if (IsWaterPath(direction))
                        return false;

                    ++connected;

                    if (otherEndObject != null)
                    {
                        if (OtherEndPoints[(int)direction].Object == otherEndObject)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        otherEndObject = OtherEndPoints[(int)direction].Object;
                    }
                }
            }

            if (connected == 2)
                return true;

            return false;
        }

        public bool CanMergeNearbyPaths()
        {
            var cycle = DirectionCycleCW.CreateDefault();
            var map = Game.Map;
            var pathsToMerge = new List<Road>();

            foreach (var direction in cycle)
            {
                var otherPosition = map.Move(Position, direction);

                if (map.Paths(otherPosition) != 0 &&
                    !map.HasPath(Position, direction))
                {
                    var road = Game.GetRoadFromPathAtPosition(otherPosition);

                    // if the road contains our flag already, we won't merge
                    if (road.StartPosition != Position && road.EndPosition != Position)
                    {
                        if (!pathsToMerge.Contains(road))
                            pathsToMerge.Add(road);
                        else
                            return true; // we need at least two points on that road
                    }
                }
            }

            return false;
        }

        public bool MergeNearbyPaths()
        {
            var cycle = DirectionCycleCW.CreateDefault();
            var map = Game.Map;
            var pathsToMerge = new Dictionary<Road, List<MapPos>>();
            var connectDirections = new Dictionary<MapPos, Direction>();

            foreach (var direction in cycle)
            {
                var otherPosition = map.Move(Position, direction);

                if (map.Paths(otherPosition) != 0 &&
                    !map.HasPath(Position, direction))
                {
                    var road = Game.GetRoadFromPathAtPosition(otherPosition);

                    // if the road contains our flag already, we won't merge
                    if (road.StartPosition == Position || road.EndPosition == Position)
                        continue;

                    connectDirections.Add(otherPosition, direction.Reverse());

                    if (!pathsToMerge.ContainsKey(road))
                        pathsToMerge.Add(road, new List<MapPos> { otherPosition });
                    else
                        pathsToMerge[road].Add(otherPosition);
                }
            }

            foreach (var pathEntry in pathsToMerge)
            {
                if (pathEntry.Value.Count < 2)
                    continue;

                var road = pathEntry.Key;
                var flag1 = Game.GetFlagAtPosition(road.StartPosition);
                var flag2 = Game.GetFlagAtPosition(road.EndPosition);

                // build two new roads from the old roads (one from each end flag)
                var roadInverseDirections = road.Directions.ToList();
                var roadDirections = new List<Direction>(roadInverseDirections);
                roadDirections.Reverse();

                for (int i = 0; i < roadInverseDirections.Count; ++i)
                {
                    roadInverseDirections[i] = roadInverseDirections[i].Reverse();
                }

                var position = flag1.Position;
                var newRoad1 = new Road();
                newRoad1.Start(position);

                foreach (var direction in roadDirections)
                {
                    position = map.Move(position, direction);
                    newRoad1.Extend(map, direction);

                    if (pathEntry.Value.Contains(position))
                    {
                        newRoad1.Extend(map, connectDirections[position]);
                        break;
                    }
                }

                position = flag2.Position;
                var newRoad2 = new Road();
                newRoad2.Start(position);

                foreach (var direction in roadInverseDirections)
                {
                    position = map.Move(position, direction);
                    newRoad2.Extend(map, direction);

                    if (pathEntry.Value.Contains(position))
                    {
                        newRoad2.Extend(map, connectDirections[position]);
                        break;
                    }
                }

                // delete the old road
                Game.RemoveRoad(road);

                var player = Game.GetPlayer(Player);

                // build new roads
                if (!Game.BuildRoad(newRoad1, player, true) ||
                    !Game.BuildRoad(newRoad2, player, true))
                    return false;
            }

            return true;
        }

        public void MergePaths(MapPos position)
        {
            var map = Game.Map;

            if (map.Paths(position) == 0)
            {
                return;
            }

            var path1Direction = Direction.Right;
            var path2Direction = Direction.Right;

            // Find first direction
            var cycleCW = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycleCW)
            {
                if (map.HasPath(position, direction))
                {
                    path1Direction = direction;
                    break;
                }
            }

            // Find second direction
            var cycleCCW = DirectionCycleCCW.CreateDefault();

            foreach (var direction in cycleCCW)
            {
                if (map.HasPath(position, direction))
                {
                    path2Direction = direction;
                    break;
                }
            }

            var path1Data = new SerfPathInfo();
            var path2Data = new SerfPathInfo();

            path1Data.Serfs = new int[16];
            path2Data.Serfs = new int[16];

            FillPathSerfInfo(Game, position, path1Direction, path1Data);
            FillPathSerfInfo(Game, position, path2Direction, path2Data);

            Flag flag1 = Game.GetFlag((uint)path1Data.FlagIndex);
            Flag flag2 = Game.GetFlag((uint)path2Data.FlagIndex);
            Direction direction1 = path1Data.FlagDirection;
            Direction direction2 = path2Data.FlagDirection;

            flag1.state.SetOtherEndpointDirection(direction1, direction2);
            flag2.state.SetOtherEndpointDirection(direction2, direction1);

            flag1.OtherEndPoints[(int)direction1].Flag = flag2;
            flag2.OtherEndPoints[(int)direction2].Flag = flag1;

            flag1.OtherEndPoints[(int)direction1].Road.Extend(Game.Map, OtherEndPoints[(int)path1Direction].Road);
            flag2.OtherEndPoints[(int)direction2].Road.Extend(Game.Map, OtherEndPoints[(int)path2Direction].Road);

            flag1.state.TransporterFlags &= ~direction1.ToTransporterFlag();
            flag2.state.TransporterFlags &= ~direction2.ToTransporterFlag();

            uint roadLength = GetRoadLengthValue((uint)(path1Data.PathLength + path2Data.PathLength));
            flag1.state.ResetRoadLength(direction1, (byte)roadLength);
            flag2.state.ResetRoadLength(direction2, (byte)roadLength);

            int maxSerfs = MaxTransporters[flag1.LengthCategory(direction1)];
            int serfCount = path1Data.SerfCount + path2Data.SerfCount;

            if (serfCount > 0)
            {
                flag1.state.TransporterFlags |= direction1.ToTransporterFlag();
                flag2.state.TransporterFlags |= direction2.ToTransporterFlag();

                if (serfCount > maxSerfs)
                {
                    // TODO 59B8B
                }

                flag1.state.IncreaseFreeTransporters(direction1, (byte)serfCount);
                flag2.state.IncreaseFreeTransporters(direction2, (byte)serfCount);
            }

            // Update serfs with reference to this flag.
            var serfs = Game.GetSerfsRelatedTo(flag1.Index, direction1);
            serfs.AddRange(Game.GetSerfsRelatedTo(flag2.Index, direction2));

            foreach (var serf in serfs)
            {
                serf.PathMerged2(flag1.Index, direction1, flag2.Index, direction2);
            }
        }

        // Find a transporter at position and change it to state.
        static int ChangeTransporterStateAtPosition(Game game, MapPos position, Serf.State state)
        {
            foreach (var serf in game.GetSerfsAtPosition(position))
            {
                if (serf.ChangeTransporterStateAtPosition(position, state))
                {
                    return (int)serf.Index;
                }
            }

            return -1;
        }

        static int WakeTransporterAtFlag(Game game, MapPos position)
        {
            return ChangeTransporterStateAtPosition(game, position, Serf.State.WakeAtFlag);
        }

        static int WakeTransporterOnPath(Game game, MapPos position)
        {
            return ChangeTransporterStateAtPosition(game, position, Serf.State.WakeOnPath);
        }

        public static void FillPathSerfInfo(Game game, MapPos position,
            Direction direction, SerfPathInfo data)
        {
            var map = game.Map;

            if (map.GetIdleSerf(position))
                WakeTransporterAtFlag(game, position);

            int serfCounter = 0;
            int pathLength = 0;

            // Handle first position.
            if (map.HasSerf(position))
            {
                var serf = game.GetSerfAtPosition(position);

                if (serf.SerfState == Serf.State.Transporting && serf.WalkingWaitCounter != -1)
                {
                    int walkingDirection = serf.WalkingDirection;

                    if (walkingDirection < 0)
                        walkingDirection += 6;

                    if ((int)direction == walkingDirection)
                    {
                        serf.SetWalkingWaitCounter(0);
                        data.Serfs[serfCounter++] = (int)serf.Index;
                    }
                }
            }

            // Trace along the path to the flag at the other end.
            int paths = 0;

            while (true)
            {
                ++pathLength;
                position = map.Move(position, direction);
                paths = (int)map.Paths(position);
                paths &= ~Misc.Bit((int)direction.Reverse());

                if (map.HasFlag(position))
                    break;

                // Find out which direction the path follows.
                var cycle = DirectionCycleCW.CreateDefault();

                foreach (var checkDirection in cycle)
                {
                    if (Misc.BitTest(paths, (int)checkDirection))
                    {
                        direction = checkDirection;
                        break;
                    }
                }

                // Check if there is a transporter waiting here.
                if (map.GetIdleSerf(position))
                {
                    int index = WakeTransporterOnPath(game, position);

                    if (index >= 0)
                        data.Serfs[serfCounter++] = index;
                }

                // Check if there is a serf occupying this space.
                if (map.HasSerf(position))
                {
                    var serf = game.GetSerfAtPosition(position);

                    if (serf.SerfState == Serf.State.Transporting && serf.WalkingWaitCounter != -1)
                    {
                        serf.SetWalkingWaitCounter(0);
                        data.Serfs[serfCounter++] = (int)serf.Index;
                    }
                }
            }

            // Handle last position.
            if (map.HasSerf(position))
            {
                var serf = game.GetSerfAtPosition(position);

                if ((serf.SerfState == Serf.State.Transporting &&
                    serf.WalkingWaitCounter != -1) ||
                    serf.SerfState == Serf.State.Delivering)
                {
                    int walkingDirection = serf.WalkingDirection;

                    if (walkingDirection < 0)
                        walkingDirection += 6;

                    if (walkingDirection == (int)direction.Reverse())
                    {
                        serf.SetWalkingWaitCounter(0);
                        data.Serfs[serfCounter++] = (int)serf.Index;
                    }
                }
            }

            // Fill the rest of the struct.
            data.PathLength = pathLength;
            data.SerfCount = serfCounter;
            data.FlagIndex = (int)map.GetObjectIndex(position);
            data.FlagDirection = direction.Reverse();
        }

        void FixScheduled()
        {
            bool anyResources = state.Slots.Any(slot => slot.Type != Resource.Type.None);

            if (anyResources)
                state.EndPointFlags |= EndPointFlags.HasUnscheduledResources;
            else
                state.EndPointFlags &= ~EndPointFlags.HasUnscheduledResources;
        }

        class ScheduleUnknownDestinationData
        {
            public Resource.Type Resource;
            public int MaxPriority;
            public Flag Flag;
        }

        static bool ScheduleUnknownDestinationCallback(Flag flag, object data)
        {
            var destinationData = data as ScheduleUnknownDestinationData;

            if (flag.HasBuilding)
            {
                var building = flag.Building;
                int buildingPriority = building.GetMaxPriorityForResource(destinationData.Resource);

                if (buildingPriority > destinationData.MaxPriority)
                {
                    destinationData.MaxPriority = buildingPriority;
                    destinationData.Flag = flag;
                }

                if (destinationData.MaxPriority > 204)
                    return true;
            }

            return false;
        }

        public bool ScheduleKnownDestinationCallback(
            Flag source,
            Flag destination,
            int slot
        )
        {
            if (this == destination)
            {
                // Destination found
                if ((int)state.SearchDirection != 6)
                {
                    if (!source.IsScheduled(state.SearchDirection))
                    {
                        // Item is requesting to be fetched
                        source.state.ScheduleOtherEndpoint(state.SearchDirection, (byte)slot);
                    }
                    else
                    {
                        var player = Game.GetPlayer(Player);
                        var otherSlot = state.GetOtherEndpointPath(state.SearchDirection).ScheduledResourceSlotIndex;
                        int priorityOld = player.GetFlagPriority(source.state.Slots[otherSlot].Type);
                        int priorityNew = player.GetFlagPriority(source.state.Slots[slot].Type);

                        if (priorityNew > priorityOld)
                        {
                            // This item has the highest priority now
                            source.state.SetOtherEndpointSlot(state.SearchDirection, (byte)slot);
                        }

                        source.state.Slots[slot].Direction = state.SearchDirection;
                    }
                }

                return true;
            }

            return false;
        }

        // Resources which should be routed directly to
        // buildings requesting them. Resources not listed
        // here will simply be moved to an inventory.
        static readonly int[] routableResources = new int[]
        {
                1,  // RESOURCE_FISH
                1,  // RESOURCE_PIG
                1,  // RESOURCE_MEAT
                1,  // RESOURCE_WHEAT
                1,  // RESOURCE_FLOUR
                1,  // RESOURCE_BREAD
                1,  // RESOURCE_LUMBER
                1,  // RESOURCE_PLANK
                0,  // RESOURCE_BOAT
                1,  // RESOURCE_STONE
                1,  // RESOURCE_IRONORE
                1,  // RESOURCE_STEEL
                1,  // RESOURCE_COAL
                1,  // RESOURCE_GOLDORE
                1,  // RESOURCE_GOLDBAR
                0,  // RESOURCE_SHOVEL
                0,  // RESOURCE_HAMMER
                0,  // RESOURCE_ROD
                0,  // RESOURCE_CLEAVER
                0,  // RESOURCE_SCYTHE
                0,  // RESOURCE_AXE
                0,  // RESOURCE_SAW
                0,  // RESOURCE_PICK
                0,  // RESOURCE_PINCER
                0,  // RESOURCE_SWORD
                0,  // RESOURCE_SHIELD
                0,  // RESOURCE_GROUP_FOOD
        };

        void ScheduleSlotToUnknownDest(int slot)
        {
            var resource = this.state.Slots[slot].Type;

            if (routableResources[(int)resource] != 0)
            {
                var search = new FlagSearch(Game);

                search.AddSource(this);

                // Handle food as one resource group
                if (resource == Resource.Type.Meat ||
                    resource == Resource.Type.Fish ||
                    resource == Resource.Type.Bread)
                {
                    resource = Resource.Type.GroupFood;
                }

                var data = new ScheduleUnknownDestinationData()
                {
                    Resource = resource,
                    Flag = null,
                    MaxPriority = 0
                };

                search.Execute(ScheduleUnknownDestinationCallback, false, true, data);

                if (data.Flag != null)
                {
                    Log.Verbose.Write(ErrorSystemType.Game, $"dest for flag {Index} res {slot} found: flag {data.Flag.Index}");
                    var destinationBuilding = data.Flag.OtherEndPoints[(int)Direction.UpLeft].Building;

                    if (!destinationBuilding.AddRequestedResource(resource, true))
                    {
                        throw new ExceptionFreeserf(Game, ErrorSystemType.Flag, "Failed to request resource.");
                    }

                    state.Slots[slot].DestinationObjectIndex = (word)destinationBuilding.FlagIndex;
                    state.EndPointFlags |= EndPointFlags.HasUnscheduledResources;

                    return;
                }
            }

            // Either this resource cannot be routed to a destination
            // other than an inventory or such destination could not be
            // found. Send to inventory instead.
            int result = FindNearestInventoryForResource();

            if (result < 0 || result == Index)
            {
                // No path to inventory was found, or
                // resource is already at destination.
                // In the latter case we need to move it
                // forth and back once before it can be delivered.
                if (Transporters == 0)
                {
                    state.EndPointFlags |= EndPointFlags.HasUnscheduledResources;
                }
                else
                {
                    var direction = Direction.None;
                    var cycle = DirectionCycleCCW.CreateDefault();

                    foreach (var checkDirection in cycle)
                    {
                        if (HasTransporter(checkDirection))
                        {
                            direction = checkDirection;
                            break;
                        }
                    }

                    if (direction < Direction.Right || direction > Direction.Up)
                    {
                        throw new ExceptionFreeserf(Game, ErrorSystemType.Flag, "Failed to request resource.");
                    }

                    if (!IsScheduled(direction))
                    {
                        state.ScheduleOtherEndpoint(direction, (byte)slot);
                    }

                    state.Slots[slot].Direction = direction;
                }
            }
            else
            {
                state.Slots[slot].DestinationObjectIndex = (word)result;
                state.EndPointFlags |= EndPointFlags.HasUnscheduledResources;
            }
        }

        class ScheduleKnownDestinationData
        {
            public Flag Source;
            public Flag Destination;
            public int Slot;
        }

        static bool ScheduleKnownDestinationCallback(Flag flag, object data)
        {
            var destinationData = data as ScheduleKnownDestinationData;

            return flag.ScheduleKnownDestinationCallback(
                destinationData.Source, destinationData.Destination, destinationData.Slot);
        }

        // resourcesWaiting = int[4]
        void ScheduleSlotToKnownDestination(int slot, int[] resourcesWaiting)
        {
            var search = new FlagSearch(Game);

            state.SearchNumber = (word)search.ID;
            state.SearchDirection = Direction.None;
            int transporters = Transporters;
            int sources = 0;

            // Directions where transporters are idle (zero slots waiting)
            int flags = (resourcesWaiting[0] ^ 0x3f) & (int)state.TransporterFlags;

            if (flags != 0)
            {
                var cycle = DirectionCycleCCW.CreateDefault();

                foreach (var direction in cycle)
                {
                    int directionIndex = (int)direction;

                    if (Misc.BitTest(flags, directionIndex))
                    {
                        transporters &= ~Misc.Bit(directionIndex);

                        var otherFlag = OtherEndPoints[directionIndex].Flag;

                        if (otherFlag.state.SearchNumber != search.ID)
                        {
                            otherFlag.state.SearchDirection = direction;
                            search.AddSource(otherFlag);
                            ++sources;
                        }
                    }
                }
            }

            if (transporters != 0)
            {
                for (int j = 0; j < 3; ++j)
                {
                    flags = resourcesWaiting[j] ^ resourcesWaiting[j + 1];

                    var cycle = DirectionCycleCCW.CreateDefault();

                    foreach (var direction in cycle)
                    {
                        int directionIndex = (int)direction;

                        if (Misc.BitTest(flags, directionIndex))
                        {
                            transporters &= ~Misc.Bit(directionIndex);

                            var otherFlag = OtherEndPoints[directionIndex].Flag;

                            if (otherFlag.state.SearchNumber != search.ID)
                            {
                                otherFlag.state.SearchDirection = direction;
                                search.AddSource(otherFlag);
                                ++sources;
                            }
                        }
                    }
                }

                if (transporters != 0)
                {
                    flags = resourcesWaiting[3];

                    var cycle = DirectionCycleCCW.CreateDefault();

                    foreach (var direction in cycle)
                    {
                        int directionIndex = (int)direction;

                        if (Misc.BitTest(flags, directionIndex))
                        {
                            transporters &= ~Misc.Bit(directionIndex);

                            var otherFlag = OtherEndPoints[directionIndex].Flag;

                            if (otherFlag.state.SearchNumber != search.ID)
                            {
                                otherFlag.state.SearchDirection = direction;
                                search.AddSource(otherFlag);
                                ++sources;
                            }
                        }
                    }

                    if (flags == 0)
                        return;
                }
            }

            if (sources > 0)
            {
                var data = new ScheduleKnownDestinationData()
                {
                    Source = this,
                    Destination = Game.GetFlag(state.Slots[slot].DestinationObjectIndex),
                    Slot = slot
                };

                bool result = search.Execute(ScheduleKnownDestinationCallback, false, true, data);

                if (!result || data.Destination == this)
                {
                    // Unable to deliver
                    bool cancel = false;

                    if (state.Slots[slot].DestinationObjectIndex != 0)
                    {
                        var flag = Game.GetFlag(state.Slots[slot].DestinationObjectIndex);

                        if (flag != null && flag.HasBuilding)
                        {
                            var building = flag.Building;

                            if (building != null && building.GetRequested(state.Slots[slot].Type) > 0)
                                cancel = true;
                        }
                    }

                    if (cancel)
                    {
                        Game.CancelTransportedResource(state.Slots[slot].Type,
                            state.Slots[slot].DestinationObjectIndex);
                    }

                    state.Slots[slot].DestinationObjectIndex = 0;
                    state.EndPointFlags |= EndPointFlags.HasUnscheduledResources;
                }
            }
            else
            {
                state.EndPointFlags |= EndPointFlags.HasUnscheduledResources;
            }
        }

        class SendSerfToRoadData
        {
            public Inventory Inventory;
            public bool Water;
        }

        static bool SendSerfToRoadSearchCallback(Flag flag, object data)
        {
            var roadData = data as SendSerfToRoadData;

            if (flag.HasInventory())
            {
                // Inventory reached
                var building = flag.Building;
                var inventory = building.Inventory;

                if (!roadData.Water)
                {
                    if (inventory.HasSerf(Serf.Type.Transporter))
                    {
                        roadData.Inventory = inventory;
                        return true;
                    }
                }
                else
                {
                    if (inventory.HasSerf(Serf.Type.Sailor))
                    {
                        roadData.Inventory = inventory;
                        return true;
                    }
                }

                if (roadData.Inventory == null && inventory.HasSerf(Serf.Type.Generic) &&
                    (!roadData.Water || inventory.GetCountOf(Resource.Type.Boat) > 0))
                {
                    roadData.Inventory = inventory;
                }
            }

            return false;
        }

        bool CallTransporter(Direction direction, bool water)
        {
            var sourceFlag = OtherEndPoints[(int)direction].Flag;
            var sourceDirection = GetOtherEndDirection(direction);

            state.SearchDirection = Direction.Right;
            sourceFlag.state.SearchDirection = Direction.DownRight;

            var search = new FlagSearch(Game);
            search.AddSource(this);
            search.AddSource(sourceFlag);

            var data = new SendSerfToRoadData()
            {
                Inventory = null,
                Water = water
            };

            search.Execute(SendSerfToRoadSearchCallback, true, false, data);

            var inventory = data.Inventory;

            if (inventory == null)
            {
                return false;
            }

            var serf = data.Inventory.CallTransporter(water);
            var destinationFlag = Game.GetFlag(inventory.Flag);

            this.state.SetSerfRequested(direction, true);
            sourceFlag.state.SetSerfRequested(sourceDirection, true);

            var inventoryFlag = this;

            if (destinationFlag.state.SearchDirection == sourceFlag.state.SearchDirection)
            {
                inventoryFlag = sourceFlag;
                direction = sourceDirection;
            }

            serf.GoOutFromInventory(inventory.Index, inventoryFlag.Index, (int)direction);

            return true;
        }
    }

    public delegate bool FlagSearchFunc(Flag flag, object data);

    public class FlagSearch
    {
        public const int SEARCH_MAX_DEPTH = 0x10000;

        readonly Game game;
        readonly Queue<Flag> queue = new Queue<Flag>();
        readonly int id;

        public FlagSearch(Game game)
        {
            this.game = game;
            id = game.NextSearchId();
        }

        public int ID => id;

        public void AddSource(Flag flag)
        {
            queue.Enqueue(flag);
            flag.SearchNumber = (uint)id;
        }

        public bool Execute(FlagSearchFunc callback, bool land, bool transporter, object data)
        {
            for (int i = 0; i < SEARCH_MAX_DEPTH && queue.Count > 0; ++i)
            {
                var flag = queue.Dequeue();

                if (callback(flag, data))
                {
                    // Clean up
                    queue.Clear();
                    return true;
                }

                var cycle = DirectionCycleCCW.CreateDefault();

                foreach (var direction in cycle)
                {
                    var otherFlag = flag.OtherEndPoints[(int)direction].Flag;

                    if (otherFlag == null)
                        continue;

                    if ((!land || !flag.IsWaterPath(direction)) &&
                        (!transporter || flag.HasTransporter(direction)) &&
                        otherFlag.SearchNumber != id)
                    {
                        otherFlag.SearchNumber = (uint)id;
                        otherFlag.SearchDirection = flag.SearchDirection;
                        otherFlag.Tag = flag.Tag;
                        queue.Enqueue(otherFlag);
                    }
                }
            }

            // Clean up
            queue.Clear();

            return false;
        }

        public static bool Single(Flag source,
            FlagSearchFunc callback, bool land, bool transporter, object data)
        {
            var search = new FlagSearch(source.Game);

            search.AddSource(source);

            return search.Execute(callback, land, transporter, data);
        }
    }
}
