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
using System.Runtime.InteropServices;

namespace Freeserf
{
    using MapPos = UInt32;
    using SerfMap = Dictionary<Serf.Type, uint>;

    // TODO: Can't we just replace things like Game.GetFlag(map.GetObjectIndex(Position)) with Game.GetFlagAtPos(Position) ???

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
                public int SlopeLen; /* C */
            }
            [FieldOffset(0)]
            public SEnteringBuilding EnteringBuilding;

            /* States: leaving_building, ready_to_leave,
               leave_for_fight */
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

            /* States: MoveResourceOut, drop_resource_out */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SMoveResourceOut
            {
                public uint Res; /* B */
                public uint ResDest; /* C */
                public State NextState; /* F */
            }
            [FieldOffset(0)]
            public SMoveResourceOut MoveResourceOut;

            /* No state: wait_for_resource_out */

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SReadyToLeaveInventory
            {
                public int Mode; /* B */
                public uint Dest; /* C */
                public uint InvIndex; /* E */
            }
            [FieldOffset(0)]
            public SReadyToLeaveInventory ReadyToLeaveInventory;

            /* States: free_walking, logging,
               planting, stonecutting, fishing,
               farming, sampling_geo_spot,
               knight_free_walking,
               knight_attacking_free,
               knight_attacking_free_wait */
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

            /* No state data: planning_logging,
               planning_planting, planning_stonecutting */

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

            /* No state data: planning_fishing,
               planning_farming */

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

            /* No state data: looking_for_geo_spot */

            /* States: knight_engaging_building,
               knight_prepare_attacking,
               knight_prepare_defending_free_wait,
               knight_attacking_defeat_free,
               knight_attacking,
               knight_attacking_victory,
               knight_engage_attacking_free,
               knight_engage_attacking_free_join,
               knight_attacking_victory_free,
            */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SAttacking
            {
                public int FieldB; /* B */
                public int FieldC; /* C */
                public int FieldD; /* D */
                public int DefIndex; /* E */
            }
            [FieldOffset(0)]
            public SAttacking Attacking;

            /* States: knight_defending_free,
               knight_engage_defending_free */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SDefendingFree
            {
                public int DistColumn; /* B */
                public int DistRow; /* C */
                public int Field_D; /* D */
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

            /* States: idle_on_path, wait_idle_on_path,
               wake_at_flag, wake_on_path. */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SIdleOnPath
            {
                // NOTE: Flag was a Flag* before! Now it is the index of it.
                public uint FlagIndex; /* C */
                public int Field_E; /* E */
                public Direction RevDir; /* B */
            }
            [FieldOffset(0)]
            public SIdleOnPath IdleOnPath;

            /* No state data: finished_building */

            /* States: defending_hut, defending_tower,
               defending_fortress, defending_castle */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SDefending
            {
                public uint NextKnight; /* E */
            }
            [FieldOffset(0)]
            public SDefending Defending;
        }

        StateInfo s;

        static readonly int[] CounterFromAnimation = new []
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

        static readonly int[] RoadBuildingSlope = new []
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

        public uint Player { get; set; }
        public State SerfState { get; private set; }
        public int Animation { get; private set; } /* Index to animation table in data file. */
        public int Counter { get; private set; }
        public MapPos Position { get; private set; }

        public Type GetSerfType()
        {
            return type;
        }

        public void SetSerfType(Type newType)
        {
            Type oldType = GetSerfType();
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
                int value = 1 << (oldType - Type.Knight0);
                player.DecreaseMilitaryScore(value);
            }
            if (newType >= Type.Knight0 &&
                newType <= Type.Knight4)
            {
                int value = 1 << (type - Type.Knight0);
                player.IncreaseMilitaryScore(value);
            }
            if (newType == Type.Transporter)
            {
                Counter = 0;
            }
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

                        Direction other_dir = flag.GetOtherEndDir(dir);
                        flag.GetOtherEndFlag(dir).CancelSerfRequest(other_dir);
                    }
                }
                else if (s.Walking.Dir1 == -1)
                {
                    Flag flag = Game.GetFlag(s.Walking.Dest);
                    Building building = flag.GetBuilding();
                    building.RequestedSerfLost();
                }

                SerfState = State.Lost;
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
                    SerfState = State.Lost;
                    s.Lost.FieldB = 0;
                }
                else
                {
                    SerfState = State.LostSailor;
                }
            }
            else
            {
                SerfState = State.Lost;
                s.Lost.FieldB = 0;
            }
        }

        public void add_to_defending_queue(uint nextKnightIndex, bool pause)
        {
            SerfState = State.DefendingCastle;

            s.Defending.NextKnight = (int)nextKnightIndex;

            if (pause)
            {
                Counter = 6000;
            }
        }

        public void InitGeneric(Inventory inventory)
        {
            SetSerfType(Type.Generic);
            Player = inventory.Owner;

            Building building = Game.GetBuilding(inventory.BuildingIndex);
            Position = building.Position;
            tick = (ushort)Game.Tick;
            SerfState = State.IdleInStock;
            s.IdleInStock.InvIndex = inventory.Index;
        }

        public void InitInventoryTransporter(Inventory inventory)
        {
            SerfState = State.BuildingCastle;
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
                        SerfState = State.Lost;
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
                    SerfState = State.EscapeBuilding;
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
                SerfState = State.Lost;
                s.Lost.FieldB = 0;
            }
            else
            {
                SerfState = State.EscapeBuilding;
            }
        }

        public bool change_transporter_state_at_pos(MapPos pos, State state)
        {
            if (Position == pos &&
              (state == State.WakeAtFlag || state == State.WakeOnPath ||
               state == State.WaitIdleOnPath || state == State.IdleOnPath))
            {
                SerfState = state;
                return true;
            }

            return false;
        }

        public void restore_path_serf_info()
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
                SerfState = State.WakeAtFlag;
            }
        }

        public void clear_destination(uint dest)
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

        public void clear_destination2(uint dest)
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

        public bool idle_to_wait_state(MapPos pos)
        {
            if (Position == pos &&
              (SerfState == State.IdleOnPath || SerfState == State.WaitIdleOnPath ||
               SerfState == State.WakeAtFlag || SerfState == State.WakeOnPath))
            {
                SerfState = State.WakeAtFlag;
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

        int GetFreeWalkingNegDist1()
        {
            return s.FreeWalking.NegDist1;
        }

        int GetFreeWalkingNegDist2()
        {
            return s.FreeWalking.NegDist2;
        }

        State GetLeavingBuildingNextState()
        {
            return s.LeavingBuilding.NextState;
        }

        int get_leaving_building_field_B()
        {
            return s.LeavingBuilding.FieldB;
        }

        Resource.Type get_mining_res()
        {
            return s.Mining.Res;
        }

        int get_attacking_field_D()
        {
            return s.Attacking.FieldD;
        }

        int get_attacking_def_index()
        {
            return s.Attacking.DefIndex;
        }

        int get_walking_wait_counter()
        {
            return s.Walking.WaitCounter;

        }
        void set_walking_wait_counter(int newCounter)
        {
            s.Walking.WaitCounter = newCounter;
        }

        int get_walking_dir()
        {
            return s.Walking.Dir;
        }

        uint get_idle_in_stock_inv_index()
        {
            return s.IdleInStock.InvIndex;
        }

        int get_mining_substate()
        {
            return s.Milling.Substate;
        }

        public Serf extract_last_knight_from_list()
        {
            uint defIndex = Index;
            Serf defSerf = Game.GetSerf(defIndex);

            while (defSerf.s.Defending.NextKnight != 0)
            {
                defIndex = defSerf.s.Defending.NextKnight;
                defSerf = Game.GetSerf(defIndex);
            }

            return defSerf;
        }

        void insert_before(Serf knight)
        {
            s.Defending.NextKnight = knight.Index;
        }

        uint get_next()
        {
            return s.Defending.NextKnight;
        }

        void set_next(uint next)
        {
            s.Defending.NextKnight = next;
        }

        // Commands

        void go_out_from_inventory(uint inventory, MapPos dest, int mode)
        {
            SerfState = State.ReadyToLeaveInventory;
            s.ReadyToLeaveInventory.Mode = mode;
            s.ReadyToLeaveInventory.Dest = dest;
            s.ReadyToLeaveInventory.InvIndex = inventory;
        }

        void send_off_to_fight(int distColumn, int distRow)
        {
            /* Send this serf off to fight. */
            SerfState = State.KnightLeaveForWalkToFight;
            s.LeaveForWalkToFight.DistColumn = distColumn;
            s.LeaveForWalkToFight.DistRow = distRow;
            s.LeaveForWalkToFight.FieldD = 0;
            s.LeaveForWalkToFight.FieldE = 0;
            s.LeaveForWalkToFight.NextState = State.KnightFreeWalking;
        }

        void stay_idle_in_stock(uint inventory)
        {
            SerfState = State.IdleInStock;
            s.IdleInStock.InvIndex = inventory;
        }

        void go_out_from_building(MapPos dest, int dir, int fieldB)
        {
            SerfState = State.ReadyToLeave;
            s.LeavingBuilding.FieldB = fieldB;
            s.LeavingBuilding.Dest = dest;
            s.LeavingBuilding.Dir = dir;
            s.LeavingBuilding.NextState = State.Walking;
        }

        void update()
        {

        }

        static string GetStateName(State state)
        {
            return SerfStateNames[(int)state];
        }

        static string GetTypeName(Type type)
        {
            return SerfTypeNames[(int)type];
        }

        public void ReadFrom(SaveReaderBinary reader)
        {

        }

        public void ReadFrom(SaveReaderText reader)
        {

        }

        public void WriteTo(SaveWriterText writer)
        {

        }

        /* Return true if serf is waiting for a position to be available.
           In this case, dir will be set to the desired direction of the serf,
           or DirectionNone if the desired direction cannot be determined. */
        bool IsWaiting(ref Direction dir)
        {
            Direction[] dirFromOffset = new Direction[]
            {
                Direction.UpLeft,   Direction.Up,       Direction.None,
                Direction.Left,     Direction.None,     Direction.Right,
                Direction.None,     Direction.Down,     Direction.DownRight
            };

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
                    dirFromOffset[(dx + 1) + 3 * (dy + 1)] > Direction.None)
                {
                    dir = dirFromOffset[(dx + 1) + 3 * (dy + 1)];
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
           If join_pos is set the serf is assumed to origin from
           a joined position so the source position will not have it's
           serf index cleared. */
        void EnterBuilding(int fieldB, bool joinPos)
        {
            SerfState = State.EnteringBuilding;

            StartWalking(Direction.UpLeft, 32, !joinPos);

            if (joinPos)
                Game.Map.SetSerfIndex(Position, (int)Index);

            Building building = Game.GetBuildingAtPos(Position);
            int slope = RoadBuildingSlope[(int)building.BuildingType];

            if (!building.IsDone())
                slope = 1;

            s.EnteringBuilding.SlopeLen = (slope * Counter) >> 5;
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

            SerfState = State.LeavingBuilding;
        }

        void EnterInventory()
        {
            Game.Map.SetSerfIndex(Position, 0);
            Building building = Game.GetBuildingAtPos(Position);
            SerfState = State.IdleInStock;
            /*serf->s.idle_in_stock.FieldB = 0;
              serf->s.idle_in_stock.FieldC = 0;*/
            s.IdleInStock.InvIndex = building.GetInventory().Index;
        }

        void DropResource(Resource.Type res)
        {
            Flag flag = Game.GetFlag(Game.Map.GetObjectIndex(Position));

            /* Resource is lost if no free slot is found */
            if (flag.DropResource(res, 0))
            {
                Game.GetPlayer(Player).IncreaseResCount(res);
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
                    SerfState = State.Walking;
                    s.Walking.Dir1 = -2;
                    s.Walking.Dest = 0;
                    s.Walking.Dir = 0;
                    Counter = 0;

                    return;
                }
            }

            SerfState = State.Lost;
            s.Lost.FieldB = 0;
            Counter = 0;
        }

        public bool CanPassMapPos(MapPos pos)
        {
            return Map.MapSpaceFromObject[(int)Game.Map.GetObject(pos)] <= Map.Space.Semipassable;
        }

        void set_fight_outcome(Serf attacker, Serf defender)
        {
            /* Calculate "morale" for attacker. */
            int expFactor = 1 << (attacker.GetSerfType() - Type.Knight0);
            int landFactor = 0x1000;

            if (attacker.Player != Game.Map.GetOwner(attacker.Position))
            {
                landFactor = Game.GetPlayer(attacker.Player).GetKnightMorale();
            }

            int morale = (0x400 * expFactor * landFactor) >> 16;

            /* Calculate "morale" for defender. */
            int defExpFactor = 1 << (defender.GetSerfType() - Type.Knight0);
            int defLandFactor = 0x1000;

            if (defender.Player != Game.Map.GetOwner(defender.Position))
            {
                defLandFactor = Game.GetPlayer(defender.Player).GetKnightMorale();
            }

            int defMorale = (0x400 * defExpFactor * defLandFactor) >> 16;

            uint playerIndex;
            int value = -1;
            Type ktype = Type.None;
            int result = ((morale + defMorale) * Game.RandomInt()) >> 16;

            if (result < morale)
            {
                playerIndex = defender.Player;
                value = defExpFactor;
                ktype = defender.GetSerfType();
                attacker.s.Attacking.FieldC = 1;
                Log.Debug.Write("serf", $"Fight: {morale} vs {defMorale} ({result}). Attacker winning.");
            }
            else
            {
                playerIndex = attacker.Player;
                value = expFactor;
                ktype = attacker.GetSerfType();
                attacker.s.Attacking.FieldC = 0;
                Log.Debug.Write("serf", $"Fight: {morale} vs {defMorale} ({result}). Defender winning.");
            }

            var player = Game.GetPlayer(playerIndex);

            player.DecreaseMilitaryScore(value);
            player.DecreaseSerfCount(ktype);
            attacker.s.Attacking.FieldB = Game.RandomInt() & 0x70;
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
                || inventory.GetSerfMode() == 1 /* in, stop */
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

                SerfState = State.ReadyToLeaveInventory;
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
                building.RequestedSerfReached(this);

                if (map.HasSerf(map.MoveUpLeft(Position)))
                {
                    Animation = 85;
                    Counter = 0;
                    SerfState = State.ReadyToEnter;
                }
                else
                {
                    EnterBuilding(s.Walking.Dir1, false);
                }
            }
            else if (s.Walking.Dir1 == 6)
            {
                SerfState = State.LookingForGeoSpot;
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

                SerfState = State.Transporting;
                s.Walking.Res = Resource.Type.None;
                s.Walking.Dir = (int)dir;
                s.Walking.Dir1 = 0;
                s.Walking.WaitCounter= 0;

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

            if ((!map.HasFlag(Position) && s.Walking.WaitCounter>= 10) ||
                s.Walking.WaitCounter >= 50)
            {
                MapPos pos = Position;

                /* Follow the chain of serfs waiting for each other and
                   see if there is a loop. */
                for (int i = 0; i < 100; i++)
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
                            SerfState = State.Lost;
                            s.Lost.FieldB = 1;
                            Counter = 0;

                            return;
                        }

                        s.Walking.Dest = r;
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
                        if (paths == Misc.BitU(d))
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
                        SerfState = State.Lost;
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
                        SerfState = State.Walking;
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
                        SerfState = State.Delivering;
                        s.Walking.WaitCounter = 0;

                        MapPos newPos = map.MoveUpLeft(Position);
                        Animation = 3 + (int)map.GetHeight(newPos) - (int)map.GetHeight(Position) +
                                    ((int)Direction.UpLeft + 6) * 9;
                        Counter = CounterFromAnimation[Animation];
                        /* TODO next call is actually into the middle of
                           handle_serf_delivering_state().
                           Why is a nice and clean state switch not enough???
                           Just ignore this call and we'll be safe, I think... */
                        /* handle_serf_delivering_state(serf); */
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
                        SerfState = State.Lost;
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
                            SerfState = State.IdleOnPath;
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

            if (Counter < 0 || Counter <= s.EnteringBuilding.SlopeLen)
            {
                if (Game.Map.GetObjectIndex(Position) == 0 ||
                    Game.GetBuildingAtPos(Position).IsBurning())
                {
                    /* Burning */
                    SerfState = State.Lost;
                    s.Lost.FieldB = 0;
                    Counter = 0;

                    return;
                }

                Counter = s.EnteringBuilding.SlopeLen;
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

                            SerfState = State.WaitForResourceOut;
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
                            SerfState = State.Digging;
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
                            SerfState = State.Building;
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
                        SerfState = State.WaitForResourceOut;
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
                            SerfState = State.PlanningLogging;
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
                                building.StockInit(1, Resource.Type.Lumber, 8);
                            }

                            SerfState = State.Sawing;
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
                            SerfState = State.PlanningStoneCutting;
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
                            SerfState = State.PlanningPlanting;
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

                            SerfState = State.Mining;
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
                            SerfState = State.Smelting;

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
                            SerfState = State.PlanningFishing;
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

                                building.SetInitialResInStock(1, 1);

                                flag.ClearFlags();
                                building.StockInit(0, Resource.Type.Wheat, 8);

                                SerfState = State.PigFarming;
                                s.PigFarming.Mode = 0;
                            }
                            else
                            {
                                SerfState = State.PigFarming;
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

                            SerfState = State.Butchering;
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
                            SerfState = State.PlanningFarming;
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

                            SerfState = State.Milling;
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

                            SerfState = State.Baking;
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

                            SerfState = State.BuildingBoat;
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

                            SerfState = State.MakingTool;
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

                            SerfState = State.MakingWeapon;
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
                            SerfState = State.LookingForGeoSpot; /* TODO Should never be reached */
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

                            SerfState = State.IdleInStock;
                            s.IdleInStock.InvIndex= inventory.Index;
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
                                SerfState = State.Lost;
                                Counter = 0;
                            }
                            else
                            {
                                map.SetSerfIndex(Position, 0);

                                if (building.HasInventory())
                                {
                                    SerfState = State.DefendingCastle;
                                    Counter = 6000;

                                    /* Prepend to knight list */
                                    s.Defending.NextKnight = building.GetFirstKnight();
                                    building.SetFirstKnight(Index);

                                    Game.GetPlayer(building.GetOwner()).IncreaseCastleKnights();

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
                                SerfState = nextState;
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
                SerfState = s.LeavingBuilding.NextState;

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
                    s.FreeWalking.NegDist1= negDist1;
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

        void HandleSerfDiggingState()
		{
            int[] hDiff = new []
            {
                -1, 1, -2, 2, -3, 3, -4, 4,
                -5, 5, -6, 6, -7, 7, -8, 8
            };

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
                    Direction dir = (Direction)((d == 0) ? Direction.Up : (Direction)(6 - d);
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
                        int h = hDiff[s.Digging.HIndex] + (int)s.Digging.TargetH;

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
                                    s.Digging.DigPos -= 1;
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
                        SerfState = State.ReadyToLeave;
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

        void HandleSerfBuildingState()
		{
            int[] materialOrder = new []
            {
                0, 0, 0, 0, 0, 4, 0, 0,
                0, 0, 0x38, 2, 8, 2, 8, 4,
                4, 0xc, 0x14, 0x2c, 2, 0x1c, 0x1f0, 4,
                0, 0, 0, 0, 0, 0, 0, 0
            };

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
                        SerfState = State.FinishedBuilding;

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

                        if (!Misc.BitTest(materialOrder[(int)building.BuildingType], materialStep))
                        {
                            /* Planks */
                            if (building.GetResCountInStock(0) == 0)
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
                            if (building.GetResCountInStock(1) == 0)
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

                    if (!Misc.BitTest(materialOrder[(int)building.BuildingType], materialStep))
                    {
                        /* Planks */
                        if (building.GetResCountInStock(0) == 0)
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
                        if (building.GetResCountInStock(1) == 0)
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
                SerfState = State.WaitForResourceOut;
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

            SerfState = State.MoveResourceOut;
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

            SerfState = State.ReadyToEnter;
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
                    SerfState = State.Transporting;
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

        void handle_serf_ready_to_leave_inventory_state()
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

                        SerfState = State.ReadyToEnter;
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
                            SerfState = State.Logging;
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

                        SerfState = State.ReadyToEnter;
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

                            SerfState = State.StoneCutting;
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
                        SerfState = State.ReadyToEnter;
                        s.ReadyToEnter.FieldB = 0;
                        Counter = 0;
                    }
                    else
                    {
                        s.FreeWalking.Dist1 = s.FreeWalking.NegDist1;
                        s.FreeWalking.Dist2 = s.FreeWalking.NegDist2;

                        if (map.GetObject(Position) == Map.Object.None)
                        {
                            SerfState = State.Planting;
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

                        SerfState = State.ReadyToEnter);
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
                            SerfState = State.Fishing;
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

                        SerfState = State.ReadyToEnter;
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

                        SerfState = State.Farming;
                        s.FreeWalking.NegDist2 = 0;
                    }
                    break;
                case Type.Geologist:
                    if (s.FreeWalking.NegDist1 == -128)
                    {
                        if (map.GetObject(Position) == Map.Object.Flag &&
                            map.GetOwner(Position) == Player)
                        {
                            SerfState = State.LookingForGeoSpot;
                            Counter = 0;
                        }
                        else
                        {
                            SerfState = State.Lost;
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
                            SerfState = State.SamplingGeoSpot;
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
                        SerfState = State.KnightOccupyEnemyBuilding;
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

            StartWalking(dir, 32, 1);

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
                MapPos new_pos = Game.Map.Move(Position, dirFromOffset);

                if (!CanPassMapPos(new_pos))
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
                        SerfState = State.Lost);
                        s.Lost.FieldB = 0;
                        Counter = 0;
                    }

                    return 0;
                }

                if (SerfState == State.KnightFreeWalking && s.FreeWalking.NegDist1 != -128 &&
                    Game.Map.HasSerf(new_pos))
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
                        SerfState = State.Lost;
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

            for (int i = 0; i < 5; i++)
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
                    SerfState = State.FreeWalking;
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
                    SerfState = State.ReadyToLeave);
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
                    SerfState = State.ReadyToLeave);
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
                    SerfState = State.FreeWalking;
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
                    SerfState = State.ReadyToLeave);
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
                    SerfState = State.FreeWalking;
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
                StartWalking(Direction.DownRight, 24, 1);
                tick = Game.Tick;

                s.FreeWalking.NegDist1 = 2;
            }
        }

        void HandleSerfSawingState()
		{
            if (s.Sawing.Mode == 0)
            {
                Building building = Game.GetBuilding(Game.Map.GetObjectIndex(Position));

                if (building.UseResourceInStock(1))
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

                if (Counter >= 0) return;

                Game.Map.SetSerfIndex(Position, 0);
                SerfState = State.MoveResourceOut;
                s.MoveResourceOut.Res = (uint)Resource.Type.Plank + 1;
                s.MoveResourceOut.ResDest = 0;
                s.MoveResourceOut.NextState = State.DropResourceOut;

                /* Update resource stats. */
                Player player = Game.GetPlayer(Player);
                player.IncreaseResCount(Resource.Type.Plank);
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
                for (int i = 0; i < 258; i++)
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
                            if (GetSerfType() >= Type.Knight0 &&
                                GetSerfType() <= Type.Knight4)
                            {
                                SerfState = State.KnightFreeWalking;
                            }
                            else
                            {
                                SerfState = State.FreeWalking;
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
                            SerfState = State.KnightFreeWalking;
                        }
                        else
                        {
                            SerfState = State.FreeWalking;
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
                for (int i = 0; i < 258; i++)
                {
                    MapPos dest = map.PosAddSpirally(Position, (uint)i);

                    if (map.HasFlag(dest))
                    {
                        Flag flag = Game.GetFlag(map.GetObjectIndex(dest));

                        if (flag.LandPaths() != 0 &&
                            map.HasOwner(dest) &&
                            map.GetOwner(dest) == Player)
                        {
                            SerfState = State.FreeSailing;

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
                        SerfState = State.FreeSailing;

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
                    SerfState = State.Lost);
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

                SerfState = State.Lost);
                s.Lost.FieldB = 0;
            }
        }

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
                            s.Mining.Substate += 1;

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
                                Resource.Type[] resFromMineType = new Resource.Type[]
                                {
                                    Resource.Type.GoldOre, Resource.Type.IronOre,
                                    Resource.Type.Coal, Resource.Type.Stone
                                };

                                s.Mining.Res = (int)resFromMineType[(int)s.Mining.Deposit - 1] + 1;
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
                        building.IncreaseMining(s.Mining.Res);
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

                            SerfState = State.MoveResourceOut);
                            s.MoveResourceOut.Res = res;
                            s.MoveResourceOut.ResDest = 0;
                            s.MoveResourceOut.NextState = State.DropResourceOut;

                            /* Update resource stats. */
                            Player player = Game.GetPlayer(Player);
                            player.IncreaseResCount(res - 1);

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
                    s.Smelting.Counter -= 1;
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

                        SerfState = State.MoveResourceOut);

                        s.MoveResourceOut.Res = res;
                        s.MoveResourceOut.ResDest = 0;
                        s.MoveResourceOut.NextState = State.DropResourceOut;

                        /* Update resource stats. */
                        Player* player = Game.get_player(Player);
                        player.increase_res_count(res - 1);
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

        void handle_serf_planning_fishing_state()
		{
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;
            while (Counter < 0)
            {
                int dist = ((Game.RandomInt() >> 2) & 0x3f) + 1;
                MapPos dest = map.PosAdd_spirally(Position, dist);

                if (map.GetObject(dest) == Map.ObjectNone &&
                    map.paths(dest) == 0 &&
                    ((map.TypeDown(dest) <= Map.TerrainWater3 &&
                      map.TypeUp(map.move_up_left(dest)) >= Map.TerrainGrass0) ||
                     (map.TypeDown(map.move_left(dest)) <= Map.TerrainWater3 &&
                      map.TypeUp(map.move_up(dest)) >= Map.TerrainGrass0)))
                {
                    SerfState = State.ReadyToLeave);
                    s.LeavingBuilding.FieldB = Map.GetSpiralPattern()[2 * dist] - 1;
                    s.LeavingBuilding.dest = Map.GetSpiralPattern()[2 * dist + 1] - 1;
                    s.LeavingBuilding.dest2 = -Map.GetSpiralPattern()[2 * dist] + 1;
                    s.LeavingBuilding.dir = -Map.GetSpiralPattern()[2 * dist + 1] + 1;
                    s.LeavingBuilding.next_state = State.FreeWalking;
                    Log.Verbose["serf"] << "planning fishing: lake found, dist "
                                         << s.LeavingBuilding.FieldB << ","
                                         << s.LeavingBuilding.dest;
                    return;
                }

                Counter += 100;
            }
        }

        void handle_serf_fishing_state()
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
                    SerfState = State.FreeWalking);
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
                Direction dir = DirectionNone;
                if (Animation == 131)
                {
                    if (map.is_in_water(map.move_left(Position)))
                    {
                        dir = DirectionLeft;
                    }
                    else
                    {
                        dir = DirectionDown;
                    }
                }
                else
                {
                    if (map.is_in_water(map.move_right(Position)))
                    {
                        dir = DirectionRight;
                    }
                    else
                    {
                        dir = DirectionDownRight;
                    }
                }

                int res = map.get_res_fish(map.move(Position, dir));
                if (res > 0 && (Game.RandomInt() & 0x3f) + 4 < res)
                {
                    /* Caught a fish. */
                    map.remove_fish(map.move(Position, dir), 1);
                    s.FreeWalking.NegDist2 = 1 + Resource.TypeFish;
                }

                s.FreeWalking.flags += 1;
                Animation += 2;
                Counter += 128;
            }
        }

        void handle_serf_planning_farming_state()
		{
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;
            while (Counter < 0)
            {
                int dist = ((Game.RandomInt() >> 2) & 0x1f) + 7;
                MapPos dest = map.PosAdd_spirally(Position, dist);

                /* If destination doesn't have an object it must be
                   of the correct type and the surrounding spaces
                   must not be occupied by large buildings.
                   If it _has_ an object it must be an existing field. */
                if ((map.GetObject(dest) == Map.ObjectNone &&
                     (map.TypeUp(dest) == Map.TerrainGrass1 &&
                      map.TypeDown(dest) == Map.TerrainGrass1 &&
                      map.paths(dest) == 0 &&
                      map.GetObject(map.move_right(dest)) != Map.ObjectLargeBuilding &&
                      map.GetObject(map.move_right(dest)) != Map.ObjectCastle &&
                     map.GetObject(map.move_down_right(dest)) != Map.ObjectLargeBuilding &&
                      map.GetObject(map.move_down_right(dest)) != Map.ObjectCastle &&
                      map.GetObject(map.move_down(dest)) != Map.ObjectLargeBuilding &&
                      map.GetObject(map.move_down(dest)) != Map.ObjectCastle &&
                      map.TypeDown(map.move_left(dest)) == Map.TerrainGrass1 &&
                      map.GetObject(map.move_left(dest)) != Map.ObjectLargeBuilding &&
                      map.GetObject(map.move_left(dest)) != Map.ObjectCastle &&
                      map.TypeUp(map.move_up_left(dest)) == Map.TerrainGrass1 &&
                      map.TypeDown(map.move_up_left(dest)) == Map.TerrainGrass1 &&
                      map.GetObject(map.move_up_left(dest)) != Map.ObjectLargeBuilding &&
                      map.GetObject(map.move_up_left(dest)) != Map.ObjectCastle &&
                      map.TypeUp(map.move_up(dest)) == Map.TerrainGrass1 &&
                      map.GetObject(map.move_up(dest)) != Map.ObjectLargeBuilding &&
                      map.GetObject(map.move_up(dest)) != Map.ObjectCastle)) ||
                    map.GetObject(dest) == Map.ObjectSeeds5 ||
                    (map.GetObject(dest) >= Map.ObjectField0 &&
                     map.GetObject(dest) <= Map.ObjectField5))
                {
                    SerfState = State.ReadyToLeave);
                    s.LeavingBuilding.FieldB = Map.GetSpiralPattern()[2 * dist] - 1;
                    s.LeavingBuilding.dest = Map.GetSpiralPattern()[2 * dist + 1] - 1;
                    s.LeavingBuilding.dest2 = -Map.GetSpiralPattern()[2 * dist] + 1;
                    s.LeavingBuilding.dir = -Map.GetSpiralPattern()[2 * dist + 1] + 1;
                    s.LeavingBuilding.next_state = State.FreeWalking;
                    Log.Verbose["serf"] << "planning farming: field spot found, dist "
                                         << s.LeavingBuilding.FieldB << ", "
                                         << s.LeavingBuilding.dest << ".";
                    return;
                }

                Counter += 500;
            }
        }

        void handle_serf_farming_state()
		{
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter >= 0) return;

            Map map = Game.Map;
            if (s.FreeWalking.NegDist1 == 0)
            {
                /* Sowing. */
                if (map.GetObject(Position) == 0 && map.paths(Position) == 0)
                {
                    map.set_object(Position, Map.ObjectSeeds0, -1);
                }
            }
            else
            {
                /* Harvesting. */
                s.FreeWalking.NegDist2 = 1;
                if (map.GetObject(Position) == Map.ObjectSeeds5)
                {
                    map.set_object(Position, Map.ObjectField0, -1);
                }
                else if (map.GetObject(Position) == Map.ObjectField5)
                {
                    map.set_object(Position, Map.ObjectFieldExpired, -1);
                }
                else if (map.GetObject(Position) != Map.ObjectFieldExpired)
                {
                    map.set_object(Position, (Map.Object)(map.GetObject(Position) + 1), -1);
                }
            }

            SerfState = State.FreeWalking);
            s.FreeWalking.NegDist1 = -128;
            s.FreeWalking.Flags = 0;
            Counter = 0;
        }

        void handle_serf_milling_state()
		{
            Building building = Game.GetBuilding(Game.Map.GetObjectIndex(Position));

            if (s.milling.mode == 0)
            {
                if (building.use_resource_in_stock(0))
                {
                    building.start_activity();

                    s.milling.mode = 1;
                    Animation = 137;
                    Counter = CounterFromAnimation[Animation];
                    tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - tick);
                tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    s.milling.mode += 1;
                    if (s.milling.mode == 5)
                    {
                        /* Done milling. */
                        building.stop_activity();
                        SerfState = State.MoveResourceOut);
                        s.MoveResourceOut.Res = 1 + Resource.TypeFlour;
                        s.MoveResourceOut.ResDest = 0;
                        s.MoveResourceOut.NextState = State.DropResourceOut;

                        Player* player = Game.get_player(Player);
                        player.increase_res_count(Resource.TypeFlour);
                        return;
                    }
                    else if (s.milling.mode == 3)
                    {
                        Game.Map.SetSerfIndex(Position, index);
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

        void handle_serf_baking_state()
		{
            Building building = Game.GetBuilding(Game.Map.GetObjectIndex(Position));

            if (s.baking.mode == 0)
            {
                if (building.use_resource_in_stock(0))
                {
                    s.baking.mode = 1;
                    Animation = 138;
                    Counter = CounterFromAnimation[Animation];
                    tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - tick);
                tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    s.baking.mode += 1;
                    if (s.baking.mode == 3)
                    {
                        /* Done baking. */
                        building.stop_activity();

                        SerfState = State.MoveResourceOut);
                        s.MoveResourceOut.Res = 1 + Resource.TypeBread;
                        s.MoveResourceOut.ResDest = 0;
                        s.MoveResourceOut.NextState = State.DropResourceOut;

                        Player* player = Game.get_player(Player);
                        player.increase_res_count(Resource.TypeBread);
                        return;
                    }
                    else
                    {
                        building.start_activity();
                        Game.Map.SetSerfIndex(Position, 0);
                        Counter += 1500;
                    }
                }
            }
        }

        void handle_serf_pigfarming_state()
		{
            /* When the serf is present there is also at least one
     pig present and at most eight. */
            const int breeding_prob[] = {
    6000, 8000, 10000, 11000, 12000, 13000, 14000, 0
  };

            Building building = Game.GetBuildingAtPos(Position);

            if (s.pigfarming.mode == 0)
            {
                if (building.use_resource_in_stock(0))
                {
                    s.pigfarming.mode = 1;
                    Animation = 139;
                    Counter = CounterFromAnimation[Animation];
                    tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - tick);
                tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    s.pigfarming.mode += 1;
                    if (s.pigfarming.mode & 1)
                    {
                        if (s.pigfarming.mode != 7)
                        {
                            Game.Map.SetSerfIndex(Position, index);
                            Animation = 139;
                            Counter = CounterFromAnimation[Animation];
                        }
                        else if (building.pigs_count() == 8 ||
                                 (building.pigs_count() > 3 &&
                                  ((20 * Game.RandomInt()) >> 16) < building.pigs_count()))
                        {
                            /* Pig is ready for the butcher. */
                            building.send_pig_to_butcher();

                            SerfState = State.MoveResourceOut);
                            s.MoveResourceOut.Res = 1 + Resource.TypePig;
                            s.MoveResourceOut.ResDest = 0;
                            s.MoveResourceOut.NextState = State.DropResourceOut;

                            /* Update resource stats. */
                            Player* player = Game.get_player(Player);
                            player.increase_res_count(Resource.TypePig);
                        }
                        else if (Game.RandomInt() & 0xf)
                        {
                            s.pigfarming.mode = 1;
                            Animation = 139;
                            Counter = CounterFromAnimation[Animation];
                            tick = Game.Tick;
                            Game.Map.SetSerfIndex(Position, index);
                        }
                        else
                        {
                            s.pigfarming.mode = 0;
                        }
                        return;
                    }
                    else
                    {
                        Game.Map.SetSerfIndex(Position, 0);
                        if (building.pigs_count() < 8 &&
                            Game.RandomInt() < breeding_prob[building.pigs_count() - 1])
                        {
                            building.place_new_pig();
                        }
                        Counter += 2048;
                    }
                }
            }
        }

        void handle_serf_butchering_state()
		{
            Building building = Game.GetBuilding(Game.Map.GetObjectIndex(Position));

            if (s.butchering.mode == 0)
            {
                if (building.use_resource_in_stock(0))
                {
                    s.butchering.mode = 1;
                    Animation = 140;
                    Counter = CounterFromAnimation[Animation];
                    tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, index);
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

                    SerfState = State.MoveResourceOut);
                    s.MoveResourceOut.Res = 1 + Resource.TypeMeat;
                    s.MoveResourceOut.ResDest = 0;
                    s.MoveResourceOut.NextState = State.DropResourceOut;

                    /* Update resource stats. */
                    Player* player = Game.get_player(Player);
                    player.increase_res_count(Resource.TypeMeat);
                }
            }
        }

        void handle_serf_making_weapon_state()
		{
            Building building = Game.GetBuilding(Game.Map.GetObjectIndex(Position));

            if (s.making_weapon.mode == 0)
            {
                /* One of each resource makes a sword and a shield.
                   Bit 3 is set if a sword has been made and a
                   shield can be made without more resources. */
                /* TODO Use of this bit overlaps with sfx check bit. */
                if (!building.is_playing_sfx())
                {
                    if (!building.use_resources_in_stocks())
                    {
                        return;
                    }
                }

                building.start_activity();

                s.making_weapon.mode = 1;
                Animation = 143;
                Counter = CounterFromAnimation[Animation];
                tick = Game.Tick;

                Game.Map.SetSerfIndex(Position, index);
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - tick);
                tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    s.making_weapon.mode += 1;
                    if (s.making_weapon.mode == 7)
                    {
                        /* Done making sword or shield. */
                        building.stop_activity();
                        Game.Map.SetSerfIndex(Position, 0);

                        Resource.Type res = building.is_playing_sfx() ? Resource.TypeShield :
                                                                          Resource.TypeSword;
                        if (building.is_playing_sfx())
                        {
                            building.stop_playing_sfx();
                        }
                        else
                        {
                            building.start_playing_sfx();
                        }

                        SerfState = State.MoveResourceOut);
                        s.MoveResourceOut.Res = 1 + res;
                        s.MoveResourceOut.ResDest = 0;
                        s.MoveResourceOut.NextState = State.DropResourceOut;

                        /* Update resource stats. */
                        Player* player = Game.get_player(Player);
                        player.increase_res_count(res);
                        return;
                    }
                    else
                    {
                        Counter += 576;
                    }
                }
            }
        }

        void handle_serf_making_tool_state()
		{
            Building building = Game.GetBuilding(Game.Map.GetObjectIndex(Position));

            if (s.making_tool.mode == 0)
            {
                if (building.use_resources_in_stocks())
                {
                    s.making_tool.mode = 1;
                    Animation = 144;
                    Counter = CounterFromAnimation[Animation];
                    tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - tick);
                tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    s.making_tool.mode += 1;
                    if (s.making_tool.mode == 4)
                    {
                        /* Done making tool. */
                        Game.Map.SetSerfIndex(Position, 0);

                        Player* player = Game.get_player(Player);
                        int total_tool_prio = 0;
                        for (int i = 0; i < 9; i++) total_tool_prio += player.get_tool_prio(i);
                        total_tool_prio >>= 4;

                        int res = -1;
                        if (total_tool_prio > 0)
                        {
                            /* Use defined tool priorities. */
                            int prio_offset = (total_tool_prio * Game.RandomInt()) >> 16;
                            for (int i = 0; i < 9; i++)
                            {
                                prio_offset -= player.get_tool_prio(i) >> 4;
                                if (prio_offset < 0)
                                {
                                    res = Resource.TypeShovel + i;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            /* Completely random. */
                            res = Resource.TypeShovel + ((9 * Game.RandomInt()) >> 16);
                        }

                        SerfState = State.MoveResourceOut);
                        s.MoveResourceOut.Res = 1 + res;
                        s.MoveResourceOut.ResDest = 0;
                        s.MoveResourceOut.NextState = State.DropResourceOut;

                        /* Update resource stats. */
                        player.increase_res_count(res);
                        return;
                    }
                    else
                    {
                        Counter += 1536;
                    }
                }
            }
        }

        void handle_serf_building_boat_state()
		{
            Map map = Game.Map;
            Building building = Game.GetBuilding(map.GetObjectIndex(Position));

            if (s.building_boat.mode == 0)
            {
                if (!building.use_resource_in_stock(0)) return;
                building.boat_clear();

                s.building_boat.mode = 1;
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
                    s.building_boat.mode += 1;
                    if (s.building_boat.mode == 9)
                    {
                        /* Boat done. */
                        MapPos new_pos = map.move_down_right(Position);
                        if (map.HasSerf(new_pos))
                        {
                            /* Wait for flag to be free. */
                            s.building_boat.mode -= 1;
                            Counter = 0;
                        }
                        else
                        {
                            /* Drop boat at flag. */
                            building.boat_clear();
                            map.SetSerfIndex(Position, 0);

                            SerfState = State.MoveResourceOut);
                            s.MoveResourceOut.Res = 1 + Resource.TypeBoat;
                            s.MoveResourceOut.ResDest = 0;
                            s.MoveResourceOut.NextState = State.DropResourceOut;

                            /* Update resource stats. */
                            Player* player = Game.get_player(Player);
                            player.increase_res_count(Resource.TypeBoat);

                            break;
                        }
                    }
                    else
                    {
                        /* Continue building. */
                        building.boat_do();
                        Animation = 145;
                        Counter += 1408;
                    }
                }
            }
        }

        void handle_serf_looking_for_geo_spot_state()
		{
            int tries = 2;
            Map map = Game.Map;
            for (int i = 0; i < 8; i++)
            {
                int dist = ((Game.RandomInt() >> 2) & 0x3f) + 1;
                MapPos dest = map.PosAdd_spirally(Position, dist);

                int obj = map.GetObject(dest);
                if (obj == Map.ObjectNone)
                {
                    Map.Terrain t1 = map.TypeDown(dest);
                    Map.Terrain t2 = map.TypeUp(dest);
                    Map.Terrain t3 = map.TypeDown(map.move_up_left(dest));
                    Map.Terrain t4 = map.TypeUp(map.move_up_left(dest));
                    if ((t1 >= Map.TerrainTundra0 && t1 <= Map.TerrainSnow0) ||
                        (t2 >= Map.TerrainTundra0 && t2 <= Map.TerrainSnow0) ||
                        (t3 >= Map.TerrainTundra0 && t3 <= Map.TerrainSnow0) ||
                        (t4 >= Map.TerrainTundra0 && t4 <= Map.TerrainSnow0))
                    {
                        SerfState = State.FreeWalking);
                        s.FreeWalking.Dist1 = Map.GetSpiralPattern()[2 * dist];
                        s.FreeWalking.Dist2 = Map.GetSpiralPattern()[2 * dist + 1];
                        s.FreeWalking.NegDist1 = -Map.GetSpiralPattern()[2 * dist];
                        s.FreeWalking.NegDist2 = -Map.GetSpiralPattern()[2 * dist + 1];
                        s.FreeWalking.Flags = 0;
                        tick = Game.Tick;
                        Log.Verbose["serf"] << "looking for geo spot: found, dist "
                                             << s.FreeWalking.Dist1 << ", "
                                             << s.FreeWalking.Dist2 << ".";
                        return;
                    }
                }
                else if (obj >= Map.ObjectSignLargeGold &&
                         obj <= Map.ObjectSignEmpty)
                {
                    tries -= 1;
                    if (tries == 0) break;
                }
            }

            SerfState = State.Walking);
            s.Walking.dest = 0;
            s.Walking.dir1 = -2;
            s.Walking.dir = 0;
            s.Walking.wait_Counter = 0;
            Counter = 0;
        }

        void handle_serf_sampling_geo_spot_state()
		{
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;
            while (Counter < 0)
            {
                if (s.FreeWalking.NegDist1 == 0 &&
                  map.GetObject(Position) == Map.ObjectNone)
                {
                    if (map.get_res_type(Position) == Map.MineralsNone ||
                        map.get_res_amount(Position) == 0)
                    {
                        /* No available resource here. Put empty sign. */
                        map.set_object(Position, Map.ObjectSignEmpty, -1);
                    }
                    else
                    {
                        s.FreeWalking.NegDist1 = -1;
                        Animation = 142;

                        /* Select small or large sign with the right resource depicted. */
                        int obj = Map.ObjectSignLargeGold +
                          2 * (map.get_res_type(Position) - 1) +
                          (map.get_res_amount(Position) < 12 ? 1 : 0);
                        map.set_object(Position, (Map.Object)obj, -1);

                        /* Check whether a new notification should be posted. */
                        int show_notification = 1;
                        for (int i = 0; i < 60; i++)
                        {
                            MapPos pos_ = map.PosAdd_spirally(Position, 1 + i);
                            if ((map.GetObject(pos_) >> 1) == (obj >> 1))
                            {
                                show_notification = 0;
                                break;
                            }
                        }

                        /* Create notification for found resource. */
                        if (show_notification)
                        {
                            Message.Type mtype;
                            switch (map.get_res_type(Position))
                            {
                                case Map.MineralsCoal:
                                    mtype = Message.TypeFoundCoal;
                                    break;
                                case Map.MineralsIron:
                                    mtype = Message.TypeFoundIron;
                                    break;
                                case Map.MineralsGold:
                                    mtype = Message.TypeFoundGold;
                                    break;
                                case Map.MineralsStone:
                                    mtype = Message.TypeFoundStone;
                                    break;
                                default:
                                    NOT_REACHED();
                            }
                            Game.get_player(Player).add_notification(mtype, pos,
                                                                      map.get_res_type(Position) - 1);
                        }

                        Counter += 64;
                        continue;
                    }
                }

                SerfState = State.FreeWalking);
                s.FreeWalking.NegDist1 = -128;
                s.FreeWalking.NegDist2 = 0;
                s.FreeWalking.Flags = 0;
                Counter = 0;
            }
        }

        void handle_serf_knight_engaging_building_state()
		{
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                Map map = Game.Map;
                Map.Object obj = map.GetObject(map.move_up_left(Position));
                if (obj >= Map.ObjectSmallBuilding &&
                    obj <= Map.ObjectCastle)
                {
                    Building building = Game.GetBuilding(map.GetObjectIndex(
                                                            map.move_up_left(Position)));
                    if (building.is_done() &&
                        building.is_military() &&
                        building.GetOwner() != Player &&
                        building.has_knight())
                    {
                        if (building.is_under_attack())
                        {
                            Game.get_player(building.GetOwner()).add_notification(
                                                                   Message.TypeUnderAttack,
                                                                         building.Position,
                                                                                   Player);
                        }

                        /* Change state of attacking knight */
                        Counter = 0;
                        state = State.KnightPrepareAttacking;
                        Animation = 168;

                        Serf* def_serf = building.call_defender_out();

                        s.attacking.def_index = def_serf.get_index();

                        /* Change state of defending knight */
                        set_other_state(def_serf, StateKnightLeaveForFight);
                        def_serf.s.LeavingBuilding.next_state = State.KnightPrepareDefending;
                        def_serf.Counter = 0;
                        return;
                    }
                }

                /* No one to defend this building. Occupy it. */
                SerfState = State.KnightOccupyEnemyBuilding);
                Animation = 179;
                Counter = CounterFromAnimation[Animation];
                tick = Game.Tick;
            }
        }

        void handle_serf_knight_prepare_attacking()
		{
            Serf* def_serf = Game.get_serf(s.attacking.def_index);

            if (def_serf.state == State.KnightPrepareDefending)
            {
                /* Change state of attacker. */
                SerfState = State.KnightAttacking);
                Counter = 0;
                tick = Game.Tick;

                /* Change state of defender. */
                set_other_state(def_serf, StateKnightDefending);
                def_serf.Counter = 0;

                set_fight_outcome(this, def_serf);
            }
        }

        void handle_serf_knight_leave_for_fight_state()
		{
            tick = Game.Tick;
            Counter = 0;

            if (Game.Map.GetSerfIndex(Position) == index ||
                !Game.Map.HasSerf(Position))
            {
                leave_building(1);
            }
        }

        void handle_serf_knight_prepare_defending_state()
		{
            Counter = 0;
            Animation = 84;
        }

        void handle_knight_attacking()
		{
            const int moves[] =  {
    1, 2, 4, 2, 0, 2, 4, 2, 1, 0, 2, 2, 3, 0, 0, -1,
    3, 2, 2, 3, 0, 4, 1, 3, 2, 4, 2, 2, 3, 0, 0, -1,
    2, 1, 4, 3, 2, 2, 2, 3, 0, 3, 1, 2, 0, 2, 0, -1,
    2, 1, 3, 2, 4, 2, 3, 0, 0, 4, 2, 0, 2, 1, 0, -1,
    3, 1, 0, 2, 2, 1, 0, 2, 4, 2, 2, 3, 0, 0, -1,
    0, 3, 1, 2, 3, 4, 2, 1, 2, 0, 2, 4, 0, 2, 0, -1,
    0, 2, 1, 2, 4, 2, 3, 0, 2, 4, 3, 2, 0, 0, -1,
    0, 0, 1, 4, 3, 2, 2, 1, 2, 0, 0, 4, 3, 0, -1
  };

            const int fight_anim[] = {
    24, 35, 41, 56, 67, 72, 83, 89, 100, 121, 0, 0, 0, 0, 0, 0,
    26, 40, 42, 57, 73, 74, 88, 104, 106, 120, 122, 0, 0, 0, 0, 0,
    17, 18, 23, 33, 34, 38, 39, 98, 102, 103, 113, 114, 118, 119, 0, 0,
    130, 133, 134, 135, 147, 148, 161, 162, 164, 166, 167, 0, 0, 0, 0, 0,
    50, 52, 53, 70, 129, 131, 132, 146, 149, 151, 0, 0, 0, 0, 0, 0
  };

            const int fight_anim_max[] = { 10, 11, 14, 11, 10 };

            Serf* def_serf = Game.get_serf(s.attacking.def_index);

            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            def_serf.tick = tick;
            Counter -= delta;
            def_serf.Counter = Counter;

            while (Counter < 0)
            {
                int move = moves[s.attacking.FieldB];
                if (move < 0)
                {
                    if (s.attacking.FieldC == 0)
                    {
                        /* Defender won. */
                        if (state == State.KnightAttackingFree)
                        {
                            set_other_state(def_serf, StateKnightDefendingVictoryFree);

                            def_serf.Animation = 180;
                            def_serf.Counter = 0;

                            /* Attacker dies. */
                            SerfState = State.KnightAttackingDefeatFree);
                            Animation = 152 + GetSerfType();
                            Counter = 255;
                            set_type(TypeDead);
                        }
                        else
                        {
                            /* Defender returns to building. */
                            def_serf.enter_building(-1, 1);

                            /* Attacker dies. */
                            SerfState = State.KnightAttackingDefeat);
                            Animation = 152 + GetSerfType();
                            Counter = 255;
                            set_type(TypeDead);
                        }
                    }
                    else
                    {
                        /* Attacker won. */
                        if (state == State.KnightAttackingFree)
                        {
                            SerfState = State.KnightAttackingVictoryFree);
                            Animation = 168;
                            Counter = 0;

                            s.attacking.FieldB = def_serf.s.defending_free.FieldD;
                            s.attacking.FieldC = def_serf.s.defending_free.other_dist_col;
                            s.attacking.FieldD = def_serf.s.defending_free.other_dist_row;
                        }
                        else
                        {
                            SerfState = State.KnightAttackingVictory);
                            Animation = 168;
                            Counter = 0;

                            int obj = Game.Map.GetObjectIndex(
                                                    Game.Map.move_up_left(def_serf.pos));
                            Building building = Game.GetBuilding(obj);
                            building.requested_knight_defeat_on_walk();
                        }

                        /* Defender dies. */
                        def_serf.tick = Game.Tick;
                        def_serf.Animation = 147 + GetSerfType();
                        def_serf.Counter = 255;
                        set_type(TypeDead);
                    }
                }
                else
                {
                    /* Go to next move in fight sequence. */
                    s.attacking.FieldB += 1;
                    if (s.attacking.FieldC == 0) move = 4 - move;
                    s.attacking.FieldD = move;

                    int off = (Game.RandomInt() * fight_anim_max[move]) >> 16;
                    int a = fight_anim[move * 16 + off];

                    Animation = 146 + ((a >> 4) & 0xf);
                    def_serf.Animation = 156 + (a & 0xf);
                    Counter = 72 + (Game.RandomInt() & 0x18);
                    def_serf.Counter = Counter;
                }
            }
        }

        void handle_serf_knight_attacking_victory_state()
		{
            Serf* def_serf = Game.get_serf(s.attacking.def_index);

            uint16_t delta = Game.Tick - def_serf.tick;
            def_serf.tick = Game.Tick;
            def_serf.Counter -= delta;

            if (def_serf.Counter < 0)
            {
                Game.delete_serf(def_serf);
                s.attacking.def_index = 0;

                SerfState = State.KnightEngagingBuilding);
                tick = Game.Tick;
                Counter = 0;
            }
        }

        void handle_serf_knight_attacking_defeat_state()
		{
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                Game.Map.SetSerfIndex(Position, 0);
                Game.delete_serf(this);
            }
        }

        void handle_knight_occupy_enemy_building()
		{
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter >= 0)
            {
                return;
            }

            Building building =
                            Game.GetBuildingAtPos(Game.Map.move_up_left(Position));
            if (building != NULL)
            {
                if (!building.is_burning() && building.is_military())
                {
                    if (building.GetOwner() == owner)
                    {
                        /* Enter building if there is space. */
                        if (building.GetSerfType() == Building.TypeCastle)
                        {
                            enter_building(-2, 0);
                            return;
                        }
                        else
                        {
                            if (building.is_enough_place_for_knight())
                            {
                                /* Enter building */
                                enter_building(-1, 0);
                                building.knight_occupy();
                                return;
                            }
                        }
                    }
                    else if (!building.has_knight())
                    {
                        /* Occupy the building. */
                        Game.occupy_enemy_building(building, Player);

                        if (building.GetSerfType() == Building.TypeCastle)
                        {
                            Counter = 0;
                        }
                        else
                        {
                            /* Enter building */
                            enter_building(-1, 0);
                            building.knight_occupy();
                        }
                        return;
                    }
                    else
                    {
                        SerfState = State.KnightEngagingBuilding);
                        Animation = 167;
                        Counter = 191;
                        return;
                    }
                }
            }

            /* Something is wrong. */
            SerfState = State.Lost);
            s.Lost.FieldB = 0;
            Counter = 0;
        }

        void handle_state_knight_free_walking()
		{
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            Map map = Game.Map;
            while (Counter < 0)
            {
                /* Check for enemy knights nearby. */
                for (Direction d : cycle_directions_cw())
                {
                    MapPos pos_ = map.move(Position, d);

                    if (map.HasSerf(pos_))
                    {
                        Serf* other = Game.GetSerfAtPos(pos_);
                        if (Player != other.Player)
                        {
                            if (other.state == State.KnightFreeWalking)
                            {
                                pos = map.move_left(pos_);
                                if (can_pass_map_pos(pos_))
                                {
                                    int dist_col = s.FreeWalking.Dist1;
                                    int dist_row = s.FreeWalking.Dist2;

                                    SerfState = State.KnightEngageDefendingFree);

                                    s.defending_free.dist_col = dist_col;
                                    s.defending_free.dist_row = dist_row;
                                    s.defending_free.other_dist_col = other.s.FreeWalking.Dist1;
                                    s.defending_free.other_dist_row = other.s.FreeWalking.Dist2;
                                    s.defending_free.FieldD = 1;
                                    Animation = 99;
                                    Counter = 255;

                                    set_other_state(other, StateKnightEngageAttackingFree);
                                    other.s.attacking.FieldD = d;
                                    other.s.attacking.def_index = get_index();
                                    return;
                                }
                            }
                            else if (other.state == State.Walking &&
                                     other.GetSerfType() >= Type.Knight0 &&
                                     other.GetSerfType() <= Type.Knight4)
                            {
                                pos_ = map.move_left(pos_);
                                if (can_pass_map_pos(pos_))
                                {
                                    int dist_col = s.FreeWalking.Dist1;
                                    int dist_row = s.FreeWalking.Dist2;

                                    SerfState = State.KnightEngageDefendingFree);
                                    s.defending_free.dist_col = dist_col;
                                    s.defending_free.dist_row = dist_row;
                                    s.defending_free.FieldD = 0;
                                    Animation = 99;
                                    Counter = 255;

                                    Flag dest = Game.GetFlag(other.s.Walking.dest);
                                    Building building = dest.GetBuilding();
                                    if (!building.has_inventory())
                                    {
                                        building.requested_knight_attacking_on_walk();
                                    }

                                    set_other_state(other, StateKnightEngageAttackingFree);
                                    other.s.attacking.FieldD = d;
                                    other.s.attacking.def_index = get_index();
                                    return;
                                }
                            }
                        }
                    }
                }

                HandleFreeWalkingCommon();
            }
        }

        void handle_state_knight_engage_defending_free()
		{
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0) Counter += 256;
        }

        void handle_state_knight_engage_attacking_free()
		{
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                SerfState = State.KnightEngageAttackingFreeJoin);
                Animation = 167;
                Counter += 191;
            }
        }

        void handle_state_knight_engage_attacking_free_join()
		{
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                SerfState = State.KnightPrepareAttackingFree);
                Animation = 168;
                Counter = 0;

                Serf* other = Game.get_serf(s.attacking.def_index);
                MapPos other_pos = other.pos;
                set_other_state(other, StateKnightPrepareDefendingFree);
                other.Counter = Counter;

                /* Adjust distance to final destination. */
                Direction d = (Direction)s.attacking.FieldD;
                if (d == DirectionRight || d == DirectionDownRight)
                {
                    other.s.defending_free.dist_col -= 1;
                }
                else if (d == DirectionLeft || d == DirectionUpLeft)
                {
                    other.s.defending_free.dist_col += 1;
                }

                if (d == DirectionDownRight || d == DirectionDown)
                {
                    other.s.defending_free.dist_row -= 1;
                }
                else if (d == DirectionUpLeft || d == DirectionUp)
                {
                    other.s.defending_free.dist_row += 1;
                }

                other.start_walking(d, 32, 0);
                Game.Map.SetSerfIndex(other_pos, 0);
            }
        }

        void handle_state_knight_prepare_attacking_free()
		{
            Serf* other = Game.get_serf(s.attacking.def_index);
            if (other.state == State.KnightPrepareDefendingFreeWait)
            {
                SerfState = State.KnightAttackingFree);
                Counter = 0;

                set_other_state(other, StateKnightDefendingFree);
                other.Counter = 0;

                set_fight_outcome(this, other);
            }
        }

        void handle_state_knight_prepare_defending_free()
		{
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                SerfState = State.KnightPrepareDefendingFreeWait);
                Counter = 0;
            }
        }

        void handle_knight_attacking_victory_free()
		{
            Serf* other = Game.get_serf(s.attacking.def_index);

            uint16_t delta = Game.Tick - other.tick;
            other.tick = Game.Tick;
            other.Counter -= delta;

            if (other.Counter < 0)
            {
                Game.delete_serf(other);

                int dist_col = s.attacking.FieldC;
                int dist_row = s.attacking.FieldD;

                SerfState = State.KnightAttackingFreeWait);

                s.FreeWalking.Dist1 = dist_col;
                s.FreeWalking.Dist2 = dist_row;
                s.FreeWalking.NegDist1 = 0;
                s.FreeWalking.NegDist2 = 0;

                if (s.attacking.FieldB != 0)
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

        void handle_knight_defending_victory_free()
		{
            Animation = 180;
            Counter = 0;
        }

        void handle_serf_knight_attacking_defeat_free_state()
		{
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                /* Change state of other. */
                Serf* other = Game.get_serf(s.attacking.def_index);
                int dist_col = other.s.defending_free.dist_col;
                int dist_row = other.s.defending_free.dist_row;

                set_other_state(other, StateKnightFreeWalking);

                other.s.FreeWalking.Dist1 = dist_col;
                other.s.FreeWalking.Dist2 = dist_row;
                other.s.FreeWalking.NegDist1 = 0;
                other.s.FreeWalking.NegDist2 = 0;
                other.s.FreeWalking.Flags = 0;

                other.Animation = 179;
                other.Counter = 0;
                other.tick = Game.Tick;

                /* Remove itself. */
                Game.Map.SetSerfIndex(Position, other.index);
                Game.delete_serf(this);
            }
        }

        void handle_knight_attacking_free_wait()
		{
            ushort delta = (ushort)(Game.Tick - tick);
            tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                if (s.FreeWalking.flags != 0)
                {
                    SerfState = State.KnightFreeWalking);
                }
                else
                {
                    SerfState = State.Lost);
                }

                Counter = 0;
            }
        }

        void handle_serf_state_knight_leave_for_walk_to_fight()
		{
            tick = Game.Tick;
            Counter = 0;

            Map map = Game.Map;
            if (map.GetSerfIndex(Position) != index && map.HasSerf(Position))
            {
                Animation = 82;
                Counter = 0;
                return;
            }

            Building building = Game.GetBuilding(map.GetObjectIndex(Position));
            MapPos new_pos = map.move_down_right(Position);

            if (!map.HasSerf(new_pos))
            {
                /* For clean state change, save the values first. */
                /* TODO maybe knight_leave_for_walk_to_fight can
                   share leaving_building state vars. */
                int dist_col = s.leave_for_walk_to_fight.dist_col;
                int dist_row = s.leave_for_walk_to_fight.dist_row;
                int field_D = s.leave_for_walk_to_fight.FieldD;
                int field_E = s.leave_for_walk_to_fight.FieldE;
                Serf.State next_state = s.leave_for_walk_to_fight.next_state;

                leave_building(0);
                /* TODO names for leaving_building vars make no sense here. */
                s.LeavingBuilding.FieldB = dist_col;
                s.LeavingBuilding.dest = dist_row;
                s.LeavingBuilding.dest2 = field_D;
                s.LeavingBuilding.dir = field_E;
                s.LeavingBuilding.next_state = next_state;
            }
            else
            {
                Serf* other = Game.GetSerfAtPos(new_pos);
                if (Player == other.Player)
                {
                    Animation = 82;
                    Counter = 0;
                }
                else
                {
                    /* Go back to defending the building. */
                    switch (building.GetSerfType())
                    {
                        case Building.TypeHut:
                            SerfState = State.DefendingHut);
                            break;
                        case Building.TypeTower:
                            SerfState = State.DefendingTower);
                            break;
                        case Building.TypeFortress:
                            SerfState = State.DefendingFortress);
                            break;
                        default:
                            NOT_REACHED();
                            break;
                    }

                    if (!building.knight_come_back_from_fight(this))
                    {
                        Animation = 82;
                        Counter = 0;
                    }
                }
            }
        }

        void handle_serf_idle_on_path_state()
		{
            Flag flag = s.IdleOnPath.flag;
            Direction rev_dir = s.IdleOnPath.rev_dir;

            /* Set walking dir in field_E. */
            if (flag.is_scheduled(rev_dir))
            {
                s.IdleOnPath.FieldE = (tick & 0xff) + 6;
            }
            else
            {
                Flag other_flag = flag.get_other_end_flag(rev_dir);
                Direction other_dir = flag.get_other_end_dir((Direction)rev_dir);
                if (other_flag && other_flag.is_scheduled(other_dir))
                {
                    s.IdleOnPath.FieldE = reverse_direction(rev_dir);
                }
                else
                {
                    return;
                }
            }

            Map map = Game.Map;
            if (!map.HasSerf(Position))
            {
                map.clear_idle_serf(Position);
                map.SetSerfIndex(Position, (int)Index);

                int dir = s.IdleOnPath.FieldE;

                SerfState = State.Transporting);
                s.Walking.res = Resource.TypeNone;
                s.Walking.wait_Counter = 0;
                s.Walking.dir = dir;
                tick = Game.Tick;
                Counter = 0;
            }
            else
            {
                SerfState = State.WaitIdleOnPath);
            }
        }

        void handle_serf_wait_idle_on_path_state()
		{
            Map map = Game.Map;
            if (!map.HasSerf(Position))
            {
                /* Duplicate code from handle_serf_idle_on_path_state() */
                map.clear_idle_serf(Position);
                map.SetSerfIndex(Position, (int)Index);

                int dir = s.IdleOnPath.FieldE;

                SerfState = State.Transporting);
                s.Walking.res = Resource.TypeNone;
                s.Walking.wait_Counter = 0;
                s.Walking.dir = dir;
                tick = Game.Tick;
                Counter = 0;
            }
        }

        void handle_scatter_state()
		{
            /* Choose a random, empty destination */
            while (true)
            {
                int r = Game.RandomInt();
                int col = (r & 0xf);
                if (col < 8) col -= 16;
                int row = ((r >> 8) & 0xf);
                if (row < 8) row -= 16;

                Map map = Game.Map;
                MapPos dest = map.PosAdd(Position, col, row);
                if (map.GetObject(dest) == 0 && map.get_height(dest) > 0)
                {
                    if (GetSerfType() >= Type.Knight0 && GetSerfType() <= Type.Knight4)
                    {
                        SerfState = State.KnightFreeWalking);
                    }
                    else
                    {
                        SerfState = State.FreeWalking);
                    }

                    s.FreeWalking.Dist1 = col;
                    s.FreeWalking.Dist2 = row;
                    s.FreeWalking.NegDist1 = -128;
                    s.FreeWalking.NegDist2 = -1;
                    s.FreeWalking.Flags = 0;
                    Counter = 0;
                    return;
                }
            }
        }

        void handle_serf_finished_building_state()
		{
            Map map = Game.Map;
            if (!map.HasSerf(map.move_down_right(Position)))
            {
                SerfState = State.ReadyToLeave);
                s.LeavingBuilding.dest = 0;
                s.LeavingBuilding.FieldB = -2;
                s.LeavingBuilding.dir = 0;
                s.LeavingBuilding.next_state = State.Walking;

                if (map.GetSerfIndex(Position) != index && map.HasSerf(Position))
                {
                    Animation = 82;
                }
            }
        }

        void handle_serf_wake_at_flag_state()
		{
            Map map = Game.Map;
            if (!map.HasSerf(Position))
            {
                map.clear_idle_serf(Position);
                map.SetSerfIndex(Position, (int)Index);
                tick = Game.Tick;
                Counter = 0;

                if (GetSerfType() == Type.Sailor)
                {
                    SerfState = State.LostSailor);
                }
                else
                {
                    SerfState = State.Lost);
                    s.Lost.FieldB = 0;
                }
            }
        }

        void handle_serf_wake_on_path_state()
		{
            SerfState = State.WaitIdleOnPath);

            for (Direction d : cycle_directions_ccw())
            {
                if (Misc.BitTest(Game.Map.Paths(Position), d))
                {
                    s.IdleOnPath.FieldE = d;
                    break;
                }
            }
        }

        void handle_serf_defending_state(int[] trainingParams)
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

        void handle_serf_defending_hut_state()
		{

		}

        void handle_serf_defending_tower_state()
		{

		}

        void handle_serf_defending_fortress_state()
		{

		}

        void handle_serf_defending_castle_state()
		{

		}
    }
}
