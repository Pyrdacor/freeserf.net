using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.AIStates
{
    class AIStateCastleBuilt : AIState
    {
        bool done = false; // TODO: testcode

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            if (done)
                return;

            // first build a lumberjack
            ai.PushStateRandomDelayed(ai.CreateState(AI.State.BuildBuilding, Building.Type.Lumberjack), 5 * 1000, (20 - (int)playerInfo.Intelligence / 5) * 1000);

            done = true;

            // then build a sawmill
            // TODO

            //Kill();
        }
    }
}
