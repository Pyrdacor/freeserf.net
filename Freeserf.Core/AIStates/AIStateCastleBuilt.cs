using System;
using System.Collections.Generic;
using System.Text;

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
                ai.CreateRandomDelayedState(AI.State.BuildBuilding, 3 * 1000, (30 - (int)playerInfo.Intelligence / 2) * 1000, Building.Type.Sawmill),
                // and then build a stonecutter
                ai.CreateRandomDelayedState(AI.State.BuildBuilding, 0, (10 - (int)playerInfo.Intelligence / 6) * 1000, Building.Type.Stonecutter),
                // then enter idle
                ai.CreateState(AI.State.Idle)
            );
        }
    }
}
