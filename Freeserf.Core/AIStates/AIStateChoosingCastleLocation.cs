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

            // TODO: for now we just try random map locations. Change later.
            var random = new Random();
            while (true)
            {
                mapPos = game.Map.GetRandomCoord(random);

                if (game.CanBuildCastle(mapPos, player))
                    break;
            }

            // If found a good spot, build the castle
            if (!game.BuildCastle(mapPos, player))
                return; // failed -> try again

            // TODO
            NextState = ai.CreateState(AI.State.CastleBuilt);
            Kill();
        }
    }
}
