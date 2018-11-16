/*
 * GameInitBox.cs - Game initialization GUI component
 *
 * Copyright (C) 2013-2016  Jon Lund Steffensen <jonlst@gmail.com>
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
using System.Collections.Generic;
using System.Text;

namespace Freeserf
{
    class RandomInput : TextInput
    {
        string savedText = "";

        public RandomInput(Interface interf)
            : base(interf)
        {
            SetFilter(TextInputFilter);
            SetSize(34, 34);
            MaxLength = 16;
        }

        public void SetRandom(Random rnd)
        {
            Text = rnd.ToString();
        }

        public Random GetRandom()
        {
            return new Random(Text);
        }

        static bool TextInputFilter(char key, TextInput textInput)
        {
            if (key < '1' || key > '8')
            {
                return false;
            }

            if (textInput.Text.Length > 16)
            {
                return false;
            }

            return true;
        }

        protected override bool HandleClickLeft(int x, int y)
        {
            base.HandleClickLeft(x, y);

            savedText = Text;
            Text = "";

            return true;
        }

        protected override bool HandleFocusLoose()
        {
            base.HandleFocusLoose();

            if (Text.Length < 16 && savedText.Length == 16)
            {
                Text = savedText;
                savedText = "";
            }

            return true;
        }
    }

    internal class GameInitBox : GuiObject
    {
        public enum Action
        {
            StartGame,
            ToggleGameType,
            ShowOptions,
            Increment,
            Decrement,
            Close,
            GenRandom,
            ApplyRandom
        }

        public enum GameType
        {
            Custom = 0,
            Mission = 1,
            Load = 2
            // TODO: Later add: Multiplayer = ?
            // TODO: Later add: Tutorial = ?
            // TODO: Later add: AIvsAI = ?
        }

        static readonly uint[] GameTypeSprites = new uint[]
        {
            262u,
            260u,
            316u
            // TODO: Later add index for multiplayer: 263u
            // TODO: Later add index for tutorial: 261u
            // TODO: Later add index for ai vs ai: 264u
        };

        Interface interf = null;

        GameType gameType = GameType.Custom;
        int gameMission = 0;

        GameInfo customMission = null;
        GameInfo mission = null;

        RandomInput randomInput = null;
        Map map = null;
        Minimap minimap = null;
        ListSavedFiles fileList = null;

        // rendering
        Render.ISprite background = null;
        Render.ISprite buttonStart = null;
        Render.ISprite buttonOptions = null;
        Render.ISprite iconGameType = null;
        Render.TextField textFieldHeader = null;
        Render.TextField textFieldName = null;
        Render.TextField textFieldValue = null;
        Render.ISprite buttonUp = null;
        Render.ISprite buttonDown = null;
        Render.ISprite buttonMapSize = null;
        readonly PlayerBox[] playerBoxes = new PlayerBox[4];

        class PlayerBox
        {
            readonly Render.ISprite[] borders = new Render.ISprite[5];
            readonly Render.ISprite playerImage = null;
            readonly Render.ISprite playerValueBox = null;
            readonly Render.IColoredRect suppliesValue = null;
            readonly Render.IColoredRect intelligenceValue = null;
            readonly Render.IColoredRect reproductionValue = null;
            bool visible = false;
            int x = -1;
            int y = -1;
            int playerFace = -1;

            public PlayerBox(Interface interf)
            {
                var spriteFactory = interf.RenderView.SpriteFactory;
                var coloredRectFactory = interf.RenderView.ColoredRectFactory;
                var type = Data.Resource.Icon;
                var layer = interf.RenderView.GetLayer(global::Freeserf.Layer.Gui);

                borders[0] = CreateSprite(spriteFactory, 80, 8, type, 251u);
                borders[1] = CreateSprite(spriteFactory, 80, 8, type, 252u);
                borders[2] = CreateSprite(spriteFactory, 8, 64, type, 255u); // the order of the last 3 is reversed so drawing order is correct
                borders[3] = CreateSprite(spriteFactory, 8, 64, type, 254u);
                borders[4] = CreateSprite(spriteFactory, 8, 64, type, 253u);

                for (int i = 0; i < 5; ++i)
                    borders[i].Layer = layer;

                playerImage = CreateSprite(spriteFactory, 32, 64, type, 281u); // empty player box
                playerImage.Layer = layer;

                playerValueBox = CreateSprite(spriteFactory, 24, 64, type, 282u);
                playerValueBox.Layer = layer;

                // max values for the values seem to be 40
                suppliesValue = coloredRectFactory.Create(4, 40, new Render.Color(0x00, 0x93, 0x87));
                intelligenceValue = coloredRectFactory.Create(4, 40, new Render.Color(0x6b, 0xab, 0x3b));
                reproductionValue = coloredRectFactory.Create(4, 40, new Render.Color(0xa7, 0x27, 0x27));
                suppliesValue.Layer = layer;
                intelligenceValue.Layer = layer;
                reproductionValue.Layer = layer;
            }

            public bool Visible
            {
                get => visible;
                set
                {
                    if (visible == value)
                        return;

                    visible = value;

                    playerImage.Visible = visible;

                    for (int i = 0; i < 5; ++i)
                        borders[i].Visible = visible;

                    if (playerFace != -1 && visible)
                    {
                        suppliesValue.Visible = true;
                        intelligenceValue.Visible = true;
                        reproductionValue.Visible = true;
                    }
                    else
                    {
                        suppliesValue.Visible = false;
                        intelligenceValue.Visible = false;
                        reproductionValue.Visible = false;
                    }
                }
            }

            public void SetPosition(int x, int y)
            {
                if (this.x == x && this.y == y)
                    return;

                this.x = x;
                this.y = y;

                playerImage.X = x + 1;
                playerImage.Y = y + 8;

                playerValueBox.X = x + 6; // is this right? do we need the sprite offsets?
                playerValueBox.Y = y + 8;

                borders[0].X = x;
                borders[0].Y = y;
                borders[1].X = x;
                borders[1].Y = y + 72;
                borders[2].X = x + 9;
                borders[2].Y = y + 8;
                borders[3].X = x + 5;
                borders[3].Y = y + 8;
                borders[4].X = x;
                borders[4].Y = y + 8;

                suppliesValue.X = x * 8 + 64;
                suppliesValue.Y = y + 76 - suppliesValue.Height;
                intelligenceValue.X = x * 8 + 70;
                intelligenceValue.Y = y + 76 - intelligenceValue.Height;
                reproductionValue.X = x * 8 + 76;
                reproductionValue.Y = y + 76 - reproductionValue.Height;
            }

            public void SetPlayerFace(int face)
            {
                if (playerFace == face)
                    return;

                playerFace = face;

                if (playerFace == -1)
                    playerImage.TextureAtlasOffset = GetTextureAtlasOffset(Data.Resource.Icon, 281u);
                else
                    playerImage.TextureAtlasOffset = GetTextureAtlasOffset(Data.Resource.Icon, 268u + (uint)playerFace);

                bool showValues = playerFace != -1 && visible;

                suppliesValue.Visible = showValues;
                intelligenceValue.Visible = showValues;
                reproductionValue.Visible = showValues;
            }

            public void SetPlayerValues(uint supplies, uint intelligence, uint reproduction)
            {
                AdjustValueRect(suppliesValue, (int)supplies);
                AdjustValueRect(intelligenceValue, (int)intelligence);
                AdjustValueRect(reproductionValue, (int)reproduction);
            }

            void AdjustValueRect(Render.IColoredRect valueRect, int value)
            {
                if (valueRect.Height != value)
                {
                    valueRect.Resize(4, value);
                    valueRect.Y = y + 76 - valueRect.Height;
                }
            }
        }

        public GameInitBox(Interface interf)
            : base(interf)
        {
            this.interf = interf;

            randomInput = new RandomInput(interf);

            SetSize(360, 254);

            customMission = new GameInfo(new Random());
            customMission.AddPlayer(12, PlayerInfo.PlayerColors[0], 40, 40, 40);
            customMission.AddPlayer(1, PlayerInfo.PlayerColors[1], 20, 30, 40);
            mission = customMission;

            minimap.Displayed = true;
            minimap.SetSize(150, 160);
            AddFloatWindow(minimap, 190, 55);

            generate_map_preview();

            randomInput.SetRandom(customMission.RandomBase);
            randomInput.Displayed = true;
            AddFloatWindow(randomInput, 19 + 31 * 8, 15);

            fileList.SetSize(160, 160);
            fileList.Displayed = false;
            fileList.SetSelectionHandler((string item) =>
            {
                Game game = new Game(interf.RenderView);

                if (GameStore.Instance.Load(item, game))
                {
                    map = game.Map;
                    minimap.SetMap(map);
                }
            });
            AddFloatWindow(fileList, 20, 55);

            InitRenderComponents();
        }

        void InitRenderComponents()
        {
            var spriteFactory = interf.RenderView.SpriteFactory;
            var type = Data.Resource.Icon;

            // We create a compound background in the TextureAtlasManager with
            // sprite index 318 inside the icon resources.
            background = CreateSprite(spriteFactory, Width, Height, type, 318u);
            background.Layer = Layer;

            buttonStart = CreateSprite(spriteFactory, 32, 32, type, 266u);
            buttonStart.Layer = Layer;
            buttonOptions = CreateSprite(spriteFactory, 32, 32, type, 267u);
            buttonOptions.Layer = Layer;

            textFieldHeader = new Render.TextField(interf.TextRenderer);
            textFieldName = new Render.TextField(interf.TextRenderer);
            textFieldValue = new Render.TextField(interf.TextRenderer);

            iconGameType = spriteFactory.Create(32, 32, 0, 0, false);
            iconGameType.Layer = Layer;

            buttonUp = CreateSprite(spriteFactory, 16, 16, type, 237u);
            buttonUp.Layer = Layer;
            buttonDown = CreateSprite(spriteFactory, 16, 16, type, 240u);
            buttonDown.Layer = Layer;
            buttonMapSize = CreateSprite(spriteFactory, 40, 32, type, 265u);
            buttonMapSize.Layer = Layer;

            for (int i = 0; i < 4; ++i)
                playerBoxes[i] = new PlayerBox(interf);
        }

        void DrawButton(int x, int y, Render.ISprite button)
        {
            button.X = X + 8 * x + 20;
            button.Y = Y + y + 16;
            button.Visible = Displayed;
        }

        void HideButton(Render.ISprite button)
        {
            button.Visible = false;
        }

        void HideBoxString(Render.TextField textField)
        {
            textField.Visible = false;
        }

        void DrawBoxIcon(int x, int y, Render.ISprite sprite, uint spriteIndex)
        {
            sprite.X = X + 8 * x + 20;
            sprite.Y = Y + y + 16;
            sprite.Visible = Displayed;

            sprite.TextureAtlasOffset = GetTextureAtlasOffset(Data.Resource.Icon, spriteIndex);
        }

        void DrawGameTypeIcon(int x, int y)
        {
            DrawBoxIcon(x, y, iconGameType, GameTypeSprites[(int)gameType]);
        }

        void DrawBoxString(int x, int y, Render.TextField textField, string str)
        {
            textField.SetPosition(X + 8 * x + 20, Y + y + 16);
            textField.Text = str;
            textField.Visible = Displayed;

            // TODO: textField.ColorText = Color.Green;
            // TODO: textField.ColorBg = Color.Black;
        }

        void DrawBackground()
        {
            background.X = X;
            background.Y = Y;
            background.Visible = Displayed;
        }

        void DrawDefaultButtons()
        {
            DrawButton(0, 0, buttonStart);
            DrawButton(36, 0, buttonOptions);
        }

        protected override void InternalDraw()
        {
            DrawBackground();
            DrawDefaultButtons();
            DrawGameTypeIcon(5, 0);

            switch (gameType)
            {
                case GameType.Custom:
                    DrawBoxString(10, 2, textFieldHeader, "New game");
                    DrawBoxString(10, 18, textFieldName, "Mapsize:");
                    DrawBoxString(20, 18, textFieldValue, mission.MapSize.ToString());

                    DrawButton(25, 0, buttonMapSize);
                    HideButton(buttonUp);
                    HideButton(buttonDown);
                    break;
                case GameType.Mission:
                    DrawBoxString(10, 2, textFieldHeader, "Start mission");
                    DrawBoxString(10, 18, textFieldName, "Mission:");
                    DrawBoxString(18, 18, textFieldValue, (gameMission + 1).ToString());

                    DrawButton(33, 0, buttonUp);
                    DrawButton(33, 16, buttonDown);
                    HideButton(buttonMapSize);
                    break;
                case GameType.Load:
                    DrawBoxString(10, 2, textFieldHeader, "Load game");
                    HideBoxString(textFieldName);
                    HideBoxString(textFieldValue);

                    HideButton(buttonUp);
                    HideButton(buttonDown);
                    HideButton(buttonMapSize);
                    break;
            }

            /* Game info */
            if (gameType != GameType.Load)
            {
                int bx = 0;
                int by = 0;

                for (int i = 0; i < 4; ++i)
                {
                    if (i >= mission.PlayerCount)
                    {
                        playerBoxes[i].SetPlayerFace(-1);
                    }
                    else
                    {
                        var player = mission.GetPlayer((uint)i);

                        playerBoxes[i].SetPlayerFace((int)player.Face);
                        playerBoxes[i].SetPlayerValues(player.Supplies, player.Intelligence, player.Reproduction);
                    }

                    playerBoxes[i].SetPosition(10 * bx, 40 + by * 80);
                    playerBoxes[i].Visible = true;

                    ++bx;

                    if (i == 1)
                    {
                        ++by;
                        bx = 0;
                    }
                }
            }
            else
            {
                for (int i = 0; i < 4; ++i)
                    playerBoxes[i].Visible = false;
            }

            /* Display program name and version in caption */
            draw_box_string(0, 212, FREESERF_VERSION);

            draw_box_icon(38, 208, 60); /* exit */
        }
    }
}
