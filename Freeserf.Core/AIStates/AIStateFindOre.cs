using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.AIStates
{
    // Find ore and build a mine there
    class AIStateFindOre : AIState
    {
        Map.Minerals oreType = Map.Minerals.None;

        public AIStateFindOre(Map.Minerals oreType)
        {
            this.oreType = oreType;
        }

        static readonly Building.Type[] mineTypes = new Building.Type[4]
        {
            Building.Type.GoldMine,
            Building.Type.IronMine,
            Building.Type.CoalMine,
            Building.Type.StoneMine
        };

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            if (oreType == Map.Minerals.None)
            {
                Kill(ai);
                return;
            }

            uint spot = 0;
            var mineType = mineTypes[(int)oreType - 1];
            var largeSpots = AI.GetMemorizedMineralSpots(oreType, true).ToList();
            var smallSpots = AI.GetMemorizedMineralSpots(oreType, true).Where(s => !largeSpots.Contains(s)).ToList();
            bool considerSmallSpots = (ai.GameTime > 120 * Global.TICKS_PER_SEC + playerInfo.Intelligence * 30 * Global.TICKS_PER_SEC) || ai.StupidDecision();

            while (true)
            {
                // look for memorized large spot
                if (largeSpots.Count > 0)
                {
                    int index = game.GetRandom().Next() % largeSpots.Count;
                    spot = largeSpots[index];

                    if (game.BuildBuilding(spot, mineType, player))
                    {
                        Kill(ai);
                        break;
                    }

                    largeSpots.RemoveAt(index);
                }
                else if (considerSmallSpots && smallSpots.Count > 0)
                {
                    int index = game.GetRandom().Next() % smallSpots.Count;
                    spot = smallSpots[index];

                    if (game.BuildBuilding(spot, mineType, player))
                    {
                        Kill(ai);
                        break;
                    }

                    smallSpots.RemoveAt(index);
                }
                else
                {
                    // no valid mineral spots found -> send geologists
                    var geologists = game.GetPlayerSerfs(player).Where(s => s.GetSerfType() == Serf.Type.Geologist).ToList();

                    if (geologists.Count == 0) // no geologists? try to train them
                    {
                        if (!SendGeologist(ai, game, player))
                        {
                            // TODO: what should we do then? -> try to craft a hammer? wait for generics? abort?
                            Kill(ai);
                            ai.CreateRandomDelayedState(AI.State.FindOre, 10000, (120 - (int)playerInfo.Intelligence) * 2000, oreType);
                            return;
                        }
                    }
                    else
                    {
                        int totalGeologistCount = geologists.Count;
                        geologists = geologists.Where(g => g.SerfState == Serf.State.IdleInStock).ToList();
                        int sentOutGeologistCount = totalGeologistCount - geologists.Count;

                        if (geologists.Count > 0 && sentOutGeologistCount < 2)
                        {
                            if (!SendGeologist(ai, game, player))
                            {
                                // TODO: what should we do then? -> try to craft a hammer? wait for generics? abort?
                                Kill(ai);
                                return;
                            }
                        }
                        else // this means there are geologist but none in stock (so they are already looking for minerals) or 2 or more are already looking for minerals
                        {
                            Kill(ai);
                            // check again in a while
                            ai.CreateRandomDelayedState(AI.State.FindOre, 30000, (120 - (int)playerInfo.Intelligence) * 2000, oreType);
                            return;
                        }
                    }

                    break;
                }
            }

            Kill(ai);
        }

        int MineralsInArea(Map map, uint basePosition, int range, Map.Minerals mineral, Func<Map, uint, Map.FindData> searchFunc, int minDist = 0)
        {
            return map.FindInArea(basePosition, range, searchFunc, minDist).Where(f => ((KeyValuePair<Map.Minerals, uint>)f).Key == mineral).Select(f => (int)((KeyValuePair<Map.Minerals, uint>)f).Value).Sum();
        }

        bool FindMountain(Map map, uint pos, bool withFlag)
        {
            if (withFlag && !map.HasFlag(pos))
                return false;

            return (map.TypeUp(pos) >= Map.Terrain.Tundra0 && map.TypeUp(pos) <= Map.Terrain.Tundra2) ||
                (map.TypeDown(pos) >= Map.Terrain.Tundra0 && map.TypeDown(pos) <= Map.Terrain.Tundra2);
        }

        bool FindMountain(Map map, uint pos)
        {
            return FindMountain(map, pos, false);
        }

        bool FindMountainWithFlag(Map map, uint pos)
        {
            return FindMountain(map, pos, true);
        }

        void FindNearbyMountain(Game game, ref uint pos)
        {
            pos = game.Map.FindSpotNear(pos, 17, FindMountainWithFlag, game.GetRandom());

            if (pos == Global.BadMapPos)
                pos = game.Map.FindSpotNear(pos, 17, FindMountain, game.GetRandom());
        }

        Map.FindData FindFlag(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.HasFlag(pos),
                Data = pos
            };
        }

        bool SendGeologist(AI ai, Game game, Player player)
        {
            var militaryBuildings = game.GetPlayerBuildings(player).Where(b => b.IsMilitary());

            List<uint> possibleSpots = new List<uint>();

            // search for mountains near military buildings
            foreach (var building in militaryBuildings)
            {
                uint pos = building.Position;
                FindNearbyMountain(game, ref pos);

                if (pos != Global.BadMapPos)
                {
                    possibleSpots.Add(pos);
                }
            }

            if (possibleSpots.Count == 0) // no mountains in territory
            {
                // we need to increase our territory
                if (player.GetIncompleteBuildingCount(Building.Type.Hut) == 0 &&
                    player.GetIncompleteBuildingCount(Building.Type.Tower) == 0 &&
                    player.GetIncompleteBuildingCount(Building.Type.Fortress) == 0 &&
                    !game.GetPlayerBuildings(player).Where(b => b.IsMilitary(false)).Any(b => !b.HasSerf()))
                {
                    // build only if there are no military buildings in progress or
                    // military buildings that are not occupied yet
                    NextState = ai.CreateState(AI.State.BuildBuilding, Building.Type.Hut);
                }

                return false;
            }

            var spotsWithFlag = possibleSpots.Where(s => game.Map.HasFlag(s)).ToList();

            uint spot = (spotsWithFlag.Count > 0) ? spotsWithFlag[game.GetRandom().Next() % spotsWithFlag.Count] :
                possibleSpots[game.GetRandom().Next() % possibleSpots.Count];

            Flag flag = game.GetFlagAtPos(spot);

            if (flag == null)
            {
                if (!game.BuildFlag(spot, player))
                    return false;

                flag = game.GetFlagAtPos(spot);

                // link flag
                var flagPositions = game.Map.FindInArea(flag.Position, 7, FindFlag, 2).Select(d => (uint)d);
                Road bestRoad = null;

                foreach (var flagPos in flagPositions)
                {
                    var road = Pathfinder.Map(game.Map, flag.Position, flagPos);

                    if (road != null && road.Valid)
                    {
                        if (bestRoad == null || bestRoad.Length > road.Length)
                            bestRoad = road;
                    }
                }

                if (bestRoad != null)
                    game.BuildRoad(bestRoad, player);
            }

            return game.SendGeologist(flag);
        }
    }
}
