/*
 * GameInitBox.cs - Game initialization GUI component
 *
 * Copyright (C) 2013-2016  Jon Lund Steffensen <jonlst@gmail.com>
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

using Freeserf.Network;
using System;
using System.Collections.Generic;

namespace Freeserf.UI
{
    using Freeserf.Event;
    using Data = Data.Data;

    class RandomInput : TextInput
    {
        string savedText = "";

        public RandomInput(Interface interf)
            : base(interf, 9, Render.TextRenderType.LegacySpecialDigits)
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
            ApplyRandom,
            CreateServer,
            IncrementLarge,
            DecrementLarge
        }

        public enum GameType
        {
            Custom = 0,
            Mission = 1,
            Load = 2,
            MultiplayerClient = 3,
            Tutorial = 4,
            AIvsAI = 5,
            MultiplayerServer = 6,
            MultiplayerJoined = 7,
            MultiplayerLoading = 8
        }

        static readonly uint[] GameTypeSprites = new uint[]
        {
            262u,
            260u,
            500u,
            263u,
            261u,
            264u,
            263u,
            263u,
            263u
        };

        Interface interf = null;
        GameType gameType = GameType.Custom;
        int gameMission = 0;

        GameInfo customMission = null;
        readonly RandomInput randomInput = null;
        readonly ListSavedFiles fileList = null;
        readonly ListServers serverList = null;

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
        TextField textCreateServer = null;
        Button buttonCreateServer = null;
        // multiplayer options
        CheckBox checkBoxServerValues = null; // the server sets the values of each player (otherwise each human client can set them for himself)
        CheckBox checkBoxSameValues = null; // the server sets the values and they are used for every player in the game (clients can't set values)
        // TODO: maybe the game speed should be setable (before the game) or changeable (option to change it in the game)

        // used only for multiplayer games
        readonly bool[] playerIsAI = new bool[4];
        readonly Dictionary<uint, Network.IRemoteClient> playerClientMapping = new Dictionary<uint, Network.IRemoteClient>(); // key: playerIndex, value: client
        public string ServerGameName { get; private set; } = "Freeserf Server";
        public GameInfo ServerGameInfo { get; private set; } = null;
        public ILocalServer Server { get; private set; } = null;
        public ILocalClient Client { get; private set; } = null;
        readonly Dictionary<uint, MultiplayerStatus> playerStatus = new Dictionary<uint, MultiplayerStatus>(); // key: playerIndex, value: status

        internal class PlayerBox
        {
            readonly Render.ILayerSprite[] borders = new Render.ILayerSprite[5];
            readonly Render.ILayerSprite playerImage = null;
            readonly Render.ILayerSprite playerValueBox = null;
            readonly Render.ILayerSprite activationButton = null;
            readonly Render.ILayerSprite copyValuesButton = null;
            readonly Render.IColoredRect suppliesValue = null;
            readonly Render.IColoredRect intelligenceValue = null;
            readonly Render.IColoredRect reproductionValue = null;
            bool visible = false;
            int x = -1;
            int y = -1;
            int playerFace = -1;
            int valueBaseLineY = 0;
            bool showActivationButton = false;
            bool showCopyValueButton = false;

            public bool ShowActivationButton
            {
                get => showActivationButton;
                set
                {
                    if (showActivationButton == value)
                        return;

                    showActivationButton = value;
                    activationButton.Visible = visible && showActivationButton;
                }
            }

            public bool ShowCopyValueButton
            {
                get => showCopyValueButton;
                set
                {
                    if (showCopyValueButton == value)
                        return;

                    showCopyValueButton = value;
                    copyValuesButton.Visible = visible && showCopyValueButton && playerFace != -1;
                }
            }


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

                activationButton = CreateSprite(spriteFactory, 24, 16, type, 259u, (byte)(baseDisplayLayer + 3));
                activationButton.Layer = layer;

                copyValuesButton = CreateSprite(spriteFactory, 8, 16, type, 308u, (byte)(baseDisplayLayer + 4));
                copyValuesButton.Layer = layer;

                // max values for the values seem to be 40
                suppliesValue = coloredRectFactory.Create(4, 40, new Render.Color(0x00, 0x93, 0x87), (byte)(baseDisplayLayer + 4));
                intelligenceValue = coloredRectFactory.Create(4, 40, new Render.Color(0x6b, 0xab, 0x3b), (byte)(baseDisplayLayer + 4));
                reproductionValue = coloredRectFactory.Create(4, 40, new Render.Color(0xa7, 0x27, 0x27), (byte)(baseDisplayLayer + 4));
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
                    activationButton.Visible = visible && showActivationButton;
                    copyValuesButton.Visible = visible && showCopyValueButton && playerFace != -1;

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
                activationButton.DisplayLayer = (byte)(displayLayer + 3);
                copyValuesButton.DisplayLayer = (byte)(displayLayer + 4);

                suppliesValue.DisplayLayer = (byte)(displayLayer + 4);
                intelligenceValue.DisplayLayer = (byte)(displayLayer + 4);
                reproductionValue.DisplayLayer = (byte)(displayLayer + 4);
            }

            public void SetPosition(int baseX, int baseY, int x, int y)
            {
                if (this.x == baseX + 8 * x + 16 && this.y == baseY + y + 16)
                    return;

                this.x = baseX + 8 * x + 16;
                this.y = baseY + y + 16;

                SetChildPosition(baseX, baseY, x + 1, y + 8, playerImage);
                SetChildPosition(baseX, baseY, x + 6, y + 8, playerValueBox);
                SetChildPosition(baseX, baseY, x + 6, y + 8, activationButton);
                SetChildPosition(baseX, baseY, x + 5, y + 8, copyValuesButton);

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
                {
                    playerImage.TextureAtlasOffset = GetTextureAtlasOffset(Data.Resource.Icon, 281u);
                    activationButton.TextureAtlasOffset = GetTextureAtlasOffset(Data.Resource.Icon, 287u);
                    copyValuesButton.Visible = false;
                }
                else
                {
                    playerImage.TextureAtlasOffset = GetTextureAtlasOffset(Data.Resource.Icon, 268u + (uint)playerFace - 1u);
                    activationButton.TextureAtlasOffset = GetTextureAtlasOffset(Data.Resource.Icon, 259u);
                    copyValuesButton.Visible = visible && showCopyValueButton;
                }

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

        public GameInitBox(Interface interf, GameType gameType)
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

            customMission = new GameInfo(new Random(), false);
            ServerGameInfo = customMission;

            randomInput.SetRandom(customMission.RandomBase);
            AddChild(randomInput, 8 + 31 * 8, 12, true);

            fileList = new ListSavedFiles(interf);
            fileList.SetSize(310, 104);
            AddChild(fileList, 20, 55, false);

            serverList = new ListServers(interf);
            serverList.SetSize(310, 95);
            AddChild(serverList, 20, 55, false);

            this.gameType = gameType;
            UpdateGameType();

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
            textCreateServer = new TextField(interf, 1, 9);
            AddChild(textFieldHeader, 0, 0, false);
            AddChild(textFieldName, 0, 0, false);
            AddChild(textFieldValue, 0, 0, false);
            AddChild(textCreateServer, 0, 0, false);

            buttonGameType = new Button(interf, 32, 32, type, GameTypeSprites[(int)gameType], buttonLayer);
            buttonGameType.Clicked += ButtonGameType_Clicked;
            AddChild(buttonGameType, 8 * 5 + 20, 16);

            buttonUp = new Button(interf, 16, 16, type, 237u, buttonLayer);
            buttonUp.Clicked += ButtonUp_Clicked;
            AddChild(buttonUp, 7 * 33 + 12, 16, false);

            buttonDown = new Button(interf, 16, 16, type, 240u, buttonLayer);
            buttonDown.Clicked += ButtonDown_Clicked;
            AddChild(buttonDown, 7 * 33 + 12, 32, false);

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

            buttonCreateServer = new Button(interf, 16, 16, type, 221u, buttonLayer);
            buttonCreateServer.Clicked += ButtonCreateServer_Clicked;
            AddChild(buttonCreateServer, 8 * 22 + 14, 151, false);

            checkBoxServerValues = new CheckBox(interf);
            checkBoxServerValues.Text = "Server Values";
            checkBoxServerValues.CheckedChanged += CheckBoxServerValues_CheckedChanged;
            AddChild(checkBoxServerValues, 24, 141, false);

            checkBoxSameValues = new CheckBox(interf);
            checkBoxSameValues.Text = "Identical Values";
            checkBoxSameValues.CheckedChanged += CheckBoxSameValues_CheckedChanged;
            AddChild(checkBoxSameValues, 180, 141, false);
        }

        private void CheckBoxServerValues_CheckedChanged(object sender, System.EventArgs e)
        {
            if (checkBoxServerValues.Checked)
            {
                if (checkBoxSameValues.Checked)
                {
                    // TODO: is this necessary?
                    checkBoxSameValues.Checked = false; // this will trigger the server update in this case
                    return;
                }
            }

            ServerUpdate();
        }

        private void CheckBoxSameValues_CheckedChanged(object sender, System.EventArgs e)
        {
            if (checkBoxSameValues.Checked)
            {
                checkBoxServerValues.Checked = false;

                var player1 = ServerGameInfo.GetPlayer(0);

                for (uint i = 1; i < ServerGameInfo.PlayerCount; ++i)
                {
                    var player = ServerGameInfo.GetPlayer(i);

                    player.Supplies = player1.Supplies;
                    player.Reproduction = player1.Reproduction;

                    playerBoxes[i].SetPlayerValues(player.Supplies, player.Intelligence, player.Reproduction);
                }
            }

            ServerUpdate();
        }

        private void ButtonCreateServer_Clicked(object sender, Button.ClickEventArgs args)
        {
            HandleAction(Action.CreateServer);
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

        ServerInfo GetSelectedServer()
        {
            return serverList.GetSelected();
        }

        string GetServerName()
        {
            var serverInfo = GetSelectedServer();

            if (serverInfo == null)
                return "";

            return serverInfo.Name;
        }

        string GetServerHostname()
        {
            var serverInfo = GetSelectedServer();

            if (serverInfo == null)
                return "";

            return serverInfo.HostName;
        }

        int GetServerCurrentPlayers()
        {
            var serverInfo = GetSelectedServer();

            if (serverInfo == null)
                return 0;

            return Math.Max(1, serverInfo.CurrentPlayers);
        }

        int GetServerMaxPlayers()
        {
            var serverInfo = GetSelectedServer();

            if (serverInfo == null)
                return 0;

            return Math.Min(4, serverInfo.MaxPlayers);
        }

        protected override void InternalDraw()
        {
            base.InternalDraw();

            buttonStart.Displayed = gameType != GameType.MultiplayerJoined && gameType != GameType.MultiplayerLoading;
            buttonOptions.Displayed = true;
            buttonGameType.Displayed = true;
            buttonExit.Displayed = true;

            buttonGameType.SetSpriteIndex(GameTypeSprites[(int)gameType]);

            checkBoxServerValues.Displayed = gameType == GameType.MultiplayerServer;
            checkBoxSameValues.Displayed = gameType == GameType.MultiplayerServer;

            switch (gameType)
            {
                case GameType.Custom:
                case GameType.AIvsAI:
                case GameType.MultiplayerClient:
                case GameType.MultiplayerServer:
                case GameType.MultiplayerJoined:
                case GameType.MultiplayerLoading:
                    string header;
                    switch (gameType)
                    {
                        case GameType.Custom:
                        default:
                            header = "New game";
                            break;
                        case GameType.AIvsAI:
                            header = "Demo";
                            break;
                        case GameType.MultiplayerClient:
                            header = "Multiplayer";
                            break;
                        case GameType.MultiplayerServer:
                            header = "Host game";
                            break;
                        case GameType.MultiplayerJoined:
                            header = "Multiplayer Join";
                            break;
                        case GameType.MultiplayerLoading:
                            header = "Starting Game";
                            break;
                    }

                    DrawBoxString(10, 2, textFieldHeader, header);

                    if (gameType == GameType.MultiplayerClient)
                    {
                        DrawBoxString(10, 18, textFieldName, "Server:");
                        string serverName = GetServerName();

                        if (serverName.Length > 15)
                            serverName = serverName.Substring(0, 12) + "...";

                        DrawBoxString(18, 18, textFieldValue, serverName);
                        DrawBoxString(24, 139, textCreateServer, "Create server");

                        buttonUp.Displayed = false;
                        buttonDown.Displayed = false;
                        buttonMapSize.Displayed = false;
                        buttonCreateServer.Displayed = true;
                    }
                    else
                    {
                        DrawBoxString(10, 18, textFieldName, "Mapsize:");
                        DrawBoxString(20, 18, textFieldValue, ServerGameInfo.MapSize.ToString());
                        HideBoxString(textCreateServer);

                        buttonUp.Displayed = false;
                        buttonDown.Displayed = false;
                        buttonMapSize.Displayed = gameType != GameType.MultiplayerJoined && gameType != GameType.MultiplayerLoading;
                        buttonCreateServer.Displayed = false;
                    }

                    for (int i = 0; i < 4; ++i)
                    {
                        if (i > 0)
                            playerBoxes[i].ShowActivationButton = gameType != GameType.MultiplayerLoading;

                        playerBoxes[i].ShowCopyValueButton = gameType != GameType.MultiplayerLoading;
                    }
                    break;
                case GameType.Mission:
                    DrawBoxString(10, 2, textFieldHeader, "Start mission");
                    DrawBoxString(10, 18, textFieldName, "Mission:");
                    DrawBoxString(20, 18, textFieldValue, (gameMission + 1).ToString());
                    HideBoxString(textCreateServer);

                    buttonUp.Displayed = true;
                    buttonDown.Displayed = true;
                    buttonMapSize.Displayed = false;
                    buttonCreateServer.Displayed = false;

                    for (int i = 0; i < 4; ++i)
                    {
                        playerBoxes[i].ShowActivationButton = false;
                        playerBoxes[i].ShowCopyValueButton = false;
                    }
                    break;
                case GameType.Load:
                    {
                        DrawBoxString(10, 2, textFieldHeader, "Load game");
                        DrawBoxString(10, 18, textFieldName, "File:");

                        string path = fileList.GetSelected()?.Path;
                        string saveGameName = string.IsNullOrWhiteSpace(path) ? "" : System.IO.Path.GetFileNameWithoutExtension(path);

                        if (saveGameName.Length > 17)
                            saveGameName = saveGameName.Substring(0, 14) + "...";

                        DrawBoxString(16, 18, textFieldValue, saveGameName);
                        HideBoxString(textCreateServer);

                        buttonUp.Displayed = false;
                        buttonDown.Displayed = false;
                        buttonMapSize.Displayed = false;
                        buttonCreateServer.Displayed = false;

                        for (int i = 0; i < 4; ++i)
                        {
                            playerBoxes[i].ShowActivationButton = false;
                            playerBoxes[i].ShowCopyValueButton = false;
                        }
                    }
                    break;
            }

            // Game info 
            if (gameType != GameType.Load && gameType != GameType.MultiplayerClient)
            {
                int bx = 0;
                int by = 0;

                for (int i = 0; i < 4; ++i)
                {
                    if (i >= ServerGameInfo.PlayerCount)
                    {
                        playerBoxes[i].SetPlayerFace(-1);
                    }
                    else
                    {
                        var player = ServerGameInfo.GetPlayer((uint)i);

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

            // Display program name and version 
            DrawBoxString(2, 162, textFieldVersion, Global.VERSION);
        }

        internal void UpdateGameType(bool resetRandomGames = true)
        {
            if (buttonStart != null)
                buttonStart.Enabled = true;

            switch (gameType)
            {
                case GameType.Mission:
                    {
                        ServerGameInfo = GameInfo.GetMission((uint)gameMission);
                        randomInput.Displayed = false;
                        fileList.Displayed = false;
                        serverList.Displayed = false;
                        SetRedraw();
                        break;
                    }
                case GameType.Custom:
                    {
                        if (resetRandomGames)
                        {
                            customMission = new GameInfo(new Random(), false);
                            ServerGameInfo = customMission;
                            randomInput.SetRandom(customMission.RandomBase);
                        }
                        randomInput.Displayed = true;
                        fileList.Displayed = false;
                        serverList.Displayed = false;
                        SetRedraw();
                        break;
                    }
                case GameType.Load:
                    {
                        randomInput.Displayed = false;
                        fileList.Displayed = true;
                        fileList.Select(0);
                        serverList.Displayed = false;
                        SetRedraw();
                        break;
                    }
                case GameType.MultiplayerClient:
                    {
                        randomInput.Displayed = false;
                        fileList.Displayed = false;
                        serverList.Displayed = true;
                        serverList.Select(0);
                        SetRedraw();
                        return;
                    }
                case GameType.Tutorial:
                    {
                        // TODO
                        HandleAction(Action.ToggleGameType);
                        return;
                    }
                case GameType.AIvsAI:
                    {
                        if (resetRandomGames)
                        {
                            customMission = new GameInfo(new Random(), true);
                            ServerGameInfo = customMission;
                            randomInput.SetRandom(customMission.RandomBase);
                        }
                        randomInput.Displayed = true;
                        fileList.Displayed = false;
                        serverList.Displayed = false;
                        SetRedraw();
                        break;
                    }
            }
        }

        public void HandleAction(Action action)
        {
            switch (action)
            {
                case Action.CreateServer:
                    gameType = GameType.MultiplayerServer;
                    customMission = new GameInfo(new Random(), false);
                    ServerGameInfo = customMission;
                    ServerGameInfo.RemoveAllPlayers();
                    ServerGameInfo.AddPlayer(PlayerFace.You, PlayerInfo.PlayerColors[0], 40u, 40u, 40u);
                    randomInput.Displayed = true;
                    randomInput.SetRandom(customMission.RandomBase);
                    fileList.Displayed = false;
                    serverList.Displayed = false;
                    SetRedraw();
                    Server = Network.Network.DefaultServerFactory.CreateLocal("TestServer", customMission); // TODO: name should be editable
                    Server.Init(checkBoxSameValues.Checked, checkBoxServerValues.Checked, customMission.MapSize, randomInput.Text, ServerGameInfo.Players);
                    Server.ClientJoined += Server_ClientJoined;
                    Server.ClientLeft += Server_ClientLeft;
                    break;
                case Action.StartGame:
                    {
                        if (gameType == GameType.Load)
                        {
                            interf.CloseGameInit();

                            string path = fileList.GetSelected()?.Path;

                            if (string.IsNullOrWhiteSpace(path))
                            {
                                // TODO: message that no save game is selected/available?
                                return;
                            }

                            var viewer = interf.Viewer;

                            if (!GameManager.Instance.LoadGame(path, interf.RenderView, interf.AudioInterface, ref viewer))
                            {
                                interf.OpenGameInit(GameType.Load);

                                interf.OpenPopup(PopupBox.Type.DiskMsg);

                                return;
                            }
                            else
                            {
                                interf = viewer.MainInterface;
                                interf.OpenPopup(PopupBox.Type.DiskMsg);
                            }
                        }
                        else
                        {
                            if (gameType != GameType.MultiplayerClient &&
                                gameType != GameType.MultiplayerJoined &&
                                gameType != GameType.MultiplayerServer)
                                interf.CloseGameInit();

                            switch (gameType)
                            {
                                case GameType.Custom:
                                case GameType.Mission:
                                case GameType.Tutorial:
                                    // in GameInitBox the viewer is already a local player
                                    break;
                                case GameType.AIvsAI:
                                    // Note: Closing the current game first is important.
                                    // Otherwise the game change will close the current game later and
                                    // therefore change the spectator viewer back to player viewer
                                    // as this is the default viewer after game closing.
                                    GameManager.Instance.CloseGame();
                                    interf = interf.Viewer.ChangeTo(Viewer.Type.LocalSpectator).MainInterface;
                                    break;
                                case GameType.MultiplayerClient:
                                    {
                                        if (Client == null)
                                        {
                                            Client = Network.Network.DefaultClientFactory.CreateLocal();
                                            Client.Disconnected += Client_Disconnected;
                                            Client.LobbyDataUpdated += Client_LobbyDataUpdated;
                                            Client.GameStarted += Client_GameStarted;
                                        }

                                        lock (Client)
                                        {
                                            // TODO for now we always use localhost as server
                                            if (!Client.JoinServer("TODO", System.Net.IPAddress.Loopback))
                                            {
                                                // TODO error
                                                return;
                                            }

                                            gameType = GameType.MultiplayerJoined;

                                            customMission = new GameInfo(new Random(), false);
                                            ServerGameInfo = customMission;
                                            ServerGameInfo.RemoveAllPlayers();
                                            randomInput.Displayed = true;
                                            randomInput.Enabled = false;
                                            randomInput.SetRandom(customMission.RandomBase);
                                            fileList.Displayed = false;
                                            serverList.Displayed = false;
                                            buttonStart.Enabled = false;
                                            SetRedraw();
                                        }

                                        break;
                                    }
                            }

                            if (gameType == GameType.MultiplayerServer)
                            {
                                GameManager.Instance.CloseGame();
                                interf = interf.Viewer.ChangeTo(Viewer.Type.Server).MainInterface;

                                playerStatus.Clear();
                                playerStatus.Add(0u, MultiplayerStatus.Ready); // server
                                foreach (var client in playerClientMapping)
                                {
                                    playerStatus.Add(client.Key, MultiplayerStatus.Unknown); // clients
                                }

                                gameType = GameType.MultiplayerLoading;

                                var game = GameManager.Instance.PrepareMultiplayerGame(ServerGameInfo, interf.RenderView, interf.AudioInterface);

                                if (game == null)
                                {
                                    // TODO: notify clients before closing the server?
                                    Server.Close();
                                    gameType = GameType.MultiplayerClient;
                                    UpdateGameType();
                                    return;
                                }

                                SetRedraw();

                                Server.GameReady += (bool ready) =>
                                {
                                    if (ready)
                                    {
                                        GameManager.Instance.StartMultiplayerGame(game);
                                        Server.ResumeGame();
                                        Server.AllowUserInput(true);
                                        interf.CloseGameInit();
                                        game.Resume();
                                        return true;
                                    }

                                    return false;
                                };
                                Server.StartGame(game);
                            }
                            else
                            {
                                if (!GameManager.Instance.StartGame(ServerGameInfo, interf.RenderView, interf.AudioInterface))
                                {
                                    return;
                                }
                            }
                        }

                        break;
                    }
                case Action.ToggleGameType:
                    if (Server != null)
                    {
                        Server.Close();
                        Server = null;
                    }

                    // TODO: Remove for multiplayer support (still WIP!)
                    if (gameType == GameType.MultiplayerClient - 1)
                    {
                        gameType += 2;
                        UpdateGameType();
                        break;
                    }

                    if (gameType == GameType.MultiplayerServer ||
                        gameType == GameType.MultiplayerJoined ||
                        gameType == GameType.MultiplayerLoading)
                    {
                        randomInput.Enabled = true; // was disabled in MultiplayerJoined
                        buttonStart.Enabled = true; // was disabled in MultiplayerJoined
                        gameType = GameType.MultiplayerClient;
                    }

                    if (++gameType > GameType.AIvsAI)
                    {
                        gameType = GameType.Custom;
                    }

                    UpdateGameType();
                    break;
                case Action.ShowOptions:
                    interf.OpenPopup(PopupBox.Type.GameInitOptions);
                    break;
                case Action.Increment:
                    switch (gameType)
                    {
                        case GameType.Mission:
                            gameMission = Math.Min(gameMission + 1, (int)GameInfo.GetMissionCount() - 1);
                            ServerGameInfo = GameInfo.GetMission((uint)gameMission);
                            SetRedraw();
                            break;
                        case GameType.Custom:
                        case GameType.AIvsAI:
                        case GameType.MultiplayerServer:
                            if (customMission.MapSize == 9u)
                                return;
                            customMission.MapSize = customMission.MapSize + 1u;
                            SetRedraw();
                            if (gameType == GameType.MultiplayerServer)
                                ServerUpdate();
                            break;
                    }
                    break;
                case Action.Decrement:
                    switch (gameType)
                    {
                        case GameType.Mission:
                            gameMission = Math.Max(0, gameMission - 1);
                            ServerGameInfo = GameInfo.GetMission((uint)gameMission);
                            SetRedraw();
                            break;
                        case GameType.Custom:
                        case GameType.AIvsAI:
                        case GameType.MultiplayerServer:
                            if (customMission.MapSize == 3u)
                                return;
                            customMission.MapSize = customMission.MapSize - 1u;
                            SetRedraw();
                            if (gameType == GameType.MultiplayerServer)
                                ServerUpdate();
                            break;
                    }
                    break;
                case Action.IncrementLarge:
                    switch (gameType)
                    {
                        case GameType.Mission:
                            gameMission = Math.Min(gameMission + 5, (int)GameInfo.GetMissionCount() - 1);
                            ServerGameInfo = GameInfo.GetMission((uint)gameMission);
                            SetRedraw();
                            break;
                        case GameType.Custom:
                        case GameType.AIvsAI:
                        case GameType.MultiplayerServer:
                            if (customMission.MapSize == 9u)
                                return;
                            customMission.MapSize = Math.Min(9u, customMission.MapSize + 2u);
                            SetRedraw();
                            if (gameType == GameType.MultiplayerServer)
                                ServerUpdate();
                            break;
                    }
                    break;
                case Action.DecrementLarge:
                    switch (gameType)
                    {
                        case GameType.Mission:
                            gameMission = Math.Max(0, gameMission - 5);
                            ServerGameInfo = GameInfo.GetMission((uint)gameMission);
                            SetRedraw();
                            break;
                        case GameType.Custom:
                        case GameType.AIvsAI:
                        case GameType.MultiplayerServer:
                            if (customMission.MapSize == 3u)
                                return;
                            customMission.MapSize = Math.Max(3u, customMission.MapSize - 2u);
                            SetRedraw();
                            if (gameType == GameType.MultiplayerServer)
                                ServerUpdate();
                            break;
                    }
                    break;
                case Action.Close:
                    if (gameType == GameType.MultiplayerServer ||
                        gameType == GameType.MultiplayerJoined ||
                        gameType == GameType.MultiplayerLoading)
                    {
                        randomInput.Enabled = true; // was disabled in MultiplayerJoined
                        buttonStart.Enabled = true; // was disabled in MultiplayerJoined
                        if (gameType == GameType.MultiplayerServer)
                            Server.Close();
                        else
                            Client.Disconnect();
                        gameType = GameType.MultiplayerClient;
                        UpdateGameType();
                        break;
                    }
                    else
                    {
                        interf.CloseGameInit();
                        interf.RenderView.Close();
                    }
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
                            customMission.SetRandomBase(randomInput.GetRandom(), gameType == GameType.AIvsAI);

                            // in a multiplayer game this will only affect the map, not the players
                            if (gameType == GameType.MultiplayerServer)
                            {
                                var players = new List<PlayerInfo>(ServerGameInfo.Players);
                                ServerGameInfo = customMission;
                                ServerGameInfo.RemoveAllPlayers();

                                foreach (var player in players)
                                {
                                    ServerGameInfo.AddPlayer(player);
                                }

                                randomInput.SetRandom(customMission.RandomBase);
                                SetRedraw();

                                ServerUpdate();
                            }
                            else
                            {
                                ServerGameInfo = customMission;
                            }

                            SetRedraw();
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        private void Client_GameStarted(object sender, System.EventArgs e)
        {
            // TODO: client or remote spectator depending on settings
            if (Client != null)
            {
                Client.Disconnected -= Client_Disconnected;
                Client.LobbyDataUpdated -= Client_LobbyDataUpdated;
                Client.GamePaused += Client_GamePaused;
                Client.GameResumed += Client_GameResumed;
                Client.InputAllowed += Client_InputAllowed;
                Client.InputDisallowed += Client_InputDisallowed;
            }

            GameManager.Instance.CloseGame();
            interf = interf.Viewer.ChangeTo(Viewer.Type.Client).MainInterface;
            gameType = GameType.MultiplayerLoading;
            // TODO: use game info from lobby/server here
            Client.Game = GameManager.Instance.PrepareMultiplayerGame(ServerGameInfo, interf.RenderView, interf.AudioInterface);
            SetRedraw();
        }

        private void Client_InputDisallowed(object sender, System.EventArgs e)
        {
            // TODO: at least the client should be able to leave the game
            interf.Enabled = false;
        }

        private void Client_InputAllowed(object sender, System.EventArgs e)
        {
            interf.Enabled = true;
        }

        private void Client_GameResumed(object sender, System.EventArgs e)
        {
            // If the game hasn't started, start it now
            if (interf.Game == null || interf.Game.GameTime == 0u)
                GameManager.Instance.StartMultiplayerGame(Client.Game);
            Client.Game.Resume();
        }

        private void Client_GamePaused(object sender, System.EventArgs e)
        {
            Client.Game.Pause();
        }

        private void Server_ClientLeft(Network.ILocalServer server, Network.IRemoteClient client)
        {
            lock (ServerGameInfo)
            {
                ServerGameInfo.RemovePlayer(client.PlayerIndex);
                playerClientMapping.Remove(client.PlayerIndex);
                SetRedraw();
            }

            ServerUpdate();
        }

        private void Server_ClientJoined(Network.ILocalServer server, Network.IRemoteClient client)
        {
            lock (ServerGameInfo)
            {
                while (ServerGameInfo.PlayerCount >= 4)
                {
                    // joined too late -> kick player
                    server.DisconnectClient(client);
                }

                uint supplies = checkBoxSameValues.Checked ? ServerGameInfo.GetPlayer(0u).Supplies : 20u;
                uint reproduction = checkBoxSameValues.Checked ? ServerGameInfo.GetPlayer(0u).Reproduction : 20u;
                uint playerIndex = ServerGameInfo.PlayerCount;

                // TODO: every face < PlayerFace.You is treated as AI so we only can have 2 human players (= 1 client) for now
                //       we should add two more human player faces later
                var playerInfo = new PlayerInfo(PlayerFace.Friend, PlayerInfo.PlayerColors[playerIndex], 40u, supplies, reproduction);

                ServerGameInfo.AddPlayer(playerInfo);
                playerIsAI[playerIndex] = false;
                playerClientMapping.Add(playerIndex, client);
                SetRedraw();
            }

            ServerUpdate();
        }

        private void ServerUpdate()
        {
            Server.Update(
                checkBoxSameValues.Checked,
                checkBoxServerValues.Checked,
                customMission.MapSize,
                randomInput.Text,
                ServerGameInfo.Players
            );
        }

        private void Client_Disconnected(object sender, System.EventArgs e)
        {
            if (gameType == GameType.MultiplayerJoined)
            {
                gameType = GameType.MultiplayerClient;
                UpdateGameType();
            }
        }

        private void Client_LobbyDataUpdated(object sender, System.EventArgs e)
        {
            lock (Client)
            {
                var serverInfo = Client.LobbyData.ServerInfo;
                var players = Client.LobbyData.Players;

                customMission = new GameInfo(new Random(serverInfo.MapSeed), false);
                customMission.MapSize = serverInfo.MapSize;
                ServerGameInfo = customMission;
                ServerGameInfo.RemoveAllPlayers();

                for (int i = 0; i < players.Count; ++i)
                {
                    ServerGameInfo.AddPlayer((PlayerFace)players[i].Face, PlayerInfo.PlayerColors[i],
                        (uint)players[i].Intelligence, (uint)players[i].Supplies, (uint)players[i].Reproduction);
                }

                randomInput.SetRandom(customMission.RandomBase);

                SetRedraw();
            }
        }

        public override bool HandleEvent(EventArgs e)
        {
            if (interf.PopupBox != null && interf.PopupBox.Displayed)
                return interf.PopupBox.HandleEvent(e);

            return base.HandleEvent(e);
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

        protected override bool HandleSystemKeyPressed(SystemKey key, int modifier)
        {
            if (key == SystemKey.PageUp) // page up
            {
                HandleAction(Action.IncrementLarge);
            }
            else if (key == SystemKey.PageDown) // page down
            {
                HandleAction(Action.DecrementLarge);
            }
            else if (key == SystemKey.Up) // up
            {
                HandleAction(Action.Increment);
            }
            else if (key == SystemKey.Down) // down
            {
                HandleAction(Action.Decrement);
            }

            return base.HandleSystemKeyPressed(key, modifier);
        }

        bool PlayerFaceAlreadyTaken(uint playerIndex, PlayerFace face)
        {
            for (uint i = 0; i < playerIndex; ++i)
            {
                if (ServerGameInfo.GetPlayer(i).Face == face)
                    return true;
            }

            return false;
        }

        bool HandlePlayerClick(uint playerIndex, int cx, int cy)
        {
            if (cx < 8 || cx > 8 + 64 || cy < 8 || cy > 76)
            {
                return false;
            }

            if (cx >= 8 + 32 + 8 && cx < 8 + 32 + 8 + 24 && cy >= 8 && cy < 24) // click on activation button
            {
                // players can only be removed or added in custom games, AI vs AI and in multiplayer
                if (gameType == GameType.Custom || gameType == GameType.AIvsAI || gameType == GameType.MultiplayerServer)
                {
                    if (playerIndex > 0) // at least one player must be active
                    {
                        if (playerIndex >= ServerGameInfo.PlayerCount) // add player
                        {
                            playerIndex = ServerGameInfo.PlayerCount;
                            PlayerInfo playerInfo;

                            do
                            {
                                playerInfo = new PlayerInfo(new Random());
                            } while (PlayerFaceAlreadyTaken(playerIndex, playerInfo.Face));

                            playerInfo.Color = PlayerInfo.PlayerColors[playerIndex];

                            if (checkBoxSameValues.Checked)
                            {
                                playerInfo.Supplies = ServerGameInfo.GetPlayer(0).Supplies;
                                playerInfo.Reproduction = ServerGameInfo.GetPlayer(0).Reproduction;
                            }

                            ServerGameInfo.AddPlayer(playerInfo);
                            playerIsAI[playerIndex] = true; // manually added players are always AI players
                            SetRedraw();
                        }
                        else // remove
                        {
                            if (gameType == GameType.MultiplayerServer && !playerIsAI[playerIndex])
                            {
                                // kick player
                                Server.DisconnectClient(playerClientMapping[playerIndex]);
                            }

                            ServerGameInfo.RemovePlayer(playerIndex);
                            playerIsAI[playerIndex] = false;
                            SetRedraw();
                        }

                        if (gameType == GameType.MultiplayerServer)
                            ServerUpdate();
                    }
                }

                return true;
            }

            if (playerIndex >= ServerGameInfo.PlayerCount)
            {
                return true;
            }

            var player = ServerGameInfo.GetPlayer(playerIndex);

            if (cx < 8 + 32 && cy < 72) // click on face
            {
                bool canNotChange = (playerIndex == 0 && gameType != GameType.AIvsAI) ||
                                    gameType == GameType.MultiplayerJoined || // TODO: maybe later choose between some special faces
                                    gameType == GameType.Mission ||
                                    gameType == GameType.Tutorial ||
                                    gameType == GameType.Load;

                if (!canNotChange)
                {
                    // Face 
                    bool inUse;

                    do
                    {
                        uint next = ((uint)player.Face + 1) % 11; // Note: Use 12 here to also allow the last enemy as a custom game player
                        next = Math.Max(1u, next);

                        player.SetCharacter((PlayerFace)next);

                        // Check that face is not already in use by another player 
                        inUse = false;

                        for (uint i = 0; i < ServerGameInfo.PlayerCount; ++i)
                        {
                            if (playerIndex != i &&
                                ServerGameInfo.GetPlayer(i).Face == (PlayerFace)next)
                            {
                                inUse = true;
                                break;
                            }
                        }

                    } while (inUse);

                    if (gameType == GameType.MultiplayerServer)
                        ServerUpdate();
                }
            }
            else if (cx >= 8 + 32 && cx < 8 + 32 + 8 && cy >= 8 && cy < 24) // click on copy values button
            {
                // values can only copied in custom games and AI vs AI
                // and to AI enemies in multiplayer
                if (gameType == GameType.Custom || gameType == GameType.AIvsAI || gameType == GameType.MultiplayerServer)
                {
                    for (uint i = 0; i < ServerGameInfo.PlayerCount; ++i)
                    {
                        if (i != playerIndex)
                        {
                            var otherPlayer = ServerGameInfo.GetPlayer(i);
                            otherPlayer.Supplies = player.Supplies;
                            otherPlayer.Intelligence = player.Intelligence;
                            otherPlayer.Reproduction = player.Reproduction;
                        }
                    }

                    SetRedraw();

                    if (gameType == GameType.MultiplayerServer)
                        ServerUpdate();
                }

                return true;
            }
            else // click on values
            {
                cx -= 8 + 32 + 8 + 3;

                if (cx < 0)
                {
                    return false;
                }

                if (cy >= 25 && cy < 72)
                {
                    uint value = (uint)Misc.Clamp(0, 67 - cy, 40);

                    if (cx >= 0 && cx < 6)
                    {
                        bool canNotChange = gameType == GameType.Mission ||
                                            gameType == GameType.Tutorial ||
                                            gameType == GameType.Load;

                        if (gameType == GameType.MultiplayerServer && playerIndex != 0 && !checkBoxServerValues.Checked && !checkBoxSameValues.Checked && !playerIsAI[playerIndex])
                        {
                            canNotChange = true;
                        }
                        else if (gameType == GameType.MultiplayerJoined)
                        {
                            canNotChange =
                                playerIndex != Client.PlayerIndex ||
                                Client.LobbyData.ServerInfo.UseSameValues ||
                                Client.LobbyData.ServerInfo.UseServerValues;
                        }

                        // Supplies 
                        if (!canNotChange)
                        {
                            if (checkBoxSameValues.Checked)
                            {
                                for (uint i = 0; i < ServerGameInfo.PlayerCount; ++i)
                                {
                                    ServerGameInfo.GetPlayer(i).Supplies = value;
                                }
                            }
                            else
                            {
                                player.Supplies = value;
                            }

                            if (gameType == GameType.MultiplayerServer)
                                ServerUpdate();
                        }
                    }
                    else if (cx >= 6 && cx < 12)
                    {
                        bool canNotChange = (playerIndex == 0 && gameType != GameType.AIvsAI) ||
                                            gameType == GameType.MultiplayerJoined ||
                                            gameType == GameType.Mission ||
                                            gameType == GameType.Tutorial ||
                                            gameType == GameType.Load;

                        if (gameType == GameType.MultiplayerServer)
                        {
                            if (!playerIsAI[playerIndex])
                                canNotChange = true;
                        }

                        // Intelligence 
                        if (!canNotChange)
                        {
                            player.Intelligence = value;

                            if (gameType == GameType.MultiplayerServer)
                                ServerUpdate();
                        }
                    }
                    else if (cx >= 12 && cx < 18)
                    {
                        bool canNotChange = gameType == GameType.Mission ||
                                            gameType == GameType.Tutorial ||
                                            gameType == GameType.Load;

                        if (gameType == GameType.MultiplayerServer && playerIndex != 0 && !checkBoxServerValues.Checked && !checkBoxSameValues.Checked && !playerIsAI[playerIndex])
                        {
                            canNotChange = true;
                        }
                        else if (gameType == GameType.MultiplayerJoined)
                        {
                            canNotChange =
                                playerIndex != Client.PlayerIndex ||
                                Client.LobbyData.ServerInfo.UseSameValues ||
                                Client.LobbyData.ServerInfo.UseServerValues;
                        }

                        // Reproduction 
                        if (!canNotChange)
                        {
                            if (checkBoxSameValues.Checked)
                            {
                                for (uint i = 0; i < ServerGameInfo.PlayerCount; ++i)
                                {
                                    ServerGameInfo.GetPlayer(i).Reproduction = value;
                                }
                            }
                            else
                            {
                                player.Reproduction = value;
                            }

                            if (gameType == GameType.MultiplayerServer)
                                ServerUpdate();
                        }
                    }
                }
            }

            SetRedraw();

            return true;
        }
    }
}
