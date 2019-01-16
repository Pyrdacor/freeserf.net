using System;

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
