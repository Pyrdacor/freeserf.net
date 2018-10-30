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

            /* States: move_resource_out, drop_resource_out */
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
                public int Res; /* D */
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
                // NOTE: Flag was a Flag* before! Now it is the MapPos of it.
                public uint FlagPos; /* C */
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
            SerfState = State..Null;
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
        public void set_lost_state()
        {
            if (SerfState == State..Walking)
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

                SerfState = State..Lost;
                s.Lost.FieldB = 0;
            }
            else if (SerfState == State..Transporting || SerfState == State..Delivering)
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
                    SerfState = State..Lost;
                    s.Lost.FieldB = 0;
                }
                else
                {
                    SerfState = State..LostSailor;
                }
            }
            else
            {
                SerfState = State..Lost;
                s.Lost.FieldB = 0;
            }
        }

        public void add_to_defending_queue(uint nextKnightIndex, bool pause)
        {
            SerfState = State..DefendingCastle;

            s.Defending.NextKnight = (int)nextKnightIndex;

            if (pause)
            {
                Counter = 6000;
            }
        }

        public void init_generic(Inventory inventory)
        {
            SetSerfType(Type.Generic);
            Player = inventory.Owner;

            Building building = Game.GetBuilding(inventory.BuildingIndex);
            Position = building.Position;
            tick = (ushort)Game.Tick;
            SerfState = State..IdleInStock;
            s.IdleInStock.InvIndex = inventory.Index;
        }

        public void init_inventory_transporter(Inventory inventory)
        {
            SerfState = State..BuildingCastle;
            s.BuildingCastle.InvIndex = inventory.Index;
        }

        public void reset_transport(Flag flag)
        {
            if (SerfState == State..Walking && s.Walking.Dest == flag.Index && s.Walking.Dir1 < 0)
            {
                s.Walking.Dir1 = -2;
                s.Walking.Dest = 0;
            }
            else if (SerfState == State..ReadyToLeaveInventory &&
                     s.ReadyToLeaveInventory.Dest == flag.Index &&
                     s.ReadyToLeaveInventory.Mode < 0)
            {
                s.ReadyToLeaveInventory.Mode = -2;
                s.ReadyToLeaveInventory.Dest = 0;
            }
            else if ((SerfState == State..LeavingBuilding || SerfState == State..ReadyToLeave) &&
                     s.LeavingBuilding.NextState == State..Walking &&
                     s.LeavingBuilding.Dest == flag.Index &&
                     s.LeavingBuilding.FieldB < 0)
            {
                s.LeavingBuilding.FieldB = -2;
                s.LeavingBuilding.Dest = 0;
            }
            else if (SerfState == State..Transporting &&
                     s.Walking.Dest == flag.Index)
            {
                s.Walking.Dest = 0;
            }
            else if (SerfState == State..MoveResourceOut &&
                     s.MoveResourceOut.NextState == State..DropResourceOut &&
                     s.MoveResourceOut.ResDest == flag.Index)
            {
                s.MoveResourceOut.ResDest = 0;
            }
            else if (SerfState == State..DropResourceOut &&
                     s.MoveResourceOut.ResDest == flag.Index)
            {
                s.MoveResourceOut.ResDest = 0;
            }
            else if (SerfState == State..LeavingBuilding &&
                     s.LeavingBuilding.NextState == State..DropResourceOut &&
                     s.LeavingBuilding.Dest == flag.Index)
            {
                s.LeavingBuilding.Dest = 0;
            }
        }

        public bool path_splited(uint flag1, Direction dir1, uint flag2, Direction dir2, ref int select)
        {
            if (SerfState == State..Walking)
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
            else if (SerfState == State..ReadyToLeaveInventory)
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
            else if ((SerfState == State..ReadyToLeave || SerfState == State..LeavingBuilding) &&
                     s.LeavingBuilding.NextState == State..Walking)
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

        public bool is_related_to(uint dest, Direction dir)
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
                        s.LeavingBuilding.NextState == State..Walking)
                    {
                        result = true;
                    }
                    break;
                default:
                    break;
            }

            return result;
        }

        public void path_deleted(uint dest, Direction dir)
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
                        s.LeavingBuilding.NextState == State..Walking)
                    {
                        s.LeavingBuilding.FieldB = -2;
                        s.LeavingBuilding.Dest = 0;
                    }
                    break;
                default:
                    break;
            }
        }

        public void path_merged(Flag flag)
        {
            if (SerfState == State..ReadyToLeaveInventory &&
                s.ReadyToLeaveInventory.Dest == flag.Index)
            {
                s.ReadyToLeaveInventory.Dest = 0;
                s.ReadyToLeaveInventory.Mode = -2;
            }
            else if (SerfState == State..Walking && s.Walking.Dest == flag.Index)
            {
                s.Walking.Dest = 0;
                s.Walking.Dir1 = -2;
            }
            else if (SerfState == State..IdleInStock && true/*...*/) // TODO: ?
            {
                /* TODO */
            }
            else if ((SerfState == State..LeavingBuilding || SerfState == State..ReadyToLeave) &&
                   s.LeavingBuilding.Dest == flag.Index &&
                   s.LeavingBuilding.NextState == State..Walking)
            {
                s.LeavingBuilding.Dest = 0;
                s.LeavingBuilding.FieldB = -2;
            }
        }

        public void path_merged2(uint flag1, Direction dir1, uint flag2, Direction dir2)
        {
            if (SerfState == State..ReadyToLeaveInventory &&
              ((s.ReadyToLeaveInventory.Dest == flag1 &&
                s.ReadyToLeaveInventory.Mode == (int)dir1) ||
               (s.ReadyToLeaveInventory.Dest == flag2 &&
                s.ReadyToLeaveInventory.Mode == (int)dir2)))
            {
                s.ReadyToLeaveInventory.Dest = 0;
                s.ReadyToLeaveInventory.Mode = -2;
            }
            else if (SerfState == State..Walking &&
                     ((s.Walking.Dest == flag1 && s.Walking.Dir1 == (int)dir1) ||
                      (s.Walking.Dest == flag2 && s.Walking.Dir1 == (int)dir2)))
            {
                s.Walking.Dest = 0;
                s.Walking.Dir1 = -2;
            }
            else if (SerfState == State..IdleInStock)
            {
                /* TODO */
            }
            else if ((SerfState == State..LeavingBuilding || SerfState == State..ReadyToLeave) &&
                     ((s.LeavingBuilding.Dest == flag1 &&
                       s.LeavingBuilding.FieldB == (int)dir1) ||
                      (s.LeavingBuilding.Dest == flag2 &&
                       s.LeavingBuilding.FieldB == (int)dir2)) &&
                     s.LeavingBuilding.NextState == State..Walking)
            {
                s.LeavingBuilding.Dest = 0;
                s.LeavingBuilding.FieldB = -2;
            }
        }

        public void flag_deleted(MapPos flagPos)
        {
            switch (SerfState)
            {
                case State.ReadyToLeave:
                case State.LeavingBuilding:
                    s.LeavingBuilding.NextState = State..Lost;
                    break;
                case State.FinishedBuilding:
                case State.Walking:
                    if (Game.Map.Paths(flagPos) == 0)
                    {
                        SerfState = State..Lost;
                    }
                    break;
                default:
                    break;
            }
        }

        public bool building_deleted(MapPos buildingPos, bool escape)
        {
            if (Position == buildingPos &&
                (SerfState == State..IdleInStock || SerfState == State..ReadyToLeaveInventory))
            {
                if (escape)
                {
                    /* Serf is escaping. */
                    SerfState = State..EscapeBuilding;
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

        public void castle_deleted(MapPos castlePos, bool transporter)
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
                SerfState = State..Lost;
                s.Lost.FieldB = 0;
            }
            else
            {
                SerfState = State..EscapeBuilding;
            }
        }

        public bool change_transporter_state_at_pos(MapPos pos, State state)
        {
            if (Position == pos &&
              (state == State..WakeAtFlag || state == State..WakeOnPath ||
               state == State..WaitIdleOnPath || state == State..IdleOnPath))
            {
                SerfState = state;
                return true;
            }

            return false;
        }

        public void restore_path_serf_info()
        {
            if (SerfState != State..WakeOnPath)
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
                SerfState = State..WakeAtFlag;
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
                        s.LeavingBuilding.NextState == State..Walking)
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
                        s.LeavingBuilding.NextState == State..DropResourceOut)
                    {
                        s.LeavingBuilding.Dest = 0;
                    }
                    break;
                case State.MoveResourceOut:
                    if (s.MoveResourceOut.ResDest == dest &&
                        s.MoveResourceOut.NextState == State..DropResourceOut)
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
              (SerfState == State..IdleOnPath || SerfState == State..WaitIdleOnPath ||
               SerfState == State..WakeAtFlag || SerfState == State..WakeOnPath))
            {
                SerfState = State..WakeAtFlag;
                return true;
            }
            return false;
        }

        public int get_delivery()
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

        int get_free_walking_neg_dist1()
        {
            return s.FreeWalking.NegDist1;
        }

        int get_free_walking_neg_dist2()
        {
            return s.FreeWalking.NegDist2;
        }

        int get_leaving_building_next_state()
        {
            return s.LeavingBuilding.NextState;
        }

        int get_leaving_building_field_B()
        {
            return s.LeavingBuilding.FieldB;
        }

        int get_mining_res()
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
            SerfState = State..ReadyToLeaveInventory;
            s.ReadyToLeaveInventory.Mode = mode;
            s.ReadyToLeaveInventory.Dest = dest;
            s.ReadyToLeaveInventory.InvIndex = inventory;
        }

        void send_off_to_fight(int distColumn, int distRow)
        {
            /* Send this serf off to fight. */
            SerfState = State..KnightLeaveForWalkToFight;
            s.LeaveForWalkToFight.DistColumn = distColumn;
            s.LeaveForWalkToFight.DistRow = distRow;
            s.LeaveForWalkToFight.FieldD = 0;
            s.LeaveForWalkToFight.FieldE = 0;
            s.LeaveForWalkToFight.NextState = State..KnightFreeWalking;
        }

        void stay_idle_in_stock(uint inventory)
        {
            SerfState = State..IdleInStock;
            s.IdleInStock.InvIndex = inventory;
        }

        void go_out_from_building(MapPos dest, int dir, int fieldB)
        {
            SerfState = State..ReadyToLeave;
            s.LeavingBuilding.FieldB = fieldB;
            s.LeavingBuilding.Dest = dest;
            s.LeavingBuilding.Dir = dir;
            s.LeavingBuilding.NextState = State..Walking;
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

            if ((SerfState == State..Transporting || SerfState == State..Walking ||
                 SerfState == State..Delivering) &&
                 s.Walking.Dir < 0)
            {
                dir = (Direction)(s.Walking.Dir + 6);
                return true;
            }
            else if ((SerfState == State..FreeWalking ||
                      SerfState == State..KnightFreeWalking ||
                      SerfState == State..StoneCutterFreeWalking) &&
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
            else if (SerfState == State..Digging && s.Digging.Substate < 0)
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
            if ((SerfState == State..Transporting || SerfState == State..Walking ||
                SerfState == State..Delivering) &&
                s.Walking.Dir < 0)
            {
                s.Walking.Dir = (int)dir.Reverse();
                return true;
            }
            else if ((SerfState == State..FreeWalking ||
                      SerfState == State..KnightFreeWalking ||
                      SerfState == State..StoneCutterFreeWalking) &&
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
            else if (SerfState == State..Digging && s.Digging.Substate < 0)
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
            SerfState = State..EnteringBuilding;

            StartWalking(Direction.UpLeft, 32, !joinPos);

            if (joinPos)
                Game.Map.SetSerfIndex(Position, (int)Index);

            Building building = Game.GetBuildingAtPos(Position);
            int slope = RoadBuildingSlope[(int)building.Type];

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
            int slope = 31 - RoadBuildingSlope[(int)building.Type];

            if (!building.IsDone())
                slope = 30;

            if (joinPos)
                Game.Map.SetSerfIndex(Position, 0);

            StartWalking(Direction.DownRight, slope, !joinPos);

            SerfState = State..LeavingBuilding;
        }

        void EnterInventory()
        {
            Game.Map.SetSerfIndex(Position, 0);
            Building building = Game.GetBuildingAtPos(Position);
            SerfState = State..IdleInStock;
            /*serf->s.idle_in_stock.field_B = 0;
              serf->s.idle_in_stock.field_C = 0;*/
            s.IdleInStock.InvIndex = building.GetInventory().Index;
        }

        void drop_resource(Resource.Type res)
        {

        }

        void find_inventory()
        {

        }

        bool can_pass_map_pos(MapPos pos)
        {

        }

        void set_fight_outcome(Serf attacker, Serf defender)
        {

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

                SerfState = State..ReadyToLeaveInventory;
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
                    SerfState = State..ReadyToEnter;
                }
                else
                {
                    EnterBuilding(s.Walking.Dir1, false);
                }
            }
            else if (s.Walking.Dir1 == 6)
            {
                SerfState = State..LookingForGeoSpot;
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

                SerfState = State..Transporting;
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

                    if (otherSerf.SerfState != State..Walking &&
                        otherSerf.SerfState != State..Transporting)
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
                            SerfState = State..Lost;
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
                        SerfState = State..Lost;
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
                        SerfState = State..Walking;
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
                        SerfState = State..Delivering;
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
                        SerfState = State..Lost;
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
                            SerfState = State..IdleOnPath;
                            s.IdleOnPath.RevDir = revDir;
                            s.IdleOnPath.FlagPos = flag.Position;
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

                            SerfState = State.WaitForResourceOut);
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
                            SerfState = State.Digging);
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

        void handle_serf_ready_to_enter_state()
        {

        }

        void handle_serf_ready_to_leave_state()
		{

		}

        void handle_serf_digging_state()
		{

		}

        void handle_serf_building_state()
		{

		}

        void handle_serf_building_castle_state()
		{

		}

        void handle_serf_move_resource_out_state()
		{

		}

        void handle_serf_wait_for_resource_out_state()
		{

		}

        void handle_serf_drop_resource_out_state()
		{

		}

        void handle_serf_delivering_state()
		{

		}

        void handle_serf_ready_to_leave_inventory_state()
		{

		}

        void handle_serf_free_walking_state_dest_reached()
		{

		}

        void handle_serf_free_walking_switch_on_dir(Direction dir)
		{

		}

        void handle_serf_free_walking_switch_with_other()
		{

		}

        int handle_free_walking_follow_edge()
		{

		}

        void handle_free_walking_common()
		{

		}

        void handle_serf_free_walking_state()
		{

		}

        void handle_serf_logging_state()
		{

		}

        void handle_serf_planning_logging_state()
		{

		}

        void handle_serf_planning_planting_state()
		{

		}

        void handle_serf_planting_state()
		{

		}

        void handle_serf_planning_stonecutting()
		{

		}

        void handle_stonecutter_free_walking()
		{

		}

        void handle_serf_stonecutting_state()
		{

		}

        void handle_serf_sawing_state()
		{

		}

        void handle_serf_lost_state()
		{

		}

        void handle_lost_sailor()
		{

		}

        void handle_free_sailing()
		{

		}

        void handle_serf_escape_building_state()
		{

		}

        void handle_serf_mining_state()
		{

		}

        void handle_serf_smelting_state()
		{

		}

        void handle_serf_planning_fishing_state()
		{

		}

        void handle_serf_fishing_state()
		{

		}

        void handle_serf_planning_farming_state()
		{

		}

        void handle_serf_farming_state()
		{

		}

        void handle_serf_milling_state()
		{

		}

        void handle_serf_baking_state()
		{

		}

        void handle_serf_pigfarming_state()
		{

		}

        void handle_serf_butchering_state()
		{

		}

        void handle_serf_making_weapon_state()
		{

		}

        void handle_serf_making_tool_state()
		{

		}

        void handle_serf_building_boat_state()
		{

		}

        void handle_serf_looking_for_geo_spot_state()
		{

		}

        void handle_serf_sampling_geo_spot_state()
		{

		}

        void handle_serf_knight_engaging_building_state()
		{

		}

        void handle_serf_knight_prepare_attacking()
		{

		}

        void handle_serf_knight_leave_for_fight_state()
		{

		}

        void handle_serf_knight_prepare_defending_state()
		{

		}

        void handle_knight_attacking()
		{

		}

        void handle_serf_knight_attacking_victory_state()
		{

		}

        void handle_serf_knight_attacking_defeat_state()
		{

		}

        void handle_knight_occupy_enemy_building()
		{

		}

        void handle_state_knight_free_walking()
		{

		}

        void handle_state_knight_engage_defending_free()
		{

		}

        void handle_state_knight_engage_attacking_free()
		{

		}

        void handle_state_knight_engage_attacking_free_join()
		{

		}

        void handle_state_knight_prepare_attacking_free()
		{

		}

        void handle_state_knight_prepare_defending_free()
		{

		}

        void handle_knight_attacking_victory_free()
		{

		}

        void handle_knight_defending_victory_free()
		{

		}

        void handle_serf_knight_attacking_defeat_free_state()
		{

		}

        void handle_knight_attacking_free_wait()
		{

		}

        void handle_serf_state_knight_leave_for_walk_to_fight()
		{

		}

        void handle_serf_idle_on_path_state()
		{

		}

        void handle_serf_wait_idle_on_path_state()
		{

		}

        void handle_scatter_state()
		{

		}

        void handle_serf_finished_building_state()
		{

		}

        void handle_serf_wake_at_flag_state()
		{

		}

        void handle_serf_wake_on_path_state()
		{

		}

        void handle_serf_defending_state(const int training_params[])
		{

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
