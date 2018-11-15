/*
 * AI.cs - Character AI logic
 *
 * Copyright (C) 2018  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    /// <summary>
    /// Note: The ai will not be the same as in the original game.
    /// But it will reflect some of the ai character descriptions.
    /// The plan is to add some new ai characters with better ai.
    /// 
    /// Maybe the campaign will be extended for this sake as well.
    /// </summary>
    public class AI
    {
        /// <summary>
        /// How aggressive (2 = very aggressive)
        /// </summary>
        int aggressivity = 0; // 0 - 2
        /// <summary>
        /// How skilled in fights (2 = skilled fighter)
        /// </summary>
        int militarySkill = 0; // 0 - 2
        /// <summary>
        /// How much focus on military (2 = high focus)
        /// </summary>
        int militaryFocus = 0; // 0 - 2
        /// <summary>
        /// How much focus on expanding (2 = aggressive expansion)
        /// </summary>
        int expandFocus = 0; // 0 - 2
        /// <summary>
        /// How much focus at having much buildings
        /// </summary>
        int buildingFocus = 0; // 0 - 2
        /// <summary>
        /// How much focus at having much gold
        /// </summary>
        int goldFocus = 0; // 0 - 2
        /// <summary>
        /// The ai will try to gather prioritized food if possible
        /// </summary>
        int[] foodSourcePriorities = new int[3]; // 0 - 2 for fish, bread and meat
        /// <summary>
        /// The ai will try to build prioritized military buildings if possible
        /// </summary>
        int[] militaryBuildingPriorities = new int[3]; // 0 - 2 for hut, tower and fortress
        int[] minPlanksForMilitaryBuildings = new int[2]; // minimum free planks for tower and fortress
        int[] minStonesForMilitaryBuildings = new int[2]; // minimum free stones for tower and fortress

        /* Military Focus
         * 
         * - Focuses on getting coal and iron
         * - Focuses on crafting weapons and shields
         * - High knight production
         * - Many military buildings
         */

        /* Military Skill
         * 
         * - Will protect important buildings with some military buildings
         * - Will have enough knights where needed
         * - Will try to attack strategic spots of the enemy
         * - Prefers attacking spots of the enemy that are able to capture
         */

        /// <summary>
        /// This is called if an enemy is in range to attack the castle.
        /// </summary>
        /// <param name="lastChance">If only the castle and a few buildings remain. Only set for better ai characters.</param>
        public void PrepareForDefendingCastle(bool lastChance)
        {
            // Order all knights to castle
            // - Set maximum knights of military buildings to minimum
            // - Order knights back to castle

            // If lastChance use drastic methods like burning down military buildings
            if (lastChance)
            {
                // Burn down all military buildings to free knights
            }
        }

        public void PrepareFights(bool quick)
        {
            // Set maximum knights of military buildings to maximum

            if (!quick)
            {
                // If bad knights are in military buildings and there are better
                // ones in inventories, than swap with better ones
            }
        }
    }
}
