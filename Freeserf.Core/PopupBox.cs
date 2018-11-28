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
            SettlerMenu,
            FoodDistribution,
            PlanksAndSteelDistribution,
            CoalAndWheatDistribution,
            KnightLevel,
            ToolmakerPriorities,
            TransportPriorities,
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
            InventoryPriorities,
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
            BuildFlag,
            BuildBuilding,
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
            NewName
        }

        Interface interf;
        ListSavedFiles fileList;
        TextInput fileField;

        public Type Box { get; private set; }
        public MinimapGame MiniMap { get; }

        readonly BuildingButton[] buildings = new BuildingButton[8]; // max 8 buildings per popup
        readonly Button flipButton = null;
        readonly Dictionary<Icon, bool> icons = new Dictionary<Icon, bool>(); // value: in use
        readonly Dictionary<Button, bool> buttons = new Dictionary<Button, bool>(); // value: in use
        readonly SlideBar[] slideBars = new SlideBar[5]; // TODO: is 5 enough?
        const int SlideBarFactor = 1310;

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

            flipButton = new Button(interf, 16, 16, Data.Resource.Icon, 61u, 1);
            flipButton.Clicked += FlipButton_Clicked;
            AddChild(flipButton, 0, 0, false);

            for (int i = 0; i < slideBars.Length; ++i)
            {
                slideBars[i] = new SlideBar(interf);
                slideBars[i].FillChanged += PopupBox_SlideBarFillChanged;
                AddChild(slideBars[i], 0, 0, false);
            }

            InitBuildings();

            InitRenderComponents();
        }

        private void FlipButton_Clicked(object sender, Button.ClickEventArgs args)
        {
            // TODO

            switch (Box)
            {
                case Type.BasicBldFlip:
                    SetBox(Type.Adv1Bld);
                    break;
                case Type.Adv1Bld:
                    SetBox(Type.Adv2Bld);
                    break;
                case Type.Adv2Bld:
                    SetBox(Type.BasicBldFlip);
                    break;
                // TODO ...
            }
        }

        void PopupBox_SlideBarFillChanged(object sender, EventArgs args)
        {
            int index = -1;

            for (int i = 0; i < slideBars.Length; ++i)
            {
                if (slideBars[i] == sender)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
                return;

            HandleSlideBarClick(index);
        }

        void HandleSlideBarClick(int index)
        {
            var player = interf.GetPlayer();
            uint realAmount = (uint)slideBars[index].Fill * SlideBarFactor;

            switch (Box)
            {
                case Type.PlanksAndSteelDistribution:
                    if (index == 0) // construction planks
                        player.SetPlanksConstruction(realAmount);
                    else if (index == 1) // boatbuilder planks
                        player.SetPlanksBoatbuilder(realAmount);
                    else if (index == 2) // toolmaker planks
                        player.SetPlanksToolmaker(realAmount);
                    else if (index == 3) // toolmaker steel
                        player.SetSteelToolmaker(realAmount);
                    else if (index == 4) // weaponsmith steel
                        player.SetSteelWeaponsmith(realAmount);
                    break;
                // TODO ...
            }
        }

        void HandleButtonClick(object buttonTag)
        {
            // TODO

            switch (Box)
            {
                case Type.SettlerMenu:
                    SetBox((Type)buttonTag);
                    break;
                // TODO ...
            }
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
                case Type.SettlerMenu:
                case Type.FoodDistribution:
                case Type.PlanksAndSteelDistribution:
                case Type.CoalAndWheatDistribution:
                case Type.KnightLevel:
                case Type.ToolmakerPriorities:
                case Type.TransportPriorities:
                case Type.InventoryPriorities:
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
                buildings[i] = new BuildingButton(interf, 0, 0, 128u, 1);
                buildings[i].Clicked += PopupBox_BuildingClicked;
                AddChild(buildings[i], 0, 0, false);
            }
        }

        private void PopupBox_BuildingClicked(object sender, Button.ClickEventArgs args)
        {
            switch (Box)
            {
                case Type.BasicBld:
                case Type.BasicBldFlip:
                case Type.Adv1Bld:
                case Type.Adv2Bld:
                    HandleBuildingClick((sender as BuildingButton).Tag, args.X, args.Y);
                    break;
                // TODO ...
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

            buildings[index].Tag = Map.Object.Flag;

            SetBuilding(index, x, y, 128 + playerIndex, data.GetSpriteInfo(Data.Resource.MapObject, 128u));
        }

        void SetBuilding(int index, int x, int y, Building.Type type)
        {
            uint spriteIndex = Render.RenderBuilding.MapBuildingSprite[(int)type];
            var data = interf.RenderView.DataSource;
            var spriteInfo = data.GetSpriteInfo(Data.Resource.MapObject, spriteIndex);

            buildings[index].Tag = type;

            SetBuilding(index, x, y, spriteIndex, spriteInfo);
        }

        void SetBuilding(int index, int x, int y, uint spriteIndex, SpriteInfo spriteInfo)
        {
            buildings[index].SetSpriteIndex(spriteIndex);
            buildings[index].MoveTo(x, y);
            buildings[index].Resize(spriteInfo.Width, spriteInfo.Height);
        }

        #endregion


        #region Slidebars

        void ClearSlideBars()
        {
            for (int i = 0; i < slideBars.Length; ++i)
                slideBars[i].Displayed = false;
        }

        #endregion


        #region Icons

        void SetIcon(int x, int y, uint spriteIndex, Data.Resource resourceType = Data.Resource.Icon)
        {
            // check if we already have the icon
            var icon = icons.FirstOrDefault(i => i.Key.SpriteIndex == spriteIndex);

            if (icon.Key != null)
            {
                icon.Key.MoveTo(x, y);
                icon.Key.Displayed = Displayed;
                icons[icon.Key] = true;
                return;
            }

            var info = interf.RenderView.DataSource.GetSpriteInfo(resourceType, spriteIndex);

            // otherwise check if there is a free icon
            foreach (var i in icons)
            {
                if (i.Value == false)
                {
                    i.Key.SetSpriteIndex(resourceType, spriteIndex);
                    i.Key.Resize(info.Width, info.Height);
                    i.Key.MoveTo(x, y);
                    i.Key.Displayed = Displayed;
                    icons[icon.Key] = true;
                    return;
                }
            }

            var newIcon = new Icon(interf, info.Width, info.Height, resourceType, spriteIndex, 1);

            newIcon.Displayed = Displayed;
            AddChild(newIcon, x, y, true);

            icons.Add(newIcon, true);
        }

        void ClearIcons()
        {
            foreach (var icon in icons)
                icon.Key.Displayed = false;
        }

        #endregion


        #region Buttons

        void SetButton(int x, int y, uint spriteIndex, object tag, Data.Resource resourceType = Data.Resource.Icon)
        {
            // check if we already have the button
            var button = buttons.FirstOrDefault(i => i.Key.SpriteIndex == spriteIndex);

            if (button.Key != null)
            {
                button.Key.Tag = tag;
                button.Key.MoveTo(x, y);
                button.Key.Displayed = Displayed;
                buttons[button.Key] = true;
                return;
            }

            var info = interf.RenderView.DataSource.GetSpriteInfo(resourceType, spriteIndex);

            // otherwise check if there is a free icon
            foreach (var b in buttons)
            {
                if (b.Value == false)
                {
                    b.Key.Tag = tag;
                    b.Key.SetSpriteIndex(resourceType, spriteIndex);
                    b.Key.Resize(info.Width, info.Height);
                    b.Key.MoveTo(x, y);
                    b.Key.Displayed = Displayed;
                    buttons[button.Key] = true;
                    return;
                }
            }

            var newButton = new Button(interf, info.Width, info.Height, resourceType, spriteIndex, 1);

            newButton.Tag = tag;
            newButton.Displayed = Displayed;
            newButton.Clicked += PopupBox_ButtonClicked;
            AddChild(newButton, x, y, true);

            buttons.Add(newButton, true);
        }

        private void PopupBox_ButtonClicked(object sender, Button.ClickEventArgs args)
        {
            HandleButtonClick((sender as Button).Tag);
        }

        void ClearButtons()
        {
            foreach (var button in buttons)
                button.Key.Displayed = false;
        }

        #endregion


        /* Draw the frame around the popup box. */
        void draw_popup_box_frame()
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

        void DrawPopupIcon(int x, int y, uint spriteIndex)
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

        // flip means the user can change the page
        void DrawBasicBuildingBox(bool flip)
		{
            int num = 6;
            int index = 0;

            // add hut if military buildings are possible
            if (interf.Game.CanBuildMilitary(interf.GetMapCursorPos()))
            {
                SetBuilding(index++, 88, 22, Building.Type.Hut);
                ++num;
            }

            SetBuilding(index++, 24, 22, Building.Type.Stonecutter);
            SetBuilding(index++, 8, 67, Building.Type.Lumberjack);
            SetBuilding(index++, 56, 65, Building.Type.Forester);
            SetBuilding(index++, 104, 64, Building.Type.Fisher);
            SetBuilding(index++, 24, 94, Building.Type.Mill);
            SetBuilding(index++, 88, 96, Building.Type.Boatbuilder);

            if (interf.Game.CanBuildFlag(interf.GetMapCursorPos(), interf.GetPlayer()))
            {
                SetFlag(index, 72, 117);
                ++num;
            }

            ShowBuildings(num);

            flipButton.MoveTo(8, 137);
            flipButton.Displayed = flip && Displayed;
        }

        // 8 * x + 8, y + 9
        void DrawAdv1BuildingBox()
		{
            int index = 0;

            SetBuilding(index++, 8, 24, Building.Type.Butcher);
            SetBuilding(index++, 72, 24, Building.Type.WeaponSmith);
            SetBuilding(index++, 8, 59, Building.Type.SteelSmelter);
            SetBuilding(index++, 72, 59, Building.Type.Sawmill);
            SetBuilding(index++, 24, 109, Building.Type.Baker);
            SetBuilding(index++, 88, 105, Building.Type.GoldSmelter);

            ShowBuildings(6);

            flipButton.MoveTo(8, 137);
            flipButton.Displayed = Displayed;
        }

        void DrawAdv2BuildingBox()
		{
            int num = 4;
            int index = 0;

            // add hut if military buildings are possible
            if (interf.Game.CanBuildMilitary(interf.GetMapCursorPos()))
            {
                SetBuilding(index++, 24, 108, Building.Type.Tower);
                SetBuilding(index++, 72, 93, Building.Type.Fortress);

                num += 2;
            }

            SetBuilding(index++, 8, 10, Building.Type.ToolMaker);
            SetBuilding(index++, 8, 55, Building.Type.Stock);
            SetBuilding(index++, 72, 10, Building.Type.Farm);
            SetBuilding(index++, 72, 54, Building.Type.PigFarm);

            ShowBuildings(num);

            flipButton.MoveTo(8, 137);
            flipButton.Displayed = Displayed;
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

        void DrawSettlerMenuBox()
		{
            SetButton(16, 17, 230u, Type.FoodDistribution);
            SetButton(56, 17, 231u, Type.PlanksAndSteelDistribution);
            SetButton(96, 17, 232u, Type.CoalAndWheatDistribution);

            SetButton(16, 57, 234u, Type.ToolmakerPriorities);
            SetButton(56, 57, 235u, Type.TransportPriorities);
            SetButton(96, 57, 299u, Type.InventoryPriorities);

            SetButton(16, 97, 233u, Type.Sett8); // TODO: Knights, Check Type
            SetButton(56, 97, 298u, Type.KnightLevel); // TODO: Check Type

            SetButton(104, 113, 61u, null); // TODO: Flip
            SetButton(120, 137, 60u, null); // TODO: Exit

            SetButton(40, 137, 285u, Type.Options);
            SetButton(8, 137, 286u, Type.QuitConfirm);
            SetButton(72, 137, 224u, Type.LoadSave); // TODO: Is Save. Is this type right?
        }

        void draw_slide_bar(int x, int y, int value)
		{
			
		}

        void DrawFoodDistributionBox()
		{
			
		}

        void DrawPlanksAndSteelDistributionBox()
		{
            // TODO: buttons and icons

            Player player = interf.GetPlayer();

            slideBars[0].MoveTo(8, 35);
            slideBars[0].Displayed = Displayed;
            slideBars[0].Fill = (int)player.GetPlanksConstruction() / SlideBarFactor;

            slideBars[1].MoveTo(8, 45);
            slideBars[1].Displayed = Displayed;
            slideBars[1].Fill = (int)player.GetPlanksBoatbuilder() / SlideBarFactor;

            slideBars[2].MoveTo(72, 53);
            slideBars[2].Displayed = Displayed;
            slideBars[2].Fill = (int)player.GetPlanksToolmaker() / SlideBarFactor;

            slideBars[3].MoveTo(72, 112);
            slideBars[3].Displayed = Displayed;
            slideBars[3].Fill = (int)player.GetSteelToolmaker() / SlideBarFactor;

            slideBars[4].MoveTo(8, 139);
            slideBars[4].Displayed = Displayed;
            slideBars[4].Fill = (int)player.GetSteelWeaponsmith() / SlideBarFactor;
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


        void HandleAction(Action action, int x, int y, object tag = null)
		{
            SetRedraw();

            var player = interf.GetPlayer();

            // TODO
            switch (action)
            {
                case Action.BuildFlag:
                    interf.BuildFlag();
                    interf.ClosePopup();
                    break;
                case Action.BuildBuilding:
                    interf.BuildBuilding((Building.Type)tag);
                    interf.ClosePopup();
                    break;
                // TODO ...
                default:
                    Log.Warn.Write("popup", "unhandled action " + action.ToString());
                    break;
            }
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

        // TODO: can also be used for mines, large buildings, etc
        void HandleBuildingClick(object tag, int x, int y)
		{
            if (tag is Building.Type)
            {
                HandleAction(Action.BuildBuilding, x, y, tag);
            }
            else if (tag is Map.Object && (Map.Object)tag == Map.Object.Flag)
            {
                HandleAction(Action.BuildFlag, x, y);
            }
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
            // first hide all building sprites
            ShowBuildings(0);

            // hide all icons, buttons and slidebars
            ClearIcons();
            ClearButtons();
            ClearSlideBars();

            // TODO

            /* Dispatch to one of the popup box functions above. */
            switch (Box)
            {
                case Type.Map:
                    draw_map_box();
                    break;
                case Type.MineBuilding:
                    draw_mine_building_box();
                    break;
                case Type.BasicBld:
                    DrawBasicBuildingBox(false);
                    break;
                case Type.BasicBldFlip:
                    DrawBasicBuildingBox(true);
                    break;
                case Type.Adv1Bld:
                    DrawAdv1BuildingBox();
                    break;
                case Type.Adv2Bld:
                    DrawAdv2BuildingBox();
                    break;
                case Type.StatSelect:
                    draw_stat_select_box();
                    break;
                case Type.Stat4:
                    draw_stat_4_box();
                    break;
                case Type.StatBld1:
                    draw_stat_bld_1_box();
                    break;
                case Type.StatBld2:
                    draw_stat_bld_2_box();
                    break;
                case Type.StatBld3:
                    draw_stat_bld_3_box();
                    break;
                case Type.StatBld4:
                    draw_stat_bld_4_box();
                    break;
                case Type.Stat8:
                    draw_stat_8_box();
                    break;
                case Type.Stat7:
                    draw_stat_7_box();
                    break;
                case Type.Stat1:
                    draw_stat_1_box();
                    break;
                case Type.Stat2:
                    draw_stat_2_box();
                    break;
                case Type.Stat6:
                    draw_stat_6_box();
                    break;
                case Type.Stat3:
                    draw_stat_3_box();
                    break;
                case Type.StartAttack:
                    draw_start_attack_box();
                    break;
                case Type.StartAttackRedraw:
                    draw_start_attack_redraw_box();
                    break;
                case Type.GroundAnalysis:
                    draw_ground_analysis_box();
                    break;
                /* TODO */
                case Type.SettlerMenu:
                    DrawSettlerMenuBox();
                    break;
                case Type.FoodDistribution:
                    DrawFoodDistributionBox();
                    break;
                case Type.PlanksAndSteelDistribution:
                    DrawPlanksAndSteelDistributionBox();
                    break;
                case Type.CoalAndWheatDistribution:
                    draw_sett_3_box();
                    break;
                case Type.KnightLevel:
                    draw_knight_level_box();
                    break;
                case Type.ToolmakerPriorities:
                    draw_sett_4_box();
                    break;
                case Type.TransportPriorities:
                    draw_sett_5_box();
                    break;
                case Type.QuitConfirm:
                    draw_quit_confirm_box();
                    break;
                case Type.NoSaveQuitConfirm:
                    draw_no_save_quit_confirm_box();
                    break;
                case Type.Options:
                    draw_options_box();
                    break;
                case Type.CastleRes:
                    draw_castle_res_box();
                    break;
                case Type.MineOutput:
                    draw_mine_output_box();
                    break;
                case Type.OrderedBld:
                    draw_ordered_building_box();
                    break;
                case Type.Defenders:
                    draw_defenders_box();
                    break;
                case Type.TransportInfo:
                    draw_transport_info_box();
                    break;
                case Type.CastleSerf:
                    draw_castle_serf_box();
                    break;
                case Type.ResDir:
                    draw_resdir_box();
                    break;
                case Type.Sett8:
                    draw_sett_8_box();
                    break;
                case Type.InventoryPriorities:
                    draw_sett_6_box();
                    break;
                case Type.Bld1:
                    draw_bld_1_box();
                    break;
                case Type.Bld2:
                    draw_bld_2_box();
                    break;
                case Type.Bld3:
                    draw_bld_3_box();
                    break;
                case Type.Bld4:
                    draw_bld_4_box();
                    break;
                case Type.BldStock:
                    draw_building_stock_box();
                    break;
                case Type.PlayerFaces:
                    draw_player_faces_box();
                    break;
                case Type.Demolish:
                    draw_demolish_box();
                    break;
                case Type.LoadSave:
                    draw_save_box();
                    break;
                default:
                    break;
            }

            base.InternalDraw();
        }

        protected override bool HandleClickLeft(int x, int y)
        {
            base.HandleClickLeft(x, y);
                
            return true; // always return true to avoid passing click events through
        }
    }
}
