/*
 * Player.cs - Player related functions
 *
 * Copyright (C) 2013-2017  Jon Lund Steffensen <jonlst@gmail.com>
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
using System.Linq;

namespace Freeserf
{
    using Serialize;
    using dword = UInt32;
    using GameTime = UInt32;
    using MapPos = UInt32;
    using Notifications = Queue<Notification>;
    using PositionTimers = List<PositionTimer>;
    using ResourceMap = Dictionary<Resource.Type, uint>;
    using SerfMap = Dictionary<Serf.Type, int>;
    using word = UInt16;

    public class Player : GameObject, IState
    {
        // We only serialize the settings and the player state
        [Data]
        private PlayerSettings settings = new PlayerSettings();
        [Data]
        private PlayerState state = new PlayerState();

        // Those are only saved locally
        ushort lastTick = 0;
        bool notificationFlag = false;
        readonly Notifications notifications = new Notifications();
        readonly PositionTimers timers = new PositionTimers();
        readonly Dictionary<MapPos, GameTime> lastUnderAttackNotificationTimes = new Dictionary<MapPos, GameTime>();
        uint selectedBuildingIndex = 0;
        int knightsToSpawn = 0;

        int sendGenericDelay = 0;
        int sendKnightDelay = 0;

        uint[,] playerStatHistory = new uint[16, 112];
        uint[,] resourceCountHistory = new uint[26, 120];

        /// <summary>
        /// Target building that should be attacked.
        /// Used in start attack gui window.
        /// </summary>
        public int BuildingToAttack { get; set; } = 0;
        /// <summary>
        /// The amount of knights that should be send to fight.
        /// Used in start attack gui window.
        /// </summary>
        public int TotalKnightsAttacking { get; set; } = 0;
        /// <summary>
        /// Number of buildings that provide knights for the attack.
        /// </summary>
        public int AttackingBuildingCount { get; set; } = 0;
        /// <summary>
        /// Indices of the attacking buildings.
        /// </summary>
        private uint[] attackingBuildings = new uint[64];
        /// <summary>
        /// Maximum knights per distance level that can be send to fight.
        /// Used in start attack gui window.
        /// </summary>
        public int[] MaxAttackingKnightsByDistance { get; set; } = new int[4];
        /// <summary>
        /// Maximum knights that can be send to fight.
        /// Used in start attack gui window.
        /// </summary>
        public int MaxAttackingKnights { get; set; } = 0;
        /// <summary>
        /// The currently selected building or flag.
        /// </summary>
        public uint SelectedObjectIndex { get; set; } = 0;

        public AI AI { get; set; } = null;

        public Player(Game game, uint index)
            : base(game, index)
        {
            ResetFoodPriority();
            ResetPlanksPriority();
            ResetSteelPriority();
            ResetCoalPriority();
            ResetWheatPriority();
            ResetToolPriority();

            ResetFlagPriority();
            ResetInventoryPriority();

            settings.KnightOccupation[0] = 0x10;
            settings.KnightOccupation[1] = 0x21;
            settings.KnightOccupation[2] = 0x32;
            settings.KnightOccupation[3] = 0x43;

            settings.SerfToKnightRate = 20000;
            state.SerfToKnightCounter = 0x8000; // Overflow is important 
        }

        // Used for multiplayer games to see if an update is necessary.
        public bool Dirty => settings.Dirty || state.Dirty;

        public bool EmergencyProgramActive
        {
            get => state.EmergencyProgramActive;
            private set
            {
                if (value == state.EmergencyProgramActive)
                    return;

                if (value && !state.IsAI && state.EmergencyProgramWasDeactivatedOnce)
                    return; // can't be reactivated for human players

                if (value)
                {
                    AddNotification(Notification.Type.EmergencyActive, state.CastlePosition, state.CastlePosition);
                }
                else
                {
                    AddNotification(Notification.Type.EmergencyNeutral, state.CastlePosition, state.CastlePosition);
                }

                state.EmergencyProgramActive = value;

                if (!value)
                    state.EmergencyProgramWasDeactivatedOnce = true;
            }
        }

        // Initialize player values.
        //
        // Supplies and reproduction are usually limited to 0-40 in random map games.
        public void Init(uint intelligence, uint supplies, uint reproduction)
        {
            settings.Flags = PlayerSettingFlags.None;
            state.Flags = PlayerStateFlags.None;

            state.InitialSupplies = (byte)supplies;
            state.ReproductionReset = (word)((60 - reproduction) * 50);
            state.Intelligence = (byte)intelligence;
            state.ReproductionCounter = state.ReproductionReset;

            if (!Face.IsHuman())
                state.IsAI = true;
        }

        public void ResetDirtyFlag()
        {
            settings.ResetDirtyFlag();
            state.ResetDirtyFlag();
        }

        public PlayerInfo GetPlayerInfo()
        {
            uint reproduction = (dword)(60 - state.ReproductionReset / 50);

            var info = new PlayerInfo(state.Face, state.Color, state.Intelligence, state.InitialSupplies, reproduction);

            if (state.HasCastle)
            {
                info.CastlePosition = new PlayerInfo.Position(
                    (int)Game.Map.PositionColumn(state.CastlePosition),
                    (int)Game.Map.PositionRow(state.CastlePosition)
                );
            }

            return info;
        }

        public void InitView(Color color, PlayerFace face)
        {
            state.Face = face;
            state.Color = color;
            state.IsAI = !face.IsHuman();
        }

        public Color Color => state.Color;
        public PlayerFace Face => state.Face;
        /// <summary>
        /// Whether player has built the initial castle.
        /// </summary>
        public bool HasCastle => state.HasCastle;
        public MapPos CastlePosition
        {
            get => state.CastlePosition;
            internal set => state.CastlePosition = value;
        }
        public bool CanSpawn => state.CanSpawn;
        /// <summary>
        /// Whether the strongest knight should be sent to fight.
        /// </summary>
        public bool SendStrongest
        {
            get => settings.SendStrongest;
            set => settings.SendStrongest = value;
        }
        /// <summary>
        /// Whether cycling of knights is in progress.
        /// </summary>
        public bool CyclingKnights => state.CyclingKnightsInProgress;
        /// <summary>
        /// Whether a notification is queued for this player.
        /// </summary>
        public bool HasNotifications => notificationFlag;
        public void DropNotifications()
        {
            notificationFlag = false;
        }
        /// <summary>
        /// Whether the knight level of military buildings is temporarily
        /// reduced bacause of cycling of the knights.
        /// </summary>
        public bool ReducedKnightLevel => state.CyclingKnightsReducedLevel;
        /// <summary>
        /// Whether the cycling of knights is in the second phase.
        /// </summary>
        /// <returns></returns>
        public bool CyclingKnightsInSecondPhase => state.CyclingKnightsSecondPhase;
        /// <summary>
        /// Whether this player is a computer controlled opponent.
        /// </summary>
        public bool IsAI => state.IsAI;

        public uint GetSerfCount(Serf.Type type)
        {
            return state.SerfCounts[(int)type];
        }

        public uint TotalKnightCount =>
            GetSerfCount(Serf.Type.Knight0) +
            GetSerfCount(Serf.Type.Knight1) +
            GetSerfCount(Serf.Type.Knight2) +
            GetSerfCount(Serf.Type.Knight3) +
            GetSerfCount(Serf.Type.Knight4);

        public int GetFlagPriority(Resource.Type resource)
        {
            if (resource <= Resource.Type.None || resource >= Resource.Type.GroupFood)
                return 0;

            return settings.FlagPriorities[(int)resource];
        }

        // Enqueue a new notification message for player.
        public void AddNotification(Notification.Type type, MapPos position, uint data)
        {
            notificationFlag = true;

            var notification = new Notification();
            notification.NotificationType = type;
            notification.Position = position;
            notification.Data = data;

            if (type == Notification.Type.UnderAttack)
                lastUnderAttackNotificationTimes[position] = Game.GameTime;

            notifications.Enqueue(notification);
        }

        public bool HasAnyNotification => notifications.Count > 0;

        public bool HasNotificationOfType(Notification.Type type)
        {
            return notifications.Any(m => m.NotificationType == type);
        }

        public void ResetUnderAttackNotificationTime(MapPos position)
        {
            lastUnderAttackNotificationTimes[position] = Game.GameTime;
        }

        public GameTime GetMostRecentUnderAttackNotificationTime(uint position = Global.INVALID_MAPPOS)
        {
            return lastUnderAttackNotificationTimes.ContainsKey(position)
                ? lastUnderAttackNotificationTimes[position]
                : 0;
        }

        public Notification PopNotification()
        {
            return notifications.Dequeue();
        }

        public Notification PeekNotification()
        {
            return notifications.Peek();
        }

        public void AddPositionTimer(int timeout, MapPos position)
        {
            var timer = new PositionTimer();

            timer.Timeout = timeout;
            timer.Position = position;

            timers.Add(timer);
        }

        /// <summary>
        /// Set defaults for food distribution priorities.
        /// </summary>
        public void ResetFoodPriority()
        {
            settings.FoodStonemine = 13100;
            settings.FoodCoalmine = 45850;
            settings.FoodIronmine = 45850;
            settings.FoodGoldmine = 65500;
        }

        /// <summary>
        /// Set defaults for planks distribution priorities.
        /// </summary>
        public void ResetPlanksPriority()
        {
            settings.PlanksConstruction = 65500;
            settings.PlanksBoatbuilder = 3275;
            settings.PlanksToolmaker = 19650;
        }

        /// <summary>
        /// Set defaults for steel distribution priorities.
        /// </summary>
        public void ResetSteelPriority()
        {
            settings.SteelToolmaker = 45850;
            settings.SteelWeaponsmith = 65500;
        }

        /// <summary>
        /// Set defaults for coal distribution priorities.
        /// </summary>
        public void ResetCoalPriority()
        {
            settings.CoalSteelsmelter = 32750;
            settings.CoalGoldsmelter = 65500;
            settings.CoalWeaponsmith = 52400;
        }

        /// <summary>
        /// Set defaults for wheat distribution priorities.
        /// </summary>
        public void ResetWheatPriority()
        {
            settings.WheatPigfarm = 65500;
            settings.WheatMill = 32750;
        }

        /// <summary>
        /// Set defaults for tool production priorities.
        /// </summary>
        public void ResetToolPriority()
        {
            settings.ToolPriorities[0] = 9825;  // SHOVEL
            settings.ToolPriorities[1] = 65500; // HAMMER
            settings.ToolPriorities[2] = 13100; // ROD
            settings.ToolPriorities[3] = 6550;  // CLEAVER
            settings.ToolPriorities[4] = 13100; // SCYTHE
            settings.ToolPriorities[5] = 26200; // AXE
            settings.ToolPriorities[6] = 32750; // SAW
            settings.ToolPriorities[7] = 45850; // PICK
            settings.ToolPriorities[8] = 6550;  // PINCER
        }

        /// <summary>
        /// Set defaults for flag priorities.
        /// </summary>
        public void ResetFlagPriority()
        {
            settings.FlagPriorities[(int)Resource.Type.GoldOre] = 1;
            settings.FlagPriorities[(int)Resource.Type.GoldBar] = 2;
            settings.FlagPriorities[(int)Resource.Type.Wheat] = 3;
            settings.FlagPriorities[(int)Resource.Type.Flour] = 4;
            settings.FlagPriorities[(int)Resource.Type.Pig] = 5;

            settings.FlagPriorities[(int)Resource.Type.Boat] = 6;
            settings.FlagPriorities[(int)Resource.Type.Pincer] = 7;
            settings.FlagPriorities[(int)Resource.Type.Scythe] = 8;
            settings.FlagPriorities[(int)Resource.Type.Rod] = 9;
            settings.FlagPriorities[(int)Resource.Type.Cleaver] = 10;

            settings.FlagPriorities[(int)Resource.Type.Saw] = 11;
            settings.FlagPriorities[(int)Resource.Type.Axe] = 12;
            settings.FlagPriorities[(int)Resource.Type.Pick] = 13;
            settings.FlagPriorities[(int)Resource.Type.Shovel] = 14;
            settings.FlagPriorities[(int)Resource.Type.Hammer] = 15;

            settings.FlagPriorities[(int)Resource.Type.Shield] = 16;
            settings.FlagPriorities[(int)Resource.Type.Sword] = 17;
            settings.FlagPriorities[(int)Resource.Type.Bread] = 18;
            settings.FlagPriorities[(int)Resource.Type.Meat] = 19;
            settings.FlagPriorities[(int)Resource.Type.Fish] = 20;

            settings.FlagPriorities[(int)Resource.Type.IronOre] = 21;
            settings.FlagPriorities[(int)Resource.Type.Lumber] = 22;
            settings.FlagPriorities[(int)Resource.Type.Coal] = 23;
            settings.FlagPriorities[(int)Resource.Type.Steel] = 24;
            settings.FlagPriorities[(int)Resource.Type.Stone] = 25;
            settings.FlagPriorities[(int)Resource.Type.Plank] = 26;
        }

        /// <summary>
        /// Set defaults for inventory priorities.
        /// </summary>
        public void ResetInventoryPriority()
        {
            settings.InventoryPriorities[(int)Resource.Type.Wheat] = 1;
            settings.InventoryPriorities[(int)Resource.Type.Flour] = 2;
            settings.InventoryPriorities[(int)Resource.Type.Pig] = 3;
            settings.InventoryPriorities[(int)Resource.Type.Bread] = 4;
            settings.InventoryPriorities[(int)Resource.Type.Fish] = 5;

            settings.InventoryPriorities[(int)Resource.Type.Meat] = 6;
            settings.InventoryPriorities[(int)Resource.Type.Lumber] = 7;
            settings.InventoryPriorities[(int)Resource.Type.Plank] = 8;
            settings.InventoryPriorities[(int)Resource.Type.Boat] = 9;
            settings.InventoryPriorities[(int)Resource.Type.Stone] = 10;

            settings.InventoryPriorities[(int)Resource.Type.Coal] = 11;
            settings.InventoryPriorities[(int)Resource.Type.IronOre] = 12;
            settings.InventoryPriorities[(int)Resource.Type.Steel] = 13;
            settings.InventoryPriorities[(int)Resource.Type.Shovel] = 14;
            settings.InventoryPriorities[(int)Resource.Type.Hammer] = 15;

            settings.InventoryPriorities[(int)Resource.Type.Rod] = 16;
            settings.InventoryPriorities[(int)Resource.Type.Cleaver] = 17;
            settings.InventoryPriorities[(int)Resource.Type.Scythe] = 18;
            settings.InventoryPriorities[(int)Resource.Type.Axe] = 19;
            settings.InventoryPriorities[(int)Resource.Type.Saw] = 20;

            settings.InventoryPriorities[(int)Resource.Type.Pick] = 21;
            settings.InventoryPriorities[(int)Resource.Type.Pincer] = 22;
            settings.InventoryPriorities[(int)Resource.Type.Shield] = 23;
            settings.InventoryPriorities[(int)Resource.Type.Sword] = 24;
            settings.InventoryPriorities[(int)Resource.Type.GoldOre] = 25;
            settings.InventoryPriorities[(int)Resource.Type.GoldBar] = 26;
        }

        public uint GetKnightOccupation(int threatLevel)
        {
            return settings.KnightOccupation[threatLevel];
        }

        public void ChangeKnightOccupation(int index, bool adjustMax, int delta)
        {
            int max = (settings.KnightOccupation[index] >> 4) & 0xf;
            int min = settings.KnightOccupation[index] & 0xf;

            if (adjustMax)
            {
                max = Misc.Clamp(min, max + delta, 4);
            }
            else
            {
                min = Misc.Clamp(0, min + delta, max);
            }

            settings.KnightOccupation[index] = (byte)((max << 4) | min);
        }

        public void SetLowKnightOccupation()
        {
            settings.KnightOccupation[0] = 0x00;
            settings.KnightOccupation[1] = 0x00;
            settings.KnightOccupation[2] = 0x00;
            settings.KnightOccupation[3] = 0x00;
        }

        public void SetMediumKnightOccupation(bool offensive)
        {
            byte value = offensive ? (byte)0x20u : (byte)0x21u;

            settings.KnightOccupation[0] = 0x00;
            settings.KnightOccupation[1] = 0x00;
            settings.KnightOccupation[2] = value;
            settings.KnightOccupation[3] = value;
        }

        public void SetHighKnightOccupation(bool offensive)
        {
            byte value = offensive ? (byte)0x40u : (byte)0x42u;

            settings.KnightOccupation[0] = 0x20;
            settings.KnightOccupation[1] = 0x20;
            settings.KnightOccupation[2] = value;
            settings.KnightOccupation[3] = value;
        }

        public void IncreaseCastleKnights()
        {
            ++state.CastleKnights;
        }

        public void DecreaseCastleKnights()
        {
            --state.CastleKnights;
        }

        public uint CastleKnights => state.CastleKnights;

        public uint CastleKnightsWanted => settings.CastleKnightsWanted;

        public void SetCastleKnightsWanted(uint amount)
        {
            settings.CastleKnightsWanted = (byte)Misc.Clamp(1, amount, 99);
        }

        public void IncreaseCastleKnightsWanted()
        {
            settings.CastleKnightsWanted = (byte)Math.Min(settings.CastleKnightsWanted + 1, 99);
        }

        public void DecreaseCastleKnightsWanted()
        {
            settings.CastleKnightsWanted = (byte)Math.Max(1, settings.CastleKnightsWanted - 1);
        }

        public uint KnightMorale => state.KnightMorale;

        public uint GoldDeposited => state.GoldDeposited;

        // Turn a number of serfs into knight for the given player. 
        public int PromoteSerfsToKnights(int number)
        {
            if (number <= 0)
                return 0;

            int promoted = 0;

            foreach (var serf in Game.GetPlayerSerfs(this))
            {
                if (serf.SerfState == Serf.State.IdleInStock &&
                    serf.SerfType == Serf.Type.Generic)
                {
                    var inventory = Game.GetInventory(serf.IdleInStockInventoryIndex);

                    if (inventory.PromoteSerfToKnight(serf))
                    {
                        ++promoted;

                        if (--number == 0)
                            break;
                    }
                }
            }

            return promoted;
        }

        public int KnightsAvailableForAttack(MapPos position)
        {
            // Reset counters. 
            for (int i = 0; i < 4; ++i)
            {
                MaxAttackingKnightsByDistance[i] = 0;
            }

            int count = 0;
            var map = Game.Map;

            // Iterate each shell around the position.
            for (int i = 0; i < 32; ++i)
            {
                position = map.MoveRight(position);

                for (int j = 0; j < i + 1; ++j)
                {
                    count = AvailableKnightsAtPosition(position, count, i >> 3);
                    position = map.MoveDown(position);
                }

                for (int j = 0; j < i + 1; ++j)
                {
                    count = AvailableKnightsAtPosition(position, count, i >> 3);
                    position = map.MoveLeft(position);
                }
                for (int j = 0; j < i + 1; ++j)
                {
                    count = AvailableKnightsAtPosition(position, count, i >> 3);
                    position = map.MoveUpLeft(position);
                }

                for (int j = 0; j < i + 1; ++j)
                {
                    count = AvailableKnightsAtPosition(position, count, i >> 3);
                    position = map.MoveUp(position);
                }

                for (int j = 0; j < i + 1; ++j)
                {
                    count = AvailableKnightsAtPosition(position, count, i >> 3);
                    position = map.MoveRight(position);
                }

                for (int j = 0; j < i + 1; ++j)
                {
                    count = AvailableKnightsAtPosition(position, count, i >> 3);
                    position = map.MoveDownRight(position);
                }
            }

            AttackingBuildingCount = count;
            MaxAttackingKnights = 0;

            for (int i = 0; i < 4; ++i)
            {
                MaxAttackingKnights += MaxAttackingKnightsByDistance[i];
            }

            return MaxAttackingKnights;
        }

        public bool PrepareAttack(uint targetPosition, int maxKnights = -1)
        {
            var building = Game.GetBuildingAtPosition(targetPosition);

            BuildingToAttack = (int)building.Index;

            if (building.IsDone &&
                building.IsMilitary())
            {
                if (!building.IsActive ||
                    building.ThreatLevel != 3)
                {
                    // It is not allowed to attack
                    // if currently not occupied or
                    // is too far from the border.
                    return false;
                }

                bool found = false;
                var map = Game.Map;

                for (int i = 257; i >= 0; --i)
                {
                    var position = map.PositionAddSpirally(building.Position, (uint)(7 + 257 - i));

                    if (map.HasOwner(position) && map.GetOwner(position) == Index)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }

                if (maxKnights == -1)
                {
                    switch (building.BuildingType)
                    {
                        case Building.Type.Hut: maxKnights = 3; break;
                        case Building.Type.Tower: maxKnights = 6; break;
                        case Building.Type.Fortress: maxKnights = 12; break;
                        case Building.Type.Castle: maxKnights = 20; break;
                        default: Debug.NotReached(); break;
                    }
                }

                int knights = KnightsAvailableForAttack(building.Position);
                TotalKnightsAttacking = Math.Min(knights, maxKnights);

                return true;
            }

            return false;
        }

        public void StartAttack()
        {
            var target = Game.GetBuilding((uint)BuildingToAttack);

            if (!target.IsDone || !target.IsMilitary() ||
                !target.IsActive || target.ThreatLevel != 3)
            {
                return;
            }

            var map = Game.Map;

            for (int i = 0; i < AttackingBuildingCount; i++)
            {
                var building = Game.GetBuilding(attackingBuildings[i]);

                if (building == null || building.IsBurning || map.GetOwner(building.Position) != Index)
                {
                    continue;
                }

                var flagPosition = map.MoveDownRight(building.Position);

                if (map.HasSerf(flagPosition))
                {
                    // Check if building is under siege. 
                    var serf = Game.GetSerfAtPosition(flagPosition);

                    if (serf.Player != Index)
                        continue;
                }

                int[] minLevel;

                switch (building.BuildingType)
                {
                    case Building.Type.Hut: minLevel = minLevelHut; break;
                    case Building.Type.Tower: minLevel = minLevelTower; break;
                    case Building.Type.Fortress: minLevel = minLevelFortress; break;
                    default: continue;
                }

                uint state = building.ThreatLevel;
                uint knightsPresent = building.KnightCount;
                int toSend = (int)knightsPresent - minLevel[settings.KnightOccupation[state] & 0xf];

                for (int j = 0; j < toSend; ++j)
                {
                    // Find most appropriate knight to send according to player settings.
                    var bestType = SendStrongest ? Serf.Type.Knight0 : Serf.Type.Knight4;
                    var knightIndex = building.FirstKnight;
                    uint bestIndex = 0;

                    while (knightIndex != 0)
                    {
                        var knight = Game.GetSerf(knightIndex);

                        if (SendStrongest)
                        {
                            if (knight.SerfType >= bestType)
                            {
                                bestIndex = knightIndex;
                                bestType = knight.SerfType;
                            }
                        }
                        else
                        {
                            if (knight.SerfType <= bestType)
                            {
                                bestIndex = knightIndex;
                                bestType = knight.SerfType;
                            }
                        }

                        knightIndex = knight.NextKnight;
                    }

                    var defendingSerf = building.CallAttackerOut(bestIndex);

                    target.SetUnderAttack();

                    // Calculate distance to target. 
                    int distanceColumn = map.DistanceX(defendingSerf.Position, target.Position);
                    int distanceRow = map.DistanceY(defendingSerf.Position, target.Position);

                    // Send this serf off to fight. 
                    defendingSerf.SendOffToFight(distanceColumn, distanceRow);

                    if (--TotalKnightsAttacking == 0)
                        return;
                }
            }
        }

        /// <summary>
        /// Begin cycling knights by sending knights from military buildings
        /// to inventories.The knights can then be replaced by more experienced
        /// knights.
        /// </summary>
        public void CycleKnights()
        {
            state.CyclingKnightsInProgress = true;
            state.CyclingKnightsReducedLevel = true;
            state.KnightCycleCounter = 2400;
        }

        /// <summary>
        /// Create the initial serfs that occupies the castle.
        /// </summary>
        public void CreateInitialCastleSerfs(Building castle)
        {
            // Spawn castle transporter serf
            var inventory = castle.Inventory;
            var serf = inventory.SpawnSerfGeneric();

            if (serf == null)
            {
                return;
            }

            inventory.SpecializeSerf(serf, Serf.Type.TransporterInventory);
            serf.InitInventoryTransporter(inventory);

            Game.Map.SetSerfIndex(serf.Position, (int)serf.Index);

            var building = Game.GetBuilding((uint)this.selectedBuildingIndex);

            // Spawn 3 generic serfs
            for (int i = 0; i < 5; i++)
            {
                SpawnSerf(null, null, false);
            }

            // Spawn three knights
            for (int i = 0; i < 3; i++)
            {
                serf = inventory.SpawnSerfGeneric();

                if (serf == null)
                    return;

                if (inventory.PromoteSerfToKnight(serf) && building.FirstKnight == 0)
                    building.FirstKnight = serf.Index;
            }

            // Spawn toolmaker
            serf = inventory.SpawnSerfGeneric();

            if (serf == null)
                return;

            inventory.SpecializeSerf(serf, Serf.Type.Toolmaker);

            // Spawn timberman
            serf = inventory.SpawnSerfGeneric();

            if (serf == null)
                return;

            inventory.SpecializeSerf(serf, Serf.Type.Lumberjack);

            // Spawn sawmiller
            serf = inventory.SpawnSerfGeneric();

            if (serf == null)
                return;

            inventory.SpecializeSerf(serf, Serf.Type.Sawmiller);

            // Spawn stonecutter
            serf = inventory.SpawnSerfGeneric();

            if (serf == null)
                return;

            inventory.SpecializeSerf(serf, Serf.Type.Stonecutter);

            // Spawn digger
            serf = inventory.SpawnSerfGeneric();

            if (serf == null)
                return;

            inventory.SpecializeSerf(serf, Serf.Type.Digger);

            // Spawn builder
            serf = inventory.SpawnSerfGeneric();

            if (serf == null)
                return;

            inventory.SpecializeSerf(serf, Serf.Type.Builder);

            // Spawn fisherman
            serf = inventory.SpawnSerfGeneric();

            if (serf == null)
                return;

            inventory.SpecializeSerf(serf, Serf.Type.Fisher);

            // Spawn two geologists
            for (int i = 0; i < 2; i++)
            {
                serf = inventory.SpawnSerfGeneric();

                if (serf == null)
                    return;

                inventory.SpecializeSerf(serf, Serf.Type.Geologist);
            }

            // Spawn two miners
            for (int i = 0; i < 2; i++)
            {
                serf = inventory.SpawnSerfGeneric();

                if (serf == null)
                    return;

                inventory.SpecializeSerf(serf, Serf.Type.Miner);
            }
        }

        public Serf SpawnSerfGeneric()
        {
            var serf = Game.CreateSerf();

            if (serf == null)
                return null;

            serf.Player = Index;

            ++state.SerfCounts[(int)Serf.Type.Generic];

            return serf;
        }

        /// <summary>
        /// Spawn new serf.
        /// 
        /// The serf object and inventory are returned if non-null.
        /// </summary>
        /// <param name="serf"></param>
        /// <param name="inventory"></param>
        /// <param name="wantKnight"></param>
        /// <returns></returns>
        public bool SpawnSerf(Pointer<Serf> serf, Pointer<Inventory> inventory, bool wantKnight)
        {
            if (!CanSpawn)
                return false;

            var inventories = Game.GetPlayerInventories(this);

            if (inventories.Count() == 0)
            {
                return false;
            }

            Inventory spawnInventory = null;

            foreach (var loopInventory in inventories)
            {
                if (loopInventory.SerfMode == Inventory.Mode.In)
                {
                    if (wantKnight && (loopInventory.GetCountOf(Resource.Type.Sword) == 0 ||
                                       loopInventory.GetCountOf(Resource.Type.Shield) == 0))
                    {
                        continue;
                    }
                    else if (loopInventory.FreeSerfCount() == 0)
                    {
                        spawnInventory = loopInventory;
                        break;
                    }
                    else if (spawnInventory == null || loopInventory.FreeSerfCount() < spawnInventory.FreeSerfCount())
                    {
                        spawnInventory = loopInventory;
                    }
                }
            }

            if (spawnInventory == null)
            {
                if (wantKnight)
                {
                    return SpawnSerf(serf, inventory, false);
                }
                else
                {
                    return false;
                }
            }

            var spawnedSerf = spawnInventory.SpawnSerfGeneric();

            if (spawnedSerf == null)
            {
                return false;
            }

            if (serf != null)
                serf.Value = spawnedSerf;

            if (inventory != null)
                inventory.Value = spawnInventory;

            return true;
        }

        public bool TickSendGenericDelay()
        {
            --sendGenericDelay;

            if (sendGenericDelay < 0)
            {
                sendGenericDelay = 5;
                return true;
            }

            return false;
        }

        public bool TickSendKnightDelay()
        {
            --sendKnightDelay;

            if (sendKnightDelay < 0)
            {
                sendKnightDelay = 5;
                return true;
            }

            return false;
        }

        public int GetCyclingSerfType(Serf.Type type)
        {
            if (state.CyclingKnightsSecondPhase)
            {
                return -((state.KnightCycleCounter >> 8) + 1);
            }

            return (int)type;
        }

        public void IncreaseSerfCount(Serf.Type type)
        {
            if (type == Serf.Type.None || type == Serf.Type.Dead)
                return;

            ++state.SerfCounts[(int)type];
        }

        public void DecreaseSerfCount(Serf.Type type)
        {
            if (type == Serf.Type.None || type == Serf.Type.Dead)
                return;

            if (state.SerfCounts[(int)type] == 0)
            {
                throw new ExceptionFreeserf(Game, ErrorSystemType.Player, "Failed to decrease serf count");
            }

            --state.SerfCounts[(int)type];
        }

        public DirtyArray<uint> GetSerfCounts()
        {
            return state.SerfCounts;
        }

        public void NotifyCraftedTool(Resource.Type tool)
        {
            if (AI != null)
                AI.NotifyCraftedTool(tool);
        }

        public void IncreaseResourceCount(Resource.Type type)
        {
            ++state.ResourceCounts[(int)type];
        }

        public void DecreaseResourceCount(Resource.Type type)
        {
            --state.ResourceCounts[(int)type];
        }

        public void BuildingFounded(Building building)
        {
            building.Player = Index;

            if (building.BuildingType == Building.Type.Castle)
            {
                state.HasCastle = true;
                state.CanSpawn = true;
                state.TotalBuildingScore += Building.BuildingGetScoreFromType(Building.Type.Castle);
                state.CastleInventoryIndex = building.Inventory.Index;
                selectedBuildingIndex = building.Index;
                CreateInitialCastleSerfs(building);
                lastTick = Game.Tick;
            }
            else
            {
                ++state.IncompleteBuildingCount[(int)building.BuildingType];
            }
        }

        public void BuildingBuilt(Building building)
        {
            var type = building.BuildingType;

            state.TotalBuildingScore += Building.BuildingGetScoreFromType(type);
            ++state.CompletedBuildingCount[(int)type];
            --state.IncompleteBuildingCount[(int)type];
        }

        public void BuildingCaptured(Building building)
        {
            var defendingPlayer = Game.GetPlayer(building.Player);

            defendingPlayer.AddNotification(Notification.Type.LoseFight, building.Position, Index);
            AddNotification(Notification.Type.WinFight, building.Position, Index);

            if (building.BuildingType == Building.Type.Castle)
            {
                ++state.CastleScore;
            }
            else
            {
                var buildingType = building.BuildingType;

                // Update player scores.
                defendingPlayer.state.TotalBuildingScore -= Building.BuildingGetScoreFromType(buildingType);
                defendingPlayer.state.TotalLandArea -= 7;
                --defendingPlayer.state.CompletedBuildingCount[(int)buildingType];

                state.TotalBuildingScore += Building.BuildingGetScoreFromType(buildingType);
                state.TotalLandArea += 7;
                ++state.CompletedBuildingCount[(int)buildingType];

                // Change owner of building
                building.Player = Index;
            }
        }

        public void BuildingDemolished(Building building)
        {
            var buildingType = building.BuildingType;

            // Update player fields.
            if (building.IsDone)
            {
                state.TotalBuildingScore -= Building.BuildingGetScoreFromType(buildingType);

                if (buildingType != Building.Type.Castle)
                {
                    --state.CompletedBuildingCount[(int)buildingType];

                    if (!HasCastle && buildingType == Building.Type.Stock &&
                        state.CompletedBuildingCount[(int)buildingType] == 0)
                    {
                        state.CanSpawn = false;
                    }
                }
                else
                {
                    if (state.CompletedBuildingCount[(int)Building.Type.Stock] == 0)
                        state.CanSpawn = false;

                    --state.CastleScore;
                }
            }
            else
            {
                --state.IncompleteBuildingCount[(int)buildingType];
            }
        }

        public uint GetCompletedBuildingCount(Building.Type type)
        {
            return state.CompletedBuildingCount[(int)type];
        }

        public uint GetIncompleteBuildingCount(Building.Type type)
        {
            return state.IncompleteBuildingCount[(int)type];
        }

        public uint GetTotalBuildingCount(Building.Type type)
        {
            return GetCompletedBuildingCount(type) + GetIncompleteBuildingCount(type);
        }

        public uint IncompleteBuildingCount => state.IncompleteBuildingCount.Aggregate((a, b) => a + b);

        public int GetToolPriority(int type)
        {
            return settings.ToolPriorities[type];
        }

        public void SetToolPriority(int type, int priority)
        {
            settings.ToolPriorities[type] = (word)priority;
        }

        public void SetFullToolPriority(Resource.Type tool)
        {
            // set all tool priorities to 0
            for (int i = 0; i < 9; ++i)
                SetToolPriority(i, ushort.MinValue);

            // set the priority for the tool to 100%
            SetToolPriority(tool - Resource.Type.Shovel, ushort.MaxValue);
        }

        public DirtyArray<byte> GetFlagPriorities()
        {
            return settings.FlagPriorities;
        }

        public byte GetInventoryPriority(Resource.Type type)
        {
            return settings.InventoryPriorities[(int)type];
        }

        public DirtyArray<byte> GetInventoryPriorities()
        {
            return settings.InventoryPriorities;
        }

        public uint TotalMilitaryScore => state.TotalMilitaryScore;

        // Update player game state as part of the game progression.
        public void Update()
        {
            try
            {
                ushort delta = (ushort)(Game.Tick - lastTick);
                lastTick = Game.Tick;

                if (state.TotalLandArea > 0xffff0000)
                    state.TotalLandArea = 0;
                if (state.TotalMilitaryScore > 0xffff0000)
                    state.TotalMilitaryScore = 0;
                if (state.TotalBuildingScore > 0xffff0000)
                    state.TotalBuildingScore = 0;

                if (CyclingKnights)
                {
                    state.KnightCycleCounter -= delta;

                    if (state.KnightCycleCounter < 1)
                    {
                        state.CyclingKnightsReducedLevel = false;
                        state.CyclingKnightsSecondPhase = false;
                        state.CyclingKnightsInProgress = false;
                    }
                    else if (state.KnightCycleCounter < 2048 && ReducedKnightLevel)
                    {
                        state.CyclingKnightsReducedLevel = false;
                        state.CyclingKnightsSecondPhase = true;
                    }
                }

                if (HasCastle)
                {
                    state.ReproductionCounter -= delta;

                    while (state.ReproductionCounter < 0)
                    {
                        state.SerfToKnightCounter = (word)((state.SerfToKnightCounter + settings.SerfToKnightRate) % ushort.MaxValue);

                        if (state.SerfToKnightCounter < settings.SerfToKnightRate)
                        {
                            ++knightsToSpawn;

                            if (knightsToSpawn > 2)
                                knightsToSpawn = 2;
                        }

                        if (knightsToSpawn == 0)
                        {
                            // Create unassigned serf
                            SpawnSerf(null, null, false);
                        }
                        else
                        {
                            // Create knight serf
                            Pointer<Serf> serf = new Pointer<Serf>();
                            Pointer<Inventory> inventory = new Pointer<Inventory>();

                            if (SpawnSerf(serf, inventory, true))
                            {
                                if (inventory.Value.GetCountOf(Resource.Type.Sword) != 0 &&
                                    inventory.Value.GetCountOf(Resource.Type.Shield) != 0)
                                {
                                    --knightsToSpawn;
                                    inventory.Value.PromoteSerfToKnight(serf.Value);
                                }
                            }
                        }

                        state.ReproductionCounter = (word)(state.ReproductionCounter + state.ReproductionReset); // may overflow but this is on purpose
                    }

                    // Update emergency program
                    UpdateEmergencyProgram();
                }

                // Update timers
                List<int> timersToErase = new List<int>();

                for (int i = 0; i < timers.Count; ++i)
                {
                    timers[i].Timeout -= delta;

                    if (timers[i].Timeout < 0)
                    {
                        // Timer has expired.
                        // TODO box (+ position) timer
                        AddNotification(Notification.Type.CallToLocation, timers[i].Position, 0);
                        timersToErase.Add(i);
                    }
                }

                for (int i = timersToErase.Count - 1; i >= 0; --i)
                    timers.RemoveAt(timersToErase[i]);
            }
            catch (Exception ex)
            {
                throw new ExceptionFreeserf(Game, ErrorSystemType.Player, ex);
            }
        }

        void UpdateEmergencyProgram()
        {
            int numLumberjacks = (int)GetTotalBuildingCount(Building.Type.Lumberjack);
            int numStoneCutters = (int)GetTotalBuildingCount(Building.Type.Stonecutter);
            int numSawMills = (int)GetTotalBuildingCount(Building.Type.Sawmill);

            if (numLumberjacks != 0 && numStoneCutters != 0 && numSawMills != 0)
            {
                var lumberjacks = Game.GetPlayerBuildings(this, Building.Type.Lumberjack);
                var stonecutters = Game.GetPlayerBuildings(this, Building.Type.Stonecutter);
                var sawmills = Game.GetPlayerBuildings(this, Building.Type.Sawmill);

                // Check if all resources are delivered to the construction sites
                if (lumberjacks.Any(lumberjack => lumberjack.IsDone || lumberjack.HasAllConstructionMaterialsAtLocation()) &&
                    stonecutters.Any(stonecutter => stonecutter.IsDone || stonecutter.HasAllConstructionMaterialsAtLocation()) &&
                    sawmills.Any(sawmill => sawmill.IsDone || sawmill.HasAllConstructionMaterialsAtLocation()))
                {
                    EmergencyProgramActive = false;
                    return;
                }
            }

            uint planks = Game.GetResourceAmountInInventories(this, Resource.Type.Plank);
            uint stones = Game.GetResourceAmountInInventories(this, Resource.Type.Stone);

            uint numPlanksNeeded = 0;
            uint numStonesNeeded = 0;

            if (numLumberjacks == 0)
            {
                var info = Building.ConstructionInfos[(int)Building.Type.Lumberjack];
                numPlanksNeeded += info.Planks;
                numStonesNeeded += info.Stones;
            }

            if (numStoneCutters == 0)
            {
                var info = Building.ConstructionInfos[(int)Building.Type.Stonecutter];
                numPlanksNeeded += info.Planks;
                numStonesNeeded += info.Stones;
            }

            if (numSawMills == 0)
            {
                var info = Building.ConstructionInfos[(int)Building.Type.Sawmill];
                numPlanksNeeded += info.Planks;
                numStonesNeeded += info.Stones;
            }

            int remainingPlanks = (int)planks - (int)numPlanksNeeded;
            int remainingStones = (int)stones - (int)numStonesNeeded;

            if (remainingPlanks <= 0 || remainingStones <= 0)
            {
                if (!EmergencyProgramActive)
                {
                    EmergencyProgramActive = true;

                    if (EmergencyProgramActive) // Test if successful
                    {
                        // If the emergency program gets activated we cancel all
                        // transported resources to non-essential buildings.
                        foreach (var building in Game.GetPlayerBuildings(this).ToArray())
                        {
                            if (building.IsDone)
                                continue;

                            if (building.BuildingType != Building.Type.Lumberjack &&
                                building.BuildingType != Building.Type.Sawmill &&
                                building.BuildingType != Building.Type.Stonecutter)
                            {
                                var flag = Game.GetFlag(building.FlagIndex);

                                if (flag != null)
                                    Game.FlagResetTransport(flag);

                                // Set priority for construction materials to 0
                                building.SetPriorityInStock(0, 0u);
                                building.SetPriorityInStock(1, 0u);
                            }
                        }
                    }
                }
            }
        }

        public void UpdateStats(int resource)
        {
            resourceCountHistory[resource, Index] = state.ResourceCounts[resource];
            state.ResourceCounts[resource] = 0;
        }

        // Stats
        public void UpdateKnightMorale()
        {
            uint inventoryGold = 0;
            uint militaryGold = 0;

            // Sum gold collected in inventories
            foreach (var inventory in Game.GetPlayerInventories(this))
            {
                inventoryGold += inventory.GetCountOf(Resource.Type.GoldBar);
            }

            // Sum gold deposited in military buildings
            foreach (var building in Game.GetPlayerBuildings(this))
            {
                militaryGold += building.MilitaryGoldCount();
            }

            uint depot = inventoryGold + militaryGold;
            state.GoldDeposited = inventoryGold + militaryGold;

            // Calculate according to gold collected.
            uint totalGold = Game.GoldTotal;

            if (totalGold != 0)
            {
                while (totalGold > 0xffff)
                {
                    totalGold >>= 1;
                    depot >>= 1;
                }

                depot = Math.Min(depot, totalGold - 1);
                state.KnightMorale = 1024u + (Game.MapGoldMoraleFactor * depot) / totalGold;
            }
            else
            {
                state.KnightMorale = 4096u;
            }

            // Adjust based on castle score.
            if (state.CastleScore < 0)
            {
                state.KnightMorale = Math.Max(1, state.KnightMorale - 1023);
            }
            else if (state.CastleScore > 0)
            {
                state.KnightMorale = Math.Min((uint)(state.KnightMorale + 1024 * state.CastleScore), 0xffffu);
            }

            uint militaryScore = state.TotalMilitaryScore;
            uint morale = state.KnightMorale >> 5;

            while (militaryScore > 0xffff)
            {
                militaryScore >>= 1;
                morale <<= 1;
            }

            state.MilitaryMaxGold = 0;
        }

        public uint LandArea => state.TotalLandArea;

        public void IncreaseLandArea()
        {
            ++state.TotalLandArea;
        }

        public void DecreaseLandArea()
        {
            --state.TotalLandArea;
        }

        public uint BuildingScore => state.TotalBuildingScore;

        // Calculate condensed score from military score and knight morale.
        public uint MilitaryScore => 2048u + (state.KnightMorale >> 1) * (state.TotalMilitaryScore << 6);

        public void IncreaseMilitaryScore(uint val)
        {
            state.TotalMilitaryScore += val;
        }

        public void DecreaseMilitaryScore(uint val)
        {
            state.TotalMilitaryScore = (uint)Misc.Max(0, (int)state.TotalMilitaryScore - val);
        }

        public void IncreaseMilitaryMaxGold(int val)
        {
            state.MilitaryMaxGold = (uint)Misc.Max(0, (int)state.MilitaryMaxGold + val);
        }

        public uint Score => state.TotalBuildingScore + ((state.TotalLandArea + MilitaryScore) >> 4);

        public uint InitialSupplies => state.InitialSupplies;

        public uint[] GetResourceCountHistory(Resource.Type type)
        {
            return resourceCountHistory.SliceRow((int)type).ToArray();
        }

        public void SetPlayerStatHistory(int mode, int index, uint val)
        {
            playerStatHistory[mode, index] = val;
        }

        public uint[] GetPlayerStatHistory(int mode)
        {
            return playerStatHistory.SliceRow(mode).ToArray();
        }

        public ResourceMap GetStatsResources()
        {
            var resources = new ResourceMap();

            for (int j = 0; j < 26; ++j)
            {
                // Sum up resources of all inventories.
                resources[(Resource.Type)j] = Game.GetResourceAmountInInventories(this, (Resource.Type)j);
            }

            return resources;
        }

        public SerfMap GetStatsSerfsIdle()
        {
            var serfs = new SerfMap();

            foreach (Serf.Type type in Enum.GetValues(typeof(Serf.Type)))
                serfs.Add(type, 0);

            // Sum up all existing serfs.
            foreach (var serf in Game.GetPlayerSerfs(this))
            {
                if (serf.SerfState == Serf.State.IdleInStock)
                {
                    ++serfs[serf.SerfType];
                }
            }

            return serfs;
        }

        public SerfMap GetStatsSerfsPotential()
        {
            var serfs = new SerfMap();

            foreach (Serf.Type type in Enum.GetValues(typeof(Serf.Type)))
                serfs.Add(type, 0);

            // Sum up potential serfs of all inventories.
            foreach (var inventory in Game.GetPlayerInventories(this))
            {
                if (inventory.FreeSerfCount() > 0)
                {
                    for (int i = 0; i < 27; ++i)
                    {
                        serfs[(Serf.Type)i] += (int)inventory.SerfPotentialCount((Serf.Type)i);
                    }
                }
            }

            return serfs;
        }

        // Settings
        public int SerfToKnightRate
        {
            get => settings.SerfToKnightRate;
            set => settings.SerfToKnightRate = (word)value;
        }

        public uint GetFoodForBuilding(Building.Type buildingType)
        {
            switch (buildingType)
            {
                case Building.Type.StoneMine:
                    return FoodStonemine;
                case Building.Type.CoalMine:
                    return FoodCoalmine;
                case Building.Type.IronMine:
                    return FoodIronmine;
                case Building.Type.GoldMine:
                    return FoodGoldmine;
                default:
                    return 0u;
            }
        }

        public uint FoodStonemine
        {
            get => settings.FoodStonemine;
            set => settings.FoodStonemine = (word)value;
        }
        public uint GetFoodStonemine() { return FoodStonemine; }

        public uint FoodCoalmine
        {
            get => settings.FoodCoalmine;
            set => settings.FoodCoalmine = (word)value;
        }
        public uint GetFoodCoalmine() { return FoodCoalmine; }

        public uint FoodIronmine
        {
            get => settings.FoodIronmine;
            set => settings.FoodIronmine = (word)value;
        }
        public uint GetFoodIronmine() { return FoodIronmine; }

        public uint FoodGoldmine
        {
            get => settings.FoodGoldmine;
            set => settings.FoodGoldmine = (word)value;
        }
        public uint GetFoodGoldmine() { return FoodGoldmine; }

        public uint PlanksConstruction
        {
            get => settings.PlanksConstruction;
            set => settings.PlanksConstruction = (word)value;
        }
        public uint GetPlanksConstruction() { return PlanksConstruction; }

        public uint PlanksBoatbuilder
        {
            get => settings.PlanksBoatbuilder;
            set => settings.PlanksBoatbuilder = (word)value;
        }
        public uint GetPlanksBoatbuilder() { return PlanksBoatbuilder; }

        public uint PlanksToolmaker
        {
            get => settings.PlanksToolmaker;
            set => settings.PlanksToolmaker = (word)value;
        }
        public uint GetPlanksToolmaker() { return PlanksToolmaker; }

        public uint SteelToolmaker
        {
            get => settings.SteelToolmaker;
            set => settings.SteelToolmaker = (word)value;
        }
        public uint GetSteelToolmaker() { return SteelToolmaker; }

        public uint SteelWeaponsmith
        {
            get => settings.SteelWeaponsmith;
            set => settings.SteelWeaponsmith = (word)value;
        }
        public uint GetSteelWeaponsmith() { return SteelWeaponsmith; }

        public uint CoalSteelsmelter
        {
            get => settings.CoalSteelsmelter;
            set => settings.CoalSteelsmelter = (word)value;
        }
        public uint GetCoalSteelsmelter() { return CoalSteelsmelter; }

        public uint CoalGoldsmelter
        {
            get => settings.CoalGoldsmelter;
            set => settings.CoalGoldsmelter = (word)value;
        }
        public uint GetCoalGoldsmelter() { return CoalGoldsmelter; }

        public uint CoalWeaponsmith
        {
            get => settings.CoalWeaponsmith;
            set => settings.CoalWeaponsmith = (word)value;
        }
        public uint GetCoalWeaponsmith() { return CoalWeaponsmith; }

        public uint WheatPigfarm
        {
            get => settings.WheatPigfarm;
            set => settings.WheatPigfarm = (word)value;
        }
        public uint GetWheatPigfarm() { return WheatPigfarm; }

        public uint WheatMill
        {
            get => settings.WheatMill;
            set => settings.WheatMill = (word)value;
        }
        public uint GetWheatMill() { return WheatMill; }

        static readonly int[] minLevelHut = new int[] { 1, 1, 2, 2, 3 };
        static readonly int[] minLevelTower = new int[] { 1, 2, 3, 4, 6 };
        static readonly int[] minLevelFortress = new int[] { 1, 3, 6, 9, 12 };

        int AvailableKnightsAtPosition(MapPos position, int buildingCount, int distance)
        {
            var map = Game.Map;

            if (map.GetOwner(position) != Index ||
                map.TypeUp(position) <= Map.Terrain.Water3 ||
                map.TypeDown(position) <= Map.Terrain.Water3 ||
                map.GetObject(position) < Map.Object.SmallBuilding ||
                map.GetObject(position) > Map.Object.Castle)
            {
                return buildingCount;
            }

            var buildingIndex = map.GetObjectIndex(position);

            for (int i = 0; i < buildingCount; ++i)
            {
                if (attackingBuildings[i] == buildingIndex)
                {
                    return buildingCount;
                }
            }

            var building = Game.GetBuilding(buildingIndex);

            if (!building.IsDone || building.IsBurning)
            {
                return buildingCount;
            }

            int[] minLevel;

            switch (building.BuildingType)
            {
                case Building.Type.Hut: minLevel = minLevelHut; break;
                case Building.Type.Tower: minLevel = minLevelTower; break;
                case Building.Type.Fortress: minLevel = minLevelFortress; break;
                default: return buildingCount;
            }

            if (buildingCount >= 64)
                return buildingCount;

            attackingBuildings[buildingCount] = buildingIndex;

            var threatLevel = building.ThreatLevel;
            var knightsPresent = building.KnightCount;
            int toSend = (int)knightsPresent - minLevel[settings.KnightOccupation[threatLevel] & 0xf];

            if (toSend > 0)
                MaxAttackingKnightsByDistance[distance] += toSend;

            return buildingCount + 1;
        }

        public static readonly Color[] DefaultPlayerColors = new Color[4]
        {
            new Color { Red = 0x00, Green = 0xe3, Blue = 0xe3},
            new Color { Red = 0xcf, Green = 0x63, Blue = 0x63},
            new Color { Red = 0xdf, Green = 0x7f, Blue = 0xef},
            new Color { Red = 0xef, Green = 0xef, Blue = 0x8f}
        };

        /// <summary>
        /// Read legacy savegame.
        /// </summary>
        /// <param name="reader"></param>
        public void ReadFrom(SaveReaderBinary reader)
        {
            for (int j = 0; j < 9; ++j)
            {
                settings.ToolPriorities[j] = reader.ReadWord(); // 0
            }

            for (int j = 0; j < 26; ++j)
            {
                state.ResourceCounts[j] = reader.ReadByte(); // 18
            }

            for (int j = 0; j < 26; ++j)
            {
                settings.FlagPriorities[j] = reader.ReadByte(); // 44
            }

            for (int j = 0; j < 27; ++j)
            {
                state.SerfCounts[j] = reader.ReadWord(); // 70
            }

            for (int j = 0; j < 4; ++j)
            {
                settings.KnightOccupation[j] = reader.ReadByte(); // 124
            }

            Index = reader.ReadWord(); // 128
            state.Color = DefaultPlayerColors[Index];
            byte flags = reader.ReadByte(); // 130
            byte buildFlags = reader.ReadByte(); // 131

            state.HasCastle = (flags & 0x01) != 0;
            settings.SendStrongest = (flags & 0x02) != 0;
            state.CyclingKnightsInProgress = (flags & 0x04) != 0;
            notificationFlag = (flags & 0x08) != 0;
            state.CyclingKnightsReducedLevel = (flags & 0x10) != 0;
            state.CyclingKnightsSecondPhase = (flags & 0x20) != 0;
            state.IsAI = (flags & 0x80) != 0;
            state.CanSpawn = (buildFlags & 0x04) != 0;

            for (int j = 0; j < 23; ++j)
            {
                state.CompletedBuildingCount[j] = reader.ReadWord(); // 132
            }
            for (int j = 0; j < 23; ++j)
            {
                state.IncompleteBuildingCount[j] = reader.ReadWord(); // 178
            }

            for (int j = 0; j < 26; ++j)
            {
                settings.InventoryPriorities[j] = reader.ReadByte(); // 224
            }

            for (int j = 0; j < 64; ++j)
            {
                attackingBuildings[j] = reader.ReadWord(); // 250
            }

            reader.ReadWord();  // 378, player.current_sett_5_item = reader.ReadWord();
            reader.ReadWord();  // 380 ???
            reader.ReadWord();  // 382 ???
            reader.ReadWord();  // 384 ???
            reader.ReadWord();  // 386 ???
            selectedBuildingIndex = reader.ReadWord(); // 388

            reader.ReadWord();  // 390 // castleflag
            state.CastleInventoryIndex = reader.ReadWord(); // 392
            knightsToSpawn = reader.ReadWord(); // 396
            reader.ReadWord();  // 398
            reader.ReadWord();  // 400, player->field_110 = v16;
            reader.ReadWord();  // 402 ???
            reader.ReadWord();  // 404 ???

            state.TotalBuildingScore = reader.ReadDWord(); // 406
            state.TotalMilitaryScore = reader.ReadDWord(); // 410

            lastTick = reader.ReadWord(); // 414

            state.ReproductionCounter = reader.ReadWord(); // 416
            state.ReproductionReset = reader.ReadWord(); // 418
            settings.SerfToKnightRate = reader.ReadWord(); // 420
            state.SerfToKnightCounter = reader.ReadWord(); // 422

            AttackingBuildingCount = reader.ReadWord(); // 424

            for (int j = 0; j < 4; ++j)
            {
                MaxAttackingKnightsByDistance[j] = reader.ReadWord(); // 426
            }

            MaxAttackingKnights = reader.ReadWord(); // 434
            BuildingToAttack = reader.ReadWord(); // 436
            TotalKnightsAttacking = reader.ReadWord(); // 438

            settings.FoodStonemine = reader.ReadWord(); // 448
            settings.FoodCoalmine = reader.ReadWord(); // 450
            settings.FoodIronmine = reader.ReadWord(); // 452
            settings.FoodGoldmine = reader.ReadWord(); // 454

            settings.PlanksConstruction = reader.ReadWord(); // 456
            settings.PlanksBoatbuilder = reader.ReadWord(); // 458
            settings.PlanksToolmaker = reader.ReadWord(); // 460

            settings.SteelToolmaker = reader.ReadWord(); // 462
            settings.SteelWeaponsmith = reader.ReadWord(); // 464

            settings.CoalSteelsmelter = reader.ReadWord(); // 466
            settings.CoalGoldsmelter = reader.ReadWord(); // 468
            settings.CoalWeaponsmith = reader.ReadWord(); // 470

            settings.WheatPigfarm = reader.ReadWord(); // 472
            settings.WheatMill = reader.ReadWord(); // 474

            reader.ReadWord(); // 476, currentett_6tem = reader.ReadWord();

            state.CastleScore = (sbyte)reader.ReadWord(); // 478
        }

        /// <summary>
        /// Read savegames from freeserf project.
        /// </summary>
        public void ReadFrom(SaveReaderText reader)
        {
            var flags = reader.Value("flags").ReadUInt();
            var buildFlags = reader.Value("build").ReadUInt();

            state.HasCastle = (flags & 0x01) != 0;
            settings.SendStrongest = (flags & 0x02) != 0;
            state.CyclingKnightsInProgress = (flags & 0x04) != 0;
            notificationFlag = (flags & 0x08) != 0;
            state.CyclingKnightsReducedLevel = (flags & 0x10) != 0;
            state.CyclingKnightsSecondPhase = (flags & 0x20) != 0;
            state.IsAI = (flags & 0x80) != 0;
            state.CanSpawn = (buildFlags & 0x04) != 0;

            Color.Red = (byte)reader.Value("color")[0].ReadUInt();
            Color.Green = (byte)reader.Value("color")[1].ReadUInt();
            Color.Blue = (byte)reader.Value("color")[2].ReadUInt();
            state.Face = (PlayerFace)reader.Value("face").ReadUInt();

            for (int i = 0; i < 9; ++i)
            {
                settings.ToolPriorities[i] = (word)reader.Value("tool_prio")[i].ReadInt();
            }

            for (int i = 0; i < 26; ++i)
            {
                state.ResourceCounts[i] = reader.Value("resource_count")[i].ReadUInt();
                settings.FlagPriorities[i] = (byte)reader.Value("flag_prio")[i].ReadInt();
                state.SerfCounts[i] = reader.Value("serf_count")[i].ReadUInt();
                settings.InventoryPriorities[i] = (byte)reader.Value("inventory_prio")[i].ReadInt();
            }
            state.SerfCounts[26] = reader.Value("serf_count")[26].ReadUInt();

            for (int i = 0; i < 4; ++i)
            {
                settings.KnightOccupation[i] = (byte)reader.Value("knight_occupation")[i].ReadUInt();
                MaxAttackingKnightsByDistance[i] = reader.Value("attacking_knights")[i].ReadInt();
            }

            for (int i = 0; i < 23; ++i)
            {
                state.CompletedBuildingCount[i] = reader.Value("completed_building_count")[i].ReadUInt();
                state.IncompleteBuildingCount[i] = reader.Value("incomplete_building_count")[i].ReadUInt();
            }

            for (int i = 0; i < 64; ++i)
            {
                attackingBuildings[i] = reader.Value("attacking_buildings")[i].ReadUInt();
            }

            state.InitialSupplies = (byte)reader.Value("initial_supplies").ReadUInt();
            knightsToSpawn = reader.Value("knights_to_spawn").ReadInt();
            state.TotalBuildingScore = reader.Value("total_building_score").ReadUInt();
            state.TotalMilitaryScore = reader.Value("total_military_score").ReadUInt();
            lastTick = (word)reader.Value("last_tick").ReadUInt();
            state.ReproductionCounter = (word)reader.Value("reproduction_counter").ReadInt();
            state.ReproductionReset = (word)reader.Value("reproduction_reset").ReadUInt();
            settings.SerfToKnightRate = (word)reader.Value("serf_to_knight_rate").ReadInt();
            state.SerfToKnightCounter = (word)reader.Value("serf_to_knight_counter").ReadUInt();
            AttackingBuildingCount = reader.Value("attacking_building_count").ReadInt();
            MaxAttackingKnights = reader.Value("total_attacking_knights").ReadInt();
            BuildingToAttack = reader.Value("building_attacked").ReadInt();
            TotalKnightsAttacking = reader.Value("knights_attacking").ReadInt();
            settings.FoodStonemine = (word)reader.Value("food_stonemine").ReadUInt();
            settings.FoodCoalmine = (word)reader.Value("food_coalmine").ReadUInt();
            settings.FoodIronmine = (word)reader.Value("food_ironmine").ReadUInt();
            settings.FoodGoldmine = (word)reader.Value("food_goldmine").ReadUInt();
            settings.PlanksConstruction = (word)reader.Value("planks_construction").ReadUInt();
            settings.PlanksBoatbuilder = (word)reader.Value("planks_boatbuilder").ReadUInt();
            settings.PlanksToolmaker = (word)reader.Value("planks_toolmaker").ReadUInt();
            settings.SteelToolmaker = (word)reader.Value("steel_toolmaker").ReadUInt();
            settings.SteelWeaponsmith = (word)reader.Value("steel_weaponsmith").ReadUInt();
            settings.CoalSteelsmelter = (word)reader.Value("coal_steelsmelter").ReadUInt();
            settings.CoalGoldsmelter = (word)reader.Value("coal_goldsmelter").ReadUInt();
            settings.CoalWeaponsmith = (word)reader.Value("coal_weaponsmith").ReadUInt();
            settings.WheatPigfarm = (word)reader.Value("wheat_pigfarm").ReadUInt();
            settings.WheatMill = (word)reader.Value("wheat_mill").ReadUInt();
            state.CastleScore = (sbyte)reader.Value("castle_score").ReadInt();
            state.CastleKnights = (byte)reader.Value("castle_knights").ReadUInt();
            settings.CastleKnightsWanted = (byte)reader.Value("castle_knights_wanted").ReadUInt();
        }

        /// <summary>
        /// Write savegames for freeserf project.
        /// </summary>
        public void WriteTo(SaveWriterText writer)
        {
            byte flags = 0;
            byte buildFlags = 0;

            if (state.HasCastle)
                flags |= 0x01;
            if (settings.SendStrongest)
                flags |= 0x02;
            if (state.CyclingKnightsInProgress)
                flags |= 0x04;
            if (notificationFlag)
                flags |= 0x08;
            if (state.CyclingKnightsReducedLevel)
                flags |= 0x10;
            if (state.CyclingKnightsSecondPhase)
                flags |= 0x20;
            if (state.IsAI)
                flags |= 0x80;
            if (state.CanSpawn)
                buildFlags |= 0x04;

            writer.Value("flags").Write(flags);
            writer.Value("build").Write(buildFlags);
            writer.Value("color").Write((uint)Color.Red);
            writer.Value("color").Write((uint)Color.Green);
            writer.Value("color").Write((uint)Color.Blue);
            writer.Value("face").Write((uint)Face);

            for (int i = 0; i < 9; ++i)
            {
                writer.Value("tool_prio").Write(settings.ToolPriorities[i]);
            }

            for (int i = 0; i < 26; ++i)
            {
                writer.Value("resource_count").Write(state.ResourceCounts[i]);
                writer.Value("flag_prio").Write(settings.FlagPriorities[i]);
                writer.Value("serf_count").Write(state.SerfCounts[i]);
                writer.Value("inventory_prio").Write(settings.InventoryPriorities[i]);
            }
            writer.Value("serf_count").Write(state.SerfCounts[26]);

            for (int i = 0; i < 4; ++i)
            {
                writer.Value("knight_occupation").Write(settings.KnightOccupation[i]);
                writer.Value("attacking_knights").Write(MaxAttackingKnightsByDistance[i]);
            }

            for (int i = 0; i < 23; ++i)
            {
                writer.Value("completed_building_count").Write(state.CompletedBuildingCount[i]);
                writer.Value("incomplete_building_count").Write(state.IncompleteBuildingCount[i]);
            }

            for (int i = 0; i < 64; ++i)
            {
                writer.Value("attacking_buildings").Write(attackingBuildings[i]);
            }

            writer.Value("initial_supplies").Write(state.InitialSupplies);
            writer.Value("knights_to_spawn").Write(knightsToSpawn);

            writer.Value("total_building_score").Write(state.TotalBuildingScore);
            writer.Value("total_military_score").Write(state.TotalMilitaryScore);

            writer.Value("last_tick").Write(lastTick);

            writer.Value("reproduction_counter").Write(state.ReproductionCounter);
            writer.Value("reproduction_reset").Write(state.ReproductionReset);
            writer.Value("serf_to_knight_rate").Write(settings.SerfToKnightRate);
            writer.Value("serf_to_knight_counter").Write(state.SerfToKnightCounter);

            writer.Value("attacking_building_count").Write(AttackingBuildingCount);
            writer.Value("total_attacking_knights").Write(MaxAttackingKnights);
            writer.Value("building_attacked").Write(BuildingToAttack);
            writer.Value("knights_attacking").Write(TotalKnightsAttacking);

            writer.Value("food_stonemine").Write(FoodStonemine);
            writer.Value("food_coalmine").Write(FoodCoalmine);
            writer.Value("food_ironmine").Write(FoodIronmine);
            writer.Value("food_goldmine").Write(FoodGoldmine);

            writer.Value("planks_construction").Write(PlanksConstruction);
            writer.Value("planks_boatbuilder").Write(PlanksBoatbuilder);
            writer.Value("planks_toolmaker").Write(PlanksToolmaker);

            writer.Value("steel_toolmaker").Write(SteelToolmaker);
            writer.Value("steel_weaponsmith").Write(SteelWeaponsmith);

            writer.Value("coal_steelsmelter").Write(CoalSteelsmelter);
            writer.Value("coal_goldsmelter").Write(CoalGoldsmelter);
            writer.Value("coal_weaponsmith").Write(CoalWeaponsmith);

            writer.Value("wheat_pigfarm").Write(WheatPigfarm);
            writer.Value("wheat_mill").Write(WheatMill);

            writer.Value("castle_score").Write(state.CastleScore);

            writer.Value("castle_knights").Write(state.CastleKnights);
            writer.Value("castle_knights_wanted").Write(settings.CastleKnightsWanted);
        }
    }
}
