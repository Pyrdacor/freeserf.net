/*
 * AIStateAvoidCongestion.cs - AI state for actions to avoid congestion
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
using System.Collections.Generic;

namespace Freeserf.AIStates
{
    using MapPos = UInt32;

    // A flag is congested if a serf waits in front of it for several minutes (see AI.CheckCongestion).
    // This happens when the serfs of roads with several transporters always hand over the flag to
    // each other by switching places, so the flag is never free for the serfs of the other roads.
    //
    // 1. Roads with more than one transporter get a new flag two positions away from the congested
    //    flag. Then only one transporter serves the part next to the flag, which leaves it to a free
    //    position, so the flag becomes free for the others.
    // 2. If no such flag can be built, a road without a gap (a serf at each position) is demolished
    //    if its flags are still connected without it. This happens at most every 10 minutes and the
    //    road is not built again for a while.
    class AIStateAvoidCongestion : AIState
    {
        const int MinRoadDemolitionInterval = 10 * Global.TICKS_PER_MIN;
        const int MaxFlagsToCheckConnection = 2000;

        readonly uint flagIndex = 0;

        public AIStateAvoidCongestion()
            : base(AI.State.AvoidCongestion)
        {

        }

        public AIStateAvoidCongestion(uint flagIndex)
            : base(AI.State.AvoidCongestion)
        {
            this.flagIndex = flagIndex;
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            var flag = flagIndex == 0 ? null : game.GetFlag(flagIndex);

            if (flag != null && flag.Player == player.Index)
            {
                if (!SplitRoads(game, player, flag))
                    DemolishGaplessRoad(ai, game, player, flag);
            }

            Kill(ai);
        }

        static bool SplitRoads(Game game, Player player, Flag flag)
        {
            bool split = false;

            foreach (var direction in DirectionCycleCW.CreateDefault())
            {
                if (!flag.HasPath(direction) || flag.FreeTransporterCount(direction) <= 1)
                    continue;

                var position = PositionOnPath(game.Map, flag.Position, direction, 2);

                if (position != Global.INVALID_MAPPOS && game.BuildFlag(position, player))
                    split = true;
            }

            return split;
        }

        static void DemolishGaplessRoad(AI ai, Game game, Player player, Flag flag)
        {
            if (ai.GameTime - ai.LastRoadDemolition < MinRoadDemolitionInterval)
                return;

            var map = game.Map;

            foreach (var direction in DirectionCycleCW.CreateDefault())
            {
                if (!flag.HasPath(direction) || flag.FreeTransporterCount(direction) <= 1)
                    continue;

                var otherFlag = flag.GetOtherEndFlag(direction);

                if (otherFlag == null || otherFlag == flag || !IsRoadWithoutGap(map, flag.Position, direction))
                    continue;

                if (!AreConnectedWithoutRoad(flag, otherFlag, direction))
                    continue;

                var roadPosition = map.Move(flag.Position, direction);

                if (game.DemolishRoad(roadPosition, player))
                {
                    ai.LastRoadDemolition = ai.GameTime;
                    ai.RememberDemolishedRoad(flag.Position, otherFlag.Position);
                    return;
                }
            }
        }

        // Returns the position at the given distance along the road or INVALID_MAPPOS
        // if the road ends at a flag before.
        static MapPos PositionOnPath(Map map, MapPos flagPosition, Direction direction, int distance)
        {
            var position = flagPosition;

            for (int i = 0; i < distance; ++i)
            {
                position = map.Move(position, direction);

                if (map.HasFlag(position))
                    return Global.INVALID_MAPPOS;

                if (i == distance - 1)
                    return position;

                direction = NextPathDirection(map, position, direction);

                if (direction == Direction.None)
                    return Global.INVALID_MAPPOS;
            }

            return Global.INVALID_MAPPOS;
        }

        static Direction NextPathDirection(Map map, MapPos position, Direction incomingDirection)
        {
            var reverse = incomingDirection.Reverse();

            foreach (var direction in DirectionCycleCW.CreateDefault())
            {
                if (direction != reverse && map.HasPath(position, direction))
                    return direction;
            }

            return Direction.None;
        }

        static bool IsRoadWithoutGap(Map map, MapPos flagPosition, Direction direction)
        {
            var position = map.Move(flagPosition, direction);

            while (!map.HasFlag(position))
            {
                if (!map.HasSerf(position))
                    return false;

                direction = NextPathDirection(map, position, direction);

                if (direction == Direction.None)
                    return false;

                position = map.Move(position, direction);
            }

            return true;
        }

        // Checks if the two flags of a road are still connected by other roads.
        static bool AreConnectedWithoutRoad(Flag flag, Flag otherFlag, Direction direction)
        {
            var otherDirection = flag.GetOtherEndDirection(direction);
            var visited = new HashSet<Flag> { flag };
            var queue = new Queue<Flag>();
            queue.Enqueue(flag);

            while (queue.Count != 0 && visited.Count < MaxFlagsToCheckConnection)
            {
                var current = queue.Dequeue();

                foreach (var checkDirection in DirectionCycleCW.CreateDefault())
                {
                    if (!current.HasPath(checkDirection))
                        continue;

                    if ((current == flag && checkDirection == direction) ||
                        (current == otherFlag && checkDirection == otherDirection))
                        continue; // the road to demolish

                    var next = current.GetOtherEndFlag(checkDirection);

                    if (next == null || !visited.Add(next))
                        continue;

                    if (next == otherFlag)
                        return true;

                    queue.Enqueue(next);
                }
            }

            return false;
        }
    }
}
