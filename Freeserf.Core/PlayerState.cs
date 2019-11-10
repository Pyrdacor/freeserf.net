/*
 * PlayerState.cs - Player related functions
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
    using MapPos = UInt32;

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
        None =                                  0x00,
        /// <summary>
        /// The player has built a castle
        /// </summary>
        HasCastle =                             0x01,
        /// <summary>
        /// The player is an AI
        /// </summary>
        IsAI =                                  0x02,
        /// <summary>
        /// The emergency program is active
        /// </summary>
        EmergencyProgramActive =                0x04,
        /// <summary>
        /// The emergency program was deactivated once before
        /// </summary>
        EmergencyProgramWasDeactivatedOnce =    0x08,
        /// <summary>
        /// Can spawn serfs (has castle or inventory)
        /// </summary>
        CanSpawn =                              0x10,
        /// <summary>
        /// The cycling of knights is active
        /// (swap weak ones with good ones)
        /// </summary>
        CyclingKnightsInProgress =              0x20,
        /// <summary>
        /// The knight level is reduced due
        /// to knight cycling. Is set to off
        /// when the second phase starts.
        /// </summary>
        CyclingKnightsReducedLevel =            0x40,
        /// <summary>
        /// This will be set later when new
        /// knights are sent out to the military
        /// buildings while cycling knights.
        /// </summary>
        CyclingKnightsSecondPhase =             0x80
    }

    [DataClass]
    public class PlayerState : IData
    {
        [Ignore]
        public bool Dirty
        {
            get;
            internal set;
        }

        public PlayerStateFlags Flags { get; set; } = PlayerStateFlags.None;
        public PlayerFace Face { get; set; } = PlayerFace.None;
        public Color Color { get; set; } = new Color();
        public MapPos CastlePosition { get; set; } = Constants.INVALID_MAPPOS;
        public dword CastleInventoryIndex { get; set; } = 0;
        public sbyte CastleScore { get; set; } = 0;
        public byte CastleKnights { get; set; } = 0;
        public dword KnightMorale { get; set; } = 0;
        public dword GoldDeposited { get; set; } = 0;
        public byte InitialSupplies { get; set; } = 0;
        public byte Intelligence { get; set; } = 0; // only for AI, otherwise ignored (maybe later for human player penalties?)
        public word ReproductionCounter { get; set; } = 0;
        public word ReproductionReset { get; set; } = 0;
        public word SerfToKnightCounter { get; set; } = 0;
        public word KnightCycleCounter { get; set; } = 0;

        public dword TotalLandArea { get; set; } = 0;
        public dword TotalBuildingScore { get; set; } = 0;
        public dword TotalMilitaryScore { get; set; } = 0;

        public dword[] SerfCounts { get; set; } = new dword[Constants.NUM_SERF_TYPES];
        public dword[] ResourceCounts { get; set; } = new dword[Constants.NUM_RESOURCE_TYPES];
        public dword[] CompletedBuildingCount { get; set; } = new uint[Constants.NUM_BUILDING_TYPES];
        public dword[] IncompleteBuildingCount { get; set; } = new uint[Constants.NUM_BUILDING_TYPES];
        public dword MilitaryMaxGold { get; set; } = 0;        

        [Ignore]
        public bool HasCastle
        {
            get => Flags.HasFlag(PlayerStateFlags.HasCastle);
            set => Flags |= PlayerStateFlags.HasCastle;
        }
        [Ignore]
        public bool IsAI
        {
            get => Flags.HasFlag(PlayerStateFlags.IsAI);
            set => Flags |= PlayerStateFlags.IsAI;
        }
        [Ignore]
        public bool EmergencyProgramActive
        {
            get => Flags.HasFlag(PlayerStateFlags.EmergencyProgramActive);
            set => Flags |= PlayerStateFlags.EmergencyProgramActive;
        }
        [Ignore]
        public bool EmergencyProgramWasDeactivatedOnce
        {
            get => Flags.HasFlag(PlayerStateFlags.EmergencyProgramWasDeactivatedOnce);
            set => Flags |= PlayerStateFlags.EmergencyProgramWasDeactivatedOnce;
        }
        [Ignore]
        public bool CanSpawn
        {
            get => Flags.HasFlag(PlayerStateFlags.CanSpawn);
            set => Flags |= PlayerStateFlags.CanSpawn;
        }
        [Ignore]
        public bool CyclingKnightsInProgress
        {
            get => Flags.HasFlag(PlayerStateFlags.CyclingKnightsInProgress);
            set => Flags |= PlayerStateFlags.CyclingKnightsInProgress;
        }
        [Ignore]
        public bool CyclingKnightsReducedLevel
        {
            get => Flags.HasFlag(PlayerStateFlags.CyclingKnightsReducedLevel);
            set => Flags |= PlayerStateFlags.CyclingKnightsReducedLevel;
        }
        [Ignore]
        public bool CyclingKnightsSecondPhase
        {
            get => Flags.HasFlag(PlayerStateFlags.CyclingKnightsSecondPhase);
            set => Flags |= PlayerStateFlags.CyclingKnightsSecondPhase;
        }
    }
}
