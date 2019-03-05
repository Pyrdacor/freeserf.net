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
    // TODO: Change settings like tool and flag priorities, military settings and so on.
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
            int highKnightOccupationPreference = Misc.Max(ai.DefendFocus, ai.Aggressivity, (ai.MilitarySkill + 1) / 2, (ai.MilitaryFocus + 1) / 2);

            if (ai.ExpandFocus == 2 && ai.DefendFocus == 2 && ai.Aggressivity < 2)
                highKnightOccupationPreference = 1;

            if (highKnightOccupationPreference == 0 || ai.HardTimes())
            {
                player.SetLowKnightOccupation();
            }
            else if (highKnightOccupationPreference == 1)
            {
                player.SetMediumKnightOccupation(ai.DefendFocus < 1);
            }
            else if (highKnightOccupationPreference == 2)
            {
                player.SetHighKnightOccupation(ai.DefendFocus < 2);
            }

            // castle knights
            if (ai.HardTimes() || (game.GetPossibleFreeKnightCount(player) == 0 && player.GetTotalBuildingCount(Building.Type.WeaponSmith) == 0))
                player.SetCastleKnightsWanted(1u);
            else
            {
                uint additionalKnights = 0;

                if (game.GetPlayerBuildings(player).Count(b => b.IsMilitary() && !b.HasKnight()) == 0)
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
            player.SetSerfToKnightRate((1 + Misc.Max(ai.Aggressivity, ai.MilitaryFocus, ai.ExpandFocus, ai.DefendFocus)) * ushort.MaxValue / 3);

            // food distribution
            player.ResetFoodPriority();

            if (ai.HardTimes())
            {
                player.SetFoodCoalmine(ushort.MaxValue);
                player.SetFoodGoldmine(ushort.MinValue);
                player.SetFoodIronmine(ushort.MaxValue);
                player.SetFoodStonemine(ushort.MinValue);
            }
            else
            {
                int focusSum = ai.GoldFocus + System.Math.Max(ai.SteelFocus, ai.MilitaryFocus) + ai.ConstructionMaterialFocus + 4;

                float goldPerc = (ai.GoldFocus + 1) / (float)focusSum;
                float coalAndIronPerc = (System.Math.Max(ai.SteelFocus, ai.MilitaryFocus) + 1) / (float)focusSum;
                float stonePerc = (ai.ConstructionMaterialFocus + 1) / (float)focusSum;

                player.SetFoodGoldmine((uint)Misc.Round(goldPerc * ushort.MaxValue));
                player.SetFoodCoalmine((uint)Misc.Round(coalAndIronPerc * ushort.MaxValue));
                player.SetFoodIronmine((uint)Misc.Round(coalAndIronPerc * ushort.MaxValue));
                player.SetFoodStonemine((uint)Misc.Round(stonePerc * ushort.MaxValue));

                // TODO: Maybe adjust later a bit, depending on count of mine type and the total resource amount.
            }

            // plank distribution
            int numBoats = game.GetResourceAmountInInventories(player, Resource.Type.Boat);
            int numPlanks = game.GetResourceAmountInInventories(player, Resource.Type.Plank);

            if (numPlanks < 10)
            {
                // with low planks we only give planks to constructions (the craft tool state may change that)
                player.SetPlanksBoatbuilder(ushort.MinValue);
                player.SetPlanksToolmaker(ushort.MinValue);
                player.SetPlanksConstruction(ushort.MaxValue); // max
            }
            else
            {
                // otherwise use the defaults
                player.ResetPlanksPriority();
            }

            if (numBoats >= 100 || (numBoats >= 50 && numPlanks < 100) || (numBoats >= 10 && numPlanks < 30))
                player.SetPlanksBoatbuilder(ushort.MinValue);
            else
                player.SetPlanksBoatbuilder(3275u); // this is the default value

            if (player.EmergencyProgramActive)
            {
                player.SetPlanksBoatbuilder(ushort.MinValue);
                player.SetPlanksToolmaker(ushort.MinValue);
                player.SetPlanksConstruction(ushort.MaxValue); // max
            }

            // steel distribution
            player.ResetSteelPriority();

            if (ai.HardTimes())
            {
                // in hard times distribute all steel to the toolmaker
                player.SetSteelToolmaker(ushort.MaxValue);
                player.SetSteelWeaponsmith(ushort.MinValue);
            }
            // TODO: if military focus is high, the weaponsmith should become nearly any steel there is

            // coal distribution
            player.ResetCoalPriority();

            if (ai.HardTimes() || (ai.GameTime < 30 * Global.TICKS_PER_MIN && game.GetResourceAmountInInventories(player, Resource.Type.Steel) == 0))
            {
                // in hard times distribute all coal to the steelsmelter
                player.SetCoalGoldsmelter(0u);
                player.SetCoalSteelsmelter(65500u);
                player.SetCoalWeaponsmith(0u);
            }
            else
            {
                int focusSum = ai.GoldFocus + ai.SteelFocus + ai.MilitaryFocus + 3;

                float goldPerc = (ai.GoldFocus + 1) / (float)focusSum;
                float steelPerc = (System.Math.Max(ai.SteelFocus, ai.MilitaryFocus - 1) + 1) / (float)focusSum;
                float weaponPerc = (ai.MilitaryFocus + 1) / (float)focusSum;

                player.SetCoalGoldsmelter((uint)Misc.Round(goldPerc * ushort.MaxValue));
                player.SetCoalSteelsmelter((uint)Misc.Round(steelPerc * ushort.MaxValue));
                player.SetCoalWeaponsmith((uint)Misc.Round(weaponPerc * ushort.MaxValue));
            }

            // wheat distribution
            player.ResetWheatPriority();

            if (ai.GetFoodSourcePriority(1) > ai.GetFoodSourcePriority(2))
            {
                player.SetWheatMill(65500u);
                player.SetWheatPigfarm(45850u);
            }
            else
            {
                player.SetWheatMill(45850u);
                player.SetWheatPigfarm(65500u);
            }

            // TODO: send strongest and knight cycling

            // Note: Toolmaker prios are handled by the CraftTool AI state.
            // But if hard times are active we force a scythe or a pick.
            if (ai.HardTimes())
            {
                // set all tool priorities to 0
                for (int i = 0; i < 9; ++i)
                    player.SetToolPriority(i, ushort.MinValue);

                if (player.GetSerfCount(Serf.Type.Farmer) == 0 && !game.HasAnyOfResource(player, Resource.Type.Scythe))
                {
                    // set the priority for the scythe to 100%
                    player.SetToolPriority(Resource.Type.Scythe - Resource.Type.Shovel, ushort.MaxValue);
                }
                else
                {
                    // set the priority for the pick to 100%
                    player.SetToolPriority(Resource.Type.Pick - Resource.Type.Shovel, ushort.MaxValue);
                }
            }

            Kill(ai);
        }
    }
}
