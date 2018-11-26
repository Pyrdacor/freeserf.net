using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.AIStates
{
    class AIStateChoosingCastleLocation : AIState
    {
        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            uint mapPos = Global.BadMapPos;
            // TODO

            // If found a good spot, build the castle
            game.BuildCastle(mapPos, player);

            // TODO
            //NextState = ...;
            Kill();
        }
    }
}
