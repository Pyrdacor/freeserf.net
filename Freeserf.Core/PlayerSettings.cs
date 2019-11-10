/*
 * PlayerSettings.cs - Player related functions
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

namespace Freeserf
{
    using Serialize;
    using word = UInt16;
    using dword = UInt32;

    [Flags]
    public enum PlayerSettingFlags : byte
    {
        None =                          0x00,
        /// <summary>
        /// Send strongest knights to fights
        /// </summary>
        SendStrongest =                 0x01,
        /// <summary>
        /// The cycling of knights is active
        /// (swap weak ones with good ones)
        /// </summary>
        CyclingKnightsInProgress =      0x02,
        /// <summary>
        /// The knight level is reduced due
        /// to knight cycling. Is set to off
        /// when the second phase starts.
        /// </summary>
        CyclingKnightsReducedLevel =    0x04,
        /// <summary>
        /// This will be set later when new
        /// knights are sent out to the military
        /// buildings while cycling knights.
        /// </summary>
        CyclingKnightsSecondPhase =     0x08
    }

    [DataClass]
    public class PlayerSettings : IData
    {
        [Ignore]
        public bool Dirty
        {
            get;
            internal set;
        }

        public PlayerSettingFlags Flags { get; set; } = PlayerSettingFlags.None;
        /// <summary>
        /// 0 = minimum (0%), 65535 = maximum (100%)
        /// </summary>
        public word[] ToolPriorities { get; set; } = new word[Constants.NUM_TOOL_TYPES];
        public byte[] FlagPriorities { get; set; } = new byte[Constants.NUM_RESOURCE_TYPES];
        public byte[] InventoryPriorities { get; set; } = new byte[Constants.NUM_RESOURCE_TYPES];
        /// <summary>
        /// Lower 4 bits = min level, higher 4 bits = max level
        /// </summary>
        public byte[] KnightOccupation { get; set; } = new byte[Constants.NUM_TREATMENT_LEVEL_TYPES];
        public word SerfToKnightRate { get; set; } = 0;
        /* +1 for every castle defeated,
           -1 for own castle lost. */
        public byte CastleKnightsWanted { get; set; } = 3;
        public word FoodStonemine { get; set; } = 0; /* Food delivery priority of food for mines. */
        public word FoodCoalmine { get; set; } = 0;
        public word FoodIronmine { get; set; } = 0;
        public word FoodGoldmine { get; set; } = 0;
        public word PlanksConstruction { get; set; } = 0; /* Planks delivery priority. */
        public word PlanksBoatbuilder { get; set; } = 0;
        public word PlanksToolmaker { get; set; } = 0;
        public word SteelToolmaker { get; set; } = 0;
        public word SteelWeaponsmith { get; set; } = 0;
        public word CoalSteelsmelter { get; set; } = 0;
        public word CoalGoldsmelter { get; set; } = 0;
        public word CoalWeaponsmith { get; set; } = 0;
        public word WheatPigfarm { get; set; } = 0;
        public word WheatMill { get; set; } = 0;

        [Ignore]
        public bool SendStrongest
        {
            get => Flags.HasFlag(PlayerSettingFlags.SendStrongest);
            set => Flags |= PlayerSettingFlags.SendStrongest;
        }
    }
}
