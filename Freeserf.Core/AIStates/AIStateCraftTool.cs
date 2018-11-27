using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.AIStates
{
    class AIStateCraftTool : AIState
    {
        Resource.Type tool = Resource.Type.None;
        Player player = null;
        int triesBuildToolmaker = 0;

        public AIStateCraftTool(Resource.Type tool)
        {
            this.tool = tool;
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            this.player = player;

            var toolmakers = game.GetPlayerBuildings(player, Building.Type.ToolMaker);

            // First ensure that we have a toolmaker
            if (!toolmakers.Any())
            {
                if (++triesBuildToolmaker > 3) // we couldn't create the toolmaker for 3 times
                {
                    Kill(ai);
                    return;
                }

                ai.PushState(ai.CreateState(AI.State.BuildBuilding, Building.Type.ToolMaker));
                return;
            }

            // Check all toolmaker flags for the tool
            foreach (var toolmaker in toolmakers)
            {
                var flag = game.GetFlagAtPos(game.Map.MoveDownRight(toolmaker.Position));

                if (flag.HasResources())
                {
                    for (int i = 0; i < 8; ++i)
                    {
                        if (flag.GetResourceAtSlot(i) == tool) // tool was crafted
                        {
                            Kill(ai);
                            return;
                        }
                    }
                }
            }

            // give planks and steel to toolmaker
            player.SetPlanksToolmaker(65500);
            player.SetSteelToolmaker(65500);

            // set all tool priorities to 0
            for (int i = 0; i < 9; ++i)
                player.SetToolPriority(i, 0);

            // set the priority for the tool to 100%
            player.SetToolPriority(tool - Resource.Type.Shovel, 65500);
        }

        public override void Kill(AI ai)
        {
            player.ResetToolPriority();

            base.Kill(ai);
        }
    }
}
