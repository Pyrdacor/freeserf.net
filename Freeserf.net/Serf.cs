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
                public uint Type; /* D */
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
                public int Field_B; /* B */
                public int Field_C; /* C */
                public int Field_D; /* D */
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
                public int Field_D; /* D */
                public int Field_E; /* E */
                public State NextState; /* F */
            }
            [FieldOffset(0)]
            public SLeaveForWalkToFight LeaveForWalkToFight;

            /* States: idle_on_path, wait_idle_on_path,
               wake_at_flag, wake_on_path. */
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SIdleOnPath
            {
                // NOTE: Flag was a Flag* before!
                public int Flag; /* C */
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

        static readonly int[] counter_from_animation = new []
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

        static readonly string[] serf_state_name = new string[]
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

        static readonly string[] serf_type_name = new string[] 
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

        }

        public void set_lost_state()
        {

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

        public void init_generic(Inventory inventory)
        {
            SetSerfType(Type.Generic);
            Player = inventory.Owner;

            Building building = Game.GetBuilding(inventory.BuildingIndex);
            Position = building.Position;
            tick = (ushort)Game.Tick;
            SerfState = State.IdleInStock;
            s.IdleInStock.InvIndex = inventory.Index;
        }

        public void init_inventory_transporter(Inventory inventory)
        {
            SerfState = State.BuildingCastle;
            s.BuildingCastle.InvIndex = inventory.Index;
        }

        public void reset_transport(Flag flag)
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

        public bool path_splited(uint flag1, Direction dir1, uint flag2, Direction dir2, ref int select)
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

        public void path_merged(Flag flag)
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

        public void path_merged2(uint flag1, Direction dir1, uint flag2, Direction dir2)
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

        public void flag_deleted(MapPos flagPos)
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

        public bool building_deleted(MapPos buildingPos, bool escape)
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
            return s.free_walking.neg_dist1;
        }

        int get_free_walking_neg_dist2()
        {
            return s.free_walking.neg_dist2;
        }

        int get_leaving_building_next_state()
        {
            return s.leaving_building.next_state;
        }

        int get_leaving_building_field_B()
        {
            return s.leaving_building.field_B;
        }

        int get_mining_res()
        {
            return s.mining.res;
        }

        int get_attacking_field_D()
        {
            return s.attacking.field_D;
        }

        int get_attacking_def_index()
        {
            return s.attacking.def_index;
        }

        int get_walking_wait_counter()
        {
            return s.walking.wait_counter;

        }
        void set_walking_wait_counter(int new_counter)
        {
            s.walking.wait_counter = new_counter;
        }

        int get_walking_dir()
        {
            return s.walking.dir;
        }

        uint get_idle_in_stock_inv_index()
        {
            return s.idle_in_stock.inv_index;
        }

        int get_mining_substate()
        {
            return s.mining.substate;
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

        }

        void send_off_to_fight(int dist_col, int dist_row)
        {

        }

        void stay_idle_in_stock(uint inventory)
        {

        }

        void go_out_from_building(MapPos dest, int dir, int field_B)
        {

        }

        void update()
        {

        }

        static string get_state_name(State state)
        {

        }

        static string get_type_name(Type type)
        {

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

        //protected:
        bool is_waiting(Direction* dir)
        {

        }

        int switch_waiting(Direction dir)
        {

        }

        int get_walking_animation(int h_diff, Direction dir, int switch_pos)
        {

        }

        void change_direction(Direction dir, int alt_end)
        {

        }

        void transporter_move_to_flag(Flag* flag)
        {

        }

        void start_walking(Direction dir, int slope, int change_pos)
        {

        }

        void enter_building(int field_B, int join_pos)
        {

        }

        void leave_building(int join_pos)
        {

        }

        void enter_inventory()
        {

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

        void set_fight_outcome(Serf* attacker, Serf* defender)
        {

        }

        static bool handle_serf_walking_state_search_cb(Flag* flag, void* data)
        {

        }

        void handle_serf_idle_in_stock_state()
        {

        }

        void handle_serf_walking_state_dest_reached()
        {

        }

        void handle_serf_walking_state_waiting()
        {

        }

        void handle_serf_walking_state()
        {

        }

        void handle_serf_transporting_state()
        {

        }

        void handle_serf_entering_building_state()
        {

        }

        void handle_serf_leaving_building_state()
        {

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
