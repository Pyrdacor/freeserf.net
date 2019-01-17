/*
 * AIStateCastleBuilt.cs - AI state that is active after the castle was built
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

namespace Freeserf.AIStates
{
    class AIStateCastleBuilt : AIState
    {
        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            Kill(ai); // always kill before pushes!

            ai.PushStates
            (
                // first build a lumberjack
                ai.CreateRandomDelayedState(AI.State.BuildBuilding, 3 * 1000, (20 - (int)playerInfo.Intelligence / 5) * 1000, Building.Type.Lumberjack),
                // then build a sawmill
                ai.CreateRandomDelayedState(AI.State.BuildBuilding, 3 * 1000, (25 - (int)playerInfo.Intelligence / 2) * 1000, Building.Type.Sawmill),
                // and then build a stonecutter
                ai.CreateRandomDelayedState(AI.State.BuildBuilding, 2 * 1000, (15 - (int)playerInfo.Intelligence / 6) * 1000, Building.Type.Stonecutter),
                // then enter idle
                ai.CreateState(AI.State.Idle)
            );
        }
    }
}
