/*
 * AIStateIdle.cs - AI idle state
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

using System;

namespace Freeserf.AIStates
{
    // The idle state is always active and should not be killed.
    // It runs in the background and when all states are killed,
    // this state will run.
    //
    // At the game start the first state will be either ChoosingCastleLocation
    // or CastleBuilt. The first one will switch to CastleBuilt when finished.
    // CastleBuilt will add the idle state to the queue of states.
    //
    // The idle state will decide what to do next.
    class AIStateIdle : AIState
    {
        int checkDisconnectedFlagsTick = 0;
        int buildNeededBuildingsTick = 0;
        int adjustSettingsTick = 0;
        int avoidCongestionTick = 0;
        int destroyUselessBuildingsTick = 0;
        int attackTick = 0;
        AIStateCheckNeededBuilding buildNeededBuildingState = new AIStateCheckNeededBuilding();
        AIStateLinkDisconnectedFlags linkDisconnectedFlagsState = new AIStateLinkDisconnectedFlags();

        public AIStateIdle()
            : base(AI.State.Idle)
        {

        }

        protected override void ReadFrom(Game game, AI ai, string name, SaveReaderText reader)
        {
            base.ReadFrom(game, ai, name, reader);

            checkDisconnectedFlagsTick = reader.Value($"{name}.check_disconnected_flags_tick").ReadInt();
            buildNeededBuildingsTick = reader.Value($"{name}.build_needed_buildings_tick").ReadInt();
            adjustSettingsTick = reader.Value($"{name}.adjust_settings_tick").ReadInt();
            avoidCongestionTick = reader.Value($"{name}.avoid_congestion_tick").ReadInt();
            destroyUselessBuildingsTick = reader.Value($"{name}.destroy_useless_buildings_tick").ReadInt();
            attackTick = reader.Value($"{name}.attack_tick").ReadInt();

            buildNeededBuildingState = (AIStateCheckNeededBuilding)Read(game, ai, $"{name}.build_needed_building_state", reader);
            linkDisconnectedFlagsState = (AIStateLinkDisconnectedFlags)Read(game, ai, $"{name}.link_disconnected_flags_state", reader);
        }

        public override void WriteTo(string name, SaveWriterText writer)
        {
            base.WriteTo(name, writer);

            writer.Value($"{name}.check_disconnected_flags_tick").Write(checkDisconnectedFlagsTick);
            writer.Value($"{name}.build_needed_buildings_tick").Write(buildNeededBuildingsTick);
            writer.Value($"{name}.adjust_settings_tick").Write(adjustSettingsTick);
            writer.Value($"{name}.avoid_congestion_tick").Write(avoidCongestionTick);
            writer.Value($"{name}.destroy_useless_buildings_tick").Write(destroyUselessBuildingsTick);
            writer.Value($"{name}.attack_tick").Write(attackTick);

            if (buildNeededBuildingState == null)
                writer.Value($"{name}.build_needed_building_state.type").Write(AI.State.None);
            else
                buildNeededBuildingState.WriteTo($"{name}.build_needed_building_state", writer);

            if (linkDisconnectedFlagsState == null)
                writer.Value($"{name}.link_disconnected_flags_state.type").Write(AI.State.None);
            else
                linkDisconnectedFlagsState.WriteTo($"{name}.link_disconnected_flags_state", writer);
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            // check for unconnected flags once in a while
            checkDisconnectedFlagsTick += tick;

            if (checkDisconnectedFlagsTick > (180 - (int)playerInfo.Intelligence * 3) * Global.TICKS_PER_SEC)
            {
                checkDisconnectedFlagsTick = 0;

                if (ai.Chance(50 + (int)playerInfo.Intelligence / 4))
                {
                    if (!ai.ContainsState(linkDisconnectedFlagsState))
                    {
                        linkDisconnectedFlagsState.Reset();
                        ai.PushState(linkDisconnectedFlagsState);
                    }

                    return;
                }
            }

            // check for needed buildings and build them
            buildNeededBuildingsTick += tick;

            if (buildNeededBuildingsTick > (18 - (int)playerInfo.Intelligence / 6) * Global.TICKS_PER_SEC)
            {
                buildNeededBuildingsTick = 0;

                if (ai.Chance(85 + (int)playerInfo.Intelligence / 4))
                {
                    if (!ai.ContainsState(buildNeededBuildingState))
                    {
                        buildNeededBuildingState.Reset();
                        ai.PushState(buildNeededBuildingState);
                    }

                    return;
                }
            }

            // check for setting adjustment
            adjustSettingsTick += tick;

            if (adjustSettingsTick > (60 - (int)playerInfo.Intelligence) * Global.TICKS_PER_SEC ||
                (player.GetSerfCount(Serf.Type.Generic) < 10 && player.SerfToKnightRate > 20000))
            {
                adjustSettingsTick = 0;

                if (ai.Chance(30 + (int)playerInfo.Intelligence))
                {
                    ai.PushState(ai.CreateState(AI.State.AdjustSettings));
                    return;
                }
            }

            // check for avoid congestion
            avoidCongestionTick += tick;

            if (avoidCongestionTick > (300 - (int)playerInfo.Intelligence) * Global.TICKS_PER_SEC)
            {
                avoidCongestionTick = 0;

                if (ai.Chance(30 + (int)playerInfo.Intelligence / 4))
                {
                    ai.PushState(ai.CreateState(AI.State.AvoidCongestion));
                    return;
                }
            }

            // check for destroy useless buildings
            if (ai.GameTime >= 5 * Global.TICKS_PER_MIN)
                destroyUselessBuildingsTick += tick;

            if (destroyUselessBuildingsTick > (180 - (int)playerInfo.Intelligence / 2) * Global.TICKS_PER_SEC)
            {
                destroyUselessBuildingsTick = 0;

                if (ai.Chance(40 + (int)playerInfo.Intelligence))
                {
                    ai.PushState(ai.CreateState(AI.State.DestroyUselessBuildings));
                    return;
                }
            }

            // check for attacking
            if (ai.CanAttack && !ai.HardTimes && game.GetPossibleFreeKnightCount(player) > (15 - ai.Aggressivity * 3))
            {
                int attackCheckInterval = (80 - (int)playerInfo.Intelligence / 5 - 18 * Math.Max(ai.Aggressivity, (ai.MilitaryFocus + 1) / 2)) * Global.TICKS_PER_SEC;

                if (ai.GameTime >= (60 - ai.Aggressivity - ai.MilitaryFocus - ai.RushAffinity * 8) * Global.TICKS_PER_MIN)
                    attackTick += tick;

                if (attackTick > attackCheckInterval)
                {
                    attackTick = 0;

                    if (ai.Chance(3 + Misc.Max(ai.MilitaryFocus, ai.Aggressivity + 1, ai.RushAffinity + 1) * 2))
                    {
                        ai.PushState(ai.CreateState(AI.State.Attack));
                        return;
                    }
                }
            }

            // TODO ...
        }

        public override void Kill(AI ai)
        {
            // Leave empty! This state can never be killed.
        }
    }
}
