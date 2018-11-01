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

        int[] tool_prio = new int[9];
        int[] resource_count = new int[26];
        int[] flag_prio = new int[26];
        int[] serf_count = new int[27];
        int[] knight_occupation = new int[4];

        Color color;
        uint face;
        int flags;
        int build;
        int[] completed_building_count = new int[24];
        int[] incomplete_building_count = new int[24];
        int[] inventory_prio = new int[26];
        int[] attacking_buildings = new int[64];

        Messages messages = new Messages();
        PosTimers timers = new PosTimers();

        int building;
        int castle_inventory;
        int cont_search_after_non_optimal_find;
        int knights_to_spawn;
        uint total_land_area;
        uint total_building_score;
        uint total_military_score;
        ushort last_tick;

        int reproduction_counter;
        uint reproduction_reset;
        int serf_to_knight_rate;
        ushort serf_to_knight_counter; /* Overflow is important */
        int analysis_goldore;
        int analysis_ironore;
        int analysis_coal;
        int analysis_stone;

        int food_stonemine; /* Food delivery priority of food for mines. */
        int food_coalmine;
        int food_ironmine;
        int food_goldmine;
        int planks_construction; /* Planks delivery priority. */
        int planks_boatbuilder;
        int planks_toolmaker;
        int steel_toolmaker;
        int steel_weaponsmith;
        int coal_steelsmelter;
        int coal_goldsmelter;
        int coal_weaponsmith;
        int wheat_pigfarm;
        int wheat_mill;

        /* +1 for every castle defeated,
           -1 for own castle lost. */
        int castle_score;
        int send_generic_delay;
        uint initial_supplies;
        int serf_index;
        int knight_cycle_counter;
        int send_knight_delay;
        int military_max_gold;

        int knight_morale;
        int gold_deposited;
        int castle_knights_wanted;
        int castle_knights;
        int ai_value_0;
        int ai_value_1;
        int ai_value_2;
        int ai_value_3;
        int ai_value_4;
        int ai_value_5;
        uint ai_intelligence;

        int[,] player_stat_history = new int[16,112];
        int[,] resource_count_history = new int[26,120];

        // TODO(Digger): remove it to UI
        public int building_attacked;
        public int knights_attacking;
        public int attacking_building_count;
        public int[] attacking_knights = new int[4];
        public int total_attacking_knights;
        public uint temp_index;

        public Player(Game game, uint index)
            : base(game, index)
        {

        }

        public void init(uint intelligence, uint supplies, uint reproduction)
        {

        }

        public void init_view(Color color, uint face)
        {

        }

        public Color get_color()
        {
            return color;
        }

        public uint get_face()
        {
            return face;
        }

        /* Whether player has built the initial castle. */
        public bool has_castle() { return (flags & 1); }
        /* Whether the strongest knight should be sent to fight. */
        public bool send_strongest() { return ((flags >> 1) & 1); }
        public void drop_send_strongest() { flags &= ~BIT(1); }
        public void set_send_strongest() { flags |= BIT(1); }
        /* Whether cycling of knights is in progress. */
        public bool cycling_knight() { return ((flags >> 2) & 1); }
        /* Whether a message is queued for this player. */
        public bool has_message() { return ((flags >> 3) & 1); }
        public void drop_message() { flags &= ~BIT(3); }
        /* Whether the knight level of military buildings is temporarily
        reduced bacause of cycling of the knights. */
        public bool reduced_knight_level() { return ((flags >> 4) & 1); }
        /* Whether the cycling of knights is in the second phase. */
        public bool cycling_second() const { return ((flags >> 5) & 1); }
        /* Whether this player is a computer controlled opponent. */
        public bool is_ai() const { return ((flags >> 7) & 1); }

        /* Whether player is prohibited from building military
        buildings at current position. */
        public bool allow_military() const { return !(build & 1); }
        /* Whether player is prohibited from building flag at
        current position. */
        public bool allow_flag() const { return !((build >> 1) & 1); }
        /* Whether player can spawn new serfs. */
        public bool can_spawn() const { return ((build >> 2) & 1); }

        public unsigned int get_serf_count(int type) const { return serf_count[type]; }
        public int get_flag_prio(int res) const { return flag_prio[res]; }

        public void add_notification(Message::Type type, MapPos pos, unsigned int data);

        public bool has_notification();

        public Message pop_notification();

        public Message peek_notification();


        public void add_timer(int timeout, MapPos pos);


        public void reset_food_priority();

        public void reset_planks_priority();

        public void reset_steel_priority();

        public void reset_coal_priority();

        public void reset_wheat_priority();

        public void reset_tool_priority();


        public void reset_flag_priority();

        public void reset_inventory_priority();


        public int get_knight_occupation(size_t threat_level) const {
        public return knight_occupation[threat_level]; }
        public void change_knight_occupation(int index, int adjust_max, int delta);

        public void increase_castle_knights() { castle_knights++; }

        public void decrease_castle_knights() { castle_knights--; }

        public int get_castle_knights() const { return castle_knights; }
        public int get_castle_knights_wanted() const { return castle_knights_wanted; }
        public void increase_castle_knights_wanted();

        public void decrease_castle_knights_wanted();

        public int get_knight_morale() const { return knight_morale; }
        public int get_gold_deposited() const { return gold_deposited; }

        public int promote_serfs_to_knights(int number);

        public int knights_available_for_attack(MapPos pos);

        public void start_attack();

        public void cycle_knights();


        public void create_initial_castle_serfs(Building* castle);

        public Serf* spawn_serf_generic();

        public int spawn_serf(Serf** serf, Inventory** inventory, bool want_knight);

        public bool tick_send_generic_delay();

        public bool tick_send_knight_delay();

        public Serf::Type get_cycling_sert_type(Serf::Type type) const;


        public void increase_serf_count(unsigned int type) { serf_count[type]++; }

        public void decrease_serf_count(unsigned int type);

        public int* get_serfs() { return reinterpret_cast<int*>(serf_count); }


        public void increase_res_count(unsigned int type) { resource_count[type]++; }

        public void decrease_res_count(unsigned int type) { resource_count[type]--; }


        public void building_founded(Building* building);

        public void building_built(Building* building);

        public void building_captured(Building* building);

        public void building_demolished(Building* building);


        public int get_completed_building_count(int type)
        {
            return completed_building_count[type];
        }

        public int get_incomplete_building_count(int type)
        {
            return incomplete_building_count[type];
        }

        public int get_tool_prio(int type) const { return tool_prio[type]; }
        public void set_tool_prio(int type, int prio) { tool_prio[type] = prio; }

        public int* get_flag_prio() { return flag_prio; }

        public int get_inventory_prio(int type) const { return inventory_prio[type]; }
        public int* get_inventory_prio() { return inventory_prio; }

        public int get_total_military_score() const { return total_military_score; }

        public void update();

        public void update_stats(int res);

        // Stats
        public void update_knight_morale();

        public int get_land_area() const { return total_land_area; }
        public void increase_land_area() { total_land_area++; }

        public void decrease_land_area() { total_land_area--; }

        public int get_building_score() const { return total_building_score; }
        public int get_military_score() const;

        public void increase_military_score(int val) { total_military_score += val; }

        public void decrease_military_score(int val) { total_military_score -= val; }

        public void increase_military_max_gold(int val) { military_max_gold += val; }

        public int get_score() const;

        public unsigned int get_initial_supplies() const { return initial_supplies; }
  
        public int* get_resource_count_history(Resource::Type type)
        {
            return resource_count_history[type];
        }

        public void set_player_stat_history(int mode, int ind, int val)
        {
            player_stat_history[mode][ind] = val;
        }

        public int* get_player_stat_history(int mode) { return player_stat_history[mode]; }

        public ResourceMap get_stats_resources()
        {

        }

        public Serf::SerfMap get_stats_serfs_idle()
        {

        }

        public Serf::SerfMap get_stats_serfs_potential()
        {

        }

        // Settings
        public int get_serf_to_knight_rate() const { return serf_to_knight_rate; }
        public void set_serf_to_knight_rate(int rate) { serf_to_knight_rate = rate; }

        public unsigned int get_food_for_building(unsigned int bld_type) const;

        public int get_food_stonemine() const { return food_stonemine; }
        public void set_food_stonemine(int val) { food_stonemine = val; }

        public int get_food_coalmine() const { return food_coalmine; }
        public void set_food_coalmine(int val) { food_coalmine = val; }

        public int get_food_ironmine() const { return food_ironmine; }
        public void set_food_ironmine(int val) { food_ironmine = val; }

        public int get_food_goldmine() const { return food_goldmine; }
        public void set_food_goldmine(int val) { food_goldmine = val; }

        public int get_planks_construction() const { return planks_construction; }
        public void set_planks_construction(int val) { planks_construction = val; }

        public int get_planks_boatbuilder() const { return planks_boatbuilder; }
        public void set_planks_boatbuilder(int val) { planks_boatbuilder = val; }

        public int get_planks_toolmaker() const { return planks_toolmaker; }
        public void set_planks_toolmaker(int val) { planks_toolmaker = val; }

        public int get_steel_toolmaker() const { return steel_toolmaker; }
        public void set_steel_toolmaker(int val) { steel_toolmaker = val; }

        public int get_steel_weaponsmith() const { return steel_weaponsmith; }
        public void set_steel_weaponsmith(int val) { steel_weaponsmith = val; }

        public int get_coal_steelsmelter() const { return coal_steelsmelter; }
        public void set_coal_steelsmelter(int val) { coal_steelsmelter = val; }

        public int get_coal_goldsmelter() const { return coal_goldsmelter; }
        public void set_coal_goldsmelter(int val) { coal_goldsmelter = val; }

        public int get_coal_weaponsmith() const { return coal_weaponsmith; }
        public void set_coal_weaponsmith(int val) { coal_weaponsmith = val; }

        public int get_wheat_pigfarm() const { return wheat_pigfarm; }
        public void set_wheat_pigfarm(int val) { wheat_pigfarm = val; }

        public int get_wheat_mill() const { return wheat_mill; }
        public void set_wheat_mill(int val) { wheat_mill = val; }

        protected void init_ai_values(size_t face)
        {

        }

        protected int available_knights_at_pos(MapPos pos, int index, int dist)
        {

        }
    }
}
