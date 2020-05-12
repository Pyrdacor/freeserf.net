/*
 * AIStateBuildBuilding.cs - AI state for building a building
 *
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
using System.Threading;

namespace Freeserf.AIStates
{
    using MapPos = UInt32;

    // TODO: If we don't have enough materials, we should switch to a state where we increase our material production
    //       or increase the plank amount for constructions.
    // TODO: The ai should prefer large buildings in large spots (especially at the beginning). Otherwise there might
    //       be no room for large buildings when one is needed.
    // TODO: Some buildings should never been placed too near to enemies like toolmaker, weaponsmith, stock, etc.
    //       Especially the toolmaker in the beginning is crucial. Smart players should never build non-military
    //       buildings at the border if an enemy is near (except for mines).
    // TODO: Avoid building too much buildings near foresters/farms if possible.
    class AIStateBuildBuilding : AIState
    {
        Building.Type type = Building.Type.None;
        uint builtPosition = Global.INVALID_MAPPOS;
        int tries = 0;
        AI ai = null;
        Game game = null;
        Player player = null;
        PlayerInfo playerInfo = null;
        bool searching = false;
        readonly object searchingLock = new object();

        public AIStateBuildBuilding(Building.Type buildingType)
            : base(AI.State.BuildBuilding)
        {
            type = buildingType;
        }

        protected override void ReadFrom(Game game, AI ai, string name, SaveReaderText reader)
        {
            base.ReadFrom(game, ai, name, reader);

            type = reader.Value($"{name}.building_type").ReadBuilding();
            builtPosition = reader.Value($"{name}.built_position").ReadUInt();
        }

        public override void WriteTo(string name, SaveWriterText writer)
        {
            base.WriteTo(name, writer);

            writer.Value($"{name}.building_type").Write(type);
            writer.Value($"{name}.built_position").Write(builtPosition);
        }

        void Search()
        {
            MapPos position;

            // find a nice spot
            if (!IsEssentialBuilding(game, player) && !IsResourceNeedingBuilding && ai.StupidDecision()) // no stupid decisions for essential buildings and buildings that need resources!
                position = FindRandomSpot(game, player, false);
            else
            {
                position = FindSpot(ai, game, player, (int)playerInfo.Intelligence);

                if (position == Global.INVALID_MAPPOS && !IsResourceNeedingBuilding && (!ai.HardTimes || type == Building.Type.Sawmill || type == Building.Type.ToolMaker))
                    position = FindRandomSpot(game, player, true);
            }

            if (position != Global.INVALID_MAPPOS && game.CanBuildBuilding(position, type, player) && ai.CanLinkFlag(game.Map.MoveDownRight(position)))
            {
                // military buildings could not be built too close to others
                if ((type != Building.Type.Hut && type != Building.Type.Tower && type != Building.Type.Fortress) || !game.Map.HasAnyInArea(position, 3, FindMilitary, 1))
                    builtPosition = position;
            }

            lock (searchingLock)
            {
                searching = false;
            }

            if (builtPosition == Global.INVALID_MAPPOS && ++tries > 10 + playerInfo.Intelligence / 5)
                Kill(ai); // not able to build the building
        }

        void Search(object param)
        {
            var state = param as AIStateBuildBuilding;

            try
            {
                state.Search();
            }
            finally
            {
                lock (state.searchingLock)
                {
                    state.searching = false;
                }
            }
        }

        public override void Kill(AI ai)
        {
            lock (searchingLock)
            {
                searching = false;
            }

            base.Kill(ai);
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            this.ai = ai;
            this.game = game;
            this.player = player;
            this.playerInfo = playerInfo;

            if (builtPosition != Global.INVALID_MAPPOS)
            {
                if (game.BuildBuilding(builtPosition, type, player))
                {
                    NextState = ai.CreateRandomDelayedState(AI.State.LinkBuilding, 0, (5 - (int)playerInfo.Intelligence / 9) * 1000, builtPosition);

                    // If this was a toolmaker and we are in hard times we set planks to half for toolmaker.
                    // Also we decide which tool to craft first.
                    if (type == Building.Type.ToolMaker && ai.HardTimes)
                    {
                        player.PlanksToolmaker = ushort.MaxValue / 2;

                        // With only one fisher and only a small lake, the scythe has prio.
                        // Without a fisher we have no water at all and therefore the scythe is very important.
                        if (player.GetTotalBuildingCount(Building.Type.Fisher) != 0 || CountFish(game.Map.FindInTerritory(player.Index, FindFish)) < 20)
                        {
                            player.SetFullToolPriority(Resource.Type.Scythe);
                        }
                        // With a fisher and enough fish, the pincer/hammer for weaponsmith has prio.
                        // As the hammer might be consumed by another serf, we first craft the pincer.
                        else
                        {
                            player.SetFullToolPriority(Resource.Type.Pincer);
                        }
                    }
                }

                Kill(ai);

                return;
            }

            lock (searchingLock)
            {
                if (searching)
                    return;

                searching = true;
            }

            new Thread(new ParameterizedThreadStart(Search)).Start(this);
        }

        bool IsEssentialBuilding(Game game, Player player)
        {
            switch (type)
            {
                case Building.Type.Lumberjack:
                case Building.Type.Stonecutter:
                case Building.Type.Sawmill:
                    return player.GetTotalBuildingCount(type) == 0;
                default:
                    return false;
            }
        }

        bool IsResourceNeedingBuilding
        {
            get
            {
                switch (type)
                {
                    // For these buildings we will not choose a random spot if there is no valid spot.
                    // It would not make sense to do so as they need resources around them.
                    case Building.Type.CoalMine:
                    case Building.Type.Fisher:
                    case Building.Type.GoldMine:
                    case Building.Type.IronMine:
                    case Building.Type.Lumberjack:
                    case Building.Type.Stonecutter:
                    case Building.Type.StoneMine:
                        return true;
                    default:
                        return false;
                }
            }
        }

        uint FindRandomSpot(Game game, Player player, bool checkCanBuild)
        {
            var spot = game.Map.GetRandomCoordinate(game.GetRandom());

            if (checkCanBuild)
            {
                int tries = 0;

                while (!game.CanBuildBuilding(spot, type, player))
                {
                    if (++tries > 20)
                    {
                        spot = FindSpotNearBuilding(game, player, 40, Building.Type.Hut);

                        if (spot == Global.INVALID_MAPPOS)
                            spot = FindSpotNearBuilding(game, player, 40, Building.Type.Tower);

                        if (spot == Global.INVALID_MAPPOS)
                            spot = FindSpotNearBuilding(game, player, 40, Building.Type.Fortress);

                        if (spot == Global.INVALID_MAPPOS)
                            spot = FindSpotNearBuilding(game, player, 40, Building.Type.Castle);

                        return spot;
                    }

                    spot = game.Map.GetRandomCoordinate(game.GetRandom());
                }
            }

            return spot;
        }

        uint FindSpot(AI ai, Game game, Player player, int intelligence)
        {
            // TODO: Fishers, lumberjacks, stonecutters and so on should prefer spots with more resources
            // over those with only few resources. Same goes for mines.

            switch (type)
            {
                case Building.Type.Baker:
                    return FindSpotNearBuilding(game, player, intelligence, Building.Type.Mill, 2);
                case Building.Type.Boatbuilder:
                    if (intelligence >= 20)
                        return FindSpotNearBuilding(game, player, intelligence, Building.Type.Stock, 1);
                    else
                        return FindRandomSpot(game, player, true);
                case Building.Type.Butcher:
                    return FindSpotNearBuilding(game, player, intelligence, Building.Type.PigFarm, 1 + ai.FoodFocus);
                case Building.Type.CoalMine:
                    return FindSpotWithMinerals(game, player, intelligence, Map.Minerals.Coal, 2 + ai.SteelFocus);
                case Building.Type.Farm:
                    return FindSpotWithSpace(game, player, intelligence, 1 + ai.FoodFocus);
                case Building.Type.Fisher:
                    if (ai.HardTimes)
                    {
                        // we need to find fish in territory or near territory
                        var spot = game.Map.FindFirstInTerritory(player.Index, FindFishInTerritory);

                        if (spot != null)
                            return FindSpotNear(game, player, (MapPos)spot, 3);

                        spot = game.Map.FindFirstInTerritory(player.Index, FindFishNearBorder);

                        if (spot != null)
                            return FindSpotNear(game, player, (MapPos)spot, 4);

                        return Global.INVALID_MAPPOS;
                    }
                    else
                        return FindSpotWithFish(game, player, intelligence, 1 + ai.FoodFocus);
                case Building.Type.Forester:
                    {
                        var spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.Lumberjack, 1 + ai.ConstructionMaterialFocus);

                        if (spot != Global.INVALID_MAPPOS)
                            return spot;
                        else
                            return FindSpotWithSpace(game, player, intelligence, 1 + ai.ConstructionMaterialFocus);
                    }
                case Building.Type.Hut:
                case Building.Type.Tower:
                case Building.Type.Fortress:
                    {
                        if (ai.HardTimes && type == Building.Type.Hut)
                        {
                            Func<Map, uint, bool> targetFunc = null;
                            int searchRange = 0;

                            if (player.GetTotalBuildingCount(Building.Type.Fisher) != 0 ||
                                game.Map.FindFirstInTerritory(player.Index, FindFishInTerritory) != null)
                            {
                                targetFunc = FindMountainOutsideTerritory;
                                searchRange = 32;
                            }
                            else
                            {
                                targetFunc = FindWater;
                                searchRange = 50; // this may take a while but we really need that fish!
                            }

                            var bestSpot = Global.INVALID_MAPPOS;
                            int bestDist = int.MaxValue;

                            Func<Map, MapPos, uint> costFunction = (Map map, MapPos position) =>
                            {
                                uint baseCost = (uint)map.FindInArea(position, 6, FindMilitary, 1).Count * 4;

                                if (!game.Map.HasOwner(position))
                                    return baseCost;

                                if (game.Map.GetOwner(position) == player.Index)
                                    return baseCost + 2u;

                                return baseCost + 10u;
                            };

                            Func<Map, MapPos, bool> checkSpotFunc = (Map map, MapPos position) =>
                            {
                                return game.CanBuildBuilding(position, type, player);
                            };

                            Func<Map, MapPos, int> rateSpotFunc = (Map map, MapPos position) =>
                            {
                                var militaryBuildings = map.FindInArea(position, 8, FindMilitary, 1);
                                int minDistance = int.MaxValue;

                                foreach (var building in militaryBuildings)
                                {
                                    int distance = map.Distance(position, (building as Building).Position);

                                    if (distance < minDistance)
                                        minDistance = distance;
                                }

                                return int.MaxValue - minDistance;
                            };

                            foreach (var building in game.GetPlayerBuildings(player).Where(playerBuilding => playerBuilding.IsMilitary()))
                            {
                                var target = Pathfinder.FindNearestSpot(game.Map, building.Position, targetFunc, costFunction, searchRange);

                                if (target != Global.INVALID_MAPPOS)
                                {
                                    var spot = building.Position;

                                    for (int i = 0; i < 8; ++i)
                                        spot = game.Map.MoveTowards(spot, target);

                                    spot = game.Map.FindSpotNear(spot, 3, checkSpotFunc, rateSpotFunc);

                                    if (spot != Global.INVALID_MAPPOS && game.Map.Distance(building.Position, spot) < bestDist)
                                    {
                                        bestSpot = spot;
                                        bestDist = game.Map.Distance(building.Position, target);
                                    }
                                }
                            }

                            if (bestSpot != Global.INVALID_MAPPOS)
                            {
                                return bestSpot;
                            }

                            return FindSpotNearBorder(game, player, intelligence, 2);
                        }

                        int defendChance = 2 + (2 - (ai.ExpandFocus - ai.DefendFocus)) * 8; // 2, 10, 18, 26 or 34 %

                        if (game.RandomInt() % 100 < defendChance)
                        {
                            var spot = Global.INVALID_MAPPOS;

                            if (ai.Chance(10))
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.Stock, 1 + ai.DefendFocus);

                            if (spot == Global.INVALID_MAPPOS && ai.Chance(10))
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.WeaponSmith, 1 + ai.DefendFocus);

                            if (spot == Global.INVALID_MAPPOS && ai.Chance(10))
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.GoldSmelter, 1 + ai.DefendFocus);

                            if (spot == Global.INVALID_MAPPOS && ai.Chance(10))
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.CoalMine, 1 + ai.DefendFocus);

                            if (spot == Global.INVALID_MAPPOS && ai.Chance(10))
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.IronMine, 1 + ai.DefendFocus);

                            if (spot == Global.INVALID_MAPPOS && ai.Chance(10))
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.GoldMine, 1 + ai.DefendFocus);

                            if (spot == Global.INVALID_MAPPOS && ai.Chance(10))
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.SteelSmelter, 1 + ai.DefendFocus);

                            if (spot == Global.INVALID_MAPPOS && ai.Chance(10))
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.ToolMaker, 1 + ai.DefendFocus);

                            if (spot == Global.INVALID_MAPPOS && ai.Chance(10))
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.Sawmill, 1 + ai.DefendFocus);

                            if (spot == Global.INVALID_MAPPOS && ai.Chance(10))
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.Castle, 1 + ai.DefendFocus);

                            if (spot == Global.INVALID_MAPPOS)
                                spot = FindSpotNearBorder(game, player, intelligence, 1 + Math.Min(2, (ai.MilitaryFocus + ai.ExpandFocus + ai.DefendFocus) / 2));

                            return spot;
                        }
                        else
                            return FindSpotNearBorder(game, player, intelligence, 1 + Math.Min(2, (ai.MilitaryFocus + ai.ExpandFocus + ai.DefendFocus) / 2));
                    }
                case Building.Type.GoldMine:
                    return FindSpotWithMinerals(game, player, intelligence, Map.Minerals.Coal, 1 + ai.GoldFocus);
                case Building.Type.GoldSmelter:
                    {
                        var spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.GoldMine, 2 + ai.GoldFocus);

                        if (spot == Global.INVALID_MAPPOS)
                            spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.CoalMine, 2 + ai.GoldFocus);

                        if (spot != Global.INVALID_MAPPOS)
                            return spot;
                        else
                            return FindSpotNearBuilding(game, player, intelligence, Building.Type.Stock, 2 + ai.GoldFocus);
                    }
                case Building.Type.IronMine:
                    return FindSpotWithMinerals(game, player, intelligence, Map.Minerals.Iron, 1 + ai.SteelFocus);
                case Building.Type.Lumberjack:
                    for (int i = 7; i >= 4; --i)
                    {
                        var spot = FindSpotWithTrees(game, player, intelligence, i, 1 + ai.ConstructionMaterialFocus);

                        if (spot != Global.INVALID_MAPPOS)
                            return spot;
                    }
                    return Global.INVALID_MAPPOS;
                case Building.Type.Mill:
                    return FindSpotNearBuilding(game, player, intelligence, Building.Type.Farm, 1 + ai.FoodFocus);
                case Building.Type.PigFarm:
                    return FindSpotNearBuilding(game, player, intelligence, Building.Type.Farm, 2 + ai.FoodFocus);
                case Building.Type.Sawmill:
                    return FindSpotNearBuilding(game, player, intelligence, Building.Type.Lumberjack, 1 + Math.Min(1, ai.ConstructionMaterialFocus));
                case Building.Type.SteelSmelter:
                    {
                        var spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.IronMine, 2 + Math.Max(ai.SteelFocus, ai.MilitaryFocus));

                        if (spot == Global.INVALID_MAPPOS)
                            spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.CoalMine, 2 + Math.Max(ai.SteelFocus, ai.MilitaryFocus));

                        if (spot != Global.INVALID_MAPPOS)
                            return spot;
                        else if (ai.MilitaryFocus >= ai.SteelFocus)
                            spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.WeaponSmith, 2 + ai.MilitaryFocus);
                        else
                            spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.ToolMaker, 2 + ai.SteelFocus);

                        if (spot == Global.INVALID_MAPPOS)
                            spot = FindRandomSpot(game, player, true);

                        return spot;
                    }
                case Building.Type.Stock:
                    return FindSpotForStock(game, player, intelligence, 1 + ai.BuildingFocus);
                case Building.Type.Stonecutter:
                    {
                        var spot = FindSpotWithStones(game, player, intelligence, 1 + Math.Min(1, ai.ConstructionMaterialFocus / 2));

                        if (spot != Global.INVALID_MAPPOS && BuildingsInArea(game.Map, spot, 7, Building.Type.Stonecutter, FindBuilding, 1) > 0)
                            spot = Global.INVALID_MAPPOS; // don't build two stonecutters near each other

                        return spot;
                    }
                case Building.Type.StoneMine:
                    return FindSpotWithMinerals(game, player, intelligence, Map.Minerals.Stone, 1 + ai.ConstructionMaterialFocus);
                case Building.Type.ToolMaker:
                    return FindSpotNearBuilding(game, player, intelligence, Building.Type.Stock, 1);
                case Building.Type.WeaponSmith:
                    {
                        var spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.SteelSmelter, 2 + ai.MilitaryFocus);

                        if (spot != Global.INVALID_MAPPOS)
                            return spot;
                        else
                            spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.Stock, 2 + ai.MilitaryFocus);

                        if (spot == Global.INVALID_MAPPOS)
                            spot = FindRandomSpot(game, player, true);

                        return spot;
                    }
                default:
                    return FindRandomSpot(game, player, true);
            }
        }

        uint FindSpotNear(Game game, Player player, MapPos position, int maxDistance)
        {
            var spots = game.Map.FindInArea(position, maxDistance, FindEmptySpot, 1);

            while (spots.Count > 0)
            {
                var spot = (MapPos)spots[game.RandomInt() % spots.Count];

                if (game.CanBuildBuilding(spot, type, player))
                    return spot;

                spots.Remove(spot);
            }

            return Global.INVALID_MAPPOS;
        }

        uint FindSpotNearBuilding(Game game, Player player, int intelligence, Building.Type buildingType, int maxInArea = int.MaxValue)
        {
            var buildings = game.GetPlayerBuildings(player).Where(building => building.BuildingType == buildingType).ToList();

            while (buildings.Count > 0)
            {
                var randomBuilding = buildings[game.RandomInt() % buildings.Count];

                if (CheckMaxInAreaOk(game.Map, randomBuilding.Position, 6, buildingType, maxInArea))
                    return FindSpotNear(game, player, randomBuilding.Position, 4);

                buildings.Remove(randomBuilding);
            }

            return Global.INVALID_MAPPOS;
        }

        uint FindSpotWithMinerals(Game game, Player player, int intelligence, Map.Minerals mineral, int maxInArea = int.MaxValue)
        {
            // search for minerals near castle, mines and military buildings
            var buildings = game.GetPlayerBuildings(player).Where(building =>
                building.BuildingType == Building.Type.Castle ||
                building.BuildingType == Building.Type.StoneMine ||
                building.BuildingType == Building.Type.CoalMine ||
                building.BuildingType == Building.Type.IronMine ||
                building.BuildingType == Building.Type.GoldMine ||
                building.BuildingType == Building.Type.Hut ||
                building.BuildingType == Building.Type.Tower ||
                building.BuildingType == Building.Type.Fortress
            ).ToList();

            while (buildings.Count > 0)
            {
                var randomBuilding = buildings[game.RandomInt() % buildings.Count];

                if (CheckMaxInAreaOk(game.Map, randomBuilding.Position, 9, AIStateFindMinerals.MineTypes[(int)mineral - 1], maxInArea))
                {
                    if (MineralsInArea(game.Map, randomBuilding.Position, 9, mineral, FindMineral, 1) > 0)
                    {
                        // 5 tries per spot
                        for (int i = 0; i < 5; ++i)
                        {
                            var spot = FindSpotNear(game, player, randomBuilding.Position, 9);

                            if (game.Map.GetResourceType(spot) == mineral)
                                return spot;
                        }
                    }
                }

                buildings.Remove(randomBuilding);
            }

            return Global.INVALID_MAPPOS;
        }

        uint FindSpotWithSpace(Game game, Player player, int intelligence, int maxInArea = int.MaxValue)
        {
            // TODO
            return FindRandomSpot(game, player, true);
        }

        uint FindSpotWithFish(Game game, Player player, int intelligence, int maxInArea = int.MaxValue)
        {
            // search for fish near castle, fishers and military buildings
            var buildings = game.GetPlayerBuildings(player).Where(building =>
                building.BuildingType == Building.Type.Castle ||
                building.BuildingType == Building.Type.Fisher ||
                building.BuildingType == Building.Type.Hut ||
                building.BuildingType == Building.Type.Tower ||
                building.BuildingType == Building.Type.Fortress
            ).ToList();

            while (buildings.Count > 0)
            {
                var randomBuilding = buildings[game.RandomInt() % buildings.Count];

                if (CheckMaxInAreaOk(game.Map, randomBuilding.Position, 7, Building.Type.Fisher, maxInArea))
                {
                    if (AmountInArea(game.Map, randomBuilding.Position, 8, CountFish, FindFish) > 0)
                    {
                        Func<Map, uint, bool> findShore = (Map map, MapPos position) =>
                        {
                            return map.GetObject(position) == Map.Object.None &&
                                map.Paths(position) == 0 &&
                                ((map.TypeDown(position) <= Map.Terrain.Water3 &&
                                  map.TypeUp(map.MoveUpLeft(position)) >= Map.Terrain.Grass0) ||
                                 (map.TypeDown(map.MoveLeft(position)) <= Map.Terrain.Water3 &&
                                  map.TypeUp(map.MoveUp(position)) >= Map.Terrain.Grass0));
                        };

                        var spot = game.Map.FindSpotNear(randomBuilding.Position, 8, findShore, game.GetRandom(), 1);

                        return FindSpotNear(game, player, spot, 4);
                    }
                }

                buildings.Remove(randomBuilding);
            }

            return Global.INVALID_MAPPOS;
        }

        uint FindSpotWithTrees(Game game, Player player, int intelligence, int minTrees = 6, int maxInArea = int.MaxValue)
        {
            // search for trees near castle, lumberjacks, foresters and military buildings
            var buildings = game.GetPlayerBuildings(player).Where(playerBuilding =>
                playerBuilding.BuildingType == Building.Type.Castle ||
                playerBuilding.BuildingType == Building.Type.Forester ||
                playerBuilding.BuildingType == Building.Type.Lumberjack ||
                playerBuilding.BuildingType == Building.Type.Hut ||
                playerBuilding.BuildingType == Building.Type.Tower ||
                playerBuilding.BuildingType == Building.Type.Fortress
            ).ToList();

            while (buildings.Count > 0)
            {
                var randomBuilding = buildings[game.RandomInt() % buildings.Count];

                if (CheckMaxInAreaOk(game.Map, randomBuilding.Position, 7, Building.Type.Lumberjack, maxInArea))
                {
                    if (AmountInArea(game.Map, randomBuilding.Position, 6, CountMapObjects, FindTree) >= minTrees)
                    {
                        Func<Map, uint, bool> findTree = (Map map, MapPos position) =>
                        {
                            return FindTree(map, position).Success;
                        };

                        var spot = game.Map.FindSpotNear(randomBuilding.Position, 8, findTree, game.GetRandom(), 1);

                        int maxDistance = 3;

                        if (!game.Map.HasOwner(spot) || game.Map.GetOwner(spot) != player.Index)
                            maxDistance = 6;

                        return FindSpotNear(game, player, spot, maxDistance);
                    }
                }

                buildings.Remove(randomBuilding);
            }

            return Global.INVALID_MAPPOS;
        }

        uint FindSpotWithStones(Game game, Player player, int intelligence, int maxInArea = int.MaxValue)
        {
            // search for stones near castle, stonecutters and military buildings
            var buildings = game.GetPlayerBuildings(player).Where(building =>
                building.BuildingType == Building.Type.Castle ||
                building.BuildingType == Building.Type.Stonecutter ||
                building.BuildingType == Building.Type.Hut ||
                building.BuildingType == Building.Type.Tower ||
                building.BuildingType == Building.Type.Fortress
            ).ToList();

            while (buildings.Count > 0)
            {
                var randomBuilding = buildings[game.RandomInt() % buildings.Count];

                if (CheckMaxInAreaOk(game.Map, randomBuilding.Position, 7, Building.Type.Stonecutter, maxInArea))
                {
                    if (AmountInArea(game.Map, randomBuilding.Position, 6, CountMapObjects, FindStone) > 0)
                    {
                        Func<Map, uint, bool> findStone = (Map map, MapPos position) =>
                        {
                            return FindStone(map, position).Success;
                        };

                        var spot = game.Map.FindSpotNear(randomBuilding.Position, 8, findStone, game.GetRandom(), 1);

                        return FindSpotNear(game, player, spot, 3);
                    }
                }

                buildings.Remove(randomBuilding);
            }

            return Global.INVALID_MAPPOS;
        }

        uint FindSpotNearBorder(Game game, Player player, int intelligence, int maxInArea = int.MaxValue)
        {
            var militaryBuildings = game.GetPlayerBuildings(player).Where(building => building.IsMilitary());
            var possibleBaseBuildings = new List<Building>();
            Building bestBaseBuilding = null;

            foreach (var militaryBuilding in militaryBuildings)
            {
                int numMilitaryBuildingsInArea = MilitaryBuildingsInArea(game.Map, militaryBuilding.Position, 9, 1, false);

                if (numMilitaryBuildingsInArea == 0)
                {
                    bestBaseBuilding = militaryBuilding;
                    break;
                }

                if (numMilitaryBuildingsInArea == 1)
                    possibleBaseBuildings.Add(militaryBuilding);
            }

            if (bestBaseBuilding == null && possibleBaseBuildings.Count == 0)
            {
                foreach (var militaryBuilding in militaryBuildings)
                {
                    int numMilitaryBuildingsInArea = MilitaryBuildingsInArea(game.Map, militaryBuilding.Position, 6, 1, false);

                    if (numMilitaryBuildingsInArea == 0)
                    {
                        bestBaseBuilding = militaryBuilding;
                        break;
                    }

                    if (numMilitaryBuildingsInArea == 1)
                        possibleBaseBuildings.Add(militaryBuilding);
                }
            }

            if (bestBaseBuilding == null && possibleBaseBuildings.Count == 0)
                return FindRandomSpot(game, player, true);

            if (bestBaseBuilding == null)
            {
                bestBaseBuilding = possibleBaseBuildings[game.RandomInt() % possibleBaseBuildings.Count];
            }

            var spot = game.Map.FindSpotNear(bestBaseBuilding.Position, 9, IsEmptySpotWithoutMilitary, game.GetRandom(), 5);

            if (spot == Global.INVALID_MAPPOS)
                spot = game.Map.FindSpotNear(bestBaseBuilding.Position, 9, IsEmptySpotWithoutMuchMilitary, game.GetRandom(), 4);

            return spot;
        }

        uint FindSpotForStock(Game game, Player player, int intelligence, int maxInArea = int.MaxValue)
        {
            var militaryBuildings = game.GetPlayerBuildings(player).Where(building => building.IsMilitary(false));
            var possibleBaseBuildings = new List<Building>();

            foreach (var militaryBuilding in militaryBuildings)
            {
                var buildingsInArea = game.Map.FindInArea(militaryBuilding.Position, 9, FindBuilding, 1);
                int numMilitary = 0;
                bool nearCastle = false;

                foreach (var buildingInArea in buildingsInArea)
                {
                    var building = buildingInArea as Building;

                    if (building.BuildingType == Building.Type.Castle)
                    {
                        nearCastle = true;
                        break;
                    }

                    if (building.IsMilitary(false))
                        ++numMilitary;
                }

                if (!nearCastle && numMilitary > 3)
                {
                    possibleBaseBuildings.Add(militaryBuilding);
                }
            }

            int bestBuildingInventoryDist = 0;
            var bestBuildingPosition = Global.INVALID_MAPPOS;
            var inventories = game.GetPlayerInventories(player);

            foreach (var building in possibleBaseBuildings)
            {
                int buildingDistance = int.MaxValue;
                var flag = game.GetFlag(building.FlagIndex);

                foreach (var inventory in inventories)
                {
                    var inventoryBuilding = game.GetBuilding(inventory.Building);

                    int distance = Pathfinder.FindShortestRoad(game.Map, game.GetFlag(inventoryBuilding.FlagIndex), flag, out uint cost).Count;

                    if (distance < buildingDistance)
                        buildingDistance = distance;
                }

                if (buildingDistance > bestBuildingInventoryDist)
                {
                    bestBuildingInventoryDist = buildingDistance;
                    bestBuildingPosition = building.Position;
                }
            }

            if (bestBuildingPosition == Global.INVALID_MAPPOS)
            {
                return bestBuildingPosition;
            }

            return game.Map.FindSpotNear(bestBuildingPosition, 6, CanBuildLarge, game.GetRandom(), 1);
        }


        #region Map analysis

        static Map.FindData FindTree(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = map.GetObject(position) >= Map.Object.Tree0 && map.GetObject(position) <= Map.Object.Pine7
            };
        }

        static Map.FindData FindStone(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = map.GetObject(position) >= Map.Object.Stone0 && map.GetObject(position) <= Map.Object.Stone7 &&
                    Map.MapSpaceFromObject[(int)map.GetObject(map.MoveDownRight(position))] <= Map.Space.Semipassable
            };
        }

        Map.FindData FindBuilding(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = map.HasBuilding(position) && map.GetOwner(position) == player.Index,
                Data = game.GetBuildingAtPosition(position)
            };
        }

        Map.FindData FindMilitary(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = map.HasBuilding(position) && game.GetBuildingAtPosition(position).IsMilitary() && map.GetOwner(position) == player.Index,
                Data = game.GetBuildingAtPosition(position)
            };
        }

        static Map.FindData FindFish(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = map.IsInWater(position) && map.GetResourceFish(position) > 0u,
                Data = map.GetResourceFish(position)
            };
        }

        static Map.FindData FindFishInTerritory(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = map.IsInWater(position) && map.GetResourceFish(position) > 0u,
                Data = position
            };
        }

        static Map.FindData FindFishNearBorder(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = map.FindInArea(position, 4, FindFish, 1).Any(),
                Data = position
            };
        }

        static Map.FindData FindMineral(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = FindMountain(map, position) && map.GetResourceAmount(position) > 0u,
                Data = new KeyValuePair<Map.Minerals, uint>(map.GetResourceType(position), map.GetResourceAmount(position))
            };
        }

        static bool FindMountainOutsideTerritory(Map map, MapPos position)
        {
            return FindMountain(map, position) && !map.HasOwner(position);
        }

        static bool FindMountain(Map map, MapPos position)
        {
            return (map.TypeUp(position) >= Map.Terrain.Tundra0 && map.TypeUp(position) <= Map.Terrain.Tundra2) ||
                   (map.TypeDown(position) >= Map.Terrain.Tundra0 && map.TypeDown(position) <= Map.Terrain.Tundra2);
        }

        static bool FindWater(Map map, MapPos position)
        {
            return (map.TypeUp(position) >= Map.Terrain.Water0 && map.TypeUp(position) <= Map.Terrain.Water3) ||
                   (map.TypeDown(position) >= Map.Terrain.Water0 && map.TypeDown(position) <= Map.Terrain.Water3);
        }

        Map.FindData FindEmptySpot(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = Map.MapSpaceFromObject[(int)map.GetObject(position)] == Map.Space.Open && map.GetOwner(position) == player.Index,
                Data = position
            };
        }

        static int CountMapObjects(List<object> data)
        {
            return data.Count;
        }

        static int CountFish(List<object> data)
        {
            return data.Select(fish => (int)(uint)fish).Sum();
        }

        static int AmountInArea(Map map, uint basePosition, int range, Func<List<object>, int> countFunc, Func<Map, uint, Map.FindData> searchFunc, int minDistance = 0)
        {
            return countFunc(map.FindInArea(basePosition, range, searchFunc, minDistance));
        }

        static int BuildingsInArea(Map map, uint basePosition, int range, Building.Type buildingType, Func<Map, uint, Map.FindData> searchFunc, int minDistance = 0)
        {
            return map.FindInArea(basePosition, range, searchFunc, minDistance).Count(building => (building as Building).BuildingType == buildingType);
        }

        int MilitaryBuildingsInArea(Map map, uint basePosition, int range, int minDistance = 0, bool includeCastle = true)
        {
            return map.FindInArea(basePosition, range, FindBuilding, minDistance).Count(building => (building as Building).IsMilitary(includeCastle));
        }

        int UnfinishedMilitaryBuildingsInArea(Map map, uint basePosition, int range, int minDistance = 0, bool includeCastle = true)
        {
            return map.FindInArea(basePosition, range, FindBuilding, minDistance).Count(building => (building as Building).IsMilitary(includeCastle) && !(building as Building).IsDone);
        }

        bool IsEmptySpotWithoutMuchMilitary(Map map, uint basePosition)
        {
            return FindEmptySpot(map, basePosition).Success && MilitaryBuildingsInArea(map, basePosition, 8) < 3 && UnfinishedMilitaryBuildingsInArea(map, basePosition, 8) == 0;
        }

        bool IsEmptySpotWithoutMilitary(Map map, uint basePosition)
        {
            return FindEmptySpot(map, basePosition).Success && MilitaryBuildingsInArea(map, basePosition, 8) < 2;
        }

        bool CanBuildLarge(Map map, MapPos position)
        {
            return game.CanBuildLarge(position);
        }

        static int MineralsInArea(Map map, uint basePosition, int range, Map.Minerals mineral, Func<Map, uint, Map.FindData> searchFunc, int minDistance = 0)
        {
            return map.FindInArea(basePosition, range, searchFunc, minDistance).Where(finding =>
                ((KeyValuePair<Map.Minerals, uint>)finding).Key == mineral).Select(f => (int)((KeyValuePair<Map.Minerals, uint>)f).Value).Sum();
        }

        bool CheckMaxInAreaOk(Map map, uint basePosition, int range, Building.Type buildingType, int maxInArea = int.MaxValue)
        {
            if (maxInArea == int.MaxValue)
                return true;

            return BuildingsInArea(map, basePosition, range, buildingType, FindBuilding, 1) < maxInArea;
        }

        #endregion
    }
}
