using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.AIStates
{
    // The idle state is always active and should not be killed.
    // It runs in the background and when all states are killed,
    // this state will run.
    //
    // At the game start the first state will be either ChoosingCastleLocation
    // or CastleBuilt. The first one will switch to CastleBuilt when finished.
    // CastleBuilt will add the idle state to the queue of states.
    //
    // The idle state will decide what to do next.
    class AIStateIdle : AIState
    {
        int checkUnconnectedFlagsTick = 0;
        int buildNeededBuildingsTick = 0;
        
        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            // check for unconnected flags once in a while
            checkUnconnectedFlagsTick += tick;

            if (checkUnconnectedFlagsTick > (5 - (int)playerInfo.Intelligence / 15) * Global.TICKS_PER_SEC)
            {
                checkUnconnectedFlagsTick = 0;

                if (game.RandomInt() % 10 > 6)
                {
                    ai.PushState(ai.CreateState(AI.State.LinkDisconnectedFlags));
                    return;
                }
            }

            // check for needed buildings and build them
            buildNeededBuildingsTick += tick;

            if (buildNeededBuildingsTick > (10 - (int)playerInfo.Intelligence / 9) * Global.TICKS_PER_SEC)
            {
                buildNeededBuildingsTick = 0;

                if (game.RandomInt() % 10 > 3)
                {
                    ai.PushState(ai.CreateState(AI.State.BuildNeededBuilding));
                    return;
                }
            }

            // TODO ...
        }

        public override void Kill(AI ai)
        {
            // Leave empty! This state can never be killed.
        }
    }
}
