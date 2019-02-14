/*
 * Serf.cs - Serf related functions
 *
 * Copyright (C) 2013  Jon Lund Steffensen <jonlst@gmail.com>
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Freeserf
{
    using MapPos = UInt32;

    // TODO: Can't we just replace things like Game.GetFlag(map.GetObjectIndex(Position)) with Game.GetFlagAtPos(Position) ???
    // TODO: Give the state values plausible names instead of FieldX and so on!

    public class Serf : GameObject
    {
        public enum Type
        {
            None = -1,
            Transporter = 0,
            Sailor,
            Digger,
            Builder,
            TransporterInventory,
            Lumberjack,
            Sawmiller,
            Stonecutter,
            Forester,
            Miner,
            Smelter,
            Fisher,
            PigFarmer,
            Butcher,
            Farmer,
            Miller,
            Baker,
            BoatBuilder,
            Toolmaker,
            WeaponSmith,
            Geologist,
            Generic,
            Knight0,
            Knight1,
            Knight2,
            Knight3,
            Knight4,
            Dead
        }

        /* The term FREE is used loosely in the following
         names to denote a state where the serf is not
         bound to a road or a flag. */
        public enum State
        {
            Invalid = -1,

            Null = 0,
            IdleInStock,
            Walking,
            Transporting,
            EnteringBuilding,
            LeavingBuilding, /* 5 */
            ReadyToEnter,
            ReadyToLeave,
            Digging,
            Building,
            BuildingCastle, /* 10 */
            MoveResourceOut,
            WaitForResourceOut,
            DropResourceOut,
            Delivering,
            ReadyToLeaveInventory, /* 15 */
            FreeWalking,
            Logging,
            PlanningLogging,
            PlanningPlanting,
            Planting, /* 20 */
            PlanningStoneCutting,
            StoneCutterFreeWalking,
            StoneCutting,
            Sawing,
            Lost, /* 25 */
            LostSailor,
            FreeSailing,
            EscapeBuilding,
            Mining,
            Smelting, /* 30 */
            PlanningFishing,
            Fishing,
            PlanningFarming,
            Farming,
            Milling, /* 35 */
            Baking,
            PigFarming,
            Butchering,
            MakingWeapon,
            MakingTool, /* 40 */
            BuildingBoat,
            LookingForGeoSpot,
            SamplingGeoSpot,
            KnightEngagingBuilding,
            KnightPrepareAttacking, /* 45 */
            KnightLeaveForFight,
            KnightPrepareDefending,
            KnightAttacking,
            KnightDefending,
            KnightAttackingVictory, /* 50 */
            KnightAttackingDefeat,
            KnightOccupyEnemyBuilding,
            KnightFreeWalking,
            KnightEngageDefendingFree,
            KnightEngageAttackingFree, /* 55 */
            KnightEngageAttackingFreeJoin,
            KnightPrepareAttackingFree,
            KnightPrepareDefendingFree,
            KnightPrepareDefendingFreeWait,
            KnightAttackingFree, /* 60 */
            KnightDefendingFree,
            KnightAttackingVictoryFree,
            KnightDefendingVictoryFree,
            KnightAttackingFreeWait,
            KnightLeaveForWalkToFight, /* 65 */
            IdleOnPath,
            WaitIdleOnPath,
            WakeAtFlag,
            WakeOnPath,
            DefendingHut, /* 70 */
            DefendingTower,
            DefendingFortress,
            Scatter,
            FinishedBuilding,
            DefendingCastle, /* 75 */

            /* Additional state: goes at the end to ease loading of
             original save game. */
            KnightAttackingDefeatFree
        }

        bool sound;
        ushort tick;
        Type type;

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public struct StateInfo
        {
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SIdleInStock
            {
                public uint InvIndex; /* E */
            }
            [FieldOffset(0)]
            public SIdleInStock IdleInStock;

            /* States: walking, transporting, delivering */
            /* res: resource carried (when transporting), otherwise direction. */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SWalking
            {
                public int Dir1; /* newly added */
                public Resource.Type Res; /* B */
                public uint Dest; /* C */
                public int Dir; /* E */
                public int WaitCounter; /* F */
            }
            [FieldOffset(0)]
            public SWalking Walking;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SEnteringBuilding
            {
                // FieldB = -2: Enter inventory (castle, etc)
                public int FieldB; /* B */
                public int SlopeLength; /* C */
            }
            [FieldOffset(0)]
            public SEnteringBuilding EnteringBuilding;

            /* States: leavingBuilding, readyToLeave,
               leaveForFight */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SLeavingBuilding
            {
                public int FieldB; /* B */
                public uint Dest; /* C */
                public int Dest2; /* D */
                public int Dir; /* E */
                public State NextState; /* F */
            }
            [FieldOffset(0)]
            public SLeavingBuilding LeavingBuilding;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SReadyToEnter
            {
                public int FieldB; /* B */
            }
            [FieldOffset(0)]
            public SReadyToEnter ReadyToEnter;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SDigging
            {
                // Substate < 0 -> Wait for serf
                // Substate = 0 -> Looking for a place to dig
                // Substate = 1 -> Change height and go back to center (last step of digging)
                // Substate > 1 -> Digging
                // TargetH is the height after digging
                // DigPos is the dig position (0 = center, 1-6 = directions)
                public int HIndex; /* B */
                public uint TargetH; /* C */
                public int DigPos; /* D */
                public int Substate; /* E */
            }
            [FieldOffset(0)]
            public SDigging Digging;

            /* mode: one of three substates (negative, positive, zero).
               Index: index of building. */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SBuilding
            {
                public int Mode; /* B */
                public uint Index; /* C */
                public uint MaterialStep; /* E */
                public uint Counter; /* F */
            }
            [FieldOffset(0)]
            public SBuilding Building;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SBuildingCastle
            {
                public uint InvIndex; /* C */
            }
            [FieldOffset(0)]
            public SBuildingCastle BuildingCastle;

            /* States: MoveResourceOut, dropResourceOut */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SMoveResourceOut
            {
                public uint Res; /* B */
                public uint ResDest; /* C */
                public State NextState; /* F */
            }
            [FieldOffset(0)]
            public SMoveResourceOut MoveResourceOut;

            /* No state: waitForResourceOut */

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SReadyToLeaveInventory
            {
                public int Mode; /* B */
                public uint Dest; /* C */
                public uint InvIndex; /* E */
            }
            [FieldOffset(0)]
            public SReadyToLeaveInventory ReadyToLeaveInventory;

            /* States: freeWalking, logging,
               planting, stonecutting, fishing,
               farming, samplingGeoSpot,
               knightFreeWalking,
               knightAttackingFree,
               knightAttackingFreeWait */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SFreeWalking
            {
                public int Dist1; /* B */
                public int Dist2; /* C */
                public int NegDist1; /* D */
                public int NegDist2; /* E */
                public int Flags; /* F */
            }
            [FieldOffset(0)]
            public SFreeWalking FreeWalking;

            /* No state data: planningLogging,
               planningPlanting, planningStonecutting */

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SSawing
            {
                public int Mode; /* B */
            }
            [FieldOffset(0)]
            public SSawing Sawing;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SLost
            {
                public int FieldB; /* B */
            }
            [FieldOffset(0)]
            public SLost Lost;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SMining
            {
                public uint Substate; /* B */
                public uint Res; /* D */
                public Map.Minerals Deposit; /* E */
            }
            [FieldOffset(0)]
            public SMining Mining;

            /* type: Type of smelter (0 is steel, else gold). */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SSmelting
            {
                public int Mode; /* B */
                public int Counter; /* C */
                public int Type; /* D */
            }
            [FieldOffset(0)]
            public SSmelting Smelting;

            /* No state data: planningFishing,
               planningFarming */

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SMilling
            {
                public int Mode; /* B */
            }
            [FieldOffset(0)]
            public SMilling Milling;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SBaking
            {
                public int Mode; /* B */
            }
            [FieldOffset(0)]
            public SBaking Baking;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SPigFarming
            {
                public int Mode; /* B */
            }
            [FieldOffset(0)]
            public SPigFarming PigFarming;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SButchering
            {
                public int Mode; /* B */
            }
            [FieldOffset(0)]
            public SButchering Butchering;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SMakingWeapon
            {
                public int Mode; /* B */
            }
            [FieldOffset(0)]
            public SMakingWeapon MakingWeapon;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SMakingTool
            {
                public int Mode; /* B */
            }
            [FieldOffset(0)]
            public SMakingTool MakingTool;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SBuildingBoat
            {
                public int Mode; /* B */
            }
            [FieldOffset(0)]
            public SBuildingBoat BuildingBoat;

            /* No state data: lookingForGeoSpot */

            /* States: knightEngagingBuilding,
               knightPrepareAttacking,
               knightPrepareDefendingFreeWait,
               knightAttackingDefeatFree,
               knightAttacking,
               knightAttackingVictory,
               knightEngageAttackingFree,
               knightEngageAttackingFreeJoin
            */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SAttacking
            {
                public int Move; /* B */
                public int AttackerWon; /* C */
                public int FieldD; /* D */
                public int DefIndex; /* E */
            }
            [FieldOffset(0)]
            public SAttacking Attacking;

            /* States: knightAttackingVictoryFree
            */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SAttackingVictoryFree
            {
                public int Move; /* B */
                public int DistColumn; /* C */
                public int DistRow; /* D */
                public int DefIndex; /* E */
            }
            [FieldOffset(0)]
            public SAttackingVictoryFree AttackingVictoryFree;

            /* States: knightDefendingFree,
               knightEngageDefendingFree */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SDefendingFree
            {
                public int DistColumn; /* B */
                public int DistRow; /* C */
                public int FieldD; /* D */
                public int OtherDistColumn; /* E */
                public int OtherDistRow; /* F */
            }
            [FieldOffset(0)]
            public SDefendingFree DefendingFree;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SLeaveForWalkToFight
            {
                public int DistColumn; /* B */
                public int DistRow; /* C */
                public int FieldD; /* D */
                public int FieldE; /* E */
                public State NextState; /* F */
            }
            [FieldOffset(0)]
            public SLeaveForWalkToFight LeaveForWalkToFight;

            /* States: idleOnPath, waitIdleOnPath,
               wakeAtFlag, wakeOnPath. */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SIdleOnPath
            {
                // NOTE: Flag was a Flag* before! Now it is the index of it.
                public uint FlagIndex; /* C */
                public int FieldE; /* E */
                public Direction RevDir; /* B */
            }
            [FieldOffset(0)]
            public SIdleOnPath IdleOnPath;

            /* No state data: finishedBuilding */

            /* States: defendingHut, defendingTower,
               defendingFortress, defendingCastle */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SDefending
            {
                public uint NextKnight; /* E */
            }
            [FieldOffset(0)]
            public SDefending Defending;
        }

        StateInfo s;

        static readonly int[] CounterFromAnimation = new int[]
        {
            /* Walking (0-80) */
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,

            /* Waiting (81-86) */
            127, 127, 127, 127, 127, 127,

            /* Digging (87-88) */
            383, 383,

            255, 223, 191, 159, 127, 159, 255, 383,  511,

            /* Building (98) */
            255,

            /* Engage defending free (99) */
            255,

            /* Building large building (100) */
            255,

            0,

            /* Building (102-105) */
            767, 511, 511, 767,

            1023, 639, 639, 1023,

            /* Transporting (turning?) (110-115) */
            63, 63, 63, 63, 63, 63,

            /* Logging (116-120) */
            1023, 31, 767, 767, 255,

            /* Planting (121-122) */
            191, 127,

            /* Stonecutting (123) */
            1535,

            /* Sawing (124) */
            2367,

            /* Mining (125-128) */
            383, 303, 303, 383,

            /* Smelting (129-130) */
            383, 383,

            /* Fishing (131-134) */
            767, 767, 127, 127,

            /* Farming (135-136) */
            1471, 1983,

            /* Milling (137) */
            383,

            /* Baking (138) */
            767,

            /* Pig farming (139) */
            383,

            /* Butchering (140) */
            1535,

            /* Sampling geology (142) */
            783, 63,

            /* Making weapon (143) */
            575,

            /* Making tool (144) */
            1535,

            /* Building boat (145-146) */
            1407, 159,

            /* Attacking (147-156) */
            127, 127, 127, 127, 127, 127, 127, 127, 127, 127,

            /* Defending (157-166) */
            127, 127, 127, 127, 127, 127, 127, 127, 127, 127,

            /* Engage attacking (167) */
            191,

            /* Victory attacking (168) */
            7,

            /* Dying attacking (169-173) */
            255, 255, 255, 255, 255,

            /* Dying defending (174-178) */
            255, 255, 255, 255, 255,

            /* Occupy attacking (179) */
            127,

            /* Victory defending (180) */
            7
        };

        static readonly string[] SerfStateNames = new string[]
        {
            "NULL",  // SERF_STATE_NULL
            "IDLE IN STOCK",  // SERF_STATE_IDLE_IN_STOCK
            "WALKING",  // SERF_STATE_WALKING
            "TRANSPORTING",  // SERF_STATE_TRANSPORTING
            "ENTERING BUILDING",  // SERF_STATE_ENTERING_BUILDING
            "LEAVING BUILDING",  // SERF_STATE_LEAVING_BUILDING
            "READY TO ENTER",  // SERF_STATE_READY_TO_ENTER
            "READY TO LEAVE",  // SERF_STATE_READY_TO_LEAVE
            "DIGGING",  // SERF_STATE_DIGGING
            "BUILDING",  // SERF_STATE_BUILDING
            "BUILDING CASTLE",  // SERF_STATE_BUILDING_CASTLE
            "MOVE RESOURCE OUT",  // SERF_STATE_MOVE_RESOURCE_OUT
            "WAIT FOR RESOURCE OUT",  // SERF_STATE_WAIT_FOR_RESOURCE_OUT
            "DROP RESOURCE OUT",  // SERF_STATE_DROP_RESOURCE_OUT
            "DELIVERING",  // SERF_STATE_DELIVERING
            "READY TO LEAVE INVENTORY",  // SERF_STATE_READY_TO_LEAVE_INVENTORY
            "FREE WALKING",  // SERF_STATE_FREE_WALKING
            "LOGGING",  // SERF_STATE_LOGGING
            "PLANNING LOGGING",  // SERF_STATE_PLANNING_LOGGING
            "PLANNING PLANTING",  // SERF_STATE_PLANNING_PLANTING
            "PLANTING",  // SERF_STATE_PLANTING
            "PLANNING STONECUTTING",  // SERF_STATE_PLANNING_STONECUTTING
            "STONECUTTER FREE WALKING",  // SERF_STATE_STONECUTTER_FREE_WALKING
            "STONECUTTING",  // SERF_STATE_STONECUTTING
            "SAWING",  // SERF_STATE_SAWING
            "LOST",  // SERF_STATE_LOST
            "LOST SAILOR",  // SERF_STATE_LOST_SAILOR
            "FREE SAILING",  // SERF_STATE_FREE_SAILING
            "ESCAPE BUILDING",  // SERF_STATE_ESCAPE_BUILDING
            "MINING",  // SERF_STATE_MINING
            "SMELTING",  // SERF_STATE_SMELTING
            "PLANNING FISHING",  // SERF_STATE_PLANNING_FISHING
            "FISHING",  // SERF_STATE_FISHING
            "PLANNING FARMING",  // SERF_STATE_PLANNING_FARMING
            "FARMING",  // SERF_STATE_FARMING
            "MILLING",  // SERF_STATE_MILLING
            "BAKING",  // SERF_STATE_BAKING
            "PIGFARMING",  // SERF_STATE_PIGFARMING
            "BUTCHERING",  // SERF_STATE_BUTCHERING
            "MAKING WEAPON",  // SERF_STATE_MAKING_WEAPON
            "MAKING TOOL",  // SERF_STATE_MAKING_TOOL
            "BUILDING BOAT",  // SERF_STATE_BUILDING_BOAT
            "LOOKING FOR GEO SPOT",  // SERF_STATE_LOOKING_FOR_GEO_SPOT
            "SAMPLING GEO SPOT",  // SERF_STATE_SAMPLING_GEO_SPOT
            "KNIGHT ENGAGING BUILDING",  // SERF_STATE_KNIGHT_ENGAGING_BUILDING
            "KNIGHT PREPARE ATTACKING",  // SERF_STATE_KNIGHT_PREPARE_ATTACKING
            "KNIGHT LEAVE FOR FIGHT",  // SERF_STATE_KNIGHT_LEAVE_FOR_FIGHT
            "KNIGHT PREPARE DEFENDING",  // SERF_STATE_KNIGHT_PREPARE_DEFENDING
            "KNIGHT ATTACKING",  // SERF_STATE_KNIGHT_ATTACKING
            "KNIGHT DEFENDING",  // SERF_STATE_KNIGHT_DEFENDING
            "KNIGHT ATTACKING VICTORY",  // SERF_STATE_KNIGHT_ATTACKING_VICTORY
            "KNIGHT ATTACKING DEFEAT",  // SERF_STATE_KNIGHT_ATTACKING_DEFEAT
            "KNIGHT OCCUPY ENEMY BUILDING",  // SERF_STATE_KNIGHT_OCCUPY_ENEMY_BUILDING
            "KNIGHT FREE WALKING",  // SERF_STATE_KNIGHT_FREE_WALKING
            "KNIGHT ENGAGE DEFENDING FREE",  // SERF_STATE_KNIGHT_ENGAGE_DEFENDING_FREE
            "KNIGHT ENGAGE ATTACKING FREE",  // SERF_STATE_KNIGHT_ENGAGE_ATTACKING_FREE
            "KNIGHT ENGAGE ATTACKING FREE JOIN",
                                            // SERF_STATE_KNIGHT_ENGAGE_ATTACKING_FREE_JOIN
            "KNIGHT PREPARE ATTACKING FREE",  // SERF_STATE_KNIGHT_PREPARE_ATTACKING_FREE
            "KNIGHT PREPARE DEFENDING FREE",  // SERF_STATE_KNIGHT_PREPARE_DEFENDING_FREE
            "KNIGHT PREPARE DEFENDING FREE WAIT",
                                        // SERF_STATE_KNIGHT_PREPARE_DEFENDING_FREE_WAIT
            "KNIGHT ATTACKING FREE",  // SERF_STATE_KNIGHT_ATTACKING_FREE
            "KNIGHT DEFENDING FREE",  // SERF_STATE_KNIGHT_DEFENDING_FREE
            "KNIGHT ATTACKING VICTORY FREE",  // SERF_STATE_KNIGHT_ATTACKING_VICTORY_FREE
            "KNIGHT DEFENDING VICTORY FREE",  // SERF_STATE_KNIGHT_DEFENDING_VICTORY_FREE
            "KNIGHT ATTACKING FREE WAIT",  // SERF_STATE_KNIGHT_ATTACKING_FREE_WAIT
            "KNIGHT LEAVE FOR WALK TO FIGHT",
                                            // SERF_STATE_KNIGHT_LEAVE_FOR_WALK_TO_FIGHT
            "IDLE ON PATH",  // SERF_STATE_IDLE_ON_PATH
            "WAIT IDLE ON PATH",  // SERF_STATE_WAIT_IDLE_ON_PATH
            "WAKE AT FLAG",  // SERF_STATE_WAKE_AT_FLAG
            "WAKE ON PATH",  // SERF_STATE_WAKE_ON_PATH
            "DEFENDING HUT",  // SERF_STATE_DEFENDING_HUT
            "DEFENDING TOWER",  // SERF_STATE_DEFENDING_TOWER
            "DEFENDING FORTRESS",  // SERF_STATE_DEFENDING_FORTRESS
            "SCATTER",  // SERF_STATE_SCATTER
            "FINISHED BUILDING",  // SERF_STATE_FINISHED_BUILDING
            "DEFENDING CASTLE",  // SERF_STATE_DEFENDING_CASTLE
            "KNIGHT ATTACKING DEFEAT FREE",  // SERF_STATE_KNIGHT_ATTACKING_DEFEAT_FREE
        };

        static readonly string[] SerfTypeNames = new string[]
        {
            "TRANSPORTER",  // SERF_TRANSPORTER = 0,
            "SAILOR",  // SERF_SAILOR,
            "DIGGER",  // SERF_DIGGER,
            "BUILDER",  // SERF_BUILDER,
            "TRANSPORTER_INVENTORY",  // SERF_TRANSPORTER_INVENTORY,
            "LUMBERJACK",  // SERF_LUMBERJACK,
            "SAWMILLER",  // TypeSawmiller,
            "STONECUTTER",  // TypeStonecutter,
            "FORESTER",  // TypeForester,
            "MINER",  // TypeMiner,
            "SMELTER",  // TypeSmelter,
            "FISHER",  // TypeFisher,
            "PIGFARMER",  // TypePigFarmer,
            "BUTCHER",  // TypeButcher,
            "FARMER",  // TypeFarmer,
            "MILLER",  // TypeMiller,
            "BAKER",  // TypeBaker,
            "BOATBUILDER",  // TypeBoatBuilder,
            "TOOLMAKER",  // TypeToolmaker,
            "WEAPONSMITH",  // TypeWeaponSmith,
            "GEOLOGIST",  // TypeGeologist,
            "GENERIC",  // TypeGeneric,
            "KNIGHT_0",  // TypeKnight0,
            "KNIGHT_1",  // TypeKnight1,
            "KNIGHT_2",  // TypeKnight2,
            "KNIGHT_3",  // TypeKnight3,
            "KNIGHT_4",  // TypeKnight4,
            "DEAD",  // TypeDead
        };

        static readonly int[] RoadBuildingSlope = new int[]
        {
            /* Finished building */
            5, 18, 18, 15, 18, 22, 22, 22,
            22, 18, 16, 18, 1, 10, 1, 15,
            15, 16, 15, 15, 10, 15, 20, 15,
            18
        };

        public Serf(Game game, uint index)
            : base(game, index)
        {
            SerfState = State.Null;
            Player = uint.MaxValue;
            type = Type.None;
            sound = false;
            Animation = 0;
            Counter = 0;
            Position = Global.BadMapPos;
            tick = 0;

            s = new StateInfo();
        }

        State serfState = State.Null;

        public uint Player { get; set; }
        public State SerfState
        {
            get => serfState;
            private set
            {
                if (serfState == value)
                    return;

                serfState = value;

                switch (serfState)
                {
                    case State.BuildingCastle:
                    case State.IdleInStock:
                    case State.DefendingCastle:
                    case State.DefendingFortress:
                    case State.DefendingHut:
                    case State.DefendingTower:
                    case State.Invalid:
                    case State.Null:
                    case State.WaitForResourceOut:
                        Game.RemoveSerfFromDrawing(this);
                        break;
                }
            }
        }
        public int Animation { get; private set; } /* Index to animation table in data file. */
        public int Counter { get; private set; }
        public MapPos Position { get; private set; }

        void SetState(State newState, [CallerMemberName] string function = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log.Verbose.Write("serf", $"serf {Index} ({SerfTypeNames[(int)type]}): state {SerfStateNames[(int)SerfState]} -> {SerfStateNames[(int)newState]} ({function}:{lineNumber})");
            SerfState = newState;
        }

        static void SetOtherState(Serf otherSerf, State newState, [CallerMemberName] string function = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log.Verbose.Write("serf", $"serf {otherSerf.Index} ({SerfTypeNames[(int)otherSerf.type]}): state {SerfStateNames[(int)otherSerf.SerfState]} -> {SerfStateNames[(int)newState]} ({function}:{lineNumber})");
            otherSerf.SerfState = newState;
        }

        public Type GetSerfType()
        {
            return type;
        }

        public void SetSerfType(Type newType)
        {
            Type oldType = GetSerfType();

            if (oldType == newType)
                return;

            type = newType;

            /* Register this type as transporter */
            if (newType == Type.TransporterInventory)
                newType = Type.Transporter;
            if (oldType == Type.TransporterInventory)
                oldType = Type.Transporter;

            Player player = Game.GetPlayer(Player);

            if (oldType != Type.None)
            {
                player.DecreaseSerfCount(oldType);
            }
            if (type != Type.Dead)
            {
                player.IncreaseSerfCount(newType);
            }

            if (oldType >= Type.Knight0 &&
                oldType <= Type.Knight4)
            {
                uint value = 1u << (oldType - Type.Knight0);
                player.DecreaseMilitaryScore(value);
            }
            if (newType >= Type.Knight0 &&
                newType <= Type.Knight4)
            {
                uint value = 1u << (type - Type.Knight0);
                player.IncreaseMilitaryScore(value);
            }
            if (newType == Type.Transporter)
            {
                Counter = 0;
            }
        }

        public bool IsKnight()
        {
            return type >= Type.Knight0 && type <= Type.Knight4;
        }

        public bool PlayingSfx()
        {
            return sound;
        }

        public void StartPlayingSfx()
        {
            sound = true;
        }

        public void StopPlayingSfx()
        {
            sound = false;
        }

        public int TrainKnight(int p)
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                if (Game.RandomInt() < p)
                {
                    /* Level up */
                    Type oldType = GetSerfType();
                    SetSerfType((Type)(oldType + 1));
                    Counter = 6000;

                    return 0;
                }

                Counter += 6000;
            }

            return -1;
        }

        /* Change serf state to lost, but make necessary clean up
           from any earlier state first. */
        public void SetLostState()
        {
            if (SerfState == State.Walking)
            {
                if (s.Walking.Dir1 >= 0)
                {
                    if (s.Walking.Dir1 != 6)
                    {
                        Direction dir = (Direction)s.Walking.Dir1;
                        Flag flag = Game.GetFlag(s.Walking.Dest);
                        flag.CancelSerfRequest(dir);

                        Direction otherDir = flag.GetOtherEndDir(dir);
                        flag.GetOtherEndFlag(dir).CancelSerfRequest(otherDir);
                    }
                }
                else if (s.Walking.Dir1 == -1)
                {
                    Flag flag = Game.GetFlag(s.Walking.Dest);
                    Building building = flag.GetBuilding();
                    building.RequestedSerfLost();
                }

                SetState(State.Lost);
                s.Lost.FieldB = 0;
            }
            else if (SerfState == State.Transporting || SerfState == State.Delivering)
            {
                if (s.Walking.Res != Resource.Type.None)
                {
                    Resource.Type res = s.Walking.Res;
                    uint dest = s.Walking.Dest;

                    Game.CancelTransportedResource(res, dest);
                    Game.LoseResource(res);
                }

                if (GetSerfType() != Type.Sailor)
                {
                    SetState(State.Lost);
                    s.Lost.FieldB = 0;
                }
                else
                {
                    SetState(State.LostSailor);
                }
            }
            else
            {
                SetState(State.Lost);
                s.Lost.FieldB = 0;
            }
        }

        public void AddToDefendingQueue(uint nextKnightIndex, bool pause)
        {
            SetState(State.DefendingCastle);

            s.Defending.NextKnight = nextKnightIndex;

            if (pause)
            {
                Counter = 6000;
            }
        }

        public void InitGeneric(Inventory inventory)
        {
            SetSerfType(Type.Generic);
            Player = inventory.Player;

            Building building = Game.GetBuilding(inventory.GetBuildingIndex());
            Position = building.Position;
            tick = (ushort)Game.Tick;
            SetState(State.IdleInStock);
            s.IdleInStock.InvIndex = inventory.Index;
        }

        public void InitInventoryTransporter(Inventory inventory)
        {
            SetState(State.BuildingCastle);
            s.BuildingCastle.InvIndex = inventory.Index;
        }

        public void ResetTransport(Flag flag)
        {
            if (SerfState == State.Walking && s.Walking.Dest == flag.Index && s.Walking.Dir1 < 0)
            {
                s.Walking.Dir1 = -2;
                s.Walking.Dest = 0;
            }
            else if (SerfState == State.ReadyToLeaveInventory &&
                     s.ReadyToLeaveInventory.Dest == flag.Index &&
                     s.ReadyToLeaveInventory.Mode < 0)
            {
                s.ReadyToLeaveInventory.Mode = -2;
                s.ReadyToLeaveInventory.Dest = 0;
            }
            else if ((SerfState == State.LeavingBuilding || SerfState == State.ReadyToLeave) &&
                     s.LeavingBuilding.NextState == State.Walking &&
                     s.LeavingBuilding.Dest == flag.Index &&
                     s.LeavingBuilding.FieldB < 0)
            {
                s.LeavingBuilding.FieldB = -2;
                s.LeavingBuilding.Dest = 0;
            }
            else if (SerfState == State.Transporting &&
                     s.Walking.Dest == flag.Index)
            {
                s.Walking.Dest = 0;
            }
            else if (SerfState == State.MoveResourceOut &&
                     s.MoveResourceOut.NextState == State.DropResourceOut &&
                     s.MoveResourceOut.ResDest == flag.Index)
            {
                s.MoveResourceOut.ResDest = 0;
            }
            else if (SerfState == State.DropResourceOut &&
                     s.MoveResourceOut.ResDest == flag.Index)
            {
                s.MoveResourceOut.ResDest = 0;
            }
            else if (SerfState == State.LeavingBuilding &&
                     s.LeavingBuilding.NextState == State.DropResourceOut &&
                     s.LeavingBuilding.Dest == flag.Index)
            {
                s.LeavingBuilding.Dest = 0;
            }
        }

        public bool PathSplited(uint flag1, Direction dir1, uint flag2, Direction dir2, ref int select)
        {
            if (SerfState == State.Walking)
            {
                if (s.Walking.Dest == flag1 && s.Walking.Dir1 == (int)dir1)
                {
                    select = 0; // TODO: change required?
                    return true;
                }
                else if (s.Walking.Dest == flag2 && s.Walking.Dir1 == (int)dir2)
                {
                    select = 1;
                    return true;
                }
            }
            else if (SerfState == State.ReadyToLeaveInventory)
            {
                if (s.ReadyToLeaveInventory.Dest == flag1 &&
                    s.ReadyToLeaveInventory.Mode == (int)dir1)
                {
                    select = 0; // TODO: change required?
                    return true;
                }
                else if (s.ReadyToLeaveInventory.Dest == flag2 &&
                         s.ReadyToLeaveInventory.Mode == (int)dir2)
                {
                    select = 1;
                    return true;
                }
            }
            else if ((SerfState == State.ReadyToLeave || SerfState == State.LeavingBuilding) &&
                     s.LeavingBuilding.NextState == State.Walking)
            {
                if (s.LeavingBuilding.Dest == flag1 &&
                    s.LeavingBuilding.FieldB == (int)dir1)
                {
                    select = 0; // TODO: change required?
                    return true;
                }
                else if (s.LeavingBuilding.Dest == flag2 &&
                         s.LeavingBuilding.FieldB == (int)dir2)
                {
                    select = 1;
                    return true;
                }
            }

            return false;
        }

        public bool IsRelatedTo(uint dest, Direction dir)
        {
            bool result = false;

            switch (SerfState)
            {
                case State.Walking:
                    if (s.Walking.Dest == dest && s.Walking.Dir1 == (int)dir)
                    {
                        result = true;
                    }
                    break;
                case State.ReadyToLeaveInventory:
                    if (s.ReadyToLeaveInventory.Dest == dest &&
                        s.ReadyToLeaveInventory.Mode == (int)dir)
                    {
                        result = true;
                    }
                    break;
                case State.LeavingBuilding:
                case State.ReadyToLeave:
                    if (s.LeavingBuilding.Dest == dest &&
                        s.LeavingBuilding.FieldB == (int)dir &&
                        s.LeavingBuilding.NextState == State.Walking)
                    {
                        result = true;
                    }
                    break;
                default:
                    break;
            }

            return result;
        }

        public void PathDeleted(uint dest, Direction dir)
        {
            switch (SerfState)
            {
                case State.Walking:
                    if (s.Walking.Dest == dest && s.Walking.Dir1 == (int)dir)
                    {
                        s.Walking.Dir1 = -2;
                        s.Walking.Dest = 0;
                    }
                    break;
                case State.ReadyToLeaveInventory:
                    if (s.ReadyToLeaveInventory.Dest == dest &&
                        s.ReadyToLeaveInventory.Mode == (int)dir)
                    {
                        s.ReadyToLeaveInventory.Mode = -2;
                        s.ReadyToLeaveInventory.Dest = 0;
                    }
                    break;
                case State.LeavingBuilding:
                case State.ReadyToLeave:
                    if (s.LeavingBuilding.Dest == dest &&
                        s.LeavingBuilding.FieldB == (int)dir &&
                        s.LeavingBuilding.NextState == State.Walking)
                    {
                        s.LeavingBuilding.FieldB = -2;
                        s.LeavingBuilding.Dest = 0;
                    }
                    break;
                default:
                    break;
            }
        }

        public void PathMerged(Flag flag)
        {
            if (SerfState == State.ReadyToLeaveInventory &&
                s.ReadyToLeaveInventory.Dest == flag.Index)
            {
                s.ReadyToLeaveInventory.Dest = 0;
                s.ReadyToLeaveInventory.Mode = -2;
            }
            else if (SerfState == State.Walking && s.Walking.Dest == flag.Index)
            {
                s.Walking.Dest = 0;
                s.Walking.Dir1 = -2;
            }
            else if (SerfState == State.IdleInStock && true/*...*/) // TODO: ?
            {
                /* TODO */
            }
            else if ((SerfState == State.LeavingBuilding || SerfState == State.ReadyToLeave) &&
                   s.LeavingBuilding.Dest == flag.Index &&
                   s.LeavingBuilding.NextState == State.Walking)
            {
                s.LeavingBuilding.Dest = 0;
                s.LeavingBuilding.FieldB = -2;
            }
        }

        public void PathMerged2(uint flag1, Direction dir1, uint flag2, Direction dir2)
        {
            if (SerfState == State.ReadyToLeaveInventory &&
              ((s.ReadyToLeaveInventory.Dest == flag1 &&
                s.ReadyToLeaveInventory.Mode == (int)dir1) ||
               (s.ReadyToLeaveInventory.Dest == flag2 &&
                s.ReadyToLeaveInventory.Mode == (int)dir2)))
            {
                s.ReadyToLeaveInventory.Dest = 0;
                s.ReadyToLeaveInventory.Mode = -2;
            }
            else if (SerfState == State.Walking &&
                     ((s.Walking.Dest == flag1 && s.Walking.Dir1 == (int)dir1) ||
                      (s.Walking.Dest == flag2 && s.Walking.Dir1 == (int)dir2)))
            {
                s.Walking.Dest = 0;
                s.Walking.Dir1 = -2;
            }
            else if (SerfState == State.IdleInStock)
            {
                /* TODO */
            }
            else if ((SerfState == State.LeavingBuilding || SerfState == State.ReadyToLeave) &&
                     ((s.LeavingBuilding.Dest == flag1 &&
                       s.LeavingBuilding.FieldB == (int)dir1) ||
                      (s.LeavingBuilding.Dest == flag2 &&
                       s.LeavingBuilding.FieldB == (int)dir2)) &&
                     s.LeavingBuilding.NextState == State.Walking)
            {
                s.LeavingBuilding.Dest = 0;
                s.LeavingBuilding.FieldB = -2;
            }
        }

        public void FlagDeleted(MapPos flagPos)
        {
            switch (SerfState)
            {
                case State.ReadyToLeave:
                case State.LeavingBuilding:
                    s.LeavingBuilding.NextState = State.Lost;
                    break;
                case State.FinishedBuilding:
                case State.Walking:
                    if (Game.Map.Paths(flagPos) == 0)
                    {
                        SetState(State.Lost);
                    }
                    break;
                default:
                    break;
            }
        }

        public bool BuildingDeleted(MapPos buildingPos, bool escape)
        {
            if (Position == buildingPos &&
                (SerfState == State.IdleInStock || SerfState == State.ReadyToLeaveInventory))
            {
                if (escape)
                {
                    /* Serf is escaping. */
                    SetState(State.EscapeBuilding);
                }
                else
                {
                    /* Kill this serf. */
                    SetSerfType(Type.Dead);
                    Game.DeleteSerf(this);
                }
                return true;
            }

            return false;
        }

        public void CastleDeleted(MapPos castlePos, bool transporter)
        {
            if ((!transporter || (GetSerfType() == Type.TransporterInventory)) &&
                Position == castlePos)
            {
                if (transporter)
                {
                    SetSerfType(Type.Transporter);
                }
            }

            Counter = 0;

            if (Game.Map.GetSerfIndex(Position) == Index)
            {
                SetState(State.Lost);
                s.Lost.FieldB = 0;
            }
            else
            {
                SetState(State.EscapeBuilding);
            }
        }

        public bool ChangeTransporterStateAtPos(MapPos pos, State state)
        {
            if (Position == pos &&
              (state == State.WakeAtFlag || state == State.WakeOnPath ||
               state == State.WaitIdleOnPath || state == State.IdleOnPath))
            {
                SetState(state);
                return true;
            }

            return false;
        }

        public void RestorePathSerfInfo()
        {
            if (SerfState != State.WakeOnPath)
            {
                s.Walking.WaitCounter = -1;

                if (s.Walking.Res != Resource.Type.None)
                {
                    Resource.Type res = s.Walking.Res;
                    s.Walking.Res = Resource.Type.None;

                    Game.CancelTransportedResource(res, s.Walking.Dest);
                    Game.LoseResource(res);
                }
            }
            else
            {
                SetState(State.WakeAtFlag);
            }
        }

        public void ClearDestination(uint dest)
        {
            switch (SerfState)
            {
                case State.Walking:
                    if (s.Walking.Dest == dest && s.Walking.Dir1 < 0)
                    {
                        s.Walking.Dir1 = -2;
                        s.Walking.Dest = 0;
                    }
                    break;
                case State.ReadyToLeaveInventory:
                    if (s.ReadyToLeaveInventory.Dest == dest &&
                        s.ReadyToLeaveInventory.Mode < 0)
                    {
                        s.ReadyToLeaveInventory.Mode = -2;
                        s.ReadyToLeaveInventory.Dest = 0;
                    }
                    break;
                case State.LeavingBuilding:
                case State.ReadyToLeave:
                    if (s.LeavingBuilding.Dest == dest &&
                        s.LeavingBuilding.FieldB < 0 &&
                        s.LeavingBuilding.NextState == State.Walking)
                    {
                        s.LeavingBuilding.FieldB = -2;
                        s.LeavingBuilding.Dest = 0;
                    }
                    break;
                default:
                    break;
            }
        }

        public void ClearDestination2(uint dest)
        {
            switch (SerfState)
            {
                case State.Transporting:
                    if (s.Walking.Dest == dest)
                    {
                        s.Walking.Dest = 0;
                    }
                    break;
                case State.DropResourceOut:
                    if (s.MoveResourceOut.ResDest == dest)
                    {
                        s.MoveResourceOut.ResDest = 0;
                    }
                    break;
                case State.LeavingBuilding:
                    if (s.LeavingBuilding.Dest == dest &&
                        s.LeavingBuilding.NextState == State.DropResourceOut)
                    {
                        s.LeavingBuilding.Dest = 0;
                    }
                    break;
                case State.MoveResourceOut:
                    if (s.MoveResourceOut.ResDest == dest &&
                        s.MoveResourceOut.NextState == State.DropResourceOut)
                    {
                        s.MoveResourceOut.ResDest = 0;
                    }
                    break;
                default:
                    break;
            }
        }

        public bool IdleToWaitState(MapPos pos)
        {
            if (Position == pos &&
              (SerfState == State.IdleOnPath || SerfState == State.WaitIdleOnPath ||
               SerfState == State.WakeAtFlag || SerfState == State.WakeOnPath))
            {
                SetState(State.WakeAtFlag);
                return true;
            }
            return false;
        }

        public int GetDelivery()
        {
            int res = 0;

            switch (SerfState)
            {
                case State.Delivering:
                case State.Transporting:
                    res = (int)s.Walking.Res + 1;
                    break;
                case State.EnteringBuilding:
                    res = s.EnteringBuilding.FieldB;
                    break;
                case State.LeavingBuilding:
                    res = s.LeavingBuilding.FieldB;
                    break;
                case State.ReadyToEnter:
                    res = s.ReadyToEnter.FieldB;
                    break;
                case State.MoveResourceOut:
                case State.DropResourceOut:
                    res = (int)s.MoveResourceOut.Res;
                    break;

                default:
                    break;
            }

            return res;
        }

        internal int GetFreeWalkingNegDist1()
        {
            return s.FreeWalking.NegDist1;
        }

        internal int GetFreeWalkingNegDist2()
        {
            return s.FreeWalking.NegDist2;
        }

        internal State GetLeavingBuildingNextState()
        {
            return s.LeavingBuilding.NextState;
        }

        internal int GetLeavingBuildingFieldB()
        {
            return s.LeavingBuilding.FieldB;
        }

        internal uint GetMiningRes()
        {
            return s.Mining.Res;
        }

        internal Resource.Type GetTransportedResource()
        {
            return s.Walking.Res;
        }

        internal uint GetTransportDestination()
        {
            return s.Walking.Dest;
        }

        internal int GetAttackingFieldD()
        {
            return s.Attacking.FieldD;
        }

        internal int GetAttackingDefIndex()
        {
            return s.Attacking.DefIndex;
        }

        internal int GetWalkingWaitCounter()
        {
            return s.Walking.WaitCounter;
        }

        internal void SetWalkingWaitCounter(int newCounter)
        {
            s.Walking.WaitCounter = newCounter;
        }

        internal int GetWalkingDir()
        {
            return s.Walking.Dir;
        }

        internal uint GetIdleInStockInventoryIndex()
        {
            return s.IdleInStock.InvIndex;
        }

        internal int GetMiningSubstate()
        {
            return (int)s.Mining.Substate;
        }

        public Serf ExtractLastKnightFromList()
        {
            uint defIndex = Index;
            Serf defSerf = Game.GetSerf(defIndex);
            Serf lastKnight = null;

            while (defSerf.s.Defending.NextKnight != 0)
            {
                lastKnight = defSerf;
                defIndex = defSerf.s.Defending.NextKnight;
                defSerf = Game.GetSerf(defIndex);
            }

            if (lastKnight != null)
            {
                lastKnight.s.Defending.NextKnight = defSerf.s.Defending.NextKnight;
                defSerf.s.Defending.NextKnight = 0;
            }

            return defSerf;
        }

        public Serf ExtractKnightFromList(uint index, Serf lastKnight = null)
        {
            if (Index == index)
            {
                if (lastKnight != null)
                {
                    lastKnight.s.Defending.NextKnight = s.Defending.NextKnight;
                    s.Defending.NextKnight = 0;
                }

                return this;
            }

            if (s.Defending.NextKnight == 0)
                return null;

            var nextKnight = Game.GetSerf(s.Defending.NextKnight);

            return nextKnight.ExtractKnightFromList(index, this);
        }

        internal void InsertKnightBefore(Serf knight)
        {
            s.Defending.NextKnight = knight.Index;
        }

        internal uint GetNextKnight()
        {
            return s.Defending.NextKnight;
        }

        internal void SetNextKnight(uint next)
        {
            s.Defending.NextKnight = next;
        }

        internal Building GetBuilding()
        {
            switch (SerfState)
            {
                case State.Baking:
                case State.BuildingBoat:
                case State.BuildingCastle:
                case State.Butchering:
                case State.DefendingCastle:
                case State.DefendingFortress:
                case State.DefendingHut:
                case State.DefendingTower:
                case State.EnteringBuilding:
                case State.FinishedBuilding:
                case State.IdleInStock:
                case State.KnightLeaveForFight:
                case State.KnightLeaveForWalkToFight:
                case State.LeavingBuilding:
                case State.MakingTool:
                case State.MakingWeapon:
                case State.MoveResourceOut:
                case State.Milling:
                case State.PigFarming:
                case State.Sawing:
                case State.Smelting:
                case State.PlanningFarming:
                case State.PlanningFishing:
                case State.PlanningLogging:
                case State.PlanningPlanting:
                case State.PlanningStoneCutting:
                case State.ReadyToLeave:
                case State.ReadyToLeaveInventory:
                case State.WaitForResourceOut:
                    return Game.GetBuildingAtPos(Position);
                case State.Building:
                    return Game.GetBuilding(s.Building.Index);
                case State.Digging:
                    if (s.Digging.Substate <= 0)
                    {
                        return Game.GetBuildingAtPos(Position);
                    }
                    else
                    {
                        if (s.Digging.DigPos == 0)
                            return Game.GetBuildingAtPos(Position);
                        else
                        {
                            var building = Game.GetBuildingAtPos(Game.Map.Move(Position, ((Direction)(6 - s.Digging.DigPos)).Reverse()));

                            if (building != null)
                                return building;

                            return Game.GetBuildingAtPos(Position);
                        }
                    }
                case State.DropResourceOut:
                case State.ReadyToEnter:
                    return Game.GetBuildingAtPos(Game.Map.MoveUpLeft(Position));
                default:
                    return null;
            }
        }

        // Commands

        internal void GoOutFromInventory(uint inventory, MapPos dest, int mode)
        {
            SetState(State.ReadyToLeaveInventory);
            s.ReadyToLeaveInventory.Mode = mode;
            s.ReadyToLeaveInventory.Dest = dest;
            s.ReadyToLeaveInventory.InvIndex = inventory;
        }

        internal void SendOffToFight(int distColumn, int distRow)
        {
            /* Send this serf off to fight. */
            SetState(State.KnightLeaveForWalkToFight);
            s.LeaveForWalkToFight.DistColumn = distColumn;
            s.LeaveForWalkToFight.DistRow = distRow;
            s.LeaveForWalkToFight.FieldD = 0;
            s.LeaveForWalkToFight.FieldE = 0;
            s.LeaveForWalkToFight.NextState = State.KnightFreeWalking;
        }

        internal void StayIdleInStock(uint inventory)
        {
            SetState(State.IdleInStock);
            s.IdleInStock.InvIndex = inventory;
        }

        internal void GoOutFromBuilding(MapPos dest, int dir, int fieldB)
        {
            SetState(State.ReadyToLeave);
            s.LeavingBuilding.FieldB = fieldB;
            s.LeavingBuilding.Dest = dest;
            s.LeavingBuilding.Dir = dir;
            s.LeavingBuilding.NextState = State.Walking;
        }

        internal void Update()
        {
            switch (SerfState)
            {
                case State.Null: /* 0 */
                    break;
                case State.Walking:
                    HandleSerfWalkingState();
                    break;
                case State.Transporting:
                    HandleSerfTransportingState();
                    break;
                case State.IdleInStock:
                    HandleSerfIdleInStockState();
                    break;
                case State.EnteringBuilding:
                    HandleSerfEnteringBuildingState();
                    break;
                case State.LeavingBuilding: /* 5 */
                    HandleSerfLeavingBuildingState();
                    break;
                case State.ReadyToEnter:
                    HandleSerfReadyToEnterState();
                    break;
                case State.ReadyToLeave:
                    HandleSerfReadyToLeaveState();
                    break;
                case State.Digging:
                    HandleSerfDiggingState();
                    break;
                case State.Building:
                    HandleSerfBuildingState();
                    break;
                case State.BuildingCastle: /* 10 */
                    HandleSerfBuildingCastleState();
                    break;
                case State.MoveResourceOut:
                    HandleSerfMoveResourceOutState();
                    break;
                case State.WaitForResourceOut:
                    HandleSerfWaitForResourceOutState();
                    break;
                case State.DropResourceOut:
                    HandleSerfDropResourceOutState();
                    break;
                case State.Delivering:
                    HandleSerfDeliveringState();
                    break;
                case State.ReadyToLeaveInventory: /* 15 */
                    HandleSerfReadyToLeaveInventoryState();
                    break;
                case State.FreeWalking:
                    HandleSerfFreeWalkingState();
                    break;
                case State.Logging:
                    HandleSerfLoggingState();
                    break;
                case State.PlanningLogging:
                    HandleSerfPlanningLoggingState();
                    break;
                case State.PlanningPlanting:
                    HandleSerfPlanningPlantingState();
                    break;
                case State.Planting: /* 20 */
                    HandleSerfPlantingState();
                    break;
                case State.PlanningStoneCutting:
                    HandleSerfPlanningStonecutting();
                    break;
                case State.StoneCutterFreeWalking:
                    HandleStonecutterFreeWalking();
                    break;
                case State.StoneCutting:
                    HandleSerfStonecuttingState();
                    break;
                case State.Sawing:
                    HandleSerfSawingState();
                    break;
                case State.Lost: /* 25 */
                    HandleSerfLostState();
                    break;
                case State.LostSailor:
                    HandleLostSailor();
                    break;
                case State.FreeSailing:
                    HandleFreeSailing();
                    break;
                case State.EscapeBuilding:
                    HandleSerfEscapeBuildingState();
                    break;
                case State.Mining:
                    HandleSerfMiningState();
                    break;
                case State.Smelting: /* 30 */
                    HandleSerfSmeltingState();
                    break;
                case State.PlanningFishing:
                    HandleSerfPlanningFishingState();
                    break;
                case State.Fishing:
                    HandleSerfFishingState();
                    break;
                case State.PlanningFarming:
                    HandleSerfPlanningFarmingState();
                    break;
                case State.Farming:
                    HandleSerfFarmingState();
                    break;
                case State.Milling: /* 35 */
                    HandleSerfMillingState();
                    break;
                case State.Baking:
                    HandleSerfBakingState();
                    break;
                case State.PigFarming:
                    HandleSerfPigfarmingState();
                    break;
                case State.Butchering:
                    HandleSerfButcheringState();
                    break;
                case State.MakingWeapon:
                    HandleSerfMakingWeaponState();
                    break;
                case State.MakingTool: /* 40 */
                    HandleSerfMakingToolState();
                    break;
                case State.BuildingBoat:
                    HandleSerfBuildingBoatState();
                    break;
                case State.LookingForGeoSpot:
                    HandleSerfLookingForGeoSpotState();
                    break;
                case State.SamplingGeoSpot:
                    HandleSerfSamplingGeoSpotState();
                    break;
                case State.KnightEngagingBuilding:
                    HandleSerfKnightEngagingBuildingState();
                    break;
                case State.KnightPrepareAttacking: /* 45 */
                    HandleSerfKnightPrepareAttacking();
                    break;
                case State.KnightLeaveForFight:
                    HandleSerfKnightLeaveForFightState();
                    break;
                case State.KnightPrepareDefending:
                    HandleSerfKnightPrepareDefendingState();
                    break;
                case State.KnightAttacking:
                case State.KnightAttackingFree:
                    HandleKnightAttacking();
                    break;
                case State.KnightDefending:
                case State.KnightDefendingFree:
                    /* The actual fight update is handled for the attacking knight. */
                    break;
                case State.KnightAttackingVictory: /* 50 */
                    HandleSerfKnightAttackingVictoryState();
                    break;
                case State.KnightAttackingDefeat:
                    HandleSerfKnightAttackingDefeatState();
                    break;
                case State.KnightOccupyEnemyBuilding:
                    HandleKnightOccupyEnemyBuilding();
                    break;
                case State.KnightFreeWalking:
                    HandleStateKnightFreeWalking();
                    break;
                case State.KnightEngageDefendingFree:
                    HandleStateKnightEngageDefendingFree();
                    break;
                case State.KnightEngageAttackingFree:
                    HandleStateKnightEngageAttackingFree();
                    break;
                case State.KnightEngageAttackingFreeJoin:
                    HandleStateKnightEngageAttackingFreeJoin();
                    break;
                case State.KnightPrepareAttackingFree:
                    HandleStateKnightPrepareAttackingFree();
                    break;
                case State.KnightPrepareDefendingFree:
                    HandleStateKnightPrepareDefendingFree();
                    break;
                case State.KnightPrepareDefendingFreeWait:
                    /* Nothing to do for this state. */
                    break;
                case State.KnightAttackingVictoryFree:
                    HandleKnightAttackingVictoryFree();
                    break;
                case State.KnightDefendingVictoryFree:
                    HandleKnightDefendingVictoryFree();
                    break;
                case State.KnightAttackingDefeatFree:
                    HandleSerfKnightAttackingDefeatFreeState();
                    break;
                case State.KnightAttackingFreeWait:
                    HandleKnightAttackingFreeWait();
                    break;
                case State.KnightLeaveForWalkToFight: /* 65 */
                    HandleSerfStateKnightLeaveForWalkToFight();
                    break;
                case State.IdleOnPath:
                    HandleSerfIdleOnPathState();
                    break;
                case State.WaitIdleOnPath:
                    HandleSerfWaitIdleOnPathState();
                    break;
                case State.WakeAtFlag:
                    HandleSerfWakeAtFlagState();
                    break;
                case State.WakeOnPath:
                    HandleSerfWakeOnPathState();
                    break;
                case State.DefendingHut: /* 70 */
                    HandleSerfDefendingHutState();
                    break;
                case State.DefendingTower:
                    HandleSerfDefendingTowerState();
                    break;
                case State.DefendingFortress:
                    HandleSerfDefendingFortressState();
                    break;
                case State.Scatter:
                    HandleScatterState();
                    break;
                case State.FinishedBuilding:
                    HandleSerfFinishedBuildingState();
                    break;
                case State.DefendingCastle: /* 75 */
                    HandleSerfDefendingCastleState();
                    break;
                default:
                    Log.Debug.Write("serf", $"Serf state {SerfState} isn't processed");
                    SerfState = State.Null;
                    break;
            }
        }

        public static string GetStateName(State state)
        {
            return SerfStateNames[(int)state];
        }

        static string GetTypeName(Type type)
        {
            return SerfTypeNames[(int)type];
        }

        public void ReadFrom(SaveReaderBinary reader)
        {
            byte v8 = reader.ReadByte(); // 0

            Player = (uint)v8 & 3;
            type = (Type)((v8 >> 2) & 0x1F);
            sound = ((v8 >> 7) != 0);

            Animation = reader.ReadByte(); // 1
            Counter = reader.ReadWord(); // 2
            Position = reader.ReadDWord(); // 4

            if (Position != 0xFFFFFFFF)
            {
                Position = Game.Map.PosFromSavedValue(Position);
            }

            tick = reader.ReadWord(); // 8
            SerfState = (State)reader.ReadByte(); // 10

            Log.Verbose.Write("savegame", $"load serf {Index}: {SerfStateNames[(int)SerfState]}");

            switch (SerfState)
            {
                case State.IdleInStock:
                    reader.Skip(3); // 11
                    s.IdleInStock.InvIndex = reader.ReadWord(); // 14
                    break;

                case State.Walking:
                    {
                        s.Walking.Dir1 = reader.ReadByte(); // 11
                        s.Walking.Dest = reader.ReadWord(); // 12
                        s.Walking.Dir = reader.ReadByte(); // 14
                        s.Walking.WaitCounter = reader.ReadByte(); // 15
                        break;
                    }
                case State.Transporting:
                case State.Delivering:
                    {
                        s.Walking.Res = (Resource.Type)((reader.ReadByte()) - 1); // 11
                        s.Walking.Dest = reader.ReadWord(); // 12
                        s.Walking.Dir = reader.ReadByte(); // 14
                        s.Walking.WaitCounter = reader.ReadByte(); // 15
                        break;
                    }
                case State.EnteringBuilding:
                    s.EnteringBuilding.FieldB = reader.ReadByte(); // 11
                    s.EnteringBuilding.SlopeLength = reader.ReadWord(); // 12
                    break;

                case State.LeavingBuilding:
                case State.ReadyToLeave:
                    s.LeavingBuilding.FieldB = reader.ReadByte(); // 11
                    s.LeavingBuilding.Dest = reader.ReadByte(); // 12
                    s.LeavingBuilding.Dest2 = reader.ReadByte(); // 13
                    s.LeavingBuilding.Dir = reader.ReadByte(); // 14
                    s.LeavingBuilding.NextState = (State)reader.ReadByte(); // 15
                    break;

                case State.ReadyToEnter:
                    s.ReadyToEnter.FieldB = reader.ReadByte(); // 11
                    break;

                case State.Digging:
                    s.Digging.HIndex = reader.ReadByte(); // 11
                    s.Digging.TargetH = reader.ReadByte(); // 12
                    s.Digging.DigPos = reader.ReadByte(); // 13
                    s.Digging.Substate = reader.ReadByte(); // 14
                    break;

                case State.Building:
                    s.Building.Mode = reader.ReadByte(); // 11
                    s.Building.Index = reader.ReadWord(); // 12
                    s.Building.MaterialStep = reader.ReadByte(); // 14
                    s.Building.Counter = reader.ReadByte(); // 15
                    break;

                case State.BuildingCastle:
                    reader.Skip(1); // 11
                    s.BuildingCastle.InvIndex = reader.ReadWord(); // 12
                    break;

                case State.MoveResourceOut:
                case State.DropResourceOut:
                    s.MoveResourceOut.Res = reader.ReadByte(); // 11
                    s.MoveResourceOut.ResDest = reader.ReadWord(); // 12
                    reader.Skip(1); // 14
                    s.MoveResourceOut.NextState = (State)reader.ReadByte(); // 15
                    break;

                case State.ReadyToLeaveInventory:
                    s.ReadyToLeaveInventory.Mode = reader.ReadByte(); // 11
                    s.ReadyToLeaveInventory.Dest = reader.ReadWord(); // 12
                    s.ReadyToLeaveInventory.InvIndex = reader.ReadWord(); // 14
                    break;

                case State.FreeWalking:
                case State.Logging:
                case State.Planting:
                case State.StoneCutting:
                case State.StoneCutterFreeWalking:
                case State.Fishing:
                case State.Farming:
                case State.SamplingGeoSpot:
                    s.FreeWalking.Dist1 = reader.ReadByte(); // 11
                    s.FreeWalking.Dist2 = reader.ReadByte(); // 12
                    s.FreeWalking.NegDist1 = reader.ReadByte(); // 13
                    s.FreeWalking.NegDist2 = reader.ReadByte(); // 14
                    s.FreeWalking.Flags = reader.ReadByte(); // 15
                    break;

                case State.Sawing:
                    s.Sawing.Mode = reader.ReadByte(); // 11
                    break;

                case State.Lost:
                    s.Lost.FieldB = reader.ReadByte(); // 11
                    break;

                case State.Mining:
                    s.Mining.Substate = reader.ReadByte(); // 11
                    reader.Skip(1); // 12
                    s.Mining.Res = reader.ReadByte(); // 13
                    s.Mining.Deposit = (Map.Minerals)reader.ReadByte(); // 14
                    break;

                case State.Smelting:
                    s.Smelting.Mode = reader.ReadByte(); // 11
                    s.Smelting.Counter = reader.ReadByte(); // 12
                    s.Smelting.Type = reader.ReadByte(); // 13
                    break;

                case State.Milling:
                    s.Milling.Mode = reader.ReadByte(); // 11
                    break;

                case State.Baking:
                    s.Baking.Mode = reader.ReadByte(); // 11
                    break;

                case State.PigFarming:
                    s.PigFarming.Mode = reader.ReadByte(); // 11
                    break;

                case State.Butchering:
                    s.Butchering.Mode = reader.ReadByte(); // 11
                    break;

                case State.MakingWeapon:
                    s.MakingWeapon.Mode = reader.ReadByte(); // 11
                    break;

                case State.MakingTool:
                    s.MakingTool.Mode = reader.ReadByte(); // 11
                    break;

                case State.BuildingBoat:
                    s.BuildingBoat.Mode = reader.ReadByte(); // 11
                    break;

                case State.KnightDefendingVictoryFree:
                    /* TODO This will be tricky to load since the
                     function of this state has been changed to one
                     that is driven by the attacking serf instead
                     (StateKnightAttackingDefeatFree). */
                    break;

                case State.IdleOnPath:
                case State.WaitIdleOnPath:
                case State.WakeAtFlag:
                case State.WakeOnPath:
                    {
                        s.IdleOnPath.RevDir = (Direction)reader.ReadByte(); // 11
                        var v16 = reader.ReadWord(); // 12
                        Game.CreateFlag(v16 / 70);
                        s.IdleOnPath.FlagIndex = Game.CreateFlag(v16 / 70).Index;
                        s.IdleOnPath.FieldE = reader.ReadByte(); // 14
                        break;
                    }
                case State.DefendingHut:
                case State.DefendingTower:
                case State.DefendingFortress:
                case State.DefendingCastle:
                    reader.Skip(3); // 11
                    s.Defending.NextKnight = reader.ReadWord(); // 14
                    break;

                default:
                    break;
            }
        }

        public void ReadFrom(SaveReaderText reader)
        {
            int type = reader.Value("type").ReadInt();

            try
            {
                Player = reader.Value("owner").ReadUInt();
                this.type = (Type)type;
            }
            catch
            {
                this.type = (Type)((type >> 2) & 0x1f);
                Player = (uint)type & 3;
            }

            Animation = reader.Value("animation").ReadInt();
            Counter = reader.Value("counter").ReadInt();

            uint x = reader.Value("pos")[0].ReadUInt();
            uint y = reader.Value("pos")[1].ReadUInt();

            Position = Game.Map.Pos(x, y);
            tick = (ushort)reader.Value("tick").ReadUInt();
            SerfState = (State)reader.Value("state").ReadInt();

            switch (SerfState)
            {
                case State.IdleInStock:
                    s.IdleInStock.InvIndex = reader.Value("state.inventory").ReadUInt();
                    break;

                case State.Walking:
                case State.Transporting:
                case State.Delivering:
                    s.Walking.Res = (Resource.Type)reader.Value("state.res").ReadInt();
                    s.Walking.Dest = reader.Value("state.dest").ReadUInt();
                    s.Walking.Dir = reader.Value("state.dir").ReadInt();
                    s.Walking.Dir1 = reader.Value("state.dir1").ReadInt();
                    s.Walking.WaitCounter = reader.Value("state.wait_counter").ReadInt();
                    break;

                case State.EnteringBuilding:
                    s.EnteringBuilding.FieldB = reader.Value("state.field_b").ReadInt();
                    s.EnteringBuilding.SlopeLength = reader.Value("state.slope_len").ReadInt();
                    break;

                case State.LeavingBuilding:
                case State.ReadyToLeave:
                case State.KnightLeaveForFight:
                    s.LeavingBuilding.FieldB = reader.Value("state.field_b").ReadInt();
                    s.LeavingBuilding.Dest = reader.Value("state.dest").ReadUInt();
                    s.LeavingBuilding.Dest2 = reader.Value("state.dest2").ReadInt();
                    s.LeavingBuilding.Dir = reader.Value("state.dir").ReadInt();
                    s.LeavingBuilding.NextState = (State)reader.Value("state.next_state").ReadInt();
                    break;

                case State.ReadyToEnter:
                    s.ReadyToEnter.FieldB = reader.Value("state.field_b").ReadInt();
                    break;

                case State.Digging:
                    s.Digging.HIndex = reader.Value("state.h_index").ReadInt();
                    s.Digging.TargetH = reader.Value("state.target_h").ReadUInt();
                    s.Digging.DigPos = reader.Value("state.dig_pos").ReadInt();
                    s.Digging.Substate = reader.Value("state.substate").ReadInt();
                    break;

                case State.Building:
                    s.Building.Mode = reader.Value("state.mode").ReadInt();
                    s.Building.Index = reader.Value("state.bld_index").ReadUInt();
                    s.Building.MaterialStep = reader.Value("state.material_step").ReadUInt();
                    s.Building.Counter = reader.Value("state.counter").ReadUInt();
                    break;

                case State.BuildingCastle:
                    s.BuildingCastle.InvIndex = reader.Value("state.inv_index").ReadUInt();
                    break;

                case State.MoveResourceOut:
                case State.DropResourceOut:
                    s.MoveResourceOut.Res = reader.Value("state.res").ReadUInt();
                    s.MoveResourceOut.ResDest = reader.Value("state.res_dest").ReadUInt();
                    s.MoveResourceOut.NextState = (State)reader.Value("state.next_state").ReadInt();
                    break;

                case State.ReadyToLeaveInventory:
                    s.ReadyToLeaveInventory.Mode = reader.Value("state.mode").ReadInt();
                    s.ReadyToLeaveInventory.Dest = reader.Value("state.dest").ReadUInt();
                    s.ReadyToLeaveInventory.InvIndex = reader.Value("state.inv_index").ReadUInt();
                    break;

                case State.FreeWalking:
                case State.Logging:
                case State.Planting:
                case State.StoneCutting:
                case State.StoneCutterFreeWalking:
                case State.Fishing:
                case State.Farming:
                case State.SamplingGeoSpot:
                case State.KnightFreeWalking:
                case State.KnightAttackingFree:
                case State.KnightAttackingFreeWait:
                    s.FreeWalking.Dist1 = reader.Value("state.dist1").ReadInt();
                    s.FreeWalking.Dist2 = reader.Value("state.dist2").ReadInt();
                    s.FreeWalking.NegDist1 = reader.Value("state.neg_dist").ReadInt();
                    s.FreeWalking.NegDist2 = reader.Value("state.neg_dist2").ReadInt();
                    s.FreeWalking.Flags = reader.Value("state.flags").ReadInt();
                    break;

                case State.Sawing:
                    s.Sawing.Mode = reader.Value("state.mode").ReadInt();
                    break;

                case State.Lost:
                    s.Lost.FieldB = reader.Value("state.field_b").ReadInt();
                    break;

                case State.Mining:
                    s.Mining.Substate = reader.Value("state.substate").ReadUInt();
                    s.Mining.Res = reader.Value("state.res").ReadUInt();
                    s.Mining.Deposit = (Map.Minerals)reader.Value("state.deposit").ReadInt();
                    break;

                case State.Smelting:
                    s.Smelting.Mode = reader.Value("state.mode").ReadInt();
                    s.Smelting.Counter = reader.Value("state.counter").ReadInt();
                    s.Smelting.Type = reader.Value("state.type").ReadInt();
                    break;

                case State.Milling:
                    s.Milling.Mode = reader.Value("state.mode").ReadInt();
                    break;

                case State.Baking:
                    s.Baking.Mode = reader.Value("state.mode").ReadInt();
                    break;

                case State.PigFarming:
                    s.PigFarming.Mode = reader.Value("state.mode").ReadInt();
                    break;

                case State.Butchering:
                    s.Butchering.Mode = reader.Value("state.mode").ReadInt();
                    break;

                case State.MakingWeapon:
                    s.MakingWeapon.Mode = reader.Value("state.mode").ReadInt();
                    break;

                case State.MakingTool:
                    s.MakingTool.Mode = reader.Value("state.mode").ReadInt();
                    break;

                case State.BuildingBoat:
                    s.BuildingBoat.Mode = reader.Value("state.mode").ReadInt();
                    break;

                case State.KnightEngagingBuilding:
                case State.KnightPrepareAttacking:
                case State.KnightPrepareDefendingFreeWait:
                case State.KnightAttackingDefeatFree:
                case State.KnightAttacking:
                case State.KnightAttackingVictory:
                case State.KnightEngageAttackingFree:
                case State.KnightEngageAttackingFreeJoin:
                    if (reader.HasValue("state.move"))
                        s.Attacking.Move = reader.Value("state.move").ReadInt();
                    else
                        s.Attacking.Move = reader.Value("state.field_b").ReadInt();
                    if (reader.HasValue("state.attacker_won"))
                        s.Attacking.AttackerWon = reader.Value("state.attacker_won").ReadInt();
                    else
                        s.Attacking.AttackerWon = reader.Value("state.field_c").ReadInt();
                    s.Attacking.FieldD = reader.Value("state.field_d").ReadInt();
                    s.Attacking.DefIndex = reader.Value("state.def_index").ReadInt();
                    break;

                case State.KnightAttackingVictoryFree:
                    if (reader.HasValue("state.move"))
                        s.AttackingVictoryFree.Move = reader.Value("state.move").ReadInt();
                    else
                        s.AttackingVictoryFree.Move = reader.Value("state.field_b").ReadInt();
                    if (reader.HasValue("state.dist_col"))
                        s.AttackingVictoryFree.DistColumn = reader.Value("state.dist_col").ReadInt();
                    else
                        s.AttackingVictoryFree.DistColumn = reader.Value("state.field_c").ReadInt();
                    if (reader.HasValue("state.dist_row"))
                        s.AttackingVictoryFree.DistRow = reader.Value("state.dist_row").ReadInt();
                    else
                        s.AttackingVictoryFree.DistRow = reader.Value("state.field_c").ReadInt();
                    s.Attacking.DefIndex = reader.Value("state.def_index").ReadInt();
                    break;

                case State.KnightDefendingFree:
                case State.KnightEngageDefendingFree:
                    s.DefendingFree.DistColumn = reader.Value("state.dist_col").ReadInt();
                    s.DefendingFree.DistRow = reader.Value("state.dist_row").ReadInt();
                    s.DefendingFree.FieldD = reader.Value("state.field_d").ReadInt();
                    s.DefendingFree.OtherDistColumn = reader.Value("state.other_dist_col").ReadInt();
                    s.DefendingFree.OtherDistRow = reader.Value("state.other_dist_row").ReadInt();
                    break;

                case State.KnightLeaveForWalkToFight:
                    s.LeaveForWalkToFight.DistColumn = reader.Value("state.dist_col").ReadInt();
                    s.LeaveForWalkToFight.DistRow = reader.Value("state.dist_row").ReadInt();
                    s.LeaveForWalkToFight.FieldD = reader.Value("state.field_d").ReadInt();
                    s.LeaveForWalkToFight.FieldE = reader.Value("state.field_e").ReadInt();
                    s.LeaveForWalkToFight.NextState = (State)reader.Value("state.next_state").ReadInt();
                    break;

                case State.IdleOnPath:
                case State.WaitIdleOnPath:
                case State.WakeAtFlag:
                case State.WakeOnPath:
                    s.IdleOnPath.RevDir = (Direction)reader.Value("state.rev_dir").ReadInt();
                    s.IdleOnPath.FlagIndex = Game.CreateFlag(reader.Value("state.flag").ReadInt()).Index;
                    s.IdleOnPath.FieldE = reader.Value("state.field_E").ReadInt();
                    break;
                case State.DefendingHut:
                case State.DefendingTower:
                case State.DefendingFortress:
                case State.DefendingCastle:
                    s.Defending.NextKnight = reader.Value("state.next_knight").ReadUInt();
                    break;

                default:
                    break;
            }
        }

        public void WriteTo(SaveWriterText writer)
        {
            writer.Value("type").Write((int)type);
            writer.Value("owner").Write(Player);
            writer.Value("animation").Write(Animation);
            writer.Value("counter").Write(Counter);
            writer.Value("pos").Write(Game.Map.PosColumn(Position));
            writer.Value("pos").Write(Game.Map.PosRow(Position));
            writer.Value("tick").Write(tick);
            writer.Value("state").Write((int)SerfState);

            switch (SerfState)
            {
                case State.IdleInStock:
                    writer.Value("state.inventory").Write(s.IdleInStock.InvIndex);
                    break;

                case State.Walking:
                case State.Transporting:
                case State.Delivering:
                    writer.Value("state.res").Write((int)s.Walking.Res);
                    writer.Value("state.dest").Write(s.Walking.Dest);
                    writer.Value("state.dir").Write(s.Walking.Dir);
                    writer.Value("state.dir1").Write(s.Walking.Dir1);
                    writer.Value("state.wait_counter").Write(s.Walking.WaitCounter);
                    break;

                case State.EnteringBuilding:
                    writer.Value("state.field_b").Write(s.EnteringBuilding.FieldB);
                    writer.Value("state.slope_len").Write(s.EnteringBuilding.SlopeLength);
                    break;

                case State.LeavingBuilding:
                case State.ReadyToLeave:
                case State.KnightLeaveForFight:
                    writer.Value("state.field_b").Write(s.LeavingBuilding.FieldB);
                    writer.Value("state.dest").Write(s.LeavingBuilding.Dest);
                    writer.Value("state.dest2").Write(s.LeavingBuilding.Dest2);
                    writer.Value("state.dir").Write(s.LeavingBuilding.Dir);
                    writer.Value("state.next_state").Write((int)s.LeavingBuilding.NextState);
                    break;

                case State.ReadyToEnter:
                    writer.Value("state.field_b").Write(s.ReadyToEnter.FieldB);
                    break;

                case State.Digging:
                    writer.Value("state.h_index").Write(s.Digging.HIndex);
                    writer.Value("state.target_h").Write(s.Digging.TargetH);
                    writer.Value("state.dig_pos").Write(s.Digging.DigPos);
                    writer.Value("state.substate").Write(s.Digging.Substate);
                    break;

                case State.Building:
                    writer.Value("state.mode").Write(s.Building.Mode);
                    writer.Value("state.bld_index").Write(s.Building.Index);
                    writer.Value("state.material_step").Write(s.Building.MaterialStep);
                    writer.Value("state.counter").Write(s.Building.Counter);
                    break;

                case State.BuildingCastle:
                    writer.Value("state.inv_index").Write(s.BuildingCastle.InvIndex);
                    break;

                case State.MoveResourceOut:
                case State.DropResourceOut:
                    writer.Value("state.res").Write(s.MoveResourceOut.Res);
                    writer.Value("state.res_dest").Write(s.MoveResourceOut.ResDest);
                    writer.Value("state.next_state").Write((int)s.MoveResourceOut.NextState);
                    break;

                case State.ReadyToLeaveInventory:
                    writer.Value("state.mode").Write(s.ReadyToLeaveInventory.Mode);
                    writer.Value("state.dest").Write(s.ReadyToLeaveInventory.Dest);
                    writer.Value("state.inv_index").Write(s.ReadyToLeaveInventory.InvIndex);
                    break;

                case State.FreeWalking:
                case State.Logging:
                case State.Planting:
                case State.StoneCutting:
                case State.StoneCutterFreeWalking:
                case State.Fishing:
                case State.Farming:
                case State.SamplingGeoSpot:
                case State.KnightFreeWalking:
                case State.KnightAttackingFree:
                case State.KnightAttackingFreeWait:
                    writer.Value("state.dist1").Write(s.FreeWalking.Dist1);
                    writer.Value("state.dist2").Write(s.FreeWalking.Dist2);
                    writer.Value("state.neg_dist").Write(s.FreeWalking.NegDist1);
                    writer.Value("state.neg_dist2").Write(s.FreeWalking.NegDist2);
                    writer.Value("state.flags").Write(s.FreeWalking.Flags);
                    break;

                case State.Sawing:
                    writer.Value("state.mode").Write(s.Sawing.Mode);
                    break;

                case State.Lost:
                    writer.Value("state.field_b").Write(s.Lost.FieldB);
                    break;

                case State.Mining:
                    writer.Value("state.substate").Write(s.Mining.Substate);
                    writer.Value("state.res").Write(s.Mining.Res);
                    writer.Value("state.deposit").Write((int)s.Mining.Deposit);
                    break;

                case State.Smelting:
                    writer.Value("state.mode").Write(s.Smelting.Mode);
                    writer.Value("state.counter").Write(s.Smelting.Counter);
                    writer.Value("state.type").Write(s.Smelting.Type);
                    break;

                case State.Milling:
                    writer.Value("state.mode").Write(s.Milling.Mode);
                    break;

                case State.Baking:
                    writer.Value("state.mode").Write(s.Baking.Mode);
                    break;

                case State.PigFarming:
                    writer.Value("state.mode").Write(s.PigFarming.Mode);
                    break;

                case State.Butchering:
                    writer.Value("state.mode").Write(s.Butchering.Mode);
                    break;

                case State.MakingWeapon:
                    writer.Value("state.mode").Write(s.MakingWeapon.Mode);
                    break;

                case State.MakingTool:
                    writer.Value("state.mode").Write(s.MakingTool.Mode);
                    break;

                case State.BuildingBoat:
                    writer.Value("state.mode").Write(s.BuildingBoat.Mode);
                    break;

                case State.KnightEngagingBuilding:
                case State.KnightPrepareAttacking:
                case State.KnightPrepareDefendingFreeWait:
                case State.KnightAttackingDefeatFree:
                case State.KnightAttacking:
                case State.KnightAttackingVictory:
                case State.KnightEngageAttackingFree:
                case State.KnightEngageAttackingFreeJoin:                
                    writer.Value("state.move").Write(s.Attacking.Move);
                    writer.Value("state.field_c").Write(s.Attacking.AttackerWon);
                    writer.Value("state.field_d").Write(s.Attacking.FieldD);
                    writer.Value("state.def_index").Write(s.Attacking.DefIndex);
                    break;

                case State.KnightAttackingVictoryFree:
                    writer.Value("state.move").Write(s.AttackingVictoryFree.Move);
                    writer.Value("state.dist_col").Write(s.AttackingVictoryFree.DistColumn);
                    writer.Value("state.dist_row").Write(s.AttackingVictoryFree.DistRow);
                    writer.Value("state.def_index").Write(s.AttackingVictoryFree.DefIndex);
                    break;

                case State.KnightDefendingFree:
                case State.KnightEngageDefendingFree:
                    writer.Value("state.dist_col").Write(s.DefendingFree.DistColumn);
                    writer.Value("state.dist_row").Write(s.DefendingFree.DistRow);
                    writer.Value("state.field_d").Write(s.DefendingFree.FieldD);
                    writer.Value("state.other_dist_col").Write(s.DefendingFree.OtherDistColumn);
                    writer.Value("state.other_dist_row").Write(s.DefendingFree.OtherDistRow);
                    break;

                case State.KnightLeaveForWalkToFight:
                    writer.Value("state.dist_col").Write(s.LeaveForWalkToFight.DistColumn);
                    writer.Value("state.dist_row").Write(s.LeaveForWalkToFight.DistRow);
                    writer.Value("state.field_d").Write(s.LeaveForWalkToFight.FieldD);
                    writer.Value("state.field_e").Write(s.LeaveForWalkToFight.FieldE);
                    writer.Value("state.next_state").Write((int)s.LeaveForWalkToFight.NextState);
                    break;

                case State.IdleOnPath:
                case State.WaitIdleOnPath:
                case State.WakeAtFlag:
                case State.WakeOnPath:
                    writer.Value("state.rev_dir").Write((int)s.IdleOnPath.RevDir);
                    writer.Value("state.flag").Write(Game.GetFlag(s.IdleOnPath.FlagIndex).Index);
                    writer.Value("state.field_e").Write(s.IdleOnPath.FieldE);
                    break;

                case State.DefendingHut:
                case State.DefendingTower:
                case State.DefendingFortress:
                case State.DefendingCastle:
                    writer.Value("state.next_knight").Write(s.Defending.NextKnight);
                    break;

                default: break;
            }
        }

        /* Return true if serf is waiting for a position to be available.
           In this case, dir will be set to the desired direction of the serf,
           or DirectionNone if the desired direction cannot be determined. */
        bool IsWaiting(ref Direction dir)
        {
            if ((SerfState == State.Transporting || SerfState == State.Walking ||
                 SerfState == State.Delivering) &&
                 s.Walking.Dir < 0)
            {
                dir = (Direction)(s.Walking.Dir + 6);
                return true;
            }
            else if ((SerfState == State.FreeWalking ||
                      SerfState == State.KnightFreeWalking ||
                      SerfState == State.StoneCutterFreeWalking) &&
                      Animation == 82)
            {
                int dx = s.FreeWalking.Dist1;
                int dy = s.FreeWalking.Dist2;

                if (Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1 &&
                    DirFromOffset[(dx + 1) + 3 * (dy + 1)] > Direction.None)
                {
                    dir = DirFromOffset[(dx + 1) + 3 * (dy + 1)];
                }
                else
                {
                    dir = Direction.None;
                }

                return true;
            }
            else if (SerfState == State.Digging && s.Digging.Substate < 0)
            {
                int d = s.Digging.DigPos;

                dir = (d == 0) ? Direction.Up : (Direction)(6 - d);

                return true;
            }

            return false;
        }

        /* Signal waiting serf that it is possible to move in direction
           while switching position with another serf. Returns 0 if the
           switch is not acceptable. */
        bool SwitchWaiting(Direction dir)
        {
            if ((SerfState == State.Transporting || SerfState == State.Walking ||
                SerfState == State.Delivering) &&
                s.Walking.Dir < 0)
            {
                s.Walking.Dir = (int)dir.Reverse();
                return true;
            }
            else if ((SerfState == State.FreeWalking ||
                      SerfState == State.KnightFreeWalking ||
                      SerfState == State.StoneCutterFreeWalking) &&
                      Animation == 82)
            {
                int dx = (((int)dir < 3) ? 1 : -1) * ((((int)dir % 3) < 2) ? 1 : 0);
                int dy = (((int)dir < 3) ? 1 : -1) * ((((int)dir % 3) > 0) ? 1 : 0);

                s.FreeWalking.Dist1 -= dx;
                s.FreeWalking.Dist2 -= dy;

                if (s.FreeWalking.Dist1 == 0 && s.FreeWalking.Dist2 == 0)
                {
                    /* Arriving to destination */
                    s.FreeWalking.Flags = Misc.Bit(3);
                }

                return true;
            }
            else if (SerfState == State.Digging && s.Digging.Substate < 0)
            {
                return false;
            }

            return false;
        }

        int GetWalkingAnimation(int hDiff, Direction dir, bool switchPos)
        {
            int d = (int)dir;

            if (switchPos && d < 3)
                d += 6;

            return 4 + hDiff + 9 * d;
        }

        /* Preconditon: serf is in WALKING or TRANSPORTING state */
        void ChangeDirection(Direction dir, bool altEnd)
        {
            Map map = Game.Map;
            MapPos newPos = map.Move(Position, dir);

            if (!map.HasSerf(newPos))
            {
                /* Change direction, not occupied. */
                map.SetSerfIndex(Position, 0);
                Animation = GetWalkingAnimation((int)map.GetHeight(newPos) - (int)map.GetHeight(Position), dir, false);
                s.Walking.Dir = (int)dir.Reverse();
            }
            else
            {
                /* Direction is occupied. */
                Serf otherSerf = Game.GetSerfAtPos(newPos);
                Direction otherDir = Direction.None;

                if (otherSerf.IsWaiting(ref otherDir) &&
                    (otherDir == dir.Reverse() || otherDir == Direction.None) &&
                    otherSerf.SwitchWaiting(dir.Reverse()))
                {
                    /* Do the switch */
                    otherSerf.Position = Position;
                    map.SetSerfIndex(otherSerf.Position, (int)otherSerf.Index);
                    otherSerf.Animation = GetWalkingAnimation(
                        (int)map.GetHeight(otherSerf.Position) - (int)map.GetHeight(newPos), dir.Reverse(), true);
                    otherSerf.Counter = CounterFromAnimation[otherSerf.Animation];

                    Animation = GetWalkingAnimation(
                        (int)map.GetHeight(newPos) - (int)map.GetHeight(Position), dir, true);
                    s.Walking.Dir = (int)dir.Reverse();
                }
                else
                {
                    /* Wait for other serf */
                    Animation = 81 + (int)dir;
                    Counter = CounterFromAnimation[Animation];
                    s.Walking.Dir = (int)dir - 6;
                    return;
                }
            }

            if (!altEnd)
                s.Walking.WaitCounter = 0;

            Position = newPos;
            map.SetSerfIndex(Position, (int)Index);
            Counter += CounterFromAnimation[Animation];

            if (altEnd && Counter < 0)
            {
                if (map.HasFlag(newPos))
                {
                    Counter = 0;
                }
                else
                {
                    Log.Debug.Write("serf", "unhandled jump to 31B82.");
                }
            }
        }

        /* Precondition: serf state is in WALKING or TRANSPORTING state */
        void TransporterMoveToFlag(Flag flag)
        {
            Direction dir = (Direction)s.Walking.Dir;

            if (flag.IsScheduled(dir))
            {
                /* Fetch resource from flag */
                s.Walking.WaitCounter = 0;
                uint resIndex = flag.ScheduledSlot(dir);

                if (s.Walking.Res == Resource.Type.None)
                {
                    /* Pick up resource. */
                    flag.PickUpResource(resIndex, ref s.Walking.Res, ref s.Walking.Dest);
                }
                else
                {
                    /* Switch resources and destination. */
                    Resource.Type tempRes = s.Walking.Res;
                    uint tempDest = s.Walking.Dest;

                    flag.PickUpResource(resIndex, ref s.Walking.Res, ref s.Walking.Dest);

                    flag.DropResource(tempRes, tempDest);
                }

                /* Find next resource to be picked up */
                Player player = Game.GetPlayer(Player);
                flag.PrioritizePickup(dir, player);
            }
            else if (s.Walking.Res != Resource.Type.None)
            {
                /* Drop resource at flag */
                if (flag.DropResource(s.Walking.Res, s.Walking.Dest))
                {
                    s.Walking.Res = Resource.Type.None;
                }
            }

            ChangeDirection(dir, true);
        }

        void StartWalking(Direction dir, int slope, bool changePos)
        {
            Map map = Game.Map;
            MapPos newPos = map.Move(Position, dir);
            Animation = GetWalkingAnimation((int)map.GetHeight(newPos) - (int)map.GetHeight(Position), dir, false);
            Counter += (slope * CounterFromAnimation[Animation]) >> 5;

            if (changePos)
            {
                map.SetSerfIndex(Position, 0);
                map.SetSerfIndex(newPos, (int)Index);
            }

            Position = newPos;
        }

        /* Start entering building in direction up-left.
           If joinPos is set the serf is assumed to origin from
           a joined position so the source position will not have it's
           serf index cleared. */
        void EnterBuilding(int fieldB, bool joinPos)
        {
            SetState(State.EnteringBuilding);

            StartWalking(Direction.UpLeft, 32, !joinPos);

            if (joinPos)
                Game.Map.SetSerfIndex(Position, (int)Index);

            Building building = Game.GetBuildingAtPos(Position);
            int slope = RoadBuildingSlope[(int)building.BuildingType];

            if (!building.IsDone())
                slope = 1;

            s.EnteringBuilding.SlopeLength = (slope * Counter) >> 5;
            s.EnteringBuilding.FieldB = fieldB;
        }

        /* Start leaving building by switching to LEAVING BUILDING and
           setting appropriate state. */
        void LeaveBuilding(bool joinPos)
        {
            Building building = Game.GetBuildingAtPos(Position);
            int slope = 31 - RoadBuildingSlope[(int)building.BuildingType];

            if (!building.IsDone())
                slope = 30;

            if (joinPos)
                Game.Map.SetSerfIndex(Position, 0);

            StartWalking(Direction.DownRight, slope, !joinPos);

            SetState(State.LeavingBuilding);

            Game.AddSerfForDrawing(this, Position);
        }

        void EnterInventory()
        {
            Game.Map.SetSerfIndex(Position, 0);
            Building building = Game.GetBuildingAtPos(Position);
            SetState(State.IdleInStock);
            /*serf->s.idleInStock.FieldB = 0;
              serf->s.idleInStock.FieldC = 0;*/
            s.IdleInStock.InvIndex = building.GetInventory().Index;
        }

        void DropResource(Resource.Type resourceType)
        {
            Flag flag = Game.GetFlag(Game.Map.GetObjectIndex(Position));

            /* Resource is lost if no free slot is found */
            if (flag.DropResource(resourceType, 0))
            {
                Game.GetPlayer(Player).IncreaseResourceCount(resourceType);
            }
        }

        void FindInventory()
        {
            Map map = Game.Map;

            if (map.HasFlag(Position))
            {
                Flag flag = Game.GetFlag(map.GetObjectIndex(Position));

                if ((flag.LandPaths() != 0 ||
                     (flag.HasInventory() && flag.AcceptsSerfs())) &&
                      map.GetOwner(Position) == Player)
                {
                    SetState(State.Walking);
                    s.Walking.Dir1 = -2;
                    s.Walking.Dest = 0;
                    s.Walking.Dir = 0;
                    Counter = 0;

                    return;
                }
            }

            SetState(State.Lost);
            s.Lost.FieldB = 0;
            Counter = 0;
        }

        public bool CanPassMapPos(MapPos pos)
        {
            return Map.MapSpaceFromObject[(int)Game.Map.GetObject(pos)] <= Map.Space.Semipassable;
        }

        void SetFightOutcome(Serf attacker, Serf defender)
        {
            /* Calculate "morale" for attacker. */
            uint expFactor = 1u << (attacker.GetSerfType() - Type.Knight0);
            uint landFactor = 0x1000u;

            if (attacker.Player != Game.Map.GetOwner(attacker.Position))
            {
                landFactor = Game.GetPlayer(attacker.Player).GetKnightMorale();
            }

            uint morale = (0x400u * expFactor * landFactor) >> 16;

            /* Calculate "morale" for defender. */
            uint defExpFactor = 1u << (defender.GetSerfType() - Type.Knight0);
            uint defLandFactor = 0x1000u;

            if (defender.Player != Game.Map.GetOwner(defender.Position))
            {
                defLandFactor = Game.GetPlayer(defender.Player).GetKnightMorale();
            }

            uint defMorale = (0x400u * defExpFactor * defLandFactor) >> 16;

            uint playerIndex;
            uint value = 0;
            Type ktype = Type.None;
            uint result = ((morale + defMorale) * Game.RandomInt()) >> 16;

            if (result < morale)
            {
                playerIndex = defender.Player;
                value = defExpFactor;
                ktype = defender.GetSerfType();
                attacker.s.Attacking.AttackerWon = 1;
                Log.Debug.Write("serf", $"Fight: {morale} vs {defMorale} ({result}). Attacker winning.");
            }
            else
            {
                playerIndex = attacker.Player;
                value = expFactor;
                ktype = attacker.GetSerfType();
                attacker.s.Attacking.AttackerWon = 0;
                Log.Debug.Write("serf", $"Fight: {morale} vs {defMorale} ({result}). Defender winning.");
            }

            var player = Game.GetPlayer(playerIndex);

            player.DecreaseMilitaryScore(value);
            player.DecreaseSerfCount(ktype);
            attacker.s.Attacking.Move = Game.RandomInt() & 0x70;
        }

        static bool HandleSerfWalkingStateSearchCB(Flag flag, object data)
        {
            Serf serf = data as Serf;
            Flag dest = flag.Game.GetFlag(serf.s.Walking.Dest);

            if (flag == dest)
            {
                Log.Verbose.Write("serf", " dest found: " + dest.SearchDir);
                serf.ChangeDirection(dest.SearchDir, false);
                return true;
            }

            return false;
        }

        void HandleSerfIdleInStockState()
        {
            Inventory inventory = Game.GetInventory(s.IdleInStock.InvIndex);

            if (inventory.GetSerfMode() == 0
                || inventory.GetSerfMode() == Inventory.Mode.Stop /* in, stop */
                || inventory.GetSerfQueueLength() >= 3)
            {
                switch (GetSerfType())
                {
                    case Type.Knight0:
                        inventory.KnightTraining(this, 4000);
                        break;
                    case Type.Knight1:
                        inventory.KnightTraining(this, 2000);
                        break;
                    case Type.Knight2:
                        inventory.KnightTraining(this, 1000);
                        break;
                    case Type.Knight3:
                        inventory.KnightTraining(this, 500);
                        break;
                    case Type.Smelter: /* TODO ??? */
                        break;
                    default:
                        inventory.SerfIdleInStock(this);
                        break;
                }
            }
            else
            { /* out */
                inventory.CallOutSerf(this);

                SetState(State.ReadyToLeaveInventory);
                s.ReadyToLeaveInventory.Mode = -3;
                s.ReadyToLeaveInventory.InvIndex = inventory.Index;
                /* TODO immediate switch to next state. */
            }
        }

        void HandleSerfWalkingStateDestReached()
        {
            /* Destination reached. */
            if (s.Walking.Dir1 < 0)
            {
                Map map = Game.Map;
                Building building = Game.GetBuildingAtPos(map.MoveUpLeft(Position));
                building.RequestedSerfReached(this); // TODO: null ref exception

                if (map.HasSerf(map.MoveUpLeft(Position)))
                {
                    Animation = 85;
                    Counter = 0;
                    SetState(State.ReadyToEnter);
                }
                else
                {
                    EnterBuilding(s.Walking.Dir1, false);
                }
            }
            else if (s.Walking.Dir1 == 6)
            {
                SetState(State.LookingForGeoSpot);
                Counter = 0;
            }
            else
            {
                Flag flag = Game.GetFlagAtPos(Position);

                if (flag == null)
                {
                    throw new ExceptionFreeserf("Flag expected as destination of walking serf.");
                }

                Direction dir = (Direction)s.Walking.Dir1;
                Flag otherFlag = flag.GetOtherEndFlag(dir);

                if (otherFlag == null)
                {
                    throw new ExceptionFreeserf("Path has no other end flag in selected dir.");
                }

                Direction otherDir = flag.GetOtherEndDir(dir);

                /* Increment transport serf count */
                flag.CompleteSerfRequest(dir);
                otherFlag.CompleteSerfRequest(otherDir);

                SetState(State.Transporting);
                s.Walking.Res = Resource.Type.None;
                s.Walking.Dir = (int)dir;
                s.Walking.Dir1 = 0;
                s.Walking.WaitCounter = 0;

                TransporterMoveToFlag(flag);
            }
        }

        void HandleSerfWalkingStateWaiting()
        {
            /* Waiting for other serf. */
            Direction dir = (Direction)(s.Walking.Dir + 6);

            Map map = Game.Map;
            /* Only check for loops once in a while. */
            ++s.Walking.WaitCounter;

            if ((!map.HasFlag(Position) && s.Walking.WaitCounter >= 10) ||
                s.Walking.WaitCounter >= 50)
            {
                MapPos pos = Position;

                /* Follow the chain of serfs waiting for each other and
                   see if there is a loop. */
                for (int i = 0; i < 100; ++i)
                {
                    pos = map.Move(pos, dir);

                    if (!map.HasSerf(pos))
                    {
                        break;
                    }
                    else if (map.GetSerfIndex(pos) == Index)
                    {
                        /* We have found a loop, try a different direction. */
                        ChangeDirection(dir.Reverse(), false);
                        return;
                    }

                    /* Get next serf and follow the chain */
                    Serf otherSerf = Game.GetSerfAtPos(Position);

                    if (otherSerf.SerfState != State.Walking &&
                        otherSerf.SerfState != State.Transporting)
                    {
                        break;
                    }

                    if (otherSerf.s.Walking.Dir >= 0 ||
                        (otherSerf.s.Walking.Dir + 6) == (int)dir.Reverse())
                    {
                        break;
                    }

                    dir = (Direction)(otherSerf.s.Walking.Dir + 6);
                }
            }

            /* Stick to the same direction */
            s.Walking.WaitCounter = 0;
            ChangeDirection((Direction)(s.Walking.Dir + 6), false);
        }

        void HandleSerfWalkingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                if (s.Walking.Dir < 0)
                {
                    HandleSerfWalkingStateWaiting();
                    continue;
                }

                /* 301F0 */
                if (Game.Map.HasFlag(Position))
                {
                    /* Serf has reached a flag.
                       Search for a destination if none is known. */
                    if (s.Walking.Dest == 0)
                    {
                        uint flagIndex = Game.Map.GetObjectIndex(Position);
                        Flag src = Game.GetFlag(flagIndex);
                        int r = src.FindNearestInventoryForSerf();

                        if (r < 0)
                        {
                            SetState(State.Lost);
                            s.Lost.FieldB = 1;
                            Counter = 0;

                            return;
                        }

                        s.Walking.Dest = (uint)r;
                    }

                    /* Check whether destination has been reached.
                       If not, find out which direction to move in
                       to reach the destination. */
                    if (s.Walking.Dest == Game.Map.GetObjectIndex(Position))
                    {
                        HandleSerfWalkingStateDestReached();
                        return;
                    }
                    else
                    {
                        Flag src = Game.GetFlagAtPos(Position);
                        FlagSearch search = new FlagSearch(Game);
                        var cycle = DirectionCycleCCW.CreateDefault();

                        foreach (Direction i in cycle)
                        {
                            if (!src.IsWaterPath(i))
                            {
                                Flag otherFlag = src.GetOtherEndFlag(i);
                                otherFlag.SearchDir = i;
                                search.AddSource(otherFlag);
                            }
                        }

                        if (search.Execute(HandleSerfWalkingStateSearchCB, true, false, this))
                            continue;
                    }
                }
                else
                {
                    /* 30A37 */
                    /* Serf is not at a flag. Just follow the road. */
                    uint paths = Game.Map.Paths(Position) & (byte)~Misc.BitU(s.Walking.Dir);
                    Direction dir = Direction.None;
                    var cycle = DirectionCycleCW.CreateDefault();

                    foreach (Direction d in cycle)
                    {
                        if (paths == Misc.BitU((int)d))
                        {
                            dir = d;
                            break;
                        }
                    }

                    if (dir >= 0)
                    {
                        ChangeDirection(dir, false);
                        continue;
                    }

                    Counter = 0;
                }

                /* Either the road is a dead end; or
                   we are at a flag, but the flag search for
                   the destination failed. */
                if (s.Walking.Dir1 < 0)
                {
                    if (s.Walking.Dir1 < -1)
                    {
                        SetState(State.Lost);
                        s.Lost.FieldB = 1;
                        Counter = 0;

                        return;
                    }

                    Flag flag = Game.GetFlag(s.Walking.Dest);
                    Building building = flag.GetBuilding();

                    building.RequestedSerfLost();
                }
                else if (s.Walking.Dir1 != 6)
                {
                    Flag flag = Game.GetFlag(s.Walking.Dest);
                    Direction d = (Direction)s.Walking.Dir1;

                    flag.CancelSerfRequest(d);
                    flag.GetOtherEndFlag(d).CancelSerfRequest(flag.GetOtherEndDir(d));
                }

                s.Walking.Dir1 = -2;
                s.Walking.Dest = 0;
                Counter = 0;
            }
        }

        void HandleSerfTransportingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter >= 0)
                return;

            if (s.Walking.Dir < 0)
            {
                ChangeDirection((Direction)(s.Walking.Dir + 6), true);
            }
            else
            {
                Map map = Game.Map;

                /* 31549 */
                if (map.HasFlag(Position))
                {
                    /* Current position occupied by waiting transporter */
                    if (s.Walking.WaitCounter < 0)
                    {
                        SetState(State.Walking);
                        s.Walking.WaitCounter = 0;
                        s.Walking.Dir1 = -2;
                        s.Walking.Dest = 0;
                        Counter = 0;

                        return;
                    }

                    /* 31590 */
                    if (s.Walking.Res != Resource.Type.None &&
                      map.GetObjectIndex(Position) == s.Walking.Dest)
                    {
                        /* At resource destination */
                        SetState(State.Delivering);
                        s.Walking.WaitCounter = 0;

                        MapPos newPos = map.MoveUpLeft(Position);
                        Animation = 3 + (int)map.GetHeight(newPos) - (int)map.GetHeight(Position) +
                                    ((int)Direction.UpLeft + 6) * 9;
                        Counter = CounterFromAnimation[Animation];
                        /* TODO next call is actually into the middle of
                           handleSerfDeliveringState().
                           Why is a nice and clean state switch not enough???
                           Just ignore this call and we'll be safe, I think... */
                        /* handleSerfDeliveringState(serf); */
                        return;
                    }

                    Flag flag = Game.GetFlagAtPos(Position);
                    TransporterMoveToFlag(flag);
                }
                else
                {
                    uint paths = map.Paths(Position) & (byte)~Misc.BitU(s.Walking.Dir);
                    Direction dir = Direction.None;
                    var cycle = DirectionCycleCW.CreateDefault();

                    foreach (Direction d in cycle)
                    {
                        if (paths == Misc.BitU((int)d))
                        {
                            dir = d;
                            break;
                        }
                    }

                    if (dir < 0)
                    {
                        SetState(State.Lost);
                        Counter = 0;

                        return;
                    }

                    if (!map.HasFlag(map.Move(Position, dir)) ||
                        s.Walking.Res != Resource.Type.None ||
                        s.Walking.WaitCounter < 0)
                    {
                        ChangeDirection(dir, true);
                        return;
                    }

                    Flag flag = Game.GetFlagAtPos(map.Move(Position, dir));
                    Direction revDir = dir.Reverse();
                    Flag otherFlag = flag.GetOtherEndFlag(revDir);
                    Direction otherDir = flag.GetOtherEndDir(revDir);

                    if (flag.IsScheduled(revDir))
                    {
                        ChangeDirection(dir, true);
                        return;
                    }

                    Animation = 110 + s.Walking.Dir;
                    Counter = CounterFromAnimation[Animation];
                    s.Walking.Dir -= 6;

                    if (flag.FreeTransporterCount(revDir) > 1)
                    {
                        ++s.Walking.WaitCounter;

                        if (s.Walking.WaitCounter > 3)
                        {
                            flag.TransporterToServe(revDir);
                            otherFlag.TransporterToServe(otherDir);
                            s.Walking.WaitCounter = -1;
                        }
                    }
                    else
                    {
                        if (!otherFlag.IsScheduled(otherDir))
                        {
                            /* TODO Don't use anim as state var */
                            tick = (ushort)((tick & 0xff00) | (s.Walking.Dir & 0xff));
                            SetState(State.IdleOnPath);
                            s.IdleOnPath.RevDir = revDir;
                            s.IdleOnPath.FlagIndex = flag.Index;
                            map.SetIdleSerf(Position);
                            map.SetSerfIndex(Position, 0);

                            return;
                        }
                    }
                }
            }
        }

        void HandleSerfEnteringBuildingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0 || Counter <= s.EnteringBuilding.SlopeLength)
            {
                if (Game.Map.GetObjectIndex(Position) == 0 ||
                    Game.GetBuildingAtPos(Position).IsBurning())
                {
                    /* Burning */
                    SetState(State.Lost);
                    s.Lost.FieldB = 0;
                    Counter = 0;

                    return;
                }

                Counter = s.EnteringBuilding.SlopeLength;
                Map map = Game.Map;

                switch (GetSerfType())
                {
                    case Type.Transporter:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);
                            uint flagIndex = map.GetObjectIndex(map.MoveDownRight(Position));
                            Flag flag = Game.GetFlag(flagIndex);

                            /* Mark as inventory accepting resources and serfs. */
                            flag.SetHasInventory();
                            flag.SetAcceptsResources(true);
                            flag.SetAcceptsSerfs(true);

                            SetState(State.WaitForResourceOut);
                            Counter = 63;
                            SetSerfType(Type.TransporterInventory);
                        }
                        break;
                    case Type.Sailor:
                        EnterInventory();
                        break;
                    case Type.Digger:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            SetState(State.Digging);
                            s.Digging.HIndex = 15;

                            Building building = Game.GetBuildingAtPos(Position);
                            s.Digging.DigPos = 6;
                            s.Digging.TargetH = building.GetLevel();
                            s.Digging.Substate = 1;
                        }
                        break;
                    case Type.Builder:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            SetState(State.Building);
                            Animation = 98;
                            Counter = 127;
                            s.Building.Mode = 1;
                            s.Building.Index = map.GetObjectIndex(Position);
                            s.Building.MaterialStep = 0;

                            Building building = Game.GetBuilding(s.Building.Index);

                            switch (building.BuildingType)
                            {
                                case Building.Type.Stock:
                                case Building.Type.Sawmill:
                                case Building.Type.ToolMaker:
                                case Building.Type.Fortress:
                                    s.Building.MaterialStep |= Misc.BitU(7);
                                    Animation = 100;
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;
                    case Type.TransporterInventory:
                        map.SetSerfIndex(Position, 0);
                        SetState(State.WaitForResourceOut);
                        Counter = 63;
                        break;
                    case Type.Lumberjack:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);
                            SetState(State.PlanningLogging);
                        }
                        break;
                    case Type.Sawmiller:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);

                            if (s.EnteringBuilding.FieldB != 0)
                            {
                                Building building = Game.GetBuildingAtPos(Position);
                                uint flagIndex = map.GetObjectIndex(map.MoveDownRight(Position));
                                Flag flag = Game.GetFlag(flagIndex);
                                flag.ClearFlags();
                                building.StockInit(0, Resource.Type.Lumber, 8);
                            }

                            SetState(State.Sawing);
                            s.Sawing.Mode = 0;
                        }
                        break;
                    case Type.Stonecutter:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);
                            SetState(State.PlanningStoneCutting);
                        }
                        break;
                    case Type.Forester:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);
                            SetState(State.PlanningPlanting);
                        }
                        break;
                    case Type.Miner:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);
                            Building building = Game.GetBuildingAtPos(Position);
                            Building.Type buildingType = building.BuildingType;

                            if (s.EnteringBuilding.FieldB != 0)
                            {
                                building.StartActivity();
                                building.StopPlayingSfx();

                                Flag flag = Game.GetFlagAtPos(map.MoveDownRight(Position));
                                flag.ClearFlags();
                                building.StockInit(0, Resource.Type.GroupFood, 8);
                            }

                            SetState(State.Mining);
                            s.Mining.Substate = 0;
                            s.Mining.Deposit = (Map.Minerals)(4 - (buildingType - Building.Type.StoneMine));
                            /*s.Mining.FieldC = 0;*/
                            s.Mining.Res = 0;
                        }
                        break;
                    case Type.Smelter:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);

                            Building building = Game.GetBuildingAtPos(Position);

                            if (s.EnteringBuilding.FieldB != 0)
                            {
                                Flag flag = Game.GetFlagAtPos(map.MoveDownRight(Position));
                                flag.ClearFlags();
                                building.StockInit(0, Resource.Type.Coal, 8);

                                if (building.BuildingType == Building.Type.SteelSmelter)
                                {
                                    building.StockInit(1, Resource.Type.IronOre, 8);
                                }
                                else
                                {
                                    building.StockInit(1, Resource.Type.GoldOre, 8);
                                }
                            }

                            /* Switch to smelting state to begin work. */
                            SetState(State.Smelting);

                            if (building.BuildingType == Building.Type.SteelSmelter)
                            {
                                s.Smelting.Type = 0;
                            }
                            else
                            {
                                s.Smelting.Type = -1;
                            }

                            s.Smelting.Mode = 0;
                        }
                        break;
                    case Type.Fisher:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);
                            SetState(State.PlanningFishing);
                        }
                        break;
                    case Type.PigFarmer:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);

                            if (s.EnteringBuilding.FieldB != 0)
                            {
                                Building building = Game.GetBuildingAtPos(Position);
                                Flag flag = Game.GetFlagAtPos(map.MoveDownRight(Position));

                                building.SetInitialResourcesInStock(1, 1);

                                flag.ClearFlags();
                                building.StockInit(0, Resource.Type.Wheat, 8);

                                SetState(State.PigFarming);
                                s.PigFarming.Mode = 0;
                            }
                            else
                            {
                                SetState(State.PigFarming);
                                s.PigFarming.Mode = 6;
                                Counter = 0;
                            }
                        }
                        break;
                    case Type.Butcher:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);

                            if (s.EnteringBuilding.FieldB != 0)
                            {
                                Building building = Game.GetBuildingAtPos(Position);
                                Flag flag = Game.GetFlagAtPos(map.MoveDownRight(Position));
                                flag.ClearFlags();
                                building.StockInit(0, Resource.Type.Pig, 8);
                            }

                            SetState(State.Butchering);
                            s.Butchering.Mode = 0;
                        }
                        break;
                    case Type.Farmer:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);
                            SetState(State.PlanningFarming);
                        }
                        break;
                    case Type.Miller:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);

                            if (s.EnteringBuilding.FieldB != 0)
                            {
                                Building building = Game.GetBuildingAtPos(Position);
                                Flag flag = Game.GetFlagAtPos(map.MoveDownRight(Position));
                                flag.ClearFlags();
                                building.StockInit(0, Resource.Type.Wheat, 8);
                            }

                            SetState(State.Milling);
                            s.Milling.Mode = 0;
                        }
                        break;
                    case Type.Baker:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);

                            if (s.EnteringBuilding.FieldB != 0)
                            {
                                Building building = Game.GetBuildingAtPos(Position);
                                Flag flag = Game.GetFlagAtPos(map.MoveDownRight(Position));
                                flag.ClearFlags();
                                building.StockInit(0, Resource.Type.Flour, 8);
                            }

                            SetState(State.Baking);
                            s.Baking.Mode = 0;
                        }
                        break;
                    case Type.BoatBuilder:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);

                            if (s.EnteringBuilding.FieldB != 0)
                            {
                                Building building = Game.GetBuildingAtPos(Position);
                                Flag flag = Game.GetFlagAtPos(map.MoveDownRight(Position));
                                flag.ClearFlags();
                                building.StockInit(0, Resource.Type.Plank, 8);
                            }

                            SetState(State.BuildingBoat);
                            s.BuildingBoat.Mode = 0;
                        }
                        break;
                    case Type.Toolmaker:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);

                            if (s.EnteringBuilding.FieldB != 0)
                            {
                                Building building = Game.GetBuildingAtPos(Position);
                                Flag flag = Game.GetFlagAtPos(map.MoveDownRight(Position));
                                flag.ClearFlags();
                                building.StockInit(0, Resource.Type.Plank, 8);
                                building.StockInit(1, Resource.Type.Steel, 8);
                            }

                            SetState(State.MakingTool);
                            s.MakingTool.Mode = 0;
                        }
                        break;
                    case Type.WeaponSmith:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);

                            if (s.EnteringBuilding.FieldB != 0)
                            {
                                Building building = Game.GetBuildingAtPos(Position);
                                Flag flag = Game.GetFlagAtPos(map.MoveDownRight(Position));
                                flag.ClearFlags();
                                building.StockInit(0, Resource.Type.Coal, 8);
                                building.StockInit(1, Resource.Type.Steel, 8);
                            }

                            SetState(State.MakingWeapon);
                            s.MakingWeapon.Mode = 0;
                        }
                        break;
                    case Type.Geologist:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            SetState(State.LookingForGeoSpot); /* TODO Should never be reached */
                            Counter = 0;
                        }
                        break;
                    case Type.Generic:
                        {
                            map.SetSerfIndex(Position, 0);

                            Building building = Game.GetBuildingAtPos(Position);
                            Inventory inventory = building.GetInventory();

                            if (inventory == null)
                            {
                                throw new ExceptionFreeserf("Not inventory.");
                            }

                            inventory.SerfComeBack();

                            SetState(State.IdleInStock);
                            s.IdleInStock.InvIndex = inventory.Index;
                            break;
                        }
                    case Type.Knight0:
                    case Type.Knight1:
                    case Type.Knight2:
                    case Type.Knight3:
                    case Type.Knight4:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            Building building = Game.GetBuildingAtPos(Position);

                            if (building.IsBurning())
                            {
                                SetState(State.Lost);
                                Counter = 0;
                            }
                            else
                            {
                                map.SetSerfIndex(Position, 0);

                                if (building.HasInventory())
                                {
                                    SetState(State.DefendingCastle);
                                    Counter = 6000;

                                    /* Prepend to knight list */
                                    s.Defending.NextKnight = building.GetFirstKnight();
                                    building.SetFirstKnight(Index);

                                    Game.GetPlayer(building.Player).IncreaseCastleKnights();

                                    return;
                                }

                                building.RequestedKnightArrived();

                                State nextState = State.Invalid;

                                switch (building.BuildingType)
                                {
                                    case Building.Type.Hut:
                                        nextState = State.DefendingHut;
                                        break;
                                    case Building.Type.Tower:
                                        nextState = State.DefendingTower;
                                        break;
                                    case Building.Type.Fortress:
                                        nextState = State.DefendingFortress;
                                        break;
                                    default:
                                        Debug.NotReached();
                                        break;
                                }

                                /* Switch to defending state */
                                SetState(nextState);
                                Counter = 6000;

                                /* Prepend to knight list */
                                s.Defending.NextKnight = building.GetFirstKnight();
                                building.SetFirstKnight(Index);
                            }
                        }
                        break;
                    default:
                        Debug.NotReached();
                        break;
                }
            }
        }

        void HandleSerfLeavingBuildingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                Counter = 0;
                SetState(s.LeavingBuilding.NextState);

                /* Set FieldF to 0, do this for individual states if necessary */
                if (SerfState == State.Walking)
                {
                    int mode = s.LeavingBuilding.FieldB;
                    uint dest = s.LeavingBuilding.Dest;
                    s.Walking.Dir1 = mode;
                    s.Walking.Dest = dest;
                    s.Walking.WaitCounter = 0;
                }
                else if (SerfState == State.DropResourceOut)
                {
                    uint res = (uint)s.LeavingBuilding.FieldB;
                    uint resDest = s.LeavingBuilding.Dest;
                    s.MoveResourceOut.Res = res;
                    s.MoveResourceOut.ResDest = resDest;
                }
                else if (SerfState == State.FreeWalking ||
                         SerfState == State.KnightFreeWalking ||
                         SerfState == State.StoneCutterFreeWalking)
                {
                    int dist1 = s.LeavingBuilding.FieldB;
                    int dist2 = (int)s.LeavingBuilding.Dest;
                    int negDist1 = s.LeavingBuilding.Dest2;
                    int negDist2 = s.LeavingBuilding.Dir;
                    s.FreeWalking.Dist1 = dist1;
                    s.FreeWalking.Dist2 = dist2;
                    s.FreeWalking.NegDist1 = negDist1;
                    s.FreeWalking.NegDist2 = negDist2;
                    s.FreeWalking.Flags = 0;
                }
                else if (SerfState == State.KnightPrepareDefending ||
                         SerfState == State.Scatter)
                {
                    /* No state. */
                }
                else
                {
                    Log.Debug.Write("serf", "unhandled next state when leaving building.");
                }
            }
        }

        void HandleSerfReadyToEnterState()
        {
            MapPos newPos = Game.Map.MoveUpLeft(Position);

            if (Game.Map.HasSerf(newPos))
            {
                Animation = 85;
                Counter = 0;

                return;
            }

            EnterBuilding(s.ReadyToEnter.FieldB, false);
        }

        void HandleSerfReadyToLeaveState()
        {
            tick = Game.Tick;
            Counter = 0;

            Map map = Game.Map;
            MapPos newPos = map.MoveDownRight(Position);

            if ((map.GetSerfIndex(Position) != Index && map.HasSerf(Position)) || map.HasSerf(newPos))
            {
                Animation = 82;
                Counter = 0;

                return;
            }

            LeaveBuilding(false);
        }

        static readonly int[] DiggingHDiff = new int[]
        {
            -1, 1, -2, 2, -3, 3, -4, 4,
            -5, 5, -6, 6, -7, 7, -8, 8
        };

        void HandleSerfDiggingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;

            while (Counter < 0)
            {
                --s.Digging.Substate;

                if (s.Digging.Substate < 0)
                {
                    Log.Verbose.Write("serf", "substate -1: wait for serf.");

                    int d = s.Digging.DigPos;
                    Direction dir = (d == 0) ? Direction.Up : (Direction)(6 - d);
                    MapPos newPos = map.Move(Position, dir);

                    if (map.HasSerf(newPos))
                    {
                        Serf otherSerf = Game.GetSerfAtPos(newPos);
                        Direction otherDir = Direction.None;

                        if (otherSerf.IsWaiting(ref otherDir) &&
                            otherDir == dir.Reverse() &&
                            otherSerf.SwitchWaiting(otherDir))
                        {
                            /* Do the switch */
                            otherSerf.Position = Position;
                            map.SetSerfIndex(otherSerf.Position, (int)otherSerf.Index);
                            otherSerf.Animation = GetWalkingAnimation(
                                (int)map.GetHeight(otherSerf.Position) - (int)map.GetHeight(newPos), dir.Reverse(), true);
                            otherSerf.Counter = CounterFromAnimation[otherSerf.Animation];

                            if (d != 0)
                            {
                                Animation = GetWalkingAnimation((int)map.GetHeight(newPos) - (int)map.GetHeight(Position), dir, true);
                            }
                            else
                            {
                                Animation = (int)map.GetHeight(newPos) - (int)map.GetHeight(Position);
                            }
                        }
                        else
                        {
                            Counter = 127;
                            s.Digging.Substate = 0;

                            return;
                        }
                    }
                    else
                    {
                        map.SetSerfIndex(Position, 0);

                        if (d != 0)
                        {
                            Animation = GetWalkingAnimation((int)map.GetHeight(newPos) - (int)map.GetHeight(Position), dir, false);
                        }
                        else
                        {
                            Animation = (int)map.GetHeight(newPos) - (int)map.GetHeight(Position);
                        }
                    }

                    map.SetSerfIndex(newPos, (int)Index);
                    Position = newPos;
                    s.Digging.Substate = 3;
                    Counter += CounterFromAnimation[Animation];
                }
                else if (s.Digging.Substate == 1)
                {
                    /* 34CD6: Change height, head back to center */
                    int h = (int)map.GetHeight(Position);
                    h += ((s.Digging.HIndex & 1) != 0) ? -1 : 1;
                    Log.Verbose.Write("serf", "substate 1: change height " + (((s.Digging.HIndex & 1) != 0) ? "down." : "up."));
                    map.SetHeight(Position, (uint)h);

                    if (s.Digging.DigPos == 0)
                    {
                        s.Digging.Substate = 1;
                    }
                    else
                    {
                        Direction dir = ((Direction)(6 - s.Digging.DigPos)).Reverse();
                        StartWalking(dir, 32, true);
                    }
                }
                else if (s.Digging.Substate > 1)
                {
                    Log.Verbose.Write("serf", "substate 2: dig.");
                    /* 34E89 */
                    Animation = 88 - (s.Digging.HIndex & 1);
                    Counter += 383;
                }
                else
                {
                    /* 34CDC: Looking for a place to dig */
                    Log.Verbose.Write("serf", $"substate 0: looking for place to dig {s.Digging.DigPos}, {s.Digging.HIndex}");

                    do
                    {
                        int h = DiggingHDiff[s.Digging.HIndex] + (int)s.Digging.TargetH;

                        if (s.Digging.DigPos >= 0 && h >= 0 && h < 32)
                        {
                            if (s.Digging.DigPos == 0)
                            {
                                int height = (int)map.GetHeight(Position);

                                if (height != h)
                                {
                                    --s.Digging.DigPos;
                                    continue;
                                }

                                /* Dig here */
                                s.Digging.Substate = 2;
                                Animation = 87 + (s.Digging.HIndex & 1);
                                Counter += 383;
                            }
                            else
                            {
                                Direction dir = (Direction)(6 - s.Digging.DigPos);
                                MapPos newPos = map.Move(Position, dir);
                                int newHeight = (int)map.GetHeight(newPos);

                                if (newHeight != h)
                                {
                                    --s.Digging.DigPos;
                                    continue;
                                }

                                Log.Verbose.Write("serf", $"  found at: {s.Digging.DigPos}.");

                                /* Digging spot found */
                                if (map.HasSerf(newPos))
                                {
                                    /* Occupied by other serf, wait */
                                    s.Digging.Substate = 0;
                                    Animation = 87 - s.Digging.DigPos;
                                    Counter = CounterFromAnimation[Animation];

                                    return;
                                }

                                /* Go to dig there */
                                StartWalking(dir, 32, true);
                                s.Digging.Substate = 3;
                            }

                            break;
                        }

                        s.Digging.DigPos = 6;
                        --s.Digging.HIndex;

                    } while (s.Digging.HIndex >= 0);

                    if (s.Digging.HIndex < 0)
                    {
                        /* Done Digging */
                        Building building = Game.GetBuilding(map.GetObjectIndex(Position));
                        building.DoneLeveling();
                        SetState(State.ReadyToLeave);
                        s.LeavingBuilding.Dest = 0;
                        s.LeavingBuilding.FieldB = -2;
                        s.LeavingBuilding.Dir = 0;
                        s.LeavingBuilding.NextState = State.Walking;
                        HandleSerfReadyToLeaveState();  // TODO(jonls): why isn't a state switch enough?

                        return;
                    }
                }
            }
        }

        static readonly int[] MaterialOrder = new int[]
        {
            0, 0, 0, 0, 0, 4, 0, 0,
            0, 0, 0x38, 2, 8, 2, 8, 4,
            4, 0xc, 0x14, 0x2c, 2, 0x1c, 0x1f0, 4,
            0, 0, 0, 0, 0, 0, 0, 0
        };

        void HandleSerfBuildingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                Building building = Game.GetBuilding(s.Building.Index);

                if (s.Building.Mode < 0)
                {
                    if (building.BuildProgress())
                    {
                        Counter = 0;
                        SetState(State.FinishedBuilding);

                        return;
                    }

                    --s.Building.Counter;

                    if (s.Building.Counter == 0)
                    {
                        s.Building.Mode = 1;
                        Animation = 98;

                        if (Misc.BitTest(s.Building.MaterialStep, 7))
                            Animation = 100;

                        /* 353A5 */
                        int materialStep = (int)s.Building.MaterialStep & 0xf;

                        if (!Misc.BitTest(MaterialOrder[(int)building.BuildingType], materialStep))
                        {
                            /* Planks */
                            if (building.GetResourceCountInStock(0) == 0)
                            {
                                Counter += 256;

                                if (Counter < 0)
                                    Counter = 255;

                                return;
                            }

                            building.PlankUsedForBuild();
                        }
                        else
                        {
                            /* Stone */
                            if (building.GetResourceCountInStock(1) == 0)
                            {
                                Counter += 256;

                                if (Counter < 0)
                                    Counter = 255;

                                return;
                            }

                            building.StoneUsedForBuild();
                        }

                        ++s.Building.MaterialStep;
                        s.Building.Counter = 8;
                        s.Building.Mode = -1;
                    }
                }
                else
                {
                    if (s.Building.Mode == 0)
                    {
                        s.Building.Mode = 1;
                        Animation = 98;

                        if (Misc.BitTest(s.Building.MaterialStep, 7))
                            Animation = 100;
                    }

                    /* 353A5: Duplicate code */
                    int materialStep = (int)s.Building.MaterialStep & 0xf;

                    if (!Misc.BitTest(MaterialOrder[(int)building.BuildingType], materialStep))
                    {
                        /* Planks */
                        if (building.GetResourceCountInStock(0) == 0)
                        {
                            Counter += 256;

                            if (Counter < 0)
                                Counter = 255;

                            return;
                        }

                        building.PlankUsedForBuild();
                    }
                    else
                    {
                        /* Stone */
                        if (building.GetResourceCountInStock(1) == 0)
                        {
                            Counter += 256;

                            if (Counter < 0)
                                Counter = 255;

                            return;
                        }

                        building.StoneUsedForBuild();
                    }

                    ++s.Building.MaterialStep;
                    s.Building.Counter = 8;
                    s.Building.Mode = -1;
                }

                int random = (Game.RandomInt() & 3) + 102;

                if (Misc.BitTest(s.Building.MaterialStep, 7))
                    random += 4;

                Animation = random;
                Counter += CounterFromAnimation[Animation];
            }
        }

        void HandleSerfBuildingCastleState()
        {
            tick = Game.Tick;

            Inventory inventory = Game.GetInventory(s.BuildingCastle.InvIndex);
            Building building = Game.GetBuilding(inventory.GetBuildingIndex());

            if (building.BuildProgress())
            {
                /* Finished */
                Game.Map.SetSerfIndex(Position, 0);
                SetState(State.WaitForResourceOut);
            }
        }

        void HandleSerfMoveResourceOutState()
        {
            tick = Game.Tick;
            Counter = 0;

            Map map = Game.Map;
            if ((map.GetSerfIndex(Position) != Index && map.HasSerf(Position)) || map.HasSerf(map.MoveDownRight(Position)))
            {
                /* Occupied by serf, wait */
                Animation = 82;
                Counter = 0;

                return;
            }

            Flag flag = Game.GetFlagAtPos(map.MoveDownRight(Position));

            if (!flag.HasEmptySlot())
            {
                /* All resource slots at flag are occupied, wait */
                Animation = 82;
                Counter = 0;

                return;
            }

            uint res = s.MoveResourceOut.Res;
            uint resDest = s.MoveResourceOut.ResDest;
            State nextState = s.MoveResourceOut.NextState;

            LeaveBuilding(false);

            s.LeavingBuilding.NextState = nextState;
            s.LeavingBuilding.FieldB = (int)res;
        }

        void HandleSerfWaitForResourceOutState()
        {
            if (Counter != 0)
            {
                ushort delta = (ushort)(Game.Tick - tick);
                tick = Game.Tick;
                Counter -= delta;

                if (Counter >= 0)
                    return;

                Counter = 0;
            }

            uint objIndex = Game.Map.GetObjectIndex(Position);
            Building building = Game.GetBuilding(objIndex);
            Inventory inventory = building.GetInventory();

            if (inventory.GetSerfQueueLength() > 0 ||
                !inventory.HasResourceInQueue())
            {
                return;
            }

            SetState(State.MoveResourceOut);
            Resource.Type res = Resource.Type.None;
            uint dest = 0;

            inventory.GetResourceFromQueue(ref res, ref dest);
            s.MoveResourceOut.Res = (uint)(res + 1);
            s.MoveResourceOut.ResDest = dest;
            s.MoveResourceOut.NextState = State.DropResourceOut;

            /* why isn't a state switch enough? */
            /*HandleSerfMoveResourceOutState(serf);*/
        }

        void HandleSerfDropResourceOutState()
        {
            Flag flag = Game.GetFlag(Game.Map.GetObjectIndex(Position));

            if (!flag.DropResource((Resource.Type)(s.MoveResourceOut.Res - 1), s.MoveResourceOut.ResDest))
            {
                throw new ExceptionFreeserf("Failed to drop resource.");
            }

            SetState(State.ReadyToEnter);
            s.ReadyToEnter.FieldB = 0;
        }

        void HandleSerfDeliveringState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                if (s.Walking.WaitCounter != 0)
                {
                    SetState(State.Transporting);
                    s.Walking.WaitCounter = 0;

                    Flag flag = Game.GetFlag(Game.Map.GetObjectIndex(Position));

                    TransporterMoveToFlag(flag);

                    return;
                }

                if (s.Walking.Res != Resource.Type.None)
                {
                    Resource.Type res = s.Walking.Res;
                    s.Walking.Res = Resource.Type.None;
                    Building building = Game.GetBuildingAtPos(Game.Map.MoveUpLeft(Position));
                    building.RequestedResourceDelivered(res);
                }

                Animation = 4 + 9 - (Animation - (3 + 10 * 9));
                s.Walking.WaitCounter = -s.Walking.WaitCounter - 1;
                Counter += CounterFromAnimation[Animation] >> 1;
            }
        }

        void HandleSerfReadyToLeaveInventoryState()
        {
            tick = Game.Tick;
            Counter = 0;

            Map map = Game.Map;
            if (map.HasSerf(Position) || map.HasSerf(map.MoveDownRight(Position)))
            {
                Animation = 82;
                Counter = 0;
                return;
            }

            if (s.ReadyToLeaveInventory.Mode == -1)
            {
                Flag flag = Game.GetFlag(s.ReadyToLeaveInventory.Dest);

                if (flag.HasBuilding())
                {
                    Building building = flag.GetBuilding();

                    if (map.HasSerf(building.Position))
                    {
                        Animation = 82;
                        Counter = 0;

                        return;
                    }
                }
            }

            Inventory inventory = Game.GetInventory(s.ReadyToLeaveInventory.InvIndex);

            inventory.SerfAway();

            State nextState = State.Walking;
            int mode = s.ReadyToLeaveInventory.Mode;

            if (mode == -3)
            {
                nextState = State.Scatter;
            }

            uint dest = s.ReadyToLeaveInventory.Dest;

            LeaveBuilding(false);
            s.LeavingBuilding.NextState = nextState;
            s.LeavingBuilding.FieldB = mode;
            s.LeavingBuilding.Dest = dest;
            s.LeavingBuilding.Dir = 0;
        }

        void HandleSerfFreeWalkingStateDestReached()
        {
            if (s.FreeWalking.NegDist1 == -128 && s.FreeWalking.NegDist2 < 0)
            {
                FindInventory();
                return;
            }

            Map map = Game.Map;

            switch (GetSerfType())
            {
                case Type.Lumberjack:
                    if (s.FreeWalking.NegDist1 == -128)
                    {
                        if (s.FreeWalking.NegDist2 > 0)
                        {
                            DropResource(Resource.Type.Lumber);
                        }

                        SetState(State.ReadyToEnter);
                        s.ReadyToEnter.FieldB = 0;
                        Counter = 0;
                    }
                    else
                    {
                        s.FreeWalking.Dist1 = s.FreeWalking.NegDist1;
                        s.FreeWalking.Dist2 = s.FreeWalking.NegDist2;
                        var obj = map.GetObject(Position);

                        if (obj >= Map.Object.Tree0 &&
                            obj <= Map.Object.Pine7)
                        {
                            SetState(State.Logging);
                            s.FreeWalking.NegDist1 = 0;
                            s.FreeWalking.NegDist2 = 0;

                            if ((int)obj < 16)
                                s.FreeWalking.NegDist1 = -1;

                            Animation = 116;
                            Counter = CounterFromAnimation[Animation];
                        }
                        else
                        {
                            /* The expected tree is gone */
                            s.FreeWalking.NegDist1 = -128;
                            s.FreeWalking.NegDist2 = 0;
                            s.FreeWalking.Flags = 0;
                            Counter = 0;
                        }
                    }
                    break;
                case Type.Stonecutter:
                    if (s.FreeWalking.NegDist1 == -128)
                    {
                        if (s.FreeWalking.NegDist2 > 0)
                        {
                            DropResource(Resource.Type.Stone);
                        }

                        SetState(State.ReadyToEnter);
                        s.ReadyToEnter.FieldB = 0;
                        Counter = 0;
                    }
                    else
                    {
                        s.FreeWalking.Dist1 = s.FreeWalking.NegDist1;
                        s.FreeWalking.Dist2 = s.FreeWalking.NegDist2;

                        MapPos newPos = map.MoveUpLeft(Position);

                        var obj = map.GetObject(newPos);

                        if (!map.HasSerf(newPos) &&
                            obj >= Map.Object.Stone0 &&
                            obj <= Map.Object.Stone7)
                        {
                            Counter = 0;
                            StartWalking(Direction.UpLeft, 32, true);

                            SetState(State.StoneCutting);
                            s.FreeWalking.NegDist2 = Counter >> 2;
                            s.FreeWalking.NegDist1 = 0;
                        }
                        else
                        {
                            /* The expected stone is gone or unavailable */
                            s.FreeWalking.NegDist1 = -128;
                            s.FreeWalking.NegDist2 = 0;
                            s.FreeWalking.Flags = 0;
                            Counter = 0;
                        }
                    }
                    break;
                case Type.Forester:
                    if (s.FreeWalking.NegDist1 == -128)
                    {
                        SetState(State.ReadyToEnter);
                        s.ReadyToEnter.FieldB = 0;
                        Counter = 0;
                    }
                    else
                    {
                        s.FreeWalking.Dist1 = s.FreeWalking.NegDist1;
                        s.FreeWalking.Dist2 = s.FreeWalking.NegDist2;

                        if (map.GetObject(Position) == Map.Object.None)
                        {
                            SetState(State.Planting);
                            s.FreeWalking.NegDist2 = 0;
                            Animation = 121;
                            Counter = CounterFromAnimation[Animation];
                        }
                        else
                        {
                            /* The expected free space is no longer empty */
                            s.FreeWalking.NegDist1 = -128;
                            s.FreeWalking.NegDist2 = 0;
                            s.FreeWalking.Flags = 0;
                            Counter = 0;
                        }
                    }
                    break;
                case Type.Fisher:
                    if (s.FreeWalking.NegDist1 == -128)
                    {
                        if (s.FreeWalking.NegDist2 > 0)
                        {
                            DropResource(Resource.Type.Fish);
                        }

                        SetState(State.ReadyToEnter);
                        s.ReadyToEnter.FieldB = 0;
                        Counter = 0;
                    }
                    else
                    {
                        s.FreeWalking.Dist1 = s.FreeWalking.NegDist1;
                        s.FreeWalking.Dist2 = s.FreeWalking.NegDist2;

                        int a = -1;

                        if (map.Paths(Position) == 0)
                        {
                            if (map.TypeDown(Position) <= Map.Terrain.Water3 &&
                                map.TypeUp(map.MoveUpLeft(Position)) >= Map.Terrain.Grass0)
                            {
                                a = 132;
                            }
                            else if (map.TypeDown(map.MoveLeft(Position)) <= Map.Terrain.Water3 &&
                                     map.TypeUp(map.MoveUp(Position)) >= Map.Terrain.Grass0)
                            {
                                a = 131;
                            }
                        }

                        if (a < 0)
                        {
                            /* Cannot fish here after all. */
                            s.FreeWalking.NegDist1 = -128;
                            s.FreeWalking.NegDist2 = 0;
                            s.FreeWalking.Flags = 0;
                            Counter = 0;
                        }
                        else
                        {
                            SetState(State.Fishing);
                            s.FreeWalking.NegDist1 = 0;
                            s.FreeWalking.NegDist2 = 0;
                            s.FreeWalking.Flags = 0;
                            Animation = a;
                            Counter = CounterFromAnimation[a];
                        }
                    }
                    break;
                case Type.Farmer:
                    if (s.FreeWalking.NegDist1 == -128)
                    {
                        if (s.FreeWalking.NegDist2 > 0)
                        {
                            DropResource(Resource.Type.Wheat);
                        }

                        SetState(State.ReadyToEnter);
                        s.ReadyToEnter.FieldB = 0;
                        Counter = 0;
                    }
                    else
                    {
                        s.FreeWalking.Dist1 = s.FreeWalking.NegDist1;
                        s.FreeWalking.Dist2 = s.FreeWalking.NegDist2;

                        var obj = map.GetObject(Position);

                        if (obj == Map.Object.Seeds5 ||
                            (obj >= Map.Object.Field0 &&
                             obj <= Map.Object.Field5))
                        {
                            /* Existing field. */
                            Animation = 136;
                            s.FreeWalking.NegDist1 = 1;
                            Counter = CounterFromAnimation[Animation];
                        }
                        else if (obj == Map.Object.None &&
                                 map.Paths(Position) == 0)
                        {
                            /* Empty space. */
                            Animation = 135;
                            s.FreeWalking.NegDist1 = 0;
                            Counter = CounterFromAnimation[Animation];
                        }
                        else
                        {
                            /* Space not available after all. */
                            s.FreeWalking.NegDist1 = -128;
                            s.FreeWalking.NegDist2 = 0;
                            s.FreeWalking.Flags = 0;
                            Counter = 0;
                            break;
                        }

                        SetState(State.Farming);
                        s.FreeWalking.NegDist2 = 0;
                    }
                    break;
                case Type.Geologist:
                    if (s.FreeWalking.NegDist1 == -128)
                    {
                        if (map.GetObject(Position) == Map.Object.Flag &&
                            map.GetOwner(Position) == Player)
                        {
                            SetState(State.LookingForGeoSpot);
                            Counter = 0;
                        }
                        else
                        {
                            SetState(State.Lost);
                            s.Lost.FieldB = 0;
                            Counter = 0;
                        }
                    }
                    else
                    {
                        s.FreeWalking.Dist1 = s.FreeWalking.NegDist1;
                        s.FreeWalking.Dist2 = s.FreeWalking.NegDist2;

                        if (map.GetObject(Position) == Map.Object.None)
                        {
                            SetState(State.SamplingGeoSpot);
                            s.FreeWalking.NegDist1 = 0;
                            Animation = 141;
                            Counter = CounterFromAnimation[Animation];
                        }
                        else
                        {
                            /* Destination is not a free space after all. */
                            s.FreeWalking.NegDist1 = -128;
                            s.FreeWalking.NegDist2 = 0;
                            s.FreeWalking.Flags = 0;
                            Counter = 0;
                        }
                    }
                    break;
                case Type.Knight0:
                case Type.Knight1:
                case Type.Knight2:
                case Type.Knight3:
                case Type.Knight4:
                    if (s.FreeWalking.NegDist1 == -128)
                    {
                        FindInventory();
                    }
                    else
                    {
                        SetState(State.KnightOccupyEnemyBuilding);
                        Counter = 0;
                    }
                    break;
                default:
                    FindInventory();
                    break;
            }
        }

        void HandleSerfFreeWalkingSwitchOnDir(Direction dir)
        {
            // A suitable direction has been found; walk.
            if (dir < Direction.Right)
            {
                throw new ExceptionFreeserf("Wrong direction.");
            }

            int dx = (((int)dir < 3) ? 1 : -1) * ((((int)dir % 3) < 2) ? 1 : 0);
            int dy = (((int)dir < 3) ? 1 : -1) * ((((int)dir % 3) > 0) ? 1 : 0);

            Log.Verbose.Write("serf", $"serf {Index}: free walking: dest {s.FreeWalking.Dist1}, {s.FreeWalking.Dist2}, move {dx}, {dy}");

            s.FreeWalking.Dist1 -= dx;
            s.FreeWalking.Dist2 -= dy;

            StartWalking(dir, 32, true);

            if (s.FreeWalking.Dist1 == 0 &&
                s.FreeWalking.Dist2 == 0)
            {
                /* Arriving to destination */
                s.FreeWalking.Flags = Misc.Bit(3);
            }
        }

        void HandleSerfFreeWalkingSwitchWithOther()
        {
            /* No free position can be found. Switch with other serf. */
            MapPos newPos = 0;
            Direction dir = Direction.None;
            Serf otherSerf = null;
            Map map = Game.Map;
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction i in cycle)
            {
                newPos = map.Move(Position, i);

                if (map.HasSerf(newPos))
                {
                    otherSerf = Game.GetSerfAtPos(newPos);
                    Direction otherDir = Direction.None;

                    if (otherSerf.IsWaiting(ref otherDir) &&
                        otherDir == i.Reverse() &&
                        otherSerf.SwitchWaiting(otherDir))
                    {
                        dir = i;
                        break;
                    }
                }
            }

            if (dir > Direction.None)
            {
                int dx = (((int)dir < 3) ? 1 : -1) * ((((int)dir % 3) < 2) ? 1 : 0);
                int dy = (((int)dir < 3) ? 1 : -1) * ((((int)dir % 3) > 0) ? 1 : 0);

                Log.Verbose.Write("serf", $"free walking (switch): dest {s.FreeWalking.Dist1}, {s.FreeWalking.Dist2}, move {dx}, {dy}");

                s.FreeWalking.Dist1 -= dx;
                s.FreeWalking.Dist2 -= dy;

                if (s.FreeWalking.Dist1 == 0 &&
                    s.FreeWalking.Dist2 == 0)
                {
                    /* Arriving to destination */
                    s.FreeWalking.Flags = Misc.Bit(3);
                }

                /* Switch with other serf. */
                map.SetSerfIndex(Position, (int)otherSerf.Index);
                map.SetSerfIndex(newPos, (int)Index);

                otherSerf.Animation = GetWalkingAnimation((int)map.GetHeight(Position) - (int)map.GetHeight(otherSerf.Position), dir.Reverse(), true);
                Animation = GetWalkingAnimation((int)map.GetHeight(newPos) - (int)map.GetHeight(Position), dir, true);

                otherSerf.Counter = CounterFromAnimation[otherSerf.Animation];
                Counter = CounterFromAnimation[Animation];

                otherSerf.Position = Position;
                Position = newPos;
            }
            else
            {
                Animation = 82;
                Counter = CounterFromAnimation[Animation];
            }
        }

        static readonly Direction[] DirFromOffset = new Direction[]
        {
            Direction.UpLeft, Direction.Up,   Direction.None,
            Direction.Left,   Direction.None, Direction.Right,
            Direction.None,   Direction.Down, Direction.DownRight
        };

        /* Follow right-hand edge */
        static readonly Direction[] DirRightEdge = new Direction[]
        {
                Direction.Down, Direction.DownRight, Direction.Right, Direction.Up,
                Direction.UpLeft, Direction.Left, Direction.Left, Direction.Down,
                Direction.DownRight, Direction.Right, Direction.Up, Direction.UpLeft,
                Direction.UpLeft, Direction.Left, Direction.Down, Direction.DownRight,
                Direction.Right, Direction.Up, Direction.Up, Direction.UpLeft, Direction.Left,
                Direction.Down, Direction.DownRight, Direction.Right, Direction.Right,
                Direction.Up, Direction.UpLeft, Direction.Left, Direction.Down,
                Direction.DownRight, Direction.DownRight, Direction.Right, Direction.Up,
                Direction.UpLeft, Direction.Left, Direction.Down,
        };

        /* Follow left-hand edge */
        static readonly Direction[] DirLeftEdge = new Direction[]
        {
                Direction.UpLeft, Direction.Up, Direction.Right, Direction.DownRight,
                Direction.Down, Direction.Left, Direction.Up, Direction.Right,
                Direction.DownRight, Direction.Down, Direction.Left, Direction.UpLeft,
                Direction.Right, Direction.DownRight, Direction.Down, Direction.Left,
                Direction.UpLeft, Direction.Up, Direction.DownRight, Direction.Down,
                Direction.Left, Direction.UpLeft, Direction.Up, Direction.Right, Direction.Down,
                Direction.Left, Direction.UpLeft, Direction.Up, Direction.Right,
                Direction.DownRight, Direction.Left, Direction.UpLeft, Direction.Up,
                Direction.Right, Direction.DownRight, Direction.Down,
        };

        int HandleFreeWalkingFollowEdge()
        {
            bool water = SerfState == State.FreeSailing;
            int dirIndex = -1;
            Direction[] dirArray = null;

            if (Misc.BitTest(s.FreeWalking.Flags, 3))
            {
                /* Follow right-hand edge */
                dirArray = DirLeftEdge;
                dirIndex = (s.FreeWalking.Flags & 7) - 1;
            }
            else
            {
                /* Follow right-hand edge */
                dirArray = DirRightEdge;
                dirIndex = (s.FreeWalking.Flags & 7) - 1;
            }

            int d1 = s.FreeWalking.Dist1;
            int d2 = s.FreeWalking.Dist2;

            /* Check if dest is only one step away. */
            if (!water && Math.Abs(d1) <= 1 && Math.Abs(d2) <= 1 &&
                DirFromOffset[(d1 + 1) + 3 * (d2 + 1)] > Direction.None)
            {
                /* Convert offset in two dimensions to
                   direction variable. */
                Direction dirFromOffset = DirFromOffset[(d1 + 1) + 3 * (d2 + 1)];
                MapPos newPos = Game.Map.Move(Position, dirFromOffset);

                if (!CanPassMapPos(newPos))
                {
                    if (SerfState != State.KnightFreeWalking && s.FreeWalking.NegDist1 != -128)
                    {
                        s.FreeWalking.Dist1 += s.FreeWalking.NegDist1;
                        s.FreeWalking.Dist2 += s.FreeWalking.NegDist2;
                        s.FreeWalking.NegDist1 = 0;
                        s.FreeWalking.NegDist2 = 0;
                        s.FreeWalking.Flags = 0;
                        Animation = 82;
                        Counter = CounterFromAnimation[Animation];
                    }
                    else
                    {
                        SetState(State.Lost);
                        s.Lost.FieldB = 0;
                        Counter = 0;
                    }

                    return 0;
                }

                if (SerfState == State.KnightFreeWalking && s.FreeWalking.NegDist1 != -128 &&
                    Game.Map.HasSerf(newPos))
                {
                    /* Wait for other serfs */
                    s.FreeWalking.Flags = 0;
                    Animation = 82;
                    Counter = CounterFromAnimation[Animation];
                    return 0;
                }
            }

            int dirOffset = 6 * dirIndex;
            Direction i0 = Direction.None;
            Direction dir = Direction.None;
            Map map = Game.Map;
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction i in cycle)
            {
                MapPos newPos = map.Move(Position, dirArray[dirOffset + (int)i]);

                if (((water && map.GetObject(newPos) == 0) ||
                     (!water && !map.IsInWater(newPos) &&
                      CanPassMapPos(newPos))) && !map.HasSerf(newPos))
                {
                    dir = dirArray[dirOffset + (int)i];
                    i0 = i;
                    break;
                }
            }

            if (i0 > Direction.None)
            {
                int upper = ((s.FreeWalking.Flags >> 4) & 0xf) + (int)i0 - 2;

                if ((int)i0 < 2 && upper < 0)
                {
                    s.FreeWalking.Flags = 0;
                    HandleSerfFreeWalkingSwitchOnDir(dir);
                    return 0;
                }
                else if ((int)i0 > 2 && upper > 15)
                {
                    s.FreeWalking.Flags = 0;
                }
                else
                {
                    int dirIndex2 = (int)dir + 1;
                    s.FreeWalking.Flags = (upper << 4) | (s.FreeWalking.Flags & 0x8) | dirIndex2;
                    HandleSerfFreeWalkingSwitchOnDir(dir);
                    return 0;
                }
            }
            else
            {
                int dirIndex3 = 0;
                s.FreeWalking.Flags = (s.FreeWalking.Flags & 0xf8) | dirIndex3;
                s.FreeWalking.Flags &= ~Misc.Bit(3);
                HandleSerfFreeWalkingSwitchWithOther();
                return 0;
            }

            return -1;
        }

        /*  Directions for moving forwards. Each of the 12 lines represents
            a general direction as shown in the diagram below.
            The lines list the local directions in order of preference for that
            general direction.

            *         1    0
            *    2   ________   11
            *       /\      /\
            *      /  \    /  \
            *  3  /    \  /    \  10
            *    /______\/______\
            *    \      /\      /
            *  4  \    /  \    /  9
            *      \  /    \  /
            *       \/______\/
            *    5             8
            *         6    7
            */
        static readonly Direction[] DirForward = new Direction[]
        {
            Direction.Up, Direction.UpLeft, Direction.Right, Direction.Left,
            Direction.DownRight, Direction.Down, Direction.UpLeft, Direction.Up,
            Direction.Left, Direction.Right, Direction.Down, Direction.DownRight,
            Direction.UpLeft, Direction.Left, Direction.Up, Direction.Down, Direction.Right,
            Direction.DownRight, Direction.Left, Direction.UpLeft, Direction.Down,
            Direction.Up, Direction.DownRight, Direction.Right, Direction.Left,
            Direction.Down, Direction.UpLeft, Direction.DownRight, Direction.Up,
            Direction.Right, Direction.Down, Direction.Left, Direction.DownRight,
            Direction.UpLeft, Direction.Right, Direction.Up, Direction.Down,
            Direction.DownRight, Direction.Left, Direction.Right, Direction.UpLeft,
            Direction.Up, Direction.DownRight, Direction.Down, Direction.Right,
            Direction.Left, Direction.Up, Direction.UpLeft, Direction.DownRight,
            Direction.Right, Direction.Down, Direction.Up, Direction.Left, Direction.UpLeft,
            Direction.Right, Direction.DownRight, Direction.Up, Direction.Down,
            Direction.UpLeft, Direction.Left, Direction.Right, Direction.Up,
            Direction.DownRight, Direction.UpLeft, Direction.Down, Direction.Left,
            Direction.Up, Direction.Right, Direction.UpLeft, Direction.DownRight,
            Direction.Left, Direction.Down
        };

        void HandleFreeWalkingCommon()
        {
            bool water = SerfState == State.FreeSailing;

            if (Misc.BitTest(s.FreeWalking.Flags, 3) &&
                (s.FreeWalking.Flags & 7) == 0)
            {
                /* Destination reached */
                HandleSerfFreeWalkingStateDestReached();
                return;
            }

            if ((s.FreeWalking.Flags & 7) != 0)
            {
                /* Obstacle encountered, follow along the edge */
                if (HandleFreeWalkingFollowEdge() >= 0)
                    return;
            }

            /* Move fowards */
            int dirIndex = -1;
            int d1 = s.FreeWalking.Dist1;
            int d2 = s.FreeWalking.Dist2;

            if (d1 < 0)
            {
                if (d2 < 0)
                {
                    if (-d2 < -d1)
                    {
                        if (-2 * d2 < -d1)
                        {
                            dirIndex = 3;
                        }
                        else
                        {
                            dirIndex = 2;
                        }
                    }
                    else
                    {
                        if (-d2 < -2 * d1)
                        {
                            dirIndex = 1;
                        }
                        else
                        {
                            dirIndex = 0;
                        }
                    }
                }
                else
                {
                    if (d2 >= -d1)
                    {
                        dirIndex = 5;
                    }
                    else
                    {
                        dirIndex = 4;
                    }
                }
            }
            else
            {
                if (d2 < 0)
                {
                    if (-d2 >= d1)
                    {
                        dirIndex = 11;
                    }
                    else
                    {
                        dirIndex = 10;
                    }
                }
                else
                {
                    if (d2 < d1)
                    {
                        if (2 * d2 < d1)
                        {
                            dirIndex = 9;
                        }
                        else
                        {
                            dirIndex = 8;
                        }
                    }
                    else
                    {
                        if (d2 < 2 * d1)
                        {
                            dirIndex = 7;
                        }
                        else
                        {
                            dirIndex = 6;
                        }
                    }
                }
            }

            /* Try to move directly in the preferred direction */
            int dirOffset = 6 * dirIndex;
            Direction dir = DirForward[dirOffset];
            Map map = Game.Map;
            MapPos newPos = map.Move(Position, dir);

            if (((water && map.GetObject(newPos) == 0) ||
                 (!water && !map.IsInWater(newPos) &&
                 CanPassMapPos(newPos))) &&
                 !map.HasSerf(newPos))
            {
                HandleSerfFreeWalkingSwitchOnDir(dir);
                return;
            }

            /* Check if dest is only one step away. */
            if (!water && Math.Abs(d1) <= 1 && Math.Abs(d2) <= 1 &&
                DirFromOffset[(d1 + 1) + 3 * (d2 + 1)] > Direction.None)
            {
                /* Convert offset in two dimensions to
                   direction variable. */
                Direction d = DirFromOffset[(d1 + 1) + 3 * (d2 + 1)];
                MapPos newPos2 = map.Move(Position, d);

                if (!CanPassMapPos(newPos2))
                {
                    if (SerfState != State.KnightFreeWalking && s.FreeWalking.NegDist1 != -128)
                    {
                        s.FreeWalking.Dist1 += s.FreeWalking.NegDist1;
                        s.FreeWalking.Dist2 += s.FreeWalking.NegDist2;
                        s.FreeWalking.NegDist1 = 0;
                        s.FreeWalking.NegDist2 = 0;
                        s.FreeWalking.Flags = 0;
                    }
                    else
                    {
                        SetState(State.Lost);
                        s.Lost.FieldB = 0;
                        Counter = 0;
                    }

                    return;
                }

                if (SerfState == State.KnightFreeWalking && s.FreeWalking.NegDist1 != -128
                    && map.HasSerf(newPos2))
                {
                    Serf otherSerf = Game.GetSerfAtPos(newPos2);
                    Direction otherDir = Direction.None;

                    if (otherSerf.IsWaiting(ref otherDir) &&
                        (otherDir == d.Reverse() || otherDir == Direction.None) &&
                        otherSerf.SwitchWaiting(d.Reverse()))
                    {
                        /* Do the switch */
                        otherSerf.Position = Position;
                        map.SetSerfIndex(otherSerf.Position, (int)otherSerf.Index);
                        otherSerf.Animation = GetWalkingAnimation(
                            (int)map.GetHeight(otherSerf.Position) - (int)map.GetHeight(newPos2), d.Reverse(), true);
                        otherSerf.Counter = CounterFromAnimation[otherSerf.Animation];

                        Animation = GetWalkingAnimation(
                            (int)map.GetHeight(newPos2) - (int)map.GetHeight(Position), d, true);
                        Counter = CounterFromAnimation[Animation];

                        Position = newPos2;
                        map.SetSerfIndex(Position, (int)Index);

                        return;
                    }

                    if (otherSerf.SerfState == State.Walking ||
                        otherSerf.SerfState == State.Transporting)
                    {
                        ++s.FreeWalking.NegDist2;

                        if (s.FreeWalking.NegDist2 >= 10)
                        {
                            s.FreeWalking.NegDist2 = 0;

                            if (otherSerf.SerfState == State.Transporting)
                            {
                                if (map.HasFlag(newPos2))
                                {
                                    if (otherSerf.s.Walking.WaitCounter != -1)
                                    {
                                        //                int dir = otherSerf.s.Walking.Dir;
                                        //                if (dir < 0) dir += 6;
                                        Log.Debug.Write("serf", $"TODO remove {otherSerf.Index} from path");
                                    }

                                    otherSerf.SetLostState();
                                }
                            }
                            else
                            {
                                otherSerf.SetLostState();
                            }
                        }
                    }

                    Animation = 82;
                    Counter = CounterFromAnimation[Animation];

                    return;
                }
            }

            /* Look for another direction to go in. */
            Direction i0 = Direction.None;

            for (int i = 0; i < 5; ++i)
            {
                dir = DirForward[dirOffset + 1 + i];

                MapPos newPos2 = map.Move(Position, dir);

                if (((water && map.GetObject(newPos2) == 0) ||
                     (!water && !map.IsInWater(newPos2) &&
                      CanPassMapPos(newPos2))) && !map.HasSerf(newPos2))
                {
                    i0 = (Direction)i;
                    break;
                }
            }

            if (i0 < 0)
            {
                HandleSerfFreeWalkingSwitchWithOther();
                return;
            }

            int edge = 0;

            if (Misc.BitTest(dirIndex ^ (int)i0, 0))
                edge = 1;

            int upper = ((int)i0 / 2) + 1;

            s.FreeWalking.Flags = (upper << 4) | (edge << 3) | ((int)dir + 1);

            HandleSerfFreeWalkingSwitchOnDir(dir);
        }

        void HandleSerfFreeWalkingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                HandleFreeWalkingCommon();
            }
        }

        void HandleSerfLoggingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                s.FreeWalking.NegDist2 += 1;

                int newObject = -1;

                if (s.FreeWalking.NegDist1 != 0)
                {
                    newObject = (int)Map.Object.FelledTree0 + s.FreeWalking.NegDist2 - 1;
                }
                else
                {
                    newObject = (int)Map.Object.FelledPine0 + s.FreeWalking.NegDist2 - 1;
                }

                /* Change map object. */
                Game.Map.SetObject(Position, (Map.Object)newObject, -1);

                if (s.FreeWalking.NegDist2 < 5)
                {
                    Animation = 116 + s.FreeWalking.NegDist2;
                    Counter += CounterFromAnimation[Animation];
                }
                else
                {
                    SetState(State.FreeWalking);
                    Counter = 0;
                    s.FreeWalking.NegDist1 = -128;
                    s.FreeWalking.NegDist2 = 1;
                    s.FreeWalking.Flags = 0;

                    return;
                }
            }
        }

        void HandleSerfPlanningLoggingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                int dist = (Game.RandomInt() & 0x7f) + 1;
                MapPos pos = Game.Map.PosAddSpirally(Position, (uint)dist);
                var obj = Game.Map.GetObject(pos);

                if (obj >= Map.Object.Tree0 && obj <= Map.Object.Pine7)
                {
                    SetState(State.ReadyToLeave);
                    s.LeavingBuilding.FieldB = Map.GetSpiralPattern()[2 * dist] - 1;
                    s.LeavingBuilding.Dest = (uint)Map.GetSpiralPattern()[2 * dist + 1] - 1;
                    s.LeavingBuilding.Dest2 = -Map.GetSpiralPattern()[2 * dist] + 1;
                    s.LeavingBuilding.Dir = -Map.GetSpiralPattern()[2 * dist + 1] + 1;
                    s.LeavingBuilding.NextState = State.FreeWalking;
                    Log.Verbose.Write("serf", $"planning logging: tree found, dist {s.LeavingBuilding.FieldB}, {s.LeavingBuilding.Dest}.");

                    return;
                }

                Counter += 400;
            }
        }

        void HandleSerfPlanningPlantingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;

            while (Counter < 0)
            {
                int dist = (Game.RandomInt() & 0x7f) + 1;
                MapPos pos = map.PosAddSpirally(Position, (uint)dist);

                if (map.Paths(pos) == 0 &&
                    map.GetObject(pos) == Map.Object.None &&
                    map.TypeUp(pos) == Map.Terrain.Grass1 &&
                    map.TypeDown(pos) == Map.Terrain.Grass1 &&
                    map.TypeUp(map.MoveUpLeft(pos)) == Map.Terrain.Grass1 &&
                    map.TypeDown(map.MoveUpLeft(pos)) == Map.Terrain.Grass1)
                {
                    SetState(State.ReadyToLeave);
                    s.LeavingBuilding.FieldB = Map.GetSpiralPattern()[2 * dist] - 1;
                    s.LeavingBuilding.Dest = (uint)Map.GetSpiralPattern()[2 * dist + 1] - 1;
                    s.LeavingBuilding.Dest2 = -Map.GetSpiralPattern()[2 * dist] + 1;
                    s.LeavingBuilding.Dir = -Map.GetSpiralPattern()[2 * dist + 1] + 1;
                    s.LeavingBuilding.NextState = State.FreeWalking;
                    Log.Verbose.Write("serf", $"planning planting: free space found, dist {s.LeavingBuilding.FieldB}, {s.LeavingBuilding.Dest}.");

                    return;
                }

                Counter += 700;
            }
        }

        void HandleSerfPlantingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;

            while (Counter < 0)
            {
                if (s.FreeWalking.NegDist2 != 0)
                {
                    SetState(State.FreeWalking);
                    s.FreeWalking.NegDist1 = -128;
                    s.FreeWalking.NegDist2 = 0;
                    s.FreeWalking.Flags = 0;
                    Counter = 0;

                    return;
                }

                /* Plant a tree */
                Animation = 122;
                Map.Object newObject = (Map.Object)(Map.Object.NewPine + (Game.RandomInt() & 1));

                if (map.Paths(Position) == 0 && map.GetObject(Position) == Map.Object.None)
                {
                    map.SetObject(Position, newObject, -1);
                }

                s.FreeWalking.NegDist2 = -s.FreeWalking.NegDist2 - 1;
                Counter += 128;
            }
        }

        void HandleSerfPlanningStonecutting()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;
            while (Counter < 0)
            {
                int dist = (Game.RandomInt() & 0x7f) + 1;
                MapPos pos = map.PosAddSpirally(Position, (uint)dist);
                var obj = map.GetObject(map.MoveUpLeft(pos));

                if (obj >= Map.Object.Stone0 && obj <= Map.Object.Stone7 &&
                    CanPassMapPos(pos))
                {
                    SetState(State.ReadyToLeave);
                    s.LeavingBuilding.FieldB = Map.GetSpiralPattern()[2 * dist] - 1;
                    s.LeavingBuilding.Dest = (uint)Map.GetSpiralPattern()[2 * dist + 1] - 1;
                    s.LeavingBuilding.Dest2 = -Map.GetSpiralPattern()[2 * dist] + 1;
                    s.LeavingBuilding.Dir = -Map.GetSpiralPattern()[2 * dist + 1] + 1;
                    s.LeavingBuilding.NextState = State.StoneCutterFreeWalking;
                    Log.Verbose.Write("serf", $"planning stonecutting: stone found, dist {s.LeavingBuilding.FieldB}, {s.LeavingBuilding.Dest}.");

                    return;
                }

                Counter += 100;
            }
        }

        void HandleStonecutterFreeWalking()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;

            while (Counter < 0)
            {
                MapPos pos = map.MoveUpLeft(Position);

                if (!map.HasSerf(Position) && map.GetObject(pos) >= Map.Object.Stone0 &&
                    map.GetObject(pos) <= Map.Object.Stone7)
                {
                    s.FreeWalking.NegDist1 += s.FreeWalking.Dist1;
                    s.FreeWalking.NegDist2 += s.FreeWalking.Dist2;
                    s.FreeWalking.Dist1 = 0;
                    s.FreeWalking.Dist2 = 0;
                    s.FreeWalking.Flags = 8;
                }

                HandleFreeWalkingCommon();
            }
        }

        void HandleSerfStonecuttingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (s.FreeWalking.NegDist1 == 0)
            {
                if (Counter > s.FreeWalking.NegDist2)
                    return;

                Counter -= s.FreeWalking.NegDist2 + 1;
                s.FreeWalking.NegDist1 = 1;
                Animation = 123;
                Counter += 1536;
            }

            while (Counter < 0)
            {
                if (s.FreeWalking.NegDist1 != 1)
                {
                    SetState(State.FreeWalking);
                    s.FreeWalking.NegDist1 = -128;
                    s.FreeWalking.NegDist2 = 1;
                    s.FreeWalking.Flags = 0;
                    Counter = 0;
                    return;
                }

                Map map = Game.Map;

                if (map.HasSerf(map.MoveDownRight(Position)))
                {
                    Counter = 0;
                    return;
                }

                /* Decrement stone quantity or remove entirely if this
                   was the last piece. */
                var obj = map.GetObject(Position);

                if (obj <= Map.Object.Stone6)
                {
                    map.SetObject(Position, obj + 1, -1);
                }
                else
                {
                    map.SetObject(Position, Map.Object.None, -1);
                }

                Counter = 0;
                StartWalking(Direction.DownRight, 24, true);
                tick = Game.Tick;

                s.FreeWalking.NegDist1 = 2;
            }
        }

        void HandleSerfSawingState()
        {
            if (s.Sawing.Mode == 0)
            {
                Building building = Game.GetBuilding(Game.Map.GetObjectIndex(Position));

                if (building.UseResourceInStock(0))
                {
                    s.Sawing.Mode = 1;
                    Animation = 124;
                    Counter = CounterFromAnimation[Animation];
                    tick = Game.Tick;
                    Game.Map.SetSerfIndex(Position, (int)Index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - tick);
                tick = Game.Tick;
                Counter -= delta;

                if (Counter >= 0)
                    return;

                Game.Map.SetSerfIndex(Position, 0);
                SetState(State.MoveResourceOut);
                s.MoveResourceOut.Res = (uint)Resource.Type.Plank + 1;
                s.MoveResourceOut.ResDest = 0;
                s.MoveResourceOut.NextState = State.DropResourceOut;

                /* Update resource stats. */
                Player player = Game.GetPlayer(Player);
                player.IncreaseResourceCount(Resource.Type.Plank);
            }
        }

        void HandleSerfLostState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;

            while (Counter < 0)
            {
                /* Try to find a suitable destination. */
                for (int i = 0; i < 258; ++i)
                {
                    int dist = (s.Lost.FieldB == 0) ? 1 + i : 258 - i;

                    MapPos dest = map.PosAddSpirally(Position, (uint)dist);

                    if (map.HasFlag(dest))
                    {
                        Flag flag = Game.GetFlag(map.GetObjectIndex(dest));
                        if ((flag.LandPaths() != 0 ||
                             (flag.HasInventory() && flag.AcceptsSerfs())) &&
                              map.HasOwner(dest) &&
                              map.GetOwner(dest) == Player)
                        {
                            if (IsKnight())
                            {
                                SetState(State.KnightFreeWalking);
                            }
                            else
                            {
                                SetState(State.FreeWalking);
                            }

                            s.FreeWalking.Dist1 = Map.GetSpiralPattern()[2 * dist];
                            s.FreeWalking.Dist2 = Map.GetSpiralPattern()[2 * dist + 1];
                            s.FreeWalking.NegDist1 = -128;
                            s.FreeWalking.NegDist2 = -1;
                            s.FreeWalking.Flags = 0;
                            Counter = 0;

                            return;
                        }
                    }
                }

                /* Choose a random destination */
                int size = 16;
                int tries = 10;

                while (true)
                {
                    --tries;

                    if (tries < 0)
                    {
                        if (size < 64)
                        {
                            tries = 19;
                            size *= 2;
                        }
                        else
                        {
                            tries = -1;
                            size = 16;
                        }
                    }

                    int r = Game.RandomInt();
                    int column = (r & (size - 1)) - (size / 2);
                    int row = ((r >> 8) & (size - 1)) - (size / 2);

                    MapPos dest = map.PosAdd(Position, column, row);

                    if ((map.GetObject(dest) == 0 && map.GetHeight(dest) > 0) ||
                        (map.HasFlag(dest) &&
                         (map.HasOwner(dest) &&
                           map.GetOwner(dest) == Player)))
                    {
                        if (GetSerfType() >= Type.Knight0 && GetSerfType() <= Type.Knight4)
                        {
                            SetState(State.KnightFreeWalking);
                        }
                        else
                        {
                            SetState(State.FreeWalking);
                        }

                        s.FreeWalking.Dist1 = column;
                        s.FreeWalking.Dist2 = row;
                        s.FreeWalking.NegDist1 = -128;
                        s.FreeWalking.NegDist2 = -1;
                        s.FreeWalking.Flags = 0;
                        Counter = 0;

                        return;
                    }
                }
            }
        }

        void HandleLostSailor()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;
            while (Counter < 0)
            {
                /* Try to find a suitable destination. */
                for (int i = 0; i < 258; ++i)
                {
                    MapPos dest = map.PosAddSpirally(Position, (uint)i);

                    if (map.HasFlag(dest))
                    {
                        Flag flag = Game.GetFlag(map.GetObjectIndex(dest));

                        if (flag.LandPaths() != 0 &&
                            map.HasOwner(dest) &&
                            map.GetOwner(dest) == Player)
                        {
                            SetState(State.FreeSailing);

                            s.FreeWalking.Dist1 = Map.GetSpiralPattern()[2 * i];
                            s.FreeWalking.Dist2 = Map.GetSpiralPattern()[2 * i + 1];
                            s.FreeWalking.NegDist1 = -128;
                            s.FreeWalking.NegDist2 = -1;
                            s.FreeWalking.Flags = 0;
                            Counter = 0;

                            return;
                        }
                    }
                }

                /* Choose a random, empty destination */
                while (true)
                {
                    int r = Game.RandomInt();
                    int column = (r & 0x1f) - 16;
                    int row = ((r >> 8) & 0x1f) - 16;

                    MapPos dest = map.PosAdd(Position, column, row);

                    if (map.GetObject(dest) == 0)
                    {
                        SetState(State.FreeSailing);

                        s.FreeWalking.Dist1 = column;
                        s.FreeWalking.Dist2 = row;
                        s.FreeWalking.NegDist1 = -128;
                        s.FreeWalking.NegDist2 = -1;
                        s.FreeWalking.Flags = 0;
                        Counter = 0;

                        return;
                    }
                }
            }
        }

        void HandleFreeSailing()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                if (!Game.Map.IsInWater(Position))
                {
                    SetState(State.Lost);
                    s.Lost.FieldB = 0;
                    return;
                }

                HandleFreeWalkingCommon();
            }
        }

        void HandleSerfEscapeBuildingState()
        {
            if (!Game.Map.HasSerf(Position))
            {
                Game.Map.SetSerfIndex(Position, (int)Index);
                Animation = 82;
                Counter = 0;
                tick = Game.Tick;

                SetState(State.Lost);
                s.Lost.FieldB = 0;

                Game.AddSerfForDrawing(this, Position);
            }
        }

        static readonly Resource.Type[] ResFromMineType = new Resource.Type[]
        {
            Resource.Type.GoldOre, Resource.Type.IronOre,
            Resource.Type.Coal, Resource.Type.Stone
        };

        void HandleSerfMiningState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;
            while (Counter < 0)
            {
                Building building = Game.GetBuilding(map.GetObjectIndex(Position));

                Log.Verbose.Write("serf", $"mining substate: {s.Mining.Substate}.");

                switch (s.Mining.Substate)
                {
                    case 0:
                        {
                            /* There is a small chance that the miner will
                               not require food and skip to state 2. */
                            int r = Game.RandomInt();

                            if ((r & 7) == 0)
                            {
                                s.Mining.Substate = 2;
                            }
                            else
                            {
                                s.Mining.Substate = 1;
                            }

                            Counter += 100 + (r & 0x1ff);
                            break;
                        }
                    case 1:
                        if (building.UseResourceInStock(0))
                        {
                            /* Eat the food. */
                            s.Mining.Substate = 3;
                            map.SetSerfIndex(Position, (int)Index);
                            Animation = 125;
                            Counter = CounterFromAnimation[Animation];
                        }
                        else
                        {
                            map.SetSerfIndex(Position, (int)Index);
                            Animation = 98;
                            Counter += 256;
                            if (Counter < 0) Counter = 255;
                        }
                        break;
                    case 2:
                        s.Mining.Substate = 3;
                        map.SetSerfIndex(Position, (int)Index);
                        Animation = 125;
                        Counter = CounterFromAnimation[Animation];
                        break;
                    case 3:
                        s.Mining.Substate = 4;
                        building.StopActivity();
                        Animation = 126;
                        Counter = 304; /* TODO CounterFromAnimation[126] == 303 */
                        break;
                    case 4:
                        {
                            building.StartPlayingSfx();
                            map.SetSerfIndex(Position, 0);
                            /* fall through */
                        }
                        goto case 5;
                    case 5:
                    case 6:
                    case 7:
                        {
                            ++s.Mining.Substate;

                            /* Look for resource in ground. */
                            MapPos dest = map.PosAddSpirally(Position, (uint)(Game.RandomInt() >> 2) & 0x1f);
                            if ((map.GetObject(dest) == Map.Object.None ||
                                 map.GetObject(dest) > Map.Object.Castle) &&
                                map.GetResourceType(dest) == s.Mining.Deposit &&
                                map.GetResourceAmount(dest) > 0)
                            {
                                /* Decrement resource count in ground. */
                                map.RemoveGroundDeposit(dest, 1);

                                /* Hand resource to miner. */
                                s.Mining.Res = (uint)ResFromMineType[(int)s.Mining.Deposit - 1] + 1;
                                s.Mining.Substate = 8;
                            }

                            Counter += 1000;
                            break;
                        }
                    case 8:
                        map.SetSerfIndex(Position, (int)Index);
                        s.Mining.Substate = 9;
                        building.StopPlayingSfx();
                        Animation = 127;
                        Counter = CounterFromAnimation[Animation];
                        break;
                    case 9:
                        s.Mining.Substate = 10;
                        building.IncreaseMining((int)s.Mining.Res);
                        Animation = 128;
                        Counter = 384; /* TODO CounterFromAnimation[128] == 383 */
                        break;
                    case 10:
                        map.SetSerfIndex(Position, 0);
                        if (s.Mining.Res == 0)
                        {
                            s.Mining.Substate = 0;
                            Counter = 0;
                        }
                        else
                        {
                            uint res = s.Mining.Res;
                            map.SetSerfIndex(Position, 0);

                            SetState(State.MoveResourceOut);
                            s.MoveResourceOut.Res = res;
                            s.MoveResourceOut.ResDest = 0;
                            s.MoveResourceOut.NextState = State.DropResourceOut;

                            /* Update resource stats. */
                            Player player = Game.GetPlayer(Player);
                            player.IncreaseResourceCount((Resource.Type)(res - 1));

                            return;
                        }
                        break;
                    default:
                        Debug.NotReached();
                        break;
                }
            }
        }

        void HandleSerfSmeltingState()
        {
            Building building = Game.GetBuilding(Game.Map.GetObjectIndex(Position));

            if (s.Smelting.Mode == 0)
            {
                if (building.UseResourcesInStocks())
                {
                    building.StartActivity();

                    s.Smelting.Mode = 1;

                    if (s.Smelting.Type == 0)
                    {
                        Animation = 130;
                    }
                    else
                    {
                        Animation = 129;
                    }

                    s.Smelting.Counter = 20;
                    Counter = CounterFromAnimation[Animation];
                    tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, (int)Index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - tick);
                tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    --s.Smelting.Counter;

                    if (s.Smelting.Counter < 0)
                    {
                        building.StopActivity();

                        int res = -1;

                        if (s.Smelting.Type == 0)
                        {
                            res = 1 + (int)Resource.Type.Steel;
                        }
                        else
                        {
                            res = 1 + (int)Resource.Type.GoldBar;
                        }

                        SetState(State.MoveResourceOut);

                        s.MoveResourceOut.Res = (uint)res;
                        s.MoveResourceOut.ResDest = 0;
                        s.MoveResourceOut.NextState = State.DropResourceOut;

                        /* Update resource stats. */
                        Player player = Game.GetPlayer(Player);
                        player.IncreaseResourceCount((Resource.Type)(res - 1));

                        return;
                    }
                    else if (s.Smelting.Counter == 0)
                    {
                        Game.Map.SetSerfIndex(Position, 0);
                    }

                    Counter += 384;
                }
            }
        }

        void HandleSerfPlanningFishingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;
            while (Counter < 0)
            {
                int dist = ((Game.RandomInt() >> 2) & 0x3f) + 1;
                MapPos dest = map.PosAddSpirally(Position, (uint)dist);

                if (map.GetObject(dest) == Map.Object.None &&
                    map.Paths(dest) == 0 &&
                    ((map.TypeDown(dest) <= Map.Terrain.Water3 &&
                      map.TypeUp(map.MoveUpLeft(dest)) >= Map.Terrain.Grass0) ||
                     (map.TypeDown(map.MoveLeft(dest)) <= Map.Terrain.Water3 &&
                      map.TypeUp(map.MoveUp(dest)) >= Map.Terrain.Grass0)))
                {
                    SetState(State.ReadyToLeave);
                    s.LeavingBuilding.FieldB = Map.GetSpiralPattern()[2 * dist] - 1;
                    s.LeavingBuilding.Dest = (uint)Map.GetSpiralPattern()[2 * dist + 1] - 1;
                    s.LeavingBuilding.Dest2 = -Map.GetSpiralPattern()[2 * dist] + 1;
                    s.LeavingBuilding.Dir = -Map.GetSpiralPattern()[2 * dist + 1] + 1;
                    s.LeavingBuilding.NextState = State.FreeWalking;
                    Log.Verbose.Write("serf", $"planning fishing: lake found, dist {s.LeavingBuilding.FieldB},{s.LeavingBuilding.Dest}");

                    return;
                }

                Counter += 100;
            }
        }

        void HandleSerfFishingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                if (s.FreeWalking.NegDist2 != 0 ||
                    s.FreeWalking.Flags == 10)
                {
                    /* Stop fishing. Walk back. */
                    SetState(State.FreeWalking);
                    s.FreeWalking.NegDist1 = -128;
                    s.FreeWalking.Flags = 0;
                    Counter = 0;

                    return;
                }

                s.FreeWalking.NegDist1 += 1;
                if ((s.FreeWalking.NegDist1 % 2) == 0)
                {
                    Animation -= 2;
                    Counter += 768;
                    continue;
                }

                Map map = Game.Map;
                Direction dir = Direction.None;

                if (Animation == 131)
                {
                    if (map.IsInWater(map.MoveLeft(Position)))
                    {
                        dir = Direction.Left;
                    }
                    else
                    {
                        dir = Direction.Down;
                    }
                }
                else
                {
                    if (map.IsInWater(map.MoveRight(Position)))
                    {
                        dir = Direction.Right;
                    }
                    else
                    {
                        dir = Direction.DownRight;
                    }
                }

                uint res = map.GetResourceFish(map.Move(Position, dir));

                if (res > 0 && (Game.RandomInt() & 0x3f) + 4 < res)
                {
                    /* Caught a fish. */
                    map.RemoveFish(map.Move(Position, dir), 1);
                    s.FreeWalking.NegDist2 = 1 + (int)Resource.Type.Fish;
                }

                ++s.FreeWalking.Flags;
                Animation += 2;
                Counter += 128;
            }
        }

        void HandleSerfPlanningFarmingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;
            while (Counter < 0)
            {
                int dist = ((Game.RandomInt() >> 2) & 0x1f) + 7;

                MapPos dest = map.PosAddSpirally(Position, (uint)dist);

                /* If destination doesn't have an object it must be
                   of the correct type and the surrounding spaces
                   must not be occupied by large buildings.
                   If it Has_ an object it must be an existing field. */
                if ((map.GetObject(dest) == Map.Object.None &&
                      map.TypeUp(dest) == Map.Terrain.Grass1 &&
                      map.TypeDown(dest) == Map.Terrain.Grass1 &&
                      map.Paths(dest) == 0 &&
                      map.GetObject(map.MoveRight(dest)) != Map.Object.LargeBuilding &&
                      map.GetObject(map.MoveRight(dest)) != Map.Object.Castle &&
                      map.GetObject(map.MoveDownRight(dest)) != Map.Object.LargeBuilding &&
                      map.GetObject(map.MoveDownRight(dest)) != Map.Object.Castle &&
                      map.GetObject(map.MoveDown(dest)) != Map.Object.LargeBuilding &&
                      map.GetObject(map.MoveDown(dest)) != Map.Object.Castle &&
                      map.TypeDown(map.MoveLeft(dest)) == Map.Terrain.Grass1 &&
                      map.GetObject(map.MoveLeft(dest)) != Map.Object.LargeBuilding &&
                      map.GetObject(map.MoveLeft(dest)) != Map.Object.Castle &&
                      map.TypeUp(map.MoveUpLeft(dest)) == Map.Terrain.Grass1 &&
                      map.TypeDown(map.MoveUpLeft(dest)) == Map.Terrain.Grass1 &&
                      map.GetObject(map.MoveUpLeft(dest)) != Map.Object.LargeBuilding &&
                      map.GetObject(map.MoveUpLeft(dest)) != Map.Object.Castle &&
                      map.TypeUp(map.MoveUp(dest)) == Map.Terrain.Grass1 &&
                      map.GetObject(map.MoveUp(dest)) != Map.Object.LargeBuilding &&
                      map.GetObject(map.MoveUp(dest)) != Map.Object.Castle) ||
                      map.GetObject(dest) == Map.Object.Seeds5 ||
                      (map.GetObject(dest) >= Map.Object.Field0 &&
                      map.GetObject(dest) <= Map.Object.Field5))
                {
                    SetState(State.ReadyToLeave);
                    s.LeavingBuilding.FieldB = Map.GetSpiralPattern()[2 * dist] - 1;
                    s.LeavingBuilding.Dest = (uint)Map.GetSpiralPattern()[2 * dist + 1] - 1;
                    s.LeavingBuilding.Dest2 = -Map.GetSpiralPattern()[2 * dist] + 1;
                    s.LeavingBuilding.Dir = -Map.GetSpiralPattern()[2 * dist + 1] + 1;
                    s.LeavingBuilding.NextState = State.FreeWalking;
                    Log.Verbose.Write("serf", $"planning farming: field spot found, dist {s.LeavingBuilding.FieldB}, {s.LeavingBuilding.Dest}.");

                    return;
                }

                Counter += 500;
            }
        }

        void HandleSerfFarmingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter >= 0)
                return;

            Map map = Game.Map;

            if (s.FreeWalking.NegDist1 == 0)
            {
                /* Sowing. */
                if (map.GetObject(Position) == 0 && map.Paths(Position) == 0)
                {
                    map.SetObject(Position, Map.Object.Seeds0, -1);
                }
            }
            else
            {
                /* Harvesting. */
                s.FreeWalking.NegDist2 = 1;

                if (map.GetObject(Position) == Map.Object.Seeds5)
                {
                    map.SetObject(Position, Map.Object.Field0, -1);
                }
                else if (map.GetObject(Position) == Map.Object.Field5)
                {
                    map.SetObject(Position, Map.Object.FieldExpired, -1);
                }
                else if (map.GetObject(Position) != Map.Object.FieldExpired)
                {
                    map.SetObject(Position, (Map.Object)(map.GetObject(Position) + 1), -1);
                }
            }

            SetState(State.FreeWalking);
            s.FreeWalking.NegDist1 = -128;
            s.FreeWalking.Flags = 0;
            Counter = 0;
        }

        void HandleSerfMillingState()
        {
            Building building = Game.GetBuilding(Game.Map.GetObjectIndex(Position));

            if (s.Milling.Mode == 0)
            {
                if (building.UseResourceInStock(0))
                {
                    building.StartActivity();

                    s.Milling.Mode = 1;
                    Animation = 137;
                    Counter = CounterFromAnimation[Animation];
                    tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, (int)Index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - tick);
                tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    ++s.Milling.Mode;

                    if (s.Milling.Mode == 5)
                    {
                        /* Done milling. */
                        building.StopActivity();
                        SetState(State.MoveResourceOut);
                        s.MoveResourceOut.Res = 1 + (uint)Resource.Type.Flour;
                        s.MoveResourceOut.ResDest = 0;
                        s.MoveResourceOut.NextState = State.DropResourceOut;

                        Player player = Game.GetPlayer(Player);
                        player.IncreaseResourceCount(Resource.Type.Flour);
                        return;
                    }
                    else if (s.Milling.Mode == 3)
                    {
                        Game.Map.SetSerfIndex(Position, (int)Index);
                        Animation = 137;
                        Counter = CounterFromAnimation[Animation];
                    }
                    else
                    {
                        Game.Map.SetSerfIndex(Position, 0);
                        Counter += 1500;
                    }
                }
            }
        }

        void HandleSerfBakingState()
        {
            Building building = Game.GetBuilding(Game.Map.GetObjectIndex(Position));

            if (s.Baking.Mode == 0)
            {
                if (building.UseResourceInStock(0))
                {
                    s.Baking.Mode = 1;
                    Animation = 138;
                    Counter = CounterFromAnimation[Animation];
                    tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, (int)Index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - tick);
                tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    ++s.Baking.Mode;

                    if (s.Baking.Mode == 3)
                    {
                        /* Done baking. */
                        building.StopActivity();

                        SetState(State.MoveResourceOut);
                        s.MoveResourceOut.Res = 1 + (uint)Resource.Type.Bread;
                        s.MoveResourceOut.ResDest = 0;
                        s.MoveResourceOut.NextState = State.DropResourceOut;

                        Player player = Game.GetPlayer(Player);
                        player.IncreaseResourceCount(Resource.Type.Bread);
                        return;
                    }
                    else
                    {
                        building.StartActivity();
                        Game.Map.SetSerfIndex(Position, 0);
                        Counter += 1500;
                    }
                }
            }
        }

        static readonly int[] BreedingProbability = new int[]
        {
            6000, 8000, 10000, 11000, 12000, 13000, 14000, 0
        };

        void HandleSerfPigfarmingState()
        {
            /* When the serf is present there is also at least one
               pig present and at most eight. */

            Building building = Game.GetBuildingAtPos(Position);

            if (s.PigFarming.Mode == 0)
            {
                if (building.UseResourceInStock(0))
                {
                    s.PigFarming.Mode = 1;
                    Animation = 139;
                    Counter = CounterFromAnimation[Animation];
                    tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, (int)Index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - tick);
                tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    ++s.PigFarming.Mode;

                    if ((s.PigFarming.Mode & 1) != 0)
                    {
                        if (s.PigFarming.Mode != 7)
                        {
                            Game.Map.SetSerfIndex(Position, (int)Index);
                            Animation = 139;
                            Counter = CounterFromAnimation[Animation];
                        }
                        else if (building.PigsCount() == 8 ||
                                 (building.PigsCount() > 3 &&
                                 ((20 * Game.RandomInt()) >> 16) < building.PigsCount()))
                        {
                            /* Pig is ready for the butcher. */
                            building.SendPigToButcher();

                            SetState(State.MoveResourceOut);
                            s.MoveResourceOut.Res = 1 + (uint)Resource.Type.Pig;
                            s.MoveResourceOut.ResDest = 0;
                            s.MoveResourceOut.NextState = State.DropResourceOut;

                            /* Update resource stats. */
                            Player player = Game.GetPlayer(Player);
                            player.IncreaseResourceCount(Resource.Type.Pig);
                        }
                        else if ((Game.RandomInt() & 0xf) != 0)
                        {
                            s.PigFarming.Mode = 1;
                            Animation = 139;
                            Counter = CounterFromAnimation[Animation];
                            tick = Game.Tick;
                            Game.Map.SetSerfIndex(Position, (int)Index);
                        }
                        else
                        {
                            s.PigFarming.Mode = 0;
                        }

                        return;
                    }
                    else
                    {
                        Game.Map.SetSerfIndex(Position, 0);
                        if (building.PigsCount() < 8 &&
                            Game.RandomInt() < BreedingProbability[building.PigsCount() - 1])
                        {
                            building.PlaceNewPig();
                        }

                        Counter += 2048;
                    }
                }
            }
        }

        void HandleSerfButcheringState()
        {
            Building building = Game.GetBuilding(Game.Map.GetObjectIndex(Position));

            if (s.Butchering.Mode == 0)
            {
                if (building.UseResourceInStock(0))
                {
                    s.Butchering.Mode = 1;
                    Animation = 140;
                    Counter = CounterFromAnimation[Animation];
                    tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, (int)Index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - tick);
                tick = Game.Tick;
                Counter -= delta;

                if (Counter < 0)
                {
                    /* Done butchering. */
                    Game.Map.SetSerfIndex(Position, 0);

                    SetState(State.MoveResourceOut);
                    s.MoveResourceOut.Res = 1 + (uint)Resource.Type.Meat;
                    s.MoveResourceOut.ResDest = 0;
                    s.MoveResourceOut.NextState = State.DropResourceOut;

                    /* Update resource stats. */
                    Player player = Game.GetPlayer(Player);
                    player.IncreaseResourceCount(Resource.Type.Meat);
                }
            }
        }

        void HandleSerfMakingWeaponState()
        {
            Building building = Game.GetBuilding(Game.Map.GetObjectIndex(Position));

            if (s.MakingWeapon.Mode == 0)
            {
                /* One of each resource makes a sword and a shield.
                   Bit 3 is set if a sword has been made and a
                   shield can be made without more resources. */
                /* TODO Use of this bit overlaps with sfx check bit. */
                if (!building.IsPlayingSfx())
                {
                    if (!building.UseResourcesInStocks())
                    {
                        return;
                    }
                }

                building.StartActivity();

                s.MakingWeapon.Mode = 1;
                Animation = 143;
                Counter = CounterFromAnimation[Animation];
                tick = Game.Tick;

                Game.Map.SetSerfIndex(Position, (int)Index);
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - tick);
                tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    ++s.MakingWeapon.Mode;

                    if (s.MakingWeapon.Mode == 7)
                    {
                        /* Done making sword or shield. */
                        building.StopActivity();
                        Game.Map.SetSerfIndex(Position, 0);

                        Resource.Type res = building.IsPlayingSfx() ? Resource.Type.Shield : Resource.Type.Sword;

                        if (building.IsPlayingSfx())
                        {
                            building.StopPlayingSfx();
                        }
                        else
                        {
                            building.StartPlayingSfx();
                        }

                        SetState(State.MoveResourceOut);
                        s.MoveResourceOut.Res = 1 + (uint)res;
                        s.MoveResourceOut.ResDest = 0;
                        s.MoveResourceOut.NextState = State.DropResourceOut;

                        /* Update resource stats. */
                        Player player = Game.GetPlayer(Player);
                        player.IncreaseResourceCount(res);
                        return;
                    }
                    else
                    {
                        Counter += 576;
                    }
                }
            }
        }

        void HandleSerfMakingToolState()
        {
            Building building = Game.GetBuilding(Game.Map.GetObjectIndex(Position));

            if (s.MakingTool.Mode == 0)
            {
                if (building.UseResourcesInStocks())
                {
                    s.MakingTool.Mode = 1;
                    Animation = 144;
                    Counter = CounterFromAnimation[Animation];
                    tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, (int)Index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - tick);
                tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    ++s.MakingTool.Mode;

                    if (s.MakingTool.Mode == 4)
                    {
                        /* Done making tool. */
                        Game.Map.SetSerfIndex(Position, 0);

                        Player player = Game.GetPlayer(Player);
                        int totalToolPrio = 0;

                        for (int i = 0; i < 9; ++i)
                            totalToolPrio += player.GetToolPriority(i);

                        totalToolPrio >>= 4;

                        int res = -1;

                        if (totalToolPrio > 0)
                        {
                            /* Use defined tool priorities. */
                            int prioOffset = (totalToolPrio * Game.RandomInt()) >> 16;

                            for (int i = 0; i < 9; ++i)
                            {
                                prioOffset -= player.GetToolPriority(i) >> 4;

                                if (prioOffset < 0)
                                {
                                    res = (int)Resource.Type.Shovel + i;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            /* Completely random. */
                            res = (int)Resource.Type.Shovel + ((9 * Game.RandomInt()) >> 16);
                        }

                        SetState(State.MoveResourceOut);
                        s.MoveResourceOut.Res = 1 + (uint)res;
                        s.MoveResourceOut.ResDest = 0;
                        s.MoveResourceOut.NextState = State.DropResourceOut;

                        /* Update resource stats. */
                        player.IncreaseResourceCount((Resource.Type)res);

                        return;
                    }
                    else
                    {
                        Counter += 1536;
                    }
                }
            }
        }

        void HandleSerfBuildingBoatState()
        {
            Map map = Game.Map;
            Building building = Game.GetBuilding(map.GetObjectIndex(Position));

            if (s.BuildingBoat.Mode == 0)
            {
                if (!building.UseResourceInStock(0))
                    return;

                building.BoatClear();

                s.BuildingBoat.Mode = 1;
                Animation = 146;
                Counter = CounterFromAnimation[Animation];
                tick = Game.Tick;

                map.SetSerfIndex(Position, (int)Index);
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - tick);
                tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    ++s.BuildingBoat.Mode;

                    if (s.BuildingBoat.Mode == 9)
                    {
                        /* Boat done. */
                        MapPos newPos = map.MoveDownRight(Position);

                        if (map.HasSerf(newPos))
                        {
                            /* Wait for flag to be free. */
                            --s.BuildingBoat.Mode;
                            Counter = 0;
                        }
                        else
                        {
                            /* Drop boat at flag. */
                            building.BoatClear();
                            map.SetSerfIndex(Position, 0);

                            SetState(State.MoveResourceOut);
                            s.MoveResourceOut.Res = 1 + (uint)Resource.Type.Boat;
                            s.MoveResourceOut.ResDest = 0;
                            s.MoveResourceOut.NextState = State.DropResourceOut;

                            /* Update resource stats. */
                            Player player = Game.GetPlayer(Player);
                            player.IncreaseResourceCount(Resource.Type.Boat);

                            break;
                        }
                    }
                    else
                    {
                        /* Continue building. */
                        building.BoatDo();
                        Animation = 145;
                        Counter += 1408;
                    }
                }
            }
        }

        void HandleSerfLookingForGeoSpotState()
        {
            int tries = 2;
            Map map = Game.Map;
            for (int i = 0; i < 8; ++i)
            {
                int dist = ((Game.RandomInt() >> 2) & 0x3f) + 1;
                MapPos dest = map.PosAddSpirally(Position, (uint)dist);

                var obj = map.GetObject(dest);

                if (obj == Map.Object.None)
                {
                    Map.Terrain t1 = map.TypeDown(dest);
                    Map.Terrain t2 = map.TypeUp(dest);
                    Map.Terrain t3 = map.TypeDown(map.MoveUpLeft(dest));
                    Map.Terrain t4 = map.TypeUp(map.MoveUpLeft(dest));

                    if ((t1 >= Map.Terrain.Tundra0 && t1 <= Map.Terrain.Snow0) ||
                        (t2 >= Map.Terrain.Tundra0 && t2 <= Map.Terrain.Snow0) ||
                        (t3 >= Map.Terrain.Tundra0 && t3 <= Map.Terrain.Snow0) ||
                        (t4 >= Map.Terrain.Tundra0 && t4 <= Map.Terrain.Snow0))
                    {
                        SetState(State.FreeWalking);
                        s.FreeWalking.Dist1 = Map.GetSpiralPattern()[2 * dist];
                        s.FreeWalking.Dist2 = Map.GetSpiralPattern()[2 * dist + 1];
                        s.FreeWalking.NegDist1 = -Map.GetSpiralPattern()[2 * dist];
                        s.FreeWalking.NegDist2 = -Map.GetSpiralPattern()[2 * dist + 1];
                        s.FreeWalking.Flags = 0;
                        tick = Game.Tick;
                        Log.Verbose.Write("serf", $"looking for geo spot: found, dist {s.FreeWalking.Dist1}, {s.FreeWalking.Dist2}.");

                        return;
                    }
                }
                else if (obj >= Map.Object.SignLargeGold &&
                         obj <= Map.Object.SignEmpty)
                {
                    if (--tries == 0)
                        break;
                }
            }

            SetState(State.Walking);
            s.Walking.Dest = 0;
            s.Walking.Dir1 = -2;
            s.Walking.Dir = 0;
            s.Walking.WaitCounter = 0;
            Counter = 0;
        }

        void HandleSerfSamplingGeoSpotState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;

            while (Counter < 0)
            {
                if (s.FreeWalking.NegDist1 == 0 && map.GetObject(Position) == Map.Object.None)
                {
                    if (map.GetResourceType(Position) == Map.Minerals.None ||
                        map.GetResourceAmount(Position) == 0)
                    {
                        /* No available resource here. Put empty sign. */
                        map.SetObject(Position, Map.Object.SignEmpty, -1);
                    }
                    else
                    {
                        s.FreeWalking.NegDist1 = -1;
                        Animation = 142;

                        /* Select small or large sign with the right resource depicted. */
                        var obj = Map.Object.SignLargeGold +
                            2 * ((int)map.GetResourceType(Position) - 1) +
                            (map.GetResourceAmount(Position) < 12 ? 1 : 0);
                        map.SetObject(Position, obj, -1);

                        /* Check whether a new notification should be posted. */
                        bool showNotification = true;

                        for (uint i = 0; i < 60; ++i)
                        {
                            MapPos pos = map.PosAddSpirally(Position, 1u + i);

                            if (((int)map.GetObject(pos) >> 1) == ((int)obj >> 1))
                            {
                                showNotification = false;
                                break;
                            }
                        }

                        /* Create notification for found resource. */
                        if (showNotification)
                        {
                            Message.Type messageType = Message.Type.None;

                            switch (map.GetResourceType(Position))
                            {
                                case Map.Minerals.Coal:
                                    messageType = Message.Type.FoundCoal;
                                    break;
                                case Map.Minerals.Iron:
                                    messageType = Message.Type.FoundIron;
                                    break;
                                case Map.Minerals.Gold:
                                    messageType = Message.Type.FoundGold;
                                    break;
                                case Map.Minerals.Stone:
                                    messageType = Message.Type.FoundStone;
                                    break;
                                default:
                                    Debug.NotReached();
                                    break;
                            }

                            Game.GetPlayer(Player).AddNotification(messageType, Position, (uint)map.GetResourceType(Position) - 1);
                        }

                        Counter += 64;
                        continue;
                    }
                }

                SetState(State.FreeWalking);
                s.FreeWalking.NegDist1 = -128;
                s.FreeWalking.NegDist2 = 0;
                s.FreeWalking.Flags = 0;
                Counter = 0;
            }
        }

        void HandleSerfKnightEngagingBuildingState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                Map map = Game.Map;
                Map.Object obj = map.GetObject(map.MoveUpLeft(Position));

                if (obj >= Map.Object.SmallBuilding &&
                    obj <= Map.Object.Castle)
                {
                    Building building = Game.GetBuilding(map.GetObjectIndex(map.MoveUpLeft(Position)));

                    if (building.IsDone() &&
                        building.IsMilitary() &&
                        building.Player != Player &&
                        building.HasKnight())
                    {
                        if (building.IsUnderAttack())
                        {
                            Game.GetPlayer(building.Player).AddNotification(Message.Type.UnderAttack, building.Position, Player);
                        }

                        /* Change state of attacking knight */
                        Counter = 0;
                        SetState(State.KnightPrepareAttacking);
                        Animation = 168;

                        Serf defSerf = building.CallDefenderOut();

                        s.Attacking.DefIndex = (int)defSerf.Index;

                        /* Change state of defending knight */
                        SetOtherState(defSerf, State.KnightLeaveForFight);
                        defSerf.s.LeavingBuilding.NextState = State.KnightPrepareDefending;
                        defSerf.Counter = 0;
                        return;
                    }
                }

                /* No one to defend this building. Occupy it. */
                SetState(State.KnightOccupyEnemyBuilding);
                Animation = 179;
                Counter = CounterFromAnimation[Animation];
                tick = Game.Tick;
            }
        }

        void HandleSerfKnightPrepareAttacking()
        {
            Serf defSerf = Game.GetSerf((uint)s.Attacking.DefIndex);

            if (defSerf.SerfState == State.KnightPrepareDefending)
            {
                /* Change state of attacker. */
                SetState(State.KnightAttacking);
                Counter = 0;
                tick = Game.Tick;

                /* Change state of defender. */
                SetOtherState(defSerf, State.KnightDefending);
                defSerf.Counter = 0;

                SetFightOutcome(this, defSerf);
            }
        }

        void HandleSerfKnightLeaveForFightState()
        {
            tick = Game.Tick;
            Counter = 0;

            if (Game.Map.GetSerfIndex(Position) == Index || !Game.Map.HasSerf(Position))
            {
                LeaveBuilding(true);
            }
        }

        void HandleSerfKnightPrepareDefendingState()
        {
            Counter = 0;
            Animation = 84;
        }

        static readonly int[] KnightAttackMoves = new int[]
        {
            1, 2, 4, 2, 0, 2, 4, 2, 1, 0, 2, 2, 3, 0, 0, -1,
            3, 2, 2, 3, 0, 4, 1, 3, 2, 4, 2, 2, 3, 0, 0, -1,
            2, 1, 4, 3, 2, 2, 2, 3, 0, 3, 1, 2, 0, 2, 0, -1,
            2, 1, 3, 2, 4, 2, 3, 0, 0, 4, 2, 0, 2, 1, 0, -1,
            3, 1, 0, 2, 2, 1, 0, 2, 4, 2, 2, 3, 0, 0, -1,
            0, 3, 1, 2, 3, 4, 2, 1, 2, 0, 2, 4, 0, 2, 0, -1,
            0, 2, 1, 2, 4, 2, 3, 0, 2, 4, 3, 2, 0, 0, -1,
            0, 0, 1, 4, 3, 2, 2, 1, 2, 0, 0, 4, 3, 0, -1
        };

        static readonly int[] KnightFightAnim = new int[]
        {
            24, 35, 41, 56, 67, 72, 83, 89, 100, 121, 0, 0, 0, 0, 0, 0,
            26, 40, 42, 57, 73, 74, 88, 104, 106, 120, 122, 0, 0, 0, 0, 0,
            17, 18, 23, 33, 34, 38, 39, 98, 102, 103, 113, 114, 118, 119, 0, 0,
            130, 133, 134, 135, 147, 148, 161, 162, 164, 166, 167, 0, 0, 0, 0, 0,
            50, 52, 53, 70, 129, 131, 132, 146, 149, 151, 0, 0, 0, 0, 0, 0
        };

        static readonly int[] KnightFightAnimMax = new int[] { 10, 11, 14, 11, 10 };

        void HandleKnightAttacking()
        {
            Serf defSerf = Game.GetSerf((uint)s.Attacking.DefIndex);

            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            defSerf.tick = tick;
            Counter -= delta;
            defSerf.Counter = Counter;

            while (Counter < 0)
            {
                int move = KnightAttackMoves[s.Attacking.Move];

                if (move < 0)
                {
                    if (s.Attacking.AttackerWon == 0)
                    {
                        /* Defender won. */
                        if (SerfState == State.KnightAttackingFree)
                        {
                            SetOtherState(defSerf, State.KnightDefendingVictoryFree);

                            defSerf.Animation = 180;
                            defSerf.Counter = 0;

                            /* Attacker dies. */
                            SetState(State.KnightAttackingDefeatFree);
                            Animation = 152 + (int)GetSerfType();
                            Counter = 255;
                            SetSerfType(Type.Dead);
                        }
                        else
                        {
                            /* Defender returns to building. */
                            defSerf.EnterBuilding(-1, true);

                            /* Attacker dies. */
                            SetState(State.KnightAttackingDefeat);
                            Animation = 152 + (int)GetSerfType();
                            Counter = 255;
                            SetSerfType(Type.Dead);
                        }
                    }
                    else
                    {
                        /* Attacker won. */
                        if (SerfState == State.KnightAttackingFree)
                        {
                            SetState(State.KnightAttackingVictoryFree);
                            Animation = 168;
                            Counter = 0;

                            s.AttackingVictoryFree.Move = defSerf.s.DefendingFree.FieldD;
                            s.AttackingVictoryFree.DistColumn = defSerf.s.DefendingFree.OtherDistColumn;
                            s.AttackingVictoryFree.DistRow = defSerf.s.DefendingFree.OtherDistRow;
                        }
                        else
                        {
                            SetState(State.KnightAttackingVictory);
                            Animation = 168;
                            Counter = 0;

                            uint objectIndex = Game.Map.GetObjectIndex(Game.Map.MoveUpLeft(defSerf.Position));
                            Building building = Game.GetBuilding(objectIndex);
                            building.RequestedKnightDefeatOnWalk();
                        }

                        /* Defender dies. */
                        defSerf.tick = Game.Tick;
                        defSerf.Animation = 147 + (int)GetSerfType();
                        defSerf.Counter = 255;
                        defSerf.SetSerfType(Type.Dead);
                    }
                }
                else
                {
                    /* Go to next move in fight sequence. */
                    ++s.Attacking.Move;

                    if (s.Attacking.AttackerWon == 0)
                        move = 4 - move;

                    s.Attacking.FieldD = move;

                    int off = (Game.RandomInt() * KnightFightAnimMax[move]) >> 16;
                    int a = KnightFightAnim[move * 16 + off];

                    Animation = 146 + ((a >> 4) & 0xf);
                    defSerf.Animation = 156 + (a & 0xf);
                    Counter = 72 + (Game.RandomInt() & 0x18);
                    defSerf.Counter = Counter;
                }
            }
        }

        void HandleSerfKnightAttackingVictoryState()
        {
            Serf defSerf = Game.GetSerf((uint)s.Attacking.DefIndex);

            ushort delta = (ushort)(Game.Tick - defSerf.tick);
            defSerf.tick = Game.Tick;
            defSerf.Counter -= delta;

            if (defSerf.Counter < 0)
            {
                Game.DeleteSerf(defSerf);
                s.Attacking.DefIndex = 0;

                SetState(State.KnightEngagingBuilding);
                tick = Game.Tick;
                Counter = 0;
            }
        }

        void HandleSerfKnightAttackingDefeatState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                Game.Map.SetSerfIndex(Position, 0);
                Game.DeleteSerf(this);
            }
        }

        void HandleKnightOccupyEnemyBuilding()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter >= 0)
            {
                return;
            }

            Building building = Game.GetBuildingAtPos(Game.Map.MoveUpLeft(Position));

            if (building != null)
            {
                if (!building.IsBurning() && building.IsMilitary())
                {
                    if (building.Player == Player)
                    {
                        /* Enter building if there is space. */
                        if (building.BuildingType == Building.Type.Castle)
                        {
                            EnterBuilding(-2, false);
                            return;
                        }
                        else
                        {
                            if (building.IsEnoughPlaceForKnight())
                            {
                                /* Enter building */
                                EnterBuilding(-1, false);
                                building.KnightOccupy();
                                return;
                            }
                        }
                    }
                    else if (!building.HasKnight())
                    {
                        /* Occupy the building. */
                        Game.OccupyEnemyBuilding(building, Player);

                        if (building.BuildingType == Building.Type.Castle)
                        {
                            Counter = 0;
                        }
                        else
                        {
                            /* Enter building */
                            EnterBuilding(-1, false);
                            building.KnightOccupy();
                        }
                        return;
                    }
                    else
                    {
                        SetState(State.KnightEngagingBuilding);
                        Animation = 167;
                        Counter = 191;
                        return;
                    }
                }
            }

            /* Something is wrong. */
            SetState(State.Lost);
            s.Lost.FieldB = 0;
            Counter = 0;
        }

        void HandleStateKnightFreeWalking()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;

            while (Counter < 0)
            {
                /* Check for enemy knights nearby. */
                var cycle = DirectionCycleCW.CreateDefault();

                foreach (Direction d in cycle)
                {
                    MapPos pos = map.Move(Position, d);

                    if (map.HasSerf(pos))
                    {
                        Serf other = Game.GetSerfAtPos(pos);

                        if (Player != other.Player)
                        {
                            if (other.SerfState == State.KnightFreeWalking)
                            {
                                pos = map.MoveLeft(pos);

                                if (CanPassMapPos(pos))
                                {
                                    int distCol = s.FreeWalking.Dist1;
                                    int distRow = s.FreeWalking.Dist2;

                                    SetState(State.KnightEngageDefendingFree);

                                    s.DefendingFree.DistColumn = distCol;
                                    s.DefendingFree.DistRow = distRow;
                                    s.DefendingFree.OtherDistColumn = other.s.FreeWalking.Dist1;
                                    s.DefendingFree.OtherDistRow = other.s.FreeWalking.Dist2;
                                    s.DefendingFree.FieldD = 1;
                                    Animation = 99;
                                    Counter = 255;

                                    SetOtherState(other, State.KnightEngageAttackingFree);
                                    other.s.Attacking.FieldD = (int)d;
                                    other.s.Attacking.DefIndex = (int)Index;
                                    return;
                                }
                            }
                            else if (other.SerfState == State.Walking && other.IsKnight())
                            {
                                pos = map.MoveLeft(pos);

                                if (CanPassMapPos(pos))
                                {
                                    int distCol = s.FreeWalking.Dist1;
                                    int distRow = s.FreeWalking.Dist2;

                                    SetState(State.KnightEngageDefendingFree);
                                    s.DefendingFree.DistColumn = distCol;
                                    s.DefendingFree.DistRow = distRow;
                                    s.DefendingFree.FieldD = 0;
                                    Animation = 99;
                                    Counter = 255;

                                    Flag dest = Game.GetFlag(other.s.Walking.Dest);
                                    Building building = dest.GetBuilding();

                                    if (!building.HasInventory())
                                    {
                                        building.RequestedKnightAttackingOnWalk();
                                    }

                                    SetOtherState(other, State.KnightEngageAttackingFree);
                                    other.s.Attacking.FieldD = (int)d;
                                    other.s.Attacking.DefIndex = (int)Index;

                                    return;
                                }
                            }
                        }
                    }
                }

                HandleFreeWalkingCommon();
            }
        }

        void HandleStateKnightEngageDefendingFree()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0) Counter += 256;
        }

        void HandleStateKnightEngageAttackingFree()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                SetState(State.KnightEngageAttackingFreeJoin);
                Animation = 167;
                Counter += 191;
            }
        }

        void HandleStateKnightEngageAttackingFreeJoin()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                SetState(State.KnightPrepareAttackingFree);
                Animation = 168;
                Counter = 0;

                Serf other = Game.GetSerf((uint)s.Attacking.DefIndex);
                MapPos otherPos = other.Position;
                SetOtherState(other, State.KnightPrepareDefendingFree);
                other.Counter = Counter;

                /* Adjust distance to final destination. */
                Direction d = (Direction)s.Attacking.FieldD;

                if (d == Direction.Right || d == Direction.DownRight)
                {
                    --other.s.DefendingFree.DistColumn;
                }
                else if (d == Direction.Left || d == Direction.UpLeft)
                {
                    ++other.s.DefendingFree.DistColumn;
                }

                if (d == Direction.DownRight || d == Direction.Down)
                {
                    --other.s.DefendingFree.DistRow;
                }
                else if (d == Direction.UpLeft || d == Direction.Up)
                {
                    ++other.s.DefendingFree.DistRow;
                }

                other.StartWalking(d, 32, false);
                Game.Map.SetSerfIndex(otherPos, 0);
            }
        }

        void HandleStateKnightPrepareAttackingFree()
        {
            Serf other = Game.GetSerf((uint)s.Attacking.DefIndex);

            if (other.SerfState == State.KnightPrepareDefendingFreeWait)
            {
                SetState(State.KnightAttackingFree);
                Counter = 0;

                SetOtherState(other, State.KnightDefendingFree);
                other.Counter = 0;

                SetFightOutcome(this, other);
            }
        }

        void HandleStateKnightPrepareDefendingFree()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                SetState(State.KnightPrepareDefendingFreeWait);
                Counter = 0;
            }
        }

        void HandleKnightAttackingVictoryFree()
        {
            Serf other = Game.GetSerf((uint)s.AttackingVictoryFree.DefIndex);

            ushort delta = (ushort)(Game.Tick - other.tick);
            other.tick = Game.Tick;
            other.Counter -= delta;

            if (other.Counter < 0)
            {
                Game.DeleteSerf(other);

                int distColumn = s.AttackingVictoryFree.DistColumn;
                int distRow = s.AttackingVictoryFree.DistRow;

                SetState(State.KnightAttackingFreeWait);

                s.FreeWalking.Dist1 = distColumn;
                s.FreeWalking.Dist2 = distRow;
                s.FreeWalking.NegDist1 = 0;
                s.FreeWalking.NegDist2 = 0;

                if (s.Attacking.Move != 0)
                {
                    s.FreeWalking.Flags = 1;
                }
                else
                {
                    s.FreeWalking.Flags = 0;
                }

                Animation = 179;
                Counter = 127;
                tick = Game.Tick;
            }
        }

        void HandleKnightDefendingVictoryFree()
        {
            Animation = 180;
            Counter = 0;
        }

        void HandleSerfKnightAttackingDefeatFreeState()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                /* Change state of other. */
                Serf other = Game.GetSerf((uint)s.Attacking.DefIndex);
                int distCol = other.s.DefendingFree.DistColumn;
                int distRow = other.s.DefendingFree.DistRow;

                SetOtherState(other, State.KnightFreeWalking);

                other.s.FreeWalking.Dist1 = distCol;
                other.s.FreeWalking.Dist2 = distRow;
                other.s.FreeWalking.NegDist1 = 0;
                other.s.FreeWalking.NegDist2 = 0;
                other.s.FreeWalking.Flags = 0;

                other.Animation = 179;
                other.Counter = 0;
                other.tick = Game.Tick;

                /* Remove itself. */
                Game.Map.SetSerfIndex(Position, (int)other.Index);
                Game.DeleteSerf(this);
            }
        }

        void HandleKnightAttackingFreeWait()
        {
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                if (s.FreeWalking.Flags != 0)
                {
                    SetState(State.KnightFreeWalking);
                }
                else
                {
                    SetState(State.Lost);
                }

                Counter = 0;
            }
        }

        void HandleSerfStateKnightLeaveForWalkToFight()
        {
            tick = Game.Tick;
            Counter = 0;

            Map map = Game.Map;
            if (map.GetSerfIndex(Position) != Index && map.HasSerf(Position))
            {
                Animation = 82;
                Counter = 0;
                return;
            }

            Building building = Game.GetBuilding(map.GetObjectIndex(Position));
            MapPos newPos = map.MoveDownRight(Position);

            if (!map.HasSerf(newPos))
            {
                /* For clean state change, save the values first. */
                /* TODO maybe knightLeaveForWalkToFight can
                   share leavingBuilding state vars. */
                int distCol = s.LeaveForWalkToFight.DistColumn;
                int distRow = s.LeaveForWalkToFight.DistRow;
                int fieldD = s.LeaveForWalkToFight.FieldD;
                int fieldE = s.LeaveForWalkToFight.FieldE;
                Serf.State nextState = s.LeaveForWalkToFight.NextState;

                LeaveBuilding(false);
                /* TODO names for leavingBuilding vars make no sense here. */
                s.LeavingBuilding.FieldB = distCol;
                s.LeavingBuilding.Dest = (uint)distRow;
                s.LeavingBuilding.Dest2 = fieldD;
                s.LeavingBuilding.Dir = fieldE;
                s.LeavingBuilding.NextState = nextState;
            }
            else
            {
                Serf other = Game.GetSerfAtPos(newPos);

                if (Player == other.Player)
                {
                    Animation = 82;
                    Counter = 0;
                }
                else
                {
                    /* Go back to defending the building. */
                    switch (building.BuildingType)
                    {
                        case Building.Type.Hut:
                            SetState(State.DefendingHut);
                            break;
                        case Building.Type.Tower:
                            SetState(State.DefendingTower);
                            break;
                        case Building.Type.Fortress:
                            SetState(State.DefendingFortress);
                            break;
                        default:
                            Debug.NotReached();
                            break;
                    }

                    if (!building.KnightComeBackFromFight(this))
                    {
                        Animation = 82;
                        Counter = 0;
                    }
                }
            }
        }

        void HandleSerfIdleOnPathState()
        {
            Flag flag = Game.GetFlag(s.IdleOnPath.FlagIndex);
            Direction revDir = s.IdleOnPath.RevDir;

            /* Set walking dir in fieldE. */
            if (flag.IsScheduled(revDir))
            {
                s.IdleOnPath.FieldE = (tick & 0xff) + 6;
            }
            else
            {
                Flag otherFlag = flag.GetOtherEndFlag(revDir);
                Direction otherDir = flag.GetOtherEndDir((Direction)revDir);

                if (otherFlag != null && otherFlag.IsScheduled(otherDir))
                {
                    s.IdleOnPath.FieldE = (int)revDir.Reverse();
                }
                else
                {
                    return;
                }
            }

            Map map = Game.Map;

            if (!map.HasSerf(Position))
            {
                map.ClearIdleSerf(Position);
                map.SetSerfIndex(Position, (int)Index);

                int dir = s.IdleOnPath.FieldE;

                SetState(State.Transporting);
                s.Walking.Res = Resource.Type.None;
                s.Walking.WaitCounter = 0;
                s.Walking.Dir = dir;
                tick = Game.Tick;
                Counter = 0;
            }
            else
            {
                SetState(State.WaitIdleOnPath);
            }
        }

        void HandleSerfWaitIdleOnPathState()
        {
            Map map = Game.Map;

            if (!map.HasSerf(Position))
            {
                /* Duplicate code from handleSerfIdleOnPathState() */
                map.ClearIdleSerf(Position);
                map.SetSerfIndex(Position, (int)Index);

                int dir = s.IdleOnPath.FieldE;

                SetState(State.Transporting);
                s.Walking.Res = Resource.Type.None;
                s.Walking.WaitCounter = 0;
                s.Walking.Dir = dir;
                tick = Game.Tick;
                Counter = 0;
            }
        }

        void HandleScatterState()
        {
            /* Choose a random, empty destination */
            while (true)
            {
                int r = Game.RandomInt();
                int column = (r & 0xf);

                if (column < 8)
                    column -= 16;

                int row = ((r >> 8) & 0xf);

                if (row < 8)
                    row -= 16;

                Map map = Game.Map;
                MapPos dest = map.PosAdd(Position, column, row);

                if (map.GetObject(dest) == 0 && map.GetHeight(dest) > 0)
                {
                    if (IsKnight())
                    {
                        SetState(State.KnightFreeWalking);
                    }
                    else
                    {
                        SetState(State.FreeWalking);
                    }

                    s.FreeWalking.Dist1 = column;
                    s.FreeWalking.Dist2 = row;
                    s.FreeWalking.NegDist1 = -128;
                    s.FreeWalking.NegDist2 = -1;
                    s.FreeWalking.Flags = 0;
                    Counter = 0;

                    return;
                }
            }
        }

        void HandleSerfFinishedBuildingState()
        {
            Map map = Game.Map;

            if (!map.HasSerf(map.MoveDownRight(Position)))
            {
                SetState(State.ReadyToLeave);
                s.LeavingBuilding.Dest = 0;
                s.LeavingBuilding.FieldB = -2;
                s.LeavingBuilding.Dir = 0;
                s.LeavingBuilding.NextState = State.Walking;

                if (map.GetSerfIndex(Position) != Index && map.HasSerf(Position))
                {
                    Animation = 82;
                }
            }
        }

        void HandleSerfWakeAtFlagState()
        {
            Map map = Game.Map;

            if (!map.HasSerf(Position))
            {
                map.ClearIdleSerf(Position);
                map.SetSerfIndex(Position, (int)Index);
                tick = Game.Tick;
                Counter = 0;

                if (GetSerfType() == Type.Sailor)
                {
                    SetState(State.LostSailor);
                }
                else
                {
                    SetState(State.Lost);
                    s.Lost.FieldB = 0;
                }
            }
        }

        void HandleSerfWakeOnPathState()
        {
            SetState(State.WaitIdleOnPath);

            var cycle = DirectionCycleCCW.CreateDefault();

            foreach (Direction d in cycle)
            {
                if (Misc.BitTest(Game.Map.Paths(Position), (int)d))
                {
                    s.IdleOnPath.FieldE = (int)d;
                    break;
                }
            }
        }

        void HandleSerfDefendingState(int[] trainingParams)
        {
            switch (GetSerfType())
            {
                case Type.Knight0:
                case Type.Knight1:
                case Type.Knight2:
                case Type.Knight3:
                    TrainKnight(trainingParams[GetSerfType() - Type.Knight0]);
                    break;
                case Type.Knight4: /* Cannot train anymore. */
                    break;
                default:
                    Debug.NotReached();
                    break;
            }
        }

        void HandleSerfDefendingHutState()
        {
            HandleSerfDefendingState(new int[] { 250, 125, 62, 31 });
        }

        void HandleSerfDefendingTowerState()
        {
            HandleSerfDefendingState(new int[] { 1000, 500, 250, 125 });
        }

        void HandleSerfDefendingFortressState()
        {
            HandleSerfDefendingState(new int[] { 2000, 1000, 500, 250 });
        }

        void HandleSerfDefendingCastleState()
        {
            HandleSerfDefendingState(new int[] { 4000, 2000, 1000, 500 });
        }
    }
}
