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

namespace Freeserf.AIStates
{
    class AIStateCraftTool : AIState
    {
        Resource.Type tool = Resource.Type.None;
        Player player = null;
        int triesBuildToolmaker = 0;
        int tries = 0;
        uint previousCount = 0u;

        public AIStateCraftTool(Game game, Player player, Resource.Type tool)
            : base(AI.State.CraftTool)
        {
            this.tool = tool;

            previousCount = (tool == Resource.Type.None) ? 0u : GetCurrentToolCount(player);
        }

        protected override void ReadFrom(Game game, AI ai, string name, SaveReaderText reader)
        {
            base.ReadFrom(game, ai, name, reader);

            tool = reader.Value($"{name}.tool").ReadResource();
            triesBuildToolmaker = reader.Value($"{name}.tries_build_toolmaker").ReadInt();
            tries = reader.Value($"{name}.tries").ReadInt();
            previousCount = reader.Value($"{name}.previous_count").ReadUInt();
        }

        public override void WriteTo(string name, SaveWriterText writer)
        {
            base.WriteTo(name, writer);

            writer.Value($"{name}.tool").Write(tool);
            writer.Value($"{name}.tries_build_toolmaker").Write(triesBuildToolmaker);
            writer.Value($"{name}.tries").Write(tries);
            writer.Value($"{name}.previous_count").Write(previousCount);
        }

        // This includes tool count and amount of serfs that use the tool.
        uint GetCurrentToolCount(Player player)
        {
            uint count = player.GetResourceCount(tool);

            switch (tool)
            {
                case Resource.Type.Axe:
                    count += player.GetSerfCount(Serf.Type.Lumberjack);
                    break;
                case Resource.Type.Cleaver:
                    count += player.GetSerfCount(Serf.Type.Butcher);
                    break;
                case Resource.Type.Hammer:
                    count += player.GetSerfCount(Serf.Type.BoatBuilder);
                    count += player.GetSerfCount(Serf.Type.Builder);
                    count += player.GetSerfCount(Serf.Type.Geologist);
                    count += player.GetSerfCount(Serf.Type.Toolmaker);
                    count += player.GetSerfCount(Serf.Type.WeaponSmith);
                    break;
                case Resource.Type.Pick:
                    count += player.GetSerfCount(Serf.Type.Miner);
                    count += player.GetSerfCount(Serf.Type.Stonecutter);
                    break;
                case Resource.Type.Pincer:
                    count += player.GetSerfCount(Serf.Type.WeaponSmith);
                    break;
                case Resource.Type.Rod:
                    count += player.GetSerfCount(Serf.Type.Fisher);
                    break;
                case Resource.Type.Saw:
                    count += player.GetSerfCount(Serf.Type.Sawmiller);
                    count += player.GetSerfCount(Serf.Type.Toolmaker);
                    break;
                case Resource.Type.Scythe:
                    count += player.GetSerfCount(Serf.Type.Farmer);
                    break;
                case Resource.Type.Shovel:
                    count += player.GetSerfCount(Serf.Type.Digger);
                    break;
            }

            return count;
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            this.player = player;

            // First ensure that we have a toolmaker
            if (player.GetTotalBuildingCount(Building.Type.ToolMaker) == 0)
            {
                if (++triesBuildToolmaker > 3) // we couldn't create the toolmaker for 3 times
                {
                    Kill(ai);
                    return;
                }

                ai.PushState(ai.CreateState(AI.State.BuildBuilding, Building.Type.ToolMaker));
                return;
            }

            // Tool was crafted?
            if (GetCurrentToolCount(player) > previousCount)
            {
                // After crafting reset the values.
                // This can be changed by another craft tool state
                // or by the adjust settings state then.
                // We do this to avoid building the same tool
                // multiple times in a row.
                player.PlanksToolmaker = ushort.MinValue;
                player.SteelToolmaker = ushort.MinValue;
                player.ResetToolPriority();

                Kill(ai);
                return;
            }

            // Can we even provide enough steel?
            if (player.GetResourceCount(Resource.Type.Steel) == 0)
            {
                if ((player.GetResourceCount(Resource.Type.Coal) == 0 &&
                    player.GetCompletedBuildingCount(Building.Type.CoalMine) == 0) ||
                    (player.GetResourceCount(Resource.Type.IronOre) == 0 &&
                    player.GetCompletedBuildingCount(Building.Type.IronMine) == 0))
                {
                    Kill(ai);
                    return;
                }
            }

            // Give planks and steel to toolmaker
            player.PlanksToolmaker = ushort.MaxValue;
            player.SteelToolmaker = ushort.MaxValue;

            // Set the priority for the tool to 100%
            player.SetFullToolPriority(tool);

            if (++tries == 20) // don't block for too long
                Kill(ai);
        }

        public override void Kill(AI ai)
        {
            tries = 0;
            triesBuildToolmaker = 0;
            player?.ResetToolPriority();
            player?.ResetPlanksPriority();
            player?.ResetSteelPriority();

            base.Kill(ai);
        }
    }
}
