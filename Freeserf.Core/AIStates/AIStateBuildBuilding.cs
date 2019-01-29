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
    // TODO: If we don't have enough materials, we should switch to a state where we increase our material production
    //       or increase the plank amount for constructions.
    // TODO: The ai should prefer large buildings in large spots (especially at the beginning). Otherwise there might
    //       be no room for large building when one is needed.
    // TODO: Some buildings should never been placed too near to enemies like toolmaker, weaponsmith, stock, etc.
    //       Especially the toolmaker in the beginning is crucial.
    class AIStateBuildBuilding : AIState
    {
        Building.Type type = Building.Type.None;
        uint builtPosition = Global.BadMapPos;
        int tries = 0;
        AI ai = null;
        Game game = null;
        Player player = null;
        PlayerInfo playerInfo = null;
        bool searching = false;
        readonly object searchingLock = new object();

        public AIStateBuildBuilding(Building.Type buildingType)
        {
            type = buildingType;
        }

        void Search()
        {
            uint pos;

            // find a nice spot
            if (!IsEssentialBuilding(game, player) && !IsResourceNeedingBuilding() && ai.StupidDecision()) // no stupid decisions for essential buildings and buildings that need resources!
                pos = FindRandomSpot(game, player, false);
            else
            {
                pos = FindSpot(ai, game, player, (int)playerInfo.Intelligence);

                if (pos == Global.BadMapPos && !IsResourceNeedingBuilding() && (!ai.HardTimes() || type == Building.Type.Sawmill || type == Building.Type.ToolMaker))
                    pos = FindRandomSpot(game, player, true);
            }

            if (pos != Global.BadMapPos && game.CanBuildBuilding(pos, type, player))
            {
                builtPosition = pos;
            }

            lock (searchingLock)
            {
                searching = false;
            }

            if (builtPosition == Global.BadMapPos && ++tries > 10 + playerInfo.Intelligence / 5)
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
                lock(searchingLock)
                {
                    searching = false;
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

            if (builtPosition != Global.BadMapPos)
            {
                if (game.BuildBuilding(builtPosition, type, player))
                {
                    NextState = ai.CreateRandomDelayedState(AI.State.LinkBuilding, 0, (5 - (int)playerInfo.Intelligence / 9) * 1000, builtPosition);

                    // If this was a toolmaker and we are in hard times we set planks for toolmaker to zero for now.
                    // If a tool is needed, this setting is changed by the craft tool AI state.
                    if (type == Building.Type.ToolMaker && ai.HardTimes())
                        player.SetPlanksToolmaker(0u);
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

        bool IsResourceNeedingBuilding()
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

        uint FindRandomSpot(Game game, Player player, bool checkCanBuild)
        {
            var spot = game.Map.GetRandomCoord(game.GetRandom());

            if (checkCanBuild)
            {
                int tries = 0;

                while (!game.CanBuildBuilding(spot, type, player))
                {
                    if (++tries > 20)
                    {
                        spot = FindSpotNearBuilding(game, player, 40, Building.Type.Hut);

                        if (spot == Global.BadMapPos)
                            spot = FindSpotNearBuilding(game, player, 40, Building.Type.Tower);

                        if (spot == Global.BadMapPos)
                            spot = FindSpotNearBuilding(game, player, 40, Building.Type.Fortress);

                        if (spot == Global.BadMapPos)
                            spot = FindSpotNearBuilding(game, player, 40, Building.Type.Castle);

                        return spot;
                    }

                    spot = game.Map.GetRandomCoord(game.GetRandom());
                }
            }

            return spot;
        }

        uint FindSpot(AI ai, Game game, Player player, int intelligence)
        {
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
                    if (ai.HardTimes())
                    {
                        // we need to find fish in territory or near territory
                        var spot = game.Map.FindFirstInTerritory(player.Index, FindFishInTerritory);

                        if (spot != null)
                            return FindSpotNear(game, player, (uint)spot, 3);

                        spot = game.Map.FindFirstInTerritory(player.Index, FindFishNearBorder);

                        if (spot != null)
                            return FindSpotNear(game, player, (uint)spot, 4);

                        return Global.BadMapPos;
                    }
                    else
                        return FindSpotWithFish(game, player, intelligence, 1 + ai.FoodFocus);
                case Building.Type.Forester:
                    {
                        uint spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.Lumberjack, 1 + ai.ConstructionMaterialFocus);

                        if (spot != Global.BadMapPos)
                            return spot;
                        else
                            return FindSpotWithSpace(game, player, intelligence, 1 + ai.ConstructionMaterialFocus);
                    }
                case Building.Type.Hut:
                case Building.Type.Tower:
                case Building.Type.Fortress:
                    {
                        if (ai.HardTimes() && type == Building.Type.Hut)
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

                            uint bestSpot = Global.BadMapPos;
                            int bestDist = int.MaxValue;

                            Func<Map, uint, uint> costFunction = (Map map, uint pos) =>
                            {
                                uint baseCost = (uint)map.FindInArea(pos, 6, FindMilitary, 1).Count * 4;

                                if (!game.Map.HasOwner(pos))
                                    return baseCost;

                                if (game.Map.GetOwner(pos) == player.Index)
                                    return baseCost + 2u;

                                return baseCost + 10u;
                            };

                            Func<Map, uint, bool> checkSpotFunc = (Map map, uint pos) =>
                            {
                                return game.CanBuildBuilding(pos, type, player);
                            };

                            Func<Map, uint, int> rateSpotFunc = (Map map, uint pos) =>
                            {
                                var militaryBuildings = map.FindInArea(pos, 8, FindMilitary, 1);
                                int minDist = int.MaxValue;

                                foreach (var building in militaryBuildings)
                                {
                                    int dist = map.Dist(pos, (building as Building).Position);

                                    if (dist < minDist)
                                        minDist = dist;
                                }

                                return int.MaxValue - minDist;
                            };

                            foreach (var building in game.GetPlayerBuildings(player).Where(b => b.IsMilitary()))
                            {
                                var target = Pathfinder.FindNearestSpot(game.Map, building.Position, targetFunc, costFunction, searchRange);

                                if (target != Global.BadMapPos)
                                {
                                    var spot = building.Position;

                                    for (int i = 0; i < 8; ++i)
                                        spot = game.Map.MoveTowards(spot, target);

                                    spot = game.Map.FindSpotNear(spot, 3, checkSpotFunc, rateSpotFunc);

                                    if (spot != Global.BadMapPos && game.Map.Dist(building.Position, spot) < bestDist)
                                    {
                                        bestSpot = spot;
                                        bestDist = game.Map.Dist(building.Position, target);
                                    }
                                }
                            }

                            if (bestSpot != Global.BadMapPos)
                            {
                                return bestSpot;
                            }

                            return FindSpotNearBorder(game, player, intelligence, 2);
                        }

                        int defendChance = (2 - (ai.ExpandFocus - ai.DefendFocus)) * 25;

                        if (defendChance == 0)
                            defendChance = 15;
                        else if (defendChance == 100)
                            defendChance = 85;

                        defendChance -= 10;

                        if (game.RandomInt() % 100 < defendChance)
                        {
                            // TODO: The order should be a bit random. Otherwise some buildings will be decorated with huts every game.
                            var spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.Stock, 1 + ai.DefendFocus);

                            if (spot == Global.BadMapPos)
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.WeaponSmith, 1 + ai.DefendFocus);

                            if (spot == Global.BadMapPos)
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.GoldSmelter, 1 + ai.DefendFocus);

                            if (spot == Global.BadMapPos)
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.CoalMine, 1 + ai.DefendFocus);

                            if (spot == Global.BadMapPos)
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.IronMine, 1 + ai.DefendFocus);

                            if (spot == Global.BadMapPos)
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.GoldMine, 1 + ai.DefendFocus);

                            if (spot == Global.BadMapPos)
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.SteelSmelter, 1 + ai.DefendFocus);

                            if (spot == Global.BadMapPos)
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.ToolMaker, 1 + ai.DefendFocus);

                            if (spot == Global.BadMapPos)
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.Sawmill, 1 + ai.DefendFocus);

                            if (spot == Global.BadMapPos)
                                spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.Castle, 1 + ai.DefendFocus);

                            if (spot == Global.BadMapPos)
                                return FindSpotNearBorder(game, player, intelligence, 1 + Math.Min(2, (ai.MilitaryFocus + ai.ExpandFocus + ai.DefendFocus) / 2));

                            return spot;
                        }
                        else
                            return FindSpotNearBorder(game, player, intelligence, 1 + Math.Min(2, (ai.MilitaryFocus + ai.ExpandFocus + ai.DefendFocus) / 2));
                    }
                case Building.Type.GoldMine:
                    return FindSpotWithMinerals(game, player, intelligence, Map.Minerals.Coal, 1 + ai.GoldFocus);
                case Building.Type.GoldSmelter:
                    {
                        uint spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.GoldMine, 2 + ai.GoldFocus);

                        if (spot == Global.BadMapPos)
                            spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.CoalMine, 2 + ai.GoldFocus);

                        if (spot != Global.BadMapPos)
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

                        if (spot != Global.BadMapPos)
                            return spot;
                    }
                    return Global.BadMapPos;
                case Building.Type.Mill:
                    return FindSpotNearBuilding(game, player, intelligence, Building.Type.Farm, 1 + ai.FoodFocus);
                case Building.Type.PigFarm:
                    return FindSpotNearBuilding(game, player, intelligence, Building.Type.Farm, 2 + ai.FoodFocus);
                case Building.Type.Sawmill:
                    return FindSpotNearBuilding(game, player, intelligence, Building.Type.Lumberjack, 1 + Math.Min(1, ai.ConstructionMaterialFocus));
                case Building.Type.SteelSmelter:
                    {
                        uint spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.IronMine, 2 + Math.Max(ai.SteelFocus, ai.MilitaryFocus));

                        if (spot == Global.BadMapPos)
                            spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.CoalMine, 2 + Math.Max(ai.SteelFocus, ai.MilitaryFocus));

                        if (spot != Global.BadMapPos)
                            return spot;
                        else if (ai.MilitaryFocus >= ai.SteelFocus)
                            return FindSpotNearBuilding(game, player, intelligence, Building.Type.WeaponSmith, 2 + ai.MilitaryFocus);
                        else
                            return FindSpotNearBuilding(game, player, intelligence, Building.Type.ToolMaker, 2 + ai.SteelFocus);
                    }
                case Building.Type.Stock:
                    return FindSpotForStock(game, player, intelligence, 1 + ai.BuildingFocus);
                case Building.Type.Stonecutter:
                    {
                        var spot = FindSpotWithStones(game, player, intelligence, 1 + Math.Min(1, ai.ConstructionMaterialFocus));

                        if (spot != Global.BadMapPos && BuildingsInArea(game.Map, spot, 7, Building.Type.Stonecutter, FindBuilding, 1) > 0)
                            spot = Global.BadMapPos; // don't build two stonecutters near each other

                        return spot;
                    }
                case Building.Type.StoneMine:
                    return FindSpotWithMinerals(game, player, intelligence, Map.Minerals.Stone, 1 + ai.ConstructionMaterialFocus);
                case Building.Type.ToolMaker:
                    return FindSpotNearBuilding(game, player, intelligence, Building.Type.Stock, 1);
                case Building.Type.WeaponSmith:
                    {
                        uint spot = FindSpotNearBuilding(game, player, intelligence, Building.Type.SteelSmelter, 2 + ai.MilitaryFocus);

                        if (spot != Global.BadMapPos)
                            return spot;
                        else
                            return FindSpotNearBuilding(game, player, intelligence, Building.Type.Stock, 2 + ai.MilitaryFocus);
                    }
                default:
                    return FindRandomSpot(game, player, true);
            }
        }

        uint FindSpotNear(Game game, Player player, uint position, int maxDist)
        {
            var spots = game.Map.FindInArea(position, maxDist, FindEmptySpot, 1);

            while (spots.Count > 0)
            {
                uint spot = (uint)spots[game.RandomInt() % spots.Count];

                if (game.CanBuildBuilding(spot, type, player))
                    return spot;

                spots.Remove(spot);
            }

            return Global.BadMapPos;
        }

        uint FindSpotNearBuilding(Game game, Player player, int intelligence, Building.Type buildingType, int maxInArea = int.MaxValue)
        {
            var buildings = game.GetPlayerBuildings(player).Where(b => b.BuildingType == buildingType).ToList();

            while (buildings.Count > 0)
            {
                var randomBuilding = buildings[game.RandomInt() % buildings.Count];

                if (CheckMaxInAreaOk(game.Map, randomBuilding.Position, 6, buildingType, maxInArea))
                    return FindSpotNear(game, player, randomBuilding.Position, 4);

                buildings.Remove(randomBuilding);
            }

            return Global.BadMapPos;
        }

        uint FindSpotWithMinerals(Game game, Player player, int intelligence, Map.Minerals mineral, int maxInArea = int.MaxValue)
        {
            // search for minerals near castle, mines and military buildings
            var buildings = game.GetPlayerBuildings(player).Where(b =>
                b.BuildingType == Building.Type.Castle ||
                b.BuildingType == Building.Type.StoneMine ||
                b.BuildingType == Building.Type.CoalMine ||
                b.BuildingType == Building.Type.IronMine ||
                b.BuildingType == Building.Type.GoldMine ||
                b.BuildingType == Building.Type.Hut ||
                b.BuildingType == Building.Type.Tower ||
                b.BuildingType == Building.Type.Fortress
            ).ToList();

            while (buildings.Count > 0)
            {
                var randomBuilding = buildings[game.RandomInt() % buildings.Count];

                if (CheckMaxInAreaOk(game.Map, randomBuilding.Position, 9, AIStateFindOre.MineTypes[(int)mineral], maxInArea))
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

            return Global.BadMapPos;
        }

        uint FindSpotWithSpace(Game game, Player player, int intelligence, int maxInArea = int.MaxValue)
        {
            // TODO
            return Global.BadMapPos;
        }

        uint FindSpotWithFish(Game game, Player player, int intelligence, int maxInArea = int.MaxValue)
        {
            // search for fish near castle, fishers and military buildings
            var buildings = game.GetPlayerBuildings(player).Where(b =>
                b.BuildingType == Building.Type.Castle ||
                b.BuildingType == Building.Type.Fisher ||
                b.BuildingType == Building.Type.Hut ||
                b.BuildingType == Building.Type.Tower ||
                b.BuildingType == Building.Type.Fortress
            ).ToList();

            while (buildings.Count > 0)
            {
                var randomBuilding = buildings[game.RandomInt() % buildings.Count];

                if (CheckMaxInAreaOk(game.Map, randomBuilding.Position, 7, Building.Type.Fisher, maxInArea))
                {
                    if (AmountInArea(game.Map, randomBuilding.Position, 8, CountFish, FindFish) > 0)
                    {
                        Func<Map, uint, bool> findFish = (Map map, uint pos) =>
                        {
                            return FindFish(map, pos).Success;
                        };

                        var spot = game.Map.FindSpotNear(randomBuilding.Position, 8, findFish, game.GetRandom(), 1);

                        return FindSpotNear(game, player, spot, 4);
                    }
                }

                buildings.Remove(randomBuilding);
            }

            return Global.BadMapPos;
        }

        uint FindSpotWithTrees(Game game, Player player, int intelligence, int minTrees = 6, int maxInArea = int.MaxValue)
        {
            // search for trees near castle, lumberjacks, foresters and military buildings
            var buildings = game.GetPlayerBuildings(player).Where(b =>
                b.BuildingType == Building.Type.Castle ||
                b.BuildingType == Building.Type.Forester ||
                b.BuildingType == Building.Type.Lumberjack ||
                b.BuildingType == Building.Type.Hut ||
                b.BuildingType == Building.Type.Tower ||
                b.BuildingType == Building.Type.Fortress
            ).ToList();

            while (buildings.Count > 0)
            {
                var randomBuilding = buildings[game.RandomInt() % buildings.Count];

                if (CheckMaxInAreaOk(game.Map, randomBuilding.Position, 7, Building.Type.Lumberjack, maxInArea))
                {
                    if (AmountInArea(game.Map, randomBuilding.Position, 8, CountMapObjects, FindTree) >= minTrees)
                    {
                        Func<Map, uint, bool> findTree = (Map map, uint pos) =>
                        {
                            return FindTree(map, pos).Success;
                        };

                        var spot = game.Map.FindSpotNear(randomBuilding.Position, 8, findTree, game.GetRandom(), 1);

                        int maxDist = 3;

                        if (!game.Map.HasOwner(spot) || game.Map.GetOwner(spot) != player.Index)
                            maxDist = 6;

                        return FindSpotNear(game, player, spot, maxDist);
                    }
                }

                buildings.Remove(randomBuilding);
            }

            return Global.BadMapPos;
        }

        uint FindSpotWithStones(Game game, Player player, int intelligence, int maxInArea = int.MaxValue)
        {
            // search for stones near castle, stonecutters and military buildings
            var buildings = game.GetPlayerBuildings(player).Where(b =>
                b.BuildingType == Building.Type.Castle ||
                b.BuildingType == Building.Type.Stonecutter ||
                b.BuildingType == Building.Type.Hut ||
                b.BuildingType == Building.Type.Tower ||
                b.BuildingType == Building.Type.Fortress
            ).ToList();

            while (buildings.Count > 0)
            {
                var randomBuilding = buildings[game.RandomInt() % buildings.Count];

                if (CheckMaxInAreaOk(game.Map, randomBuilding.Position, 7, Building.Type.Stonecutter, maxInArea))
                {
                    if (AmountInArea(game.Map, randomBuilding.Position, 8, CountMapObjects, FindStone) > 0)
                    {
                        Func<Map, uint, bool> findStone = (Map map, uint pos) =>
                        {
                            return FindStone(map, pos).Success;
                        };

                        var spot = game.Map.FindSpotNear(randomBuilding.Position, 8, findStone, game.GetRandom(), 1);

                        return FindSpotNear(game, player, spot, 3);
                    }
                }

                buildings.Remove(randomBuilding);
            }

            return Global.BadMapPos;
        }

        uint FindSpotNearBorder(Game game, Player player, int intelligence, int maxInArea = int.MaxValue)
        {
            var militaryBuildings = game.GetPlayerBuildings(player).Where(b => b.IsMilitary());
            List<Building> possibleBaseBuildings = new List<Building>();
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

            if (spot == Global.BadMapPos)
                spot = game.Map.FindSpotNear(bestBaseBuilding.Position, 9, IsEmptySpotWithoutMuchMilitary, game.GetRandom(), 4);

            return spot;
        }

        uint FindSpotForStock(Game game, Player player, int intelligence, int maxInArea = int.MaxValue)
        {
            // TODO
            return Global.BadMapPos;
        }


        #region Map analysis

        static Map.FindData FindTree(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.GetObject(pos) >= Map.Object.Tree0 && map.GetObject(pos) <= Map.Object.Pine7
            };
        }

        static Map.FindData FindStone(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.GetObject(pos) >= Map.Object.Stone0 && map.GetObject(pos) <= Map.Object.Stone7
            };
        }

        Map.FindData FindBuilding(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.HasBuilding(pos) && map.GetOwner(pos) == player.Index,
                Data = game.GetBuildingAtPos(pos)
            };
        }

        Map.FindData FindMilitary(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.HasBuilding(pos) && game.GetBuildingAtPos(pos).IsMilitary() && map.GetOwner(pos) == player.Index,
                Data = game.GetBuildingAtPos(pos)
            };
        }

        static Map.FindData FindFish(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.IsInWater(pos) && map.GetResourceFish(pos) > 0u,
                Data = map.GetResourceFish(pos)
            };
        }

        static Map.FindData FindFishInTerritory(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.IsInWater(pos) && map.GetResourceFish(pos) > 0u,
                Data = pos
            };
        }

        static Map.FindData FindFishNearBorder(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.FindInArea(pos, 4, FindFish, 1).Any(),
                Data = pos
            };
        }

        static Map.FindData FindMineral(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = FindMountain(map, pos) && map.GetResourceAmount(pos) > 0u,
                Data = new KeyValuePair<Map.Minerals, uint>(map.GetResourceType(pos), map.GetResourceAmount(pos))
            };
        }

        static bool FindMountainOutsideTerritory(Map map, uint pos)
        {
            return FindMountain(map, pos) && !map.HasOwner(pos);
        }

        static bool FindMountain(Map map, uint pos)
        {
            return (map.TypeUp(pos) >= Map.Terrain.Tundra0 && map.TypeUp(pos) <= Map.Terrain.Tundra2) ||
                   (map.TypeDown(pos) >= Map.Terrain.Tundra0 && map.TypeDown(pos) <= Map.Terrain.Tundra2);
        }

        static bool FindWater(Map map, uint pos)
        {
            return (map.TypeUp(pos) >= Map.Terrain.Water0 && map.TypeUp(pos) <= Map.Terrain.Water3) ||
                   (map.TypeDown(pos) >= Map.Terrain.Water0 && map.TypeDown(pos) <= Map.Terrain.Water3);
        }

        Map.FindData FindEmptySpot(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = Map.MapSpaceFromObject[(int)map.GetObject(pos)] == Map.Space.Open && map.GetOwner(pos) == player.Index,
                Data = pos
            };
        }

        static int CountMapObjects(List<object> data)
        {
            return data.Count;
        }

        static int CountFish(List<object> data)
        {
            return data.Select(d => (int)(uint)d).Sum();
        }

        static int AmountInArea(Map map, uint basePosition, int range, Func<List<object>, int> countFunc, Func<Map, uint, Map.FindData> searchFunc, int minDist = 0)
        {
            return countFunc(map.FindInArea(basePosition, range, searchFunc, minDist));
        }

        static int BuildingsInArea(Map map, uint basePosition, int range, Building.Type buildingType, Func<Map, uint, Map.FindData> searchFunc, int minDist = 0)
        {
            return map.FindInArea(basePosition, range, searchFunc, minDist).Count(f => (f as Building).BuildingType == buildingType);
        }

        int MilitaryBuildingsInArea(Map map, uint basePosition, int range, int minDist = 0, bool includeCastle = true)
        {
            return map.FindInArea(basePosition, range, FindBuilding, minDist).Count(f => (f as Building).IsMilitary(includeCastle));
        }

        bool IsEmptySpotWithoutMuchMilitary(Map map, uint basePosition)
        {
            return FindEmptySpot(map, basePosition).Success && MilitaryBuildingsInArea(map, basePosition, 8) < 3;
        }

        bool IsEmptySpotWithoutMilitary(Map map, uint basePosition)
        {
            return FindEmptySpot(map, basePosition).Success && MilitaryBuildingsInArea(map, basePosition, 8) <= 1;
        }

        static int MineralsInArea(Map map, uint basePosition, int range, Map.Minerals mineral, Func<Map, uint, Map.FindData> searchFunc, int minDist = 0)
        {
            return map.FindInArea(basePosition, range, searchFunc, minDist).Where(f => ((KeyValuePair<Map.Minerals, uint>)f).Key == mineral).Select(f => (int)((KeyValuePair<Map.Minerals, uint>)f).Value).Sum();
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
