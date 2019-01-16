using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.AIStates
{
    // Removes stonecutters or mines that can no longer quarry resources.
    // For mines this is called directly with a specific building index.
    // Otherwise it is a cyclic check and we check all stonecutters.
    class AIStateDestroyUselessBuildings : AIState
    {
        uint buildingIndex = uint.MaxValue;

        public AIStateDestroyUselessBuildings()
        {

        }

        public AIStateDestroyUselessBuildings(uint buildingIndex)
        {
            this.buildingIndex = buildingIndex;
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
                    if (game.Map.FindInArea(stonecutter.Position, 8, FindStone, 1).Count == 0)
                    {
                        game.DemolishBuilding(stonecutter.Position, player);
                    }
                }
            }

            Kill(ai);
        }

        static Map.FindData FindStone(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.GetObject(pos) >= Map.Object.Stone0 && map.GetObject(pos) <= Map.Object.Stone7
            };
        }
    }
}
