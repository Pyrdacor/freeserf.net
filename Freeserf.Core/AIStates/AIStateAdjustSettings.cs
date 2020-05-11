/*
 * AIStateAdjustSettings.cs - AI state for adjusting settings
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

using System.Linq;

namespace Freeserf.AIStates
{
    class AIStateAdjustSettings : AIState
    {
        public AIStateAdjustSettings()
            : base(AI.State.AdjustSettings)
        {

        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            // knight occupation setting 
            // TODO: should change if amount of knights change in relation to number of military buildings
            int highKnightOccupationPreference = Misc.Max(ai.ExpandFocus, ai.DefendFocus, ai.Aggressivity, (ai.MilitarySkill + 1) / 2, (ai.MilitaryFocus + 1) / 2);

            if (highKnightOccupationPreference != 0)
            {
                int numFreeKnights = game.GetPossibleFreeKnightCount(player);

                if (highKnightOccupationPreference == 2 && numFreeKnights < 5)
                {
                    highKnightOccupationPreference = 0;
                }
                else if (numFreeKnights < 10 - ai.Aggressivity + (ai.MilitarySkill + ai.DefendFocus + 1) / 2)
                {
                    --highKnightOccupationPreference;
                }
            }

            int defendingOrientation = Misc.Max(0, ai.DefendFocus - (ai.ExpandFocus + ai.Aggressivity + 1) / 2);

            if (highKnightOccupationPreference == 0 || ai.HardTimes)
            {
                player.SetLowKnightOccupation();
            }
            else if (highKnightOccupationPreference == 1)
            {
                player.SetMediumKnightOccupation(defendingOrientation < 1);
            }
            else if (highKnightOccupationPreference == 2)
            {
                player.SetHighKnightOccupation(defendingOrientation < 2);
            }

            // castle knights
            if (ai.HardTimes || (game.GetPossibleFreeKnightCount(player) == 0 && player.GetTotalBuildingCount(Building.Type.WeaponSmith) == 0))
                player.SetCastleKnightsWanted(1u);
            else
            {
                uint additionalKnights = 0;

                if (game.GetPlayerBuildings(player).Count(building => building.IsMilitary() && !building.HasKnight()) == 0)
                {
                    // every 45-53 minutes (depending on intelligence) an additional knight should protect the castle (starting at 35 minutes)
                    additionalKnights = (uint)Misc.Max(0, (int)((ai.GameTime - 35 * Global.TICKS_PER_MIN) / ((53 - playerInfo.Intelligence / 5) * Global.TICKS_PER_MIN)));

                    // every 40-60 minutes (depending on focus) an additional knight should protect the castle
                    additionalKnights += (uint)(ai.GameTime / ((60 - Misc.Max(ai.DefendFocus, ai.MilitaryFocus, ai.MilitarySkill) * 10) * Global.TICKS_PER_MIN));
                }

                player.SetCastleKnightsWanted(3u + (uint)Misc.Max(ai.DefendFocus, ai.MilitaryFocus, ai.MilitarySkill) + additionalKnights);
            }

            // TODO: flag/inventory prios

            // TODO: inventory serf/resource in/out modes

            // serf to knight rate
            if (player.GetSerfCount(Serf.Type.Generic) >= 10)
                player.SerfToKnightRate = (1 + Misc.Max(ai.Aggressivity, ai.MilitaryFocus, ai.ExpandFocus, ai.DefendFocus)) * ushort.MaxValue / 3;
            else
                player.SerfToKnightRate = 20000; // default value

            // food distribution
            player.ResetFoodPriority();

            if (ai.HardTimes)
            {
                player.FoodCoalmine = ushort.MaxValue;
                player.FoodGoldmine = ushort.MinValue;
                player.FoodIronmine = ushort.MaxValue;
                player.FoodStonemine = ushort.MinValue;
            }
            else if ((ai.GoldFocus >= ai.SteelFocus || ai.ConstructionMaterialFocus >= ai.SteelFocus) && player.TotalKnightCount < 15 && game.GetPossibleFreeKnightCount(player) < 5)
            {
                // If gold or stone focus is higher than coal/iron focus and we don't have many knights yet and need some more,
                // we will distribute the food to iron and coal mines a bit longer.
                player.FoodCoalmine = ushort.MaxValue;
                player.FoodGoldmine = ushort.MinValue;
                player.FoodIronmine = ushort.MaxValue;
                player.FoodStonemine = ushort.MinValue;
            }
            else
            {
                int focusSum = ai.GoldFocus + System.Math.Max(ai.SteelFocus, ai.MilitaryFocus) + ai.ConstructionMaterialFocus + 4;

                float goldPerc = (ai.GoldFocus + 1) / (float)focusSum;
                float coalAndIronPerc = (System.Math.Max(ai.SteelFocus, ai.MilitaryFocus) + 1) / (float)focusSum;
                float stonePerc = (ai.ConstructionMaterialFocus + 1) / (float)focusSum;

                player.FoodGoldmine = (uint)Misc.Round(goldPerc * ushort.MaxValue);
                player.FoodCoalmine = (uint)Misc.Round(coalAndIronPerc * ushort.MaxValue);
                player.FoodIronmine = (uint)Misc.Round(coalAndIronPerc * ushort.MaxValue);
                player.FoodStonemine = (uint)Misc.Round(stonePerc * ushort.MaxValue);

                // TODO: Maybe adjust later a bit, depending on count of mine type and the total resource amount.
            }

            // plank distribution
            uint numBoats = game.GetResourceAmountInInventories(player, Resource.Type.Boat);
            uint numPlanks = game.GetResourceAmountInInventories(player, Resource.Type.Plank);

            if (!ai.HardTimes) // In hard times this is set directly when planning a tool.
            {
                if (numPlanks < 10)
                {
                    // with low planks we only give planks to constructions (the craft tool state may change that)
                    player.PlanksBoatbuilder = ushort.MinValue;
                    player.PlanksToolmaker = ushort.MinValue;
                    player.PlanksConstruction = ushort.MaxValue; // max
                }
                else
                {
                    // otherwise use the defaults
                    player.ResetPlanksPriority();
                }
            }

            if (numBoats >= 100 || (numBoats >= 50 && numPlanks < 100) || (numBoats >= 10 && numPlanks < 30))
                player.PlanksBoatbuilder = ushort.MinValue;
            else
                player.PlanksBoatbuilder = 3275u; // this is the default value

            if (player.EmergencyProgramActive)
            {
                player.PlanksBoatbuilder = ushort.MinValue;
                player.PlanksToolmaker = ushort.MinValue;
                player.PlanksConstruction = ushort.MaxValue; // max
            }

            // steel distribution
            player.ResetSteelPriority();

            if (ai.HardTimes)
            {
                // in hard times distribute all steel to the toolmaker
                player.SteelToolmaker = ushort.MaxValue;
                player.SteelWeaponsmith = ushort.MinValue;
            }
            else if (ai.MilitaryFocus == 2)
            {
                // if military focus is high, the weaponsmith gets nearly any steel there is
                player.SteelToolmaker = 1 * ushort.MaxValue / 8;
                player.SteelWeaponsmith = 7 * ushort.MaxValue / 8;
            }

            // coal distribution
            player.ResetCoalPriority();

            if (ai.HardTimes || (ai.GameTime < 30 * Global.TICKS_PER_MIN && game.GetResourceAmountInInventories(player, Resource.Type.Steel) == 0))
            {
                // in hard times distribute all coal to the steelsmelter
                player.CoalGoldsmelter = 0u;
                player.CoalSteelsmelter = 65500u;
                player.CoalWeaponsmith = 0u;
            }
            else
            {
                int focusSum = ai.GoldFocus + ai.SteelFocus + ai.MilitaryFocus + 3;

                float goldPerc = (ai.GoldFocus + 1) / (float)focusSum;
                float steelPerc = (System.Math.Max(ai.SteelFocus, ai.MilitaryFocus - 1) + 1) / (float)focusSum;
                float weaponPerc = (ai.MilitaryFocus + 1) / (float)focusSum;

                player.CoalGoldsmelter = (uint)Misc.Round(goldPerc * ushort.MaxValue);
                player.CoalSteelsmelter = (uint)Misc.Round(steelPerc * ushort.MaxValue);
                player.CoalWeaponsmith = (uint)Misc.Round(weaponPerc * ushort.MaxValue);
            }

            // wheat distribution
            player.ResetWheatPriority();

            if (ai.GetFoodSourcePriority(1) > ai.GetFoodSourcePriority(2))
            {
                player.WheatMill = 65500u;
                player.WheatPigfarm = 45850u;
            }
            else
            {
                player.WheatMill = 45850u;
                player.WheatPigfarm = 65500u;
            }

            // send strongest
            player.SendStrongest = ai.DefendFocus != 2 && (ai.MilitarySkill > 0 || ai.MilitaryFocus > ai.DefendFocus);

            // TODO: knight cycling

            // Note: Toolmaker prios are handled by the CraftTool AI state.

            Kill(ai);
        }
    }
}
