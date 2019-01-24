/*
 * AIStateCraftTool.cs - AI state to craft a specific tool
 *
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net. freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with freeserf.net. If not, see <http://www.gnu.org/licenses/>.
 */

using System.Linq;

namespace Freeserf.AIStates
{
    class AIStateCraftTool : AIState
    {
        Resource.Type tool = Resource.Type.None;
        Player player = null;
        int triesBuildToolmaker = 0;
        int tries = 0;

        public AIStateCraftTool(Resource.Type tool)
        {
            this.tool = tool;
        }

        // TODO: If we don't have steel or planks and not are able to get some, this will run forever
        // and will block other AI states that might be necessary to get other stuff!
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
            player.SetPlanksToolmaker(ushort.MaxValue);
            player.SetSteelToolmaker(ushort.MaxValue);

            // set all tool priorities to 0
            for (int i = 0; i < 9; ++i)
                player.SetToolPriority(i, ushort.MinValue);

            // set the priority for the tool to 100%
            player.SetToolPriority(tool - Resource.Type.Shovel, ushort.MaxValue);

            if (++tries == 20) // don't block for too long
                Kill(ai);
        }

        public override void Kill(AI ai)
        {
            tries = 0;
            triesBuildToolmaker = 0;
            player.ResetToolPriority();
            player.ResetPlanksPriority();
            player.ResetSteelPriority();

            base.Kill(ai);
        }
    }
}
