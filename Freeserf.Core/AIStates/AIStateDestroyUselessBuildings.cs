/*
 * AIStateDestroyUselessBuildings.cs - AI state to destroy useless buildings
 *
 * Copyright (C) 2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using System;
using System.Linq;

namespace Freeserf.AIStates
{
    using MapPos = UInt32;

    // Removes stonecutters or mines that can no longer quarry resources.
    // For mines this is called directly with a specific building index.
    // Otherwise it is a cyclic check and we check all stonecutters.
    // The cyclic check also releases a knight of a military building which
    // is no longer needed for the territory if there are no knights left.
    
    class AIStateDestroyUselessBuildings : AIState
    {
        uint buildingIndex = uint.MaxValue;

        public AIStateDestroyUselessBuildings()
            : base(AI.State.DestroyUselessBuildings)
        {

        }

        public AIStateDestroyUselessBuildings(uint buildingIndex)
            : base(AI.State.DestroyUselessBuildings)
        {
            this.buildingIndex = buildingIndex;
        }

        protected override void ReadFrom(Game game, AI ai, string name, SaveReaderText reader)
        {
            base.ReadFrom(game, ai, name, reader);

            buildingIndex = reader.Value($"{name}.building_index").ReadUInt();
        }

        public override void WriteTo(string name, SaveWriterText writer)
        {
            base.WriteTo(name, writer);

            writer.Value($"{name}.building_index").Write(buildingIndex);
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            if (buildingIndex != uint.MaxValue)
            {
                // a specific mine
                var mine = game.GetBuilding(buildingIndex);

                if (mine != null)
                {
                    game.DemolishBuilding(mine.Position, player);
                }
            }
            else
            {
                var stonecutters = game.GetPlayerBuildings(player, Building.Type.Stonecutter).ToList(); // use ToList as we might change the collection below

                foreach (var stonecutter in stonecutters)
                {
                    if (game.Map.FindInArea(stonecutter.Position, 6, FindStone, 1).Count == 0)
                    {
                        game.DemolishBuilding(stonecutter.Position, player);
                    }
                }

                ReleaseKnightOfNeedlessMilitaryBuilding(game, player);
            }

            Kill(ai);
        }

        static readonly Building.Type[] ProtectedBuildingTypes = new[]
        {
            Building.Type.Castle,
            Building.Type.Stock,
            Building.Type.WeaponSmith,
            Building.Type.ToolMaker,
            Building.Type.SteelSmelter,
            Building.Type.GoldSmelter,
            Building.Type.CoalMine,
            Building.Type.IronMine,
            Building.Type.GoldMine,
            Building.Type.Sawmill
        };

        // If there is no knight left to build new military buildings and none can be
        // trained, a military building inside the territory is demolished so that its
        // knight can expand the territory. Otherwise the AI could get stuck, e.g. when it
        // needs to expand to find iron to produce weapons for new knights.
        // Only buildings are demolished which don't change the owner of any position.
        // So no buildings, flags or roads are lost and the territory is not split.
        // Military buildings near the enemy or near important buildings are kept.
        static void ReleaseKnightOfNeedlessMilitaryBuilding(Game game, Player player)
        {
            if (game.GetPossibleFreeKnightCount(player) != 0)
                return;

            var map = game.Map;

            foreach (var type in new[] { Building.Type.Hut, Building.Type.Tower, Building.Type.Fortress })
            {
                foreach (var building in game.GetPlayerBuildings(player, type).ToList())
                {
                    if (!building.IsDone || building.IsBurning || !building.HasKnight())
                        continue;

                    if (IsNearEnemy(map, player, building.Position) || IsProtectingImportantBuilding(game, player, building.Position))
                        continue;

                    if (game.IsLandOwnershipKeptWithout(building))
                    {
                        game.DemolishBuilding(building.Position, player);
                        return; // only one building at a time
                    }
                }
            }
        }

        static bool IsNearEnemy(Map map, Player player, MapPos position)
        {
            // All positions within a distance of 8 (the influence radius of military buildings).
            for (uint i = 0; i < 217; ++i)
            {
                var checkPosition = map.PositionAddSpirally(position, i);

                if (map.HasOwner(checkPosition) && map.GetOwner(checkPosition) != player.Index)
                    return true;
            }

            return false;
        }

        static bool IsProtectingImportantBuilding(Game game, Player player, MapPos position)
        {
            return game.GetPlayerBuildings(player).Any(building => ProtectedBuildingTypes.Contains(building.BuildingType) &&
                game.Map.Distance(position, building.Position) <= 6);
        }

        static Map.FindData FindStone(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = map.GetObject(position) >= Map.Object.Stone0 && map.GetObject(position) <= Map.Object.Stone7 &&
                    Map.MapSpaceFromObject[(int)map.GetObject(map.MoveDownRight(position))] <= Map.Space.Semipassable
            };
        }
    }
}
