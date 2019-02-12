/*
 * AIStateAttack.cs - AI state for attacking
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

using System.Collections.Generic;

namespace Freeserf.AIStates
{
    // TODO: Attack enemy buildings and find good spots to attack.
    class AIStateAttack : AIState
    {
        int GetTargetPlayer(AI ai, Game game, uint selfIndex)
        {
            List<int> players = new List<int>(3);
            List<int> humanPlayers = new List<int>(3);
            List<int> aiPlayers = new List<int>(3);

            for (int i = 0; i < game.GetPlayerCount(); ++i)
            {
                if (i != selfIndex)
                {
                    players.Add(i);

                    if (game.GetPlayer((uint)i).IsAi())
                        aiPlayers.Add(i);
                    else
                        humanPlayers.Add(i);
                }
            }

            if (players.Count == 0)
                return -1;

            switch (ai.PrioritizedPlayer)
            {
                case AI.AttackPlayer.Random:
                    return players[game.RandomInt() % players.Count];
                case AI.AttackPlayer.RandomHuman:
                    if (humanPlayers.Count != 0)
                        return humanPlayers[game.RandomInt() % humanPlayers.Count];
                    break;
                case AI.AttackPlayer.RandomAI:
                    if (aiPlayers.Count != 0)
                        return aiPlayers[game.RandomInt() % aiPlayers.Count];
                    break;
                case AI.AttackPlayer.Weakest:
                    // TODO
                    break;
                case AI.AttackPlayer.Worst:
                    // TODO
                    break;
                case AI.AttackPlayer.WorstProtected:
                    // TODO
                    break;
            }

            switch (ai.SecondPrioritizedPlayer)
            {
                case AI.AttackPlayer.Random:
                    return players[game.RandomInt() % players.Count];
                case AI.AttackPlayer.RandomHuman:
                    return (humanPlayers.Count == 0) ? -1 : humanPlayers[game.RandomInt() % humanPlayers.Count];
                case AI.AttackPlayer.RandomAI:
                    return (aiPlayers.Count == 0) ? -1 : aiPlayers[game.RandomInt() % aiPlayers.Count];
                case AI.AttackPlayer.Weakest:
                    // TODO
                    break;
                case AI.AttackPlayer.Worst:
                    // TODO
                    break;
                case AI.AttackPlayer.WorstProtected:
                    // TODO
                    break;
            }

            return -1;
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            if (ai.CanAttack)
            {
                int targetPlayerIndex = GetTargetPlayer(ai, game, player.Index);

                if (targetPlayerIndex == -1)
                {
                    Kill(ai);
                    return;
                }

                bool foundTarget = false;

                switch (ai.PrioritizedAttackTarget)
                {
                    default:
                    case AI.AttackTarget.Random:
                        // with random we won't check for secondary targets
                        AttackRandom(game, player, (int)playerInfo.Intelligence);
                        Kill(ai);
                        break;
                    case AI.AttackTarget.SmallMilitary:
                        foundTarget = AttackSmallMilitary(game, player, (int)playerInfo.Intelligence);
                        break;
                    case AI.AttackTarget.FoodProduction:
                        foundTarget = AttackFoodProduction(game, player, (int)playerInfo.Intelligence);
                        break;
                    case AI.AttackTarget.MaterialProduction:
                        foundTarget = AttackMaterialProduction(game, player, (int)playerInfo.Intelligence);
                        break;
                    case AI.AttackTarget.Mines:
                        foundTarget = AttackMines(game, player, (int)playerInfo.Intelligence);
                        break;
                    case AI.AttackTarget.WeaponProduction:
                        foundTarget = AttackWeaponProduction(game, player, (int)playerInfo.Intelligence);
                        break;
                    case AI.AttackTarget.Stocks:
                        foundTarget = AttackStocks(game, player, (int)playerInfo.Intelligence);
                        break;
                }

                if (foundTarget)
                {
                    Kill(ai);
                    return;
                }

                switch (ai.SecondPrioritizedAttackTarget)
                {
                    default:
                    case AI.AttackTarget.Random:
                        AttackRandom(game, player, (int)playerInfo.Intelligence);
                        break;
                    case AI.AttackTarget.SmallMilitary:
                        AttackSmallMilitary(game, player, (int)playerInfo.Intelligence);
                        break;
                    case AI.AttackTarget.FoodProduction:
                        AttackFoodProduction(game, player, (int)playerInfo.Intelligence);
                        break;
                    case AI.AttackTarget.MaterialProduction:
                        AttackMaterialProduction(game, player, (int)playerInfo.Intelligence);
                        break;
                    case AI.AttackTarget.Mines:
                        AttackMines(game, player, (int)playerInfo.Intelligence);
                        break;
                    case AI.AttackTarget.WeaponProduction:
                        AttackWeaponProduction(game, player, (int)playerInfo.Intelligence);
                        break;
                    case AI.AttackTarget.Stocks:
                        AttackStocks(game, player, (int)playerInfo.Intelligence);
                        break;
                }
            }

            Kill(ai);
        }

        void AttackRandom(Game game, Player player, int intelligence)
        {

        }

        bool AttackSmallMilitary(Game game, Player player, int intelligence)
        {
            // TODO
            return false;
        }

        bool AttackFoodProduction(Game game, Player player, int intelligence)
        {
            // TODO
            return false;
        }

        bool AttackMaterialProduction(Game game, Player player, int intelligence)
        {
            // TODO
            return false;
        }

        bool AttackMines(Game game, Player player, int intelligence)
        {
            // TODO
            return false;
        }

        bool AttackWeaponProduction(Game game, Player player, int intelligence)
        {
            // TODO
            return false;
        }

        bool AttackStocks(Game game, Player player, int intelligence)
        {
            // TODO
            return false;
        }
    }
}
