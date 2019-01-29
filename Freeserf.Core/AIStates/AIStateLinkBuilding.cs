/*
 * AIStateLinkBuilding.cs - AI state for linking a building to the road system
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

namespace Freeserf.AIStates
{
    class AIStateLinkBuilding : AIState
    {
        uint buildingPos = Global.BadMapPos;

        public AIStateLinkBuilding(uint buildingPos)
        {
            this.buildingPos = buildingPos;
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            if (game.GetBuildingAtPos(buildingPos) == null) // building has gone (maybe an enemy took the land)
            {
                Kill(ai);
                return;
            }

            var buildingType = game.GetBuildingAtPos(buildingPos).BuildingType;
            var flagPos = game.Map.MoveDownRight(buildingPos);

            // don't link if already linked
            if (game.Map.Paths(flagPos) > 0 && game.GetFlagAtPos(flagPos).FindNearestInventoryForSerf() != -1)
            {
                Kill(ai);
                return;
            }

            // if we cannot link the building, we will demolish it
            if (!ai.LinkFlag(game.GetFlagAtPos(flagPos)))
            {
                game.DemolishBuilding(buildingPos, player);

                if (game.Map.Paths(flagPos) == 0)
                    game.DemolishFlag(flagPos, player);
            }

            // return to idle state and decide there what to do
            Kill(ai);
        }
    }
}
