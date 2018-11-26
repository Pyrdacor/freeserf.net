using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.AIStates
{
    class AIStateLinkBuilding : AIState
    {
        uint buildingPos = Global.BadMapPos;

        public AIStateLinkBuilding(uint buildingPos)
        {
            this.buildingPos = buildingPos;
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            // TODO
        }
    }
}
