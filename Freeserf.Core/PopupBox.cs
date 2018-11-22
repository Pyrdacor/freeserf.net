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
using System.Linq;

namespace Freeserf
{
    using ResourceMap = Dictionary<Resource.Type, uint>;

    internal class PopupBox : Box
    {
        public enum Type
        {
            None = 0,
            Map,
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

        Interface interf;
        ListSavedFiles fileList;
        TextInput fileField;

        public Type Box { get; private set; }
        public MinimapGame MiniMap { get; }

        BuildingIcon[] buildings = new BuildingIcon[8]; // max 8 buildings per popup

        int currentSett5Item;
        int currentSett6Item;
        int currentStat7Item;
        int currentStat8Mode;

        static Freeserf.BackgroundPattern[] backgrounds = null;

        static void InitBackgrounds(Render.ISpriteFactory spriteFactory)
        {
            if (backgrounds != null)
                return;

            var patterns = Enum.GetValues(typeof(BackgroundPattern));

            backgrounds = new Freeserf.BackgroundPattern[patterns.Length];

            for (uint i = 0; i < patterns.Length; ++i)
                backgrounds[i] = Freeserf.BackgroundPattern.CreatePopupBoxBackground(spriteFactory, 320u + i);
        }

        public PopupBox(Interface interf)
            : base
            (
                  interf, 
                  Freeserf.BackgroundPattern.CreatePopupBoxBackground(interf.RenderView.SpriteFactory, 320u),
                  Border.CreatePopupBoxBorder(interf.RenderView.SpriteFactory)
            )
        {
            InitBackgrounds(interf.RenderView.SpriteFactory);

            this.interf = interf;
            MiniMap = new MinimapGame(interf, interf.Game);
            fileList = new ListSavedFiles(interf);
            fileField = new TextInput(interf);

            currentSett5Item = 8;
            currentSett6Item = 15;
            currentStat7Item = 7;
            currentStat8Mode = 0;

            /* Initialize minimap */
            MiniMap.SetSize(128, 128);
            AddChild(MiniMap, 8, 9, false);

            fileList.SetSize(120, 100);
            fileList.SetSelectionHandler((string item) =>
            {
                int pos = item.LastIndexOfAny(new char[] { '/', '\\' });
                string fileName = item.Substring(pos + 1);
                fileField.Text = fileName;
            });
            AddChild(fileList, 12, 22, false);

            fileField.SetSize(120, 10);
            AddChild(fileField, 12, 124, false);

            InitBuildings();

            InitRenderComponents();
        }

        void InitRenderComponents()
        {
            // TODO
        }

        public void Show(Type box)
        {
            SetBox(box);
            Displayed = true;
        }

        public void Hide()
        {
            SetBox(Type.None);
            Displayed = false;
        }

        void SetBox(Type box)
        {
            Box = box;

            MiniMap.Displayed = box == Type.Map;

            SetBackground(BackgroundFromType());

            SetRedraw();
        }

        Freeserf.BackgroundPattern BackgroundFromType()
        {
            BackgroundPattern pattern = BackgroundPattern.StripedGreen;

            switch (Box)
            {
                default:
                case Type.Map:
                case Type.MapOverlay: /* UNUSED */
                    break; // no background, but just use default from above
                case Type.MineBuilding:
                case Type.BasicBld:
                case Type.BasicBldFlip:
                case Type.Adv1Bld:
                case Type.Adv2Bld:
                    pattern = BackgroundPattern.Construction;
                    break;
                case Type.StartAttack:
                case Type.StartAttackRedraw:
                    // TODO
                    break;
                case Type.GroundAnalysis:
                case Type.StatSelect:
                case Type.Stat4:
                case Type.StatBld1:
                case Type.StatBld2:
                case Type.StatBld3:
                case Type.StatBld4:
                case Type.Stat8:
                case Type.Stat7:
                case Type.Stat1:
                case Type.Stat2:
                case Type.Stat6:
                case Type.Stat3:
                case Type.PlayerFaces:
                    // TODO: maybe some of those have different background pattern
                    pattern = BackgroundPattern.StripedGreen;
                    break;
                case Type.SettSelect:
                case Type.Sett1:
                case Type.Sett2:
                case Type.Sett3:
                case Type.KnightLevel:
                case Type.Sett4:
                case Type.Sett5:
                case Type.Sett6:
                case Type.Sett8:
                    pattern = BackgroundPattern.CheckerdDiagonalBrown;
                    break;                
                case Type.QuitConfirm:
                case Type.Options:
                case Type.LoadSave:
                    pattern = BackgroundPattern.DiagonalGreen;
                    break;
                case Type.Bld1:
                case Type.Bld2:
                case Type.Bld3:
                case Type.Bld4:
                    pattern = BackgroundPattern.StaresGreen;
                    break;
                case Type.Message:
                case Type.NoSaveQuitConfirm:
                case Type.SettSelectFile: /* UNUSED */
                case Type.LoadArchive:
                case Type.Type25:
                case Type.DiskMsg:
                case Type.GameEnd:
                case Type.JsCalib:
                case Type.JsCalibUpLeft:
                case Type.JsCalibDownRight:
                case Type.JsCalibCenter:
                case Type.CtrlsInfo:
                    // TODO: these are unknown? check later and add the right pattern!
                    break;
                case Type.Demolish:
                    pattern = BackgroundPattern.SquaresGreen;
                    break;
                case Type.CastleRes:
                case Type.MineOutput:
                case Type.OrderedBld:
                case Type.Defenders:
                case Type.TransportInfo:
                case Type.CastleSerf:
                case Type.ResDir:
                case Type.BldStock:
                    pattern = BackgroundPattern.PlaidAlongGreen;
                    break;
            }

            int index = Array.IndexOf(Enum.GetValues(typeof(BackgroundPattern)), pattern);

            return backgrounds[index];
        }


        #region Buildings

        void InitBuildings()
        {
            for (int i = 0; i < buildings.Length; ++i)
            {
                buildings[i] = new BuildingIcon(interf, 0, 0, 128u, 1);
            }
        }

        void ShowBuildings(int num)
        {
            for (int i = 0; i < buildings.Length; ++i)
            {
                buildings[i].Displayed = i < num;
            }
        }

        void SetFlag(int index, int x, int y)
        {
            var data = interf.RenderView.DataSource;
            var playerIndex = interf.GetPlayer().Index;

            SetBuilding(index, x, y, 128 + playerIndex, data.GetSpriteInfo(Data.Resource.MapObject, 128u));
        }

        void SetBuilding(int index, int x, int y, Building.Type type)
        {
            uint spriteIndex = Render.RenderBuilding.MapBuildingSprite[(int)type];
            var data = interf.RenderView.DataSource;
            var spriteInfo = data.GetSpriteInfo(Data.Resource.MapObject, spriteIndex);

            SetBuilding(index, x, y, spriteIndex, spriteInfo);
        }

        void SetBuilding(int index, int x, int y, uint spriteIndex, SpriteInfo spriteInfo)
        {
            buildings[index].SetSpriteIndex(spriteIndex);
            buildings[index].MoveTo(TotalX + x + spriteInfo.OffsetX, TotalY + y + spriteInfo.OffsetY);
            buildings[index].SetSize(spriteInfo.Width, spriteInfo.Height);
        }

        #endregion


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
        static uint GetPlayerFaceSprite(uint face)
		{
            if (face != 0)
                return 0x10bu + face;

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
            DrawPopupIcon(x, y, GetPlayerFaceSprite(face));
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

        protected override void InternalHide()
        {
            base.InternalHide();

            // TODO
        }

        protected override void InternalDraw()
        {
            base.InternalDraw();

            // TODO
        }

        protected override bool HandleClickLeft(int x, int y)
        {
            return base.HandleClickLeft(x, y);
        }
    }
}
