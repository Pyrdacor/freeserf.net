/*
 * Player.cs - Player related functions
 *
 * Copyright (C) 2013-2017  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018       Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Freeserf
{
    using MapPos = UInt32;
    using Messages = Queue<Message>;
    using PosTimers = List<PosTimer>;
    using ListInventories = List<Inventory>;
    using ResourceMap = Dictionary<Resource.Type, uint>;
    using SerfMap = Dictionary<Serf.Type, uint>;

    public class Message
    {
        public enum Type
        {
            None = 0,
            UnderAttack = 1,
            LoseFight = 2,
            WinFight = 3,
            MineEmpty = 4,
            CallToLocation = 5,
            KnightOccupied = 6,
            NewStock = 7,
            LostLand = 8,
            LostBuildings = 9,
            EmergencyActive = 10,
            EmergencyNeutral = 11,
            FoundGold = 12,
            FoundIron = 13,
            FoundCoal = 14,
            FoundStone = 15,
            CallToMenu = 16,
            ThirtyMinutesSinceSave = 17,
            OneHourSinceSave = 18,
            CallToStock = 19
        }

        public Type MessageType { get; set; } = Type.None;
        public MapPos Pos { get; set; } = 0;
        public uint Data { get; set; } = 0;
    }

    class PosTimer
    {
        public int Timeout = 0;
        public MapPos Pos = 0;
    }

    public class Player : GameObject
    {
        public struct Color
        {
            public byte Red;
            public byte Green;
            public byte Blue;
        }

        int[] toolPriorities = new int[9];
        uint[] resourceCount = new uint[26];
        int[] flagPriorities = new int[26];
        uint[] serfCount = new uint[27];
        uint[] knightOccupation = new uint[4];

        Color color = new Color()
        {
            Red = 0, Green = 0, Blue = 0
        };
        uint face = uint.MaxValue;

        //Bit 0: Has castle
        //Bit 1: Send strongest knights
        //Bit 2: Cycling knights is in progress
        //Bit 3: Message/Notification in queue/active
        //Bit 4: Knight level reduces due to knight cycling
        //Bit 5: Knight cycling in phase 2 (new knights from inventory to buildings)
        //Bit 6: Unused
        //Bit 7: AI
        uint flags = 0u;
        //Bit 0: Allow military building at current pos
        //Bit 1: Allow flag at current pos
        //Bit 2: Player can spawn new serfs
        //Bit 3-7: Unused
        uint build = 0u;
        uint[] completedBuildingCount = new uint[24];
        uint[] incompleteBuildingCount = new uint[24];
        int[] inventoryPriorities = new int[26];
        uint[] attackingBuildings = new uint[64];

        Messages messages = new Messages();
        PosTimers timers = new PosTimers();

        int building = 0;
        int castleInventory = 0;
        int contSearchAfterNonOptimalFind = 7;
        int knightsToSpawn = 0;
        uint totalLandArea = 0;
        uint totalBuildingScore = 0;
        uint totalMilitaryScore = 0;
        ushort lastTick = 0;

        int reproductionCounter = 0;
        uint reproductionReset = 0;
        int serfToKnightRate = 20000;
        ushort serfToKnightCounter = 0x8000; /* Overflow is important */
        int analysisGoldore = 0;
        int analysisIronore = 0;
        int analysisCoal = 0;
        int analysisStone = 0;

        uint foodStonemine = 0 ; /* Food delivery priority of food for mines. */
        uint foodCoalmine = 0;
        uint foodIronmine = 0;
        uint foodGoldmine = 0;
        uint planksConstruction = 0; /* Planks delivery priority. */
        uint planksBoatbuilder = 0;
        uint planksToolmaker = 0;
        uint steelToolmaker = 0;
        uint steelWeaponsmith = 0;
        uint coalSteelsmelter = 0;
        uint coalGoldsmelter = 0;
        uint coalWeaponsmith = 0;
        uint wheatPigfarm = 0;
        uint wheatMill = 0;

        /* +1 for every castle defeated,
           -1 for own castle lost. */
        int castleScore = 0;
        int sendGenericDelay = 0;
        uint initialSupplies = 0;
        int serfIndex = 0;
        int knightCycleCounter = 0;
        int sendKnightDelay = 0;
        int militaryMaxGold = 0;

        uint knightMorale = 0;
        uint goldDeposited = 0;
        uint castleKnightsWanted = 3;
        uint castleKnights = 0;
        int aiValue0 = 0;
        int aiValue1 = 0;
        int aiValue2 = 0;
        int aiValue3 = 0;
        int aiValue4 = 0;
        int aiValue5 = 0;
        uint aiIntelligence = 0;

        uint[,] playerStatHistory = new uint[16,112];
        uint[,] resourceCountHistory = new uint[26,120];

        // TODO(Digger): remove it to UI
        public int buildingAttacked = 0;
        public int knightsAttacking = 0;
        public int attackingBuildingCount = 0;
        public int[] attackingKnights = new int[4];
        public int totalAttackingKnights = 0;
        //public uint tempIndex = 0;

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

            /* player.field_1b0 = 0; AI */
            /* player.field_1b2 = 0; AI */

            /* TODO AI: Set array field_402 of length 25 to -1. */
            /* TODO AI: Set array field_434 of length 280*2 to 0 */
            /* TODO AI: Set array field_1bc of length 8 to -1 */
        }

        // Initialize player values.
        //
        // Supplies and reproduction are usually limited to 0-40 in random map games.
        //
        // Args:
        //     face: the face image that represents this player.
        //           1-12 is AI, 13-14 is human player.
        //     color: Color of player as palette color index.
        //     supplies: Initial resource supplies at castle (0-50).
        //     reproduction: How quickly new serfs spawn during the game (0-60).
        //     intelligence: AI only (unused) (0-40).
        public void Init(uint intelligence, uint supplies, uint reproduction)
        {
            flags = 0;

            initialSupplies = supplies;
            reproductionReset = (60 - reproduction) * 50;
            aiIntelligence = (1300 * intelligence) + 13535;
            reproductionCounter = (int)reproductionReset;
        }

        public void InitView(Color color, uint face)
        {
            this.face = face;

            if (face < 12)
            { 
                /* AI player */
                flags |= Misc.BitU(7); /* Set AI bit */
                                       /* TODO ... */
                                       /*Game.max_next_index = 49;*/
            }

            if (IsAi())
                InitAiValues(face);

            this.color = color;
        }

        public Color GetColor()
        {
            return color;
        }

        public uint GetFace()
        {
            return face;
        }

        /* Whether player has built the initial castle. */
        public bool HasCastle()
        {
            return (flags & 1) != 0;
        }

        /* Whether the strongest knight should be sent to fight. */
        public bool SendStrongest()
        {
            return (flags & 2) != 0;
        }

        public void DropSendStrongest()
        {
            flags &= ~Misc.BitU(1);
        }

        public void SetSendStrongest()
        {
            flags |= Misc.BitU(1);
        }

        /* Whether cycling of knights is in progress. */
        public bool CyclingKnight()
        {
            return (flags & 4) != 0;
        }

        /* Whether a message is queued for this player. */
        public bool HasMessage()
        {
            return (flags& 8) != 0;
        }

        public void DropMessage()
        {
            flags &= ~Misc.BitU(3);
        }

        /* Whether the knight level of military buildings is temporarily
        reduced bacause of cycling of the knights. */
        public bool ReducedKnightLevel()
        {
            return (flags & 16) != 0;
        }

        /* Whether the cycling of knights is in the second phase. */
        public bool CyclingSecond()
        {
            return (flags & 32) != 0;
        }

        /* Whether this player is a computer controlled opponent. */
        public bool IsAi()
        {
            return (flags & 128) != 0;
        }

        /* Whether player is prohibited from building military
           buildings at current position. */
        public bool AllowMilitary()
        {
            return (build & 1) == 0;
        }

        /* Whether player is prohibited from building flag at
           current position. */
        public bool AllowFlag()
        {
            return (build & 2) == 0;
        }

        /* Whether player can spawn new serfs. */
        public bool CanSpawn()
        {
            return (build & 4) != 0;
        }

        public uint GetSerfCount(int type)
        {
            return serfCount[type];
        }

        public int GetFlagPriority(int resource)
        {
            return flagPriorities[resource];
        }

        /* Enqueue a new notification message for player. */
        public void AddNotification(Message.Type type, MapPos pos, uint data)
        {
            flags |= Misc.BitU(3); /* Message in queue. */

            Message newMessage = new Message();
            newMessage.MessageType = type;
            newMessage.Pos = pos;
            newMessage.Data = data;

            messages.Enqueue(newMessage);
        }

        public bool HasNotification()
        {
            return messages.Count > 0;
        }

        public Message PopNotification()
        {
            return messages.Dequeue();
        }

        public Message PeekNotification()
        {
            return messages.Peek();
        }


        public void AddTimer(int timeout, MapPos pos)
        {
            PosTimer newTimer = new PosTimer();

            newTimer.Timeout = timeout;
            newTimer.Pos = pos;

            timers.Add(newTimer);
        }

        /* Set defaults for food distribution priorities. */
        public void ResetFoodPriority()
        {
            foodStonemine = 13100;
            foodCoalmine = 45850;
            foodIronmine = 45850;
            foodGoldmine = 65500;
        }

        /* Set defaults for planks distribution priorities. */
        public void ResetPlanksPriority()
        {
            planksConstruction = 65500;
            planksBoatbuilder = 3275;
            planksToolmaker = 19650;
        }

        /* Set defaults for steel distribution priorities. */
        public void ResetSteelPriority()
        {
            steelToolmaker = 45850;
            steelWeaponsmith = 65500;
        }

        /* Set defaults for coal distribution priorities. */
        public void ResetCoalPriority()
        {
            coalSteelsmelter = 32750;
            coalGoldsmelter = 65500;
            coalWeaponsmith = 52400;
        }

        /* Set defaults for wheat distribution priorities. */
        public void ResetWheatPriority()
		{
            wheatPigfarm = 65500;
            wheatMill = 32750;
        }

        /* Set defaults for tool production priorities. */
        public void ResetToolPriority()
		{
            toolPriorities[0] = 9825; /* SHOVEL */
            toolPriorities[1] = 65500; /* HAMMER */
            toolPriorities[2] = 13100; /* ROD */
            toolPriorities[3] = 6550; /* CLEAVER */
            toolPriorities[4] = 13100; /* SCYTHE */
            toolPriorities[5] = 26200; /* AXE */
            toolPriorities[6] = 32750; /* SAW */
            toolPriorities[7] = 45850; /* PICK */
            toolPriorities[8] = 6550; /* PINCER */
        }

        /* Set defaults for flag priorities. */
        public void ResetFlagPriority()
		{
            flagPriorities[(int)Resource.Type.GoldOre] = 1;
            flagPriorities[(int)Resource.Type.GoldBar] = 2;
            flagPriorities[(int)Resource.Type.Wheat] = 3;
            flagPriorities[(int)Resource.Type.Flour] = 4;
            flagPriorities[(int)Resource.Type.Pig] = 5;

            flagPriorities[(int)Resource.Type.Boat] = 6;
            flagPriorities[(int)Resource.Type.Pincer] = 7;
            flagPriorities[(int)Resource.Type.Scythe] = 8;
            flagPriorities[(int)Resource.Type.Rod] = 9;
            flagPriorities[(int)Resource.Type.Cleaver] = 10;

            flagPriorities[(int)Resource.Type.Saw] = 11;
            flagPriorities[(int)Resource.Type.Axe] = 12;
            flagPriorities[(int)Resource.Type.Pick] = 13;
            flagPriorities[(int)Resource.Type.Shovel] = 14;
            flagPriorities[(int)Resource.Type.Hammer] = 15;

            flagPriorities[(int)Resource.Type.Shield] = 16;
            flagPriorities[(int)Resource.Type.Sword] = 17;
            flagPriorities[(int)Resource.Type.Bread] = 18;
            flagPriorities[(int)Resource.Type.Meat] = 19;
            flagPriorities[(int)Resource.Type.Fish] = 20;

            flagPriorities[(int)Resource.Type.IronOre] = 21;
            flagPriorities[(int)Resource.Type.Lumber] = 22;
            flagPriorities[(int)Resource.Type.Coal] = 23;
            flagPriorities[(int)Resource.Type.Steel] = 24;
            flagPriorities[(int)Resource.Type.Stone] = 25;
            flagPriorities[(int)Resource.Type.Plank] = 26;
        }

        /* Set defaults for inventory priorities. */
        public void ResetInventoryPriority()
		{
            inventoryPriorities[(int)Resource.Type.Wheat] = 1;
            inventoryPriorities[(int)Resource.Type.Flour] = 2;
            inventoryPriorities[(int)Resource.Type.Pig] = 3;
            inventoryPriorities[(int)Resource.Type.Bread] = 4;
            inventoryPriorities[(int)Resource.Type.Fish] = 5;

            inventoryPriorities[(int)Resource.Type.Meat] = 6;
            inventoryPriorities[(int)Resource.Type.Lumber] = 7;
            inventoryPriorities[(int)Resource.Type.Plank] = 8;
            inventoryPriorities[(int)Resource.Type.Boat] = 9;
            inventoryPriorities[(int)Resource.Type.Stone] = 10;

            inventoryPriorities[(int)Resource.Type.Coal] = 11;
            inventoryPriorities[(int)Resource.Type.IronOre] = 12;
            inventoryPriorities[(int)Resource.Type.Steel] = 13;
            inventoryPriorities[(int)Resource.Type.Shovel] = 14;
            inventoryPriorities[(int)Resource.Type.Hammer] = 15;

            inventoryPriorities[(int)Resource.Type.Rod] = 16;
            inventoryPriorities[(int)Resource.Type.Cleaver] = 17;
            inventoryPriorities[(int)Resource.Type.Scythe] = 18;
            inventoryPriorities[(int)Resource.Type.Axe] = 19;
            inventoryPriorities[(int)Resource.Type.Saw] = 20;

            inventoryPriorities[(int)Resource.Type.Pick] = 21;
            inventoryPriorities[(int)Resource.Type.Pincer] = 22;
            inventoryPriorities[(int)Resource.Type.Shield] = 23;
            inventoryPriorities[(int)Resource.Type.Sword] = 24;
            inventoryPriorities[(int)Resource.Type.GoldOre] = 25;
            inventoryPriorities[(int)Resource.Type.GoldBar] = 26;
        }

        public uint GetKnightOccupation(uint threatLevel)
        {
            return knightOccupation[(int)threatLevel];
        }

        public void ChangeKnightOccupation(int index, bool adjustMax, int delta)
		{
            uint max = (knightOccupation[index] >> 4) & 0xf;
            uint min = knightOccupation[index] & 0xf;

            if (adjustMax)
            {
                max = (uint)Misc.Clamp((int)min, (int)max + delta, 4);
            }
            else
            {
                min = (uint)Misc.Clamp(0, (int)min + delta, (int)max);
            }

            knightOccupation[index] = (max << 4) | min;
        }

        public void IncreaseCastleKnights()
        {
            ++castleKnights;
        }

        public void DecreaseCastleKnights()
        {
            --castleKnights;
        }

        public uint GetCastleKnights()
        {
            return castleKnights;
        }

        public uint GetCastleKnightsWanted()
        {
            return castleKnightsWanted;
        }

        public void IncreaseCastleKnightsWanted()
		{
            castleKnightsWanted = Math.Min(castleKnightsWanted + 1, 99);
        }

        public void DecreaseCastleKnightsWanted()
		{
            castleKnightsWanted = Math.Max(1, castleKnightsWanted - 1);
        }

        public uint GetKnightMorale()
        {
            return knightMorale;
        }

        public uint GetGoldDeposited()
        {
            return goldDeposited;
        }

        /* Turn a number of serfs into knight for the given player. */
        public int PromoteSerfsToKnights(int number)
		{
            int promoted = 0;

            foreach (Serf serf in Game.GetPlayerSerfs(this))
            {
                if (serf.SerfState == Serf.State.IdleInStock &&
                    serf.GetSerfType() == Serf.Type.Generic)
                {
                    Inventory inv = Game.GetInventory(serf.GetIdleInStockInventoryIndex());

                    if (inv.PromoteSerfToKnight(serf))
                    {
                        ++promoted;
                        --number;
                    }
                }
            }

            return promoted;
        }

        public int KnightsAvailableForAttack(MapPos pos)
		{
            /* Reset counters. */
            for (int i = 0; i < 4; ++i)
            {
                attackingKnights[i] = 0;
            }

            int count = 0;
            Map map = Game.Map;

            /* Iterate each shell around the position.*/
            for (int i = 0; i < 32; ++i)
            {
                pos = map.MoveRight(pos);

                for (int j = 0; j < i + 1; ++j)
                {
                    count = AvailableKnightsAtPos(pos, count, i >> 3);
                    pos = map.MoveDown(pos);
                }

                for (int j = 0; j < i + 1; ++j)
                {
                    count = AvailableKnightsAtPos(pos, count, i >> 3);
                    pos = map.MoveLeft(pos);
                }
                for (int j = 0; j < i + 1; ++j)
                {
                    count = AvailableKnightsAtPos(pos, count, i >> 3);
                    pos = map.MoveUpLeft(pos);
                }

                for (int j = 0; j < i + 1; ++j)
                {
                    count = AvailableKnightsAtPos(pos, count, i >> 3);
                    pos = map.MoveUp(pos);
                }

                for (int j = 0; j < i + 1; ++j)
                {
                    count = AvailableKnightsAtPos(pos, count, i >> 3);
                    pos = map.MoveRight(pos);
                }

                for (int j = 0; j < i + 1; ++j)
                {
                    count = AvailableKnightsAtPos(pos, count, i >> 3);
                    pos = map.MoveDownRight(pos);
                }
            }

            attackingBuildingCount = count;
            totalAttackingKnights = 0;

            for (int i = 0; i < 4; ++i)
            {
                totalAttackingKnights += attackingKnights[i];
            }

            return totalAttackingKnights;
        }

        public void StartAttack()
		{
            Building target = Game.GetBuilding((uint)buildingAttacked);

            if (!target.IsDone()   || !target.IsMilitary() ||
                !target.IsActive() || target.GetThreatLevel() != 3)
            {
                return;
            }

            Map map = Game.Map;

            for (int i = 0; i < attackingBuildingCount; i++)
            {
                /* TODO building index may not be valid any more(?). */
                Building building = Game.GetBuilding((uint)attackingBuildings[i]);

                if (building.IsBurning() || map.GetOwner(building.Position) != Index)
                {
                    continue;
                }

                MapPos flagPos = map.MoveDownRight(building.Position);

                if (map.HasSerf(flagPos))
                {
                    /* Check if building is under siege. */
                    Serf serf = Game.GetSerfAtPos(flagPos);

                    if (serf.Player != Index)
                        continue;
                }

                int[] minLevel = null;

                switch (building.BuildingType)
                {
                    case Building.Type.Hut: minLevel = minLevelHut; break;
                    case Building.Type.Tower: minLevel = minLevelTower; break;
                    case Building.Type.Fortress: minLevel = minLevelFortress; break;
                    default: continue;
                }

                uint state = building.GetThreatLevel();
                uint knightsPresent = building.GetKnightCount();
                int toSend = (int)knightsPresent - minLevel[knightOccupation[state] & 0xf];

                for (int j = 0; j < toSend; ++j)
                {
                    /* Find most appropriate knight to send according to player settings. */
                    var bestType = SendStrongest() ? Serf.Type.Knight0 : Serf.Type.Knight4;
                    uint bestIndex = 0;

                    uint knightIndex = building.GetFirstKnight();

                    while (knightIndex != 0)
                    {
                        Serf knight = Game.GetSerf(knightIndex);

                        if (SendStrongest())
                        {
                            if (knight.GetSerfType() >= bestType)
                            {
                                bestIndex = knightIndex;
                                bestType = knight.GetSerfType();
                            }
                        }
                        else
                        {
                            if (knight.GetSerfType() <= bestType)
                            {
                                bestIndex = knightIndex;
                                bestType = knight.GetSerfType();
                            }
                        }

                        knightIndex = knight.GetNextKnight();
                    }

                    Serf defSerf = building.CallAttackerOut(bestIndex);

                    target.SetUnderAttack();

                    /* Calculate distance to target. */
                    int distColumn = map.DistX(target.Position, defSerf.Position);
                    int distRow = map.DistY(target.Position, defSerf.Position);

                    /* Send this serf off to fight. */
                    defSerf.SendOffToFight(distColumn, distRow);

                    if (--knightsAttacking == 0)
                        return;
                }
            }
        }

        /* Begin cycling knights by sending knights from military buildings
           to inventories. The knights can then be replaced by more experienced
           knights. */
        public void CycleKnights()
		{
            flags |= Misc.BitU(2) | Misc.BitU(4);
            knightCycleCounter = 2400;
        }

        /* Create the initial serfs that occupies the castle. */
        public void CreateInitialCastleSerfs(Building castle)
		{
            build |= Misc.BitU(2);

            /* Spawn serf 4 */
            Inventory inventory = castle.GetInventory();
            Serf serf = inventory.SpawnSerfGeneric();

            if (serf == null)
            {
                return;
            }

            inventory.SpecializeSerf(serf, Serf.Type.TransporterInventory);
            serf.InitInventoryTransporter(inventory);

            Game.Map.SetSerfIndex(serf.Position, (int)serf.Index);

            Building building = Game.GetBuilding((uint)this.building);
            building.SetFirstKnight(serf.Index);

            /* Spawn generic serfs */
            for (int i = 0; i < 5; i++)
            {
                SpawnSerf(null, null, false);
            }

            /* Spawn three knights */
            for (int i = 0; i < 3; i++)
            {
                serf = inventory.SpawnSerfGeneric();

                if (serf == null)
                    return;

                inventory.PromoteSerfToKnight(serf);
            }

            /* Spawn toolmaker */
            serf = inventory.SpawnSerfGeneric();

            if (serf == null)
                return;

            inventory.SpecializeSerf(serf, Serf.Type.Toolmaker);

            /* Spawn timberman */
            serf = inventory.SpawnSerfGeneric();

            if (serf == null)
                return;

            inventory.SpecializeSerf(serf, Serf.Type.Lumberjack);

            /* Spawn sawmiller */
            serf = inventory.SpawnSerfGeneric();

            if (serf == null)
                return;

            inventory.SpecializeSerf(serf, Serf.Type.Sawmiller);

            /* Spawn stonecutter */
            serf = inventory.SpawnSerfGeneric();

            if (serf == null)
                return;

            inventory.SpecializeSerf(serf, Serf.Type.Stonecutter);

            /* Spawn digger */
            serf = inventory.SpawnSerfGeneric();

            if (serf == null)
                return;

            inventory.SpecializeSerf(serf, Serf.Type.Digger);

            /* Spawn builder */
            serf = inventory.SpawnSerfGeneric();

            if (serf == null)
                return;

            inventory.SpecializeSerf(serf, Serf.Type.Builder);

            /* Spawn fisherman */
            serf = inventory.SpawnSerfGeneric();

            if (serf == null)
                return;

            inventory.SpecializeSerf(serf, Serf.Type.Fisher);

            /* Spawn two geologists */
            for (int i = 0; i < 2; i++)
            {
                serf = inventory.SpawnSerfGeneric();

                if (serf == null)
                    return;

                inventory.SpecializeSerf(serf, Serf.Type.Geologist);
            }

            /* Spawn two miners */
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
            Serf serf = Game.CreateSerf();

            if (serf == null)
                return null;

            serf.Player = Index;

            ++serfCount[(int)Serf.Type.Generic];

            return serf;
        }

        /* Spawn new serf. Returns 0 on success.
           The serf object and inventory are returned if non-NULL. */
        public int SpawnSerf(Pointer<Serf> serf, Pointer<Inventory> inventory, bool wantKnight)
		{
            if (!CanSpawn())
                return -1;

            ListInventories inventories = Game.GetPlayerInventories(this);

            if (inventories.Count < 1)
            {
                return -1;
            }

            Inventory inv = null;

            foreach (Inventory loopInv in inventories)
            {
                if (loopInv.GetSerfMode() == Inventory.Mode.In)
                {
                    if (wantKnight && (loopInv.GetCountOf(Resource.Type.Sword) == 0 ||
                                       loopInv.GetCountOf(Resource.Type.Shield) == 0))
                    {
                        continue;
                    }
                    else if (loopInv.FreeSerfCount() == 0)
                    {
                        inv = loopInv;
                        break;
                    }
                    else if (inv == null || loopInv.FreeSerfCount() < inv.FreeSerfCount())
                    {
                        inv = loopInv;
                    }
                }
            }

            if (inv == null)
            {
                if (wantKnight)
                {
                    return SpawnSerf(serf, inventory, false);
                }
                else
                {
                    return -1;
                }
            }

            Serf s = inv.SpawnSerfGeneric();

            if (s == null)
            {
                return -1;
            }

            if (serf != null)
                serf.Value = s;

            if (inventory != null)
                inventory.Value = inv;

            return 0;
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

        public Serf.Type GetCyclingSerfType(Serf.Type type)
        {
            if (CyclingSecond())
            {
                type = (Serf.Type)(-((knightCycleCounter >> 8) + 1)); // TODO: ??? Is this right with minus?
            }

            return type;
        }

        public void IncreaseSerfCount(Serf.Type type)
        {
            ++serfCount[(int)type];
        }

        public void DecreaseSerfCount(Serf.Type type)
		{
            if (serfCount[(int)type] == 0)
            {
                throw new ExceptionFreeserf("Failed to decrease serf count");
            }

            --serfCount[(int)type];
        }

        public uint[] GetSerfCounts()
        {
            return serfCount;
        }

        public void IncreaseResourceCount(Resource.Type type)
        {
            ++resourceCount[(int)type];
        }

        public void DecreaseResourceCount(Resource.Type type)
        {
            --resourceCount[(int)type];
        }

        public void BuildingFounded(Building building)
		{
            building.Player = Index;

            if (building.BuildingType == Building.Type.Castle)
            {
                flags |= Misc.BitU(0); /* Has castle */
                build |= Misc.BitU(3);
                totalBuildingScore += Building.BuildingGetScoreFromType(Building.Type.Castle);
                castleInventory = (int)building.GetInventory().Index;
                this.building = (int)building.Index;
                CreateInitialCastleSerfs(building);
                lastTick = Game.Tick;
            }
            else
            {
                ++incompleteBuildingCount[(int)building.BuildingType];
            }
        }

        public void BuildingBuilt(Building building)
		{
            Building.Type type = building.BuildingType;

            totalBuildingScore += Building.BuildingGetScoreFromType(type);
            ++completedBuildingCount[(int)type];
            --incompleteBuildingCount[(int)type];
        }

        public void BuildingCaptured(Building building)
		{
            Player defPlayer = Game.GetPlayer(building.Player);

            defPlayer.AddNotification(Message.Type.LoseFight, building.Position, Index);
            AddNotification(Message.Type.WinFight, building.Position, Index);

            if (building.BuildingType == Building.Type.Castle)
            {
                ++castleScore;
            }
            else
            {
                var buildingType = building.BuildingType;

                /* Update player scores. */
                defPlayer.totalBuildingScore -= Building.BuildingGetScoreFromType(buildingType);
                defPlayer.totalLandArea -= 7;
                --defPlayer.completedBuildingCount[(int)buildingType];

                totalBuildingScore += Building.BuildingGetScoreFromType(buildingType);
                totalLandArea += 7;
                ++completedBuildingCount[(int)buildingType];

                /* Change owner of building */
                building.Player = Index;

                if (IsAi())
                {
                    /* TODO AI */
                }
            }
        }

        public void BuildingDemolished(Building building)
		{
            var buildingType = building.BuildingType;

            /* Update player fields. */
            if (building.IsDone())
            {
                totalBuildingScore -= Building.BuildingGetScoreFromType(buildingType);

                if (buildingType != Building.Type.Castle)
                {
                    completedBuildingCount[(int)buildingType] -= 1;
                }
                else
                {
                    build &= ~Misc.BitU(3);
                    --castleScore;
                }
            }
            else
            {
                --incompleteBuildingCount[(int)buildingType];
            }
        }

        public uint GetCompletedBuildingCount(int type)
        {
            return completedBuildingCount[type];
        }

        public uint GetIncompleteBuildingCount(int type)
        {
            return incompleteBuildingCount[type];
        }

        public int GetToolPriority(int type)
        {
            return toolPriorities[type];
        }

        public void SetToolPriority(int type, int priority)
        {
            toolPriorities[type] = priority;
        }

        public int[] GetFlagPriorities()
        {
            return flagPriorities;
        }

        public int GetInventoryPriority(int type)
        {
            return inventoryPriorities[type];
        }

        public int[] GetInventoryPriorities()
        {
            return inventoryPriorities;
        }

        public uint GetTotalMilitaryScore()
        {
            return totalMilitaryScore;
        }

        /* Update player game state as part of the game progression. */
        public void Update()
		{
            ushort delta = (ushort)(Game.Tick - lastTick);
            lastTick = Game.Tick;

            if (totalLandArea > 0xffff0000)
                totalLandArea = 0;
            if (totalMilitaryScore > 0xffff0000)
                totalMilitaryScore = 0;
            if (totalBuildingScore > 0xffff0000)
                totalBuildingScore = 0;

            if (IsAi())
            {
                /*if (player.field_1B2 != 0) player.field_1B2 -= 1;*/
                /*if (player.field_1B0 != 0) player.field_1B0 -= 1;*/
            }

            if (CyclingKnight())
            {
                knightCycleCounter -= delta;

                if (knightCycleCounter < 1)
                {
                    flags &= ~Misc.BitU(5);
                    flags &= ~Misc.BitU(2);
                }
                else if (knightCycleCounter < 2048 && ReducedKnightLevel())
                {
                    flags |= Misc.BitU(5);
                    flags &= ~Misc.BitU(4);
                }
            }

            if (HasCastle())
            {
                reproductionCounter -= delta;

                while (reproductionCounter < 0)
                {
                    serfToKnightCounter = (ushort)(serfToKnightCounter + serfToKnightRate);

                    if (serfToKnightCounter < serfToKnightRate)
                    {
                        ++knightsToSpawn;

                        if (knightsToSpawn > 2)
                            knightsToSpawn = 2;
                    }

                    if (knightsToSpawn == 0)
                    {
                        /* Create unassigned serf */
                        SpawnSerf(null, null, false);
                    }
                    else
                    {
                        /* Create knight serf */
                        Pointer<Serf> serf = new Pointer<Serf>();
                        Pointer<Inventory> inventory = new Pointer<Inventory>();
                        int r = SpawnSerf(serf, inventory, true);

                        if (r >= 0)
                        {
                            if (inventory.Value.GetCountOf(Resource.Type.Sword) != 0 &&
                                inventory.Value.GetCountOf(Resource.Type.Shield) != 0)
                            {
                                --knightsToSpawn;
                                inventory.Value.SpecializeSerf(serf.Value, Serf.Type.Knight0);
                            }
                        }
                    }

                    reproductionCounter += (int)reproductionReset;
                }
            }

            /* Update timers */
            List<int> timersToErase = new List<int>();

            for (int i = 0; i < timers.Count; ++i)
            {
                timers[i].Timeout -= delta;

                if (timers[i].Timeout < 0)
                {
                    /* Timer has expired. */
                    /* TODO box (+ pos) timer */
                    AddNotification(Message.Type.CallToLocation, timers[i].Pos, 0);
                    timersToErase.Add(i);
                }
            }

            for (int i = timersToErase.Count - 1; i >= 0; --i)
                timers.RemoveAt(timersToErase[i]);
        }

        public void UpdateStats(int resource)
		{
            resourceCountHistory[resource, Index] = resourceCount[resource];
            resourceCount[resource] = 0;
        }

        // Stats
        public void UpdateKnightMorale()
		{
            uint inventoryGold = 0;
            uint militaryGold = 0;

            /* Sum gold collected in inventories */
            foreach (Inventory inventory in Game.GetPlayerInventories(this))
            {
                inventoryGold += inventory.GetCountOf(Resource.Type.GoldBar);
            }

            /* Sum gold deposited in military buildings */
            foreach (Building building in Game.GetPlayerBuildings(this))
            {
                militaryGold += building.MilitaryGoldCount();
            }

            uint depot = inventoryGold + militaryGold;
            goldDeposited = inventoryGold + militaryGold;

            /* Calculate according to gold collected. */
            uint totalGold = Game.GoldTotal;

            if (totalGold != 0)
            {
                while (totalGold > 0xffff)
                {
                    totalGold >>= 1;
                    depot >>= 1;
                }

                depot = Math.Min(depot, totalGold - 1);
                knightMorale = 1024u + (Game.MapGoldMoraleFactor * depot) / totalGold;
            }
            else
            {
                knightMorale = 4096u;
            }

            /* Adjust based on castle score. */
            if (castleScore < 0)
            {
                knightMorale = Math.Max(1, knightMorale - 1023);
            }
            else if (castleScore > 0)
            {
                knightMorale = Math.Min((uint)(knightMorale + 1024 * castleScore), 0xffffu);
            }

            uint militaryScore = totalMilitaryScore;
            uint morale = knightMorale >> 5;

            while (militaryScore > 0xffff)
            {
                militaryScore >>= 1;
                morale <<= 1;
            }

            /* Calculate fractional score used by AI */
            uint playerScore = (militaryScore * morale) >> 7;
            uint enemyScore = Game.GetEnemyScore(this);

            while (playerScore > 0xffff && enemyScore > 0xffff)
            {
                playerScore >>= 1;
                enemyScore >>= 1;
            }
            /*
              player_score >>= 1;
              uint frac_score = 0;
              if (player_score != 0 && enemy_score != 0) {
                if (player_score > enemy_score) {
                  frac_score = 0xffffffff;
                } else {
                  frac_score = (player_score * 0x10000) / enemy_score;
                }
              }
            */
            militaryMaxGold = 0;
        }

        public uint GetLandArea()
        {
            return totalLandArea;
        }

        public void IncreaseLandArea()
        {
            ++totalLandArea;
        }

        public void DecreaseLandArea()
        {
            --totalLandArea;
        }

        public uint GetBuildingScore()
        {
            return totalBuildingScore;
        }

        /* Calculate condensed score from military score and knight morale. */
        public uint GetMilitaryScore()
        {
            return 2048u + (knightMorale >> 1) * (totalMilitaryScore << 6);
        }

        public void IncreaseMilitaryScore(uint val)
        {
            totalMilitaryScore += val;
        }

        public void DecreaseMilitaryScore(uint val)
        {
            totalMilitaryScore -= val;
        }

        public void IncreaseMilitaryMaxGold(int val)
        {
            militaryMaxGold += val;
        }

        public uint GetScore()
        {
            uint militaryScore = GetMilitaryScore();

            return totalBuildingScore + ((totalLandArea + militaryScore) >> 4);
        }

        public uint GetInitialSupplies()
        {
            return initialSupplies;
        }
  
        public IEnumerable<uint> GetResourceCountHistory(Resource.Type type)
        {
            return resourceCountHistory.SliceRow((int)type);
        }

        public void SetPlayerStatHistory(int mode, int index, uint val)
        {
            playerStatHistory[mode, index] = val;
        }

        public IEnumerable<uint> GetPlayerStatHistory(int mode)
        {
            return playerStatHistory.SliceRow(mode);
        }

        public ResourceMap GetStatsResources()
        {
            ResourceMap resources = new ResourceMap();

            /* Sum up resources of all inventories. */
            foreach (Inventory inventory in Game.GetPlayerInventories(this))
            {
                for (int j = 0; j < 26; ++j)
                {
                    resources[(Resource.Type)j] += inventory.GetCountOf((Resource.Type)j);
                }
            }

            return resources;
        }

        public SerfMap GetStatsSerfsIdle()
        {
            SerfMap serfs = new SerfMap();

            /* Sum up all existing serfs. */
            foreach (Serf serf in Game.GetPlayerSerfs(this))
            {
                if (serf.SerfState == Serf.State.IdleInStock)
                {
                    ++serfs[serf.GetSerfType()];
                }
            }

            return serfs;
        }

        public SerfMap GetStatsSerfsPotential()
        {
            SerfMap serfs = new SerfMap();

            /* Sum up potential serfs of all inventories. */
            foreach (Inventory inventory in Game.GetPlayerInventories(this))
            {
                if (inventory.FreeSerfCount() > 0)
                {
                    for (int i = 0; i < 27; ++i)
                    {
                        serfs[(Serf.Type)i] += inventory.SerfPotentialCount((Serf.Type)i);
                    }
                }
            }

            return serfs;
        }

        // Settings
        public int GetSerfToKnightRate()
        {
            return serfToKnightRate;
        }

        public void SetSerfToKnightRate(int rate)
        {
            serfToKnightRate = rate;
        }

        public uint GetFoodForBuilding(Building.Type buildingType)
        {
            uint resource = 0;

            switch (buildingType)
            {
                case Building.Type.StoneMine:
                    resource = GetFoodStonemine();
                    break;
                case Building.Type.CoalMine:
                    resource = GetFoodCoalmine();
                    break;
                case Building.Type.IronMine:
                    resource = GetFoodIronmine();
                    break;
                case Building.Type.GoldMine:
                    resource = GetFoodGoldmine();
                    break;
                default:
                    break;
            }

            return resource;
        }

        public uint GetFoodStonemine()
        {
            return foodStonemine;
        }

        public void SetFoodStonemine(uint val)
        {
            foodStonemine = val;
        }

        public uint GetFoodCoalmine()
        {
            return foodCoalmine;
        }

        public void SetFoodCoalmine(uint val)
        {
            foodCoalmine = val;
        }

        public uint GetFoodIronmine()
        {
            return foodIronmine;
        }

        public void SetFoodIronmine(uint val)
        {
            foodIronmine = val;
        }

        public uint GetFoodGoldmine()
        {
            return foodGoldmine;
        }

        public void SetFoodGoldmine(uint val)
        {
            foodGoldmine = val;
        }

        public uint GetPlanksConstruction()
        {
            return planksConstruction;
        }

        public void SetPlanksConstruction(uint val)
        {
            planksConstruction = val;
        }

        public uint GetPlanksBoatbuilder()
        {
            return planksBoatbuilder;
        }

        public void SetPlanksBoatbuilder(uint val)
        {
            planksBoatbuilder = val;
        }

        public uint GetPlanksToolmaker()
        {
            return planksToolmaker;
        }

        public void SetPlanksToolmaker(uint val)
        {
            planksToolmaker = val;
        }

        public uint GetSteelToolmaker()
        {
            return steelToolmaker;
        }

        public void SetSteelToolmaker(uint val)
        {
            steelToolmaker = val;
        }

        public uint GetSteelWeaponsmith()
        {
            return steelWeaponsmith;
        }

        public void SetSteelWeaponsmith(uint val)
        {
            steelWeaponsmith = val;
        }

        public uint GetCoalSteelsmelter()
        {
            return coalSteelsmelter;
        }

        public void SetCoalSteelsmelter(uint val)
        {
            coalSteelsmelter = val;
        }

        public uint GetCoalGoldsmelter()
        {
            return coalGoldsmelter;
        }

        public void SetCoalGoldsmelter(uint val)
        {
            coalGoldsmelter = val;
        }

        public uint GetCoalWeaponsmith()
        {
            return coalWeaponsmith;
        }

        public void SetCoalWeaponsmith(uint val)
        {
            coalWeaponsmith = val;
        }

        public uint GetWheatPigfarm()
        {
            return wheatPigfarm;
        }

        public void SetWheatPigfarm(uint val)
        {
            wheatPigfarm = val;
        }

        public uint GetWheatMill()
        {
            return wheatMill;
        }

        public void SetWheatMill(uint val)
        {
            wheatMill = val;
        }

        /* Initialize AI parameters. */
        protected void InitAiValues(uint face)
        {
            // TODO
            /*const int ai_values_0[] = { 13, 10, 16, 9, 10, 8, 6, 10, 12, 5, 8 };
            const int ai_values_1[] = { 10000, 13000, 16000, 16000, 18000, 20000,
                                  19000, 18000, 30000, 23000, 26000 };
            const int ai_values_2[] = { 10000, 35000, 20000, 27000, 37000, 25000,
                                  40000, 30000, 50000, 35000, 40000 };
            const int ai_values_3[] = { 0, 36, 0, 31, 8, 480, 3, 16, 0, 193, 39 };
            const int ai_values_4[] = { 0, 30000, 5000, 40000, 50000, 20000, 45000,
                                  35000, 65000, 25000, 30000 };
            const int ai_values_5[] = { 60000, 61000, 60000, 65400, 63000, 62000,
                                  65000, 63000, 64000, 64000, 64000 };

            ai_value_0 = ai_values_0[face_ - 1];
            ai_value_1 = ai_values_1[face_ - 1];
            ai_value_2 = ai_values_2[face_ - 1];
            ai_value_3 = ai_values_3[face_ - 1];
            ai_value_4 = ai_values_4[face_ - 1];
            ai_value_5 = ai_values_5[face_ - 1];*/
        }

        static readonly int[] minLevelHut = new int[] { 1, 1, 2, 2, 3 };
        static readonly int[] minLevelTower = new int[] { 1, 2, 3, 4, 6 };
        static readonly int[] minLevelFortress = new int[] { 1, 3, 6, 9, 12 };

        int AvailableKnightsAtPos(MapPos pos, int index, int dist)
        {
            Map map = Game.Map;

            if (map.GetOwner(pos) != Index ||
                map.TypeUp(pos) <= Map.Terrain.Water3 ||
                map.TypeDown(pos) <= Map.Terrain.Water3 ||
                map.GetObject(pos) < Map.Object.SmallBuilding ||
                map.GetObject(pos) > Map.Object.Castle)
            {
                return index;
            }

            uint buildingIndex = map.GetObjectIndex(pos);

            for (int i = 0; i < index; ++i)
            {
                if (attackingBuildings[i] == buildingIndex)
                {
                    return index; // TODO: is index right here? not i?
                }
            }

            Building building = Game.GetBuilding(buildingIndex);

            if (!building.IsDone() || building.IsBurning())
            {
                return index;
            }

            int[] minLevel = null;

            switch (building.BuildingType)
            {
                case Building.Type.Hut: minLevel = minLevelHut; break;
                case Building.Type.Tower: minLevel = minLevelTower; break;
                case Building.Type.Fortress: minLevel = minLevelFortress; break;
                default: return index;
            }

            if (index >= 64)
                return index;

            attackingBuildings[index] = buildingIndex;

            uint state = building.GetThreatLevel();
            uint knightsPresent = building.GetKnightCount();
            int toSend = (int)knightsPresent - minLevel[knightOccupation[state] & 0xf];

            if (toSend > 0)
                attackingKnights[dist] += toSend;

            return index + 1;
        }

        static readonly Color[] DefaultPlayerColors = new Color[4]
        {
            new Color { Red = 0x00, Green = 0xe3, Blue = 0xe3},
            new Color { Red = 0xcf, Green = 0x63, Blue = 0x63},
            new Color { Red = 0xdf, Green = 0x7f, Blue = 0xef},
            new Color { Red = 0xef, Green = 0xef, Blue = 0x8f}
        };

        public void ReadFrom(SaveReaderBinary reader)
        {
            for (int j = 0; j < 9; ++j)
            {
                toolPriorities[j] = reader.ReadWord(); // 0
            }

            for (int j = 0; j < 26; ++j)
            {
                resourceCount[j] = reader.ReadByte(); // 18
            }

            for (int j = 0; j < 26; ++j)
            {
                flagPriorities[j] = reader.ReadByte(); // 44
            }

            for (int j = 0; j < 27; ++j)
            {
                serfCount[j] = reader.ReadWord(); // 70
            }

            for (int j = 0; j < 4; ++j)
            {
                knightOccupation[j] = reader.ReadByte(); // 124
            }

            Index = reader.ReadWord(); // 128
            color = DefaultPlayerColors[Index];
            flags = reader.ReadByte(); // 130
            build = reader.ReadByte(); // 131

            for (int j = 0; j < 23; ++j)
            {
                completedBuildingCount[j] = reader.ReadWord(); // 132
            }
            for (int j = 0; j < 23; ++j)
            {
                incompleteBuildingCount[j] = reader.ReadWord(); // 178
            }

            for (int j = 0; j < 26; ++j)
            {
                inventoryPriorities[j] = reader.ReadByte(); // 224
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
            building = reader.ReadWord(); // 388

            reader.ReadWord();  // 390 // castleflag
            castleInventory = reader.ReadWord(); // 392
            contSearchAfterNonOptimalFind = reader.ReadWord(); // 394
            knightsToSpawn = reader.ReadWord(); // 396
            reader.ReadWord();  // 398
            reader.ReadWord();  // 400, player->field_110 = v16;
            reader.ReadWord();  // 402 ???
            reader.ReadWord();  // 404 ???

            totalBuildingScore = reader.ReadDWord(); // 406
            totalMilitaryScore = reader.ReadDWord(); // 410

            lastTick = reader.ReadWord(); // 414

            reproductionCounter = reader.ReadWord(); // 416
            reproductionReset = reader.ReadWord(); // 418
            serfToKnightRate = reader.ReadWord(); // 420
            serfToKnightCounter = reader.ReadWord(); // 422

            attackingBuildingCount = reader.ReadWord(); // 424

            for (int j = 0; j < 4; ++j)
            {
                attackingKnights[j] = reader.ReadWord(); // 426
            }

            totalAttackingKnights = reader.ReadWord(); // 434
            buildingAttacked = reader.ReadWord(); // 436
            knightsAttacking = reader.ReadWord(); // 438

            analysisGoldore = reader.ReadWord(); // 440
            analysisIronore = reader.ReadWord(); // 442
            analysisCoal = reader.ReadWord(); // 444
            analysisStone = reader.ReadWord(); // 446

            foodStonemine = reader.ReadWord(); // 448
            foodCoalmine = reader.ReadWord(); // 450
            foodIronmine = reader.ReadWord(); // 452
            foodGoldmine = reader.ReadWord(); // 454

            planksConstruction = reader.ReadWord(); // 456
            planksBoatbuilder = reader.ReadWord(); // 458
            planksToolmaker = reader.ReadWord(); // 460

            steelToolmaker = reader.ReadWord(); // 462
            steelWeaponsmith = reader.ReadWord(); // 464

            coalSteelsmelter = reader.ReadWord(); // 466
            coalGoldsmelter = reader.ReadWord(); // 468
            coalWeaponsmith = reader.ReadWord(); // 470

            wheatPigfarm = reader.ReadWord(); // 472
            wheatMill = reader.ReadWord(); // 474

            reader.ReadWord(); // 476, currentett_6tem = reader.ReadWord();

            castleScore = reader.ReadWord(); // 478

            /* TODO */
        }

        public void ReadFrom(SaveReaderText reader)
        {
            flags = reader.Value("flags").ReadUInt();
            build = reader.Value("build").ReadUInt();
            color.Red = (byte)reader.Value("color")[0].ReadUInt();
            color.Green = (byte)reader.Value("color")[1].ReadUInt();
            color.Blue = (byte)reader.Value("color")[2].ReadUInt();
            face = reader.Value("face").ReadUInt();

            for (int i = 0; i < 9; ++i)
            {
                toolPriorities[i] = reader.Value("toolPrio")[i].ReadInt();
            }

            for (int i = 0; i < 26; ++i)
            {
                resourceCount[i] = reader.Value("resourceCount")[i].ReadUInt();
                flagPriorities[i] = reader.Value("flagPrio")[i].ReadInt();
                serfCount[i] = reader.Value("serfCount")[i].ReadUInt();
                inventoryPriorities[i] = reader.Value("inventoryPrio")[i].ReadInt();
            }
            serfCount[26] = reader.Value("serfCount")[26].ReadUInt();

            for (int i = 0; i < 4; ++i)
            {
                knightOccupation[i] = reader.Value("knightOccupation")[i].ReadUInt();
                attackingKnights[i] = reader.Value("attackingKnights")[i].ReadInt();
            }

            for (int i = 0; i < 23; ++i)
            {
                completedBuildingCount[i] = reader.Value("completedBuildingCount")[i].ReadUInt();
                incompleteBuildingCount[i] = reader.Value("incompleteBuildingCount")[i].ReadUInt();
            }

            for (int i = 0; i < 64; ++i)
            {
                attackingBuildings[i] = reader.Value("attackingBuildings")[i].ReadUInt();
            }

            initialSupplies = reader.Value("initialSupplies").ReadUInt();
            knightsToSpawn = reader.Value("knightsToSpawn").ReadInt();
            totalBuildingScore = reader.Value("totalBuildingScore").ReadUInt();
            totalMilitaryScore = reader.Value("totalMilitaryScore").ReadUInt();
            lastTick = (ushort)reader.Value("lastTick").ReadUInt();
            reproductionCounter = reader.Value("reproductionCounter").ReadInt();
            reproductionReset = reader.Value("reproductionReset").ReadUInt();
            serfToKnightRate = reader.Value("serfToKnightRate").ReadInt();
            serfToKnightCounter = (ushort)reader.Value("serfToKnightCounter").ReadUInt();
            attackingBuildingCount = reader.Value("attackingBuildingCount").ReadInt();
            totalAttackingKnights = reader.Value("totalAttackingKnights").ReadInt();
            buildingAttacked = reader.Value("buildingAttacked").ReadInt();
            knightsAttacking = reader.Value("knightsAttacking").ReadInt();
            foodStonemine = reader.Value("foodStonemine").ReadUInt();
            foodCoalmine = reader.Value("foodCoalmine").ReadUInt();
            foodIronmine = reader.Value("foodIronmine").ReadUInt();
            foodGoldmine = reader.Value("foodGoldmine").ReadUInt();
            planksConstruction = reader.Value("planksConstruction").ReadUInt();
            planksBoatbuilder = reader.Value("planksBoatbuilder").ReadUInt();
            planksToolmaker = reader.Value("planksToolmaker").ReadUInt();
            steelToolmaker = reader.Value("steelToolmaker").ReadUInt();
            steelWeaponsmith = reader.Value("steelWeaponsmith").ReadUInt();
            coalSteelsmelter = reader.Value("coalSteelsmelter").ReadUInt();
            coalGoldsmelter = reader.Value("coalGoldsmelter").ReadUInt();
            coalWeaponsmith = reader.Value("coalWeaponsmith").ReadUInt();
            wheatPigfarm = reader.Value("wheatPigfarm").ReadUInt();
            wheatMill = reader.Value("wheatMill").ReadUInt();
            castleScore = reader.Value("castleScore").ReadInt();
            castleKnights = reader.Value("castleKnights").ReadUInt();
            castleKnightsWanted = reader.Value("castleKnightsWanted").ReadUInt();
        }

        public void WriteTo(SaveWriterText writer)
        {
            writer.Value("flags").Write(flags);
            writer.Value("build").Write(build);
            writer.Value("color").Write((uint)color.Red);
            writer.Value("color").Write((uint)color.Green);
            writer.Value("color").Write((uint)color.Blue);
            writer.Value("face").Write(face);

            for (int i = 0; i < 9; ++i)
            {
                writer.Value("tool_prio").Write(toolPriorities[i]);
            }

            for (int i = 0; i < 26; ++i)
            {
                writer.Value("resource_count").Write(resourceCount[i]);
                writer.Value("flag_prio").Write(flagPriorities[i]);
                writer.Value("serf_count").Write(serfCount[i]);
                writer.Value("inventory_prio").Write(inventoryPriorities[i]);
            }
            writer.Value("serf_count").Write(serfCount[26]);

            for (int i = 0; i < 4; ++i)
            {
                writer.Value("knight_occupation").Write(knightOccupation[i]);
                writer.Value("attacking_knights").Write(attackingKnights[i]);
            }

            for (int i = 0; i < 23; ++i)
            {
                writer.Value("completed_building_count").Write(completedBuildingCount[i]);
                writer.Value("incomplete_building_count").Write(incompleteBuildingCount[i]);
            }

            for (int i = 0; i< 64; ++i)
            {
                writer.Value("attacking_buildings").Write(attackingBuildings[i]);
            }

            writer.Value("initial_supplies").Write(initialSupplies);
            writer.Value("knights_to_spawn").Write(knightsToSpawn);

            writer.Value("total_building_score").Write(totalBuildingScore);
            writer.Value("total_military_score").Write(totalMilitaryScore);

            writer.Value("last_tick").Write(lastTick);

            writer.Value("reproduction_counter").Write(reproductionCounter);
            writer.Value("reproduction_reset").Write(reproductionReset);
            writer.Value("serf_to_knight_rate").Write(serfToKnightRate);
            writer.Value("serf_to_knight_counter").Write(serfToKnightCounter);

            writer.Value("attacking_building_count").Write(attackingBuildingCount);
            writer.Value("total_attacking_knights").Write(totalAttackingKnights);
            writer.Value("building_attacked").Write(buildingAttacked);
            writer.Value("knights_attacking").Write(knightsAttacking);

            writer.Value("food_stonemine").Write(foodStonemine);
            writer.Value("food_coalmine").Write(foodCoalmine);
            writer.Value("food_ironmine").Write(foodIronmine);
            writer.Value("food_goldmine").Write(foodGoldmine);

            writer.Value("planks_construction").Write(planksConstruction);
            writer.Value("planks_boatbuilder").Write(planksBoatbuilder);
            writer.Value("planks_toolmaker").Write(planksToolmaker);

            writer.Value("steel_toolmaker").Write(steelToolmaker);
            writer.Value("steel_weaponsmith").Write(steelWeaponsmith);

            writer.Value("coal_steelsmelter").Write(coalSteelsmelter);
            writer.Value("coal_goldsmelter").Write(coalGoldsmelter);
            writer.Value("coal_weaponsmith").Write(coalWeaponsmith);

            writer.Value("wheat_pigfarm").Write(wheatPigfarm);
            writer.Value("wheat_mill").Write(wheatMill);

            writer.Value("castle_score").Write(castleScore);

            writer.Value("castle_knights").Write(castleKnights);
            writer.Value("castle_knights_wanted").Write(castleKnightsWanted);
        }
    }
}
