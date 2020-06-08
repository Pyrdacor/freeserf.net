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
        Friend,
        YouRed,
        YouMagenta,
        YouYellow,
        FriendBlue,
        FriendMagenta,
        FriendYellow
    }

    public static class PlayerFaceExtensions
    {
        public static bool IsHuman(this PlayerFace face)
        {
            return face == PlayerFace.You || face == PlayerFace.Friend;
        }

        public static uint GetGraphicIndex(this PlayerFace face)
        {
            if (face == PlayerFace.None)
                return 281u;

            if (face <= PlayerFace.Friend)
                return 267u + (uint)face;

            return 600u + (uint)face - (uint)PlayerFace.YouRed;
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
        private dword militaryMaxGold = 0;

        public PlayerState()
        {
            SerfCounts.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(SerfCounts)); };
            ResourceCounts.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(ResourceCounts)); };
            CompletedBuildingCount.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(CompletedBuildingCount)); };
            IncompleteBuildingCount.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(IncompleteBuildingCount)); };
        }

        public override void ResetDirtyFlag()
        {
            lock (dirtyLock)
            {
                SerfCounts.ResetDirtyFlag();
                ResourceCounts.ResetDirtyFlag();
                CompletedBuildingCount.ResetDirtyFlag();
                IncompleteBuildingCount.ResetDirtyFlag();

                ResetDirtyFlagUnlocked();
            }
        }

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
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

        [Data]
        public DirtyArrayWithEnumIndex<Serf.Type, dword> SerfCounts { get; } = new DirtyArrayWithEnumIndex<Serf.Type, dword>(Global.NUM_SERF_TYPES);
        [Data]
        public DirtyArrayWithEnumIndex<Resource.Type, dword> ResourceCounts { get; } = new DirtyArrayWithEnumIndex<Resource.Type, dword>(Global.NUM_RESOURCE_TYPES);
        [Data]
        public DirtyArrayWithEnumIndex<Building.Type, dword> CompletedBuildingCount { get; } = new DirtyArrayWithEnumIndex<Building.Type, dword>(Global.NUM_BUILDING_TYPES);
        [Data]
        public DirtyArrayWithEnumIndex<Building.Type, dword> IncompleteBuildingCount { get; } = new DirtyArrayWithEnumIndex<Building.Type, dword>(Global.NUM_BUILDING_TYPES);

        [Data]
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
