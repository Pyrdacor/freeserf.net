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
            BuildingStats1,
            BuildingStats2,
            BuildingStats3,
            BuildingStats4,
            Stat8,
            Stat7,
            Stat1,
            Stat2,
            SettlerStats,
            IdleAndPotentialSettlerStats,
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
            CastleResources,
            MineOutput,
            OrderedBld,
            Defenders,
            TransportInfo,
            CastleSerfs,
            ResourceDirections,
            KnightSettingsBox,
            InventoryPriorities,
            Bld1,
            Bld2,
            Bld3,
            Bld4,
            Message,
            BuildingStock,
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
            ShowIdleAndPotentialSettlerStats,
            ShowStatMenu,
            BuildingStatsFlip,
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
            SetTransportItem1,
            SetTransportItem2,
            SetTransportItem3,
            SetTransportItem4,
            SetTransportItem5,
            SetTransportItem6,
            SetTransportItem7,
            SetTransportItem8,
            SetTransportItem9,
            SetTransportItem10,
            SetTransportItem11,
            SetTransportItem12,
            SetTransportItem13,
            SetTransportItem14,
            SetTransportItem15,
            SetTransportItem16,
            SetTransportItem17,
            SetTransportItem18,
            SetTransportItem19,
            SetTransportItem20,
            SetTransportItem21,
            SetTransportItem22,
            SetTransportItem23,
            SetTransportItem24,
            SetTransportItem25,
            SetTransportItem26,
            TransportPriorityToTop,
            TransportPriorityUp,
            TransportPriorityDown,
            TransportPriorityToBottom,
            QuitConfirm,
            QuitCancel,
            NoSaveQuitConfirm,
            ShowQuit,
            Actionshowoptions,
            ShowSave,
            CycleKnights,
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
            DefaultTransportPriorities,
            BuildStock,
            ShowCastleSerfs,
            ShowResourceDirections,
            ShowCastleResources,
            SendGeologist,
            ResourceModeIn,
            ResourceModeStop,
            ResourceModeOut,
            SerfModeIn,
            SerfModeStop,
            SerfModeOut,
            ShowKnightSettings,
            ShowInventoryPriorities,
            Sett8AdjustRate,
            TrainKnights,
            DefaultCoalAndWheatDistribution,
            SetCombatMode,
            SetCombatModeWeak,
            SetCombatModeStrong,
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
            DecreaseCastleKnights,
            IncreaseCastleKnights,
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

        uint CurrentTransportPriorityItem = 0u;
        uint CurrentInventoryPriorityItem = 0u;
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

            CurrentTransportPriorityItem = 8;
            CurrentInventoryPriorityItem = 15;
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
                case Type.KnightSettingsBox:
                    if (index == 0) // serf to knight rate
                        player.SetSerfToKnightRate((int)realAmount);
                    break;
                    // TODO ...
            }
        }

        void HandleButtonClick(object buttonTag, int x, int y)
        {
            var action = (Action)buttonTag;

            if (action == Action.CycleKnights ||
                action == Action.ResourceModeIn ||
                action == Action.ResourceModeStop ||
                action == Action.ResourceModeOut ||
                action == Action.SerfModeIn ||
                action == Action.SerfModeStop ||
                action == Action.SerfModeOut)
                return; // needs double click

            HandleAction(action, x, y);
        }

        void HandleButtonDoubleClick(object buttonTag, int x, int y)
        {
            var action = (Action)buttonTag;

            if (action != Action.CycleKnights &&
                action != Action.ResourceModeIn &&
                action != Action.ResourceModeStop &&
                action != Action.ResourceModeOut &&
                action != Action.SerfModeIn &&
                action != Action.SerfModeStop &&
                action != Action.SerfModeOut)
                return; // needs single click

            HandleAction(action, x, y);
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
                case Type.BuildingStats1:
                case Type.BuildingStats2:
                case Type.BuildingStats3:
                case Type.BuildingStats4:
                case Type.Stat8:
                case Type.Stat7:
                case Type.Stat1:
                case Type.Stat2:
                case Type.SettlerStats:
                case Type.IdleAndPotentialSettlerStats:
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
                case Type.KnightSettingsBox:
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
                case Type.CastleResources:
                case Type.MineOutput:
                case Type.OrderedBld:
                case Type.Defenders:
                case Type.TransportInfo:
                case Type.CastleSerfs:
                case Type.ResourceDirections:
                case Type.BuildingStock:
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
            HandleBuildingClick((sender as BuildingButton).Tag, args.X, args.Y);
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

        void SetNumberText(int x, int y, uint number)
        {
            if (number >= 1000)
            {
                SetIcon(x, y, 213u);
                SetIcon(x + 8, y, 214u);
                SetIcon(x + 16, y, 215u);
            }
            else
            {
                SetText(x, y, number.ToString(), true);
            }
        }

        void SetText(int x, int y, string text, bool useSpecialDigits = false)
        {
            // check if there is a free text
            foreach (var textField in texts.Keys.ToList())
            {
                if (texts[textField] == false)
                {
                    textField.Text = text;
                    textField.MoveTo(x, y);
                    textField.UseSpecialDigits(useSpecialDigits);
                    textField.Displayed = Displayed;
                    texts[textField] = true;
                    return;
                }
            }

            var newText = new TextField(interf, 1, 8, useSpecialDigits);

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

        void SetFlagIcon(int x, int y)
        {
            var playerIndex = interf.GetPlayer().Index;

            SetBuildingIcon(x, y, 128u + playerIndex);
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
            newButton.DoubleClicked += PopupBox_ButtonDoubleClicked;
            AddChild(newButton, x, y, true);

            buttons.Add(newButton, true);
        }

        private void PopupBox_ButtonDoubleClicked(object sender, Button.ClickEventArgs args)
        {
            HandleButtonDoubleClick((sender as Button).Tag, args.X, args.Y);
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

        void DrawMapBox()
		{
            SetButton(8, 137, (uint)MiniMap.GetOwnershipMode(), Action.MinimapMode);
            SetButton(40, 137, MiniMap.DrawRoads ? 3u : 4u, Action.MinimapRoads);
            SetButton(72, 137, MiniMap.DrawBuildings ? 5u : 6u, Action.MinimapBuildings);
            SetButton(104, 137, MiniMap.DrawGrid ? 7u : 8u, Action.MinimapGrid);
            SetButton(120, 137, MiniMap.GetScale() == 1 ? 91u : 92u, Action.MinimapScale);

            MiniMap.UpdateMinimap(true);
        }

        void DrawMineBuildingBox()
		{
            int index = 0;

            SetBuilding(index++, 24, 17, Building.Type.StoneMine);
            SetBuilding(index++, 72, 17, Building.Type.CoalMine);
            SetBuilding(index++, 40, 86, Building.Type.IronMine);
            SetBuilding(index++, 88, 86, Building.Type.GoldMine);

            ShowBuildings(4);
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

            SetNumberText(32,   13, (uint)resources[Resource.Type.Lumber]);
            SetNumberText(32,   29, (uint)resources[Resource.Type.Plank]);
            SetNumberText(32,   45, (uint)resources[Resource.Type.Boat]);
            SetNumberText(32,   61, (uint)resources[Resource.Type.Stone]);
            SetNumberText(32,   77, (uint)resources[Resource.Type.Coal]);
            SetNumberText(32,   93, (uint)resources[Resource.Type.IronOre]);
            SetNumberText(32,  109, (uint)resources[Resource.Type.Steel]);
            SetNumberText(32,  125, (uint)resources[Resource.Type.GoldOre]);
            SetNumberText(32,  141, (uint)resources[Resource.Type.GoldBar]);
            SetNumberText(72,   13, (uint)resources[Resource.Type.Shovel]);
            SetNumberText(72,   29, (uint)resources[Resource.Type.Hammer]);
            SetNumberText(72,   45, (uint)resources[Resource.Type.Axe]);
            SetNumberText(72,   61, (uint)resources[Resource.Type.Saw]);
            SetNumberText(72,   77, (uint)resources[Resource.Type.Scythe]);
            SetNumberText(72,   93, (uint)resources[Resource.Type.Pick]);
            SetNumberText(72,  109, (uint)resources[Resource.Type.Pincer]);
            SetNumberText(72,  125, (uint)resources[Resource.Type.Cleaver]);
            SetNumberText(72,  141, (uint)resources[Resource.Type.Rod]);
            SetNumberText(112,  13, (uint)resources[Resource.Type.Sword]);
            SetNumberText(112,  29, (uint)resources[Resource.Type.Shield]);
            SetNumberText(112,  45, (uint)resources[Resource.Type.Fish]);
            SetNumberText(112,  61, (uint)resources[Resource.Type.Pig]);
            SetNumberText(112,  77, (uint)resources[Resource.Type.Meat]);
            SetNumberText(112,  93, (uint)resources[Resource.Type.Wheat]);
            SetNumberText(112, 109, (uint)resources[Resource.Type.Flour]);
            SetNumberText(112, 125, (uint)resources[Resource.Type.Bread]);
        }

        // TODO: If a serf type counts more than 99 the space is too small
        void DrawSerfsBox(uint[] serfCounts, int total)
		{
            SetIcon(16, 9, 0x09);
            SetIcon(16, 25, 0x0a);
            SetIcon(16, 41, 0x0b);
            SetIcon(16, 57, 0x0c);
            SetIcon(16, 73, 0x21);
            SetIcon(16, 89, 0x20);
            SetIcon(16, 105, 0x1f);
            SetIcon(16, 121, 0x1e);
            SetIcon(16, 137, 0x1d);
            SetIcon(56, 9, 0x0d);
            SetIcon(56, 25, 0x0e);
            SetIcon(56, 41, 0x12);
            SetIcon(56, 57, 0x0f);
            SetIcon(56, 73, 0x10);
            SetIcon(56, 89, 0x11);
            SetIcon(56, 105, 0x19);
            SetIcon(56, 121, 0x1a);
            SetIcon(56, 137, 0x1b);
            SetIcon(96, 9, 0x13);
            SetIcon(96, 25, 0x14);
            SetIcon(96, 41, 0x15);
            SetIcon(96, 57, 0x16);
            SetIcon(96, 73, 0x17);
            SetIcon(96, 89, 0x18);
            SetIcon(96, 105, 0x1c);
            SetIcon(96, 121, 0x82);

            SetNumberText(32, 13, serfCounts[(int)Serf.Type.Transporter]);
            SetNumberText(32, 29, serfCounts[(int)Serf.Type.Sailor]);
            SetNumberText(32, 45, serfCounts[(int)Serf.Type.Digger]);
            SetNumberText(32, 61, serfCounts[(int)Serf.Type.Builder]);
            SetNumberText(32, 77, serfCounts[(int)Serf.Type.Knight4]);
            SetNumberText(32, 93, serfCounts[(int)Serf.Type.Knight3]);
            SetNumberText(32, 109, serfCounts[(int)Serf.Type.Knight2]);
            SetNumberText(32, 125, serfCounts[(int)Serf.Type.Knight1]);
            SetNumberText(32, 141, serfCounts[(int)Serf.Type.Knight0]);
            SetNumberText(72, 13, serfCounts[(int)Serf.Type.Lumberjack]);
            SetNumberText(72, 29, serfCounts[(int)Serf.Type.Sawmiller]);
            SetNumberText(72, 45, serfCounts[(int)Serf.Type.Smelter]);
            SetNumberText(72, 61, serfCounts[(int)Serf.Type.Stonecutter]);
            SetNumberText(72, 77, serfCounts[(int)Serf.Type.Forester]);
            SetNumberText(72, 93, serfCounts[(int)Serf.Type.Miner]);
            SetNumberText(72, 109, serfCounts[(int)Serf.Type.BoatBuilder]);
            SetNumberText(72, 125, serfCounts[(int)Serf.Type.Toolmaker]);
            SetNumberText(72, 141, serfCounts[(int)Serf.Type.WeaponSmith]);
            SetNumberText(112, 13, serfCounts[(int)Serf.Type.Fisher]);
            SetNumberText(112, 29, serfCounts[(int)Serf.Type.PigFarmer]);
            SetNumberText(112, 45, serfCounts[(int)Serf.Type.Butcher]);
            SetNumberText(112, 61, serfCounts[(int)Serf.Type.Farmer]);
            SetNumberText(112, 77, serfCounts[(int)Serf.Type.Miller]);
            SetNumberText(112, 93, serfCounts[(int)Serf.Type.Baker]);
            SetNumberText(112, 109, serfCounts[(int)Serf.Type.Geologist]);
            SetNumberText(112, 125, serfCounts[(int)Serf.Type.Generic]);

            if (total >= 0)
                SetNumberText(94, 141, (uint)total);
        }

        void DrawStatMenuBox()
		{
            SetButton(16, 21, 72u, Action.ShowStat1);
            SetButton(56, 21, 73u, Action.ShowStat2);
            SetButton(96, 21, 77u, Action.ShowIdleAndPotentialSettlerStats);

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

        void DrawBuildingCount(int x, int y, Building.Type type)
		{
            var player = interf.GetPlayer();

            uint numComplete = player.GetCompletedBuildingCount(type);

            SetNumberText(x, y, numComplete);

            int xOffset = 8;

            if (numComplete > 99)
                xOffset = 24;
            else if (numComplete > 9)
                xOffset = 16;

            uint numIncomplete = player.GetIncompleteBuildingCount(type);

            if (numIncomplete > 0)
                SetIcon(x + xOffset, y, 240u + Math.Min(numIncomplete, 10u));
		}

        void DrawBuildingStats1Box()
		{
            SetBuildingIcon(8, 14, Building.Type.Stock);
            SetBuildingIcon(24, 86, Building.Type.Hut);
            SetBuildingIcon(72, 16, Building.Type.Tower);
            SetBuildingIcon(56, 78, Building.Type.Fortress);

            DrawBuildingCount(40, 70, Building.Type.Stock);
            DrawBuildingCount(24, 114, Building.Type.Hut);
            DrawBuildingCount(88, 62, Building.Type.Tower);
            DrawBuildingCount(80, 139, Building.Type.Fortress);

            SetButton(8, 137, 61u, Action.BuildingStatsFlip);
            SetButton(120, 137, 60u, Action.ShowStatMenu);
        }

        void DrawBuildingStats2Box()
		{
            SetBuildingIcon(8, 13, Building.Type.ToolMaker);
            SetBuildingIcon(72, 15, Building.Type.Sawmill);
            SetBuildingIcon(8, 77, Building.Type.WeaponSmith);
            SetBuildingIcon(72, 74, Building.Type.Stonecutter);
            SetBuildingIcon(104, 66, Building.Type.Boatbuilder);
            SetBuildingIcon(40, 114, Building.Type.Forester);
            SetBuildingIcon(72, 116, Building.Type.Lumberjack);

            DrawBuildingCount(32, 63, Building.Type.ToolMaker);
            DrawBuildingCount(88, 57, Building.Type.Sawmill);
            DrawBuildingCount(32, 104, Building.Type.WeaponSmith);
            DrawBuildingCount(72, 104, Building.Type.Stonecutter);
            DrawBuildingCount(104, 104, Building.Type.Boatbuilder);
            DrawBuildingCount(48, 141, Building.Type.Forester);
            DrawBuildingCount(80, 141, Building.Type.Lumberjack);

            SetButton(8, 137, 61u, Action.BuildingStatsFlip);
            SetButton(120, 137, 60u, Action.ShowStatMenu);
        }

        void DrawBuildingStats3Box()
		{
            SetBuildingIcon(8, 11, Building.Type.PigFarm);
            SetBuildingIcon(72, 12, Building.Type.Farm);
            SetBuildingIcon(8, 70, Building.Type.Fisher);
            SetBuildingIcon(72, 69, Building.Type.Butcher);
            SetBuildingIcon(40, 84, Building.Type.Mill);
            SetBuildingIcon(72, 109, Building.Type.Baker);

            DrawBuildingCount(32, 57, Building.Type.PigFarm);
            DrawBuildingCount(96, 57, Building.Type.Farm);
            DrawBuildingCount(8, 101, Building.Type.Fisher);
            DrawBuildingCount(96, 96, Building.Type.Butcher);
            DrawBuildingCount(48, 143, Building.Type.Mill);
            DrawBuildingCount(88, 143, Building.Type.Baker);

            SetButton(8, 137, 61u, Action.BuildingStatsFlip);
            SetButton(120, 137, 60u, Action.ShowStatMenu);
        }

        void DrawBuildingStats4Box()
		{
            SetBuildingIcon(8, 13, Building.Type.StoneMine);
            SetBuildingIcon(40, 13, Building.Type.CoalMine);
            SetBuildingIcon(72, 13, Building.Type.IronMine);
            SetBuildingIcon(104, 13, Building.Type.GoldMine);
            SetBuildingIcon(24, 99, Building.Type.SteelSmelter);
            SetBuildingIcon(72, 99, Building.Type.GoldSmelter);

            DrawBuildingCount(8, 80, Building.Type.StoneMine);
            DrawBuildingCount(40, 80, Building.Type.CoalMine);
            DrawBuildingCount(72, 80, Building.Type.IronMine);
            DrawBuildingCount(104, 80, Building.Type.GoldMine);
            DrawBuildingCount(40, 139, Building.Type.SteelSmelter);
            DrawBuildingCount(80, 139, Building.Type.GoldSmelter);

            SetButton(8, 137, 61u, Action.BuildingStatsFlip);
            SetButton(120, 137, 60u, Action.ShowStatMenu);
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

        void DrawSerfCountBox()
		{
            var player = interf.GetPlayer();
            int total = 0;

            for (int i = 0; i < 27; i++)
            {
                if (i != (int)Serf.Type.TransporterInventory)
                {
                    total += (int)player.GetSerfCount(i);
                }
            }

            DrawSerfsBox(player.GetSerfCounts(), total);

            SetButton(120, 137, 60u, Action.ShowStatMenu);
		}

        void DrawSerfMeter(int x, int y, int value)
		{
            uint sprite = 0xc6;

            if (value < 1)
            {
                sprite = 0xbc;
            }
            else if (value < 2)
            {
                sprite = 0xbe;
            }
            else if (value < 3)
            {
                sprite = 0xc0;
            }
            else if (value < 4)
            {
                sprite = 0xc1;
            }
            else if (value < 5)
            {
                sprite = 0xc2;
            }
            else if (value < 7)
            {
                sprite = 0xc3;
            }
            else if (value < 10)
            {
                sprite = 0xc4;
            }
            else if (value < 20)
            {
                sprite = 0xc5;
            }

            SetIcon(8 * x + 8, y + 9, sprite);
        }

        void DrawIdleAndPotentialSerfsBox()
		{
            var player = interf.GetPlayer();
            var serfs = player.GetStatsSerfsIdle();
            var serfsPotential = player.GetStatsSerfsPotential();

            for (int i = 0; i < 27; ++i)
            {
                serfs[(Serf.Type)i] += serfsPotential[(Serf.Type)i];
            }

            SetIcon(16, 9, 0x09);
            SetIcon(16, 25, 0x0a);
            SetIcon(16, 41, 0x0b);
            SetIcon(16, 57, 0x0c);
            SetIcon(16, 73, 0x21);
            SetIcon(16, 89, 0x20);
            SetIcon(16, 105, 0x1f);
            SetIcon(16, 121, 0x1e);
            SetIcon(16, 137, 0x1d);
            SetIcon(56, 9, 0x0d);
            SetIcon(56, 25, 0x0e);
            SetIcon(56, 41, 0x12);
            SetIcon(56, 57, 0x0f);
            SetIcon(56, 73, 0x10);
            SetIcon(56, 89, 0x11);
            SetIcon(56, 105, 0x19);
            SetIcon(56, 121, 0x1a);
            SetIcon(56, 137, 0x1b);
            SetIcon(96, 9, 0x13);
            SetIcon(96, 25, 0x14);
            SetIcon(96, 41, 0x15);
            SetIcon(96, 57, 0x16);
            SetIcon(96, 73, 0x17);
            SetIcon(96, 89, 0x18);
            SetIcon(96, 105, 0x1c);
            SetIcon(96, 121, 0x82);

            /* First column */
            DrawSerfMeter(3,   0, serfs[Serf.Type.Transporter]);
            DrawSerfMeter(3,  16, serfs[Serf.Type.Sailor]);
            DrawSerfMeter(3,  32, serfs[Serf.Type.Digger]);
            DrawSerfMeter(3,  48, serfs[Serf.Type.Builder]);
            DrawSerfMeter(3,  64, serfs[Serf.Type.Knight4]);
            DrawSerfMeter(3,  80, serfs[Serf.Type.Knight3]);
            DrawSerfMeter(3,  96, serfs[Serf.Type.Knight2]);
            DrawSerfMeter(3, 112, serfs[Serf.Type.Knight1]);
            DrawSerfMeter(3, 128, serfs[Serf.Type.Knight0]);

            /* Second column */
            DrawSerfMeter(8,   0, serfs[Serf.Type.Lumberjack]);
            DrawSerfMeter(8,  16, serfs[Serf.Type.Sawmiller]);
            DrawSerfMeter(8,  32, serfs[Serf.Type.Smelter]);
            DrawSerfMeter(8,  48, serfs[Serf.Type.Stonecutter]);
            DrawSerfMeter(8,  64, serfs[Serf.Type.Forester]);
            DrawSerfMeter(8,  80, serfs[Serf.Type.Miner]);
            DrawSerfMeter(8,  96, serfs[Serf.Type.BoatBuilder]);
            DrawSerfMeter(8, 112, serfs[Serf.Type.Toolmaker]);
            DrawSerfMeter(8, 128, serfs[Serf.Type.WeaponSmith]);

            /* Third column */
            DrawSerfMeter(13,  0, serfs[Serf.Type.Fisher]);
            DrawSerfMeter(13,  16, serfs[Serf.Type.PigFarmer]);
            DrawSerfMeter(13,  32, serfs[Serf.Type.Butcher]);
            DrawSerfMeter(13,  48, serfs[Serf.Type.Farmer]);
            DrawSerfMeter(13,  64, serfs[Serf.Type.Miller]);
            DrawSerfMeter(13,  80, serfs[Serf.Type.Baker]);
            DrawSerfMeter(13,  96, serfs[Serf.Type.Geologist]);
            DrawSerfMeter(13, 112, serfs[Serf.Type.Generic]);

            SetButton(120, 137, 60u, Action.ShowStatMenu);
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
            SetButton(56, 97, 298u, Action.ShowKnightSettings); // TODO: Check Type

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

        static readonly int[] ResourceStairLayout = new int[]
        {
             5,  4,
             7,  6,
             9,  8,
            11, 10,
            13, 12,
            13, 28,
            11, 30,
             9, 32,
             7, 34,
             5, 36,
             3, 38,
             1, 40,
             1, 56,
             3, 58,
             5, 60,
             7, 62,
             9, 64,
            11, 66,
            13, 68,
            13, 84,
            11, 86,
             9, 88,
             7, 90,
             5, 92,
             3, 94,
             1, 96
        };

        void DrawPopupResourceStairs(int[] order)
		{
            int count = ResourceStairLayout.Length / 2;

            for (int i = 0; i < count; ++i)
            {
                int pos = count - order[i];

                SetButton(8 * ResourceStairLayout[2 * pos] + 8, ResourceStairLayout[2 * pos + 1] + 9, 34u + (uint)i, Action.SetTransportItem1 + pos);
            }
		}

        void DrawTransportPrioritiesBox()
		{
            SetButton(16, 129, 237u, Action.TransportPriorityToTop);
            SetButton(32, 129, 238u, Action.TransportPriorityUp);
            SetButton(80, 129, 239u, Action.TransportPriorityDown);
            SetButton(96, 129, 240u, Action.TransportPriorityToBottom);

            SetButton(120, 137, 60u, Action.ShowSettlerMenu); // exit button
            SetButton(16, 13, 295u, Action.DefaultTransportPriorities); // reset values button

            if (Box == Type.TransportPriorities)
            {
                SetIcon(56, 129, 33u + CurrentTransportPriorityItem);
                DrawPopupResourceStairs(interf.GetPlayer().GetFlagPriorities());
            }
            else
            {
                SetIcon(56, 129, 33u + CurrentInventoryPriorityItem);
                DrawPopupResourceStairs(interf.GetPlayer().GetInventoryPriorities());
            }
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

        void draw_mine_output_box()
		{
			
		}

        void DrawOrderedBuildingBox()
		{
            var building = TryToOpenBuildingPopup();

            if (building == null)
                return;

            var type = building.BuildingType;
            var info = interf.RenderView.DataSource.GetSpriteInfo(Data.Resource.MapObject, Render.RenderBuilding.MapBuildingSprite[(int)type]);

            int x = (Width - info.Width) / 2;

            SetBuildingIcon(x, 49, type);

            SetText(16, 13, "Ordered");
            SetText(16, 23, "Building");

            if (building.HasSerf())
            {
                if (building.GetProgress() == 0)
                {
                    /* Digger */
                    SetIcon(18, 121, 0xbu);
                }
                else
                {
                    /* Builder */
                    SetIcon(18, 121, 0xcu);
                }
            }
            else
            {
                /* Minus box */
                SetIcon(18, 121, 0xdcu);
            }

            // draw construction materials
            SetIcon(48, 121, 41u); // plank icon
            SetText(50, 137, $"{building.GetResourceCountInStock(0)}");
            SetIcon(78, 121, 43u); // stone icon
            SetText(80, 137, $"{building.GetResourceCountInStock(1)}");

            SetButton(120, 137, 0x3c, Action.CloseBox); // exit
        }

        void draw_defenders_box()
		{
			
		}

        static readonly int[] FlagLayout = new int[]
        {
            9, 24,
            5, 24,
            3, 44,
            5, 64,
            9, 64,
            11, 44
        };

        // flag menu
        void DrawTransportInfoBox()
		{
            if (interf.GetPlayer().tempIndex == 0)
            {
                interf.ClosePopup();
                return;
            }

            var flag = interf.Game.GetFlag(interf.GetPlayer().tempIndex);

            SetFlagIcon(72, 49);

            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var dir in cycle)
            {
                int index = 5 - (int)dir;
                int x = 8 + 8 * FlagLayout[index * 2];
                int y = 9 + FlagLayout[index * 2 + 1];

                if (flag.HasPath(dir))
                {
                    uint sprite = 0xdcu; // minus box

                    if (flag.HasTransporter(dir))
                        sprite = 0x120u; // check box

                    SetIcon(x, y, sprite);
                }
            }

            /* TODO show path merge button. */
            /* if (r == 0) draw_popup_icon(7, 51, 0x135); */

            SetText(8, 13, "Transport Info:");
            SetButton(24, 105, 0x1cu, Action.SendGeologist); // send geologist
            SetButton(120, 137, 0x3c, Action.CloseBox); // exit

            // draw resources
            for (int i = 0; i < Flag.FLAG_MAX_RES_COUNT; ++i)
            {
                if (flag.GetResourceAtSlot(i) != Resource.Type.None)
                {
                    SetIcon(8 + 8 * (7 + 2 * (i & 3)), 97 + 16 * (i >> 2), 0x22u + (uint)flag.GetResourceAtSlot(i));
                }
            }
        }

        Building TryToOpenBuildingPopup()
        {
            if (interf.GetPlayer().tempIndex == 0)
            {
                interf.ClosePopup();
                return null;
            }

            var building = interf.Game.GetBuilding(interf.GetPlayer().tempIndex);

            if (building.IsBurning())
            {
                interf.ClosePopup();
                return null;
            }

            return building;
        }

        static readonly uint[] MapBuildingSerfSprite = new uint[]
        {
            0xdc, 0x13, 0x0d, 0x19,
            0x0f, 0xdc, 0xdc, 0xdc,
            0xdc, 0x10, 0xdc, 0xdc,
            0x16, 0x15, 0x14, 0x17,
            0x18, 0x0e, 0x12, 0x1a,
            0x1b, 0xdc, 0xdc, 0x12,
            0xdc
        };

        void DrawBuildingStockBox()
        {
            var building = TryToOpenBuildingPopup();

            if (building == null)
                return;

            // draw list of resources
            for (uint j = 0; j < Building.MaxStock; ++j)
            {
                if (building.IsStockActive(j))
                {
                    uint stock = building.GetResourceCountInStock(j);

                    if (stock > 0)
                    {
                        uint sprite = 34u + (uint)building.GetResourceTypeInStock(j);

                        for (int i = 0; i < stock; ++i)
                            SetIcon(8 + 8 * (8 - (int)stock + 2 * i), 119 - (int)j * 20, sprite);
                    }
                    else
                    {
                        SetIcon(64, 119 - (int)j * 20, 0xdcu); // minus box
                    }
                }
            }

            // draw picture of serf present
            uint serfSprite = 0xdcu; // minus box

            if (building.HasSerf())
                serfSprite = MapBuildingSerfSprite[(int)building.BuildingType];

            SetIcon(16, 45, serfSprite);

            // draw building
            var info = interf.RenderView.DataSource.GetSpriteInfo(Data.Resource.MapObject, Render.RenderBuilding.MapBuildingSprite[(int)building.BuildingType]);

            int x = (Width - info.Width) / 2;
            SetBuildingIcon(x, 39, Render.RenderBuilding.MapBuildingSprite[(int)building.BuildingType]);

            // draw texts
            SetText(16, 13, "Stock of");
            SetText(16, 23, "this building:");

            SetButton(120, 137, 60u, Action.CloseBox); // exit
        }

        void DrawCastleResourcesBox()
        {
            var building = TryToOpenBuildingPopup();

            if (building == null)
                return;

            var inventory = building.GetInventory();
            var resources = inventory.GetAllResources();

            DrawResourcesBox(resources);

            SetButton(104, 137, 61u, Action.ShowCastleSerfs); // flip
            SetButton(120, 137, 60u, Action.CloseBox); // exit
        }

        void DrawCastleSerfBox()
		{
            var building = TryToOpenBuildingPopup();

            if (building == null)
                return;

            var type = building.BuildingType;

            if (type != Building.Type.Stock && type != Building.Type.Castle)
            {
                interf.ClosePopup();
                return;
            }

            uint[] serfCounts = new uint[27];
            var inventory = building.GetInventory();

            foreach (var serf in interf.Game.GetSerfsInInventory(inventory))
            {
                ++serfCounts[(int)serf.GetSerfType()];
            }

            DrawSerfsBox(serfCounts, -1);

            SetButton(104, 137, 61u, Action.ShowResourceDirections); // flip
            SetButton(120, 137, 60u, Action.CloseBox); // exit
        }

        void DrawResourceDirectionBox()
		{
            var building = TryToOpenBuildingPopup();

            if (building == null)
                return;

            var type = building.BuildingType;

            if (type == Building.Type.Castle)
            {
                uint[] knights = new uint[5];

                // follow linked list of knights on duty
                uint serfIndex = building.GetFirstKnight();

                while (serfIndex != 0)
                {
                    var serf = interf.Game.GetSerf(serfIndex);
                    var serfType = serf.GetSerfType();

                    if (serfType < Serf.Type.Knight0 || serfType > Serf.Type.Knight4)
                        throw new ExceptionFreeserf("Not a knight among the castle defenders.");

                    ++knights[(int)(serfType - Serf.Type.Knight0)];

                    serfIndex = serf.GetNextKnight();
                }

                // 5 knight types
                for (int i = 0; i < 5; ++i)
                {
                    SetIcon(104, 25 + i * 20, (uint)(33 - i));
                    SetNumberText(120, 29 + i * 20, knights[4 - i]);
                }
            }
            else if (type != Building.Type.Stock)
            {
                interf.ClosePopup();
                return;
            }

            SetIcon(40, 25, 0x128); // resource direction box
            SetIcon(40, 89, 0x129); // serf direction box

            var inventory = building.GetInventory();
            var resourceMode = inventory.GetResourceMode();
            var serfMode = inventory.GetSerfMode();

            switch (resourceMode)
            {
                case Inventory.Mode.In:
                    SetIcon(80, 25, 288);
                    SetButton(80, 41, 220, Action.ResourceModeStop);
                    SetButton(80, 57, 220, Action.ResourceModeOut);
                    break;
                case Inventory.Mode.Stop:
                    SetButton(80, 25, 220, Action.ResourceModeIn);
                    SetIcon(80, 41, 288);                    
                    SetButton(80, 57, 220, Action.ResourceModeOut);
                    break;
                case Inventory.Mode.Out:
                    SetButton(80, 25, 220, Action.ResourceModeIn);
                    SetButton(80, 41, 220, Action.ResourceModeStop);
                    SetIcon(80, 57, 288);
                    break;
            }

            switch (serfMode)
            {
                case Inventory.Mode.In:
                    SetIcon(80, 89, 288);
                    SetButton(80, 105, 220, Action.SerfModeStop);
                    SetButton(80, 121, 220, Action.SerfModeOut);
                    break;
                case Inventory.Mode.Stop:
                    SetButton(80, 89, 220, Action.SerfModeIn);
                    SetIcon(80, 105, 288);
                    SetButton(80, 121, 220, Action.SerfModeOut);
                    break;
                case Inventory.Mode.Out:
                    SetButton(80, 89, 220, Action.SerfModeIn);
                    SetButton(80, 105, 220, Action.SerfModeStop);
                    SetIcon(80, 121, 288);
                    break;
            }

            SetButton(104, 137, 61u, Action.ShowCastleResources); // flip
            SetButton(120, 137, 60u, Action.CloseBox); // exit
        }

        void DrawKnightSettingsBox()
		{
            var player = interf.GetPlayer();

            // serf to knight rate setting
            SetIcon(24, 17, 9u); // settler
            SetIcon(104, 17, 29u); // knight
            slideBars[0].MoveTo(40, 21);
            slideBars[0].Displayed = Displayed;
            slideBars[0].Fill = (int)player.GetSerfToKnightRate() / SlideBarFactor;

            // manual knight conversion
            SetButton(24, 37, 300u, Action.TrainKnights);
            SetIcon(64, 53, 59u); // shield
            SetIcon(80, 53, 58u); // sword
            SetIcon(72, 37, 130u); // free serfs

            uint numConvertibleToKnights = 0;

            foreach (var inventory in interf.Game.GetPlayerInventories(player))
            {
                uint count = Math.Min(inventory.GetCountOf(Resource.Type.Sword), inventory.GetCountOf(Resource.Type.Shield));
                numConvertibleToKnights += Math.Min(count, inventory.FreeSerfCount());
            }

            SetNumberText(104, 49, numConvertibleToKnights);

            // knight morale and gold deposit
            SetText(56, 72, ((100 * player.GetKnightMorale()) / 0x1000).ToString() + "%");
            SetText(56, 82, player.GetGoldDeposited().ToString());
            SetIcon(32, 73, 304u); // scared knight
            SetIcon(96, 73, 303u); // angry knight

            // castle knights wanted
            SetNumberText(56, 128, player.GetCastleKnightsWanted());
            SetNumberText(56, 138, player.GetCastleKnights());
            SetButton(32, 129, 220u, Action.DecreaseCastleKnights); // minus
            SetButton(80, 129, 221u, Action.IncreaseCastleKnights); // plus

            // send strongest
            SetButton(24, 93, 302u, Action.SetCombatMode); // send strongest control
            // checkbox for send strongest
            if (player.SendStrongest())
            {
                SetButton(56, 93, 220u, Action.SetCombatModeWeak);
                SetIcon(56, 109, 288u);
            }
            else
            {
                SetIcon(56, 93, 288u);
                SetButton(56, 109, 220u, Action.SetCombatModeStrong);
            }

            // cycle knights
            SetButton(88, 93, 301u, Action.CycleKnights);

            // exit
            SetButton(120, 137, 60u, Action.ShowSettlerMenu); // exit button
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

        void draw_player_faces_box()
		{
			
		}

        void draw_demolish_box()
		{
			
		}

        void draw_save_box()
		{
			
		}

        void ActivateTransportItem(int index)
		{
            var player = interf.GetPlayer();
            int i;

            if (Box == Type.TransportPriorities)
            {
                for (i = 0; i < 26; ++i)
                {
                    if (player.GetFlagPriority((Resource.Type)i) == index)
                        break;
                }

                CurrentTransportPriorityItem = (uint)i + 1;
            }
            else // inventory priorities
            {
                for (i = 0; i < 26; ++i)
                {
                    if (player.GetInventoryPriority((Resource.Type)i) == index)
                        break;
                }

                CurrentInventoryPriorityItem = (uint)i + 1;
            }
		}

        void MoveTransportItem(bool up, bool toEnd)
		{
            var player = interf.GetPlayer();
            int[] priorities;
            int current = -1;

            if (Box == Type.TransportPriorities)
            {
                priorities = player.GetFlagPriorities();
                current = (int)CurrentTransportPriorityItem;
            }
            else // inventory priorities
            {
                priorities = player.GetInventoryPriorities();
                current = (int)CurrentInventoryPriorityItem;
            }

            int currentValue = priorities[current - 1];
            int nextValue = -1;

            if (up)
            {
                if (toEnd)
                {
                    nextValue = 26;
                }
                else
                {
                    nextValue = currentValue + 1;
                }
            }
            else // down
            {
                if (toEnd)
                {
                    nextValue = 1;
                }
                else
                {
                    nextValue = currentValue - 1;
                }
            }

            if (nextValue >= 1 && nextValue <= 26)
            {
                int delta = (nextValue > currentValue) ? -1 : 1;
                int min = (nextValue > currentValue) ? currentValue + 1 : nextValue;
                int max = (nextValue > currentValue) ? nextValue : currentValue - 1;

                for (int i = 0; i < 26; ++i)
                {
                    if (priorities[i] >= min && priorities[i] <= max)
                        priorities[i] += delta;
                }

                priorities[current - 1] = nextValue;
            }
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
                case Action.ShowIdleAndPotentialSettlerStats:
                    SetBox(Type.IdleAndPotentialSettlerStats);
                    break;
                case Action.ShowResourceStats:
                    SetBox(Type.ResourceStats);
                    break;
                case Action.ShowBuildingStats:
                    SetBox(Type.BuildingStats1);
                    break;
                case Action.ShowSettlerStats:
                    SetBox(Type.SettlerStats);
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
                case Action.ShowTransportPriorities:
                    SetBox(Type.TransportPriorities);
                    break;
                case Action.ShowInventoryPriorities:
                    SetBox(Type.InventoryPriorities);
                    break;
                case Action.ShowKnightSettings:
                    SetBox(Type.KnightSettingsBox);
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
                case Action.DefaultTransportPriorities:
                    if (Box == Type.TransportPriorities)
                        player.ResetFlagPriority();
                    else
                        player.ResetInventoryPriority();
                    break;
                case Action.TransportPriorityToTop:
                    MoveTransportItem(true, true);
                    break;
                case Action.TransportPriorityToBottom:
                    MoveTransportItem(false, true);
                    break;
                case Action.TransportPriorityUp:
                    MoveTransportItem(true, false);
                    break;
                case Action.TransportPriorityDown:
                    MoveTransportItem(false, false);
                    break;
                case Action.SetTransportItem1:
                case Action.SetTransportItem2:
                case Action.SetTransportItem3:
                case Action.SetTransportItem4:
                case Action.SetTransportItem5:
                case Action.SetTransportItem6:
                case Action.SetTransportItem7:
                case Action.SetTransportItem8:
                case Action.SetTransportItem9:
                case Action.SetTransportItem10:
                case Action.SetTransportItem11:
                case Action.SetTransportItem12:
                case Action.SetTransportItem13:
                case Action.SetTransportItem14:
                case Action.SetTransportItem15:
                case Action.SetTransportItem16:
                case Action.SetTransportItem17:
                case Action.SetTransportItem18:
                case Action.SetTransportItem19:
                case Action.SetTransportItem20:
                case Action.SetTransportItem21:
                case Action.SetTransportItem22:
                case Action.SetTransportItem23:
                case Action.SetTransportItem24:
                case Action.SetTransportItem25:
                case Action.SetTransportItem26:
                    {
                        int item = action - Action.SetTransportItem1;
                        ActivateTransportItem(26 - item);
                    }
                    break;
                case Action.BuildingStatsFlip:
                    if (Box == Type.BuildingStats4)
                        SetBox(Type.BuildingStats1);
                    else
                        SetBox(Box + 1);
                    break;
                case Action.TrainKnights:
                    // the button/icon is 32x32
                    if (x < 16)
                    {
                        if (y < 16)
                            PromoteKnights(1);
                        else
                            PromoteKnights(20);
                    }
                    else
                    {
                        if (y < 16)
                            PromoteKnights(5);
                        else
                            PromoteKnights(100);
                    }
                    break;
                case Action.SetCombatMode:
                    // the button/icon is 32x32 but we use only the right 16x32 half of it
                    if (x >= 16)
                    {
                        if (y < 16)
                            HandleAction(Action.SetCombatModeWeak, x, y);
                        else
                            HandleAction(Action.SetCombatModeStrong, x, y);
                    }
                    break;
                case Action.SetCombatModeWeak:
                    if (player.SendStrongest())
                    {
                        player.DropSendStrongest();
                        PlaySound(Audio.TypeSfx.Accepted);
                    }
                    break;
                case Action.SetCombatModeStrong:
                    if (!player.SendStrongest())
                    {
                        player.SetSendStrongest();
                        PlaySound(Audio.TypeSfx.Accepted);
                    }
                    break;
                case Action.CycleKnights:
                    player.CycleKnights();
                    PlaySound(Audio.TypeSfx.Accepted);
                    break;
                case Action.DecreaseCastleKnights:
                    player.DecreaseCastleKnightsWanted();
                    break;
                case Action.IncreaseCastleKnights:
                    player.IncreaseCastleKnightsWanted();
                    break;
                case Action.ShowCastleResources:
                    SetBox(Type.CastleResources);
                    break;
                case Action.ShowCastleSerfs:
                    SetBox(Type.CastleSerfs);
                    break;
                case Action.ShowResourceDirections:
                    SetBox(Type.ResourceDirections);
                    break;
                case Action.ResourceModeIn:
                case Action.ResourceModeStop:
                case Action.ResourceModeOut:
                case Action.SerfModeIn:
                case Action.SerfModeStop:
                case Action.SerfModeOut:
                    SetInventoryMode(action);
                    break;
                case Action.MinimapMode:
                    if (MiniMap.GetOwnershipMode() == MinimapGame.OwnershipMode.Last)
                        MiniMap.SetOwnershipMode(MinimapGame.OwnershipMode.None);
                    else
                        MiniMap.SetOwnershipMode(MiniMap.GetOwnershipMode() + 1);
                    break;
                case Action.MinimapRoads:
                    MiniMap.SetDrawRoads(!MiniMap.DrawRoads);
                    break;
                case Action.MinimapBuildings:
                    MiniMap.SetDrawBuildings(!MiniMap.DrawBuildings);
                    break;
                case Action.MinimapGrid:
                    MiniMap.SetDrawGrid(!MiniMap.DrawGrid);
                    break;
                case Action.MinimapScale:
                    MiniMap.SetScale(MiniMap.GetScale() == 1 ? 2 : 1);
                    break;
                case Action.SendGeologist:
                    {
                        var pos = interf.GetMapCursorPos();
                        Flag flag = interf.Game.GetFlagAtPos(pos);

                        if (!interf.Game.SendGeologist(flag))
                        {
                            PlaySound(Audio.TypeSfx.NotAccepted);
                        }
                        else
                        {
                            PlaySound(Audio.TypeSfx.Accepted);
                            interf.ClosePopup();
                        }
                    }
                    break;
                // TODO ...
                default:
                    Log.Warn.Write("popup", "unhandled action " + action.ToString());
                    break;
            }
		}

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
                    DrawMapBox();
                    break;
                case Type.MineBuilding:
                    DrawMineBuildingBox();
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
                case Type.BuildingStats1:
                    DrawBuildingStats1Box();
                    break;
                case Type.BuildingStats2:
                    DrawBuildingStats2Box();
                    break;
                case Type.BuildingStats3:
                    DrawBuildingStats3Box();
                    break;
                case Type.BuildingStats4:
                    DrawBuildingStats4Box();
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
                case Type.SettlerStats:
                    DrawSerfCountBox();
                    break;
                case Type.IdleAndPotentialSettlerStats:
                    DrawIdleAndPotentialSerfsBox();
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
                case Type.InventoryPriorities:
                    DrawTransportPrioritiesBox();
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
                case Type.CastleResources:
                    DrawCastleResourcesBox();
                    break;
                case Type.MineOutput:
                    draw_mine_output_box();
                    break;
                case Type.OrderedBld:
                    DrawOrderedBuildingBox();
                    break;
                case Type.Defenders:
                    draw_defenders_box();
                    break;
                case Type.TransportInfo:
                    DrawTransportInfoBox();
                    break;
                case Type.CastleSerfs:
                    DrawCastleSerfBox();
                    break;
                case Type.ResourceDirections:
                    DrawResourceDirectionBox();
                    break;
                case Type.KnightSettingsBox:
                    DrawKnightSettingsBox();
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
                case Type.BuildingStock:
                    DrawBuildingStockBox();
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

        void PromoteKnights(int number)
        {
            var player = interf.GetPlayer();

            if (player.PromoteSerfsToKnights(number) == 0)
                PlaySound(Audio.TypeSfx.NotAccepted);
            else
                PlaySound(Audio.TypeSfx.Accepted);
        }

        void SetInventoryMode(Action action)
        {
            var building = interf.Game.GetBuilding(interf.GetPlayer().tempIndex);
            var inventory = building.GetInventory();

            switch (action)
            {
                case Action.ResourceModeIn:
                    inventory.SetResourceMode(Inventory.Mode.In);
                    break;
                case Action.ResourceModeStop:
                    inventory.SetResourceMode(Inventory.Mode.Stop);
                    break;
                case Action.ResourceModeOut:
                    inventory.SetResourceMode(Inventory.Mode.Out);
                    break;
                case Action.SerfModeIn:
                    inventory.SetSerfMode(Inventory.Mode.In);
                    break;
                case Action.SerfModeStop:
                    inventory.SetSerfMode(Inventory.Mode.Stop);
                    break;
                case Action.SerfModeOut:
                    inventory.SetSerfMode(Inventory.Mode.Out);
                    break;
                default:
                    return;
            }

            PlaySound(Audio.TypeSfx.Accepted);
        }
    }
}
