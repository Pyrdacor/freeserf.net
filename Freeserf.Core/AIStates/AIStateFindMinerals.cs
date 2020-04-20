/*
 * AIStateFindOre.cs - AI state to search for minerals and build mines
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

    // Find ore and build a mine there
    class AIStateFindMinerals : AIState
    {
        Map.Minerals mineralType = Map.Minerals.None;

        public AIStateFindMinerals(Map.Minerals mineralType)
            : base(AI.State.FindOre)
        {
            this.mineralType = mineralType;
        }

        protected override void ReadFrom(Game game, AI ai, string name, SaveReaderText reader)
        {
            base.ReadFrom(game, ai, name, reader);

            mineralType = reader.Value($"{name}.mineral_type").ReadEnum<Map.Minerals>();
        }

        public override void WriteTo(string name, SaveWriterText writer)
        {
            base.WriteTo(name, writer);

            writer.Value($"{name}.mineral_type").Write(mineralType);
        }

        internal static readonly Building.Type[] MineTypes = new Building.Type[4]
        {
            Building.Type.GoldMine,
            Building.Type.IronMine,
            Building.Type.CoalMine,
            Building.Type.StoneMine
        };

        bool CanBuild(Game game, Player player)
        {
            uint numPossibleMiners = player.GetSerfCount(Serf.Type.Miner) + game.GetResourceAmountInInventories(player, Resource.Type.Pick);
            uint numMines = player.GetTotalBuildingCount(Building.Type.GoldMine) + player.GetTotalBuildingCount(Building.Type.IronMine) +
                player.GetTotalBuildingCount(Building.Type.CoalMine) + player.GetTotalBuildingCount(Building.Type.StoneMine);

            return numMines < numPossibleMiners;
        }

        bool CanBuildAtSpot(Game game, Player player, uint spot, int maxInArea = 1)
        {
            var mines = game.Map.FindInArea(spot, 4, FindMine, 1);
            var mine = MineTypes[(int)mineralType - 1];

            return mines.Count(mineIndex => game.GetBuilding((uint)mineIndex).BuildingType == mine) < maxInArea;
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            if (mineralType == Map.Minerals.None)
            {
                Kill(ai);
                return;
            }

            uint spot = 0;
            int maxGeologists = ai.HardTimes ? 2 : 2 + (int)(ai.GameTime / ((45 - Misc.Max(ai.GoldFocus, ai.SteelFocus) * 5) * Global.TICKS_PER_MIN));
            var mineType = MineTypes[(int)mineralType - 1];
            var largeSpots = AI.GetMemorizedMineralSpots(mineralType, true)
                .Where(mineralSpot => game.Map.HasOwner(mineralSpot) && game.Map.GetOwner(mineralSpot) == player.Index).ToList();
            var smallSpots = AI.GetMemorizedMineralSpots(mineralType, false)
                .Where(mineralSpot => game.Map.HasOwner(mineralSpot) && game.Map.GetOwner(mineralSpot) == player.Index && !largeSpots.Contains(mineralSpot)).ToList();
            bool considerSmallSpots = (ai.GameTime > 120 * Global.TICKS_PER_MIN + playerInfo.Intelligence * 30 * Global.TICKS_PER_MIN) || ai.StupidDecision();

            if (ai.HardTimes && ((smallSpots.Count > 0 && ai.GameTime >= 60 * Global.TICKS_PER_MIN) || (smallSpots.Count > 3 && ai.GameTime >= 35 * Global.TICKS_PER_MIN)))
                considerSmallSpots = true;

            while (true)
            {
                bool canBuild = CanBuild(game, player);

                if (!canBuild)
                {
                    ai.PushState(ai.CreateState(AI.State.CraftTool, Resource.Type.Pick));
                    break;
                }

                // look for memorized large spot
                if (largeSpots.Count > 0)
                {
                    int index = game.GetRandom().Next() % largeSpots.Count;
                    spot = largeSpots[index];

                    if (CanBuildAtSpot(game, player, spot, 1 + (int)ai.GameTime / (60 * Global.TICKS_PER_MIN)) && game.BuildBuilding(spot, mineType, player))
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

                    if (CanBuildAtSpot(game, player, spot, 1 + (int)ai.GameTime / (60 * Global.TICKS_PER_MIN)) && game.BuildBuilding(spot, mineType, player))
                    {
                        Kill(ai);
                        break;
                    }

                    smallSpots.RemoveAt(index);
                }
                else if (largeSpots.Count < 6)
                {
                    // no valid mineral spots found -> send geologists
                    var geologists = game.GetPlayerSerfs(player).Where(serf => serf.SerfType == Serf.Type.Geologist).ToList();

                    if (geologists.Count == 0) // no geologists? try to train them
                    {
                        if (!SendGeologist(ai, game, player))
                        {
                            // TODO: what should we do then? -> try to craft a hammer? wait for generics? abort?
                            Kill(ai);
                            ai.CreateRandomDelayedState(AI.State.FindOre, 10000, (120 - (int)playerInfo.Intelligence) * 2000, mineralType);
                            return;
                        }
                    }
                    else
                    {
                        int totalGeologistCount = geologists.Count;
                        geologists = geologists.Where(g => g.SerfState == Serf.State.IdleInStock).ToList();
                        int sentOutGeologistCount = totalGeologistCount - geologists.Count;

                        if (geologists.Count > 0 && sentOutGeologistCount < maxGeologists)
                        {
                            if (!SendGeologist(ai, game, player))
                            {
                                // TODO: what should we do then? -> try to craft a hammer? wait for generics? abort?
                                Kill(ai);
                                return;
                            }
                        }
                        else // this means there are geologists but none in stock (so they are already looking for minerals) or 2 or more are already looking for minerals
                        {
                            Kill(ai);
                            // check again in a while
                            ai.CreateRandomDelayedState(AI.State.FindOre, 30000, (120 - (int)playerInfo.Intelligence) * 2000, mineralType);
                            return;
                        }
                    }

                    break;
                }
            }

            Kill(ai);
        }

        int MineralsInArea(Map map, uint basePosition, int range, Map.Minerals mineral,
            Func<Map, uint, Map.FindData> searchFunc, int minDistance = 0)
        {
            return map.FindInArea(basePosition, range, searchFunc, minDistance)
                .Where(finding => ((KeyValuePair<Map.Minerals, uint>)finding).Key == mineral)
                .Select(finding => (int)((KeyValuePair<Map.Minerals, uint>)finding).Value).Sum();
        }

        bool FindMountain(Map map, MapPos position, bool withFlag)
        {
            if (withFlag && !map.HasFlag(position))
                return false;

            return (map.TypeUp(position) >= Map.Terrain.Tundra0 && map.TypeUp(position) <= Map.Terrain.Tundra2) ||
                   (map.TypeDown(position) >= Map.Terrain.Tundra0 && map.TypeDown(position) <= Map.Terrain.Tundra2);
        }

        bool FindMountain(Map map, MapPos position)
        {
            return FindMountain(map, position, false);
        }

        bool FindMountainWithFlag(Map map, MapPos position)
        {
            return FindMountain(map, position, true);
        }

        void FindNearbyMountain(Game game, Player player, ref MapPos position)
        {
            position = game.Map.FindSpotNear(position, 9, FindMountainWithFlag, game.GetRandom(), 1);

            if (position == Global.INVALID_MAPPOS || game.Map.GetOwner(position) != player.Index)
                position = game.Map.FindSpotNear(position, 9, FindMountain, game.GetRandom(), 1);
        }

        Map.FindData FindFlag(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = map.HasFlag(position),
                Data = position
            };
        }

        Map.FindData FindMine(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = map.HasBuilding(position) && FindMountain(map, position),
                Data = map.GetObjectIndex(position)
            };
        }

        // TODO: With many military buildings, this will take quiet a while. Needs improvement.
        bool SendGeologist(AI ai, Game game, Player player)
        {
            var militaryBuildings = game.GetPlayerBuildings(player).Where(building => building.IsMilitary());

            List<uint> possibleSpots = new List<uint>();

            // search for mountains near military buildings
            foreach (var building in militaryBuildings)
            {
                if (game.Map.FindSpotNear(building.Position, 3, FindMountain, game.GetRandom(), 1) != Global.INVALID_MAPPOS)
                {
                    possibleSpots.Add(game.Map.MoveDownRight(building.Position));
                    continue;
                }

                var position = building.Position;
                FindNearbyMountain(game, player, ref position);

                if (position != Global.INVALID_MAPPOS)
                {
                    possibleSpots.Add(position);
                }
            }

            if (possibleSpots.Count == 0) // no mountains in territory
            {
                // we need to increase our territory
                if (player.GetIncompleteBuildingCount(Building.Type.Hut) == 0 &&
                    player.GetIncompleteBuildingCount(Building.Type.Tower) == 0 &&
                    player.GetIncompleteBuildingCount(Building.Type.Fortress) == 0 &&
                    !game.GetPlayerBuildings(player).Where(building => building.IsMilitary(false)).Any(b => !b.HasSerf))
                {
                    // build only if there are no military buildings in progress or
                    // military buildings that are not occupied yet
                    NextState = ai.CreateState(AI.State.BuildBuilding, Building.Type.Hut);
                }

                return false;
            }

            var spotsWithFlag = possibleSpots.Where(possibleSpot => game.Map.HasFlag(possibleSpot) && game.Map.GetOwner(possibleSpot) == player.Index).ToList();

            for (int i = 0; i < 10; ++i)
            {
                if (possibleSpots.Count == 0)
                    break;

                uint spot = (spotsWithFlag.Count > 0) ? spotsWithFlag[game.GetRandom().Next() % spotsWithFlag.Count] :
                    possibleSpots[game.GetRandom().Next() % possibleSpots.Count];
                var flag = game.GetFlagAtPosition(spot);

                if (flag == null)
                {
                    if (!game.BuildFlag(spot, player))
                    {
                        bool built = false;

                        for (int d = 0; d < 5; ++d)
                        {
                            spot = game.Map.MoveTowards(spot, player.CastlePosition);

                            if (game.BuildFlag(spot, player))
                            {
                                built = true;
                                break;
                            }
                        }

                        if (!built)
                        {
                            possibleSpots.Remove(spot);
                            continue;
                        }
                    }

                    flag = game.GetFlagAtPosition(spot);

                    // link flag
                    var flagPositions = game.Map.FindInArea(flag.Position, 7, FindFlag, 2).Select(d => (uint)d);
                    Road bestRoad = null;

                    foreach (var flagPosition in flagPositions)
                    {
                        var road = Pathfinder.FindShortestPath(game.Map, flag.Position, flagPosition, null, 10);

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

            return false;
        }
    }
}
