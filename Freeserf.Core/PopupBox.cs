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
    using ResourceMap = Dictionary<Resource.Type, int>;

    // TODO: If stats should reflect the current state we have
    //       to redraw the stat popup from time to time.
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
            StatMenu,
            ResourceStats,
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
            ShowBuildingStats,
            ShowSettlerStats,
            ShowStat7,
            ShowResourceStats,
            ShowStat3,
            ShowStatMenu,
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
            ShowFoodDistribution,
            ShowPlanksAndSteelDistribution,
            ShowCoalAndWheatDistribution,
            ShowSett7,
            ShowToolmakerPriorities,
            ShowTransportPriorities,
            ShowSettlerMenu,
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
            DefaultFoodDistribution,
            DefaultPlanksAndSteelDistribution,
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
            ShowInventoryPriorities,
            Sett8AdjustRate,
            Sett8Train1,
            Sett8Train5,
            Sett8Train20,
            Sett8Train100,
            DefaultCoalAndWheatDistribution,
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
            DefaultToolmakerPriorities,
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
        readonly Dictionary<TextField, bool> texts = new Dictionary<TextField, bool>(); // value: in use
        readonly SlideBar[] slideBars = new SlideBar[9]; // TODO: is 9 enough?
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
                slideBars[i] = new SlideBar(interf, 20); // should always be in front of other elements
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
                case Type.FoodDistribution:
                    if (index == 0) // stonemine food
                        player.SetFoodStonemine(realAmount);
                    else if (index == 1) // coalmine food
                        player.SetFoodCoalmine(realAmount);
                    else if (index == 2) // ironmine food
                        player.SetFoodIronmine(realAmount);
                    else if (index == 3) // goldmine food
                        player.SetFoodGoldmine(realAmount);
                    break;
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
                case Type.CoalAndWheatDistribution:
                    if (index == 0) // steelsmelter coal
                        player.SetCoalSteelsmelter(realAmount);
                    else if (index == 1) // goldsmelter coal
                        player.SetCoalGoldsmelter(realAmount);
                    else if (index == 2) // weaponsmith coal
                        player.SetCoalWeaponsmith(realAmount);
                    else if (index == 3) // pigfarm wheat
                        player.SetWheatPigfarm(realAmount);
                    else if (index == 4) // mill wheat
                        player.SetWheatMill(realAmount);
                    break;
                case Type.ToolmakerPriorities:
                    player.SetToolPriority(index, (int)realAmount);
                    break;
                    // TODO ...
            }
        }

        void HandleButtonClick(object buttonTag, int x, int y)
        {
            HandleAction((Action)buttonTag, x, y);
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
                case Type.StatMenu:
                case Type.ResourceStats:
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


        #region Texts

        void SetText(int x, int y, string text)
        {
            // check if there is a free text
            foreach (var textField in texts.Keys.ToList())
            {
                if (texts[textField] == false)
                {
                    textField.Text = text;
                    textField.MoveTo(x, y);
                    textField.Displayed = Displayed;
                    texts[textField] = true;
                    return;
                }
            }

            var newText = new TextField(interf, 1);

            newText.Text = text;
            newText.Displayed = Displayed;
            AddChild(newText, x, y, true);

            texts.Add(newText, true);
        }

        void ClearTexts()
        {
            foreach (var text in texts.Keys.ToList())
            {
                texts[text] = false;
                text.Displayed = false;
            }
        }

        #endregion


        #region Icons

        void SetBuildingIcon(int x, int y, Building.Type buildingType)
        {
            SetBuildingIcon(x, y, Render.RenderBuilding.MapBuildingSprite[(int)buildingType]);
        }

        void SetBuildingIcon(int x, int y, uint spriteIndex)
        {
            SetIcon(x, y, spriteIndex, Data.Resource.MapObject, true);
        }

        void SetIcon(int x, int y, uint spriteIndex, Data.Resource resourceType = Data.Resource.Icon, bool building = false)
        {
            var info = interf.RenderView.DataSource.GetSpriteInfo(resourceType, spriteIndex);

            // check if there is a free icon
            foreach (var icon in icons.Keys.ToList())
            {
                if (icons[icon] == false)
                {
                    if (building == icon is BuildingIcon)
                    {
                        icon.SetSpriteIndex(resourceType, spriteIndex);
                        icon.Resize(info.Width, info.Height);
                        icon.MoveTo(x, y);
                        icon.Displayed = Displayed;
                        icons[icon] = true;
                        return;
                    }
                }
            }

            var newIcon = (building) ? new BuildingIcon(interf, info.Width, info.Height, spriteIndex, 1) :
                new Icon(interf, info.Width, info.Height, resourceType, spriteIndex, 1);

            newIcon.Displayed = Displayed;
            AddChild(newIcon, x, y, true);

            icons.Add(newIcon, true);
        }

        void ClearIcons()
        {
            foreach (var icon in icons.Keys.ToList())
            {
                icons[icon] = false;
                icon.Displayed = false;
            }
        }

        #endregion


        #region Buttons

        void SetButton(int x, int y, uint spriteIndex, object tag, Data.Resource resourceType = Data.Resource.Icon)
        {
            var info = interf.RenderView.DataSource.GetSpriteInfo(resourceType, spriteIndex);

            // check if there is a free button
            foreach (var button in buttons.Keys.ToList())
            {
                if (buttons[button] == false)
                {
                    button.Tag = tag;
                    button.SetSpriteIndex(resourceType, spriteIndex);
                    button.Resize(info.Width, info.Height);
                    button.MoveTo(x, y);
                    button.Displayed = Displayed;
                    buttons[button] = true;
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
            HandleButtonClick((sender as Button).Tag, args.X, args.Y);
        }

        void ClearButtons()
        {
            foreach (var button in buttons.Keys.ToList())
            {
                buttons[button] = false;
                button.Displayed = false;
            }
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

        static readonly int[] ResourcesLayout = new int[]
        {
            0x28, 1, 0, /* resources */
            0x29, 1, 16,
            0x2a, 1, 32,
            0x2b, 1, 48,
            0x2e, 1, 64,
            0x2c, 1, 80,
            0x2d, 1, 96,
            0x2f, 1, 112,
            0x30, 1, 128,
            0x31, 6, 0,
            0x32, 6, 16,
            0x36, 6, 32,
            0x37, 6, 48,
            0x35, 6, 64,
            0x38, 6, 80,
            0x39, 6, 96,
            0x34, 6, 112,
            0x33, 6, 128,
            0x3a, 11, 0,
            0x3b, 11, 16,
            0x22, 11, 32,
            0x23, 11, 48,
            0x24, 11, 64,
            0x25, 11, 80,
            0x26, 11, 96,
            0x27, 11, 112,
        };

        void DrawResourcesBox(ResourceMap resources)
		{
            SetIcon(16,   9, 0x28);
            SetIcon(16,  25, 0x29);
            SetIcon(16,  41, 0x2a);
            SetIcon(16,  57, 0x2b);
            SetIcon(16,  73, 0x2e);
            SetIcon(16,  89, 0x2c);
            SetIcon(16, 105, 0x2d);
            SetIcon(16, 121, 0x2f);
            SetIcon(16, 137, 0x30);
            SetIcon(56,   9, 0x31);
            SetIcon(56,  25, 0x32);
            SetIcon(56,  41, 0x36);
            SetIcon(56,  57, 0x37);
            SetIcon(56,  73, 0x35);
            SetIcon(56,  89, 0x38);
            SetIcon(56, 105, 0x39);
            SetIcon(56, 121, 0x34);
            SetIcon(56, 137, 0x33);
            SetIcon(96,   9, 0x3a);
            SetIcon(96,  25, 0x3b);
            SetIcon(96,  41, 0x22);
            SetIcon(96,  57, 0x23);
            SetIcon(96,  73, 0x24);
            SetIcon(96,  89, 0x25);
            SetIcon(96, 105, 0x26);
            SetIcon(96, 121, 0x27);

            SetText(32,   13, resources[Resource.Type.Lumber].ToString());
            SetText(32,   29, resources[Resource.Type.Plank].ToString());
            SetText(32,   45, resources[Resource.Type.Boat].ToString());
            SetText(32,   61, resources[Resource.Type.Stone].ToString());
            SetText(32,   77, resources[Resource.Type.Coal].ToString());
            SetText(32,   93, resources[Resource.Type.IronOre].ToString());
            SetText(32,  109, resources[Resource.Type.Steel].ToString());
            SetText(32,  125, resources[Resource.Type.GoldOre].ToString());
            SetText(32,  141, resources[Resource.Type.GoldBar].ToString());
            SetText(72,   13, resources[Resource.Type.Shovel].ToString());
            SetText(72,   29, resources[Resource.Type.Hammer].ToString());
            SetText(72,   45, resources[Resource.Type.Axe].ToString());
            SetText(72,   61, resources[Resource.Type.Saw].ToString());
            SetText(72,   77, resources[Resource.Type.Scythe].ToString());
            SetText(72,   93, resources[Resource.Type.Pick].ToString());
            SetText(72,  109, resources[Resource.Type.Pincer].ToString());
            SetText(72,  125, resources[Resource.Type.Cleaver].ToString());
            SetText(72,  141, resources[Resource.Type.Rod].ToString());
            SetText(112,  13, resources[Resource.Type.Sword].ToString());
            SetText(112,  29, resources[Resource.Type.Shield].ToString());
            SetText(112,  45, resources[Resource.Type.Fish].ToString());
            SetText(112,  61, resources[Resource.Type.Pig].ToString());
            SetText(112,  77, resources[Resource.Type.Meat].ToString());
            SetText(112,  93, resources[Resource.Type.Wheat].ToString());
            SetText(112, 109, resources[Resource.Type.Flour].ToString());
            SetText(112, 125, resources[Resource.Type.Bread].ToString());
        }

        void draw_serfs_box(int[] serfs, int total)
		{
			
		}

        void DrawStatMenuBox()
		{
            SetButton(16, 21, 72u, Action.ShowStat1);
            SetButton(56, 21, 73u, Action.ShowStat2);
            SetButton(96, 21, 77u, Action.ShowStat3);

            SetButton(16, 65, 74u, Action.ShowResourceStats);
            SetButton(56, 65, 76u, Action.ShowBuildingStats);
            SetButton(96, 65, 75u, Action.ShowSettlerStats);

            SetButton(16, 109, 71u, Action.ShowStat7);
            SetButton(56, 109, 70u, Action.ShowStat8);

            SetButton(104, 113, 61u, Action.ShowSettlerMenu);
            SetButton(120, 137, 60u, Action.CloseBox);

        }

        void DrawResourceStatsBox()
		{
            DrawResourcesBox(interf.GetPlayer().GetStatsResources());

            SetButton(120, 137, 60u, Action.ShowStatMenu);
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
            SetButton(16, 17, 230u, Action.ShowFoodDistribution);
            SetButton(56, 17, 231u, Action.ShowPlanksAndSteelDistribution);
            SetButton(96, 17, 232u, Action.ShowCoalAndWheatDistribution);

            SetButton(16, 57, 234u, Action.ShowToolmakerPriorities);
            SetButton(56, 57, 235u, Action.ShowTransportPriorities);
            SetButton(96, 57, 299u, Action.ShowInventoryPriorities);

            SetButton(16, 97, 233u, Action.ShowSett7); // TODO: Check Type
            SetButton(56, 97, 298u, Action.ShowSett8); // TODO: Check Type

            SetButton(104, 113, 61u, Action.ShowStatMenu);
            SetButton(120, 137, 60u, Action.CloseBox);

            SetButton(40, 137, 285u, null); // TODO: What action?
            SetButton(8, 137, 286u, Action.ShowQuit);
            SetButton(72, 137, 224u, Action.ShowSave);
        }

        void draw_slide_bar(int x, int y, int value)
		{
			
		}

        void DrawFoodDistributionBox()
		{
            SetBuildingIcon(104, 30, Building.Type.StoneMine);
            SetBuildingIcon(72, 50, Building.Type.CoalMine);
            SetBuildingIcon(40, 70, Building.Type.IronMine);
            SetBuildingIcon(8, 90, Building.Type.GoldMine);

            SetIcon(40, 10, 34u); // fish icon
            SetIcon(64, 10, 36u); // meat icon
            SetIcon(88, 10, 39u); // bread icon

            SetButton(120, 137, 60u, Action.ShowSettlerMenu); // exit button
            SetButton(16, 17, 295u, Action.DefaultFoodDistribution); // reset values button

            Player player = interf.GetPlayer();

            slideBars[0].MoveTo(40, 30);
            slideBars[0].Displayed = Displayed;
            slideBars[0].Fill = (int)player.GetFoodStonemine() / SlideBarFactor;

            slideBars[1].MoveTo(8, 50);
            slideBars[1].Displayed = Displayed;
            slideBars[1].Fill = (int)player.GetFoodCoalmine() / SlideBarFactor;

            slideBars[2].MoveTo(72, 123);
            slideBars[2].Displayed = Displayed;
            slideBars[2].Fill = (int)player.GetFoodIronmine() / SlideBarFactor;

            slideBars[3].MoveTo(40, 142);
            slideBars[3].Displayed = Displayed;
            slideBars[3].Fill = (int)player.GetFoodGoldmine() / SlideBarFactor;
        }

        void DrawPlanksAndSteelDistributionBox()
		{
            SetBuildingIcon(24, 9, Render.RenderBuilding.MapBuildingFrameSprite[(int)Building.Type.Lumberjack]);
            SetBuildingIcon(24, 50, Building.Type.Boatbuilder);
            SetBuildingIcon(72, 63, Building.Type.ToolMaker);
            SetBuildingIcon(8, 111, Building.Type.WeaponSmith);

            SetIcon(80, 34, 41u); // plank icon
            SetIcon(80, 128, 45u); // steel icon

            SetButton(120, 137, 60u, Action.ShowSettlerMenu); // exit button
            SetButton(112, 17, 295u, Action.DefaultPlanksAndSteelDistribution); // reset values button

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

        void DrawCoalAndWheatDistributionBox()
		{
            SetBuildingIcon(8, 10, Building.Type.SteelSmelter);
            SetBuildingIcon(88, 9, Building.Type.GoldSmelter);
            SetBuildingIcon(40, 65, Building.Type.WeaponSmith);
            SetBuildingIcon(104, 70, Building.Type.Mill);
            SetBuildingIcon(8, 110, Building.Type.PigFarm);

            SetIcon(64, 28, 46u); // coal icon
            SetIcon(72, 110, 37u); // wheet icon

            SetButton(120, 137, 60u, Action.ShowSettlerMenu); // exit button
            SetButton(16, 69, 295u, Action.DefaultCoalAndWheatDistribution); // reset values button

            Player player = interf.GetPlayer();

            slideBars[0].MoveTo(8, 48);
            slideBars[0].Displayed = Displayed;
            slideBars[0].Fill = (int)player.GetCoalSteelsmelter() / SlideBarFactor;

            slideBars[1].MoveTo(72, 48);
            slideBars[1].Displayed = Displayed;
            slideBars[1].Fill = (int)player.GetCoalGoldsmelter() / SlideBarFactor;

            slideBars[2].MoveTo(40, 56);
            slideBars[2].Displayed = Displayed;
            slideBars[2].Fill = (int)player.GetCoalWeaponsmith() / SlideBarFactor;

            slideBars[3].MoveTo(8, 99);
            slideBars[3].Displayed = Displayed;
            slideBars[3].Fill = (int)player.GetWheatPigfarm() / SlideBarFactor;

            slideBars[4].MoveTo(72, 127);
            slideBars[4].Displayed = Displayed;
            slideBars[4].Fill = (int)player.GetWheatMill() / SlideBarFactor;
        }

        void draw_knight_level_box()
		{
			
		}

        void DrawToolmakerPrioritiesBox()
		{
            SetIcon(8,   9, 49u); // shovel
            SetIcon(8,  25, 50u); // hammer
            SetIcon(8,  41, 54u); // axe
            SetIcon(8,  57, 55u); // saw
            SetIcon(8,  73, 53u); // scythe
            SetIcon(8,  89, 56u); // pick
            SetIcon(8, 105, 57u); // pincer
            SetIcon(8, 121, 52u); // cleaver
            SetIcon(8, 137, 51u); // rod

            SetButton(120, 137, 60u, Action.ShowSettlerMenu); // exit button
            SetButton(112, 17, 295u, Action.DefaultToolmakerPriorities); // reset values button

            Player player = interf.GetPlayer();

            int[] locations = new int[9]
            {
                13, 29, 141, 125, 77, 45, 61, 93, 109
            };

            for (int i = 0; i < 9; ++i)
            {
                slideBars[i].MoveTo(40, locations[i]);
                slideBars[i].Displayed = Displayed;
                slideBars[i].Fill = player.GetToolPriority(i) / SlideBarFactor;
            }
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
                case Action.CloseBox:
                    interf.ClosePopup();
                    break;
                case Action.ShowSettlerMenu:
                    SetBox(Type.SettlerMenu);
                    break;
                case Action.ShowStatMenu:
                    SetBox(Type.StatMenu);
                    break;
                case Action.BuildFlag:
                    interf.BuildFlag();
                    interf.ClosePopup();
                    break;
                case Action.BuildBuilding:
                    interf.BuildBuilding((Building.Type)tag);
                    interf.ClosePopup();
                    break;
                case Action.ShowResourceStats:
                    SetBox(Type.ResourceStats);
                    break;
                case Action.ShowFoodDistribution:
                    SetBox(Type.FoodDistribution);
                    break;
                case Action.ShowPlanksAndSteelDistribution:
                    SetBox(Type.PlanksAndSteelDistribution);
                    break;                
                case Action.ShowCoalAndWheatDistribution:
                    SetBox(Type.CoalAndWheatDistribution);
                    break;
                case Action.ShowToolmakerPriorities:
                    SetBox(Type.ToolmakerPriorities);
                    break;
                case Action.DefaultFoodDistribution:
                    player.ResetFoodPriority();
                    break;
                case Action.DefaultPlanksAndSteelDistribution:
                    player.ResetPlanksPriority();
                    player.ResetSteelPriority();
                    break;
                case Action.DefaultCoalAndWheatDistribution:
                    player.ResetCoalPriority();
                    player.ResetWheatPriority();
                    break;
                case Action.DefaultToolmakerPriorities:
                    player.ResetToolPriority();
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

            // hide all icons, buttons, slidebars and texts
            ClearIcons();
            ClearButtons();
            ClearSlideBars();
            ClearTexts();

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
                case Type.StatMenu:
                    DrawStatMenuBox();
                    break;
                case Type.ResourceStats:
                    DrawResourceStatsBox();
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
                    DrawCoalAndWheatDistributionBox();
                    break;
                case Type.KnightLevel:
                    draw_knight_level_box();
                    break;
                case Type.ToolmakerPriorities:
                    DrawToolmakerPrioritiesBox();
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
