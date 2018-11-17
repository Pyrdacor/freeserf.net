/*
 * PopupBox.cs - Popup GUI component
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

        public enum Action
        {
            MinimapClick = 0,
            MinimapMode,
            MinimapRoads,
            MinimapBuildings,
            MinimapGrid,
            BuildStonemine,
            BuildCoalmine,
            BuildIronmine,
            BuildGoldmine,
            BuildFlag,
            BuildStonecutter,
            BuildHut,
            BuildLumberjack,
            BuildForester,
            BuildFisher,
            BuildMill,
            BuildBoatbuilder,
            BuildButcher,
            BuildWeaponsmith,
            BuildSteelsmelter,
            BuildSawmill,
            BuildBaker,
            BuildGoldsmelter,
            BuildFortress,
            BuildTower,
            BuildToolmaker,
            BuildFarm,
            BuildPigfarm,
            BldFlipPage,
            ShowStat1,
            ShowStat2,
            ShowStat8,
            ShowStatBld,
            ShowStat6,
            ShowStat7,
            ShowStat4,
            ShowStat3,
            ShowStatSelect,
            StatBldFlip,
            CloseBox,
            Sett8SetAspectAll,
            Sett8SetAspectLand,
            Sett8SetAspectBuildings,
            Sett8SetAspectMilitary,
            Sett8SetScale30Min,
            Sett8SetScale60Min,
            Sett8SetScale600Min,
            Sett8SetScale3000Min,
            Stat7SelectFish,
            Stat7SelectPig,
            Stat7SelectMeat,
            Stat7SelectWheat,
            Stat7SelectFlour,
            Stat7SelectBread,
            Stat7SelectLumber,
            Stat7SelectPlank,
            Stat7SelectBoat,
            Stat7SelectStone,
            Stat7SelectIronore,
            Stat7SelectSteel,
            Stat7SelectCoal,
            Stat7SelectGoldore,
            Stat7SelectGoldbar,
            Stat7SelectShovel,
            Stat7SelectHammer,
            Stat7SelectRod,
            Stat7SelectCleaver,
            Stat7SelectScythe,
            Stat7SelectAxe,
            Stat7SelectSaw,
            Stat7SelectPick,
            Stat7SelectPincer,
            Stat7SelectSword,
            Stat7SelectShield,
            AttackingKnightsDec,
            AttackingKnightsInc,
            StartAttack,
            CloseAttackBox,
            /* ... 78 - 91 ... */
            CloseSettBox = 92,
            ShowSett1,
            ShowSett2,
            ShowSett3,
            ShowSett7,
            ShowSett4,
            ShowSett5,
            ShowSettSelect,
            Sett1AdjustStonemine,
            Sett1AdjustCoalmine,
            Sett1AdjustIronmine,
            Sett1AdjustGoldmine,
            Sett2AdjustConstruction,
            Sett2AdjustBoatbuilder,
            Sett2AdjustToolmakerPlanks,
            Sett2AdjustToolmakerSteel,
            Sett2AdjustWeaponsmith,
            Sett3AdjustSteelsmelter,
            Sett3AdjustGoldsmelter,
            Sett3AdjustWeaponsmith,
            Sett3AdjustPigfarm,
            Sett3AdjustMill,
            KnightLevelClosestMinDec,
            KnightLevelClosestMinInc,
            KnightLevelClosestMaxDec,
            KnightLevelClosestMaxInc,
            KnightLevelCloseMinDec,
            KnightLevelCloseMinInc,
            KnightLevelCloseMaxDec,
            KnightLevelCloseMaxInc,
            KnightLevelFarMinDec,
            KnightLevelFarMinInc,
            KnightLevelFarMaxDec,
            KnightLevelFarMaxInc,
            KnightLevelFarthestMinDec,
            KnightLevelFarthestMinInc,
            KnightLevelFarthestMaxDec,
            KnightLevelFarthestMaxInc,
            Sett4AdjustShovel,
            Sett4AdjustHammer,
            Sett4AdjustAxe,
            Sett4AdjustSaw,
            Sett4AdjustScythe,
            Sett4AdjustPick,
            Sett4AdjustPincer,
            Sett4AdjustCleaver,
            Sett4AdjustRod,
            Sett56Item1,
            Sett56Item2,
            Sett56Item3,
            Sett56Item4,
            Sett56Item5,
            Sett56Item6,
            Sett56Item7,
            Sett56Item8,
            Sett56Item9,
            Sett56Item10,
            Sett56Item11,
            Sett56Item12,
            Sett56Item13,
            Sett56Item14,
            Sett56Item15,
            Sett56Item16,
            Sett56Item17,
            Sett56Item18,
            Sett56Item19,
            Sett56Item20,
            Sett56Item21,
            Sett56Item22,
            Sett56Item23,
            Sett56Item24,
            Sett56Item25,
            Sett56Item26,
            Sett56Top,
            Sett56Up,
            Sett56Down,
            Sett56Bottom,
            QuitConfirm,
            QuitCancel,
            NoSaveQuitConfirm,
            ShowQuit,
            Actionshowoptions,
            ShowSave,
            Sett8Cycle,
            CloseOptions,
            OptionsPathwayScrolling1,
            OptionsPathwayScrolling2,
            OptionsFastMapClick1,
            OptionsFastMapClick2,
            OptionsFastBuilding1,
            OptionsFastBuilding2,
            OptionsMessageCount1,
            OptionsMessageCount2,
            ShowSettSelectFile, /* Unused */
            ShowStatSelectFile, /* Unused */
            DefaultSett1,
            DefaultSett2,
            DefaultSett56,
            BuildStock,
            ShowCastleSerf,
            ShowResdir,
            ShowCastleRes,
            SendGeologist,
            ResModeIn,
            ResModeStop,
            ResModeOut,
            SerfModeIn,
            SerfModeStop,
            SerfModeOut,
            ShowSett8,
            ShowSett6,
            Sett8AdjustRate,
            Sett8Train1,
            Sett8Train5,
            Sett8Train20,
            Sett8Train100,
            DefaultSett3,
            Sett8SetCombatModeWeak,
            Sett8SetCombatModeStrong,
            AttackingSelectAll1,
            AttackingSelectAll2,
            AttackingSelectAll3,
            AttackingSelectAll4,
            MinimapBld1,
            MinimapBld2,
            MinimapBld3,
            MinimapBld4,
            MinimapBld5,
            MinimapBld6,
            MinimapBld7,
            MinimapBld8,
            MinimapBld9,
            MinimapBld10,
            MinimapBld11,
            MinimapBld12,
            MinimapBld13,
            MinimapBld14,
            MinimapBld15,
            MinimapBld16,
            MinimapBld17,
            MinimapBld18,
            MinimapBld19,
            MinimapBld20,
            MinimapBld21,
            MinimapBld22,
            MinimapBld23,
            MinimapBldFlag,
            MinimapBldNext,
            MinimapBldExit,
            CloseMessage,
            DefaultSett4,
            ShowPlayerFaces,
            MinimapScale,
            OptionsRightSide,
            CloseGroundAnalysis,
            UnknownTpInfoFlag,
            Sett8CastleDefDec,
            Sett8CastleDefInc,
            OptionsMusic,
            OptionsFullscreen,
            OptionsVolumeMinus,
            OptionsVolumePlus,
            Demolish,
            OptionsSfx,
            Save,
            NewNamE
        }

        // rendering
        Render.ISprite borderUp = null;
        Render.ISprite borderLeft = null;
        Render.ISprite borderRight = null;
        Render.ISprite borderDown = null;

        Interface interf;
        ListSavedFiles fileList;
        TextInput fileField;

        public Type Box { get; }
        public MinimapGame MiniMap { get; }

        int currentSett5Item;
        int currentSett6Item;
        int currentStat7Item;
        int currentStat8Mode;

        public PopupBox(Interface interf)
            : base(interf)
        {
            this.interf = interf;
            MiniMap = new MinimapGame(interf, interf.Game);
            fileList = new ListSavedFiles(interf);
            fileField = new TextInput(interf);

            currentSett5Item = 8;
            currentSett6Item = 15;
            currentStat7Item = 7;
            currentStat8Mode = 0;

            // TODO: the locations should be relative to the virtual screen!

            /* Initialize minimap */
            MiniMap.Displayed = false;
            MiniMap.Parent = this;
            MiniMap.SetSize(128, 128);
            AddFloatWindow(MiniMap, 8, 9);

            fileList.SetSize(120, 100);
            fileList.Displayed = false;
            fileList.SetSelectionHandler((string item) =>
            {
                int pos = item.LastIndexOfAny(new char[] { '/', '\\' });
                string fileName = item.Substring(pos + 1);
                fileField.Text = fileName;
            });
            AddFloatWindow(fileList, 12, 22);

            fileField.SetSize(120, 10);
            fileField.Displayed = false;
            AddFloatWindow(fileField, 12, 124);

            InitRenderComponents();
        }

        void InitRenderComponents()
        {
            // Top: 144x9
            // Bottom: 144x7
            // Left: 8x144
            // Right: 8x144

            var renderView = interf.RenderView;

            // Top
            var offset = GetTextureAtlasOffset(Data.Resource.FramePopup, 0u);
            borderUp = renderView.SpriteFactory.Create(144, 9, offset.X, offset.Y, false);
            borderUp.X = X;
            borderUp.Y = Y;
            borderUp.Layer = Layer;

            // Bottom
            offset = GetTextureAtlasOffset(Data.Resource.FramePopup, 1u);
            borderDown = renderView.SpriteFactory.Create(144, 7, offset.X, offset.Y, false);
            borderDown.X = X;
            borderDown.Y = Y + 153;
            borderDown.Layer = Layer;

            // Left
            offset = GetTextureAtlasOffset(Data.Resource.FramePopup, 2u);
            borderLeft = renderView.SpriteFactory.Create(8, 144, offset.X, offset.Y, false);
            borderLeft.X = X;
            borderLeft.Y = Y + 9;
            borderLeft.Layer = Layer;

            // Right
            offset = GetTextureAtlasOffset(Data.Resource.FramePopup, 3u);
            borderRight = renderView.SpriteFactory.Create(8, 144, offset.X, offset.Y, false);
            borderRight.X = X + 136;
            borderRight.Y = Y + 9;
            borderRight.Layer = Layer;
        }

        public void Show(Type box)
        {

        }

        public void Hide()
        {

        }

        /* Draw the frame around the popup box. */
        void draw_popup_box_frame()
		{
			
		}

        /* Draw icon in a popup frame. */
        void DrawPopupIcon(int x, int y, uint sprite)
		{
			
		}

        /* Draw building in a popup frame. */
        void draw_popup_building(int x, int y, uint sprite)
		{
			
		}

        /* Fill the background of a popup frame. */
        void draw_box_background(BackgroundPattern sprite)
		{
			
		}

        /* Fill one row of a popup frame. */
        void draw_box_row(uint sprite, int y)
		{
			
		}

        /* Draw a green string in a popup frame. */
        void draw_green_string(int x, int y, string str)
		{
			
		}

        /* Draw a green number in a popup frame.
           n must be non-negative. If > 999 simply draw ">999" (three characters). */
        void draw_green_number(int x, int y, uint n)
		{
            if (n >= 1000)
            {
                DrawPopupIcon(x, y, 0xd5u); /* Draw >999 */
                DrawPopupIcon(x + 1, y, 0xd6u);
                DrawPopupIcon(x + 2, y, 0xd7u);
            }
            else
            {
                /* Not the same sprites as are used to draw numbers
                   in gfx_draw_number(). */
                bool drawZero = false;

                if (n >= 100)
                {
                    uint n100 = (uint)((n / 100.0f) + 0.5f);
                    n -= n100 * 100;
                    DrawPopupIcon(x, y, 0x4e + n100);
                    ++x;
                    drawZero = true;
                }

                if (n >= 10 || drawZero)
                {
                    uint n10 = (uint)((n / 10.0f) + 0.5f);
                    n -= n10 * 10;
                    DrawPopupIcon(x, y, 0x4e + n10);
                    ++x;
                }

                DrawPopupIcon(x, y, 0x4e + n);
            }
        }

        /* Draw a green number in a popup frame.
           No limits on n. */
        void draw_green_large_number(int x, int y, uint n)
		{
			
		}

        /* Draw small green number. */
        void draw_additional_number(int x, int y, uint n)
		{
            if (n > 0)
            {
                DrawPopupIcon(x, y, 240u + Math.Min(n, 10u));
            }
        }

        /* Get the sprite number for a face. */
        uint get_player_face_sprite(uint face)
		{
            if (face != 0)
            {
                return 0x10bu + face;
            }

            return 0x119u; /* sprite_face_none */
        }

        /* Draw player face in popup frame. */
        void draw_player_face(int x, int y, uint playerIndex)
		{
            Player.Color color;
            uint face = 0;
            Player player = interf.Game.GetPlayer(playerIndex);

            if (player != null)
            {
                color = interf.GetPlayerColor(playerIndex);
                face = player.GetFace();
            }

            //frame->fill_rect(8 * x, y + 5, 48, 72, color);
            DrawPopupIcon(x, y, get_player_face_sprite(face));
        }

        /* Draw a layout of buildings in a popup box. */
        void draw_custom_bld_box(int[] sprites)
		{
			
		}

        /* Draw a layout of icons in a popup box. */
        void draw_custom_icon_box(int[] sprites)
		{
            int index = 0;

            while (sprites[index] > 0)
            {
                DrawPopupIcon(sprites[index + 1], sprites[index + 2], (uint)sprites[index + 0]);
                index += 3;
            }
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

        protected override void InternalHide()
        {
            borderUp.Visible = false ;
            borderLeft.Visible = false;
            borderRight.Visible = false;
            borderDown.Visible = false;
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
