/*
 * Flag.cs - Flag related functions.
 *
 * Copyright (C) 2013  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Text;
using System.Runtime.InteropServices;

namespace Freeserf
{
    using MapPos = UInt32;
    using ListSerfs = List<Serf>;

    public class SerfPathInfo
    {
        public int PathLength;
        public int SerfCount;
        public int FlagIndex;
        public Direction FlagDir;
        public int[] Serfs; // int[16]
    }

    public class Flag : GameObject
    {
        class ResourceSlot
        {
            public Resource.Type Type;
            public Direction Dir;
            public uint Dest;
        }

        public const int FLAG_MAX_RES_COUNT = 8;
        static readonly int[] MaxTransporters = new[] { 1, 2, 3, 4, 6, 8, 11, 15 };

        public MapPos Position { get; internal set; }
        public Direction SearchDir { get; set; }
        public int SearchNum { get; internal set; }
        public object Tag { get; set; } = null; // General purpose tagged object (used in Game.UpdateInventories)

        // Bit 0: Has connected road right
        // Bit 1: Has connected road down right
        // Bit 2: Has connected road down
        // Bit 3: Has connected road left
        // Bit 4: Has connected road up left
        // Bit 5: Has connected road up
        // Bit 6-7: Owner Index (0-3)
        int pathCon;
        // Bit 0: Is land road right (otherwise water)
        // Bit 1: Is land road down right (otherwise water)
        // Bit 2: Is land road down (otherwise water)
        // Bit 3: Is land road left (otherwise water)
        // Bit 4: Is land road up left (otherwise water)
        // Bit 5: Is land road up (otherwise water)
        // Bit 6: Has connected building
        // Bit 7: Has unscheduled resources
        int endPoint;
        ResourceSlot[] slot = new ResourceSlot[FLAG_MAX_RES_COUNT];
        // Bit 0: Has transporter at road right
        // Bit 1: Has transporter at  road down right
        // Bit 2: Has transporter at  road down
        // Bit 3: Has transporter at  road left
        // Bit 4: Has transporter at  road up left
        // Bit 5: Has transporter at  road up
        // Bit 6: Unused?
        // Bit 7: Serf request failed
        int transporter;
        uint[] length = new uint[6];

        internal struct OtherEndpoint
        {
            private object obj;
            public Building Building { get { return obj as Building; } set { obj = value; } }
            public Flag Flag { get { return obj as Flag; } set { obj = value; } }
            public object Object { get { return obj; } set { obj = value; } }
        }

        int[] otherEndDir = new int[6];

        internal OtherEndpoint[] OtherEndPoints { get; } = new OtherEndpoint[6];

        // Bit 0-5: Unused?
        // Bit 6: Flag has an associated inventory building
        // Bit 7: Associated invertory building accepts serfs
        uint buildingFlags;
        // Bit 0-6: Unused?
        // Bit 7: Associated invertory building accepts resources
        uint buildingFlags2;

        public Flag(Game game, uint index)
            : base(game, index)
        {
            Position = 0u;
            SearchNum = 0;
            SearchDir = Direction.Right;
            pathCon = 0;
            endPoint = 0;
            transporter = 0;

            for (int j = 0; j < FLAG_MAX_RES_COUNT; ++j)
            {
                slot[j] = new ResourceSlot()
                {
                    Type = Resource.Type.None,
                    Dest = 0u,
                    Dir = Direction.None
                };
            }

            buildingFlags = 0u;
            buildingFlags2 = 0u;

            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction dir in cycle)
            {
                int i = (int)dir;

                length[i] = 0u;
                otherEndDir[i] = 0;
                OtherEndPoints[i].Flag = null;
            }
        }

        /* Bitmap of all directions with outgoing paths. */
        public int Paths()
        {
            return pathCon & 0x3f;
        }

        public void AddPath(Direction dir, bool water)
        {
            int bit = Misc.Bit((int)dir);

            pathCon |= bit;

            if (water)
            {
                endPoint &= ~bit;
            }
            else
            {
                endPoint |= bit;
            }

            transporter &= ~bit;
        }

        public void DeletePath(Direction dir)
        {
            int bit = Misc.Bit((int)dir);

            pathCon &= ~bit;
            endPoint &= ~bit;
            transporter &= ~bit;

            if (SerfRequested(dir))
            {
                CancelSerfRequest(dir);

                uint dest = Game.Map.GetObjectIndex(Position);

                foreach (Serf serf in Game.GetSerfsRelatedTo(dest, dir))
                {
                    serf.PathDeleted(dest, dir);
                }
            }

            otherEndDir[(int)dir] &= 0x78;
            OtherEndPoints[(int)dir].Flag = null;

            /* Mark resource path for recalculation if they would
            have followed the removed path. */
            InvalidateResourcePath(dir);
        }

        /* Whether a path exists in a given direction. */
        public bool HasPath(Direction dir)
        {
            return (pathCon & (1 << ((int)dir))) != 0;
        }

        public void PrioritizePickup(Direction dir, Player player)
        {
            int resNext = -1;
            int resPrio = -1;

            for (int i = 0; i < FLAG_MAX_RES_COUNT; ++i)
            {
                if (slot[i].Type != Resource.Type.None)
                {
                    /* Use flag_prio to prioritize resource pickup. */
                    Direction resDir = slot[i].Dir;
                    Resource.Type resType = slot[i].Type;
                    var flagPrio = player.GetFlagPriority(resType);

                    if (resDir == dir && flagPrio > resPrio)
                    {
                        resNext = i;
                        resPrio = flagPrio;
                    }
                }
            }

            otherEndDir[(int)dir] &= 0x78;

            if (resNext > -1)
                otherEndDir[(int)dir] |= Misc.Bit(7) | resNext;
        }

        /* Owner of this flag. */
        public uint GetOwner()
        {
            return (uint)(pathCon >> 6) & 3u;
        }

        public void SetOwner(uint owner)
        {
            pathCon = (int)((owner << 6) | ((uint)pathCon & 0x3fu));
        }

        /* Bitmap showing whether the outgoing paths are land paths. */
        public int LandPaths()
        {
            return endPoint & 0x3f;
        }

        /* Whether the path in the given direction is a water path. */
        public bool IsWaterPath(Direction dir)
        {
            return (endPoint & (1 << ((int)dir))) == 0;
        }

        /* Whether a building is connected to this flag. If so, the pointer to
        the other endpoint is a valid building pointer. (Always at UP LEFT direction). */
        public bool HasBuilding()
        {
            return ((endPoint >> 6) & 1) != 0;
        }

        /* Whether resources exist that are not yet scheduled. */
        public bool HasResources()
        {
            return ((endPoint >> 7) & 1) != 0;
        }

        /* Bitmap showing whether the outgoing paths have transporters
        servicing them. */
        public int Transporters()
        {
            return transporter & 0x3f;
        }

        /* Whether the path in the given direction has a transporter
        serving it. */
        public bool HasTransporter(Direction dir)
        {
            return (transporter & (1 << ((int)dir))) != 0;
        }

        /* Whether this flag has tried to request a transporter without success. */
        public bool SerfRequestFail()
        {
            return ((transporter >> 7) & 1) != 0;
        }

        public void SerfRequestClear()
        {
            transporter &= ~Misc.Bit(7);
        }

        /* Current number of transporters on path. */
        public uint FreeTransporterCount(Direction dir)
        {
            return length[(int)dir] & 0xfu;
        }

        public void TransporterToServe(Direction dir)
        {
            --length[(int)dir];
        }

        /* Length category of path determining max number of transporters. */
        public uint LengthCategory(Direction dir)
        {
            return (length[(int)dir] >> 4) & 7u;
        }

        /* Whether a transporter serf was successfully requested for this path. */
        public bool SerfRequested(Direction dir)
        {
            return ((length[(int)dir] >> 7) & 1) != 0;
        }

        public void CancelSerfRequest(Direction dir)
        {
            length[(int)dir] &= ~Misc.BitU(7);
        }

        public void CompleteSerfRequest(Direction dir)
        {
            length[(int)dir] &= ~Misc.BitU(7);
            ++length[(int)dir];
        }

        /* The slot that is scheduled for pickup by the given path. */
        public uint ScheduledSlot(Direction dir)
        {
            return (uint)otherEndDir[(int)dir] & 7u;
        }

        /* The direction from the other endpoint leading back to this flag. */
        public Direction GetOtherEndDir(Direction dir)
        {
            return (Direction)((otherEndDir[(int)dir] >> 3) & 7);
        }

        public Flag GetOtherEndFlag(Direction dir)
        {
            return OtherEndPoints[(int)dir].Flag;
        }

        /* Whether the given direction has a resource pickup scheduled. */
        public bool IsScheduled(Direction dir)
        {
            return ((otherEndDir[(int)dir] >> 7) & 1) != 0;
        }

        public bool PickUpResource(uint fromSlot, ref Resource.Type res, ref uint dest)
        {
            if (fromSlot >= FLAG_MAX_RES_COUNT)
            {
                throw new ExceptionFreeserf("Wrong flag slot index.");
            }

            if (slot[fromSlot].Type == Resource.Type.None)
            {
                return false;
            }

            res = slot[fromSlot].Type;
            dest = slot[fromSlot].Dest;
            slot[fromSlot].Type = Resource.Type.None;
            slot[fromSlot].Dir = Direction.None;

            FixScheduled();

            return true;
        }

        public bool DropResource(Resource.Type res, uint dest)
        {
            if (res < Resource.Type.MinValue || res > Resource.Type.MaxValue)
            {
                throw new ExceptionFreeserf("Wrong resource type.");
            }

            for (int i = 0; i < FLAG_MAX_RES_COUNT; ++i)
            {
                if (slot[i].Type == Resource.Type.None)
                {
                    slot[i].Type = res;
                    slot[i].Dest = dest;
                    slot[i].Dir = Direction.None;
                    endPoint |= Misc.Bit(7);

                    return true;
                }
            }

            return false;
        }

        public bool HasEmptySlot()
        {
            return slot.Any(s => s.Type == Resource.Type.None);
        }

        public void RemoveAllResources()
        {
            for (int i = 0; i < FLAG_MAX_RES_COUNT; ++i)
            {
                var res = slot[i].Type;

                if (res != Resource.Type.None)
                {
                    Game.CancelTransportedResource(res, slot[i].Dest);
                    Game.LoseResource(res);
                }
            }
        }

        public Resource.Type GetResourceAtSlot(int slot)
        {
            return this.slot[slot].Type;
        }

        /* Whether this flag has an inventory building. */
        public bool HasInventory()
        {
            return Misc.BitTest(buildingFlags, 6);
        }
  
        /* Whether this inventory accepts resources. */
        public bool AcceptsResources()
        {
            return Misc.BitTest(buildingFlags2, 7);
        }

        /* Whether this inventory accepts serfs. */
        public bool AcceptsSerfs()
        {
            return Misc.BitTest(buildingFlags, 7);
        }

        public void SetHasInventory()
        {
            buildingFlags |= Misc.BitU(6);
        }

        public void SetAcceptsResources(bool accepts)
        {
            Misc.SetBit(ref buildingFlags2, 7, accepts);
        }

        public void SetAcceptsSerfs(bool accepts)
        {
            Misc.SetBit(ref buildingFlags, 7, accepts);
        }

        public void ClearFlags()
        {
            buildingFlags = 0u;
            buildingFlags2 = 0u;
        }

        public void ReadFrom(SaveReaderBinary reader)
        {
            Position = 0; /* Set correctly later. */
            SearchNum = reader.ReadWord(); // 0
            SearchDir = (Direction)reader.ReadByte(); // 2
            pathCon = reader.ReadByte(); // 3
            endPoint = reader.ReadByte(); // 4
            transporter = reader.ReadByte(); // 5

            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction j in cycle)
            {
                length[(int)j] = reader.ReadByte(); // 6 + j
            }

            for (int j = 0; j < 8; ++j)
            {
                byte val = reader.ReadByte(); // 12 + j

                slot[j].Type = (Resource.Type)((val & 0x1f) - 1);
                slot[j].Dir = (Direction)(((val >> 5) & 7) - 1);
            }

            for (int j = 0; j < 8; ++j)
            {
                slot[j].Dest = reader.ReadWord(); // 20 + j*2
            }

            // base + 36
            cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction j in cycle)
            {
                int offset = (int)reader.ReadDWord();

                /* Other endpoint could be a building in direction up left. */
                if (j == Direction.UpLeft && HasBuilding())
                {
                    OtherEndPoints[(int)j].Building = Game.CreateBuilding(offset / 18);
                }
                else
                {
                    if (offset < 0)
                    {
                        OtherEndPoints[(int)j].Flag = null;
                    }
                    else
                    {
                        OtherEndPoints[(int)j].Flag = Game.CreateFlag(offset / 70);
                    }
                }
            }

            // base + 60
            cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction j in cycle)
            {
                otherEndDir[(int)j] = reader.ReadByte();
            }

            buildingFlags = reader.ReadByte(); // 66

            byte prio = reader.ReadByte(); // 67

            if (HasBuilding())
            {
                OtherEndPoints[(int)Direction.UpLeft].Building.SetPriorityInStock(0, prio);
            }

            buildingFlags2 = reader.ReadByte(); // 68

            prio = reader.ReadByte(); // 69

            if (HasBuilding())
            {
                OtherEndPoints[(int)Direction.UpLeft].Building.SetPriorityInStock(1, prio);
            }
        }

        public void ReadFrom(SaveReaderText reader)
        {
            uint x = 0;
            uint y = 0;
            x = reader.Value("pos")[0].ReadUInt();
            y = reader.Value("pos")[1].ReadUInt();
            Position = Game.Map.Pos(x, y);
            SearchNum = reader.Value("search_num").ReadInt();
            SearchDir = reader.Value("search_dir").ReadDirection();
            pathCon = reader.Value("path_con").ReadInt();
            endPoint = reader.Value("endpoints").ReadInt();
            transporter = reader.Value("transporter").ReadInt();

            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction i in cycle)
            {
                length[(int)i] = reader.Value("length")[(int)i].ReadUInt();
                int objectIndex = reader.Value("other_endpoint")[(int)i].ReadInt();

                if (i == Direction.UpLeft && HasBuilding())
                {
                    OtherEndPoints[(int)i].Building = Game.CreateBuilding(objectIndex);
                }
                else
                {
                    Flag otherFlag = null;

                    if (objectIndex != 0)
                    {
                        otherFlag = Game.CreateFlag(objectIndex);
                    }

                    OtherEndPoints[(int)i].Flag = otherFlag;
                }

                otherEndDir[(int)i] = reader.Value("other_end_dir")[(int)i].ReadInt();
            }

            for (int i = 0; i < FLAG_MAX_RES_COUNT; ++i)
            {
                slot[i].Type = reader.Value("slot.type")[i].ReadResource();
                slot[i].Dir = reader.Value("slot.dir")[i].ReadDirection();
                slot[i].Dest = reader.Value("slot.dest")[i].ReadUInt();
            }

            buildingFlags = reader.Value("bld_flags").ReadUInt();
            buildingFlags2 = reader.Value("bld2_flags").ReadUInt();
        }

        public void WriteTo(SaveWriterText writer)
        {
            writer.Value("pos").Write(Game.Map.PosColumn(Position));
            writer.Value("pos").Write(Game.Map.PosRow(Position));
            writer.Value("search_num").Write(SearchNum);
            writer.Value("search_dir").Write((int)SearchDir);
            writer.Value("path_con").Write(pathCon);
            writer.Value("endpoints").Write(endPoint);
            writer.Value("transporter").Write(transporter);

            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction d in cycle)
            {
                writer.Value("length").Write(length[(int)d]);

                if (d == Direction.UpLeft && HasBuilding())
                {
                    writer.Value("other_endpoint").Write(OtherEndPoints[(int)d].Building.Index);
                }
                else
                {
                    if (HasPath(d))
                    {
                        writer.Value("other_endpoint").Write(OtherEndPoints[(int)d].Flag.Index);
                    }
                    else
                    {
                        writer.Value("other_endpoint").Write(0u);
                    }
                }

                writer.Value("other_end_dir").Write(otherEndDir[(int)d]);
            }

            for (int i = 0; i < FLAG_MAX_RES_COUNT; ++i)
            {
                writer.Value("slot.type").Write((int)slot[i].Type);
                writer.Value("slot.dir").Write((int)slot[i].Dir);
                writer.Value("slot.dest").Write(slot[i].Dest);
            }

            writer.Value("bld_flags").Write(buildingFlags);
            writer.Value("bld2_flags").Write(buildingFlags2);
        }

        public void ResetTransport(Flag other)
        {
            for (int slot = 0; slot < FLAG_MAX_RES_COUNT; ++slot)
            {
                if (other.slot[slot].Type != Resource.Type.None &&
                    other.slot[slot].Dest == Index)
                {
                    other.slot[slot].Dest = 0;
                    other.endPoint |= Misc.Bit(7);

                    if (other.slot[slot].Dir != Direction.None)
                    {
                        Direction dir = other.slot[slot].Dir;
                        Player player = Game.GetPlayer(other.GetOwner());

                        other.PrioritizePickup(dir, player);
                    }
                }
            }
        }

        public void ResetDestinationOfStolenResources()
        {
            for (int i = 0; i < FLAG_MAX_RES_COUNT; ++i)
            {
                if (slot[i].Type != Resource.Type.None)
                {
                    Resource.Type res = slot[i].Type;
                    Game.CancelTransportedResource(res, slot[i].Dest);
                    slot[i].Dest = 0;
                }
            }
        }

        public void LinkBuilding(Building building)
        {
            OtherEndPoints[(int)Direction.UpLeft].Building = building;
            endPoint |= Misc.Bit(6);
        }

        public void UnlinkBuilding()
        {
            OtherEndPoints[(int)Direction.UpLeft].Building = null;
            endPoint &= ~Misc.Bit(6);
            ClearFlags();
        }

        public Building GetBuilding()
        {
            return OtherEndPoints[(int)Direction.UpLeft].Building;
        }

        public void InvalidateResourcePath(Direction dir)
        {
            for (int i = 0; i < FLAG_MAX_RES_COUNT; ++i)
            {
                if (slot[i].Type != Resource.Type.None && slot[i].Dir == dir)
                {
                    slot[i].Dir = Direction.None;
                    endPoint |= Misc.Bit(7);
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
            FlagPointerHelper dest = data as FlagPointerHelper;

            if (flag.AcceptsResources())
            {
                dest.Value = flag;
                return true;
            }

            return false;
        }

        static bool FlagSearchInventorySearchCB(Flag flag, object data)
        {
            IntPointerHelper destIndex = data as IntPointerHelper;

            if (flag.AcceptsSerfs())
            {
                Building building = flag.GetBuilding();

                destIndex.Value = (int)building.GetFlagIndex();

                return true;
            }

            return false;
        }

        public int FindNearestInventoryForResource()
        {
            FlagPointerHelper dest = new FlagPointerHelper()
            {
                Value = null
            };

            FlagSearch.Single(this, FindNearestInventorySearchCB, false, true, dest);

            if (dest.Value != null)
                return (int)dest.Value.Index;

            return -1;
        }

        public int FindNearestInventoryForSerf()
        {
            IntPointerHelper destIndex = new IntPointerHelper()
            {
                Value = -1
            };

            FlagSearch.Single(this, FlagSearchInventorySearchCB, true, false, destIndex);

            return destIndex.Value;
        }

        public void LinkWithFlag(Flag destFlag, bool waterPath, uint length, Direction inDir, Direction outDir)
        {
            destFlag.AddPath(inDir, waterPath);
            AddPath(outDir, waterPath);

            destFlag.otherEndDir[(int)inDir] = (destFlag.otherEndDir[(int)inDir] & 0xc7) | ((int)outDir << 3);
            otherEndDir[(int)outDir] = (otherEndDir[(int)outDir] & 0xc7) | ((int)inDir << 3);

            uint len = GetRoadLengthValue(length);

            destFlag.length[(int)inDir] = len << 4;
            this.length[(int)outDir] = len << 4;

            destFlag.OtherEndPoints[(int)inDir].Flag = this;
            OtherEndPoints[(int)outDir].Flag = destFlag;
        }

        public void Update()
        {
            /* Count and store in bitfield which directions
               have strictly more than 0,1,2,3 slots waiting. */
            int[] resWaiting = new int[4] { 0, 0, 0, 0 };

            for (int j = 0; j < FLAG_MAX_RES_COUNT; ++j)
            {
                if (this.slot[j].Type != Resource.Type.None && this.slot[j].Dir != Direction.None)
                {
                    Direction resDir = slot[j].Dir;

                    for (int k = 0; k < 4; k++)
                    {
                        if (!Misc.BitTest(resWaiting[k], (int)resDir))
                        {
                            resWaiting[k] |= Misc.Bit((int)resDir);
                            break;
                        }
                    }
                }
            }

            /* Count of total resources waiting at flag */
            int waitingCount = 0;

            if (HasResources())
            {
                endPoint &= ~Misc.Bit(7);

                for (int slot = 0; slot < FLAG_MAX_RES_COUNT; slot++)
                {
                    if (this.slot[slot].Type != Resource.Type.None)
                    {
                        ++waitingCount;

                        /* Only schedule the slot if it has not already
                         been scheduled for fetch. */
                        int resDir = (int)this.slot[slot].Dir;

                        if (resDir < 0)
                        {
                            if (this.slot[slot].Dest != 0)
                            {
                                /* Destination is known */
                                ScheduleSlotToKnownDest(slot, resWaiting);
                            }
                            else
                            {
                                /* Destination is not known */
                                ScheduleSlotToUnknownDest(slot);
                            }
                        }
                    }
                }
            }

            /* Update transporter flags, decide if serf needs to be sent to road */
            var cycle = DirectionCycleCCW.CreateDefault();

            foreach (Direction j in cycle)
            {
                if (HasPath(j))
                {
                    if (SerfRequested(j))
                    {
                        if (Misc.BitTest(resWaiting[2], (int)j))
                        {
                            if (waitingCount >= 7)
                            {
                                transporter &= Misc.Bit((int)j);
                            }
                        }
                        else if (FreeTransporterCount(j) != 0)
                        {
                            transporter |= Misc.Bit((int)j);
                        }
                    }
                    else if (FreeTransporterCount(j) == 0 || Misc.BitTest(resWaiting[2], (int)j))
                    {
                        int maxTransporters = MaxTransporters[LengthCategory(j)];

                        if (FreeTransporterCount(j) < (uint)maxTransporters && !SerfRequestFail()) 
                        {
                            if (!CallTransporter(j, IsWaterPath(j)))
                                transporter |= Misc.Bit(7);
                        }

                        if (waitingCount >= 7)
                        {
                            transporter &= Misc.Bit((int)j);
                        }
                    }
                    else
                    {
                        transporter |= Misc.Bit((int)j);
                    }
                }
            }
        }

        /* Get road length category value for real length.
           Determines number of serfs servicing the path segment.(?) */
        public static uint GetRoadLengthValue(uint length)
        {
            if (length >= 24) return 7;
            else if (length >= 18) return 6;
            else if (length >= 13) return 5;
            else if (length >= 10) return 4;
            else if (length >= 7) return 3;
            else if (length >= 6) return 2;
            else if (length >= 4) return 1;
            return 0;
        }

        public void RestorePathSerfInfo(Direction dir, SerfPathInfo data)
        {
            int[] maxPathSerfs = new[] { 1, 2, 3, 4, 6, 8, 11, 15 };

            Flag otherFlag = Game.GetFlag((uint)data.FlagIndex);
            Direction otherDir = data.FlagDir;

            AddPath(dir, otherFlag.IsWaterPath(otherDir));

            otherFlag.transporter &= ~Misc.Bit((int)otherDir);

            uint len = Flag.GetRoadLengthValue((uint)data.PathLength);

            length[(int)dir] = len << 4;
            otherFlag.length[(int)otherDir] = (0x80 & otherFlag.length[(int)otherDir]) | (len << 4);

            if (otherFlag.SerfRequested(otherDir))
            {
                length[(int)dir] |= Misc.BitU(7);
            }

            otherEndDir[(int)dir] = (otherEndDir[(int)dir] & 0xc7) | ((int)otherDir << 3);
            otherFlag.otherEndDir[(int)otherDir] = (otherFlag.otherEndDir[(int)otherDir] & 0xc7) | ((int)dir << 3);

            OtherEndPoints[(int)dir].Flag = otherFlag;
            otherFlag.OtherEndPoints[(int)otherDir].Flag = this;

            int maxSerfs = maxPathSerfs[len];

            if (SerfRequested(dir))
                --maxSerfs;

            if (data.SerfCount > maxSerfs)
            {
                for (int i = 0; i < data.SerfCount - maxSerfs; ++i)
                {
                    Serf serf = Game.GetSerf((uint)data.Serfs[i]);
                    serf.RestorePathSerfInfo();
                }
            }

            int min = Math.Min(data.SerfCount, maxSerfs);

            if (min > 0)
            {
                /* There are still transporters on the paths. */
                transporter |= Misc.Bit((int)dir);
                otherFlag.transporter |= Misc.Bit((int)otherDir);

                length[(int)dir] |= (uint)min;
                otherFlag.length[(int)otherDir] |= (uint)min;
            }
        }

        public void ClearSearchId()
        {
            SearchNum = 0;
        }

        public bool CanDemolish()
        {
            int connected = 0;
            object otherEnd = null;
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction d in cycle)
            {
                if (HasPath(d))
                {
                    if (IsWaterPath(d))
                        return false;

                    ++connected;

                    if (otherEnd != null)
                    {
                        if (OtherEndPoints[(int)d].Object == otherEnd)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        otherEnd = OtherEndPoints[(int)d].Object;
                    }
                }
            }

            if (connected == 2)
                return true;

            return false;
        }

        public void MergePaths(MapPos pos)
        {
            Map map = Game.Map;

            if (map.Paths(pos) == 0)
            {
                return;
            }

            Direction path1Dir = Direction.Right;
            Direction path2Dir = Direction.Right;

            /* Find first direction */
            var cycleCW = DirectionCycleCW.CreateDefault();

            foreach (Direction d in cycleCW)
            {
                if (map.HasPath(pos, d))
                {
                    path1Dir = d;
                    break;
                }
            }

            /* Find second direction */
            var cycleCCW = DirectionCycleCCW.CreateDefault();

            foreach (Direction d in cycleCCW)
            {
                if (map.HasPath(pos, d))
                {
                    path2Dir = d;
                    break;
                }
            }

            SerfPathInfo path1Data = new SerfPathInfo();
            SerfPathInfo path2Data = new SerfPathInfo();

            path1Data.Serfs = new int[16];
            path2Data.Serfs = new int[16];

            FillPathSerfInfo(Game, pos, path1Dir, path1Data);
            FillPathSerfInfo(Game, pos, path2Dir, path2Data);

            Flag flag1 = Game.GetFlag((uint)path1Data.FlagIndex);
            Flag flag2 = Game.GetFlag((uint)path2Data.FlagIndex);
            Direction dir1 = path1Data.FlagDir;
            Direction dir2 = path2Data.FlagDir;

            flag1.otherEndDir[(int)dir1] = (flag1.otherEndDir[(int)dir1] & 0xc7) | ((int)dir2 << 3);
            flag2.otherEndDir[(int)dir2] = (flag2.otherEndDir[(int)dir2] & 0xc7) | ((int)dir1 << 3);

            flag1.OtherEndPoints[(int)dir1].Flag = flag2;
            flag2.OtherEndPoints[(int)dir2].Flag = flag1;

            flag1.transporter &= ~Misc.Bit((int)dir1);
            flag2.transporter &= ~Misc.Bit((int)dir2);

            uint len = Flag.GetRoadLengthValue((uint)(path1Data.PathLength + path2Data.PathLength));
            flag1.length[(int)dir1] = len << 4;
            flag2.length[(int)dir2] = len << 4;

            int maxSerfs = MaxTransporters[flag1.LengthCategory(dir1)];
            int serfCount = path1Data.SerfCount + path2Data.SerfCount;

            if (serfCount > 0)
            {
                flag1.transporter |= Misc.Bit((int)dir1);
                flag2.transporter |= Misc.Bit((int)dir2);

                if (serfCount > maxSerfs)
                {
                    /* TODO 59B8B */
                }

                flag1.length[(int)dir1] += (uint)serfCount;
                flag2.length[(int)dir2] += (uint)serfCount;
            }

            /* Update serfs with reference to this flag. */
            var serfs = Game.GetSerfsRelatedTo(flag1.Index, dir1);
            var serfs2 = Game.GetSerfsRelatedTo(flag2.Index, dir2);

            serfs.AddRange(serfs2);

            foreach (Serf serf in serfs)
            {
                serf.PathMerged2(flag1.Index, dir1, flag2.Index, dir2);
            }
        }

        /* Find a transporter at pos and change it to state. */
        static int ChangeTransporterStateAtPos(Game game, MapPos pos, Serf.State state)
        {
            foreach (Serf serf in game.GetSerfsAtPos(pos))
            {
                if (serf.ChangeTransporterStateAtPos(pos, state))
                {
                    return (int)serf.Index;
                }
            }

            return -1;
        }

        static int WakeTransporterAtFlag(Game game, MapPos pos)
        {
            return ChangeTransporterStateAtPos(game, pos, Serf.State.WakeAtFlag);
        }

        static int WakeTransporterOnPath(Game game, MapPos pos)
        {
            return ChangeTransporterStateAtPos(game, pos, Serf.State.WakeOnPath);
        }

        public static void FillPathSerfInfo(Game game, MapPos pos, Direction dir, SerfPathInfo data)
        {
            Map map = game.Map;

            if (map.GetIdleSerf(pos))
                WakeTransporterAtFlag(game, pos);

            int serfCounter = 0;
            int pathLength = 0;

            /* Handle first position. */
            if (map.HasSerf(pos))
            {
                Serf serf = game.GetSerfAtPos(pos);

                if (serf.SerfState == Serf.State.Transporting && serf.GetWalkingWaitCounter() != -1)
                {
                    int d = serf.GetWalkingDir();

                    if (d < 0)
                        d += 6;

                    if ((int)dir == d)
                    {
                        serf.SetWalkingWaitCounter(0);
                        data.Serfs[serfCounter++] = (int)serf.Index;
                    }
                }
            }

            /* Trace along the path to the flag at the other end. */
            int paths = 0;

            while (true)
            {
                ++pathLength;
                pos = map.Move(pos, dir);
                paths = (int)map.Paths(pos);
                paths &= ~Misc.Bit((int)dir.Reverse());

                if (map.HasFlag(pos))
                    break;

                /* Find out which direction the path follows. */
                var cycle = DirectionCycleCW.CreateDefault();

                foreach (Direction d in cycle)
                {
                    if (Misc.BitTest(paths, (int)d))
                    {
                        dir = d;
                        break;
                    }
                }

                /* Check if there is a transporter waiting here. */
                if (map.GetIdleSerf(pos))
                {
                    int index = WakeTransporterOnPath(game, pos);

                    if (index >= 0)
                        data.Serfs[serfCounter++] = index;
                }

                /* Check if there is a serf occupying this space. */
                if (map.HasSerf(pos))
                {
                    Serf serf = game.GetSerfAtPos(pos);

                    if (serf.SerfState == Serf.State.Transporting && serf.GetWalkingWaitCounter() != -1)
                    {
                        serf.SetWalkingWaitCounter(0);
                        data.Serfs[serfCounter++] = (int)serf.Index;
                    }
                }
            }

            /* Handle last position. */
            if (map.HasSerf(pos))
            {
                Serf serf = game.GetSerfAtPos(pos);

                if ((serf.SerfState == Serf.State.Transporting && serf.GetWalkingWaitCounter() != -1) || serf.SerfState == Serf.State.Delivering)
                {
                    int d = serf.GetWalkingDir();

                    if (d < 0)
                        d += 6;

                    if (d == (int)dir.Reverse())
                    {
                        serf.SetWalkingWaitCounter(0);
                        data.Serfs[serfCounter++] = (int)serf.Index;
                    }
                }
            }

            /* Fill the rest of the struct. */
            data.PathLength = pathLength;
            data.SerfCount = serfCounter;
            data.FlagIndex = (int)map.GetObjectIndex(pos);
            data.FlagDir = dir.Reverse();
        }

        void FixScheduled()
        {
            bool anyResources = slot.Any(s => s.Type != Resource.Type.None);

            Misc.SetBit(ref endPoint, 7, anyResources);
        }

        class ScheduleUnknownDestData
        {
            public Resource.Type Resource;
            public int MaxPrio;
            public Flag Flag;
        }

        static bool ScheduleUnknownDestCB(Flag flag, object data)
        {
            var destData = data as ScheduleUnknownDestData;

            if (flag.HasBuilding())
            {
                Building building = flag.GetBuilding();

                int buildPrio = building.GetMaxPriorityForResource(destData.Resource);

                if (buildPrio > destData.MaxPrio)
                {
                    destData.MaxPrio = buildPrio;
                    destData.Flag = flag;
                }

                if (destData.MaxPrio > 204)
                    return true;
            }

            return false;
        }

        public bool ScheduleKnownDestCB(Flag src, Flag dest, int slot)
        {
            if (this == dest)
            {
                /* Destination found */
                if ((int)SearchDir != 6)
                {
                    if (!src.IsScheduled(SearchDir))
                    {
                        /* Item is requesting to be fetched */
                        src.otherEndDir[(int)SearchDir] = Misc.Bit(7) | (src.otherEndDir[(int)SearchDir] & 0x78) | slot;
                    }
                    else
                    {
                        Player player = Game.GetPlayer(GetOwner());
                        int otherDir = src.otherEndDir[(int)SearchDir];
                        int prioOld = player.GetFlagPriority(src.slot[otherDir & 7].Type);
                        int prioNew = player.GetFlagPriority(src.slot[slot].Type);

                        if (prioNew > prioOld)
                        {
                            /* This item has the highest priority now */
                            src.otherEndDir[(int)SearchDir] = (src.otherEndDir[(int)SearchDir] & 0xf8) | slot;
                        }

                        src.slot[slot].Dir = SearchDir;
                    }
                }

                return true;
            }

            return false;
        }

        /* Resources which should be routed directly to
           buildings requesting them. Resources not listed
           here will simply be moved to an inventory. */
        static readonly int[] routable = new int[]
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
            Resource.Type res = this.slot[slot].Type;

            if (routable[(int)res] != 0)
            {
                FlagSearch search = new FlagSearch(Game);

                search.AddSource(this);

                /* Handle food as one resource group */
                if (res == Resource.Type.Meat ||
                    res == Resource.Type.Fish ||
                    res == Resource.Type.Bread)
                {
                    res = Resource.Type.GroupFood;
                }

                ScheduleUnknownDestData data = new ScheduleUnknownDestData()
                {
                    Resource = res,
                    Flag = null,
                    MaxPrio = 0
                };

                search.Execute(ScheduleUnknownDestCB, false, true, data);

                if (data.Flag != null)
                {
                    Log.Verbose.Write("game", $"dest for flag {Index} res {slot} found: flag {data.Flag.Index}");
                    Building destBuilding = data.Flag.OtherEndPoints[(int)Direction.UpLeft].Building;

                    if (!destBuilding.AddRequestedResource(res, true))
                    {
                        throw new ExceptionFreeserf("Failed to request resource.");
                    }

                    this.slot[slot].Dest = destBuilding.GetFlagIndex();
                    endPoint |= Misc.Bit(7);

                    return;
                }
            }

            /* Either this resource cannot be routed to a destination
               other than an inventory or such destination could not be
               found. Send to inventory instead. */
            int r = FindNearestInventoryForResource();

            if (r < 0 || r == Index)
            {
                /* No path to inventory was found, or
                   resource is already at destination.
                   In the latter case we need to move it
                   forth and back once before it can be delivered. */
                if (Transporters() == 0)
                {
                    endPoint |= Misc.Bit(7);
                }
                else
                {
                    Direction dir = Direction.None;
                    var cycle = DirectionCycleCCW.CreateDefault();

                    foreach (Direction d in cycle)
                    {
                        if (HasTransporter(d))
                        {
                            dir = d;
                            break;
                        }
                    }

                    if ((dir < Direction.Right) || (dir > Direction.Up))
                    {
                        throw new ExceptionFreeserf("Failed to request resource.");
                    }

                    if (!IsScheduled(dir))
                    {
                        otherEndDir[(int)dir] = Misc.Bit(7) | (otherEndDir[(int)dir] & 0x38) | slot;
                    }

                    this.slot[slot].Dir = dir;
                }
            }
            else
            {
                this.slot[slot].Dest = (MapPos)r;
                endPoint |= Misc.Bit(7);
            }
        }

        class ScheduleKnownDestData
        {
            public Flag Source;
            public Flag Dest;
            public int Slot;
        }

        static bool ScheduleKnownDestCB(Flag flag, object data)
        {
            var destData = data as ScheduleKnownDestData;

            return flag.ScheduleKnownDestCB(destData.Source, destData.Dest, destData.Slot);
        }

        // resWaiting = int[4]
        void ScheduleSlotToKnownDest(int slot, int[] resWaiting)
        {
            FlagSearch search = new FlagSearch(Game);

            SearchNum = search.ID;
            SearchDir = Direction.None;
            int tr = Transporters();
            int sources = 0;

            /* Directions where transporters are idle (zero slots waiting) */
            int flags = (resWaiting[0] ^ 0x3f) & transporter;

            if (flags != 0)
            {
                var cycle = DirectionCycleCCW.CreateDefault();

                foreach (Direction k in cycle)
                {
                    int i = (int)k;

                    if (Misc.BitTest(flags, i))
                    {
                        tr &= ~Misc.Bit(i);

                        Flag otherFlag = OtherEndPoints[i].Flag;

                        if (otherFlag.SearchNum != search.ID)
                        {
                            otherFlag.SearchDir = k;
                            search.AddSource(otherFlag);
                            ++sources;
                        }
                    }
                }
            }

            if (tr != 0)
            {
                for (int j = 0; j < 3; ++j)
                {
                    flags = resWaiting[j] ^ resWaiting[j + 1];

                    var cycle = DirectionCycleCCW.CreateDefault();

                    foreach (Direction k in cycle)
                    {
                        int i = (int)k;

                        if (Misc.BitTest(flags, i))
                        {
                            tr &= ~Misc.Bit(i);

                            Flag otherFlag = OtherEndPoints[i].Flag;

                            if (otherFlag.SearchNum != search.ID)
                            {
                                otherFlag.SearchDir = k;
                                search.AddSource(otherFlag);
                                ++sources;
                            }
                        }
                    }
                }

                if (tr != 0)
                {
                    flags = resWaiting[3];

                    var cycle = DirectionCycleCCW.CreateDefault();

                    foreach (Direction k in cycle)
                    {
                        int i = (int)k;

                        if (Misc.BitTest(flags, i))
                        {
                            tr &= ~Misc.Bit(i);

                            Flag otherFlag = OtherEndPoints[i].Flag;

                            if (otherFlag.SearchNum != search.ID)
                            {
                                otherFlag.SearchDir = k;
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
                ScheduleKnownDestData data = new ScheduleKnownDestData()
                {
                    Source = this,
                    Dest = Game.GetFlag(this.slot[slot].Dest),
                    Slot = slot
                };

                bool r = search.Execute(ScheduleKnownDestCB, false, true, data);

                if (!r || data.Dest == this)
                {
                    /* Unable to deliver */
                    Game.CancelTransportedResource(this.slot[slot].Type, this.slot[slot].Dest);
                    this.slot[slot].Dest = 0u;
                    endPoint |= Misc.Bit(7);
                }
            }
            else
            {
                endPoint |= Misc.Bit(7);
            }
        }

        class SendSerfToRoadData
        {
            public Inventory Inventory;
            public bool Water;
        }

        static bool SendSerfToRoadSearchCB(Flag flag, object data)
        {
            var roadData = data as SendSerfToRoadData;

            if (flag.HasInventory())
            {
                /* Inventory reached */
                Building building = flag.GetBuilding();
                Inventory inventory = building.GetInventory();

                if (!roadData.Water)
                {
                    if (inventory.HaveSerf(Serf.Type.Transporter))
                    {
                        roadData.Inventory = inventory;
                        return true;
                    }
                }
                else
                {
                    if (inventory.HaveSerf(Serf.Type.Sailor))
                    {
                        roadData.Inventory = inventory;
                        return true;
                    }
                }

                if (roadData.Inventory == null && inventory.HaveSerf(Serf.Type.Generic) &&
                    (!roadData.Water || inventory.GetCountOf(Resource.Type.Boat) > 0))
                {
                    roadData.Inventory = inventory;
                }
            }

            return false;
        }

        bool CallTransporter(Direction dir, bool water)
        {
            Flag source2 = OtherEndPoints[(int)dir].Flag;
            Direction dir2 = GetOtherEndDir(dir);

            SearchDir = Direction.Right;
            source2.SearchDir = Direction.DownRight;

            FlagSearch search = new FlagSearch(Game);
            search.AddSource(this);
            search.AddSource(source2);

            SendSerfToRoadData data = new SendSerfToRoadData()
            {
                Inventory = null,
                Water = water
            };

            search.Execute(SendSerfToRoadSearchCB, true, false, data);

            Inventory inventory = data.Inventory;

            if (inventory == null)
            {
                return false;
            }

            Serf serf = data.Inventory.CallTransporter(water);
            Flag destFlag = Game.GetFlag(inventory.GetFlagIndex());

            length[(int)dir] |= Misc.BitU(7);
            source2.length[(int)dir2] |= Misc.BitU(7);

            Flag source = this;

            if (destFlag.SearchDir == source2.SearchDir)
            {
                source = source2;
                dir = dir2;
            }

            serf.GoOutFromInventory(inventory.Index, source.Index, (int)dir);

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
            flag.SearchNum = id;
        }

        public bool Execute(FlagSearchFunc callback, bool land, bool transporter, object data)
        {
            for (int i = 0; i < SEARCH_MAX_DEPTH && queue.Count > 0; ++i)
            {
                var flag = queue.Dequeue();

                if (callback(flag, data))
                {
                    /* Clean up */
                    queue.Clear();
                    return true;
                }

                var cycle = DirectionCycleCCW.CreateDefault();

                foreach (Direction dir in cycle)
                {
                    var otherFlag = flag.OtherEndPoints[(int)dir].Flag;

                    if (otherFlag == null)
                        continue;

                    if ((!land || !flag.IsWaterPath(dir)) &&
                        (!transporter || flag.HasTransporter(dir)) &&
                        otherFlag.SearchNum != id)
                    {
                        otherFlag.SearchNum = id;
                        otherFlag.SearchDir = flag.SearchDir;
                        otherFlag.Tag = flag.Tag;
                        queue.Enqueue(otherFlag);
                    }
                }
            }

            /* Clean up */
            queue.Clear();

            return false;
        }

        public static bool Single(Flag src, FlagSearchFunc callback, bool land, bool transporter, object data)
        {
            FlagSearch search = new FlagSearch(src.Game);

            search.AddSource(src);

            return search.Execute(callback, land, transporter, data);
        }
    }
}
