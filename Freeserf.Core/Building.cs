/*
 * Building.cs - Building related functions.
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

    public class Building : GameObject
    {
        public enum Type
        {
            None = 0,
            Fisher,
            Lumberjack,
            Boatbuilder,
            Stonecutter,
            StoneMine,
            CoalMine,
            IronMine,
            GoldMine,
            Forester,
            Stock,
            Hut,
            Farm,
            Butcher,
            PigFarm,
            Mill,
            Baker,
            Sawmill,
            SteelSmelter,
            ToolMaker,
            WeaponSmith,
            Tower,
            Fortress,
            GoldSmelter,
            Castle
        }

        // Max number of different types of resources accepted by buildings.
        const uint MaxStock = 3;

        class Stock
        {
            public Resource.Type Type;
            public uint Priority;
            public uint Available;
            public uint Requested;
            public uint Maximum;
        }

        class ConstructionInfo
        {
            public ConstructionInfo(Map.Object obj, uint planks, uint stones, uint phase1, uint phase2)
            {
                MapObject = obj;
                Planks = planks;
                Stones = stones;
                Phase1 = phase1;
                Phase2 = phase2;
            }

            public Map.Object MapObject;
            public uint Planks;
            public uint Stones;
            public uint Phase1;
            public uint Phase2;
        }

        static readonly ConstructionInfo[] ConstructionInfos = new ConstructionInfo[]
        {
            new ConstructionInfo(Map.Object.None,          0, 0,    0,    0),  // BUILDING_NONE
            new ConstructionInfo(Map.Object.SmallBuilding, 2, 0, 4096, 4096),  // BUILDING_FISHER
            new ConstructionInfo(Map.Object.SmallBuilding, 2, 0, 4096, 4096),  // BUILDING_LUMBERJACK
            new ConstructionInfo(Map.Object.SmallBuilding, 3, 0, 4096, 2048),  // BUILDING_BOATBUILDER
            new ConstructionInfo(Map.Object.SmallBuilding, 2, 0, 4096, 4096),  // BUILDING_STONECUTTER
            new ConstructionInfo(Map.Object.SmallBuilding, 4, 1, 2048, 1366),  // BUILDING_STONEMINE
            new ConstructionInfo(Map.Object.SmallBuilding, 5, 0, 2048, 1366),  // BUILDING_COALMINE
            new ConstructionInfo(Map.Object.SmallBuilding, 5, 0, 2048, 1366),  // BUILDING_IRONMINE
            new ConstructionInfo(Map.Object.SmallBuilding, 5, 0, 2048, 1366),  // BUILDING_GOLDMINE
            new ConstructionInfo(Map.Object.SmallBuilding, 2, 0, 4096, 4096),  // BUILDING_FORESTER
            new ConstructionInfo(Map.Object.LargeBuilding, 4, 3, 1366, 1024),  // BUILDING_STOCK
            new ConstructionInfo(Map.Object.SmallBuilding, 1, 1, 4096, 4096),  // BUILDING_HUT
            new ConstructionInfo(Map.Object.LargeBuilding, 4, 1, 2048, 1366),  // BUILDING_FARM
            new ConstructionInfo(Map.Object.LargeBuilding, 2, 1, 4096, 2048),  // BUILDING_BUTCHER
            new ConstructionInfo(Map.Object.LargeBuilding, 4, 1, 2048, 1366),  // BUILDING_PIGFARM
            new ConstructionInfo(Map.Object.SmallBuilding, 3, 1, 2048, 2048),  // BUILDING_MILL
            new ConstructionInfo(Map.Object.LargeBuilding, 2, 1, 4096, 2048),  // BUILDING_BAKER
            new ConstructionInfo(Map.Object.LargeBuilding, 3, 2, 2048, 1366),  // BUILDING_SAWMILL
            new ConstructionInfo(Map.Object.LargeBuilding, 3, 2, 2048, 1366),  // BUILDING_STEELSMELTER
            new ConstructionInfo(Map.Object.LargeBuilding, 3, 3, 2048, 1024),  // BUILDING_TOOLMAKER
            new ConstructionInfo(Map.Object.LargeBuilding, 2, 1, 4096, 2048),  // BUILDING_WEAPONSMITH
            new ConstructionInfo(Map.Object.LargeBuilding, 2, 3, 2048, 1366),  // BUILDING_TOWER
            new ConstructionInfo(Map.Object.LargeBuilding, 5, 5, 1024,  683),  // BUILDING_FORTRESS
            new ConstructionInfo(Map.Object.LargeBuilding, 4, 1, 2048, 1366),  // BUILDING_GOLDSMELTER
            new ConstructionInfo(Map.Object.Castle,        0, 0,  256,  256),  // BUILDING_CASTLE
        };

        /* Building under construction */
        bool constructing = true; /* Initial: unfinished building */
        /* Flags */
        uint threatLevel = 0;
        bool playingSfx = false;
        bool serfRequestFailed = false;
        bool serfRequested = false;
        bool burning = false;
        bool active = false;
        bool holder = false;
        /* Index of flag connected to this building */
        uint flag = 0;
        /* Stock of this building */
        readonly Stock[] stock = new Stock[MaxStock];
        uint firstKnight = 0;
        int burningCounter = 0;
        uint progress = 0;

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public struct U
        {
            [FieldOffset(0)]
            public int InvIndex;

            [FieldOffset(0)]
            public uint Tick; /* Used for burning building. */

            [FieldOffset(0)]
            public uint Level;
        }

        U u = new U();

        /* Map position of building */
        public MapPos Position { get; internal set; } = 0;

        /* Type of building. */
        public Type BuildingType { get; private set; } = Type.None;

        /* Building owner */
        public uint Player { get; internal set; } = 0;

        public Building(Game game, uint index)
            : base(game, index)
        {
            for (int j = 0; j < MaxStock; ++j)
            {
                stock[j] = new Stock()
                {
                    Type = Resource.Type.None,
                    Priority = 0,
                    Available = 0,
                    Requested = 0,
                    Maximum = 0
                };
            }
        }

        public uint GetFlagIndex()
        {
            return flag;
        }

        public void LinkFlag(uint flagIndex)
        {
            flag = flagIndex;
        }

        public bool HasKnight()
        {
            return firstKnight != 0;
        }

        public uint GetFirstKnight()
        {
            return firstKnight;
        }

        public void SetFirstKnight(uint serf)
        {
            firstKnight = serf;

            /* Test whether building is already occupied by knights */
            if (!active)
            {
                active = true;

                uint militaryType = 0;
                uint maxGold = uint.MaxValue;

                switch (BuildingType)
                {
                    case Type.Hut:
                        militaryType = 0;
                        maxGold = 2;
                        break;
                    case Type.Tower:
                        militaryType = 1;
                        maxGold = 4;
                        break;
                    case Type.Fortress:
                        militaryType = 2;
                        maxGold = 8;
                        break;
                    default:
                        Debug.NotReached();
                        break;
                }

                Game.GetPlayer(Player).AddNotification(Message.Type.KnightOccupied, Position, militaryType);

                Flag flag = Game.GetFlagAtPos(Game.Map.MoveDownRight(Position));
                flag.ClearFlags();
                StockInit(1, Resource.Type.GoldBar, maxGold);
                Game.BuildingCaptured(this);
            }
        }

        public int GetBurningCounter()
        {
            return burningCounter;
        }

        public void SetBurningCounter(int counter)
        {
            burningCounter = counter;
        }

        public void DecreaseBurningCounter(int delta)
        {
            burningCounter -= delta;
        }

        public bool IsMilitary()
        {
            return (BuildingType == Type.Hut) ||
                    (BuildingType == Type.Tower) ||
                    (BuildingType == Type.Fortress) ||
                    (BuildingType == Type.Castle);
        }

        /* Whether construction of the building is finished. */
        public bool IsDone()
        {
            return !constructing;
        }

        public bool IsLeveling()
        {
            return !IsDone() && progress == 0;
        }

        public void DoneLeveling()
        {
            progress = 1;
            holder = false;
            firstKnight = 0;
        }

        public Map.Object StartBuilding(Type type)
        {
            BuildingType = type;
            Map.Object mapObject = ConstructionInfos[(int)type].MapObject;
            progress = (mapObject == Map.Object.LargeBuilding) ? 0u : 1u;

            if (type == Type.Castle)
            {
                active = true;
                holder = true;

                stock[0].Available = 0xff;
                stock[0].Requested = 0xff;
                stock[1].Available = 0xff;
                stock[1].Requested = 0xff;
            }
            else
            {
                stock[0].Type = Resource.Type.Plank;
                stock[0].Priority = 0;
                stock[0].Maximum = ConstructionInfos[(int)type].Planks;
                stock[1].Type = Resource.Type.Stone;
                stock[1].Priority = 0;
                stock[1].Maximum = ConstructionInfos[(int)type].Stones;
            }

            return mapObject;
        }

        public uint GetProgress()
        {
            return progress;
        }

        public bool BuildProgress()
        {
            bool frameFinished = Misc.BitTest(progress, 15);

            progress += (!frameFinished) ? ConstructionInfos[(int)BuildingType].Phase1 : ConstructionInfos[(int)BuildingType].Phase2;

            if (progress <= 0xffff)
            {
                // Not finished yet
                return false;
            }

            progress = 0;
            constructing = false; /* Building finished */
            firstKnight = 0;

            if (BuildingType == Type.Castle)
            {
                return true;
            }

            holder = false;

            if (IsMilitary())
            {
                UpdateMilitaryFlagState();
            }

            Flag flag = Game.GetFlag(GetFlagIndex());

            StockInit(0, Resource.Type.None, 0);
            StockInit(1, Resource.Type.None, 0);
            flag.ClearFlags();

            /* Update player fields. */
            Player player = Game.GetPlayer(Player);
            player.BuildingBuilt(this);

            return true;
        }

        public void IncreaseMining(int res)
        {
            active = true;

            if (progress == 0x8000)
            {
                /* Handle empty mine. */
                Player player = Game.GetPlayer(Player);

                if (player.IsAi())
                {
                    /* TODO Burn building. */
                }

                player.AddNotification(Message.Type.MineEmpty, Position, (uint)(BuildingType - Type.StoneMine));
            }

            progress = (progress << 1) & 0xffff;

            if (res > 0)
            {
                ++progress;
            }
        }

        public void SetUnderAttack()
        {
            progress |= Misc.BitU(0);
        }

        public bool IsUnderAttack()
        {
            return Misc.BitTest(progress, 0);
        }

        /* The threat level of the building. Higher values mean that
           the building is closer to the enemy. */
        public uint GetThreatLevel()
        {
            return threatLevel;
        }

        /* Building is currently playing back a sound effect. */
        public bool IsPlayingSfx()
        {
            return playingSfx;
        }

        public void StartPlayingSfx()
        {
            playingSfx = true;
        }

        public void StopPlayingSfx()
        {
            playingSfx = false;
        }

        /* Building is active (specifics depend on building type). */
        public bool IsActive()
        {
            return active;
        }

        public void StartActivity()
        {
            active = true;
        }

        public void StopActivity()
        {
            active = false;
        }

        /* Building is burning. */
        public bool IsBurning()
        {
            return burning;
        }

        public bool BurnUp()
        {
            if (IsBurning())
            {
                return false;
            }

            burning = true;

            /* Remove lost gold stock from total count. */
            if (!constructing &&
                (BuildingType == Type.Hut ||
                 BuildingType == Type.Tower ||
                 BuildingType == Type.Fortress ||
                 BuildingType == Type.GoldSmelter))
            {
                int goldStock = (int)GetResourceCountInStock(1);

                Game.AddGoldTotal(-goldStock);
            }

            /* Update land owner ship if the building is military. */
            if (!constructing && active && IsMilitary())
            {
                Game.UpdateLandOwnership(Position);
            }

            if (!constructing && (BuildingType == Type.Castle || BuildingType == Type.Stock))
            {
                /* Cancel resources in the out queue and remove gold from map total. */
                if (active)
                {
                    Game.DeleteInventory((uint)u.InvIndex);
                    u.InvIndex = -1;
                }

                /* Let some serfs escape while the building is burning. */
                uint escapingSerfs = 0;

                foreach (Serf serf in Game.GetSerfsAtPos(Position))
                {
                    if (serf.BuildingDeleted(Position, escapingSerfs < 12))
                    {
                        ++escapingSerfs;
                    }
                }
            }
            else
            {
                active = false;
            }

            /* Remove stock from building. */
            RemoveStock();

            StopPlayingSfx();

            uint serfIndex = firstKnight;
            burningCounter = 2047;
            u.Tick = Game.Tick;

            Player player = Game.GetPlayer(Player);
            player.BuildingDemolished(this);

            if (holder)
            {
                holder = false;

                if (!constructing && BuildingType == Type.Castle)
                {
                    SetBurningCounter(8191);

                    foreach (Serf serf in Game.GetSerfsAtPos(Position))
                    {
                        serf.CastleDeleted(Position, true);
                    }
                }

                if (!constructing && IsMilitary())
                {
                    while (serfIndex != 0)
                    {
                        Serf serf = Game.GetSerf(serfIndex);
                        serfIndex = serf.GetNextKnight();

                        serf.CastleDeleted(Position, false);
                    }
                }
                else
                {
                    Serf serf = Game.GetSerf(serfIndex);

                    if (serf.GetSerfType() == Serf.Type.TransporterInventory)
                    {
                        serf.SetSerfType(Serf.Type.Transporter);
                    }

                    serf.CastleDeleted(Position, false);
                }
            }

            Map map = Game.Map;
            MapPos flagPos = map.MoveDownRight(Position);

            if (map.Paths(flagPos) == 0 && map.GetObject(flagPos) == Map.Object.Flag)
            {
                Game.DemolishFlag(flagPos, player);
            }

            return true;
        }

        /* Building has an associated serf. */
        public bool HasSerf()
        {
            return holder;
        }

        /* Building has succesfully requested a serf. */
        public void SerfRequestGranted()
        {
            serfRequested = true;
        }

        public void RequestedSerfLost()
        {
            if (serfRequested)
            {
                serfRequested = false;
            }
            else if (!HasInventory())
            {
                DecreaseRequestedForStock(0);
            }
        }

        public void RequestedSerfReached(Serf serf)
        {
            holder = true;

            if (serfRequested)
            {
                firstKnight = serf.Index;
            }

            serfRequested = false;
        }

        /* Building has requested a serf but none was available. */
        public void ClearSerfRequestFailure()
        {
            serfRequestFailed = false;
        }

        public void KnightRequestGranted()
        {
            ++stock[0].Requested;
            serfRequested = false;
        }

        /* Building has inventory and the inventory pointer is valid. */
        public bool HasInventory()
        {
            return stock[0].Requested == 0xff;
        }

        public Inventory GetInventory()
        {
            if (u.InvIndex == -1)
                return null;

            return Game.GetInventory((uint)u.InvIndex);
        }

        public void SetInventory(Inventory inventory)
        {
            if (inventory == null)
                u.InvIndex = -1;
            else
                u.InvIndex = (int)inventory.Index;
        }

        public uint GetLevel()
        {
            return u.Level;
        }

        public void SetLevel(uint level)
        {
            u.Level = level;
        }

        public uint GetTick()
        {
            return u.Tick;
        }

        public void SetTick(uint tick)
        {
            u.Tick = tick;
        }

        public uint GetKnightCount()
        {
            return stock[0].Available;
        }

        public uint WaitingStone()
        {
            return stock[1].Available; // Stone always in stock #1
        }

        public uint WaitingPlanks()
        {
            return stock[0].Available; // Planks always in stock #0
        }

        public uint MilitaryGoldCount()
        {
            uint count = 0;

            if (BuildingType == Type.Hut ||
                BuildingType == Type.Tower ||
                BuildingType == Type.Fortress)
            {
                for (int j = 0; j < MaxStock; ++j)
                {
                    if (stock[j].Type == Resource.Type.GoldBar)
                    {
                        count += stock[j].Available;
                    }
                }
            }

            return count;
        }

        public void CancelTransportedResource(Resource.Type res)
        {
            if (res == Resource.Type.Fish ||
                res == Resource.Type.Meat ||
                res == Resource.Type.Bread)
            {
                res = Resource.Type.GroupFood;
            }

            int inStock = -1;

            for (int i = 0; i < MaxStock; ++i)
            {
                if (stock[i].Type == res)
                {
                    inStock = i;
                    break;
                }
            }

            if (inStock >= 0)
            {
                --stock[inStock].Requested;

                if (stock[inStock].Requested < 0)
                {
                    throw new ExceptionFreeserf("Failed to cancel unrequested resource delivery.");
                }
            }
            else
            {
                if (!HasInventory())
                {
                    throw new ExceptionFreeserf("Not inventory");
                }
            }
        }

        public Serf CallDefenderOut()
        {
            /* Remove knight from stats of defending building */
            if (HasInventory())
            { 
                /* Castle */
                Game.GetPlayer(Player).DecreaseCastleKnights();
            }
            else
            {
                --stock[0].Available;
                ++stock[0].Requested;
            }

            /* The last knight in the list has to defend. */
            Serf firstSerf = Game.GetSerf(firstKnight);
            Serf defSerf = firstSerf.ExtractLastKnightFromList();

            if (defSerf.Index == firstKnight)
            {
                firstKnight = 0;
            }

            return defSerf;
        }

        public Serf CallAttackerOut(uint knightIndex) // TODO: is this workigng with knightIndex?
        {
            --stock[0].Available;

            /* Unlink knight from list. */
            Serf firstSerf = Game.GetSerf((knightIndex != 0) ? knightIndex : firstKnight);
            Serf defSerf = firstSerf.ExtractLastKnightFromList();

            if (defSerf.Index == firstKnight)
            {
                firstKnight = 0;
            }

            return defSerf;
        }

        public bool AddRequestedResource(Resource.Type res, bool fixPriority)
        {
            for (int j = 0; j < MaxStock; ++j)
            {
                if (stock[j].Type == res)
                {
                    if (fixPriority)
                    {
                        uint prio = stock[j].Priority;

                        if ((prio & 1) == 0)
                            prio = 0;

                        stock[j].Priority = prio >> 1;
                    }
                    else
                    {
                        stock[j].Priority = 0;
                    }

                    ++stock[j].Requested;

                    return true;
                }
            }

            return false;
        }

        public bool IsStockActive(int stockNumber)
        {
            return stock[stockNumber].Type > 0;
        }

        public uint GetResourceCountInStock(int stockNumber)
        {
            return stock[stockNumber].Available;
        }

        public Resource.Type GetResourceTypeInStock(int stockNumber)
        {
            return stock[stockNumber].Type;
        }

        public void StockInit(uint stockNum, Resource.Type type, uint maximum)
        {
            stock[stockNum].Type = type;
            stock[stockNum].Priority = 0;
            stock[stockNum].Maximum = maximum;
        }

        public void RemoveStock()
        {
            stock[0].Available = 0;
            stock[0].Requested = 0;
            stock[1].Available = 0;
            stock[1].Requested = 0;
        }

        public int GetMaxPriorityForResource(Resource.Type resource, int minimum = 0)
        {
            int maxPrio = -1;

            for (int i = 0; i < MaxStock; ++i)
            {
                if (stock[i].Type == resource &&
                    stock[i].Priority >= minimum &&
                    stock[i].Priority > maxPrio)
                {
                    maxPrio = (int)stock[i].Priority;
                }
            }

            return maxPrio;
        }

        public uint GetMaximumInStock(int stockNumber)
        {
            return stock[stockNumber].Maximum;
        }

        public uint GetRequestedInStock(int stockNumber)
        {
            return stock[stockNumber].Requested;
        }

        public void SetPriorityInStock(int stockNumber, uint priority)
        {
            stock[stockNumber].Priority = priority;
        }

        public void SetInitialResourcesInStock(int stockNumber, uint count)
        {
            stock[stockNumber].Available = count;
        }

        public void RequestedResourceDelivered(Resource.Type resource)
        {
            if (burning)
            {
                return;
            }

            if (HasInventory())
            {
                Game.GetInventory((uint)u.InvIndex).PushResource(resource);
            }
            else
            {
                if (resource == Resource.Type.Fish ||
                    resource == Resource.Type.Meat ||
                    resource == Resource.Type.Bread)
                {
                    resource = Resource.Type.GroupFood;
                }

                /* Add to building stock */
                for (int i = 0; i < MaxStock; ++i)
                {
                    if (stock[i].Type == resource)
                    {
                        ++stock[i].Available;
                        --stock[i].Requested;

                        if (stock[i].Requested < 0)
                        {
                            throw new ExceptionFreeserf("Delivered more resources than requested.");
                        }

                        return;
                    }
                }

                throw new ExceptionFreeserf("Delivered unexpected resource.");
            }
        }

        public void PlankUsedForBuild()
        {
            --stock[0].Available;
            --stock[0].Maximum;
        }

        public void StoneUsedForBuild()
        {
            --stock[1].Available;
            --stock[1].Maximum;
        }

        public bool UseResourceInStock(int stockNum)
        {
            if (stock[stockNum].Available > 0)
            {
                --stock[stockNum].Available;
                return true;
            }

            return false;
        }

        public bool UseResourcesInStocks()
        {
            if (stock[0].Available > 0 && stock[1].Available > 0)
            {
                --stock[0].Available;
                --stock[1].Available;
                return true;
            }

            return false;
        }

        public void DecreaseRequestedForStock(int stockNumber)
        {
            --stock[stockNumber].Requested;
        }

        public uint PigsCount()
        {
            return stock[1].Available;
        }

        public void SendPigToButcher()
        {
            --stock[1].Available;
        }

        public void PlaceNewPig()
        {
            ++stock[1].Available;
        }

        public void BoatClear()
        {
            stock[1].Available = 0;
        }

        public void BoatDo()
        {
            ++stock[1].Available;
        }

        public void RequestedKnightArrived()
        {
            ++stock[0].Available;
            --stock[0].Requested;
        }

        public void RequestedKnightAttackingOnWalk()
        {
            --stock[0].Requested;
        }

        public void RequestedKnightDefeatOnWalk()
        {
            if (!HasInventory())
                --stock[0].Requested;
        }

        public bool IsEnoughPlaceForKnight()
        {
            int maxCapacity = -1;

            switch (BuildingType)
            {
                case Type.Hut: maxCapacity = 3; break;
                case Type.Tower: maxCapacity = 6; break;
                case Type.Fortress: maxCapacity = 12; break;
                default: Debug.NotReached(); break;
            }

            uint totalKnights = stock[0].Requested + stock[0].Available;

            return totalKnights < maxCapacity;
        }

        public bool KnightComeBackFromFight(Serf knight)
        {
            if (IsEnoughPlaceForKnight())
            {
                ++stock[0].Available;
                Serf serf = Game.GetSerf(firstKnight);
                knight.InsertKnightBefore(serf);
                firstKnight = knight.Index;

                return true;
            }

            return false;
        }

        public void KnightOccupy()
        {
            if (!HasKnight())
            {
                stock[0].Available = 0;
                stock[0].Requested = 1;
            }
            else
            {
                ++stock[0].Requested;
            }
        }

        static readonly int[] BorderCheckOffsets = new int[]
        {
            31,  32,  33,  34,  35,  36,  37,  38,  39,  40,  41,  42,
            100, 101, 102, 103, 104, 105, 106, 107, 108,
            259, 260, 261, 262, 263, 264,
            241, 242, 243, 244, 245, 246,
            217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228,
            247, 248, 249, 250, 251, 252,
            -1,

            265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276,
            -1,

            277, 278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 288,
            289, 290, 291, 292, 293, 294,
            -1
        };

        public void UpdateMilitaryFlagState()
        {
            int f, k;
            Map map = Game.Map;

            for (f = 3, k = 0; f > 0; --f)
            {
                int offset;

                while ((offset = BorderCheckOffsets[k++]) >= 0)
                {
                    MapPos checkPos = map.PosAddSpirally(Position, (uint)offset);

                    if (map.HasOwner(checkPos) && map.GetOwner(checkPos) != Player)
                    {
                        threatLevel = (uint)f;
                        return;
                    }
                }
            }
        }

        public void Update(uint tick)
        {
            if (burning)
            {
                ushort delta = (ushort)(tick - u.Tick);
                u.Tick = tick;

                if (burningCounter >= delta)
                {
                    burningCounter -= delta;
                }
                else
                {
                    Game.DeleteBuilding(this);
                }
            }
            else
            {
                Update();
            }
        }

        public void ReadFrom(SaveReaderBinary reader)
        {
            Position = Game.Map.PosFromSavedValue(reader.ReadDWord()); // 0

            byte v8 = reader.ReadByte(); // 4
            BuildingType = (Type)((v8 >> 2) & 0x1f);
            Player = v8 & 3u;
            constructing = (v8 & 0x80) != 0;

            v8 = reader.ReadByte(); // 5
            threatLevel = v8 & 3u;
            serfRequestFailed = (v8 & 4) != 0;
            playingSfx = (v8 & 8) != 0;
            active = (v8 & 16) != 0;
            burning = (v8 & 32) != 0;
            holder = (v8 & 64) != 0;
            serfRequested = (v8 & 128) != 0;

            flag = reader.ReadWord(); // 6

            for (int i = 0; i < 2; ++i)
            {
                v8 = reader.ReadByte(); // 8, 9
                stock[i].Type = Resource.Type.None;
                stock[i].Available = 0;
                stock[i].Requested = 0;

                if (v8 != 0xff)
                {
                    stock[i].Available = (uint)(v8 >> 4) & 0xfu;
                    stock[i].Requested = v8 & 0xfu;
                }
            }

            firstKnight = reader.ReadWord(); // 10
            progress = reader.ReadWord(); // 12

            if (!burning && IsDone() &&
                (BuildingType == Type.Stock ||
                BuildingType == Type.Castle))
            {
                int offset = (int)reader.ReadDWord(); // 14
                u.InvIndex = (int)Game.CreateInventory(offset / 120).Index;
                stock[0].Requested = 0xff;
                return;
            }
            else
            {
                u.Level = reader.ReadWord(); // 14
            }

            if (!IsDone())
            {
                stock[0].Type = Resource.Type.Plank;
                stock[0].Maximum = reader.ReadByte(); // 16
                stock[1].Type = Resource.Type.Stone;
                stock[1].Maximum = reader.ReadByte(); // 17
            }
            else if (holder)
            {
                switch (BuildingType)
                {
                    case Type.Boatbuilder:
                        stock[0].Type = Resource.Type.Plank;
                        stock[0].Maximum = 8;
                        break;
                    case Type.StoneMine:
                    case Type.CoalMine:
                    case Type.IronMine:
                    case Type.GoldMine:
                        stock[0].Type = Resource.Type.GroupFood;
                        stock[0].Maximum = 8;
                        break;
                    case Type.Hut:
                        stock[1].Type = Resource.Type.GoldBar;
                        stock[1].Maximum = 2;
                        break;
                    case Type.Tower:
                        stock[1].Type = Resource.Type.GoldBar;
                        stock[1].Maximum = 4;
                        break;
                    case Type.Fortress:
                        stock[1].Type = Resource.Type.GoldBar;
                        stock[1].Maximum = 8;
                        break;
                    case Type.Butcher:
                        stock[0].Type = Resource.Type.Pig;
                        stock[0].Maximum = 8;
                        break;
                    case Type.PigFarm:
                        stock[0].Type = Resource.Type.Wheat;
                        stock[0].Maximum = 8;
                        break;
                    case Type.Mill:
                        stock[0].Type = Resource.Type.Wheat;
                        stock[0].Maximum = 8;
                        break;
                    case Type.Baker:
                        stock[0].Type = Resource.Type.Flour;
                        stock[0].Maximum = 8;
                        break;
                    case Type.Sawmill:
                        stock[1].Type = Resource.Type.Lumber;
                        stock[1].Maximum = 8;
                        break;
                    case Type.SteelSmelter:
                        stock[0].Type = Resource.Type.Coal;
                        stock[0].Maximum = 8;
                        stock[1].Type = Resource.Type.IronOre;
                        stock[1].Maximum = 8;
                        break;
                    case Type.ToolMaker:
                        stock[0].Type = Resource.Type.Plank;
                        stock[0].Maximum = 8;
                        stock[1].Type = Resource.Type.Steel;
                        stock[1].Maximum = 8;
                        break;
                    case Type.WeaponSmith:
                        stock[0].Type = Resource.Type.Coal;
                        stock[0].Maximum = 8;
                        stock[1].Type = Resource.Type.Steel;
                        stock[1].Maximum = 8;
                        break;
                    case Type.GoldSmelter:
                        stock[0].Type = Resource.Type.Coal;
                        stock[0].Maximum = 8;
                        stock[1].Type = Resource.Type.GoldOre;
                        stock[1].Maximum = 8;
                        break;
                    default:
                        break;
                }
            }
        }

        public void ReadFrom(SaveReaderText reader)
        {
            uint x = reader.Value("pos")[0].ReadUInt();
            uint y = reader.Value("pos")[1].ReadUInt();
            Position = Game.Map.Pos(x, y);
            BuildingType = (Type)reader.Value("type").ReadInt();

            try
            {
                Player = reader.Value("owner").ReadUInt();
                constructing = reader.Value("constructing").ReadBool();
            }
            catch
            {
                uint n = reader.Value("bld").ReadUInt();
                Player = n & 3;
                constructing = (n & 0x80) != 0;
            }
            try
            {
                threatLevel = reader.Value("military_state").ReadUInt();
                serfRequestFailed = reader.Value("serf_request_failed").ReadBool();
                playingSfx = reader.Value("playing_sfx").ReadBool();
                active = reader.Value("active").ReadBool();
                burning = reader.Value("burning").ReadBool();
                holder = reader.Value("holder").ReadBool();
                serfRequested = reader.Value("serf_requested").ReadBool();
            }
            catch
            {
                uint n = reader.Value("serf").ReadUInt();
                threatLevel = n & 3;
                serfRequestFailed = (n & 4) != 0;
                playingSfx = (n & 8) != 0;
                active = (n & 16) != 0;
                burning = (n & 32) != 0;
                holder = (n & 64) != 0;
                serfRequested = (n & 128) != 0;
            }

            flag = reader.Value("flag").ReadUInt();

            stock[0].Type = (Resource.Type)reader.Value("stock[0].type").ReadInt();
            stock[0].Priority = reader.Value("stock[0].prio").ReadUInt();
            stock[0].Available = reader.Value("stock[0].available").ReadUInt();
            stock[0].Requested = reader.Value("stock[0].requested").ReadUInt();
            stock[0].Maximum = reader.Value("stock[0].maximum").ReadUInt();

            stock[1].Type = (Resource.Type)reader.Value("stock[1].type").ReadInt();
            stock[1].Priority = reader.Value("stock[1].prio").ReadUInt();
            stock[1].Available = reader.Value("stock[1].available").ReadUInt();
            stock[1].Requested = reader.Value("stock[1].requested").ReadUInt();
            stock[1].Maximum = reader.Value("stock[1].maximum").ReadUInt();

            firstKnight = reader.Value("serf_index").ReadUInt();
            progress = reader.Value("progress").ReadUInt();

            /* Load various values that depend on the building type. */
            /* TODO Check validity of pointers when loading. */
            if (!burning && (IsDone() || BuildingType == Type.Castle))
            {
                if (BuildingType == Type.Stock || BuildingType == Type.Castle)
                {
                    u.InvIndex = reader.Value("inventory").ReadInt();
                    Game.CreateInventory((int)u.InvIndex);
                }
            }
            else if (burning)
            {
                u.Tick = reader.Value("tick").ReadUInt();
            }
            else
            {
                u.Level = reader.Value("level").ReadUInt();
            }
        }

        public void WriteTo(SaveWriterText writer)
        {
            writer.Value("pos").Write(Game.Map.PosColumn(Position));
            writer.Value("pos").Write(Game.Map.PosRow(Position));
            writer.Value("type").Write((int)BuildingType);
            writer.Value("owner").Write(Player);
            writer.Value("constructing").Write(constructing);

            writer.Value("military_state").Write(threatLevel);
            writer.Value("playing_sfx").Write(playingSfx);
            writer.Value("serf_request_failed").Write(serfRequestFailed);
            writer.Value("serf_requested").Write(serfRequested);
            writer.Value("burning").Write(burning);
            writer.Value("active").Write(active);
            writer.Value("holder").Write(holder);

            writer.Value("flag").Write(flag);

            writer.Value("stock[0].type").Write((int)stock[0].Type);
            writer.Value("stock[0].prio").Write(stock[0].Priority);
            writer.Value("stock[0].available").Write(stock[0].Available);
            writer.Value("stock[0].requested").Write(stock[0].Requested);
            writer.Value("stock[0].maximum").Write(stock[0].Maximum);

            writer.Value("stock[1].type").Write((int)stock[1].Type);
            writer.Value("stock[1].prio").Write(stock[1].Priority);
            writer.Value("stock[1].available").Write(stock[1].Available);
            writer.Value("stock[1].requested").Write(stock[1].Requested);
            writer.Value("stock[1].maximum").Write(stock[1].Maximum);

            writer.Value("serf_index").Write(firstKnight);
            writer.Value("progress").Write(progress);

            if (!IsBurning() && (IsDone() || BuildingType == Type.Castle))
            {
                if (BuildingType == Type.Stock ||
                    BuildingType == Type.Castle)
                {
                    writer.Value("inventory").Write(u.InvIndex);
                }
            }
            else if (IsBurning())
            {
                writer.Value("tick").Write(u.Tick);
            }
            else
            {
                writer.Value("level").Write(u.Level);
            }
        }

        void Update()
        {
            if (!constructing)
            {
                RequestSerfIfNeeded();

                Player player = Game.GetPlayer(Player);
                uint totalResource1 = stock[0].Requested + stock[0].Available;
                uint totalResource2 = stock[1].Requested + stock[1].Available;
                int resource1Modifier = 8 + (int)totalResource1;
                int resource2Modifier = 8 + (int)totalResource2;
                Func<uint> GetPrioFuncResource1 = null;
                Func<uint> GetPrioFuncResource2 = null;

                switch (BuildingType)
                {
                    case Type.Boatbuilder:
                        if (holder)
                        {
                            GetPrioFuncResource1 = player.GetPlanksBoatbuilder; // Planks
                        }
                        break;
                    case Type.StoneMine:
                        if (holder)
                        {
                            GetPrioFuncResource1 = player.GetFoodStonemine; // Food
                        }
                        break;
                    case Type.CoalMine:
                        if (holder)
                        {
                            GetPrioFuncResource1 = player.GetFoodCoalmine; // Food
                        }
                        break;
                    case Type.IronMine:
                        if (holder)
                        {
                            GetPrioFuncResource1 = player.GetFoodIronmine; // Food
                        }
                        break;
                    case Type.GoldMine:
                        if (holder)
                        {
                            GetPrioFuncResource1 = player.GetFoodGoldmine; // Food
                        }
                        break;
                    case Type.Stock:
                        if (!IsActive())
                        {
                            Inventory inventory = Game.CreateInventory();

                            if (inventory == null)
                                return;

                            inventory.Player = Player;
                            inventory.SetBuildingIndex(Index);
                            inventory.SetFlagIndex(flag);

                            u.InvIndex = (int)inventory.Index;
                            stock[0].Requested = 0xff;
                            stock[0].Available = 0xff;
                            stock[1].Requested = 0xff;
                            stock[1].Available = 0xff;
                            active = true;

                            Game.GetPlayer(Player).AddNotification(Message.Type.NewStock, Position, 0);
                        }
                        else
                        {
                            if (!serfRequestFailed && !holder && !serfRequested)
                            {
                                SendSerfToBuilding(Serf.Type.Transporter,
                                                   Resource.Type.None,
                                                   Resource.Type.None);
                            }

                            Inventory inventory = Game.GetInventory((uint)u.InvIndex);

                            if (holder &&
                                !inventory.HaveAnyOutMode() && /* Not serf or res OUT mode */
                                inventory.FreeSerfCount() == 0)
                            {
                                if (player.TickSendGenericDelay())
                                {
                                    SendSerfToBuilding(Serf.Type.Generic,
                                                       Resource.Type.None,
                                                       Resource.Type.None);
                                }
                            }

                            /* TODO Following code looks like a hack */
                            Map map = Game.Map;
                            MapPos flagPos = map.MoveDownRight(Position);

                            if (map.HasSerf(flagPos))
                            {
                                Serf serf = Game.GetSerfAtPos(flagPos);

                                if (serf.Position != flagPos)
                                {
                                    map.SetSerfIndex(flagPos, 0);
                                }
                            }
                        }
                        break;
                    case Type.Hut:
                    case Type.Tower:
                    case Type.Fortress:
                        UpdateMilitary();
                        break;
                    case Type.Butcher:
                        if (holder)
                        {
                            // Meat
                            GetPrioFuncResource1 = () =>
                            {
                                return 0xffu >> (int)totalResource1;
                            };
                            resource1Modifier = 0;
                        }
                        break;
                    case Type.PigFarm:
                        if (holder)
                        {
                            GetPrioFuncResource1 = player.GetWheatPigfarm; // Wheat
                        }
                        break;
                    case Type.Mill:
                        if (holder)
                        {
                            GetPrioFuncResource1 = player.GetWheatMill; // Wheat
                        }
                        break;
                    case Type.Baker:
                        if (holder)
                        {
                            // Flour
                            GetPrioFuncResource1 = () =>
                            {
                                return 0xffu >> (int)totalResource1;
                            };
                            resource1Modifier = 0;
                        }
                        break;
                    case Type.Sawmill:
                        if (holder)
                        {
                            // Lumber
                            GetPrioFuncResource1 = () =>
                            {
                                return 0xffu >> (int)totalResource1;
                            };
                            resource1Modifier = 0;
                        }
                        break;
                    case Type.SteelSmelter:
                        if (holder)
                        {
                            /* Request more coal */
                            GetPrioFuncResource1 = player.GetCoalSteelsmelter;

                            /* Request more iron ore */
                            GetPrioFuncResource2 = () =>
                            {
                                return 0xffu >> (int)totalResource2;
                            };
                            resource2Modifier = 0;
                        }
                        break;
                    case Type.ToolMaker:
                        if (holder)
                        {
                            /* Request more planks. */
                            GetPrioFuncResource1 = player.GetPlanksToolmaker;

                            /* Request more steel. */
                            GetPrioFuncResource2 = player.GetSteelToolmaker;
                        }
                        break;
                    case Type.WeaponSmith:
                        if (holder)
                        {
                            /* Request more coal. */
                            GetPrioFuncResource1 = player.GetCoalWeaponsmith;

                            /* Request more steel. */
                            GetPrioFuncResource2 = player.GetSteelWeaponsmith;
                        }
                        break;
                    case Type.GoldSmelter:
                        if (holder)
                        {
                            /* Request more coal. */
                            GetPrioFuncResource1 = player.GetCoalGoldsmelter;

                            /* Request more gold ore. */
                            GetPrioFuncResource2 = () =>
                            {
                                return 0xffu >> (int)totalResource2;
                            };
                            resource2Modifier = 0;
                        }
                        break;
                    case Type.Castle:
                        UpdateCastle();
                        break;
                    default:
                        break;
                }

                // Set priority for resource 1
                if (GetPrioFuncResource1 != null)
                {
                    if (totalResource1 < stock[0].Maximum)
                        stock[0].Priority = GetPrioFuncResource1() >> resource1Modifier;
                    else
                        stock[0].Priority = 0;
                }

                // Set priority for resource 2
                if (GetPrioFuncResource2 != null)
                {
                    if (totalResource2 < stock[1].Maximum)
                        stock[1].Priority = GetPrioFuncResource2() >> resource2Modifier;
                    else
                        stock[1].Priority = 0;
                }
            }
            else
            { 
                /* Unfinished */
                switch (BuildingType)
                {
                    case Type.None:
                    case Type.Castle:
                        break;
                    case Type.Fisher:
                    case Type.Lumberjack:
                    case Type.Boatbuilder:
                    case Type.Stonecutter:
                    case Type.StoneMine:
                    case Type.CoalMine:
                    case Type.IronMine:
                    case Type.GoldMine:
                    case Type.Forester:
                    case Type.Hut:
                    case Type.Mill:
                        UpdateUnfinished();
                        break;
                    case Type.Stock:
                    case Type.Farm:
                    case Type.Butcher:
                    case Type.PigFarm:
                    case Type.Baker:
                    case Type.Sawmill:
                    case Type.SteelSmelter:
                    case Type.ToolMaker:
                    case Type.WeaponSmith:
                    case Type.Tower:
                    case Type.Fortress:
                    case Type.GoldSmelter:
                        UpdateUnfinishedAdvanced();
                        break;
                    default:
                        Debug.NotReached();
                        break;
                }
            }
        }

        void UpdateUnfinished()
        {
            Player player = Game.GetPlayer(Player);

            /* Request builder serf */
            if (!serfRequestFailed && !holder && !serfRequested)
            {
                progress = 1;
                serfRequestFailed = !SendSerfToBuilding(Serf.Type.Builder,
                                                        Resource.Type.Hammer,
                                                        Resource.Type.None);
            }

            /* Request planks */
            uint totalPlanks = stock[0].Requested + stock[0].Available;

            if (totalPlanks < stock[0].Maximum)
            {
                uint planksPrio = player.GetPlanksConstruction() >> (8 + (int)totalPlanks);

                if (!holder)
                    planksPrio >>= 2;

                stock[0].Priority = planksPrio & ~Misc.BitU(0);
            }
            else
            {
                stock[0].Priority = 0;
            }

            /* Request stone */
            uint totalStone = stock[1].Requested + stock[1].Available;

            if (totalStone < stock[1].Maximum)
            {
                uint stonePrio = 0xffu >> (int)totalStone;

                if (!holder)
                    stonePrio >>= 2;

                stock[1].Priority = stonePrio & ~Misc.BitU(0);
            }
            else
            {
                stock[1].Priority = 0;
            }
        }

        void UpdateUnfinishedAdvanced()
        {
            if (progress > 0)
            {
                UpdateUnfinished();
                return;
            }

            if (holder || serfRequested)
            {
                return;
            }

            /* Check whether building needs leveling */
            bool needLeveling = false;
            uint height = (uint)Game.GetLevelingHeight(Position);

            for (uint i = 0; i < 7; ++i)
            {
                MapPos pos = Game.Map.PosAddSpirally(Position, i);

                if (Game.Map.GetHeight(pos) != height)
                {
                    needLeveling = true;
                    break;
                }
            }

            if (!needLeveling)
            {
                /* Already at the correct level, don't send digger */
                progress = 1;
                UpdateUnfinished();
                return;
            }

            /* Request digger */
            if (!serfRequestFailed)
            {
                serfRequestFailed = !SendSerfToBuilding(Serf.Type.Digger,
                                                        Resource.Type.Shovel,
                                                        Resource.Type.None);
            }
        }

        void UpdateCastle()
        {
            Player player = Game.GetPlayer(Player);
            Inventory inventory = Game.GetInventory((uint)u.InvIndex);

            if (player.GetCastleKnights() == player.GetCastleKnightsWanted())
            {
                Serf bestKnight = null;
                Serf lastKnight = null;
                uint nextSerfIndex = firstKnight;

                while (nextSerfIndex != 0)
                {
                    Serf serf = Game.GetSerf(nextSerfIndex);

                    if (serf == null)
                    {
                        throw new ExceptionFreeserf("Index of nonexistent serf in the queue.");
                    }

                    if (bestKnight == null || serf.GetSerfType() < bestKnight.GetSerfType())
                    {
                        bestKnight = serf;
                    }

                    lastKnight = serf;
                    nextSerfIndex = serf.GetNextKnight();
                }

                if (bestKnight != null)
                {
                    Serf.Type knightType = bestKnight.GetSerfType();

                    for (int t = (int)Serf.Type.Knight0; t <= (int)Serf.Type.Knight4; ++t)
                    {
                        if ((int)knightType > t)
                        {
                            inventory.CallInternal(bestKnight);
                        }
                    }

                    /* Switch types */
                    Serf.Type tmp = bestKnight.GetSerfType();
                    bestKnight.SetSerfType(lastKnight.GetSerfType());
                    lastKnight.SetSerfType(tmp);
                }
            }
            else if (player.GetCastleKnights() < player.GetCastleKnightsWanted())
            {
                Serf.Type knightType = Serf.Type.None;

                for (int t = (int)Serf.Type.Knight4; t >= (int)Serf.Type.Knight0; --t)
                {
                    if (inventory.HaveSerf((Serf.Type)t))
                    {
                        knightType = (Serf.Type)t;
                        break;
                    }
                }

                if (knightType < 0)
                {
                    /* None found */
                    if (inventory.HaveSerf(Serf.Type.Generic) &&
                        inventory.GetCountOf(Resource.Type.Sword) != 0 &&
                        inventory.GetCountOf(Resource.Type.Shield) != 0)
                    {
                        Serf serf = inventory.SpecializeFreeSerf(Serf.Type.Knight0);
                        inventory.CallInternal(serf);

                        serf.AddToDefendingQueue(firstKnight, false);
                        firstKnight = serf.Index;
                        player.IncreaseCastleKnights();
                    }
                    else
                    {
                        if (player.TickSendKnightDelay())
                        {
                            SendSerfToBuilding(Serf.Type.None,
                                               Resource.Type.None,
                                               Resource.Type.None);
                        }
                    }
                }
                else
                {
                    /* Prepend to knights list */
                    Serf serf = inventory.CallInternal(knightType);
                    serf.AddToDefendingQueue(firstKnight, true);
                    firstKnight = serf.Index;
                    player.IncreaseCastleKnights();
                }
            }
            else
            {
                player.DecreaseCastleKnights();

                uint serfIndex = firstKnight;
                Serf serf = Game.GetSerf(serfIndex);
                firstKnight = serf.GetNextKnight();

                serf.StayIdleInStock((uint)u.InvIndex);
            }

            if (holder &&
                !inventory.HaveAnyOutMode() && /* Not serf or res OUT mode */
                inventory.FreeSerfCount() == 0)
            {
                if (player.TickSendGenericDelay())
                {
                    SendSerfToBuilding(Serf.Type.Generic,
                                       Resource.Type.None,
                                       Resource.Type.None);
                }
            }

            Map map = Game.Map;
            MapPos flagPos = map.MoveDownRight(Position);

            if (map.HasSerf(flagPos))
            {
                Serf serf = Game.GetSerfAtPos(flagPos);

                if (serf.Position != flagPos)
                {
                    map.SetSerfIndex(flagPos, 0);
                }
            }
        }

        static readonly uint[] HutOccupantsFromLevel = new uint[]
        {
            1, 1, 2, 2, 3,
            1, 1, 1, 1, 2
        };

        static readonly uint[] TowerOccupantsFromLevel = new uint[]
        {
            1, 2, 3, 4, 6,
            1, 1, 2, 3, 4
        };

        static readonly uint[] FortressOccupantsFromLevel = new uint[]
        {
            1, 3, 6, 9, 12,
            1, 2, 4, 6, 8
        };

        void UpdateMilitary()
        {
            Player player = Game.GetPlayer(Player);
            uint maxOccupiedLevel = (player.GetKnightOccupation(threatLevel) >> 4) & 0xf;

            if (player.ReducedKnightLevel())
                maxOccupiedLevel += 5;

            if (maxOccupiedLevel > 9)
                maxOccupiedLevel = 9;

            uint neededOccupants = 0;
            int maxGold = -1;

            switch (BuildingType)
            {
                case Type.Hut:
                    neededOccupants = HutOccupantsFromLevel[maxOccupiedLevel];
                    maxGold = 2;
                    break;
                case Type.Tower:
                    neededOccupants = TowerOccupantsFromLevel[maxOccupiedLevel];
                    maxGold = 4;
                    break;
                case Type.Fortress:
                    neededOccupants = FortressOccupantsFromLevel[maxOccupiedLevel];
                    maxGold = 8;
                    break;
                default:
                    Debug.NotReached();
                    break;
            }

            uint totalKnights = stock[0].Requested + stock[0].Available;
            uint presentKnights = stock[0].Available;

            if (totalKnights < neededOccupants)
            {
                if (!serfRequestFailed)
                {
                    serfRequestFailed = !SendSerfToBuilding(Serf.Type.None,
                                                            Resource.Type.None,
                                                            Resource.Type.None);
                }
            }
            else if (neededOccupants < presentKnights && !Game.Map.HasSerf(Game.Map.MoveDownRight(Position)))
            {
                /* Kick least trained knight out. */
                Serf leavingSerf = null;
                uint serfIndex = firstKnight;

                while (serfIndex != 0)
                {
                    Serf serf = Game.GetSerf(serfIndex);

                    if (serf == null)
                    {
                        throw new ExceptionFreeserf("Index of nonexistent serf in the queue.");
                    }

                    if (leavingSerf == null || serf.GetSerfType() < leavingSerf.GetSerfType())
                    {
                        leavingSerf = serf;
                    }

                    serfIndex = serf.GetNextKnight();
                }

                if (leavingSerf != null)
                {
                    /* Remove leaving serf from list. */
                    if (leavingSerf.Index == firstKnight)
                    {
                        firstKnight = leavingSerf.GetNextKnight();
                    }
                    else
                    {
                        serfIndex = firstKnight;

                        while (serfIndex != 0)
                        {
                            Serf serf = Game.GetSerf(serfIndex);

                            if (serf.GetNextKnight() == leavingSerf.Index)
                            {
                                serf.SetNextKnight(leavingSerf.GetNextKnight());
                                break;
                            }

                            serfIndex = serf.GetNextKnight();
                        }
                    }

                    /* Update serf state. */
                    leavingSerf.GoOutFromBuilding(0, 0, -2);

                    stock[0].Available -= 1;
                }
            }

            /* Request gold */
            if (holder)
            {
                uint totalGold = stock[1].Requested + stock[1].Available;

                player.IncreaseMilitaryMaxGold(maxGold);

                if (totalGold < maxGold)
                {
                    stock[1].Priority = ((0xfeu >> (int)totalGold) + 1) & 0xfe;
                }
                else
                {
                    stock[1].Priority = 0;
                }
            }
        }

        struct Request
        {
            public Request(Serf.Type serfType, Resource.Type resType1, Resource.Type resType2)
            {
                SerfType = serfType;
                ResType1 = resType1;
                ResType2 = resType2;
            }

            public Serf.Type SerfType;
            public Resource.Type ResType1;
            public Resource.Type ResType2;
        }

        static readonly Request[] Requests = new Request[] 
        {
            new Request(Serf.Type.None       , Resource.Type.None   , Resource.Type.None  ),
            new Request(Serf.Type.Fisher     , Resource.Type.Rod    , Resource.Type.None  ),
            new Request(Serf.Type.Lumberjack , Resource.Type.Axe    , Resource.Type.None  ),
            new Request(Serf.Type.BoatBuilder, Resource.Type.Hammer , Resource.Type.None  ),
            new Request(Serf.Type.Stonecutter, Resource.Type.Pick   , Resource.Type.None  ),
            new Request(Serf.Type.Miner      , Resource.Type.Pick   , Resource.Type.None  ),
            new Request(Serf.Type.Miner      , Resource.Type.Pick   , Resource.Type.None  ),
            new Request(Serf.Type.Miner      , Resource.Type.Pick   , Resource.Type.None  ),
            new Request(Serf.Type.Miner      , Resource.Type.Pick   , Resource.Type.None  ),
            new Request(Serf.Type.Forester   , Resource.Type.None   , Resource.Type.None  ),
            new Request(Serf.Type.None       , Resource.Type.None   , Resource.Type.None  ),
            new Request(Serf.Type.None       , Resource.Type.None   , Resource.Type.None  ),
            new Request(Serf.Type.Farmer     , Resource.Type.Scythe , Resource.Type.None  ),
            new Request(Serf.Type.Butcher    , Resource.Type.Cleaver, Resource.Type.None  ),
            new Request(Serf.Type.PigFarmer  , Resource.Type.None   , Resource.Type.None  ),
            new Request(Serf.Type.Miller     , Resource.Type.None   , Resource.Type.None  ),
            new Request(Serf.Type.Baker      , Resource.Type.None   , Resource.Type.None  ),
            new Request(Serf.Type.Sawmiller  , Resource.Type.Saw    , Resource.Type.None  ),
            new Request(Serf.Type.Smelter    , Resource.Type.None   , Resource.Type.None  ),
            new Request(Serf.Type.Toolmaker  , Resource.Type.Hammer , Resource.Type.Saw   ),
            new Request(Serf.Type.WeaponSmith, Resource.Type.Hammer , Resource.Type.Pincer),
            new Request(Serf.Type.None       , Resource.Type.None   , Resource.Type.None  ),
            new Request(Serf.Type.None       , Resource.Type.None   , Resource.Type.None  ),
            new Request(Serf.Type.Smelter    , Resource.Type.None   , Resource.Type.None  ),
            new Request(Serf.Type.None       , Resource.Type.None   , Resource.Type.None  ),
        };

        void RequestSerfIfNeeded()
        {
            if (!serfRequestFailed && !holder && !serfRequested)
            {
                int type = (int)BuildingType;

                if (Requests[type].SerfType != Serf.Type.None)
                {
                    serfRequestFailed = !SendSerfToBuilding(Requests[type].SerfType,
                                                            Requests[type].ResType1,
                                                            Requests[type].ResType2);
                }
            }
        }

        bool SendSerfToBuilding(Serf.Type type, Resource.Type res1, Resource.Type res2)
        {
            return Game.SendSerfToFlag(Game.GetFlag(flag), type, res1, res2);
        }

        static readonly uint[] BuildingScoreFromType = new uint[]
        {
            2, 2, 2, 2, 5, 5, 5, 5, 2, 10,
            3, 6, 4, 6, 5, 4, 7, 7, 9, 4,
            8, 15, 6, 20
        };

        internal static uint BuildingGetScoreFromType(Type type)
        {
            return BuildingScoreFromType[(int)type - 1];
        }
    }
}
