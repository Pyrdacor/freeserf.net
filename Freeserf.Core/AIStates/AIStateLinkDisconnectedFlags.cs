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

namespace Freeserf.AIStates
{
    class AIStateLinkDisconnectedFlags : AIState
    {
        readonly Dictionary<Flag, int> connectTriesPerFlag = new Dictionary<Flag, int>();

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            bool remainingFlagsToLinkExist = false;

            foreach (var flag in game.GetPlayerFlags(player))
            {
                if (!connectTriesPerFlag.ContainsKey(flag))
                    connectTriesPerFlag[flag] = 0;

                if ((flag.Paths() == 0 || flag.FindNearestInventoryForSerf() == -1) && ++connectTriesPerFlag[flag] < 3)
                {
                    if (!LinkFlag(game, player, flag.Position))
                        remainingFlagsToLinkExist = true;                    
                }
            }

            if (!remainingFlagsToLinkExist)
                Kill(ai);
        }

        Map.FindData FindFlag(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.HasFlag(pos),
                Data = pos
            };
        }

        bool LinkFlag(Game game, Player player, uint pos)
        {
            var flagsInRange = game.Map.FindInArea(pos, 9, FindFlag, 1);

            if (flagsInRange.Count == 0)
                return false;

            var flagPos = (uint)flagsInRange[game.RandomInt() % flagsInRange.Count];
            var road = Pathfinder.Map(game.Map, pos, flagPos);

            if (road != null && road.Valid)
                return game.BuildRoad(road, player);

            return false;
        }
    }
}
