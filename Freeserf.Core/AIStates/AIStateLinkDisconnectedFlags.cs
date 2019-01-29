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

using System.Collections.Generic;
using System.Linq;

namespace Freeserf.AIStates
{
    class AIStateLinkDisconnectedFlags : AIState
    {
        readonly Dictionary<Flag, int> connectTriesPerFlag = new Dictionary<Flag, int>();

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            bool remainingFlagsToLinkExist = false;
            var flags = game.GetPlayerFlags(player).ToList(); // ToList is important because the collection could be changed in foreach below.

            foreach (var flag in flags)
            {
                if (!connectTriesPerFlag.ContainsKey(flag))
                    connectTriesPerFlag[flag] = 0;

                if ((flag.LandPaths() == 0 || flag.FindNearestInventoryForSerf() == -1) && ++connectTriesPerFlag[flag] < 3)
                {
                    if (!ai.LinkFlag(flag))
                        remainingFlagsToLinkExist = true;
                    else
                        continue;
                }

                // we also check if the link is good
                DirectionCycleCW cycle = DirectionCycleCW.CreateDefault();
                List<Direction> pathes = new List<Direction>();

                foreach (var dir in cycle)
                {
                    if (flag.HasPath(dir))
                    {
                        pathes.Add(dir);
                    }
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

                if (!finishedLinking && pathes.Count == 1)
                {
                    int roadLength = 0;
                    uint pos = flag.Position;
                    Direction dir = pathes[0];

                    while (true)
                    {
                        ++roadLength;
                        pos = game.Map.Move(pos, dir);

                        if (game.Map.HasFlag(pos))
                            break;

                        var pathCycle = DirectionCycleCW.CreateDefault();

                        foreach (var pathDir in pathCycle)
                        {
                            if (pathDir != dir.Reverse() && game.Map.HasPath(pos, pathDir))
                            {
                                dir = pathDir;
                                break;
                            }
                        }
                    }

                    if (roadLength > 4)
                    {
                        ai.LinkFlag(flag, 4, true);
                    }
                }
            }

            if (!remainingFlagsToLinkExist)
                Kill(ai);
        }
    }
}
