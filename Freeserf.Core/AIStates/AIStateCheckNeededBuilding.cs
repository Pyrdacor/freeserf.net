/*
 * AIStateBuildNeededBuilding.cs - AI state for determining which building to build next
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
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.AIStates
{
    // The idle state holds one single instance of this
    // which is pushed to the state stack from time to time.
    // So each AI only uses one instance of this state.
    // Therefore we can store data over multiple executions,
    // like the last build attempt.
    class AIStateCheckNeededBuilding : ResetableAIState
    {
        enum CheckResult
        {
            NotNeeded,
            Needed,
            NeededButNoSpecialistOrRes,
            NeededButNoGenerics
        }

        static readonly Map.Minerals[] MineralFromMine = new Map.Minerals[4]
        {
            Map.Minerals.Stone,
            Map.Minerals.Coal,
            Map.Minerals.Iron,
            Map.Minerals.Gold
        };

        readonly Dictionary<Building.Type, long> lastBuildAttempts = new Dictionary<Building.Type, long>();

        static readonly Building.Type[] OrderedBuildingTypes = new Building.Type[]
        {
            Building.Type.Lumberjack,
            Building.Type.Stonecutter,
            Building.Type.Sawmill,
            Building.Type.Hut,
            Building.Type.Tower,
            Building.Type.Fortress,
            Building.Type.Forester,
            Building.Type.ToolMaker,
            Building.Type.SteelSmelter,
            Building.Type.WeaponSmith,
            Building.Type.GoldSmelter,
            Building.Type.Fisher,
            Building.Type.Farm,
            Building.Type.PigFarm,
            Building.Type.Mill,
            Building.Type.Baker,
            Building.Type.Butcher,
            Building.Type.CoalMine,
            Building.Type.IronMine,
            Building.Type.GoldMine,
            Building.Type.StoneMine,
            Building.Type.Stock,
            Building.Type.Boatbuilder
        };

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            CheckResult result = CheckResult.NotNeeded;
            int intelligence = (int)playerInfo.Intelligence;
            bool checkedHut = false;

            foreach (var type in OrderedBuildingTypes)
            {
                if (checkedHut && game.GetPlayerBuildings(player, Building.Type.Hut).Count() == 0)
                    return; // don't build anything (except for essential buildings) before the first hut!

                if (type == Building.Type.Hut)
                    checkedHut = true;

                if (lastBuildAttempts.ContainsKey(type) && ai.GameTime - lastBuildAttempts[type] < 120 * Global.TICKS_PER_SEC)
                {
                    continue;
                }

                result = CheckBuilding(ai, game, player, intelligence, type);

                if (result == CheckResult.Needed)
                {
                    if (type >= Building.Type.StoneMine && type <= Building.Type.GoldMine)
                        GoToState(ai, AI.State.FindOre, MineralFromMine[type - Building.Type.StoneMine]);
                    else
                        GoToState(ai, AI.State.BuildBuilding, type);

                    lastBuildAttempts[type] = ai.GameTime;

                    return;
                }
                else if (result == CheckResult.NeededButNoGenerics)
                {
                    // we can't do much but wait
                    // TODO: Maybe lower the "generic to knights" ratio?
                    continue;
                }
                else if (result == CheckResult.NeededButNoSpecialistOrRes)
                {
                    if (type == Building.Type.Hut ||
                        type == Building.Type.Tower ||
                        type == Building.Type.Fortress)
                    {
                        GoToState(ai, AI.State.CraftWeapons);
                        return;
                    }
                    else
                    {
                        var neededForBuilding = Building.Requests[(int)type];

                        if (neededForBuilding.ResType2 == Resource.Type.None)
                        {
                            GoToState(ai, AI.State.CraftTool, neededForBuilding.ResType1);
                            return;
                        }
                        else
                        {
                            // TODO: In some cases only one tool is missing so we should
                            //       not craft the other one. Or the tools are in different
                            //       inventories. We have to optimize this later.

                            Kill(ai);
                            ai.PushStates
                            (
                                ai.CreateState(AI.State.CraftTool, neededForBuilding.ResType1),
                                ai.CreateState(AI.State.CraftTool, neededForBuilding.ResType2)
                            );
                            return;
                        }
                    }
                }
            }

            Kill(ai);
        }

        CheckResult NeedBuilding(AI ai, Game game, Player player, Building.Type type)
        {
            if (!ai.HasResourcesForBuilding(type))
                return CheckResult.NotNeeded;

            var neededForBuilding = Building.Requests[(int)type];

            int inventory = game.FindInventoryWithValidSpecialist(player, neededForBuilding.SerfType,
                neededForBuilding.ResType1, neededForBuilding.ResType2);

            if (inventory != -1)
            {
                if (inventory > 0xffff)
                    return CheckResult.NeededButNoGenerics;

                return CheckResult.Needed;
            }

            return CheckResult.NeededButNoSpecialistOrRes;
        }

        bool CanBuildMilitary(AI ai, Game game, Player player)
        {
            if (ai.MaxMilitaryBuildings == -1)
                return true;

            var militaryBuildings = game.GetPlayerBuildings(player).Where(b => b.IsMilitary(false));

            return militaryBuildings.Count() < ai.MaxMilitaryBuildings;
        }

        CheckResult CheckBuilding(AI ai, Game game, Player player, int intelligence, Building.Type type)
        {
            int count = game.GetPlayerBuildings(player, type).Count();

            if (count < 1)
            {
                switch (type)
                {
                    // these are essential buildings
                    case Building.Type.Lumberjack:
                    case Building.Type.Sawmill:
                    case Building.Type.Stonecutter:
                        return NeedBuilding(ai, game, player, type);
                }
            }

            // Don't build new buildings while the emergency program is active.
            // The essential buildings are handled above.
            if (player.EmergencyProgramActive)
                return CheckResult.NotNeeded;

            switch (type)
            {
                case Building.Type.Lumberjack:
                    if (count < 2 && ai.GameTime > (20 - ai.ConstructionMaterialFocus * 8 + game.RandomInt() % 6) * Global.TICKS_PER_MIN)
                    {
                        return NeedBuilding(ai, game, player, type);
                    }
                    else if (count < 3 && ai.GameTime > (45 - ai.ConstructionMaterialFocus * 15 + game.RandomInt() % 11) * Global.TICKS_PER_MIN)
                    {
                        return NeedBuilding(ai, game, player, type);
                    }
                    // TODO ...
                    break;
                case Building.Type.Forester:
                    {
                        int lumberjackCount = game.GetPlayerBuildings(player, Building.Type.Lumberjack).Count();

                        if (count < lumberjackCount + ai.ConstructionMaterialFocus / 2 && ai.GameTime > 30 * Global.TICKS_PER_SEC - ai.ConstructionMaterialFocus * 3 * Global.TICKS_PER_SEC + count * (2 * Global.TICKS_PER_MIN - ai.ConstructionMaterialFocus * 20 * Global.TICKS_PER_SEC))
                        {
                            return NeedBuilding(ai, game, player, type);
                        }
                    }
                    break;
                case Building.Type.Stonecutter:
                    if (count < 2 && ai.GameTime > (60 - ai.ConstructionMaterialFocus * 7 + game.RandomInt() % 17) * Global.TICKS_PER_MIN)
                    {
                        return NeedBuilding(ai, game, player, type);
                    }
                    else if (count < 3 && ai.GameTime > (108 - ai.ConstructionMaterialFocus * 12 + game.RandomInt() % 21) * Global.TICKS_PER_MIN)
                    {
                        return NeedBuilding(ai, game, player, type);
                    }
                    // TODO ...
                    break;
                case Building.Type.ToolMaker:
                    if (count < 1 && ai.GameTime > 30 * Global.TICKS_PER_SEC + (2 - intelligence / 20) * Global.TICKS_PER_MIN)
                        return NeedBuilding(ai, game, player, type);
                    // TODO ...
                    break;
                case Building.Type.Hut:
                    {
                        // TODO: decide if build hut, tower or fortress
                        int focus = Math.Max(ai.MilitaryFocus, Math.Max((ai.DefendFocus + 1) / 2, ai.ExpandFocus)) + (ai.MilitaryFocus + ai.DefendFocus + ai.ExpandFocus) / 4;

                        if (focus == 0 && game.GetPlayerBuildings(player, Building.Type.Hut).Count() == 0)
                            focus = 1;

                        if (CanBuildMilitary(ai, game, player) && count < (focus * 20 + player.GetLandArea()) / 400 + (focus + 1) * ai.GameTime / (850 * Global.TICKS_PER_SEC) - 1 &&
                            ai.GameTime > (90 - intelligence - focus * 15) * Global.TICKS_PER_SEC)
                        {
                            return NeedBuilding(ai, game, player, type);
                        }
                        // TODO ...
                        break;
                    }
                case Building.Type.CoalMine:
                    if (count < 1 && ai.GameTime > (30 - Math.Max(ai.GoldFocus, Math.Max(ai.SteelFocus, ai.MilitaryFocus)) * 10 - intelligence / 7) * 5 * Global.TICKS_PER_SEC)
                    {
                        return NeedBuilding(ai, game, player, type);
                    }
                    // TODO ...
                    break;
                case Building.Type.IronMine:
                    if (count < 1 && ai.GameTime > (33 - Math.Max(ai.SteelFocus, ai.MilitaryFocus) * 10 - intelligence / 8) * 5 * Global.TICKS_PER_SEC)
                    {
                        return NeedBuilding(ai, game, player, type);
                    }
                    // TODO ...
                    break;
                case Building.Type.GoldMine:
                    if (count < 1 && ai.GameTime > (45 - Math.Max(ai.GoldFocus, ai.MilitaryFocus - 1) * 10 - intelligence / 9) * 5 * Global.TICKS_PER_SEC)
                    {
                        return NeedBuilding(ai, game, player, type);
                    }
                    // TODO ...
                    break;
                case Building.Type.StoneMine:
                    if (count < 1 && ai.GameTime > (120 - Math.Max(ai.BuildingFocus, ai.ConstructionMaterialFocus) * 20 - intelligence / 10) * 5 * Global.TICKS_PER_SEC)
                    {
                        return NeedBuilding(ai, game, player, type);
                    }
                    // TODO ...
                    break;
                case Building.Type.Farm:
                case Building.Type.Mill:
                case Building.Type.Baker:
                case Building.Type.PigFarm:
                case Building.Type.Butcher:
                case Building.Type.Fisher:
                    if (ai.GameTime > (120 - Math.Max(ai.FoodFocus, ai.BuildingFocus) * 15) * Global.TICKS_PER_SEC && NeedFoodBuilding(ai, game, player, type))
                        return NeedBuilding(ai, game, player, type);
                    break;
                // TODO ...
            }

            return CheckResult.NotNeeded;
        }

        bool NeedFoodBuilding(AI ai, Game game, Player player, Building.Type type)
        {
            int numberOfMines = game.GetPlayerBuildings(player).Where(b =>
                b.BuildingType == Building.Type.CoalMine ||
                b.BuildingType == Building.Type.IronMine ||
                b.BuildingType == Building.Type.GoldMine ||
                b.BuildingType == Building.Type.StoneMine).Count();
            int numberOfFishers = game.GetPlayerBuildings(player, Building.Type.Fisher).Count();
            int numberOfFarms = game.GetPlayerBuildings(player, Building.Type.Farm).Count();
            int numberOfMills = game.GetPlayerBuildings(player, Building.Type.Mill).Count();
            int numberOfBakers = game.GetPlayerBuildings(player, Building.Type.Baker).Count();
            int numberOfPigFarms = game.GetPlayerBuildings(player, Building.Type.PigFarm).Count();
            int numberOfButchers = game.GetPlayerBuildings(player, Building.Type.Butcher).Count();

            int numPossibleMines = numberOfFishers + numberOfButchers * 3 + numberOfBakers * 3;
            int numMineDiff = ai.FoodFocus + numberOfMines - numPossibleMines;

            if (numMineDiff <= 0)
            {
                return false;
            }
            else
            {
                int numFoodEndBuildings = numberOfFishers + numberOfBakers + numberOfButchers;

                switch (type)
                {
                    case Building.Type.Fisher:
                        if (numFoodEndBuildings == 0)
                            return ai.GetFoodSourcePriority(0) == 2;
                        return (float)numberOfFishers / numFoodEndBuildings < ai.GetFoodSourcePriorityInPercentage(0) / 100.0f;
                    case Building.Type.Mill:
                        if (numberOfFarms == 0)
                            return false;
                        if (numberOfMills < numberOfBakers * 2)
                            return true;
                        if (numFoodEndBuildings == 0)
                            return ai.GetFoodSourcePriority(1) == 2;
                        return (float)numberOfBakers / numFoodEndBuildings < ai.GetFoodSourcePriorityInPercentage(1) / 100.0f;
                    case Building.Type.Baker:
                        if (numberOfMills == 0)
                            return false;
                        if (numFoodEndBuildings == 0)
                            return ai.GetFoodSourcePriority(1) == 2;
                        return (float)numberOfBakers / numFoodEndBuildings < ai.GetFoodSourcePriorityInPercentage(1) / 100.0f;
                    case Building.Type.PigFarm:
                        if (numberOfFarms == 0)
                            return false;
                        if (numberOfPigFarms < numberOfButchers * 6)
                            return true;
                        if (numFoodEndBuildings == 0)
                            return ai.GetFoodSourcePriority(2) == 2;
                        return (float)numberOfButchers / numFoodEndBuildings < ai.GetFoodSourcePriorityInPercentage(2) / 100.0f;
                    case Building.Type.Butcher:
                        if (numberOfPigFarms == 0)
                            return false;
                        if (numFoodEndBuildings == 0)
                            return ai.GetFoodSourcePriority(2) == 2;
                        return (float)numberOfButchers / numFoodEndBuildings < ai.GetFoodSourcePriorityInPercentage(2) / 100.0f;
                    case Building.Type.Farm:
                        {
                            int numNeeded = numberOfMills * 2 + (numberOfPigFarms + 5) / 6;

                            if (numNeeded > numberOfFarms)
                                return true;

                            if (numFoodEndBuildings == 0)
                                return ai.GetFoodSourcePriority(0) != 2;

                            return (float)numberOfBakers / numFoodEndBuildings < ai.GetFoodSourcePriorityInPercentage(1) / 100.0f ||
                                (float)numberOfButchers / numFoodEndBuildings < ai.GetFoodSourcePriorityInPercentage(2) / 100.0f;
                        }
                }
            }

            return false;
        }
    }
}
