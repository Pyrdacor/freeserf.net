/*
 * Game.cs - Gameplay related functions
 *
 * Copyright (C) 2013-2017   Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018	    Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Threading.Tasks;

namespace Freeserf
{
    using MapPos = UInt32;

    using ListSerfs = List<Serf>;
    using ListBuildings = List<Building>;
    using ListInventories = List<Inventory>;
    using Flags = Collection<Flag>;
    using Inventories = Collection<Inventory>;
    using Buildings = Collection<Building>;
    using Serfs = Collection<Serf>;
    using Players = Collection<Player>;
    using Values = Dictionary<uint, uint>;
    using Dirs = Stack<Direction>;

    public class Game
    {
        public const int DEFAULT_GAME_SPEED = 2;
        public const int GAME_MAX_PLAYER_COUNT = 4;

        protected Map map;

        protected uint mapGoldMoraleFactor;
        protected uint goldTotal;

        protected Players players;
        protected Flags flags;
        protected Inventories inventories;
        protected Buildings buildings;
        protected Serfs serfs;

        protected Random initMapRandom;
        protected uint gameSpeedSave;
        protected uint gameSpeed;
        protected ushort tick;
        protected uint lastTick;
        protected uint constTick;
        protected uint gameStatsCounter;
        protected uint historyCounter;
        protected Random random;
        protected ushort nextIndex;
        protected ushort flagSearchCounter;

        protected ushort updateMapLastTick;
        protected short updateMapCounter;
        protected MapPos updateMapInitialPos;
        protected int tickDiff;
        protected ushort maxNextIndex;
        protected short updateMap16Loop;
        protected int[] playerHistoryIndex = new int[4];
        protected int[] playerHistoryCounter = new int[3];
        protected int resourceHistoryIndex;
        protected ushort field340;
        protected ushort field342;
        protected Inventory field344;
        protected int gameType;
        protected int tutorialLevel;
        protected int missionLevel;
        protected int mapPreserveBugs;
        protected int playerScoreLeader;

        protected int knightMoraleCounter;
        protected int inventoryScheduleCounter;

        public Map Map => map;
        public ushort Tick => tick;
        public uint ConstTick => constTick;
        public uint MapGoldMoraleFactor => mapGoldMoraleFactor;
        public uint GoldTotal => goldTotal;

        public Game()
        {
            random = new Random(DateTime.Now.Millisecond);

            players = new Players(this);
            flags = new Flags(this);
            inventories = new Inventories(this);
            buildings = new Buildings(this);
            serfs = new Serfs(this);

            /* Create NULL-serf */
            serfs.Allocate();

            /* Create NULL-building (index 0 is undefined) */
            buildings.Allocate();

            /* Create NULL-flag (index 0 is undefined) */
            flags.Allocate();

            /* Initialize global lookup tables */
            gameSpeed = DEFAULT_GAME_SPEED;

            updateMapLastTick = 0;
            updateMapCounter = 0;
            updateMap16Loop = 0;
            updateMapInitialPos = 0;
            nextIndex = 0;

            resourceHistoryIndex = 0;

            tick = 0;
            constTick = 0;
            tickDiff = 0;

            maxNextIndex = 0;
            gameType = 0;
            flagSearchCounter = 0;
            gameStatsCounter = 0;
            historyCounter = 0;

            knightMoraleCounter = 0;
            inventoryScheduleCounter = 0;

            goldTotal = 0;
        }

        public void AddGoldTotal(int delta)
        {
            goldTotal = (uint)Math.Max(0, goldTotal + delta);
        }

        public Building GetBuildingAtPos(MapPos pos)
        {

        }

        public Flag GetFlagAtPos(MapPos pos)
        {

        }

        public Serf GetSerfAtPos(MapPos pos)
        {

        }


        #region External interface

        public uint AddPlayer(uint intelligence, uint supplies, uint reproduction)
        {

        }

        public bool Init(uint mapSize, Random random)
        {
            initMapRandom = random;

            map = new Map(new MapGeometry(mapSize));
            var generator = new ClassicMissionMapGenerator(map, initMapRandom);

            generator.Init();
            generator.Generate();
            map.InitTiles(generator);
            goldTotal = map.GetGoldDeposit();

            return true;
        }

        /* Update game state after tick increment. */
        public void Update()
        {
            /* Increment tick counters */
            ++constTick;

            /* Update tick counters based on game speed */
            lastTick = tick;
            tick += (ushort)gameSpeed;
            tickDiff = (int)(tick - lastTick);

            ClearSerfRequestFailure();
            map.Update(tick, initMapRandom);

            /* Update players */
            foreach (Player player in players)
            {
                player.Update();
            }

            /* Update knight morale */
            knightMoraleCounter -= tickDiff;

            if (knightMoraleCounter < 0)
            {
                UpdateKnightMorale();
                knightMoraleCounter += 256;
            }

            /* Schedule resources to go out of inventories */
            inventoryScheduleCounter -= tickDiff;

            if (inventoryScheduleCounter < 0)
            {
                UpdateInventories();
                inventoryScheduleCounter += 64;
            }

            // TODO: AI
#if false
              /* AI related updates */
              game.next_index = (game.next_index + 1) % game.max_next_index;
              if (game.next_index > 32) {
                for (int i = 0; i < game.max_next_index) {
                  int i = 33 - game.next_index;
                  player_t *player = game.player[i & 3];
                  if (PLAYER_IS_ACTIVE(player) && PLAYER_IS_AI(player)) {
                    /* AI */
                    /* TODO */
                  }
                  game.next_index += 1;
                }
              } else if (game.game_speed > 0 &&
                   game.max_flag_index < 50) {
                player_t *player = game.player[game.next_index & 3];
                if (PLAYER_IS_ACTIVE(player) && PLAYER_IS_AI(player)) {
                  /* AI */
                  /* TODO */
                }
              }
#endif

            UpdateFlags();
            UpdateBuildings();
            UpdateSerfs();
            UpdateGameStats();
        }

        public void Pause()
        {
            if (gameSpeed != 0)
            {
                gameSpeedSave = gameSpeed;
                gameSpeed = 0;
            }
            else
            {
                gameSpeed = gameSpeedSave;
            }

            Log.Info.Write("game", $"Game speed: {gameSpeed}");
        }

        public void IncreaseSpeed()
        {
            if (gameSpeed < 40) // TODO: is this really correct? May should be 4?
            {
                ++gameSpeed;
                Log.Info.Write("game", $"Game speed: {gameSpeed}");
            }
        }

        public void DecreaseSpeed()
        {
            if (gameSpeed > 0)
            {
                --gameSpeed;
                Log.Info.Write("game", $"Game speed: {gameSpeed}");
            }
        }

        public void ResetSpeed()
        {
            gameSpeed = DEFAULT_GAME_SPEED;
            Log.Info.Write("game", $"Game speed: {gameSpeed}");
        }

        /* Prepare a ground analysis at position. */
        public void PrepareGroundAnalysis(MapPos pos, uint[] estimates)
        {
            const uint groundAnalysisRadius = 25;

            for (int i = 0; i < 5; ++i)
                estimates[i] = 0;

            /* Sample the cursor position with maximum weighting. */
            GetResourceEstimate(pos, groundAnalysisRadius, estimates);

            /* Move outward in a spiral around the initial pos.
               The weighting of the samples attenuates linearly
               with the distance to the center. */
            for (uint i = 0; i < groundAnalysisRadius - 1; ++i)
            {
                pos = map.MoveRight(pos);

                var cycle = new DirectionCycleCW(Direction.Down, 6);

                foreach (Direction d in cycle)
                {
                    for (uint j = 0; j < i + 1; ++j)
                    {
                        GetResourceEstimate(pos, groundAnalysisRadius - i, estimates);
                        pos = map.Move(pos, d);
                    }
                }
            }

            /* Process the samples. */
            for (int i = 0; i < 5; ++i)
            {
                estimates[i] >>= 4;
                estimates[i] = Math.Min(estimates[i], 999);
            }
        }

        public bool SendGeologist(Flag dest)
        {
            return SendSerfToFlag(dest, Serf.Type.Geologist, Resource.Type.Hammer, Resource.Type.None);
        }

        public uint GetLevelingHeight(MapPos pos)
        {

        }

        public bool CanBuildMilitary(MapPos pos)
        {

        }

        public bool CanBuildSmall(MapPos pos)
        {

        }

        public bool CanBuildMine(MapPos pos)
        {

        }

        public bool CanBuildLarge(MapPos pos)
        {

        }

        public bool CanBuildBuilding(MapPos pos, Building.Type type, Player player)
        {

        }

        public bool CanBuildCastle(MapPos pos, Player player)
        {

        }

        public bool CanBuildFlag(MapPos pos, Player player)
        {

        }

        public bool CanPlayerBuild(MapPos pos, Player player)
        {

        }

        /* Test whether a given road can be constructed by player. The final
           destination will be returned in dest, and water will be set if the
           resulting path is a water path.
           This will return success even if the destination does _not_ contain
           a flag, and therefore partial paths can be validated with this function. */
        public int CanBuildRoad(Road road, Player player, ref MapPos dest, ref bool water)
        {
            /* Follow along path to other flag. Test along the way
               whether the path is on ground or in water. */
            MapPos pos = road.Source;
            int test = 0;

            if (!map.HasOwner(pos) || map.GetOwner(pos) != player.Index || !map.HasFlag(pos))
            {
                return 0;
            }

            Dirs dirs = road.Dirs;
            int i = 0;

            foreach (var dir in dirs)
            {
                ++i;

                if (!map.IsRoadSegmentValid(pos, dir))
                {
                    return -1;
                }

                if (map.RoadSegmentInWater(pos, dir))
                {
                    test |= Misc.Bit(1);
                }
                else
                {
                    test |= Misc.Bit(0);
                }

                pos = map.Move(pos, dir);

                /* Check that owner is correct, and that only the destination has a flag. */
                if (!map.HasOwner(pos) || map.GetOwner(pos) != player.Index ||
                    (map.HasFlag(pos) && i != dirs.Count))
                {
                    return 0;
                }
            }

            dest = pos;

            /* Bit 0 indicates a ground path, bit 1 indicates
               awater path. Abort if path went through both
               ground and water. */
            bool w = false;

            if (Misc.BitTest(test, 1))
            {
                w = true;

                if (Misc.BitTest(test, 0))
                    return 0;
            }

            water = w;

            return 1;
        }

        public bool CanDemolishFlag(MapPos pos, Player player)
        {

        }

        public bool CanDemolishRoad(MapPos pos, Player player)
        {

        }

        /* Construct a road specified by a source and a list of directions. */
        public bool BuildRoad(Road road, Player player)
        {
            if (road.Length == 0)
                return false;

            MapPos dest = 0;
            bool waterPath = false;

            if (CanBuildRoad(road, player, ref dest, ref waterPath) == 0)
            {
                return false;
            }

            if (!map.HasFlag(dest))
                return false;

            Dirs dirs = road.Dirs;
            Direction outDir = dirs.Peek();
            Direction inDir = dirs.Last().Reverse();

            /* Actually place road segments */
            if (!map.PlaceRoadSegments(road))
                return false;

            /* Connect flags */
            Flag sourceFlag = GetFlagAtPos(road.Source);
            Flag destFlag = GetFlagAtPos(dest);

            sourceFlag.LinkWithFlag(destFlag, waterPath, road.Length, inDir, outDir);

            return true;
        }

        public bool BuildFlag(MapPos pos, Player player)
        {

        }

        public bool BuildBuilding(MapPos pos, Building.Type type, Player player)
        {

        }

        public bool BuildCastle(MapPos pos, Player player)
        {

        }

        /* Demolish road at position. */
        public bool DemolishRoad(MapPos pos, Player player)
        {
            if (!CanDemolishRoad(pos, player))
                return false;

            return DemolishRoad(pos);
        }

        public bool DemolishFlag(MapPos pos, Player player)
        {

        }

        public bool DemolishBuilding(MapPos pos, Player player)
        {

        }

        public void SetInventoryResourceMode(Inventory inventory, int mode)
        {

        }

        public void SetInventorySerfMode(Inventory inventory, int mode)
        {

        }

        #endregion


        #region Internal interface

        void InitLandOwnership()
        {

        }

        internal void UpdateLandOwnership(MapPos pos)
        {

        }

        internal void OccupyEnemyBuilding(Building building, int player)
        {

        }

        internal void CancelTransportedResource(Resource.Type type, uint dest)
        {

        }

        internal void LoseResource(Resource.Type type)
        {

        }

        internal ushort RandomInt()
        {

        }

        /* Dispatch serf from (nearest?) inventory to flag. */
        internal bool SendSerfToFlag(Flag dest, Serf.Type type, Resource.Type resource1, Resource.Type resource2)
        {
            Building building = null;

            if (dest.HasBuilding())
            {
                building = dest.GetBuilding();
            }

            int serfType = (int)type;

            /* If type is negative, building is non-null. */
            if (serfType < 0 && building != null)
            {
                Player player = players[building.Player];
                serfType = player.GetCyclingSerfType(type);
            }

            SendSerfToFlagData data = new SendSerfToFlagData();
            data.Inventory = null;
            data.Building = building;
            data.SerfType = serfType;
            data.DestIndex = (int)dest.Index;
            data.Resource1 = resource1;
            data.Resource2 = resource2;

            if (!FlagSearch.Single(dest, SendSerfToFlagSearchCb, true, false, data))
            {
                return false;
            }
            else if (data.Inventory != null)
            {
                Inventory inventory = data.Inventory;
                Serf serf = inventory.CallOutSerf(Serf.Type.Generic);

                if (type < 0 && building != null)
                {
                    /* Knight */
                    building.KnightRequestGranted();

                    serf.SetSerfType(Serf.Type.Knight0);
                    serf.GoOutFromInventory(inventory.Index, building.GetFlagIndex(), -1);

                    inventory.PopResource(Resource.Type.Sword);
                    inventory.PopResource(Resource.Type.Shield);
                }
                else
                {
                    serf.SetSerfType((Serf.Type)serfType);

                    int mode = 0;

                    if (type == Serf.Type.Geologist)
                    {
                        mode = 6;
                    }
                    else
                    {
                        if (building == null)
                        {
                            return false;
                        }

                        building.SerfRequestGranted();
                        mode = -1;
                    }

                    serf.GoOutFromInventory(inventory.Index, dest.Index, mode);

                    if (resource1 != Resource.Type.None)
                        inventory.PopResource(resource1);
                    if (resource2 != Resource.Type.None)
                        inventory.PopResource(resource2);
                }

                return true;
            }

            return true;
        }

        int GetPlayerHistoryIndex(uint scale)
        {
            return playerHistoryIndex[scale];
        }

        int GetResourceHistoryIndex()
        {
            return resourceHistoryIndex;
        }

        internal int NextSearchId()
        {

        }

        internal Serf CreateSerf(int index = -1)
        {

        }

        internal void DeleteSerf(Serf serf)
        {

        }

        internal Flag CreateFlag(int index = -1)
        {

        }

        internal Inventory CreateInventory(int index = -1)
        {

        }

        internal void DeleteInventory(uint index)
        {
            DeleteInventory(GetInventory(index));
        }

        internal void DeleteInventory(Inventory inventory)
        {

        }

        internal Building CreateBuilding(int index = -1)
        {

        }

        internal void DeleteBuilding(Building building)
        {

        }

        public Serf GetSerf(uint index)
        {
            return serfs[index];
        }

        public Flag GetFlag(uint index)
        {
            return flags[index];
        }

        public Inventory GetInventory(uint index)
        {
            return inventories[index];
        }

        public Building GetBuilding(uint index)
        {
            return buildings[index];
        }

        public Player GetPlayer(uint index)
        {
            return players[index];
        }

        public ListSerfs GetPlayerSerfs(Player player)
        {

        }

        public ListBuildings GetPlayerBuildings(Player player)
        {

        }

        public ListSerfs GetSerfsInInventory(Inventory inventory)
        {

        }

        internal ListSerfs GetSerfsRelatedTo(uint dest, Direction dir)
        {

        }

        public ListInventories GetPlayerInventories(Player player)
        {

        }

        public ListSerfs GetSerfsAtPos(MapPos pos)
        {

        }

        public Player GetNextPlayer(Player player)
        {

        }

        public uint GetEnemyScore(Player player)
        {

        }

        internal void BuildingCaptured(Building building)
        {

        }

        void ClearSearchId()
        {

        }

        /* Clear the serf request bit of all flags and buildings.
           This allows the flag or building to try and request a
           serf again. */
        protected void ClearSerfRequestFailure()
        {
            foreach (Building building in buildings)
            {
                building.ClearSerfRequestFailure();
            }

            foreach (Flag flag in flags)
            {
                flag.SerfRequestClear();
            }
        }

        protected void UpdateKnightMorale()
        {
            foreach (Player player in players)
            {
                player.UpdateKnightMorale();
            }
        }

        class UpdateInventoriesData
        {
            public Resource.Type Resource;
            public int[] MaxPrio;
            public Flag[] Flags;
        }

        protected static bool UpdateInventoriesCb(Flag flag, object data)
        {
            UpdateInventoriesData updateData = data as UpdateInventoriesData;

            int index = (int)flag.Tag;

            if (updateData.MaxPrio[index] < 255 && flag.HasBuilding())
            {
                Building building = flag.GetBuilding();

                int buildingPrio = building.GetMaxPriorityForResource(updateData.Resource, 16);

                if (buildingPrio > updateData.MaxPrio[index])
                {
                    updateData.MaxPrio[index] = buildingPrio;
                    updateData.Flags[index] = flag;
                }
            }

            return false;
        }

        static readonly Resource.Type[] ResourceArray1 = new Resource.Type[]
        {
            Resource.Type.Plank,
            Resource.Type.Stone,
            Resource.Type.Steel,
            Resource.Type.Coal,
            Resource.Type.Lumber,
            Resource.Type.IronOre,
            Resource.Type.GroupFood,
            Resource.Type.Pig,
            Resource.Type.Flour,
            Resource.Type.Wheat,
            Resource.Type.GoldBar,
            Resource.Type.GoldOre,
            Resource.Type.None,
        };

        static readonly Resource.Type[] ResourceArray2 = new Resource.Type[]
        {
            Resource.Type.Stone,
            Resource.Type.IronOre,
            Resource.Type.GoldOre,
            Resource.Type.Coal,
            Resource.Type.Steel,
            Resource.Type.GoldBar,
            Resource.Type.GroupFood,
            Resource.Type.Pig,
            Resource.Type.Flour,
            Resource.Type.Wheat,
            Resource.Type.Lumber,
            Resource.Type.Plank,
            Resource.Type.None,
        };

        static readonly Resource.Type[] ResourceArray3 = new Resource.Type[]
        {
            Resource.Type.GroupFood,
            Resource.Type.Wheat,
            Resource.Type.Pig,
            Resource.Type.Flour,
            Resource.Type.GoldBar,
            Resource.Type.Stone,
            Resource.Type.Plank,
            Resource.Type.Steel,
            Resource.Type.Coal,
            Resource.Type.Lumber,
            Resource.Type.GoldOre,
            Resource.Type.IronOre,
            Resource.Type.None,
        };

        /* Update inventories as part of the game progression. Moves the appropriate
           resources that are needed outside of the inventory into the out queue. */
        protected void UpdateInventories()
        {
            /* AI: TODO */

            Resource.Type[] resources = null;
            int arrIndex = 0;

            // TODO: really use random to select the order? There seems to be no fixed order. Maybe use flag priorities of player?
            switch (RandomInt() & 7)
            {
                case 0: resources = ResourceArray2;
                    break;
                case 1: resources = ResourceArray3;
                    break;
                default: resources = ResourceArray1;
                    break;
            }

            while (resources[arrIndex] != Resource.Type.None)
            {
                foreach (Player player in players)
                {
                    Inventory[] sourceInventories = new Inventory[256];
                    int n = 0;

                    foreach (Inventory inventory in inventories)
                    {
                        if (inventory.Player == player.Index && !inventory.IsQueueFull())
                        {
                            Inventory.Mode resMode = inventory.GetResourceMode();

                            if (resMode == Inventory.Mode.In || resMode == Inventory.Mode.Stop)
                            {
                                if (resources[arrIndex] == Resource.Type.GroupFood)
                                {
                                    if (inventory.HasFood())
                                    {
                                        sourceInventories[n++] = inventory;

                                        if (n == 256)
                                            break;
                                    }
                                }
                                else if (inventory.GetCountOf(resources[arrIndex]) != 0)
                                {
                                    sourceInventories[n++] = inventory;

                                    if (n == 256)
                                        break;
                                }
                            }
                            else
                            { 
                                /* Out mode */
                                int prio = 0;
                                Resource.Type type = Resource.Type.None;

                                for (int i = 0; i < 26; i++)
                                {
                                    if (inventory.GetCountOf((Resource.Type)i) != 0 &&
                                        player.GetInventoryPriority(i) >= prio)
                                    {
                                        prio = player.GetInventoryPriority(i);
                                        type = (Resource.Type)i;
                                    }
                                }

                                if (type != Resource.Type.None)
                                {
                                    inventory.AddToQueue(type, 0);
                                }
                            }
                        }
                    }

                    if (n == 0)
                        continue;

                    FlagSearch search = new FlagSearch(this);

                    int[] maxPrio = new int[256];
                    Flag[] flags = new Flag[256];

                    for (int i = 0; i < n; ++i)
                    {
                        maxPrio[i] = 0;
                        flags[i] = null;
                        Flag flag = this.flags[sourceInventories[i].GetFlagIndex()];
                        // Note: it seems that SearchDir was abused for indexing here but (Direction)i will not work with i >= 6.
                        // We added a general purpose tagged object for flags instead.
                        flag.Tag = i;
                        search.AddSource(flag);
                    }

                    UpdateInventoriesData data = new UpdateInventoriesData();
                    data.Resource = resources[arrIndex];
                    data.MaxPrio = maxPrio;
                    data.Flags = flags;
                    search.Execute(UpdateInventoriesCb, false, true, data);

                    for (int i = 0; i < n; ++i)
                    {
                        if (maxPrio[i] > 0)
                        {
                            Log.Verbose.Write("game", $" dest for inventory {i} found");
                            Resource.Type resource = resources[arrIndex];

                            Building destBuilding = flags[i].GetBuilding();

                            if (!destBuilding.AddRequestedResource(resource, false))
                            {
                                throw new ExceptionFreeserf("Failed to request resource.");
                            }

                            /* Put resource in out queue */
                            sourceInventories[i].AddToQueue(resource, destBuilding.GetFlagIndex());
                        }
                    }
                }

                ++arrIndex;
            }
        }

        protected void UpdateFlags()
        {
            foreach (Flag flag in flags)
            {
                flag.Update();
            }
        }

        class SendSerfToFlagData
        {
            public Inventory Inventory;
            public Building Building;
            public int SerfType;
            public int DestIndex;
            public Resource.Type Resource1;
            public Resource.Type Resource2;
        }

        protected static bool SendSerfToFlagSearchCb(Flag flag, object data)
        {
            if (!flag.HasInventory())
            {
                return false;
            }

            SendSerfToFlagData sendData = data as SendSerfToFlagData;

            /* Inventory reached */
            Building building = flag.GetBuilding();
            Inventory inventory = building.GetInventory();

            int type = sendData.SerfType;

            if (type < 0)
            {
                int knightType = -1;

                for (int i = 4; i >= -type - 1; --i)
                {
                    if (inventory.HaveSerf(Serf.Type.Knight0 + i))
                    {
                        knightType = i;
                        break;
                    }
                }

                if (knightType >= 0)
                {
                    /* Knight of appropriate type was found. */
                    Serf serf = inventory.CallOutSerf(Serf.Type.Knight0 + knightType);

                    sendData.Building.KnightRequestGranted();

                    serf.GoOutFromInventory(inventory.Index, sendData.Building.GetFlagIndex(), -1);

                    return true;
                }
                else if (type == -1)
                {
                    /* See if a knight can be created here. */
                    if (inventory.HaveSerf(Serf.Type.Generic) &&
                        inventory.GetCountOf(Resource.Type.Sword) > 0 &&
                        inventory.GetCountOf(Resource.Type.Shield) > 0)
                    {
                        sendData.Inventory = inventory;
                        return true;
                    }
                }
            }
            else
            {
                if (inventory.HaveSerf((Serf.Type)type))
                {
                    if (type != (int)Serf.Type.Generic || inventory.FreeSerfCount() > 4)
                    {
                        Serf serf = inventory.CallOutSerf((Serf.Type)type);

                        int mode = 0;

                        if (type == (int)Serf.Type.Generic)
                        {
                            mode = -2;
                        }
                        else if (type == (int)Serf.Type.Geologist)
                        {
                            mode = 6;
                        }
                        else
                        {
                            Building destBuilding = flag.Game.flags[(uint)sendData.DestIndex].GetBuilding();
                            destBuilding.SerfRequestGranted();
                            mode = -1;
                        }

                        serf.GoOutFromInventory(inventory.Index, (uint)sendData.DestIndex, mode);

                        return true;
                    }
                }
                else
                {
                    if (sendData.Inventory == null &&
                        inventory.HaveSerf(Serf.Type.Generic) &&
                        (sendData.Resource1 == Resource.Type.None || inventory.GetCountOf(sendData.Resource1) > 0) &&
                        (sendData.Resource2 == Resource.Type.None || inventory.GetCountOf(sendData.Resource2) > 0))
                    {
                        sendData.Inventory = inventory;
                        /* player_t *player = globals.player[SERF_PLAYER(serf)]; */
                        /* game.field_340 = player.cont_search_after_non_optimal_find; */
                        return true;
                    }
                }
            }

            return false;
        }

        protected void UpdateBuildings()
        {
            // Note: Do not use foreac here as building.Update()
            // may delete the building and therefore change the
            // collection while we iterate through it!

            // Therefore we use a copied list here.
            var buildingList = buildings.ToList();

            for (int i = 0; i < buildingList.Count; ++i)
            {
                buildingList[i].Update(tick);
            }
        }

        protected void UpdateSerfs()
        {
            foreach (Serf serf in serfs)
            {
                serf.Update();
            }
        }

        protected void RecordPlayerHistory(int maxLevel, int aspect, int[] historyIndex, Values values)
        {
            uint total = 0;

            foreach (var value in values)
            {
                total += value.Value;
            }

            total = Math.Max(1u, total);

            for (int i = 0; i < maxLevel + 1; ++i)
            {
                int mode = (aspect << 2) | i;
                int index = historyIndex[i];

                foreach (var value in values)
                {
                    players[value.Key].SetPlayerStatHistory(mode, index, (uint)(100ul * value.Value / total));
                }
            }
        }

        /* Calculate whether one player has enough advantage to be
           considered a clear winner regarding one aspect.
           Return -1 if there is no clear winner. */
        protected int CalculateClearWinner(Values values)
        {
            uint total = 0;

            foreach (var value in values)
            {
                total += value.Value;
            }

            total = Math.Max(1u, total);

            foreach (var value in values)
            {
                if (100ul * value.Value / total >= 75)
                    return (int)value.Key;
            }

            return -1;
        }

        /* Update statistics of the game. */
        protected void UpdateGameStats()
        {
            if ((int)gameStatsCounter > tickDiff)
            {
                gameStatsCounter -= (uint)tickDiff;
            }
            else
            {
                gameStatsCounter += (uint)(1500 - tickDiff);

                playerScoreLeader = 0;

                int updateLevel = 0;

                /* Update first level index */
                playerHistoryIndex[0] = playerHistoryIndex[0] + 1 < 112 ? playerHistoryIndex[0] + 1 : 0;
                --playerHistoryCounter[0];

                if (playerHistoryCounter[0] < 0)
                {
                    updateLevel = 1;
                    playerHistoryCounter[0] = 3;

                    /* Update second level index */
                    playerHistoryIndex[1] = playerHistoryIndex[1] + 1 < 112 ? playerHistoryIndex[1] + 1 : 0;
                    --playerHistoryCounter[1];

                    if (playerHistoryCounter[1] < 0)
                    {
                        updateLevel = 2;
                        playerHistoryCounter[1] = 4;

                        /* Update third level index */
                        playerHistoryIndex[2] = playerHistoryIndex[2] + 1 < 112 ? playerHistoryIndex[2] + 1 : 0;
                        --playerHistoryCounter[2];

                        if (playerHistoryCounter[2] < 0)
                        {
                            updateLevel = 3;
                            playerHistoryCounter[2] = 4;

                            /* Update fourth level index */
                            playerHistoryIndex[3] = playerHistoryIndex[3] + 1 < 112 ? playerHistoryIndex[3] + 1 : 0;
                        }
                    }
                }

                Dictionary<uint, uint> values = new Values();

                /* Store land area stats in history. */
                foreach (Player player in players)
                {
                    values[player.Index] = player.GetLandArea();
                }

                RecordPlayerHistory(updateLevel, 1, playerHistoryIndex, values);

                int clearWinner = CalculateClearWinner(values);

                if (clearWinner != -1)
                    playerScoreLeader |= Misc.Bit(clearWinner);

                /* Store building stats in history. */
                foreach (Player player in players)
                {
                    values[player.Index] = player.GetBuildingScore();
                }

                RecordPlayerHistory(updateLevel, 2, playerHistoryIndex, values);

                /* Store military stats in history. */
                foreach (Player player in players)
                {
                    values[player.Index] = player.GetMilitaryScore();
                }

                RecordPlayerHistory(updateLevel, 3, playerHistoryIndex, values);

                clearWinner = CalculateClearWinner(values);

                if (clearWinner != -1)
                    playerScoreLeader |= Misc.Bit(clearWinner) << 4;

                /* Store condensed score of all aspects in history. */
                foreach (Player player in players)
                {
                    values[player.Index] = player.GetScore();
                }

                RecordPlayerHistory(updateLevel, 0, playerHistoryIndex, values);

                /* TODO Determine winner based on game.player_score_leader */
            }

            if ((int)historyCounter > tickDiff)
            {
                historyCounter -= (uint)tickDiff;
            }
            else
            {
                historyCounter += (uint)(6000 - tickDiff);

                int index = resourceHistoryIndex;

                for (int res = 0; res < 26; ++res)
                {
                    foreach (Player player in players)
                    {
                        player.UpdateStats(res);
                    }
                }

                resourceHistoryIndex = index + 1 < 120 ? index + 1 : 0;
            }
        }

        /* Generate an estimate of the amount of resources in the ground at map pos.*/
        protected void GetResourceEstimate(MapPos pos, uint weight, uint[] estimates)
        {
            if ((map.GetObject(pos) == Map.Object.None ||
                map.GetObject(pos) >= Map.Object.Tree0) &&
                map.GetResourceType(pos) != Map.Minerals.None)
            {
                uint value = weight * map.GetResourceAmount(pos);
                estimates[(int)map.GetResourceType(pos)] += value;
            }
        }

        protected bool RoadSegmentInWater(MapPos pos, Direction dir)
        {
            if (dir > Direction.Down)
            {
                pos = map.Move(pos, dir);
                dir = dir.Reverse();
            }

            bool water = false;

            switch (dir)
            {
                case Direction.Right:
                    if (map.TypeDown(pos) <= Map.Terrain.Water3 &&
                        map.TypeUp(map.MoveUp(pos)) <= Map.Terrain.Water3)
                    {
                        water = true;
                    }
                    break;
                case Direction.DownRight:
                    if (map.TypeUp(pos) <= Map.Terrain.Water3 &&
                        map.TypeDown(pos) <= Map.Terrain.Water3)
                    {
                        water = true;
                    }
                    break;
                case Direction.Down:
                    if (map.TypeUp(pos) <= Map.Terrain.Water3 &&
                        map.TypeDown(map.MoveLeft(pos)) <= Map.Terrain.Water3)
                    {
                        water = true;
                    }
                    break;
                default:
                    Debug.NotReached();
                    break;
            }

            return water;
        }

        protected void FlagResetTransport(Flag flag)
        {
            /* Clear destination for any serf with resources for this flag. */
            foreach (Serf serf in serfs)
            {
                serf.ResetTransport(flag);
            }

            /* Flag. */
            foreach (Flag otherFlag in flags)
            {
                flag.ResetTransport(otherFlag);
            }

            /* Inventories. */
            foreach (Inventory inventory in inventories)
            {
                inventory.ResetQueueForDest(flag);
            }
        }

        protected void BuildingRemovePlayerRefs(Building building)
        {
            foreach (Player player in players)
            {
                if (player.tempIndex == building.Index)
                {
                    player.tempIndex = 0;
                }
            }
        }

        protected bool PathSerfIdleToWaitState(MapPos pos)
        {
            /* Look through serf array for the corresponding serf. */
            foreach (Serf serf in serfs)
            {
                if (serf.IdleToWaitState(pos))
                {
                    return true;
                }
            }

            return false;
        }

        protected void RemoveRoadForwards(MapPos pos, Direction dir)
        {
            Direction inDir = Direction.None;

            while (true)
            {
                if (map.GetIdleSerf(pos))
                {
                    PathSerfIdleToWaitState(pos);
                }

                if (map.HasSerf(pos))
                {
                    Serf serf = GetSerfAtPos(pos);

                    if (!map.HasFlag(pos))
                    {
                        serf.SetLostState();
                    }
                    else
                    {
                        /* Handle serf close to flag, where
                           it should only be lost if walking
                           in the wrong direction. */
                        int d = serf.GetWalkingDir();

                        if (d < 0)
                            d += 6;

                        if (d == (int)dir.Reverse())
                        {
                            serf.SetLostState();
                        }
                    }
                }

                if (map.HasFlag(pos))
                {
                    Flag flag = flags[map.GetObjectIndex(pos)];
                    flag.DeletePath(inDir.Reverse());
                    break;
                }

                inDir = dir;
                dir = map.RemoveRoadSegment(ref pos, dir);
            }
        }

        protected bool DemolishRoad(MapPos pos)
        {
            /* TODO necessary?
               game.player[0]->flags |= BIT(4);
               game.player[1]->flags |= BIT(4);
            */

            if (!map.RemoveRoadBackrefs(pos))
            {
                /* TODO */
                return false;
            }

            /* Find directions of path segments to be split. */
            Direction path1Dir = Direction.None;
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction d in cycle)
            {
                if (map.HasPath(pos, d))
                {
                    path1Dir = d;
                    break;
                }
            }

            Direction path2Dir = Direction.None;

            for (int d = (int)path1Dir + 1; d <= (int)Direction.Up; ++d)
            {
                if (map.HasPath(pos, (Direction)d))
                {
                    path2Dir = (Direction)d;
                    break;
                }
            }

            /* If last segment direction is UP LEFT it could
               be to a building and the real path is at UP. */
            if (path2Dir == Direction.UpLeft && map.HasPath(pos, Direction.Up))
            {
                path2Dir = Direction.Up;
            }

            RemoveRoadForwards(pos, path1Dir);
            RemoveRoadForwards(pos, path2Dir);

            return true;
        }

        /* Build flag on existing path. Path must be split in two segments. */
        protected void BuildFlagSplitPath(MapPos pos)
        {
            /* Find directions of path segments to be split. */
            Direction path1Dir = Direction.None;
            var cycle = DirectionCycleCW.CreateDefault();
            var it = cycle.Begin() as Iterator<Direction>;

            for (; it != cycle.End(); ++it)
            {
                if (map.HasPath(pos, it.Current))
                {
                    path1Dir = it.Current;
                    break;
                }
            }

            Direction path2Dir = Direction.None;
            ++it;

            for (; it != cycle.End(); ++it)
            {
                if (map.HasPath(pos, it.Current))
                {
                    path2Dir = it.Current;
                    break;
                }
            }

            /* If last segment direction is UP LEFT it could
               be to a building and the real path is at UP. */
            if (path2Dir == Direction.UpLeft && map.HasPath(pos, Direction.Up))
            {
                path2Dir = Direction.Up;
            }

            SerfPathInfo path1Data = new SerfPathInfo();
            SerfPathInfo path2Data = new SerfPathInfo();

            Flag.FillPathSerfInfo(this, pos, path1Dir, path1Data);
            Flag.FillPathSerfInfo(this, pos, path2Dir, path2Data);

            Flag flag2 = flags[(uint)path2Data.FlagIndex];
            Direction dir2 = path2Data.FlagDir;

            int select = -1;

            if (flag2.SerfRequested(dir2))
            {
                foreach (Serf serf in serfs)
                {
                    if (serf.PathSplited((uint)path1Data.FlagIndex, path1Data.FlagDir,
                                         (uint)path2Data.FlagIndex, path2Data.FlagDir,
                                         ref select))
                    {
                        break;
                    }
                }

                SerfPathInfo pathData = (select == 0) ? path2Data : path1Data;
                Flag selectedFlag = flags[(uint)pathData.FlagIndex];
                selectedFlag.CancelSerfRequest(pathData.FlagDir);
            }

            Flag flag = flags[map.GetObjectIndex(pos)];

            flag.RestorePathSerfInfo(path1Dir, path1Data);
            flag.RestorePathSerfInfo(path2Dir, path2Data);
        }

        protected bool MapTypesWithin(MapPos pos, Map.Terrain low, Map.Terrain high)
        {

        }

        protected void FlagRemovePlayerRefs(Flag flag)
        {

        }

        protected bool DemolishFlag_(MapPos pos)
        {

        }

        protected bool DemolishBuilding_(MapPos pos)
        {

        }

        protected void SurrenderLand(MapPos pos)
        {

        }

        protected void DemolishFlagAndRoads(MapPos pos)
        {

        }

        #endregion


        public void ReadFrom(SaveReaderBinary reader)
        {

        }

        public void ReadFrom(SaveReaderText reader)
        {

        }

        public void WriteTo(SaveWriterText writer)
        {

        }

        protected bool LoadSerfs(SaveReaderBinary reader, int maxSerfIndex)
        {

        }

        protected bool LoadFlags(SaveReaderBinary reader, int maxFlagIndex)
        {

        }

        protected bool LoadBuildings(SaveReaderBinary reader, int maxBuildingIndex)
        {

        }

        protected bool LoadInventories(SaveReaderBinary reader, int maxInventoryIndex)
        {

        }
    }
}
