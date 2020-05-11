/*
 * PlayerSettings.cs - Player game-related settings
 *
 * Copyright (C) 2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net. freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General private License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General private License for more details.
 *
 * You should have received a copy of the GNU General private License
 * along with freeserf.net. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Freeserf
{
    using Serialize;
    using word = UInt16;

    [Flags]
    internal enum PlayerSettingFlags : byte
    {
        None = 0x00,
        /// <summary>
        /// Send strongest knights to fights
        /// </summary>
        SendStrongest = 0x01,
        /// <summary>
        /// The cycling of knights is active
        /// (swap weak ones with good ones)
        /// </summary>
        CyclingKnightsInProgress = 0x02,
        /// <summary>
        /// The knight level is reduced due
        /// to knight cycling. Is set to off
        /// when the second phase starts.
        /// </summary>
        CyclingKnightsReducedLevel = 0x04,
        /// <summary>
        /// This will be set later when new
        /// knights are sent out to the military
        /// buildings while cycling knights.
        /// </summary>
        CyclingKnightsSecondPhase = 0x08
    }

    [DataClass]
    internal class PlayerSettings : State
    {
        private PlayerSettingFlags flags = PlayerSettingFlags.None;
        private DirtyArray<word> toolPriorities = new DirtyArray<word>(Global.NUM_TOOL_TYPES);
        private DirtyArray<byte> flagPriorities = new DirtyArray<byte>(Global.NUM_RESOURCE_TYPES);
        private DirtyArray<byte> inventoryPriorities = new DirtyArray<byte>(Global.NUM_RESOURCE_TYPES);
        private DirtyArray<byte> knightOccupation = new DirtyArray<byte>(Global.NUM_TREATMENT_LEVEL_TYPES);
        private word serfToKnightRate = 0;
        private byte castleKnightsWanted = 3;
        private word foodStonemine = 0;
        private word foodCoalmine = 0;
        private word foodIronmine = 0;
        private word foodGoldmine = 0;
        private word planksConstruction = 0;
        private word planksBoatbuilder = 0;
        private word planksToolmaker = 0;
        private word steelToolmaker = 0;
        private word steelWeaponsmith = 0;
        private word coalSteelsmelter = 0;
        private word coalGoldsmelter = 0;
        private word coalWeaponsmith = 0;
        private word wheatPigfarm = 0;
        private word wheatMill = 0;

        public PlayerSettings()
        {
            toolPriorities.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(ToolPriorities)); };
            flagPriorities.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(FlagPriorities)); };
            inventoryPriorities.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(InventoryPriorities)); };
            knightOccupation.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(KnightOccupation)); };
        }

        public override void ResetDirtyFlag()
        {
            lock (dirtyLock)
            {
                toolPriorities.Dirty = false;
                flagPriorities.Dirty = false;
                inventoryPriorities.Dirty = false;
                knightOccupation.Dirty = false;

                ResetDirtyFlagUnlocked();
            }
        }

        public PlayerSettingFlags Flags
        {
            get => flags;
            set
            {
                if (flags != value)
                {
                    flags = value;
                    MarkPropertyAsDirty(nameof(Flags));
                }
            }
        }
        /// <summary>
        /// 0 = minimum (0%), 65535 = maximum (100%)
        /// </summary>
        public DirtyArray<word> ToolPriorities => toolPriorities;
        public DirtyArray<byte> FlagPriorities => flagPriorities;
        public DirtyArray<byte> InventoryPriorities => inventoryPriorities;
        /// <summary>
        /// Lower 4 bits = min level, higher 4 bits = max level
        /// </summary>
        public DirtyArray<byte> KnightOccupation => knightOccupation;
        public word SerfToKnightRate
        {
            get => serfToKnightRate;
            set
            {
                if (serfToKnightRate != value)
                {
                    serfToKnightRate = value;
                    MarkPropertyAsDirty(nameof(SerfToKnightRate));
                }
            }
        }
        // +1 for every castle defeated,
        // -1 for own castle lost.
        public byte CastleKnightsWanted
        {
            get => castleKnightsWanted;
            set
            {
                if (castleKnightsWanted != value)
                {
                    castleKnightsWanted = value;
                    MarkPropertyAsDirty(nameof(CastleKnightsWanted));
                }
            }
        }
        // Food delivery priority of food for mines.
        public word FoodStonemine
        {
            get => foodStonemine;
            set
            {
                if (foodStonemine != value)
                {
                    foodStonemine = value;
                    MarkPropertyAsDirty(nameof(FoodStonemine));
                }
            }
        }
        public word FoodCoalmine
        {
            get => foodCoalmine;
            set
            {
                if (foodCoalmine != value)
                {
                    foodCoalmine = value;
                    MarkPropertyAsDirty(nameof(FoodCoalmine));
                }
            }
        }
        public word FoodIronmine
        {
            get => foodIronmine;
            set
            {
                if (foodIronmine != value)
                {
                    foodIronmine = value;
                    MarkPropertyAsDirty(nameof(FoodIronmine));
                }
            }
        }
        public word FoodGoldmine
        {
            get => foodGoldmine;
            set
            {
                if (foodGoldmine != value)
                {
                    foodGoldmine = value;
                    MarkPropertyAsDirty(nameof(FoodGoldmine));
                }
            }
        }
        // Planks delivery priority.
        public word PlanksConstruction
        {
            get => planksConstruction;
            set
            {
                if (planksConstruction != value)
                {
                    planksConstruction = value;
                    MarkPropertyAsDirty(nameof(PlanksConstruction));
                }
            }
        }
        public word PlanksBoatbuilder
        {
            get => planksBoatbuilder;
            set
            {
                if (planksBoatbuilder != value)
                {
                    planksBoatbuilder = value;
                    MarkPropertyAsDirty(nameof(PlanksBoatbuilder));
                }
            }
        }
        public word PlanksToolmaker
        {
            get => planksToolmaker;
            set
            {
                if (planksToolmaker != value)
                {
                    planksToolmaker = value;
                    MarkPropertyAsDirty(nameof(PlanksToolmaker));
                }
            }
        }
        public word SteelToolmaker
        {
            get => steelToolmaker;
            set
            {
                if (steelToolmaker != value)
                {
                    steelToolmaker = value;
                    MarkPropertyAsDirty(nameof(SteelToolmaker));
                }
            }
        }
        public word SteelWeaponsmith
        {
            get => steelWeaponsmith;
            set
            {
                if (steelWeaponsmith != value)
                {
                    steelWeaponsmith = value;
                    MarkPropertyAsDirty(nameof(SteelWeaponsmith));
                }
            }
        }
        public word CoalSteelsmelter
        {
            get => coalSteelsmelter;
            set
            {
                if (coalSteelsmelter != value)
                {
                    coalSteelsmelter = value;
                    MarkPropertyAsDirty(nameof(CoalSteelsmelter));
                }
            }
        }
        public word CoalGoldsmelter
        {
            get => coalGoldsmelter;
            set
            {
                if (coalGoldsmelter != value)
                {
                    coalGoldsmelter = value;
                    MarkPropertyAsDirty(nameof(CoalGoldsmelter));
                }
            }
        }
        public word CoalWeaponsmith
        {
            get => coalWeaponsmith;
            set
            {
                if (coalWeaponsmith != value)
                {
                    coalWeaponsmith = value;
                    MarkPropertyAsDirty(nameof(CoalWeaponsmith));
                }
            }
        }
        public word WheatPigfarm
        {
            get => wheatPigfarm;
            set
            {
                if (wheatPigfarm != value)
                {
                    wheatPigfarm = value;
                    MarkPropertyAsDirty(nameof(WheatPigfarm));
                }
            }
        }
        public word WheatMill
        {
            get => wheatMill;
            set
            {
                if (wheatMill != value)
                {
                    wheatMill = value;
                    MarkPropertyAsDirty(nameof(WheatMill));
                }
            }
        }

        [Ignore]
        public bool SendStrongest
        {
            get => Flags.HasFlag(PlayerSettingFlags.SendStrongest);
            set
            {
                if (value)
                    Flags |= PlayerSettingFlags.SendStrongest;
                else
                    Flags &= ~PlayerSettingFlags.SendStrongest;
            }
        }
    }
}
