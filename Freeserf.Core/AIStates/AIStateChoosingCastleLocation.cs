/*
 * AIStateChoosingCastleLocation.cs - AI state to find a good castle location
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

namespace Freeserf.AIStates
{
    using MapPos = UInt32;

    class AIStateChooseCastleLocation : AIState
    {
        const int scansPerUpdate = 50;
        bool built = false;
        int numTries = 0;
        int goldFocus = -1;
        int steelFocus = -1;
        int militaryFocus = -1;
        int buildingFocus = -1;
        int foodFocus = -1;
        int constructionMaterialFocus = -1;
        int aggressivity = -1;

        public AIStateChooseCastleLocation()
            : base(AI.State.ChooseCastleLocation)
        {

        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            if (goldFocus == -1)
            {
                goldFocus = ai.GoldFocus;
                steelFocus = ai.SteelFocus;
                militaryFocus = ai.MilitaryFocus;
                buildingFocus = ai.BuildingFocus;
                foodFocus = ai.FoodFocus;
                constructionMaterialFocus = ai.ConstructionMaterialFocus;
                aggressivity = ai.Aggressivity;
            }

            for (int i = 0; i < scansPerUpdate; ++i)
            {
                var position = game.Map.GetRandomCoordinate(game.GetRandom());
                int foundPosition = CheckCastleSpot(game, player, position, (int)playerInfo.Intelligence);

                if (foundPosition != -1 && game.CanBuildCastle((uint)foundPosition, player))
                {
                    // If found a good spot, build the castle
                    if (!game.BuildCastle((uint)foundPosition, player))
                        continue; // failed -> try again
                    else
                    {
                        built = true;
                        break;
                    }
                }
            }

            // lower the ai focus after several tries to finally find a spot (only on small maps)
            if (++numTries % (5 + game.Map.Size * 5) == 0)
            {
                if (goldFocus > 0)
                    --goldFocus;

                if (steelFocus > 0)
                    --steelFocus;

                if (militaryFocus > 0)
                    --militaryFocus;

                if (buildingFocus > 0)
                    --buildingFocus;

                if (foodFocus > 0)
                    --foodFocus;

                if (constructionMaterialFocus > 0)
                    --constructionMaterialFocus;
            }

            if (built)
                GoToState(ai, AI.State.CastleBuilt);
        }

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

        static Map.FindData FindFish(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = FindWater(map, position).Success && map.GetResourceFish(position) > 0u
            };
        }

        static Map.FindData FindMineral(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = FindMountain(map, position).Success && map.GetResourceAmount(position) > 0u,
                Data = new KeyValuePair<Map.Minerals, uint>(map.GetResourceType(position), map.GetResourceAmount(position))
            };
        }

        static Map.FindData FindMountain(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = (map.TypeUp(position) >= Map.Terrain.Tundra0 && map.TypeUp(position) <= Map.Terrain.Tundra2) ||
                    (map.TypeDown(position) >= Map.Terrain.Tundra0 && map.TypeDown(position) <= Map.Terrain.Tundra2)
            };
        }

        static Map.FindData FindWater(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = (map.TypeUp(position) >= Map.Terrain.Water0 && map.TypeUp(position) <= Map.Terrain.Water3) ||
                    (map.TypeDown(position) >= Map.Terrain.Water0 && map.TypeDown(position) <= Map.Terrain.Water3)
            };
        }

        static Map.FindData FindDesert(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = (map.TypeUp(position) >= Map.Terrain.Desert0 && map.TypeUp(position) <= Map.Terrain.Desert2) ||
                    (map.TypeDown(position) >= Map.Terrain.Desert0 && map.TypeDown(position) <= Map.Terrain.Desert2)
            };
        }

        int CheckCastleSpot(Game game, Player player, MapPos position, int intelligence)
        {
            var map = game.Map;

            int treeCount = map.FindInArea(position, 5, FindTree, 1).Count;
            int stoneCount = map.FindInArea(position, 5, FindStone, 1).Count;
            int fishCount = map.FindInArea(position, 7, FindFish, 1).Count;
            int mountainCountNear = map.FindInArea(position, 3, FindMountain, 0).Count;
            int mountainCountFar = map.FindInArea(position, 9, FindMountain, 4).Count;
            int desertCount = map.FindInArea(position, 6, FindDesert, 0).Count;
            int waterCount = map.FindInArea(position, 6, FindWater, 0).Count;

            int numLargeSpots = 3;
            int numSmallSpots = 3;
            int keepDistanceToEnemies = 30 - (aggressivity / 2) * 10;

            if (player.InitialSupplies < 5)
            {
                // we need coal and iron close enough when starting with low supplies
                var minerals = map.FindInArea(position, 9, FindMineral, 1).Select(m => (KeyValuePair<Map.Minerals, uint>)m);

                int ironCount = minerals.Where(m => m.Key == Map.Minerals.Iron).Select(m => (int)m.Value).Sum();
                int coalCount = minerals.Where(m => m.Key == Map.Minerals.Coal).Select(m => (int)m.Value).Sum();

                if (ironCount == 0 || coalCount == 0)
                    return -1;
            }
            else // good amount of starting resources
            {
                if (map.Size > 5)
                {
                    if (treeCount < 12 || stoneCount < 6)
                        return -1;
                }
                else if (map.Size == 5)
                {
                    if (treeCount < 9 || stoneCount < 5)
                        return -1;
                }
                else if (map.Size == 4)
                {
                    if (treeCount < 7 || stoneCount < 4)
                        return -1;
                }
            }

            // if we tried too often we will only assure that there is a bit of trees and stones
            if (numTries >= 25 + map.Size * 5)
            {
                if (treeCount < 5 || stoneCount < 2)
                    return -1;

                if (numTries < 500) // after 500 tries, just place it somewhere
                {
                    if (mountainCountNear > 7) // too close to mountain
                        return -1;

                    if (desertCount > 6) // too much desert
                        return -1;

                    if (waterCount > 8) // too much water
                        return -1;

                    if (player.InitialSupplies < 3) // with 3 or more we have 1 iron ore and 1 coal
                    {
                        if (fishCount == 0 || stoneCount < 2 || treeCount < 5)
                            return -1;
                    }

                    numLargeSpots = 2; // the toolmaker can be build when we have expanded the land

                    if (numTries >= 80 && player.InitialSupplies > 4)
                        numLargeSpots = 1; // we need to expand the territory to build the sawmill

                    if (game.Map.Size < 5)
                    {
                        // in small maps we no longer force enemy distance after many tries
                        if (numTries >= 120)
                            keepDistanceToEnemies = 0;
                        else if (numTries >= 80)
                            keepDistanceToEnemies = 15;
                    }
                    else if (numTries >= 200) // if tried very long we will have mercy in any case
                    {
                        keepDistanceToEnemies = 0;
                    }
                }
            }
            else
            {
                if (mountainCountNear > 4) // too close to mountain
                    return -1;

                if (desertCount > 3) // too much desert
                    return -1;

                if (desertCount + waterCount + mountainCountNear + mountainCountFar > 10) // too much desert/water/mountain
                    return -1;

                var minerals = map.FindInArea(position, 9, FindMineral, 1).Select(m => (KeyValuePair<Map.Minerals, uint>)m);

                int stoneOreCount = minerals.Where(m => m.Key == Map.Minerals.Stone).Select(m => (int)m.Value).Sum();
                int goldCount = minerals.Where(m => m.Key == Map.Minerals.Gold).Select(m => (int)m.Value).Sum();
                int ironCount = minerals.Where(m => m.Key == Map.Minerals.Iron).Select(m => (int)m.Value).Sum();
                int coalCount = minerals.Where(m => m.Key == Map.Minerals.Coal).Select(m => (int)m.Value).Sum();

                if (player.InitialSupplies < 5)
                {
                    if (treeCount < 8 || stoneCount < 4 || fishCount < 1 || ironCount == 0 || coalCount == 0)
                        return -1;
                }

                // low intelligence will treat half the mountains as the right mineral without checking
                int halfMountainCount = (mountainCountNear + mountainCountFar) / 2;

                int minConstructionCount = 3 + intelligence / 10 + Math.Max(buildingFocus, constructionMaterialFocus) * 5;

                if (treeCount + stoneCount + Math.Max(halfMountainCount, stoneOreCount) < minConstructionCount)
                    return -1;

                int minFishCount = foodFocus * 5 + (player.InitialSupplies < 5 ? 1 : 0);

                if (fishCount < minFishCount)
                    return -1;

                int minSteelCount = Math.Max(steelFocus, militaryFocus / 2) * 2;
                int steelOreCount = (intelligence < 20) ? Math.Max(halfMountainCount, ironCount + coalCount) : ironCount + coalCount;

                if (steelOreCount < minSteelCount)
                    return -1;

                int minGoldCount = goldFocus / 2;
                int goldOreCount = (intelligence < 20) ? Math.Max(halfMountainCount, goldCount) : ironCount + coalCount;

                if (goldCount < minGoldCount)
                    return -1;
            }

            if (keepDistanceToEnemies > 0 && numTries < 1000)
            {
                for (uint i = 0; i < game.PlayerCount; ++i)
                {
                    var enemy = game.GetPlayer(i);

                    if (enemy == player || !enemy.HasCastle)
                        continue;

                    int distance = Math.Min(Math.Abs(game.Map.DistanceX(position, enemy.CastlePosition)), Math.Abs(game.Map.DistanceY(position, enemy.CastlePosition)));

                    if (distance < keepDistanceToEnemies)
                        return -1;
                }
            }

            // check if we can build at least: lumberjack, stonecutter, sawmill, toolmaker and one hut
            // -> 3 small and 2 large buildings
            // but we check the castle too, so 3 large buildings
            int numSmall = 0;
            int numLarge = 0;
            List<uint> largeSpots = new List<uint>();

            for (int i = 0; i < 100; ++i)
            {
                uint checkPosition = map.PositionAddSpirally(position, (uint)i);

                if (game.CanBuildLarge(checkPosition))
                {
                    ++numSmall;
                    ++numLarge;
                    largeSpots.Add(checkPosition);
                }
                else if (game.CanBuildSmall(checkPosition))
                {
                    ++numSmall;
                }

                // TODO: What if a building would block another building spot.
                if (numLarge >= numLargeSpots && (numSmall - numLargeSpots) >= numSmallSpots)
                    return (int)largeSpots[0];
            }

            return -1;
        }
    }
}
