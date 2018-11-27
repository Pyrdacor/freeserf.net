using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.AIStates
{
    class AIStateLinkDisconnectedFlags : AIState
    {
        readonly Dictionary<Flag, int> connectTriesPerFlag = new Dictionary<Flag, int>();

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            bool remainingFlagsToLinkExist = false;

            foreach (var flag in game.GetPlayerFlags(player))
            {
                if (!connectTriesPerFlag.ContainsKey(flag))
                    connectTriesPerFlag[flag] = 0;

                if ((flag.Paths() == 0 || flag.FindNearestInventoryForSerf() == -1) && ++connectTriesPerFlag[flag] < 3)
                {
                    if (!LinkFlag(game, player, flag.Position))
                        remainingFlagsToLinkExist = true;                    
                }
            }

            if (!remainingFlagsToLinkExist)
                Kill(ai);
        }

        Map.FindData FindFlag(Map map, uint pos)
        {
            return new Map.FindData()
            {
                Success = map.HasFlag(pos),
                Data = pos
            };
        }

        bool LinkFlag(Game game, Player player, uint pos)
        {
            var flagsInRange = game.Map.FindInArea(pos, 9, FindFlag, 1);

            if (flagsInRange.Count == 0)
                return false;

            var flagPos = (uint)flagsInRange[game.RandomInt() % flagsInRange.Count];
            var road = Pathfinder.Map(game.Map, pos, flagPos);

            return game.BuildRoad(road, player);
        }
    }
}
