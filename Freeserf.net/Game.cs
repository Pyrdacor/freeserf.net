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

        }

        public void Update()
        {

        }

        public void Pause()
        {

        }

        public void IncreaseSpeed()
        {

        }

        public void DecreaseSpeed()
        {

        }

        public void ResetSpeed()
        {

        }

        // estimates is int[5]
        public void PrepareGroundAnalysis(MapPos pos, int[] estimates)
        {

        }

        public bool SendGeologist(Flag dest)
        {

        }

        public int GetLevelingHeight(MapPos pos)
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

        public int CanBuildRoad(Road road, Player player, MapPos dest, bool water)
        {

        }

        public bool CanDemolishFlag(MapPos pos, Player player)
        {

        }

        public bool CanDemolishRoad(MapPos pos, Player player)
        {

        }

        public bool BuildRoad(Road road, Player player)
        {

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

        public bool DemolishRoad(MapPos pos, Player player)
        {

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

        void UpdateLandOwnership(MapPos pos)
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

        bool SendSerfToFlag(Flag dest, Serf.Type type, Resource.Type res1, Resource.Type res2)
        {

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

        Serf CreateSerf(int index = -1)
        {

        }

        internal void DeleteSerf(Serf serf)
        {

        }

        internal Flag CreateFlag(int index = -1)
        {

        }

        Inventory CreateInventory(int index = -1)
        {

        }

        void DeleteInventory(Inventory inventory)
        {

        }

        internal Building CreateBuilding(int index = -1)
        {

        }

        void DeleteBuilding(Building building)
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

        void BuildingCaptured(Building building)
        {

        }

        void ClearSearchId()
        {

        }

        protected void ClearSerfRequestFailure()
        {

        }

        protected void UpdateKnightMorale()
        {

        }

        protected static bool UpdateInventoriesCb(Flag flag, byte[] data)
        {

        }

        protected void UpdateInventories()
        {

        }

        protected void UpdateFlags()
        {

        }

        protected static bool SendSerfToFlagSearchCb(Flag flag, byte[] data)
        {

        }

        protected void UpdateBuildings()
        {

        }

        protected void UpdateSerfs()
        {

        }

        protected void RecordPlayerHistory(int maxLevel, int aspect, int historyIndex[], Values values)
        {

        }

        protected int CalculateClearWinner(Values values)
        {

        }

        protected void UpdateGameStats()
        {

        }

        // estimates = int[5]
        protected void GetResourceEstimate(MapPos pos, int weight, int[] estimates)
        {

        }

        protected bool RoadSegmentInWater(MapPos pos, Direction dir)
        {

        }

        protected void FlagResetTransport(Flag flag)
        {

        }

        protected void BuildingRemovePlayerRefs(Building building)
        {

        }

        protected bool PathSerfIdleToWaitState(MapPos pos)
        {

        }

        protected void RemoveRoadForwards(MapPos pos, Direction dir)
        {

        }

        protected bool DemolishRoad_(MapPos pos)
        {

        }

        protected void BuildFlagSplitPath(MapPos pos)
        {

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
