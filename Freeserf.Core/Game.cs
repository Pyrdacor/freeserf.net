﻿/*
 * Game.cs - Gameplay related functions
 *
 * Copyright (C) 2013-2017   Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018-2020   Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using Freeserf.Audio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf
{
    
    using Serialize;
    using word = UInt16;
    using dword = UInt32;
    using MapPos = UInt32;
    using GameTime = UInt32;
    using Buildings = Collection<Building>;
    using Flags = Collection<Flag>;
    using Inventories = Collection<Inventory>;
    using Players = Collection<Player>;
    using Serfs = Collection<Serf>;
    using Values = Dictionary<uint, uint>;

    [Flags]
    public enum Option
    {
        MessagesImportant = 0x0001, // This is always set as these messages will always be notified.
        InvertScrolling = 0x0002,
        FastBuilding = 0x0004,
        MessagesAll = 0x0008,
        MessagesMost = 0x0010,
        MessagesFew = 0x0020,
        PathwayScrolling = 0x0040,
        FastMapClick = 0x0080,
        HideCursorWhileScrolling = 0x0100,
        ResetCursorAfterScrolling = 0x0200,
        Default = 0x0039
    }

    public class Game : MapHandler, IState
    {
        public const int MAX_PLAYER_COUNT = 4;
        internal Players Players { get; }
        internal Flags Flags { get; }
        internal Inventories Inventories { get; }
        internal Buildings Buildings { get; }
        internal Serfs Serfs { get; }
        readonly List<Serf>[] knights = new List<Serf>[MAX_PLAYER_COUNT];

        // Rendering
        readonly Render.IRenderView renderView = null;
        readonly IAudioInterface audioInterface = null;
        readonly ConcurrentDictionary<Serf, Render.RenderSerf> renderSerfs = new ConcurrentDictionary<Serf, Render.RenderSerf>();
        readonly ConcurrentDictionary<Building, Render.RenderBuilding> renderBuildings = new ConcurrentDictionary<Building, Render.RenderBuilding>();
        readonly ConcurrentDictionary<Flag, Render.RenderFlag> renderFlags = new ConcurrentDictionary<Flag, Render.RenderFlag>();
        readonly ConcurrentDictionary<MapPos, Render.RenderMapObject> renderObjects = new ConcurrentDictionary<MapPos, Render.RenderMapObject>();
        readonly ConcurrentDictionary<long, Render.RenderRoadSegment> renderRoadSegments = new ConcurrentDictionary<long, Render.RenderRoadSegment>();
        readonly ConcurrentDictionary<long, Render.RenderBorderSegment> renderBorderSegments = new ConcurrentDictionary<long, Render.RenderBorderSegment>();
        readonly List<Render.RenderBuilding> renderBuildingsInProgress = new List<Render.RenderBuilding>();

        [Data]
        private readonly GameState state = new GameState();

        public bool Dirty => state.Dirty ||
            Map.Dirty ||
            Players.Any(p => p.Dirty) ||
            Flags.Any(f => f.Dirty) ||
            Inventories.Any(i => i.Dirty) ||
            Buildings.Any(b => b.Dirty) ||
            Serfs.Any(s => s.Dirty);

        public IReadOnlyList<string> DirtyProperties
        {
            get
            {
                // Note: The object collections are serialized
                // separately in GameStateSerializer.
                var dirtyProperties = new List<string>(2);
                if (state.Dirty)
                    dirtyProperties.Add(nameof(state));
                if (Map.Dirty)
                    dirtyProperties.Add(nameof(Map));
                return dirtyProperties;
            }
        }

        public void ResetDirtyFlag()
        {
            state.ResetDirtyFlag();
            Map?.ResetDirtyFlag();

            foreach (var player in Players)
                player.ResetDirtyFlag();
            foreach (var flag in Flags)
                flag.ResetDirtyFlag();
            foreach (var inventory in Inventories)
                inventory.ResetDirtyFlag();
            foreach (var building in Buildings)
                building.ResetDirtyFlag();
            foreach (var serf in Serfs)
                serf.ResetDirtyFlag();
        }

        ushort gameSpeedSave;

        int tickDifference;
        int gameType; // TODO: this is never used beside in savegames
        int playerScoreLeader; // TODO: this is never used beside in savegames

        int birdSoundCounter;

        [Data]
        internal Map Map { get; private set; }
        internal uint MapGoldMoraleFactor => state.MapGoldMoraleFactor;
        internal uint GoldTotal => state.GoldTotal;
        internal word Tick => state.Tick;
        internal dword ConstTick => state.ConstTick;
        public GameTime GameTime => state.GameTime; // in seconds
        public GameTime NextGameTime => GameTime + (GameTime)(state.GameTimeTicksOfSecond + state.GameSpeed) / Global.TICKS_PER_SEC;

        internal Game(Map map)
        {
            AI.ClearMemory();

            Players = new Players(this);
            Flags = new Flags(this);
            Inventories = new Inventories(this);
            Buildings = new Buildings(this);
            Serfs = new Serfs(this);

            tickDifference = 0;
            gameType = 0;
            birdSoundCounter = 0;

            if (map != null)
                Map = new Map(map.Geometry, null, map);
        }

        internal Game(Render.IRenderView renderView, IAudioInterface audioInterface)
            : this(null)
        {
            this.renderView = renderView;
            this.audioInterface = audioInterface;

            // Create NULL-serf 
            Serfs.Allocate();

            // Create NULL-building (index 0 is undefined) 
            Buildings.Allocate();

            // Create NULL-flag (index 0 is undefined) 
            Flags.Allocate();
        }

        public void ClearVisuals()
        {
            // delete all render objects

            foreach (var building in renderBuildings)
                building.Value.Delete();

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
        }

        public void Close()
        {
            ClearVisuals();

            // close map (and delete render map)
            Map.Close();
        }

        internal void PlayerDefeated(Player player)
        {
            // TODO: no more AI actions, no more client input, show outro, etc
        }

        internal void PlayerSurrender(Player player)
        {
            // TODO: similar to PlayerDefeated
        }

        // This is used for the background game when the game init box is active.
        public void ScrollMapRandomly()
        {
            if (Map != null)
                Map.ScrollTo(state.Random.Next() % Map.Columns, state.Random.Next() % Map.Rows);
        }

        internal void AddGoldTotal(int delta)
        {
            if (delta < 0)
            {
                if ((int)GoldTotal < -delta)
                {
                    throw new ExceptionFreeserf(this, ErrorSystemType.Game, "Failed to decrease global gold counter.");
                }
            }

            state.GoldTotal = (uint)((int)GoldTotal + delta);
        }

        internal Building GetBuildingAtPosition(MapPos position)
        {
            var mapObject = Map.GetObject(position);

            if (mapObject >= Map.Object.SmallBuilding && mapObject <= Map.Object.Castle)
            {
                return Buildings[Map.GetObjectIndex(position)];
            }

            return null;
        }

        internal Flag GetFlagAtPosition(MapPos position)
        {
            if (Map.GetObject(position) != Map.Object.Flag)
            {
                return null;
            }

            return Flags[Map.GetObjectIndex(position)];
        }

        internal Flag GetFlagForBuildingAtPosition(MapPos position)
        {
            return GetFlagAtPosition(Map.MoveDownRight(position));
        }

        internal Serf GetSerfAtPosition(MapPos position)
        {
            var serf = Serfs[Map.GetSerfIndex(position)];

            if (serf != null && serf.Index == 0)
                return null;

            return serf;
        }


        #region External interface

        /// <summary>
        /// Adds a new player to the game. Returns the added player.
        /// </summary>
        internal Player InsertPlayer(uint index, uint intelligence, uint supplies, uint reproduction)
        {
            if (index >= Game.MAX_PLAYER_COUNT)
                throw new ExceptionFreeserf(ErrorSystemType.Application, "Invalid player index.");

            // Allocate object 
            var player = Players.GetOrInsert(index);

            if (player == null)
            {
                throw new ExceptionFreeserf(this, ErrorSystemType.Game, "Failed to create new player.");
            }

            player.Init(intelligence, supplies, reproduction);

            // Update map values dependent on player count 
            state.MapGoldMoraleFactor = 10u * 1024u * (uint)Players.Size;

            return player;
        }

        internal bool Init(uint mapSize, Random random)
        {
            Map = new Map(new MapGeometry(mapSize), renderView);
            var generator = new ClassicMissionMapGenerator(Map, random);

            generator.Init();
            generator.Generate();

            Map.AddChangeHandler(this);
            Map.InitTiles(generator);
            state.GoldTotal = Map.GetGoldDeposit();

            return true;
        }

        internal void InitKnights()
        {
            for (int i = 0; i < MAX_PLAYER_COUNT; ++i)
            {
                if (i >= PlayerCount)
                {
                    knights[i] = null;
                }
                else
                {
                    knights[i] = Serfs.Where(s => s.Player == i && s.IsKnight).ToList();
                }
            }
        }

        internal void AddKnight(Serf serf)
        {
            knights[serf.Player].Add(serf);
        }

        internal void RemoveKnight(Serf serf)
        {
            knights[serf.Player].Remove(serf);
        }

        /// <summary>
        /// Update game state after tick increment.
        /// </summary>
        public void Update()
        {
            // Increment tick counters
            if (state.ConstTick == uint.MaxValue)
                state.ConstTick = 0;
            else
                ++state.ConstTick;

            // Update tick counters based on game speed 
            uint lastTick = Tick;

            state.Tick += (ushort)state.GameSpeed;
            state.GameTimeTicksOfSecond += state.GameSpeed;
            while (state.GameTimeTicksOfSecond >= Global.TICKS_PER_SEC)
            {
                state.GameTimeTicksOfSecond -= Global.TICKS_PER_SEC;
                ++state.GameTime;
            }

            if (lastTick > Tick) // ushort overflow
            {
                tickDifference = (int)Tick + (int)ushort.MaxValue - (int)lastTick;
            }
            else
            {
                tickDifference = (int)(Tick - lastTick);
            }

            ClearSerfRequestFailure();
            Map.Update(state.Tick, state.Random);

            // Update players 
            foreach (var player in Players.ToList())
            {
                player.Update();
            }

            // Update knight morale 
            state.KnightMoraleCounter -= tickDifference;

            if (state.KnightMoraleCounter < 0)
            {
                UpdateKnightMorale();
                state.KnightMoraleCounter += 256;
            }

            // Schedule resources to go out of inventories 
            state.InventoryScheduleCounter -= tickDifference;

            if (state.InventoryScheduleCounter < 0)
            {
                UpdateInventories();
                state.InventoryScheduleCounter += 64;
            }

            // AI related updates 
            foreach (var player in Players.ToList())
            {
                if (player.IsAI)
                {
                    if (player.AI != null)
                        player.AI.Update(this);
                }
            }

            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (Map?.RenderMap != null)
                Map.RenderMap.UpdateWaves(Tick);

            UpdateRoads();
            UpdateBorders();
            UpdateMapObjects();
            UpdateFlags();
            UpdateBuildings();
            UpdateSerfs();
            UpdateGameStats();

            // Play bird sounds 
            birdSoundCounter -= tickDifference;

            if (birdSoundCounter < 0)
            {
                PlaySound(Audio.Audio.TypeSfx.BirdChirp0 + 4 * (RandomInt() & 0x3));
                birdSoundCounter += 0xfff + RandomInt() & 0x3ff;
            }
        }

        void PlaySound(Audio.Audio.TypeSfx type)
        {
            audioInterface?.AudioFactory?.GetAudio()?.GetSoundPlayer()?.PlayTrack((int)type);
        }

        public void TogglePause()
        {
            if (state.GameSpeed != 0)
            {
                gameSpeedSave = state.GameSpeed;
                state.GameSpeed = 0;
            }
            else
            {
                state.GameSpeed = gameSpeedSave;
            }

            Log.Info.Write(ErrorSystemType.Game, $"Game speed: {state.GameSpeed}");
        }

        public uint GameSpeed => state.GameSpeed;
        public bool IsPaused => state.GameSpeed == 0;

        public void Pause()
        {
            if (state.GameSpeed != 0)
            {
                gameSpeedSave = state.GameSpeed;
                state.GameSpeed = 0;
                Log.Info.Write(ErrorSystemType.Game, $"Game speed: {state.GameSpeed}");
            }
        }

        public void Resume()
        {
            if (state.GameSpeed == 0)
            {
                state.GameSpeed = gameSpeedSave;
                Log.Info.Write(ErrorSystemType.Game, $"Game speed: {state.GameSpeed}");
            }
        }

        public void IncreaseSpeed()
        {
            if (state.GameSpeed < Global.MAX_GAME_SPEED)
            {
                ++state.GameSpeed;
                Log.Info.Write(ErrorSystemType.Game, $"Game speed: {state.GameSpeed}");
            }
        }

        public void DecreaseSpeed()
        {
            if (state.GameSpeed > 0)
            {
                --state.GameSpeed;
                Log.Info.Write(ErrorSystemType.Game, $"Game speed: {state.GameSpeed}");
            }
        }

        public void SetSpeed(uint speed)
        {
            state.GameSpeed = Convert.ToUInt16(speed);
        }
        
        public void ResetSpeed()
        {
            state.GameSpeed = GameState.DEFAULT_GAME_SPEED;
            Log.Info.Write(ErrorSystemType.Game, $"Game speed: {state.GameSpeed}");
        }

        public void MaximizeSpeed()
        {
            state.GameSpeed = Global.MAX_GAME_SPEED;
            Log.Info.Write(ErrorSystemType.Game, $"Game speed: {state.GameSpeed}");
        }

        // Prepare a ground analysis at position. 
        internal void PrepareGroundAnalysis(MapPos position, uint[] estimates)
        {
            const uint groundAnalysisRadius = 25;

            for (int i = 0; i < 5; ++i)
                estimates[i] = 0;

            // Sample the cursor position with maximum weighting. 
            GetResourceEstimate(position, groundAnalysisRadius, estimates);

            // Move outward in a spiral around the initial position.
            // The weighting of the samples attenuates linearly
            // with the distance to the center.
            for (uint i = 0; i < groundAnalysisRadius - 1; ++i)
            {
                position = Map.MoveRight(position);

                var cycle = new DirectionCycleCW(Direction.Down, 6);

                foreach (var direction in cycle)
                {
                    for (uint j = 0; j < i + 1; ++j)
                    {
                        GetResourceEstimate(position, groundAnalysisRadius - i, estimates);
                        position = Map.Move(position, direction);
                    }
                }
            }

            // Process the samples. 
            for (int i = 0; i < 5; ++i)
            {
                estimates[i] >>= 4;
                estimates[i] = Math.Min(estimates[i], 999);
            }
        }

        internal bool SendGeologist(Flag destination)
        {
            return SendSerfToFlag(destination, Serf.Type.Geologist, Resource.Type.Hammer, Resource.Type.None);
        }

        /// <summary>
        /// Return the height that is needed before a large building can be built.
        /// Returns negative if the needed height cannot be reached.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        internal int GetLevelingHeight(MapPos position)
        {
            // Find min and max height 
            uint heightMin = 31;
            uint heightMax = 0;

            for (uint i = 0; i < 12; ++i)
            {
                var adjacentPosition = Map.PositionAddSpirally(position, 7u + i);
                var adjacentHeight = Map.GetHeight(adjacentPosition);

                if (heightMin > adjacentHeight)
                    heightMin = adjacentHeight;
                if (heightMax < adjacentHeight)
                    heightMax = adjacentHeight;
            }

            // Adjust for height of adjacent unleveled buildings 
            for (uint i = 0; i < 18; ++i)
            {
                var adjacentPosition = Map.PositionAddSpirally(position, 19u + i);

                if (Map.GetObject(adjacentPosition) == Map.Object.LargeBuilding)
                {
                    var building = Buildings[Map.GetObjectIndex(adjacentPosition)];

                    if (building.IsLeveling)
                    {
                        // Leveling in progress 
                        uint height = building.Level;

                        if (heightMin > height)
                            heightMin = height;
                        if (heightMax < height)
                            heightMax = height;
                    }
                }
            }

            // Return if height difference is too big 
            if (heightMax - heightMin >= 9)
                return -1;

            // Calculate "mean" height. Height of center is added twice. 
            uint heightMean = Map.GetHeight(position);

            for (uint i = 0; i < 7; ++i)
            {
                var adjacentPosition = Map.PositionAddSpirally(position, i);

                heightMean += Map.GetHeight(adjacentPosition);
            }

            heightMean >>= 3;

            // Calcualte height after leveling 
            uint heightNewMin = Math.Max((heightMax > 4) ? (heightMax - 4) : 1, 1);
            uint heightNewmax = heightMin + 4;
            uint heightNew = Misc.Clamp(heightNewMin, heightMean, heightNewmax);

            return (int)heightNew;
        }

        /// <summary>
        /// Check whether military buildings are allowed at position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        internal bool CanBuildMilitary(MapPos position)
        {
            // Check that no military buildings are nearby 
            for (uint i = 0; i < 1 + 6 + 12; ++i)
            {
                var adjacentPosition = Map.PositionAddSpirally(position, i);

                if (Map.HasBuilding(adjacentPosition))
                {
                    var building = Buildings[Map.GetObjectIndex(adjacentPosition)];

                    if (building.IsMilitary())
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether a small building is possible at position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        internal bool CanBuildSmall(MapPos position)
        {
            return MapTypesWithin(position, Map.Terrain.Grass0, Map.Terrain.Grass3);
        }

        /// <summary>
        /// Checks whether a mine is possible at position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        internal bool CanBuildMine(MapPos position)
        {
            bool canBuild = false;

            Map.Terrain[] types = new Map.Terrain[]
            {
                Map.TypeDown(position),
                Map.TypeUp(position),
                Map.TypeDown(Map.MoveLeft(position)),
                Map.TypeUp(Map.MoveUpLeft(position)),
                Map.TypeDown(Map.MoveUpLeft(position)),
                Map.TypeUp(Map.MoveUp(position))
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

        /// <summary>
        /// Checks whether a large building is possible at position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        internal bool CanBuildLarge(MapPos position)
        {
            // Check that surroundings are passable by serfs. 
            for (uint i = 0; i < 6; ++i)
            {
                var adjacentPosition = Map.PositionAddSpirally(position, 1 + i);
                var adjacentSpace = Map.MapSpaceFromObject[(int)Map.GetObject(adjacentPosition)];

                if (adjacentSpace >= Map.Space.Semipassable)
                    return false;
            }

            // Check that buildings in the second shell aren't large or castle. 
            for (uint i = 0; i < 12; ++i)
            {
                var adjacentPosition = Map.PositionAddSpirally(position, 7u + i);

                if (Map.GetObject(adjacentPosition) >= Map.Object.LargeBuilding &&
                    Map.GetObject(adjacentPosition) <= Map.Object.Castle)
                {
                    return false;
                }
            }

            // Check if center hexagon is not type grass. 
            if (Map.TypeUp(position) != Map.Terrain.Grass1 ||
                Map.TypeDown(position) != Map.Terrain.Grass1 ||
                Map.TypeDown(Map.MoveLeft(position)) != Map.Terrain.Grass1 ||
                Map.TypeUp(Map.MoveUpLeft(position)) != Map.Terrain.Grass1 ||
                Map.TypeDown(Map.MoveUpLeft(position)) != Map.Terrain.Grass1 ||
                Map.TypeUp(Map.MoveUp(position)) != Map.Terrain.Grass1)
            {
                return false;
            }

            // Check that leveling is possible 
            if (GetLevelingHeight(position) < 0)
                return false;

            return true;
        }

        internal bool CanBuildAnyBuilding(MapPos position, Player player)
        {
            if (!CanPlayerBuild(position, player))
                return false;

            if (Map.MapSpaceFromObject[(int)Map.GetObject(position)] != Map.Space.Open)
                return false;

            var flagPosition = Map.MoveDownRight(position);

            if (!Map.HasFlag(flagPosition) && !CanBuildFlag(flagPosition, player))
                return false;

            return CanBuildSmall(position) || CanBuildMine(position);
        }

        /// <summary>
        /// Checks whether a building of the specified type is possible at position.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="type"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        internal bool CanBuildBuilding(MapPos position, Building.Type type, Player player)
        {
            if (!CanPlayerBuild(position, player))
                return false;

            // Check that space is clear 
            if (Map.MapSpaceFromObject[(int)Map.GetObject(position)] != Map.Space.Open)
            {
                return false;
            }

            // Check that building flag is possible if it
            // doesn't already exist.
            var flagPosition = Map.MoveDownRight(position);

            if (!Map.HasFlag(flagPosition) && !CanBuildFlag(flagPosition, player))
            {
                return false;
            }

            // Check if building size is possible. 
            switch (type)
            {
                case Building.Type.Fisher:
                case Building.Type.Lumberjack:
                case Building.Type.Boatbuilder:
                case Building.Type.Stonecutter:
                case Building.Type.Forester:
                case Building.Type.Hut:
                case Building.Type.Mill:
                    if (!CanBuildSmall(position))
                        return false;
                    break;
                case Building.Type.StoneMine:
                case Building.Type.CoalMine:
                case Building.Type.IronMine:
                case Building.Type.GoldMine:
                    if (!CanBuildMine(position))
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
                    if (!CanBuildLarge(position))
                        return false;
                    break;
                default:
                    Debug.NotReached();
                    break;
            }

            // Check if military building is possible 
            if ((type == Building.Type.Hut ||
                 type == Building.Type.Tower ||
                 type == Building.Type.Fortress) &&
                !CanBuildMilitary(position))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether a castle can be built by player at position.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        internal bool CanBuildCastle(MapPos position, Player player)
        {
            if (player.HasCastle)
                return false;

            // Check owner of land around position 
            for (uint i = 0; i < 7; ++i)
            {
                var adjacentPosition = Map.PositionAddSpirally(position, i);

                if (Map.HasOwner(adjacentPosition))
                    return false;
            }

            // Check that land is clear at position 
            if (Map.MapSpaceFromObject[(int)Map.GetObject(position)] != Map.Space.Open ||
                Map.Paths(position) != 0)
            {
                return false;
            }

            var flagPosition = Map.MoveDownRight(position);

            // Check that land is clear at position 
            if (Map.MapSpaceFromObject[(int)Map.GetObject(flagPosition)] != Map.Space.Open ||
                Map.Paths(flagPosition) != 0)
            {
                return false;
            }

            return CanBuildLarge(position);
        }

        internal bool CanBuildFlag(MapPos position, Player player)
        {
            // Check owner of land 
            if (!Map.HasOwner(position) || Map.GetOwner(position) != player.Index)
            {
                return false;
            }

            // Check that land is clear 
            if (Map.MapSpaceFromObject[(int)Map.GetObject(position)] != Map.Space.Open)
            {
                return false;
            }

            // Check whether cursor is in water 
            if (Map.TypeUp(position) <= Map.Terrain.Water3 &&
                Map.TypeDown(position) <= Map.Terrain.Water3 &&
                Map.TypeDown(Map.MoveLeft(position)) <= Map.Terrain.Water3 &&
                Map.TypeUp(Map.MoveUpLeft(position)) <= Map.Terrain.Water3 &&
                Map.TypeDown(Map.MoveUpLeft(position)) <= Map.Terrain.Water3 &&
                Map.TypeUp(Map.MoveUp(position)) <= Map.Terrain.Water3)
            {
                return false;
            }

            // Check that no flags are nearby 
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                if (Map.GetObject(Map.Move(position, direction)) == Map.Object.Flag)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check whether player is allowed to build anything
        /// at position. To determine if the initial castle can
        /// be built use <see cref="CanBuildCastle()"/> instead.
        /// 
        /// TODO Existing buildings at position should be
        /// disregarded so this can be used to determine what
        /// can be built after the existing building has been
        /// demolished.
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <param name="player">Player who wants to build</param>
        /// <returns></returns>
        internal bool CanPlayerBuild(MapPos position, Player player)
        {
            if (!player.HasCastle)
                return false;

            // Check owner of land around position 
            for (uint i = 0; i < 7; ++i)
            {
                var adjacentPosition = Map.PositionAddSpirally(position, i);

                if (!Map.HasOwner(adjacentPosition) || Map.GetOwner(adjacentPosition) != player.Index)
                {
                    return false;
                }
            }

            // Check whether cursor is in water 
            if (Map.TypeUp(position) <= Map.Terrain.Water3 &&
                Map.TypeDown(position) <= Map.Terrain.Water3 &&
                Map.TypeDown(Map.MoveLeft(position)) <= Map.Terrain.Water3 &&
                Map.TypeUp(Map.MoveUpLeft(position)) <= Map.Terrain.Water3 &&
                Map.TypeDown(Map.MoveUpLeft(position)) <= Map.Terrain.Water3 &&
                Map.TypeUp(Map.MoveUp(position)) <= Map.Terrain.Water3)
            {
                return false;
            }

            // Check that no paths are blocking. 
            if (Map.Paths(position) != 0)
                return false;

            return true;
        }

        /// <summary>
        /// Test whether a given road can be constructed by player. The final
        /// destination will be returned in destination, and water will be set if the
        /// resulting path is a water path.
        /// 
        /// This will return success even if the destination does _not_ contain
        /// a flag, and therefore partial paths can be validated with this function.
        /// </summary>
        /// <param name="road">Road that should be built</param>
        /// <param name="player">Player who wants to build the road</param>
        /// <param name="destination"></param>
        /// <param name="water"></param>
        /// <param name="endThere"></param>
        /// <returns></returns>
        internal int CanBuildRoad(Road road, Player player, ref MapPos destination, ref bool water, bool endThere = false)
        {
            // Follow along path to other flag. Test along the way
            // whether the path is on ground or in water.
            var position = road.StartPosition;
            int test = 0;

            if (!Map.HasOwner(position) || Map.GetOwner(position) != player.Index || !Map.HasFlag(position))
            {
                return 0;
            }

            var directions = road.Directions.Reverse();
            int directionCount = road.Directions.Count;
            int directionCounter = 0;

            foreach (var direction in directions)
            {
                ++directionCounter;

                if (!Map.IsRoadSegmentValid(position, direction, endThere))
                {
                    return -1;
                }

                if (Map.RoadSegmentInWater(position, direction))
                {
                    test |= Misc.Bit(1);
                }
                else
                {
                    test |= Misc.Bit(0);
                }

                position = Map.Move(position, direction);

                // Check that owner is correct, and that only the destination has a flag. 
                if (!Map.HasOwner(position) || Map.GetOwner(position) != player.Index ||
                    (Map.HasFlag(position) && directionCounter != directionCount))
                {
                    return 0;
                }
            }

            destination = position;

            // Bit 0 indicates a ground path, bit 1 indicates
            // a water path. Abort if path went through both
            // ground and water.
            bool inWater = false;

            if (Misc.BitTest(test, 1))
            {
                inWater = true;

                if (Misc.BitTest(test, 0))
                    return 0;
            }

            water = inWater;

            return 1;
        }

        /// <summary>
        /// Check whether flag can be demolished.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        internal bool CanDemolishFlag(MapPos position, Player player)
        {
            if (Map.GetObject(position) != Map.Object.Flag)
                return false;

            var flag = Flags[Map.GetObjectIndex(position)];

            if (flag.HasBuilding)
            {
                return false;
            }

            if (Map.Paths(position) == 0)
                return true;

            if (flag.Player != player.Index)
                return false;

            return flag.CanDemolish();
        }

        /// <summary>
        /// Check whether road can be demolished.
        /// </summary>
        internal bool CanDemolishRoad(MapPos position, Player player)
        {
            if (!Map.HasOwner(position) || Map.GetOwner(position) != player.Index)
            {
                return false;
            }

            if (Map.Paths(position) == 0 ||
                Map.HasFlag(position) ||
                Map.HasBuilding(position))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Construct a road specified by a source and a list of directions.
        /// </summary>
        /// <param name="road"></param>
        /// <param name="player"></param>
        /// <param name="roadEndsThere"></param>
        /// <returns></returns>
        internal bool BuildRoad(Road road, Player player, bool roadEndsThere = false)
        {
            if (road.Length == 0)
                return false;

            uint destination = 0;
            bool waterPath = false;

            if (CanBuildRoad(road, player, ref destination, ref waterPath, roadEndsThere) == 0)
            {
                return false;
            }

            if (!Map.HasFlag(destination))
                return false;

            var directions = road.Directions;
            var outDirection = directions.Last();
            var inDirection = directions.Peek().Reverse();

            // Actually place road segments 
            if (!Map.PlaceRoadSegments(road))
                return false;

            // Connect flags 
            var sourceFlag = GetFlagAtPosition(road.StartPosition);
            var destinationFlag = GetFlagAtPosition(destination);

            sourceFlag.LinkWithFlag(destinationFlag, waterPath, inDirection, outDirection, road);

            return true;
        }

        // Build flag at position. 
        internal bool BuildFlag(MapPos position, Player player)
        {
            if (!CanBuildFlag(position, player))
            {
                return false;
            }

            var flag = Flags.Allocate();

            if (flag == null)
                return false;

            flag.Player = player.Index;
            flag.Position = position;
            Map.SetObject(position, Map.Object.Flag, (int)flag.Index);

            if (Map.Paths(position) != 0)
            {
                BuildFlagSplitPath(position);
            }

            return true;
        }

        // Build building at position. 
        internal bool BuildBuilding(MapPos position, Building.Type type, Player player)
        {
            if (!CanBuildBuilding(position, type, player))
            {
                return false;
            }

            if (type == Building.Type.Stock)
            {
                // TODO Check that more stocks are allowed to be built 
            }

            var building = Buildings.Allocate();

            if (building == null)
            {
                return false;
            }

            var flagPosition = Map.MoveDownRight(position);
            var flag = GetFlagAtPosition(flagPosition);

            // TODO: Sometimes there is no flag but a path in only one direction. This should not happen.

            if (flag == null)
            {
                if (!BuildFlag(flagPosition, player))
                {
                    Buildings.Erase(building.Index);
                    return false;
                }

                flag = GetFlagAtPosition(flagPosition);
            }

            uint flagIndex = flag.Index;

            building.Level = (uint)GetLevelingHeight(position);
            building.Position = position;

            var mapObject = building.StartBuilding(type);
            player.BuildingFounded(building);

            bool splitPath = false;

            if (Map.GetObject(flagPosition) != Map.Object.Flag)
            {
                flag.Player = player.Index;
                splitPath = Map.Paths(flagPosition) != 0;
            }
            else
            {
                flagIndex = Map.GetObjectIndex(flagPosition);
                flag = Flags[flagIndex];
            }

            flag.Position = flagPosition;
            building.LinkFlag(flagIndex);
            flag.LinkBuilding(building);

            flag.ClearFlags();

            Map.ClearIdleSerf(position);

            Map.SetObject(position, mapObject, (int)building.Index);
            Map.AddPath(position, Direction.DownRight);

            if (Map.GetObject(flagPosition) != Map.Object.Flag)
            {
                Map.SetObject(flagPosition, Map.Object.Flag, (int)flagIndex);
            }

            Map.AddPath(flagPosition, Direction.UpLeft);

            if (splitPath)
                BuildFlagSplitPath(flagPosition);

            return true;
        }

        // Build castle at position. 
        internal bool BuildCastle(MapPos position, Player player)
        {
            if (!CanBuildCastle(position, player))
            {
                return false;
            }

            var inventory = Inventories.Allocate();

            if (inventory == null)
            {
                return false;
            }

            var castle = Buildings.Allocate();

            if (castle == null)
            {
                Inventories.Erase(inventory.Index);
                return false;
            }

            var flag = Flags.Allocate();

            if (flag == null)
            {
                Buildings.Erase(castle.Index);
                Inventories.Erase(inventory.Index);
                return false;
            }

            castle.Inventory = inventory;

            inventory.Building = castle.Index;
            inventory.Flag = flag.Index;
            inventory.Player = player.Index;
            inventory.ApplySuppliesPreset(player.InitialSupplies);

            AddGoldTotal((int)inventory.GetCountOf(Resource.Type.GoldBar));
            AddGoldTotal((int)inventory.GetCountOf(Resource.Type.GoldOre));

            castle.Position = position;
            flag.Position = Map.MoveDownRight(position);
            castle.Player = player.Index;
            castle.StartBuilding(Building.Type.Castle);

            flag.Player = player.Index;
            flag.SetAcceptsSerfs(true);
            flag.SetHasInventory();
            flag.SetAcceptsResources(true);
            castle.LinkFlag(flag.Index);
            flag.LinkBuilding(castle);

            // Level land in hexagon below castle 
            uint height = (uint)GetLevelingHeight(position);
            Map.SetHeight(position, height);

            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                Map.SetHeight(Map.Move(position, direction), height);
            }

            Map.SetObject(position, Map.Object.Castle, (int)castle.Index);
            Map.AddPath(position, Direction.DownRight);

            Map.SetObject(Map.MoveDownRight(position), Map.Object.Flag, (int)flag.Index);
            Map.AddPath(Map.MoveDownRight(position), Direction.UpLeft);

            UpdateLandOwnership(position);

            player.BuildingFounded(castle);

            castle.UpdateMilitaryFlagState();

            player.CastlePosition = position;

            return true;
        }

        // Demolish road at position. 
        internal bool DemolishRoad(MapPos position, Player player)
        {
            if (!CanDemolishRoad(position, player))
                return false;

            return DemolishRoad(position);
        }

        internal bool DemolishFlag(MapPos position, Player player)
        {
            if (!CanDemolishFlag(position, player))
                return false;

            return DemolishFlag(position);
        }

        // Demolish building at position. 
        internal bool DemolishBuilding(MapPos position, Player player)
        {
            var building = Buildings[Map.GetObjectIndex(position)];

            if (building.Player != player.Index)
                return false;

            if (building.IsBurning)
                return false;

            return DemolishBuilding(position);
        }

        internal void SetInventoryResourceMode(Inventory inventory, Inventory.Mode mode)
        {
            var flag = Flags[inventory.Flag];

            inventory.ResourceMode = mode;

            if (mode > 0)
            {
                flag.SetAcceptsResources(false);

                // Clear destination of serfs with resources destined
                // for this inventory.
                var destination = flag.Index;

                foreach (var serf in Serfs.ToList())
                {
                    serf.ClearDestination2(destination);
                }
            }
            else
            {
                flag.SetAcceptsResources(true);
            }
        }

        internal void SetInventorySerfMode(Inventory inventory, Inventory.Mode mode)
        {
            var flag = Flags[inventory.Flag];

            inventory.SerfMode = mode;

            if (mode > 0)
            {
                flag.SetAcceptsSerfs(false);

                // Clear destination of serfs destined for this inventory. 
                var destination = flag.Index;

                foreach (var serf in Serfs.ToList())
                {
                    serf.ClearDestination(destination);
                }
            }
            else
            {
                flag.SetAcceptsSerfs(true);
            }
        }

        #endregion


        #region Internal interface

        // Initialize land ownership for whole map. 
        void InitLandOwnership()
        {
            foreach (var building in Buildings.ToList())
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
            0, 1, 2, 4, 7, 12, 18, 29, -1, -1,      // hut 
            0, 3, 5, 8, 11, 15, 22, 30, -1, -1,     // tower 
            0, 6, 10, 14, 19, 23, 27, 31, -1, -1    // fortress 
        };

        static readonly int[] mapCloseness = new int[]
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

        // Update land ownership around map position. 
        internal void UpdateLandOwnership(MapPos position)
        {
            // Currently the below algorithm will only work when
            // both influence_radius and calculate_radius are 8.
            const int influenceRadius = 8;
            const int influenceDiameter = 1 + 2 * influenceRadius;

            int calculateRadius = influenceRadius;
            int calculateDiameter = 1 + 2 * calculateRadius;

            int tempArraySize = calculateDiameter * calculateDiameter * Players.Size;
            var tempArray = new int[tempArraySize];

            // Find influence from buildings in 33*33 square
            // around the center.
            for (int i = -(influenceRadius + calculateRadius);
                 i <= influenceRadius + calculateRadius; ++i)
            {
                for (int j = -(influenceRadius + calculateRadius);
                     j <= influenceRadius + calculateRadius; ++j)
                {
                    var checkPosition = Map.PositionAdd(position, j, i);

                    if (Map.HasBuilding(checkPosition))
                    {
                        var building = GetBuildingAtPosition(checkPosition);
                        int militaryType = -1;

                        if (building.BuildingType == Building.Type.Castle)
                        {
                            // Castle has military influence even when not done. 
                            militaryType = 2;
                        }
                        else if (building.IsDone && building.IsActive)
                        {
                            switch (building.BuildingType)
                            {
                                case Building.Type.Hut: militaryType = 0; break;
                                case Building.Type.Tower: militaryType = 1; break;
                                case Building.Type.Fortress: militaryType = 2; break;
                                default: break;
                            }
                        }

                        if (militaryType >= 0 && !building.IsBurning)
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

            // Update owner of 17*17 square. 
            for (int i = -calculateRadius; i <= calculateRadius; ++i)
            {
                for (int j = -calculateRadius; j <= calculateRadius; ++j)
                {
                    int maxValue = 0;
                    int playerIndex = -1;

                    foreach (var player in Players.ToList())
                    {
                        int arrayIndex = (int)player.Index * calculateDiameter * calculateDiameter +
                          calculateDiameter * (i + calculateRadius) + (j + calculateRadius);

                        if (tempArray[arrayIndex] > maxValue)
                        {
                            maxValue = tempArray[arrayIndex];
                            playerIndex = (int)player.Index;
                        }
                    }

                    var checkPosition = Map.PositionAdd(position, j, i);
                    int oldPlayer = -1;

                    if (Map.HasOwner(checkPosition))
                        oldPlayer = (int)Map.GetOwner(checkPosition);

                    if (oldPlayer >= 0 && playerIndex != oldPlayer)
                    {
                        Players[(uint)oldPlayer].DecreaseLandArea();
                        SurrenderLand(checkPosition);
                    }

                    if (playerIndex >= 0)
                    {
                        if (playerIndex != oldPlayer)
                        {
                            Players[(uint)playerIndex].IncreaseLandArea();
                            Map.SetOwner(checkPosition, (uint)playerIndex);
                        }
                    }
                    else
                    {
                        Map.DeleteOwner(checkPosition);
                    }
                }
            }

            // Update borders of 18*18 square. 
            for (int i = -calculateRadius - 1; i <= calculateRadius; ++i)
            {
                for (int j = -calculateRadius - 1; j <= calculateRadius; ++j)
                {
                    UpdateBorders(Map.PositionAdd(position, j, i));
                }
            }

            // Update military building flag state. 
            for (int i = -25; i <= 25; ++i)
            {
                for (int j = -25; j <= 25; ++j)
                {
                    var checkPosition = Map.PositionAdd(position, i, j);

                    if (Map.GetObject(checkPosition) >= Map.Object.SmallBuilding &&
                        Map.GetObject(checkPosition) <= Map.Object.Castle &&
                        Map.HasPath(checkPosition, Direction.DownRight))
                    {
                        var building = Buildings[Map.GetObjectIndex(checkPosition)];

                        if (building.IsDone && building.IsMilitary())
                        {
                            building.UpdateMilitaryFlagState();
                        }
                    }
                }
            }
        }

        void UpdateBorders(MapPos position)
        {
            var cycle = new DirectionCycleCW(Direction.Right, 3);

            foreach (var direction in cycle)
            {
                long index = Render.RenderBorderSegment.CreateIndex(position, direction);

                if (Map.HasOwner(position) != Map.HasOwner(Map.Move(position, direction)) ||
                    Map.GetOwner(position) != Map.GetOwner(Map.Move(position, direction)))
                {
                    if (!renderBorderSegments.ContainsKey(index))
                    {
                        if (!renderBorderSegments.TryAdd(index, new Render.RenderBorderSegment(Map, position, direction,
                            renderView.GetLayer(Layer.Objects), renderView.SpriteFactory, renderView.DataSource)))
                        {
                            throw new ExceptionFreeserf(ErrorSystemType.Application, "Unable to add render border segment.");
                        }
                    }
                }
                else
                {
                    if (renderBorderSegments.TryRemove(index, out var renderBorderSegment) && renderBorderSegment != null)
                        renderBorderSegment.Delete();
                }
            }
        }

        /// <summary>
        /// The given building has been defeated and is being
        /// occupied by player.
        /// </summary>
        /// <param name="building"></param>
        /// <param name="playerIndex"></param>
        internal void OccupyEnemyBuilding(Building building, uint playerIndex)
        {
            // Take the building. 
            var player = Players[playerIndex];

            player.BuildingCaptured(building);

            if (building.BuildingType == Building.Type.Castle)
            {
                DemolishBuilding(building.Position);
            }
            else
            {
                var flag = Flags[building.FlagIndex];
                FlagResetTransport(flag);

                // Demolish nearby buildings. 
                for (uint i = 0; i < 12; ++i)
                {
                    var position = Map.PositionAddSpirally(building.Position, 7u + i);

                    if (Map.GetObject(position) >= Map.Object.SmallBuilding &&
                        Map.GetObject(position) < Map.Object.Castle)
                    {
                        DemolishBuilding(position);
                    }
                }

                // Change owner of land and remove roads and flags
                // except the flag associated with the building. */
                Map.SetOwner(building.Position, playerIndex);

                var cycle = DirectionCycleCW.CreateDefault();

                foreach (var direction in cycle)
                {
                    var position = Map.Move(building.Position, direction);

                    Map.SetOwner(position, playerIndex);

                    if (position != flag.Position)
                    {
                        DemolishFlagAndRoads(position);
                    }
                }

                // Change owner of flag. 
                flag.Player = playerIndex;

                // Reset destination of stolen resources. 
                flag.ResetDestinationOfStolenResources();

                // Remove paths from flag. 
                cycle = DirectionCycleCW.CreateDefault();

                foreach (var direction in cycle)
                {
                    if (flag.HasPath(direction))
                    {
                        DemolishRoad(Map.Move(flag.Position, direction));
                    }
                }

                UpdateLandOwnership(building.Position);
            }
        }

        /// <summary>
        /// Cancel a resource being transported to destination. This
        /// ensures that the destination can request a new resource.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="destination"></param>
        internal void CancelTransportedResource(Resource.Type type, uint destination)
        {
            if (destination == 0)
            {
                return;
            }

            var flag = Flags[destination];

            if (flag == null || !flag.HasBuilding)
            {
                // the flag and/or building might have gone
                return;
            }

            flag.Building?.CancelTransportedResource(type);
        }

        /// <summary>
        /// Called when a resource is lost forever from the game. This will
        /// update any global state keeping track of that resource.
        /// </summary>
        /// <param name="type"></param>
        internal void LoseResource(Resource.Type type)
        {
            if (type == Resource.Type.GoldOre || type == Resource.Type.GoldBar)
            {
                AddGoldTotal(-1);
            }
        }

        internal ushort RandomInt()
        {
            return state.Random.Next();
        }

        internal Random GetRandom()
        {
            return state.Random;
        }

        // Dispatch serf from (nearest?) inventory to flag. 
        internal bool SendSerfToFlag(Flag destination, Serf.Type type, Resource.Type resource1, Resource.Type resource2)
        {
            Building building = null;

            if (destination.HasBuilding)
            {
                building = destination.Building;
            }

            int serfType = (int)type;

            // If type is negative, building is non-null. 
            if (serfType < 0 && building != null)
            {
                var player = Players[building.Player];
                serfType = player.GetCyclingSerfType(type);
            }

            var data = new SendSerfToFlagData();
            data.Inventory = null;
            data.Building = building;
            data.SerfType = serfType;
            data.DestIndex = (int)destination.Index;
            data.Resource1 = resource1;
            data.Resource2 = resource2;

            if (!FlagSearch.Single(destination, SendSerfToFlagSearchCallback, true, false, data))
            {
                return false;
            }
            else if (data.Inventory != null)
            {
                var inventory = data.Inventory;
                var serf = inventory.CallOutSerf(Serf.Type.Generic);

                if (type < 0 && building != null)
                {
                    // Knight 
                    building.KnightRequestGranted();

                    serf.SerfType = Serf.Type.Knight0;
                    AddKnight(serf);
                    serf.GoOutFromInventory(inventory.Index, building.FlagIndex, -1);

                    inventory.PopResource(Resource.Type.Sword);
                    inventory.PopResource(Resource.Type.Shield);
                }
                else
                {
                    if (serfType < 0) // safety check
                    {
                        return false;
                    }

                    serf.SerfType = (Serf.Type)serfType;

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

                    serf.GoOutFromInventory(inventory.Index, destination.Index, mode);

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
            return state.PlayerHistoryIndex[scale];
        }

        internal int GetResourceHistoryIndex()
        {
            return state.ResourceHistoryIndex;
        }

        internal int NextSearchId()
        {
            ++state.FlagSearchCounter;

            // If we're back at zero the counter has overflown,
            // everything needs a reset to be safe.
            if (state.FlagSearchCounter == 0)
            {
                ++state.FlagSearchCounter;
                ClearSearchId();
            }

            return state.FlagSearchCounter;
        }

        internal Serf CreateSerf(int index = -1)
        {
            if (index == -1)
            {
                return Serfs.Allocate();
            }
            else
            {
                return Serfs.GetOrInsert((uint)index);
            }
        }

        internal void DeleteSerf(Serf serf)
        {
            if (Map != null && serf.Position != Global.INVALID_MAPPOS && Map.GetSerfIndex(serf.Position) == serf.Index)
                Map.SetSerfIndex(serf.Position, 0);

            if (serf.IsKnight)
                RemoveKnight(serf);

            RemoveSerfFromDrawing(serf);

            Serfs.Erase(serf.Index);
        }

        internal Flag CreateFlag(int index = -1)
        {
            if (index == -1)
            {
                return Flags.Allocate();
            }
            else
            {
                return Flags.GetOrInsert((uint)index);
            }
        }

        internal Inventory CreateInventory(int index = -1)
        {
            if (index == -1)
            {
                return Inventories.Allocate();
            }
            else
            {
                return Inventories.GetOrInsert((uint)index);
            }
        }

        internal void DeleteInventory(uint index)
        {
            Inventories.Erase(index);
        }

        internal void DeleteInventory(Inventory inventory)
        {
            DeleteInventory(inventory.Index);
        }

        internal Building CreateBuilding(int index = -1)
        {
            if (index == -1)
            {
                return Buildings.Allocate();
            }
            else
            {
                return Buildings.GetOrInsert((uint)index);
            }
        }

        internal void DeleteBuilding(Building building)
        {
            if (building.Position != Global.INVALID_MAPPOS)
                Map?.SetObject(building.Position, Map.Object.None, 0);

            if (renderBuildings.TryRemove(building, out var renderBuilding) && renderBuilding != null)
            {
                renderBuildingsInProgress.Remove(renderBuilding);
                renderBuilding.Delete();
            }

            Buildings.Erase(building.Index);
        }

        internal void DeleteFlag(Flag flag)
        {
            if (flag.Position != Global.INVALID_MAPPOS)
                Map?.SetObject(flag.Position, Map.Object.None, 0);

            if (renderFlags.TryRemove(flag, out var renderFlag) && renderFlag != null)
                renderFlag.Delete();

            Flags.Erase(flag.Index);
        }

        internal Serf GetSerf(uint index)
        {
            return Serfs[index];
        }

        internal Flag GetFlag(uint index)
        {
            return Flags[index];
        }

        internal Inventory GetInventory(uint index)
        {
            return Inventories[index];
        }

        internal Building GetBuilding(uint index)
        {
            return Buildings[index];
        }

        internal Player GetPlayer(uint index)
        {
            return Players[index];
        }

        public int PlayerCount => Players.Size;

        internal int GetFreeKnightCount(Player player)
        {
            return Math.Max(0, knights[player.Index].Count(k => k.SerfState == Serf.State.IdleInStock)
                - ((int)player.CastleKnightsWanted - (int)player.CastleKnights));
        }

        internal int GetPossibleFreeKnightCount(Player player)
        {
            int count = GetFreeKnightCount(player);

            count += (int)Math.Min(GetResourceAmountInInventories(player, Resource.Type.Sword), GetResourceAmountInInventories(player, Resource.Type.Shield));
            count -= (int)player.GetIncompleteBuildingCount(Building.Type.Hut);
            count -= (int)player.GetIncompleteBuildingCount(Building.Type.Tower);
            count -= (int)player.GetIncompleteBuildingCount(Building.Type.Fortress);
            count -= player.Game.GetPlayerBuildings(player).Count(building => building.IsMilitary(false) && !building.HasKnight());

            return Math.Max(0, count);
        }

        // Checks if at least one of the given building is completed or all
        // the required materials are at a buildings spot.
        internal bool HasAnyOfBuildingCompletedOrMaterialsAtPlace(Player player, Building.Type type)
        {
            if (player.GetCompletedBuildingCount(type) != 0)
                return true;

            return GetPlayerBuildings(player, type).Any(building => building.HasAllConstructionMaterialsAtLocation());
        }

        internal IEnumerable<Serf> GetPlayerSerfs(Player player)
        {
            return Serfs.Where(serf => serf.Player == player.Index).ToList();
        }

        internal IEnumerable<Building> GetPlayerBuildings(Player player)
        {
            return Buildings.Where(building => building.Player == player.Index).ToList();
        }

        internal IEnumerable<Building> GetPlayerBuildings(Player player, Building.Type type)
        {
            return GetPlayerBuildings(player).Where(building => building.BuildingType == type).ToList();
        }

        internal IEnumerable<Flag> GetPlayerFlags(Player player)
        {
            return Flags.Where(flag => flag.Player == player.Index).ToList();
        }

        internal IEnumerable<Serf> GetSerfsInInventory(Inventory inventory)
        {
            return Serfs.Where(serf => serf.SerfState == Serf.State.IdleInStock && inventory.Index == serf.IdleInStockInventoryIndex).ToList();
        }

        internal List<Serf> GetSerfsRelatedTo(uint destination, Direction direction)
        {
            return Serfs.Where(serf => serf.IsRelatedTo(destination, direction)).ToList();
        }

        internal IEnumerable<Inventory> GetPlayerInventories(Player player)
        {
            return Inventories.Where(inventory => inventory.Player == player.Index).ToList();
        }

        internal IEnumerable<Serf> GetSerfsAtPosition(MapPos position)
        {
            return Serfs.Where(serf => serf.Position == position).ToList();
        }

        internal int FindInventoryWithValidSpecialist(Player player, Serf.Type serfType, Resource.Type resource1, Resource.Type resource2)
        {
            int inventoryWithResButNoGeneric = -1;

            foreach (var inventory in GetPlayerInventories(player))
            {
                if (inventory.HasSerf(serfType))
                    return (int)inventory.Index;

                if ((resource1 == Resource.Type.None || inventory.GetCountOf(resource1) > 0) &&
                    (resource2 == Resource.Type.None || inventory.GetCountOf(resource2) > 0))
                {
                    inventoryWithResButNoGeneric = 0xffff + (int)inventory.Index;

                    if (inventory.HasSerf(Serf.Type.Generic))
                        return (int)inventory.Index;
                }
            }

            return inventoryWithResButNoGeneric;
        }

        internal uint GetResourceAmountInInventories(Player player, Resource.Type type)
        {
            uint amount = 0;

            foreach (var inventory in GetPlayerInventories(player))
            {
                amount += inventory.GetCountOf(type);
            }

            return amount;
        }

        internal Player GetNextPlayer(Player player)
        {
            bool next = false;

            foreach (var nextPlayer in Players.ToList())
            {
                if (next)
                    return nextPlayer;

                if (nextPlayer == player)
                    next = true;
            }

            return Players.First;
        }

        internal uint GetEnemyScore(Player player)
        {
            uint enemyScore = 0;

            foreach (var enemy in Players.ToList())
            {
                if (player.Index != enemy.Index)
                {
                    enemyScore += enemy.TotalMilitaryScore;
                }
            }

            return enemyScore;
        }

        internal void BuildingCaptured(Building building)
        {
            // Save amount of land and buildings for each player 
            var landBefore = new Dictionary<int, uint>();
            var buildingsBefore = new Dictionary<int, uint>();

            foreach (var player in Players.ToList())
            {
                landBefore[(int)player.Index] = player.LandArea;
                buildingsBefore[(int)player.Index] = player.BuildingScore;
            }

            // Update land ownership 
            UpdateLandOwnership(building.Position);

            // Create notifications for lost land and buildings 
            foreach (var player in Players.ToList())
            {
                if (landBefore[(int)player.Index] > player.LandArea)
                {
                    player.AddNotification(Notification.Type.LostLand,
                                           building.Position,
                                           building.Player);
                }
            }
        }

        void ClearSearchId()
        {
            foreach (var flag in Flags.ToList())
            {
                flag.ClearSearchId();
            }
        }

        /// <summary>
        /// Clear the serf request bit of all flags and buildings.
        /// This allows the flag or building to try and request a
        /// serf again.
        /// </summary>
        void ClearSerfRequestFailure()
        {
            foreach (var building in Buildings.ToList())
            {
                building.ClearSerfRequestFailure();
            }

            foreach (var flag in Flags.ToList())
            {
                flag.SerfRequestClear();
            }
        }

        void UpdateKnightMorale()
        {
            foreach (var player in Players.ToList())
            {
                player.UpdateKnightMorale();
            }
        }

        class UpdateInventoriesData
        {
            public Resource.Type Resource;
            public int[] MaxPriority;
            public Flag[] Flags;
        }

        static bool UpdateInventoriesCb(Flag flag, object data)
        {
            var updateData = data as UpdateInventoriesData;
            int index = (int)flag.Tag;

            if (updateData.MaxPriority[index] < 255 && flag.HasBuilding)
            {
                var building = flag.Building;
                int buildingPriority = building.GetMaxPriorityForResource(updateData.Resource, 16);

                if (buildingPriority > updateData.MaxPriority[index])
                {
                    updateData.MaxPriority[index] = buildingPriority;
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

        /// <summary>
        /// Update inventories as part of the game progression. Moves the appropriate
        /// resources that are needed outside of the inventory into the out queue.
        /// </summary>
        void UpdateInventories()
        {
            Resource.Type[] resources = null;
            int arrayIndex = 0;

            // TODO: really use random to select the order? There seems to be no fixed order. Maybe use flag priorities of player?
            switch (RandomInt() & 7)
            {
                case 0:
                    resources = ResourceArray2;
                    break;
                case 1:
                    resources = ResourceArray3;
                    break;
                default:
                    resources = ResourceArray1;
                    break;
            }

            while (resources[arrayIndex] != Resource.Type.None)
            {
                foreach (var player in Players.ToList())
                {
                    var sourceInventories = new Inventory[256];
                    int sourceInventoryIndex = 0;

                    foreach (var inventory in Inventories.ToList())
                    {
                        if (inventory.Player == player.Index && !inventory.IsQueueFull())
                        {
                            var resourceMode = inventory.ResourceMode;

                            if (resourceMode == Inventory.Mode.In || resourceMode == Inventory.Mode.Stop)
                            {
                                if (resources[arrayIndex] == Resource.Type.GroupFood)
                                {
                                    if (inventory.HasFood())
                                    {
                                        sourceInventories[sourceInventoryIndex++] = inventory;

                                        if (sourceInventoryIndex == 256)
                                            break;
                                    }
                                }
                                else if (inventory.GetCountOf(resources[arrayIndex]) != 0)
                                {
                                    sourceInventories[sourceInventoryIndex++] = inventory;

                                    if (sourceInventoryIndex == 256)
                                        break;
                                }
                            }
                            else
                            {
                                // Out mode 
                                int priority = 0;
                                var type = Resource.Type.None;

                                for (int i = 0; i < 26; i++)
                                {
                                    if (inventory.GetCountOf((Resource.Type)i) != 0 &&
                                        player.GetInventoryPriority((Resource.Type)i) >= priority)
                                    {
                                        priority = player.GetInventoryPriority((Resource.Type)i);
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

                    if (sourceInventoryIndex == 0)
                        continue;

                    var search = new FlagSearch(this);
                    var maxPriority = new int[256];
                    var flags = new Flag[256];

                    for (int i = 0; i < sourceInventoryIndex; ++i)
                    {
                        var flag = this.Flags[sourceInventories[i].Flag];
                        // Note: it seems that SearchDirection was abused for indexing here but (Direction)i will not work with i >= 6.
                        // We added a general purpose tagged object for flags instead.
                        flag.Tag = i;
                        search.AddSource(flag);
                    }

                    var data = new UpdateInventoriesData
                    {
                        Resource = resources[arrayIndex],
                        MaxPriority = maxPriority,
                        Flags = flags
                    };
                    search.Execute(UpdateInventoriesCb, false, true, data);

                    for (int i = 0; i < sourceInventoryIndex; ++i)
                    {
                        if (maxPriority[i] > 0)
                        {
                            Log.Verbose.Write(ErrorSystemType.Game, $" dest for inventory {i} found");
                            var resource = resources[arrayIndex];

                            var destinationBuilding = flags[i].Building;

                            if (!destinationBuilding.AddRequestedResource(resource, false))
                            {
                                throw new ExceptionFreeserf(this, ErrorSystemType.Game, "Failed to request resource.");
                            }

                            // Put resource in out queue 
                            sourceInventories[i].AddToQueue(resource, destinationBuilding.FlagIndex);
                        }
                    }
                }

                ++arrayIndex;
            }
        }

        void UpdateMapObjects()
        {
            foreach (var renderObject in renderObjects.ToArray())
                renderObject.Value.Update(Tick, Map.RenderMap, renderObject.Key);
        }

        void UpdateFlags()
        {
            foreach (var flag in Flags.ToList())
            {
                flag.Update();

                if (flag.Index > 0u && renderFlags.ContainsKey(flag))
                    renderFlags[flag]?.Update(Tick, Map.RenderMap, flag.Position);
            }
        }

        void UpdateRoads()
        {
            foreach (var renderRoadSegment in renderRoadSegments.ToArray())
                renderRoadSegment.Value?.Update(Map.RenderMap);
        }

        void UpdateBorders()
        {
            foreach (var renderBorderSegment in renderBorderSegments.ToArray())
                renderBorderSegment.Value.Update(Map.RenderMap);
        }

        // This is called after loading a game.
        // As no roads are built manually in this case
        // we have to scan the map and add render objects
        // for all road segments.
        void PostLoadRoads()
        {
            foreach (var position in Map.Geometry)
            {
                if (Map.Paths(position) != 0)
                {
                    var cycle = new DirectionCycleCW(Direction.Right, 3);

                    foreach (var direction in cycle)
                    {
                        if (Map.HasPath(position, direction))
                            AddRoadSegment(position, direction);
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

        static bool SendSerfToFlagSearchCallback(Flag flag, object data)
        {
            if (!flag.HasInventory())
            {
                return false;
            }

            var sendData = data as SendSerfToFlagData;

            // Inventory reached
            var building = flag.Building;
            var inventory = building.Inventory;

            int type = sendData.SerfType;

            if (type < 0)
            {
                int knightType = -1;

                for (int i = 4; i >= -type - 1; --i)
                {
                    if (inventory.HasSerf((Serf.Type)((int)Serf.Type.Knight0 + i)))
                    {
                        knightType = i;
                        break;
                    }
                }

                if (knightType >= 0)
                {
                    // Knight of appropriate type was found.
                    var serf = inventory.CallOutSerf((Serf.Type)((int)Serf.Type.Knight0 + knightType));

                    sendData.Building.KnightRequestGranted();

                    serf.GoOutFromInventory(inventory.Index, sendData.Building.FlagIndex, -1);

                    return true;
                }
                else if (type == -1)
                {
                    // See if a knight can be created here.
                    if (inventory.HasSerf(Serf.Type.Generic) &&
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
                if (inventory.HasSerf((Serf.Type)type))
                {
                    if (type != (int)Serf.Type.Generic || inventory.FreeSerfCount > 4)
                    {
                        var serf = inventory.CallOutSerf((Serf.Type)type);
                        int mode;

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
                            var destinationBuilding = flag.Game.Flags[(uint)sendData.DestIndex].Building;
                            destinationBuilding.SerfRequestGranted();
                            mode = -1;
                        }

                        serf.GoOutFromInventory(inventory.Index, (uint)sendData.DestIndex, mode);

                        return true;
                    }
                }
                else
                {
                    if (sendData.Inventory == null &&
                        inventory.HasSerf(Serf.Type.Generic) &&
                        (sendData.Resource1 == Resource.Type.None || inventory.GetCountOf(sendData.Resource1) > 0) &&
                        (sendData.Resource2 == Resource.Type.None || inventory.GetCountOf(sendData.Resource2) > 0))
                    {
                        sendData.Inventory = inventory;
                        // player_t *player = globals.player[SERF_PLAYER(serf)];
                        // game.field_340 = player.cont_search_after_non_optimal_find;
                        return true;
                    }
                }
            }

            return false;
        }

        void UpdateBuildings()
        {
            // Note: Do not use foreach here as building.Update()
            // may delete the building and therefore change the
            // collection while we iterate through it!

            // Therefore we use a copied list here.
            var buildingList = Buildings.ToList();

            for (int i = 0; i < buildingList.Count; ++i)
            {
                buildingList[i].Update(Tick);

                if (buildingList[i].Index > 0u)
                {
                    if (renderBuildings.ContainsKey(buildingList[i]))
                    {
                        // if a building burns we have to update its rendering so ensure that is is updated when visible
                        if (buildingList[i].IsBurning)
                        {
                            if (renderBuildings[buildingList[i]].Visible)
                            {
                                if (!renderBuildingsInProgress.Contains(renderBuildings[buildingList[i]]))
                                    renderBuildingsInProgress.Add(renderBuildings[buildingList[i]]);
                            }
                        }

                        renderBuildings[buildingList[i]].Update(Tick, Map.RenderMap, buildingList[i].Position);
                    }
                }
            }

            for (int i = renderBuildingsInProgress.Count - 1; i >= 0; --i)
            {
                if (!renderBuildingsInProgress[i].UpdateProgress()) // no more updating needed
                    renderBuildingsInProgress.RemoveAt(i);
            }
        }

        void UpdateSerfs()
        {
            // Note: Do not use foreach here as serf.Update()
            // may delete the serf and therefore change the
            // collection while we iterate through it!

            // Therefore we use a copied list here.
            var serfList = Serfs.ToList();

            for (int i = 0; i < serfList.Count; ++i)
            {
                serfList[i].Update();

                if (serfList[i].Index > 0u && renderSerfs.ContainsKey(serfList[i]))
                {
                    renderSerfs[serfList[i]].Update(this, renderView.DataSource, Tick, Map, serfList[i].Position);
                }
            }
        }

        void RecordPlayerHistory(int maxLevel, int aspect, int[] historyIndex, Values values)
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
                    Players[value.Key].SetPlayerStatHistory(mode, index, (uint)(100ul * value.Value / total));
                }
            }
        }

        /* Calculate whether one player has enough advantage to be
           considered a clear winner regarding one aspect.
           Return -1 if there is no clear winner. */
        int CalculateClearWinner(Values values)
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

        // Update statistics of the game. 
        void UpdateGameStats()
        {
            var playerList = Players.ToList();

            if ((int)state.GameStatsCounter > tickDifference)
            {
                state.GameStatsCounter -= (uint)tickDifference;
            }
            else
            {
                state.GameStatsCounter += (uint)(1500 - tickDifference);
                playerScoreLeader = 0;

                int updateLevel = 0;

                // Update first level index 
                state.PlayerHistoryIndex[0] = state.PlayerHistoryIndex[0] + 1 < 112 ? state.PlayerHistoryIndex[0] + 1 : 0;
                --state.PlayerHistoryCounter[0];

                if (state.PlayerHistoryCounter[0] < 0)
                {
                    updateLevel = 1;
                    state.PlayerHistoryCounter[0] = 3;

                    // Update second level index 
                    state.PlayerHistoryIndex[1] = state.PlayerHistoryIndex[1] + 1 < 112 ? state.PlayerHistoryIndex[1] + 1 : 0;
                    --state.PlayerHistoryCounter[1];

                    if (state.PlayerHistoryCounter[1] < 0)
                    {
                        updateLevel = 2;
                        state.PlayerHistoryCounter[1] = 4;

                        // Update third level index 
                        state.PlayerHistoryIndex[2] = state.PlayerHistoryIndex[2] + 1 < 112 ? state.PlayerHistoryIndex[2] + 1 : 0;
                        --state.PlayerHistoryCounter[2];

                        if (state.PlayerHistoryCounter[2] < 0)
                        {
                            updateLevel = 3;
                            state.PlayerHistoryCounter[2] = 4;

                            // Update fourth level index 
                            state.PlayerHistoryIndex[3] = state.PlayerHistoryIndex[3] + 1 < 112 ? state.PlayerHistoryIndex[3] + 1 : 0;
                        }
                    }
                }

                var values = new Values();

                // Store land area stats in history. 
                foreach (var player in playerList)
                {
                    values[player.Index] = player.LandArea;
                }

                RecordPlayerHistory(updateLevel, 1, state.PlayerHistoryIndex, values);

                int clearWinner = CalculateClearWinner(values);

                if (clearWinner != -1)
                    playerScoreLeader |= Misc.Bit(clearWinner);

                // Store building stats in history. 
                foreach (var player in playerList)
                {
                    values[player.Index] = player.BuildingScore;
                }

                RecordPlayerHistory(updateLevel, 2, state.PlayerHistoryIndex, values);

                // Store military stats in history. 
                foreach (var player in playerList)
                {
                    values[player.Index] = player.MilitaryScore;
                }

                RecordPlayerHistory(updateLevel, 3, state.PlayerHistoryIndex, values);

                clearWinner = CalculateClearWinner(values);

                if (clearWinner != -1)
                    playerScoreLeader |= Misc.Bit(clearWinner) << 4;

                // Store condensed score of all aspects in history. 
                foreach (var player in playerList)
                {
                    values[player.Index] = player.Score;
                }

                RecordPlayerHistory(updateLevel, 0, state.PlayerHistoryIndex, values);

                // TODO Determine winner based on game.player_score_leader 
            }

            if ((int)state.HistoryCounter > tickDifference)
            {
                state.HistoryCounter -= (uint)tickDifference;
            }
            else
            {
                state.HistoryCounter += (uint)(6000 - tickDifference);

                int index = state.ResourceHistoryIndex;

                for (int resource = 0; resource < 26; ++resource)
                {
                    foreach (var player in playerList)
                    {
                        player.UpdateStats(resource, index);
                    }
                }

                state.ResourceHistoryIndex = index + 1 < 120 ? index + 1 : 0;
            }
        }

        // Generate an estimate of the amount of resources in the ground at map position.
        void GetResourceEstimate(MapPos position, uint weight, uint[] estimates)
        {
            if ((Map.GetObject(position) == Map.Object.None ||
                Map.GetObject(position) >= Map.Object.Tree0) &&
                Map.GetResourceType(position) != Map.Minerals.None)
            {
                uint value = weight * Map.GetResourceAmount(position);
                estimates[(int)Map.GetResourceType(position)] += value;
            }
        }

        bool RoadSegmentInWater(MapPos position, Direction direction)
        {
            if (direction > Direction.Down)
            {
                position = Map.Move(position, direction);
                direction = direction.Reverse();
            }

            bool water = false;

            switch (direction)
            {
                case Direction.Right:
                    if (Map.TypeDown(position) <= Map.Terrain.Water3 &&
                        Map.TypeUp(Map.MoveUp(position)) <= Map.Terrain.Water3)
                    {
                        water = true;
                    }
                    break;
                case Direction.DownRight:
                    if (Map.TypeUp(position) <= Map.Terrain.Water3 &&
                        Map.TypeDown(position) <= Map.Terrain.Water3)
                    {
                        water = true;
                    }
                    break;
                case Direction.Down:
                    if (Map.TypeUp(position) <= Map.Terrain.Water3 &&
                        Map.TypeDown(Map.MoveLeft(position)) <= Map.Terrain.Water3)
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

        internal void FlagResetTransport(Flag flag)
        {
            // Clear destination for any serf with resources for this flag. 
            foreach (var serf in Serfs.ToList())
            {
                serf.ResetTransport(flag);
            }

            // Flag. 
            foreach (var otherFlag in Flags.ToList())
            {
                flag.ResetTransport(otherFlag);
            }

            // Inventories
            foreach (var inventory in Inventories.ToList())
            {
                inventory.ResetQueueForDest(flag);
            }
        }

        void BuildingRemovePlayerRefs(Building building)
        {
            foreach (var player in Players.ToList())
            {
                if (player.SelectedObjectIndex == building.Index)
                {
                    player.SelectedObjectIndex = 0;
                }
            }
        }

        bool PathSerfIdleToWaitState(MapPos position)
        {
            // Look through serf array for the corresponding serf. 
            foreach (var serf in Serfs.ToList())
            {
                if (serf.IdleToWaitState(position))
                {
                    return true;
                }
            }

            return false;
        }

        void RemoveRoadForwards(MapPos position, Direction direction)
        {
            var inDirection = Direction.None;

            while (true)
            {
                if (Map.GetIdleSerf(position))
                {
                    PathSerfIdleToWaitState(position);
                }

                if (Map.HasSerf(position))
                {
                    var serf = GetSerfAtPosition(position);

                    if (!Map.HasFlag(position))
                    {
                        serf.SetLostState();
                    }
                    else
                    {
                        // Handle serf close to flag, where
                        // it should only be lost if walking
                        // in the wrong direction.
                        int walkingDirection = serf.WalkingDirection;

                        if (walkingDirection < 0)
                            walkingDirection += 6;

                        if (direction != Direction.None && walkingDirection == (int)direction.Reverse())
                        {
                            serf.SetLostState();
                        }
                    }
                }

                if (Map.HasFlag(position))
                {
                    if (inDirection == Direction.None)
                        inDirection = direction;

                    var flag = Flags[Map.GetObjectIndex(position)];
                    flag.DeletePath(inDirection.Reverse());
                    break;
                }

                RemoveRoadSegment(position, direction);

                inDirection = direction;
                direction = Map.RemoveRoadSegment(ref position, direction);
            }
        }

        internal void RemoveRoad(Road road)
        {
            if (!road.Valid)
                return;

            // NOTE: This will not remove the end flags!

            // move one path away from start flag and then call DemolishRoad which
            // will remove a whole road when given an inside path segment position
            DemolishRoad(Map.Move(road.StartPosition, road.Directions.Last()));
        }

        bool DemolishRoad(MapPos position)
        {
            if (!Map.RemoveRoadBackrefs(position))
            {
                // TODO 
                return false;
            }

            // Find directions of path segments to be split. 
            var path1Direction = Direction.None;
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                if (Map.HasPath(position, direction))
                {
                    path1Direction = direction;
                    break;
                }
            }

            var path2Direction = Direction.None;

            for (int direction = (int)path1Direction + 1; direction <= (int)Direction.Up; ++direction)
            {
                if (Map.HasPath(position, (Direction)direction))
                {
                    path2Direction = (Direction)direction;
                    break;
                }
            }

            // If last segment direction is UP LEFT it could
            // be to a building and the real path is at UP.
            if (path2Direction == Direction.UpLeft && Map.HasPath(position, Direction.Up))
            {
                path2Direction = Direction.Up;
            }

            RemoveRoadForwards(position, path1Direction);
            RemoveRoadForwards(position, path2Direction);

            return true;
        }

        /// <summary>
        /// Build flag on existing path. Path must be split in two segments.
        /// </summary>
        /// <param name="position"></param>
        internal void BuildFlagSplitPath(MapPos position)
        {
            // Find directions of path segments to be split. 
            var path1Direction = Direction.None;
            var cycle = DirectionCycleCW.CreateDefault();
            var iter = cycle.Begin() as Iterator<Direction>;

            for (; iter != cycle.End(); ++iter)
            {
                if (Map.HasPath(position, iter.Current))
                {
                    path1Direction = iter.Current;
                    break;
                }
            }

            var path2Direction = Direction.None;
            ++iter;

            for (; iter != cycle.End(); ++iter)
            {
                if (Map.HasPath(position, iter.Current))
                {
                    path2Direction = iter.Current;
                    break;
                }
            }

            // If last segment direction is UP LEFT it could
            // be to a building and the real path is at UP.
            if (path2Direction == Direction.UpLeft && Map.HasPath(position, Direction.Up))
            {
                path2Direction = Direction.Up;
            }

            if (path1Direction == Direction.None || path2Direction == Direction.None)
                return; // Should not happen, but to avoid exceptions just stop path splitting if it happens.

            var path1Data = new SerfPathInfo();
            var path2Data = new SerfPathInfo();

            path1Data.Serfs = new int[16];
            path2Data.Serfs = new int[16];

            Flag.FillPathSerfInfo(this, position, path1Direction, path1Data);
            Flag.FillPathSerfInfo(this, position, path2Direction, path2Data);

            var flag2 = Flags[(uint)path2Data.FlagIndex];
            var direction2 = path2Data.FlagDirection;
            int select = -1;

            if (flag2.SerfRequested(direction2))
            {
                foreach (var serf in Serfs.ToList())
                {
                    if (serf.PathSplited((uint)path1Data.FlagIndex, path1Data.FlagDirection,
                                         (uint)path2Data.FlagIndex, path2Data.FlagDirection,
                                         ref select))
                    {
                        break;
                    }
                }

                var pathData = (select == 0) ? path2Data : path1Data;
                var selectedFlag = Flags[(uint)pathData.FlagIndex];
                selectedFlag.CancelSerfRequest(pathData.FlagDirection);
            }

            var flag = Flags[Map.GetObjectIndex(position)];

            flag.RestorePathSerfInfo(path1Direction, path1Data);
            flag.RestorePathSerfInfo(path2Direction, path2Data);
        }

        bool MapTypesWithin(MapPos position, Map.Terrain low, Map.Terrain high)
        {
            if (Map.TypeUp(position) >= low &&
                Map.TypeUp(position) <= high &&
                Map.TypeDown(position) >= low &&
                Map.TypeDown(position) <= high &&
                Map.TypeDown(Map.MoveLeft(position)) >= low &&
                Map.TypeDown(Map.MoveLeft(position)) <= high &&
                Map.TypeUp(Map.MoveUpLeft(position)) >= low &&
                Map.TypeUp(Map.MoveUpLeft(position)) <= high &&
                Map.TypeDown(Map.MoveUpLeft(position)) >= low &&
                Map.TypeDown(Map.MoveUpLeft(position)) <= high &&
                Map.TypeUp(Map.MoveUp(position)) >= low &&
                Map.TypeUp(Map.MoveUp(position)) <= high)
            {
                return true;
            }

            return false;
        }

        void FlagRemovePlayerRefs(Flag flag)
        {
            foreach (var player in Players.ToList())
            {
                if (player.SelectedObjectIndex == flag.Index)
                {
                    player.SelectedObjectIndex = 0;
                }
            }
        }

        bool DemolishFlag(MapPos position)
        {
            // Handle any serf at position. 
            if (Map.HasSerf(position))
            {
                var serf = GetSerfAtPosition(position);
                serf.FlagDeleted(position);
            }

            var flag = Flags[Map.GetObjectIndex(position)];

            if (flag.HasBuilding && !flag.Building.IsBurning)
            {
                throw new ExceptionFreeserf(this, ErrorSystemType.Game, "Failed to demolish flag with building.");
            }

            FlagRemovePlayerRefs(flag);

            // Handle connected flag. 
            flag.MergePaths(position);

            // Update serfs with reference to this flag. 
            foreach (var serf in Serfs.ToList())
            {
                serf.PathMerged(flag);
            }

            // Remove resources from flag. 
            flag.RemoveAllResources();

            DeleteFlag(flag);

            return true;
        }

        bool DemolishBuilding(MapPos position)
        {
            var building = Buildings[Map.GetObjectIndex(position)];

            if (building.BurnUp())
            {
                BuildingRemovePlayerRefs(building);

                // Remove path to building. 
                Map.DeletePath(position, Direction.DownRight);
                Map.DeletePath(Map.MoveDownRight(position), Direction.UpLeft);

                // Disconnect flag. 
                var flag = Flags[building.FlagIndex];

                if (flag != null)
                {
                    flag.UnlinkBuilding();
                    FlagResetTransport(flag);
                }

                return true;
            }

            return false;
        }

        internal void PlayerDefeated(uint playerIndex)
        {
            // TODO
        }

        public void PlayerSurrendered(uint playerIndex)
        {
            // TODO
        }

        /// <summary>
        /// Map position is lost to the owner, demolish everything.
        /// </summary>
        /// <param name="position"></param>
        void SurrenderLand(MapPos position)
        {
            // Remove building
            if (Map.HasBuilding(position))
            {
                DemolishBuilding(position);
            }

            if (!Map.HasFlag(position) && Map.Paths(position) != 0)
            {
                DemolishRoad(position);
            }

            // Remove roads and building around position
            bool removeRoads = Map.HasFlag(position);
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                var adjacentPosition = Map.Move(position, direction);

                if (Map.GetObject(adjacentPosition) >= Map.Object.SmallBuilding &&
                    Map.GetObject(adjacentPosition) <= Map.Object.Castle)
                {
                    DemolishBuilding(adjacentPosition);
                }

                if (removeRoads && (Map.Paths(adjacentPosition) & Misc.Bit((int)direction.Reverse())) != 0)
                {
                    DemolishRoad(adjacentPosition);
                }
            }

            // Remove flag
            if (Map.GetObject(position) == Map.Object.Flag)
            {
                // Ensure that buildings are demolished before their flags
                if (Map.HasBuilding(Map.MoveUpLeft(position)))
                    DemolishBuilding(Map.MoveUpLeft(position));

                DemolishFlag(position);
            }
        }

        void DemolishFlagAndRoads(MapPos position)
        {
            if (Map.HasFlag(position))
            {
                // Remove roads around position
                var cycle = DirectionCycleCW.CreateDefault();

                foreach (var direction in cycle)
                {
                    var adjacentPosition = Map.Move(position, direction);

                    if ((Map.Paths(adjacentPosition) & Misc.Bit((int)direction.Reverse())) != 0)
                    {
                        DemolishRoad(adjacentPosition);
                    }
                }

                DemolishFlag(position);
            }
            else if (Map.Paths(position) != 0)
            {
                DemolishRoad(position);
            }
        }

        #endregion


        internal void ReadFrom(SaveReaderBinary reader)
        {
            // Load these first so map dimensions can be reconstructed.
            // This is necessary to load map positions.

            reader.Skip(74);
            gameType = reader.ReadWord(); // 74
            reader.Skip(2);  // 76
            state.Tick = reader.ReadWord(); // 78
            state.GameStatsCounter = 0;
            state.HistoryCounter = 0;

            reader.Skip(4);

            state.Random = new Random(reader.ReadWord(), reader.ReadWord(), reader.ReadWord()); // 84, 86, 88

            int maxFlagIndex = reader.ReadWord(); // 90
            int maxBuildingIndex = reader.ReadWord(); // 92
            int maxSerfIndex = reader.ReadWord(); // 94

            reader.Skip(2); // 96, was next_index
            state.FlagSearchCounter = reader.ReadWord(); // 98

            reader.Skip(4);

            for (int i = 0; i < 4; ++i)
            {
                state.PlayerHistoryIndex[i] = reader.ReadWord(); // 104 + i*2
            }

            for (int i = 0; i < 3; ++i)
            {
                state.PlayerHistoryCounter[i] = reader.ReadWord(); // 112 + i*2
            }

            state.ResourceHistoryIndex = reader.ReadWord(); // 118

            //  if (0//gameType == TUTORIAL) {
            //    tutorial_level = *reinterpret_cast<uint16_t*>(&data[122]);
            //  } else if (0//gameType == MISSION) {
            //    mission_level = *reinterpret_cast<uint16_t*>(&data[124]);
            //  }

            reader.Skip(54);

            int maxInventoryIndex = reader.ReadWord(); // 174

            reader.Skip(14); // 180 was max_next_index

            int mapSize = reader.ReadWord(); // 190

            // Avoid allocating a huge map if the input file is invalid
            if (mapSize < 3 || mapSize > 10)
            {
                throw new ExceptionFreeserf(ErrorSystemType.Game, "Invalid map size in file");
            }

            Map = new Map(new MapGeometry((uint)mapSize), renderView);

            reader.Skip(8);
            state.MapGoldMoraleFactor = reader.ReadWord(); // 200
            reader.Skip(2);
            playerScoreLeader = reader.ReadByte(); // 204

            reader.Skip(45);

            // Load players state from save game.
            for (uint i = 0; i < MAX_PLAYER_COUNT; ++i)
            {
                var playerReader = reader.Extract(8628);
                playerReader.Skip(130);

                if (Misc.BitTest(reader.ReadByte(), 6))
                {
                    playerReader.Reset();

                    var player = Players.GetOrInsert(i);

                    player.ReadFrom(playerReader);
                }
            }
            InitKnights();

            // Load map state from save game. 
            uint tileCount = Map.Columns * Map.Rows;
            var mapReader = reader.Extract(8 * tileCount);
            Map.ReadFrom(mapReader);

            LoadSerfs(reader, maxSerfIndex);
            LoadFlags(reader, maxFlagIndex);
            LoadBuildings(reader, maxBuildingIndex);
            LoadInventories(reader, maxInventoryIndex);

            state.GameSpeed = 0;
            gameSpeedSave = GameState.DEFAULT_GAME_SPEED;

            Map.AttachToRenderLayer(renderView.GetLayer(Layer.Landscape), renderView.GetLayer(Layer.Waves), renderView.DataSource);

            InitLandOwnership();
            PostLoadRoads();

            state.GoldTotal = Map.GetGoldDeposit();

            Map.AddChangeHandler(this);
        }

        internal void ReadFrom(SaveReaderText reader)
        {
            // Load essential values for calculating map positions
            // so that map positions can be loaded properly.
            var sections = reader.GetSections("game");

            if (sections == null || sections.Count == 0 || sections[0] == null)
            {
                throw new ExceptionFreeserf(ErrorSystemType.Game, "Failed to find section \"game\"");
            }

            var gameReader = sections[0];
            uint size;

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
            Map = new Map(new MapGeometry(size), renderView);

            foreach (var subreader in reader.GetSections("map"))
            {
                Map.ReadFrom(subreader);
            }

            gameType = gameReader.Value("game_type").ReadInt();
            state.Tick = (ushort)gameReader.Value("tick").ReadUInt();
            state.GameStatsCounter = gameReader.Value("game_stats_counter").ReadUInt();
            state.HistoryCounter = gameReader.Value("history_counter").ReadUInt();

            state.Random = new Random(gameReader.Value("random").ReadString());
            state.FlagSearchCounter = (ushort)gameReader.Value("flag_search_counter").ReadUInt();

            for (int i = 0; i < 4; ++i)
            {
                state.PlayerHistoryIndex[i] = gameReader.Value("player_history_index")[i].ReadInt();
            }

            for (int i = 0; i < 3; ++i)
            {
                state.PlayerHistoryCounter[i] = gameReader.Value("player_history_counter")[i].ReadInt();
            }

            state.ResourceHistoryIndex = gameReader.Value("resource_history_index").ReadInt();
            state.MapGoldMoraleFactor = gameReader.Value("map.gold_morale_factor").ReadUInt();
            playerScoreLeader = gameReader.Value("player_score_leader").ReadInt();

            state.GoldTotal = gameReader.Value("gold_deposit").ReadUInt();

            var updateState = new Map.UpdateState();

            updateState.RemoveSignsCounter = gameReader.Value("update_state.remove_signs_counter").ReadInt();
            updateState.LastTick = (ushort)gameReader.Value("update_state.last_tick").ReadUInt();
            updateState.Counter = gameReader.Value("update_state.counter").ReadInt();
            uint x = gameReader.Value("update_state.initial_pos")[0].ReadUInt();
            uint y = gameReader.Value("update_state.initial_pos")[1].ReadUInt();
            updateState.InitialPosition = Map.Position(x, y);

            Map.SetUpdateState(updateState);

            foreach (var subreader in reader.GetSections("player"))
            {
                var player = Players.GetOrInsert((uint)subreader.Number);
                player.ReadFrom(subreader);
            }

            InitKnights();

            foreach (var subreader in reader.GetSections("flag"))
            {
                var flag = Flags.GetOrInsert((uint)subreader.Number);
                flag.ReadFrom(subreader);
            }

            foreach (var subreader in reader.GetSections("building"))
            {
                var building = Buildings.GetOrInsert((uint)subreader.Number);
                building.ReadFrom(subreader);
            }

            foreach (var subreader in reader.GetSections("inventory"))
            {
                var inventory = Inventories.GetOrInsert((uint)subreader.Number);
                inventory.ReadFrom(subreader);
            }

            foreach (var subreader in reader.GetSections("serf"))
            {
                var serf = Serfs.GetOrInsert((uint)subreader.Number);
                serf.ReadFrom(subreader);
            }

            // Restore idle serf flag
            foreach (var serf in Serfs.ToList())
            {
                if (serf.Index == 0)
                    continue;

                if (serf.SerfState == Serf.State.IdleOnPath ||
                    serf.SerfState == Serf.State.WaitIdleOnPath)
                {
                    Map.SetIdleSerf(serf.Position);
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
            foreach (var building in Buildings.ToList())
            {
                if (building.Index == 0)
                    continue;

                if (Map.GetObject(building.Position) < Map.Object.SmallBuilding ||
                    Map.GetObject(building.Position) > Map.Object.Castle)
                {
                    throw new ExceptionFreeserf(ErrorSystemType.Game, "Map data does not match building " + building.Index + " position.");
                }

                Map.SetObjectIndex(building.Position, building.Index);

                OnObjectPlaced(building.Position);
            }

            // Restore flag index
            foreach (var flag in Flags.ToList())
            {
                if (flag.Index == 0)
                    continue;

                if (Map.GetObject(flag.Position) != Map.Object.Flag)
                {
                    throw new ExceptionFreeserf(ErrorSystemType.Game, "Map data does not match flag " + flag.Index + " position.");
                }

                Map.SetObjectIndex(flag.Position, flag.Index);

                OnObjectPlaced(flag.Position);
            }

            state.GameSpeed = 0;
            gameSpeedSave = GameState.DEFAULT_GAME_SPEED;

            Map.AttachToRenderLayer(renderView.GetLayer(Layer.Landscape), renderView.GetLayer(Layer.Waves), renderView.DataSource);

            InitLandOwnership();
            PostLoadRoads();

            Map.AddChangeHandler(this);

            // make map objects visible
            foreach (var tile in Map.Geometry)
            {
                if (Map.GetObject(tile) != Map.Object.None && !Map.HasFlag(tile) && !Map.HasBuilding(tile))
                    OnObjectPlaced(tile);
            }

            // load AI
            bool firstAI = true;

            foreach (var subreader in reader.GetSections("player_ai"))
            {
                var player = Players.GetOrInsert((uint)subreader.Number);

                player.AI = AI.Read(subreader, this, firstAI);

                firstAI = false;
            }
        }

        internal void WriteTo(SaveWriterText writer)
        {
            writer.Value("map.size").Write(Map.Size);
            writer.Value("game_type").Write(gameType);
            writer.Value("tick").Write(Tick);
            writer.Value("game_stats_counter").Write(state.GameStatsCounter);
            writer.Value("history_counter").Write(state.HistoryCounter);
            writer.Value("random").Write(state.Random.ToString());

            writer.Value("next_index").Write(0); // next_index (we keep this to be compatible to freeserf save games)
            writer.Value("flag_search_counter").Write(state.FlagSearchCounter);

            for (int i = 0; i < 4; ++i)
            {
                writer.Value("player_history_index").Write(state.PlayerHistoryIndex[i]);
            }

            for (int i = 0; i < 3; ++i)
            {
                writer.Value("player_history_counter").Write(state.PlayerHistoryCounter[i]);
            }

            writer.Value("resource_history_index").Write(state.ResourceHistoryIndex);

            writer.Value("max_next_index").Write(0); // max_next_index (we keep this to be compatible to freeserf save games)
            writer.Value("map.gold_morale_factor").Write(MapGoldMoraleFactor);
            writer.Value("player_score_leader").Write(playerScoreLeader);

            writer.Value("gold_deposit").Write(GoldTotal);

            var updateState = Map.GetUpdateState();

            writer.Value("update_state.remove_signs_counter").Write(updateState.RemoveSignsCounter);
            writer.Value("update_state.last_tick").Write(updateState.LastTick);
            writer.Value("update_state.counter").Write(updateState.Counter);
            writer.Value("update_state.initial_pos").Write(Map.PositionColumn(updateState.InitialPosition));
            writer.Value("update_state.initial_pos").Write(Map.PositionRow(updateState.InitialPosition));

            foreach (var player in Players.ToList())
            {
                var playerWriter = writer.AddSection("player", player.Index);

                player.WriteTo(playerWriter);
            }

            foreach (var flag in Flags.ToList())
            {
                if (flag.Index == 0)
                    continue;

                var flagWriter = writer.AddSection("flag", flag.Index);
                flag.WriteTo(flagWriter);
            }

            foreach (var building in Buildings.ToList())
            {
                if (building.Index == 0)
                    continue;

                var buildingWriter = writer.AddSection("building", building.Index);
                building.WriteTo(buildingWriter);
            }

            foreach (var inventory in Inventories.ToList())
            {
                var inventoryWriter = writer.AddSection("inventory", inventory.Index);
                inventory.WriteTo(inventoryWriter);
            }

            foreach (var serf in Serfs.ToList())
            {
                if (serf.Index == 0)
                    continue;

                var serfWriter = writer.AddSection("serf", serf.Index);
                serf.WriteTo(serfWriter);
            }

            Map.WriteTo(writer);

            // store AI
            bool firstAI = true;

            foreach (var player in Players.ToList())
            {
                if (player.IsAI)
                {
                    var aiWriter = writer.AddSection("player_ai", player.Index);

                    player.AI.WriteTo(aiWriter, firstAI);

                    firstAI = false;
                }
            }
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
                    var serf = Serfs.GetOrInsert((uint)i);
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
                    var flag = Flags.GetOrInsert((uint)i);
                    flag.ReadFrom(flagReader);
                }
            }

            // Set flag positions.
            foreach (var position in Map.Geometry)
            {
                if (Map.GetObject(position) == Map.Object.Flag)
                {
                    var flag = Flags[Map.GetObjectIndex(position)];
                    flag.Position = position;

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
                    var building = Buildings.GetOrInsert((uint)i);
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
                    var inventory = Inventories.GetOrInsert((uint)i);
                    inventory.ReadFrom(inventoryReader);
                }
            }

            return true;
        }

        internal override void OnHeightChanged(MapPos position)
        {
            // Update road segments
            for (int i = 0; i < 7; ++i)
            {
                var checkPosition = Map.PositionAddSpirally(position, (uint)i);
                var cycle = new DirectionCycleCW(Direction.Right, 3u);

                foreach (var direction in cycle)
                {
                    if (Map.HasPath(checkPosition, direction))
                    {
                        long index = Render.RenderRoadSegment.CreateIndex(checkPosition, direction);

                        if (renderRoadSegments.ContainsKey(index))
                            renderRoadSegments[index].UpdateAppearance();
                    }
                }
            }
        }

        internal override void OnObjectChanged(uint position, Map.Object oldObjectType, uint oldObjectIndex)
        {
            // In multiplayer update territory if a military building was placed, removed or captured
            var newBuilding = GetBuildingAtPosition(position);
            var oldBuilding = GetBuilding(oldObjectType switch
            {
                Map.Object.SmallBuilding => oldObjectIndex,
                Map.Object.LargeBuilding => oldObjectIndex,
                Map.Object.Castle => oldObjectIndex,
                _ => GameObject.INVALID_INDEX
            });

            if (newBuilding == null && oldBuilding != null && oldBuilding.IsMilitary())
            {
                UpdateLandOwnership(position);
            }
            else if (newBuilding != null && newBuilding.IsMilitary())
            {
                if (oldBuilding == newBuilding)
                {
                    if (oldBuilding.Player != newBuilding.Player)
                        UpdateLandOwnership(position);
                }
                else
                {
                    UpdateLandOwnership(position);
                }
            }
        }

        internal override void OnObjectChanged(MapPos position)
        {
            // memorize mineral for AI
            if (Map.GetObject(position) >= Map.Object.SignLargeGold && Map.GetObject(position) < Map.Object.SignEmpty)
            {
                int index = Map.GetObject(position) - Map.Object.SignLargeGold;

                AI.MemorizeMineralSpot(position, (Map.Minerals)(1 + index / 2), index % 2 == 0);
            }
        }

        internal override void OnObjectExchanged(MapPos position, Map.Object oldObject, Map.Object newObject)
        {
            if (oldObject != Map.Object.None && newObject < Map.Object.Tree0)
            {
                // we don't draw buildings etc with renderObjects
                if (renderObjects.TryRemove(position, out var renderObject) && renderObject != null)
                    renderObject.Delete();
            }
            else if (renderObjects.ContainsKey(position))
            {
                renderObjects[position].ChangeObjectType(newObject);
            }
        }

        internal override void OnObjectPlaced(MapPos position)
        {
            var obj = Map.GetObject(position);

            // rendering
            if (obj == Map.Object.Flag)
            {
                var flag = GetFlagAtPosition(position);

                if (!renderFlags.ContainsKey(flag))
                {
                    var renderFlag = new Render.RenderFlag(flag, renderView.GetLayer(Layer.Objects), renderView.SpriteFactory, renderView.DataSource);

                    renderFlag.Visible = true;

                    renderFlags.TryAdd(flag, renderFlag);
                }
            }
            else if (obj == Map.Object.SmallBuilding ||
                     obj == Map.Object.LargeBuilding ||
                     obj == Map.Object.Castle)
            {
                var building = GetBuildingAtPosition(position);

                if (!renderBuildings.ContainsKey(building))
                {
                    var renderBuilding = new Render.RenderBuilding(building, renderView.GetLayer(Layer.Buildings),
                        renderView.GetLayer(Layer.Objects), renderView.SpriteFactory, renderView.DataSource, audioInterface.AudioFactory.GetAudio());

                    renderBuilding.Visible = true;

                    if (!renderBuildings.TryAdd(building, renderBuilding))
                        throw new ExceptionFreeserf(ErrorSystemType.Application, "Unable to add render building.");

                    if (!building.IsDone || building.IsBurning)
                        renderBuildingsInProgress.Add(renderBuilding);
                }
            }
            else // map object
            {
                if (obj != Map.Object.None)
                {
                    var renderObject = new Render.RenderMapObject(obj, renderView.GetLayer(Layer.Objects), renderView.SpriteFactory, renderView.DataSource);

                    renderObject.Visible = true;

                    renderObjects.TryAdd(position, renderObject);
                }
            }
        }

        internal void AddSerfForDrawing(Serf serf, MapPos position)
        {
            if (renderSerfs.ContainsKey(serf))
                return;

            var renderSerf = new Render.RenderSerf(serf, renderView.GetLayer(Layer.Serfs), renderView.SpriteFactory,
                renderView.DataSource, audioInterface.AudioFactory.GetAudio());

            renderSerf.Visible = true;

            if (!renderSerfs.TryAdd(serf, renderSerf))
                throw new ExceptionFreeserf(ErrorSystemType.Application, "Unable to add render serf.");
        }

        internal void RemoveSerfFromDrawing(Serf serf)
        {
            if (renderSerfs.TryRemove(serf, out var renderSerf) && renderSerf != null)
                renderSerf.Delete();
        }

        internal override void OnRoadSegmentPlaced(MapPos position, Direction direction)
        {
            AddRoadSegment(position, direction);
        }

        internal override void OnRoadSegmentDeleted(MapPos position, Direction direction)
        {
            RemoveRoadSegment(position, direction);
        }

        void AddRoadSegment(MapPos position, Direction direction)
        {
            if (direction < Direction.Right || direction > Direction.Down)
                return;

            long index = Render.RenderRoadSegment.CreateIndex(position, direction);

            var renderRoadSegment = new Render.RenderRoadSegment(Map, position, direction, renderView.GetLayer(Layer.Paths),
                renderView.SpriteFactory, renderView.DataSource);

            renderRoadSegment.Visible = true;

            if (!renderRoadSegments.TryAdd(index, renderRoadSegment))
                Log.Error.Write(ErrorSystemType.Game, $"Failed to add road segment at position {position} direction {direction}.");
        }

        void RemoveRoadSegment(MapPos position, Direction direction)
        {
            if (direction < Direction.Right)
                return;

            if (direction > Direction.Down)
            {
                RemoveRoadSegment(Map.Move(position, direction), direction.Reverse());
                return;
            }

            long index = Render.RenderRoadSegment.CreateIndex(position, direction);

            if (renderRoadSegments.TryRemove(index, out var renderRoadSegment) && renderRoadSegment != null)
                renderRoadSegment.Delete();
            else
                Log.Error.Write(ErrorSystemType.Game, $"Failed to remove road segment at position {position} direction {direction}.");
        }

        internal Road GetRoadFromPathAtPosition(MapPos position)
        {
            var cycle = DirectionCycleCW.CreateDefault();
            Flag flag = null;
            var endDirection = Direction.None;

            foreach (var direction in cycle)
            {
                if (Map.HasPath(position, direction))
                {
                    flag = TracePathAndGetFlagAtEnd(position, direction, out endDirection);
                    break;
                }
            }

            if (flag == null)
                return null;

            return flag.GetRoad(endDirection);
        }

        internal Flag TracePathAndGetFlagAtEnd(MapPos position, Direction direction, out Direction endFlagReverseDirection)
        {
            var nextPosition = Map.Move(position, direction);

            if (Map.HasFlag(nextPosition))
            {
                endFlagReverseDirection = direction.Reverse();
                return GetFlagAtPosition(nextPosition);
            }
            else
            {
                // cycle through all directions except for reverse of the one we've gone
                var cycle = new DirectionCycleCW((Direction)(((int)direction.Reverse() + 1) % 6), 5u);

                foreach (var nextDirection in cycle)
                {
                    if (Map.HasPath(nextPosition, nextDirection))
                        return TracePathAndGetFlagAtEnd(nextPosition, nextDirection, out endFlagReverseDirection);
                }

                endFlagReverseDirection = Direction.None;

                return null; // none found, should not happen
            }
        }
    }
}
