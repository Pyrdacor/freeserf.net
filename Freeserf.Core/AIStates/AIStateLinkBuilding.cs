using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.AIStates
{
    // TODO: this should try to link to flags, that are connected to the castle or at least a stock
    // TODO: this should not fail if there is a possibility (right now it does quiet often)
    class AIStateLinkBuilding : AIState
    {
        uint buildingPos = Global.BadMapPos;

        public AIStateLinkBuilding(uint buildingPos)
        {
            this.buildingPos = buildingPos;
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            var flagPos = game.Map.MoveDownRight(buildingPos);

            // don't link if already linked
            if (game.Map.Paths(flagPos) > 0 && game.GetFlagAtPos(flagPos).FindNearestInventoryForSerf() != -1)
            {
                Kill(ai);
                return;
            }

            Road bestRoad = null;

            foreach (var flag in game.GetPlayerFlags(player))
            {
                if (flagPos == flag.Position)
                    continue; // not link to self

                int distX = game.Map.DistX(flag.Position, flagPos);
                int distY = game.Map.DistY(flag.Position, flagPos);
                int dist = Misc.Round(Math.Sqrt(distX * distX + distY * distY));

                if (dist > 30) // too far away
                    continue;

                var road = Pathfinder.Map(game.Map, flag.Position, flagPos);

                if (road != null && road.Valid && (bestRoad == null || road.Length < bestRoad.Length))
                    bestRoad = road;
            }

            if (bestRoad == null)
            {
                // could not find a valid flag to link to
                // return to idle state and decide there what to do
                Kill(ai);
                return;
            }

            game.BuildRoad(bestRoad, player);

            // return to idle state and decide there what to do
            Kill(ai);
        }
    }
}
