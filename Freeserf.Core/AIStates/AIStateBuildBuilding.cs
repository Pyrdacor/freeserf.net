using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.AIStates
{
    // TODO: If we don't have enough materials, we should switch to a state where we increase our material production
    //       or increase the plank amount for constructions.
    // TODO: The ai should prefer large buildings in large spots (especially at the beginning). Otherwise there might
    //       be no room for large building when one is needed.
    class AIStateBuildBuilding : AIState
    {
        bool built = false;
        Building.Type type = Building.Type.None;
        uint builtPosition = Global.BadMapPos;
        int tries = 0;
        Game game = null;

        public AIStateBuildBuilding(Building.Type buildingType)
        {
            type = buildingType;
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            this.game = game;

            if (built)
            {
                if (!Killed)
                {
                    NextState = ai.CreateRandomDelayedState(AI.State.LinkBuilding, 0, (5 - (int)playerInfo.Intelligence / 9) * 1000, builtPosition);
                    Kill(ai);
                }

                return;
            }

            uint pos;

            // find a nice spot
            if (!IsEssentialBuilding(game, player) && ai.StupidDecision()) // no stupid decisions for essential buildings!
                pos = FindRandomSpot(game, player, false);
            else
            {
                pos = FindSpot(ai, game, player, (int)playerInfo.Intelligence);

                if (pos == Global.BadMapPos)
                    pos = FindRandomSpot(game, player, true);
            }

            if (pos != Global.BadMapPos && game.CanBuildBuilding(pos, type, player))
            {
                built = game.BuildBuilding(pos, type, player);

                if (built)
                    builtPosition = pos;
            }

            if (!built && ++tries > 10 + playerInfo.Intelligence / 5)
                Kill(ai); // not able to build the building
        }

        bool IsEssentialBuilding(Game game, Player player)
        {
            switch (type)
            {
                case Building.Type.Lumberjack:
                case Building.Type.Stonecutter:
                case Building.Type.Sawmill:
                    return game.GetPlayerBuildings(player, type).Count() == 0;
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
                        return Global.BadMapPos;

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
                    return FindSpotNearBorder(game, player, intelligence, 1 + ai.MilitaryFocus);
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
                    return FindSpotWithTrees(game, player, intelligence, 1 + ai.ConstructionMaterialFocus);
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
                    return FindSpotWithStones(game, player, intelligence, 1 + Math.Min(1, ai.ConstructionMaterialFocus));
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

                if (CheckMaxInAreaOk(game.Map, randomBuilding.Position, 9, Building.Type.Stonecutter, maxInArea))
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
            // search for stones near castle, fishers and military buildings
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

                if (CheckMaxInAreaOk(game.Map, randomBuilding.Position, 7, Building.Type.Stonecutter, maxInArea))
                {
                    if (AmountInArea(game.Map, randomBuilding.Position, 8, CountFish, FindFish) > 0)
                        return FindSpotNear(game, player, randomBuilding.Position, 3);
                }

                buildings.Remove(randomBuilding);
            }

            return Global.BadMapPos;
        }

        uint FindSpotWithTrees(Game game, Player player, int intelligence, int maxInArea = int.MaxValue)
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
                    if (AmountInArea(game.Map, randomBuilding.Position, 8, CountMapObjects, FindTree) > 6)
                    {
                        Func<Map, uint, bool> findTree = (Map map, uint pos) =>
                        {
                            return FindTree(map, pos).Success;
                        };

                        var spot = game.Map.FindSpotNear(randomBuilding.Position, 8, findTree, game.GetRandom(), 1);

                        return FindSpotNear(game, player, spot, 3);
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
            var militaryBuildings = game.GetPlayerBuildings(player).Where(b =>
                b.BuildingType == Building.Type.Hut ||
                b.BuildingType == Building.Type.Tower ||
                b.BuildingType == Building.Type.Fortress ||
                b.BuildingType == Building.Type.Castle);

            List<Building> possibleBaseBuildings = new List<Building>();
            Building bestBaseBuilding = null;

            foreach (var militaryBuilding in militaryBuildings)
            {
                int numMilitaryBuildingsInArea = MilitaryBuildingsInArea(game.Map, militaryBuilding.Position, 9, 1, false);

                if (numMilitaryBuildingsInArea <= 1)
                {
                    bestBaseBuilding = militaryBuilding;
                    break;
                }

                if (numMilitaryBuildingsInArea <= 2)
                    possibleBaseBuildings.Add(militaryBuilding);
            }

            if (bestBaseBuilding == null && possibleBaseBuildings.Count == 0)
            {
                foreach (var militaryBuilding in militaryBuildings)
                {
                    int numMilitaryBuildingsInArea = MilitaryBuildingsInArea(game.Map, militaryBuilding.Position, 6, 1, false);

                    if (numMilitaryBuildingsInArea <= 1)
                    {
                        bestBaseBuilding = militaryBuilding;
                        break;
                    }

                    if (numMilitaryBuildingsInArea <= 2)
                        possibleBaseBuildings.Add(militaryBuilding);
                }
            }

            if (bestBaseBuilding == null && possibleBaseBuildings.Count == 0)
                return FindRandomSpot(game, player, true);

            if (bestBaseBuilding == null)
            {
                bestBaseBuilding = possibleBaseBuildings[game.RandomInt() % possibleBaseBuildings.Count];
            }

            return game.Map.FindSpotNear(bestBaseBuilding.Position, 8, IsEmptySpot, game.GetRandom(), 4);
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
                Success = map.GetObject(pos) >= Map.Object.SmallBuilding && map.GetObject(pos) <= Map.Object.Castle,
                Data = game.GetBuildingAtPos(pos)
            };
        }

        static Map.FindData FindFish(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.GetResourceFish(pos) > 0u,
                Data = map.GetResourceFish(pos)
            };
        }

        static Map.FindData FindMineral(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.GetResourceAmount(pos) > 0u,
                Data = new KeyValuePair<Map.Minerals, uint>(map.GetResourceType(pos), map.GetResourceAmount(pos))
            };
        }

        static Map.FindData FindEmptySpot(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = Map.MapSpaceFromObject[(int)map.GetObject(pos)] == Map.Space.Open,
                Data = pos
            };
        }

        static int CountMapObjects(List<object> data)
        {
            return data.Count;
        }

        static int CountFish(List<object> data)
        {
            return data.Select(d => (int)d).Sum();
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

        static bool IsEmptySpot(Map map, uint basePosition)
        {
            return FindEmptySpot(map, basePosition).Success;
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
