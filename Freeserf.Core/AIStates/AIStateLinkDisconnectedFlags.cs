/*
 * AIStateLinkDisconnectedFlags.cs - AI state for linking disconnected flags
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
    class AIStateLinkDisconnectedFlags : AIState
    {
        readonly Dictionary<Flag, int> connectTriesPerFlag = new Dictionary<Flag, int>();
        readonly Dictionary<Flag, int> linkingFailed = new Dictionary<Flag, int>();
        int linkingCount = 0;
        readonly object linkingLock = new object();
        readonly object linkingFailedLock = new object();

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            lock (linkingLock)
            {
                if (linkingCount != 0)
                    return;
            }

            var flags = game.GetPlayerFlags(player);

            foreach (var flag in flags)
            {
                // If it failed to link a flag we will only re-check it
                // after 2 minutes.
                lock (linkingFailedLock)
                {
                    if (linkingFailed.ContainsKey(flag))
                    {
                        int time = linkingFailed[flag];

                        if (tick < time)
                            time = int.MaxValue - time + tick;
                        else
                            time = tick - time;

                        if (time >= 2 * Global.TICKS_PER_MIN)
                            linkingFailed.Remove(flag);
                        else
                            continue;
                    }
                }

                if (!connectTriesPerFlag.ContainsKey(flag))
                    connectTriesPerFlag[flag] = 0;

                if ((flag.LandPaths() == 0 || flag.FindNearestInventoryForSerf() == -1) && ++connectTriesPerFlag[flag] < 3)
                {
                    LinkFlag(ai, flag, 9, false, tick);
                }

                bool finishedLinking = false;

                if (flag.HasBuilding())
                {
                    var building = flag.GetBuilding();

                    // don't link flags of foresters and farms more than necessary as they need space for their work
                    if (building.BuildingType == Building.Type.Forester || building.BuildingType == Building.Type.Farm)
                        finishedLinking = true;
                }

                // TODO: maybe check later if there are foresters or farms around and stop linking in the area then

                // we also check if the link is good
                if (!finishedLinking)
                {
                    DirectionCycleCW cycle = DirectionCycleCW.CreateDefault();
                    List<Direction> pathes = new List<Direction>();

                    foreach (var dir in cycle)
                    {
                        if (flag.HasPath(dir))
                        {
                            pathes.Add(dir);
                        }
                    }
                
                    if (pathes.Count < 6)
                    {
                        int shortestPath = int.MaxValue;

                        foreach (var dir in pathes)
                        {
                            int pathLength = (int)flag.GetRoad(dir).Length;

                            if (pathLength < shortestPath)
                                shortestPath = pathLength;

                            if (shortestPath == 2)
                                break;
                        }

                        if (shortestPath > 2 + pathes.Count * 2)
                        {
                            LinkFlag(ai, flag, 2 + pathes.Count * 2, true, tick);
                        }
                        else
                        {
                            var nearbyFlags = game.Map.FindInArea(flag.Position, 4, FindFlag, 2);

                            foreach (var nearbyFlag in nearbyFlags)
                            {
                                if (Pathfinder.FindShortestRoad(game.Map, flag, game.GetFlagAtPos((uint)nearbyFlag), out uint cost) == null ||
                                    cost >= 1500u)
                                {
                                    LinkFlag(ai, flag, 4, true, tick);
                                }
                            }
                        }
                    }
                }
            }

            lock (linkingLock)
            {
                if (linkingCount == 0)
                    Kill(ai);
            }
        }

        Map.FindData FindFlag(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.HasFlag(pos),
                Data = pos
            };
        }

        void LinkFlag(AI ai, Flag flag, int maxLength, bool allowWater, int tick)
        {
            var state = this;

            Action action = () =>
            {
                try
                {
                    if (!ai.LinkFlag(flag, maxLength, allowWater))
                    {
                        lock (state.linkingFailedLock)
                        {
                            state.linkingFailed[flag] = tick;
                        }
                    }
                }
                finally
                {
                    lock (linkingLock)
                    {
                        --linkingCount;
                    }
                }
            };

            ++linkingCount;

            new Thread(new ParameterizedThreadStart(Link)).Start(action);
        }

        void Link(Action action)
        {
            action?.Invoke();
        }

        void Link(object param)
        {
            Link(param as Action);
        }
    }
}
