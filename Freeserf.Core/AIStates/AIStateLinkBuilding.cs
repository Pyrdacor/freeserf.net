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

            uint bestFlagPos = Global.BadMapPos;
            int minDist = int.MaxValue;
            Dictionary<uint, int> flags = new Dictionary<uint, int>();

            foreach (var flag in game.GetPlayerFlags(player))
            {
                if (flagPos == flag.Position)
                    continue; // not link to self

                int distX = game.Map.DistX(flagPos, flag.Position);
                int distY = game.Map.DistY(flagPos, flag.Position);
                int dist = Misc.Round(Math.Sqrt(distX * distX + distY * distY));

                if (dist < minDist)
                {
                    bestFlagPos = flag.Position;
                    minDist = dist;

                    if (dist == 2)
                        break;
                }

                flags.Add(flag.Position, dist);
            }

            if (bestFlagPos == Global.BadMapPos)
            {
                // could not find a valid flag to link to
                // return to idle state and decide there what to do
                Kill(ai);
                return;
            }

            while (flags.Count > 0)
            {
                var road = Pathfinder.Map(game.Map, flagPos, bestFlagPos);

                if (road == null || !road.Valid ||
                    road.Length > minDist * 2) // maybe the nearest flag is behind the border and the way is much longer as thought
                {
                    flags.Remove(bestFlagPos);

                    if (flags.Count == 0)
                        break;

                    bestFlagPos = flags.OrderBy(f => f.Value).First().Key;
                    continue;
                }

                if (game.BuildRoad(road, player))
                    break;
                else
                    flags.Remove(bestFlagPos);
            }

            // could not link the flags
            // return to idle state and decide there what to do
            Kill(ai);
        }
    }
}
