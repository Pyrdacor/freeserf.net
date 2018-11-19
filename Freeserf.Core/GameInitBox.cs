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

// TODO: changing from gametype load to something different will not re-show the player values in the boxes.
// In Debug with breakpoint it seem to show up.

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
        static readonly Dictionary<Action, Rect> ClickmapGeneral = new Dictionary<Action, Rect>
        {
            { Action.StartGame,         new Rect( 20, 16,  32, 32) },
            { Action.ToggleGameType,    new Rect( 60, 16,  32, 32) },
            { Action.ShowOptions,       new Rect(308, 16,  32, 32) },
            { Action.Close,             new Rect(324, 216, 16, 16) }
        };

        static readonly Dictionary<Action, Rect> ClickmapMission = new Dictionary<Action, Rect>(ClickmapGeneral)
        {
            { Action.Increment,         new Rect(284, 16, 16, 16) },
            { Action.Decrement,         new Rect(284, 32, 16, 16) }
        };

        static readonly Dictionary<Action, Rect> ClickmapCustom = new Dictionary<Action, Rect>(ClickmapGeneral)
        {
            { Action.Increment,         new Rect(220, 24, 24, 24) },
            { Action.Decrement,         new Rect(220, 16,  8,  8) },
            { Action.GenRandom,         new Rect(244, 16, 16,  8) },
            { Action.ApplyRandom,       new Rect(244, 24, 16, 24) }
        };

        static readonly Dictionary<Action, Rect> ClickmapLoad = new Dictionary<Action, Rect>(ClickmapGeneral)
        {
            // no additional actions
        };

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
        Minimap minimap = null;
        ListSavedFiles fileList = null;

        // rendering
        readonly List<Render.ILayerSprite> background = new List<Render.ILayerSprite>();
        Render.ILayerSprite buttonStart = null;
        Render.ILayerSprite buttonOptions = null;
        Render.ILayerSprite iconGameType = null;
        Render.TextField textFieldHeader = null;
        Render.TextField textFieldName = null;
        Render.TextField textFieldValue = null;
        Render.ILayerSprite buttonUp = null;
        Render.ILayerSprite buttonDown = null;
        Render.ILayerSprite buttonMapSize = null;
        readonly PlayerBox[] playerBoxes = new PlayerBox[4];
        Render.TextField textFieldVersion = null;
        Render.ILayerSprite buttonExit = null;

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
                var layer = interf.RenderView.GetLayer(global::Freeserf.Layer.Gui);
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
                if (this.x  == baseX + x && this.y == baseY + y)
                    return;

                this.x = baseX + x;
                this.y = baseY + y;

                SetChildPosition(baseX, baseY, x + 1, y + 8, playerImage);
                SetChildPosition(baseX, baseY, x + 6, y + 8, playerValueBox);

                SetChildPosition(baseX, baseY, x, y, borders[0]);
                SetChildPosition(baseX, baseY, x, y + 72, borders[1]);
                SetChildPosition(baseX, baseY, x + 9, y + 8, borders[2]);
                SetChildPosition(baseX, baseY, x + 5, y + 8, borders[3]);
                SetChildPosition(baseX, baseY, x, y + 8, borders[4]);

                ++x;
                y += 8;

                suppliesValue.X = baseX + 8 * x + 64;
                suppliesValue.Y = baseY + y + 76 - suppliesValue.Height;
                intelligenceValue.X = baseX + 8 * x + 70;
                intelligenceValue.Y = baseY + y + 76 - intelligenceValue.Height;
                reproductionValue.X = baseX + 8 * x + 76;
                reproductionValue.Y = baseY + y + 76 - reproductionValue.Height;

                valueBaseLineY = baseY + y + 76;
            }

            void SetChildPosition(int baseX, int baseY, int x, int y, Render.IRenderNode child)
            {
                child.X = baseX + 8 * x + 20;
                child.Y = baseY + y + 16;
            }

            public void SetPlayerFace(int face)
            {
                if (playerFace == face)
                    return;

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
            : base(interf)
        {
            this.interf = interf;

            randomInput = new RandomInput(interf);

            SetSize(360, 254);

            customMission = new GameInfo(new Random());
            mission = customMission;

            minimap = new Minimap(interf);
            minimap.Displayed = true;
            minimap.SetSize(150, 160);
            AddFloatWindow(minimap, 190, 55);

            GenerateMapPreview();

            randomInput.SetRandom(customMission.RandomBase);
            randomInput.Displayed = true;
            AddFloatWindow(randomInput, 15 + 31 * 8, 15);

            fileList = new ListSavedFiles(interf);
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
            var buttonLayer = (byte)(BaseDisplayLayer + 1);
            var bgLayer = BaseDisplayLayer;

            buttonStart = CreateSprite(spriteFactory, 32, 32, type, 266u, buttonLayer);
            buttonStart.Layer = Layer;
            buttonOptions = CreateSprite(spriteFactory, 32, 32, type, 267u, buttonLayer);
            buttonOptions.Layer = Layer;

            textFieldHeader = new Render.TextField(interf.TextRenderer);
            textFieldName = new Render.TextField(interf.TextRenderer);
            textFieldValue = new Render.TextField(interf.TextRenderer);

            iconGameType = spriteFactory.Create(32, 32, 0, 0, false, true, buttonLayer) as Render.ILayerSprite;
            iconGameType.Layer = Layer;

            buttonUp = CreateSprite(spriteFactory, 16, 16, type, 237u, buttonLayer);
            buttonUp.Layer = Layer;
            buttonDown = CreateSprite(spriteFactory, 16, 16, type, 240u, buttonLayer);
            buttonDown.Layer = Layer;
            buttonMapSize = CreateSprite(spriteFactory, 40, 32, type, 265u, buttonLayer);
            buttonMapSize.Layer = Layer;

            for (int i = 0; i < 4; ++i)
                playerBoxes[i] = new PlayerBox(interf, buttonLayer);

            textFieldVersion = new Render.TextField(interf.TextRenderer);

            buttonExit = CreateSprite(spriteFactory, 16, 16, type, 60u, buttonLayer);
            buttonExit.Layer = Layer;

            // We create a compound background in the TextureAtlasManager with
            // sprite index 318 inside the icon resources.
            // It is 360x80 in size
            int bgX = 0;
            int bgY = 0;
            while (bgY < Height)
            {
                var bg = CreateSprite(spriteFactory, Math.Min(360, Width - bgX), Math.Min(80, Height - bgY), type, 318u, bgLayer);
                bg.Layer = Layer;

                background.Add(bg);

                bgX += bg.Width;

                if (bgX == Width)
                {
                    bgX = 0;
                    bgY += bg.Height;
                }
            }
        }

        protected internal override void UpdateParent()
        {
            randomInput?.UpdateParent();
            minimap?.UpdateParent();
            fileList?.UpdateParent();
        }

        void DrawButton(int x, int y, Render.ILayerSprite button)
        {
            button.X = X + 8 * x + 20;
            button.Y = Y + y + 16;
            button.Visible = Displayed;
            button.DisplayLayer = (byte)(BaseDisplayLayer + 1);
        }

        void HideButton(Render.ISprite button)
        {
            button.Visible = false;
        }

        void HideBoxString(Render.TextField textField)
        {
            textField.Visible = false;
        }

        void DrawBoxIcon(int x, int y, Render.ILayerSprite sprite, uint spriteIndex)
        {
            sprite.X = X + 8 * x + 20;
            sprite.Y = Y + y + 16;
            sprite.Visible = Displayed;
            sprite.DisplayLayer = (byte)(BaseDisplayLayer + 1);

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
            textField.DisplayLayer = (byte)(BaseDisplayLayer + 1);

            // TODO: textField.ColorText = Color.Green;
            // TODO: textField.ColorBg = Color.Black;
        }

        void DrawBackground()
        {
            int bgX = 0;
            int bgY = 0;
            int i = 0;

            while (bgY < Height)
            {
                background[i].X = X + bgX;
                background[i].Y = Y + bgY;
                background[i].Visible = Displayed;
                background[i].DisplayLayer = BaseDisplayLayer;

                bgX += background[i].Width;

                if (bgX == Width)
                {
                    bgX = 0;
                    bgY += background[i].Height;
                }

                ++i;
            }
        }

        void DrawDefaultButtons()
        {
            DrawButton(0, 0, buttonStart);
            DrawButton(36, 0, buttonOptions);
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            foreach (var bg in background)
                bg.Visible = false;

            buttonStart.Visible = false;
            buttonOptions.Visible = false;
            iconGameType.Visible = false;
            textFieldHeader.Visible = false;
            textFieldName.Visible = false;
            textFieldValue.Visible = false;
            buttonUp.Visible = false;
            buttonDown.Visible = false;
            buttonMapSize.Visible = false;

            for (int i = 0; i < 4; ++i)
                playerBoxes[i].Visible = false;

            textFieldVersion.Visible = false;
            buttonExit.Visible = false;
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

                    playerBoxes[i].SetPosition(X, Y, 10 * bx, 40 + by * 80);
                    playerBoxes[i].Visible = true;
                    playerBoxes[i].SetBaseDisplayLayer(BaseDisplayLayer);

                    ++bx;
                }
            }
            else
            {
                for (int i = 0; i < 4; ++i)
                    playerBoxes[i].Visible = false;
            }

            /* Display program name and version in caption */
            DrawBoxString(0, 212, textFieldVersion, Freeserf.VERSION);

            DrawButton(38, 208, buttonExit);
        }

        public void HandleAction(Action action)
        {
            switch (action)
            {
                case Action.StartGame:
                {
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

                    interf.CloseGameInit();
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
                                GenerateMapPreview();
                                break;
                            }
                        case GameType.Custom:
                            {
                                mission = customMission;
                                randomInput.Displayed = true;
                                randomInput.SetRandom(customMission.RandomBase);
                                fileList.Displayed = false;
                                GenerateMapPreview();
                                break;
                            }
                        case GameType.Load:
                            {
                                randomInput.Displayed = false;
                                fileList.Displayed = true;
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
                            break;
                        case GameType.Custom:
                            customMission.MapSize = Math.Min(8u, customMission.MapSize + 1u);
                        break;
                    }

                    GenerateMapPreview();
                    break;
                case Action.Decrement:
                    switch (gameType)
                    {
                        case GameType.Mission:
                            gameMission = Math.Max(0, gameMission - 1);
                            mission = GameInfo.GetMission((uint)gameMission);
                            break;
                        case GameType.Custom:
                            customMission.MapSize = Math.Max(3u, customMission.MapSize - 1u);
                            break;
                    }

                    GenerateMapPreview();
                    break;
                case Action.Close:
                    // TODO: what happens then? close whole app?
                    interf.CloseGameInit();
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
                            GenerateMapPreview();
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        protected override bool HandleClickLeft(int x, int y)
        {
            Dictionary<Action, Rect> clickmap = null;

            switch (gameType)
            {
                case GameType.Mission:
                    clickmap = ClickmapMission;
                    break;
                case GameType.Custom:
                    clickmap = ClickmapCustom;
                    break;
                case GameType.Load:
                    clickmap = ClickmapLoad;
                    break;
                default:
                    return false;
            }

            var clickPosition = new Position(x, y);

            foreach (var clickArea in  clickmap)
            {
                if (clickArea.Value.Contains(clickPosition))
                {
                    SetRedraw();
                    HandleAction(clickArea.Key);
                    return true;
                }
            }

            /* Check player area */
            int lx = 0;
            int ly = 0;

            for (uint i = 0; i < 4; ++i)
            {
                var playerRect = new Rect(20 + lx * 80, 56 + ly * 80, 80, 80);
                
                if (playerRect.Contains(clickPosition))
                {
                    if (HandlePlayerClick(i, clickPosition.X - playerRect.Position.X, clickPosition.Y - playerRect.Position.Y))
                    {
                        break;
                    }
                }

                ++lx;
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
                cx -= 8 + 32 + 8 + 3;

                if (cx < 0)
                {
                    return false;
                }

                if (cy >= 27 && cy < 69)
                {
                    uint value = (uint)Misc.Clamp(0, 68 - cy, 40);

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

        void GenerateMapPreview()
        {
            map = new Map(new MapGeometry(mission.MapSize), interf.RenderView);

            if (gameType == GameType.Mission)
            {
                ClassicMissionMapGenerator generator = new ClassicMissionMapGenerator(map, mission.RandomBase);
                generator.Init();
                generator.Generate();
                map.InitTiles(generator);
            }
            else
            {
                ClassicMapGenerator generator = new ClassicMapGenerator(map, mission.RandomBase);
                generator.Init(MapGenerator.HeightGenerator.Midpoints, true);
                generator.Generate();
                map.InitTiles(generator);
            }

            minimap.SetMap(map);

            SetRedraw();
        }
    }
}
