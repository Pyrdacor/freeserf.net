using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.AIStates
{
    // TODO: Attack enemy buildings and find good spots to attack.
    class AIStateAttack : AIState
    {
        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            // TODO

            Kill(ai);
        }
    }
}
