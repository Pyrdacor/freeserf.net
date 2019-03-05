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
using System.Linq;

namespace Freeserf.AIStates
{
    // TODO: Attack enemy buildings and find good spots to attack.
    class AIStateAttack : AIState
    {
        public AIStateAttack()
            : base(AI.State.Attack)
        {

        }

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
                default:
                case AI.AttackPlayer.Random:
                    break;
                case AI.AttackPlayer.RandomHuman:
                    if (humanPlayers.Count != 0)
                        players = new List<int>(humanPlayers);
                    break;
                case AI.AttackPlayer.RandomAI:
                    if (aiPlayers.Count != 0)
                        players = new List<int>(aiPlayers);
                    break;
                case AI.AttackPlayer.Weakest:
                    // TODO
                    break;
                case AI.AttackPlayer.Worst:
                    // TODO
                    break;
                case AI.AttackPlayer.WorstProtected:
                    {
                        // Here more than one player could be selected. Just remove indices from the lists above.
                        int minOccupation = 3;
                        List<int> playersToRemove = new List<int>(2);

                        for (int i = 0; i < players.Count; ++i)
                        {
                            int occupation = (int)((game.GetPlayer((uint)players[i]).GetKnightOccupation(3u) >> 4) & 0x7);

                            if (occupation < minOccupation)
                            {
                                for (int j = 0; j < i; ++j)
                                {
                                    if (!playersToRemove.Contains(players[j]))
                                        playersToRemove.Add(players[j]);
                                }

                                minOccupation = occupation;
                            }
                            else if (occupation > minOccupation)
                            {
                                playersToRemove.Add(players[i]);
                            }
                        }

                        foreach (var player in playersToRemove)
                        {
                            players.Remove(player);
                            humanPlayers.Remove(player);
                            aiPlayers.Remove(player);
                        }
                    }
                    break;
            }

            switch (ai.SecondPrioritizedPlayer)
            {
                default:
                case AI.AttackPlayer.Random:
                    break;
                case AI.AttackPlayer.RandomHuman:
                    if (humanPlayers.Count != 0)
                        players = new List<int>(humanPlayers);
                    break;
                case AI.AttackPlayer.RandomAI:
                    if (aiPlayers.Count != 0)
                        players = new List<int>(aiPlayers);
                    break;
                case AI.AttackPlayer.Weakest:
                    if (players.Count != 0)
                    {
                        // TODO
                    }
                    break;
                case AI.AttackPlayer.Worst:
                    if (players.Count != 0)
                    {
                        // TODO
                    }
                    break;
                case AI.AttackPlayer.WorstProtected:
                    if (players.Count != 0)
                    {
                        int minOccupation = 3;
                        List<int> playersToRemove = new List<int>(2);

                        for (int i = 0; i < players.Count; ++i)
                        {
                            int occupation = (int)((game.GetPlayer((uint)players[i]).GetKnightOccupation(3u) >> 4) & 0x7);

                            if (occupation < minOccupation)
                            {
                                for (int j = 0; j < i; ++j)
                                {
                                    if (!playersToRemove.Contains(players[j]))
                                        playersToRemove.Add(players[j]);
                                }

                                minOccupation = occupation;
                            }
                            else if (occupation > minOccupation)
                            {
                                playersToRemove.Add(players[i]);
                            }
                        }

                        foreach (var player in playersToRemove)
                        {
                            players.Remove(player);
                        }
                    }
                    break;
            }

            return (players.Count == 0) ? -1 : players[game.RandomInt() % players.Count];
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
                        AttackRandom(game, player, (int)playerInfo.Intelligence, (uint)targetPlayerIndex);
                        Kill(ai);
                        break;
                    case AI.AttackTarget.SmallMilitary:
                        foundTarget = AttackSmallMilitary(game, player, (int)playerInfo.Intelligence, (uint)targetPlayerIndex);
                        break;
                    case AI.AttackTarget.FoodProduction:
                        foundTarget = AttackFoodProduction(game, player, (int)playerInfo.Intelligence, (uint)targetPlayerIndex);
                        break;
                    case AI.AttackTarget.MaterialProduction:
                        foundTarget = AttackMaterialProduction(game, player, (int)playerInfo.Intelligence, (uint)targetPlayerIndex);
                        break;
                    case AI.AttackTarget.Mines:
                        foundTarget = AttackMines(game, player, (int)playerInfo.Intelligence, (uint)targetPlayerIndex);
                        break;
                    case AI.AttackTarget.WeaponProduction:
                        foundTarget = AttackWeaponProduction(game, player, (int)playerInfo.Intelligence, (uint)targetPlayerIndex);
                        break;
                    case AI.AttackTarget.Stocks:
                        foundTarget = AttackStocks(game, player, (int)playerInfo.Intelligence, (uint)targetPlayerIndex);
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
                        AttackRandom(game, player, (int)playerInfo.Intelligence, (uint)targetPlayerIndex);
                        break;
                    case AI.AttackTarget.SmallMilitary:
                        foundTarget = AttackSmallMilitary(game, player, (int)playerInfo.Intelligence, (uint)targetPlayerIndex);
                        break;
                    case AI.AttackTarget.FoodProduction:
                        foundTarget = AttackFoodProduction(game, player, (int)playerInfo.Intelligence, (uint)targetPlayerIndex);
                        break;
                    case AI.AttackTarget.MaterialProduction:
                        foundTarget = AttackMaterialProduction(game, player, (int)playerInfo.Intelligence, (uint)targetPlayerIndex);
                        break;
                    case AI.AttackTarget.Mines:
                        foundTarget = AttackMines(game, player, (int)playerInfo.Intelligence, (uint)targetPlayerIndex);
                        break;
                    case AI.AttackTarget.WeaponProduction:
                        foundTarget = AttackWeaponProduction(game, player, (int)playerInfo.Intelligence, (uint)targetPlayerIndex);
                        break;
                    case AI.AttackTarget.Stocks:
                        foundTarget = AttackStocks(game, player, (int)playerInfo.Intelligence, (uint)targetPlayerIndex);
                        break;
                }

                if (!foundTarget)
                    AttackRandom(game, player, (int)playerInfo.Intelligence, (uint)targetPlayerIndex);
            }

            Kill(ai);
        }

        bool Attack(Game game, Player player, uint targetPosition)
        {
            if (!player.PrepareAttack(targetPosition))
                return false;

            player.StartAttack();

            return true;
        }

        bool AttackRandom(Game game, Player player, List<Building> possibleTargets)
        {
            List<uint> bestTargets = new List<uint>();
            int bestBuildingScore = int.MinValue;

            foreach (var building in possibleTargets)
            {
                if (!building.IsMilitary() || !building.IsDone() || building.IsBurning())
                    continue;

                if (!building.IsActive() || building.GetThreatLevel() != 3)
                    continue;

                int score = CheckTargetBuilding(game, player, building.Position);

                if (score > bestBuildingScore)
                {
                    bestBuildingScore = score;
                    bestTargets.Clear();
                    bestTargets.Add(building.Position);
                }
                else if (score == bestBuildingScore)
                {
                    bestTargets.Add(building.Position);
                }
            }

            if (bestBuildingScore == int.MinValue)
                return false; // no valid target

            // TODO: smart players should think twice if they attack and the score is too bad

            uint targetPosition = bestTargets[game.RandomInt() % bestTargets.Count];

            return Attack(game, player, targetPosition);
        }

        void AttackRandom(Game game, Player player, int intelligence, uint targetPlayerIndex)
        {
            var targetPlayer = game.GetPlayer(targetPlayerIndex);
            var targetPlayerMilitaryBuildings = game.GetPlayerBuildings(targetPlayer).Where(b => b.IsMilitary(true)).ToList();

            AttackRandom(game, player, targetPlayerMilitaryBuildings);
        }

        bool AttackSmallMilitary(Game game, Player player, int intelligence, uint targetPlayerIndex)
        {
            // TODO
            return false;
        }

        bool AttackFoodProduction(Game game, Player player, int intelligence, uint targetPlayerIndex)
        {
            // TODO
            return false;
        }

        bool AttackMaterialProduction(Game game, Player player, int intelligence, uint targetPlayerIndex)
        {
            // TODO
            return false;
        }

        bool AttackMines(Game game, Player player, int intelligence, uint targetPlayerIndex)
        {
            // TODO
            return false;
        }

        bool AttackWeaponProduction(Game game, Player player, int intelligence, uint targetPlayerIndex)
        {
            // TODO
            return false;
        }

        bool AttackStocks(Game game, Player player, int intelligence, uint targetPlayerIndex)
        {
            // TODO
            return false;
        }

        // The higher the return value, the better is this target in terms of winning chance.
        int CheckTargetBuilding(Game game, Player player, uint pos)
        {
            int numMaxAttackKnights = player.KnightsAvailableForAttack(pos);

            if (numMaxAttackKnights == 0)
                return int.MinValue;

            int numKnights = (int)game.GetBuildingAtPos(pos).GetKnightCount();

            return numMaxAttackKnights - numKnights;
        }
    }
}
