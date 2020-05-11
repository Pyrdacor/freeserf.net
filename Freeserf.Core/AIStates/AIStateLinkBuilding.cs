/*
 * AIStateLinkBuilding.cs - AI state for linking a building to the road system
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

namespace Freeserf.AIStates
{
    class AIStateLinkBuilding : AIState
    {
        uint buildingPosition = Global.INVALID_MAPPOS;

        public AIStateLinkBuilding(uint buildingPosition)
            : base(AI.State.LinkBuilding)
        {
            this.buildingPosition = buildingPosition;
        }

        protected override void ReadFrom(Game game, AI ai, string name, SaveReaderText reader)
        {
            base.ReadFrom(game, ai, name, reader);

            buildingPosition = reader.Value($"{name}.building_position").ReadUInt();
        }

        public override void WriteTo(string name, SaveWriterText writer)
        {
            base.WriteTo(name, writer);

            writer.Value($"{name}.building_position").Write(buildingPosition);
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            if (game.GetBuildingAtPosition(buildingPosition) == null) // building has gone (maybe an enemy took the land)
            {
                Kill(ai);
                return;
            }

            var flagPosition = game.Map.MoveDownRight(buildingPosition);
            var flag = game.GetFlagAtPosition(flagPosition);

            // don't link if already linked
            if (game.Map.Paths(flagPosition) > 0 && flag.FindNearestInventoryForSerf() != -1)
            {
                Kill(ai);
                return;
            }

            // if we cannot link the building, we will demolish it
            if (!ai.LinkFlag(flag))
            {
                game.DemolishBuilding(buildingPosition, player);

                if (game.Map.Paths(flagPosition) == 0)
                    game.DemolishFlag(flagPosition, player);
            }

            // return to idle state and decide there what to do
            Kill(ai);
        }
    }
}
