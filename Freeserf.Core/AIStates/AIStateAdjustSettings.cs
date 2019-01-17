/*
 * AIStateAdjustSettings.cs - AI state for adjusting settings
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
    // TODO: Change settings like tool and flag priorities, military settings and so on.
    class AIStateAdjustSettings : AIState
    {
        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            // knight occupation setting 
            // TODO: should change if amount of knights change in relation to number of military buildings
            int highKnightOccupationPreference = Misc.Max(ai.DefendFocus, ai.Aggressivity, (ai.MilitarySkill + 1) / 2, (ai.MilitaryFocus + 1) / 2);

            if (ai.ExpandFocus == 2 && ai.DefendFocus == 2 && ai.Aggressivity < 2)
                highKnightOccupationPreference = 1;

            if (highKnightOccupationPreference == 0)
            {
                player.SetLowKnightOccupation();
            }
            else if (highKnightOccupationPreference == 1)
            {
                player.SetMediumKnightOccupation(ai.DefendFocus < 1);
            }
            else if (highKnightOccupationPreference == 2)
            {
                player.SetHighKnightOccupation(ai.DefendFocus < 2);
            }

            // TODO: castle knights (always keep at least 3, only in really rare situations we may use 1 or 2)

            // TODO: flag/inventory prios

            // TODO: inventory serf/resource in/out modes

            // TODO: serf to knight rate

            // TODO: food distribution

            // TODO: plank, steel, coal and wheat distribution

            // TODO: send strongest and knight cycling

            // Note: Toolmaker prios are handled by the CraftTool AI state.

            Kill(ai);
        }
    }
}
