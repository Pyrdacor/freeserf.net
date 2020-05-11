/*
 * Building.cs - Building related functions.
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
using System.Linq;

namespace Freeserf
{
    using Serialize;
    using MapPos = UInt32;
    using word = UInt16;

    internal class ConstructionInfo
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

    public class Building : GameObject, IState
    {
        public enum Type : byte
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
        public const uint MaxStock = 2;

        [DataClass]
        internal class Stock : IComparable
        {
            public Resource.Type Type;
            public byte Priority;
            public byte Available;
            public byte Requested;
            public byte Maximum;

            public int CompareTo(object other)
            {
                if (other is Stock)
                {
                    var otherStock = other as Stock;

                    if (Type == otherStock.Type)
                    {
                        if (Priority == otherStock.Priority)
                        {
                            if (Available == otherStock.Available)
                            {
                                if (Requested == otherStock.Requested)
                                    return Maximum.CompareTo(otherStock.Maximum);

                                return Requested.CompareTo(otherStock.Requested);
                            }

                            return Available.CompareTo(otherStock.Available);
                        }

                        return Priority.CompareTo(otherStock.Priority);
                    }

                    return Type.CompareTo(otherStock.Type);
                }

                return 1;
            }
        }

        internal static readonly ConstructionInfo[] ConstructionInfos = new ConstructionInfo[]
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

        [Data]
        private BuildingState state = new BuildingState();

        public Building(Game game, uint index)
            : base(game, index)
        {

        }

        public bool Dirty => state.Dirty;

        /// <summary>
        /// Map position of the building
        /// </summary>
        public MapPos Position
        {
            get => state.Position;
            internal set => state.Position = value;
        }

        /// <summary>
        /// Type of building
        /// </summary>
        public Type BuildingType => state.Type;

        /// <summary>
        /// Building owner
        /// </summary>
        public uint Player
        {
            get => state.Player;
            internal set => state.Player = (byte)value;
        }

        public uint FlagIndex => state.Flag;

        public void ResetDirtyFlag()
        {
            state.ResetDirtyFlag();
        }

        public void LinkFlag(uint flagIndex)
        {
            state.Flag = (word)flagIndex;
        }

        public bool HasKnight()
        {
            return state.FirstKnight != 0;
        }

        public uint FirstKnight
        {
            get => state.FirstKnight;
            set
            {
                state.FirstKnight = (word)value;

                if (state.FirstKnight == 0)
                    throw new ExceptionFreeserf(ErrorSystemType.Building, "First knight was set to 0.");

                // Test whether building is already occupied by knights
                if (!state.Active)
                {
                    state.Active = true;

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

                    Game.GetPlayer(Player).AddNotification(Notification.Type.KnightOccupied, Position, militaryType);

                    var flag = Game.GetFlagForBuildingAtPosition(Position);
                    flag.ClearFlags();
                    StockInit(1, Resource.Type.GoldBar, maxGold);
                    Game.BuildingCaptured(this);
                }
            }
        }

        public int BurningCounter { get; set; } = 0;

        public void DecreaseBurningCounter(int delta)
        {
            BurningCounter -= delta;
        }

        public bool IsMilitary(bool includeCastle = true)
        {
            return (BuildingType == Type.Hut) ||
                   (BuildingType == Type.Tower) ||
                   (BuildingType == Type.Fortress) ||
                   (BuildingType == Type.Castle && includeCastle);
        }

        // Whether construction of the building is finished.
        public bool IsDone => !state.Constructing;

        public bool IsLeveling => !IsDone && state.Progress == 0;

        public void DoneLeveling()
        {
            state.Progress = 1;
            state.Holder = false;
            state.FirstKnight = 0;
        }

        public Map.Object StartBuilding(Type type)
        {
            state.Type = type;
            var mapObject = ConstructionInfos[(int)type].MapObject;
            state.Progress = (word)((mapObject == Map.Object.LargeBuilding) ? 0u : 1u);

            if (type == Type.Castle)
            {
                state.Active = true;
                state.Holder = true;

                state.Stock[0].Available = 0xff;
                state.Stock[0].Requested = 0xff;
                state.Stock[1].Available = 0xff;
                state.Stock[1].Requested = 0xff;
            }
            else
            {
                state.Stock[0].Type = Resource.Type.Plank;
                state.Stock[0].Priority = 0;
                state.Stock[0].Maximum = (byte)ConstructionInfos[(int)type].Planks;
                state.Stock[1].Type = Resource.Type.Stone;
                state.Stock[1].Priority = 0;
                state.Stock[1].Maximum = (byte)ConstructionInfos[(int)type].Stones;
            }

            return mapObject;
        }

        /// <summary>
        /// Progress of unfinished building:
        /// 0: Need leveling
        /// 1: No leveling needed
        /// >= 0x8000: Frame finished
        /// > 0xffff: Building finished
        /// </summary>
        public uint Progress => state.Progress;

        public bool FrameFinished => Misc.BitTest(Progress, 15);

        public bool BuildProgress()
        {
            var progress = state.Progress + ((!FrameFinished) ? ConstructionInfos[(int)BuildingType].Phase1 : ConstructionInfos[(int)BuildingType].Phase2);

            if (progress <= 0xffff)
            {
                state.Progress = (word)progress;

                // Not finished yet
                return false;
            }

            state.Progress = 0;
            state.Constructing = false; // Building finished
            state.FirstKnight = 0;

            if (BuildingType == Type.Castle)
            {
                return true;
            }

            state.Holder = false;

            if (IsMilitary())
            {
                UpdateMilitaryFlagState();
            }

            var flag = Game.GetFlag(FlagIndex);

            StockInit(0, Resource.Type.None, 0);
            StockInit(1, Resource.Type.None, 0);
            flag.ClearFlags();

            // Update player fields.
            var player = Game.GetPlayer(Player);
            player.BuildingBuilt(this);

            return true;
        }

        public void IncreaseMining(int resource)
        {
            state.Active = true;

            if (state.Progress == 0x8000)
            {
                // Handle empty mine.
                var player = Game.GetPlayer(Player);

                if (player.IsAI)
                {
                    if (player.AI != null)
                        player.AI.HandleEmptyMine(Index);
                }

                player.AddNotification(Notification.Type.MineEmpty, Position, (uint)(BuildingType - Type.StoneMine));
            }

            state.Progress = (word)((state.Progress << 1) & 0xffff);

            if (resource > 0)
            {
                ++state.Progress;
            }
        }

        public void SetUnderAttack()
        {
            state.Progress |= (word)Misc.BitU(0);
        }

        public bool IsUnderAttack => Misc.BitTest(state.Progress, 0);

        /// <summary>
        /// The threat level of the building. Higher values mean that
        /// the building is closer to the enemy.
        /// </summary>
        public uint ThreatLevel => state.ThreatLevel;

        /// <summary>
        /// Building is currently playing back a sound effect.
        /// </summary>
        public bool IsPlayingSfx => state.PlayingSfx;

        public void StartPlayingSfx()
        {
            state.PlayingSfx = true;
        }

        public void StopPlayingSfx()
        {
            state.PlayingSfx = false;
        }

        // Building is active (specifics depend on building type).
        public bool IsActive => state.Active;

        public void StartActivity()
        {
            state.Active = true;
        }

        public void StopActivity()
        {
            state.Active = false;
        }

        // Building is burning.
        public bool IsBurning => state.Burning;

        public bool BurnUp()
        {
            if (IsBurning)
            {
                return false;
            }

            state.Burning = true;

            // Remove lost gold stock from total count.
            if (!state.Constructing &&
                (BuildingType == Type.Hut ||
                 BuildingType == Type.Tower ||
                 BuildingType == Type.Fortress ||
                 BuildingType == Type.GoldSmelter))
            {
                int goldStock = (int)GetResourceCountInStock(1);

                Game.AddGoldTotal(-goldStock);
            }

            // Update land owner ship if the building is military.
            if (!state.Constructing && state.Active && IsMilitary())
            {
                Game.UpdateLandOwnership(Position);
            }

            if (!state.Constructing && (BuildingType == Type.Castle || BuildingType == Type.Stock))
            {
                // Cancel resources in the out queue and remove gold from map total.
                if (state.Active)
                {
                    Game.DeleteInventory(state.Inventory);
                    state.Inventory = word.MaxValue;
                }

                // Let some serfs escape while the building is burning.
                uint escapingSerfs = 0;

                foreach (var serf in Game.GetSerfsAtPosition(Position).ToArray())
                {
                    if (serf.BuildingDeleted(Position, escapingSerfs < 12))
                    {
                        ++escapingSerfs;
                    }
                }
            }
            else
            {
                state.Active = false;
            }

            // Remove stock from building.
            RemoveStock();

            StopPlayingSfx();

            uint serfIndex = state.FirstKnight;
            BurningCounter = 2047;
            state.Tick = Game.Tick;

            var player = Game.GetPlayer(Player);
            player.BuildingDemolished(this);

            if (state.Holder)
            {
                state.Holder = false;

                if (!state.Constructing && BuildingType == Type.Castle)
                {
                    BurningCounter = 8191;

                    foreach (var serf in Game.GetSerfsAtPosition(Position).ToArray())
                    {
                        serf.CastleDeleted(Position, true);
                    }

                    // TODO: player defeated? mission outro etc?
                }

                if (!state.Constructing && IsMilitary())
                {
                    while (serfIndex != 0)
                    {
                        var serf = Game.GetSerf(serfIndex);
                        serfIndex = serf.NextKnight;

                        serf.CastleDeleted(Position, false);
                    }
                }
                else
                {
                    var serf = Game.GetSerfAtPosition(Position);

                    if (serf == null)
                        serf = Game.GetPlayerSerfs(Game.GetPlayer(Player)).FirstOrDefault(serfToCheck => serfToCheck.Position == Position);

                    if (serf != null)
                    {
                        if (serf.SerfType == Serf.Type.TransporterInventory)
                        {
                            serf.SerfType = Serf.Type.Transporter;
                        }

                        serf.BuildingDeleted(Position, true);
                    }
                }
            }

            var map = Game.Map;
            var flagPosition = map.MoveDownRight(Position);

            if (map.GetOwner(flagPosition) != Player && map.Paths(flagPosition) == 0 && map.GetObject(flagPosition) == Map.Object.Flag)
            {
                Game.DemolishFlag(flagPosition, player);
            }

            return true;
        }

        // Building has an associated serf.
        public bool HasSerf => state.Holder;

        // Weaponsmith can craft a shield for free.
        public bool FreeShieldPossible
        {
            get => state.FreeShieldPossible;
            set => state.FreeShieldPossible = value;
        }

        // Building has succesfully requested a serf.
        public void SerfRequestGranted()
        {
            state.SerfRequested = true;
        }

        public void RequestedSerfLost()
        {
            if (state.SerfRequested)
            {
                state.SerfRequested = false;
            }
            else if (!HasInventory())
            {
                DecreaseRequestedForStock(0);
            }
        }

        public void RequestedSerfReached(Serf serf)
        {
            state.Holder = true;

            if (state.SerfRequested && serf.IsKnight)
            {
                if (state.FirstKnight == 0)
                    state.FirstKnight = (word)serf.Index;
            }

            state.SerfRequested = false;
        }

        // Building has requested a serf but none was available.
        public void ClearSerfRequestFailure()
        {
            state.SerfRequestFailed = false;
        }

        public void KnightRequestGranted()
        {
            ++state.Stock[0].Requested;
            state.SerfRequested = false;
        }

        // Building has inventory and the inventory pointer is valid.
        public bool HasInventory()
        {
            return state.Stock[0].Requested == 0xff;
        }

        public Inventory Inventory
        {
            get
            {
                if (state.Inventory == word.MaxValue)
                    return null;

                return Game.GetInventory(state.Inventory);
            }
            set
            {
                if (value == null)
                    state.Inventory = word.MaxValue;
                else
                    state.Inventory = (word)value.Index;
            }
        }

        public uint Level
        {
            get => state.Level;
            set => state.Level = (word)value;
        }

        public uint Tick
        {
            get => state.Tick;
            set => state.Tick = (word)value;
        }

        public uint KnightCount => state.Stock[0].Available;

        public uint WaitingPlanks => state.Stock[0].Available; // Planks always in stock #0

        public uint WaitingStone => state.Stock[1].Available; // Stone always in stock #1        

        public bool HasAllConstructionMaterials()
        {
            if (IsDone)
                return true;

            var numPlanks = state.Stock[0].Available + state.Stock[0].Requested;
            var numStones = state.Stock[1].Available + state.Stock[1].Requested;

            return numPlanks == state.Stock[0].Maximum && numStones == state.Stock[1].Maximum;
        }

        public bool HasAllConstructionMaterialsAtLocation()
        {
            if (IsDone)
                return true;

            return state.Stock[0].Available == state.Stock[0].Maximum && state.Stock[1].Available == state.Stock[1].Maximum;
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
                    if (state.Stock[j].Type == Resource.Type.GoldBar)
                    {
                        count += state.Stock[j].Available;
                    }
                }
            }

            return count;
        }

        public void CancelTransportedResource(Resource.Type resource)
        {
            if (resource == Resource.Type.Fish ||
                resource == Resource.Type.Meat ||
                resource == Resource.Type.Bread)
            {
                resource = Resource.Type.GroupFood;
            }

            int inStock = -1;

            for (int i = 0; i < MaxStock; ++i)
            {
                if (state.Stock[i].Type == resource)
                {
                    inStock = i;
                    break;
                }
            }

            if (inStock >= 0)
            {
                if (state.Stock[inStock].Requested > 0)
                    --state.Stock[inStock].Requested;
                // TODO: else log error
            }
            else
            {
                if (!HasInventory())
                {
                    throw new ExceptionFreeserf(Game, ErrorSystemType.Building, "Not inventory");
                }
            }
        }

        public Serf CallDefenderOut()
        {
            // Remove knight from stats of defending building
            if (HasInventory())
            {
                // Castle
                Game.GetPlayer(Player).DecreaseCastleKnights();
            }
            else
            {
                --state.Stock[0].Available;
                ++state.Stock[0].Requested;
            }

            // The last knight in the list has to defend.
            var firstSerf = Game.GetSerf(state.FirstKnight);
            var defendingSerf = firstSerf.ExtractLastKnightFromList();

            if (defendingSerf.Index == state.FirstKnight)
            {
                state.FirstKnight = 0;
            }

            return defendingSerf;
        }

        public Serf CallAttackerOut(uint knightIndex)
        {
            --state.Stock[0].Available;

            // Unlink knight from list.
            var firstSerf = Game.GetSerf(state.FirstKnight);
            uint firstKnight = state.FirstKnight;

            var result = firstSerf.ExtractKnightFromList(knightIndex, ref firstKnight);

            if (firstKnight != state.FirstKnight)
            {
                state.FirstKnight = (word)firstKnight;
            }

            return result;
        }

        public bool AddRequestedResource(Resource.Type resource, bool fixPriority)
        {
            for (int j = 0; j < MaxStock; ++j)
            {
                if (state.Stock[j].Type == resource)
                {
                    if (fixPriority)
                    {
                        byte priority = state.Stock[j].Priority;

                        if ((priority & 1) == 0)
                            priority = 0;

                        state.Stock[j].Priority = (byte)(priority >> 1);
                    }
                    else
                    {
                        state.Stock[j].Priority = 0;
                    }

                    ++state.Stock[j].Requested;

                    return true;
                }
            }

            return false;
        }

        public bool IsStockActive(int stockNumber)
        {
            return state.Stock[stockNumber].Type > 0;
        }

        public uint GetResourceCountInStock(int stockNumber)
        {
            return state.Stock[stockNumber].Available;
        }

        public Resource.Type GetResourceTypeInStock(int stockNumber)
        {
            return state.Stock[stockNumber].Type;
        }

        public void StockInit(uint stockNum, Resource.Type type, uint maximum)
        {
            state.Stock[stockNum].Type = type;
            state.Stock[stockNum].Priority = 0;
            state.Stock[stockNum].Maximum = (byte)maximum;
        }

        public void RemoveStock()
        {
            state.Stock[0].Available = 0;
            state.Stock[0].Requested = 0;
            state.Stock[1].Available = 0;
            state.Stock[1].Requested = 0;
        }

        public int GetMaxPriorityForResource(Resource.Type resource, int minimum = 0)
        {
            int maxPriority = -1;

            // If the emergency program is active and this is no essential building
            // we give no priority to this building.
            var player = Game.GetPlayer(Player);

            if (player.EmergencyProgramActive && (resource == Resource.Type.Plank || resource == Resource.Type.Stone) &&
                BuildingType != Type.Lumberjack && BuildingType != Type.Sawmill && BuildingType != Type.Stonecutter)
            {
                return maxPriority; // = -1
            }

            for (int i = 0; i < MaxStock; ++i)
            {
                if (state.Stock[i].Type == resource &&
                    state.Stock[i].Priority >= minimum &&
                    state.Stock[i].Priority > maxPriority)
                {
                    maxPriority = state.Stock[i].Priority;
                }
            }

            return maxPriority;
        }

        public uint GetMaximumInStock(int stockNumber)
        {
            return state.Stock[stockNumber].Maximum;
        }

        public uint GetRequestedInStock(int stockNumber)
        {
            return state.Stock[stockNumber].Requested;
        }

        public uint GetRequested(Resource.Type resource)
        {
            int inStock = -1;

            for (int i = 0; i < MaxStock; ++i)
            {
                if (state.Stock[i].Type == resource)
                {
                    inStock = i;
                    break;
                }
            }

            if (inStock == -1)
                return 0u;

            return GetRequestedInStock(inStock);
        }

        public void SetPriorityInStock(int stockNumber, uint priority)
        {
            state.Stock[stockNumber].Priority = (byte)priority;
        }

        public void SetInitialResourcesInStock(int stockNumber, uint count)
        {
            state.Stock[stockNumber].Available = (byte)count;
        }

        /// <summary>
        /// Tries to deliver the resource.
        /// 
        /// Returns false if this is not possible or should not be done.
        /// This may happen if the emergency program was activated during
        /// delivery or an unrequested resource was delivered.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public bool RequestedResourceDelivered(Resource.Type resource)
        {
            if (state.Burning)
            {
                return false;
            }

            if (resource == Resource.Type.None)
            {
                return false;
            }

            if (HasInventory())
            {
                Game.GetInventory(state.Inventory).PushResource(resource);
                return true;
            }
            else
            {
                if (resource == Resource.Type.Fish ||
                    resource == Resource.Type.Meat ||
                    resource == Resource.Type.Bread)
                {
                    resource = Resource.Type.GroupFood;
                }

                // Add to building stock
                for (int i = 0; i < MaxStock; ++i)
                {
                    if (state.Stock[i].Type == resource)
                    {
                        if (state.Stock[i].Requested == 0)
                        {
                            if (Game.GetPlayer(Player).EmergencyProgramActive && !IsDone)
                            {
                                // In emergency program we set the requested amount to zero for most buildings.
                                // But sometimes the resource might still be delivered. We stop delivery so the
                                // resource is brought back to castle/stock. No logging needed here as it is
                                // a valid scenario.
                                return false;
                            }
                            else
                            {
                                // We did not request the resource at all
                                Log.Debug.Write(ErrorSystemType.Building, $"Delivered more resources than requested. Index {Index}, Type {BuildingType.ToString()}, Resource {resource.ToString()}");
                                return false;
                            }
                        }
                        else
                        {
                            ++state.Stock[i].Available;
                            --state.Stock[i].Requested;
                            return true; // Resource delivered successfully
                        }
                    }
                }

                // A resource was delivered that is not meant for this building.
                // This should not happen but for safety reasons we log it and deny delivery.
                Log.Debug.Write(ErrorSystemType.Building, $"Delivered unexpected resource. Index {Index}, Type {BuildingType.ToString()}, Resource {resource.ToString()}, Finished {IsDone.ToString()}");
                return false;
            }
        }

        public void PlankUsedForBuild()
        {
            --state.Stock[0].Available;
            --state.Stock[0].Maximum;
        }

        public void StoneUsedForBuild()
        {
            --state.Stock[1].Available;
            --state.Stock[1].Maximum;
        }

        public bool UseResourceInStock(int stockNum)
        {
            if (state.Stock[stockNum].Available > 0)
            {
                --state.Stock[stockNum].Available;
                return true;
            }

            return false;
        }

        public bool UseResourcesInStocks()
        {
            if (state.Stock[0].Available > 0 && state.Stock[1].Available > 0)
            {
                --state.Stock[0].Available;
                --state.Stock[1].Available;
                return true;
            }

            return false;
        }

        public void DecreaseRequestedForStock(int stockNumber)
        {
            --state.Stock[stockNumber].Requested;
        }

        public uint PigsCount()
        {
            return state.Stock[1].Available;
        }

        public void SendPigToButcher()
        {
            --state.Stock[1].Available;
        }

        public void PlaceNewPig()
        {
            ++state.Stock[1].Available;
        }

        public void BoatClear()
        {
            state.Stock[1].Available = 0;
        }

        public void BoatDo()
        {
            ++state.Stock[1].Available;
        }

        public void RequestedKnightArrived()
        {
            ++state.Stock[0].Available;
            --state.Stock[0].Requested;
        }

        public void RequestedKnightAttackingOnWalk()
        {
            --state.Stock[0].Requested;
        }

        public void RequestedKnightDefeatOnWalk()
        {
            if (!HasInventory())
            {
                --state.Stock[0].Requested;
            }
        }

        public bool IsEnoughPlaceForKnight
        {
            get
            {
                int maxCapacity = -1;

                switch (BuildingType)
                {
                    case Type.Hut: maxCapacity = 3; break;
                    case Type.Tower: maxCapacity = 6; break;
                    case Type.Fortress: maxCapacity = 12; break;
                    default: Debug.NotReached(); break;
                }

                var totalKnights = state.Stock[0].Requested + state.Stock[0].Available;

                return totalKnights < maxCapacity;
            }
        }

        public bool KnightComeBackFromFight(Serf knight)
        {
            if (IsEnoughPlaceForKnight)
            {
                ++state.Stock[0].Available;
                var serf = Game.GetSerf(state.FirstKnight);
                knight.InsertKnightBefore(serf);
                state.FirstKnight = (word)knight.Index;

                return true;
            }

            return false;
        }

        public void KnightOccupy()
        {
            if (!HasKnight())
            {
                state.Stock[0].Available = 0;
                state.Stock[0].Requested = 1;
            }
            else
            {
                ++state.Stock[0].Requested;
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
            int threatLevel, borderCheckIndex;
            var map = Game.Map;

            for (threatLevel = 3, borderCheckIndex = 0; threatLevel > 0; --threatLevel)
            {
                int offset;

                while ((offset = BorderCheckOffsets[borderCheckIndex++]) >= 0)
                {
                    var checkPosition = map.PositionAddSpirally(Position, (uint)offset);

                    if (map.HasOwner(checkPosition) && map.GetOwner(checkPosition) != Player)
                    {
                        state.ThreatLevel = (byte)threatLevel;
                        return;
                    }
                }
            }
        }

        public void Update(uint tick)
        {
            if (state.Burning)
            {
                word delta = (word)(tick - state.Tick);
                state.Tick = (word)tick;

                if (BurningCounter >= delta)
                {
                    BurningCounter -= delta;
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
            Position = Game.Map.PositionFromSavedValue(reader.ReadDWord()); // 0

            byte v8 = reader.ReadByte(); // 4
            state.Type = (Type)((v8 >> 2) & 0x1f);
            state.Player = (byte)(v8 & 3u);
            state.Constructing = (v8 & 0x80) != 0;

            v8 = reader.ReadByte(); // 5
            state.ThreatLevel = (byte)(v8 & 3u);
            state.SerfRequestFailed = (v8 & 4) != 0;
            state.PlayingSfx = (v8 & 8) != 0;
            state.Active = (v8 & 16) != 0;
            state.Burning = (v8 & 32) != 0;
            state.Holder = (v8 & 64) != 0;
            state.SerfRequested = (v8 & 128) != 0;

            if (BuildingType == Building.Type.WeaponSmith && state.PlayingSfx)
                state.FreeShieldPossible = true;

            state.Flag = reader.ReadWord(); // 6

            for (int i = 0; i < 2; ++i)
            {
                v8 = reader.ReadByte(); // 8, 9
                state.Stock[i].Type = Resource.Type.None;
                state.Stock[i].Available = 0;
                state.Stock[i].Requested = 0;

                if (v8 != 0xff)
                {
                    state.Stock[i].Available = (byte)((v8 >> 4) & 0xfu);
                    state.Stock[i].Requested = (byte)(v8 & 0xfu);
                }
            }

            state.FirstKnight = reader.ReadWord(); // 10
            state.Progress = reader.ReadWord(); // 12

            if (!state.Burning && IsDone &&
                (BuildingType == Type.Stock ||
                BuildingType == Type.Castle))
            {
                int offset = (int)reader.ReadDWord(); // 14
                state.Inventory = (word)Game.CreateInventory(offset / 120).Index;
                state.Stock[0].Requested = 0xff;
                return;
            }
            else
            {
                state.Level = reader.ReadWord(); // 14
            }

            if (!IsDone)
            {
                state.Stock[0].Type = Resource.Type.Plank;
                state.Stock[0].Maximum = reader.ReadByte(); // 16
                state.Stock[1].Type = Resource.Type.Stone;
                state.Stock[1].Maximum = reader.ReadByte(); // 17
            }
            else if (state.Holder)
            {
                switch (BuildingType)
                {
                    case Type.Boatbuilder:
                        state.Stock[0].Type = Resource.Type.Plank;
                        state.Stock[0].Maximum = 8;
                        break;
                    case Type.StoneMine:
                    case Type.CoalMine:
                    case Type.IronMine:
                    case Type.GoldMine:
                        state.Stock[0].Type = Resource.Type.GroupFood;
                        state.Stock[0].Maximum = 8;
                        break;
                    case Type.Hut:
                        state.Stock[1].Type = Resource.Type.GoldBar;
                        state.Stock[1].Maximum = 2;
                        break;
                    case Type.Tower:
                        state.Stock[1].Type = Resource.Type.GoldBar;
                        state.Stock[1].Maximum = 4;
                        break;
                    case Type.Fortress:
                        state.Stock[1].Type = Resource.Type.GoldBar;
                        state.Stock[1].Maximum = 8;
                        break;
                    case Type.Butcher:
                        state.Stock[0].Type = Resource.Type.Pig;
                        state.Stock[0].Maximum = 8;
                        break;
                    case Type.PigFarm:
                        state.Stock[0].Type = Resource.Type.Wheat;
                        state.Stock[0].Maximum = 8;
                        break;
                    case Type.Mill:
                        state.Stock[0].Type = Resource.Type.Wheat;
                        state.Stock[0].Maximum = 8;
                        break;
                    case Type.Baker:
                        state.Stock[0].Type = Resource.Type.Flour;
                        state.Stock[0].Maximum = 8;
                        break;
                    case Type.Sawmill:
                        state.Stock[1].Type = Resource.Type.Lumber;
                        state.Stock[1].Maximum = 8;
                        break;
                    case Type.SteelSmelter:
                        state.Stock[0].Type = Resource.Type.Coal;
                        state.Stock[0].Maximum = 8;
                        state.Stock[1].Type = Resource.Type.IronOre;
                        state.Stock[1].Maximum = 8;
                        break;
                    case Type.ToolMaker:
                        state.Stock[0].Type = Resource.Type.Plank;
                        state.Stock[0].Maximum = 8;
                        state.Stock[1].Type = Resource.Type.Steel;
                        state.Stock[1].Maximum = 8;
                        break;
                    case Type.WeaponSmith:
                        state.Stock[0].Type = Resource.Type.Coal;
                        state.Stock[0].Maximum = 8;
                        state.Stock[1].Type = Resource.Type.Steel;
                        state.Stock[1].Maximum = 8;
                        break;
                    case Type.GoldSmelter:
                        state.Stock[0].Type = Resource.Type.Coal;
                        state.Stock[0].Maximum = 8;
                        state.Stock[1].Type = Resource.Type.GoldOre;
                        state.Stock[1].Maximum = 8;
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
            state.Position = Game.Map.Position(x, y);
            state.Type = (Type)reader.Value("type").ReadInt();

            try
            {
                state.Player = (byte)reader.Value("owner").ReadUInt();
                state.Constructing = reader.Value("constructing").ReadBool();
            }
            catch
            {
                uint n = reader.Value("bld").ReadUInt();
                state.Player = (byte)(n & 3);
                state.Constructing = (n & 0x80) != 0;
            }
            try
            {
                state.ThreatLevel = (byte)reader.Value("military_state").ReadUInt();
                state.SerfRequestFailed = reader.Value("serf_request_failed").ReadBool();
                state.PlayingSfx = reader.Value("playing_sfx").ReadBool();
                state.Active = reader.Value("active").ReadBool();
                state.Burning = reader.Value("burning").ReadBool();
                state.Holder = reader.Value("holder").ReadBool();
                state.SerfRequested = reader.Value("serf_requested").ReadBool();
            }
            catch
            {
                uint n = reader.Value("serf").ReadUInt();
                state.ThreatLevel = (byte)(n & 3);
                state.SerfRequestFailed = (n & 4) != 0;
                state.PlayingSfx = (n & 8) != 0;
                state.Active = (n & 16) != 0;
                state.Burning = (n & 32) != 0;
                state.Holder = (n & 64) != 0;
                state.SerfRequested = (n & 128) != 0;
            }

            // This is new in freeserf.net
            try
            {
                if (BuildingType == Building.Type.WeaponSmith)
                {
                    if (reader.HasValue("free_shield_possible"))
                        state.FreeShieldPossible = reader.Value("free_shield_possible").ReadBool();
                    else
                    {
                        // In original game the playing sfx bit was
                        // used for switching between swords and shields.
                        state.FreeShieldPossible = state.PlayingSfx;
                    }
                }
                else
                    state.FreeShieldPossible = false;
            }
            catch
            {
                // In original game the playing sfx bit was
                // used for switching between swords and shields.
                state.FreeShieldPossible = state.PlayingSfx;
            }

            state.Flag = (word)reader.Value("flag").ReadUInt();

            state.Stock[0].Type = (Resource.Type)reader.Value("stock[0].type").ReadInt();
            state.Stock[0].Priority = (byte)reader.Value("stock[0].prio").ReadUInt();
            state.Stock[0].Available = (byte)reader.Value("stock[0].available").ReadUInt();
            state.Stock[0].Requested = (byte)reader.Value("stock[0].requested").ReadUInt();
            state.Stock[0].Maximum = (byte)reader.Value("stock[0].maximum").ReadUInt();

            state.Stock[1].Type = (Resource.Type)reader.Value("stock[1].type").ReadInt();
            state.Stock[1].Priority = (byte)reader.Value("stock[1].prio").ReadUInt();
            state.Stock[1].Available = (byte)reader.Value("stock[1].available").ReadUInt();
            state.Stock[1].Requested = (byte)reader.Value("stock[1].requested").ReadUInt();
            state.Stock[1].Maximum = (byte)reader.Value("stock[1].maximum").ReadUInt();

            state.FirstKnight = (word)reader.Value("serf_index").ReadUInt();
            state.Progress = (word)reader.Value("progress").ReadUInt();

            // Load various values that depend on the building type.
            if (!state.Burning && (IsDone || BuildingType == Type.Castle))
            {
                if (BuildingType == Type.Stock || BuildingType == Type.Castle)
                {
                    state.Inventory = (word)reader.Value("inventory").ReadInt();
                    Game.CreateInventory(state.Inventory);
                }
            }
            else if (state.Burning)
            {
                state.Tick = (word)reader.Value("tick").ReadUInt();
            }
            else
            {
                state.Level = (word)reader.Value("level").ReadUInt();
            }
        }

        public void WriteTo(SaveWriterText writer)
        {
            writer.Value("pos").Write(Game.Map.PositionColumn(state.Position));
            writer.Value("pos").Write(Game.Map.PositionRow(state.Position));
            writer.Value("type").Write((int)state.Type);
            writer.Value("owner").Write(Player);
            writer.Value("constructing").Write(state.Constructing);

            writer.Value("military_state").Write((uint)state.ThreatLevel);
            writer.Value("playing_sfx").Write(state.PlayingSfx);
            writer.Value("serf_request_failed").Write(state.SerfRequestFailed);
            writer.Value("serf_requested").Write(state.SerfRequested);
            writer.Value("burning").Write(state.Burning);
            writer.Value("active").Write(state.Active);
            writer.Value("holder").Write(state.Holder);

            // This is new in freeserf.net
            writer.Value("free_shield_possible").Write((BuildingType == Building.Type.WeaponSmith) ? state.FreeShieldPossible : false);

            writer.Value("flag").Write(state.Flag);

            writer.Value("stock[0].type").Write((int)state.Stock[0].Type);
            writer.Value("stock[0].prio").Write((uint)state.Stock[0].Priority);
            writer.Value("stock[0].available").Write((uint)state.Stock[0].Available);
            writer.Value("stock[0].requested").Write((uint)state.Stock[0].Requested);
            writer.Value("stock[0].maximum").Write((uint)state.Stock[0].Maximum);

            writer.Value("stock[1].type").Write((int)state.Stock[1].Type);
            writer.Value("stock[1].prio").Write((uint)state.Stock[1].Priority);
            writer.Value("stock[1].available").Write((uint)state.Stock[1].Available);
            writer.Value("stock[1].requested").Write((uint)state.Stock[1].Requested);
            writer.Value("stock[1].maximum").Write((uint)state.Stock[1].Maximum);

            writer.Value("serf_index").Write((uint)state.FirstKnight);
            writer.Value("progress").Write((uint)state.Progress);

            if (!IsBurning && (IsDone || BuildingType == Type.Castle))
            {
                if (BuildingType == Type.Stock ||
                    BuildingType == Type.Castle)
                {
                    writer.Value("inventory").Write((uint)state.Inventory);
                }
            }
            else if (IsBurning)
            {
                writer.Value("tick").Write((uint)state.Tick);
            }
            else
            {
                writer.Value("level").Write((uint)state.Level);
            }
        }

        void Update()
        {
            if (!state.Constructing)
            {
                RequestSerfIfNeeded();

                var player = Game.GetPlayer(Player);
                var totalResource1 = (uint)(state.Stock[0].Requested + state.Stock[0].Available);
                var totalResource2 = (uint)(state.Stock[1].Requested + state.Stock[1].Available);
                int resource1Modifier = 8 + (int)totalResource1;
                int resource2Modifier = 8 + (int)totalResource2;
                Func<uint> GetPriorityFuncResource1 = null;
                Func<uint> GetPriorityFuncResource2 = null;

                switch (BuildingType)
                {
                    case Type.Boatbuilder:
                        if (state.Holder)
                        {
                            GetPriorityFuncResource1 = player.GetPlanksBoatbuilder; // Planks
                        }
                        break;
                    case Type.StoneMine:
                        if (state.Holder)
                        {
                            GetPriorityFuncResource1 = player.GetFoodStonemine; // Food
                        }
                        break;
                    case Type.CoalMine:
                        if (state.Holder)
                        {
                            GetPriorityFuncResource1 = player.GetFoodCoalmine; // Food
                        }
                        break;
                    case Type.IronMine:
                        if (state.Holder)
                        {
                            GetPriorityFuncResource1 = player.GetFoodIronmine; // Food
                        }
                        break;
                    case Type.GoldMine:
                        if (state.Holder)
                        {
                            GetPriorityFuncResource1 = player.GetFoodGoldmine; // Food
                        }
                        break;
                    case Type.Stock:
                        if (!IsActive)
                        {
                            var inventory = Game.CreateInventory();

                            if (inventory == null)
                                return;

                            inventory.Player = Player;
                            inventory.Building = Index;
                            inventory.Flag = state.Flag;

                            state.Inventory = (word)inventory.Index;
                            state.Stock[0].Requested = 0xff;
                            state.Stock[0].Available = 0xff;
                            state.Stock[1].Requested = 0xff;
                            state.Stock[1].Available = 0xff;
                            state.Active = true;

                            Game.GetPlayer(Player).AddNotification(Notification.Type.NewStock, Position, 0);
                        }
                        else
                        {
                            if (!state.SerfRequestFailed && !state.Holder && !state.SerfRequested)
                            {
                                SendSerfToBuilding(Serf.Type.Transporter,
                                                   Resource.Type.None,
                                                   Resource.Type.None);
                            }

                            var inventory = Game.GetInventory(state.Inventory);

                            if (state.Holder &&
                                !inventory.HasAnyOutMode() && // Not serf or resource OUT mode 
                                inventory.FreeSerfCount() == 0)
                            {
                                if (player.TickSendGenericDelay())
                                {
                                    SendSerfToBuilding(Serf.Type.Generic,
                                                       Resource.Type.None,
                                                       Resource.Type.None);
                                }
                            }

                            // TODO Following code looks like a hack
                            var map = Game.Map;
                            var flagPosition = map.MoveDownRight(Position);

                            if (map.HasSerf(flagPosition))
                            {
                                var serf = Game.GetSerfAtPosition(flagPosition);

                                if (serf.Position != flagPosition)
                                {
                                    map.SetSerfIndex(flagPosition, 0);
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
                        if (state.Holder)
                        {
                            // Meat
                            GetPriorityFuncResource1 = () =>
                            {
                                return 0xffu >> (int)totalResource1;
                            };
                            resource1Modifier = 0;
                        }
                        break;
                    case Type.PigFarm:
                        if (state.Holder)
                        {
                            GetPriorityFuncResource1 = player.GetWheatPigfarm; // Wheat
                        }
                        break;
                    case Type.Mill:
                        if (state.Holder)
                        {
                            GetPriorityFuncResource1 = player.GetWheatMill; // Wheat
                        }
                        break;
                    case Type.Baker:
                        if (state.Holder)
                        {
                            // Flour
                            GetPriorityFuncResource1 = () =>
                            {
                                return 0xffu >> (int)totalResource1;
                            };
                            resource1Modifier = 0;
                        }
                        break;
                    case Type.Sawmill:
                        if (state.Holder)
                        {
                            // Lumber
                            GetPriorityFuncResource1 = () =>
                            {
                                return 0xffu >> (int)totalResource1;
                            };
                            resource1Modifier = 0;
                        }
                        break;
                    case Type.SteelSmelter:
                        if (state.Holder)
                        {
                            // Request more coal 
                            GetPriorityFuncResource1 = player.GetCoalSteelsmelter;

                            // Request more iron ore 
                            GetPriorityFuncResource2 = () =>
                            {
                                return 0xffu >> (int)totalResource2;
                            };
                            resource2Modifier = 0;
                        }
                        break;
                    case Type.ToolMaker:
                        if (state.Holder)
                        {
                            // Request more planks. 
                            GetPriorityFuncResource1 = player.GetPlanksToolmaker;

                            // Request more steel. 
                            GetPriorityFuncResource2 = player.GetSteelToolmaker;
                        }
                        break;
                    case Type.WeaponSmith:
                        if (state.Holder)
                        {
                            // Request more coal. 
                            GetPriorityFuncResource1 = player.GetCoalWeaponsmith;

                            // Request more steel. 
                            GetPriorityFuncResource2 = player.GetSteelWeaponsmith;
                        }
                        break;
                    case Type.GoldSmelter:
                        if (state.Holder)
                        {
                            // Request more coal. 
                            GetPriorityFuncResource1 = player.GetCoalGoldsmelter;

                            // Request more gold ore. 
                            GetPriorityFuncResource2 = () =>
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
                if (GetPriorityFuncResource1 != null)
                {
                    if (totalResource1 < state.Stock[0].Maximum)
                        state.Stock[0].Priority = (byte)(GetPriorityFuncResource1() >> resource1Modifier);
                    else
                        state.Stock[0].Priority = 0;
                }

                // Set priority for resource 2
                if (GetPriorityFuncResource2 != null)
                {
                    if (totalResource2 < state.Stock[1].Maximum)
                        state.Stock[1].Priority = (byte)(GetPriorityFuncResource2() >> resource2Modifier);
                    else
                        state.Stock[1].Priority = 0;
                }
            }
            else
            {
                // Unfinished
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
            var player = Game.GetPlayer(Player);

            // don't send a builder during active emergency program
            // if this is not an essential building
            if (player.EmergencyProgramActive && BuildingType != Type.Lumberjack &&
                BuildingType != Type.Sawmill && BuildingType != Type.Stonecutter)
            {
                if (!state.Holder && state.SerfRequested && !state.SerfRequestFailed)
                    state.SerfRequested = false;

                state.Stock[0].Requested = 0;
                state.Stock[1].Requested = 0;

                return;
            }

            // Request builder serf 
            if (!state.SerfRequestFailed && !state.Holder && !state.SerfRequested)
            {
                state.Progress = 1;
                state.SerfRequestFailed = !SendSerfToBuilding(Serf.Type.Builder,
                                                              Resource.Type.Hammer,
                                                              Resource.Type.None);
            }

            // Request planks
            uint totalPlanks = (uint)(state.Stock[0].Requested + state.Stock[0].Available);

            if (totalPlanks < state.Stock[0].Maximum)
            {
                uint planksPriority = player.GetPlanksConstruction() >> (8 + (int)totalPlanks);

                if (!state.Holder)
                    planksPriority >>= 2;

                state.Stock[0].Priority = (byte)(planksPriority & ~Misc.BitU(0));
            }
            else
            {
                state.Stock[0].Priority = 0;
            }

            // Request stone
            uint totalStone = (uint)(state.Stock[1].Requested + state.Stock[1].Available);

            if (totalStone < state.Stock[1].Maximum)
            {
                uint stonePriority = 0xffu >> (int)totalStone;

                if (!state.Holder)
                    stonePriority >>= 2;

                state.Stock[1].Priority = (byte)(stonePriority & ~Misc.BitU(0));
            }
            else
            {
                state.Stock[1].Priority = 0;
            }
        }

        void UpdateUnfinishedAdvanced()
        {
            if (state.Progress > 0)
            {
                UpdateUnfinished();
                return;
            }

            if (state.Holder || state.SerfRequested)
            {
                return;
            }

            // Check whether building needs leveling
            bool needLeveling = false;
            uint height = (uint)Game.GetLevelingHeight(Position);

            for (uint i = 0; i < 7; ++i)
            {
                var position = Game.Map.PositionAddSpirally(Position, i);

                if (Game.Map.GetHeight(position) != height)
                {
                    needLeveling = true;
                    break;
                }
            }

            if (!needLeveling)
            {
                // Already at the correct level, don't send digger
                state.Progress = 1;
                UpdateUnfinished();
                return;
            }

            // Request digger
            if (!state.SerfRequestFailed)
            {
                state.SerfRequestFailed = !SendSerfToBuilding(Serf.Type.Digger,
                                                              Resource.Type.Shovel,
                                                              Resource.Type.None);
            }
        }

        void UpdateCastle()
        {
            var player = Game.GetPlayer(Player);
            var inventory = Game.GetInventory(state.Inventory);

            if (player.CastleKnights == player.CastleKnightsWanted)
            {
                Serf bestKnight = null;
                Serf lastKnight = null;
                uint nextSerfIndex = state.FirstKnight;

                while (nextSerfIndex != 0)
                {
                    var serf = Game.GetSerf(nextSerfIndex);

                    if (serf == null)
                    {
                        throw new ExceptionFreeserf(Game, ErrorSystemType.Building, "Index of nonexistent serf in the queue.");
                    }

                    if (bestKnight == null || serf.SerfType < bestKnight.SerfType)
                    {
                        bestKnight = serf;
                    }

                    lastKnight = serf;
                    nextSerfIndex = serf.NextKnight;
                }

                if (bestKnight != null)
                {
                    var knightType = bestKnight.SerfType;

                    for (int type = (int)Serf.Type.Knight0; type <= (int)Serf.Type.Knight4; ++type)
                    {
                        if ((int)knightType > type)
                        {
                            inventory.CallInternal(bestKnight);
                        }
                    }

                    // Switch types
                    var bestKnightType = bestKnight.SerfType;
                    bestKnight.SerfType = lastKnight.SerfType;
                    lastKnight.SerfType = bestKnightType;
                }
            }
            else if (player.CastleKnights < player.CastleKnightsWanted)
            {
                var knightType = Serf.Type.None;

                for (int type = (int)Serf.Type.Knight4; type >= (int)Serf.Type.Knight0; --type)
                {
                    if (inventory.HasSerf((Serf.Type)type))
                    {
                        knightType = (Serf.Type)type;
                        break;
                    }
                }

                if (knightType < 0)
                {
                    // None found
                    if (inventory.HasSerf(Serf.Type.Generic) &&
                        inventory.GetCountOf(Resource.Type.Sword) != 0 &&
                        inventory.GetCountOf(Resource.Type.Shield) != 0)
                    {
                        var serf = inventory.SpecializeFreeSerf(Serf.Type.Knight0);
                        inventory.CallInternal(serf);

                        serf.AddToDefendingQueue(state.FirstKnight, false);
                        state.FirstKnight = (word)serf.Index;
                        player.IncreaseCastleKnights();
                    }
                    else
                    {
                        // if we don't have a knight, send one
                        if (player.TickSendKnightDelay())
                            SendKnightToBuilding();
                    }
                }
                else
                {
                    // Prepend to knights list
                    var serf = inventory.CallInternal(knightType);
                    serf.AddToDefendingQueue(state.FirstKnight, true);
                    state.FirstKnight = (word)serf.Index;
                    player.IncreaseCastleKnights();
                }
            }
            else
            {
                player.DecreaseCastleKnights();

                var serfIndex = state.FirstKnight;
                var serf = Game.GetSerf(serfIndex);
                state.FirstKnight = (word)serf.NextKnight;

                serf.StayIdleInStock(state.Inventory);
            }

            if (state.Holder &&
                !inventory.HasAnyOutMode() && // Not serf or resource OUT mode
                inventory.FreeSerfCount() == 0)
            {
                if (player.TickSendGenericDelay())
                {
                    SendSerfToBuilding(Serf.Type.Generic,
                                       Resource.Type.None,
                                       Resource.Type.None);
                }
            }

            var map = Game.Map;
            var flagPosition = map.MoveDownRight(Position);

            if (map.HasSerf(flagPosition))
            {
                var serf = Game.GetSerfAtPosition(flagPosition);

                if (serf.Position != flagPosition)
                {
                    map.SetSerfIndex(flagPosition, 0);
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
            var player = Game.GetPlayer(Player);
            uint maxOccupiedLevel = (player.GetKnightOccupation(state.ThreatLevel) >> 4) & 0xf;

            if (player.ReducedKnightLevel)
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

            uint totalKnights = (uint)(state.Stock[0].Requested + state.Stock[0].Available);
            uint presentKnights = state.Stock[0].Available;

            if (totalKnights < neededOccupants)
            {
                if (!state.SerfRequestFailed)
                {
                    state.SerfRequestFailed = !SendKnightToBuilding();
                }
            }
            else if (neededOccupants < presentKnights && !Game.Map.HasSerf(Game.Map.MoveDownRight(Position)))
            {
                // Kick least trained knight out.
                Serf leavingSerf = null;
                uint serfIndex = state.FirstKnight;

                while (serfIndex != 0)
                {
                    var serf = Game.GetSerf(serfIndex);

                    if (serf == null)
                    {
                        throw new ExceptionFreeserf(Game, ErrorSystemType.Building, "Index of nonexistent serf in the queue.");
                    }

                    if (leavingSerf == null || serf.SerfType < leavingSerf.SerfType)
                    {
                        leavingSerf = serf;
                    }

                    serfIndex = serf.NextKnight;
                }

                if (leavingSerf != null)
                {
                    // Remove leaving serf from list.
                    if (leavingSerf.Index == state.FirstKnight)
                    {
                        state.FirstKnight = (word)leavingSerf.NextKnight;
                    }
                    else
                    {
                        serfIndex = state.FirstKnight;

                        while (serfIndex != 0)
                        {
                            var serf = Game.GetSerf(serfIndex);

                            if (serf.NextKnight == leavingSerf.Index)
                            {
                                serf.SetNextKnight(leavingSerf.NextKnight);
                                break;
                            }

                            serfIndex = serf.NextKnight;
                        }
                    }

                    // Update serf state.
                    leavingSerf.GoOutFromBuilding(0, 0, -2);

                    state.Stock[0].Available -= 1;
                }
            }

            // Request gold
            if (state.Holder)
            {
                uint totalGold = (uint)(state.Stock[1].Requested + state.Stock[1].Available);

                player.IncreaseMilitaryMaxGold(maxGold);

                if (totalGold < maxGold)
                {
                    state.Stock[1].Priority = (byte)(((0xfeu >> (int)totalGold) + 1) & 0xfe);
                }
                else
                {
                    state.Stock[1].Priority = 0;
                }
            }
        }

        internal struct Request
        {
            public Request(Serf.Type serfType, Resource.Type resourceType1, Resource.Type resourceType2)
            {
                SerfType = serfType;
                ResourceType1 = resourceType1;
                ResourceType2 = resourceType2;
            }

            public Serf.Type SerfType;
            public Resource.Type ResourceType1;
            public Resource.Type ResourceType2;
        }

        internal static readonly Request[] Requests = new Request[]
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
            if (!state.SerfRequestFailed && !state.Holder && !state.SerfRequested)
            {
                int type = (int)BuildingType;

                if (Requests[type].SerfType != Serf.Type.None)
                {
                    state.SerfRequestFailed = !SendSerfToBuilding(Requests[type].SerfType,
                                                                  Requests[type].ResourceType1,
                                                                  Requests[type].ResourceType2);
                }
            }
        }

        bool SendSerfToBuilding(Serf.Type type, Resource.Type resource1, Resource.Type resource2)
        {
            return Game.SendSerfToFlag(Game.GetFlag(state.Flag), type, resource1, resource2);
        }

        bool SendKnightToBuilding()
        {
            // Note: If serf type is None, any knight is used (preferring strong knights while cycling them)
            return SendSerfToBuilding(Serf.Type.None, Resource.Type.None, Resource.Type.None);
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
