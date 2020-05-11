/*
 * Serf.cs - Serf related functions
 *
 * Copyright (C) 2013       Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Runtime.CompilerServices;

namespace Freeserf
{
    using Serialize;
    using MapPos = UInt32;

    // TODO: Give the state values plausible names instead of FieldX and so on!

    public class Serf : GameObject, IState
    {
        public enum Type : sbyte
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

        // The term FREE is used loosely in the following
        // names to denote a state where the serf is not
        // bound to a road or a flag.
        public enum State : sbyte
        {
            Invalid = -1,

            Null = 0,
            IdleInStock,
            Walking,
            Transporting,
            EnteringBuilding,
            LeavingBuilding, // 5 
            ReadyToEnter,
            ReadyToLeave,
            Digging,
            Building,
            BuildingCastle, // 10 
            MoveResourceOut,
            WaitForResourceOut,
            DropResourceOut,
            Delivering,
            ReadyToLeaveInventory, // 15 
            FreeWalking,
            Logging,
            PlanningLogging,
            PlanningPlanting,
            Planting, // 20 
            PlanningStoneCutting,
            StoneCutterFreeWalking,
            StoneCutting,
            Sawing,
            Lost, // 25 
            LostSailor,
            FreeSailing,
            EscapeBuilding,
            Mining,
            Smelting, // 30 
            PlanningFishing,
            Fishing,
            PlanningFarming,
            Farming,
            Milling, // 35 
            Baking,
            PigFarming,
            Butchering,
            MakingWeapon,
            MakingTool, // 40 
            BuildingBoat,
            LookingForGeoSpot,
            SamplingGeoSpot,
            KnightEngagingBuilding,
            KnightPrepareAttacking, // 45 
            KnightLeaveForFight,
            KnightPrepareDefending,
            KnightAttacking,
            KnightDefending,
            KnightAttackingVictory, // 50 
            KnightAttackingDefeat,
            KnightOccupyEnemyBuilding,
            KnightFreeWalking,
            KnightEngageDefendingFree,
            KnightEngageAttackingFree, // 55 
            KnightEngageAttackingFreeJoin,
            KnightPrepareAttackingFree,
            KnightPrepareDefendingFree,
            KnightPrepareDefendingFreeWait,
            KnightAttackingFree, // 60 
            KnightDefendingFree,
            KnightAttackingVictoryFree,
            KnightDefendingVictoryFree,
            KnightAttackingFreeWait,
            KnightLeaveForWalkToFight, // 65 
            IdleOnPath,
            WaitIdleOnPath,
            WakeAtFlag,
            WakeOnPath,
            DefendingHut, // 70 
            DefendingTower,
            DefendingFortress,
            Scatter,
            FinishedBuilding,
            DefendingCastle, // 75 

            // Additional state: goes at the end to ease loading of
            // original save game.
            KnightAttackingDefeatFree
        }

        internal class StateData : Serialize.State
        {
            [Data]
            public StateDataBase Data { get; private set; } = null;

            private readonly Serf serf;

            public StateData(Serf serf)
            {
                this.serf = serf;
            }

            public class StateDataBase
            {
                private readonly StateData parent;

                protected StateDataBase(StateData parent)
                {
                    this.parent = parent;
                }

                protected void MarkAsDirty()
                {
                    parent?.MarkPropertyAsDirty(nameof(Data));
                }
            }

            public class StateDataIdleInStock : StateDataBase
            {
                private uint inventoryIndex;

                public StateDataIdleInStock(StateData parent) : base(parent) { }

                public uint InventoryIndex
                {
                    get => inventoryIndex;
                    set
                    {
                        if (inventoryIndex != value)
                        {
                            inventoryIndex = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataIdleInStock IdleInStock
            {
                get
                {
                    if (Data == null || !(Data is StateDataIdleInStock))
                    {
                        if (serf.SerfState != State.IdleInStock)
                        {
                            return new StateDataIdleInStock(null);
                        }

                        Data = new StateDataIdleInStock(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataIdleInStock;
                }
            }

            // States: Walking, Transporting, Delivering 
            // Resource: resource carried (when transporting), otherwise direction. 
            public class StateDataWalking : StateDataBase
            {
                private int direction1; // newly added 
                private Resource.Type resource = Freeserf.Resource.Type.None; // B 
                private uint destination; // C 
                private int direction; // E 
                private int waitCounter; // F 

                public StateDataWalking(StateData parent) : base(parent) { }

                public int Direction1
                {
                    get => direction1;
                    set
                    {
                        if (direction1 != value)
                        {
                            direction1 = value;
                            MarkAsDirty();
                        }
                    }
                }
                public Resource.Type Resource
                {
                    get => resource;
                    set
                    {
                        if (resource != value)
                        {
                            resource = value;
                            MarkAsDirty();
                        }
                    }
                }
                public uint Destination
                {
                    get => destination;
                    set
                    {
                        if (destination != value)
                        {
                            destination = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int Direction
                {
                    get => direction;
                    set
                    {
                        if (direction != value)
                        {
                            direction = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int WaitCounter
                {
                    get => waitCounter;
                    set
                    {
                        if (waitCounter != value)
                        {
                            waitCounter = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataWalking Walking
            {
                get
                {
                    if (Data == null || !(Data is StateDataWalking))
                    {
                        if (serf.SerfState != State.Walking &&
                            serf.SerfState != State.Transporting &&
                            serf.SerfState != State.Delivering)
                        {
                            return new StateDataWalking(null);
                        }

                        Data = new StateDataWalking(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataWalking;
                }
            }

            public class StateDataEnteringBuilding : StateDataBase
            {
                // FieldB = -2: Enter inventory (castle, etc)
                private int fieldB; // B
                private int slopeLength; // C

                public StateDataEnteringBuilding(StateData parent) : base(parent) { }

                public int FieldB
                {
                    get => fieldB;
                    set
                    {
                        if (fieldB != value)
                        {
                            fieldB = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int SlopeLength
                {
                    get => slopeLength;
                    set
                    {
                        if (slopeLength != value)
                        {
                            slopeLength = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataEnteringBuilding EnteringBuilding
            {
                get
                {
                    if (Data == null || !(Data is StateDataEnteringBuilding))
                    {
                        if (serf.SerfState != State.EnteringBuilding)
                        {
                            return new StateDataEnteringBuilding(null);
                        }

                        Data = new StateDataEnteringBuilding(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataEnteringBuilding;
                }
            }

            // States: LeavingBuilding, ReadyToLeave, LeaveForFight
            public class StateDataLeavingBuilding : StateDataBase
            {
                private int fieldB; // B
                private uint destination; // C
                private int destination2; // D
                private int direction; // E
                private State nextState; // F

                public StateDataLeavingBuilding(StateData parent) : base(parent) { }

                public int FieldB
                {
                    get => fieldB;
                    set
                    {
                        if (fieldB != value)
                        {
                            fieldB = value;
                            MarkAsDirty();
                        }
                    }
                }
                public uint Destination
                {
                    get => destination;
                    set
                    {
                        if (destination != value)
                        {
                            destination = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int Destination2
                {
                    get => destination2;
                    set
                    {
                        if (destination2 != value)
                        {
                            destination2 = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int Direction
                {
                    get => direction;
                    set
                    {
                        if (direction != value)
                        {
                            direction = value;
                            MarkAsDirty();
                        }
                    }
                }
                public State NextState
                {
                    get => nextState;
                    set
                    {
                        if (nextState != value)
                        {
                            nextState = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataLeavingBuilding LeavingBuilding
            {
                get
                {
                    if (Data == null || !(Data is StateDataLeavingBuilding))
                    {
                        if (serf.SerfState != State.LeavingBuilding &&
                            serf.SerfState != State.ReadyToLeave &&
                            serf.SerfState != State.KnightLeaveForFight)
                        {
                            return new StateDataLeavingBuilding(null);
                        }

                        Data = new StateDataLeavingBuilding(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataLeavingBuilding;
                }
            }

            public class StateDataReadyToEnter : StateDataBase
            {
                private int fieldB; // B

                public StateDataReadyToEnter(StateData parent) : base(parent) { }

                public int FieldB
                {
                    get => fieldB;
                    set
                    {
                        if (fieldB != value)
                        {
                            fieldB = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataReadyToEnter ReadyToEnter
            {
                get
                {
                    if (Data == null || !(Data is StateDataReadyToEnter))
                    {
                        if (serf.SerfState != State.ReadyToEnter)
                        {
                            return new StateDataReadyToEnter(null);
                        }

                        Data = new StateDataReadyToEnter(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataReadyToEnter;
                }
            }

            public class StateDataDigging : StateDataBase
            {
                // Substate < 0 -> Wait for serf
                // Substate = 0 -> Looking for a place to dig
                // Substate = 1 -> Change height and go back to center (last step of digging)
                // Substate > 1 -> Digging
                // TargetH is the height after digging
                // DigPosition is the dig position (0 = center, 1-6 = directions)
                private int heightIndex; // B
                private uint targetHeight; // C
                private int digPosition; // D
                private int substate; // E

                public StateDataDigging(StateData parent) : base(parent) { }

                public int HeightIndex
                {
                    get => heightIndex;
                    set
                    {
                        if (heightIndex != value)
                        {
                            heightIndex = value;
                            MarkAsDirty();
                        }
                    }
                }
                public uint TargetHeight
                {
                    get => targetHeight;
                    set
                    {
                        if (targetHeight != value)
                        {
                            targetHeight = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int DigPosition
                {
                    get => digPosition;
                    set
                    {
                        if (digPosition != value)
                        {
                            digPosition = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int Substate
                {
                    get => substate;
                    set
                    {
                        if (substate != value)
                        {
                            substate = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataDigging Digging
            {
                get
                {
                    if (Data == null || !(Data is StateDataDigging))
                    {
                        if (serf.SerfState != State.Digging)
                        {
                            return new StateDataDigging(null);
                        }

                        Data = new StateDataDigging(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataDigging;
                }
            }

            // Mode: one of three substates (negative, positive, zero).
            // Index: index of building.
            public class StateDataBuilding : StateDataBase
            {
                private int mode; // B
                private uint index; // C
                private uint materialStep; // E
                private uint counter; // F

                public StateDataBuilding(StateData parent) : base(parent) { }

                public int Mode
                {
                    get => mode;
                    set
                    {
                        if (mode != value)
                        {
                            mode = value;
                            MarkAsDirty();
                        }
                    }
                }
                public uint Index
                {
                    get => index;
                    set
                    {
                        if (index != value)
                        {
                            index = value;
                            MarkAsDirty();
                        }
                    }
                }
                public uint MaterialStep
                {
                    get => materialStep;
                    set
                    {
                        if (materialStep != value)
                        {
                            materialStep = value;
                            MarkAsDirty();
                        }
                    }
                }
                public uint Counter
                {
                    get => counter;
                    set
                    {
                        if (counter != value)
                        {
                            counter = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataBuilding Building
            {
                get
                {
                    if (Data == null || !(Data is StateDataBuilding))
                    {
                        if (serf.SerfState != State.Building)
                        {
                            return new StateDataBuilding(null);
                        }

                        Data = new StateDataBuilding(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataBuilding;
                }
            }

            public class StateDataBuildingCastle : StateDataBase
            {
                private uint inventoryIndex; // C

                public StateDataBuildingCastle(StateData parent) : base(parent) { }

                public uint InventoryIndex
                {
                    get => inventoryIndex;
                    set
                    {
                        if (inventoryIndex != value)
                        {
                            inventoryIndex = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataBuildingCastle BuildingCastle
            {
                get
                {
                    if (Data == null || !(Data is StateDataBuildingCastle))
                    {
                        if (serf.SerfState != State.BuildingCastle)
                        {
                            return new StateDataBuildingCastle(null);
                        }

                        Data = new StateDataBuildingCastle(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataBuildingCastle;
                }
            }

            // States: MoveResourceOut, DropResourceOut 
            public class StateDataMoveResourceOut : StateDataBase
            {
                private uint resource; // B
                private uint resourceDestination; // C
                private State nextState; // F

                public StateDataMoveResourceOut(StateData parent) : base(parent) { }

                public uint Resource
                {
                    get => resource;
                    set
                    {
                        if (resource != value)
                        {
                            resource = value;
                            MarkAsDirty();
                        }
                    }
                }
                public uint ResourceDestination
                {
                    get => resourceDestination;
                    set
                    {
                        if (resourceDestination != value)
                        {
                            resourceDestination = value;
                            MarkAsDirty();
                        }
                    }
                }
                public State NextState
                {
                    get => nextState;
                    set
                    {
                        if (nextState != value)
                        {
                            nextState = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataMoveResourceOut MoveResourceOut
            {
                get
                {
                    if (Data == null || !(Data is StateDataMoveResourceOut))
                    {
                        if (serf.SerfState != State.MoveResourceOut &&
                            serf.SerfState != State.DropResourceOut)
                        {
                            return new StateDataMoveResourceOut(null);
                        }

                        Data = new StateDataMoveResourceOut(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataMoveResourceOut;
                }
            }

            // No state: WaitForResourceOut 

            public class StateDataReadyToLeaveInventory : StateDataBase
            {
                private int mode; // B
                private uint destination; // C
                private uint inventoryIndex; // E

                public StateDataReadyToLeaveInventory(StateData parent) : base(parent) { }

                public int Mode
                {
                    get => mode;
                    set
                    {
                        if (mode != value)
                        {
                            mode = value;
                            MarkAsDirty();
                        }
                    }
                }
                public uint Destination
                {
                    get => destination;
                    set
                    {
                        if (destination != value)
                        {
                            destination = value;
                            MarkAsDirty();
                        }
                    }
                }
                public uint InventoryIndex
                {
                    get => inventoryIndex;
                    set
                    {
                        if (inventoryIndex != value)
                        {
                            inventoryIndex = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataReadyToLeaveInventory ReadyToLeaveInventory
            {
                get
                {
                    if (Data == null || !(Data is StateDataReadyToLeaveInventory))
                    {
                        if (serf.SerfState != State.ReadyToLeaveInventory)
                        {
                            return new StateDataReadyToLeaveInventory(null);
                        }

                        Data = new StateDataReadyToLeaveInventory(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataReadyToLeaveInventory;
                }
            }

            // States: FreeWalking, Logging, Planting, Stonecutting, Fishing,
            // Farming, SamplingGeoSpot, KnightFreeWalking, KnightAttackingFree,
            // KnightAttackingFreeWait, FreeSailing, StonecutterFreeWalking
            public class StateDataFreeWalking : StateDataBase
            {
                private int distanceX; // B
                private int distanceY; // C
                private int negDistance1; // D
                private int negDistance2; // E
                private int flags; // F

                public StateDataFreeWalking(StateData parent) : base(parent) { }

                public int DistanceX
                {
                    get => distanceX;
                    set
                    {
                        if (distanceX != value)
                        {
                            distanceX = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int DistanceY
                {
                    get => distanceY;
                    set
                    {
                        if (distanceY != value)
                        {
                            distanceY = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int NegDistance1
                {
                    get => negDistance1;
                    set
                    {
                        if (negDistance1 != value)
                        {
                            negDistance1 = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int NegDistance2
                {
                    get => negDistance2;
                    set
                    {
                        if (negDistance2 != value)
                        {
                            negDistance2 = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int Flags
                {
                    get => flags;
                    set
                    {
                        if (flags != value)
                        {
                            flags = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataFreeWalking FreeWalking
            {
                get
                {
                    if (Data == null || !(Data is StateDataFreeWalking))
                    {
                        if (serf.SerfState != State.FreeWalking &&
                            serf.SerfState != State.Logging &&
                            serf.SerfState != State.Planting &&
                            serf.SerfState != State.StoneCutting &&
                            serf.SerfState != State.Fishing &&
                            serf.SerfState != State.Farming &&
                            serf.SerfState != State.SamplingGeoSpot &&
                            serf.SerfState != State.KnightFreeWalking &&
                            serf.SerfState != State.KnightAttackingFree &&
                            serf.SerfState != State.KnightAttackingFreeWait &&
                            serf.SerfState != State.FreeSailing &&
                            serf.SerfState != State.StoneCutterFreeWalking)
                        {
                            return new StateDataFreeWalking(null);
                        }

                        Data = new StateDataFreeWalking(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataFreeWalking;
                }
            }

            // No state data: PlanningLogging,
            // PlanningPlanting, PlanningStonecutting

            public class StateDataSawing : StateDataBase
            {
                private int mode; // B

                public StateDataSawing(StateData parent) : base(parent) { }

                public int Mode
                {
                    get => mode;
                    set
                    {
                        if (mode != value)
                        {
                            mode = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataSawing Sawing
            {
                get
                {
                    if (Data == null || !(Data is StateDataSawing))
                    {
                        if (serf.SerfState != State.Sawing)
                        {
                            return new StateDataSawing(null);
                        }

                        Data = new StateDataSawing(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataSawing;
                }
            }

            public class StateDataLost : StateDataBase
            {
                private int fieldB; // B

                public StateDataLost(StateData parent) : base(parent) { }

                public int FieldB
                {
                    get => fieldB;
                    set
                    {
                        if (fieldB != value)
                        {
                            fieldB = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataLost Lost
            {
                get
                {
                    if (Data == null || !(Data is StateDataLost))
                    {
                        if (serf.SerfState != State.Lost)
                        {
                            return new StateDataLost(null);
                        }

                        Data = new StateDataLost(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataLost;
                }
            }

            public class StateDataMining : StateDataBase
            {
                private uint substate; // B
                private uint resource; // D
                private Map.Minerals deposit; // E

                public StateDataMining(StateData parent) : base(parent) { }

                public uint Substate
                {
                    get => substate;
                    set
                    {
                        if (substate != value)
                        {
                            substate = value;
                            MarkAsDirty();
                        }
                    }
                }
                public uint Resource
                {
                    get => resource;
                    set
                    {
                        if (resource != value)
                        {
                            resource = value;
                            MarkAsDirty();
                        }
                    }
                }
                public Map.Minerals Deposit
                {
                    get => deposit;
                    set
                    {
                        if (deposit != value)
                        {
                            deposit = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataMining Mining
            {
                get
                {
                    if (Data == null || !(Data is StateDataMining))
                    {
                        if (serf.SerfState != State.Mining)
                        {
                            return new StateDataMining(null);
                        }

                        Data = new StateDataMining(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataMining;
                }
            }

            // Type: Type of smelter (0 is steel, else gold). 
            public class StateDataSmelting : StateDataBase
            {
                private int mode; // B
                private int counter; // C
                private int type; // D

                public StateDataSmelting(StateData parent) : base(parent) { }

                public int Mode
                {
                    get => mode;
                    set
                    {
                        if (mode != value)
                        {
                            mode = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int Counter
                {
                    get => counter;
                    set
                    {
                        if (counter != value)
                        {
                            counter = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int Type
                {
                    get => type;
                    set
                    {
                        if (type != value)
                        {
                            type = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataSmelting Smelting
            {
                get
                {
                    if (Data == null || !(Data is StateDataSmelting))
                    {
                        if (serf.SerfState != State.Smelting)
                        {
                            return new StateDataSmelting(null);
                        }

                        Data = new StateDataSmelting(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataSmelting;
                }
            }

            // No state data: PlanningFishing, PlanningFarming

            public class StateDataMilling : StateDataBase
            {
                private int mode; // B

                public StateDataMilling(StateData parent) : base(parent) { }

                public int Mode
                {
                    get => mode;
                    set
                    {
                        if (mode != value)
                        {
                            mode = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataMilling Milling
            {
                get
                {
                    if (Data == null || !(Data is StateDataMilling))
                    {
                        if (serf.SerfState != State.Milling)
                        {
                            return new StateDataMilling(null);
                        }

                        Data = new StateDataMilling(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataMilling;
                }
            }

            public class StateDataBaking : StateDataBase
            {
                private int mode; // B

                public StateDataBaking(StateData parent) : base(parent) { }

                public int Mode
                {
                    get => mode;
                    set
                    {
                        if (mode != value)
                        {
                            mode = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataBaking Baking
            {
                get
                {
                    if (Data == null || !(Data is StateDataBaking))
                    {
                        if (serf.SerfState != State.Baking)
                        {
                            return new StateDataBaking(null);
                        }

                        Data = new StateDataBaking(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataBaking;
                }
            }

            public class StateDataPigFarming : StateDataBase
            {
                private int mode; // B

                public StateDataPigFarming(StateData parent) : base(parent) { }

                public int Mode
                {
                    get => mode;
                    set
                    {
                        if (mode != value)
                        {
                            mode = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataPigFarming PigFarming
            {
                get
                {
                    if (Data == null || !(Data is StateDataPigFarming))
                    {
                        if (serf.SerfState != State.PigFarming)
                        {
                            return new StateDataPigFarming(null);
                        }

                        Data = new StateDataPigFarming(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataPigFarming;
                }
            }

            public class StateDataButchering : StateDataBase
            {
                private int mode; // B

                public StateDataButchering(StateData parent) : base(parent) { }

                public int Mode
                {
                    get => mode;
                    set
                    {
                        if (mode != value)
                        {
                            mode = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataButchering Butchering
            {
                get
                {
                    if (Data == null || !(Data is StateDataButchering))
                    {
                        if (serf.SerfState != State.Butchering)
                        {
                            return new StateDataButchering(null);
                        }

                        Data = new StateDataButchering(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataButchering;
                }
            }

            public class StateDataMakingWeapon : StateDataBase
            {
                private int mode; // B

                public StateDataMakingWeapon(StateData parent) : base(parent) { }

                public int Mode
                {
                    get => mode;
                    set
                    {
                        if (mode != value)
                        {
                            mode = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataMakingWeapon MakingWeapon
            {
                get
                {
                    if (Data == null || !(Data is StateDataMakingWeapon))
                    {
                        if (serf.SerfState != State.MakingWeapon)
                        {
                            return new StateDataMakingWeapon(null);
                        }

                        Data = new StateDataMakingWeapon(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataMakingWeapon;
                }
            }

            public class StateDataMakingTool : StateDataBase
            {
                private int mode; // B

                public StateDataMakingTool(StateData parent) : base(parent) { }

                public int Mode
                {
                    get => mode;
                    set
                    {
                        if (mode != value)
                        {
                            mode = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataMakingTool MakingTool
            {
                get
                {
                    if (Data == null || !(Data is StateDataMakingTool))
                    {
                        if (serf.SerfState != State.MakingTool)
                        {
                            return new StateDataMakingTool(null);
                        }

                        Data = new StateDataMakingTool(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataMakingTool;
                }
            }

            public class StateDataBuildingBoat : StateDataBase
            {
                private int mode; // B

                public StateDataBuildingBoat(StateData parent) : base(parent) { }

                public int Mode
                {
                    get => mode;
                    set
                    {
                        if (mode != value)
                        {
                            mode = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataBuildingBoat BuildingBoat
            {
                get
                {
                    if (Data == null || !(Data is StateDataBuildingBoat))
                    {
                        if (serf.SerfState != State.BuildingBoat)
                        {
                            return new StateDataBuildingBoat(null);
                        }

                        Data = new StateDataBuildingBoat(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataBuildingBoat;
                }
            }

            // No state data: LookingForGeoSpot 

            // States: KnightEngagingBuilding, KnightPrepareAttacking,
            // KnightPrepareDefendingFreeWait, KnightAttackingDefeatFree,
            // KnightAttacking, KnightAttackingVictory, KnightEngageAttackingFree,
            // KnightEngageAttackingFreeJoin
            public class StateDataAttacking : StateDataBase
            {
                private int move; // B
                private int attackerWon; // C
                private int fieldD; // D
                private int defenderIndex; // E

                public StateDataAttacking(StateData parent) : base(parent) { }

                public int Move
                {
                    get => move;
                    set
                    {
                        if (move != value)
                        {
                            move = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int AttackerWon
                {
                    get => attackerWon;
                    set
                    {
                        if (attackerWon != value)
                        {
                            attackerWon = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int FieldD
                {
                    get => fieldD;
                    set
                    {
                        if (fieldD != value)
                        {
                            fieldD = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int DefenderIndex
                {
                    get => defenderIndex;
                    set
                    {
                        if (defenderIndex != value)
                        {
                            defenderIndex = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataAttacking Attacking
            {
                get
                {
                    if (Data == null || !(Data is StateDataAttacking))
                    {
                        if (serf.SerfState != State.KnightEngagingBuilding &&
                            serf.SerfState != State.KnightPrepareAttacking &&
                            serf.SerfState != State.KnightPrepareDefendingFreeWait &&
                            serf.SerfState != State.KnightAttackingDefeatFree &&
                            serf.SerfState != State.KnightAttacking &&
                            serf.SerfState != State.KnightAttackingVictory &&
                            serf.SerfState != State.KnightEngageAttackingFree &&
                            serf.SerfState != State.KnightEngageAttackingFreeJoin)
                        {
                            return new StateDataAttacking(null);
                        }

                        Data = new StateDataAttacking(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataAttacking;
                }
            }

            // States: KnightAttackingVictoryFree
            public class StateDataAttackingVictoryFree : StateDataBase
            {
                private int move; // B
                private int distanceColumn; // C
                private int distanceRow; // D
                private int defenderIndex; // E

                public StateDataAttackingVictoryFree(StateData parent) : base(parent) { }

                public int Move
                {
                    get => move;
                    set
                    {
                        if (move != value)
                        {
                            move = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int DistanceColumn
                {
                    get => distanceColumn;
                    set
                    {
                        if (distanceColumn != value)
                        {
                            distanceColumn = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int DistanceRow
                {
                    get => distanceRow;
                    set
                    {
                        if (distanceRow != value)
                        {
                            distanceRow = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int DefenderIndex
                {
                    get => defenderIndex;
                    set
                    {
                        if (defenderIndex != value)
                        {
                            defenderIndex = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataAttackingVictoryFree AttackingVictoryFree
            {
                get
                {
                    if (Data == null || !(Data is StateDataAttackingVictoryFree))
                    {
                        if (serf.SerfState != State.KnightAttackingVictoryFree)
                        {
                            return new StateDataAttackingVictoryFree(null);
                        }

                        Data = new StateDataAttackingVictoryFree(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataAttackingVictoryFree;
                }
            }

            // States: KnightDefendingFree, KnightEngageDefendingFree
            public class StateDataDefendingFree : StateDataBase
            {
                private int distanceColumn; // B
                private int distanceRow; // C
                private int fieldD; // D
                private int otherDistanceColumn; // E
                private int otherDistanceRow; // F

                public StateDataDefendingFree(StateData parent) : base(parent) { }

                public int DistanceColumn
                {
                    get => distanceColumn;
                    set
                    {
                        if (distanceColumn != value)
                        {
                            distanceColumn = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int DistanceRow
                {
                    get => distanceRow;
                    set
                    {
                        if (distanceRow != value)
                        {
                            distanceRow = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int FieldD
                {
                    get => fieldD;
                    set
                    {
                        if (fieldD != value)
                        {
                            fieldD = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int OtherDistanceColumn
                {
                    get => otherDistanceColumn;
                    set
                    {
                        if (otherDistanceColumn != value)
                        {
                            otherDistanceColumn = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int OtherDistanceRow
                {
                    get => otherDistanceRow;
                    set
                    {
                        if (otherDistanceRow != value)
                        {
                            otherDistanceRow = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataDefendingFree DefendingFree
            {
                get
                {
                    if (Data == null || !(Data is StateDataDefendingFree))
                    {
                        if (serf.SerfState != State.KnightDefendingFree &&
                            serf.SerfState != State.KnightEngageDefendingFree)
                        {
                            return new StateDataDefendingFree(null);
                        }

                        Data = new StateDataDefendingFree(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataDefendingFree;
                }
            }

            public class StateDataLeaveForWalkToFight : StateDataBase
            {
                private int distanceColumn; // B
                private int distanceRow; // C
                private int fieldD; // D
                private int fieldE; // E
                private State nextState; // F

                public StateDataLeaveForWalkToFight(StateData parent) : base(parent) { }

                public int DistanceColumn
                {
                    get => distanceColumn;
                    set
                    {
                        if (distanceColumn != value)
                        {
                            distanceColumn = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int DistanceRow
                {
                    get => distanceRow;
                    set
                    {
                        if (distanceRow != value)
                        {
                            distanceRow = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int FieldD
                {
                    get => fieldD;
                    set
                    {
                        if (fieldD != value)
                        {
                            fieldD = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int FieldE
                {
                    get => fieldE;
                    set
                    {
                        if (fieldE != value)
                        {
                            fieldE = value;
                            MarkAsDirty();
                        }
                    }
                }
                public State NextState
                {
                    get => nextState;
                    set
                    {
                        if (nextState != value)
                        {
                            nextState = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataLeaveForWalkToFight LeaveForWalkToFight
            {
                get
                {
                    if (Data == null || !(Data is StateDataLeaveForWalkToFight))
                    {
                        if (serf.SerfState != State.KnightLeaveForWalkToFight)
                        {
                            return new StateDataLeaveForWalkToFight(null);
                        }

                        Data = new StateDataLeaveForWalkToFight(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataLeaveForWalkToFight;
                }
            }

            // States: IdleOnPath, WaitIdleOnPath, WakeAtFlag, WakeOnPath.
            public class StateDataIdleOnPath : StateDataBase
            {
                // NOTE: Flag was a Flag* before! Now it is the index of it.
                private uint flagIndex; // C
                private int fieldE; // E
                private Direction reverseDirection; // B

                public StateDataIdleOnPath(StateData parent) : base(parent) { }

                public uint FlagIndex
                {
                    get => flagIndex;
                    set
                    {
                        if (flagIndex != value)
                        {
                            flagIndex = value;
                            MarkAsDirty();
                        }
                    }
                }
                public int FieldE
                {
                    get => fieldE;
                    set
                    {
                        if (fieldE != value)
                        {
                            fieldE = value;
                            MarkAsDirty();
                        }
                    }
                }
                public Direction ReverseDirection
                {
                    get => reverseDirection;
                    set
                    {
                        if (reverseDirection != value)
                        {
                            reverseDirection = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataIdleOnPath IdleOnPath
            {
                get
                {
                    if (Data == null || !(Data is StateDataIdleOnPath))
                    {
                        if (serf.SerfState != State.IdleOnPath &&
                            serf.SerfState != State.WaitIdleOnPath &&
                            serf.SerfState != State.WakeAtFlag &&
                            serf.SerfState != State.WakeOnPath)
                        {
                            return new StateDataIdleOnPath(null);
                        }

                        Data = new StateDataIdleOnPath(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataIdleOnPath;
                }
            }

            // No state data: FinishedBuilding 

            // States: DefendingHut, DefendingTower,
            // DefendingFortress, DefendingCastle
            public class StateDataDefending : StateDataBase
            {
                private uint nextKnight; // E

                public StateDataDefending(StateData parent) : base(parent) { }

                public uint NextKnight
                {
                    get => nextKnight;
                    set
                    {
                        if (nextKnight != value)
                        {
                            nextKnight = value;
                            MarkAsDirty();
                        }
                    }
                }
            }
            public StateDataDefending Defending
            {
                get
                {
                    if (Data == null || !(Data is StateDataDefending))
                    {
                        if (serf.SerfState != State.DefendingHut &&
                            serf.SerfState != State.DefendingTower &&
                            serf.SerfState != State.DefendingFortress &&
                            serf.SerfState != State.DefendingCastle)
                        {
                            return new StateDataDefending(null);
                        }

                        Data = new StateDataDefending(this);
                        MarkPropertyAsDirty(nameof(Data));
                    }

                    return Data as StateDataDefending;
                }
            }
        }

        static readonly int[] CounterFromAnimation = new int[]
        {
            // Walking (0-80) 
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,
            511, 447, 383, 319, 255, 319, 511, 767, 1023,

            // Waiting (81-86) 
            127, 127, 127, 127, 127, 127,

            // Digging (87-88) 
            383, 383,

            255, 223, 191, 159, 127, 159, 255, 383,  511,

            // Building (98) 
            255,

            // Engage defending free (99) 
            255,

            // Building large building (100) 
            255,

            0,

            // Building (102-105) 
            767, 511, 511, 767,

            1023, 639, 639, 1023,

            // Transporting (turning?) (110-115) 
            63, 63, 63, 63, 63, 63,

            // Logging (116-120) 
            1023, 31, 767, 767, 255,

            // Planting (121-122) 
            191, 127,

            // Stonecutting (123) 
            1535,

            // Sawing (124) 
            2367,

            // Mining (125-128) 
            383, 303, 303, 383,

            // Smelting (129-130) 
            383, 383,

            // Fishing (131-134) 
            767, 767, 127, 127,

            // Farming (135-136) 
            1471, 1983,

            // Milling (137) 
            383,

            // Baking (138) 
            767,

            // Pig farming (139) 
            383,

            // Butchering (140) 
            1535,

            // Sampling geology (142) 
            783, 63,

            // Making weapon (143) 
            575,

            // Making tool (144) 
            1535,

            // Building boat (145-146) 
            1407, 159,

            // Attacking (147-156) 
            127, 127, 127, 127, 127, 127, 127, 127, 127, 127,

            // Defending (157-166) 
            127, 127, 127, 127, 127, 127, 127, 127, 127, 127,

            // Engage attacking (167) 
            191,

            // Victory attacking (168) 
            7,

            // Dying attacking (169-173) 
            255, 255, 255, 255, 255,

            // Dying defending (174-178) 
            255, 255, 255, 255, 255,

            // Occupy attacking (179) 
            127,

            // Victory defending (180) 
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
            // Finished building 
            5, 18, 18, 15, 18, 22, 22, 22,
            22, 18, 16, 18, 1, 10, 1, 15,
            15, 16, 15, 15, 10, 15, 20, 15,
            18
        };

        [Data]
        private SerfState state = new SerfState();
        readonly StateData s = null;

        public Serf(Game game, uint index)
            : base(game, index)
        {
            s = new StateData(this);
        }

        public bool Dirty => state.Dirty;

        public uint Player
        {
            get => state.Player;
            set => state.Player = (byte)value;
        }
        public State SerfState
        {
            get => state.State;
            private set
            {
                if (state.State == value)
                    return;

                state.State = value;

                switch (state.State)
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
        /// <summary>
        /// Index to animation table in data file.
        /// </summary>
        public int Animation
        {
            get => state.Animation;
            private set => state.Animation = (byte)value;
        }
        public int Counter
        {
            get => state.Counter;
            private set => state.Counter = value;
        }
        public MapPos Position
        {
            get => state.Position;
            set => state.Position = value;
        }

        public void ResetDirtyFlag()
        {
            state.ResetDirtyFlag();
        }

        void SetState(State newState, [CallerMemberName] string function = "", [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                Log.Verbose.Write(ErrorSystemType.Serf, $"serf {Index} ({SerfTypeNames[(int)state.Type]}): state {SerfStateNames[(int)SerfState]} -> {SerfStateNames[(int)newState]} ({function}:{lineNumber})");
            }
            catch
            {
                Log.Verbose.Write(ErrorSystemType.Serf, $"Missing serf type name or serf state name: serf type name index = {(int)state.Type}, serf state name index = {(int)SerfState}");
            }

            SerfState = newState;
        }

        static void SetOtherState(Serf otherSerf, State newState, [CallerMemberName] string function = "", [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                Log.Verbose.Write(ErrorSystemType.Serf, $"serf {otherSerf.Index} ({SerfTypeNames[(int)otherSerf.state.Type]}): state {SerfStateNames[(int)otherSerf.SerfState]} -> {SerfStateNames[(int)newState]} ({function}:{lineNumber})");
            }
            catch
            {
                Log.Verbose.Write(ErrorSystemType.Serf, $"Missing other serf type name or other serf state name: serf type name index = {(int)otherSerf.state.Type}, serf state name index = {(int)otherSerf.SerfState}");
            }

            otherSerf.SerfState = newState;
        }

        public Type SerfType
        {
            get => state.Type;
            set
            {
                var oldType = state.Type;
                var newType = value;

                if (oldType == newType)
                    return;

                state.Type = newType;

                // Register this type as transporter 
                if (newType == Type.TransporterInventory)
                    newType = Type.Transporter;
                if (oldType == Type.TransporterInventory)
                    oldType = Type.Transporter;

                var player = Game.GetPlayer(Player);

                if (oldType != Type.None)
                {
                    player.DecreaseSerfCount(oldType);
                }
                if (state.Type != Type.Dead && state.Type != Type.Generic) // generic count is increased on creating
                {
                    player.IncreaseSerfCount(newType);
                }

                if (oldType >= Type.Knight0 &&
                    oldType <= Type.Knight4)
                {
                    uint score = 1u << (oldType - Type.Knight0);
                    player.DecreaseMilitaryScore(score);
                }
                if (newType >= Type.Knight0 &&
                    newType <= Type.Knight4)
                {
                    uint score = 1u << (state.Type - Type.Knight0);
                    player.IncreaseMilitaryScore(score);
                }
                if (newType == Type.Transporter)
                {
                    state.Counter = 0;
                }
            }
        }

        public bool IsKnight => state.Type >= Type.Knight0 && state.Type <= Type.Knight4;

        public bool IsPlayingSfx => state.PlayingSfx;

        public void StartPlayingSfx()
        {
            state.PlayingSfx = true;
        }

        public void StopPlayingSfx()
        {
            state.PlayingSfx = false;
        }

        public bool TrainKnight(int probability)
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            while (state.Counter < 0)
            {
                if (Game.RandomInt() < probability)
                {
                    // Level up 
                    ++SerfType;
                    Counter = 6000;

                    return true;
                }

                Counter += 6000;
            }

            return false;
        }

        // Change serf state to lost, but make necessary clean up
        // from any earlier state first.
        public void SetLostState()
        {
            if (SerfState == State.Walking)
            {
                if (s.Walking.Direction1 >= 0)
                {
                    if (s.Walking.Direction1 != 6)
                    {
                        var direction = (Direction)s.Walking.Direction1;
                        var flag = Game.GetFlag(s.Walking.Destination);
                        flag.CancelSerfRequest(direction);

                        var otherDirection = flag.GetOtherEndDirection(direction);
                        flag.GetOtherEndFlag(direction).CancelSerfRequest(otherDirection);
                    }
                }
                else if (s.Walking.Direction1 == -1)
                {
                    var flag = Game.GetFlag(s.Walking.Destination);
                    var building = flag.Building;
                    building.RequestedSerfLost();
                }

                SetState(State.Lost);
                s.Lost.FieldB = 0;
            }
            else if (SerfState == State.Transporting || SerfState == State.Delivering)
            {
                if (s.Walking.Resource != Resource.Type.None)
                {
                    var resource = s.Walking.Resource;
                    var destination = s.Walking.Destination;

                    Game.CancelTransportedResource(resource, destination);
                    Game.LoseResource(resource);
                }

                if (SerfType != Type.Sailor)
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

        /// <summary>
        /// This is used to resolve some traffic loops.
        /// </summary>
        public void PutBackToInventory(Inventory inventory)
        {
            if (SerfState == State.Walking)
            {
                if (s.Walking.Direction1 >= 0)
                {
                    if (s.Walking.Direction1 != 6)
                    {
                        var direction = (Direction)s.Walking.Direction1;
                        var flag = Game.GetFlag(s.Walking.Destination);
                        flag.CancelSerfRequest(direction);

                        var otherDirection = flag.GetOtherEndDirection(direction);
                        flag.GetOtherEndFlag(direction).CancelSerfRequest(otherDirection);
                    }
                }
                else if (s.Walking.Direction1 == -1)
                {
                    var flag = Game.GetFlag(s.Walking.Destination);
                    var building = flag.Building;
                    building.RequestedSerfLost();
                }
            }
            else if (SerfState == State.Transporting || SerfState == State.Delivering)
            {
                if (s.Walking.Resource != Resource.Type.None)
                {
                    var resource = s.Walking.Resource;
                    var destination = s.Walking.Destination;

                    Game.CancelTransportedResource(resource, destination);
                    inventory.PushResource(resource);
                }
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
            SerfType = Type.Generic;
            Player = inventory.Player;

            var building = Game.GetBuilding(inventory.Building);
            Position = building.Position;
            state.Tick = Game.Tick;
            SetState(State.IdleInStock);
            s.IdleInStock.InventoryIndex = inventory.Index;
        }

        public void InitInventoryTransporter(Inventory inventory)
        {
            SetState(State.BuildingCastle);
            s.BuildingCastle.InventoryIndex = inventory.Index;
        }

        public void ResetTransport(Flag flag)
        {
            if (SerfState == State.Walking && s.Walking.Destination == flag.Index && s.Walking.Direction1 < 0)
            {
                s.Walking.Direction1 = -2;
                s.Walking.Destination = 0;
            }
            else if (SerfState == State.ReadyToLeaveInventory &&
                     s.ReadyToLeaveInventory.Destination == flag.Index &&
                     s.ReadyToLeaveInventory.Mode < 0)
            {
                s.ReadyToLeaveInventory.Mode = -2;
                s.ReadyToLeaveInventory.Destination = 0;
            }
            else if ((SerfState == State.LeavingBuilding || SerfState == State.ReadyToLeave) &&
                     s.LeavingBuilding.NextState == State.Walking &&
                     s.LeavingBuilding.Destination == flag.Index &&
                     s.LeavingBuilding.FieldB < 0)
            {
                s.LeavingBuilding.FieldB = -2;
                s.LeavingBuilding.Destination = 0;
            }
            else if (SerfState == State.Transporting &&
                     s.Walking.Destination == flag.Index)
            {
                s.Walking.Destination = 0;
            }
            else if (SerfState == State.MoveResourceOut &&
                     s.MoveResourceOut.NextState == State.DropResourceOut &&
                     s.MoveResourceOut.ResourceDestination == flag.Index)
            {
                s.MoveResourceOut.ResourceDestination = 0;
            }
            else if (SerfState == State.DropResourceOut &&
                     s.MoveResourceOut.ResourceDestination == flag.Index)
            {
                s.MoveResourceOut.ResourceDestination = 0;
            }
            else if (SerfState == State.LeavingBuilding &&
                     s.LeavingBuilding.NextState == State.DropResourceOut &&
                     s.LeavingBuilding.Destination == flag.Index)
            {
                s.LeavingBuilding.Destination = 0;
            }
        }

        public bool PathSplited(uint flag1, Direction direction1, uint flag2, Direction direction2, ref int select)
        {
            if (SerfState == State.Walking)
            {
                if (s.Walking.Destination == flag1 && s.Walking.Direction1 == (int)direction1)
                {
                    select = 0; // TODO: change required?
                    return true;
                }
                else if (s.Walking.Destination == flag2 && s.Walking.Direction1 == (int)direction2)
                {
                    select = 1;
                    return true;
                }
            }
            else if (SerfState == State.ReadyToLeaveInventory)
            {
                if (s.ReadyToLeaveInventory.Destination == flag1 &&
                    s.ReadyToLeaveInventory.Mode == (int)direction1)
                {
                    select = 0; // TODO: change required?
                    return true;
                }
                else if (s.ReadyToLeaveInventory.Destination == flag2 &&
                         s.ReadyToLeaveInventory.Mode == (int)direction2)
                {
                    select = 1;
                    return true;
                }
            }
            else if ((SerfState == State.ReadyToLeave || SerfState == State.LeavingBuilding) &&
                     s.LeavingBuilding.NextState == State.Walking)
            {
                if (s.LeavingBuilding.Destination == flag1 &&
                    s.LeavingBuilding.FieldB == (int)direction1)
                {
                    select = 0; // TODO: change required?
                    return true;
                }
                else if (s.LeavingBuilding.Destination == flag2 &&
                         s.LeavingBuilding.FieldB == (int)direction2)
                {
                    select = 1;
                    return true;
                }
            }

            return false;
        }

        public bool IsRelatedTo(uint destination, Direction direction)
        {
            bool result = false;

            switch (SerfState)
            {
                case State.Walking:
                    if (s.Walking.Destination == destination && s.Walking.Direction1 == (int)direction)
                    {
                        result = true;
                    }
                    break;
                case State.ReadyToLeaveInventory:
                    if (s.ReadyToLeaveInventory.Destination == destination &&
                        s.ReadyToLeaveInventory.Mode == (int)direction)
                    {
                        result = true;
                    }
                    break;
                case State.LeavingBuilding:
                case State.ReadyToLeave:
                    if (s.LeavingBuilding.Destination == destination &&
                        s.LeavingBuilding.FieldB == (int)direction &&
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

        public void PathDeleted(uint destination, Direction direction)
        {
            switch (SerfState)
            {
                case State.Walking:
                    if (s.Walking.Destination == destination && s.Walking.Direction1 == (int)direction)
                    {
                        s.Walking.Direction1 = -2;
                        s.Walking.Destination = 0;
                    }
                    break;
                case State.ReadyToLeaveInventory:
                    if (s.ReadyToLeaveInventory.Destination == destination &&
                        s.ReadyToLeaveInventory.Mode == (int)direction)
                    {
                        s.ReadyToLeaveInventory.Mode = -2;
                        s.ReadyToLeaveInventory.Destination = 0;
                    }
                    break;
                case State.LeavingBuilding:
                case State.ReadyToLeave:
                    if (s.LeavingBuilding.Destination == destination &&
                        s.LeavingBuilding.FieldB == (int)direction &&
                        s.LeavingBuilding.NextState == State.Walking)
                    {
                        s.LeavingBuilding.FieldB = -2;
                        s.LeavingBuilding.Destination = 0;
                    }
                    break;
                default:
                    break;
            }
        }

        public void PathMerged(Flag flag)
        {
            if (SerfState == State.ReadyToLeaveInventory &&
                s.ReadyToLeaveInventory.Destination == flag.Index)
            {
                s.ReadyToLeaveInventory.Destination = 0;
                s.ReadyToLeaveInventory.Mode = -2;
            }
            else if (SerfState == State.Walking && s.Walking.Destination == flag.Index)
            {
                s.Walking.Destination = 0;
                s.Walking.Direction1 = -2;
            }
            else if (SerfState == State.IdleInStock && true/*...*/) // TODO: ?
            {
                // TODO 
            }
            else if ((SerfState == State.LeavingBuilding || SerfState == State.ReadyToLeave) &&
                   s.LeavingBuilding.Destination == flag.Index &&
                   s.LeavingBuilding.NextState == State.Walking)
            {
                s.LeavingBuilding.Destination = 0;
                s.LeavingBuilding.FieldB = -2;
            }
        }

        public void PathMerged2(uint flag1Index, Direction direction1, uint flag2Index, Direction direction2)
        {
            if (SerfState == State.ReadyToLeaveInventory &&
              ((s.ReadyToLeaveInventory.Destination == flag1Index &&
                s.ReadyToLeaveInventory.Mode == (int)direction1) ||
               (s.ReadyToLeaveInventory.Destination == flag2Index &&
                s.ReadyToLeaveInventory.Mode == (int)direction2)))
            {
                s.ReadyToLeaveInventory.Destination = 0;
                s.ReadyToLeaveInventory.Mode = -2;
            }
            else if (SerfState == State.Walking &&
                     ((s.Walking.Destination == flag1Index && s.Walking.Direction1 == (int)direction1) ||
                      (s.Walking.Destination == flag2Index && s.Walking.Direction1 == (int)direction2)))
            {
                s.Walking.Destination = 0;
                s.Walking.Direction1 = -2;
            }
            else if (SerfState == State.IdleInStock)
            {
                // TODO 
            }
            else if ((SerfState == State.LeavingBuilding || SerfState == State.ReadyToLeave) &&
                     ((s.LeavingBuilding.Destination == flag1Index &&
                       s.LeavingBuilding.FieldB == (int)direction1) ||
                      (s.LeavingBuilding.Destination == flag2Index &&
                       s.LeavingBuilding.FieldB == (int)direction2)) &&
                     s.LeavingBuilding.NextState == State.Walking)
            {
                s.LeavingBuilding.Destination = 0;
                s.LeavingBuilding.FieldB = -2;
            }
        }

        public void FlagDeleted(MapPos flagPosition)
        {
            switch (SerfState)
            {
                case State.ReadyToLeave:
                case State.LeavingBuilding:
                    s.LeavingBuilding.NextState = State.Lost;
                    break;
                case State.FinishedBuilding:
                case State.Walking:
                    if (Game.Map.Paths(flagPosition) == 0)
                    {
                        SetState(State.Lost);
                    }
                    break;
                default:
                    break;
            }
        }

        public bool BuildingDeleted(MapPos buildingPosition, bool escape)
        {
            if (Position == buildingPosition &&
                (SerfState == State.IdleInStock || SerfState == State.ReadyToLeaveInventory))
            {
                if (escape)
                {
                    // Serf is escaping. 
                    SetState(State.EscapeBuilding);
                }
                else
                {
                    // Kill this serf. 
                    SerfType = Type.Dead;
                    Game.DeleteSerf(this);
                }

                return true;
            }
            else if ((state.Type == Type.Builder && SerfState == State.Building) || (state.Type == Type.Digger && SerfState == State.Digging))
            {
                SetLostState();
            }
            else
            {
                SetState(State.EscapeBuilding);
            }

            return false;
        }

        public void CastleDeleted(MapPos castlePos, bool transporter)
        {
            // TODO: There seem to be a null-serf in the castle. Maybe delete later?
            if (state.Type == Type.None || Position == Global.INVALID_MAPPOS)
                return;

            if ((!transporter || SerfType == Type.TransporterInventory) &&
                Position == castlePos)
            {
                if (transporter)
                {
                    SerfType = Type.Transporter;
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

        public bool ChangeTransporterStateAtPosition(MapPos position, State state)
        {
            if (Position == position &&
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

                if (s.Walking.Resource != Resource.Type.None)
                {
                    var resource = s.Walking.Resource;
                    s.Walking.Resource = Resource.Type.None;

                    Game.CancelTransportedResource(resource, s.Walking.Destination);
                    Game.LoseResource(resource);
                }
            }
            else
            {
                SetState(State.WakeAtFlag);
            }
        }

        public void ClearDestination(uint destination)
        {
            switch (SerfState)
            {
                case State.Walking:
                    if (s.Walking.Destination == destination && s.Walking.Direction1 < 0)
                    {
                        s.Walking.Direction1 = -2;
                        s.Walking.Destination = 0;
                    }
                    break;
                case State.ReadyToLeaveInventory:
                    if (s.ReadyToLeaveInventory.Destination == destination &&
                        s.ReadyToLeaveInventory.Mode < 0)
                    {
                        s.ReadyToLeaveInventory.Mode = -2;
                        s.ReadyToLeaveInventory.Destination = 0;
                    }
                    break;
                case State.LeavingBuilding:
                case State.ReadyToLeave:
                    if (s.LeavingBuilding.Destination == destination &&
                        s.LeavingBuilding.FieldB < 0 &&
                        s.LeavingBuilding.NextState == State.Walking)
                    {
                        s.LeavingBuilding.FieldB = -2;
                        s.LeavingBuilding.Destination = 0;
                    }
                    break;
                default:
                    break;
            }
        }

        public void ClearDestination2(uint destination)
        {
            switch (SerfState)
            {
                case State.Transporting:
                    if (s.Walking.Destination == destination)
                    {
                        s.Walking.Destination = 0;
                    }
                    break;
                case State.DropResourceOut:
                    if (s.MoveResourceOut.ResourceDestination == destination)
                    {
                        s.MoveResourceOut.ResourceDestination = 0;
                    }
                    break;
                case State.LeavingBuilding:
                    if (s.LeavingBuilding.Destination == destination &&
                        s.LeavingBuilding.NextState == State.DropResourceOut)
                    {
                        s.LeavingBuilding.Destination = 0;
                    }
                    break;
                case State.MoveResourceOut:
                    if (s.MoveResourceOut.ResourceDestination == destination &&
                        s.MoveResourceOut.NextState == State.DropResourceOut)
                    {
                        s.MoveResourceOut.ResourceDestination = 0;
                    }
                    break;
                default:
                    break;
            }
        }

        public bool IdleToWaitState(MapPos position)
        {
            if (Position == position &&
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
            int resource = 0;

            switch (SerfState)
            {
                case State.Delivering:
                case State.Transporting:
                    resource = (int)s.Walking.Resource + 1;
                    break;
                case State.EnteringBuilding:
                    resource = s.EnteringBuilding.FieldB;
                    break;
                case State.LeavingBuilding:
                    resource = s.LeavingBuilding.FieldB;
                    break;
                case State.ReadyToEnter:
                    resource = s.ReadyToEnter.FieldB;
                    break;
                case State.MoveResourceOut:
                case State.DropResourceOut:
                    resource = (int)s.MoveResourceOut.Resource;
                    break;

                default:
                    break;
            }

            return resource;
        }

        internal int FreeWalkingNegDistance1 => s.FreeWalking.NegDistance1;

        internal int FreeWalkingNegDistance2 => s.FreeWalking.NegDistance2;

        internal State LeavingBuildingNextState => s.LeavingBuilding.NextState;

        internal int LeavingBuildingFieldB => s.LeavingBuilding.FieldB;

        internal uint MiningResource => s.Mining.Resource;

        internal Resource.Type TransportedResource => s.Walking.Resource;

        internal uint TransportDestination => s.Walking.Destination;

        internal int AttackingFieldD => s.Attacking.FieldD;

        internal int AttackingDefenderIndex => s.Attacking.DefenderIndex;

        internal int WalkingWaitCounter => s.Walking.WaitCounter;

        internal void SetWalkingWaitCounter(int newCounter)
        {
            s.Walking.WaitCounter = newCounter;
        }

        internal int WalkingDirection => s.Walking.Direction;

        internal uint IdleInStockInventoryIndex => s.IdleInStock.InventoryIndex;

        internal int MiningSubstate => (int)s.Mining.Substate;

        public Serf ExtractLastKnightFromList()
        {
            uint defenderIndex = Index;
            var defendingSerf = Game.GetSerf(defenderIndex);
            Serf lastKnight = null;

            while (defendingSerf.s.Defending.NextKnight != 0)
            {
                lastKnight = defendingSerf;
                defenderIndex = defendingSerf.s.Defending.NextKnight;
                defendingSerf = Game.GetSerf(defenderIndex);
            }

            if (lastKnight != null)
            {
                lastKnight.s.Defending.NextKnight = defendingSerf.s.Defending.NextKnight;
                defendingSerf.s.Defending.NextKnight = 0;
            }

            return defendingSerf;
        }

        public Serf ExtractKnightFromList(uint index, ref uint firstKnight, Serf lastKnight = null)
        {
            if (Index == index)
            {
                if (lastKnight != null)
                {
                    lastKnight.s.Defending.NextKnight = s.Defending.NextKnight;
                }
                else // this is the first knight
                {
                    firstKnight = s.Defending.NextKnight;
                }

                s.Defending.NextKnight = 0;

                return this;
            }

            if (s.Defending.NextKnight == 0)
                return null;

            var nextKnight = Game.GetSerf(s.Defending.NextKnight);

            return nextKnight.ExtractKnightFromList(index, ref firstKnight, this);
        }

        internal void SetPosition(MapPos position)
        {
            Position = position;
        }

        internal void InsertKnightBefore(Serf knight)
        {
            s.Defending.NextKnight = knight.Index;
        }

        internal uint NextKnight => s.Defending.NextKnight;

        internal void SetNextKnight(uint nextKnightIndex)
        {
            s.Defending.NextKnight = nextKnightIndex;
        }

        private Flag GetFlagAtPosition()
        {
            return Game.GetFlagAtPosition(Position);
        }

        private Building GetBuildingAtPosition()
        {
            return Game.GetBuildingAtPosition(Position);
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
                    return GetBuildingAtPosition();
                case State.Building:
                    return Game.GetBuilding(s.Building.Index);
                case State.Digging:
                    if (s.Digging.Substate <= 0)
                    {
                        return GetBuildingAtPosition();
                    }
                    else
                    {
                        if (s.Digging.DigPosition == 0)
                            return GetBuildingAtPosition();
                        else
                        {
                            var building = Game.GetBuildingAtPosition(Game.Map.Move(Position, ((Direction)(6 - s.Digging.DigPosition)).Reverse()));

                            if (building != null)
                                return building;

                            return GetBuildingAtPosition();
                        }
                    }
                case State.DropResourceOut:
                case State.ReadyToEnter:
                    return Game.GetBuildingAtPosition(Game.Map.MoveUpLeft(Position));
                default:
                    return null;
            }
        }

        // Commands

        internal void GoOutFromInventory(uint inventoryIndex, MapPos destination, int mode)
        {
            SetState(State.ReadyToLeaveInventory);
            s.ReadyToLeaveInventory.Mode = mode;
            s.ReadyToLeaveInventory.Destination = destination;
            s.ReadyToLeaveInventory.InventoryIndex = inventoryIndex;
        }

        internal void SendOffToFight(int distanceColumn, int distanceRow)
        {
            // Send this serf off to fight. 
            SetState(State.KnightLeaveForWalkToFight);
            s.LeaveForWalkToFight.DistanceColumn = distanceColumn;
            s.LeaveForWalkToFight.DistanceRow = distanceRow;
            s.LeaveForWalkToFight.FieldD = 0;
            s.LeaveForWalkToFight.FieldE = 0;
            s.LeaveForWalkToFight.NextState = State.KnightFreeWalking;
        }

        internal void StayIdleInStock(uint inventory)
        {
            SetState(State.IdleInStock);
            s.IdleInStock.InventoryIndex = inventory;
        }

        internal void GoOutFromBuilding(MapPos destination, int direction, int fieldB)
        {
            SetState(State.ReadyToLeave);
            s.LeavingBuilding.FieldB = fieldB;
            s.LeavingBuilding.Destination = destination;
            s.LeavingBuilding.Direction = direction;
            s.LeavingBuilding.NextState = State.Walking;
        }

        internal void Update()
        {
            try
            {
                switch (SerfState)
                {
                    case State.Null: // 0 
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
                    case State.LeavingBuilding: // 5 
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
                    case State.BuildingCastle: // 10 
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
                    case State.ReadyToLeaveInventory: // 15 
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
                    case State.Planting: // 20 
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
                    case State.Lost: // 25 
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
                    case State.Smelting: // 30 
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
                    case State.Milling: // 35 
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
                    case State.MakingTool: // 40 
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
                    case State.KnightPrepareAttacking: // 45 
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
                        // The actual fight update is handled for the attacking knight. 
                        break;
                    case State.KnightAttackingVictory: // 50 
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
                        // Nothing to do for this state. 
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
                    case State.KnightLeaveForWalkToFight: // 65 
                        HandleSerfStateKnightLeaveForWalkToFight();
                        break;
                    case State.IdleOnPath:
                        FixNonTransporterState();
                        HandleSerfIdleOnPathState();
                        break;
                    case State.WaitIdleOnPath:
                        FixNonTransporterState();
                        HandleSerfWaitIdleOnPathState();
                        break;
                    case State.WakeAtFlag:
                        FixNonTransporterState();
                        HandleSerfWakeAtFlagState();
                        break;
                    case State.WakeOnPath:
                        FixNonTransporterState();
                        HandleSerfWakeOnPathState();
                        break;
                    case State.DefendingHut: // 70 
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
                    case State.DefendingCastle: // 75 
                        HandleSerfDefendingCastleState();
                        break;
                    default:
                        Log.Debug.Write(ErrorSystemType.Serf, $"Serf state {SerfState} isn't processed");
                        SerfState = State.Null;
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new ExceptionFreeserf(Game, ErrorSystemType.Serf, ex);
            }
        }

        void FixNonTransporterState()
        {
            if (state.Type != Type.Transporter && state.Type != Type.Sailor)
                FindInventory();
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

            state.Player = (byte)(v8 & 3);
            state.Type = (Type)((v8 >> 2) & 0x1F);
            state.PlayingSfx = ((v8 >> 7) != 0);

            state.Animation = reader.ReadByte(); // 1
            state.Counter = reader.ReadWord(); // 2
            state.Position = reader.ReadDWord(); // 4

            if (state.Position != 0xFFFFFFFF)
            {
                state.Position = Game.Map.PositionFromSavedValue(state.Position);
            }

            state.Tick = reader.ReadWord(); // 8
            state.State = (State)reader.ReadByte(); // 10

            Log.Verbose.Write(ErrorSystemType.Savegame, $"load serf {Index}: {SerfStateNames[(int)SerfState]}");

            switch (SerfState)
            {
                case State.IdleInStock:
                    reader.Skip(3); // 11
                    s.IdleInStock.InventoryIndex = reader.ReadWord(); // 14
                    break;

                case State.Walking:
                    {
                        s.Walking.Direction1 = reader.ReadByte(); // 11
                        s.Walking.Destination = reader.ReadWord(); // 12
                        s.Walking.Direction = reader.ReadByte(); // 14
                        s.Walking.WaitCounter = reader.ReadByte(); // 15
                        break;
                    }
                case State.Transporting:
                case State.Delivering:
                    {
                        s.Walking.Resource = (Resource.Type)((reader.ReadByte()) - 1); // 11
                        s.Walking.Destination = reader.ReadWord(); // 12
                        s.Walking.Direction = reader.ReadByte(); // 14
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
                    s.LeavingBuilding.Destination = reader.ReadByte(); // 12
                    s.LeavingBuilding.Destination2 = reader.ReadByte(); // 13
                    s.LeavingBuilding.Direction = reader.ReadByte(); // 14
                    s.LeavingBuilding.NextState = (State)reader.ReadByte(); // 15
                    break;

                case State.ReadyToEnter:
                    s.ReadyToEnter.FieldB = reader.ReadByte(); // 11
                    break;

                case State.Digging:
                    s.Digging.HeightIndex = reader.ReadByte(); // 11
                    s.Digging.TargetHeight = reader.ReadByte(); // 12
                    s.Digging.DigPosition = reader.ReadByte(); // 13
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
                    s.BuildingCastle.InventoryIndex = reader.ReadWord(); // 12
                    break;

                case State.MoveResourceOut:
                case State.DropResourceOut:
                    s.MoveResourceOut.Resource = reader.ReadByte(); // 11
                    s.MoveResourceOut.ResourceDestination = reader.ReadWord(); // 12
                    reader.Skip(1); // 14
                    s.MoveResourceOut.NextState = (State)reader.ReadByte(); // 15
                    break;

                case State.ReadyToLeaveInventory:
                    s.ReadyToLeaveInventory.Mode = reader.ReadByte(); // 11
                    s.ReadyToLeaveInventory.Destination = reader.ReadWord(); // 12
                    s.ReadyToLeaveInventory.InventoryIndex = reader.ReadWord(); // 14
                    break;

                case State.FreeWalking:
                case State.Logging:
                case State.Planting:
                case State.StoneCutting:
                case State.StoneCutterFreeWalking:
                case State.Fishing:
                case State.Farming:
                case State.SamplingGeoSpot:
                    s.FreeWalking.DistanceX = reader.ReadByte(); // 11
                    s.FreeWalking.DistanceY = reader.ReadByte(); // 12
                    s.FreeWalking.NegDistance1 = reader.ReadByte(); // 13
                    s.FreeWalking.NegDistance2 = reader.ReadByte(); // 14
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
                    s.Mining.Resource = reader.ReadByte(); // 13
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
                        s.IdleOnPath.ReverseDirection = (Direction)reader.ReadByte(); // 11
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
                SerfType = (Type)type;
            }
            catch
            {
                SerfType = (Type)((type >> 2) & 0x1f);
                Player = (uint)type & 3;
            }

            Animation = reader.Value("animation").ReadInt();
            Counter = reader.Value("counter").ReadInt();

            uint x = reader.Value("pos")[0].ReadUInt();
            uint y = reader.Value("pos")[1].ReadUInt();

            Position = Game.Map.Position(x, y);
            state.Tick = (ushort)reader.Value("state.tick").ReadUInt();
            SerfState = (State)reader.Value("state").ReadInt();

            // TODO: Legacy loading seems to miss several states like FreeSailing or several attacking states

            switch (SerfState)
            {
                case State.IdleInStock:
                    s.IdleInStock.InventoryIndex = reader.Value("state.inventory").ReadUInt();
                    break;

                case State.Walking:
                case State.Transporting:
                case State.Delivering:
                    s.Walking.Resource = (Resource.Type)reader.Value("state.res").ReadInt();
                    s.Walking.Destination = reader.Value("state.dest").ReadUInt();
                    s.Walking.Direction = reader.Value("state.dir").ReadInt();
                    s.Walking.Direction1 = reader.Value("state.dir1").ReadInt();
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
                    s.LeavingBuilding.Destination = reader.Value("state.dest").ReadUInt();
                    s.LeavingBuilding.Destination2 = reader.Value("state.dest2").ReadInt();
                    s.LeavingBuilding.Direction = reader.Value("state.dir").ReadInt();
                    s.LeavingBuilding.NextState = (State)reader.Value("state.next_state").ReadInt();
                    break;

                case State.ReadyToEnter:
                    s.ReadyToEnter.FieldB = reader.Value("state.field_b").ReadInt();
                    break;

                case State.Digging:
                    s.Digging.HeightIndex = reader.Value("state.h_index").ReadInt();
                    s.Digging.TargetHeight = reader.Value("state.target_h").ReadUInt();
                    s.Digging.DigPosition = reader.Value("state.dig_pos").ReadInt();
                    s.Digging.Substate = reader.Value("state.substate").ReadInt();
                    break;

                case State.Building:
                    s.Building.Mode = reader.Value("state.mode").ReadInt();
                    s.Building.Index = reader.Value("state.bld_index").ReadUInt();
                    s.Building.MaterialStep = reader.Value("state.material_step").ReadUInt();
                    s.Building.Counter = reader.Value("state.counter").ReadUInt();
                    break;

                case State.BuildingCastle:
                    s.BuildingCastle.InventoryIndex = reader.Value("state.inv_index").ReadUInt();
                    break;

                case State.MoveResourceOut:
                case State.DropResourceOut:
                    s.MoveResourceOut.Resource = reader.Value("state.res").ReadUInt();
                    s.MoveResourceOut.ResourceDestination = reader.Value("state.res_dest").ReadUInt();
                    s.MoveResourceOut.NextState = (State)reader.Value("state.next_state").ReadInt();
                    break;

                case State.ReadyToLeaveInventory:
                    s.ReadyToLeaveInventory.Mode = reader.Value("state.mode").ReadInt();
                    s.ReadyToLeaveInventory.Destination = reader.Value("state.dest").ReadUInt();
                    s.ReadyToLeaveInventory.InventoryIndex = reader.Value("state.inv_index").ReadUInt();
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
                    s.FreeWalking.DistanceX = reader.Value("state.dist1").ReadInt();
                    s.FreeWalking.DistanceY = reader.Value("state.dist2").ReadInt();
                    s.FreeWalking.NegDistance1 = reader.Value("state.neg_dist").ReadInt();
                    s.FreeWalking.NegDistance2 = reader.Value("state.neg_dist2").ReadInt();
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
                    s.Mining.Resource = reader.Value("state.res").ReadUInt();
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
                    s.Attacking.DefenderIndex = reader.Value("state.def_index").ReadInt();
                    break;

                case State.KnightAttackingVictoryFree:
                    if (reader.HasValue("state.move"))
                        s.AttackingVictoryFree.Move = reader.Value("state.move").ReadInt();
                    else
                        s.AttackingVictoryFree.Move = reader.Value("state.field_b").ReadInt();
                    if (reader.HasValue("state.dist_col"))
                        s.AttackingVictoryFree.DistanceColumn = reader.Value("state.dist_col").ReadInt();
                    else
                        s.AttackingVictoryFree.DistanceColumn = reader.Value("state.field_c").ReadInt();
                    if (reader.HasValue("state.dist_row"))
                        s.AttackingVictoryFree.DistanceRow = reader.Value("state.dist_row").ReadInt();
                    else
                        s.AttackingVictoryFree.DistanceRow = reader.Value("state.field_c").ReadInt();
                    s.AttackingVictoryFree.DefenderIndex = reader.Value("state.def_index").ReadInt();
                    break;

                case State.KnightDefendingFree:
                case State.KnightEngageDefendingFree:
                    s.DefendingFree.DistanceColumn = reader.Value("state.dist_col").ReadInt();
                    s.DefendingFree.DistanceRow = reader.Value("state.dist_row").ReadInt();
                    s.DefendingFree.FieldD = reader.Value("state.field_d").ReadInt();
                    s.DefendingFree.OtherDistanceColumn = reader.Value("state.other_dist_col").ReadInt();
                    s.DefendingFree.OtherDistanceRow = reader.Value("state.other_dist_row").ReadInt();
                    break;

                case State.KnightLeaveForWalkToFight:
                    s.LeaveForWalkToFight.DistanceColumn = reader.Value("state.dist_col").ReadInt();
                    s.LeaveForWalkToFight.DistanceRow = reader.Value("state.dist_row").ReadInt();
                    s.LeaveForWalkToFight.FieldD = reader.Value("state.field_d").ReadInt();
                    s.LeaveForWalkToFight.FieldE = reader.Value("state.field_e").ReadInt();
                    s.LeaveForWalkToFight.NextState = (State)reader.Value("state.next_state").ReadInt();
                    break;

                case State.IdleOnPath:
                case State.WaitIdleOnPath:
                case State.WakeAtFlag:
                case State.WakeOnPath:
                    s.IdleOnPath.ReverseDirection = (Direction)reader.Value("state.rev_dir").ReadInt();
                    s.IdleOnPath.FlagIndex = reader.Value("state.flag").ReadUInt();
                    s.IdleOnPath.FieldE = reader.Value("state.field_e").ReadInt();
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
            writer.Value("type").Write((int)SerfType);
            writer.Value("owner").Write(Player);
            writer.Value("animation").Write(Animation);
            writer.Value("counter").Write(Counter);
            writer.Value("pos").Write(Game.Map.PositionColumn(Position));
            writer.Value("pos").Write(Game.Map.PositionRow(Position));
            writer.Value("state.tick").Write(state.Tick);
            writer.Value("state").Write((int)SerfState);

            switch (SerfState)
            {
                case State.IdleInStock:
                    writer.Value("state.inventory").Write(s.IdleInStock.InventoryIndex);
                    break;

                case State.Walking:
                case State.Transporting:
                case State.Delivering:
                    writer.Value("state.res").Write((int)s.Walking.Resource);
                    writer.Value("state.dest").Write(s.Walking.Destination);
                    writer.Value("state.dir").Write(s.Walking.Direction);
                    writer.Value("state.dir1").Write(s.Walking.Direction1);
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
                    writer.Value("state.dest").Write(s.LeavingBuilding.Destination);
                    writer.Value("state.dest2").Write(s.LeavingBuilding.Destination2);
                    writer.Value("state.dir").Write(s.LeavingBuilding.Direction);
                    writer.Value("state.next_state").Write((int)s.LeavingBuilding.NextState);
                    break;

                case State.ReadyToEnter:
                    writer.Value("state.field_b").Write(s.ReadyToEnter.FieldB);
                    break;

                case State.Digging:
                    writer.Value("state.h_index").Write(s.Digging.HeightIndex);
                    writer.Value("state.target_h").Write(s.Digging.TargetHeight);
                    writer.Value("state.dig_pos").Write(s.Digging.DigPosition);
                    writer.Value("state.substate").Write(s.Digging.Substate);
                    break;

                case State.Building:
                    writer.Value("state.mode").Write(s.Building.Mode);
                    writer.Value("state.bld_index").Write(s.Building.Index);
                    writer.Value("state.material_step").Write(s.Building.MaterialStep);
                    writer.Value("state.counter").Write(s.Building.Counter);
                    break;

                case State.BuildingCastle:
                    writer.Value("state.inv_index").Write(s.BuildingCastle.InventoryIndex);
                    break;

                case State.MoveResourceOut:
                case State.DropResourceOut:
                    writer.Value("state.res").Write(s.MoveResourceOut.Resource);
                    writer.Value("state.res_dest").Write(s.MoveResourceOut.ResourceDestination);
                    writer.Value("state.next_state").Write((int)s.MoveResourceOut.NextState);
                    break;

                case State.ReadyToLeaveInventory:
                    writer.Value("state.mode").Write(s.ReadyToLeaveInventory.Mode);
                    writer.Value("state.dest").Write(s.ReadyToLeaveInventory.Destination);
                    writer.Value("state.inv_index").Write(s.ReadyToLeaveInventory.InventoryIndex);
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
                    writer.Value("state.dist1").Write(s.FreeWalking.DistanceX);
                    writer.Value("state.dist2").Write(s.FreeWalking.DistanceY);
                    writer.Value("state.neg_dist").Write(s.FreeWalking.NegDistance1);
                    writer.Value("state.neg_dist2").Write(s.FreeWalking.NegDistance2);
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
                    writer.Value("state.res").Write(s.Mining.Resource);
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
                    writer.Value("state.def_index").Write(s.Attacking.DefenderIndex);
                    break;

                case State.KnightAttackingVictoryFree:
                    writer.Value("state.move").Write(s.AttackingVictoryFree.Move);
                    writer.Value("state.dist_col").Write(s.AttackingVictoryFree.DistanceColumn);
                    writer.Value("state.dist_row").Write(s.AttackingVictoryFree.DistanceRow);
                    writer.Value("state.def_index").Write(s.AttackingVictoryFree.DefenderIndex);
                    break;

                case State.KnightDefendingFree:
                case State.KnightEngageDefendingFree:
                    writer.Value("state.dist_col").Write(s.DefendingFree.DistanceColumn);
                    writer.Value("state.dist_row").Write(s.DefendingFree.DistanceRow);
                    writer.Value("state.field_d").Write(s.DefendingFree.FieldD);
                    writer.Value("state.other_dist_col").Write(s.DefendingFree.OtherDistanceColumn);
                    writer.Value("state.other_dist_row").Write(s.DefendingFree.OtherDistanceRow);
                    break;

                case State.KnightLeaveForWalkToFight:
                    writer.Value("state.dist_col").Write(s.LeaveForWalkToFight.DistanceColumn);
                    writer.Value("state.dist_row").Write(s.LeaveForWalkToFight.DistanceRow);
                    writer.Value("state.field_d").Write(s.LeaveForWalkToFight.FieldD);
                    writer.Value("state.field_e").Write(s.LeaveForWalkToFight.FieldE);
                    writer.Value("state.next_state").Write((int)s.LeaveForWalkToFight.NextState);
                    break;

                case State.IdleOnPath:
                case State.WaitIdleOnPath:
                case State.WakeAtFlag:
                case State.WakeOnPath:
                    writer.Value("state.rev_dir").Write((int)s.IdleOnPath.ReverseDirection);
                    writer.Value("state.flag").Write(s.IdleOnPath.FlagIndex);
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

        /// <summary>
        /// Return true if serf is waiting for a position to be available.
        /// In this case, direction will be set to the desired direction of the serf,
        /// or DirectionNone if the desired direction cannot be determined.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        bool IsWaiting(ref Direction direction)
        {
            if ((SerfState == State.Transporting || SerfState == State.Walking ||
                 SerfState == State.Delivering) &&
                 s.Walking.Direction < 0)
            {
                direction = (Direction)(s.Walking.Direction + 6);
                return true;
            }
            else if ((SerfState == State.FreeWalking ||
                      SerfState == State.KnightFreeWalking ||
                      SerfState == State.StoneCutterFreeWalking) &&
                      Animation == 82)
            {
                int distanceX = s.FreeWalking.DistanceX;
                int distanceY = s.FreeWalking.DistanceY;

                if (Math.Abs(distanceX) <= 1 && Math.Abs(distanceY) <= 1 &&
                    DirectionFromOffset[(distanceX + 1) + 3 * (distanceY + 1)] > Direction.None)
                {
                    direction = DirectionFromOffset[(distanceX + 1) + 3 * (distanceY + 1)];
                }
                else
                {
                    direction = Direction.None;
                }

                return true;
            }
            else if (SerfState == State.Digging && s.Digging.Substate < 0)
            {
                int digPosition = s.Digging.DigPosition;

                direction = (digPosition == 0) ? Direction.Up : (Direction)(6 - digPosition);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Signal waiting serf that it is possible to move in direction
        /// while switching position with another serf. Returns 0 if the
        /// switch is not acceptable.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        bool SwitchWaiting(Direction direction)
        {
            if ((SerfState == State.Transporting || SerfState == State.Walking ||
                SerfState == State.Delivering) &&
                s.Walking.Direction < 0)
            {
                s.Walking.Direction = (int)direction.Reverse();
                return true;
            }
            else if ((SerfState == State.FreeWalking ||
                      SerfState == State.KnightFreeWalking ||
                      SerfState == State.StoneCutterFreeWalking) &&
                      Animation == 82)
            {
                int dx = (((int)direction < 3) ? 1 : -1) * ((((int)direction % 3) < 2) ? 1 : 0);
                int dy = (((int)direction < 3) ? 1 : -1) * ((((int)direction % 3) > 0) ? 1 : 0);

                s.FreeWalking.DistanceX -= dx;
                s.FreeWalking.DistanceY -= dy;

                if (s.FreeWalking.DistanceX == 0 && s.FreeWalking.DistanceY == 0)
                {
                    // Arriving to destination 
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

        int GetWalkingAnimation(int heightDifference, Direction direction, bool switchPosition)
        {
            int directionIndex = (int)direction;

            if (switchPosition && directionIndex < 3)
                directionIndex += 6;

            return 4 + heightDifference + 9 * directionIndex;
        }

        // Preconditon: serf is in WALKING or TRANSPORTING state 
        void ChangeDirection(Direction direction, bool altEnd)
        {
            var map = Game.Map;
            var newPosition = map.Move(Position, direction);

            if (!map.HasSerf(newPosition))
            {
                // Change direction, not occupied. 
                map.SetSerfIndex(Position, 0);
                Animation = GetWalkingAnimation((int)map.GetHeight(newPosition) - (int)map.GetHeight(Position), direction, false);
                s.Walking.Direction = (int)direction.Reverse();
            }
            else
            {
                // Direction is occupied. 
                var otherSerf = Game.GetSerfAtPosition(newPosition);
                var otherDirection = Direction.None;

                // Sometimes an idle serf blocks us. This should be avoided.
                // TODO: Maybe check later why this even happens and look if this helps.
                if (otherSerf.SerfState == State.IdleOnPath ||
                    otherSerf.SerfState == State.WaitIdleOnPath)
                {
                    // Change direction, not occupied. 
                    map.SetSerfIndex(Position, 0);
                    Animation = GetWalkingAnimation((int)map.GetHeight(newPosition) - (int)map.GetHeight(Position), direction, false);
                    s.Walking.Direction = (int)direction.Reverse();
                }
                else if (otherSerf.IsWaiting(ref otherDirection) &&
                    (otherDirection == direction.Reverse() || otherDirection == Direction.None) &&
                    otherSerf.SwitchWaiting(direction.Reverse()))
                {
                    // Do the switch 
                    otherSerf.Position = Position;
                    map.SetSerfIndex(otherSerf.Position, (int)otherSerf.Index);
                    otherSerf.Animation = GetWalkingAnimation(
                        (int)map.GetHeight(otherSerf.Position) - (int)map.GetHeight(newPosition), direction.Reverse(), true);
                    otherSerf.Counter = CounterFromAnimation[otherSerf.Animation];

                    Animation = GetWalkingAnimation(
                        (int)map.GetHeight(newPosition) - (int)map.GetHeight(Position), direction, true);
                    s.Walking.Direction = (int)direction.Reverse();
                }
                else
                {
                    // Wait for other serf 
                    Animation = 81 + (int)direction;
                    Counter = CounterFromAnimation[Animation];
                    s.Walking.Direction = (int)direction - 6;
                    return;
                }
            }

            if (!altEnd)
                s.Walking.WaitCounter = 0;

            Position = newPosition;
            map.SetSerfIndex(Position, (int)Index);
            Counter += CounterFromAnimation[Animation];

            if (altEnd && Counter < 0)
            {
                if (map.HasFlag(newPosition))
                {
                    Counter = 0;
                }
                else
                {
                    Log.Debug.Write(ErrorSystemType.Serf, "unhandled jump to 31B82.");
                }
            }
        }

        // Precondition: serf state is in WALKING or TRANSPORTING state 
        void TransporterMoveToFlag(Flag flag)
        {
            var direction = (Direction)s.Walking.Direction;

            if (flag.IsScheduled(direction))
            {
                // Fetch resource from flag 
                s.Walking.WaitCounter = 0;
                var resourceIndex = flag.ScheduledSlot(direction);

                if (s.Walking.Resource == Resource.Type.None)
                {
                    // Pick up resource.
                    Resource.Type resource = s.Walking.Resource;
                    MapPos destination = s.Walking.Destination;

                    if (flag.PickUpResource(resourceIndex, ref resource, ref destination))
                    {
                        s.Walking.Resource = resource;
                        s.Walking.Destination = destination;
                    }
                    else
                    {
                        s.Walking.Resource = Resource.Type.None;
                        s.Walking.Destination = 0u;
                    }
                }
                else
                {
                    // Switch resources and destination.
                    Resource.Type resource = s.Walking.Resource;
                    MapPos destination = s.Walking.Destination;

                    if (flag.PickUpResource(resourceIndex, ref resource, ref destination))
                    {
                        flag.DropResource(s.Walking.Resource, s.Walking.Destination);
                        s.Walking.Resource = resource;
                        s.Walking.Destination = destination;
                    }
                }

                // Find next resource to be picked up 
                var player = Game.GetPlayer(Player);
                flag.PrioritizePickup(direction, player);
            }
            else if (s.Walking.Resource != Resource.Type.None)
            {
                // Drop resource at flag 
                if (flag.DropResource(s.Walking.Resource, s.Walking.Destination))
                {
                    s.Walking.Resource = Resource.Type.None;
                }
            }

            ChangeDirection(direction, true);
        }

        void StartWalking(Direction direction, int slope, bool changePosition)
        {
            var map = Game.Map;
            var newPosition = map.Move(Position, direction);
            Animation = GetWalkingAnimation((int)map.GetHeight(newPosition) - (int)map.GetHeight(Position), direction, false);
            Counter += (slope * CounterFromAnimation[Animation]) >> 5;

            if (changePosition)
            {
                map.SetSerfIndex(Position, 0);
                map.SetSerfIndex(newPosition, (int)Index);
            }

            Position = newPosition;
        }

        /// <summary>
        /// Start entering building in direction up-left.
        /// If joinPosition is set the serf is assumed to origin from
        /// a joined position so the source position will not have it's
        /// serf index cleared.
        /// </summary>
        /// <param name="fieldB"></param>
        /// <param name="joinPosition"></param>
        void EnterBuilding(int fieldB, bool joinPosition)
        {
            SetState(State.EnteringBuilding);

            StartWalking(Direction.UpLeft, 32, !joinPosition);

            if (joinPosition)
                Game.Map.SetSerfIndex(Position, (int)Index);

            int slope;
            var building = GetBuildingAtPosition();

            if (building == null)
            {
                slope = 1;
                SetLostState(); // try to enter a building that is no longer there
                return;
            }
            else
            {
                slope = RoadBuildingSlope[(int)building.BuildingType];

                if (!building.IsDone)
                    slope = 1;
            }

            s.EnteringBuilding.SlopeLength = (slope * Counter) >> 5;
            s.EnteringBuilding.FieldB = fieldB;
        }

        /// <summary>
        /// Start leaving building by switching to LaevingBuilding state
        /// and setting appropriate state.
        /// </summary>
        /// <param name="joinPos"></param>
        void LeaveBuilding(bool joinPos)
        {
            int slope;
            var building = GetBuildingAtPosition();

            if (building == null)
            {
                slope = 30;
            }
            else
            {
                slope = 31 - RoadBuildingSlope[(int)building.BuildingType];

                if (!building.IsDone)
                    slope = 30;
            }

            if (joinPos)
                Game.Map.SetSerfIndex(Position, 0);

            StartWalking(Direction.DownRight, slope, !joinPos);
            SetState(State.LeavingBuilding);
            Game.AddSerfForDrawing(this, Position);
        }

        void EnterInventory()
        {
            Game.Map.SetSerfIndex(Position, 0);
            var building = GetBuildingAtPosition();
            SetState(State.IdleInStock);
            // TODO ?
            /*serf->s.idleInStock.FieldB = 0;
              serf->s.idleInStock.FieldC = 0;*/
            s.IdleInStock.InventoryIndex = building.Inventory.Index;
        }

        bool DropResource(Resource.Type resourceType)
        {
            var flag = GetFlagAtPosition();

            if (flag == null)
                return false;

            // Resource is lost if no free slot is found 
            if (flag.DropResource(resourceType, 0))
            {
                Game.GetPlayer(Player).IncreaseResourceCount(resourceType);
            }

            return true;
        }

        void FindInventory()
        {
            var map = Game.Map;

            if (map.HasFlag(Position))
            {
                var flag = GetFlagAtPosition();

                if ((flag.LandPaths != 0 ||
                    (flag.HasInventory() && flag.AcceptsSerfs())) &&
                    map.GetOwner(Position) == Player)
                {
                    SetState(State.Walking);
                    s.Walking.Direction1 = -2;
                    s.Walking.Destination = 0;
                    s.Walking.Direction = 0;
                    Counter = 0;

                    return;
                }
            }

            SetState(State.Lost);
            s.Lost.FieldB = 0;
            Counter = 0;
        }

        public bool CanPassMapPosition(MapPos position)
        {
            return Map.MapSpaceFromObject[(int)Game.Map.GetObject(position)] <= Map.Space.Semipassable;
        }

        void SetFightOutcome(Serf attacker, Serf defender)
        {
            // Calculate "morale" for attacker. 
            uint expFactor = 1u << (attacker.SerfType - Type.Knight0);
            uint landFactor = 0x1000u;

            if (attacker.Player != Game.Map.GetOwner(attacker.Position))
            {
                landFactor = Game.GetPlayer(attacker.Player).KnightMorale;
            }

            uint morale = (0x400u * expFactor * landFactor) >> 16;

            // Calculate "morale" for defender. 
            uint defenderExpFactor = 1u << (defender.SerfType - Type.Knight0);
            uint defenderLandFactor = 0x1000u;

            if (defender.Player != Game.Map.GetOwner(defender.Position))
            {
                defenderLandFactor = Game.GetPlayer(defender.Player).KnightMorale;
            }

            uint defenderMorale = (0x400u * defenderExpFactor * defenderLandFactor) >> 16;

            uint playerIndex;
            uint value;
            Type knightType;
            uint result = ((morale + defenderMorale) * Game.RandomInt()) >> 16;

            if (result < morale)
            {
                playerIndex = defender.Player;
                value = defenderExpFactor;
                knightType = defender.SerfType;
                attacker.s.Attacking.AttackerWon = 1;
                Log.Debug.Write(ErrorSystemType.Serf, $"Fight: {morale} vs {defenderMorale} ({result}). Attacker winning.");
            }
            else
            {
                playerIndex = attacker.Player;
                value = expFactor;
                knightType = attacker.SerfType;
                attacker.s.Attacking.AttackerWon = 0;
                Log.Debug.Write(ErrorSystemType.Serf, $"Fight: {morale} vs {defenderMorale} ({result}). Defender winning.");
            }

            var player = Game.GetPlayer(playerIndex);

            // TODO: Commented the following lines as the serf type later is set to 'Dead' and so the serf count and military score would be decreased twice. Needs testing.
            //player.DecreaseMilitaryScore(value);
            //player.DecreaseSerfCount(knightType);
            attacker.s.Attacking.Move = Game.RandomInt() & 0x70;
        }

        static bool HandleSerfWalkingStateSearchCallback(Flag flag, object data)
        {
            var serf = data as Serf;
            var destination = flag.Game.GetFlag(serf.s.Walking.Destination);

            if (flag == destination)
            {
                Log.Verbose.Write(ErrorSystemType.Serf, " dest found: " + destination.SearchDirection);
                serf.ChangeDirection(destination.SearchDirection, false);
                return true;
            }

            return false;
        }

        void HandleSerfIdleInStockState()
        {
            var inventory = Game.GetInventory(s.IdleInStock.InventoryIndex);

            if (inventory == null)
            {
                // TODO: should not happen but actually it does rarely (I guess when the building is burned down in the wrong moment)
                return;
            }

            if (inventory.SerfMode == Inventory.Mode.In
                || inventory.SerfMode == Inventory.Mode.Stop // in, stop 
                || inventory.GetSerfQueueLength() >= 3)
            {
                switch (SerfType)
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
                    default:
                        inventory.SerfIdleInStock(this);
                        break;
                }
            }
            else
            { // out 
                inventory.CallOutSerf(this);

                SetState(State.ReadyToLeaveInventory);
                s.ReadyToLeaveInventory.Mode = -3;
                s.ReadyToLeaveInventory.InventoryIndex = inventory.Index;
                // TODO immediate switch to next state. 
            }
        }

        void HandleSerfWalkingStateDestReached()
        {
            // Destination reached. 
            if (s.Walking.Direction1 < 0)
            {
                var map = Game.Map;
                var building = Game.GetBuildingAtPosition(map.MoveUpLeft(Position));

                if (building == null) // not exists anymore?
                {
                    // TODO: is this the right handling for this case?
                    SetLostState();
                    return;
                }

                building.RequestedSerfReached(this);

                if (map.HasSerf(map.MoveUpLeft(Position)))
                {
                    Animation = 85;
                    Counter = 0;
                    SetState(State.ReadyToEnter);
                }
                else
                {
                    EnterBuilding(s.Walking.Direction1, false);
                }
            }
            else if (s.Walking.Direction1 == 6)
            {
                SetState(State.LookingForGeoSpot);
                Counter = 0;
            }
            else
            {
                var flag = GetFlagAtPosition();

                if (flag == null)
                {
                    throw new ExceptionFreeserf(Game, ErrorSystemType.Serf, "Flag expected as destination of walking serf.");
                }

                var direction = (Direction)s.Walking.Direction1;
                var otherFlag = flag.GetOtherEndFlag(direction);

                if (otherFlag == null)
                {
                    throw new ExceptionFreeserf(Game, ErrorSystemType.Serf, "Path has no other end flag in selected dir.");
                }

                var otherDirection = flag.GetOtherEndDirection(direction);

                // Increment transport serf count 
                flag.CompleteSerfRequest(direction);
                otherFlag.CompleteSerfRequest(otherDirection);

                SetState(State.Transporting);
                s.Walking.Resource = Resource.Type.None;
                s.Walking.Direction = (int)direction;
                s.Walking.Direction1 = 0;
                s.Walking.WaitCounter = 0;

                TransporterMoveToFlag(flag);
            }
        }

        void HandleSerfWalkingStateWaiting()
        {
            // Waiting for other serf. 
            var direction = (Direction)(s.Walking.Direction + 6);
            var map = Game.Map;

            // Only check for loops once in a while. 
            ++s.Walking.WaitCounter;

            if ((!map.HasFlag(Position) && s.Walking.WaitCounter >= 10) ||
                s.Walking.WaitCounter >= 50)
            {
                s.Walking.WaitCounter = 0;
                var position = Position;
                var loopSerfs = new List<Serf>() { this };

                // Follow the chain of serfs waiting for each other and
                // see if there is a loop.
                for (int i = 0; i < 100; ++i)
                {
                    position = map.Move(position, direction);

                    if (!map.HasSerf(position))
                    {
                        break;
                    }
                    else if (map.GetSerfIndex(position) == Index)
                    {
                        // We found a loop, check if the loop is at an inventory 
                        foreach (var loopSerf in loopSerfs)
                        {
                            if (Game.Map.HasFlag(loopSerf.Position))
                            {
                                var flag = Game.GetFlagAtPosition(loopSerf.Position);

                                if (flag.HasInventory() && flag.AcceptsSerfs())
                                {
                                    loopSerf.PutBackToInventory(flag.Building.Inventory);
                                    loopSerf.FindInventory();
                                    return;
                                }
                            }
                        }

                        // We have found a loop, try a different direction. 
                        ChangeDirection(direction.Reverse(), false);
                        return;
                    }

                    // Get next serf and follow the chain 
                    var otherSerf = Game.GetSerfAtPosition(position);

                    if (otherSerf.SerfState != State.Walking &&
                        otherSerf.SerfState != State.Transporting)
                    {
                        break;
                    }

                    if (otherSerf.s.Walking.Direction >= 0 ||
                        (otherSerf.s.Walking.Direction + 6) == (int)direction.Reverse())
                    {
                        break;
                    }

                    direction = (Direction)(otherSerf.s.Walking.Direction + 6);
                    loopSerfs.Add(otherSerf);
                }
            }

            // Stick to the same direction 
            ChangeDirection((Direction)(s.Walking.Direction + 6), false);
        }

        void HandleSerfWalkingState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                if (s.Walking.Direction < 0)
                {
                    HandleSerfWalkingStateWaiting();
                    continue;
                }

                if (Game.Map.HasFlag(Position))
                {
                    // Serf has reached a flag.
                    // Search for a destination if none is known.
                    if (s.Walking.Destination == 0)
                    {
                        var sourceFlag = GetFlagAtPosition();
                        int nearestInventory = sourceFlag.FindNearestInventoryForSerf();

                        if (nearestInventory < 0)
                        {
                            SetState(State.Lost);
                            s.Lost.FieldB = 1;
                            Counter = 0;

                            return;
                        }

                        s.Walking.Destination = (uint)nearestInventory;
                    }

                    // Check whether destination has been reached.
                    // If not, find out which direction to move in
                    // to reach the destination.
                    if (s.Walking.Destination == Game.Map.GetObjectIndex(Position))
                    {
                        HandleSerfWalkingStateDestReached();
                        return;
                    }
                    else
                    {
                        var sourceFlag = GetFlagAtPosition();
                        var search = new FlagSearch(Game);
                        var cycle = DirectionCycleCCW.CreateDefault();

                        foreach (var direction in cycle)
                        {
                            if (!sourceFlag.IsWaterPath(direction))
                            {
                                var otherFlag = sourceFlag.GetOtherEndFlag(direction);
                                otherFlag.SearchDirection = direction;
                                search.AddSource(otherFlag);
                            }
                        }

                        if (search.Execute(HandleSerfWalkingStateSearchCallback, true, false, this))
                            continue;
                    }
                }
                else
                {
                    // Serf is not at a flag. Just follow the road. 
                    var paths = Game.Map.Paths(Position) & (byte)~Misc.BitU(s.Walking.Direction);
                    var direction = Direction.None;
                    var cycle = DirectionCycleCW.CreateDefault();

                    foreach (var nextDirection in cycle)
                    {
                        if (paths == Misc.BitU((int)nextDirection))
                        {
                            direction = nextDirection;
                            break;
                        }
                    }

                    if (direction >= 0)
                    {
                        ChangeDirection(direction, false);
                        continue;
                    }

                    Counter = 0;
                }

                // Either the road is a dead end; or
                // we are at a flag, but the flag search for
                // the destination failed.
                if (s.Walking.Direction1 < 0)
                {
                    if (s.Walking.Direction1 < -1)
                    {
                        SetState(State.Lost);
                        s.Lost.FieldB = 1;
                        Counter = 0;

                        return;
                    }

                    var flag = Game.GetFlag(s.Walking.Destination);
                    var building = flag.Building;

                    building.RequestedSerfLost();
                }
                else if (s.Walking.Direction1 != 6)
                {
                    var flag = Game.GetFlag(s.Walking.Destination);
                    var direction = (Direction)s.Walking.Direction1;

                    flag.CancelSerfRequest(direction);
                    flag.GetOtherEndFlag(direction).CancelSerfRequest(flag.GetOtherEndDirection(direction));
                }

                s.Walking.Direction1 = -2;
                s.Walking.Destination = 0;
                Counter = 0;
            }
        }

        void HandleSerfTransportingState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            if (Counter >= 0)
                return;

            if (s.Walking.Direction < 0)
            {
                ChangeDirection((Direction)(s.Walking.Direction + 6), true);
            }
            else
            {
                var map = Game.Map;

                if (map.HasFlag(Position))
                {
                    // Current position occupied by waiting transporter 
                    if (s.Walking.WaitCounter < 0)
                    {
                        SetState(State.Walking);
                        s.Walking.WaitCounter = 0;
                        s.Walking.Direction1 = -2;
                        s.Walking.Destination = 0;
                        Counter = 0;

                        return;
                    }

                    var flag = GetFlagAtPosition();

                    if (s.Walking.Resource != Resource.Type.None &&
                        map.GetObjectIndex(Position) == s.Walking.Destination &&
                        (!flag.HasInventory() || flag.AcceptsResources()))
                    {
                        // At resource destination 
                        SetState(State.Delivering);
                        s.Walking.WaitCounter = 0;

                        var newPosition = map.MoveUpLeft(Position);
                        Animation = 3 + (int)map.GetHeight(newPosition) - (int)map.GetHeight(Position) +
                                    ((int)Direction.UpLeft + 6) * 9;
                        Counter = CounterFromAnimation[Animation];
                        return;
                    }

                    TransporterMoveToFlag(flag);
                }
                else
                {
                    var paths = map.Paths(Position) & (byte)~Misc.BitU(s.Walking.Direction);
                    var direction = Direction.None;
                    var cycle = DirectionCycleCW.CreateDefault();

                    foreach (var nextDirection in cycle)
                    {
                        if (paths == Misc.BitU((int)nextDirection))
                        {
                            direction = nextDirection;
                            break;
                        }
                    }

                    if (direction < 0)
                    {
                        SetState(State.Lost);
                        Counter = 0;

                        return;
                    }

                    if (!map.HasFlag(map.Move(Position, direction)) ||
                        s.Walking.Resource != Resource.Type.None ||
                        s.Walking.WaitCounter < 0)
                    {
                        ChangeDirection(direction, true);
                        return;
                    }

                    var flag = Game.GetFlagAtPosition(map.Move(Position, direction));
                    var reverseDirection = direction.Reverse();
                    var otherFlag = flag.GetOtherEndFlag(reverseDirection);
                    var otherDirection = flag.GetOtherEndDirection(reverseDirection);

                    if (flag.IsScheduled(reverseDirection))
                    {
                        ChangeDirection(direction, true);
                        return;
                    }

                    Animation = 110 + s.Walking.Direction;
                    Counter = CounterFromAnimation[Animation];
                    s.Walking.Direction -= 6;

                    if (flag.FreeTransporterCount(reverseDirection) > 1)
                    {
                        ++s.Walking.WaitCounter;

                        if (s.Walking.WaitCounter > 3)
                        {
                            flag.TransporterToServe(reverseDirection);
                            otherFlag.TransporterToServe(otherDirection);
                            s.Walking.WaitCounter = -1;
                        }
                    }
                    else
                    {
                        if (!otherFlag.IsScheduled(otherDirection))
                        {
                            state.Tick = (ushort)((state.Tick & 0xff00) | (s.Walking.Direction & 0xff));
                            SetState(State.IdleOnPath);
                            s.IdleOnPath.ReverseDirection = reverseDirection;
                            s.IdleOnPath.FlagIndex = flag.Index;
                            map.SetIdleSerf(Position);
                            map.SetSerfIndex(Position, 0);

                            return;
                        }
                    }
                }
            }
        }

        void InitBuilding(Resource.Type resource1, Resource.Type resource2 = Resource.Type.None)
        {
            var building = GetBuildingAtPosition();
            var flag = Game.GetFlag(building.FlagIndex);

            building.StockInit(0, resource1, 8);

            if (resource2 != Resource.Type.None)
                building.StockInit(1, resource2, 8);

            flag.ClearFlags();
        }

        void HandleSerfEnteringBuildingState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0 || Counter <= s.EnteringBuilding.SlopeLength)
            {
                if (Game.Map.GetObjectIndex(Position) == 0 ||
                    GetBuildingAtPosition().IsBurning)
                {
                    // Burning 
                    SetState(State.Lost);
                    s.Lost.FieldB = 0;
                    Counter = 0;

                    return;
                }

                Counter = s.EnteringBuilding.SlopeLength;
                var map = Game.Map;

                switch (SerfType)
                {
                    case Type.Transporter:
                        if (s.EnteringBuilding.FieldB == -2)
                        {
                            EnterInventory();
                        }
                        else
                        {
                            map.SetSerfIndex(Position, 0);
                            var flag = Game.GetFlagForBuildingAtPosition(Position);

                            // Mark as inventory accepting resources and serfs. 
                            flag.SetHasInventory();
                            flag.SetAcceptsResources(true);
                            flag.SetAcceptsSerfs(true);

                            SetState(State.WaitForResourceOut);
                            Counter = 63;
                            SerfType = Type.TransporterInventory;
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
                            s.Digging.HeightIndex = 15;

                            var building = GetBuildingAtPosition();
                            s.Digging.DigPosition = 6;
                            s.Digging.TargetHeight = building.Level;
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

                            var building = Game.GetBuilding(s.Building.Index);

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
                                InitBuilding(Resource.Type.Lumber);
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
                            var building = GetBuildingAtPosition();
                            var buildingType = building.BuildingType;

                            if (s.EnteringBuilding.FieldB != 0)
                            {
                                building.StartActivity();
                                building.StopPlayingSfx();

                                InitBuilding(Resource.Type.GroupFood);
                            }

                            SetState(State.Mining);
                            s.Mining.Substate = 0;
                            s.Mining.Deposit = (Map.Minerals)(4 - (buildingType - Building.Type.StoneMine));
                            // TODO ?
                            //s.Mining.FieldC = 0;
                            s.Mining.Resource = 0;
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

                            var building = GetBuildingAtPosition();

                            if (s.EnteringBuilding.FieldB != 0)
                            {
                                if (building.BuildingType == Building.Type.SteelSmelter)
                                {
                                    InitBuilding(Resource.Type.Coal, Resource.Type.IronOre);
                                }
                                else
                                {
                                    InitBuilding(Resource.Type.Coal, Resource.Type.GoldOre);
                                }
                            }

                            // Switch to smelting state to begin work. 
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
                                var building = GetBuildingAtPosition();

                                building.SetInitialResourcesInStock(1, 1);
                                InitBuilding(Resource.Type.Wheat);

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
                                InitBuilding(Resource.Type.Pig);
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
                                InitBuilding(Resource.Type.Wheat);
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
                                InitBuilding(Resource.Type.Flour);
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
                                InitBuilding(Resource.Type.Plank);
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
                                InitBuilding(Resource.Type.Plank, Resource.Type.Steel);
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
                                InitBuilding(Resource.Type.Coal, Resource.Type.Steel);
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
                            SetState(State.LookingForGeoSpot); // TODO Should never be reached 
                            Counter = 0;
                        }
                        break;
                    case Type.Generic:
                        {
                            map.SetSerfIndex(Position, 0);

                            var building = GetBuildingAtPosition();
                            var inventory = building.Inventory;

                            if (inventory == null)
                            {
                                throw new ExceptionFreeserf(Game, ErrorSystemType.Serf, "Not inventory.");
                            }

                            inventory.SerfComeBack();

                            SetState(State.IdleInStock);
                            s.IdleInStock.InventoryIndex = inventory.Index;
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
                            var building = GetBuildingAtPosition();

                            if (building.IsBurning)
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

                                    // Prepend to knight list 
                                    s.Defending.NextKnight = building.FirstKnight;
                                    building.FirstKnight = Index;

                                    Game.GetPlayer(building.Player).IncreaseCastleKnights();

                                    return;
                                }

                                building.RequestedKnightArrived();

                                var nextState = State.Invalid;

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

                                // Switch to defending state 
                                SetState(nextState);
                                Counter = 6000;

                                // Prepend to knight list 
                                s.Defending.NextKnight = building.FirstKnight;
                                building.FirstKnight = Index;
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
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                Counter = 0;
                SetState(s.LeavingBuilding.NextState);

                // Set FieldF to 0, do this for individual states if necessary 
                if (SerfState == State.Walking)
                {
                    int mode = s.LeavingBuilding.FieldB;
                    uint destination = s.LeavingBuilding.Destination;
                    s.Walking.Direction1 = mode;
                    s.Walking.Destination = destination;
                    s.Walking.WaitCounter = 0;
                }
                else if (SerfState == State.DropResourceOut)
                {
                    uint resource = (uint)s.LeavingBuilding.FieldB;
                    uint resourceDestination = s.LeavingBuilding.Destination;
                    s.MoveResourceOut.Resource = resource;
                    s.MoveResourceOut.ResourceDestination = resourceDestination;
                }
                else if (SerfState == State.FreeWalking ||
                         SerfState == State.KnightFreeWalking ||
                         SerfState == State.StoneCutterFreeWalking)
                {
                    int distance1 = s.LeavingBuilding.FieldB;
                    int distance2 = (int)s.LeavingBuilding.Destination;
                    int negDistance1 = s.LeavingBuilding.Destination2;
                    int negDistance2 = s.LeavingBuilding.Direction;
                    s.FreeWalking.DistanceX = distance1;
                    s.FreeWalking.DistanceY = distance2;
                    s.FreeWalking.NegDistance1 = negDistance1;
                    s.FreeWalking.NegDistance2 = negDistance2;
                    s.FreeWalking.Flags = 0;
                }
                else if (SerfState == State.KnightPrepareDefending ||
                         SerfState == State.Scatter)
                {
                    // No state. 
                }
                else
                {
                    Log.Debug.Write(ErrorSystemType.Serf, "unhandled next state when leaving building.");
                }
            }
        }

        void HandleSerfReadyToEnterState()
        {
            var newPosition = Game.Map.MoveUpLeft(Position);

            if (Game.Map.HasSerf(newPosition))
            {
                Animation = 85;
                Counter = 0;

                return;
            }

            EnterBuilding(s.ReadyToEnter.FieldB, false);
        }

        void HandleSerfReadyToLeaveState()
        {
            state.Tick = Game.Tick;
            Counter = 0;

            var map = Game.Map;
            var newPosition = map.MoveDownRight(Position);

            if ((map.GetSerfIndex(Position) != Index && map.HasSerf(Position)) || map.HasSerf(newPosition))
            {
                Animation = 82;
                Counter = 0;

                return;
            }

            LeaveBuilding(false);
        }

        static readonly int[] DiggingHeightDifferences = new int[]
        {
            -1, 1, -2, 2, -3, 3, -4, 4,
            -5, 5, -6, 6, -7, 7, -8, 8
        };

        void HandleSerfDiggingState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            var map = Game.Map;

            while (Counter < 0)
            {
                --s.Digging.Substate;

                if (s.Digging.Substate < 0)
                {
                    Log.Verbose.Write(ErrorSystemType.Serf, "substate -1: wait for serf.");

                    int digPosition = s.Digging.DigPosition;
                    var direction = (digPosition == 0) ? Direction.Up : (Direction)(6 - digPosition);
                    var newPosition = map.Move(Position, direction);

                    if (map.HasSerf(newPosition))
                    {
                        var otherSerf = Game.GetSerfAtPosition(newPosition);
                        var otherDirection = Direction.None;

                        if (otherSerf.IsWaiting(ref otherDirection) &&
                            otherDirection == direction.Reverse() &&
                            otherSerf.SwitchWaiting(otherDirection))
                        {
                            // Do the switch 
                            otherSerf.Position = Position;
                            map.SetSerfIndex(otherSerf.Position, (int)otherSerf.Index);
                            otherSerf.Animation = GetWalkingAnimation(
                                (int)map.GetHeight(otherSerf.Position) - (int)map.GetHeight(newPosition), direction.Reverse(), true);
                            otherSerf.Counter = CounterFromAnimation[otherSerf.Animation];

                            if (digPosition != 0)
                            {
                                Animation = GetWalkingAnimation((int)map.GetHeight(newPosition) - (int)map.GetHeight(Position), direction, true);
                            }
                            else
                            {
                                Animation = (int)map.GetHeight(newPosition) - (int)map.GetHeight(Position);
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

                        if (digPosition != 0)
                        {
                            Animation = GetWalkingAnimation((int)map.GetHeight(newPosition) - (int)map.GetHeight(Position), direction, false);
                        }
                        else
                        {
                            Animation = (int)map.GetHeight(newPosition) - (int)map.GetHeight(Position);
                        }
                    }

                    map.SetSerfIndex(newPosition, (int)Index);
                    Position = newPosition;
                    s.Digging.Substate = 3;
                    Counter += CounterFromAnimation[Animation];
                }
                else if (s.Digging.Substate == 1)
                {
                    // 34CD6: Change height, head back to center 
                    int height = (int)map.GetHeight(Position);
                    height += ((s.Digging.HeightIndex & 1) != 0) ? -1 : 1;
                    Log.Verbose.Write(ErrorSystemType.Serf, "substate 1: change height " + (((s.Digging.HeightIndex & 1) != 0) ? "down." : "up."));
                    map.SetHeight(Position, (uint)height);

                    if (s.Digging.DigPosition == 0)
                    {
                        s.Digging.Substate = 1;
                    }
                    else
                    {
                        var direction = ((Direction)(6 - s.Digging.DigPosition)).Reverse();
                        StartWalking(direction, 32, true);
                    }
                }
                else if (s.Digging.Substate > 1)
                {
                    Log.Verbose.Write(ErrorSystemType.Serf, "substate 2: dig.");
                    // 34E89 
                    Animation = 88 - (s.Digging.HeightIndex & 1);
                    Counter += 383;
                }
                else
                {
                    // 34CDC: Looking for a place to dig 
                    Log.Verbose.Write(ErrorSystemType.Serf, $"substate 0: looking for place to dig {s.Digging.DigPosition}, {s.Digging.HeightIndex}");

                    do
                    {
                        int heightDifference = DiggingHeightDifferences[s.Digging.HeightIndex] + (int)s.Digging.TargetHeight;

                        if (s.Digging.DigPosition >= 0 && heightDifference >= 0 && heightDifference < 32)
                        {
                            if (s.Digging.DigPosition == 0)
                            {
                                int height = (int)map.GetHeight(Position);

                                if (height != heightDifference)
                                {
                                    --s.Digging.DigPosition;
                                    continue;
                                }

                                // Dig here 
                                s.Digging.Substate = 2;
                                Animation = 87 + (s.Digging.HeightIndex & 1);
                                Counter += 383;
                            }
                            else
                            {
                                var direction = (Direction)(6 - s.Digging.DigPosition);
                                var newPosition = map.Move(Position, direction);
                                var newHeight = (int)map.GetHeight(newPosition);

                                if (newHeight != heightDifference)
                                {
                                    --s.Digging.DigPosition;
                                    continue;
                                }

                                Log.Verbose.Write(ErrorSystemType.Serf, $"  found at: {s.Digging.DigPosition}.");

                                // Digging spot found 
                                if (map.HasSerf(newPosition))
                                {
                                    // Occupied by other serf, wait 
                                    s.Digging.Substate = 0;
                                    Animation = 87 - s.Digging.DigPosition;
                                    Counter = CounterFromAnimation[Animation];

                                    return;
                                }

                                // Go to dig there 
                                StartWalking(direction, 32, true);
                                s.Digging.Substate = 3;
                            }

                            break;
                        }

                        s.Digging.DigPosition = 6;
                        --s.Digging.HeightIndex;

                    } while (s.Digging.HeightIndex >= 0);

                    if (s.Digging.HeightIndex < 0)
                    {
                        // Done digging 
                        var building = GetBuildingAtPosition();
                        building.DoneLeveling();
                        SetState(State.ReadyToLeave);
                        s.LeavingBuilding.Destination = 0;
                        s.LeavingBuilding.FieldB = -2;
                        s.LeavingBuilding.Direction = 0;
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
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                var building = Game.GetBuilding(s.Building.Index);

                if (building == null)
                    return;

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

                        // 353A5 
                        int materialStep = (int)s.Building.MaterialStep & 0xf;

                        if (!Misc.BitTest(MaterialOrder[(int)building.BuildingType], materialStep))
                        {
                            // Planks 
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
                            // Stone 
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

                    // 353A5: Duplicate code 
                    int materialStep = (int)s.Building.MaterialStep & 0xf;

                    if (!Misc.BitTest(MaterialOrder[(int)building.BuildingType], materialStep))
                    {
                        // Planks 
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
                        // Stone 
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
            state.Tick = Game.Tick;

            var inventory = Game.GetInventory(s.BuildingCastle.InventoryIndex);
            var building = Game.GetBuilding(inventory.Building);

            if (building.BuildProgress())
            {
                // Finished 
                Game.Map.SetSerfIndex(Position, 0);
                SetState(State.WaitForResourceOut);
            }
        }

        void HandleSerfMoveResourceOutState()
        {
            state.Tick = Game.Tick;
            Counter = 0;

            var map = Game.Map;

            if (map.HasSerf(map.MoveDownRight(Position)) ||
                map.HasOtherSerf(Position, this))
            {
                // Occupied by serf, wait 
                Animation = 82;
                Counter = 0;

                return;
            }

            var flag = Game.GetFlagForBuildingAtPosition(Position);

            if (flag == null)
                return;

            if (!flag.HasEmptySlot())
            {
                // All resource slots at flag are occupied, wait 
                Animation = 82;
                Counter = 0;

                return;
            }

            var resource = s.MoveResourceOut.Resource;
            var resourceDestination = s.MoveResourceOut.ResourceDestination;
            var nextState = s.MoveResourceOut.NextState;

            LeaveBuilding(false);

            s.LeavingBuilding.NextState = nextState;
            s.LeavingBuilding.FieldB = (int)resource;
            s.LeavingBuilding.Destination = resourceDestination;
        }

        void HandleSerfWaitForResourceOutState()
        {
            if (Counter != 0)
            {
                ushort delta = (ushort)(Game.Tick - state.Tick);
                state.Tick = Game.Tick;
                Counter -= delta;

                if (Counter >= 0)
                    return;

                Counter = 0;
            }

            var building = GetBuildingAtPosition();

            if (building == null)
                return;

            var inventory = building.Inventory;

            if (inventory.GetSerfQueueLength() > 0 ||
                !inventory.HasResourceInQueue())
            {
                return;
            }

            SetState(State.MoveResourceOut);
            var resource = Resource.Type.None;
            uint destination = 0;

            inventory.GetResourceFromQueue(ref resource, ref destination);
            s.MoveResourceOut.Resource = (uint)(resource + 1);
            s.MoveResourceOut.ResourceDestination = destination;
            s.MoveResourceOut.NextState = State.DropResourceOut;

            // TODO ?
            // why isn't a state switch enough? 
            //HandleSerfMoveResourceOutState(serf);
        }

        void HandleSerfDropResourceOutState()
        {
            var flag = GetFlagAtPosition();

            if (flag == null)
                return;

            if (!flag.DropResource((Resource.Type)(s.MoveResourceOut.Resource - 1), s.MoveResourceOut.ResourceDestination))
            {
                throw new ExceptionFreeserf(Game, ErrorSystemType.Serf, "Failed to drop resource.");
            }

            SetState(State.ReadyToEnter);
            s.ReadyToEnter.FieldB = 0;
        }

        void HandleSerfDeliveringState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                if (s.Walking.WaitCounter != 0)
                {
                    SetState(State.Transporting);
                    s.Walking.WaitCounter = 0;

                    TransporterMoveToFlag(GetFlagAtPosition());

                    return;
                }

                if (s.Walking.Resource != Resource.Type.None)
                {
                    var building = Game.GetBuildingAtPosition(Game.Map.MoveUpLeft(Position));

                    if (building.RequestedResourceDelivered(s.Walking.Resource))
                        s.Walking.Resource = Resource.Type.None;
                    else
                    {
                        // if we can't deliver the serf transport it back to the nearest inventory.
                        // therefore we switch to transporting and set destination to 0.
                        SetState(State.Transporting);
                        s.Walking.Destination = 0;
                    }
                }

                Animation = 4 + 9 - (Animation - (3 + 10 * 9));
                s.Walking.WaitCounter = -s.Walking.WaitCounter - 1;
                Counter += CounterFromAnimation[Animation] >> 1;
            }
        }

        void HandleSerfReadyToLeaveInventoryState()
        {
            state.Tick = Game.Tick;
            Counter = 0;

            var map = Game.Map;

            if (map.HasSerf(Position) || map.HasSerf(map.MoveDownRight(Position)))
            {
                Animation = 82;
                Counter = 0;
                return;
            }

            // Check if there is a serf that waits to approach the flag.
            // If so, we wait inside.
            var inventoryFlag = Game.GetFlagForBuildingAtPosition(Position);

            if (inventoryFlag != null)
            {
                var cycle = new DirectionCycleCW(Direction.Up, 5);

                foreach (var direction in cycle)
                {
                    var position = map.Move(inventoryFlag.Position, direction);

                    if (map.HasSerf(position))
                    {
                        var serf = Game.GetSerfAtPosition(position);
                        var tempDirection = Direction.None;

                        if ((serf.SerfState == State.Walking || serf.SerfState == State.Transporting || serf.SerfState == State.KnightEngagingBuilding) &&
                            serf.IsWaiting(ref tempDirection))
                        {
                            if (!(serf.SerfState != State.Transporting && serf.s.Walking.Destination == inventoryFlag.Position && serf.s.Walking.Direction == (int)direction.Reverse()))
                            {
                                Animation = 82;
                                Counter = 0;
                                return;
                            }
                        }
                    }
                }
            }

            if (s.ReadyToLeaveInventory.Mode == -1)
            {
                var flag = Game.GetFlag(s.ReadyToLeaveInventory.Destination);

                if (flag.HasBuilding)
                {
                    var building = flag.Building;

                    if (map.HasSerf(building.Position))
                    {
                        Animation = 82;
                        Counter = 0;

                        return;
                    }
                }
            }

            var inventory = Game.GetInventory(s.ReadyToLeaveInventory.InventoryIndex);

            inventory.SerfAway();

            var nextState = State.Walking;
            var mode = s.ReadyToLeaveInventory.Mode;

            if (mode == -3)
            {
                nextState = State.Scatter;
            }

            uint destination = s.ReadyToLeaveInventory.Destination;

            LeaveBuilding(false);
            s.LeavingBuilding.NextState = nextState;
            s.LeavingBuilding.FieldB = mode;
            s.LeavingBuilding.Destination = destination;
            s.LeavingBuilding.Direction = 0;
        }

        void HandleSerfFreeWalkingStateDestReached()
        {
            if (s.FreeWalking.NegDistance1 == -128 && s.FreeWalking.NegDistance2 < 0)
            {
                FindInventory();
                return;
            }

            var map = Game.Map;

            switch (SerfType)
            {
                case Type.Lumberjack:
                    if (s.FreeWalking.NegDistance1 == -128)
                    {
                        bool flagHasGone = false;

                        if (s.FreeWalking.NegDistance2 > 0)
                        {
                            if (!DropResource(Resource.Type.Lumber))
                            {
                                flagHasGone = true;
                            }
                        }

                        if (flagHasGone)
                        {
                            SetLostState();
                        }
                        else
                        {
                            SetState(State.ReadyToEnter);
                            s.ReadyToEnter.FieldB = 0;
                            Counter = 0;
                        }
                    }
                    else
                    {
                        s.FreeWalking.DistanceX = s.FreeWalking.NegDistance1;
                        s.FreeWalking.DistanceY = s.FreeWalking.NegDistance2;
                        var obj = map.GetObject(Position);

                        if (obj >= Map.Object.Tree0 &&
                            obj <= Map.Object.Pine7)
                        {
                            SetState(State.Logging);
                            s.FreeWalking.NegDistance1 = 0;
                            s.FreeWalking.NegDistance2 = 0;

                            if ((int)obj < 16)
                                s.FreeWalking.NegDistance1 = -1;

                            Animation = 116;
                            Counter = CounterFromAnimation[Animation];
                        }
                        else
                        {
                            // The expected tree is gone 
                            s.FreeWalking.NegDistance1 = -128;
                            s.FreeWalking.NegDistance2 = 0;
                            s.FreeWalking.Flags = 0;
                            Counter = 0;
                        }
                    }
                    break;
                case Type.Stonecutter:
                    if (s.FreeWalking.NegDistance1 == -128)
                    {
                        bool flagHasGone = false;

                        if (s.FreeWalking.NegDistance2 > 0)
                        {
                            if (!DropResource(Resource.Type.Stone))
                            {
                                flagHasGone = true;
                            }
                        }

                        if (flagHasGone)
                        {
                            SetLostState();
                        }
                        else
                        {
                            SetState(State.ReadyToEnter);
                            s.ReadyToEnter.FieldB = 0;
                            Counter = 0;
                        }
                    }
                    else
                    {
                        s.FreeWalking.DistanceX = s.FreeWalking.NegDistance1;
                        s.FreeWalking.DistanceY = s.FreeWalking.NegDistance2;

                        var newPosition = map.MoveUpLeft(Position);
                        var obj = map.GetObject(newPosition);

                        if (!map.HasSerf(newPosition) &&
                            obj >= Map.Object.Stone0 &&
                            obj <= Map.Object.Stone7)
                        {
                            Counter = 0;
                            StartWalking(Direction.UpLeft, 32, true);

                            SetState(State.StoneCutting);
                            s.FreeWalking.NegDistance2 = Counter >> 2;
                            s.FreeWalking.NegDistance1 = 0;
                        }
                        else
                        {
                            // The expected stone is gone or unavailable 
                            s.FreeWalking.NegDistance1 = -128;
                            s.FreeWalking.NegDistance2 = 0;
                            s.FreeWalking.Flags = 0;
                            Counter = 0;
                        }
                    }
                    break;
                case Type.Forester:
                    if (s.FreeWalking.NegDistance1 == -128)
                    {
                        SetState(State.ReadyToEnter);
                        s.ReadyToEnter.FieldB = 0;
                        Counter = 0;
                    }
                    else
                    {
                        s.FreeWalking.DistanceX = s.FreeWalking.NegDistance1;
                        s.FreeWalking.DistanceY = s.FreeWalking.NegDistance2;

                        if (map.GetObject(Position) == Map.Object.None)
                        {
                            SetState(State.Planting);
                            s.FreeWalking.NegDistance2 = 0;
                            Animation = 121;
                            Counter = CounterFromAnimation[Animation];
                        }
                        else
                        {
                            // The expected free space is no longer empty 
                            s.FreeWalking.NegDistance1 = -128;
                            s.FreeWalking.NegDistance2 = 0;
                            s.FreeWalking.Flags = 0;
                            Counter = 0;
                        }
                    }
                    break;
                case Type.Fisher:
                    if (s.FreeWalking.NegDistance1 == -128)
                    {
                        bool flagHasGone = false;

                        if (s.FreeWalking.NegDistance2 > 0)
                        {
                            if (!DropResource(Resource.Type.Fish))
                            {
                                flagHasGone = true;
                            }
                        }

                        if (flagHasGone)
                        {
                            SetLostState();
                        }
                        else
                        {
                            SetState(State.ReadyToEnter);
                            s.ReadyToEnter.FieldB = 0;
                            Counter = 0;
                        }
                    }
                    else
                    {
                        s.FreeWalking.DistanceX = s.FreeWalking.NegDistance1;
                        s.FreeWalking.DistanceY = s.FreeWalking.NegDistance2;

                        int fisherAnimation = 0;

                        if (map.Paths(Position) == 0)
                        {
                            if (map.TypeDown(Position) <= Map.Terrain.Water3 &&
                                map.TypeUp(map.MoveUpLeft(Position)) >= Map.Terrain.Grass0)
                            {
                                fisherAnimation = 132;
                            }
                            else if (map.TypeDown(map.MoveLeft(Position)) <= Map.Terrain.Water3 &&
                                     map.TypeUp(map.MoveUp(Position)) >= Map.Terrain.Grass0)
                            {
                                fisherAnimation = 131;
                            }
                        }

                        if (fisherAnimation == 0)
                        {
                            // Cannot fish here after all. 
                            s.FreeWalking.NegDistance1 = -128;
                            s.FreeWalking.NegDistance2 = 0;
                            s.FreeWalking.Flags = 0;
                            Counter = 0;
                        }
                        else
                        {
                            SetState(State.Fishing);
                            s.FreeWalking.NegDistance1 = 0;
                            s.FreeWalking.NegDistance2 = 0;
                            s.FreeWalking.Flags = 0;
                            Animation = fisherAnimation;
                            Counter = CounterFromAnimation[fisherAnimation];
                        }
                    }
                    break;
                case Type.Farmer:
                    if (s.FreeWalking.NegDistance1 == -128)
                    {
                        bool flagHasGone = false;

                        if (s.FreeWalking.NegDistance2 > 0)
                        {
                            if (!DropResource(Resource.Type.Wheat))
                            {
                                flagHasGone = true;
                            }
                        }

                        if (flagHasGone)
                        {
                            SetLostState();
                        }
                        else
                        {
                            SetState(State.ReadyToEnter);
                            s.ReadyToEnter.FieldB = 0;
                            Counter = 0;
                        }
                    }
                    else
                    {
                        s.FreeWalking.DistanceX = s.FreeWalking.NegDistance1;
                        s.FreeWalking.DistanceY = s.FreeWalking.NegDistance2;

                        var obj = map.GetObject(Position);

                        if (obj == Map.Object.Seeds5 ||
                            (obj >= Map.Object.Field0 &&
                             obj <= Map.Object.Field5))
                        {
                            // Existing field. 
                            Animation = 136;
                            s.FreeWalking.NegDistance1 = 1;
                            Counter = CounterFromAnimation[Animation];
                        }
                        else if (obj == Map.Object.None &&
                                 map.Paths(Position) == 0)
                        {
                            // Empty space. 
                            Animation = 135;
                            s.FreeWalking.NegDistance1 = 0;
                            Counter = CounterFromAnimation[Animation];
                        }
                        else
                        {
                            // Space not available after all. 
                            s.FreeWalking.NegDistance1 = -128;
                            s.FreeWalking.NegDistance2 = 0;
                            s.FreeWalking.Flags = 0;
                            Counter = 0;
                            break;
                        }

                        SetState(State.Farming);
                        s.FreeWalking.NegDistance2 = 0;
                    }
                    break;
                case Type.Geologist:
                    if (s.FreeWalking.NegDistance1 == -128)
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
                        s.FreeWalking.DistanceX = s.FreeWalking.NegDistance1;
                        s.FreeWalking.DistanceY = s.FreeWalking.NegDistance2;

                        if (map.GetObject(Position) == Map.Object.None)
                        {
                            SetState(State.SamplingGeoSpot);
                            s.FreeWalking.NegDistance1 = 0;
                            Animation = 141;
                            Counter = CounterFromAnimation[Animation];
                        }
                        else
                        {
                            // Destination is not a free space after all. 
                            s.FreeWalking.NegDistance1 = -128;
                            s.FreeWalking.NegDistance2 = 0;
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
                    if (s.FreeWalking.NegDistance1 == -128)
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

        void HandleSerfFreeWalkingSwitchOnDirection(Direction direction)
        {
            // A suitable direction has been found; walk.
            if (direction < Direction.Right)
            {
                throw new ExceptionFreeserf(Game, ErrorSystemType.Serf, "Wrong direction.");
            }

            int distanceX = (((int)direction < 3) ? 1 : -1) * ((((int)direction % 3) < 2) ? 1 : 0);
            int distanceY = (((int)direction < 3) ? 1 : -1) * ((((int)direction % 3) > 0) ? 1 : 0);

            Log.Verbose.Write(ErrorSystemType.Serf, $"serf {Index}: free walking: dest {s.FreeWalking.DistanceX}, {s.FreeWalking.DistanceY}, move {distanceX}, {distanceY}");

            s.FreeWalking.DistanceX -= distanceX;
            s.FreeWalking.DistanceY -= distanceY;

            StartWalking(direction, 32, true);

            if (s.FreeWalking.DistanceX == 0 &&
                s.FreeWalking.DistanceY == 0)
            {
                // Arriving to destination 
                s.FreeWalking.Flags = Misc.Bit(3);
            }
        }

        void HandleSerfFreeWalkingSwitchWithOther()
        {
            // No free position can be found. Switch with other serf. 
            MapPos newPosition = 0;
            var direction = Direction.None;
            Serf otherSerf = null;
            var map = Game.Map;
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var checkDirection in cycle)
            {
                newPosition = map.Move(Position, checkDirection);

                if (map.HasSerf(newPosition))
                {
                    otherSerf = Game.GetSerfAtPosition(newPosition);
                    var otherDirection = Direction.None;

                    if (otherSerf.IsWaiting(ref otherDirection) &&
                        otherDirection == checkDirection.Reverse() &&
                        otherSerf.SwitchWaiting(otherDirection))
                    {
                        direction = checkDirection;
                        break;
                    }
                }
            }

            if (direction > Direction.None)
            {
                int distanceX = (((int)direction < 3) ? 1 : -1) * ((((int)direction % 3) < 2) ? 1 : 0);
                int distanceY = (((int)direction < 3) ? 1 : -1) * ((((int)direction % 3) > 0) ? 1 : 0);

                Log.Verbose.Write(ErrorSystemType.Serf, $"free walking (switch): dest {s.FreeWalking.DistanceX}, {s.FreeWalking.DistanceY}, move {distanceX}, {distanceY}");

                s.FreeWalking.DistanceX -= distanceX;
                s.FreeWalking.DistanceY -= distanceY;

                if (s.FreeWalking.DistanceX == 0 &&
                    s.FreeWalking.DistanceY == 0)
                {
                    // Arriving to destination 
                    s.FreeWalking.Flags = Misc.Bit(3);
                }

                // Switch with other serf. 
                map.SetSerfIndex(Position, (int)otherSerf.Index);
                map.SetSerfIndex(newPosition, (int)Index);

                otherSerf.Animation = GetWalkingAnimation((int)map.GetHeight(Position) - (int)map.GetHeight(otherSerf.Position), direction.Reverse(), true);
                Animation = GetWalkingAnimation((int)map.GetHeight(newPosition) - (int)map.GetHeight(Position), direction, true);

                otherSerf.Counter = CounterFromAnimation[otherSerf.Animation];
                Counter = CounterFromAnimation[Animation];

                otherSerf.Position = Position;
                Position = newPosition;
            }
            else
            {
                Animation = 82;
                Counter = CounterFromAnimation[Animation];
            }
        }

        static readonly Direction[] DirectionFromOffset = new Direction[]
        {
            Direction.UpLeft, Direction.Up,   Direction.None,
            Direction.Left,   Direction.None, Direction.Right,
            Direction.None,   Direction.Down, Direction.DownRight
        };

        // Follow right-hand edge 
        static readonly Direction[] DirectionRightEdge = new Direction[]
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

        // Follow left-hand edge 
        static readonly Direction[] DirectionLeftEdge = new Direction[]
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
            Direction[] directions;
            int directionIndex;

            if (Misc.BitTest(s.FreeWalking.Flags, 3))
            {
                // Follow right-hand edge 
                directions = DirectionLeftEdge;
                directionIndex = (s.FreeWalking.Flags & 7) - 1;
            }
            else
            {
                // Follow right-hand edge 
                directions = DirectionRightEdge;
                directionIndex = (s.FreeWalking.Flags & 7) - 1;
            }

            int distanceX = s.FreeWalking.DistanceX;
            int distanceY = s.FreeWalking.DistanceY;

            // Check if destination is only one step away. 
            if (!water && Math.Abs(distanceX) <= 1 && Math.Abs(distanceY) <= 1 &&
                DirectionFromOffset[(distanceX + 1) + 3 * (distanceY + 1)] > Direction.None)
            {
                // Convert offset in two dimensions to direction variable.
                var directionFromOffset = DirectionFromOffset[(distanceX + 1) + 3 * (distanceY + 1)];
                var newPosition = Game.Map.Move(Position, directionFromOffset);

                if (!CanPassMapPosition(newPosition))
                {
                    if (SerfState != State.KnightFreeWalking && s.FreeWalking.NegDistance1 != -128)
                    {
                        s.FreeWalking.DistanceX += s.FreeWalking.NegDistance1;
                        s.FreeWalking.DistanceY += s.FreeWalking.NegDistance2;
                        s.FreeWalking.NegDistance1 = 0;
                        s.FreeWalking.NegDistance2 = 0;
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

                if (SerfState == State.KnightFreeWalking && s.FreeWalking.NegDistance1 != -128 &&
                    Game.Map.HasSerf(newPosition))
                {
                    // Wait for other serfs 
                    s.FreeWalking.Flags = 0;
                    Animation = 82;
                    Counter = CounterFromAnimation[Animation];
                    return 0;
                }
            }

            int directionOffset = 6 * directionIndex;
            var checkDirection = Direction.None;
            var direction = Direction.None;
            var map = Game.Map;
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction i in cycle)
            {
                var newPosition = map.Move(Position, directions[directionOffset + (int)i]);

                if (((water && map.GetObject(newPosition) == 0) ||
                     (!water && !map.IsInWater(newPosition) &&
                      CanPassMapPosition(newPosition))) && !map.HasSerf(newPosition))
                {
                    direction = directions[directionOffset + (int)i];
                    checkDirection = i;
                    break;
                }
            }

            if (checkDirection > Direction.None)
            {
                int upper = ((s.FreeWalking.Flags >> 4) & 0xf) + (int)checkDirection - 2;

                if ((int)checkDirection < 2 && upper < 0)
                {
                    s.FreeWalking.Flags = 0;
                    HandleSerfFreeWalkingSwitchOnDirection(direction);
                    return 0;
                }
                else if ((int)checkDirection > 2 && upper > 15)
                {
                    s.FreeWalking.Flags = 0;
                }
                else
                {
                    s.FreeWalking.Flags = (upper << 4) | (s.FreeWalking.Flags & 0x8) | ((int)direction + 1);
                    HandleSerfFreeWalkingSwitchOnDirection(direction);
                    return 0;
                }
            }
            else
            {
                s.FreeWalking.Flags = (s.FreeWalking.Flags & 0xf8);
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
        static readonly Direction[] DirectionForward = new Direction[]
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
                // Destination reached 
                HandleSerfFreeWalkingStateDestReached();
                return;
            }

            if ((s.FreeWalking.Flags & 7) != 0)
            {
                // Obstacle encountered, follow along the edge 
                if (HandleFreeWalkingFollowEdge() >= 0)
                    return;
            }

            // Move fowards 
            int directionIndex;
            int distance1 = s.FreeWalking.DistanceX;
            int distance2 = s.FreeWalking.DistanceY;

            if (distance1 < 0)
            {
                if (distance2 < 0)
                {
                    if (-distance2 < -distance1)
                    {
                        if (-2 * distance2 < -distance1)
                        {
                            directionIndex = 3;
                        }
                        else
                        {
                            directionIndex = 2;
                        }
                    }
                    else
                    {
                        if (-distance2 < -2 * distance1)
                        {
                            directionIndex = 1;
                        }
                        else
                        {
                            directionIndex = 0;
                        }
                    }
                }
                else
                {
                    if (distance2 >= -distance1)
                    {
                        directionIndex = 5;
                    }
                    else
                    {
                        directionIndex = 4;
                    }
                }
            }
            else
            {
                if (distance2 < 0)
                {
                    if (-distance2 >= distance1)
                    {
                        directionIndex = 11;
                    }
                    else
                    {
                        directionIndex = 10;
                    }
                }
                else
                {
                    if (distance2 < distance1)
                    {
                        if (2 * distance2 < distance1)
                        {
                            directionIndex = 9;
                        }
                        else
                        {
                            directionIndex = 8;
                        }
                    }
                    else
                    {
                        if (distance2 < 2 * distance1)
                        {
                            directionIndex = 7;
                        }
                        else
                        {
                            directionIndex = 6;
                        }
                    }
                }
            }

            // Try to move directly in the preferred direction.
            int directionOffset = 6 * directionIndex;
            var direction = DirectionForward[directionOffset];
            var map = Game.Map;
            var newPosition = map.Move(Position, direction);

            if (((water && map.GetObject(newPosition) == 0) ||
                 (!water && !map.IsInWater(newPosition) &&
                 CanPassMapPosition(newPosition))) &&
                 !map.HasSerf(newPosition))
            {
                HandleSerfFreeWalkingSwitchOnDirection(direction);
                return;
            }

            // Check if destination is only one step away.
            if (!water && Math.Abs(distance1) <= 1 && Math.Abs(distance2) <= 1 &&
                DirectionFromOffset[(distance1 + 1) + 3 * (distance2 + 1)] > Direction.None)
            {
                // Convert offset in two dimensions to direction variable.
                direction = DirectionFromOffset[(distance1 + 1) + 3 * (distance2 + 1)];
                newPosition = map.Move(Position, direction);

                if (!CanPassMapPosition(newPosition))
                {
                    if (SerfState != State.KnightFreeWalking && s.FreeWalking.NegDistance1 != -128)
                    {
                        s.FreeWalking.DistanceX += s.FreeWalking.NegDistance1;
                        s.FreeWalking.DistanceY += s.FreeWalking.NegDistance2;
                        s.FreeWalking.NegDistance1 = 0;
                        s.FreeWalking.NegDistance2 = 0;
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

                if (SerfState == State.KnightFreeWalking && s.FreeWalking.NegDistance1 != -128
                    && map.HasSerf(newPosition))
                {
                    var otherSerf = Game.GetSerfAtPosition(newPosition);
                    var otherDirection = Direction.None;

                    if (otherSerf.IsWaiting(ref otherDirection) &&
                        (otherDirection == direction.Reverse() || otherDirection == Direction.None) &&
                        otherSerf.SwitchWaiting(direction.Reverse()))
                    {
                        // Do the switch 
                        otherSerf.Position = Position;
                        map.SetSerfIndex(otherSerf.Position, (int)otherSerf.Index);
                        otherSerf.Animation = GetWalkingAnimation(
                            (int)map.GetHeight(otherSerf.Position) - (int)map.GetHeight(newPosition), direction.Reverse(), true);
                        otherSerf.Counter = CounterFromAnimation[otherSerf.Animation];

                        Animation = GetWalkingAnimation(
                            (int)map.GetHeight(newPosition) - (int)map.GetHeight(Position), direction, true);
                        Counter = CounterFromAnimation[Animation];

                        Position = newPosition;
                        map.SetSerfIndex(Position, (int)Index);

                        return;
                    }

                    if (otherSerf.SerfState == State.Walking ||
                        otherSerf.SerfState == State.Transporting)
                    {
                        ++s.FreeWalking.NegDistance2;

                        if (s.FreeWalking.NegDistance2 >= 10)
                        {
                            s.FreeWalking.NegDistance2 = 0;

                            if (otherSerf.SerfState == State.Transporting)
                            {
                                if (map.HasFlag(newPosition))
                                {
                                    if (otherSerf.s.Walking.WaitCounter != -1)
                                    {
                                        // TODO ?
                                        // int dir = otherSerf.s.Walking.Direction;
                                        // if (dir < 0) dir += 6;
                                        Log.Debug.Write(ErrorSystemType.Serf, $"TODO remove {otherSerf.Index} from path");
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

            // Look for another direction to go in. 
            var newDirection = Direction.None;

            for (int i = 0; i < 5; ++i)
            {
                direction = DirectionForward[directionOffset + 1 + i];
                newPosition = map.Move(Position, direction);

                if (((water && map.GetObject(newPosition) == 0) ||
                     (!water && !map.IsInWater(newPosition) &&
                      CanPassMapPosition(newPosition))) && !map.HasSerf(newPosition))
                {
                    newDirection = (Direction)i;
                    break;
                }
            }

            if (newDirection < 0)
            {
                HandleSerfFreeWalkingSwitchWithOther();
                return;
            }

            int edge = 0;

            if (Misc.BitTest(directionIndex ^ (int)newDirection, 0))
                edge = 1;

            int upper = ((int)newDirection / 2) + 1;

            s.FreeWalking.Flags = (upper << 4) | (edge << 3) | ((int)direction + 1);

            HandleSerfFreeWalkingSwitchOnDirection(direction);
        }

        void HandleSerfFreeWalkingState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                HandleFreeWalkingCommon();
            }
        }

        void HandleSerfLoggingState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                ++s.FreeWalking.NegDistance2;

                int newObject = -1;

                if (s.FreeWalking.NegDistance1 != 0)
                {
                    newObject = (int)Map.Object.FelledTree0 + s.FreeWalking.NegDistance2 - 1;
                }
                else
                {
                    newObject = (int)Map.Object.FelledPine0 + s.FreeWalking.NegDistance2 - 1;
                }

                // Change map object. 
                Game.Map.SetObject(Position, (Map.Object)newObject, -1);

                if (s.FreeWalking.NegDistance2 < 5)
                {
                    Animation = 116 + s.FreeWalking.NegDistance2;
                    Counter += CounterFromAnimation[Animation];
                }
                else
                {
                    SetState(State.FreeWalking);
                    Counter = 0;
                    s.FreeWalking.NegDistance1 = -128;
                    s.FreeWalking.NegDistance2 = 1;
                    s.FreeWalking.Flags = 0;

                    return;
                }
            }
        }

        void HandleSerfPlanningLoggingState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                int distance = (Game.RandomInt() & 0x7f) + 1;
                var position = Game.Map.PositionAddSpirally(Position, (uint)distance);
                var obj = Game.Map.GetObject(position);

                if (obj >= Map.Object.Tree0 && obj <= Map.Object.Pine7)
                {
                    SetState(State.ReadyToLeave);
                    s.LeavingBuilding.FieldB = Map.GetSpiralPattern()[2 * distance] - 1;
                    s.LeavingBuilding.Destination = (uint)Map.GetSpiralPattern()[2 * distance + 1] - 1;
                    s.LeavingBuilding.Destination2 = -Map.GetSpiralPattern()[2 * distance] + 1;
                    s.LeavingBuilding.Direction = -Map.GetSpiralPattern()[2 * distance + 1] + 1;
                    s.LeavingBuilding.NextState = State.FreeWalking;
                    Log.Verbose.Write(ErrorSystemType.Serf, $"planning logging: tree found, dist {s.LeavingBuilding.FieldB}, {s.LeavingBuilding.Destination}.");

                    return;
                }

                Counter += 400;
            }
        }

        void HandleSerfPlanningPlantingState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            var map = Game.Map;

            while (Counter < 0)
            {
                int distance = (Game.RandomInt() & 0x7f) + 1;
                var position = map.PositionAddSpirally(Position, (uint)distance);

                if (map.Paths(position) == 0 &&
                    map.GetObject(position) == Map.Object.None &&
                    map.TypeUp(position) == Map.Terrain.Grass1 &&
                    map.TypeDown(position) == Map.Terrain.Grass1 &&
                    map.TypeUp(map.MoveUpLeft(position)) == Map.Terrain.Grass1 &&
                    map.TypeDown(map.MoveUpLeft(position)) == Map.Terrain.Grass1)
                {
                    SetState(State.ReadyToLeave);
                    s.LeavingBuilding.FieldB = Map.GetSpiralPattern()[2 * distance] - 1;
                    s.LeavingBuilding.Destination = (uint)Map.GetSpiralPattern()[2 * distance + 1] - 1;
                    s.LeavingBuilding.Destination2 = -Map.GetSpiralPattern()[2 * distance] + 1;
                    s.LeavingBuilding.Direction = -Map.GetSpiralPattern()[2 * distance + 1] + 1;
                    s.LeavingBuilding.NextState = State.FreeWalking;
                    Log.Verbose.Write(ErrorSystemType.Serf, $"planning planting: free space found, dist {s.LeavingBuilding.FieldB}, {s.LeavingBuilding.Destination}.");

                    return;
                }

                Counter += 700;
            }
        }

        void HandleSerfPlantingState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            var map = Game.Map;

            while (Counter < 0)
            {
                if (s.FreeWalking.NegDistance2 != 0)
                {
                    SetState(State.FreeWalking);
                    s.FreeWalking.NegDistance1 = -128;
                    s.FreeWalking.NegDistance2 = 0;
                    s.FreeWalking.Flags = 0;
                    Counter = 0;

                    return;
                }

                // Plant a tree 
                Animation = 122;
                var newObject = (Map.Object)(Map.Object.NewPine + (Game.RandomInt() & 1));

                if (map.Paths(Position) == 0 && map.GetObject(Position) == Map.Object.None)
                {
                    map.SetObject(Position, newObject, -1);
                }

                s.FreeWalking.NegDistance2 = -s.FreeWalking.NegDistance2 - 1;
                Counter += 128;
            }
        }

        void HandleSerfPlanningStonecutting()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            var map = Game.Map;

            while (Counter < 0)
            {
                int distance = (Game.RandomInt() & 0x7f) + 1;
                var position = map.PositionAddSpirally(Position, (uint)distance);
                var obj = map.GetObject(map.MoveUpLeft(position));

                if (obj >= Map.Object.Stone0 && obj <= Map.Object.Stone7 &&
                    CanPassMapPosition(position))
                {
                    SetState(State.ReadyToLeave);
                    s.LeavingBuilding.FieldB = Map.GetSpiralPattern()[2 * distance] - 1;
                    s.LeavingBuilding.Destination = (uint)Map.GetSpiralPattern()[2 * distance + 1] - 1;
                    s.LeavingBuilding.Destination2 = -Map.GetSpiralPattern()[2 * distance] + 1;
                    s.LeavingBuilding.Direction = -Map.GetSpiralPattern()[2 * distance + 1] + 1;
                    s.LeavingBuilding.NextState = State.StoneCutterFreeWalking;
                    Log.Verbose.Write(ErrorSystemType.Serf, $"planning stonecutting: stone found, dist {s.LeavingBuilding.FieldB}, {s.LeavingBuilding.Destination}.");

                    return;
                }

                Counter += 100;
            }
        }

        void HandleStonecutterFreeWalking()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            var map = Game.Map;

            while (Counter < 0)
            {
                var position = map.MoveUpLeft(Position);

                if (!map.HasSerf(Position) && map.GetObject(position) >= Map.Object.Stone0 &&
                    map.GetObject(position) <= Map.Object.Stone7)
                {
                    s.FreeWalking.NegDistance1 += s.FreeWalking.DistanceX;
                    s.FreeWalking.NegDistance2 += s.FreeWalking.DistanceY;
                    s.FreeWalking.DistanceX = 0;
                    s.FreeWalking.DistanceY = 0;
                    s.FreeWalking.Flags = 8;
                }

                HandleFreeWalkingCommon();
            }
        }

        void HandleSerfStonecuttingState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            if (s.FreeWalking.NegDistance1 == 0)
            {
                if (Counter > s.FreeWalking.NegDistance2)
                    return;

                Counter -= s.FreeWalking.NegDistance2 + 1;
                s.FreeWalking.NegDistance1 = 1;
                Animation = 123;
                Counter += 1536;
            }

            while (Counter < 0)
            {
                if (s.FreeWalking.NegDistance1 != 1)
                {
                    SetState(State.FreeWalking);
                    s.FreeWalking.NegDistance1 = -128;
                    s.FreeWalking.NegDistance2 = 1;
                    s.FreeWalking.Flags = 0;
                    Counter = 0;
                    return;
                }

                var map = Game.Map;

                if (map.HasSerf(map.MoveDownRight(Position)))
                {
                    Counter = 0;
                    return;
                }

                // Decrement stone quantity or remove entirely if this
                // was the last piece.
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
                state.Tick = Game.Tick;

                s.FreeWalking.NegDistance1 = 2;
            }
        }

        void HandleSerfSawingState()
        {
            if (s.Sawing.Mode == 0)
            {
                var building = GetBuildingAtPosition();

                if (building.UseResourceInStock(0))
                {
                    s.Sawing.Mode = 1;
                    Animation = 124;
                    Counter = CounterFromAnimation[Animation];
                    state.Tick = Game.Tick;
                    Game.Map.SetSerfIndex(Position, (int)Index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - state.Tick);
                state.Tick = Game.Tick;
                Counter -= delta;

                if (Counter >= 0)
                    return;

                Game.Map.SetSerfIndex(Position, 0);
                SetState(State.MoveResourceOut);
                s.MoveResourceOut.Resource = (uint)Resource.Type.Plank + 1;
                s.MoveResourceOut.ResourceDestination = 0;
                s.MoveResourceOut.NextState = State.DropResourceOut;

                // Update resource stats. 
                var player = Game.GetPlayer(Player);
                player.IncreaseResourceCount(Resource.Type.Plank);
            }
        }

        void HandleSerfLostState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            var map = Game.Map;

            while (Counter < 0)
            {
                // Try to find a suitable destination. 
                for (int i = 0; i < 258; ++i)
                {
                    int distance = (s.Lost.FieldB == 0) ? 1 + i : 258 - i;
                    var destination = map.PositionAddSpirally(Position, (uint)distance);

                    if (map.HasFlag(destination))
                    {
                        var flag = Game.GetFlagAtPosition(destination);

                        if ((flag.LandPaths != 0 ||
                             (flag.HasInventory() && flag.AcceptsSerfs())) &&
                              map.HasOwner(destination) &&
                              map.GetOwner(destination) == Player)
                        {
                            if (IsKnight)
                            {
                                SetState(State.KnightFreeWalking);
                            }
                            else
                            {
                                SetState(State.FreeWalking);
                            }

                            s.FreeWalking.DistanceX = Map.GetSpiralPattern()[2 * distance];
                            s.FreeWalking.DistanceY = Map.GetSpiralPattern()[2 * distance + 1];
                            s.FreeWalking.NegDistance1 = -128;
                            s.FreeWalking.NegDistance2 = -1;
                            s.FreeWalking.Flags = 0;
                            Counter = 0;

                            return;
                        }
                    }
                }

                // Choose a random destination 
                int size = 16;
                int numRemainingTries = 10;

                while (true)
                {
                    --numRemainingTries;

                    if (numRemainingTries < 0)
                    {
                        if (size < 64)
                        {
                            numRemainingTries = 19;
                            size *= 2;
                        }
                        else
                        {
                            numRemainingTries = -1;
                            size = 16;
                        }
                    }

                    int random = Game.RandomInt();
                    int column = (random & (size - 1)) - (size / 2);
                    int row = ((random >> 8) & (size - 1)) - (size / 2);
                    var destination = map.PositionAdd(Position, column, row);

                    if ((map.GetObject(destination) == 0 && map.GetHeight(destination) > 0) ||
                        (map.HasFlag(destination) &&
                        (map.HasOwner(destination) &&
                        map.GetOwner(destination) == Player)))
                    {
                        if (SerfType >= Type.Knight0 && SerfType <= Type.Knight4)
                        {
                            SetState(State.KnightFreeWalking);
                        }
                        else
                        {
                            SetState(State.FreeWalking);
                        }

                        s.FreeWalking.DistanceX = column;
                        s.FreeWalking.DistanceY = row;
                        s.FreeWalking.NegDistance1 = -128;
                        s.FreeWalking.NegDistance2 = -1;
                        s.FreeWalking.Flags = 0;
                        Counter = 0;

                        return;
                    }
                }
            }
        }

        void HandleLostSailor()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            var map = Game.Map;

            while (Counter < 0)
            {
                // Try to find a suitable destination. 
                for (uint i = 0; i < 258; ++i)
                {
                    var destination = map.PositionAddSpirally(Position, i);

                    if (map.HasFlag(destination))
                    {
                        var flag = Game.GetFlagAtPosition(destination);

                        if (flag.LandPaths != 0 &&
                            map.HasOwner(destination) &&
                            map.GetOwner(destination) == Player)
                        {
                            SetState(State.FreeSailing);

                            s.FreeWalking.DistanceX = Map.GetSpiralPattern()[2 * i];
                            s.FreeWalking.DistanceY = Map.GetSpiralPattern()[2 * i + 1];
                            s.FreeWalking.NegDistance1 = -128;
                            s.FreeWalking.NegDistance2 = -1;
                            s.FreeWalking.Flags = 0;
                            Counter = 0;

                            return;
                        }
                    }
                }

                // Choose a random, empty destination 
                while (true)
                {
                    int random = Game.RandomInt();
                    int column = (random & 0x1f) - 16;
                    int row = ((random >> 8) & 0x1f) - 16;
                    var destination = map.PositionAdd(Position, column, row);

                    if (map.GetObject(destination) == 0)
                    {
                        SetState(State.FreeSailing);

                        s.FreeWalking.DistanceX = column;
                        s.FreeWalking.DistanceY = row;
                        s.FreeWalking.NegDistance1 = -128;
                        s.FreeWalking.NegDistance2 = -1;
                        s.FreeWalking.Flags = 0;
                        Counter = 0;

                        return;
                    }
                }
            }
        }

        void HandleFreeSailing()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
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
                state.Tick = Game.Tick;

                SetState(State.Lost);
                s.Lost.FieldB = 0;

                Game.AddSerfForDrawing(this, Position);
            }
            else
            {
                SetLostState();
            }
        }

        static readonly Resource.Type[] ResFromMineType = new Resource.Type[]
        {
            Resource.Type.GoldOre, Resource.Type.IronOre,
            Resource.Type.Coal, Resource.Type.Stone
        };

        void HandleSerfMiningState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            var map = Game.Map;

            while (Counter < 0)
            {
                var building = GetBuildingAtPosition();

                Log.Verbose.Write(ErrorSystemType.Serf, $"mining substate: {s.Mining.Substate}.");

                switch (s.Mining.Substate)
                {
                    case 0: // Base state
                        {
                            /* There is a small chance that the miner will
                               not require food and skip to state 2. */
                            int random = Game.RandomInt();

                            if ((random & 7) == 0)
                            {
                                s.Mining.Substate = 2;
                            }
                            else
                            {
                                s.Mining.Substate = 1;
                            }

                            Counter += 100 + (random & 0x1ff);
                            break;
                        }
                    case 1: // Wait idle or initiate mining by consuming food
                        if (building.UseResourceInStock(0))
                        {
                            // Eat the food. 
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

                            if (Counter < 0)
                                Counter = 255;
                        }
                        break;
                    case 2: // Initiate mining without food
                        s.Mining.Substate = 3;
                        map.SetSerfIndex(Position, (int)Index);
                        Animation = 125;
                        Counter = CounterFromAnimation[Animation];
                        break;
                    case 3: // Walk to elevator
                        s.Mining.Substate = 4;
                        building.StopActivity();
                        Animation = 126;
                        Counter = CounterFromAnimation[Animation];
                        break;
                    case 4: // Elevator moves down
                        {
                            building.StartPlayingSfx();
                            map.SetSerfIndex(Position, 0);
                            // fall through 
                        }
                        goto case 5;
                    case 5: // Underground mining
                    case 6: // Underground mining
                    case 7: // Underground mining
                        {
                            ++s.Mining.Substate;

                            // Look for resource in ground.
                            var destination = map.PositionAddSpirally(Position, (uint)(Game.RandomInt() >> 2) & 0x1f);

                            if ((map.GetObject(destination) == Map.Object.None ||
                                 map.GetObject(destination) > Map.Object.Castle) &&
                                map.GetResourceType(destination) == s.Mining.Deposit &&
                                map.GetResourceAmount(destination) > 0)
                            {
                                // Decrement resource count in ground. 
                                map.RemoveGroundDeposit(destination, 1);

                                // Hand resource to miner. 
                                s.Mining.Resource = (uint)ResFromMineType[(int)s.Mining.Deposit - 1] + 1;
                                s.Mining.Substate = 8;
                            }

                            Counter += 1000;
                            break;
                        }
                    case 8: // Finished underground mining
                        map.SetSerfIndex(Position, (int)Index);
                        s.Mining.Substate = 9;
                        building.StopPlayingSfx();
                        Animation = 127;
                        Counter = CounterFromAnimation[Animation];
                        break;
                    case 9: // Elevator comes up
                        s.Mining.Substate = 10;
                        building.IncreaseMining((int)s.Mining.Resource);
                        Animation = 128;
                        Counter = CounterFromAnimation[Animation];
                        break;
                    case 10: // Move resource out or finish working
                        map.SetSerfIndex(Position, 0);
                        if (s.Mining.Resource == 0)
                        {
                            s.Mining.Substate = 0;
                            Counter = 0;
                        }
                        else
                        {
                            uint resource = s.Mining.Resource;
                            map.SetSerfIndex(Position, 0);

                            SetState(State.MoveResourceOut);
                            s.MoveResourceOut.Resource = resource;
                            s.MoveResourceOut.ResourceDestination = 0;
                            s.MoveResourceOut.NextState = State.DropResourceOut;

                            // Update resource stats. 
                            var player = Game.GetPlayer(Player);
                            player.IncreaseResourceCount((Resource.Type)(resource - 1));

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
            var building = GetBuildingAtPosition();

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
                    state.Tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, (int)Index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - state.Tick);
                state.Tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    --s.Smelting.Counter;

                    if (s.Smelting.Counter < 0)
                    {
                        building.StopActivity();

                        Resource.Type resource;

                        if (s.Smelting.Type == 0)
                        {
                            resource = Resource.Type.Steel;
                        }
                        else
                        {
                            resource = Resource.Type.GoldBar;
                        }

                        SetState(State.MoveResourceOut);

                        s.MoveResourceOut.Resource = (uint)resource + 1u;
                        s.MoveResourceOut.ResourceDestination = 0;
                        s.MoveResourceOut.NextState = State.DropResourceOut;

                        // Update resource stats. 
                        var player = Game.GetPlayer(Player);
                        player.IncreaseResourceCount(resource);

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
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            var map = Game.Map;

            while (Counter < 0)
            {
                var distance = (uint)((Game.RandomInt() >> 2) & 0x3f) + 1u;
                var destination = map.PositionAddSpirally(Position, distance);

                if (map.GetObject(destination) == Map.Object.None &&
                    map.Paths(destination) == 0 &&
                    ((map.TypeDown(destination) <= Map.Terrain.Water3 &&
                      map.TypeUp(map.MoveUpLeft(destination)) >= Map.Terrain.Grass0) ||
                     (map.TypeDown(map.MoveLeft(destination)) <= Map.Terrain.Water3 &&
                      map.TypeUp(map.MoveUp(destination)) >= Map.Terrain.Grass0)))
                {
                    SetState(State.ReadyToLeave);
                    s.LeavingBuilding.FieldB = Map.GetSpiralPattern()[2 * distance] - 1;
                    s.LeavingBuilding.Destination = (uint)Map.GetSpiralPattern()[2 * distance + 1] - 1;
                    s.LeavingBuilding.Destination2 = -Map.GetSpiralPattern()[2 * distance] + 1;
                    s.LeavingBuilding.Direction = -Map.GetSpiralPattern()[2 * distance + 1] + 1;
                    s.LeavingBuilding.NextState = State.FreeWalking;
                    Log.Verbose.Write(ErrorSystemType.Serf, $"planning fishing: lake found, dist {s.LeavingBuilding.FieldB},{s.LeavingBuilding.Destination}");

                    return;
                }

                Counter += 100;
            }
        }

        void HandleSerfFishingState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0)
            {
                if (s.FreeWalking.NegDistance2 != 0 ||
                    s.FreeWalking.Flags == 10)
                {
                    // Stop fishing. Walk back. 
                    SetState(State.FreeWalking);
                    s.FreeWalking.NegDistance1 = -128;
                    s.FreeWalking.Flags = 0;
                    Counter = 0;

                    return;
                }

                s.FreeWalking.NegDistance1 += 1;
                if ((s.FreeWalking.NegDistance1 % 2) == 0)
                {
                    Animation -= 2;
                    Counter += 768;
                    continue;
                }

                var map = Game.Map;
                Direction direction;

                if (Animation == 131)
                {
                    if (map.IsInWater(map.MoveLeft(Position)))
                    {
                        direction = Direction.Left;
                    }
                    else
                    {
                        direction = Direction.Down;
                    }
                }
                else
                {
                    if (map.IsInWater(map.MoveRight(Position)))
                    {
                        direction = Direction.Right;
                    }
                    else
                    {
                        direction = Direction.DownRight;
                    }
                }

                uint resourceAmount = map.GetResourceFish(map.Move(Position, direction));

                if (resourceAmount > 0 && (Game.RandomInt() & 0x3f) + 4 < resourceAmount)
                {
                    // Caught a fish.
                    map.RemoveFish(map.Move(Position, direction), 1);
                    s.FreeWalking.NegDistance2 = 1 + (int)Resource.Type.Fish;
                }

                ++s.FreeWalking.Flags;
                Animation += 2;
                Counter += 128;
            }
        }

        void HandleSerfPlanningFarmingState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            var map = Game.Map;

            while (Counter < 0)
            {
                int distance = ((Game.RandomInt() >> 2) & 0x1f) + 7;
                var destination = map.PositionAddSpirally(Position, (uint)distance);

                // If destination doesn't have an object it must be
                // of the correct type and the surrounding spaces
                // must not be occupied by large buildings.
                // If it Has_ an object it must be an existing field.
                if ((map.GetObject(destination) == Map.Object.None &&
                     map.TypeUp(destination) == Map.Terrain.Grass1 &&
                     map.TypeDown(destination) == Map.Terrain.Grass1 &&
                     map.Paths(destination) == 0 &&
                     map.GetObject(map.MoveRight(destination)) != Map.Object.LargeBuilding &&
                     map.GetObject(map.MoveRight(destination)) != Map.Object.Castle &&
                     map.GetObject(map.MoveDownRight(destination)) != Map.Object.LargeBuilding &&
                     map.GetObject(map.MoveDownRight(destination)) != Map.Object.Castle &&
                     map.GetObject(map.MoveDown(destination)) != Map.Object.LargeBuilding &&
                     map.GetObject(map.MoveDown(destination)) != Map.Object.Castle &&
                     map.TypeDown(map.MoveLeft(destination)) == Map.Terrain.Grass1 &&
                     map.GetObject(map.MoveLeft(destination)) != Map.Object.LargeBuilding &&
                     map.GetObject(map.MoveLeft(destination)) != Map.Object.Castle &&
                     map.TypeUp(map.MoveUpLeft(destination)) == Map.Terrain.Grass1 &&
                     map.TypeDown(map.MoveUpLeft(destination)) == Map.Terrain.Grass1 &&
                     map.GetObject(map.MoveUpLeft(destination)) != Map.Object.LargeBuilding &&
                     map.GetObject(map.MoveUpLeft(destination)) != Map.Object.Castle &&
                     map.TypeUp(map.MoveUp(destination)) == Map.Terrain.Grass1 &&
                     map.GetObject(map.MoveUp(destination)) != Map.Object.LargeBuilding &&
                     map.GetObject(map.MoveUp(destination)) != Map.Object.Castle) ||
                     map.GetObject(destination) == Map.Object.Seeds5 ||
                     (map.GetObject(destination) >= Map.Object.Field0 &&
                     map.GetObject(destination) <= Map.Object.Field5))
                {
                    SetState(State.ReadyToLeave);
                    s.LeavingBuilding.FieldB = Map.GetSpiralPattern()[2 * distance] - 1;
                    s.LeavingBuilding.Destination = (uint)Map.GetSpiralPattern()[2 * distance + 1] - 1;
                    s.LeavingBuilding.Destination2 = -Map.GetSpiralPattern()[2 * distance] + 1;
                    s.LeavingBuilding.Direction = -Map.GetSpiralPattern()[2 * distance + 1] + 1;
                    s.LeavingBuilding.NextState = State.FreeWalking;
                    Log.Verbose.Write(ErrorSystemType.Serf, $"planning farming: field spot found, dist {s.LeavingBuilding.FieldB}, {s.LeavingBuilding.Destination}.");

                    return;
                }

                Counter += 500;
            }
        }

        void HandleSerfFarmingState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            if (Counter >= 0)
                return;

            var map = Game.Map;

            if (s.FreeWalking.NegDistance1 == 0)
            {
                // Sowing. 
                if (map.GetObject(Position) == 0 && map.Paths(Position) == 0)
                {
                    map.SetObject(Position, Map.Object.Seeds0, -1);
                }
            }
            else
            {
                // Harvesting. 
                s.FreeWalking.NegDistance2 = 1;

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
            s.FreeWalking.NegDistance1 = -128;
            s.FreeWalking.Flags = 0;
            Counter = 0;
        }

        void HandleSerfMillingState()
        {
            var building = GetBuildingAtPosition();

            if (s.Milling.Mode == 0)
            {
                if (building.UseResourceInStock(0))
                {
                    building.StartActivity();

                    s.Milling.Mode = 1;
                    Animation = 137;
                    Counter = CounterFromAnimation[Animation];
                    state.Tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, (int)Index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - state.Tick);
                state.Tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    ++s.Milling.Mode;

                    if (s.Milling.Mode == 5)
                    {
                        // Done milling. 
                        building.StopActivity();
                        SetState(State.MoveResourceOut);
                        s.MoveResourceOut.Resource = 1 + (uint)Resource.Type.Flour;
                        s.MoveResourceOut.ResourceDestination = 0;
                        s.MoveResourceOut.NextState = State.DropResourceOut;

                        var player = Game.GetPlayer(Player);
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
            var building = GetBuildingAtPosition();

            if (s.Baking.Mode == 0)
            {
                if (building.UseResourceInStock(0))
                {
                    s.Baking.Mode = 1;
                    Animation = 138;
                    Counter = CounterFromAnimation[Animation];
                    state.Tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, (int)Index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - state.Tick);
                state.Tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    ++s.Baking.Mode;

                    if (s.Baking.Mode == 3)
                    {
                        // Done baking. 
                        building.StopActivity();

                        SetState(State.MoveResourceOut);
                        s.MoveResourceOut.Resource = 1 + (uint)Resource.Type.Bread;
                        s.MoveResourceOut.ResourceDestination = 0;
                        s.MoveResourceOut.NextState = State.DropResourceOut;

                        var player = Game.GetPlayer(Player);
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
            // When the serf is present there is also at least one
            // pig present and at most eight.

            var building = GetBuildingAtPosition();

            if (s.PigFarming.Mode == 0)
            {
                if (building.UseResourceInStock(0))
                {
                    s.PigFarming.Mode = 1;
                    Animation = 139;
                    Counter = CounterFromAnimation[Animation];
                    state.Tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, (int)Index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - state.Tick);
                state.Tick = Game.Tick;
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
                            // Pig is ready for the butcher. 
                            building.SendPigToButcher();

                            SetState(State.MoveResourceOut);
                            s.MoveResourceOut.Resource = 1 + (uint)Resource.Type.Pig;
                            s.MoveResourceOut.ResourceDestination = 0;
                            s.MoveResourceOut.NextState = State.DropResourceOut;

                            // Update resource stats. 
                            var player = Game.GetPlayer(Player);
                            player.IncreaseResourceCount(Resource.Type.Pig);
                        }
                        else if ((Game.RandomInt() & 0xf) != 0)
                        {
                            s.PigFarming.Mode = 1;
                            Animation = 139;
                            Counter = CounterFromAnimation[Animation];
                            state.Tick = Game.Tick;
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

                        if (building.PigsCount() == 0 || (building.PigsCount() < 8 &&
                            Game.RandomInt() < BreedingProbability[building.PigsCount() - 1]))
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
            var building = GetBuildingAtPosition();

            if (s.Butchering.Mode == 0)
            {
                if (building.UseResourceInStock(0))
                {
                    s.Butchering.Mode = 1;
                    Animation = 140;
                    Counter = CounterFromAnimation[Animation];
                    state.Tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, (int)Index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - state.Tick);
                state.Tick = Game.Tick;
                Counter -= delta;

                if (Counter < 0)
                {
                    // Done butchering. 
                    Game.Map.SetSerfIndex(Position, 0);

                    SetState(State.MoveResourceOut);
                    s.MoveResourceOut.Resource = 1 + (uint)Resource.Type.Meat;
                    s.MoveResourceOut.ResourceDestination = 0;
                    s.MoveResourceOut.NextState = State.DropResourceOut;

                    // Update resource stats. 
                    var player = Game.GetPlayer(Player);
                    player.IncreaseResourceCount(Resource.Type.Meat);
                }
            }
        }

        void HandleSerfMakingWeaponState()
        {
            var building = GetBuildingAtPosition();

            if (s.MakingWeapon.Mode == 0)
            {
                // One of each resource makes a sword and a shield.
                // If a sword has been made a shield can be made
                // without more resources.
                if (!building.FreeShieldPossible)
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
                state.Tick = Game.Tick;

                Game.Map.SetSerfIndex(Position, (int)Index);
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - state.Tick);
                state.Tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    ++s.MakingWeapon.Mode;

                    if (s.MakingWeapon.Mode == 7)
                    {
                        // Done making sword or shield. 
                        building.StopActivity();
                        Game.Map.SetSerfIndex(Position, 0);

                        var resource = building.FreeShieldPossible ? Resource.Type.Shield : Resource.Type.Sword;

                        building.FreeShieldPossible = !building.FreeShieldPossible;

                        SetState(State.MoveResourceOut);
                        s.MoveResourceOut.Resource = 1 + (uint)resource;
                        s.MoveResourceOut.ResourceDestination = 0;
                        s.MoveResourceOut.NextState = State.DropResourceOut;

                        // Update resource stats. 
                        var player = Game.GetPlayer(Player);
                        player.IncreaseResourceCount(resource);
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
            var building = GetBuildingAtPosition();

            if (s.MakingTool.Mode == 0)
            {
                if (building.UseResourcesInStocks())
                {
                    s.MakingTool.Mode = 1;
                    Animation = 144;
                    Counter = CounterFromAnimation[Animation];
                    state.Tick = Game.Tick;

                    Game.Map.SetSerfIndex(Position, (int)Index);
                }
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - state.Tick);
                state.Tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    ++s.MakingTool.Mode;

                    if (s.MakingTool.Mode == 4)
                    {
                        // Done making tool. 
                        Game.Map.SetSerfIndex(Position, 0);

                        var player = Game.GetPlayer(Player);
                        int totalToolPriority = 0;

                        for (int i = 0; i < 9; ++i)
                            totalToolPriority += player.GetToolPriority(i);

                        totalToolPriority >>= 4;

                        int resource = -1;

                        if (totalToolPriority > 0)
                        {
                            // Use defined tool priorities. 
                            int priorityOffset = (totalToolPriority * Game.RandomInt()) >> 16;

                            for (int i = 0; i < 9; ++i)
                            {
                                priorityOffset -= player.GetToolPriority(i) >> 4;

                                if (priorityOffset < 0)
                                {
                                    resource = (int)Resource.Type.Shovel + i;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // Completely random. 
                            resource = (int)Resource.Type.Shovel + ((9 * Game.RandomInt()) >> 16);
                        }

                        if (resource == -1) // TODO: This was added because resource is sometimes -1 when totalToolPriority > 0. But should this happen? And in what case?
                            resource = (int)Resource.Type.Shovel + ((9 * Game.RandomInt()) >> 16);

                        SetState(State.MoveResourceOut);
                        s.MoveResourceOut.Resource = 1 + (uint)resource;
                        s.MoveResourceOut.ResourceDestination = 0;
                        s.MoveResourceOut.NextState = State.DropResourceOut;

                        // Update resource stats. 
                        player.IncreaseResourceCount((Resource.Type)resource);
                        player.NotifyCraftedTool((Resource.Type)resource);

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
            var map = Game.Map;
            var building = GetBuildingAtPosition();

            if (s.BuildingBoat.Mode == 0)
            {
                if (!building.UseResourceInStock(0))
                    return;

                building.BoatClear();

                s.BuildingBoat.Mode = 1;
                Animation = 146;
                Counter = CounterFromAnimation[Animation];
                state.Tick = Game.Tick;

                map.SetSerfIndex(Position, (int)Index);
            }
            else
            {
                ushort delta = (ushort)(Game.Tick - state.Tick);
                state.Tick = Game.Tick;
                Counter -= delta;

                while (Counter < 0)
                {
                    ++s.BuildingBoat.Mode;

                    if (s.BuildingBoat.Mode == 9)
                    {
                        // Boat done. 
                        var newPosition = map.MoveDownRight(Position);

                        if (map.HasSerf(newPosition))
                        {
                            // Wait for flag to be free. 
                            --s.BuildingBoat.Mode;
                            Counter = 0;
                        }
                        else
                        {
                            // Drop boat at flag. 
                            building.BoatClear();
                            map.SetSerfIndex(Position, 0);

                            SetState(State.MoveResourceOut);
                            s.MoveResourceOut.Resource = 1 + (uint)Resource.Type.Boat;
                            s.MoveResourceOut.ResourceDestination = 0;
                            s.MoveResourceOut.NextState = State.DropResourceOut;

                            // Update resource stats. 
                            var player = Game.GetPlayer(Player);
                            player.IncreaseResourceCount(Resource.Type.Boat);

                            break;
                        }
                    }
                    else
                    {
                        // Continue building. 
                        building.BoatDo();
                        Animation = 145;
                        Counter += 1408;
                    }
                }
            }
        }

        void HandleSerfLookingForGeoSpotState()
        {
            int numRemainingTries = 2;
            var map = Game.Map;

            for (int i = 0; i < 8; ++i)
            {
                int distance = ((Game.RandomInt() >> 2) & 0x3f) + 1;
                var destination = map.PositionAddSpirally(Position, (uint)distance);
                var obj = map.GetObject(destination);

                if (obj == Map.Object.None)
                {
                    var terrain1 = map.TypeDown(destination);
                    var terrain2 = map.TypeUp(destination);
                    var terrain3 = map.TypeDown(map.MoveUpLeft(destination));
                    var terrain4 = map.TypeUp(map.MoveUpLeft(destination));

                    if ((terrain1 >= Map.Terrain.Tundra0 && terrain1 <= Map.Terrain.Snow0) ||
                        (terrain2 >= Map.Terrain.Tundra0 && terrain2 <= Map.Terrain.Snow0) ||
                        (terrain3 >= Map.Terrain.Tundra0 && terrain3 <= Map.Terrain.Snow0) ||
                        (terrain4 >= Map.Terrain.Tundra0 && terrain4 <= Map.Terrain.Snow0))
                    {
                        SetState(State.FreeWalking);
                        s.FreeWalking.DistanceX = Map.GetSpiralPattern()[2 * distance];
                        s.FreeWalking.DistanceY = Map.GetSpiralPattern()[2 * distance + 1];
                        s.FreeWalking.NegDistance1 = -Map.GetSpiralPattern()[2 * distance];
                        s.FreeWalking.NegDistance2 = -Map.GetSpiralPattern()[2 * distance + 1];
                        s.FreeWalking.Flags = 0;
                        state.Tick = Game.Tick;
                        Log.Verbose.Write(ErrorSystemType.Serf, $"looking for geo spot: found, dist {s.FreeWalking.DistanceX}, {s.FreeWalking.DistanceY}.");

                        return;
                    }
                }
                else if (obj >= Map.Object.SignLargeGold &&
                         obj <= Map.Object.SignEmpty)
                {
                    if (--numRemainingTries == 0)
                        break;
                }
            }

            SetState(State.Walking);
            s.Walking.Destination = 0;
            s.Walking.Direction1 = -2;
            s.Walking.Direction = 0;
            s.Walking.WaitCounter = 0;
            Counter = 0;
        }

        void HandleSerfSamplingGeoSpotState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            var map = Game.Map;

            while (Counter < 0)
            {
                if (s.FreeWalking.NegDistance1 == 0 && map.GetObject(Position) == Map.Object.None)
                {
                    if (map.GetResourceType(Position) == Map.Minerals.None ||
                        map.GetResourceAmount(Position) == 0)
                    {
                        // No available resource here. Put empty sign. 
                        map.SetObject(Position, Map.Object.SignEmpty, -1);
                    }
                    else
                    {
                        s.FreeWalking.NegDistance1 = -1;
                        Animation = 142;

                        // Select small or large sign with the right resource depicted. 
                        var obj = Map.Object.SignLargeGold +
                            2 * ((int)map.GetResourceType(Position) - 1) +
                            (map.GetResourceAmount(Position) < 12 ? 1 : 0);
                        map.SetObject(Position, obj, -1);

                        // Check whether a new notification should be posted. 
                        bool showNotification = true;

                        for (uint i = 0; i < 60; ++i)
                        {
                            var position = map.PositionAddSpirally(Position, 1u + i);

                            if (((int)map.GetObject(position) >> 1) == ((int)obj >> 1))
                            {
                                showNotification = false;
                                break;
                            }
                        }

                        // Create notification for found resource. 
                        if (showNotification)
                        {
                            var notificationType = Notification.Type.None;

                            switch (map.GetResourceType(Position))
                            {
                                case Map.Minerals.Coal:
                                    notificationType = Notification.Type.FoundCoal;
                                    break;
                                case Map.Minerals.Iron:
                                    notificationType = Notification.Type.FoundIron;
                                    break;
                                case Map.Minerals.Gold:
                                    notificationType = Notification.Type.FoundGold;
                                    break;
                                case Map.Minerals.Stone:
                                    notificationType = Notification.Type.FoundStone;
                                    break;
                                default:
                                    Debug.NotReached();
                                    break;
                            }

                            Game.GetPlayer(Player).AddNotification(notificationType, Position, (uint)map.GetResourceType(Position) - 1);
                        }

                        Counter += 64;
                        continue;
                    }
                }

                SetState(State.FreeWalking);
                s.FreeWalking.NegDistance1 = -128;
                s.FreeWalking.NegDistance2 = 0;
                s.FreeWalking.Flags = 0;
                Counter = 0;
            }
        }

        void HandleSerfKnightEngagingBuildingState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                var map = Game.Map;

                if (map.HasBuilding(map.MoveUpLeft(Position)))
                {
                    var building = Game.GetBuildingAtPosition(map.MoveUpLeft(Position));

                    if (building.IsDone &&
                        building.IsMilitary() &&
                        building.Player != Player &&
                        building.HasKnight())
                    {
                        if (building.IsUnderAttack)
                        {
                            var player = Game.GetPlayer(building.Player);

                            var lastNotificationTime = player.GetMostRecentUnderAttackNotificationTime(building.Position);

                            if (Game.GameTime - lastNotificationTime > 45)
                                player.AddNotification(Notification.Type.UnderAttack, building.Position, Player);
                            else
                                player.ResetUnderAttackNotificationTime(building.Position);
                        }

                        // Change state of attacking knight 
                        Counter = 0;
                        SetState(State.KnightPrepareAttacking);
                        Animation = 168;

                        var defendingSerf = building.CallDefenderOut();

                        s.Attacking.DefenderIndex = (int)defendingSerf.Index;

                        // Change state of defending knight 
                        SetOtherState(defendingSerf, State.KnightLeaveForFight);
                        defendingSerf.s.LeavingBuilding.NextState = State.KnightPrepareDefending;
                        defendingSerf.Counter = 0;
                        return;
                    }
                }

                // No one to defend this building. Occupy it. 
                SetState(State.KnightOccupyEnemyBuilding);
                Animation = 179;
                Counter = CounterFromAnimation[Animation];
                state.Tick = Game.Tick;
            }
        }

        void HandleSerfKnightPrepareAttacking()
        {
            var defendingSerf = Game.GetSerf((uint)s.Attacking.DefenderIndex);

            if (defendingSerf.SerfState == State.KnightPrepareDefending)
            {
                // Change state of attacker. 
                SetState(State.KnightAttacking);
                Counter = 0;
                state.Tick = Game.Tick;

                // Change state of defender. 
                SetOtherState(defendingSerf, State.KnightDefending);
                defendingSerf.Counter = 0;

                SetFightOutcome(this, defendingSerf);
            }
        }

        void HandleSerfKnightLeaveForFightState()
        {
            state.Tick = Game.Tick;
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
            var defendingSerf = Game.GetSerf((uint)s.Attacking.DefenderIndex);
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            defendingSerf.state.Tick = state.Tick;
            Counter -= delta;
            defendingSerf.Counter = Counter;

            while (Counter < 0)
            {
                int move = KnightAttackMoves[s.Attacking.Move];

                if (move < 0)
                {
                    if (s.Attacking.AttackerWon == 0)
                    {
                        // Defender won. 
                        if (SerfState == State.KnightAttackingFree)
                        {
                            SetOtherState(defendingSerf, State.KnightDefendingVictoryFree);

                            defendingSerf.Animation = 180;
                            defendingSerf.Counter = 0;

                            // Attacker dies. 
                            SetState(State.KnightAttackingDefeatFree);
                            Animation = 152 + (int)SerfType;
                            Counter = 255;
                            SerfType = Type.Dead;
                        }
                        else
                        {
                            // Defender returns to building. 
                            defendingSerf.EnterBuilding(-1, true);

                            // Attacker dies. 
                            SetState(State.KnightAttackingDefeat);
                            Animation = 152 + (int)SerfType;
                            Counter = 255;
                            SerfType = Type.Dead;
                        }
                    }
                    else
                    {
                        // Attacker won
                        if (SerfState == State.KnightAttackingFree)
                        {
                            SetState(State.KnightAttackingVictoryFree);
                            Animation = 168;
                            Counter = 0;

                            s.AttackingVictoryFree.DefenderIndex = (int)defendingSerf.Index;
                            s.AttackingVictoryFree.Move = defendingSerf.s.DefendingFree.FieldD;
                            s.AttackingVictoryFree.DistanceColumn = defendingSerf.s.DefendingFree.OtherDistanceColumn;
                            s.AttackingVictoryFree.DistanceRow = defendingSerf.s.DefendingFree.OtherDistanceRow;
                        }
                        else
                        {
                            SetState(State.KnightAttackingVictory);
                            Animation = 168;
                            Counter = 0;

                            var building = Game.GetBuildingAtPosition(Game.Map.MoveUpLeft(defendingSerf.Position));
                            building.RequestedKnightDefeatOnWalk();
                        }

                        // Defender dies
                        defendingSerf.state.Tick = Game.Tick;
                        defendingSerf.Animation = 147 + (int)SerfType;
                        defendingSerf.Counter = 255;
                        defendingSerf.SerfType = Type.Dead;
                    }
                }
                else
                {
                    // Go to next move in fight sequence. 
                    ++s.Attacking.Move;

                    if (s.Attacking.AttackerWon == 0)
                        move = 4 - move;

                    s.Attacking.FieldD = move;

                    int animationOffset = (Game.RandomInt() * KnightFightAnimMax[move]) >> 16;
                    int knightAnimation = KnightFightAnim[move * 16 + animationOffset];

                    Animation = 146 + ((knightAnimation >> 4) & 0xf);
                    defendingSerf.Animation = 156 + (knightAnimation & 0xf);
                    Counter = 72 + (Game.RandomInt() & 0x18);
                    defendingSerf.Counter = Counter;
                }
            }
        }

        void HandleSerfKnightAttackingVictoryState()
        {
            var defendingSerf = Game.GetSerf((uint)s.Attacking.DefenderIndex);
            ushort delta = (ushort)(Game.Tick - defendingSerf.state.Tick);
            defendingSerf.state.Tick = Game.Tick;
            defendingSerf.Counter -= delta;

            if (defendingSerf.Counter < 0)
            {
                Game.DeleteSerf(defendingSerf);
                s.Attacking.DefenderIndex = 0;

                SetState(State.KnightEngagingBuilding);
                state.Tick = Game.Tick;
                Counter = 0;
            }
        }

        void HandleSerfKnightAttackingDefeatState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                Game.Map.SetSerfIndex(Position, 0);
                Game.DeleteSerf(this);
            }
        }

        void HandleKnightOccupyEnemyBuilding()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            if (Counter >= 0)
            {
                return;
            }

            var building = Game.GetBuildingAtPosition(Game.Map.MoveUpLeft(Position));

            if (building != null)
            {
                if (!building.IsBurning && building.IsMilitary())
                {
                    if (building.Player == Player)
                    {
                        // Enter building if there is space. 
                        if (building.BuildingType == Building.Type.Castle)
                        {
                            EnterBuilding(-2, false);
                            return;
                        }
                        else
                        {
                            if (building.IsEnoughPlaceForKnight)
                            {
                                // Enter building 
                                EnterBuilding(-1, false);
                                building.KnightOccupy();
                                return;
                            }
                        }
                    }
                    else if (!building.HasKnight())
                    {
                        // Occupy the building. 
                        Game.OccupyEnemyBuilding(building, Player);

                        if (building.BuildingType == Building.Type.Castle)
                        {
                            Counter = 0;
                        }
                        else
                        {
                            // Enter building 
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

            // Something is wrong. 
            SetState(State.Lost);
            s.Lost.FieldB = 0;
            Counter = 0;
        }

        void HandleStateKnightFreeWalking()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            var map = Game.Map;

            while (Counter < 0)
            {
                // Check for enemy knights nearby. 
                var cycle = DirectionCycleCW.CreateDefault();

                foreach (var direction in cycle)
                {
                    var position = map.Move(Position, direction);

                    if (map.HasSerf(position))
                    {
                        var otherSerf = Game.GetSerfAtPosition(position);

                        if (Player != otherSerf.Player)
                        {
                            if (otherSerf.SerfState == State.KnightFreeWalking)
                            {
                                position = map.MoveLeft(position);

                                if (CanPassMapPosition(position))
                                {
                                    int distanceColumn = s.FreeWalking.DistanceX;
                                    int distanceRow = s.FreeWalking.DistanceY;

                                    SetState(State.KnightEngageDefendingFree);

                                    s.DefendingFree.DistanceColumn = distanceColumn;
                                    s.DefendingFree.DistanceRow = distanceRow;
                                    s.DefendingFree.OtherDistanceColumn = otherSerf.s.FreeWalking.DistanceX;
                                    s.DefendingFree.OtherDistanceRow = otherSerf.s.FreeWalking.DistanceY;
                                    s.DefendingFree.FieldD = 1;
                                    Animation = 99;
                                    Counter = 255;

                                    SetOtherState(otherSerf, State.KnightEngageAttackingFree);
                                    otherSerf.s.Attacking.FieldD = (int)direction;
                                    otherSerf.s.Attacking.DefenderIndex = (int)Index;
                                    return;
                                }
                            }
                            else if (otherSerf.SerfState == State.Walking && otherSerf.IsKnight)
                            {
                                position = map.MoveLeft(position);

                                if (CanPassMapPosition(position))
                                {
                                    int distanceColumn = s.FreeWalking.DistanceX;
                                    int distanceRow = s.FreeWalking.DistanceY;

                                    SetState(State.KnightEngageDefendingFree);
                                    s.DefendingFree.DistanceColumn = distanceColumn;
                                    s.DefendingFree.DistanceRow = distanceRow;
                                    s.DefendingFree.FieldD = 0;
                                    Animation = 99;
                                    Counter = 255;

                                    var destination = Game.GetFlag(otherSerf.s.Walking.Destination);
                                    var building = destination.Building;

                                    if (building != null && !building.HasInventory())
                                    {
                                        building.RequestedKnightAttackingOnWalk();
                                    }

                                    SetOtherState(otherSerf, State.KnightEngageAttackingFree);
                                    otherSerf.s.Attacking.FieldD = (int)direction;
                                    otherSerf.s.Attacking.DefenderIndex = (int)Index;

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
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            while (Counter < 0) Counter += 256;
        }

        void HandleStateKnightEngageAttackingFree()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
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
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                SetState(State.KnightPrepareAttackingFree);
                Animation = 168;
                Counter = 0;

                var other = Game.GetSerf((uint)s.Attacking.DefenderIndex);
                var otherPosition = other.Position;
                SetOtherState(other, State.KnightPrepareDefendingFree);
                other.Counter = Counter;

                // Adjust distance to final destination. 
                var direction = (Direction)s.Attacking.FieldD;

                if (direction == Direction.Right || direction == Direction.DownRight)
                {
                    --other.s.DefendingFree.DistanceColumn;
                }
                else if (direction == Direction.Left || direction == Direction.UpLeft)
                {
                    ++other.s.DefendingFree.DistanceColumn;
                }

                if (direction == Direction.DownRight || direction == Direction.Down)
                {
                    --other.s.DefendingFree.DistanceRow;
                }
                else if (direction == Direction.UpLeft || direction == Direction.Up)
                {
                    ++other.s.DefendingFree.DistanceRow;
                }

                other.StartWalking(direction, 32, false);
                Game.Map.SetSerfIndex(otherPosition, 0);
            }
        }

        void HandleStateKnightPrepareAttackingFree()
        {
            var other = Game.GetSerf((uint)s.Attacking.DefenderIndex);

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
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                SetState(State.KnightPrepareDefendingFreeWait);
                Counter = 0;
            }
        }

        void HandleKnightAttackingVictoryFree()
        {
            var other = Game.GetSerf((uint)s.AttackingVictoryFree.DefenderIndex);
            ushort delta = (ushort)(Game.Tick - other.state.Tick);
            other.state.Tick = Game.Tick;
            other.Counter -= delta;

            if (other.Counter < 0)
            {
                Game.DeleteSerf(other);

                int distanceColumn = s.AttackingVictoryFree.DistanceColumn;
                int distanceRow = s.AttackingVictoryFree.DistanceRow;

                SetState(State.KnightAttackingFreeWait);

                s.FreeWalking.DistanceX = distanceColumn;
                s.FreeWalking.DistanceY = distanceRow;
                s.FreeWalking.NegDistance1 = 0;
                s.FreeWalking.NegDistance2 = 0;

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
                state.Tick = Game.Tick;
            }
        }

        void HandleKnightDefendingVictoryFree()
        {
            Animation = 180;
            Counter = 0;
        }

        void HandleSerfKnightAttackingDefeatFreeState()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
            Counter -= delta;

            if (Counter < 0)
            {
                // Change state of other. 
                var other = Game.GetSerf((uint)s.Attacking.DefenderIndex);
                int distanceColumn = other.s.DefendingFree.DistanceColumn;
                int distanceRow = other.s.DefendingFree.DistanceRow;

                SetOtherState(other, State.KnightFreeWalking);

                other.s.FreeWalking.DistanceX = distanceColumn;
                other.s.FreeWalking.DistanceY = distanceRow;
                other.s.FreeWalking.NegDistance1 = 0;
                other.s.FreeWalking.NegDistance2 = 0;
                other.s.FreeWalking.Flags = 0;

                other.Animation = 179;
                other.Counter = 0;
                other.state.Tick = Game.Tick;

                // Remove itself. 
                Game.Map.SetSerfIndex(Position, (int)other.Index);
                Game.DeleteSerf(this);
            }
        }

        void HandleKnightAttackingFreeWait()
        {
            ushort delta = (ushort)(Game.Tick - state.Tick);
            state.Tick = Game.Tick;
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
            state.Tick = Game.Tick;
            Counter = 0;

            var map = Game.Map;

            if (map.HasSerf(Position) && map.GetSerfIndex(Position) != Index)
            {
                Animation = 82;
                Counter = 0;
                return;
            }

            var building = GetBuildingAtPosition();
            var newPosition = map.MoveDownRight(Position);

            if (!map.HasSerf(newPosition))
            {
                // For clean state change, save the values first. 
                // TODO maybe KnightLeaveForWalkToFight can
                // share LeavingBuilding state vars.
                int distanceColumn = s.LeaveForWalkToFight.DistanceColumn;
                int distanceRow = s.LeaveForWalkToFight.DistanceRow;
                int fieldD = s.LeaveForWalkToFight.FieldD;
                int fieldE = s.LeaveForWalkToFight.FieldE;
                var nextState = s.LeaveForWalkToFight.NextState;

                LeaveBuilding(false);
                // TODO names for LeavingBuilding vars make no sense here. 
                s.LeavingBuilding.FieldB = distanceColumn;
                s.LeavingBuilding.Destination = (uint)distanceRow;
                s.LeavingBuilding.Destination2 = fieldD;
                s.LeavingBuilding.Direction = fieldE;
                s.LeavingBuilding.NextState = nextState;
            }
            else
            {
                var other = Game.GetSerfAtPosition(newPosition);

                if (Player == other.Player)
                {
                    Animation = 82;
                    Counter = 0;
                }
                else
                {
                    // Go back to defending the building. 
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
            var map = Game.Map;

            if (s.IdleOnPath.FlagIndex == 0)
            {
                // this should not happen, but if it happens we should fix it
                var cycle = DirectionCycleCW.CreateDefault();
                var firstDirection = Direction.None;

                foreach (Direction direction in cycle)
                {
                    if (map.HasPath(Position, direction))
                    {
                        firstDirection = direction;
                        break;
                    }
                }

                if (firstDirection == Direction.None)
                {
                    // something went wrong really bad, we can't fix it unfortunately
                    throw new ExceptionFreeserf(ErrorSystemType.Serf, "Corrupt serf state was found");
                }

                var firstFlag = Game.TracePathAndGetFlagAtEnd(Position, firstDirection, out Direction firstFlagReverseDirection);

                if (firstFlag == null || firstFlag.Index == 0)
                {
                    // something went wrong really bad, we can't fix it unfortunately
                    throw new ExceptionFreeserf(ErrorSystemType.Serf, "Corrupt serf state was found");
                }

                s.IdleOnPath.FlagIndex = firstFlag.Index;
                s.IdleOnPath.ReverseDirection = firstFlagReverseDirection;
            }

            var flag = Game.GetFlag(s.IdleOnPath.FlagIndex);
            var reverseDirection = s.IdleOnPath.ReverseDirection;

            if (flag == null) // flag has gone
            {
                SetLostState();
                return;
            }
            else if (flag.IsScheduled(reverseDirection))
            {
                // Set walking direction in fieldE.
                s.IdleOnPath.FieldE = (state.Tick & 0xff) + 6;
            }
            else
            {
                var otherFlag = flag.GetOtherEndFlag(reverseDirection);
                var otherDirection = flag.GetOtherEndDirection(reverseDirection);

                if (otherFlag != null && otherFlag.IsScheduled(otherDirection))
                {
                    s.IdleOnPath.FieldE = (int)reverseDirection.Reverse();
                }
                else
                {
                    return;
                }
            }

            if (!map.HasSerf(Position))
            {
                // No blocking serf -> start transporting the scheduled resource
                StartTransporting(map);
            }
            else
            {
                // Blocking serf -> wait till the serf is gone
                SetState(State.WaitIdleOnPath);
            }
        }

        void HandleSerfWaitIdleOnPathState()
        {
            var map = Game.Map;

            if (!map.HasSerf(Position))
            {
                // No blocking serf -> start transporting the scheduled resource
                StartTransporting(map);
            }
        }

        void StartTransporting(Map map)
        {
            map.ClearIdleSerf(Position);
            map.SetSerfIndex(Position, (int)Index);

            int direction = s.IdleOnPath.FieldE;

            SetState(State.Transporting);
            s.Walking.Resource = Resource.Type.None;
            s.Walking.WaitCounter = 0;
            s.Walking.Direction = direction;
            state.Tick = Game.Tick;
            Counter = 0;
        }

        void HandleScatterState()
        {
            // Choose a random, empty destination 
            while (true)
            {
                int random = Game.RandomInt();
                int column = (random & 0xf);

                if (column < 8)
                    column -= 16;

                int row = ((random >> 8) & 0xf);

                if (row < 8)
                    row -= 16;

                var map = Game.Map;
                var destination = map.PositionAdd(Position, column, row);

                if (map.GetObject(destination) == 0 && map.GetHeight(destination) > 0)
                {
                    if (IsKnight)
                    {
                        SetState(State.KnightFreeWalking);
                    }
                    else
                    {
                        SetState(State.FreeWalking);
                    }

                    s.FreeWalking.DistanceX = column;
                    s.FreeWalking.DistanceY = row;
                    s.FreeWalking.NegDistance1 = -128;
                    s.FreeWalking.NegDistance2 = -1;
                    s.FreeWalking.Flags = 0;
                    Counter = 0;

                    return;
                }
            }
        }

        void HandleSerfFinishedBuildingState()
        {
            var map = Game.Map;

            if (!map.HasSerf(map.MoveDownRight(Position)))
            {
                SetState(State.ReadyToLeave);
                s.LeavingBuilding.Destination = 0;
                s.LeavingBuilding.FieldB = -2;
                s.LeavingBuilding.Direction = 0;
                s.LeavingBuilding.NextState = State.Walking;

                if (map.GetSerfIndex(Position) != Index && map.HasSerf(Position))
                {
                    Animation = 82;
                }
            }
        }

        void HandleSerfWakeAtFlagState()
        {
            var map = Game.Map;

            if (!map.HasSerf(Position))
            {
                map.ClearIdleSerf(Position);
                map.SetSerfIndex(Position, (int)Index);
                state.Tick = Game.Tick;
                Counter = 0;

                if (SerfType == Type.Sailor)
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

            foreach (var direction in cycle)
            {
                if (Misc.BitTest(Game.Map.Paths(Position), (int)direction))
                {
                    s.IdleOnPath.FieldE = (int)direction;
                    break;
                }
            }
        }

        void HandleSerfDefendingState(int[] trainingParams)
        {
            switch (SerfType)
            {
                case Type.Knight0:
                case Type.Knight1:
                case Type.Knight2:
                case Type.Knight3:
                    TrainKnight(trainingParams[SerfType - Type.Knight0]);
                    break;
                case Type.Knight4: // Cannot train anymore. 
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
