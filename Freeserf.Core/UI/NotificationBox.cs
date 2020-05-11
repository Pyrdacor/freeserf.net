/*
 * NotificationBox.cs - Notification GUI component
 *
 * Copyright (C) 2013       Jon Lund Steffensen <jonlst@gmail.com>
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

namespace Freeserf.UI
{
    using Data = Data.Data;

    enum Decoration
    {
        Opponent = 0,
        Mine,
        Building,
        MapObject,
        Icon,
        Menu
    }

    struct NotificationView
    {
        public NotificationView(Notification.Type type, Decoration decoration, uint icon, string text)
        {
            Type = type;
            Decoration = decoration;
            Icon = icon;
            Text = text;
        }

        public Notification.Type Type;
        public Decoration Decoration;
        public uint Icon;
        public string Text;
    }

    internal class NotificationBox : Box
    {
        static readonly Dictionary<Notification.Type, NotificationView> notificationViews = new Dictionary<Notification.Type, NotificationView>(20)
        {
            { Notification.Type.UnderAttack, new NotificationView(Notification.Type.UnderAttack,
                Decoration.Opponent,
                0,
                "Your settlement\nis under attack") },
            { Notification.Type.LoseFight, new NotificationView(Notification.Type.LoseFight,
                Decoration.Opponent,
                0,
                "Your knights\njust lost the\nfight") },
            { Notification.Type.WinFight, new NotificationView(Notification.Type.WinFight,
                Decoration.Opponent,
                0,
                "You gained\na victory here") },
            { Notification.Type.MineEmpty, new NotificationView(Notification.Type.MineEmpty,
                Decoration.Mine,
                0,
                "This mine hauls\nno more raw\nmaterials") },
            { Notification.Type.CallToLocation, new NotificationView(Notification.Type.CallToLocation,
                Decoration.MapObject,
                0x90,
                "You wanted me\nto call you to\nthis location") },
            { Notification.Type.KnightOccupied, new NotificationView(Notification.Type.KnightOccupied,
                Decoration.Building,
                0,
                "A knight has\noccupied this\nnew building") },
            { Notification.Type.NewStock, new NotificationView(Notification.Type.NewStock,
                Decoration.MapObject,
                Render.RenderBuilding.MapBuildingSprite[(int)Building.Type.Stock],
                "A new stock\nhas been built") },
            { Notification.Type.LostLand, new NotificationView(Notification.Type.LostLand,
                Decoration.Opponent,
                0,
                "Because of this\nenemy building\nyou lost some\nland") },
            { Notification.Type.LostBuildings, new NotificationView(Notification.Type.LostBuildings,
                Decoration.Opponent,
                0,
                "Because of this\nenemy building\nyou lost some\n" +
                "land and\nsome buildings") },
            { Notification.Type.EmergencyActive, new NotificationView(Notification.Type.EmergencyActive,
                Decoration.MapObject,
                Render.RenderBuilding.MapBuildingFrameSprite[(int)Building.Type.Stock],
                "Emergency\nprogram\nactivated") },
            { Notification.Type.EmergencyNeutral, new NotificationView(Notification.Type.EmergencyNeutral,
                Decoration.MapObject,
                Render.RenderBuilding.MapBuildingSprite[(int)Building.Type.Castle],
                "Emergency\nprogram\nneutralized") },
            { Notification.Type.FoundGold, new NotificationView(Notification.Type.FoundGold,
                Decoration.Icon,
                0x2f,
                "A geologist\nhas found gold") },
            { Notification.Type.FoundIron, new NotificationView(Notification.Type.FoundIron,
                Decoration.Icon,
                0x2c,
                "A geologist\nhas found iron") },
            { Notification.Type.FoundCoal, new NotificationView(Notification.Type.FoundCoal,
                Decoration.Icon,
                0x2e,
                "A geologist\nhas found coal") },
            { Notification.Type.FoundStone, new NotificationView(Notification.Type.FoundStone,
                Decoration.Icon,
                0x2b,
                "A geologist\nhas found stone") },
            { Notification.Type.CallToMenu, new NotificationView(Notification.Type.CallToMenu,
                Decoration.Menu,
                0,
                "You wanted me\nto call you\nto this menu") },
            { Notification.Type.ThirtyMinutesSinceSave, new NotificationView(Notification.Type.ThirtyMinutesSinceSave,
                Decoration.Icon,
                0x5d,
                "30 min. passed\nsince the last\nsaving") },
            { Notification.Type.OneHourSinceSave, new NotificationView(Notification.Type.OneHourSinceSave,
                Decoration.Icon,
                0x5d,
                "One hour passed\nsince the last\nsaving") },
            { Notification.Type.CallToStock, new NotificationView(Notification.Type.CallToStock,
                Decoration.MapObject,
                Render.RenderBuilding.MapBuildingSprite[(int)Building.Type.Stock],
                "You wanted me\nto call you\nto this stock") },
            { Notification.Type.None, new NotificationView(Notification.Type.None, 0, 0, null) }
        };

        static readonly uint[] MapMenuSprite = new uint[]
        {
            0xe6, 0xe7, 0xe8, 0xe9,
            0xea, 0xeb, 0x12a, 0x12b
        };

        Interface interf = null;
        Notification notification = null;

        // rendering
        Icon checkBox = null;
        Icon icon = null;
        Icon menuIcon = null;
        Render.IColoredRect playerFaceBackground = null;
        Icon playerFace = null;
        BuildingIcon building = null; // cross, mine, stock, castle or military buildings
        TextField[] textFieldMessage = new TextField[5]; // max 5 lines

        public NotificationBox(Interface interf)
            : base
            (
                interf,
                BackgroundPattern.CreateNotificationBoxBackground(interf.RenderView.SpriteFactory),
                Border.CreateNotificationBoxBorder(interf.RenderView.SpriteFactory)
            )
        {
            this.interf = interf;

            InitRenderComponents();
        }

        void InitRenderComponents()
        {
            var spriteFactory = interf.RenderView.SpriteFactory;
            var coloredRectFactory = interf.RenderView.ColoredRectFactory;
            var type = Data.Resource.Icon;
            byte iconLayer = 2;
            byte borderLayer = 1;

            checkBox = new Icon(interf, 16, 16, type, 288u, iconLayer);
            AddChild(checkBox, 14 * 8, 128);

            icon = new Icon(interf, 16, 16, type, 93u, iconLayer); // initialize with save icon
            AddChild(icon, 64, 72, false); // initially not visible

            menuIcon = new Icon(interf, 32, 32, type, 230u, iconLayer);
            AddChild(menuIcon, 56, 72, false); // initially not visible

            playerFace = new Icon(interf, 32, 64, type, 281u, iconLayer); // initialize with empty face
            AddChild(playerFace, 56, 48, false); // initially not visible

            building = new BuildingIcon(interf, 64, 97, 178u, iconLayer); // initialize with castle
            AddChild(building, 20 * 8, 14, false); // initially not visible

            for (int i = 0; i < 5; ++i)
            {
                textFieldMessage[i] = new TextField(interf, 3);
                AddChild(textFieldMessage[i], 0, 0, false);
            }

            playerFaceBackground = coloredRectFactory.Create(48, 72, Render.Color.Transparent, borderLayer);
            playerFaceBackground.Layer = Layer;
        }

        bool UpdateBuildingSprite(Decoration decoration, uint data, uint spriteIndex)
        {
            switch (decoration)
            {
                case Decoration.MapObject:  // cross, stock or castle
                case Decoration.Building:   // military buildings
                case Decoration.Mine:       // mine
                    break;
                default:
                    return false;
            }

            var spriteInfo = interf.RenderView.DataSource.GetSpriteInfo(Data.Resource.MapObject, spriteIndex);
            int yOffset = (spriteIndex == 0x90) ? 24 : 0; // adjust position for location indicator (cross)

            building.Resize(spriteInfo.Width, spriteInfo.Height);
            building.MoveTo((Width - spriteInfo.Width) / 2, Math.Min(yOffset + 64, Height - 16 - spriteInfo.Height));

            building.SetSpriteIndex(spriteIndex);

            return true;
        }

        public void Show(Notification message)
        {
            if (message.NotificationType == Notification.Type.None)
            {
                Displayed = false;
                return;
            }

            this.notification = message;
            Displayed = true;
            checkBox.Displayed = true;
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            playerFaceBackground.Visible = false;
        }

        protected override void InternalDraw()
        {
            base.InternalDraw();

            DrawNotification(notificationViews[notification.NotificationType]);
        }

        protected override bool HandleClickLeft(int x, int y)
        {
            Displayed = false;

            return true;
        }

        void DrawNotification(NotificationView view)
        {
            DrawMessage(8, 12, view.Text);

            var mapBuildingSprite = Render.RenderBuilding.MapBuildingSprite;

            uint spriteIndex = 0u;
            bool showPlayerFace = false;
            bool showIcon = false;
            bool showMenuIcon = false;
            bool showBuilding = false;

            switch (view.Decoration)
            {
                case Decoration.Opponent:
                    {
                        var player = interf.Game.GetPlayer(notification.Data);
                        var color = player.Color;
                        playerFaceBackground.Color = new Render.Color()
                        {
                            R = color.Red,
                            G = color.Green,
                            B = color.Blue,
                            A = 255
                        };
                        playerFace.SetSpriteIndex(GetPlayerFaceSprite(player.Face));
                        playerFaceBackground.X = TotalX + 48;
                        playerFaceBackground.Y = TotalY + 44;
                        playerFaceBackground.DisplayLayer = (byte)(BaseDisplayLayer + 2);
                        showPlayerFace = Displayed;
                    }
                    break;
                case Decoration.Mine:
                    spriteIndex = mapBuildingSprite[(int)Building.Type.StoneMine] + notification.Data;
                    showBuilding = Displayed;
                    break;
                case Decoration.Building:
                    showBuilding = Displayed;
                    switch (notification.Data)
                    {
                        case 0:
                            spriteIndex = mapBuildingSprite[(int)Building.Type.Hut];
                            break;
                        case 1:
                            spriteIndex = mapBuildingSprite[(int)Building.Type.Tower];
                            break;
                        case 2:
                            spriteIndex = mapBuildingSprite[(int)Building.Type.Fortress];
                            break;
                        default:
                            showBuilding = false;
                            Debug.NotReached();
                            break;
                    }
                    break;
                case Decoration.MapObject:
                    spriteIndex = view.Icon;
                    showBuilding = Displayed;
                    break;
                case Decoration.Icon:
                    showIcon = Displayed;
                    icon.SetSpriteIndex(view.Icon);
                    break;
                case Decoration.Menu:
                    showMenuIcon = Displayed;
                    menuIcon.SetSpriteIndex(MapMenuSprite[(int)notification.Data]);
                    break;
                default:
                    break;
            }

            if (!UpdateBuildingSprite(view.Decoration, notification.Data, spriteIndex))
                showBuilding = false;

            building.Displayed = showBuilding;
            menuIcon.Displayed = showMenuIcon;
            icon.Displayed = showIcon;
            playerFace.Displayed = showPlayerFace;
            playerFaceBackground.Visible = showPlayerFace;
        }

        void DrawString(int x, int y, TextField textField, string str)
        {
            textField.Text = str;

            if (textField.Parent == null)
                AddChild(textField, x, y);
            else
            {
                textField.MoveTo(x, y);
                textField.Displayed = Displayed;
            }

            // TODO: textField.ColorText = Color.Green;
            // TODO: textField.ColorBg = Color.Black;
        }

        void DrawMessage(int x, int y, string message)
        {
            var lines = message.Split('\n');

            for (int i = 0; i < 5; ++i)
            {
                if (i >= lines.Length)
                {
                    textFieldMessage[i].Displayed = false;
                    textFieldMessage[i].Destroy();
                }
                else
                {
                    DrawString(x, y, textFieldMessage[i], lines[i]);
                    y += 10;
                }
            }
        }

        static uint GetPlayerFaceSprite(PlayerFace face)
        {
            if (face != 0u)
                return 0x10b + (uint)face;

            return 0x119u; // sprite_face_none 
        }
    }
}
