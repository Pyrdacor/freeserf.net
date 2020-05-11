/*
 * PlayerState.cs - Player state
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
    using dword = UInt32;
    using MapPos = UInt32;
    using word = UInt16;

    public enum PlayerFace : byte
    {
        None = 0,
        LadyAmalie,
        KumpyOnefinger,
        Balduin,
        Frollin,
        Kallina,
        Rasparuk,
        CountAldaba,
        KingRolph,
        HomenDoublehorn,
        Sollok,
        Enemy,
        You,
        Friend
    }

    public static class PlayerFaceExtensions
    {
        public static bool IsHuman(this PlayerFace face)
        {
            return face == PlayerFace.You || face == PlayerFace.Friend;
        }
    }

    [Flags]
    public enum PlayerStateFlags : byte
    {
        None = 0x00,
        /// <summary>
        /// The player has built a castle
        /// </summary>
        HasCastle = 0x01,
        /// <summary>
        /// The player is an AI
        /// </summary>
        IsAI = 0x02,
        /// <summary>
        /// The emergency program is active
        /// </summary>
        EmergencyProgramActive = 0x04,
        /// <summary>
        /// The emergency program was deactivated once before
        /// </summary>
        EmergencyProgramWasDeactivatedOnce = 0x08,
        /// <summary>
        /// Can spawn serfs (has castle or inventory)
        /// </summary>
        CanSpawn = 0x10,
        /// <summary>
        /// The cycling of knights is active
        /// (swap weak ones with good ones)
        /// </summary>
        CyclingKnightsInProgress = 0x20,
        /// <summary>
        /// The knight level is reduced due
        /// to knight cycling. Is set to off
        /// when the second phase starts.
        /// </summary>
        CyclingKnightsReducedLevel = 0x40,
        /// <summary>
        /// This will be set later when new
        /// knights are sent out to the military
        /// buildings while cycling knights.
        /// </summary>
        CyclingKnightsSecondPhase = 0x80
    }

    [DataClass]
    internal class PlayerState : State
    {
        private PlayerStateFlags flags = PlayerStateFlags.None;
        private PlayerFace face = PlayerFace.None;
        private Color color = new Color();
        private MapPos castlePosition = Global.INVALID_MAPPOS;
        private dword castleInventoryIndex = 0;
        private sbyte castleScore = 0;
        private byte castleKnights = 0;
        private dword knightMorale = 0;
        private dword goldDeposited = 0;
        private byte initialSupplies = 0;
        private byte intelligence = 0; // only for AI, otherwise ignored (maybe later for human player penalties?)
        private int reproductionCounter = 0;
        private word reproductionReset = 0;
        private int serfToKnightCounter = 0;
        private int knightCycleCounter = 0;
        private dword totalLandArea = 0;
        private dword totalBuildingScore = 0;
        private dword totalMilitaryScore = 0;
        private readonly DirtyArray<dword> serfCounts = new DirtyArray<dword>(Global.NUM_SERF_TYPES);
        private readonly DirtyArray<dword> resourceCounts = new DirtyArray<dword>(Global.NUM_RESOURCE_TYPES);
        private readonly DirtyArray<dword> completedBuildingCount = new DirtyArray<dword>(Global.NUM_BUILDING_TYPES);
        private readonly DirtyArray<dword> incompleteBuildingCount = new DirtyArray<dword>(Global.NUM_BUILDING_TYPES);
        private dword militaryMaxGold = 0;

        public PlayerState()
        {
            serfCounts.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(SerfCounts)); };
            resourceCounts.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(ResourceCounts)); };
            completedBuildingCount.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(CompletedBuildingCount)); };
            incompleteBuildingCount.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(IncompleteBuildingCount)); };
        }

        public override void ResetDirtyFlag()
        {
            lock (dirtyLock)
            {
                serfCounts.Dirty = false;
                resourceCounts.Dirty = false;
                completedBuildingCount.Dirty = false;
                incompleteBuildingCount.Dirty = false;

                ResetDirtyFlagUnlocked();
            }
        }

        public PlayerStateFlags Flags
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
        public PlayerFace Face
        {
            get => face;
            set
            {
                if (face != value)
                {
                    face = value;
                    MarkPropertyAsDirty(nameof(Face));
                }
            }
        }
        public Color Color
        {
            get => color;
            set
            {
                if (color != value)
                {
                    color = value;
                    MarkPropertyAsDirty(nameof(Color));
                }
            }
        }
        public MapPos CastlePosition
        {
            get => castlePosition;
            set
            {
                if (castlePosition != value)
                {
                    castlePosition = value;
                    MarkPropertyAsDirty(nameof(CastlePosition));
                }
            }
        }
        public dword CastleInventoryIndex
        {
            get => castleInventoryIndex;
            set
            {
                if (castleInventoryIndex != value)
                {
                    castleInventoryIndex = value;
                    MarkPropertyAsDirty(nameof(CastleInventoryIndex));
                }
            }
        }
        public sbyte CastleScore
        {
            get => castleScore;
            set
            {
                if (castleScore != value)
                {
                    castleScore = value;
                    MarkPropertyAsDirty(nameof(CastleScore));
                }
            }
        }
        public byte CastleKnights
        {
            get => castleKnights;
            set
            {
                if (castleKnights != value)
                {
                    castleKnights = value;
                    MarkPropertyAsDirty(nameof(CastleKnights));
                }
            }
        }
        public dword KnightMorale
        {
            get => knightMorale;
            set
            {
                if (knightMorale != value)
                {
                    knightMorale = value;
                    MarkPropertyAsDirty(nameof(KnightMorale));
                }
            }
        }
        public dword GoldDeposited
        {
            get => goldDeposited;
            set
            {
                if (goldDeposited != value)
                {
                    goldDeposited = value;
                    MarkPropertyAsDirty(nameof(GoldDeposited));
                }
            }
        }
        public byte InitialSupplies
        {
            get => initialSupplies;
            set
            {
                if (initialSupplies != value)
                {
                    initialSupplies = value;
                    MarkPropertyAsDirty(nameof(InitialSupplies));
                }
            }
        }
        public byte Intelligence
        {
            get => intelligence;
            set
            {
                if (intelligence != value)
                {
                    intelligence = value;
                    MarkPropertyAsDirty(nameof(Intelligence));
                }
            }
        }
        public int ReproductionCounter
        {
            get => reproductionCounter;
            set
            {
                if (reproductionCounter != value)
                {
                    reproductionCounter = value;
                    MarkPropertyAsDirty(nameof(ReproductionCounter));
                }
            }
        }
        public word ReproductionReset
        {
            get => reproductionReset;
            set
            {
                if (reproductionReset != value)
                {
                    reproductionReset = value;
                    MarkPropertyAsDirty(nameof(ReproductionReset));
                }
            }
        }
        public int SerfToKnightCounter
        {
            get => serfToKnightCounter;
            set
            {
                if (serfToKnightCounter != value)
                {
                    serfToKnightCounter = value;
                    MarkPropertyAsDirty(nameof(SerfToKnightCounter));
                }
            }
        }
        public int KnightCycleCounter
        {
            get => knightCycleCounter;
            set
            {
                if (knightCycleCounter != value)
                {
                    knightCycleCounter = value;
                    MarkPropertyAsDirty(nameof(KnightCycleCounter));
                }
            }
        }
        public dword TotalLandArea
        {
            get => totalLandArea;
            set
            {
                if (totalLandArea != value)
                {
                    totalLandArea = value;
                    MarkPropertyAsDirty(nameof(TotalLandArea));
                }
            }
        }
        public dword TotalBuildingScore
        {
            get => totalBuildingScore;
            set
            {
                if (totalBuildingScore != value)
                {
                    totalBuildingScore = value;
                    MarkPropertyAsDirty(nameof(TotalBuildingScore));
                }
            }
        }
        public dword TotalMilitaryScore
        {
            get => totalMilitaryScore;
            set
            {
                if (totalMilitaryScore != value)
                {
                    totalMilitaryScore = value;
                    MarkPropertyAsDirty(nameof(TotalMilitaryScore));
                }
            }
        }
        public DirtyArray<dword> SerfCounts => serfCounts;
        public DirtyArray<dword> ResourceCounts => resourceCounts;
        public DirtyArray<dword> CompletedBuildingCount => completedBuildingCount;
        public DirtyArray<dword> IncompleteBuildingCount => incompleteBuildingCount;
        public dword MilitaryMaxGold
        {
            get => militaryMaxGold;
            set
            {
                if (militaryMaxGold != value)
                {
                    militaryMaxGold = value;
                    MarkPropertyAsDirty(nameof(MilitaryMaxGold));
                }
            }
        }

        [Ignore]
        public bool HasCastle
        {
            get => Flags.HasFlag(PlayerStateFlags.HasCastle);
            set
            {
                if (value)
                    Flags |= PlayerStateFlags.HasCastle;
                else
                    Flags &= ~PlayerStateFlags.HasCastle;
            }
        }
        [Ignore]
        public bool IsAI
        {
            get => Flags.HasFlag(PlayerStateFlags.IsAI);
            set
            {
                if (value)
                    Flags |= PlayerStateFlags.IsAI;
                else
                    Flags &= ~PlayerStateFlags.IsAI;
            }
        }
        [Ignore]
        public bool EmergencyProgramActive
        {
            get => Flags.HasFlag(PlayerStateFlags.EmergencyProgramActive);
            set
            {
                if (value)
                    Flags |= PlayerStateFlags.EmergencyProgramActive;
                else
                    Flags &= ~PlayerStateFlags.EmergencyProgramActive;
            }
        }
        [Ignore]
        public bool EmergencyProgramWasDeactivatedOnce
        {
            get => Flags.HasFlag(PlayerStateFlags.EmergencyProgramWasDeactivatedOnce);
            set
            {
                if (value)
                    Flags |= PlayerStateFlags.EmergencyProgramWasDeactivatedOnce;
                else
                    Flags &= ~PlayerStateFlags.EmergencyProgramWasDeactivatedOnce;
            }
        }
        [Ignore]
        public bool CanSpawn
        {
            get => Flags.HasFlag(PlayerStateFlags.CanSpawn);
            set
            {
                if (value)
                    Flags |= PlayerStateFlags.CanSpawn;
                else
                    Flags &= ~PlayerStateFlags.CanSpawn;
            }
        }
        [Ignore]
        public bool CyclingKnightsInProgress
        {
            get => Flags.HasFlag(PlayerStateFlags.CyclingKnightsInProgress);
            set
            {
                if (value)
                    Flags |= PlayerStateFlags.CyclingKnightsInProgress;
                else
                    Flags &= ~PlayerStateFlags.CyclingKnightsInProgress;
            }
        }
        [Ignore]
        public bool CyclingKnightsReducedLevel
        {
            get => Flags.HasFlag(PlayerStateFlags.CyclingKnightsReducedLevel);
            set
            {
                if (value)
                    Flags |= PlayerStateFlags.CyclingKnightsReducedLevel;
                else
                    Flags &= ~PlayerStateFlags.CyclingKnightsReducedLevel;
            }
        }
        [Ignore]
        public bool CyclingKnightsSecondPhase
        {
            get => Flags.HasFlag(PlayerStateFlags.CyclingKnightsSecondPhase);
            set
            {
                if (value)
                    Flags |= PlayerStateFlags.CyclingKnightsSecondPhase;
                else
                    Flags &= ~PlayerStateFlags.CyclingKnightsSecondPhase;
            }
        }
    }
}
