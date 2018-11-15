using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf
{
    using ResourceMap = Dictionary<Resource.Type, uint>;

    internal class PopupBox : GuiObject
    {
        public enum Type
        {
            Map = 1,
            MapOverlay, /* UNUSED */
            MineBuilding,
            BasicBld,
            BasicBldFlip,
            Adv1Bld,
            Adv2Bld,
            StatSelect,
            Stat4,
            StatBld1,
            StatBld2,
            StatBld3,
            StatBld4,
            Stat8,
            Stat7,
            Stat1,
            Stat2,
            Stat6,
            Stat3,
            StartAttack,
            StartAttackRedraw,
            GroundAnalysis,
            LoadArchive,
            LoadSave,
            Type25,
            DiskMsg,
            SettSelect,
            Sett1,
            Sett2,
            Sett3,
            KnightLevel,
            Sett4,
            Sett5,
            QuitConfirm,
            NoSaveQuitConfirm,
            SettSelectFile, /* UNUSED */
            Options,
            CastleRes,
            MineOutput,
            OrderedBld,
            Defenders,
            TransportInfo,
            CastleSerf,
            ResDir,
            Sett8,
            Sett6,
            Bld1,
            Bld2,
            Bld3,
            Bld4,
            Message,
            BldStock,
            PlayerFaces,
            GameEnd,
            Demolish,
            JsCalib,
            JsCalibUpLeft,
            JsCalibDownRight,
            JsCalibCenter,
            CtrlsInfo
        }

        public enum BackgroundPattern
        {
            StripedGreen = 129,   // \\\.
            DiagonalGreen = 310,  // xxx
            CheckerdDiagonalBrown = 311,  // xxx
            PlaidAlongGreen = 312,  // ###
            StaresGreen = 313,       // * *
            SquaresGreen = 314,
            Construction = 131,  // many dots
            OverallComparison = 132,  // sward + building + land
            RuralProperties = 133,  // land
            Buildings = 134,  // buildings
            CombatPower = 135,  // sward
            Fish = 138,
            Pig = 139,
            Meat = 140,
            Eheat = 141,
            Flour = 142,
            Bread = 143,
            Lumber = 144,
            Plank = 145,
            Boat = 146,
            Stone = 147,
            Ironore = 148,
            Steel = 149,
            Coal = 150,
            Goldore = 151,
            Goldbar = 152,
            Shovel = 153,
            Hammer = 154,
            Rod = 155,
            Cleaver = 156,
            Scythe = 157,
            Axe = 158,
            Saw = 159,
            Pick = 160,
            Pincer = 161,
            Sword = 162,
            Shield = 163
        }

        Interface interf;
        ListSavedFiles fileList;
        TextInput fileField;

        public Type Box { get; }
        public MinimapGame MiniMap { get; }

        int current_sett_5_item;
        int current_sett_6_item;
        int current_stat_7_item;
        int current_stat_8_mode;

        public PopupBox(Interface interf)
        {
            this.interf = interf;
        }

        public void Show(Type box)
        {

        }

        public void Hide()
        {

        }

        void draw_popup_box_frame()
		{
			
		}

        void draw_popup_icon(int x, int y, int sprite)
		{
			
		}

        void draw_popup_building(int x, int y, int sprite)
		{
			
		}

        void draw_box_background(BackgroundPattern sprite)
		{
			
		}

        void draw_box_row(int sprite, int y)
		{
			
		}

        void draw_green_string(int x, int y, string str)
		{
			
		}

        void draw_green_number(int x, int y, int n)
		{
			
		}

        void draw_green_large_number(int x, int y, int n)
		{
			
		}

        void draw_additional_number(int x, int y, int n)
		{
			
		}

        uint get_player_face_sprite(uint face)
		{
            return 0;
		}

        void draw_player_face(int x, int y, int player)
		{
			
		}

        void draw_custom_bld_box(int[] sprites)
		{
			
		}

        void draw_custom_icon_box(int[] sprites)
		{
			
		}

        string prepare_res_amount_text(int amount)
		{
            return "";
        }

        void draw_map_box()
		{
			
		}

        void draw_mine_building_box()
		{
			
		}

        void draw_basic_building_box(int flip)
		{
			
		}

        void draw_adv_1_building_box()
		{
			
		}

        void draw_adv_2_building_box()
		{
			
		}

        void draw_resources_box(ResourceMap resources)
		{
			
		}

        void draw_serfs_box(int[] serfs, int total)
		{
			
		}

        void draw_stat_select_box()
		{
			
		}

        void draw_stat_4_box()
		{
			
		}

        void draw_building_count(int x, int y, int type)
		{
			
		}

        void draw_stat_bld_1_box()
		{
			
		}

        void draw_stat_bld_2_box()
		{
			
		}

        void draw_stat_bld_3_box()
		{
			
		}

        void draw_stat_bld_4_box()
		{
			
		}

        void draw_player_stat_chart(int[] data, int index, Player.Color color)
		{
			
		}

        void draw_stat_8_box()
		{
			
		}

        void draw_stat_7_box()
		{
			
		}

        void draw_gauge_balance(int x, int y, uint value, uint count)
		{
			
		}

        void draw_gauge_full(int x, int y, uint value, uint count)
		{
			
		}

        void draw_stat_1_box()
		{
			
		}

        void draw_stat_2_box()
		{
			
		}

        void draw_stat_6_box()
		{
			
		}

        void draw_stat_3_meter(int x, int y, int value)
		{
			
		}

        void draw_stat_3_box()
		{
			
		}

        void draw_start_attack_redraw_box()
		{
			
		}

        void draw_start_attack_box()
		{
			
		}

        void draw_ground_analysis_box()
		{
			
		}

        void draw_sett_select_box()
		{
			
		}

        void draw_slide_bar(int x, int y, int value)
		{
			
		}

        void draw_sett_1_box()
		{
			
		}

        void draw_sett_2_box()
		{
			
		}

        void draw_sett_3_box()
		{
			
		}

        void draw_knight_level_box()
		{
			
		}

        void draw_sett_4_box()
		{
			
		}

        void draw_popup_resource_stairs(int[] order)
		{
			
		}

        void draw_sett_5_box()
		{
			
		}

        void draw_quit_confirm_box()
		{
			
		}

        void draw_no_save_quit_confirm_box()
		{
			
		}

        void draw_options_box()
		{
			
		}

        void draw_castle_res_box()
		{
			
		}

        void draw_mine_output_box()
		{
			
		}

        void draw_ordered_building_box()
		{
			
		}

        void draw_defenders_box()
		{
			
		}

        void draw_transport_info_box()
		{
			
		}

        void draw_castle_serf_box()
		{
			
		}

        void draw_resdir_box()
		{
			
		}

        void draw_sett_8_box()
		{
			
		}

        void draw_sett_6_box()
		{
			
		}

        void draw_bld_1_box()
		{
			
		}

        void draw_bld_2_box()
		{
			
		}

        void draw_bld_3_box()
		{
			
		}

        void draw_bld_4_box()
		{
			
		}

        void draw_building_stock_box()
		{
			
		}

        void draw_player_faces_box()
		{
			
		}

        void draw_demolish_box()
		{
			
		}

        void draw_save_box()
		{
			
		}

        void activate_sett_5_6_item(int index)
		{
			
		}

        void move_sett_5_6_item(int up, int to_end)
		{
			
		}

        void handle_send_geologist()
		{
			
		}

        void sett_8_train(int number)
		{
			
		}

        void set_inventory_resource_mode(int mode)
		{
			
		}

        void set_inventory_serf_mode(int mode)
		{
			
		}


        void handle_action(int action, int x, int y)
		{
			
		}

        int handle_clickmap(int x, int y, int[] clkmap)
		{
            return 0;
		}

        void handle_box_close_clk(int x, int y)
		{
			
		}

        void handle_box_options_clk(int x, int y)
		{
			
		}

        void handle_mine_building_clk(int x, int y)
		{
			
		}

        void handle_basic_building_clk(int x, int y, int flip)
		{
			
		}

        void handle_adv_1_building_clk(int x, int y)
		{
			
		}

        void handle_adv_2_building_clk(int x, int y)
		{
			
		}

        void handle_stat_select_click(int x, int y)
		{
			
		}

        void handle_stat_bld_click(int x, int y)
		{
			
		}

        void handle_stat_8_click(int x, int y)
		{
			
		}

        void handle_stat_7_click(int x, int y)
		{
			
		}

        void handle_stat_1_2_3_4_6_click(int x, int y)
		{
			
		}

        void handle_start_attack_click(int x, int y)
		{
			
		}

        void handle_ground_analysis_clk(int x, int y)
		{
			
		}

        void handle_sett_select_clk(int x, int y)
		{
			
		}

        void handle_sett_1_click(int x, int y)
		{
			
		}

        void handle_sett_2_click(int x, int y)
		{
			
		}

        void handle_sett_3_click(int x, int y)
		{
			
		}

        void handle_knight_level_click(int x, int y)
		{
			
		}

        void handle_sett_4_click(int x, int y)
		{
			
		}

        void handle_sett_5_6_click(int x, int y)
		{
			
		}

        void handle_quit_confirm_click(int x, int y)
		{
			
		}

        void handle_no_save_quit_confirm_click(int x, int y)
		{
			
		}

        void handle_castle_res_clk(int x, int y)
		{
			
		}

        void handle_transport_info_clk(int x, int y)
		{
			
		}

        void handle_castle_serf_clk(int x, int y)
		{
			
		}

        void handle_resdir_clk(int x, int y)
		{
			
		}

        void handle_sett_8_click(int x, int y)
		{
			
		}

        void handle_message_clk(int x, int y)
		{
			
		}

        void handle_player_faces_click(int x, int y)
		{
			
		}

        void handle_box_demolish_clk(int x, int y)
		{
			
		}

        void handle_minimap_clk(int x, int y)
		{
			
		}

        void handle_box_bld_1(int x, int y)
		{
			
		}

        void handle_box_bld_2(int x, int y)
		{
			
		}

        void handle_box_bld_3(int x, int y)
		{
			
		}

        void handle_box_bld_4(int x, int y)
		{
			
		}

        void handle_save_clk(int x, int y)
		{
			
		}

        void set_box(Type box)
		{
			
		}

        protected override void InternalDraw()
        {

        }

        protected override bool HandleClickLeft(int x, int y)
        {
            return base.HandleClickLeft(x, y);
        }
    }
}
