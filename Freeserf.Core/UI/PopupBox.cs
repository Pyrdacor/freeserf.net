/*
 * PopupBox.cs - Popup GUI component
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
using System.IO;
using System.Linq;

namespace Freeserf.UI
{
    using Freeserf.Data;
    using ResourceMap = Dictionary<Resource.Type, uint>;

    // TODO: If stats should reflect the current state we have
    //       to redraw the stat popup from time to time.
    internal class PopupBox : Box
    {
        public enum Type
        {
            None = 0,
            Map,
            MapOverlay, // UNUSED 
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
            PlayerStatistics,
            ResourceStatistics,
            FoodProductionCycle,
            MaterialProductionCycle,
            SettlerStats,
            IdleAndPotentialSettlerStats,
            StartAttack,
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
            SettSelectFile, // UNUSED 
            Options,
            ExtendedOptions,
            ScrollOptions,
            GameInitOptions,
            ExtendedGameInitOptions,
            GameInitScrollOptions,
            CastleResources,
            MineOutput,
            OrderedBld,
            Defenders,
            TransportInfo,
            CastleSerfs,
            ResourceDirections,
            KnightSettings,
            InventoryPriorities,
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
            OverallComparison = 132,  // sword + building + land
            RuralProperties = 133,  // land
            Buildings = 134,  // buildings
            CombatPower = 135,  // sword
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
            ShowFoodProductionCycle,
            ShowMaterialProductionCycle,
            ShowPlayerStatistics,
            ShowBuildingStats,
            ShowSettlerStats,
            ShowResourceStatistics,
            ShowResourceStats,
            ShowIdleAndPotentialSettlerStats,
            ShowStatMenu,
            BuildingStatsFlip,
            CloseBox,
            PlayerStatisticsSetAspectAll,
            PlayerStatisticsSetAspectLand,
            PlayerStatisticsSetAspectBuildings,
            PlayerStatisticsSetAspectMilitary,
            PlayerStatisticsSetScale30Min,
            PlayerStatisticsSetScale120Min,
            PlayerStatisticsSetScale600Min,
            PlayerStatisticsSetScale3000Min,
            ResourceStatisticsSelectFish,
            ResourceStatisticsSelectPig,
            ResourceStatisticsSelectMeat,
            ResourceStatisticsSelectWheat,
            ResourceStatisticsSelectFlour,
            ResourceStatisticsSelectBread,
            ResourceStatisticsSelectLumber,
            ResourceStatisticsSelectPlank,
            ResourceStatisticsSelectBoat,
            ResourceStatisticsSelectStone,
            ResourceStatisticsSelectIronore,
            ResourceStatisticsSelectSteel,
            ResourceStatisticsSelectCoal,
            ResourceStatisticsSelectGoldore,
            ResourceStatisticsSelectGoldbar,
            ResourceStatisticsSelectShovel,
            ResourceStatisticsSelectHammer,
            ResourceStatisticsSelectRod,
            ResourceStatisticsSelectCleaver,
            ResourceStatisticsSelectScythe,
            ResourceStatisticsSelectAxe,
            ResourceStatisticsSelectSaw,
            ResourceStatisticsSelectPick,
            ResourceStatisticsSelectPincer,
            ResourceStatisticsSelectSword,
            ResourceStatisticsSelectShield,
            AttackingKnightsDec,
            AttackingKnightsInc,
            StartAttack,
            ShowFoodDistribution,
            ShowPlanksAndSteelDistribution,
            ShowCoalAndWheatDistribution,
            ShowKnightLevel,
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
            SaveAndQuit,
            ShowQuit,
            ShowOptions,
            ShowExtendedOptions,
            ShowScrollOptions,
            ShowSave,
            CycleKnights,
            OptionsMessageCount,
            ShowSettSelectFile, // Unused 
            ShowStatSelectFile, // Unused 
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
            CloseGroundAnalysis,
            MergePaths,
            DecreaseCastleKnights,
            IncreaseCastleKnights,
            OptionsMusic,
            OptionsFullscreen,
            OptionsVolumeMinus,
            OptionsVolumePlus,
            OptionsPathwayScrolling,
            OptionsFastMapclick,
            OptionsFastBuilding,
            OptionsInvertScrolling,
            OptionsHideCursorWhileScrolling,
            OptionsResetCursorAfterScrolling,
            Demolish,
            OptionsSfx,
            Save,
            NewName,
            JumpToPlayer1,
            JumpToPlayer2,
            JumpToPlayer3,
            JumpToPlayer4
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
        readonly SlideBar[] slideBars = new SlideBar[9];
        const int SlideBarFactor = 1310;
        TextField clickableTextField = null;
        readonly Render.IColoredRect[] playerFaceBackgrounds = new Render.IColoredRect[Game.GAME_MAX_PLAYER_COUNT];

        uint CurrentTransportPriorityItem = 0u;
        uint CurrentInventoryPriorityItem = 0u;
        int currentResourceForStatistics = 0;
        int currentPlayerStatisticsMode = 0;
        int iconLayer = 0;

        static UI.BackgroundPattern[] backgrounds = null;

        void SaveCurrentGame()
        {
            string fileName = Path.GetFileName(fileField.Text);
            var extension = Path.GetExtension(fileName).ToLower();

            if (extension != ".save")
                fileName = Path.GetFileNameWithoutExtension(fileName) + ".save";

            fileName = Path.Combine(fileList.GetFolderPath(), fileName);

            if (GameManager.Instance.SaveCurrentGame(fileName))
            {
                interf.ClosePopup();
            }

            // TODO: status messages, sounds?
        }

        static void InitBackgrounds(Render.ISpriteFactory spriteFactory)
        {
            if (backgrounds != null)
                return;

            var patterns = Enum.GetValues(typeof(BackgroundPattern));
            int index = 0;

            backgrounds = new UI.BackgroundPattern[patterns.Length];

            foreach (BackgroundPattern pattern in patterns)
            {
                if (pattern >= BackgroundPattern.OverallComparison && pattern <= BackgroundPattern.CombatPower)
                    backgrounds[index++] = UI.BackgroundPattern.CreatePlayerStatisticPopupBoxBackground(spriteFactory, (uint)pattern);
                else if (pattern >= BackgroundPattern.Fish && pattern <= BackgroundPattern.Shield)
                    backgrounds[index++] = UI.BackgroundPattern.CreateResourceStatisticPopupBoxBackground(spriteFactory, (uint)pattern);
                else
                    backgrounds[index++] = UI.BackgroundPattern.CreatePopupBoxBackground(spriteFactory, 320u + (uint)pattern);
            }
        }

        void InitPlayerFaceBackgrounds(Render.IColoredRectFactory coloredRectFactory)
        {
            for (int i = 0; i < Game.GAME_MAX_PLAYER_COUNT; ++i)
            {
                var playerColor = PlayerInfo.PlayerColors[i];
                var color = new Render.Color()
                {
                    R = playerColor.Red,
                    G = playerColor.Green,
                    B = playerColor.Blue,
                    A = 255
                };

                playerFaceBackgrounds[i] = coloredRectFactory.Create(64, 72, color, 1);
            }
        }

        protected internal override void UpdateParent()
        {
            base.UpdateParent();

            for (int i = 0; i < Game.GAME_MAX_PLAYER_COUNT; ++i)
            {
                playerFaceBackgrounds[i].DisplayLayer = (byte)(BaseDisplayLayer + 10);
            }
        }

        static bool FileInputFilter(char key, TextInput textInput)
        {
            if (textInput.Text.Length + 1 > (textInput.Width - textInput.Padding.X) / 8)
            {
                return false;
            }

            if ((key >= '0' && key <= '9') ||
                (key >= 'A' && key <= 'Z') ||
                (key == 'Ä' || key == 'Ö' || key == 'Ü') ||
                (key == 'ä' || key == 'ö' || key == 'ü') ||
                (key == ' ' || key == '-'))
            {
                return true;
            }

            return false;
        }

        static string TrimFileName(string text, int width)
        {
            if (text.Length * 8 <= width)
                return text;

            return text.Substring(0, width / 8);
        }

        public PopupBox(Interface interf)
            : base
            (
                  interf,
                  UI.BackgroundPattern.CreatePopupBoxBackground(interf.RenderView.SpriteFactory, 320u + (uint)BackgroundPattern.StripedGreen),
                  Border.CreatePopupBoxBorder(interf.RenderView.SpriteFactory)
            )
        {
            InitBackgrounds(interf.RenderView.SpriteFactory);
            InitPlayerFaceBackgrounds(interf.RenderView.ColoredRectFactory);

            this.interf = interf;
            MiniMap = new MinimapGame(interf, interf.Game);
            fileList = new ListSavedFiles(interf);
            fileField = new TextInput(interf, 8);

            CurrentTransportPriorityItem = 8;
            CurrentInventoryPriorityItem = 15;
            currentResourceForStatistics = 7;
            currentPlayerStatisticsMode = 0;

            // Initialize minimap 
            MiniMap.SetSize(128, 128);
            AddChild(MiniMap, 8, 8, false);

            fileList.SetSize(124, 102);
            fileList.SetSelectionHandler((GameStore.SaveInfo item) =>
            {
                fileField.Text = TrimFileName(Path.GetFileNameWithoutExtension(item.Path), fileField.Width - 3);
            });
            AddChild(fileList, 10, 22, false);

            fileField.Padding = new Position(3, 1);
            fileField.SetSize(124, 11);
            fileField.SetFilter(FileInputFilter);
            AddChild(fileField, 10, 125, false);

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

        void PopupBox_SlideBarFillChanged(object sender, System.EventArgs args)
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
            if (interf.AccessRights != Viewer.Access.Player)
            {
                PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                SetRedraw(); // redraw the slidebar
                return;
            }

            var player = interf.Player;
            uint realAmount = (uint)slideBars[index].Fill * SlideBarFactor;

            switch (Box)
            {
                case Type.FoodDistribution:
                    if (index == 0) // stonemine food
                        player.FoodStonemine = realAmount;
                    else if (index == 1) // coalmine food
                        player.FoodCoalmine = realAmount;
                    else if (index == 2) // ironmine food
                        player.FoodIronmine = realAmount;
                    else if (index == 3) // goldmine food
                        player.FoodGoldmine = realAmount;
                    break;
                case Type.PlanksAndSteelDistribution:
                    if (index == 0) // construction planks
                        player.PlanksConstruction = realAmount;
                    else if (index == 1) // boatbuilder planks
                        player.PlanksBoatbuilder = realAmount;
                    else if (index == 2) // toolmaker planks
                        player.PlanksToolmaker = realAmount;
                    else if (index == 3) // toolmaker steel
                        player.SteelToolmaker = realAmount;
                    else if (index == 4) // weaponsmith steel
                        player.SteelWeaponsmith = realAmount;
                    break;
                case Type.CoalAndWheatDistribution:
                    if (index == 0) // steelsmelter coal
                        player.CoalSteelsmelter = realAmount;
                    else if (index == 1) // goldsmelter coal
                        player.CoalGoldsmelter = realAmount;
                    else if (index == 2) // weaponsmith coal
                        player.CoalWeaponsmith = realAmount;
                    else if (index == 3) // pigfarm wheat
                        player.WheatPigfarm = realAmount;
                    else if (index == 4) // mill wheat
                        player.WheatMill = realAmount;
                    break;
                case Type.ToolmakerPriorities:
                    player.SetToolPriority(index, (int)realAmount);
                    break;
                case Type.KnightSettings:
                    if (index == 0) // serf to knight rate
                        player.SerfToKnightRate = (int)realAmount;
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
                action == Action.SerfModeOut ||
                action == Action.MergePaths)
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
                action != Action.SerfModeOut &&
                action != Action.MergePaths)
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
            if (interf.AccessRights == Viewer.Access.RestrictedSpectator)
            {
                switch (box)
                {
                    case Type.CastleResources:
                    case Type.CastleSerfs:
                    case Type.Demolish:
                    case Type.Adv1Bld:
                    case Type.Adv2Bld:
                    case Type.BasicBld:
                    case Type.BasicBldFlip:
                    case Type.MineBuilding:
                    case Type.GroundAnalysis:
                    case Type.ResourceDirections:
                    case Type.StartAttack:
                        interf.ClosePopup();
                        return;
                    case Type.CoalAndWheatDistribution:
                    case Type.FoodDistribution:
                    case Type.PlanksAndSteelDistribution:
                    case Type.InventoryPriorities:
                    case Type.IdleAndPotentialSettlerStats:
                    case Type.KnightLevel:
                    case Type.KnightSettings:
                    case Type.ResourceStatistics:
                    case Type.ResourceStats:
                    case Type.SettlerStats:
                    case Type.ToolmakerPriorities:
                    case Type.TransportPriorities:
                    case Type.BuildingStats1:
                    case Type.BuildingStats2:
                    case Type.BuildingStats3:
                    case Type.BuildingStats4:
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                        return;
                    case Type.BuildingStock:
                    case Type.MineOutput:
                    case Type.Defenders:
                        // TODO: not sure about these
                        break;
                }
            }
            else if (interf.AccessRights == Viewer.Access.Spectator)
            {
                switch (box)
                {
                    case Type.Demolish:
                    case Type.Adv1Bld:
                    case Type.Adv2Bld:
                    case Type.BasicBld:
                    case Type.BasicBldFlip:
                    case Type.MineBuilding:
                    case Type.GroundAnalysis:
                    case Type.StartAttack:
                        interf.ClosePopup();
                        return;
                }
            }

            Box = box;

            MiniMap.Displayed = box == Type.Map || box == Type.ResourceStatistics || box == Type.PlayerStatistics;
            fileList.Displayed = box == Type.LoadSave;
            fileField.Displayed = box == Type.LoadSave;

            if (box == Type.LoadSave)
                fileList.Update();

            SetBackground(BackgroundFromType());

            bool showPlayerFaceBackgrounds = box == Type.PlayerFaces;

            for (int i = 0; i < Game.GAME_MAX_PLAYER_COUNT; ++i)
            {
                playerFaceBackgrounds[i].Visible = showPlayerFaceBackgrounds;
            }

            SetRedraw();
        }

        UI.BackgroundPattern BackgroundFromType()
        {
            BackgroundPattern pattern = BackgroundPattern.StripedGreen;

            switch (Box)
            {
                case Type.None:
                    return null;
                default:
                case Type.Map:
                case Type.MapOverlay: // UNUSED 
                    break; // no background, but just use default from above
                case Type.MineBuilding:
                case Type.BasicBld:
                case Type.BasicBldFlip:
                case Type.Adv1Bld:
                case Type.Adv2Bld:
                case Type.StartAttack:
                    pattern = BackgroundPattern.Construction;
                    break;
                case Type.GroundAnalysis:
                case Type.StatMenu:
                case Type.ResourceStats:
                case Type.BuildingStats1:
                case Type.BuildingStats2:
                case Type.BuildingStats3:
                case Type.BuildingStats4:
                case Type.FoodProductionCycle:
                case Type.MaterialProductionCycle:
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
                case Type.KnightSettings:
                    pattern = BackgroundPattern.CheckerdDiagonalBrown;
                    break;
                case Type.QuitConfirm:
                case Type.NoSaveQuitConfirm:
                case Type.Options:
                case Type.ExtendedOptions:
                case Type.ScrollOptions:
                case Type.GameInitOptions:
                case Type.ExtendedGameInitOptions:
                case Type.GameInitScrollOptions:
                case Type.LoadSave:
                    pattern = BackgroundPattern.DiagonalGreen;
                    break;
                case Type.Message:
                case Type.SettSelectFile: // UNUSED 
                case Type.LoadArchive:
                case Type.Type25:
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
                case Type.ResourceStatistics:
                    pattern = BackgroundPattern.Fish + currentResourceForStatistics - 1;
                    break;
                case Type.PlayerStatistics:
                    pattern = BackgroundPattern.OverallComparison + ((currentPlayerStatisticsMode >> 2) & 3);
                    break;
                case Type.DiskMsg: // save/load success or error
                    pattern = BackgroundPattern.StaresGreen;
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
            var playerIndex = interf.Player.Index;

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
                SetText(x, y, number.ToString(), Render.TextRenderType.LegacySpecialDigits);
            }
        }

        TextField SetText(int x, int y, string text, Render.TextRenderType renderType = Render.TextRenderType.Legacy)
        {
            // check if there is a free text
            foreach (var textField in texts.Keys.ToList())
            {
                if (texts[textField] == false)
                {
                    textField.Text = text;
                    textField.MoveTo(x, y);
                    textField.SetRenderType(renderType);
                    textField.Displayed = Displayed;
                    texts[textField] = true;
                    return textField;
                }
            }

            var newText = new TextField(interf, 1, 8, renderType);

            newText.Text = text;
            newText.Displayed = Displayed;
            AddChild(newText, x, y, true);

            texts.Add(newText, true);

            return newText;
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
            var playerIndex = interf.Player.Index;

            SetBuildingIcon(x, y, 128u + playerIndex);
        }

        void SetIcon(int x, int y, uint spriteIndex, Data.Resource resourceType = Data.Resource.Icon, bool building = false)
        {
            var info = interf.RenderView.DataSource.GetSpriteInfo(resourceType, spriteIndex);
            byte displayLayer = (byte)(Math.Min(255, iconLayer++));

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
                        icon.SetDisplayLayerOffset(displayLayer);
                        icons[icon] = true;
                        return;
                    }
                }
            }

            var newIcon = (building) ? new BuildingIcon(interf, info.Width, info.Height, spriteIndex, displayLayer) :
                new Icon(interf, info.Width, info.Height, resourceType, spriteIndex, displayLayer);

            ++iconLayer;

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

            iconLayer = 0;
        }

        #endregion


        #region Buttons

        void SetButton(int x, int y, uint spriteIndex, object tag, Data.Resource resourceType = Data.Resource.Icon)
        {
            var info = interf.RenderView.DataSource.GetSpriteInfo(resourceType, spriteIndex);
            byte displayLayer = (byte)(Math.Min(255, iconLayer++));

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
                    button.SetDisplayLayerOffset(displayLayer);
                    buttons[button] = true;
                    return;
                }
            }

            var newButton = new Button(interf, info.Width, info.Height, resourceType, spriteIndex, displayLayer);

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


        // Get the sprite number for a face
        static uint GetPlayerFaceSprite(PlayerFace face)
        {
            if (face != 0)
                return 0x10bu + (uint)face;

            return 0x119u; // sprite_face_none 
        }

        void DrawMapBox()
        {
            MiniMap.SetSize(128, 128);

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
            int num = 4;

            SetBuilding(index++, 24, 17, Building.Type.StoneMine);
            SetBuilding(index++, 72, 17, Building.Type.CoalMine);
            SetBuilding(index++, 40, 86, Building.Type.IronMine);
            SetBuilding(index++, 88, 86, Building.Type.GoldMine);

            if (interf.Game.CanBuildFlag(interf.GetMapCursorPosition(), interf.Player))
            {
                SetFlag(index, 24, 123);
                ++num;
            }

            ShowBuildings(num);
        }

        // flip means the user can change the page
        void DrawBasicBuildingBox(bool flip)
        {
            int num = 6;
            int index = 0;

            // add hut if military buildings are possible
            if (interf.Game.CanBuildMilitary(interf.GetMapCursorPosition()))
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

            if (interf.Game.CanBuildFlag(interf.GetMapCursorPosition(), interf.Player))
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
            if (interf.Game.CanBuildMilitary(interf.GetMapCursorPosition()))
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
            0x28, 1, 0, // resources 
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
            SetIcon(16, 9, 0x28);
            SetIcon(16, 25, 0x29);
            SetIcon(16, 41, 0x2a);
            SetIcon(16, 57, 0x2b);
            SetIcon(16, 73, 0x2e);
            SetIcon(16, 89, 0x2c);
            SetIcon(16, 105, 0x2d);
            SetIcon(16, 121, 0x2f);
            SetIcon(16, 137, 0x30);
            SetIcon(56, 9, 0x31);
            SetIcon(56, 25, 0x32);
            SetIcon(56, 41, 0x36);
            SetIcon(56, 57, 0x37);
            SetIcon(56, 73, 0x35);
            SetIcon(56, 89, 0x38);
            SetIcon(56, 105, 0x39);
            SetIcon(56, 121, 0x34);
            SetIcon(56, 137, 0x33);
            SetIcon(96, 9, 0x3a);
            SetIcon(96, 25, 0x3b);
            SetIcon(96, 41, 0x22);
            SetIcon(96, 57, 0x23);
            SetIcon(96, 73, 0x24);
            SetIcon(96, 89, 0x25);
            SetIcon(96, 105, 0x26);
            SetIcon(96, 121, 0x27);

            SetNumberText(32, 13, (uint)resources[Resource.Type.Lumber]);
            SetNumberText(32, 29, (uint)resources[Resource.Type.Plank]);
            SetNumberText(32, 45, (uint)resources[Resource.Type.Boat]);
            SetNumberText(32, 61, (uint)resources[Resource.Type.Stone]);
            SetNumberText(32, 77, (uint)resources[Resource.Type.Coal]);
            SetNumberText(32, 93, (uint)resources[Resource.Type.IronOre]);
            SetNumberText(32, 109, (uint)resources[Resource.Type.Steel]);
            SetNumberText(32, 125, (uint)resources[Resource.Type.GoldOre]);
            SetNumberText(32, 141, (uint)resources[Resource.Type.GoldBar]);
            SetNumberText(72, 13, (uint)resources[Resource.Type.Shovel]);
            SetNumberText(72, 29, (uint)resources[Resource.Type.Hammer]);
            SetNumberText(72, 45, (uint)resources[Resource.Type.Axe]);
            SetNumberText(72, 61, (uint)resources[Resource.Type.Saw]);
            SetNumberText(72, 77, (uint)resources[Resource.Type.Scythe]);
            SetNumberText(72, 93, (uint)resources[Resource.Type.Pick]);
            SetNumberText(72, 109, (uint)resources[Resource.Type.Pincer]);
            SetNumberText(72, 125, (uint)resources[Resource.Type.Cleaver]);
            SetNumberText(72, 141, (uint)resources[Resource.Type.Rod]);
            SetNumberText(112, 13, (uint)resources[Resource.Type.Sword]);
            SetNumberText(112, 29, (uint)resources[Resource.Type.Shield]);
            SetNumberText(112, 45, (uint)resources[Resource.Type.Fish]);
            SetNumberText(112, 61, (uint)resources[Resource.Type.Pig]);
            SetNumberText(112, 77, (uint)resources[Resource.Type.Meat]);
            SetNumberText(112, 93, (uint)resources[Resource.Type.Wheat]);
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
            SetButton(16, 21, 72u, Action.ShowFoodProductionCycle);
            SetButton(56, 21, 73u, Action.ShowMaterialProductionCycle);
            SetButton(96, 21, 77u, Action.ShowIdleAndPotentialSettlerStats);

            SetButton(16, 65, 74u, Action.ShowResourceStats);
            SetButton(56, 65, 76u, Action.ShowBuildingStats);
            SetButton(96, 65, 75u, Action.ShowSettlerStats);

            SetButton(16, 109, 71u, Action.ShowResourceStatistics);
            SetButton(56, 109, 70u, Action.ShowPlayerStatistics);

            SetButton(104, 113, 61u, Action.ShowSettlerMenu);
            SetButton(120, 137, 60u, Action.CloseBox);

        }

        void DrawResourceStatsBox()
        {
            DrawResourcesBox(interf.Player.GetStatsResources());

            SetButton(120, 137, 60u, Action.ShowStatMenu);
        }

        void DrawBuildingCount(int x, int y, Building.Type type)
        {
            var player = interf.Player;

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

        void DrawResourceStatisticsBox()
        {
            SetButton(8, 84, 0x28, Action.ResourceStatisticsSelectLumber); // lumber
            SetButton(24, 84, 0x29, Action.ResourceStatisticsSelectPlank); // plank
            SetButton(40, 84, 0x2b, Action.ResourceStatisticsSelectStone); // stone

            SetButton(8, 100, 0x2e, Action.ResourceStatisticsSelectCoal); // coal
            SetButton(24, 100, 0x2c, Action.ResourceStatisticsSelectIronore); // iron ore
            SetButton(40, 100, 0x2f, Action.ResourceStatisticsSelectGoldore); // gold ore

            SetButton(8, 116, 0x2a, Action.ResourceStatisticsSelectBoat); // boat
            SetButton(24, 116, 0x2d, Action.ResourceStatisticsSelectSteel); // iron
            SetButton(40, 116, 0x30, Action.ResourceStatisticsSelectGoldbar); // gold bar

            SetButton(64, 92, 0x3a, Action.ResourceStatisticsSelectSword); // sword
            SetButton(64, 108, 0x3b, Action.ResourceStatisticsSelectShield); // shield

            SetButton(88, 84, 0x31, Action.ResourceStatisticsSelectShovel); // shovel
            SetButton(104, 84, 0x32, Action.ResourceStatisticsSelectHammer); // hammer
            SetButton(120, 84, 0x36, Action.ResourceStatisticsSelectAxe); // axe

            SetButton(88, 100, 0x37, Action.ResourceStatisticsSelectSaw); // saw
            SetButton(104, 100, 0x38, Action.ResourceStatisticsSelectPick); // pick
            SetButton(120, 100, 0x35, Action.ResourceStatisticsSelectScythe); // scythe

            SetButton(88, 116, 0x34, Action.ResourceStatisticsSelectCleaver); // cleaver
            SetButton(104, 116, 0x39, Action.ResourceStatisticsSelectPincer); // pincer
            SetButton(120, 116, 0x33, Action.ResourceStatisticsSelectRod); // rod

            SetButton(16, 134, 0x22, Action.ResourceStatisticsSelectFish); // fish
            SetButton(32, 134, 0x23, Action.ResourceStatisticsSelectPig); // pig
            SetButton(48, 134, 0x24, Action.ResourceStatisticsSelectMeat); // meat
            SetButton(64, 134, 0x25, Action.ResourceStatisticsSelectWheat); // wheat
            SetButton(80, 134, 0x26, Action.ResourceStatisticsSelectFlour); // flour
            SetButton(96, 134, 0x27, Action.ResourceStatisticsSelectBread); // bread

            // axis icons
            SetIcon(8, 73, 0x59);
            SetIcon(120, 9, 0x5a);

            // exit button
            SetButton(120, 137, 60u, Action.ShowStatMenu);

            int[] sampleWeights = { 4, 6, 8, 9, 10, 9, 8, 6, 4 };

            // Create array of historical counts
            int[] historicalData = new int[112];
            int maxValue = 0;
            int index = interf.Game.GetResourceHistoryIndex();
            Resource.Type resource = (Resource.Type)(currentResourceForStatistics - 1);
            var resourceCountHistory = interf.Player.GetResourceCountHistory(resource);

            for (int i = 0; i < 112; ++i)
            {
                historicalData[i] = 0;
                int j = index;

                for (int k = 0; k < 9; ++k)
                {
                    historicalData[i] += sampleWeights[k] * (int)resourceCountHistory[j];
                    j = j > 0 ? j - 1 : 119;
                }

                if (historicalData[i] > maxValue)
                    maxValue = historicalData[i];

                index = index > 0 ? index - 1 : 119;
            }

            uint[] axisIcons1 = { 110, 109, 108, 107 };
            uint[] axisIcons2 = { 112, 111, 110, 108 };
            uint[] axisIcons3 = { 114, 113, 112, 110 };
            uint[] axisIcons4 = { 117, 116, 114, 112 };
            uint[] axisIcons5 = { 120, 119, 118, 115 };
            uint[] axisIcons6 = { 122, 121, 120, 118 };
            uint[] axisIcons7 = { 125, 124, 122, 120 };
            uint[] axisIcons8 = { 128, 127, 126, 123 };

            uint[] axisIcons = null;
            int multiplier = 0;

            if (maxValue <= 64)
            {
                axisIcons = axisIcons1;
                multiplier = 0x8000;
            }
            else if (maxValue <= 128)
            {
                axisIcons = axisIcons2;
                multiplier = 0x4000;
            }
            else if (maxValue <= 256)
            {
                axisIcons = axisIcons3;
                multiplier = 0x2000;
            }
            else if (maxValue <= 512)
            {
                axisIcons = axisIcons4;
                multiplier = 0x1000;
            }
            else if (maxValue <= 1280)
            {
                axisIcons = axisIcons5;
                multiplier = 0x666;
            }
            else if (maxValue <= 2560)
            {
                axisIcons = axisIcons6;
                multiplier = 0x333;
            }
            else if (maxValue <= 5120)
            {
                axisIcons = axisIcons7;
                multiplier = 0x199;
            }
            else
            {
                axisIcons = axisIcons8;
                multiplier = 0xa3;
            }

            // draw axis caption icons
            for (int i = 0; i < 4; ++i)
            {
                SetIcon(120, 9 + i * 16, axisIcons[i]);
            }

            // draw chart
            byte[] chartData = new byte[112 * 64 * 4];

            for (int i = 0; i < 112; ++i)
            {
                SetResourceChartValue(chartData, i, Math.Min((historicalData[i] * multiplier) >> 16, 64));
            }

            // we use the minimap for displaying the chart as we can set pixel colors inside the minimap sprite freely
            MiniMap.SetSize(112, 64);
            interf.RenderView.MinimapTextureFactory.ResizeMinimapTexture(112, 64);
            interf.RenderView.MinimapTextureFactory.FillMinimapTexture(chartData);

            MiniMap.SetRedraw();
        }

        static readonly Render.Color chartColor = new Render.Color(0xcf, 0x63, 0x63);

        static void SetResourceChartValue(byte[] data, int x, int value)
        {
            if (value == 0)
                return;

            int index = ((64 - value) * 112 + x) * 4;
            const int rowOffset = 112 * 4;

            for (int i = 0; i < value; ++i)
            {
                data[index + 0] = chartColor.B;
                data[index + 1] = chartColor.G;
                data[index + 2] = chartColor.R;
                data[index + 3] = chartColor.A;

                index += rowOffset;
            }
        }

        static void SetPlayerChartValue(byte[] data, int x, int y, int height, Render.Color color)
        {
            if (x < 0 || x >= 112)
                return;

            if (height <= 0 || y < 0 || y >= 100)
                return;

            if (y + height > 100)
                height = 100 - y;

            int index = (y * 112 + x) * 4;
            const int rowOffset = 112 * 4;

            for (int i = 0; i < height; ++i)
            {
                data[index + 0] = color.B;
                data[index + 1] = color.G;
                data[index + 2] = color.R;
                data[index + 3] = color.A;

                index += rowOffset;
            }
        }

        void DrawPlayerStatisticChart(uint[] playerData, int index, Color playerColor, byte[] chartData)
        {
            const int width = 112;
            const int height = 100;

            var color = new Render.Color(playerColor.Red, playerColor.Green, playerColor.Blue);
            uint previousValue = playerData[index];

            for (int i = 0; i < width; ++i)
            {
                uint value = playerData[index];

                index = index > 0 ? index - 1 : width - 1;

                if (value > 0 || previousValue > 0)
                {
                    if (value == previousValue)
                    {
                        SetPlayerChartValue(chartData, width - i - 1, Misc.Clamp(0, height - (int)value, height), 1, color);
                        SetPlayerChartValue(chartData, width - i, Misc.Clamp(0, height - (int)value, height), 1, color);
                    }
                    else if (value > previousValue)
                    {
                        int difference = (int)value - (int)previousValue;
                        int differenceHeight = difference / 2;

                        SetPlayerChartValue(chartData, width - i, Misc.Clamp(0, height - differenceHeight - (int)previousValue, height), differenceHeight, color);

                        difference -= differenceHeight;

                        SetPlayerChartValue(chartData, width - i - 1, Misc.Clamp(0, height - (int)value, height), difference, color);
                    }
                    else // value < previousValue
                    {
                        int difference = (int)previousValue - (int)value;
                        int differenceHeight = difference / 2;

                        SetPlayerChartValue(chartData, width - i, Misc.Clamp(0, height - (int)previousValue, height), differenceHeight, color);

                        difference -= differenceHeight;

                        SetPlayerChartValue(chartData, width - i - 1, Misc.Clamp(0, height - (int)value - difference, height), difference, color);
                    }
                }

                previousValue = value;
            }
        }

        void DrawPlayerStatisticsBox()
        {
            // axis icons
            SetIcon(120, 9, 0x58);
            SetIcon(8, 109, 0x59);

            // time selection / scaling
            SetButton(72, 121, 0x41, Action.PlayerStatisticsSetScale30Min); // 0.5 hrs
            SetButton(88, 121, 0x42, Action.PlayerStatisticsSetScale120Min); // 2 hrs
            SetButton(72, 137, 0x43, Action.PlayerStatisticsSetScale600Min); // 10 hrs
            SetButton(88, 137, 0x44, Action.PlayerStatisticsSetScale3000Min); // 50 hrs

            // value selection / aspect
            SetButton(24, 121, 0x45, Action.PlayerStatisticsSetAspectAll); // all
            SetButton(40, 121, 0x40, Action.PlayerStatisticsSetAspectLand); // land / territory
            SetButton(24, 137, 0x3e, Action.PlayerStatisticsSetAspectBuildings); // buildings
            SetButton(40, 137, 0x3f, Action.PlayerStatisticsSetAspectMilitary); // military

            SetButton(120, 121, 0x133, Action.ShowPlayerFaces); // player faces
            SetButton(120, 137, 0x3c, Action.ShowStatMenu); // exit

            int aspect = (currentPlayerStatisticsMode >> 2) & 3;
            uint scale = (uint)currentPlayerStatisticsMode & 3;

            // selection checkmarks
            SetIcon(Misc.BitTest(aspect, 0) ? 56 : 16, Misc.BitTest(aspect, 1) ? 141 : 125, 106);
            SetIcon(Misc.BitTest(scale, 0) ? 104 : 64, Misc.BitTest(scale, 1) ? 141 : 125, 106);

            // correct numbers on time scale
            SetIcon(24, 112, 94 + 3 * scale + 0);
            SetIcon(56, 112, 94 + 3 * scale + 1);
            SetIcon(88, 112, 94 + 3 * scale + 2);

            // draw player charts
            var game = interf.Game;
            int index = game.GetPlayerHistoryIndex(scale);
            byte[] chartData = new byte[112 * 100 * 4];
            uint numPlayers = (uint)game.PlayerCount;

            for (uint i = 0; i < numPlayers; ++i)
            {
                var player = game.GetPlayer(numPlayers - i - 1);

                if (player != null)
                {
                    DrawPlayerStatisticChart(player.GetPlayerStatHistory(currentPlayerStatisticsMode), index, player.Color, chartData);
                }
            }

            // we use the minimap for displaying the chart as we can set pixel colors inside the minimap sprite freely
            MiniMap.SetSize(112, 100);
            interf.RenderView.MinimapTextureFactory.ResizeMinimapTexture(112, 100);
            interf.RenderView.MinimapTextureFactory.FillMinimapTexture(chartData);

            MiniMap.SetRedraw();
        }

        void DrawGaugeBalance(int x, int y, uint value, uint count)
        {
            uint sprite = 0xd3;

            if (count > 0)
            {
                uint v = (16 * value) / count;

                if (v >= 230)
                {
                    sprite = 0xd2;
                }
                else if (v >= 207)
                {
                    sprite = 0xd1;
                }
                else if (v >= 184)
                {
                    sprite = 0xd0;
                }
                else if (v >= 161)
                {
                    sprite = 0xcf;
                }
                else if (v >= 138)
                {
                    sprite = 0xce;
                }
                else if (v >= 115)
                {
                    sprite = 0xcd;
                }
                else if (v >= 92)
                {
                    sprite = 0xcc;
                }
                else if (v >= 69)
                {
                    sprite = 0xcb;
                }
                else if (v >= 46)
                {
                    sprite = 0xca;
                }
                else if (v >= 23)
                {
                    sprite = 0xc9;
                }
                else
                {
                    sprite = 0xc8;
                }
            }

            SetIcon(x, y, sprite);
        }

        void DrawGaugeBalance(int x, int y, uint[,,] values, Building.Type building, int stockIndex)
        {
            DrawGaugeBalance(x, y, values[(int)building, stockIndex, 0], values[(int)building, stockIndex, 1]);
        }

        void DrawGaugeFull(int x, int y, uint value, uint count)
        {
            uint sprite = 0xc7;

            if (count > 0)
            {
                uint v = (16 * value) / count;

                if (v >= 230)
                {
                    sprite = 0xc6;
                }
                else if (v >= 207)
                {
                    sprite = 0xc5;
                }
                else if (v >= 184)
                {
                    sprite = 0xc4;
                }
                else if (v >= 161)
                {
                    sprite = 0xc3;
                }
                else if (v >= 138)
                {
                    sprite = 0xc2;
                }
                else if (v >= 115)
                {
                    sprite = 0xc1;
                }
                else if (v >= 92)
                {
                    sprite = 0xc0;
                }
                else if (v >= 69)
                {
                    sprite = 0xbf;
                }
                else if (v >= 46)
                {
                    sprite = 0xbe;
                }
                else if (v >= 23)
                {
                    sprite = 0xbd;
                }
                else
                {
                    sprite = 0xbc;
                }
            }

            SetIcon(x, y, sprite);
        }

        void DrawGaugeFull(int x, int y, uint[,,] values, Building.Type building, int stockIndex)
        {
            DrawGaugeFull(x, y, values[(int)building, stockIndex, 0], values[(int)building, stockIndex, 1]);
        }

        static void CalculateGaugeValues(Player player, uint[,,] values)
        {
            foreach (var building in player.Game.GetPlayerBuildings(player))
            {
                if (building.IsBurning || !building.HasSerf)
                {
                    continue;
                }

                int type = (int)building.BuildingType;

                if (!building.IsDone)
                    type = 0;

                for (int i = 0; i < Building.MaxStock; ++i)
                {
                    if (building.GetMaximumInStock(i) > 0)
                    {
                        uint v = 2 * building.GetResourceCountInStock(i) + building.GetRequestedInStock(i);

                        values[type, i, 0] += (16 * v) / (2 * building.GetMaximumInStock(i));
                        values[type, i, 1] += 1;
                    }
                }
            }
        }

        void DrawFoodProductionCycleBox()
        {
            SetIcon(8, 9, 0x18); // baker 
            SetIcon(8, 25, 0xb4);
            SetIcon(8, 33, 0xb3);
            SetIcon(8, 41, 0xb2);
            SetIcon(8, 49, 0xb3);
            SetIcon(8, 57, 0xb2);
            SetIcon(8, 65, 0xb3);
            SetIcon(8, 73, 0xb2);
            SetIcon(8, 81, 0xb3);
            SetIcon(8, 89, 0xb2);
            SetIcon(8, 97, 0xb3);
            SetIcon(8, 105, 0xd4);
            SetIcon(8, 121, 0xb1);
            SetIcon(8, 129, 0x13); // fisher 
            SetIcon(24, 57, 0x15); // butcher 
            SetIcon(24, 73, 0xb4);
            SetIcon(24, 81, 0xb3);
            SetIcon(24, 89, 0xd4);
            SetIcon(24, 105, 0xa4);
            SetIcon(24, 121, 0xa4);
            SetIcon(40, 13, 0xae);
            SetIcon(40, 45, 0xae);
            SetIcon(40, 89, 0xa6);
            SetIcon(40, 105, 0xa6);
            SetIcon(40, 121, 0xa6);
            SetIcon(56, 9, 0x26); // flour 
            SetIcon(56, 41, 0x23); // pig 
            SetIcon(56, 73, 0xb5);
            SetIcon(56, 85, 0x24); // meat 
            SetIcon(56, 101, 0x27); // bread 
            SetIcon(56, 117, 0x22); // fish 
            SetIcon(56, 133, 0xb6);
            SetIcon(72, 9, 0x17); // miller 
            SetIcon(72, 41, 0x14); // pigfarmer 
            SetIcon(72, 73, 0xa6);
            SetIcon(72, 97, 0xab);
            SetIcon(72, 111, 0xab);
            SetIcon(72, 137, 0xa6);
            SetIcon(104, 17, 0xba);
            SetIcon(104, 65, 0x11); // miner 
            SetIcon(104, 89, 0x11); // miner 
            SetIcon(104, 113, 0x11); // miner 
            SetIcon(104, 137, 0x11); // miner 
            SetIcon(120, 9, 0x16); // farmer 
            SetIcon(120, 25, 0x25); // wheat 
            SetIcon(120, 65, 0x2f); // goldore 
            SetIcon(120, 89, 0x2e); // coal 
            SetIcon(120, 113, 0x2c); // ironore 
            SetIcon(120, 137, 0x2b); // stone 

            uint[,,] values = new uint[24, Building.MaxStock, 2];

            CalculateGaugeValues(interf.Player, values);

            DrawGaugeBalance(88, 9, values, Building.Type.Mill, 0);
            DrawGaugeBalance(24, 9, values, Building.Type.Baker, 0);
            DrawGaugeFull(88, 41, values, Building.Type.PigFarm, 0);
            DrawGaugeBalance(24, 41, values, Building.Type.Butcher, 0);
            DrawGaugeFull(88, 65, values, Building.Type.GoldMine, 0);
            DrawGaugeFull(88, 89, values, Building.Type.CoalMine, 0);
            DrawGaugeFull(88, 113, values, Building.Type.IronMine, 0);
            DrawGaugeFull(88, 137, values, Building.Type.StoneMine, 0);
        }

        void DrawMaterialProductionCycleBox()
        {
            SetIcon(8, 9, 0x11); // miner 
            SetIcon(8, 33, 0x11); // miner 
            SetIcon(8, 65, 0x11); // miner 
            SetIcon(8, 89, 0xd); // lumberjack 
            SetIcon(8, 113, 0x11); // miner 
            SetIcon(8, 137, 0xf); // stonecutter 
            SetIcon(24, 9, 0x2f); // goldore 
            SetIcon(24, 33, 0x2e); // coal 
            SetIcon(24, 49, 0xb0);
            SetIcon(24, 65, 0x2c); // ironore 
            SetIcon(24, 89, 0x28); // lumber 
            SetIcon(24, 113, 0x2b); // stone 
            SetIcon(24, 137, 0x2b); // stone 
            SetIcon(40, 13, 0xaa);
            SetIcon(40, 33, 0xab);
            SetIcon(40, 41, 0xad);
            SetIcon(40, 49, 0xa8);
            SetIcon(40, 69, 0xac);
            SetIcon(40, 73, 0xaa);
            SetIcon(40, 117, 0xbb);
            SetIcon(56, 41, 0xa4);
            SetIcon(56, 105, 0xe); // sawmiller 
            SetIcon(56, 141, 0xa5);
            SetIcon(72, 9, 0x30); // gold 
            SetIcon(72, 25, 0x12); // smelter 
            SetIcon(72, 41, 0xa4);
            SetIcon(72, 49, 0x2d); // steel 
            SetIcon(72, 65, 0x12); // smelter 
            SetIcon(72, 89, 0xb8);
            SetIcon(72, 105, 0x29); // planks 
            SetIcon(72, 121, 0xaf);
            SetIcon(72, 141, 0xa5);
            SetIcon(88, 13, 0xaa);
            SetIcon(88, 33, 0xb9);
            SetIcon(88, 49, 0xab);
            SetIcon(88, 57, 0xb7);
            SetIcon(88, 89, 0xa6);
            SetIcon(88, 105, 0xa9);
            SetIcon(88, 121, 0xa6);
            SetIcon(88, 141, 0xa7);
            SetIcon(120, 9, 0x21); // knight 4 
            SetIcon(120, 37, 0x1b); // weaponsmith 
            SetIcon(120, 73, 0x1a); // toolmaker 
            SetIcon(120, 101, 0x19); // boatbuilder 
            SetIcon(120, 129, 0xc); // builder 

            uint[,,] values = new uint[24, Building.MaxStock, 2];

            CalculateGaugeValues(interf.Player, values);

            DrawGaugeBalance(56, 9, values, Building.Type.GoldSmelter, 1);
            DrawGaugeBalance(56, 25, values, Building.Type.GoldSmelter, 0);
            DrawGaugeBalance(56, 49, values, Building.Type.SteelSmelter, 0);
            DrawGaugeBalance(56, 65, values, Building.Type.SteelSmelter, 1);
            DrawGaugeBalance(56, 89, values, Building.Type.Sawmill, 1);

            uint goldValue =
                values[(int)Building.Type.Hut, 1, 0] +
                values[(int)Building.Type.Tower, 1, 0] +
                values[(int)Building.Type.Fortress, 1, 0];
            uint goldCount =
                values[(int)Building.Type.Hut, 1, 1] +
                values[(int)Building.Type.Tower, 1, 1] +
                values[(int)Building.Type.Fortress, 1, 1];

            DrawGaugeFull(104, 9, goldValue, goldCount);

            DrawGaugeBalance(104, 29, values, Building.Type.WeaponSmith, 0);
            DrawGaugeBalance(104, 45, values, Building.Type.WeaponSmith, 1);
            DrawGaugeBalance(104, 65, values, Building.Type.ToolMaker, 1);
            DrawGaugeBalance(104, 81, values, Building.Type.ToolMaker, 0);
            DrawGaugeBalance(104, 101, values, Building.Type.Boatbuilder, 0);
            DrawGaugeFull(104, 121, values[0, 0, 0], values[0, 0, 1]); // construction planks
            DrawGaugeFull(104, 137, values[0, 1, 0], values[0, 1, 1]); // construction stones
        }

        void DrawSerfCountBox()
        {
            var player = interf.Player;
            int total = 0;

            for (int i = 0; i < 27; i++)
            {
                if (i != (int)Serf.Type.TransporterInventory)
                {
                    total += (int)player.GetSerfCount((Serf.Type)i);
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
            var player = interf.Player;
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

            // First column 
            DrawSerfMeter(3, 0, serfs[Serf.Type.Transporter]);
            DrawSerfMeter(3, 16, serfs[Serf.Type.Sailor]);
            DrawSerfMeter(3, 32, serfs[Serf.Type.Digger]);
            DrawSerfMeter(3, 48, serfs[Serf.Type.Builder]);
            DrawSerfMeter(3, 64, serfs[Serf.Type.Knight4]);
            DrawSerfMeter(3, 80, serfs[Serf.Type.Knight3]);
            DrawSerfMeter(3, 96, serfs[Serf.Type.Knight2]);
            DrawSerfMeter(3, 112, serfs[Serf.Type.Knight1]);
            DrawSerfMeter(3, 128, serfs[Serf.Type.Knight0]);

            // Second column 
            DrawSerfMeter(8, 0, serfs[Serf.Type.Lumberjack]);
            DrawSerfMeter(8, 16, serfs[Serf.Type.Sawmiller]);
            DrawSerfMeter(8, 32, serfs[Serf.Type.Smelter]);
            DrawSerfMeter(8, 48, serfs[Serf.Type.Stonecutter]);
            DrawSerfMeter(8, 64, serfs[Serf.Type.Forester]);
            DrawSerfMeter(8, 80, serfs[Serf.Type.Miner]);
            DrawSerfMeter(8, 96, serfs[Serf.Type.BoatBuilder]);
            DrawSerfMeter(8, 112, serfs[Serf.Type.Toolmaker]);
            DrawSerfMeter(8, 128, serfs[Serf.Type.WeaponSmith]);

            // Third column 
            DrawSerfMeter(13, 0, serfs[Serf.Type.Fisher]);
            DrawSerfMeter(13, 16, serfs[Serf.Type.PigFarmer]);
            DrawSerfMeter(13, 32, serfs[Serf.Type.Butcher]);
            DrawSerfMeter(13, 48, serfs[Serf.Type.Farmer]);
            DrawSerfMeter(13, 64, serfs[Serf.Type.Miller]);
            DrawSerfMeter(13, 80, serfs[Serf.Type.Baker]);
            DrawSerfMeter(13, 96, serfs[Serf.Type.Geologist]);
            DrawSerfMeter(13, 112, serfs[Serf.Type.Generic]);

            SetButton(120, 137, 60u, Action.ShowStatMenu);
        }

        void SetTree(int x, int y, uint index)
        {
            SetBuildingIcon(x, y, index);
        }

        void DrawStartAttackBox()
        {
            // draw some trees
            SetTree(24, 42, 0x0);
            SetTree(56, 39, 0xa);
            SetTree(88, 42, 0x7);
            SetTree(120, 39, 0xc);
            SetTree(24, 45, 0xe);
            SetTree(56, 48, 0x2);
            SetTree(88, 45, 0xb);
            SetTree(104, 48, 0x4);
            SetTree(72, 51, 0x8);
            SetTree(104, 51, 0xf);

            var player = interf.Player;
            var building = interf.Game.GetBuilding((uint)player.BuildingToAttack);

            int y = 0;

            switch (building.BuildingType)
            {
                case Building.Type.Hut: y = 59; break;
                case Building.Type.Tower: y = 41; break;
                case Building.Type.Fortress: y = 26; break;
                case Building.Type.Castle: y = 9; break;
                default: Debug.NotReached(); return;
            }

            // TODO: maybe adjust the icon locations
            // TODO: with the big castle it looks very bad
            SetBuildingIcon(8, y, building.BuildingType);

            // Note: Set buttons after building icon to display them infront of it
            SetButton(16, 89, 216, Action.AttackingSelectAll1); // closest knights icon
            SetButton(48, 89, 217, Action.AttackingSelectAll2); // close knights icon
            SetButton(80, 89, 218, Action.AttackingSelectAll3); // far knights icon
            SetButton(112, 89, 219, Action.AttackingSelectAll4); // farthest knights icon
            SetButton(40, 121, 220, Action.AttackingKnightsDec); // minus icon
            SetButton(88, 121, 221, Action.AttackingKnightsInc); // plus icon
            SetButton(8, 137, 222, Action.StartAttack); // attack icon

            SetButton(120, 137, 60u, Action.CloseBox); // exit button

            // draw number of knights at each distance
            for (int i = 0; i < 4; ++i)
            {
                SetNumberText(16 + i * 32, 105, (uint)player.MaxAttackingKnightsByDistance[i]);
            }

            SetNumberText(64, 125, (uint)player.TotalKnightsAttacking);
        }

        // Translate resource amount to text
        static string GetResourceAmountText(uint amount)
        {
            if (amount == 0) return "Not Present";
            else if (amount < 100) return "Minimum";
            else if (amount < 180) return "Very Few";
            else if (amount < 240) return "Few";
            else if (amount < 300) return "Below Average";
            else if (amount < 400) return "Average";
            else if (amount < 500) return "Above Average";
            else if (amount < 600) return "Much";
            else if (amount < 800) return "Very Much";
            return "Perfect";
        }

        void DrawGroundAnalysisBox()
        {
            SetIcon(64, 19, 0x1c); // geologist
            SetIcon(16, 59, 0x2f); // gold ore
            SetIcon(16, 79, 0x2c); // iron ore
            SetIcon(16, 99, 0x2e); // coal
            SetIcon(16, 119, 0x2b); // stone

            SetButton(120, 137, 60u, Action.CloseBox); // exit button

            var position = interf.GetMapCursorPosition();
            uint[] estimates = new uint[5];

            interf.Game.PrepareGroundAnalysis(position, estimates);

            SetText(8, 39, "GROUND-ANALYSIS:");

            // gold
            SetText(32, 63, GetResourceAmountText(2u * estimates[(int)Map.Minerals.Gold]));

            // iron
            SetText(32, 83, GetResourceAmountText(estimates[(int)Map.Minerals.Iron]));

            // coal
            SetText(32, 103, GetResourceAmountText(estimates[(int)Map.Minerals.Coal]));

            // stone
            SetText(32, 123, GetResourceAmountText(2u * estimates[(int)Map.Minerals.Stone]));
        }

        void DrawSettlerMenuBox()
        {
            SetButton(16, 17, 230u, Action.ShowFoodDistribution);
            SetButton(56, 17, 231u, Action.ShowPlanksAndSteelDistribution);
            SetButton(96, 17, 232u, Action.ShowCoalAndWheatDistribution);

            SetButton(16, 57, 234u, Action.ShowToolmakerPriorities);
            SetButton(56, 57, 235u, Action.ShowTransportPriorities);
            SetButton(96, 57, 299u, Action.ShowInventoryPriorities);

            SetButton(16, 97, 233u, Action.ShowKnightLevel);
            SetButton(56, 97, 298u, Action.ShowKnightSettings);

            SetButton(104, 113, 61u, Action.ShowStatMenu);
            SetButton(120, 137, 60u, Action.CloseBox);

            SetButton(40, 137, 285u, Action.ShowOptions);
            SetButton(8, 137, 286u, Action.ShowQuit);
            SetButton(72, 137, 224u, Action.ShowSave);
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

            var player = interf.Player;

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

            var player = interf.Player;

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

            var player = interf.Player;

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

        void DrawKnightLevelBox()
        {
            string[] levelTexts = new string[]
            {
                "Minimum", "Weak", "Medium", "Good", "Full", "ERROR", "ERROR", "ERROR"
            };

            // distance illustrations
            SetIcon(8, 9, 226);
            SetIcon(8, 41, 227);
            SetIcon(8, 73, 228);
            SetIcon(8, 105, 229);

            SetButton(40, 9, 220, Action.KnightLevelClosestMaxDec); // minus
            SetButton(56, 9, 221, Action.KnightLevelClosestMaxInc); // plus
            SetButton(40, 25, 220, Action.KnightLevelClosestMinDec); // minus
            SetButton(56, 25, 221, Action.KnightLevelClosestMinInc); // plus

            SetButton(40, 41, 220, Action.KnightLevelCloseMaxDec); // minus
            SetButton(56, 41, 221, Action.KnightLevelCloseMaxInc); // plus
            SetButton(40, 57, 220, Action.KnightLevelCloseMinDec); // minus
            SetButton(56, 57, 221, Action.KnightLevelCloseMinInc); // plus

            SetButton(40, 73, 220, Action.KnightLevelFarMaxDec); // minus
            SetButton(56, 73, 221, Action.KnightLevelFarMaxInc); // plus
            SetButton(40, 89, 220, Action.KnightLevelFarMinDec); // minus
            SetButton(56, 89, 221, Action.KnightLevelFarMinInc); // plus

            SetButton(40, 105, 220, Action.KnightLevelFarthestMaxDec); // minus
            SetButton(56, 105, 221, Action.KnightLevelFarthestMaxInc); // plus
            SetButton(40, 121, 220, Action.KnightLevelFarthestMinDec); // minus
            SetButton(56, 121, 221, Action.KnightLevelFarthestMinInc); // plus

            SetButton(120, 137, 60u, Action.ShowSettlerMenu); // exit button

            var player = interf.Player;

            for (int i = 0; i < 4; ++i)
            {
                int y = 14 + 32 * i;

                SetText(72, y, levelTexts[(player.GetKnightOccupation(3 - i) >> 4) & 0x7]);
                SetText(72, y + 14, levelTexts[player.GetKnightOccupation(3 - i) & 0x7]);
            }
        }

        void DrawToolmakerPrioritiesBox()
        {
            SetIcon(8, 9, 49u); // shovel
            SetIcon(8, 25, 50u); // hammer
            SetIcon(8, 41, 54u); // axe
            SetIcon(8, 57, 55u); // saw
            SetIcon(8, 73, 53u); // scythe
            SetIcon(8, 89, 56u); // pick
            SetIcon(8, 105, 57u); // pincer
            SetIcon(8, 121, 52u); // cleaver
            SetIcon(8, 137, 51u); // rod

            SetButton(120, 137, 60u, Action.ShowSettlerMenu); // exit button
            SetButton(112, 17, 295u, Action.DefaultToolmakerPriorities); // reset values button

            var player = interf.Player;

            var locations = new int[9]
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

        void DrawPopupResourceStairs(byte[] order)
        {
            int count = ResourceStairLayout.Length / 2;

            for (int i = 0; i < count; ++i)
            {
                int position = count - order[i];

                SetButton(8 * ResourceStairLayout[2 * position] + 8, ResourceStairLayout[2 * position + 1] + 9, 34u + (uint)i, Action.SetTransportItem1 + position);
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
                DrawPopupResourceStairs(interf.Player.GetFlagPriorities());
            }
            else
            {
                SetIcon(56, 129, 33u + CurrentInventoryPriorityItem);
                DrawPopupResourceStairs(interf.Player.GetInventoryPriorities());
            }
        }

        void DrawQuitConfirmBox()
        {
            SetText(8, 19, "   Do you want");
            SetText(8, 29, "     to quit");
            SetText(8, 39, "   this game?");
            clickableTextField = SetText(8, 54, "  Yes        No");
        }

        void DrawNoSaveQuitConfirmBox()
        {
            SetText(8, 79, "The game has not");
            SetText(8, 89, "   been saved");
            SetText(8, 99, "   recently.");
            SetText(8, 109, "    Are you");
            SetText(8, 119, "     sure?");
            clickableTextField = SetText(8, 134, "  Save  Yes  No");
        }

        void DrawExtendedOptionsBox(Action closeAction)
        {
            SetText(16, 20, "Fast");
            SetText(16, 29, "Mapclick");
            SetText(16, 45, "Fast");
            SetText(16, 54, "Building");

            SetButton(112, 21, interf.GetOption(Option.FastMapClick) ? 288u : 220u, Action.OptionsFastMapclick);
            SetButton(112, 47, interf.GetOption(Option.FastBuilding) ? 288u : 220u, Action.OptionsFastBuilding);

            string value = "All";

            if (!interf.GetOption(Option.MessagesAll))
            {
                value = "Most";

                if (!interf.GetOption(Option.MessagesMost))
                {
                    value = "Few";

                    if (!interf.GetOption(Option.MessagesFew))
                    {
                        value = "None";
                    }
                }
            }

            SetText(16, 104, "Messages");
            clickableTextField = SetText(96, 104, value);

            SetButton(104, 137, 61u, Action.ShowScrollOptions); // flip
            SetButton(120, 137, 60u, closeAction); // exit
        }

        void DrawScrollOptionsBox(Action closeAction)
        {
            SetText(16, 20, "Pathway-");
            SetText(16, 29, "Scrolling");
            SetText(16, 45, "Invert");
            SetText(16, 54, "Scrolling");
            SetText(16, 70, "Hide mouse");
            SetText(16, 79, "while scr.");
            SetText(16, 95, "Reset mouse");
            SetText(16, 104, "after scr.");

            SetButton(112, 21, interf.GetOption(Option.PathwayScrolling) ? 288u : 220u, Action.OptionsPathwayScrolling);
            SetButton(112, 47, interf.GetOption(Option.InvertScrolling) ? 288u : 220u, Action.OptionsInvertScrolling);
            SetButton(112, 73, interf.GetOption(Option.HideCursorWhileScrolling) ? 288u : 220u, Action.OptionsHideCursorWhileScrolling);
            SetButton(112, 98, interf.GetOption(Option.ResetCursorAfterScrolling) ? 288u : 220u, Action.OptionsResetCursorAfterScrolling);

            SetButton(104, 137, 61u, Action.ShowOptions); // flip
            SetButton(120, 137, 60u, closeAction); // exit
        }

        void DrawOptionsBox(Action closeAction)
        {
            SetText(16, 23, "Music");
            SetText(16, 39, "Sound");
            SetText(16, 48, "effects");
            SetText(16, 63, "Volume");

            // Music
            var player = Audio?.GetMusicPlayer();
            SetButton(112, 19, (player != null && player.Enabled) ? 288u : 220u, Action.OptionsMusic);

            // Sfx
            player = Audio?.GetSoundPlayer();
            SetButton(112, 39, (player != null && player.Enabled) ? 288u : 220u, Action.OptionsSfx);

            // Volume
            SetButton(96, 59, 220u, Action.OptionsVolumeMinus); // Volume minus 
            SetButton(112, 59, 221u, Action.OptionsVolumePlus); // Volume plus 

            float volume = 0.0f;
            Audio.Audio.IVolumeController volumeController = Audio?.GetVolumeController();

            if (volumeController != null)
            {
                volume = 99.0f * volumeController.Volume;
            }

            SetNumberText(72, 63, (uint)Misc.Round(volume));

            // Fullscreen
            SetText(16, 90, "Fullscreen");

            SetButton(112, 86, interf.RenderView.Fullscreen ? 288u : 220u, Action.OptionsFullscreen);

            SetButton(104, 137, 61u, Action.ShowExtendedOptions); // flip
            SetButton(120, 137, 60u, closeAction); // exit
        }

        void DrawMineOutputBox()
        {
            var building = TryToOpenBuildingPopup();

            if (building == null)
                return;

            if (building.BuildingType < Building.Type.StoneMine ||
                building.BuildingType > Building.Type.GoldMine)
            {
                interf.ClosePopup();
                return;
            }

            // Draw food present at mine
            uint stock = building.GetResourceCountInStock(0);
            uint stockLeftColumn = (stock + 1u) >> 1;
            uint stockRightColumn = stock >> 1;

            // Left column
            for (uint i = 0; i < stockLeftColumn; ++i)
            {
                SetIcon(16, 99 - 8 * (int)stockLeftColumn + (int)i * 16, 0x24); // meat (food) sprite
            }

            // Right column
            for (uint i = 0; i < stockRightColumn; ++i)
            {
                SetIcon(112, 99 - 8 * (int)stockLeftColumn + (int)i * 16, 0x24); // meat (food) sprite
            }

            // Calculate output percentage (simple WMA)
            int[] outputWeight = { 10, 10, 9, 9, 8, 8, 7, 7, 6, 6, 5, 5, 4, 3, 2, 1 };
            int output = 0;

            for (int i = 0; i < 15; ++i)
            {
                output += Misc.BitTest(building.Progress, i) ? outputWeight[i] : 0;
            }

            // Print output precentage
            SetText(56, 47, output.ToString() + "%");

            // draw picture of serf present
            uint serfSprite = 0xdcu; // minus box

            if (building.HasSerf)
                serfSprite = 0x11; // miner

            SetIcon(88, 84, serfSprite);

            // draw building
            var info = interf.RenderView.DataSource.GetSpriteInfo(Data.Resource.MapObject, Render.RenderBuilding.MapBuildingSprite[(int)building.BuildingType]);

            int x = (Width - info.Width) / 2;
            SetBuildingIcon(x, 69, Render.RenderBuilding.MapBuildingSprite[(int)building.BuildingType]);

            // draw texts
            SetText(16, 13, "Mining output:");

            SetButton(120, 137, 60u, Action.CloseBox); // exit
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

            if (building.HasSerf)
            {
                if (building.Progress == 0)
                {
                    // Digger 
                    SetIcon(18, 121, 0xbu);
                }
                else
                {
                    // Builder 
                    SetIcon(18, 121, 0xcu);
                }
            }
            else
            {
                // Minus box 
                SetIcon(18, 121, 0xdcu);
            }

            // draw construction materials
            SetIcon(48, 121, 41u); // plank icon
            SetText(50, 139, $"{building.GetResourceCountInStock(0)}");
            SetIcon(78, 121, 43u); // stone icon
            SetText(80, 139, $"{building.GetResourceCountInStock(1)}");

            SetButton(120, 137, 0x3c, Action.CloseBox); // exit
        }

        void DrawDefendersBox()
        {
            var player = interf.Player;

            if (player.SelectedObjectIndex == 0)
            {
                interf.ClosePopup();
                return;
            }

            var building = player.Game.GetBuilding(player.SelectedObjectIndex);

            if (building.IsBurning || building.Player != player.Index)
            {
                interf.ClosePopup();
                return;
            }

            var buildingType = building.BuildingType;

            if (buildingType != Building.Type.Hut &&
                buildingType != Building.Type.Tower &&
                buildingType != Building.Type.Fortress)
            {
                interf.ClosePopup();
                return;
            }

            // draw building sprite
            int x = 0;
            int y = 0;

            switch (buildingType)
            {
                case Building.Type.Hut:
                    x = 56;
                    y = 29;
                    break;
                case Building.Type.Tower:
                    x = 40;
                    y = 15;
                    break;
                case Building.Type.Fortress:
                    x = 40;
                    y = 10;
                    break;
                default:
                    Debug.NotReached();
                    return;
            }

            SetBuildingIcon(x, y, buildingType);

            // draw gold stock
            uint goldStock = building.GetResourceCountInStock(1);

            if (goldStock > 0)
            {
                uint left = (goldStock + 1) / 2;
                uint right = goldStock / 2;

                for (uint i = 0; i < left; ++i)
                {
                    SetIcon(16, (int)(41u - 8u * left + 16u * i), 0x30u);
                }

                for (uint i = 0; i < right; ++i)
                {
                    SetIcon(112, (int)(41u - 8u * left + 16u * i), 0x30u);
                }
            }

            // draw heading string
            SetText(32, 71, "Defenders:");

            // draw knights
            uint nextKnight = building.FirstKnight;

            for (int i = 0; nextKnight != 0; ++i)
            {
                var knight = player.Game.GetSerf(nextKnight);

                SetIcon(32 + 32 * (i % 3), 81 + 16 * (i / 3), 7u + (uint)knight.SerfType);

                nextKnight = knight.NextKnight;
            }

            SetText(8, 137, "State:");
            SetText(54, 137, building.ThreatLevel.ToString());

            SetButton(120, 137, 0x3c, Action.CloseBox); // exit
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
            if (interf.Player.SelectedObjectIndex == 0)
            {
                interf.ClosePopup();
                return;
            }

            var flag = interf.Game.GetFlag(interf.Player.SelectedObjectIndex);

            if (flag.CanMergeNearbyPaths())
                SetButton(64, 60, 0x135, Action.MergePaths);

            SetFlagIcon(72, 49);

            var cycle = DirectionCycleCW.CreateDefault();
            List<Direction> nearbyPathPositions = new List<Direction>();

            foreach (var direction in cycle)
            {
                int index = 5 - (int)direction;
                int x = 8 + 8 * FlagLayout[index * 2];
                int y = 9 + FlagLayout[index * 2 + 1];

                if (flag.HasPath(direction))
                {
                    nearbyPathPositions.Add(direction);

                    uint sprite = 0xdcu; // minus box

                    if (flag.HasTransporter(direction))
                        sprite = 0x120u; // check box

                    SetIcon(x, y, sprite);
                }
            }

            SetText(8, 13, "Transport Info:");

            if (interf.AccessRights == Viewer.Access.Player)
                SetButton(24, 105, 0x1cu, Action.SendGeologist); // send geologist

            SetButton(120, 137, 0x3c, Action.CloseBox); // exit

            // draw resources
            for (int i = 0; i < Global.FLAG_MAX_RES_COUNT; ++i)
            {
                if (flag.GetResourceAtSlot(i) != Resource.Type.None)
                {
                    SetIcon(8 + 8 * (7 + 2 * (i & 3)), 97 + 16 * (i >> 2), 0x22u + (uint)flag.GetResourceAtSlot(i));
                }
            }
        }

        Building TryToOpenBuildingPopup()
        {
            if (interf.Player.SelectedObjectIndex == 0)
            {
                interf.ClosePopup();
                return null;
            }

            var building = interf.Game.GetBuilding(interf.Player.SelectedObjectIndex);

            if (building.IsBurning)
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
            for (int j = 0; j < Building.MaxStock; ++j)
            {
                if (building.IsStockActive(j))
                {
                    uint stock = building.GetResourceCountInStock(j);

                    if (stock > 0)
                    {
                        uint sprite = 34u + (uint)building.GetResourceTypeInStock(j);

                        for (int i = 0; i < stock; ++i)
                            SetIcon(8 + 8 * (8 - (int)stock + 2 * i), 119 - j * 20, sprite);
                    }
                    else
                    {
                        SetIcon(64, 119 - j * 20, 0xdcu); // minus box
                    }
                }
            }

            // draw picture of serf present
            uint serfSprite = 0xdcu; // minus box

            if (building.HasSerf)
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

            var inventory = building.Inventory;
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
            var inventory = building.Inventory;

            foreach (var serf in interf.Game.GetSerfsInInventory(inventory))
            {
                ++serfCounts[(int)serf.SerfType];
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
                uint serfIndex = building.FirstKnight;

                while (serfIndex != 0)
                {
                    var serf = interf.Game.GetSerf(serfIndex);
                    var serfType = serf.SerfType;

                    if (serfType < Serf.Type.Knight0 || serfType > Serf.Type.Knight4)
                        throw new ExceptionFreeserf(interf.Game, ErrorSystemType.UI, "Not a knight among the castle defenders.");

                    ++knights[serfType - Serf.Type.Knight0];

                    serfIndex = serf.NextKnight;
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

            var inventory = building.Inventory;
            var resourceMode = inventory.ResourceMode;
            var serfMode = inventory.SerfMode;

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
            var player = interf.Player;

            // serf to knight rate setting
            SetIcon(24, 17, 9u); // settler
            SetIcon(104, 17, 29u); // knight
            slideBars[0].MoveTo(40, 21);
            slideBars[0].Displayed = Displayed;
            slideBars[0].Fill = player.SerfToKnightRate / SlideBarFactor;

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
            SetText(56, 72, ((100 * player.KnightMorale) / 0x1000).ToString() + "%");
            SetText(56, 82, player.GoldDeposited.ToString());
            SetIcon(32, 73, 304u); // scared knight
            SetIcon(96, 73, 303u); // angry knight

            // castle knights wanted
            SetNumberText(56, 128, player.CastleKnightsWanted);
            SetNumberText(56, 138, player.CastleKnights);
            SetButton(32, 129, 220u, Action.DecreaseCastleKnights); // minus
            SetButton(80, 129, 221u, Action.IncreaseCastleKnights); // plus

            // send strongest
            SetButton(24, 93, 302u, Action.SetCombatMode); // send strongest control
            // checkbox for send strongest
            if (player.SendStrongest)
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

        void DrawPlayerFacesBox()
        {
            int numPlayers = interf.Game.PlayerCount;
            int width = (Width - 16) / 2;
            int height = (Height - 16) / 2;

            for (int i = 0; i < Game.GAME_MAX_PLAYER_COUNT; ++i)
            {
                playerFaceBackgrounds[i].X = TotalX + 8 + (i % 2) * width;
                playerFaceBackgrounds[i].Y = TotalY + 9 + (i / 2) * height;
                playerFaceBackgrounds[i].Resize(width, height);
                playerFaceBackgrounds[i].Layer = Layer;

                if (i < numPlayers)
                {
                    var player = interf.Game.GetPlayer((uint)i);

                    SetButton(24 + (i % 2) * 64, 13 + (i / 2) * 72, GetPlayerFaceSprite(player.Face), Action.JumpToPlayer1 + i);
                }
                else
                {
                    SetIcon(24 + (i % 2) * 64, 13 + (i / 2) * 72, 281u);
                }
            }
        }

        void DrawDemolishBox()
        {
            SetButton(120, 137, 60u, Action.CloseBox); // exit button
            SetButton(64, 54, 288u, Action.Demolish); // checkbox

            SetText(8, 19, "    Demolish:");
            SetText(8, 39, "   Click here");
            SetText(8, 77, "   if you are");
            SetText(8, 95, "      sure");
        }

        void DrawSaveBox()
        {
            SetText(32, 11, "Save  Game");

            SetButton(8, 137, 224u, Action.Save); // save button
            SetButton(120, 137, 60u, Action.ShowSettlerMenu); // exit button
        }

        void DrawDiskMessageBox()
        {
            uint iconIndex = 220u;
            var closeAction = Action.CloseBox;

            switch (GameStore.Instance.LastOperationResult)
            {
                case GameStore.LastOperationStatus.SaveSuccess:
                    SetText(15, 33, "Game was saved");
                    SetText(22, 74, "successfully.");
                    iconIndex = 288u;
                    closeAction = Action.QuitConfirm;
                    break;
                case GameStore.LastOperationStatus.SaveFail:
                    SetText(15, 33, "Game could not");
                    SetText(39, 74, "be saved.");
                    closeAction = Action.QuitCancel;
                    break;
                case GameStore.LastOperationStatus.LoadSuccess:
                    SetText(11, 33, "Game was loaded");
                    SetText(22, 74, "successfully.");
                    iconIndex = 288u;
                    break;
                case GameStore.LastOperationStatus.LoadFail:
                    SetText(15, 33, "Game could not");
                    SetText(34, 74, "be loaded.");
                    break;
                default:
                    SetRedraw();
                    interf.ClosePopup();
                    return;
            }

            SetIcon(48, 110, 93u); // disk symbol
            SetIcon(80, 110, iconIndex); // success/failed symbol
            SetButton(120, 137, 60u, closeAction); // exit button
        }

        void ActivateTransportItem(int index)
        {
            var player = interf.Player;
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
            var player = interf.Player;
            byte[] priorities;
            int current;

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
                        priorities[i] = (byte)(priorities[i] + delta);
                }

                priorities[current - 1] = (byte)nextValue;
            }
        }

        void HandleAction(Action action, int x, int y, object tag = null)
        {
            SetRedraw();

            var player = interf.Player;
            bool spectator = interf.AccessRights != Viewer.Access.Player;

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
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        interf.BuildFlag();
                        interf.ClosePopup();
                    }
                    break;
                case Action.BuildBuilding:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        interf.BuildBuilding((Building.Type)tag);
                        interf.ClosePopup();
                    }
                    break;
                case Action.ShowFoodProductionCycle:
                    SetBox(Type.FoodProductionCycle);
                    break;
                case Action.ShowMaterialProductionCycle:
                    SetBox(Type.MaterialProductionCycle);
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
                    SetBox(Type.KnightSettings);
                    break;
                case Action.ShowOptions:
                    SetBox(Type.Options);
                    break;
                case Action.ShowExtendedOptions:
                    SetBox(Box == Type.GameInitOptions ? Type.ExtendedGameInitOptions : Type.ExtendedOptions);
                    break;
                case Action.ShowScrollOptions:
                    SetBox(Box == Type.ExtendedGameInitOptions ? Type.GameInitScrollOptions : Type.ScrollOptions);
                    break;
                case Action.ShowQuit:
                    SetBox(Type.QuitConfirm);
                    break;
                case Action.DefaultFoodDistribution:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ResetFoodPriority();
                    }
                    break;
                case Action.DefaultPlanksAndSteelDistribution:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ResetPlanksPriority();
                        player.ResetSteelPriority();
                    }
                    break;
                case Action.DefaultCoalAndWheatDistribution:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ResetCoalPriority();
                        player.ResetWheatPriority();
                    }
                    break;
                case Action.DefaultToolmakerPriorities:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ResetToolPriority();
                    }
                    break;
                case Action.DefaultTransportPriorities:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        if (Box == Type.TransportPriorities)
                            player.ResetFlagPriority();
                        else
                            player.ResetInventoryPriority();
                    }
                    break;
                case Action.TransportPriorityToTop:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        MoveTransportItem(true, true);
                    }
                    break;
                case Action.TransportPriorityToBottom:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        MoveTransportItem(false, true);
                    }
                    break;
                case Action.TransportPriorityUp:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        MoveTransportItem(true, false);
                    }
                    break;
                case Action.TransportPriorityDown:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        MoveTransportItem(false, false);
                    }
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
                        if (spectator)
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                        }
                        else
                        {
                            int item = action - Action.SetTransportItem1;
                            ActivateTransportItem(26 - item);
                        }
                    }
                    break;
                case Action.BuildingStatsFlip:
                    if (Box == Type.BuildingStats4)
                        SetBox(Type.BuildingStats1);
                    else
                        SetBox(Box + 1);
                    break;
                case Action.ShowResourceStatistics:
                    SetBox(Type.ResourceStatistics);
                    break;
                case Action.ResourceStatisticsSelectLumber:
                case Action.ResourceStatisticsSelectPlank:
                case Action.ResourceStatisticsSelectStone:
                case Action.ResourceStatisticsSelectCoal:
                case Action.ResourceStatisticsSelectIronore:
                case Action.ResourceStatisticsSelectGoldore:
                case Action.ResourceStatisticsSelectBoat:
                case Action.ResourceStatisticsSelectSteel:
                case Action.ResourceStatisticsSelectGoldbar:
                case Action.ResourceStatisticsSelectSword:
                case Action.ResourceStatisticsSelectShield:
                case Action.ResourceStatisticsSelectShovel:
                case Action.ResourceStatisticsSelectHammer:
                case Action.ResourceStatisticsSelectAxe:
                case Action.ResourceStatisticsSelectSaw:
                case Action.ResourceStatisticsSelectPick:
                case Action.ResourceStatisticsSelectScythe:
                case Action.ResourceStatisticsSelectCleaver:
                case Action.ResourceStatisticsSelectPincer:
                case Action.ResourceStatisticsSelectRod:
                case Action.ResourceStatisticsSelectFish:
                case Action.ResourceStatisticsSelectPig:
                case Action.ResourceStatisticsSelectMeat:
                case Action.ResourceStatisticsSelectWheat:
                case Action.ResourceStatisticsSelectFlour:
                case Action.ResourceStatisticsSelectBread:
                    currentResourceForStatistics = 1 + action - Action.ResourceStatisticsSelectFish;
                    SetBox(Type.ResourceStatistics); // this will update the background pattern
                    break;
                case Action.ShowPlayerStatistics:
                    SetBox(Type.PlayerStatistics);
                    break;
                case Action.PlayerStatisticsSetScale30Min:
                case Action.PlayerStatisticsSetScale120Min:
                case Action.PlayerStatisticsSetScale600Min:
                case Action.PlayerStatisticsSetScale3000Min:
                    currentPlayerStatisticsMode = (currentPlayerStatisticsMode & 0xc) | (action - Action.PlayerStatisticsSetScale30Min);
                    break;
                case Action.PlayerStatisticsSetAspectAll:
                case Action.PlayerStatisticsSetAspectLand:
                case Action.PlayerStatisticsSetAspectBuildings:
                case Action.PlayerStatisticsSetAspectMilitary:
                    currentPlayerStatisticsMode = (currentPlayerStatisticsMode & 0x3) | ((action - Action.PlayerStatisticsSetAspectAll) << 2);
                    break;
                case Action.ShowPlayerFaces:
                    SetBox(Type.PlayerFaces);
                    break;
                case Action.ShowKnightLevel:
                    SetBox(Type.KnightLevel);
                    break;
                case Action.TrainKnights:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
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
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        if (player.SendStrongest)
                        {
                            player.SendStrongest = false;
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
                        }
                    }
                    break;
                case Action.SetCombatModeStrong:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        if (!player.SendStrongest)
                        {
                            player.SendStrongest = true;
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
                        }
                    }
                    break;
                case Action.CycleKnights:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.CycleKnights();
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
                    }
                    break;
                case Action.DecreaseCastleKnights:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.DecreaseCastleKnightsWanted();
                    }
                    break;
                case Action.IncreaseCastleKnights:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.IncreaseCastleKnightsWanted();
                    }
                    break;
                case Action.KnightLevelClosestMinDec:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(3, false, -1);
                    }
                    break;
                case Action.KnightLevelClosestMinInc:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(3, false, 1);
                    }
                    break;
                case Action.KnightLevelClosestMaxDec:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(3, true, -1);
                    }
                    break;
                case Action.KnightLevelClosestMaxInc:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(3, true, 1);
                    }
                    break;
                case Action.KnightLevelCloseMinDec:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(2, false, -1);
                    }
                    break;
                case Action.KnightLevelCloseMinInc:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(2, false, 1);
                    }
                    break;
                case Action.KnightLevelCloseMaxDec:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(2, true, -1);
                    }
                    break;
                case Action.KnightLevelCloseMaxInc:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(2, true, 1);
                    }
                    break;
                case Action.KnightLevelFarMinDec:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(1, false, -1);
                    }
                    break;
                case Action.KnightLevelFarMinInc:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(1, false, 1);
                    }
                    break;
                case Action.KnightLevelFarMaxDec:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(1, true, -1);
                    }
                    break;
                case Action.KnightLevelFarMaxInc:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(1, true, 1);
                    }
                    break;
                case Action.KnightLevelFarthestMinDec:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(0, false, -1);
                    }
                    break;
                case Action.KnightLevelFarthestMinInc:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(0, false, 1);
                    }
                    break;
                case Action.KnightLevelFarthestMaxDec:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(0, true, -1);
                    }
                    break;
                case Action.KnightLevelFarthestMaxInc:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        player.ChangeKnightOccupation(0, true, 1);
                    }
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
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        SetInventoryMode(action);
                    }
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
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        var position = interf.GetMapCursorPosition();
                        var flag = interf.Game.GetFlagAtPosition(position);

                        if (!interf.Game.SendGeologist(flag))
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                        }
                        else
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
                            interf.ClosePopup();
                        }
                    }
                    break;
                case Action.MergePaths:
                    if (spectator)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    else
                    {
                        var position = interf.GetMapCursorPosition();
                        var flag = interf.Game.GetFlagAtPosition(position);

                        if (!flag.MergeNearbyPaths())
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                        }
                        else
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
                            interf.ClosePopup();
                        }
                    }
                    break;
                case Action.StartAttack:
                    if (!spectator && player.TotalKnightsAttacking > 0)
                    {
                        if (player.AttackingBuildingCount > 0)
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
                            player.StartAttack();
                        }

                        interf.ClosePopup();
                    }
                    else
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                    break;
                case Action.AttackingSelectAll1:
                    player.TotalKnightsAttacking = player.MaxAttackingKnightsByDistance[0];
                    break;
                case Action.AttackingSelectAll2:
                    player.TotalKnightsAttacking = player.MaxAttackingKnightsByDistance[0] +
                                              player.MaxAttackingKnightsByDistance[1];
                    break;
                case Action.AttackingSelectAll3:
                    player.TotalKnightsAttacking = player.MaxAttackingKnightsByDistance[0] +
                                              player.MaxAttackingKnightsByDistance[1] +
                                              player.MaxAttackingKnightsByDistance[2];
                    break;
                case Action.AttackingSelectAll4:
                    player.TotalKnightsAttacking = player.MaxAttackingKnightsByDistance[0] +
                                              player.MaxAttackingKnightsByDistance[1] +
                                              player.MaxAttackingKnightsByDistance[2] +
                                              player.MaxAttackingKnightsByDistance[3];
                    break;
                case Action.AttackingKnightsDec:
                    player.TotalKnightsAttacking = Math.Max(player.TotalKnightsAttacking - 1, 0);
                    break;
                case Action.AttackingKnightsInc:
                    player.TotalKnightsAttacking = Math.Min(player.TotalKnightsAttacking + 1, Math.Min(player.MaxAttackingKnights, 100));
                    break;
                case Action.OptionsFullscreen:
                    interf.RenderView.Fullscreen = !interf.RenderView.Fullscreen;
                    UserConfig.Video.Fullscreen = interf.RenderView.Fullscreen;
                    SetRedraw();
                    break;
                case Action.OptionsMusic:
                    {
                        var music = Audio?.GetMusicPlayer();

                        if (music != null)
                        {
                            music.Enabled = !music.Enabled;
                            UserConfig.Audio.Music = music.Enabled;
                            SetRedraw();
                        }
                        else
                        {
                            UserConfig.Audio.Music = false;
                        }
                    }
                    break;
                case Action.OptionsSfx:
                    {
                        var sfx = Audio?.GetSoundPlayer();

                        if (sfx != null)
                        {
                            sfx.Enabled = !sfx.Enabled;
                            UserConfig.Audio.Sound = sfx.Enabled;
                            SetRedraw();
                        }
                        else
                        {
                            UserConfig.Audio.Sound = false;
                        }
                    }
                    break;
                case Action.OptionsVolumeMinus:
                    {
                        var volumeControl = Audio?.GetVolumeController();

                        if (volumeControl != null)
                        {
                            volumeControl.VolumeDown();
                            UserConfig.Audio.Volume = volumeControl.Volume;
                            SetRedraw();
                        }
                        else
                        {
                            UserConfig.Audio.Volume = 0.0f;
                        }
                    }
                    break;
                case Action.OptionsVolumePlus:
                    {
                        var volumeControl = Audio?.GetVolumeController();

                        if (volumeControl != null)
                        {
                            volumeControl.VolumeUp();
                            UserConfig.Audio.Volume = volumeControl.Volume;
                            SetRedraw();
                        }
                        else
                        {
                            UserConfig.Audio.Volume = 0.0f;
                        }
                    }
                    break;
                case Action.OptionsMessageCount:
                    if (interf.GetOption(Option.MessagesAll))
                    {
                        interf.ResetOption(Option.MessagesAll);
                        interf.SetOption(Option.MessagesMost);
                    }
                    else if (interf.GetOption(Option.MessagesMost))
                    {
                        interf.ResetOption(Option.MessagesMost);
                        interf.SetOption(Option.MessagesFew);
                    }
                    else if (interf.GetOption(Option.MessagesFew))
                    {
                        interf.ResetOption(Option.MessagesFew);
                    }
                    else
                    {
                        interf.SetOption(Option.MessagesAll);
                        interf.SetOption(Option.MessagesMost);
                        interf.SetOption(Option.MessagesFew);
                    }
                    UserConfig.Game.Options = (int)interf.Options;
                    SetRedraw();
                    break;
                case Action.OptionsPathwayScrolling:
                    interf.SwitchOption(Option.PathwayScrolling);
                    UserConfig.Game.Options = (int)interf.Options;
                    break;
                case Action.OptionsFastMapclick:
                    interf.SwitchOption(Option.FastMapClick);
                    UserConfig.Game.Options = (int)interf.Options;
                    break;
                case Action.OptionsFastBuilding:
                    interf.SwitchOption(Option.FastBuilding);
                    UserConfig.Game.Options = (int)interf.Options;
                    break;
                case Action.OptionsInvertScrolling:
                    interf.SwitchOption(Option.InvertScrolling);
                    UserConfig.Game.Options = (int)interf.Options;
                    break;
                case Action.OptionsHideCursorWhileScrolling:
                    interf.SwitchOption(Option.HideCursorWhileScrolling);
                    UserConfig.Game.Options = (int)interf.Options;
                    break;
                case Action.OptionsResetCursorAfterScrolling:
                    interf.SwitchOption(Option.ResetCursorAfterScrolling);
                    UserConfig.Game.Options = (int)interf.Options;
                    break;
                case Action.QuitCancel:
                    if (Box == Type.QuitConfirm || Box == Type.NoSaveQuitConfirm || Box == Type.DiskMsg)
                        SetBox(Type.SettlerMenu);
                    // TODO
                    break;
                case Action.QuitConfirm:
                case Action.NoSaveQuitConfirm:
                    // no saving
                    interf.ClosePopup();
                    interf.OpenGameInit();
                    break;
                case Action.SaveAndQuit:
                    {
                        var saveFile = GameManager.Instance.GetCurrentGameSaveFile();

                        if (saveFile != null)
                        {
                            GameManager.Instance.SaveCurrentGame();
                        }
                        else
                        {
                            GameStore.Instance.QuickSave("quicksave", interf.Game);
                        }

                        SetBox(Type.DiskMsg);
                    }
                    break;
                case Action.Save:
                    SaveCurrentGame();
                    break;
                case Action.ShowSave:
                    SetBox(Type.LoadSave);
                    break;
                case Action.Demolish:
                    interf.DemolishObject();
                    interf.ClosePopup();
                    break;
                case Action.JumpToPlayer1:
                case Action.JumpToPlayer2:
                case Action.JumpToPlayer3:
                case Action.JumpToPlayer4:
                    {
                        var castlePos = interf.Game.GetPlayer((uint)(action - Action.JumpToPlayer1)).CastlePosition;

                        if (castlePos != Global.INVALID_MAPPOS)
                            interf.Viewport.MoveToMapPosition(castlePos, true);

                        SetBox(Type.PlayerStatistics);
                    }
                    break;
                // TODO ...
                default:
                    Log.Warn.Write(ErrorSystemType.UI, "unhandled action " + action.ToString());
                    break;
            }
        }

        void HandleBuildingClick(object tag, int x, int y)
        {
            if (interf.AccessRights != Viewer.Access.Player)
            {
                PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                return;
            }

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

            for (int i = 0; i < Game.GAME_MAX_PLAYER_COUNT; ++i)
            {
                playerFaceBackgrounds[i].Visible = false;
            }
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

            // Dispatch to one of the popup box functions above. 
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
                case Type.PlayerStatistics:
                    DrawPlayerStatisticsBox();
                    break;
                case Type.ResourceStatistics:
                    DrawResourceStatisticsBox();
                    break;
                case Type.FoodProductionCycle:
                    DrawFoodProductionCycleBox();
                    break;
                case Type.MaterialProductionCycle:
                    DrawMaterialProductionCycleBox();
                    break;
                case Type.SettlerStats:
                    DrawSerfCountBox();
                    break;
                case Type.IdleAndPotentialSettlerStats:
                    DrawIdleAndPotentialSerfsBox();
                    break;
                case Type.StartAttack:
                    DrawStartAttackBox();
                    break;
                case Type.GroundAnalysis:
                    DrawGroundAnalysisBox();
                    break;
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
                    DrawKnightLevelBox();
                    break;
                case Type.ToolmakerPriorities:
                    DrawToolmakerPrioritiesBox();
                    break;
                case Type.TransportPriorities:
                case Type.InventoryPriorities:
                    DrawTransportPrioritiesBox();
                    break;
                case Type.QuitConfirm:
                    DrawQuitConfirmBox();
                    break;
                case Type.NoSaveQuitConfirm:
                    DrawQuitConfirmBox();
                    DrawNoSaveQuitConfirmBox();
                    break;
                case Type.Options:
                    DrawOptionsBox(Action.ShowSettlerMenu);
                    break;
                case Type.ExtendedOptions:
                    DrawExtendedOptionsBox(Action.ShowSettlerMenu);
                    break;
                case Type.ScrollOptions:
                    DrawScrollOptionsBox(Action.ShowSettlerMenu);
                    break;
                case Type.GameInitOptions:
                    DrawOptionsBox(Action.CloseBox);
                    break;
                case Type.ExtendedGameInitOptions:
                    DrawExtendedOptionsBox(Action.CloseBox);
                    break;
                case Type.GameInitScrollOptions:
                    DrawScrollOptionsBox(Action.CloseBox);
                    break;
                case Type.CastleResources:
                    DrawCastleResourcesBox();
                    break;
                case Type.MineOutput:
                    DrawMineOutputBox();
                    break;
                case Type.OrderedBld:
                    DrawOrderedBuildingBox();
                    break;
                case Type.Defenders:
                    DrawDefendersBox();
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
                case Type.KnightSettings:
                    DrawKnightSettingsBox();
                    break;
                case Type.BuildingStock:
                    DrawBuildingStockBox();
                    break;
                case Type.PlayerFaces:
                    DrawPlayerFacesBox();
                    break;
                case Type.Demolish:
                    DrawDemolishBox();
                    break;
                case Type.LoadSave:
                    DrawSaveBox();
                    break;
                case Type.DiskMsg:
                    DrawDiskMessageBox();
                    break;
                default:
                    break;
            }

            base.InternalDraw();
        }

        protected override bool HandleClickLeft(int x, int y)
        {
            if (Box == Type.PlayerFaces)
            {
                SetBox(Type.PlayerStatistics);

                return true;
            }
            else if (Box == Type.FoodProductionCycle || Box == Type.MaterialProductionCycle)
            {
                SetBox(Type.StatMenu);

                return true;
            }
            else if (Box == Type.ExtendedOptions && clickableTextField != null)
            {
                if (x >= clickableTextField.TotalX && x < clickableTextField.TotalX + clickableTextField.Width &&
                    y >= clickableTextField.TotalY && y < clickableTextField.TotalY + clickableTextField.Height)
                {
                    HandleAction(Action.OptionsMessageCount, x, y);
                }

                return true;
            }
            else if (Box == Type.QuitConfirm && clickableTextField != null)
            {
                if (x >= clickableTextField.TotalX && x < clickableTextField.TotalX + clickableTextField.Width &&
                    y >= clickableTextField.TotalY && y < clickableTextField.TotalY + clickableTextField.Height)
                {
                    int relativeX = x - clickableTextField.TotalX;

                    if (relativeX >= 16 && relativeX < 16 + 24) // Yes
                    {
                        // TODO: who saves in multiplayer games? The server?
                        // TODO: What happens if the server leaves the game?
                        if (GameManager.Instance.NeedSave() && interf.AccessRights == Viewer.Access.Player)
                            SetBox(Type.NoSaveQuitConfirm);
                        else
                            HandleAction(Action.QuitConfirm, x, y);
                    }
                    else if (relativeX >= 16 + 24 + 64 && relativeX < 16 + 24 + 64 + 16) // No
                    {
                        HandleAction(Action.QuitCancel, x, y);
                    }

                    return true;
                }
            }
            else if (Box == Type.NoSaveQuitConfirm && clickableTextField != null)
            {
                if (x >= clickableTextField.TotalX && x < clickableTextField.TotalX + clickableTextField.Width &&
                    y >= clickableTextField.TotalY && y < clickableTextField.TotalY + clickableTextField.Height)
                {
                    int relativeX = x - clickableTextField.TotalX;

                    if (relativeX >= 16 && relativeX < 16 + 32) // Save
                    {
                        HandleAction(Action.SaveAndQuit, x, y);
                    }
                    else if (relativeX >= 16 + 32 + 16 && relativeX < 16 + 32 + 16 + 24) // Yes
                    {
                        HandleAction(Action.NoSaveQuitConfirm, x, y);
                    }
                    else if (relativeX >= 16 + 32 + 16 + 24 + 16 && relativeX < 16 + 32 + 16 + 24 + 16 + 16) // No
                    {
                        HandleAction(Action.QuitCancel, x, y);
                    }

                    return true;
                }
            }

            base.HandleClickLeft(x, y);

            return true; // always return true to avoid passing click events through
        }

        void PromoteKnights(int number)
        {
            var player = interf.Player;

            if (player.PromoteSerfsToKnights(number) == 0)
                PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
            else
                PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
        }

        void SetInventoryMode(Action action)
        {
            var building = interf.Game.GetBuilding(interf.Player.SelectedObjectIndex);
            var inventory = building.Inventory;

            switch (action)
            {
                case Action.ResourceModeIn:
                    inventory.Game.SetInventoryResourceMode(inventory, Inventory.Mode.In);
                    break;
                case Action.ResourceModeStop:
                    inventory.Game.SetInventoryResourceMode(inventory, Inventory.Mode.Stop);
                    break;
                case Action.ResourceModeOut:
                    inventory.Game.SetInventoryResourceMode(inventory, Inventory.Mode.Out);
                    break;
                case Action.SerfModeIn:
                    inventory.Game.SetInventorySerfMode(inventory, Inventory.Mode.In);
                    break;
                case Action.SerfModeStop:
                    inventory.Game.SetInventorySerfMode(inventory, Inventory.Mode.Stop);
                    break;
                case Action.SerfModeOut:
                    inventory.Game.SetInventorySerfMode(inventory, Inventory.Mode.Out);
                    break;
                default:
                    return;
            }

            PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
        }
    }
}
