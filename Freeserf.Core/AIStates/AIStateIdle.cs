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
        int checkUnconnectedFlagsTick = 0;
        int buildNeededBuildingsTick = 0;
        int adjustSettingsTick = 0;
        int avoidCongestionTick = 0;
        int destroyUselessBuildingsTick = 0;
        int attackTick = 0;
        AIStateBuildNeededBuilding buildNeededBuildingState = new AIStateBuildNeededBuilding();
        
        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            // check for unconnected flags once in a while
            checkUnconnectedFlagsTick += tick;

            if (checkUnconnectedFlagsTick > (5 - (int)playerInfo.Intelligence / 15) * Global.TICKS_PER_SEC)
            {
                checkUnconnectedFlagsTick = 0;

                if (game.RandomInt() % 10 < 4)
                {
                    ai.PushState(ai.CreateState(AI.State.LinkDisconnectedFlags));
                    return;
                }
            }

            // check for needed buildings and build them
            buildNeededBuildingsTick += tick;

            if (buildNeededBuildingsTick > (10 - (int)playerInfo.Intelligence / 9) * Global.TICKS_PER_SEC)
            {
                buildNeededBuildingsTick = 0;

                if (game.RandomInt() % 10 < 7)
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

            if (adjustSettingsTick > (32 - (int)playerInfo.Intelligence / 3) * Global.TICKS_PER_SEC)
            {
                adjustSettingsTick = 0;

                if (game.RandomInt() % 10 < 3)
                {
                    ai.PushState(ai.CreateState(AI.State.AdjustSettings));
                    return;
                }
            }

            // check for avoid congestion
            avoidCongestionTick += tick;

            if (avoidCongestionTick > (160 - (int)playerInfo.Intelligence) * Global.TICKS_PER_SEC)
            {
                avoidCongestionTick = 0;

                if (game.RandomInt() % 10 < 5)
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

                if (game.RandomInt() % 10 < 4)
                {
                    ai.PushState(ai.CreateState(AI.State.DestroyUselessBuildings));
                    return;
                }
            }

            // check for attacking
            if (ai.CanAttack)
            {
                int attackCheckInterval = (60 - 15 * Math.Max(ai.Aggressivity, (ai.MilitaryFocus + 1) / 2)) * Global.TICKS_PER_SEC;

                if (ai.GameTime >= (5 - (ai.Aggressivity + ai.MilitaryFocus) / 3) * Global.TICKS_PER_MIN)
                    attackTick += tick;

                if (attackTick > attackCheckInterval)
                {
                    attackTick = 0;

                    if (game.RandomInt() % 100 < 2 + Math.Max(ai.MilitarySkill, ai.Aggressivity) * 2)
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
