using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.AIStates
{
    class AIStateBuildBuilding : AIState
    {
        bool built = false;
        int range = 2;
        Building.Type type = Building.Type.None;
        uint builtPosition = Global.BadMapPos;

        public AIStateBuildBuilding(Building.Type buildingType)
        {
            type = buildingType;
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            if (built)
            {
                if (!Killed)
                {
                    NextState = ai.CreateState(AI.State.LinkBuilding, builtPosition);
                    Kill();
                }

                return;
            }

            // TODO: find a nice spot

            // TODO: this is only test code
            int dist = (game.RandomInt() & 0x7f) + 1;
            var pos = game.Map.PosAddSpirally(game.Map.MoveDownRight(player.CastlePos), (uint)dist);

            if (game.CanBuildBuilding(pos, Building.Type.Lumberjack, player))
            {
                built = game.BuildBuilding(pos, Building.Type.Lumberjack, player);

                if (built)
                    builtPosition = pos;

                return;
            }

            ++range;
        }
    }
}
