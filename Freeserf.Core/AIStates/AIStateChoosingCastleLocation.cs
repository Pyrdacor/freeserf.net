using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.AIStates
{
    class AIStateChoosingCastleLocation : AIState
    {
        const int scansPerUpdate = 50;
        bool built = false;
        int tries = 0;
        int goldFocus = -1;
        int steelFocus = -1;
        int militaryFocus = -1;
        int buildingFocus = -1;
        int foodFocus = -1;
        int constructionMaterialFocus = -1;
        int aggressivity = -1;

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
                var pos = game.Map.GetRandomCoord(game.GetRandom());
                int foundPos = CheckCastleSpot(game, player, pos, (int)playerInfo.Intelligence);

                if (foundPos != -1 && game.CanBuildCastle((uint)foundPos, player))
                {
                    // If found a good spot, build the castle
                    if (!game.BuildCastle((uint)foundPos, player))
                        continue; // failed -> try again
                    else
                        built = true;
                }
            }

            // lower the ai focus after several tries to finally find a spot
            if (++tries % 20 == 0)
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

        Map.FindData FindTree(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.GetObject(pos) >= Map.Object.Tree0 && map.GetObject(pos) <= Map.Object.Pine7
            };
        }

        Map.FindData FindStone(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.GetObject(pos) >= Map.Object.Stone0 && map.GetObject(pos) <= Map.Object.Stone7
            };
        }

        Map.FindData FindFish(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.GetResourceFish(pos) > 0u
            };
        }

        Map.FindData FindMineral(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.GetResourceAmount(pos) > 0u,
                Data = new KeyValuePair<Map.Minerals, uint>(map.GetResourceType(pos), map.GetResourceAmount(pos))
            };
        }

        Map.FindData FindMountain(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = (map.TypeUp(pos) >= Map.Terrain.Tundra0 && map.TypeUp(pos) <= Map.Terrain.Tundra2) ||
                    (map.TypeDown(pos) >= Map.Terrain.Tundra0 && map.TypeDown(pos) <= Map.Terrain.Tundra2)
            };
        }

        Map.FindData FindWater(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = (map.TypeUp(pos) >= Map.Terrain.Water0 && map.TypeUp(pos) <= Map.Terrain.Water3) ||
                    (map.TypeDown(pos) >= Map.Terrain.Water0 && map.TypeDown(pos) <= Map.Terrain.Water3)
            };
        }

        Map.FindData FindDesert(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = (map.TypeUp(pos) >= Map.Terrain.Desert0 && map.TypeUp(pos) <= Map.Terrain.Desert2) ||
                    (map.TypeDown(pos) >= Map.Terrain.Desert0 && map.TypeDown(pos) <= Map.Terrain.Desert2)
            };
        }

        int CheckCastleSpot(Game game, Player player, uint pos, int intelligence)
        {
            var map = game.Map;

            int treeCount = map.FindInArea(pos, 7, FindTree, 1).Count;
            int stoneCount = map.FindInArea(pos, 7, FindStone, 1).Count;
            int fishCount = map.FindInArea(pos, 9, FindFish, 1).Count;
            int mountainCountNear = map.FindInArea(pos, 3, FindMountain, 0).Count;
            int mountainCountFar = map.FindInArea(pos, 9, FindMountain, 4).Count;
            int desertCount = map.FindInArea(pos, 9, FindDesert, 0).Count;
            int waterCount = map.FindInArea(pos, 9, FindWater, 0).Count;

            int numLargeSpots = 3;
            int numSmallSpots = 3;
            bool keepDistanceToEnemies = aggressivity < 2;

            // if we tried too often we will only assure that there is a bit of trees and stones
            if (tries >= 40)
            {
                if (treeCount < 5 || stoneCount < 2)
                    return -1;

                if (mountainCountNear > 8) // too close to mountain
                    return -1;

                if (desertCount > 6) // too much desert
                    return -1;

                if (waterCount > 8) // too much water
                    return -1;

                numLargeSpots = 2; // the toolmaker can be build when we have expanded the land

                if (tries >= 80 && player.GetInitialSupplies() > 4)
                    numLargeSpots = 1; // we need to expand the territory to build the sawmill

                keepDistanceToEnemies = false;
            }
            else
            {
                if (mountainCountNear > 4) // too close to mountain
                    return -1;

                if (desertCount > 6) // too much desert
                    return -1;

                if (desertCount + waterCount + mountainCountNear + mountainCountFar > 10) // too much desert/water/mountain
                    return -1;

                var minerals = map.FindInArea(pos, 9, FindMineral, 1).Select(m => (KeyValuePair<Map.Minerals, uint>)m);

                int stoneOreCount = minerals.Where(m => m.Key == Map.Minerals.Stone).Select(m => (int)m.Value).Sum();
                int goldCount = minerals.Where(m => m.Key == Map.Minerals.Gold).Select(m => (int)m.Value).Sum();
                int ironCount = minerals.Where(m => m.Key == Map.Minerals.Iron).Select(m => (int)m.Value).Sum();
                int coalCount = minerals.Where(m => m.Key == Map.Minerals.Coal).Select(m => (int)m.Value).Sum();

                // low intelligence will treat half the mountains as the right mineral without checking
                int halfMountainCount = (mountainCountNear + mountainCountFar) / 2;

                int minConstructionCount = 3 + intelligence / 10 + Math.Max(buildingFocus, constructionMaterialFocus) * 5;

                if (treeCount + stoneCount + Math.Max(halfMountainCount, stoneOreCount) < minConstructionCount)
                    return -1;

                int minFishCount = foodFocus;

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

            if (keepDistanceToEnemies)
            {
                for (uint i = 0; i < game.GetPlayerCount(); ++i)
                {
                    var enemy = game.GetPlayer(i);

                    if (enemy == player || !enemy.HasCastle())
                        continue;

                    int dist = Math.Min(Math.Abs(game.Map.DistX(pos, enemy.CastlePos)), Math.Abs(game.Map.DistY(pos, enemy.CastlePos)));

                    if (dist < 30)
                        return -1;
                }
            }

            // check if we can build at least: lumberjack, stonecutter, sawmill, toolmaker and one hut
            // -> 3 small and 2 large buildings
            // but we check the castle too, so 3 large buildings
            int numSmall = 0;
            int numLarge = 0;
            List<uint> largeSpots = new List<uint>();

            for (int i = 0; i < 271; ++i)
            {
                uint checkPos = map.PosAddSpirally(pos, (uint)i);

                if (game.CanBuildLarge(checkPos))
                {
                    ++numSmall;
                    ++numLarge;
                    largeSpots.Add(checkPos);
                }
                else if (game.CanBuildSmall(checkPos))
                {
                    ++numSmall;
                }

                // TODO: What if a building would block another building spot.
                if (numLarge >= numLargeSpots && numSmall >= numSmallSpots)
                    return (int)largeSpots[0];
            }

            return -1;
        }
    }
}
