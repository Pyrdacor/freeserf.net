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

        readonly List<Building.Type> OrderedBuildingTypes = new List<Building.Type>()
        {
            Building.Type.Sawmill,
            Building.Type.Lumberjack,
            Building.Type.Stonecutter,
            Building.Type.Forester,
            Building.Type.Hut,
            Building.Type.Tower,
            Building.Type.Fortress,            
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

        public AIStateCheckNeededBuilding()
            : base(AI.State.CheckNeededBuilding)
        {

        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            CheckResult result = CheckResult.NotNeeded;
            int intelligence = (int)playerInfo.Intelligence;
            bool checkedHut = false;
            List<Building.Type> moveToEnd = new List<Building.Type>();

            try
            {
                foreach (var type in OrderedBuildingTypes)
                {
                    if (checkedHut && player.GetTotalBuildingCount(Building.Type.Hut) == 0)
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

                        // Move the building to the end later
                        if (player.GetTotalBuildingCount(Building.Type.Hut) != 0)
                            moveToEnd.Add(type);

                        return;
                    }
                    else if (result == CheckResult.NeededButNoGenerics)
                    {
                        // Move the building to the end later
                        moveToEnd.Add(type);

                        // we can't do much but wait
                        // TODO: Maybe lower the "generic to knights" ratio? Only if knights are not the needed serfs.
                        continue;
                    }
                    else if (result == CheckResult.NeededButNoSpecialistOrRes)
                    {
                        // Move the building to the end later
                        moveToEnd.Add(type);

                        if (game.GetResourceAmountInInventories(player, Resource.Type.Steel) == 0 &&
                            player.GetCompletedBuildingCount(Building.Type.SteelSmelter) == 0)
                            continue; // we can't craft when we don't have any steel

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
                                // TODO: The tools may be in different inventories. We have to optimize this later.

                                if (game.GetTotalResourceCount(player, neededForBuilding.ResType1) != 0)
                                {
                                    // We only need the second one
                                    GoToState(ai, AI.State.CraftTool, neededForBuilding.ResType2);
                                    return;
                                }
                                else if (game.GetTotalResourceCount(player, neededForBuilding.ResType2) != 0)
                                {
                                    // We only need the first one
                                    GoToState(ai, AI.State.CraftTool, neededForBuilding.ResType1);
                                    return;
                                }

                                // We need both
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
            }
            finally
            {
                foreach (var type in moveToEnd)
                {
                    // Move the buildings to the end of the check list
                    OrderedBuildingTypes.Remove(type);
                    OrderedBuildingTypes.Add(type);
                }
            }

            Kill(ai);
        }

        CheckResult NeedBuilding(AI ai, Game game, Player player, Building.Type type)
        {
            if (!ai.HasResourcesForBuilding(type))
                return CheckResult.NotNeeded;

            var neededForBuilding = Building.Requests[(int)type];
            int inventory = -1;

            if (type == Building.Type.Hut || type == Building.Type.Tower || type == Building.Type.Fortress)
            {
                if (game.GetFreeKnightCount(player) != 0)
                    return CheckResult.Needed;
                else
                    return CheckResult.NeededButNoSpecialistOrRes;
            }
            else
            {
                inventory = game.FindInventoryWithValidSpecialist(player, neededForBuilding.SerfType,
                    neededForBuilding.ResType1, neededForBuilding.ResType2);
            }

            if (inventory != -1)
            {
                if (inventory > 0xffff)
                    return CheckResult.NeededButNoGenerics;

                return CheckResult.Needed;
            }

            if (game.HasAnyOfResource(player, neededForBuilding.ResType1) &&
                (neededForBuilding.ResType2 == Resource.Type.None || game.HasAnyOfResource(player, neededForBuilding.ResType2)))
                return CheckResult.Needed;

            return CheckResult.NeededButNoSpecialistOrRes;
        }

        bool CanBuildMilitary(AI ai, Game game, Player player)
        {
            if (ai.MaxMilitaryBuildings == -1)
                return true;

            var militaryBuildings = game.GetPlayerBuildings(player).Where(b => b.IsMilitary(false));

            return militaryBuildings.Count() < ai.MaxMilitaryBuildings;
        }

        bool TestBuilding(int count, AI ai, Game game, Player player, int spawnEveryMinutes, int spawnEveryTotalLand, int chance = 100)
        {
            return ai.GameTime > (count + 1) * spawnEveryMinutes * Global.TICKS_PER_MIN && player.GetLandArea() > (count + 1) * spawnEveryTotalLand && ai.Chance(chance);
        }

        CheckResult CheckBuilding(AI ai, Game game, Player player, int intelligence, Building.Type type)
        {
            int count = (int)player.GetTotalBuildingCount(type);

            if (count == 0)
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

            // Our focus must be to get a coal and iron mine running as we need steel.
            // We also need a food source.
            if (ai.HardTimes())
            {
                switch (type)
                {
                    case Building.Type.Fisher:
                        if (count == 0 && game.HasAnyOfBuildingCompletedOrMaterialsAtPlace(player, Building.Type.Forester) &&
                            game.Map.FindInTerritory(player.Index, FindFishNear).Count > 0) // fish present
                            return CheckResult.Needed;
                        break;
                    case Building.Type.Farm:
                        if (count == 0 && player.GetTotalBuildingCount(Building.Type.Fisher) == 0)
                        {
                            if (game.HasAnyOfResource(player, Resource.Type.Scythe) ||
                                player.GetSerfCount(Serf.Type.Farmer) != 0)
                                return CheckResult.Needed;
                            else if (game.Map.FindInTerritory(player.Index, FindFishNear).Count == 0) // no fish
                            {
                                // First check if we can expand the territory.
                                // If not trying to craft a scythe is our last hope.
                                // Otherwise we can't do anything. If we are lucky
                                // there is a piece of coal and iron on the way to
                                // make a piece of steel.
                                if (game.GetPossibleFreeKnightCount(player) == 0)
                                    return CheckResult.NeededButNoSpecialistOrRes;
                            }
                        }
                        break;
                    case Building.Type.Hut:
                        if (player.GetIncompleteBuildingCount(Building.Type.Hut) == 0 &&
                            game.HasAnyOfBuildingCompletedOrMaterialsAtPlace(player, Building.Type.Forester) && 
                            !game.GetPlayerBuildings(player, Building.Type.Hut).Any(b => !b.HasKnight())
                            && ai.GameTime > count * 180 * Global.TICKS_PER_SEC && game.GetPossibleFreeKnightCount(player) > 0)
                            return CheckResult.Needed;
                        break;
                    case Building.Type.CoalMine:
                        if (count == 0)
                            return CheckResult.Needed;
                        break;
                    case Building.Type.IronMine:
                        if (count == 0)
                            return CheckResult.Needed;
                        break;
                    case Building.Type.Forester:
                        if (count == 0)
                            return CheckResult.Needed;
                        break;
                        // If we have at least one coal and iron, we can build a steelsmelter and a toolmaker to produce a scythe or pincer.
                        // This can happen with a bit more than minimum supplies or if the mines produce something without a food source.
                        // We will craft a scythe if we can't build a fisher. We will craft a pincer and/or hammer if we have no more knights.
                    case Building.Type.ToolMaker:
                        if (count == 0 && player.GetCompletedBuildingCount(Building.Type.SteelSmelter) != 0)
                            return CheckResult.Needed;
                        break;
                    case Building.Type.SteelSmelter:
                        if (count == 0 && game.GetResourceAmountInInventories(player, Resource.Type.Coal) != 0 &&
                            game.GetResourceAmountInInventories(player, Resource.Type.IronOre) != 0)
                            return CheckResult.Needed;
                        break;
                    case Building.Type.WeaponSmith:
                        if (count == 0 && player.GetCompletedBuildingCount(Building.Type.ToolMaker) != 0 &&
                            game.GetPossibleFreeKnightCount(player) == 0)
                            return NeedBuilding(ai, game, player, type);
                        break;
                }

                return CheckResult.NotNeeded;
            }

            int numIncompleteBuildings = game.GetPlayerBuildings(player).Count(b => !b.IsDone());

            if (numIncompleteBuildings > 2 + player.GetLandArea() / 200 + ai.GameTime / (20 * Global.TICKS_PER_MIN) + Math.Max(ai.ExpandFocus - 1, ai.BuildingFocus))
                return CheckResult.NotNeeded;

            // If we can't produce knights and don't have any left, we won't build
            // several buildings until we have a weaponsmith. Otherwise we might
            // run out of space for large buildings and therefore a weaponsmith.
            bool hasKnightRequirements = ai.HasRequirementsForKnights(game);

            switch (type)
            {
                case Building.Type.Lumberjack:
                    if (TestBuilding(count, ai, game, player, 45 - ai.ConstructionMaterialFocus * 7, 55 - ai.ConstructionMaterialFocus * 10, 75))
                    {
                        if (game.GetResourceAmountInInventories(player, Resource.Type.Plank) < 70 + ai.ConstructionMaterialFocus * ai.ConstructionMaterialFocus * 40 - count * 3)
                            return NeedBuilding(ai, game, player, type);
                    }
                    break;
                case Building.Type.Forester:
                    {
                        int lumberjackCount = (int)player.GetTotalBuildingCount(Building.Type.Lumberjack);

                        if (count < lumberjackCount + ai.ConstructionMaterialFocus && TestBuilding(count, ai, game, player, 30 - ai.ConstructionMaterialFocus * 7, 60 - ai.ConstructionMaterialFocus * 20, 50))
                        {
                            return NeedBuilding(ai, game, player, type);
                        }
                    }
                    break;
                case Building.Type.Stonecutter:
                    if (TestBuilding(count, ai, game, player, 110 - ai.ConstructionMaterialFocus * 32, 100 - ai.ConstructionMaterialFocus * 20, 75))
                    {
                        if (game.GetResourceAmountInInventories(player, Resource.Type.Stone) < 50 + ai.ConstructionMaterialFocus * ai.ConstructionMaterialFocus * 30 - count * 5)
                            return NeedBuilding(ai, game, player, type);
                    }
                    break;
                case Building.Type.Sawmill:
                    {
                        int lumberjackCount = (int)player.GetTotalBuildingCount(Building.Type.Lumberjack);

                        if (count < lumberjackCount * 3 - ai.ConstructionMaterialFocus && TestBuilding(count, ai, game, player, 120 - ai.ConstructionMaterialFocus * 10, 400 - ai.ConstructionMaterialFocus * 15, 30))
                            return NeedBuilding(ai, game, player, type);
                    }
                    break;
                case Building.Type.ToolMaker:
                    if (count < 1 && ai.GameTime > 30 * Global.TICKS_PER_SEC + (2 - intelligence / 20) * Global.TICKS_PER_MIN)
                        return NeedBuilding(ai, game, player, type);
                    if (count < 2 && game.GetResourceAmountInInventories(player, Resource.Type.Plank) >= 100 && game.GetResourceAmountInInventories(player, Resource.Type.Stone) >= 20 &&
                        game.GetResourceAmountInInventories(player, Resource.Type.Steel) >= 75 && TestBuilding(1, ai, game, player, 180, 800, 5))
                        return NeedBuilding(ai, game, player, type);
                    break;
                case Building.Type.Hut:
                    {
                        if (game.GetPossibleFreeKnightCount(player) == 0)
                            return CheckResult.NotNeeded;
                        // TODO: decide if build hut, tower or fortress
                        int focus = Misc.Max(ai.MilitaryFocus, (ai.DefendFocus + 1) / 2, ai.ExpandFocus) + (ai.MilitaryFocus + ai.DefendFocus + ai.ExpandFocus) / 4;

                        if (focus == 0 && player.GetTotalBuildingCount(Building.Type.Hut) == 0)
                            focus = 1;

                        if (CanBuildMilitary(ai, game, player) && count < (focus * 20 + player.GetLandArea() - 250) / 250 + (focus + 1) * ai.GameTime / (600 * Global.TICKS_PER_SEC) - 1 &&
                            ai.GameTime > (90 - intelligence - focus * 15) * Global.TICKS_PER_SEC)
                        {
                            return NeedBuilding(ai, game, player, type);
                        }

                        break;
                    }
                case Building.Type.CoalMine:
                    if (count < 1 && ai.GameTime > (30 - Misc.Max(ai.GoldFocus, ai.SteelFocus, ai.MilitaryFocus) * 10 - intelligence / 7) * 5 * Global.TICKS_PER_SEC)
                    {
                        return NeedBuilding(ai, game, player, type);
                    }
                    else if (count > 0 && count < Math.Max(0, (ai.GameTime - 30 * Global.TICKS_PER_MIN) / ((20 - Misc.Max(ai.GoldFocus, ai.SteelFocus, ai.MilitaryFocus) * 2) * Global.TICKS_PER_MIN)))
                    {
                        uint numIronMines = player.GetCompletedBuildingCount(Building.Type.IronMine);
                        uint numGoldMines = player.GetCompletedBuildingCount(Building.Type.GoldMine);
                        uint numWeaponSmiths = player.GetCompletedBuildingCount(Building.Type.WeaponSmith);

                        var maxNeededCoalMines = Math.Max(2, 1 + (numIronMines + ai.SteelFocus / 2) + (numGoldMines + ai.GoldFocus / 2) + (numWeaponSmiths + ai.MilitaryFocus / 2));

                        if (count < maxNeededCoalMines)
                            return NeedBuilding(ai, game, player, type);
                    }
                    break;
                case Building.Type.IronMine:
                    if (count < 1 && ai.GameTime > (33 - Math.Max(ai.SteelFocus, ai.MilitaryFocus) * 10 - intelligence / 8) * 5 * Global.TICKS_PER_SEC)
                    {
                        return NeedBuilding(ai, game, player, type);
                    }
                    else if (count > 0 && count < Math.Max(0, (ai.GameTime - 30 * Global.TICKS_PER_MIN) / ((24 - Misc.Max(ai.SteelFocus, ai.MilitaryFocus) * 2) * Global.TICKS_PER_MIN)))
                    {
                        uint numCoalMines = player.GetCompletedBuildingCount(Building.Type.CoalMine);

                        var maxNeededIronMines = Math.Max(2, 1 + (numCoalMines + Misc.Max(ai.SteelFocus, ai.MilitaryFocus) / 2));

                        if (count < maxNeededIronMines)
                            return NeedBuilding(ai, game, player, type);
                    }
                    break;
                case Building.Type.GoldMine:
                    if (count < 1 && ai.GameTime > (45 - Math.Max(ai.GoldFocus, ai.MilitaryFocus - 1) * 7 - intelligence / 9) * 5 * Global.TICKS_PER_SEC)
                    {
                        return NeedBuilding(ai, game, player, type);
                    }
                    else if (count > 0 && count < Math.Max(0, (ai.GameTime - 30 * Global.TICKS_PER_MIN) / ((32 - Misc.Max(ai.GoldFocus, ai.MilitaryFocus - 1) * 4) * Global.TICKS_PER_MIN)))
                    {
                        uint numCoalMines = player.GetCompletedBuildingCount(Building.Type.CoalMine);

                        var maxNeededGoldMines = Math.Max(2, 1 + (numCoalMines + Misc.Max(ai.GoldFocus, ai.MilitaryFocus) / 2));

                        if (count < maxNeededGoldMines)
                            return NeedBuilding(ai, game, player, type);
                    }
                    break;
                case Building.Type.StoneMine:
                    if (count < 1 && ai.GameTime > (120 - Math.Max(ai.BuildingFocus, ai.ConstructionMaterialFocus) * 20 - intelligence / 10) * 5 * Global.TICKS_PER_SEC)
                    {
                        return NeedBuilding(ai, game, player, type);
                    }
                    else if (count > 0 && count < Math.Max(0, (ai.GameTime - 40 * Global.TICKS_PER_MIN) / ((40 - ai.ConstructionMaterialFocus * 4) * Global.TICKS_PER_MIN)))
                    {
                        uint numSawMills = player.GetCompletedBuildingCount(Building.Type.Sawmill);

                        var maxNeededStoneMines = Math.Max(2, 1 + (numSawMills + ai.ConstructionMaterialFocus / 2));

                        if (count < maxNeededStoneMines)
                            return NeedBuilding(ai, game, player, type);
                    }
                    break;
                case Building.Type.Farm:
                case Building.Type.Mill:
                case Building.Type.Baker:
                case Building.Type.PigFarm:
                case Building.Type.Butcher:
                case Building.Type.Fisher:
                    if (!hasKnightRequirements)
                        return CheckResult.NotNeeded;
                    if (ai.GameTime > (120 - Math.Max(ai.FoodFocus, ai.BuildingFocus) * 15) * Global.TICKS_PER_SEC && NeedFoodBuilding(ai, game, player, type))
                        return NeedBuilding(ai, game, player, type);
                    break;
                case Building.Type.SteelSmelter:
                    {
                        if (count != 0 && player.GetTotalBuildingCount(Building.Type.WeaponSmith) == 0)
                            return CheckResult.NotNeeded;

                        if (count < player.GetCompletedBuildingCount(Building.Type.IronMine) * Math.Max(2, ai.SteelFocus + 1) &&
                            game.GetResourceAmountInInventories(player, Resource.Type.Plank) >= 5 &&
                            game.GetResourceAmountInInventories(player, Resource.Type.Stone) >= 3)
                            return NeedBuilding(ai, game, player, type);
                    }
                    break;
                case Building.Type.GoldSmelter:
                    {
                        if (!hasKnightRequirements)
                            return CheckResult.NotNeeded;

                        if (count < player.GetCompletedBuildingCount(Building.Type.GoldMine) * (ai.GoldFocus + 2) &&
                            game.GetResourceAmountInInventories(player, Resource.Type.Plank) >= 5 &&
                            game.GetResourceAmountInInventories(player, Resource.Type.Stone) >= 3)
                            return NeedBuilding(ai, game, player, type);
                        else if (count < ai.GoldFocus && game.GetResourceAmountInInventories(player, Resource.Type.GoldOre) >= 24 - ai.GoldFocus * 11)
                        {
                            // Gold focus: 0 -> min 24 ore, 1 -> min 13 ore, 2 -> min 2 ore
                            if (ai.GameTime > (60 - ai.GoldFocus * 20 + count * 20) * Global.TICKS_PER_MIN)
                                return NeedBuilding(ai, game, player, type);
                        }
                    }
                    break;
                case Building.Type.WeaponSmith:
                    {
                        if (count == 0)
                        {
                            if (player.GetCompletedBuildingCount(Building.Type.ToolMaker) > 0 && player.GetCompletedBuildingCount(Building.Type.SteelSmelter) > 0)
                            {
                                return NeedBuilding(ai, game, player, type);
                            }
                            else if (game.GetResourceAmountInInventories(player, Resource.Type.Coal) >= 10 && game.GetResourceAmountInInventories(player, Resource.Type.Steel) >= 10 &&
                                ai.GameTime > (10 - Misc.Max(ai.MilitaryFocus, ai.ExpandFocus, ai.DefendFocus - 1, ai.Aggressivity - 1) - game.RandomInt() % 3) * Global.TICKS_PER_MIN)
                            {
                                return NeedBuilding(ai, game, player, type);
                            }
                        }

                        if (count < Math.Min(player.GetCompletedBuildingCount(Building.Type.CoalMine) + 1, player.GetCompletedBuildingCount(Building.Type.SteelSmelter)))
                            return NeedBuilding(ai, game, player, type);
                    }
                    break;
                case Building.Type.Stock:
                    if (!hasKnightRequirements)
                        return CheckResult.NotNeeded;

                    if (count < (game.Map.Size + 1) / 2 + ai.BuildingFocus &&
                        count < ((int)player.GetLandArea() + ai.BuildingFocus * 100) / 1000 && ai.GameTime > 40 * Global.TICKS_PER_MIN &&
                        player.GetCompletedBuildingCount(Building.Type.WeaponSmith) > count)
                        return NeedBuilding(ai, game, player, type);
                    break;
                case Building.Type.Boatbuilder:
                    if (count < 1 && ai.GameTime > 45 * Global.TICKS_PER_MIN &&
                        game.GetResourceAmountInInventories(player, Resource.Type.Plank) >= 15 &&
                        ai.Chance(10))
                        return NeedBuilding(ai, game, player, type);
                    break;
            }

            return CheckResult.NotNeeded;
        }

        bool NeedFoodBuilding(AI ai, Game game, Player player, Building.Type type)
        {
            // TODO: Fishers can't be build in any case or on any map.
            // Therefore the ratio for food end buildings need adjustment in this case.
            // Otherwise no new food chains will be build.

            int numberOfMines = game.GetPlayerBuildings(player).Where(b =>
                b.BuildingType == Building.Type.CoalMine ||
                b.BuildingType == Building.Type.IronMine ||
                b.BuildingType == Building.Type.GoldMine ||
                b.BuildingType == Building.Type.StoneMine).Count();
            int numberOfFishers = (int)player.GetTotalBuildingCount(Building.Type.Fisher);
            int numberOfFarms = (int)player.GetTotalBuildingCount(Building.Type.Farm);
            int numberOfCompletedFarms = (int)player.GetCompletedBuildingCount(Building.Type.Farm);
            int numberOfMills = (int)player.GetTotalBuildingCount(Building.Type.Mill);
            int numberOfBakers = (int)player.GetTotalBuildingCount(Building.Type.Baker);
            int numberOfPigFarms = (int)player.GetTotalBuildingCount(Building.Type.PigFarm);
            int numberOfButchers = (int)player.GetTotalBuildingCount(Building.Type.Butcher);
            bool hasPotentialFarmer = player.GetSerfCount(Serf.Type.Farmer) != 0 ||
                game.GetResourceAmountInInventories(player, Resource.Type.Scythe) != 0;

            // If there are no mines and some game time has passed
            // the AI will at least build one of its favorite food source chain.
            if (numberOfMines == 0)
            {
                if (ai.GameTime >= (25 - ai.FoodFocus * 4 - (type == Building.Type.Fisher ? 5 : 0)) * Global.TICKS_PER_MIN)
                {
                    switch (type)
                    {
                        case Building.Type.Fisher:
                            if (numberOfFishers == 0 && (ai.GetFoodSourcePriority(0) == 2 || !hasPotentialFarmer))
                                return true;
                            break;
                        case Building.Type.Farm:
                            if (numberOfFarms == 0 && ai.GetFoodSourcePriority(0) != 2 && hasPotentialFarmer)
                                return true;
                            break;
                        case Building.Type.Mill:
                            if (numberOfCompletedFarms != 0 && numberOfMills < Math.Max(1, ai.FoodFocus) && ai.GetFoodSourcePriority(1) == 2 &&
                                game.GetResourceAmountInInventories(player, Resource.Type.Plank) > 3)
                                return true;
                            break;
                        case Building.Type.Baker:
                            if (numberOfMills != 0 && numberOfBakers == 0 && ai.GetFoodSourcePriority(1) == 2)
                                return true;
                            break;
                        case Building.Type.PigFarm:
                            if (numberOfCompletedFarms != 0 && numberOfPigFarms < 2 + ai.FoodFocus && ai.GetFoodSourcePriority(2) == 2 &&
                                game.GetResourceAmountInInventories(player, Resource.Type.Plank) > 4)
                                return true;
                            break;
                        case Building.Type.Butcher:
                            if (numberOfPigFarms != 0 && numberOfButchers == 0 && ai.GetFoodSourcePriority(2) == 2)
                                return true;
                            break;
                    }
                }

                return false;
            }

            int numPossibleMines = numberOfFishers + numberOfButchers * 3 + numberOfBakers * 2;
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
                        if (numberOfMills > 1 + numberOfBakers * 2)
                            return false;
                        if (numberOfCompletedFarms == 0 && game.GetResourceAmountInInventories(player, Resource.Type.Wheat) < 10)
                            return false;
                        if (numberOfMills < numberOfBakers * 2)
                            return true;
                        if (numFoodEndBuildings == 0 && numberOfMills == 0)
                            return ai.GetFoodSourcePriority(1) == 2;
                        return (float)numberOfBakers / numFoodEndBuildings < ai.GetFoodSourcePriorityInPercentage(1) / 100.0f;
                    case Building.Type.Baker:
                        if (numberOfMills == 0)
                            return false;
                        if (numFoodEndBuildings == 0)
                            return ai.GetFoodSourcePriority(1) == 2;
                        return (float)numberOfBakers / numFoodEndBuildings < ai.GetFoodSourcePriorityInPercentage(1) / 100.0f;
                    case Building.Type.PigFarm:
                        if (numberOfPigFarms > 2 + numberOfButchers * 6)
                            return false;
                        if (numberOfCompletedFarms == 0 && game.GetResourceAmountInInventories(player, Resource.Type.Wheat) < 10)
                            return false;
                        if (numberOfPigFarms < numberOfButchers * 6)
                            return true;
                        if (numFoodEndBuildings == 0 && numberOfPigFarms == 0)
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
                            {
                                if (numberOfFarms == 0)
                                    return ai.GetFoodSourcePriority(0) != 2;

                                return false;
                            }

                            /*if (ai.GetFoodSourcePriority(1) == 2)
                                return (float)numberOfButchers / numFoodEndBuildings < ai.GetFoodSourcePriorityInPercentage(2) / 100.0f;
                            else if (ai.GetFoodSourcePriority(2) == 2)
                                return (float)numberOfBakers / numFoodEndBuildings < ai.GetFoodSourcePriorityInPercentage(1) / 100.0f;
                            else
                            {
                                return (float)numberOfBakers / numFoodEndBuildings < ai.GetFoodSourcePriorityInPercentage(1) / 100.0f ||
                                    (float)numberOfButchers / numFoodEndBuildings < ai.GetFoodSourcePriorityInPercentage(2) / 100.0f;
                            }*/

                            return false;
                        }
                }
            }

            return false;
        }

        static bool FindFish(Map map, uint pos)
        {
            return map.IsInWater(pos) && map.GetResourceFish(pos) > 0u;
        }

        static Map.FindData FindFishNear(Map map, uint pos)
        {
            var foundPos = map.FindSpotNear(pos, 4, FindFish, new Random());

            return new Map.FindData()
            {
                Success = foundPos != Global.BadMapPos,
                Data = foundPos
            };
        }
    }
}
