using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.AIStates
{
    class AIStateFindOre : AIState
    {
        Map.Minerals oreType = Map.Minerals.None;

        public AIStateFindOre(Map.Minerals oreType)
        {
            this.oreType = oreType;
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            if (oreType == Map.Minerals.None)
            {
                Kill(ai);
                return;
            }

            // TODO: find mountain
            // TODO: send geologists
            // TODO: if intelligent we help a bit by sending geologists to the right mountains
        }
    }
}
