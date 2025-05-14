﻿/*
 * PanelBar.cs - Panel GUI component
 *
 * Copyright (C) 2012       Jon Lund Steffensen <jonlst@gmail.com>
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

using System.Timers;

namespace Freeserf.UI
{
    using Data = Data.Data;

    internal class PanelBar : GuiObject
    {
        enum ButtonId
        {
            BuildInactive = 0,
            BuildFlag,
            BuildMine,
            BuildSmall,
            BuildLarge,
            BuildCastle,
            Destroy,
            DestroyInactive,
            BuildRoad,
            MapInactive,
            Map,
            StatsInactive,
            Stats,
            SettInactive,
            Sett,
            DestroyRoad,
            GroundAnalysis,
            BuildSmallStarred,
            BuildLargeStarred,
            MapStarred,
            StatsStarred,
            SettStarred,
            GroundAnalysisStarred,
            BuildMineStarred,
            BuildRoadStarred
        }

        static readonly ButtonId[] inactiveButtons =
        [
            ButtonId.BuildInactive,
            ButtonId.DestroyInactive,
            ButtonId.MapInactive,
            ButtonId.StatsInactive,
            ButtonId.SettInactive
        ];

        static readonly int[] BackgroundLayout =
        [
            6, 0, 0,
            0, 40, 0,
            20, 48, 0,

            7, 64, 0,
            8, 64, 36,
            21, 96, 0,

            9, 112, 0,
            10, 112, 36,
            22, 144, 0,

            11, 160, 0,
            12, 160, 36,
            23, 192, 0,

            13, 208, 0,
            14, 208, 36,
            24, 240, 0,

            15, 256, 0,
            16, 256, 36,
            25, 288, 0,

            1, 304, 0,
            6, 312, 0,
        ];

        static readonly int[] NotificationMinutes =
        [
            1, 5, 10, 30, 60
        ];

        readonly Interface interf = null;
        readonly Button messageIcon = null;
        readonly Button returnIcon = null;

        readonly Button[] gameSpeedButtons = new Button[5];
        readonly InvisibleButton[] notificationButtons = new InvisibleButton[5];
        readonly Button[] panelButtons = new Button[5];
        readonly ButtonId[] panelButtonIds = new ButtonId[5];
        readonly Render.ILayerSprite[] background = new Render.ILayerSprite[20];
        readonly Timer blinkTimer = new();
        bool blinkTrigger = false;
        readonly Render.IColoredRect playerColorIndicator = null;

        public PanelBar(Interface interf)
            : base(interf)
        {
            this.interf = interf;

            byte layerOffset = 1;

            messageIcon = new Button(interf, 8, 12, Data.Resource.FrameBottom, 3u, layerOffset);
            messageIcon.Clicked += MessageIcon_Clicked;
            AddChild(messageIcon, 40, 4, true);

            returnIcon = new Button(interf, 8, 10, Data.Resource.FrameBottom, 4u, layerOffset);
            returnIcon.Clicked += ReturnIcon_Clicked;
            AddChild(returnIcon, 40, 28, true);

            gameSpeedButtons[0] = new Button(interf, 8, 7, Data.Resource.SpeedButtons, 0u, layerOffset);
            gameSpeedButtons[1] = new Button(interf, 8, 7, Data.Resource.SpeedButtons, 0u, layerOffset);
            gameSpeedButtons[2] = new Button(interf, 8, 7, Data.Resource.SpeedButtons, 0u, layerOffset);
            gameSpeedButtons[3] = new Button(interf, 11, 7, Data.Resource.SpeedButtons, 0u, layerOffset);
            gameSpeedButtons[4] = new Button(interf, 14, 7, Data.Resource.SpeedButtons, 0u, layerOffset);

            int[] offsets = [ 0, 8, 16, 24, 35 ];
            int gameSpeedButtonY = 1;
            int index = 0;

            foreach (Button button in gameSpeedButtons)
            {
                AddChild(button, 294 + 8 - button.Width, gameSpeedButtonY, true);
                gameSpeedButtons[index].SetRelativeTextureAtlasOffset(offsets[index++], 0);

                gameSpeedButtonY += 7;
            }

            gameSpeedButtons[0].Clicked += (sender, e) => SetGameSpeed(0);
            gameSpeedButtons[1].Clicked += (sender, e) => SetGameSpeed(GameState.DEFAULT_GAME_SPEED);
            gameSpeedButtons[2].Clicked += (sender, e) => SetGameSpeed(GameState.DEFAULT_GAME_SPEED * 7);
            gameSpeedButtons[3].Clicked += (sender, e) => SetGameSpeed(GameState.DEFAULT_GAME_SPEED * 14);
            gameSpeedButtons[4].Clicked += (sender, e) => SetGameSpeed(Global.MAX_GAME_SPEED);

            for (int i = 0; i < notificationButtons.Length; i++)
            {
                int minutes = NotificationMinutes[i];
                notificationButtons[i] = new InvisibleButton(interf);
                notificationButtons[i].SetSize(7, 7);
                notificationButtons[i].DoubleClicked += (sender, e) => AddTimedNotification(minutes);
                AddChild(notificationButtons[i], 294 + 10, 1 + i * 7, true);
            }

            panelButtons[0] = new Button(interf, 32, 32, Data.Resource.PanelButton, (uint)ButtonId.BuildInactive, layerOffset);
            panelButtons[1] = new Button(interf, 32, 32, Data.Resource.PanelButton, (uint)ButtonId.DestroyInactive, layerOffset);
            panelButtons[2] = new Button(interf, 32, 32, Data.Resource.PanelButton, (uint)ButtonId.Map, layerOffset);
            panelButtons[3] = new Button(interf, 32, 32, Data.Resource.PanelButton, (uint)ButtonId.Stats, layerOffset);
            panelButtons[4] = new Button(interf, 32, 32, Data.Resource.PanelButton, (uint)ButtonId.Sett, layerOffset);

            panelButtons[0].Clicked += PanelBarButton_Clicked;
            panelButtons[1].Clicked += PanelBarButton_Clicked;
            panelButtons[2].Clicked += PanelBarButton_Clicked;
            panelButtons[3].Clicked += PanelBarButton_Clicked;
            panelButtons[4].Clicked += PanelBarButton_Clicked;

            panelButtons[0].DoubleClicked += TogglePossibleBuilds;
            panelButtons[2].DoubleClicked += GotoCastle;

            panelButtonIds[0] = ButtonId.BuildInactive;
            panelButtonIds[1] = ButtonId.DestroyInactive;
            panelButtonIds[2] = ButtonId.Map;
            panelButtonIds[3] = ButtonId.Stats;
            panelButtonIds[4] = ButtonId.Sett;

            for (int i = 0; i < 5; ++i)
                AddChild(panelButtons[i], 64 + i * 48, 4, true);

            // player color indicator
            playerColorIndicator = interf.RenderView.ColoredRectFactory.Create(Width + 2, Height + 1, Render.Color.Transparent, 0);
            playerColorIndicator.Layer = interf.RenderView.GetLayer(Freeserf.Layer.Gui);
            playerColorIndicator.Visible = true;

            // background
            var data = interf.RenderView.DataSource;

            for (int i = 0; i < 20; ++i)
            {
                var spriteInfo = data.GetSpriteInfo(Data.Resource.FrameBottom, (uint)BackgroundLayout[i * 3]);

                background[i] = CreateSprite(interf.RenderView.SpriteFactory, spriteInfo.Width, spriteInfo.Height, Data.Resource.FrameBottom, (uint)BackgroundLayout[i * 3], BaseDisplayLayer);
            }

            // blink timer
            blinkTimer.Interval = 700;
            blinkTimer.Elapsed += BlinkTimer_Elapsed;
            blinkTimer.Start();
        }

        private void AddTimedNotification(int minutes)
        {
            interf.Player.AddPositionTimer(minutes * 60 * Global.TICKS_PER_SEC, interf.MapCursorPosition);

            PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
        }

        private void SetGameSpeed(uint speed)
        {
            if (interf.Player.Game.GameSpeed != speed)
            {
                interf.Player.Game.SetSpeed(speed);
            }
        }

        private void GotoCastle(object sender, Button.ClickEventArgs args)
        {
            interf.GotoCastle();
            interf.ClosePopup();
        }

        private void TogglePossibleBuilds(object sender, Button.ClickEventArgs args)
        {
            if (interf.AccessRights == Viewer.Access.Player)
                interf.TogglePossibleBuilds();
            else
                interf.ResetPossibleBuilds();

            interf.ClosePopup();
        }

        private void ReturnIcon_Clicked(object sender, Button.ClickEventArgs args)
        {
            interf.ReturnFromMessage();
        }

        private void MessageIcon_Clicked(object sender, Button.ClickEventArgs args)
        {
            interf.OpenMessage();
        }

        private void PanelBarButton_Clicked(object sender, Button.ClickEventArgs args)
        {
            for (int i = 0; i < 5; ++i)
            {
                if (sender == panelButtons[i])
                {
                    ButtonClick(i);
                    break;
                }
            }
        }

        private void BlinkTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            blinkTrigger = !blinkTrigger;
            SetRedraw();
        }

        void SetButton(int index, ButtonId buttonId, bool updateId = true)
        {
            if (updateId)
                panelButtonIds[index] = buttonId;

            panelButtons[index].SetSpriteIndex((uint)buttonId);
        }

        public override bool Displayed
        {
            get => base.Displayed;
            set
            {
                base.Displayed = value;

                if (value)
                {
                    foreach (var button in panelButtons)
                        button.Displayed = true;

                    messageIcon.Displayed = true;
                    returnIcon.Displayed = true;

                    foreach (var speedButton in gameSpeedButtons)
                        speedButton.Displayed = true;

                    foreach (var notificationButton in notificationButtons)
                        notificationButton.Displayed = true;
                }
            }
        }

        protected override void InternalDraw()
        {
            DrawPanelFrame();
            DrawPanelButtons();
            DrawPlayerColor();
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            foreach (var bg in background)
                bg.Visible = false;

            playerColorIndicator.Visible = false;
        }

        protected internal override void UpdateParent()
        {
            base.UpdateParent();

            foreach (var bg in background)
                bg.DisplayLayer = BaseDisplayLayer;
        }

        // Draw notification icon in action panel. 
        void DrawMessageNotify(bool visible)
        {
            if (visible)
            {
                interf.SetMsgFlag(2);
                messageIcon.SetSpriteIndex(2u);
            }
            else
            {
                messageIcon.SetSpriteIndex(3u);
            }
        }

        // Draw return arrow icon in action panel. 
        void DrawReturnArrow(bool visible)
        {
            returnIcon.Displayed = visible && Displayed;
        }

        // Draw buttons in action panel. 
        void DrawPanelButtons()
        {
            var player = interf.Player;

            // Blinking message icon. 
            DrawMessageNotify(player != null && player.HasAnyNotification && blinkTrigger && Enabled);

            // Return arrow icon. 
            DrawReturnArrow(interf.GetMsgFlag(3) && Enabled);

            for (int i = 0; i < 5; ++i)
            {
                if (Enabled)
                    SetButton(i, panelButtonIds[i]);
                else
                    SetButton(i, inactiveButtons[i], false);
            }
        }

        // Draw the frame around action buttons. 
        void DrawPanelFrame()
        {
            for (int i = 0; i < 20; ++i)
            {
                var bg = background[i];

                bg.Layer = Layer;
                bg.X = TotalX + BackgroundLayout[i * 3 + 1];
                bg.Y = TotalY + BackgroundLayout[i * 3 + 2];
                bg.Visible = Displayed;
            }
        }

        void DrawPlayerColor()
        {
            var playerColor = interf.Player?.Color;

            if (playerColor == null)
                return;

            playerColorIndicator.DisplayLayer = 0;
            playerColorIndicator.X = TotalX - 1;
            playerColorIndicator.Y = TotalY - 1;
            playerColorIndicator.Resize(Width + 2, Height + 1);
            playerColorIndicator.Color = new Render.Color(playerColor.Red, playerColor.Green, playerColor.Blue);
            playerColorIndicator.Visible = interf.AccessRights != Viewer.Access.Player;
            playerColorIndicator.DisplayLayer = 0;
        }

        public void Update()
        {
            if (interf.PopupBox != null && interf.PopupBox.Displayed)
            {
                switch (interf.PopupBox.Box)
                {
                    case PopupBox.Type.TransportInfo:
                    case PopupBox.Type.OrderedBld:
                    case PopupBox.Type.CastleResources:
                    case PopupBox.Type.Defenders:
                    case PopupBox.Type.MineOutput:
                    case PopupBox.Type.BuildingStock:
                    case PopupBox.Type.StartAttack:
                    case PopupBox.Type.QuitConfirm:
                    case PopupBox.Type.NoSaveQuitConfirm:
                    case PopupBox.Type.Options:
                    case PopupBox.Type.ExtendedOptions:
                    case PopupBox.Type.ScrollOptions:
                        {
                            SetButton(0, ButtonId.BuildInactive);
                            SetButton(1, ButtonId.DestroyInactive);
                            SetButton(2, ButtonId.MapInactive);
                            SetButton(3, ButtonId.StatsInactive);
                            SetButton(4, ButtonId.SettInactive);
                            break;
                        }
                    default:
                        break;
                }
            }
            else if (interf.IsBuildingRoad)
            {
                SetButton(0, ButtonId.BuildRoadStarred);
                SetButton(1, ButtonId.BuildInactive);
                SetButton(2, ButtonId.MapInactive);
                SetButton(3, ButtonId.StatsInactive);
                SetButton(4, ButtonId.SettInactive);
            }
            else
            {
                SetButton(2, ButtonId.Map);
                SetButton(3, ButtonId.Stats);
                SetButton(4, ButtonId.Sett);

                var buildPossibility = interf.GetBuildPossibility();

                switch (interf.MapCursorType)
                {
                    case Interface.CursorType.None:
                        SetButton(0, ButtonId.BuildInactive);

                        if (interf.Player == null || interf.Player.HasCastle)
                        {
                            SetButton(1, ButtonId.DestroyInactive);
                        }
                        else
                        {
                            SetButton(1, ButtonId.GroundAnalysis);
                        }
                        break;
                    case Interface.CursorType.Flag:
                        SetButton(0, ButtonId.BuildRoad);
                        SetButton(1, ButtonId.DestroyInactive);
                        break;
                    case Interface.CursorType.RemovableFlag:
                        SetButton(0, ButtonId.BuildRoad);
                        SetButton(1, ButtonId.Destroy);
                        break;
                    case Interface.CursorType.Building:
                        SetButton(0, ButtonTypeFromBuildPossibility(buildPossibility));
                        SetButton(1, ButtonId.Destroy);
                        break;
                    case Interface.CursorType.Path:
                        SetButton(0, ButtonId.BuildInactive);
                        SetButton(1, ButtonId.DestroyRoad);

                        if (buildPossibility != Interface.BuildPossibility.None)
                        {
                            SetButton(0, ButtonId.BuildFlag);
                        }
                        break;
                    case Interface.CursorType.ClearByFlag:
                        if (buildPossibility == Interface.BuildPossibility.None ||
                            buildPossibility == Interface.BuildPossibility.Flag)
                        {
                            SetButton(0, ButtonId.BuildInactive);

                            if (interf.Player == null || interf.Player.HasCastle)
                            {
                                SetButton(1, ButtonId.DestroyInactive);
                            }
                            else
                            {
                                SetButton(1, ButtonId.GroundAnalysis);
                            }
                        }
                        else
                        {
                            SetButton(0, ButtonTypeFromBuildPossibility(buildPossibility));
                            SetButton(1, ButtonId.DestroyInactive);
                        }
                        break;
                    case Interface.CursorType.ClearByPath:
                        SetButton(0, ButtonTypeFromBuildPossibility(buildPossibility));
                        SetButton(1, ButtonId.DestroyInactive);
                        break;
                    case Interface.CursorType.Clear:
                        SetButton(0, ButtonTypeFromBuildPossibility(buildPossibility));

                        if (interf.Player != null && interf.Player.HasCastle)
                        {
                            SetButton(1, ButtonId.DestroyInactive);
                        }
                        else
                        {
                            SetButton(1, ButtonId.GroundAnalysis);
                        }
                        break;
                    default:
                        Debug.NotReached();
                        break;
                }
            }

            if (interf.AccessRights != Viewer.Access.Player)
            {
                SetButton(0, ButtonId.BuildInactive);
                SetButton(1, ButtonId.DestroyInactive);
            }

            SetRedraw();
        }

        ButtonId ButtonTypeFromBuildPossibility(Interface.BuildPossibility buildPossibility)
        {
            ButtonId result;

            switch (buildPossibility)
            {
                case Interface.BuildPossibility.Castle:
                    result = ButtonId.BuildCastle;
                    break;
                case Interface.BuildPossibility.Mine:
                    result = ButtonId.BuildMine;
                    break;
                case Interface.BuildPossibility.Large:
                    result = ButtonId.BuildLarge;
                    break;
                case Interface.BuildPossibility.Small:
                    result = ButtonId.BuildSmall;
                    break;
                case Interface.BuildPossibility.Flag:
                    result = ButtonId.BuildFlag;
                    break;
                default:
                    result = ButtonId.BuildInactive;
                    break;
            }

            return result;
        }

        public bool CanDemolish()
        {
            return panelButtonIds[1] != ButtonId.DestroyInactive;
        }

        public void Demolish()
        {
            ButtonClick(1);
        }

        public void ToggleMiniMap()
        {
            var popup = interf.PopupBox;

            if (popup != null && popup.Displayed)
            {
                interf.ClosePopup();
            }
            else
            {
                SetButton(0, ButtonId.BuildInactive);
                SetButton(1, ButtonId.DestroyInactive);
                SetButton(2, ButtonId.MapStarred);
                SetButton(3, ButtonId.StatsInactive);
                SetButton(4, ButtonId.SettInactive);

                interf.OpenPopup(PopupBox.Type.Map);

                // Synchronize minimap window with viewport. 
                if (popup != null)
                {
                    var minimap = popup.MiniMap;

                    if (minimap != null)
                    {
                        minimap.UpdateMinimap(true);
                    }
                }
            }
        }

        // Handle a click on the panel buttons. 
        void ButtonClick(int button)
        {
            var popup = interf.PopupBox;

            if (button == 0 && panelButtonIds[button] != ButtonId.BuildInactive)
            {
                interf.ResetPossibleBuilds();
            }

            switch (panelButtonIds[button])
            {
                case ButtonId.Map:
                case ButtonId.MapStarred:
                    PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);
                    ToggleMiniMap();                    
                    break;
                case ButtonId.Sett:
                case ButtonId.SettStarred:
                    PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);

                    if (popup != null && popup.Displayed)
                    {
                        interf.ClosePopup();
                    }
                    else
                    {
                        SetButton(0, ButtonId.BuildInactive);
                        SetButton(1, ButtonId.DestroyInactive);
                        SetButton(2, ButtonId.MapInactive);
                        SetButton(3, ButtonId.StatsInactive);
                        SetButton(4, ButtonId.SettStarred);
                        interf.OpenPopup(PopupBox.Type.SettlerMenu);
                    }
                    break;
                case ButtonId.Stats:
                case ButtonId.StatsStarred:
                    PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);

                    if (popup != null && popup.Displayed)
                    {
                        interf.ClosePopup();
                    }
                    else
                    {
                        SetButton(0, ButtonId.BuildInactive);
                        SetButton(1, ButtonId.DestroyInactive);
                        SetButton(2, ButtonId.MapInactive);
                        SetButton(3, ButtonId.StatsStarred);
                        SetButton(4, ButtonId.SettInactive);
                        interf.OpenPopup(PopupBox.Type.StatMenu);
                    }
                    break;
                case ButtonId.BuildRoad:
                case ButtonId.BuildRoadStarred:
                    PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);

                    if (interf.IsBuildingRoad)
                    {
                        interf.BuildRoadEnd();
                    }
                    else
                    {
                        interf.BuildRoadBegin();
                    }
                    break;
                case ButtonId.BuildFlag:
                    PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);
                    interf.BuildFlag();
                    break;
                case ButtonId.BuildSmall:
                case ButtonId.BuildSmallStarred:
                    PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);

                    if (popup != null && popup.Displayed)
                    {
                        interf.ClosePopup();
                    }
                    else
                    {
                        SetButton(0, ButtonId.BuildSmallStarred);
                        SetButton(1, ButtonId.DestroyInactive);
                        SetButton(2, ButtonId.MapInactive);
                        SetButton(3, ButtonId.StatsInactive);
                        SetButton(4, ButtonId.SettInactive);
                        interf.OpenPopup(PopupBox.Type.BasicBld);
                    }
                    break;
                case ButtonId.BuildLarge:
                case ButtonId.BuildLargeStarred:
                    PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);

                    if (popup != null && popup.Displayed)
                    {
                        interf.ClosePopup();
                    }
                    else
                    {
                        SetButton(0, ButtonId.BuildLargeStarred);
                        SetButton(1, ButtonId.DestroyInactive);
                        SetButton(2, ButtonId.MapInactive);
                        SetButton(3, ButtonId.StatsInactive);
                        SetButton(4, ButtonId.SettInactive);
                        interf.OpenPopup(PopupBox.Type.BasicBldFlip);
                    }
                    break;
                case ButtonId.BuildMine:
                case ButtonId.BuildMineStarred:
                    PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);

                    if (popup != null && popup.Displayed)
                    {
                        interf.ClosePopup();
                    }
                    else
                    {
                        SetButton(0, ButtonId.BuildMineStarred);
                        SetButton(1, ButtonId.DestroyInactive);
                        SetButton(2, ButtonId.MapInactive);
                        SetButton(3, ButtonId.StatsInactive);
                        SetButton(4, ButtonId.SettInactive);
                        interf.OpenPopup(PopupBox.Type.MineBuilding);
                    }
                    break;
                case ButtonId.Destroy:
                    if (interf.MapCursorType == Interface.CursorType.RemovableFlag)
                    {
                        interf.DemolishObject();
                    }
                    else
                    {
                        SetButton(0, ButtonId.BuildInactive);
                        SetButton(1, ButtonId.DestroyInactive);
                        SetButton(2, ButtonId.MapInactive);
                        SetButton(3, ButtonId.StatsInactive);
                        SetButton(4, ButtonId.SettInactive);
                        interf.OpenPopup(PopupBox.Type.Demolish);
                    }
                    break;
                case ButtonId.BuildCastle:
                    interf.BuildCastle();
                    break;
                case ButtonId.DestroyRoad:
                    {
                        bool result = interf.Player.Game.DemolishRoad(interf.MapCursorPosition, interf.Player);

                        if (!result)
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                            interf.UpdateMapCursorPosition(interf.MapCursorPosition);
                        }
                        else
                        {
                            if (interf.Viewer is ClientViewer clientViewer)
                                clientViewer.SendUserAction(Network.UserActionData.CreateDemolishRoadUserAction(Network.Global.SpontaneousMessage, interf.Game, interf.MapCursorPosition));
                            else if (interf.Viewer.ViewerType == Viewer.Type.Server)
                                interf.Server.GameDirty = true;

                            PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
                            interf.UpdateMapCursorPosition(interf.MapCursorPosition);
                        }
                    }
                    break;
                case ButtonId.GroundAnalysis:
                case ButtonId.GroundAnalysisStarred:
                    PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);

                    if (popup != null && popup.Displayed)
                    {
                        interf.ClosePopup();
                    }
                    else
                    {
                        SetButton(0, ButtonId.BuildInactive);
                        SetButton(1, ButtonId.GroundAnalysisStarred);
                        SetButton(2, ButtonId.MapInactive);
                        SetButton(3, ButtonId.StatsInactive);
                        SetButton(4, ButtonId.SettInactive);
                        interf.OpenPopup(PopupBox.Type.GroundAnalysis);
                    }
                    break;
            }

            if (interf.AccessRights != Viewer.Access.Player)
            {
                SetButton(0, ButtonId.BuildInactive);
                SetButton(1, ButtonId.DestroyInactive);
            }
        }

        protected override bool HandleKeyPressed(char key, int modifier)
        {
            if (key < '1' || key > '5')
            {
                return false;
            }

            ButtonClick(key - '1');

            return true;
        }

        protected override bool HandleClickLeft(int x, int y, bool delayed)
        {
            if (!delayed)
                SetRedraw();

            return true;
        }
    }
}
