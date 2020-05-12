/*
 * AI.cs - Character AI logic
 *
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
    using MapPos = UInt32;

    abstract class AIState
    {
        readonly AI.State state = AI.State.None;
        public bool Killed { get; protected set; } = false;
        public AIState NextState { get; protected set; } = null;
        public int Delay { get; set; } = 0;

        protected AIState(AI.State state)
        {
            this.state = state;
        }

        public abstract void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick);

        public virtual void Kill(AI ai)
        {
            ai.PopState();
            Killed = true;
        }

        public void GoToState(AI ai, AI.State state, object param = null)
        {
            if (state != AI.State.Idle)
                NextState = ai.CreateState(state, param);

            Kill(ai);
        }

        public static AIState Read(Game game, AI ai, string name, SaveReaderText reader)
        {
            var type = reader.Value($"{name}.type").ReadEnum<AI.State>();

            if (type == AI.State.None)
                return null;

            var state = ai.CreateState(type);

            state.ReadFrom(game, ai, name, reader);

            return state;
        }

        protected virtual void ReadFrom(Game game, AI ai, string name, SaveReaderText reader)
        {
            // Note: The type is read in the static Read function

            Killed = reader.Value($"{name}.killed").ReadBool();
            NextState = Read(game, ai, $"{name}.next_state", reader);
            Delay = reader.Value($"{name}.delay").ReadInt();
        }

        public virtual void WriteTo(string name, SaveWriterText writer)
        {
            writer.Value($"{name}.type").Write(state);
            writer.Value($"{name}.killed").Write(Killed);

            if (NextState == null)
                writer.Value($"{name}.next_state.type").Write(AI.State.None);
            else
                // TODO: Could we come to an infinite recursive loop here?
                NextState.WriteTo($"{name}.next_state", writer);

            writer.Value($"{name}.delay").Write(Delay);
        }
    }

    abstract class ResetableAIState : AIState
    {
        protected ResetableAIState(AI.State state)
            : base(state)
        {

        }

        public void Reset()
        {
            Killed = false;
        }
    }

    // TODO: The AI should not try to build a fisher when there is no water. Instead it must switch to a different food resource.

    /*
     * AIs with higher intelligence should be more clever when dealing
     * with low starting resources:
     * 
     * Instead of building a toolmaker without having materials for
     * crafting tools the AI should focus on finding coal and iron
     * and expand their territory to get them. Then build mines and
     * a steel smelter and distribute food in a clever way to the
     * mines. An early fisher is prefered too.
     * 
     * The castle spot picking should consider mountains and a lake
     * for this purpose.
     */

    /// <summary>
    /// Note: The ai will not be the same as in the original game.
    /// But it will reflect some of the ai character descriptions.
    /// The plan is to add some new ai characters with better ai.
    /// 
    /// Maybe the campaign will be extended for this sake as well.
    /// </summary>
    public class AI
    {
        public enum State
        {
            None = -1,
            Idle,
            ChooseCastleLocation,
            CastleBuilt,
            BuildBuilding,
            LinkBuilding,
            LinkDisconnectedFlags,
            CheckNeededBuilding,
            CraftWeapons,
            CraftTool,
            FindOre,
            AdjustSettings,
            Attack,
            AvoidCongestion,
            DestroyUselessBuildings
        }

        public enum AttackTarget
        {
            Random,
            SmallMilitary,
            FoodProduction,
            MaterialProduction, // planks and stones
            Mines,
            WeaponProduction,
            Stocks
        }

        public enum AttackPlayer
        {
            Random,
            Weakest, // military
            Worst, // all statistics
            RandomHuman,
            RandomAI,
            WorstProtected // least military building occupation
        }

        Player player = null;
        PlayerInfo playerInfo = null;
        readonly Stack<AIState> states = new Stack<AIState>();
        int lastTick = 0;
        long lastUpdate = 0;
        internal long GameTime { get; private set; } = 0;
        Random random = new Random(Guid.NewGuid().ToString());
        long nextHardTimeCheck = 0;
        bool lastHardTime = true;

        // Special AI values
        public bool CanAttack { get; protected set; } = true;
        public bool CanExpand { get; protected set; } = true;
        public int MaxMilitaryBuildings { get; protected set; } = -1;

        // Memory (this is shared by all AIs)
        struct MineralSpot
        {
            public uint Position;
            public bool Large;
        }

        static readonly Dictionary<Map.Minerals, List<MineralSpot>> memorizedMineralSpots = new Dictionary<Map.Minerals, List<MineralSpot>>();

        public static void ClearMemory()
        {
            memorizedMineralSpots.Clear();

            for (int i = 1; i <= 4; ++i)
                memorizedMineralSpots.Add((Map.Minerals)i, new List<MineralSpot>());
        }

        public static void MemorizeMineralSpot(MapPos position, Map.Minerals mineral, bool large)
        {
            memorizedMineralSpots[mineral].Add(new MineralSpot()
            {
                Position = position,
                Large = large
            });
        }

        internal static IEnumerable<uint> GetMemorizedMineralSpots(Map.Minerals mineral, bool large)
        {
            return memorizedMineralSpots[mineral].Where(spot => !large || spot.Large).Select(s => s.Position);
        }

        /// <summary>
        /// How aggressive (2 = very aggressive)
        /// </summary>
        public int Aggressivity { get; private set; } = 0; // 0 - 2
        /// <summary>
        /// Prefering rushing (2 = rushes very often)
        /// </summary>
        public int RushAffinity { get; private set; } = 0; // 0 - 2
        /// <summary>
        /// How skilled in fights (2 = skilled fighter)
        /// </summary>
        public int MilitarySkill { get; private set; } = 0; // 0 - 2
        /// <summary>
        /// How much focus on military (2 = high focus)
        /// </summary>
        public int MilitaryFocus { get; private set; } = 0; // 0 - 2
        /// <summary>
        /// How much focus on expanding (2 = aggressive expansion)
        /// </summary>
        public int ExpandFocus { get; private set; } = 0; // 0 - 2
        /// <summary>
        /// How much focus on defending (2 = build many military buildings to protect important buildings and keep as many knights there as possible)
        /// </summary>
        public int DefendFocus { get; private set; } = 0; // 0 - 2
        /// <summary>
        /// How much focus at having much buildings
        /// </summary>
        public int BuildingFocus { get; private set; } = 0; // 0 - 2
        /// <summary>
        /// How much focus at having much gold
        /// </summary>
        public int GoldFocus { get; private set; } = 0; // 0 - 2
        /// <summary>
        /// How much focus at having much coal and iron
        /// </summary>
        public int SteelFocus { get; private set; } = 0; // 0 - 2
        /// <summary>
        /// How much focus at having much food
        /// </summary>
        public int FoodFocus { get; private set; } = 0; // 0 - 2
        /// <summary>
        /// How much focus at having much construction materials
        /// </summary>
        public int ConstructionMaterialFocus { get; private set; } = 0; // 0 - 2
        /// <summary>
        /// The ai will try to gather prioritized food if possible
        /// </summary>
        readonly int[] foodSourcePriorities = new int[3]; // 0 - 2 for fish, bread and meat
        /// <summary>
        /// The ai will try to build prioritized military buildings if possible
        /// </summary>
        readonly int[] militaryBuildingPriorities = new int[3]; // 0 - 2 for hut, tower and fortress
        readonly int[] minPlanksForMilitaryBuildings = new int[2]; // minimum free planks for tower and fortress
        readonly int[] minStonesForMilitaryBuildings = new int[2]; // minimum free stones for tower and fortress
        /// <summary>
        /// The ai may prioritize specific targets when attacking
        /// </summary>
        public AttackTarget PrioritizedAttackTarget { get; private set; } = AttackTarget.Random;
        /// <summary>
        /// The ai may prioritize specific targets when attacking
        /// </summary>
        public AttackTarget SecondPrioritizedAttackTarget { get; private set; } = AttackTarget.Random;
        /// <summary>
        /// The ai may prioritize specific players when attacking
        /// </summary>
        public AttackPlayer PrioritizedPlayer { get; private set; } = AttackPlayer.Random;
        /// <summary>
        /// The ai may prioritize specific players when attacking
        /// </summary>
        public AttackPlayer SecondPrioritizedPlayer { get; private set; } = AttackPlayer.Random;

        /// <summary>
        /// Priorities can be 0, 1 or 2 (each value can only be used once).
        /// </summary>
        /// <param name="food">0: fish, 1: bread, 2: meat</param>
        /// <returns></returns>
        public int GetFoodSourcePriority(int food)
        {
            return foodSourcePriorities[food];
        }
        /// <summary>
        /// Priority 2 means 60%, 1 means 30% and 0 means 10%.
        /// </summary>
        /// <param name="food">0: fish, 1: bread, 2: meat</param>
        /// <returns></returns>
        public int GetFoodSourcePriorityInPercentage(int food)
        {
            int priority = GetFoodSourcePriority(food);

            if (priority == 0)
                return 10;

            return priority * 30;
        }

        /// <summary>
        /// Priority 2 means 60%, 1 means 30% and 0 means 10%.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public int GetMilitaryBuildingPercentage(Building.Type type)
        {
            int index;

            switch (type)
            {
                default:
                    return 0;
                case Building.Type.Hut:
                    index = 0;
                    break;
                case Building.Type.Tower:
                    index = 1;
                    break;
                case Building.Type.Fortress:
                    index = 2;
                    break;
            }

            if (militaryBuildingPriorities[index] == 0)
                return 10;

            return militaryBuildingPriorities[index] * 30;
        }

        public bool CanBuildMilitaryBuilding(Building.Type type)
        {
            if (!CanExpand)
                return false;

            return type switch
            {
                Building.Type.Hut => true,
                Building.Type.Tower => player.Game.GetResourceAmountInInventories(player, Resource.Type.Plank) >= minPlanksForMilitaryBuildings[0] &&
                    player.Game.GetResourceAmountInInventories(player, Resource.Type.Stone) >= minStonesForMilitaryBuildings[0],
                Building.Type.Fortress => player.Game.GetResourceAmountInInventories(player, Resource.Type.Plank) >= minPlanksForMilitaryBuildings[1] &&
                    player.Game.GetResourceAmountInInventories(player, Resource.Type.Stone) >= minStonesForMilitaryBuildings[1],
                _ => false,
            };
        }

        /* Military Focus
         * 
         * - Focuses on getting coal and iron
         * - Focuses on crafting weapons and shields
         * - High knight production
         * - Many military buildings
         */

        /* Military Skill
         * 
         * - Will protect important buildings with some military buildings
         * - Will have enough knights where needed
         * - Will try to attack strategic spots of the enemy
         * - Prefers attacking spots of the enemy that are able to capture
         */

        public AI(Player player, PlayerInfo playerInfo)
        {
            this.player = player;
            this.playerInfo = playerInfo;

            Init();
        }

        void Init()
        {
            // Default values
            Aggressivity = 0;
            MilitarySkill = 0;
            MilitaryFocus = 0;
            ExpandFocus = 0;
            DefendFocus = 0;
            BuildingFocus = 0;
            GoldFocus = 0;
            SteelFocus = 0;
            FoodFocus = 0;
            ConstructionMaterialFocus = 0;
            foodSourcePriorities[0] = 2; // fish
            foodSourcePriorities[1] = 1; // bread
            foodSourcePriorities[2] = 0; // meat
            militaryBuildingPriorities[0] = 2; // hut
            militaryBuildingPriorities[1] = 1; // tower
            militaryBuildingPriorities[2] = 0; // fortress
            minPlanksForMilitaryBuildings[0] = 20; // tower
            minPlanksForMilitaryBuildings[1] = 30; // fortress
            minStonesForMilitaryBuildings[0] = 12; // tower
            minStonesForMilitaryBuildings[1] = 18; // fortress

            switch (playerInfo.Face)
            {
                case PlayerFace.LadyAmalie:
                    foodSourcePriorities[0] = 0; // fish
                    foodSourcePriorities[1] = 2; // bread
                    foodSourcePriorities[2] = 1; // meat
                    FoodFocus = 1;
                    PrioritizedPlayer = AttackPlayer.RandomAI;
                    SecondPrioritizedPlayer = AttackPlayer.Random;
                    break;
                case PlayerFace.KumpyOnefinger:
                    GoldFocus = 2;
                    break;
                case PlayerFace.Balduin:
                    DefendFocus = 2;
                    foodSourcePriorities[0] = 0; // fish
                    foodSourcePriorities[1] = 1; // bread
                    foodSourcePriorities[2] = 2; // meat
                    militaryBuildingPriorities[0] = 0; // hut
                    militaryBuildingPriorities[1] = 1; // tower
                    militaryBuildingPriorities[2] = 2; // fortress
                    minPlanksForMilitaryBuildings[0] = 12; // tower
                    minPlanksForMilitaryBuildings[1] = 15; // fortress
                    minStonesForMilitaryBuildings[0] = 8; // tower
                    minStonesForMilitaryBuildings[1] = 12; // fortress
                    break;
                case PlayerFace.Frollin:
                    Aggressivity = 1;
                    ExpandFocus = 2;
                    break;
                case PlayerFace.Kallina:
                    Aggressivity = 1;
                    ExpandFocus = 1;
                    MilitarySkill = 2;
                    PrioritizedAttackTarget = AttackTarget.FoodProduction;
                    break;
                case PlayerFace.Rasparuk:
                    Aggressivity = 1;
                    GoldFocus = 1;
                    SteelFocus = 1;
                    ConstructionMaterialFocus = 2;
                    MilitarySkill = 2;
                    DefendFocus = 1;
                    BuildingFocus = 2;
                    FoodFocus = 1;
                    militaryBuildingPriorities[0] = 1; // hut
                    militaryBuildingPriorities[1] = 2; // tower
                    militaryBuildingPriorities[2] = 0; // fortress
                    PrioritizedAttackTarget = AttackTarget.SmallMilitary;
                    PrioritizedPlayer = AttackPlayer.WorstProtected;
                    break;
                case PlayerFace.CountAldaba:
                    Aggressivity = 2;
                    MilitarySkill = 2;
                    MilitaryFocus = 1;
                    ExpandFocus = 1;
                    DefendFocus = 1;
                    GoldFocus = 1;
                    SteelFocus = 1;
                    militaryBuildingPriorities[0] = 1; // hut
                    militaryBuildingPriorities[1] = 2; // tower
                    militaryBuildingPriorities[2] = 0; // fortress
                    foodSourcePriorities[0] = 1; // fish
                    foodSourcePriorities[1] = 2; // bread
                    foodSourcePriorities[2] = 0; // meat
                    PrioritizedAttackTarget = AttackTarget.Stocks;
                    SecondPrioritizedAttackTarget = AttackTarget.SmallMilitary;
                    PrioritizedPlayer = AttackPlayer.WorstProtected;
                    break;
                case PlayerFace.KingRolph:
                    Aggressivity = 2;
                    MilitarySkill = 2;
                    MilitaryFocus = 2;
                    ExpandFocus = 1;
                    DefendFocus = 1;
                    BuildingFocus = 1;
                    GoldFocus = 1;
                    SteelFocus = 1;
                    FoodFocus = 1;
                    ConstructionMaterialFocus = 1;
                    militaryBuildingPriorities[0] = 0; // hut
                    militaryBuildingPriorities[1] = 1; // tower
                    militaryBuildingPriorities[2] = 2; // fortress
                    foodSourcePriorities[0] = 0; // fish
                    foodSourcePriorities[1] = 1; // bread
                    foodSourcePriorities[2] = 2; // meat
                    PrioritizedAttackTarget = AttackTarget.MaterialProduction;
                    SecondPrioritizedAttackTarget = AttackTarget.SmallMilitary;
                    PrioritizedPlayer = AttackPlayer.WorstProtected;
                    SecondPrioritizedPlayer = AttackPlayer.Weakest;
                    break;
                case PlayerFace.HomenDoublehorn:
                    Aggressivity = 2;
                    RushAffinity = 1;
                    MilitarySkill = 2;
                    MilitaryFocus = 2;
                    ExpandFocus = 2;
                    BuildingFocus = 1;
                    GoldFocus = 2;
                    SteelFocus = 2;
                    FoodFocus = 1;
                    militaryBuildingPriorities[0] = 1; // hut
                    militaryBuildingPriorities[1] = 2; // tower
                    militaryBuildingPriorities[2] = 0; // fortress
                    minPlanksForMilitaryBuildings[0] = 10; // tower
                    minPlanksForMilitaryBuildings[1] = 25; // fortress
                    minStonesForMilitaryBuildings[0] = 8; // tower
                    minStonesForMilitaryBuildings[1] = 15; // fortress
                    foodSourcePriorities[0] = 2; // fish
                    foodSourcePriorities[1] = 0; // bread
                    foodSourcePriorities[2] = 1; // meat
                    PrioritizedAttackTarget = AttackTarget.Stocks;
                    SecondPrioritizedAttackTarget = AttackTarget.SmallMilitary;
                    PrioritizedPlayer = AttackPlayer.WorstProtected;
                    SecondPrioritizedPlayer = AttackPlayer.Weakest;
                    break;
                case PlayerFace.Sollok:
                    Aggressivity = 2;
                    RushAffinity = 2;
                    MilitarySkill = 2;
                    MilitaryFocus = 2;
                    ExpandFocus = 2;
                    BuildingFocus = 2;
                    GoldFocus = 2;
                    SteelFocus = 2;
                    FoodFocus = 1;
                    ConstructionMaterialFocus = 2;
                    PrioritizedAttackTarget = AttackTarget.MaterialProduction;
                    SecondPrioritizedAttackTarget = AttackTarget.WeaponProduction;
                    PrioritizedPlayer = AttackPlayer.WorstProtected;
                    SecondPrioritizedPlayer = AttackPlayer.Weakest;
                    break;
                case PlayerFace.Enemy:
                    Aggressivity = 2;
                    RushAffinity = 1;
                    MilitarySkill = 2;
                    MilitaryFocus = 2;
                    ExpandFocus = 2;
                    DefendFocus = 1;
                    BuildingFocus = 2;
                    GoldFocus = 2;
                    SteelFocus = 2;
                    FoodFocus = 2;
                    ConstructionMaterialFocus = 2;
                    foodSourcePriorities[0] = 1; // fish
                    foodSourcePriorities[1] = 2; // bread
                    foodSourcePriorities[2] = 0; // meat
                    militaryBuildingPriorities[0] = 0; // hut
                    militaryBuildingPriorities[1] = 2; // tower
                    militaryBuildingPriorities[2] = 1; // fortress
                    minPlanksForMilitaryBuildings[0] = 20; // tower
                    minPlanksForMilitaryBuildings[1] = 30; // fortress
                    minStonesForMilitaryBuildings[0] = 15; // tower
                    minStonesForMilitaryBuildings[1] = 22; // fortress
                    PrioritizedAttackTarget = AttackTarget.Mines;
                    SecondPrioritizedAttackTarget = AttackTarget.MaterialProduction;
                    PrioritizedPlayer = AttackPlayer.WorstProtected;
                    SecondPrioritizedPlayer = AttackPlayer.RandomHuman;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// This is called if an enemy is in range to attack the castle.
        /// </summary>
        /// <param name="lastChance">If only the castle and a few buildings remain. Only set for better ai characters.</param>
        public void PrepareForDefendingCastle(bool lastChance)
        {
            // Order all knights to castle
            // - Set maximum knights of military buildings to minimum
            // - Order knights back to castle

            // If lastChance use drastic methods like burning down military buildings
            if (lastChance)
            {
                // Burn down all military buildings to free knights
            }
        }

        public void PrepareFights(bool quick)
        {
            // Set maximum knights of military buildings to maximum

            if (!quick)
            {
                // If bad knights are in military buildings and there are better
                // ones in inventories, then swap with better ones -> cycle knights
            }
        }

        public void HandleEmptyMine(uint mineIndex)
        {
            PushState(CreateState(State.DestroyUselessBuildings, mineIndex));
        }

        public void NotifyCraftedTool(Resource.Type tool)
        {
            var currentState = states.Peek();

            if (currentState != null && currentState is AIStates.AIStateCraftTool)
            {
                currentState.Kill(this);
            }

            if (HardTimes)
            {
                var game = player.Game;

                // First we craft a scythe or pincer and hammer.
                // After that we craft another axe and pick.

                if (player.GetSerfCount(Serf.Type.WeaponSmith) == 0)
                {
                    if (tool != Resource.Type.Pincer && !game.HasAnyOfResource(player, Resource.Type.Pincer))
                    {
                        player.SetFullToolPriority(Resource.Type.Pincer);
                        return;
                    }
                    else if (tool != Resource.Type.Hammer && !game.HasAnyOfResource(player, Resource.Type.Hammer))
                    {
                        player.SetFullToolPriority(Resource.Type.Hammer);
                        return;
                    }
                }

                if (tool != Resource.Type.Scythe && player.GetSerfCount(Serf.Type.Farmer) == 0 && !game.HasAnyOfResource(player, Resource.Type.Scythe))
                {
                    player.SetFullToolPriority(Resource.Type.Scythe);
                    return;
                }

                if (tool != Resource.Type.Axe && player.GetSerfCount(Serf.Type.Lumberjack) < 2 && !game.HasAnyOfResource(player, Resource.Type.Axe))
                {
                    player.SetFullToolPriority(Resource.Type.Axe);
                    return;
                }

                if (tool != Resource.Type.Pick && player.GetSerfCount(Serf.Type.Stonecutter) + player.GetSerfCount(Serf.Type.Miner) < 4 && !game.HasAnyOfResource(player, Resource.Type.Pick))
                {
                    player.SetFullToolPriority(Resource.Type.Pick);
                    return;
                }
            }

            player.ResetToolPriority();
        }

        static bool CheckLinkedBuildings(Building.Type type1, Building.Type type2, uint distance)
        {
            if (distance < 6 && type2 == Building.Type.Castle)
                return true; // prioritize a nearby castle

            switch (type1)
            {
                case Building.Type.Baker:
                    return type2 == Building.Type.Mill;
                case Building.Type.Butcher:
                    return type2 == Building.Type.PigFarm;
                case Building.Type.Castle:
                    return type2 == Building.Type.WeaponSmith || type2 == Building.Type.GoldSmelter;
                case Building.Type.CoalMine:
                    return type2 == Building.Type.WeaponSmith || type2 == Building.Type.SteelSmelter || type2 == Building.Type.GoldSmelter;
                case Building.Type.Farm:
                    return type2 == Building.Type.Mill || type2 == Building.Type.PigFarm;
                case Building.Type.GoldMine:
                    return type2 == Building.Type.GoldSmelter;
                case Building.Type.GoldSmelter:
                    return type2 == Building.Type.GoldMine || type2 == Building.Type.CoalMine || type2 == Building.Type.Stock || type2 == Building.Type.Castle;
                case Building.Type.IronMine:
                    return type2 == Building.Type.SteelSmelter;
                case Building.Type.Lumberjack:
                    return type2 == Building.Type.Sawmill;
                case Building.Type.Mill:
                case Building.Type.PigFarm:
                    return type2 == Building.Type.Farm;
                case Building.Type.Sawmill:
                    return type2 == Building.Type.Lumberjack || type2 == Building.Type.ToolMaker;
                case Building.Type.SteelSmelter:
                    return type2 == Building.Type.CoalMine || type2 == Building.Type.IronMine || type2 == Building.Type.ToolMaker || type2 == Building.Type.WeaponSmith;
                case Building.Type.Stock:
                    return type2 == Building.Type.WeaponSmith || type2 == Building.Type.GoldSmelter;
                case Building.Type.ToolMaker:
                    return type2 == Building.Type.Sawmill || type2 == Building.Type.SteelSmelter;
                case Building.Type.WeaponSmith:
                    return type2 == Building.Type.CoalMine || type2 == Building.Type.SteelSmelter || type2 == Building.Type.Stock || type2 == Building.Type.Castle;
            }

            return false;
        }

        Map.FindData FindFlag(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = map.HasFlag(position),
                Data = position
            };
        }

        Map.FindData FindPath(Map map, MapPos position)
        {
            return new Map.FindData()
            {
                Success = map.Paths(position) != 0,
                Data = position
            };
        }

        bool BuildingNeedsInventoryResIn(Building.Type type)
        {
            switch (type)
            {
                case Building.Type.Forester:
                case Building.Type.Fortress:
                case Building.Type.Hut:
                case Building.Type.None:
                case Building.Type.Stock:
                case Building.Type.Tower:
                    return false;
                default:
                    return true;
            }
        }

        bool BuildingNeedsInventoryResOut(Building.Type type)
        {
            switch (type)
            {
                case Building.Type.CoalMine:
                case Building.Type.Farm:
                case Building.Type.Fisher:
                case Building.Type.Forester:
                case Building.Type.Fortress:
                case Building.Type.GoldMine:
                case Building.Type.Hut:
                case Building.Type.IronMine:
                case Building.Type.Lumberjack:
                case Building.Type.None:
                case Building.Type.Stock:
                case Building.Type.Stonecutter:
                case Building.Type.StoneMine:
                case Building.Type.Tower:
                    return false;
                default:
                    return true;
            }
        }

        internal bool CanLinkFlag(uint flagPosition, bool noWater = true)
        {
            var map = player.Game.Map;
            // Note: For now this only checks flags in a search range of 9
            var flags = map.FindInArea(flagPosition, 9, FindFlag, 2);

            if (flags.Count == 0)
                return false;

            foreach (var flag in flags)
            {
                if (Pathfinder.FindShortestPath(map, flagPosition, (uint)flag, null, 15).Length != 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Links the flag to the road system.
        /// </summary>
        /// <param name="flag">The flag to link</param>
        /// <param name="maxLength">Build only if the best connection length is at max this</param>
        /// <param name="allowWater">If true the connection could be a water path</param>
        internal bool LinkFlag(Flag flag, int maxLength = 9, bool allowWater = false)
        {
            if (maxLength < 2)
                return false;

            Road bestRoad = null;
            uint bestRoadTotalCost = uint.MaxValue;
            uint costAdd = 0;
            var game = player.Game;
            var buildingType = flag.HasBuilding ? flag.Building.BuildingType : Building.Type.None;

            var flags = (maxLength < 10)
                ? game.Map.FindInArea(flag.Position, maxLength, FindFlag, 2).Select(position => game.GetFlagAtPosition((MapPos)position))
                : game.GetPlayerFlags(player).ToList();

            foreach (var otherFlag in flags)
            {
                if (flag.Position == otherFlag.Position)
                    continue; // not link to self

                if (flag.HasDirectConnectionTo(otherFlag)) // TODO: maybe let this happen in special cases later
                    continue; // not link if already linked

                int distance = game.Map.Distance(flag.Position, otherFlag.Position);

                if (distance > maxLength) // too far away
                    continue;

                uint flagCost = otherFlag.GetCostToNearestInventory(BuildingNeedsInventoryResIn(buildingType), BuildingNeedsInventoryResOut(buildingType));

                if (flagCost == uint.MaxValue)
                    continue; // flag has no connection to an inventory

                var road = Pathfinder.FindShortestPath(game.Map, flag.Position, otherFlag.Position, null, 15);

                if (road != null && road.Valid)
                {
                    if (road.Length > maxLength)
                        continue;

                    if (!allowWater && road.IsWaterPath(game.Map))
                        continue;

                    if (buildingType != Building.Type.None && otherFlag.HasBuilding && CheckLinkedBuildings(buildingType, otherFlag.Building.BuildingType, road.Length))
                    {
                        if (bestRoad != null && flagCost + road.Cost >= bestRoadTotalCost + costAdd)
                            continue;

                        bestRoad = road;
                        bestRoadTotalCost = flagCost + road.Cost;
                        costAdd = 750u;

                        if (bestRoad.Cost <= costAdd)
                            break;
                    }

                    if (bestRoad == null || flagCost + road.Cost < bestRoadTotalCost - costAdd)
                    {
                        bestRoad = road;
                        bestRoadTotalCost = flagCost + road.Cost;
                    }
                }
            }

            if (bestRoad == null)
            {
                // Could not find a valid flag to link to.
                if (maxLength < 6) // Only link larger paths.
                    return false;

                // Try to build one on a nearby path.
                return LinkFlagToNearbyPath(game, flag, maxLength, allowWater);
            }

            return game.BuildRoad(bestRoad, player);
        }

        bool LinkFlagToNearbyPath(Game game, Flag flag, int maxLength, bool allowWater)
        {
            // TODO: Don't link to an existing road that leads to this flag!

            if (maxLength > 6)
                maxLength = 6;

            var paths = game.Map.FindInArea(flag.Position, maxLength, FindPath, 2);
            Road bestRoad = null;
            uint bestRoadEndPosition = Global.INVALID_MAPPOS;

            foreach (var path in paths)
            {
                var position = (MapPos)path;

                if (!game.CanBuildFlag(position, player))
                    continue;

                var road = Pathfinder.FindShortestPath(game.Map, flag.Position, position, null, 15);

                if (road != null && road.Valid)
                {
                    if (road.Length > maxLength)
                        continue;

                    if (!allowWater && road.IsWaterPath(game.Map))
                        continue;

                    if (bestRoad == null || road.Cost < bestRoad.Cost)
                    {
                        bestRoad = road;
                        bestRoadEndPosition = position;
                    }
                }
            }

            if (bestRoad == null)
                return false;

            return game.BuildFlag(bestRoadEndPosition, player) && game.BuildRoad(bestRoad, player);
        }

        /// <summary>
        /// This includes knights, sword/shield pairs or a weapon smith
        /// </summary>
        /// <returns></returns>
        internal bool HasRequirementsForKnights(Game game)
        {
            if (player.GetTotalBuildingCount(Building.Type.WeaponSmith) != 0)
                return true;

            return game.GetPossibleFreeKnightCount(player) != 0;
        }

        internal AIState CreateState(State state, object param = null)
        {
            switch (state)
            {
                case State.Idle:
                    return new AIStates.AIStateIdle();
                case State.ChooseCastleLocation:
                    return new AIStates.AIStateChooseCastleLocation();
                case State.CastleBuilt:
                    return new AIStates.AIStateCastleBuilt();
                case State.BuildBuilding:
                    return new AIStates.AIStateBuildBuilding(param == null ? Building.Type.None : (Building.Type)param);
                case State.LinkBuilding:
                    return new AIStates.AIStateLinkBuilding(param == null ? 0u : (uint)param);
                case State.LinkDisconnectedFlags:
                    return new AIStates.AIStateLinkDisconnectedFlags();
                case State.CheckNeededBuilding:
                    return new AIStates.AIStateCheckNeededBuilding();
                case State.CraftTool:
                    // TODO: When loading a save game, is player and player.Game already valid when loading the state?
                    return new AIStates.AIStateCraftTool(player.Game, player, param == null ? Resource.Type.None : (Resource.Type)param);
                case State.CraftWeapons:
                    return new AIStates.AIStateCraftWeapons();
                case State.FindOre:
                    return new AIStates.AIStateFindMinerals(param == null ? Map.Minerals.None : (Map.Minerals)param);
                case State.AdjustSettings:
                    return new AIStates.AIStateAdjustSettings();
                case State.Attack:
                    return new AIStates.AIStateAttack();
                case State.AvoidCongestion:
                    return new AIStates.AIStateAvoidCongestion();
                case State.DestroyUselessBuildings:
                    if (param == null)
                        return new AIStates.AIStateDestroyUselessBuildings();
                    else
                        return new AIStates.AIStateDestroyUselessBuildings((uint)param);
            }

            throw new ExceptionFreeserf(player.Game, ErrorSystemType.AI, "Unknown AI state");
        }

        /// <summary>
        /// Create an AI state that is active after the given delay.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="delay">Delay in milliseconds</param>
        /// <param name="param"></param>
        /// <returns></returns>
        internal AIState CreateDelayedState(State state, int delay, object param = null)
        {
            var aiState = CreateState(state, param);

            aiState.Delay = delay * Global.TICKS_PER_SEC / 1000;

            return aiState;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="minDelay">Minimum delay in milliseconds</param>
        /// <param name="maxDelay">Maximum delay in milliseconds</param>
        /// <param name="param"></param>
        /// <returns></returns>
        internal AIState CreateRandomDelayedState(State state, int minDelay, int maxDelay, object param = null)
        {
            int delay = minDelay + random.Next() % (maxDelay + 1 - minDelay);

            return CreateDelayedState(state, delay, param);
        }

        internal void PushState(AIState state)
        {
            states.Push(state);
        }

        /// <summary>
        /// Note the first pushed will be on top.
        /// So the order of the parameters will also
        /// be the execution order of the states.
        /// </summary>
        /// <param name="states"></param>
        internal void PushStates(params AIState[] states)
        {
            for (int i = states.Length - 1; i >= 0; --i)
                PushState(states[i]);
        }

        internal AIState PopState()
        {
            if (states.Count == 0)
                return null;

            return states.Pop();
        }

        internal void ClearStates()
        {
            states.Clear();
        }

        internal bool ContainsState(AIState state)
        {
            return states.Contains(state);
        }

        internal bool StupidDecision()
        {
            if (HardTimes) // no stupid decisions in hard times (the game would be quickly over otherwise)
                return false;

            return random.Next() > 42000 + (int)playerInfo.Intelligence * 500;
        }

        internal bool Chance(int percentage)
        {
            if (percentage == 100)
                return true;

            return random.Next() % 100 < percentage;
        }


        #region Game analysis helper functions

        internal bool HasEssentialBuildings()
        {
            var game = player.Game;

            return
                player.GetTotalBuildingCount(Building.Type.Lumberjack) != 0 &&
                player.GetTotalBuildingCount(Building.Type.Stonecutter) != 0 &&
                player.GetTotalBuildingCount(Building.Type.Sawmill) != 0;
        }

        internal bool HasResourcesForBuilding(Building.Type type)
        {
            var game = player.Game;
            var constructionInfo = Building.ConstructionInfos[(int)type];

            return
                game.GetResourceAmountInInventories(player, Resource.Type.Plank) >= constructionInfo.Planks &&
                game.GetResourceAmountInInventories(player, Resource.Type.Stone) >= constructionInfo.Stones;
        }

        void UpdateHardTimes(bool hardTimes)
        {
            lastHardTime = hardTimes;

            if (hardTimes)
                nextHardTimeCheck = GameTime + 20 * Global.TICKS_PER_SEC;
            else
                nextHardTimeCheck = GameTime + 10 * Global.TICKS_PER_SEC;
        }

        // This is the case if the AI has to do everything right to survive.
        // Especially if starting with very few supplies.
        internal bool HardTimes
        {
            get
            {
                if (GameTime < nextHardTimeCheck)
                    return lastHardTime;

                var game = player.Game;

                uint numMiners = player.GetSerfCount(Serf.Type.Miner);
                uint numPicks = game.GetResourceAmountInInventories(player, Resource.Type.Pick);
                uint numWeaponSmiths = player.GetSerfCount(Serf.Type.WeaponSmith);

                if (game.GetPossibleFreeKnightCount(player) >= 10 && numPicks > 0 &&
                    game.GetResourceAmountInInventories(player, Resource.Type.Plank) >= 12 &&
                    game.GetResourceAmountInInventories(player, Resource.Type.Stone) >= 8)
                {
                    UpdateHardTimes(false);
                    return false;
                }

                if (numMiners + numPicks > 2 && numWeaponSmiths > 0)
                {
                    UpdateHardTimes(false);
                    return false;
                }

                bool hasCoalMines = player.GetTotalBuildingCount(Building.Type.CoalMine) != 0;
                bool hasIronMines = player.GetTotalBuildingCount(Building.Type.IronMine) != 0;

                if (hasCoalMines && hasIronMines && numMiners >= 2)
                {
                    bool hasFoodSource = player.GetTotalBuildingCount(Building.Type.Fisher) != 0 ||
                        player.GetSerfCount(Serf.Type.Farmer) != 0;

                    if (hasFoodSource && numWeaponSmiths != 0)
                    {
                        UpdateHardTimes(false);
                        return false;
                    }
                }

                UpdateHardTimes(true);

                return true;
            }
        }

        #endregion


        public void Update(Game game)
        {
            if (lastTick == 0)
            {
                lastTick = game.Tick;
                GameTime += game.Tick;
            }
            else if (game.Tick < lastTick) // overflow
            {
                GameTime += ushort.MaxValue - lastTick;
                GameTime += game.Tick;
            }
            else
            {
                GameTime += game.Tick - lastTick;
            }

            if (states.Count == 0)
            {
                if (!player.HasCastle)
                {
                    PushState(CreateState(State.ChooseCastleLocation));
                }
                else
                {
                    PushState(CreateState(State.CastleBuilt));
                }
            }

            var currentState = states.Peek();

            if (currentState == null)
            {
                // continue with next state
                states.Pop();
                Update(game);
                return;
            }

            if (currentState.Delay > 0)
                currentState.Delay -= (game.Tick - lastTick);
            else if (GameTime - lastUpdate >= Global.TICKS_PER_SEC) // only update every second
            {
                currentState.Update(this, game, player, playerInfo, (int)(GameTime - lastUpdate));
                lastUpdate = GameTime;
            }

            if (currentState.Killed)
            {
                if (currentState.NextState != null && !currentState.NextState.Killed)
                    states.Push(currentState.NextState);
            }

            lastTick = game.Tick;
        }

        public static AI Read(SaveReaderText reader, Game game, bool firstAI)
        {
            var player = game.GetPlayer(reader.Value("player").ReadUInt());
            var character = reader.Value("character").ReadEnum<PlayerFace>();
            var supplies = reader.Value("supplies").ReadUInt();
            var intelligence = reader.Value("intelligence").ReadUInt();
            var reproduction = reader.Value("reproduction").ReadUInt();
            var playerInfo = new PlayerInfo(character, PlayerInfo.PlayerColors[player.Index], intelligence, supplies, reproduction);

            var ai = new AI(player, playerInfo);

            ai.ReadFrom(reader, game, firstAI);

            return ai;
        }

        void ReadFrom(SaveReaderText reader, Game game, bool firstAI)
        {
            int numStates = reader.Value("num_states").ReadInt();

            for (int i = 0; i < numStates; ++i)
            {
                var state = AIState.Read(game, this, "state" + i, reader);

                if (state != null)
                    PushState(state);
            }

            lastTick = reader.Value("last_tick").ReadInt();
            lastUpdate = reader.Value("last_update").ReadLong();
            GameTime = reader.Value("game_time").ReadLong();
            random = new Random(reader.Value("random").ReadString());

            CanAttack = reader.Value("can_attack").ReadBool();
            CanExpand = reader.Value("can_expand").ReadBool();
            MaxMilitaryBuildings = reader.Value("max_military_buildings").ReadInt();

            // first AI stores the mineral memory
            if (firstAI)
            {
                ClearMemory();

                foreach (Map.Minerals mineral in Enum.GetValues(typeof(Map.Minerals)))
                {
                    if (mineral == Map.Minerals.None)
                        continue;

                    string name = "mineral_" + Enum.GetName(typeof(Map.Minerals), mineral).ToLower();
                    int index = 0;

                    while (true)
                    {
                        string spotName = name + "_" + index;

                        if (reader.HasValue(spotName + "_pos") && reader.HasValue(spotName + "_large"))
                        {
                            var position = reader.Value(spotName + "_pos").ReadUInt();
                            var large = reader.Value(spotName + "_large").ReadBool();
                            var spot = new MineralSpot()
                            {
                                Position = position,
                                Large = large
                            };

                            memorizedMineralSpots[mineral].Add(spot);
                        }
                        else
                        {
                            break;
                        }

                        ++index;
                    }
                }
            }

            Init();
        }

        public void WriteTo(SaveWriterText writer, bool firstAI)
        {
            writer.Value("player").Write(player.Index);
            writer.Value("character").Write((int)playerInfo.Face);
            writer.Value("supplies").Write(playerInfo.Supplies);
            writer.Value("intelligence").Write(playerInfo.Intelligence);
            writer.Value("reproduction").Write(playerInfo.Reproduction);

            writer.Value("num_states").Write(states.Count);
            int stateIndex = 0;

            foreach (var state in states)
                state.WriteTo("state" + stateIndex++, writer);

            writer.Value("last_tick").Write(lastTick);
            writer.Value("last_update").Write(lastUpdate);
            writer.Value("game_time").Write(GameTime);
            writer.Value("random").Write(random.ToString());

            writer.Value("can_attack").Write(CanAttack);
            writer.Value("can_expand").Write(CanExpand);
            writer.Value("max_military_buildings").Write(MaxMilitaryBuildings);

            // first AI stores the mineral memory
            if (firstAI)
            {
                foreach (KeyValuePair<Map.Minerals, List<MineralSpot>> memorizedMineralSpot in memorizedMineralSpots)
                {
                    string name = "mineral_" + Enum.GetName(typeof(Map.Minerals), memorizedMineralSpot.Key).ToLower();
                    int index = 0;

                    foreach (var mineralSpot in memorizedMineralSpot.Value)
                    {
                        string spotName = name + "_" + index++;
                        writer.Value(spotName + "_pos").Write(mineralSpot.Position);
                        writer.Value(spotName + "_large").Write(mineralSpot.Position);
                    }
                }
            }
        }
    }

    public class IntroAI : AI
    {
        public IntroAI(Player player, PlayerInfo playerInfo)
            : base(player, playerInfo)
        {
            CanAttack = false;
            CanExpand = true;
            MaxMilitaryBuildings = 2;
        }
    }
}
