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

namespace Freeserf
{
    class RandomInput : TextInput
    {
        string savedText = "";

        public RandomInput(Interface interf)
            : base(interf, true)
        {
            BackgroundColor = new Render.Color(0x23, 0x43, 0x00);
            BackgroundFocusColor = new Render.Color(0x23, 0x43, 0x00);

            SetFilter(TextInputFilter);
            SetSize(3 * 9 + 8, 3 * 9 + 8 + 1);
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

    // Note: Map size is limited to 3..9 which is (64x64 to 512x512)
    // 3: 64x64
    // 4: 128x64
    // 5: 128x128
    // 6: 256x128
    // 7: 256x256
    // 8: 512x256
    // 9: 512x512
    // A map size smaller than 3 like in original game would require to draw map objects more than once at the same time.
    internal class GameInitBox : Box
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
            Load = 2,
            Multiplayer = 3,
            Tutorial = 4,
            AIvsAI = 5
        }

        static readonly uint[] GameTypeSprites = new uint[]
        {
            262u,
            260u,
            316u,
            263u,
            261u,
            264u
        };

        Interface interf = null;

        GameType gameType = GameType.Custom;
        int gameMission = 0;

        GameInfo customMission = null;
        GameInfo mission = null;

        RandomInput randomInput = null;
        Map map = null;
        ListSavedFiles fileList = null;

        // rendering
        Button buttonStart = null;
        Button buttonOptions = null;
        Button buttonGameType = null;
        TextField textFieldHeader = null;
        TextField textFieldName = null;
        TextField textFieldValue = null;
        Button buttonUp = null;
        Button buttonDown = null;
        Button buttonMapSize = null;
        readonly PlayerBox[] playerBoxes = new PlayerBox[4];
        TextField textFieldVersion = null;
        Button buttonExit = null;

        class PlayerBox
        {
            readonly Render.ILayerSprite[] borders = new Render.ILayerSprite[5];
            readonly Render.ILayerSprite playerImage = null;
            readonly Render.ILayerSprite playerValueBox = null;
            readonly Render.IColoredRect suppliesValue = null;
            readonly Render.IColoredRect intelligenceValue = null;
            readonly Render.IColoredRect reproductionValue = null;
            bool visible = false;
            int x = -1;
            int y = -1;
            int playerFace = -1;
            int valueBaseLineY = 0;

            public PlayerBox(Interface interf, byte baseDisplayLayer)
            {
                var spriteFactory = interf.RenderView.SpriteFactory;
                var coloredRectFactory = interf.RenderView.ColoredRectFactory;
                var type = Data.Resource.Icon;
                var layer = interf.RenderView.GetLayer(Freeserf.Layer.Gui);
                var displayLayer = (byte)(baseDisplayLayer + 1);

                borders[0] = CreateSprite(spriteFactory, 80, 8, type, 251u, displayLayer);
                borders[1] = CreateSprite(spriteFactory, 80, 8, type, 252u, displayLayer);
                borders[2] = CreateSprite(spriteFactory, 8, 64, type, 255u, displayLayer); // the order of the last 3 is reversed so drawing order is correct
                borders[3] = CreateSprite(spriteFactory, 8, 64, type, 254u, displayLayer);
                borders[4] = CreateSprite(spriteFactory, 8, 64, type, 253u, displayLayer);

                for (int i = 0; i < 5; ++i)
                    borders[i].Layer = layer;

                playerImage = CreateSprite(spriteFactory, 32, 64, type, 281u, (byte)(baseDisplayLayer + 2)); // empty player box
                playerImage.Layer = layer;

                playerValueBox = CreateSprite(spriteFactory, 24, 64, type, 282u, (byte)(baseDisplayLayer + 2));
                playerValueBox.Layer = layer;

                // max values for the values seem to be 40
                suppliesValue = coloredRectFactory.Create(4, 40, new Render.Color(0x00, 0x93, 0x87), (byte)(baseDisplayLayer + 3));
                intelligenceValue = coloredRectFactory.Create(4, 40, new Render.Color(0x6b, 0xab, 0x3b), (byte)(baseDisplayLayer + 3));
                reproductionValue = coloredRectFactory.Create(4, 40, new Render.Color(0xa7, 0x27, 0x27), (byte)(baseDisplayLayer + 3));
                suppliesValue.Layer = layer;
                intelligenceValue.Layer = layer;
                reproductionValue.Layer = layer;
            }

            public Rect Area => new Rect(x, y, 80, 80);

            public bool Visible
            {
                get => visible;
                set
                {
                    if (visible == value)
                        return;

                    visible = value;

                    playerImage.Visible = visible;
                    playerValueBox.Visible = visible;

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

            public void SetBaseDisplayLayer(byte displayLayer)
            {
                for (int i = 0; i < 5; ++i)
                    borders[i].DisplayLayer = (byte)(displayLayer + 1);

                playerImage.DisplayLayer = (byte)(displayLayer + 2);
                playerValueBox.DisplayLayer = (byte)(displayLayer + 2);

                suppliesValue.DisplayLayer = (byte)(displayLayer + 3);
                intelligenceValue.DisplayLayer = (byte)(displayLayer + 3);
                reproductionValue.DisplayLayer = (byte)(displayLayer + 3);
            }

            public void SetPosition(int baseX, int baseY, int x, int y)
            {
                if (this.x  == baseX + 8 * x + 16 && this.y == baseY + y + 16)
                    return;

                this.x = baseX + 8 * x + 16;
                this.y = baseY + y + 16;

                SetChildPosition(baseX, baseY, x + 1, y + 8, playerImage);
                SetChildPosition(baseX, baseY, x + 6, y + 8, playerValueBox);

                SetChildPosition(baseX, baseY, x, y, borders[0]);
                SetChildPosition(baseX, baseY, x, y + 72, borders[1]);
                SetChildPosition(baseX, baseY, x + 9, y + 8, borders[2]);
                SetChildPosition(baseX, baseY, x + 5, y + 8, borders[3]);
                SetChildPosition(baseX, baseY, x, y + 8, borders[4]);

                ++x;
                y += 8;

                suppliesValue.X = baseX + 8 * x + 60;
                suppliesValue.Y = baseY + y + 76 - suppliesValue.Height;
                intelligenceValue.X = baseX + 8 * x + 66;
                intelligenceValue.Y = baseY + y + 76 - intelligenceValue.Height;
                reproductionValue.X = baseX + 8 * x + 72;
                reproductionValue.Y = baseY + y + 76 - reproductionValue.Height;

                valueBaseLineY = baseY + y + 76;
            }

            void SetChildPosition(int baseX, int baseY, int x, int y, Render.IRenderNode child)
            {
                child.X = baseX + 8 * x + 16;
                child.Y = baseY + y + 16;
            }

            public void SetPlayerFace(int face)
            {
                playerFace = face;

                if (playerFace == -1)
                    playerImage.TextureAtlasOffset = GetTextureAtlasOffset(Data.Resource.Icon, 281u);
                else
                    playerImage.TextureAtlasOffset = GetTextureAtlasOffset(Data.Resource.Icon, 268u + (uint)playerFace - 1u);

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
                    valueRect.Y = valueBaseLineY - value;
                }
            }
        }

        public GameInitBox(Interface interf)
            : base
            (
                interf, 
                BackgroundPattern.CreateGameInitBoxBackground(interf.RenderView.SpriteFactory),
                Border.CreateGameInitBoxBorder(interf.RenderView.SpriteFactory)
            )
        {
            this.interf = interf;

            randomInput = new RandomInput(interf);

            SetSize(16 + 320 + 16, 200);

            customMission = new GameInfo(new Random());
            mission = customMission;

            randomInput.SetRandom(customMission.RandomBase);
            AddChild(randomInput, 8 + 31 * 8, 12, true);

            fileList = new ListSavedFiles(interf);
            fileList.SetSize(160, 160);
            fileList.SetSelectionHandler((string item) =>
            {
                Game game = new Game(interf.RenderView);

                if (GameStore.Instance.Load(item, game))
                {
                    map = game.Map;
                }
            });
            AddChild(fileList, 20, 55, false);

            InitRenderComponents();
        }

        void InitRenderComponents()
        {
            var spriteFactory = interf.RenderView.SpriteFactory;
            var type = Data.Resource.Icon;
            byte buttonLayer = 2;

            buttonStart = new Button(interf, 32, 32, type, 266u, buttonLayer);
            buttonStart.Clicked += ButtonStart_Clicked;
            AddChild(buttonStart, 20, 16);

            buttonOptions = new Button(interf, 32, 32, type, 267u, buttonLayer);
            buttonOptions.Clicked += ButtonOptions_Clicked;
            AddChild(buttonOptions, 8 * 36 + 12, 16);

            textFieldHeader = new TextField(interf, 1, 9);
            textFieldName = new TextField(interf, 1, 9);
            textFieldValue = new TextField(interf, 1, 9);
            AddChild(textFieldHeader, 0, 0, false);
            AddChild(textFieldName, 0, 0, false);
            AddChild(textFieldValue, 0, 0, false);

            buttonGameType = new Button(interf, 32, 32, type, GameTypeSprites[(int)gameType], buttonLayer);
            buttonGameType.Clicked += ButtonGameType_Clicked;
            AddChild(buttonGameType, 8 * 5 + 20, 16);

            buttonUp = new Button(interf, 16, 16, type, 237u, buttonLayer);
            buttonUp.Clicked += ButtonUp_Clicked;
            AddChild(buttonUp, 8 * 33 + 12, 16, false);

            buttonDown = new Button(interf, 16, 16, type, 240u, buttonLayer);
            buttonDown.Clicked += ButtonDown_Clicked;
            AddChild(buttonDown, 8 * 33 + 12, 32, false);

            buttonMapSize = new Button(interf, 40, 32, type, 265u, buttonLayer);
            buttonMapSize.Clicked += ButtonMapSize_Clicked;
            AddChild(buttonMapSize, 8 * 25 + 12, 16, true);

            for (int i = 0; i < 4; ++i)
                playerBoxes[i] = new PlayerBox(interf, buttonLayer);

            textFieldVersion = new TextField(interf, 1);
            AddChild(textFieldVersion, 0, 0, false);

            buttonExit = new Button(interf, 16, 16, type, 60u, buttonLayer);
            buttonExit.Clicked += ButtonExit_Clicked;
            AddChild(buttonExit, 8 * 38 + 12, 170);
        }

        private void ButtonStart_Clicked(object sender, Button.ClickEventArgs e)
        {
            HandleAction(Action.StartGame);
        }

        private void ButtonOptions_Clicked(object sender, Button.ClickEventArgs e)
        {
            HandleAction(Action.ShowOptions);
        }

        private void ButtonGameType_Clicked(object sender, Button.ClickEventArgs e)
        {
            HandleAction(Action.ToggleGameType);
        }

        private void ButtonExit_Clicked(object sender, Button.ClickEventArgs e)
        {
            HandleAction(Action.Close);
        }

        private void ButtonMapSize_Clicked(object sender, Button.ClickEventArgs e)
        {
            if (e.X < 8 && e.Y < 8)
            {
                HandleAction(Action.Decrement);
            }
            else if (e.X < 24 && e.Y >= 8 && e.Y < 32)
            {
                HandleAction(Action.Increment);
            }
            else if (e.X >= 24 && e.X < 40)
            {
                if (e.Y < 8)
                    HandleAction(Action.GenRandom);
                else if (e.Y < 48)
                    HandleAction(Action.ApplyRandom);
            }
        }

        private void ButtonUp_Clicked(object sender, Button.ClickEventArgs e)
        {
            HandleAction(Action.Increment);
        }

        private void ButtonDown_Clicked(object sender, Button.ClickEventArgs e)
        {
            HandleAction(Action.Decrement);
        }

        void HideBoxString(TextField textField)
        {
            textField.Displayed = false;
        }

        void DrawBoxString(int x, int y, TextField textField, string str)
        {
            textField.Text = str;
            textField.MoveTo(8 * x + 16, y + 16);
            textField.Displayed = Displayed;

            // TODO: textField.ColorText = Color.Green;
            // TODO: textField.ColorBg = Color.Black;
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            for (int i = 0; i < 4; ++i)
                playerBoxes[i].Visible = false;
        }

        protected override void InternalDraw()
        {
            base.InternalDraw();

            buttonGameType.SetSpriteIndex(GameTypeSprites[(int)gameType]);

            switch (gameType)
            {
                case GameType.Custom:
                    DrawBoxString(10, 2, textFieldHeader, "New game");
                    DrawBoxString(10, 18, textFieldName, "Mapsize:");
                    DrawBoxString(20, 18, textFieldValue, mission.MapSize.ToString());

                    buttonUp.Displayed = false;
                    buttonDown.Displayed = false;
                    buttonMapSize.Displayed = true;
                    break;
                case GameType.Mission:
                    DrawBoxString(10, 2, textFieldHeader, "Start mission");
                    DrawBoxString(10, 18, textFieldName, "Mission:");
                    DrawBoxString(18, 18, textFieldValue, (gameMission + 1).ToString());

                    buttonUp.Displayed = true;
                    buttonDown.Displayed = true;
                    buttonMapSize.Displayed = false;
                    break;
                case GameType.Load:
                    DrawBoxString(10, 2, textFieldHeader, "Load game");
                    HideBoxString(textFieldName);
                    HideBoxString(textFieldValue);

                    buttonUp.Displayed = false;
                    buttonDown.Displayed = false;
                    buttonMapSize.Displayed = false;
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

                    playerBoxes[i].SetPosition(TotalX, TotalY, 10 * bx, 40 + by * 80);
                    playerBoxes[i].Visible = true;
                    playerBoxes[i].SetBaseDisplayLayer((byte)(BaseDisplayLayer + 1));

                    ++bx;
                }
            }
            else
            {
                for (int i = 0; i < 4; ++i)
                    playerBoxes[i].Visible = false;
            }

            /* Display program name and version in caption */
            DrawBoxString(2, 162, textFieldVersion, Global.VERSION);
        }

        public void HandleAction(Action action)
        {
            switch (action)
            {
                case Action.StartGame:
                {
                    interf.CloseGameInit();

                    if (gameType == GameType.Load)
                    {
                        string path = fileList.GetSelected();

                        if (!GameManager.Instance.LoadGame(path, interf.RenderView))
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (!GameManager.Instance.StartGame(mission, interf.RenderView))
                        {
                            return;
                        }
                    }
                    break;
                }
                case Action.ToggleGameType:
                    if (++gameType > GameType.Load)
                    {
                        gameType = GameType.Custom;
                    }

                    switch (gameType)
                    {
                        case GameType.Mission:
                            {
                                mission = GameInfo.GetMission((uint)gameMission);
                                randomInput.Displayed = false;
                                fileList.Displayed = false;
                                SetRedraw();
                                break;
                            }
                        case GameType.Custom:
                            {
                                mission = customMission;
                                randomInput.Displayed = true;
                                randomInput.SetRandom(customMission.RandomBase);
                                fileList.Displayed = false;
                                SetRedraw();
                                break;
                            }
                        case GameType.Load:
                            {
                                randomInput.Displayed = false;
                                fileList.Displayed = true;
                                SetRedraw();
                                break;
                            }
                    }
                    break;
                case Action.ShowOptions:
                    // TODO
                    break;
                case Action.Increment:
                    switch (gameType)
                    {
                        case GameType.Mission:
                            gameMission = Math.Min(gameMission + 1, (int)GameInfo.GetMissionCount() - 1);
                            mission = GameInfo.GetMission((uint)gameMission);
                            SetRedraw();
                            break;
                        case GameType.Custom:
                            if (customMission.MapSize == 9u)
                                return;
                            customMission.MapSize = customMission.MapSize + 1u;
                            SetRedraw();
                            break;
                    }
                    break;
                case Action.Decrement:
                    switch (gameType)
                    {
                        case GameType.Mission:
                            gameMission = Math.Max(0, gameMission - 1);
                            mission = GameInfo.GetMission((uint)gameMission);
                            SetRedraw();
                            break;
                        case GameType.Custom:
                            if (customMission.MapSize == 3u)
                                return;
                            customMission.MapSize = customMission.MapSize - 1u;
                            SetRedraw();
                            break;
                    }
                    break;
                case Action.Close:
                    interf.CloseGameInit();
                    interf.RenderView.Close();
                    break;
                case Action.GenRandom:
                    {
                        randomInput.SetRandom(new Random());
                        SetRedraw();
                        break;
                    }
                case Action.ApplyRandom:
                    {
                        string str = randomInput.Text;

                        if (str.Length == 16)
                        {
                            customMission.SetRandomBase(randomInput.GetRandom());
                            mission = customMission;
                            SetRedraw();
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        protected override bool HandleClickLeft(int x, int y)
        {
            var clickPosition = new Position(x, y);

            for (uint i = 0; i < 4; ++i)
            {
                var area = playerBoxes[i].Area;

                if (area.Contains(clickPosition))
                {
                    if (HandlePlayerClick(i, clickPosition.X - area.Position.X, clickPosition.Y - area.Position.Y))
                    {
                        break;
                    }
                }
            }

            return true;
        }

        bool HandlePlayerClick(uint playerIndex, int cx, int cy)
        {
            if (cx < 8 || cx > 8 + 64 || cy < 8 || cy > 76)
            {
                return false;
            }

            if (playerIndex >= mission.PlayerCount)
            {
                return true;
            }

            PlayerInfo player = mission.GetPlayer(playerIndex);
            
            if (cx < 8 + 32 && cy < 72) // click on face
            {
                bool canNotChange = (playerIndex == 0 && gameType != GameType.AIvsAI) ||
                                    (playerIndex == 1 && gameType == GameType.Multiplayer) ||
                                    gameType == GameType.Mission ||
                                    gameType == GameType.Tutorial ||
                                    gameType == GameType.Load;

                if (!canNotChange)
                {
                    /* Face */
                    bool inUse = false;

                    do
                    {
                        uint next = (player.Face + 1) % 11; // Note: Use 12 here to also allow the last enemy as a custom game player
                        next = Math.Max(1u, next);

                        player.SetCharacter(next);

                        /* Check that face is not already in use by another player */
                        inUse = false;

                        for (uint i = 0; i < mission.PlayerCount; ++i)
                        {
                            if (playerIndex != i &&
                                mission.GetPlayer(i).Face == next)
                            {
                                inUse = true;
                                break;
                            }
                        }

                    } while (inUse);
                }
            }
            else // click on values
            {
                cx -= 8 + 32 + 8 + 1;

                if (cx < 0)
                {
                    return false;
                }

                if (cy >= 25 && cy < 72)
                {
                    uint value = (uint)Misc.Clamp(0, 66 - cy, 40);

                    if (cx > 0 && cx < 6)
                    {
                        bool canNotChange = gameType == GameType.Mission ||
                                            gameType == GameType.Tutorial ||
                                            gameType == GameType.Load;

                        /* Supplies */
                        if (!canNotChange)
                            player.Supplies = value;
                    }
                    else if (cx > 6 && cx < 12)
                    {
                        bool canNotChange = (playerIndex == 0 && gameType != GameType.AIvsAI) ||
                                            (playerIndex == 1 && gameType == GameType.Multiplayer) ||
                                            gameType == GameType.Mission ||
                                            gameType == GameType.Tutorial ||
                                            gameType == GameType.Load;

                        /* Intelligence */
                        if (!canNotChange)
                            player.Intelligence = value;
                    }
                    else if (cx > 12 && cx < 18)
                    {
                        bool canNotChange = gameType == GameType.Mission ||
                                            gameType == GameType.Tutorial ||
                                            gameType == GameType.Load;

                        /* Reproduction */
                        if (!canNotChange)
                            player.Reproduction = value;
                    }
                }
            }

            SetRedraw();

            return true;
        }
    }
}
