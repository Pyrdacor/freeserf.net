/*
 * Mission.cs - Predefined game mission maps
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
using System.Linq;
using Freeserf.Localization;

namespace Freeserf
{
    using MapPos = UInt32;

    internal class Character
    {
        public Character(PlayerFace face, Func<string> name, Func<string> characterization)
        {
            Face = face;
            getName = name;
            getCharacterization = characterization;
        }

        public PlayerFace Face;
        readonly Func<string> getName;
        readonly Func<string> getCharacterization;
        public string Name => getName();
        public string Characterization => getCharacterization();
    }

    public class PlayerInfo
    {
        internal static readonly Character[] Characters =
        [
            new Character(PlayerFace.None, () => Strings.Character_None_Name, () => Strings.Character_None_Description),
            new Character(PlayerFace.LadyAmalie, () => Strings.Character_LadyAmalie_Name, () => Strings.Character_LadyAmalie_Description),
            new Character(PlayerFace.KumpyOnefinger, () => Strings.Character_KumpyOnefinger_Name, () => Strings.Character_KumpyOnefinger_Description),
            new Character(PlayerFace.Balduin, () => Strings.Character_Balduin_Name, () => Strings.Character_Balduin_Description),
            new Character(PlayerFace.Frollin, () => Strings.Character_Frollin_Name, () => Strings.Character_Frollin_Description),
            new Character(PlayerFace.Kallina, () => Strings.Character_Kallina_Name, () => Strings.Character_Kallina_Description),
            new Character(PlayerFace.Rasparuk, () => Strings.Character_Rasparuk_Name, () => Strings.Character_Rasparuk_Description),
            new Character(PlayerFace.CountAldaba, () => Strings.Character_CountAldaba_Name, () => Strings.Character_CountAldaba_Description),
            new Character(PlayerFace.KingRolph, () => Strings.Character_KingRolph_Name, () => Strings.Character_KingRolph_Description),
            new Character(PlayerFace.HomenDoublehorn, () => Strings.Character_HomenDoublehorn_Name, () => Strings.Character_HomenDoublehorn_Description),
            new Character(PlayerFace.Sollok, () => Strings.Character_Sollok_Name, () => Strings.Character_Sollok_Description),
            new Character(PlayerFace.Enemy, () => Strings.Character_Enemy_Name, () => Strings.Character_Enemy_Description),
            new Character(PlayerFace.You, () => Strings.Character_You_Name, () => Strings.Character_You_Description),
            new Character(PlayerFace.Friend, () => Strings.Character_Friend_Name, () => Strings.Character_Friend_Description)
        ];

        internal static readonly Color[] PlayerColors =
        [
            new() {Red = 0x00, Green = 0xe3, Blue = 0xe3}, // Blue
            new() {Red = 0xcf, Green = 0x63, Blue = 0x63}, // Red
            new() {Red = 0xdf, Green = 0x7f, Blue = 0xef}, // Magenta
            new() {Red = 0xef, Green = 0xef, Blue = 0x8f}  // Yellow
        ];

        public static PlayerInfo Create(uint playerIndex, PlayerFace character, uint intelligence, uint supplies, uint reproduction)
        {
            int colorIndex = character.GetColorIndex();

            if (colorIndex == -1)
                colorIndex = (int)playerIndex;

            return new PlayerInfo(character, PlayerColors[colorIndex], intelligence, supplies, reproduction);
        }

        internal struct Position
        {
            public Position(int column, int row)
            {
                Column = column;
                Row = row;
            }

            public int Column;
            public int Row;

            public static readonly Position None = new Position() { Column = -1, Row = -1 };
        }

        internal struct Preset
        {
            public Preset(uint character, uint intelligence, uint supplies,
                uint reproduction, Position castlePosition)
            {
                Character = Characters[character];
                Intelligence = intelligence;
                Supplies = supplies;
                Reproduction = reproduction;
                Castle = castlePosition;
            }

            public Character Character;
            public uint Intelligence;
            public uint Supplies;
            public uint Reproduction;
            public Position Castle;
        }

        public uint Intelligence { get; set; }
        public uint Supplies { get; set; }
        public uint Reproduction { get; set; }
        public PlayerFace Face { get; private set; }
        internal Color Color { get; set; }
        internal Position CastlePosition { get; set; }

        internal PlayerInfo(Random random)
        {
            var character = (((random.Next() * 10u) >> 16) + 1u) & 0xFFu;

            SetCharacter((PlayerFace)character);

            Intelligence = ((random.Next() * 41u) >> 16) & 0xFFu;
            Supplies = ((random.Next() * 41u) >> 16) & 0xFFu;
            Reproduction = ((random.Next() * 41u) >> 16) & 0xFFu;
            CastlePosition = Position.None;
        }

        internal PlayerInfo(PlayerFace character, Color color,
             uint intelligence, uint supplies, uint reproduction)
        {
            SetCharacter(character);

            Color = color;
            Intelligence = intelligence;
            Supplies = supplies;
            Reproduction = reproduction;
            CastlePosition = Position.None;
        }

        public void SetCharacter(PlayerFace character)
        {
            Face = character;
        }

        public bool HasCastle => CastlePosition.Column >= 0;
    }

    public class GameInfo
    {
        internal struct Mission
        {
            internal Mission(Func<string> name, Random random, params PlayerInfo.Preset[] players)
            {
                Name = name;
                Random = random;
                Players = players;
            }

            public Func<string> Name;
            public bool IsIntro;
            public Random Random;
            public PlayerInfo.Preset[] Players;
        }

        static readonly Mission IntroMission = new Mission
        (
            () => Strings.Mission_Intro,
            new Random(),
            new PlayerInfo.Preset(1, 0, 10, 5, PlayerInfo.Position.None),
            new PlayerInfo.Preset(2, 0, 10, 5, PlayerInfo.Position.None),
            new PlayerInfo.Preset(3, 0, 10, 5, PlayerInfo.Position.None),
            new PlayerInfo.Preset(4, 0, 10, 5, PlayerInfo.Position.None)
        ) { IsIntro = true };

        static readonly Mission[] missions = new Mission[]
        {
            new Mission(
                () => Strings.Mission_Start,
                new Random("8667715887436237"),
                new PlayerInfo.Preset(12, 40, 35, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(1, 10, 5, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                () => Strings.Mission_Station,
                new Random("2831713285431227"),
                new PlayerInfo.Preset(12, 40, 30, 40, PlayerInfo.Position.None),
                new PlayerInfo.Preset(2, 12, 15, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(3, 14, 15, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                () => Strings.Mission_Unity,
                new Random("4632253338621228"),
                new PlayerInfo.Preset(12, 40, 30, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(2, 18, 10, 25, PlayerInfo.Position.None),
                new PlayerInfo.Preset(4, 18, 10, 25, PlayerInfo.Position.None)
            ),
            new Mission(
                () => Strings.Mission_Wave,
                new Random("8447342476811762"),
                new PlayerInfo.Preset(12, 40, 25, 40, PlayerInfo.Position.None),
                new PlayerInfo.Preset(2, 16, 20, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                () => Strings.Mission_Export,
                new Random("4276472414845177"),
                new PlayerInfo.Preset(12, 40, 30, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(3, 16, 25, 20, PlayerInfo.Position.None),
                new PlayerInfo.Preset(4, 16, 25, 20, PlayerInfo.Position.None)
            ),
            new Mission(
                () => Strings.Mission_Option,
                new Random("2333577877517478"),
                new PlayerInfo.Preset(12, 40, 30, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(3, 20, 12, 14, PlayerInfo.Position.None),
                new PlayerInfo.Preset(5, 20, 12, 14, PlayerInfo.Position.None)
            ),
            new Mission(
                () => Strings.Mission_Record,
                new Random("1416541231242884"),
                new PlayerInfo.Preset(12, 40, 30, 40, PlayerInfo.Position.None),
                new PlayerInfo.Preset(3, 22, 30, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                () => Strings.Mission_Scale,
                new Random("7845187715476348"),
                new PlayerInfo.Preset(12, 40, 25, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(4, 23, 25, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(6, 24, 25, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                () => Strings.Mission_Sign,
                new Random("5185768873118642"),
                new PlayerInfo.Preset(12, 40, 25, 40, PlayerInfo.Position.None),
                new PlayerInfo.Preset(4, 26, 13, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(5, 28, 13, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(6, 30, 13, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                () => Strings.Mission_Acorn,
                new Random("3183215728814883"),
                new PlayerInfo.Preset(12, 40, 20, 16, new PlayerInfo.Position(28, 14)),
                new PlayerInfo.Preset(4, 30, 19, 20, new PlayerInfo.Position(5, 47))
            ),
            new Mission(
                () => Strings.Mission_Chopper,
                new Random("4376241846215474"),
                new PlayerInfo.Preset(12, 40, 16, 20, new PlayerInfo.Position(16, 42)),
                new PlayerInfo.Preset(5, 33, 10, 20, new PlayerInfo.Position(52, 25)),
                new PlayerInfo.Preset(7, 34, 13, 20, new PlayerInfo.Position(23, 12))
            ),
            new Mission(
                () => Strings.Mission_Gate,
                new Random("6371557668231277"),
                new PlayerInfo.Preset(12, 40, 23, 27, new PlayerInfo.Position(53, 13)),
                new PlayerInfo.Preset(5, 27, 17, 24, new PlayerInfo.Position(27, 10)),
                new PlayerInfo.Preset(6, 27, 13, 24, new PlayerInfo.Position(29, 38)),
                new PlayerInfo.Preset(7, 27, 13, 24, new PlayerInfo.Position(15, 32))
            ),
            new Mission(
                () => Strings.Mission_Island,
                new Random("8473352672411117"),
                new PlayerInfo.Preset(12, 40, 24, 20, new PlayerInfo.Position(7, 26)),
                new PlayerInfo.Preset(5, 20, 30, 20, new PlayerInfo.Position(2, 10))
            ),
            new Mission(
                () => Strings.Mission_Legion,
                new Random("1167854231884464"),
                new PlayerInfo.Preset(12, 40, 20, 23, new PlayerInfo.Position(19,  3)),
                new PlayerInfo.Preset(6, 28, 16, 20, new PlayerInfo.Position(55,  7)),
                new PlayerInfo.Preset(8, 28, 16, 20, new PlayerInfo.Position(55, 46))
            ),
            new Mission(
                () => Strings.Mission_Piece,
                new Random("2571462671725414"),
                new PlayerInfo.Preset(12, 40, 20, 17, new PlayerInfo.Position(41, 5)),
                new PlayerInfo.Preset(6, 40, 23, 20, new PlayerInfo.Position(19, 49)),
                new PlayerInfo.Preset(7, 37, 20, 20, new PlayerInfo.Position(58, 52)),
                new PlayerInfo.Preset(8, 40, 15, 15, new PlayerInfo.Position(43, 31))
            ),
            new Mission(
                () => Strings.Mission_Rival,
                new Random("4563653871271587"),
                new PlayerInfo.Preset(12, 40, 26, 23, new PlayerInfo.Position(36, 63)),
                new PlayerInfo.Preset(6, 28, 29, 40, new PlayerInfo.Position(14, 15))
            ),
            new Mission(
                () => Strings.Mission_Savage,
                new Random("7212145428156114"),
                new PlayerInfo.Preset(12, 40, 25, 12, new PlayerInfo.Position(63, 59)),
                new PlayerInfo.Preset(7, 29, 17, 10, new PlayerInfo.Position(29, 24)),
                new PlayerInfo.Preset(8, 29, 17, 10, new PlayerInfo.Position(39, 26)),
                new PlayerInfo.Preset(9, 32, 17, 10, new PlayerInfo.Position(42, 49))
            ),
            new Mission(
                () => Strings.Mission_Xaver,
                new Random("4276472414435177"),
                new PlayerInfo.Preset(12, 40, 25, 40, new PlayerInfo.Position(15, 0)),
                new PlayerInfo.Preset(7, 40, 30, 35, new PlayerInfo.Position(34, 48)),
                new PlayerInfo.Preset(9, 30, 30, 35, new PlayerInfo.Position(58, 5))
            ),
            new Mission(
                () => Strings.Mission_Blade,
                new Random("7142748441424786"),
                new PlayerInfo.Preset(12, 40, 30, 20, new PlayerInfo.Position(13, 37)),
                new PlayerInfo.Preset(7, 40, 20, 20, new PlayerInfo.Position(32, 34))
            ),
            new Mission(
                () => Strings.Mission_Beacon,
                new Random("6882188351133886"),
                new PlayerInfo.Preset(12, 40,  9, 10, new PlayerInfo.Position(14, 42)),
                new PlayerInfo.Preset(8, 40, 16, 22, new PlayerInfo.Position(62, 1)),
                new PlayerInfo.Preset(9, 40, 16, 23, new PlayerInfo.Position(32, 14))
            ),
            new Mission(
                () => Strings.Mission_Pasture,
                new Random("7742136435163436"),
                new PlayerInfo.Preset(12, 40, 20, 11, new PlayerInfo.Position(38, 17)),
                new PlayerInfo.Preset(8, 30, 22, 13, new PlayerInfo.Position(32, 51)),
                new PlayerInfo.Preset(9, 30, 23, 13, new PlayerInfo.Position(1, 50)),
                new PlayerInfo.Preset(10, 30, 21, 13, new PlayerInfo.Position(4, 9))
            ),
            new Mission(
                () => Strings.Mission_Omnus,
                new Random("6764387728224725"),
                new PlayerInfo.Preset(12, 40, 20, 40, new PlayerInfo.Position(42, 20)),
                new PlayerInfo.Preset(8, 36, 25, 40, new PlayerInfo.Position(48, 47))
            ),
            new Mission(
                () => Strings.Mission_Tribute,
                new Random("5848744734731253"),
                new PlayerInfo.Preset(12, 40,  5, 11, new PlayerInfo.Position(53, 1)),
                new PlayerInfo.Preset(9, 35, 30, 10, new PlayerInfo.Position(20, 2)),
                new PlayerInfo.Preset(10, 37, 30, 10, new PlayerInfo.Position(16, 55))
            ),
            new Mission(
                () => Strings.Mission_Fountain,
                new Random("6183541838474434"),
                new PlayerInfo.Preset(12, 40, 20, 12, new PlayerInfo.Position(3, 34)),
                new PlayerInfo.Preset(9, 30, 25, 10, new PlayerInfo.Position(47, 41)),
                new PlayerInfo.Preset(10, 30, 26, 10, new PlayerInfo.Position(42, 52))
            ),
            new Mission(
                () => Strings.Mission_Chude,
                new Random("7633126817245833"),
                new PlayerInfo.Preset(12, 40, 20, 40, new PlayerInfo.Position(23, 38)),
                new PlayerInfo.Preset(9, 40, 25, 40, new PlayerInfo.Position(57, 52))
            ),
            new Mission(
                () => Strings.Mission_Trailer,
                new Random("5554144773646312"),
                new PlayerInfo.Preset(12, 40, 20, 30, new PlayerInfo.Position(29, 11)),
                new PlayerInfo.Preset(10, 38, 30, 35, new PlayerInfo.Position(15, 40))
            ),
            new Mission(
                () => Strings.Mission_Canyon,
                new Random("3122431112682557"),
                new PlayerInfo.Preset(12, 40, 18, 28, new PlayerInfo.Position(49, 53)),
                new PlayerInfo.Preset(10, 39, 25, 40, new PlayerInfo.Position(14, 53))
            ),
            new Mission(
                () => Strings.Mission_Repress,
                new Random("2568412624848266"),
                new PlayerInfo.Preset(12, 40, 20, 40, new PlayerInfo.Position(44, 39)),
                new PlayerInfo.Preset(10, 39, 25, 40, new PlayerInfo.Position(44, 63))
            ),
            new Mission(
                () => Strings.Mission_Yoki,
                new Random("3736685353284538"),
                new PlayerInfo.Preset(12, 40,  5, 22, new PlayerInfo.Position(53, 8)),
                new PlayerInfo.Preset(11, 40, 15, 20, new PlayerInfo.Position(30, 22))
            ),
            new Mission(
                () => Strings.Mission_Passive,
                new Random("5471458635555317"),
                new PlayerInfo.Preset(12, 40,  5, 20, new PlayerInfo.Position(25, 46)),
                new PlayerInfo.Preset(11, 40, 20, 20, new PlayerInfo.Position(51, 42))
            ),
            new Mission(
                () => Strings.Mission_Pyrdacor,
                new Random("5079726461636072"),
                new PlayerInfo.Preset(12, 40,  0, 30, new PlayerInfo.Position(54, 63)),
                new PlayerInfo.Preset(11, 40, 18, 16, new PlayerInfo.Position(55, 25))
            )
        };

        /* Tutorials 1-6 (Amiga mission_table entries 0..5): one human
           player, castle placed by the player. */
        static readonly Mission[] tutorials = new Mission[]
        {
            new Mission(
                () => Strings.Mission_Tutorial1,
                new Random("3762665523225478"),
                new PlayerInfo.Preset(12, 40, 30, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                () => Strings.Mission_Tutorial2,
                new Random("1616118277634871"),
                new PlayerInfo.Preset(12, 40, 30, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                () => Strings.Mission_Tutorial3,
                new Random("2418554743842118"),
                new PlayerInfo.Preset(12, 40, 30, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                () => Strings.Mission_Tutorial4,
                new Random("4454333314864658"),
                new PlayerInfo.Preset(12, 40, 30, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                () => Strings.Mission_Tutorial5,
                new Random("4264118313621432"),
                new PlayerInfo.Preset(12, 40, 30, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                () => Strings.Mission_Tutorial6,
                new Random("2487251185361288"),
                new PlayerInfo.Preset(12, 40, 30, 30, PlayerInfo.Position.None)
            )
        };

        public uint MapSize { get; set; }

        public string Seed => RandomBase.ToString();

        internal Random RandomBase { get; private set; }

        public uint PlayerCount => (uint)players.Count;
        public uint MultiplayerPlayerCount => (uint)players.Count(p => p != null);
        public uint FirstFreeMultiplayerPlayerIndex
        {
            get
            {
                var firstFreeSlot = players.Select((p, index) => new { p, index }).FirstOrDefault(r => r.p == null);

                if (firstFreeSlot != null)
                    return (uint)firstFreeSlot.index;

                return PlayerCount;
            }
        }

        public IReadOnlyList<PlayerInfo> Players => players.AsReadOnly();

        readonly List<PlayerInfo> players = new(4);
        readonly string name = "";
        readonly Func<string> missionName;
        readonly bool intro = false;
        int gameType; // Game.GameType
        uint level = 0;

        GameInfo(Mission missionPreset)
        {
            MapSize = 3;
            missionName = missionPreset.Name;
            RandomBase = missionPreset.Random;
            intro = missionPreset.IsIntro;
            gameType = (int)Game.GameType.Mission;

            for (int i = 0; i < missionPreset.Players.Length; ++i)
            {
                var playerInfo = missionPreset.Players[i];
                var character = playerInfo.Character.Face;
                var player = new PlayerInfo(character, PlayerInfo.PlayerColors[i],
                                                  playerInfo.Intelligence, playerInfo.Supplies,
                                                  playerInfo.Reproduction);
                player.CastlePosition = playerInfo.Castle;
                player.Color = PlayerInfo.PlayerColors[i];
                AddPlayer(player);
            }
        }

        public static GameInfo CreateServerGameInfo()
        {
            var serverGameInfo = new GameInfo(new Random(), false);
            serverGameInfo.RemoveAllPlayers();

            return serverGameInfo;
        }

        // A custom game for split-screen mode: player 0 ("You", blue) and player 1
        // ("Friend", red) are both human, the other players are AI. Player 1 gets
        // the same face as the second player in a network game.
        public static GameInfo CreateSplitScreenGameInfo()
        {
            var gameInfo = new GameInfo(new Random(), false);

            gameInfo.ReplacePlayer(1, new PlayerInfo(PlayerFace.Friend, PlayerInfo.PlayerColors[1], 40u, 40u, 40u));

            return gameInfo;
        }

        internal GameInfo(Random randomBase, bool aiPlayersOnly)
        {
            MapSize = 3;
            name = randomBase.ToString();
            gameType = (int)Game.GameType.OnePlayer;
            SetRandomBase(randomBase, aiPlayersOnly);
        }

        public PlayerInfo GetPlayer(uint player)
        {
            return players[(int)player];
        }

        internal void SetRandomBase(Random randomBase, bool aiPlayersOnly)
        {
            var random = new Random(randomBase);
            RandomBase = randomBase;

            players.Clear();

            // Player 0
            players.Add(new PlayerInfo(random));

            if (!aiPlayersOnly)
            {
                players[0].SetCharacter(PlayerFace.You);
                players[0].Intelligence = 40;
            }

            PlayerInfo playerInfo;

            // Player 1

            do
            {
                playerInfo = new PlayerInfo(random);
            }
            while (playerInfo.Face == players[0].Face);

            players.Add(playerInfo);

            uint value = random.Next();

            if ((value & 7) != 0)
            {
                // Player 2

                do
                {
                    playerInfo = new PlayerInfo(random);
                }
                while (playerInfo.Face == players[0].Face || playerInfo.Face == players[1].Face);

                players.Add(playerInfo);

                value = random.Next();

                if ((value & 3) == 0)
                {
                    // Player 3

                    do
                    {
                        playerInfo = new PlayerInfo(random);
                    }
                    while (playerInfo.Face == players[0].Face || playerInfo.Face == players[1].Face || playerInfo.Face == players[2].Face);

                    players.Add(playerInfo);
                }
            }

            int i = 0;

            foreach (var info in players)
            {
                info.Color = PlayerInfo.PlayerColors[i++];
            }
        }

        public void AddPlayer(PlayerInfo player)
        {
            players.Add(player);
        }

        internal void AddPlayer(PlayerFace character, Color color,
                         uint intelligence, uint supplies,
                         uint reproduction)
        {
            AddPlayer(new PlayerInfo(character, color, intelligence, supplies, reproduction));
        }

        public void ReplacePlayer(int playerIndex, PlayerInfo player)
        {
            players[playerIndex] = player;
        }

        internal void RemoveAllPlayers()
        {
            players.Clear();
        }

        public void RemovePlayer(uint index, bool multiplayer)
        {
            if (index >= players.Count)
                return;

            if (multiplayer)
                players[(int)index] = null;
            else if (index == players.Count - 1)
                players.RemoveAt((int)index);
            else
            {
                for (int i = (int)index; i < players.Count - 1; ++i)
                {
                    players[i] = players[i + 1];
                    players[i].Color = PlayerInfo.PlayerColors[i]; // update color for the slot
                }

                players.RemoveAt(players.Count - 1);
            }
        }

        internal static GameInfo GetIntroMission()
        {
            return new GameInfo(IntroMission);
        }

        internal static GameInfo GetMission(uint mission)
        {
            if (mission >= GetMissionCount())
            {
                return null;
            }

            var gameInfo = new GameInfo(missions[(int)mission]);
            gameInfo.level = mission;

            return gameInfo;
        }

        internal static uint GetMissionCount()
        {
            return (uint)missions.Length;
        }

        /* Tutorials 1-6 (Amiga mission_table entries 0..5): one human
           player, castle placed by the player. */
        internal static GameInfo GetTutorial(uint tutorial)
        {
            if (tutorial >= GetTutorialCount())
            {
                return null;
            }

            var gameInfo = new GameInfo(tutorials[(int)tutorial]);
            gameInfo.gameType = (int)Game.GameType.Tutorial;
            gameInfo.level = tutorial + 1;

            return gameInfo;
        }

        internal static uint GetTutorialCount()
        {
            return (uint)tutorials.Length;
        }

        internal int GameType => gameType;
        internal uint Level => level;
        internal string Name => missionName != null ? missionName() : name;

        internal Character GetCharacter(uint character)
        {
            if (character >= GetCharacterCount())
            {
                return null;
            }

            return PlayerInfo.Characters[character];
        }

        internal uint GetCharacterCount()
        {
            return (uint)PlayerInfo.Characters.Length;
        }

        internal Game Instantiate(Render.IRenderView renderView, Audio.IAudioInterface audioInterface)
        {
            return Instantiate(new[] { renderView }, audioInterface);
        }

        // In split-screen mode the game is shared by several local players. Each player
        // gets his own render view (own render map and render objects).
        internal Game Instantiate(IEnumerable<Render.IRenderView> renderViews, Audio.IAudioInterface audioInterface)
        {
            var game = new Game(renderViews, audioInterface);

            if (!game.Init(MapSize, RandomBase))
            {
                return null;
            }

            game.SetGameType(gameType, (int)level);

            uint playerIndex = 0u;

            // Initialize player and build initial castle 
            foreach (var playerInfo in players)
            {
                if (playerInfo != null)
                {
                    var player = game.InsertPlayer(playerIndex, playerInfo.Intelligence,
                                                   playerInfo.Supplies, playerInfo.Reproduction);

                    if (!playerInfo.Face.IsHuman()) // not you or your partner
                    {
                        if (intro)
                            player.AI = new IntroAI(player, playerInfo);
                        else
                            player.AI = new AI(player, playerInfo);
                    }

                    player.InitView(playerInfo.Color, playerInfo.Face);
                }

                ++playerIndex;

                if (playerIndex == Game.MAX_PLAYER_COUNT)
                    break;
            }

            game.InitKnights();

            ////Build Castles with predetermined positions
            foreach (var playerInfo in players.Where(p => !p.CastlePosition.Equals(PlayerInfo.Position.None)))
            {
                var player = game.Players.GetOrInsert((uint)players.IndexOf(playerInfo));
                var castlePos = playerInfo.CastlePosition;
                var position = game.Map.Position((MapPos)castlePos.Column, (MapPos)castlePos.Row);
                game.BuildCastle(position, player);
            }

            if (gameType == (int)Game.GameType.Tutorial && level == 6)
            {
                game.InitTutorialEnemy(PlayerInfo.PlayerColors[1]);
            }

            return game;
        }
    }
}
