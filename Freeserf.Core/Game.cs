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

    public class Game : Map.Handler
    {
        public const int DEFAULT_GAME_SPEED = 2;
        public const int GAME_MAX_PLAYER_COUNT = 4;

        Map map;

        uint mapGoldMoraleFactor;
        uint goldTotal;

        Players players;
        Flags flags;
        Inventories inventories;
        Buildings buildings;
        Serfs serfs;

        // Rendering
        Render.IRenderView renderView = null;
        readonly Dictionary<Serf, Render.RenderSerf> renderSerfs = new Dictionary<Serf, Render.RenderSerf>();
        readonly Dictionary<Building, Render.RenderBuilding> renderBuildings = new Dictionary<Building, Render.RenderBuilding>();
        readonly Dictionary<Flag, Render.RenderFlag> renderFlags = new Dictionary<Flag, Render.RenderFlag>();
        readonly Dictionary<MapPos, Render.RenderMapObject> renderObjects = new Dictionary<MapPos, Render.RenderMapObject>();
        readonly Dictionary<long, Render.RenderRoadSegment> renderRoadSegments = new Dictionary<long, Render.RenderRoadSegment>();
        readonly Dictionary<long, Render.RenderBorderSegment> renderBorderSegments = new Dictionary<long, Render.RenderBorderSegment>();
        readonly List<Render.RenderBuilding> renderBuildingsInProgress = new List<Render.RenderBuilding>();

        uint gameSpeedSave;
        uint gameSpeed;
        ushort tick;
        uint lastTick;
        uint constTick;
        uint gameStatsCounter;
        uint historyCounter;
        Random random;
        ushort flagSearchCounter;

        ushort updateMapLastTick;
        short updateMapCounter;
        MapPos updateMapInitialPos;
        int tickDiff;
        short updateMap16Loop;
        int[] playerHistoryIndex = new int[4];
        int[] playerHistoryCounter = new int[3];
        int resourceHistoryIndex;
        ushort field340;
        ushort field342;
        Inventory field344;
        int gameType;
        int tutorialLevel;
        int missionLevel;
        int mapPreserveBugs;
        int playerScoreLeader;

        int knightMoraleCounter;
        int inventoryScheduleCounter;

        public Map Map => map;
        public ushort Tick => tick;
        public uint ConstTick => constTick;
        public uint MapGoldMoraleFactor => mapGoldMoraleFactor;
        public uint GoldTotal => goldTotal;

        public Game(Render.IRenderView renderView)
        {
            AI.ClearMemory();

            this.renderView = renderView;

            random = new Random();

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

            resourceHistoryIndex = 0;

            tick = 0;
            constTick = 0;
            tickDiff = 0;

            gameType = 0;
            flagSearchCounter = 0;
            gameStatsCounter = 0;
            historyCounter = 0;

            knightMoraleCounter = 0;
            inventoryScheduleCounter = 0;

            goldTotal = 0;
        }

        public void Close()
        {
            // delete all render objects

            foreach (var building in renderBuildings)
                building.Value.Delete();

            foreach (var building in renderBuildingsInProgress)
                building.Delete();

            foreach (var serf in renderSerfs)
                serf.Value.Delete();

            foreach (var flag in renderFlags)
                flag.Value.Delete();

            foreach (var mapObject in renderObjects)
                mapObject.Value.Delete();

            foreach (var roadSegment in renderRoadSegments)
                roadSegment.Value.Delete();

            foreach (var borderSegment in renderBorderSegments)
                borderSegment.Value.Delete();

            renderBuildings.Clear();
            renderBuildingsInProgress.Clear();
            renderSerfs.Clear();
            renderFlags.Clear();
            renderObjects.Clear();
            renderRoadSegments.Clear();
            renderBorderSegments.Clear();

            // close map (and delete render map)
            map.Close();
        }

        public void AddGoldTotal(int delta)
        {
            if (delta < 0)
            {
                if ((int)goldTotal < -delta)
                {
                    throw new ExceptionFreeserf("Failed to decrease global gold counter.");
                }
            }

            goldTotal = (uint)((int)goldTotal + delta);
        }

        public Building GetBuildingAtPos(MapPos pos)
        {
            Map.Object mapObject = map.GetObject(pos);

            if (mapObject >= Map.Object.SmallBuilding && mapObject <= Map.Object.Castle)
            {
                return buildings[map.GetObjectIndex(pos)];
            }

            return null;
        }

        public Flag GetFlagAtPos(MapPos pos)
        {
            if (map.GetObject(pos) != Map.Object.Flag)
            {
                return null;
            }

            return flags[map.GetObjectIndex(pos)];
        }

        public Serf GetSerfAtPos(MapPos pos)
        {
            return serfs[map.GetSerfIndex(pos)];
        }


        #region External interface

        // Add new player to the game. Returns the player number.
        public uint AddPlayer(uint intelligence, uint supplies, uint reproduction)
        {
            /* Allocate object */
            Player player = players.Allocate();

            if (player == null)
            {
                throw new ExceptionFreeserf("Failed to create new player.");
            }

            player.Init(intelligence, supplies, reproduction);

            /* Update map values dependent on player count */
            mapGoldMoraleFactor = 10u * 1024u * (uint)players.Size;

            return player.Index;
        }

        public bool Init(uint mapSize, Random random)
        {
            map = new Map(new MapGeometry(mapSize), renderView);
            var generator = new ClassicMissionMapGenerator(map, random);

            generator.Init();
            generator.Generate();

            map.AddChangeHandler(this);
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

            if (lastTick > tick) // ushort overflow
            {
                tickDiff = (int)tick + (int)ushort.MaxValue - (int)lastTick;
            }
            else
            {
                tickDiff = (int)(tick - lastTick);
            }

            ClearSerfRequestFailure();
            map.Update(tick, random);

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

            /* AI related updates */
            foreach (var player in players)
            {
                if (player.IsAi())
                {
                    if (player.AI == null)
                        throw new ExceptionFreeserf("AI is not set for AI player.");

                    player.AI.Update(this);
                }
            }

            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (map?.RenderMap != null)
                map.RenderMap.UpdateWaves(tick);

            UpdateRoads();
            UpdateBorders();
            UpdateMapObjects();
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
            if (gameSpeed < 40) // TODO: adjust later maybe
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

        /* Return the height that is needed before a large building can be built.
           Returns negative if the needed height cannot be reached. */
        public int GetLevelingHeight(MapPos pos)
        {
            /* Find min and max height */
            uint hMin = 31;
            uint hMax = 0;

            for (uint i = 0; i < 12; ++i)
            {
                MapPos p = map.PosAddSpirally(pos, 7u + i);
                uint h = map.GetHeight(p);

                if (hMin > h)
                    hMin = h;
                if (hMax < h)
                    hMax = h;
            }

            /* Adjust for height of adjacent unleveled buildings */
            for (uint i = 0; i < 18; ++i)
            {
                MapPos p = map.PosAddSpirally(pos, 19u + i);

                if (map.GetObject(p) == Map.Object.LargeBuilding)
                {
                    Building building = buildings[map.GetObjectIndex(p)];

                    if (building.IsLeveling())
                    { 
                        /* Leveling in progress */
                        uint h = building.GetLevel();

                        if (hMin > h)
                            hMin = h;
                        if (hMax < h)
                            hMax = h;
                    }
                }
            }

            /* Return if height difference is too big */
            if (hMax - hMin >= 9)
                return -1;

            /* Calculate "mean" height. Height of center is added twice. */
            uint hMean = map.GetHeight(pos);

            for (uint i = 0; i < 7; ++i)
            {
                MapPos p = map.PosAddSpirally(pos, i);

                hMean += map.GetHeight(p);
            }

            hMean >>= 3;

            /* Calcualte height after leveling */
            uint hNewMin = Math.Max((hMax > 4) ? (hMax - 4) : 1, 1);
            uint hNewmax = hMin + 4;
            uint hNew = Misc.Clamp(hNewMin, hMean, hNewmax);

            return (int)hNew;
        }

        /* Check whether military buildings are allowed at pos. */
        public bool CanBuildMilitary(MapPos pos)
        {
            /* Check that no military buildings are nearby */
            for (uint i = 0; i < 1 + 6 + 12; ++i)
            {
                MapPos p = map.PosAddSpirally(pos, i);

                if (map.GetObject(p) >= Map.Object.SmallBuilding &&
                    map.GetObject(p) <= Map.Object.Castle)
                {
                    Building building = buildings[map.GetObjectIndex(p)];

                    if (building.IsMilitary())
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /* Checks whether a small building is possible at position.*/
        public bool CanBuildSmall(MapPos pos)
        {
            return MapTypesWithin(pos, Map.Terrain.Grass0, Map.Terrain.Grass3);
        }

        /* Checks whether a mine is possible at position. */
        public bool CanBuildMine(MapPos pos)
        {
            bool canBuild = false;

            Map.Terrain[] types = new Map.Terrain[]
            {
                map.TypeDown(pos),
                map.TypeUp(pos),
                map.TypeDown(map.MoveLeft(pos)),
                map.TypeUp(map.MoveUpLeft(pos)),
                map.TypeDown(map.MoveUpLeft(pos)),
                map.TypeUp(map.MoveUp(pos))
            };

            for (int i = 0; i < 6; ++i)
            {
                if (types[i] >= Map.Terrain.Tundra0 && types[i] <= Map.Terrain.Snow0)
                {
                    canBuild = true;
                }
                else if (!(types[i] >= Map.Terrain.Grass0 &&
                           types[i] <= Map.Terrain.Grass3))
                {
                    return false;
                }
            }

            return canBuild;
        }

        /* Checks whether a large building is possible at position. */
        public bool CanBuildLarge(MapPos pos)
        {
            /* Check that surroundings are passable by serfs. */
            for (uint i = 0; i < 6; ++i)
            {
                MapPos p = map.PosAddSpirally(pos, 1 + i);
                Map.Space s = Map.MapSpaceFromObject[(int)map.GetObject(p)];

                if (s >= Map.Space.Semipassable)
                    return false;
            }

            /* Check that buildings in the second shell aren't large or castle. */
            for (uint i = 0; i < 12; ++i)
            {
                MapPos p = map.PosAddSpirally(pos, 7u + i);
                if (map.GetObject(p) >= Map.Object.LargeBuilding &&
                    map.GetObject(p) <= Map.Object.Castle)
                {
                    return false;
                }
            }

            /* Check if center hexagon is not type grass. */
            if (map.TypeUp(pos) != Map.Terrain.Grass1 ||
                map.TypeDown(pos) != Map.Terrain.Grass1 ||
                map.TypeDown(map.MoveLeft(pos)) != Map.Terrain.Grass1 ||
                map.TypeUp(map.MoveUpLeft(pos)) != Map.Terrain.Grass1 ||
                map.TypeDown(map.MoveUpLeft(pos)) != Map.Terrain.Grass1 ||
                map.TypeUp(map.MoveUp(pos)) != Map.Terrain.Grass1)
            {
                return false;
            }

            /* Check that leveling is possible */
            if (GetLevelingHeight(pos) < 0)
                return false;

            return true;
        }

        /* Checks whether a building of the specified type is possible at
           position. */
        public bool CanBuildBuilding(MapPos pos, Building.Type type, Player player)
        {
            if (!CanPlayerBuild(pos, player))
                return false;

            /* Check that space is clear */
            if (Map.MapSpaceFromObject[(int)map.GetObject(pos)] != Map.Space.Open)
            {
                return false;
            }

            /* Check that building flag is possible if it
               doesn't already exist. */
            MapPos flagPos = map.MoveDownRight(pos);

            if (!map.HasFlag(flagPos) && !CanBuildFlag(flagPos, player))
            {
                return false;
            }

            /* Check if building size is possible. */
            switch (type)
            {
                case Building.Type.Fisher:
                case Building.Type.Lumberjack:
                case Building.Type.Boatbuilder:
                case Building.Type.Stonecutter:
                case Building.Type.Forester:
                case Building.Type.Hut:
                case Building.Type.Mill:
                    if (!CanBuildSmall(pos))
                        return false;
                    break;
                case Building.Type.StoneMine:
                case Building.Type.CoalMine:
                case Building.Type.IronMine:
                case Building.Type.GoldMine:
                    if (!CanBuildMine(pos))
                        return false;
                    break;
                case Building.Type.Stock:
                case Building.Type.Farm:
                case Building.Type.Butcher:
                case Building.Type.PigFarm:
                case Building.Type.Baker:
                case Building.Type.Sawmill:
                case Building.Type.SteelSmelter:
                case Building.Type.ToolMaker:
                case Building.Type.WeaponSmith:
                case Building.Type.Tower:
                case Building.Type.Fortress:
                case Building.Type.GoldSmelter:
                    if (!CanBuildLarge(pos))
                        return false;
                    break;
                default:
                    Debug.NotReached();
                    break;
            }

            /* Check if military building is possible */
            if ((type == Building.Type.Hut ||
                 type == Building.Type.Tower ||
                 type == Building.Type.Fortress) &&
                !CanBuildMilitary(pos))
            {
                return false;
            }

            return true;
        }

        /* Checks whether a castle can be built by player at position. */
        public bool CanBuildCastle(MapPos pos, Player player)
        {
            if (player.HasCastle())
                return false;

            /* Check owner of land around position */
            for (uint i = 0; i < 7; ++i)
            {
                MapPos p = map.PosAddSpirally(pos, i);

                if (map.HasOwner(p))
                    return false;
            }

            /* Check that land is clear at position */
            if (Map.MapSpaceFromObject[(int)map.GetObject(pos)] != Map.Space.Open ||
                map.Paths(pos) != 0)
            {
                return false;
            }

            MapPos flagPos = map.MoveDownRight(pos);

            /* Check that land is clear at position */
            if (Map.MapSpaceFromObject[(int)map.GetObject(flagPos)] != Map.Space.Open ||
                map.Paths(flagPos) != 0)
            {
                return false;
            }

            return CanBuildLarge(pos);
        }

        public bool CanBuildFlag(MapPos pos, Player player)
        {
            /* Check owner of land */
            if (!map.HasOwner(pos) || map.GetOwner(pos) != player.Index)
            {
                return false;
            }

            /* Check that land is clear */
            if (Map.MapSpaceFromObject[(int)map.GetObject(pos)] != Map.Space.Open)
            {
                return false;
            }

            /* Check whether cursor is in water */
            if (map.TypeUp(pos) <= Map.Terrain.Water3 &&
                map.TypeDown(pos) <= Map.Terrain.Water3 &&
                map.TypeDown(map.MoveLeft(pos)) <= Map.Terrain.Water3 &&
                map.TypeUp(map.MoveUpLeft(pos)) <= Map.Terrain.Water3 &&
                map.TypeDown(map.MoveUpLeft(pos)) <= Map.Terrain.Water3 &&
                map.TypeUp(map.MoveUp(pos)) <= Map.Terrain.Water3)
            {
                return false;
            }

            /* Check that no flags are nearby */
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction d in cycle)
            {
                if (map.GetObject(map.Move(pos, d)) == Map.Object.Flag)
                {
                    return false;
                }
            }

            return true;
        }

        /* Check whether player is allowed to build anything
           at position. To determine if the initial castle can
           be built use can_build_castle() instead.

           TODO Existing buildings at position should be
           disregarded so this can be used to determine what
           can be built after the existing building has been
           demolished. */
        public bool CanPlayerBuild(MapPos pos, Player player)
        {
            if (!player.HasCastle())
                return false;

            /* Check owner of land around position */
            for (uint i = 0; i < 7; ++i)
            {
                MapPos p = map.PosAddSpirally(pos, i);

                if (!map.HasOwner(p) || map.GetOwner(p) != player.Index)
                {
                    return false;
                }
            }

            /* Check whether cursor is in water */
            if (map.TypeUp(pos) <= Map.Terrain.Water3 &&
                map.TypeDown(pos) <= Map.Terrain.Water3 &&
                map.TypeDown(map.MoveLeft(pos)) <= Map.Terrain.Water3 &&
                map.TypeUp(map.MoveUpLeft(pos)) <= Map.Terrain.Water3 &&
                map.TypeDown(map.MoveUpLeft(pos)) <= Map.Terrain.Water3 &&
                map.TypeUp(map.MoveUp(pos)) <= Map.Terrain.Water3)
            {
                return false;
            }

            /* Check that no paths are blocking. */
            if (map.Paths(pos) != 0)
                return false;

            // TODO: What about desert and snow? Is this handled somewhere else?

            return true;
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

            var dirs = road.Dirs.Reverse();
            int dirCount = road.Dirs.Count;
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
                    (map.HasFlag(pos) && i != dirCount))
                {
                    return 0;
                }
            }

            dest = pos;

            /* Bit 0 indicates a ground path, bit 1 indicates
               a water path. Abort if path went through both
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

        /* Check whether flag can be demolished. */
        public bool CanDemolishFlag(MapPos pos, Player player)
        {
            if (map.GetObject(pos) != Map.Object.Flag)
                return false;

            Flag flag = flags[map.GetObjectIndex(pos)];

            if (flag.HasBuilding())
            {
                return false;
            }

            if (map.Paths(pos) == 0)
                return true;

            if (flag.GetOwner() != player.Index)
                return false;

            return flag.CanDemolish();
        }

        /* Check whether road can be demolished. */
        public bool CanDemolishRoad(MapPos pos, Player player)
        {
            if (!map.HasOwner(pos) || map.GetOwner(pos) != player.Index)
            {
                return false;
            }

            if (map.Paths(pos) == 0 ||
                map.HasFlag(pos) ||
                map.HasBuilding(pos))
            {
                return false;
            }

            return true;
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
            Direction outDir = dirs.Last();
            Direction inDir = dirs.Peek().Reverse();

            /* Actually place road segments */
            if (!map.PlaceRoadSegments(road))
                return false;

            /* Connect flags */
            Flag sourceFlag = GetFlagAtPos(road.Source);
            Flag destFlag = GetFlagAtPos(dest);

            sourceFlag.LinkWithFlag(destFlag, waterPath, road.Length, inDir, outDir);

            return true;
        }

        /* Build flag at pos. */
        public bool BuildFlag(MapPos pos, Player player)
        {
            if (!CanBuildFlag(pos, player))
            {
                return false;
            }

            Flag flag = flags.Allocate();

            if (flag == null)
                return false;

            flag.SetOwner(player.Index);
            flag.Position = pos;
            map.SetObject(pos, Map.Object.Flag, (int)flag.Index);

            if (map.Paths(pos) != 0)
            {
                BuildFlagSplitPath(pos);
            }

            return true;
        }

        /* Build building at position. */
        public bool BuildBuilding(MapPos pos, Building.Type type, Player player)
        {
            if (!CanBuildBuilding(pos, type, player))
            {
                return false;
            }

            if (type == Building.Type.Stock)
            {
                /* TODO Check that more stocks are allowed to be built */
            }

            Building building = buildings.Allocate();

            if (building == null)
            {
                return false;
            }

            var flagPos = map.MoveDownRight(pos);
            Flag flag = GetFlagAtPos(flagPos);

            if (flag == null)
            {
                if (!BuildFlag(flagPos, player))
                {
                    buildings.Erase(building.Index);
                    return false;
                }

                flag = GetFlagAtPos(flagPos);
            }

            uint flagIndex = flag.Index;

            building.SetLevel((uint)GetLevelingHeight(pos));
            building.Position = pos;

            Map.Object mapObject = building.StartBuilding(type);
            player.BuildingFounded(building);

            bool splitPath = false;

            if (map.GetObject(flagPos) != Map.Object.Flag)
            {
                flag.SetOwner(player.Index);
                splitPath = map.Paths(flagPos) != 0;
            }
            else
            {
                flagIndex = map.GetObjectIndex(flagPos);
                flag = flags[flagIndex];
            }

            flag.Position = flagPos;
            building.LinkFlag(flagIndex);
            flag.LinkBuilding(building);

            flag.ClearFlags();

            map.ClearIdleSerf(pos);

            map.SetObject(pos, mapObject, (int)building.Index);
            map.AddPath(pos, Direction.DownRight);

            if (map.GetObject(flagPos) != Map.Object.Flag)
            {
                map.SetObject(flagPos, Map.Object.Flag, (int)flagIndex);
            }

            map.AddPath(flagPos, Direction.UpLeft);

            if (splitPath)
                BuildFlagSplitPath(flagPos);

            return true;
        }

        /* Build castle at position. */
        public bool BuildCastle(MapPos pos, Player player)
        {
            if (!CanBuildCastle(pos, player))
            {
                return false;
            }

            Inventory inventory = inventories.Allocate();

            if (inventory == null)
            {
                return false;
            }

            Building castle = buildings.Allocate();

            if (castle == null)
            {
                inventories.Erase(inventory.Index);
                return false;
            }

            Flag flag = flags.Allocate();

            if (flag == null)
            {
                buildings.Erase(castle.Index);
                inventories.Erase(inventory.Index);
                return false;
            }

            castle.SetInventory(inventory);

            inventory.SetBuildingIndex(castle.Index);
            inventory.SetFlagIndex(flag.Index);
            inventory.Player = player.Index;
            inventory.ApplySuppliesPreset(player.GetInitialSupplies());

            AddGoldTotal((int)inventory.GetCountOf(Resource.Type.GoldBar));
            AddGoldTotal((int)inventory.GetCountOf(Resource.Type.GoldOre));

            castle.Position = pos;
            flag.Position = map.MoveDownRight(pos);
            castle.Player = player.Index;
            castle.StartBuilding(Building.Type.Castle);

            flag.SetOwner(player.Index);
            flag.SetAcceptsSerfs(true);
            flag.SetHasInventory();
            flag.SetAcceptsResources(true);
            castle.LinkFlag(flag.Index);
            flag.LinkBuilding(castle);

            /* Level land in hexagon below castle */
            uint h = (uint)GetLevelingHeight(pos);
            map.SetHeight(pos, h);

            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction d in cycle)
            {
                map.SetHeight(map.Move(pos, d), h);
            }

            map.SetObject(pos, Map.Object.Castle, (int)castle.Index);
            map.AddPath(pos, Direction.DownRight);

            map.SetObject(map.MoveDownRight(pos), Map.Object.Flag, (int)flag.Index);
            map.AddPath(map.MoveDownRight(pos), Direction.UpLeft);

            UpdateLandOwnership(pos);

            player.BuildingFounded(castle);

            castle.UpdateMilitaryFlagState();

            player.CastlePos = pos;

            return true;
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
            if (!CanDemolishFlag(pos, player))
                return false;

            return DemolishFlag(pos);
        }

        /* Demolish building at pos. */
        public bool DemolishBuilding(MapPos pos, Player player)
        {
            Building building = buildings[map.GetObjectIndex(pos)];

            if (building.Player != player.Index)
                return false;

            if (building.IsBurning())
                return false;

            return DemolishBuilding(pos);
        }

        public void SetInventoryResourceMode(Inventory inventory, Inventory.Mode mode)
        {
            Flag flag = flags[inventory.GetFlagIndex()];

            inventory.SetResourceMode(mode);

            if (mode > 0)
            {
                flag.SetAcceptsResources(false);

                /* Clear destination of serfs with resources destined
                   for this inventory. */
                uint dest = flag.Index;

                foreach (Serf serf in serfs)
                {
                    serf.ClearDestination2(dest);
                }
            }
            else
            {
                flag.SetAcceptsResources(true);
            }
        }

        public void SetInventorySerfMode(Inventory inventory, Inventory.Mode mode)
        {
            Flag flag = flags[inventory.GetFlagIndex()];

            inventory.SetSerfMode(mode);

            if (mode > 0)
            {
                flag.SetAcceptsSerfs(false);

                /* Clear destination of serfs destined for this inventory. */
                uint dest = flag.Index;

                foreach (Serf serf in serfs)
                {
                    serf.ClearDestination(dest);
                }
            }
            else
            {
                flag.SetAcceptsSerfs(true);
            }
        }

        #endregion


        #region Internal interface

        /* Initialize land ownership for whole map. */
        void InitLandOwnership()
        {
            foreach (Building building in buildings)
            {
                if (building.IsMilitary())
                {
                    UpdateLandOwnership(building.Position);
                }
            }

            UpdateBorders();
        }

        static readonly int[] militaryInfluence = new int[]
        {
            0, 1, 2, 4, 7, 12, 18, 29, -1, -1,      /* hut */
            0, 3, 5, 8, 11, 15, 22, 30, -1, -1,     /* tower */
            0, 6, 10, 14, 19, 23, 27, 31, -1, -1    /* fortress */
        };

        static readonly int[] mapCloseness = new int []
        {
            1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 2, 2, 2, 2, 2, 2, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0,
            1, 2, 3, 3, 3, 3, 3, 3, 3, 2, 1, 0, 0, 0, 0, 0, 0,
            1, 2, 3, 4, 4, 4, 4, 4, 4, 3, 2, 1, 0, 0, 0, 0, 0,
            1, 2, 3, 4, 5, 5, 5, 5, 5, 4, 3, 2, 1, 0, 0, 0, 0,
            1, 2, 3, 4, 5, 6, 6, 6, 6, 5, 4, 3, 2, 1, 0, 0, 0,
            1, 2, 3, 4, 5, 6, 7, 7, 7, 6, 5, 4, 3, 2, 1, 0, 0,
            1, 2, 3, 4, 5, 6, 7, 8, 8, 7, 6, 5, 4, 3, 2, 1, 0,
            1, 2, 3, 4, 5, 6, 7, 8, 9, 8, 7, 6, 5, 4, 3, 2, 1,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 8, 7, 6, 5, 4, 3, 2, 1,
            0, 0, 1, 2, 3, 4, 5, 6, 7, 7, 7, 6, 5, 4, 3, 2, 1,
            0, 0, 0, 1, 2, 3, 4, 5, 6, 6, 6, 6, 5, 4, 3, 2, 1,
            0, 0, 0, 0, 1, 2, 3, 4, 5, 5, 5, 5, 5, 4, 3, 2, 1,
            0, 0, 0, 0, 0, 1, 2, 3, 4, 4, 4, 4, 4, 4, 3, 2, 1,
            0, 0, 0, 0, 0, 0, 1, 2, 3, 3, 3, 3, 3, 3, 3, 2, 1,
            0, 0, 0, 0, 0, 0, 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 1,
            0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1
        };

        /* Update land ownership around map position. */
        internal void UpdateLandOwnership(MapPos pos)
        {
            /* Currently the below algorithm will only work when
               both influence_radius and calculate_radius are 8. */
            const int influenceRadius = 8;
            const int influenceDiameter = 1 + 2 * influenceRadius;

            int calculateRadius = influenceRadius;
            int calculateDiameter = 1 + 2 * calculateRadius;

            int tempArraySize = calculateDiameter * calculateDiameter * players.Size;
            var tempArray = new int[tempArraySize];

            /* Find influence from buildings in 33*33 square
               around the center. */
            for (int i = -(influenceRadius + calculateRadius);
                 i <= influenceRadius + calculateRadius; ++i)
            {
                for (int j = -(influenceRadius + calculateRadius);
                     j <= influenceRadius + calculateRadius; ++j)
                {
                    MapPos checkPos = map.PosAdd(pos, j, i);

                    if (map.GetObject(checkPos) >= Map.Object.SmallBuilding &&
                        map.GetObject(checkPos) <= Map.Object.Castle &&
                        map.HasPath(checkPos, Direction.DownRight)) // TODO(_): Why wouldn't this be set?
                    { 
                        Building building = GetBuildingAtPos(checkPos);
                        int militaryType = -1;

                        if (building.BuildingType == Building.Type.Castle)
                        {
                            /* Castle has military influence even when not done. */
                            militaryType = 2;
                        }
                        else if (building.IsDone() && building.IsActive())
                        {
                            switch (building.BuildingType)
                            {
                                case Building.Type.Hut: militaryType = 0; break;
                                case Building.Type.Tower: militaryType = 1; break;
                                case Building.Type.Fortress: militaryType = 2; break;
                                default: break;
                            }
                        }

                        if (militaryType >= 0 && !building.IsBurning())
                        {
                            int influenceOffset = 10 * militaryType;
                            int closenessOffset = influenceDiameter * Math.Max(-i, 0) + Math.Max(-j, 0);
                            int arrayIndex = ((int)building.Player * calculateDiameter * calculateDiameter) +
                              calculateDiameter * Math.Max(i, 0) + Math.Max(j, 0);

                            for (int k = 0; k < influenceDiameter - Math.Abs(i); ++k)
                            {
                                for (int l = 0; l < influenceDiameter - Math.Abs(j); ++l)
                                {
                                    int inf = militaryInfluence[influenceOffset + mapCloseness[closenessOffset]];

                                    if (inf < 0)
                                    {
                                        tempArray[arrayIndex] = 128;
                                    }
                                    else if (tempArray[arrayIndex] < 128)
                                    {
                                        tempArray[arrayIndex] = Math.Min(tempArray[arrayIndex] + inf, 127);
                                    }

                                    ++closenessOffset;
                                    ++arrayIndex;
                                }

                                closenessOffset += Math.Abs(j);
                                arrayIndex += Math.Abs(j);
                            }
                        }
                    }
                }
            }

            /* Update owner of 17*17 square. */
            for (int i = -calculateRadius; i <= calculateRadius; ++i)
            {
                for (int j = -calculateRadius; j <= calculateRadius; ++j)
                {
                    int maxValue = 0;
                    int playerIndex = -1;

                    foreach (Player player in players)
                    {
                        int arrayIndex = (int)player.Index * calculateDiameter * calculateDiameter +
                          calculateDiameter * (i + calculateRadius) + (j + calculateRadius);

                        if (tempArray[arrayIndex] > maxValue)
                        {
                            maxValue = tempArray[arrayIndex];
                            playerIndex = (int)player.Index;
                        }
                    }

                    MapPos checkPos = map.PosAdd(pos, j, i);
                    int oldPlayer = -1;

                    if (map.HasOwner(checkPos))
                        oldPlayer = (int)map.GetOwner(checkPos);

                    if (oldPlayer >= 0 && playerIndex != oldPlayer)
                    {
                        players[(uint)oldPlayer].DecreaseLandArea();
                        SurrenderLand(checkPos);
                    }

                    if (playerIndex >= 0)
                    {
                        if (playerIndex != oldPlayer)
                        {
                            players[(uint)playerIndex].IncreaseLandArea();
                            map.SetOwner(checkPos, (uint)playerIndex);
                        }
                    }
                    else
                    {
                        map.DeleteOwner(checkPos);
                    }
                }
            }

            /* Update borders of 18*18 square. */
            for (int i = -calculateRadius - 1; i <= calculateRadius; ++i)
            {
                for (int j = -calculateRadius - 1; j <= calculateRadius; ++j)
                {
                    UpdateBorders(map.PosAdd(pos, j, i));
                }
            }

            /* Update military building flag state. */
            for (int i = -25; i <= 25; ++i)
            {
                for (int j = -25; j <= 25; ++j)
                {
                    MapPos checkPos = map.PosAdd(pos, i, j);

                    if (map.GetObject(checkPos) >= Map.Object.SmallBuilding &&
                        map.GetObject(checkPos) <= Map.Object.Castle &&
                        map.HasPath(checkPos, Direction.DownRight))
                    {
                        Building building = buildings[map.GetObjectIndex(checkPos)];

                        if (building.IsDone() && building.IsMilitary())
                        {
                            building.UpdateMilitaryFlagState();
                        }
                    }
                }
            }
        }

        void UpdateBorders(MapPos pos)
        {
            for (int dir = (int)Direction.Right; dir <= (int)Direction.Down; ++dir)
            {
                long index = Render.RenderBorderSegment.CreateIndex(pos, (Direction)dir);

                if (map.HasOwner(pos) != map.HasOwner(map.Move(pos, (Direction)dir)) ||
                    map.GetOwner(pos) != map.GetOwner(map.Move(pos, (Direction)dir)))
                {
                    if (!renderBorderSegments.ContainsKey(index))
                    {
                        renderBorderSegments.Add(index, new Render.RenderBorderSegment(map, pos, (Direction)dir,
                            renderView.GetLayer(Layer.Objects), renderView.SpriteFactory, renderView.DataSource));
                    }
                }
                else
                {
                    if (renderBorderSegments.ContainsKey(index))
                    {
                        renderBorderSegments[index].Delete();
                        renderBorderSegments.Remove(index);
                    }
                }
            }
        }

        /* The given building has been defeated and is being
           occupied by player. */
        internal void OccupyEnemyBuilding(Building building, uint playerIndex)
        {
            /* Take the building. */
            Player player = players[playerIndex];

            player.BuildingCaptured(building);

            if (building.BuildingType == Building.Type.Castle)
            {
                DemolishBuilding(building.Position);
            }
            else
            {
                Flag flag = flags[building.GetFlagIndex()];
                FlagResetTransport(flag);

                /* Demolish nearby buildings. */
                for (uint i = 0; i < 12; ++i)
                {
                    MapPos pos = map.PosAddSpirally(building.Position, 7u + i);

                    if (map.GetObject(pos) >= Map.Object.SmallBuilding &&
                        map.GetObject(pos) < Map.Object.Castle)
                    {
                        DemolishBuilding(pos);
                    }
                }

                /* Change owner of land and remove roads and flags
                   except the flag associated with the building. */
                map.SetOwner(building.Position, playerIndex);

                var cycle = DirectionCycleCW.CreateDefault();

                foreach (Direction d in cycle)
                {
                    MapPos pos = map.Move(building.Position, d);

                    map.SetOwner(pos, playerIndex);

                    if (pos != flag.Position)
                    {
                        DemolishFlagAndRoads(pos);
                    }
                }

                /* Change owner of flag. */
                flag.SetOwner(playerIndex);

                /* Reset destination of stolen resources. */
                flag.ResetDestinationOfStolenResources();

                /* Remove paths from flag. */
                cycle = DirectionCycleCW.CreateDefault();

                foreach (Direction d in cycle)
                {
                    if (flag.HasPath(d))
                    {
                        DemolishRoad(map.Move(flag.Position, d));
                    }
                }

                UpdateLandOwnership(building.Position);
            }
        }

        /* Cancel a resource being transported to destination. This
           ensures that the destination can request a new resource. */
        internal void CancelTransportedResource(Resource.Type type, uint dest)
        {
            if (dest == 0)
            {
                return;
            }

            Flag flag = flags[dest];

            if (!flag.HasBuilding())
            {
                throw new ExceptionFreeserf("Failed to cancel transported resource.");
            }

            flag.GetBuilding().CancelTransportedResource(type);
        }

        /* Called when a resource is lost forever from the game. This will
           update any global state keeping track of that resource. */
        internal void LoseResource(Resource.Type type)
        {
            if (type == Resource.Type.GoldOre || type == Resource.Type.GoldBar)
            {
                AddGoldTotal(-1);
            }
        }

        internal ushort RandomInt()
        {
            return random.Next();
        }

        internal Random GetRandom()
        {
            return random;
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

        internal int GetPlayerHistoryIndex(uint scale)
        {
            return playerHistoryIndex[scale];
        }

        internal int GetResourceHistoryIndex()
        {
            return resourceHistoryIndex;
        }

        internal int NextSearchId()
        {
            ++flagSearchCounter;

            /* If we're back at zero the counter has overflown,
             everything needs a reset to be safe. */
            if (flagSearchCounter == 0)
            {
                ++flagSearchCounter;
                ClearSearchId();
            }

            return flagSearchCounter;
        }

        internal Serf CreateSerf(int index = -1)
        {
            if (index == -1)
            {
                return serfs.Allocate();
            }
            else
            {
                return serfs.GetOrInsert((uint)index);
            }
        }

        internal void DeleteSerf(Serf serf)
        {
            if (renderSerfs.ContainsKey(serf))
            {
                renderSerfs[serf].Delete();
                renderSerfs.Remove(serf);
            }

            serfs.Erase(serf.Index);
        }

        internal Flag CreateFlag(int index = -1)
        {
            if (index == -1)
            {
                return flags.Allocate();
            }
            else
            {
                return flags.GetOrInsert((uint)index);
            }
        }

        internal Inventory CreateInventory(int index = -1)
        {
            if (index == -1)
            {
                return inventories.Allocate();
            }
            else
            {
                return inventories.GetOrInsert((uint)index);
            }
        }

        internal void DeleteInventory(uint index)
        {
            inventories.Erase(index);
        }

        internal void DeleteInventory(Inventory inventory)
        {
            DeleteInventory(inventory.Index);
        }

        internal Building CreateBuilding(int index = -1)
        {
            if (index == -1)
            {
                return buildings.Allocate();
            }
            else
            {
                return buildings.GetOrInsert((uint)index);
            }
        }

        internal void DeleteBuilding(Building building)
        {
            map.SetObject(building.Position, Map.Object.None, 0);

            renderBuildings[building].Delete();
            renderBuildings.Remove(building);

            buildings.Erase(building.Index);
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

        public int GetPlayerCount()
        {
            return players.Count();
        }

        public int GetFreeKnightCount(Player player)
        {
            return serfs.Count(s => s.Player == player.Index && s.IsKnight() && s.SerfState == Serf.State.IdleInStock) - ((int)player.GetCastleKnightsWanted() - (int)player.GetCastleKnights());
        }

        public int GetPossibleFreeKnightCount(Player player)
        {
            int count = GetFreeKnightCount(player);

            count += Math.Min(GetResourceAmountInInventories(player, Resource.Type.Sword), GetResourceAmountInInventories(player, Resource.Type.Shield));
            count -= (int)player.GetIncompleteBuildingCount(Building.Type.Hut);
            count -= (int)player.GetIncompleteBuildingCount(Building.Type.Tower);
            count -= (int)player.GetIncompleteBuildingCount(Building.Type.Fortress);

            return count;
        }

        public IEnumerable<Serf> GetPlayerSerfs(Player player)
        {
            return serfs.Where(s => s.Player == player.Index);
        }

        public IEnumerable<Building> GetPlayerBuildings(Player player)
        {
            return buildings.Where(b => b.Player == player.Index);
        }

        public IEnumerable<Building> GetPlayerBuildings(Player player, Building.Type type)
        {
            return GetPlayerBuildings(player).Where(b => b.BuildingType == type);
        }

        public IEnumerable<Flag> GetPlayerFlags(Player player)
        {
            return flags.Where(f => f.GetOwner() == player.Index);
        }

        public IEnumerable<Serf> GetSerfsInInventory(Inventory inventory)
        {
            return serfs.Where(s => s.SerfState == Serf.State.IdleInStock && inventory.Index == s.GetIdleInStockInventoryIndex());
        }

        internal List<Serf> GetSerfsRelatedTo(uint dest, Direction dir)
        {
            return serfs.Where(s => s.IsRelatedTo(dest, dir)).ToList();
        }

        public IEnumerable<Inventory> GetPlayerInventories(Player player)
        {
            return inventories.Where(i => i.Player == player.Index);
        }

        public IEnumerable<Serf> GetSerfsAtPos(MapPos pos)
        {
            return serfs.Where(s => s.Position == pos);
        }

        public int FindInventoryWithValidSpecialist(Player player, Serf.Type serfType, Resource.Type res1, Resource.Type res2)
        {
            int inventoryWithResButNoGeneric = -1;

            foreach (var inventory in GetPlayerInventories(player))
            {
                if (inventory.HaveSerf(serfType))
                    return (int)inventory.Index;

                if ((res1 == Resource.Type.None || inventory.GetCountOf(res1) > 0) &&
                    (res2 == Resource.Type.None || inventory.GetCountOf(res2) > 0))
                {
                    inventoryWithResButNoGeneric = 0xffff + (int)inventory.Index;

                    if (inventory.HaveSerf(Serf.Type.Generic))
                        return (int)inventory.Index;
                }
            }

            return inventoryWithResButNoGeneric;
        }

        public int GetResourceAmountInInventories(Player player, Resource.Type type)
        {
            int amount = 0;

            foreach (var inventory in GetPlayerInventories(player))
            {
                amount += (int)inventory.GetCountOf(type);
            }

            return amount;
        }

        public bool HasAnyOfResource(Player player, Resource.Type type)
        {
            foreach (var inventory in GetPlayerInventories(player))
            {
                if (inventory.GetCountOf(type) > 0)
                    return true;
            }

            foreach (var flag in GetPlayerFlags(player))
            {
                if (flag.HasResources())
                {
                    for (int i = 0; i < Flag.FLAG_MAX_RES_COUNT; ++i)
                    {
                        if (flag.GetResourceAtSlot(i) == type)
                            return true;
                        else if (flag.GetResourceAtSlot(i) == Resource.Type.None)
                            break;
                    }
                }
            }

            foreach (var transporter in GetPlayerSerfs(player).Where(s => s.GetSerfType() == Serf.Type.Transporter))
            {
                if (transporter.GetTransportedResource() == type)
                    return true;
            }

            return false;
        }

        public Player GetNextPlayer(Player player)
        {
            bool next = false;

            foreach (var p in players)
            {
                if (next)
                    return p;

                if (p == player)
                    next = true;
            }

            return players.First;
        }

        public uint GetEnemyScore(Player player)
        {
            uint enemyScore = 0;

            foreach (Player p in players)
            {
                if (player.Index != p.Index)
                {
                    enemyScore += p.GetTotalMilitaryScore();
                }
            }

            return enemyScore;
        }

        internal void BuildingCaptured(Building building)
        {
            /* Save amount of land and buildings for each player */
            Dictionary<int, uint> landBefore = new Dictionary<int, uint>();
            Dictionary<int, uint> buildingsBefore = new Dictionary<int, uint>();

            foreach (Player player in players)
            {
                landBefore[(int)player.Index] = player.GetLandArea();
                buildingsBefore[(int)player.Index] = player.GetBuildingScore();
            }

            /* Update land ownership */
            UpdateLandOwnership(building.Position);

            /* Create notifications for lost land and buildings */
            foreach (Player player in players)
            {
                if (buildingsBefore[(int)player.Index] > player.GetBuildingScore())
                {
                    player.AddNotification(Message.Type.LostBuildings,
                                           building.Position,
                                           building.Player);
                }
                else if (landBefore[(int)player.Index] > player.GetLandArea())
                {
                    player.AddNotification(Message.Type.LostLand,
                                            building.Position,
                                            building.Player);
                }
            }
        }

        void ClearSearchId()
        {
            foreach (Flag flag in flags)
            {
                flag.ClearSearchId();
            }
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
                                        player.GetInventoryPriority((Resource.Type)i) >= prio)
                                    {
                                        prio = player.GetInventoryPriority((Resource.Type)i);
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

        void UpdateMapObjects()
        {
            foreach (var renderObject in renderObjects)
                renderObject.Value.Update(tick, map.RenderMap, renderObject.Key);
        }

        void UpdateFlags()
        {
            foreach (Flag flag in flags)
            {
                flag.Update();

                if (flag.Index > 0u)
                    renderFlags[flag].Update(tick, map.RenderMap, flag.Position);
            }
        }

        void UpdateRoads()
        {
            foreach (var renderRoadSegment in renderRoadSegments)
                renderRoadSegment.Value.Update(map.RenderMap);
        }

        void UpdateBorders()
        {
            foreach (var renderBorderSegment in renderBorderSegments)
                renderBorderSegment.Value.Update(map.RenderMap);
        }

        // This is called after loading a game.
        // As no roads are built manually in this case
        // we have to scan the map and add render objects
        // for all road segments.
        void PostLoadRoads()
        {
            foreach (var pos in map.Geometry)
            {
                if (map.Paths(pos) != 0)
                {
                    var cycle = new DirectionCycleCW(Direction.Right, 3u);

                    foreach (Direction dir in cycle)
                    {
                        if (map.HasPath(pos, dir))
                            AddRoadSegment(pos, dir);
                    }
                }
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
            // Note: Do not use foreach here as building.Update()
            // may delete the building and therefore change the
            // collection while we iterate through it!

            // Therefore we use a copied list here.
            var buildingList = buildings.ToList();

            for (int i = 0; i < buildingList.Count; ++i)
            {
                buildingList[i].Update(tick);

                if (buildingList[i].Index > 0u)
                {
                    // if a building burns we have to update its rendering so ensure that is is updated when visible
                    if (buildingList[i].IsBurning())
                    {
                        if (renderBuildings.ContainsKey(buildingList[i]) && renderBuildings[buildingList[i]].Visible)
                        {
                            if (!renderBuildingsInProgress.Contains(renderBuildings[buildingList[i]]))
                                renderBuildingsInProgress.Add(renderBuildings[buildingList[i]]);
                        }
                    }

                    if (renderBuildings.ContainsKey(buildingList[i]))
                        renderBuildings[buildingList[i]].Update(tick, map.RenderMap, buildingList[i].Position);
                }
            }

            for (int i = renderBuildingsInProgress.Count - 1; i >= 0; --i)
            {
                if (!renderBuildingsInProgress[i].UpdateProgress()) // no more updating needed
                    renderBuildingsInProgress.RemoveAt(i);
            }
        }

        protected void UpdateSerfs()
        {
            // Note: Do not use foreach here as serf.Update()
            // may delete the building and therefore change the
            // collection while we iterate through it!

            // Therefore we use a copied list here.
            var serfList = serfs.ToList();

            for (int i = 0; i < serfList.Count; ++i)
            {
                serfList[i].Update();

                if (serfList[i].Index > 0u && renderSerfs.ContainsKey(serfList[i]))
                {
                    renderSerfs[serfList[i]].Update(this, renderView.DataSource, tick, map, serfList[i].Position);
                }
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

                        if (dir != Direction.None && d == (int)dir.Reverse()) // TODO: shouldn't this be d == (int)inDir.Reverse() ?
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

                RemoveRoadSegment(pos, dir);

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

            path1Data.Serfs = new int[16];
            path2Data.Serfs = new int[16];

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
            if (map.TypeUp(pos) >= low &&
                map.TypeUp(pos) <= high &&
                map.TypeDown(pos) >= low &&
                map.TypeDown(pos) <= high &&
                map.TypeDown(map.MoveLeft(pos)) >= low &&
                map.TypeDown(map.MoveLeft(pos)) <= high &&
                map.TypeUp(map.MoveUpLeft(pos)) >= low &&
                map.TypeUp(map.MoveUpLeft(pos)) <= high &&
                map.TypeDown(map.MoveUpLeft(pos)) >= low &&
                map.TypeDown(map.MoveUpLeft(pos)) <= high &&
                map.TypeUp(map.MoveUp(pos)) >= low &&
                map.TypeUp(map.MoveUp(pos)) <= high)
            {
                return true;
            }

            return false;
        }

        protected void FlagRemovePlayerRefs(Flag flag)
        {
            foreach (Player player in players)
            {
                if (player.tempIndex == flag.Index)
                {
                    player.tempIndex = 0;
                }
            }
        }

        protected bool DemolishFlag(MapPos pos)
        {
            /* Handle any serf at pos. */
            if (map.HasSerf(pos))
            {
                Serf serf = GetSerfAtPos(pos);
                serf.FlagDeleted(pos);
            }

            Flag flag = flags[map.GetObjectIndex(pos)];

            if (flag.HasBuilding())
            {
                throw new ExceptionFreeserf("Failed to demolish flag with building.");
            }

            FlagRemovePlayerRefs(flag);

            /* Handle connected flag. */
            flag.MergePaths(pos);

            /* Update serfs with reference to this flag. */
            foreach (Serf serf in serfs)
            {
                serf.PathMerged(flag);
            }

            map.SetObject(pos, Map.Object.None, 0);

            /* Remove resources from flag. */
            flag.RemoveAllResources();

            renderFlags[flag].Delete();
            renderFlags.Remove(flag);

            flags.Erase(flag.Index);

            return true;
        }

        protected bool DemolishBuilding(MapPos pos)
        {
            Building building = buildings[map.GetObjectIndex(pos)];

            if (building.BurnUp())
            {
                BuildingRemovePlayerRefs(building);

                /* Remove path to building. */
                map.DeletePath(pos, Direction.DownRight);
                map.DeletePath(map.MoveDownRight(pos), Direction.UpLeft);

                /* Disconnect flag. */
                Flag flag = flags[building.GetFlagIndex()];
                flag.UnlinkBuilding();
                FlagResetTransport(flag);

                return true;
            }

            return false;
        }

        /* Map pos is lost to the owner, demolish everything. */
        protected void SurrenderLand(MapPos pos)
        {
            /* Remove building. */
            if (map.GetObject(pos) >= Map.Object.SmallBuilding &&
                map.GetObject(pos) <= Map.Object.Castle)
            {
                DemolishBuilding(pos);
            }

            if (!map.HasFlag(pos) && map.Paths(pos) != 0)
            {
                DemolishRoad(pos);
            }

            bool removeRoads = map.HasFlag(pos);

            /* Remove roads and building around pos. */
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction d in cycle)
            {
                MapPos p = map.Move(pos, d);

                if (map.GetObject(p) >= Map.Object.SmallBuilding &&
                    map.GetObject(p) <= Map.Object.Castle)
                {
                    DemolishBuilding(p);
                }

                if (removeRoads && (map.Paths(p) & Misc.Bit((int)d.Reverse())) != 0)
                {
                    DemolishRoad(p);
                }
            }

            /* Remove flag. */
            if (map.GetObject(pos) == Map.Object.Flag)
            {
                DemolishFlag(pos);
            }
        }

        protected void DemolishFlagAndRoads(MapPos pos)
        {
            if (map.HasFlag(pos))
            {
                /* Remove roads around pos. */
                var cycle = DirectionCycleCW.CreateDefault();

                foreach (Direction d in cycle)
                {
                    MapPos p = map.Move(pos, d);

                    if ((map.Paths(p) & Misc.Bit((int)d.Reverse())) != 0)
                    {
                        DemolishRoad(p);
                    }
                }

                DemolishFlag(pos);
            }
            else if (map.Paths(pos) != 0)
            {
                DemolishRoad(pos);
            }
        }

        #endregion


        public void ReadFrom(SaveReaderBinary reader)
        {
            // Load these first so map dimensions can be reconstructed.
            // This is necessary to load map positions.

            reader.Skip(74);
            gameType = reader.ReadWord(); // 74
            reader.Skip(2);  // 76
            tick = reader.ReadWord(); // 78
            gameStatsCounter = 0;
            historyCounter = 0;

            reader.Skip(4);

            random = new Random(reader.ReadWord(), reader.ReadWord(), reader.ReadWord()); // 84, 86, 88

            int maxFlagIndex = reader.ReadWord(); // 90
            int maxBuildingIndex = reader.ReadWord(); // 92
            int maxSerfIndex = reader.ReadWord(); // 94

            reader.Skip(2); // 96, was next_index
            flagSearchCounter = reader.ReadWord(); // 98

            reader.Skip(4);

            for (int i = 0; i < 4; ++i)
            {
                playerHistoryIndex[i] = reader.ReadWord(); // 104 + i*2
            }

            for (int i = 0; i < 3; ++i)
            {
                playerHistoryCounter[i] = reader.ReadWord(); // 112 + i*2
            }

            resourceHistoryIndex = reader.ReadWord(); // 118

            //  if (0/*gameType == TUTORIAL*/) {
            //    tutorial_level = *reinterpret_cast<uint16_t*>(&data[122]);
            //  } else if (0/*gameType == MISSION*/) {
            //    mission_level = *reinterpret_cast<uint16_t*>(&data[124]);
            //  }

            reader.Skip(54);

            int maxInventoryIndex = reader.ReadWord(); // 174

            reader.Skip(14); // 180 was max_next_index

            int mapSize = reader.ReadWord(); // 190

            // Avoid allocating a huge map if the input file is invalid
            if (mapSize < 3 || mapSize > 10)
            {
                throw new ExceptionFreeserf("Invalid map size in file");
            }

            map = new Map(new MapGeometry((uint)mapSize), renderView);

            reader.Skip(8);
            mapGoldMoraleFactor= reader.ReadWord(); // 200
            reader.Skip(2);
            playerScoreLeader = reader.ReadByte(); // 204

            reader.Skip(45);

            /* Load players state from save game. */
            for (uint i = 0; i < 4; ++i)
            {
                var playerReader = reader.Extract(8628);
                playerReader.Skip(130);

                if (Misc.BitTest(reader.ReadByte(), 6))
                {
                    playerReader.Reset();

                    Player player = players.GetOrInsert(i);

                    player.ReadFrom(playerReader);
                }
            }

            /* Load map state from save game. */
            uint tileCount = map.Columns * map.Rows;
            var mapReader = reader.Extract(8 * tileCount);
            map.ReadFrom(mapReader);

            LoadSerfs(reader, maxSerfIndex);
            LoadFlags(reader, maxFlagIndex);
            LoadBuildings(reader, maxBuildingIndex);
            LoadInventories(reader, maxInventoryIndex);

            gameSpeed = 0;
            gameSpeedSave = DEFAULT_GAME_SPEED;

            map.AttachToRenderLayer(renderView.GetLayer(Layer.Landscape), renderView.GetLayer(Layer.Waves), renderView.DataSource);

            InitLandOwnership();
            PostLoadRoads();

            goldTotal = map.GetGoldDeposit();

            map.AddChangeHandler(this);
        }

        public void ReadFrom(SaveReaderText reader)
        {
            // Load essential values for calculating map positions
            // so that map positions can be loaded properly.
            var sections = reader.GetSections("game");

            if (sections == null || sections.Count == 0 || sections[0] == null)
            {
                throw new ExceptionFreeserf("Failed to find section \"game\"");
            }

            var gameReader = sections[0];
            uint size = 0;

            try
            {
                size = gameReader.Value("map.size").ReadUInt();
            }
            catch
            {
                uint columnSize = gameReader.Value("map.col_size").ReadUInt();
                uint rowSize = gameReader.Value("map.row_size").ReadUInt();
                size = columnSize + rowSize - 9;
            }

            // Initialize remaining map dimensions.
            map = new Map(new MapGeometry(size), renderView);

            foreach (var subreader in reader.GetSections("map"))
            {
                map.ReadFrom(subreader);
            }

            gameType = gameReader.Value("game_type").ReadInt();
            tick = (ushort)gameReader.Value("tick").ReadUInt();
            gameStatsCounter = gameReader.Value("game_stats_counter").ReadUInt();
            historyCounter = gameReader.Value("history_counter").ReadUInt();

            random = new Random(gameReader.Value("random").ReadString());
            flagSearchCounter = (ushort)gameReader.Value("flag_search_counter").ReadUInt();

            for (int i = 0; i < 4; ++i)
            {
                playerHistoryIndex[i] = gameReader.Value("player_history_index")[i].ReadInt();
            }

            for (int i = 0; i < 3; ++i)
            {
                playerHistoryCounter[i] = gameReader.Value("player_history_counter")[i].ReadInt();
            }

            resourceHistoryIndex = gameReader.Value("resource_history_index").ReadInt();
            mapGoldMoraleFactor = gameReader.Value("map.gold_morale_factor").ReadUInt();
            playerScoreLeader = gameReader.Value("player_score_leader").ReadInt();

            goldTotal = gameReader.Value("gold_deposit").ReadUInt();

            Map.UpdateState updateState = new Map.UpdateState();

            updateState.RemoveSignsCounter = gameReader.Value("update_state.remove_signs_counter").ReadInt();
            updateState.LastTick = (ushort)gameReader.Value("update_state.last_tick").ReadUInt();
            updateState.Counter = gameReader.Value("update_state.counter").ReadInt();
            uint x = gameReader.Value("update_state.initial_pos")[0].ReadUInt();
            uint y = gameReader.Value("update_state.initial_pos")[1].ReadUInt();
            updateState.InitialPos = map.Pos(x, y);

            map.SetUpdateState(updateState);

            foreach (var subreader in reader.GetSections("player"))
            {
                Player player = players.GetOrInsert((uint)subreader.Number);
                player.ReadFrom(subreader);
            }

            foreach (var subreader in reader.GetSections("flag"))
            {
                Flag flag = flags.GetOrInsert((uint)subreader.Number);
                flag.ReadFrom(subreader);
            }

            foreach (var subreader in reader.GetSections("building"))
            {
                Building building = buildings.GetOrInsert((uint)subreader.Number);
                building.ReadFrom(subreader);
            }

            foreach (var subreader in reader.GetSections("inventory"))
            {
                Inventory inventory = inventories.GetOrInsert((uint)subreader.Number);
                inventory.ReadFrom(subreader);
            }

            foreach (var subreader in reader.GetSections("serf"))
            {
                Serf serf = serfs.GetOrInsert((uint)subreader.Number);
                serf.ReadFrom(subreader);
            }

            // Restore idle serf flag
            foreach (Serf serf in serfs)
            {
                if (serf.Index == 0)
                    continue;

                if (serf.SerfState == Serf.State.IdleOnPath ||
                    serf.SerfState == Serf.State.WaitIdleOnPath)
                {
                    map.SetIdleSerf(serf.Position);
                }

                switch (serf.SerfState)
                {
                    case Serf.State.BuildingCastle:
                    case Serf.State.IdleInStock:
                    case Serf.State.DefendingCastle:
                    case Serf.State.DefendingFortress:
                    case Serf.State.DefendingHut:
                    case Serf.State.DefendingTower:
                    case Serf.State.Invalid:
                    case Serf.State.Null:
                    case Serf.State.WaitForResourceOut:
                        break;
                    default:
                        AddSerfForDrawing(serf, serf.Position);
                        break;
                }
            }

            // Restore building index
            foreach (Building building in buildings)
            {
                if (building.Index == 0)
                    continue;

                if (map.GetObject(building.Position) < Map.Object.SmallBuilding ||
                    map.GetObject(building.Position) > Map.Object.Castle)
                {
                    throw new ExceptionFreeserf("Map data does not match building " + building.Index + " position.");
                }

                map.SetObjectIndex(building.Position, building.Index);

                OnObjectPlaced(building.Position);
            }

            // Restore flag index
            foreach (Flag flag in flags)
            {
                if (flag.Index == 0)
                    continue;

                if (map.GetObject(flag.Position) != Map.Object.Flag)
                {
                    throw new ExceptionFreeserf("Map data does not match flag " + flag.Index + " position.");
                }

                map.SetObjectIndex(flag.Position, flag.Index);

                OnObjectPlaced(flag.Position);
            }

            gameSpeed = 0;
            gameSpeedSave = DEFAULT_GAME_SPEED;

            map.AttachToRenderLayer(renderView.GetLayer(Layer.Landscape), renderView.GetLayer(Layer.Waves), renderView.DataSource);

            InitLandOwnership();
            PostLoadRoads();

            map.AddChangeHandler(this);
        }

        public void WriteTo(SaveWriterText writer)
        {
            writer.Value("map.size").Write(map.Size);
            writer.Value("game_type").Write(gameType);
            writer.Value("tick").Write(tick);
            writer.Value("game_stats_counter").Write(gameStatsCounter);
            writer.Value("history_counter").Write(historyCounter);
            writer.Value("random").Write(random.ToString());

            writer.Value("next_index").Write(0); // next_index (we keep this to be compatible to freeserf save games)
            writer.Value("flag_search_counter").Write(flagSearchCounter);

            for (int i = 0; i < 4; ++i)
            {
                writer.Value("player_history_index").Write(playerHistoryIndex[i]);
            }

            for (int i = 0; i < 3; ++i)
            {
                writer.Value("player_history_counter").Write(playerHistoryCounter[i]);
            }

            writer.Value("resource_history_index").Write(resourceHistoryIndex);

            writer.Value("max_next_index").Write(0); // max_next_index (we keep this to be compatible to freeserf save games)
            writer.Value("map.gold_morale_factor").Write(mapGoldMoraleFactor);
            writer.Value("player_score_leader").Write(playerScoreLeader);

            writer.Value("gold_deposit").Write(goldTotal);

            var updateState = map.GetUpdateState();

            writer.Value("update_state.remove_signs_counter").Write(updateState.RemoveSignsCounter);
            writer.Value("update_state.last_tick").Write(updateState.LastTick);
            writer.Value("update_state.counter").Write(updateState.Counter);
            writer.Value("update_state.initial_pos").Write(map.PosColumn(updateState.InitialPos));
            writer.Value("update_state.initial_pos").Write(map.PosRow(updateState.InitialPos));

            foreach (var player in players)
            {
                var playerWriter = writer.AddSection("player", player.Index);
                
                player.WriteTo(playerWriter);
            }

            foreach (var flag in flags)
            {
                if (flag.Index == 0)
                    continue;

                var flagWriter = writer.AddSection("flag", flag.Index);
                flag.WriteTo(flagWriter);
            }

            foreach (var building in buildings)
            {
                if (building.Index == 0)
                    continue;

                var buildingWriter = writer.AddSection("building", building.Index);
                building.WriteTo(buildingWriter);
            }

            foreach (var inventory in inventories)
            {
                var inventoryWriter = writer.AddSection("inventory", inventory.Index);
                inventory.WriteTo(inventoryWriter);
            }

            foreach (var serf in serfs)
            {
                if (serf.Index == 0)
                    continue;

                var serfWriter = writer.AddSection("serf", serf.Index);
                serf.WriteTo(serfWriter);
            }

            map.WriteTo(writer);
        }

        // Load serf state from save game.
        bool LoadSerfs(SaveReaderBinary reader, int maxSerfIndex)
        {
            // Load serf bitmap.
            int bitmapSize = 4 * ((maxSerfIndex + 31) / 32);
            var bitmap = reader.Read((uint)bitmapSize);

            if (bitmap == null)
                return false;

            // Load serf data.
            for (int i = 0; i < maxSerfIndex; ++i)
            {
                var serfReader = reader.Extract(16);

                if (Misc.BitTest(bitmap[(i) >> 3], 7 - ((i) & 7)))
                {
                    Serf serf = serfs.GetOrInsert((uint)i);
                    serf.ReadFrom(serfReader);

                    switch (serf.SerfState)
                    {
                        case Serf.State.BuildingCastle:
                        case Serf.State.IdleInStock:
                        case Serf.State.DefendingCastle:
                        case Serf.State.DefendingFortress:
                        case Serf.State.DefendingHut:
                        case Serf.State.DefendingTower:
                        case Serf.State.Invalid:
                        case Serf.State.Null:
                        case Serf.State.WaitForResourceOut:
                            break;
                        default:
                            AddSerfForDrawing(serf, serf.Position);
                            break;
                    }
                }
            }

            return true;
        }

        // Load flags state from save game.
        bool LoadFlags(SaveReaderBinary reader, int maxFlagIndex)
        {
            // Load flag bitmap.
            int bitmapSize = 4 * ((maxFlagIndex + 31) / 32);
            var bitmap = reader.Read((uint)bitmapSize);

            if (bitmap == null)
                return false;

            // Load flag data.
            for (int i = 0; i < maxFlagIndex; ++i)
            {
                var flagReader = reader.Extract(70);

                if (Misc.BitTest(bitmap[(i) >> 3], 7 - ((i) & 7)))
                {
                    Flag flag = flags.GetOrInsert((uint)i);
                    flag.ReadFrom(flagReader);
                }
            }

            // Set flag positions.
            foreach (MapPos pos in map.Geometry)
            {
                if (map.GetObject(pos) == Map.Object.Flag)
                {
                    Flag flag = flags[map.GetObjectIndex(pos)];
                    flag.Position = pos;

                    OnObjectPlaced(flag.Position);
                }
            }

            return true;
        }

        // Load buildings state from save game.
        bool LoadBuildings(SaveReaderBinary reader, int maxBuildingIndex)
        {
            // Load building bitmap.
            int bitmapSize = 4 * ((maxBuildingIndex + 31) / 32);
            var bitmap = reader.Read((uint)bitmapSize);

            if (bitmap == null)
                return false;

            // Load building data.
            for (int i = 0; i < maxBuildingIndex; ++i)
            {
                var buildingReader = reader.Extract(18);

                if (Misc.BitTest(bitmap[(i) >> 3], 7 - ((i) & 7)))
                {
                    Building building = buildings.GetOrInsert((uint)i);
                    building.ReadFrom(buildingReader);

                    OnObjectPlaced(building.Position);
                }
            }

            return true;
        }

        // Load inventories state from save game.
        bool LoadInventories(SaveReaderBinary reader, int maxInventoryIndex)
        {
            // Load inventory bitmap.
            int bitmapSize = 4 * ((maxInventoryIndex + 31) / 32);
            var bitmap = reader.Read((uint)bitmapSize);

            if (bitmap == null)
                return false;

            // Load inventory data.
            for (int i = 0; i < maxInventoryIndex; ++i)
            {
                var inventoryReader = reader.Extract(120);

                if (Misc.BitTest(bitmap[(i) >> 3], 7 - ((i) & 7)))
                {
                    Inventory inventory = inventories.GetOrInsert((uint)i);
                    inventory.ReadFrom(inventoryReader);
                }
            }

            return true;
        }

        public override void OnHeightChanged(MapPos pos)
        {
            // Update road segments
            for (int i = 0; i < 7; ++i)
            {
                var checkPos = map.PosAddSpirally(pos, (uint)i);
                var cycle = new DirectionCycleCW(Direction.Right, 3u);

                foreach (var dir in cycle)
                {
                    if (map.HasPath(checkPos, dir))
                    {
                        long index = Render.RenderRoadSegment.CreateIndex(checkPos, dir);

                        if (renderRoadSegments.ContainsKey(index))
                            renderRoadSegments[index].UpdateAppearance();
                    }
                }
            }
        }

        public override void OnObjectChanged(MapPos pos)
        {
            // memorize mineral for AI
            if (map.GetObject(pos) >= Map.Object.SignLargeGold && map.GetObject(pos) < Map.Object.SignEmpty)
            {
                int index = map.GetObject(pos) - Map.Object.SignLargeGold;

                AI.MemorizeMineralSpot(pos, (Map.Minerals)(1 + index / 2), index % 2 == 0);
            }
        }

        public override void OnObjectExchanged(uint pos, Map.Object oldObject, Map.Object newObject)
        {
            if (oldObject != Map.Object.None && newObject < Map.Object.Tree0)
            {
                // we don't draw buildings etc with renderObjects
                if (renderObjects.ContainsKey(pos))
                {
                    renderObjects[pos].Delete();
                    renderObjects.Remove(pos);
                }
            }
            else if (renderObjects.ContainsKey(pos))
            {
                renderObjects[pos].ChangeObjectType(newObject);
            }
        }

        public override void OnObjectPlaced(MapPos pos)
        {
            var obj = map.GetObject(pos);

            // rendering
            if (obj == Map.Object.Flag)
            {
                var flag = GetFlagAtPos(pos);

                if (!renderFlags.ContainsKey(flag))
                {
                    var renderFlag = new Render.RenderFlag(flag, renderView.GetLayer(Layer.Objects), renderView.SpriteFactory, renderView.DataSource);

                    renderFlag.Visible = true;

                    renderFlags.Add(flag, renderFlag);
                }
            }
            else if (obj == Map.Object.SmallBuilding ||
                     obj == Map.Object.LargeBuilding ||
                     obj == Map.Object.Castle)
            {
                var building = GetBuildingAtPos(pos);

                if (!renderBuildings.ContainsKey(building))
                {
                    var renderBuilding = new Render.RenderBuilding(building, renderView.GetLayer(Layer.Buildings),
                        renderView.GetLayer(Layer.Objects), renderView.SpriteFactory, renderView.DataSource, renderView.AudioFactory.GetAudio());

                    renderBuilding.Visible = true;

                    renderBuildings.Add(building, renderBuilding);

                    if (!building.IsDone() || building.IsBurning())
                        renderBuildingsInProgress.Add(renderBuilding);
                }
            }
            else // map object
            {
                if (obj != Map.Object.None)
                {
                    var renderObject = new Render.RenderMapObject(obj, renderView.GetLayer(Layer.Objects), renderView.SpriteFactory, renderView.DataSource);

                    renderObject.Visible = true;

                    renderObjects.Add(pos, renderObject);
                }
            }
        }

        public void AddSerfForDrawing(Serf serf, MapPos pos)
        {
            if (renderSerfs.ContainsKey(serf))
                return;

            var renderSerf = new Render.RenderSerf(serf, renderView.GetLayer(Layer.Serfs), renderView.SpriteFactory,
                renderView.DataSource, renderView.AudioFactory.GetAudio());

            renderSerf.Visible = true;

            renderSerfs.Add(serf, renderSerf);
        }

        public void RemoveSerfFromDrawing(Serf serf)
        {
            if (renderSerfs.ContainsKey(serf))
            {
                renderSerfs[serf].Delete();
                renderSerfs.Remove(serf);
            }
        }

        public override void OnRoadSegmentPlaced(MapPos pos, Direction dir)
        {
            AddRoadSegment(pos, dir);
        }

        public override void OnRoadSegmentDeleted(MapPos pos, Direction dir)
        {
            RemoveRoadSegment(pos, dir);
        }

        void AddRoadSegment(MapPos pos, Direction dir)
        {
            if (dir < Direction.Right || dir > Direction.Down)
                return;

            long index = Render.RenderRoadSegment.CreateIndex(pos, dir);

            var renderRoadSegment = new Render.RenderRoadSegment(Map, pos, dir, renderView.GetLayer(Layer.Paths),
                renderView.SpriteFactory, renderView.DataSource);

            renderRoadSegment.Visible = true;

            renderRoadSegments.Add(index, renderRoadSegment);
        }

        void RemoveRoadSegment(MapPos pos, Direction dir)
        {
            if (dir < Direction.Right)
                return;

            if (dir > Direction.Down)
            {
                RemoveRoadSegment(map.Move(pos, dir), dir.Reverse());
                return;
            }

            long index = Render.RenderRoadSegment.CreateIndex(pos, dir);

            // TODO: the following code crashed from time to time without the contains key check
            // TODO: which case could lead to this? maybe look further into it as it should not happen
            // TODO: even without the check.
            if (renderRoadSegments.ContainsKey(index))
            {
                renderRoadSegments[index].Delete();
                renderRoadSegments.Remove(index);
            }
        }
    }
}
