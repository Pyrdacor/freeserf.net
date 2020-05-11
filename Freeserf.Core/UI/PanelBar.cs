/*
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

using System;

namespace Freeserf.UI
{
    using Data = Data.Data;

    // TODO: Click events for timers
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

        static readonly ButtonId[] inactiveButtons = new ButtonId[5]
        {
            ButtonId.BuildInactive,
            ButtonId.DestroyInactive,
            ButtonId.MapInactive,
            ButtonId.StatsInactive,
            ButtonId.SettInactive
        };

        static readonly int[] BackgroundLayout = new int[]
        {
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
        };

        Interface interf = null;
        Button messageIcon = null;
        Button returnIcon = null;
        Button[] panelButtons = new Button[5];
        ButtonId[] panelButtonIds = new ButtonId[5];
        Render.ILayerSprite[] background = new Render.ILayerSprite[20];
        System.Timers.Timer blinkTimer = new System.Timers.Timer();
        bool blinkTrigger = false;
        Render.IColoredRect playerColorIndicator = null;

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

                switch (interf.GetMapCursorType())
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
                    if (interf.GetMapCursorType() == Interface.CursorType.RemovableFlag)
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
                        bool result = interf.Player.Game.DemolishRoad(interf.GetMapCursorPosition(), interf.Player);

                        if (!result)
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                            interf.UpdateMapCursorPosition(interf.GetMapCursorPosition());
                        }
                        else
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
                            interf.UpdateMapCursorPosition(interf.GetMapCursorPosition());
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

        protected override bool HandleClickLeft(int x, int y)
        {
            SetRedraw();

            if (x >= 41 && x < 53)
            {
                // Message bar click 
                if (y < 16)
                {
                    // Message icon 
                    interf.OpenMessage();
                }
                else if (y >= 28)
                {
                    // Return arrow 
                    interf.ReturnFromMessage();
                }
            }
            else if (x >= 301 && x < 313)
            {
                // Timer bar click 
                // Call to map position 
                int timerLength;

                if (y < 7)
                {
                    timerLength = 5 * 60;
                }
                else if (y < 14)
                {
                    timerLength = 10 * 60;
                }
                else if (y < 21)
                {
                    timerLength = 20 * 60;
                }
                else if (y < 28)
                {
                    timerLength = 30 * 60;
                }
                else
                {
                    timerLength = 60 * 60;
                }

                interf.Player.AddPositionTimer(timerLength * Global.TICKS_PER_SEC, interf.GetMapCursorPosition());

                PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
            }

            return true;
        }
    }
}
