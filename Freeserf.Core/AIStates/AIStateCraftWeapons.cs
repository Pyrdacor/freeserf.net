using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.AIStates
{
    // TODO: Craft weapons (including building weapon smiths, train a weapon smith and so on).
    class AIStateCraftWeapons : AIState
    {
        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            // TODO

            Kill(ai);
        }
    }
}
