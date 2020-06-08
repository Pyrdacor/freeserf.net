/*
 * AIStateAvoidCongestion.cs - AI state for actions to avoid congestion
 *
 * Copyright (C) 2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
    // TODO: Avoid congestion by building new roads, removing roads, building stocks and so on.
    class AIStateAvoidCongestion : AIState
    {
        public AIStateAvoidCongestion()
            : base(AI.State.AvoidCongestion)
        {

        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            // TODO
            // Step 1: Find flags with high traffic (waiting serfs, many resources)
            // Step 2: Do something to decongest the nearby roads

            // Strategies:
            // - Build roads around
            // - Demolish roads
            // - Build stocks
            // - Build another building which processes a good

            Kill(ai);
        }
    }
}
