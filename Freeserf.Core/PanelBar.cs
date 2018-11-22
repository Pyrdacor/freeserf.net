/*
 * PanelBar.cs - Panel GUI component
 *
 * Copyright (C) 2012  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Freeserf
{
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
        Icon messageIcon = null;
        Icon returnIcon = null;
        Button[] panelButtons = new Button[5];
        ButtonId[] panelButtonIds = new ButtonId[5];
        Render.ILayerSprite[] background = new Render.ILayerSprite[20];
        System.Timers.Timer blinkTimer = new System.Timers.Timer();
        bool blinkTrigger = false;

        public PanelBar(Interface interf)
            : base(interf)
        {
            this.interf = interf;

            var layer = (byte)(BaseDisplayLayer + 1);

            messageIcon = new Icon(interf, 8, 12, Data.Resource.FrameBottom, 3u, layer);
            AddChild(messageIcon, 40, 4, true);

            returnIcon = new Icon(interf, 8, 10, Data.Resource.FrameBottom, 4u, layer);
            AddChild(returnIcon, 40, 28, true);

            panelButtons[0] = new Button(interf, 32, 32, Data.Resource.PanelButton, (uint)ButtonId.BuildInactive, layer);
            panelButtons[1] = new Button(interf, 32, 32, Data.Resource.PanelButton, (uint)ButtonId.DestroyInactive, layer);
            panelButtons[2] = new Button(interf, 32, 32, Data.Resource.PanelButton, (uint)ButtonId.Map, layer);
            panelButtons[3] = new Button(interf, 32, 32, Data.Resource.PanelButton, (uint)ButtonId.Stats, layer);
            panelButtons[4] = new Button(interf, 32, 32, Data.Resource.PanelButton, (uint)ButtonId.Sett, layer);

            panelButtonIds[0] = ButtonId.BuildInactive;
            panelButtonIds[1] = ButtonId.DestroyInactive;
            panelButtonIds[2] = ButtonId.Map;
            panelButtonIds[3] = ButtonId.Stats;
            panelButtonIds[4] = ButtonId.Sett;

            for (int i = 0; i < 5; ++i)
                AddChild(panelButtons[i], 64 + i * 48, 4, true);

            // background
            var data = interf.RenderView.DataSource;

            for (int i = 0; i < 20; ++i)
            {
                var sprite = data.GetSprite(Data.Resource.FrameBottom, (uint)BackgroundLayout[i * 3], Sprite.Color.Transparent);

                background[i] = CreateSprite(interf.RenderView.SpriteFactory, (int)sprite.Width, (int)sprite.Height, Data.Resource.FrameBottom, (uint)BackgroundLayout[i * 3], BaseDisplayLayer);
            }

            // blink timer
            blinkTimer.Interval = 700;
            blinkTimer.Elapsed += BlinkTimer_Elapsed;
            blinkTimer.Start();
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

        public void Update()
        {

        }

        protected override void InternalDraw()
        {
            DrawPanelFrame();
            DrawPanelButtons();
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            foreach (var bg in background)
                bg.Visible = false;
        }

        protected internal override void UpdateParent()
        {
            base.UpdateParent();

            foreach (var bg in background)
                bg.DisplayLayer = BaseDisplayLayer;
        }

        /* Draw notification icon in action panel. */
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

        /* Draw return arrow icon in action panel. */
        void DrawReturnArrow(bool visible)
        {
            returnIcon.Displayed = visible && Displayed;
        }

        /* Draw buttons in action panel. */
        void DrawPanelButtons()
        {
            Player player = interf.GetPlayer();

            /* Blinking message icon. */
            DrawMessageNotify(player != null && player.HasNotification() && blinkTrigger && Enabled);

            /* Return arrow icon. */
            DrawReturnArrow(interf.GetMsgFlag(3) && Enabled);

            for (int i = 0; i < 5; ++i)
            {
                if (Enabled)
                    SetButton(i, panelButtonIds[i]);
                else
                    SetButton(i, inactiveButtons[i], false);
            }
        }

        /* Draw the frame around action buttons. */
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
    }
}
