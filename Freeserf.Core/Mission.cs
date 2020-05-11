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

namespace Freeserf
{
    using MapPos = UInt32;

    public class Character
    {
        public Character(PlayerFace face, string name, string characterization)
        {
            Face = face;
            Name = name;
            Characterization = characterization;
        }

        public PlayerFace Face;
        public string Name;
        public string Characterization;
    }

    public class PlayerInfo
    {
        public static readonly Character[] Characters = new Character[]
        {
            new Character(PlayerFace.None, "ERROR", "ERROR"),
            new Character(PlayerFace.LadyAmalie, "Lady Amalie", "An inoffensive lady, reserved, who goes about her work peacefully."),
            new Character(PlayerFace.KumpyOnefinger, "Kumpy Onefinger", "A very hostile character, who loves gold above all else."),
            new Character(PlayerFace.Balduin, "Balduin, a former monk", "A very discrete character, who worries chiefly about the protection of his lands and his important buildings."),
            new Character(PlayerFace.Frollin, "Frollin", "His unpredictable behaviour will always take you by surprise. He will \"pilfer\" away lands that are not occupied."),
            new Character(PlayerFace.Kallina, "Kallina", "She is a fighter who attempts to block the enemy’s food supply by using strategic tactics."),
            new Character(PlayerFace.Rasparuk, "Rasparuk the druid", "His tactics consist in amassing large stocks of raw materials. But he attacks slyly."),
            new Character(PlayerFace.CountAldaba, "Count Aldaba", "Protect your warehouses well, because he is aggressive and knows exactly where he must attack."),
            new Character(PlayerFace.KingRolph, "The King Rolph VII", "He is a prudent ruler, without any particular weakness. He will try to check the supply of construction materials of his adversaries."),
            new Character(PlayerFace.HomenDoublehorn, "Homen Doublehorn", "He is the most aggressive enemy. Watch your buildings carefully, otherwise he might take you by surprise."),
            new Character(PlayerFace.Sollok, "Sollok the Joker", "A sly and repugnant adversary, he will try to stop the supply of raw materials of his enemies right from the beginning of the game."),
            new Character(PlayerFace.Enemy, "Enemy", "Last enemy."),
            new Character(PlayerFace.You, "You", "You."),
            new Character(PlayerFace.Friend, "Friend", "Your partner.")
        };

        internal static readonly Color[] PlayerColors = new Color[4]
        {
            new Color() {Red = 0x00, Green = 0xe3, Blue = 0xe3},
            new Color() {Red = 0xcf, Green = 0x63, Blue = 0x63},
            new Color() {Red = 0xdf, Green = 0x7f, Blue = 0xef},
            new Color() {Red = 0xef, Green = 0xef, Blue = 0x8f}
        };

        public struct Position
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

        public struct Preset
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
        public Color Color { get; set; }
        public Position CastlePosition { get; set; }

        public PlayerInfo(Random random)
        {
            var character = (((random.Next() * 10u) >> 16) + 1u) & 0xFFu;

            SetCharacter((PlayerFace)character);

            Intelligence = ((random.Next() * 41u) >> 16) & 0xFFu;
            Supplies = ((random.Next() * 41u) >> 16) & 0xFFu;
            Reproduction = ((random.Next() * 41u) >> 16) & 0xFFu;
            CastlePosition = Position.None;
        }

        public PlayerInfo(PlayerFace character, Color color,
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
            Face = Characters[(int)character].Face;
        }

        public bool HasCastle => CastlePosition.Column >= 0;
    }

    public class GameInfo
    {
        public struct Mission
        {
            internal Mission(string name, Random random, params PlayerInfo.Preset[] players)
            {
                Name = name;
                Random = random;
                Players = players;
            }

            public string Name;
            public Random Random;
            public PlayerInfo.Preset[] Players;
        }

        static readonly Mission IntroMission = new Mission
        (
            "INTRO",
            new Random(),
            new PlayerInfo.Preset(1, 0, 10, 5, PlayerInfo.Position.None),
            new PlayerInfo.Preset(2, 0, 10, 5, PlayerInfo.Position.None),
            new PlayerInfo.Preset(3, 0, 10, 5, PlayerInfo.Position.None),
            new PlayerInfo.Preset(4, 0, 10, 5, PlayerInfo.Position.None)
        );

        static readonly Mission[] missions = new Mission[]
        {
            new Mission(
                "START",
                new Random("8667715887436237"),
                new PlayerInfo.Preset(12, 40, 35, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(1, 10, 5, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                "STATION",
                new Random("2831713285431227"),
                new PlayerInfo.Preset(12, 40, 30, 40, PlayerInfo.Position.None),
                new PlayerInfo.Preset(2, 12, 15, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(3, 14, 15, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                "UNITY",
                new Random("4632253338621228"),
                new PlayerInfo.Preset(12, 40, 30, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(2, 18, 10, 25, PlayerInfo.Position.None),
                new PlayerInfo.Preset(4, 18, 10, 25, PlayerInfo.Position.None)
            ),
            new Mission(
                "WAVE",
                new Random("8447342476811762"),
                new PlayerInfo.Preset(12, 40, 25, 40, PlayerInfo.Position.None),
                new PlayerInfo.Preset(2, 16, 20, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                "EXPORT",
                new Random("4276472414845177"),
                new PlayerInfo.Preset(12, 40, 30, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(3, 16, 25, 20, PlayerInfo.Position.None),
                new PlayerInfo.Preset(4, 16, 25, 20, PlayerInfo.Position.None)
            ),
            new Mission(
                "OPTION",
                new Random("2333577877517478"),
                new PlayerInfo.Preset(12, 40, 30, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(3, 20, 12, 14, PlayerInfo.Position.None),
                new PlayerInfo.Preset(5, 20, 12, 14, PlayerInfo.Position.None)
            ),
            new Mission(
                "RECORD",
                new Random("1416541231242884"),
                new PlayerInfo.Preset(12, 40, 30, 40, PlayerInfo.Position.None),
                new PlayerInfo.Preset(3, 22, 30, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                "SCALE",
                new Random("7845187715476348"),
                new PlayerInfo.Preset(12, 40, 25, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(4, 23, 25, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(6, 24, 25, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                "SIGN",
                new Random("5185768873118642"),
                new PlayerInfo.Preset(12, 40, 25, 40, PlayerInfo.Position.None),
                new PlayerInfo.Preset(4, 26, 13, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(5, 28, 13, 30, PlayerInfo.Position.None),
                new PlayerInfo.Preset(6, 30, 13, 30, PlayerInfo.Position.None)
            ),
            new Mission(
                "ACORN",
                new Random("3183215728814883"),
                new PlayerInfo.Preset(12, 40, 20, 16, new PlayerInfo.Position(28, 14)),
                new PlayerInfo.Preset(4, 30, 19, 20, new PlayerInfo.Position(5, 47))
            ),
            new Mission(
                "CHOPPER",
                new Random("4376241846215474"),
                new PlayerInfo.Preset(12, 40, 16, 20, new PlayerInfo.Position(16, 42)),
                new PlayerInfo.Preset(5, 33, 10, 20, new PlayerInfo.Position(52, 25)),
                new PlayerInfo.Preset(7, 34, 13, 20, new PlayerInfo.Position(23, 12))
            ),
            new Mission(
                "GATE",
                new Random("6371557668231277"),
                new PlayerInfo.Preset(12, 40, 23, 27, new PlayerInfo.Position(53, 13)),
                new PlayerInfo.Preset(5, 27, 17, 24, new PlayerInfo.Position(27, 10)),
                new PlayerInfo.Preset(6, 27, 13, 24, new PlayerInfo.Position(29, 38)),
                new PlayerInfo.Preset(7, 27, 13, 24, new PlayerInfo.Position(15, 32))
            ),
            new Mission(
                "ISLAND",
                new Random("8473352672411117"),
                new PlayerInfo.Preset(12, 40, 24, 20, new PlayerInfo.Position(7, 26)),
                new PlayerInfo.Preset(5, 20, 30, 20, new PlayerInfo.Position(2, 10))
            ),
            new Mission(
                "LEGION",
                new Random("1167854231884464"),
                new PlayerInfo.Preset(12, 40, 20, 23, new PlayerInfo.Position(19,  3)),
                new PlayerInfo.Preset(6, 28, 16, 20, new PlayerInfo.Position(55,  7)),
                new PlayerInfo.Preset(8, 28, 16, 20, new PlayerInfo.Position(55, 46))
            ),
            new Mission(
                "PIECE",
                new Random("2571462671725414"),
                new PlayerInfo.Preset(12, 40, 20, 17, new PlayerInfo.Position(41, 5)),
                new PlayerInfo.Preset(6, 40, 23, 20, new PlayerInfo.Position(19, 49)),
                new PlayerInfo.Preset(7, 37, 20, 20, new PlayerInfo.Position(58, 52)),
                new PlayerInfo.Preset(8, 40, 15, 15, new PlayerInfo.Position(43, 31))
            ),
            new Mission(
                "RIVAL",
                new Random("4563653871271587"),
                new PlayerInfo.Preset(12, 40, 26, 23, new PlayerInfo.Position(36, 63)),
                new PlayerInfo.Preset(6, 28, 29, 40, new PlayerInfo.Position(14, 15))
            ),
            new Mission(
                "SAVAGE",
                new Random("7212145428156114"),
                new PlayerInfo.Preset(12, 40, 25, 12, new PlayerInfo.Position(63, 59)),
                new PlayerInfo.Preset(7, 29, 17, 10, new PlayerInfo.Position(29, 24)),
                new PlayerInfo.Preset(8, 29, 17, 10, new PlayerInfo.Position(39, 26)),
                new PlayerInfo.Preset(9, 32, 17, 10, new PlayerInfo.Position(42, 49))
            ),
            new Mission(
                "XAVER",
                new Random("4276472414435177"),
                new PlayerInfo.Preset(12, 40, 25, 40, new PlayerInfo.Position(15, 0)),
                new PlayerInfo.Preset(7, 40, 30, 35, new PlayerInfo.Position(34, 48)),
                new PlayerInfo.Preset(9, 30, 30, 35, new PlayerInfo.Position(58, 5))
            ),
            new Mission(
                "BLADE",
                new Random("7142748441424786"),
                new PlayerInfo.Preset(12, 40, 30, 20, new PlayerInfo.Position(13, 37)),
                new PlayerInfo.Preset(7, 40, 20, 20, new PlayerInfo.Position(32, 34))
            ),
            new Mission(
                "BEACON",
                new Random("6882188351133886"),
                new PlayerInfo.Preset(12, 40,  9, 10, new PlayerInfo.Position(14, 42)),
                new PlayerInfo.Preset(8, 40, 16, 22, new PlayerInfo.Position(62, 1)),
                new PlayerInfo.Preset(9, 40, 16, 23, new PlayerInfo.Position(32, 14))
            ),
            new Mission(
                "PASTURE",
                new Random("7742136435163436"),
                new PlayerInfo.Preset(12, 40, 20, 11, new PlayerInfo.Position(38, 17)),
                new PlayerInfo.Preset(8, 30, 22, 13, new PlayerInfo.Position(32, 51)),
                new PlayerInfo.Preset(9, 30, 23, 13, new PlayerInfo.Position(1, 50)),
                new PlayerInfo.Preset(10, 30, 21, 13, new PlayerInfo.Position(4, 9))
            ),
            new Mission(
                "OMNUS",
                new Random("6764387728224725"),
                new PlayerInfo.Preset(12, 40, 20, 40, new PlayerInfo.Position(42, 20)),
                new PlayerInfo.Preset(8, 36, 25, 40, new PlayerInfo.Position(48, 47))
            ),
            new Mission(
                "TRIBUTE",
                new Random("5848744734731253"),
                new PlayerInfo.Preset(12, 40,  5, 11, new PlayerInfo.Position(53, 1)),
                new PlayerInfo.Preset(9, 35, 30, 10, new PlayerInfo.Position(20, 2)),
                new PlayerInfo.Preset(10, 37, 30, 10, new PlayerInfo.Position(16, 55))
            ),
            new Mission(
                "FOUNTAIN",
                new Random("6183541838474434"),
                new PlayerInfo.Preset(12, 40, 20, 12, new PlayerInfo.Position(3, 34)),
                new PlayerInfo.Preset(9, 30, 25, 10, new PlayerInfo.Position(47, 41)),
                new PlayerInfo.Preset(10, 30, 26, 10, new PlayerInfo.Position(42, 52))
            ),
            new Mission(
                "CHUDE",
                new Random("7633126817245833"),
                new PlayerInfo.Preset(12, 40, 20, 40, new PlayerInfo.Position(23, 38)),
                new PlayerInfo.Preset(9, 40, 25, 40, new PlayerInfo.Position(57, 52))
            ),
            new Mission(
                "TRAILER",
                new Random("5554144773646312"),
                new PlayerInfo.Preset(12, 40, 20, 30, new PlayerInfo.Position(29, 11)),
                new PlayerInfo.Preset(10, 38, 30, 35, new PlayerInfo.Position(15, 40))
            ),
            new Mission(
                "CANYON",
                new Random("3122431112682557"),
                new PlayerInfo.Preset(12, 40, 18, 28, new PlayerInfo.Position(49, 53)),
                new PlayerInfo.Preset(10, 39, 25, 40, new PlayerInfo.Position(14, 53))
            ),
            new Mission(
                "REPRESS",
                new Random("2568412624848266"),
                new PlayerInfo.Preset(12, 40, 20, 40, new PlayerInfo.Position(44, 39)),
                new PlayerInfo.Preset(10, 39, 25, 40, new PlayerInfo.Position(44, 63))
            ),
            new Mission(
                "YOKI",
                new Random("3736685353284538"),
                new PlayerInfo.Preset(12, 40,  5, 22, new PlayerInfo.Position(53, 8)),
                new PlayerInfo.Preset(11, 40, 15, 20, new PlayerInfo.Position(30, 22))
            ),
            new Mission(
                "PASSIVE",
                new Random("5471458635555317"),
                new PlayerInfo.Preset(12, 40,  5, 20, new PlayerInfo.Position(25, 46)),
                new PlayerInfo.Preset(11, 40, 20, 20, new PlayerInfo.Position(51, 42))
            ),
            new Mission(
                "PYRDACOR",
                new Random("5079726461636072"),
                new PlayerInfo.Preset(12, 40,  0, 30, new PlayerInfo.Position(54, 63)),
                new PlayerInfo.Preset(11, 40, 18, 16, new PlayerInfo.Position(55, 25))
            )
        };

        public uint MapSize { get; set; }

        public Random RandomBase { get; private set; }

        public uint PlayerCount => (uint)players.Count;
        public IReadOnlyList<PlayerInfo> Players => players.AsReadOnly();

        readonly List<PlayerInfo> players = new List<PlayerInfo>(4);
        readonly string name = "";
        readonly bool intro = false;

        GameInfo(Mission missionPreset)
        {
            MapSize = 3;
            name = missionPreset.Name;
            RandomBase = missionPreset.Random;
            intro = missionPreset.Name == "INTRO";

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

        public GameInfo(Random randomBase, bool aiPlayersOnly)
        {
            MapSize = 3;
            name = randomBase.ToString();
            SetRandomBase(randomBase, aiPlayersOnly);
        }

        public PlayerInfo GetPlayer(uint player)
        {
            return players[(int)player];
        }

        public void SetRandomBase(Random randomBase, bool aiPlayersOnly)
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

        public void AddPlayer(PlayerFace character, Color color,
                         uint intelligence, uint supplies,
                         uint reproduction)
        {
            AddPlayer(new PlayerInfo(character, color, intelligence, supplies, reproduction));
        }

        public void RemoveAllPlayers()
        {
            players.Clear();
        }

        public void RemovePlayer(uint index)
        {
            if (index >= players.Count)
                return;

            if (index == players.Count - 1)
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

        public static GameInfo GetIntroMission()
        {
            return new GameInfo(IntroMission);
        }

        public static GameInfo GetMission(uint mission)
        {
            if (mission >= GetMissionCount())
            {
                return null;
            }

            return new GameInfo(missions[(int)mission]);
        }

        public static uint GetMissionCount()
        {
            return (uint)missions.Length;
        }

        public Character GetCharacter(uint character)
        {
            if (character >= GetCharacterCount())
            {
                return null;
            }

            return PlayerInfo.Characters[character];
        }

        public uint GetCharacterCount()
        {
            return (uint)PlayerInfo.Characters.Length;
        }

        public Game Instantiate(Render.IRenderView renderView, Audio.IAudioInterface audioInterface)
        {
            var game = new Game(renderView, audioInterface);

            if (!game.Init(MapSize, RandomBase))
            {
                return null;
            }

            // Initialize player and build initial castle 
            foreach (var playerInfo in players)
            {
                var index = game.AddPlayer(playerInfo.Intelligence,
                                           playerInfo.Supplies,
                                           playerInfo.Reproduction);
                var player = game.GetPlayer(index);

                if (!playerInfo.Face.IsHuman()) // not you or your partner
                {
                    if (intro)
                        player.AI = new IntroAI(player, playerInfo);
                    else
                        player.AI = new AI(player, playerInfo);
                }

                player.InitView(playerInfo.Color, playerInfo.Face);

                var castlePos = playerInfo.CastlePosition;

                if (castlePos.Column > -1 && castlePos.Row > -1)
                {
                    var position = game.Map.Position((MapPos)castlePos.Column, (MapPos)castlePos.Row);

                    game.BuildCastle(position, player);
                }
            }

            return game;
        }
    }
}
