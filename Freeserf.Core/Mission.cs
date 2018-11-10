/*
 * Mission.cs - Predefined game mission maps
 *
 * Copyright (C) 2013-2017  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018       Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
    public class Character
    {
        public Character(uint face, string name, string characterization)
        {
            Face = face;
            Name = name;
            Characterization = characterization;
        }

        public uint Face;
        public string Name;
        public string Characterization;
    }

    public class PlayerInfo
    {
        public static readonly Character[] Characters = new Character[]
        {
            new Character( 0, "ERROR", "ERROR"),
            new Character( 1, "Lady Amalie", "An inoffensive lady, reserved, who goes about her work peacefully."),
            new Character( 2, "Kumpy Onefinger", "A very hostile character, who loves gold above all else."),
            new Character( 3, "Balduin, a former monk", "A very discrete character, who worries chiefly about the protection of his lands and his important buildings."),
            new Character( 4, "Frollin", "His unpredictable behaviour will always take you by surprise. He will \"pilfer\" away lands that are not occupied."),
            new Character( 5, "Kallina", "She is a fighter who attempts to block the enemy’s food supply by using strategic tactics."),
            new Character( 6, "Rasparuk the druid", "His tactics consist in amassing large stocks of raw materials. But he attacks slyly."),
            new Character( 7, "Count Aldaba", "Protect your warehouses well, because he is aggressive and knows exactly where he must attack."),
            new Character( 8, "The King Rolph VII", "He is a prudent ruler, without any particular weakness. He will try to check the supply of construction materials of his adversaries."),
            new Character( 9, "Homen Doublehorn", "He is the most aggressive enemy. Watch your buildings carefully, otherwise he might take you by surprise."),
            new Character(10, "Sollok the Joker", "A sly and repugnant adversary, he will try to stop the supply of raw materials of his enemies right from the beginning of the game."),
            new Character(11, "Enemy", "Last enemy."),
            new Character(12, "You", "You."),
            new Character(13, "Friend", "Your partner.")
        };

        internal static readonly Player.Color[] PlayerColors = new Player.Color[4]
        {
            new Player.Color() {Red = 0x00, Green = 0xe3, Blue = 0xe3},
            new Player.Color() {Red = 0xcf, Green = 0x63, Blue = 0x63},
            new Player.Color() {Red = 0xdf, Green = 0x7f, Blue = 0xef},
            new Player.Color() {Red = 0xef, Green = 0xef, Blue = 0x8f}
        };

        public struct Pos
        {
            public Pos(int column, int row)
            {
                Column = column;
                Row = row;
            }

            public int Column;
            public int Row;

            public static readonly Pos None = new Pos() { Column = -1, Row = -1 };
        }

        public struct Preset
        {
            public Preset(uint character, uint intelligence, uint supplies,
                uint reproduction, Pos castlePos)
            {
                Character = Characters[character];
                Intelligence = intelligence;
                Supplies = supplies;
                Reproduction = reproduction;
                Castle = castlePos;
            }

            public Character Character;
            public uint Intelligence;
            public uint Supplies;
            public uint Reproduction;
            public Pos Castle;
        }

        public uint Intelligence { get; set; }
        public uint Supplies { get; set; }
        public uint Reproduction { get; set; }
        public uint Face { get; private set; }
        public Player.Color Color { get; set; }
        public Pos CastlePos { get; set; }

        string name;
        string characterization;


        public PlayerInfo(Random random)
        {
            uint character = (((random.Next() * 10u) >> 16) + 1u) & 0xFFu;

            SetCharacter(character);

            Intelligence = ((random.Next() * 41u) >> 16) & 0xFFu;
            Supplies = ((random.Next() * 41u) >> 16) & 0xFFu;
            Reproduction = ((random.Next() * 41u) >> 16) & 0xFFu;
            CastlePos = Pos.None;
        }

        public PlayerInfo(uint character, Player.Color color,
             uint intelligence, uint supplies, uint reproduction)
        {
            SetCharacter(character);

            Intelligence = intelligence; ;
            Supplies = supplies;
            Reproduction = reproduction;
            CastlePos = Pos.None;
        }


        public void SetCharacter(uint character)
        {
            Face = Characters[character].Face;
            name = Characters[character].Name;
            characterization = Characters[character].Characterization;
        }

        public bool HasCastle => CastlePos.Column >= 0;
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

        static readonly Mission[] missions = new Mission[]
        {
            new Mission(
                "START",
                new Random("8667715887436237"),
                new PlayerInfo.Preset(12, 40, 35, 30, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(1, 10, 5, 30, PlayerInfo.Pos.None)
            ),
            new Mission(
                "STATION",
                new Random("2831713285431227"),
                new PlayerInfo.Preset(12, 40, 30, 40, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(2, 12, 15, 30, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(3, 14, 15, 30, PlayerInfo.Pos.None)
			),
            new Mission(
                "UNITY",
                new Random("4632253338621228"),
                new PlayerInfo.Preset(12, 40, 30, 30, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(2, 18, 10, 25, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(4, 18, 10, 25, PlayerInfo.Pos.None)
			),
            new Mission(
                "WAVE",
                new Random("8447342476811762"),
                new PlayerInfo.Preset(12, 40, 25, 40, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(2, 16, 20, 30, PlayerInfo.Pos.None)
			),
            new Mission(
                "EXPORT",
                new Random("4276472414845177"),
                new PlayerInfo.Preset(12, 40, 30, 30, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(3, 16, 25, 20, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(4, 16, 25, 20, PlayerInfo.Pos.None)
			),
            new Mission(
                "OPTION",
                new Random("2333577877517478"),
                new PlayerInfo.Preset(12, 40, 30, 30, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(3, 20, 12, 14, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(5, 20, 12, 14, PlayerInfo.Pos.None)
			),
            new Mission(
                "RECORD",
                new Random("1416541231242884"),
                new PlayerInfo.Preset(12, 40, 30, 40, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(3, 22, 30, 30, PlayerInfo.Pos.None)
			),
            new Mission(
                "SCALE",
                new Random("7845187715476348"),
                new PlayerInfo.Preset(12, 40, 25, 30, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(4, 23, 25, 30, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(6, 24, 25, 30, PlayerInfo.Pos.None)
			),
            new Mission(
                "SIGN",
                new Random("5185768873118642"),
                new PlayerInfo.Preset(12, 40, 25, 40, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(4, 26, 13, 30, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(5, 28, 13, 30, PlayerInfo.Pos.None),
                new PlayerInfo.Preset(6, 30, 13, 30, PlayerInfo.Pos.None)
			),
            new Mission(
                "ACORN",
                new Random("3183215728814883"),
                new PlayerInfo.Preset(12, 40, 20, 16, new PlayerInfo.Pos(28, 14)),
                new PlayerInfo.Preset(4, 30, 19, 20, new PlayerInfo.Pos(5, 47))
            ),
            new Mission(
                "CHOPPER",
                new Random("4376241846215474"),
                new PlayerInfo.Preset(12, 40, 16, 20, new PlayerInfo.Pos(16, 42)),
                new PlayerInfo.Preset(5, 33, 10, 20, new PlayerInfo.Pos(52, 25)),
                new PlayerInfo.Preset(7, 34, 13, 20, new PlayerInfo.Pos(23, 12))
			),
            new Mission(
                "GATE",
                new Random("6371557668231277"),
                new PlayerInfo.Preset(12, 40, 23, 27, new PlayerInfo.Pos(53, 13)),
                new PlayerInfo.Preset(5, 27, 17, 24, new PlayerInfo.Pos(27, 10)),
                new PlayerInfo.Preset(6, 27, 13, 24, new PlayerInfo.Pos(29, 38)),
                new PlayerInfo.Preset(7, 27, 13, 24, new PlayerInfo.Pos(15, 32))
			),
            new Mission(
                "ISLAND",
                new Random("8473352672411117"),
                new PlayerInfo.Preset(12, 40, 24, 20, new PlayerInfo.Pos(7, 26)),
                new PlayerInfo.Preset(5, 20, 30, 20, new PlayerInfo.Pos(2, 10))
            ),
            new Mission(
                "LEGION",
                new Random("1167854231884464"),
                new PlayerInfo.Preset(12, 40, 20, 23, new PlayerInfo.Pos(19,  3)),
                new PlayerInfo.Preset(6, 28, 16, 20, new PlayerInfo.Pos(55,  7)),
                new PlayerInfo.Preset(8, 28, 16, 20, new PlayerInfo.Pos(55, 46))
			),
            new Mission(
                "PIECE",
                new Random("2571462671725414"),
                new PlayerInfo.Preset(12, 40, 20, 17, new PlayerInfo.Pos(41, 5)),
                new PlayerInfo.Preset(6, 40, 23, 20, new PlayerInfo.Pos(19, 49)),
                new PlayerInfo.Preset(7, 37, 20, 20, new PlayerInfo.Pos(58, 52)),
                new PlayerInfo.Preset(8, 40, 15, 15, new PlayerInfo.Pos(43, 31))
			),
            new Mission(
                "RIVAL",
                new Random("4563653871271587"),
                new PlayerInfo.Preset(12, 40, 26, 23, new PlayerInfo.Pos(36, 63)),
                new PlayerInfo.Preset(6, 28, 29, 40, new PlayerInfo.Pos(14, 15))
			),
            new Mission(
                "SAVAGE",
                new Random("7212145428156114"),
                new PlayerInfo.Preset(12, 40, 25, 12, new PlayerInfo.Pos(63, 59)),
                new PlayerInfo.Preset(7, 29, 17, 10, new PlayerInfo.Pos(29, 24)),
                new PlayerInfo.Preset(8, 29, 17, 10, new PlayerInfo.Pos(39, 26)),
                new PlayerInfo.Preset(9, 32, 17, 10, new PlayerInfo.Pos(42, 49))
			),
            new Mission(
                "XAVER",
                new Random("4276472414435177"),
                new PlayerInfo.Preset(12, 40, 25, 40, new PlayerInfo.Pos(15, 0)),
                new PlayerInfo.Preset(7, 40, 30, 35, new PlayerInfo.Pos(34, 48)),
                new PlayerInfo.Preset(9, 30, 30, 35, new PlayerInfo.Pos(58, 5))
            ),
            new Mission(
                "BLADE",
                new Random("7142748441424786"),
                new PlayerInfo.Preset(12, 40, 30, 20, new PlayerInfo.Pos(13, 37)),
                new PlayerInfo.Preset(7, 40, 20, 20, new PlayerInfo.Pos(32, 34))
			),
            new Mission(
                "BEACON",
                new Random("6882188351133886"),
                new PlayerInfo.Preset(12, 40,  9, 10, new PlayerInfo.Pos(14, 42)),
                new PlayerInfo.Preset(8, 40, 16, 22, new PlayerInfo.Pos(62, 1)),
                new PlayerInfo.Preset(9, 40, 16, 23, new PlayerInfo.Pos(32, 14))
			),
            new Mission(
                "PASTURE",
                new Random("7742136435163436"),
                new PlayerInfo.Preset(12, 40, 20, 11, new PlayerInfo.Pos(38, 17)),
                new PlayerInfo.Preset(8, 30, 22, 13, new PlayerInfo.Pos(32, 51)),
                new PlayerInfo.Preset(9, 30, 23, 13, new PlayerInfo.Pos(1, 50)),
                new PlayerInfo.Preset(10, 30, 21, 13, new PlayerInfo.Pos(4, 9))
            ),
            new Mission(
                "OMNUS",
                new Random("6764387728224725"),
                new PlayerInfo.Preset(12, 40, 20, 40, new PlayerInfo.Pos(42, 20)),
                new PlayerInfo.Preset(8, 36, 25, 40, new PlayerInfo.Pos(48, 47))
			),
            new Mission(
                "TRIBUTE",
                new Random("5848744734731253"),
                new PlayerInfo.Preset(12, 40,  5, 11, new PlayerInfo.Pos(53, 1)),
                new PlayerInfo.Preset(9, 35, 30, 10, new PlayerInfo.Pos(20, 2)),
                new PlayerInfo.Preset(10, 37, 30, 10, new PlayerInfo.Pos(16, 55))
			),
            new Mission(
                "FOUNTAIN",
                new Random("6183541838474434"),
                new PlayerInfo.Preset(12, 40, 20, 12, new PlayerInfo.Pos(3, 34)),
                new PlayerInfo.Preset(9, 30, 25, 10, new PlayerInfo.Pos(47, 41)),
                new PlayerInfo.Preset(10, 30, 26, 10, new PlayerInfo.Pos(42, 52))
			),
            new Mission(
                "CHUDE",
                new Random("7633126817245833"),
                new PlayerInfo.Preset(12, 40, 20, 40, new PlayerInfo.Pos(23, 38)),
                new PlayerInfo.Preset(9, 40, 25, 40, new PlayerInfo.Pos(57, 52))
			),
            new Mission(
                "TRAILER",
                new Random("5554144773646312"),
                new PlayerInfo.Preset(12, 40, 20, 30, new PlayerInfo.Pos(29, 11)),
                new PlayerInfo.Preset(10, 38, 30, 35, new PlayerInfo.Pos(15, 40))
			),
            new Mission(
                "CANYON",
                new Random("3122431112682557"),
                new PlayerInfo.Preset(12, 40, 18, 28, new PlayerInfo.Pos(49, 53)),
                new PlayerInfo.Preset(10, 39, 25, 40, new PlayerInfo.Pos(14, 53))
			),
            new Mission(
                "REPRESS",
                new Random("2568412624848266"),
                new PlayerInfo.Preset(12, 40, 20, 40, new PlayerInfo.Pos(44, 39)),
                new PlayerInfo.Preset(10, 39, 25, 40, new PlayerInfo.Pos(44, 63))
			),
            new Mission(
                "YOKI",
                new Random("3736685353284538"),
                new PlayerInfo.Preset(12, 40,  5, 22, new PlayerInfo.Pos(53, 8)),
                new PlayerInfo.Preset(11, 40, 15, 20, new PlayerInfo.Pos(30, 22))
			),
            new Mission(
                "PASSIVE",
                new Random("5471458635555317"),
                new PlayerInfo.Preset(12, 40,  5, 20, new PlayerInfo.Pos(25, 46)),
                new PlayerInfo.Preset(11, 40, 20, 20, new PlayerInfo.Pos(51, 42))
            )
        };

        public uint MapSize { get; set; }

        public Random RandomBase { get; private set; }

        public uint PlayerCount => (uint)players.Count;

        readonly List<PlayerInfo> players = new List<PlayerInfo>(4);
        readonly string name = "";

        GameInfo(Mission missionPreset)
        {
            MapSize = 3;
            name = missionPreset.Name;
            RandomBase = missionPreset.Random;

            for (int i = 0; i < missionPreset.Players.Length; ++i)
            {
                PlayerInfo.Preset playerInfo = missionPreset.Players[i];
                uint character = playerInfo.Character.Face;
                PlayerInfo player = new PlayerInfo(character, PlayerInfo.PlayerColors[i],
                                                  playerInfo.Intelligence, playerInfo.Supplies,
                                                  playerInfo.Reproduction);
                player.CastlePos = playerInfo.Castle;
                AddPlayer(player);
            }
        }

        public GameInfo(Random randomBase)
        {
            MapSize = 3;
            name = randomBase.ToString();
            SetRandomBase(randomBase);
        }

        public PlayerInfo GetPlayer(uint player)
        {
            return players[(int)player];
        }

        public void SetRandomBase(Random randomBase)
        {
            Random random = randomBase;
            RandomBase = randomBase;

            players.Clear();

            // Player 0
            players.Add(new PlayerInfo(random));
            players[0].SetCharacter(12);
            players[0].Intelligence = 40;

            // Player 1
            players.Add(new PlayerInfo(random));

            uint val = random.Next();

            if ((val & 7) != 0)
            {
                // Player 2
                players.Add(new PlayerInfo(random));

                val = random.Next();

                if ((val & 3) == 0)
                {
                    // Player 3
                    players.Add(new PlayerInfo(random));
                }
            }

            int i = 0;

            foreach (PlayerInfo info in players)
            {
                info.Color = PlayerInfo.PlayerColors[i++];
            }
        }

        public void AddPlayer(PlayerInfo player)
        {
            players.Add(player);
        }

        public void AddPlayer(uint character, Player.Color color,
                         uint intelligence, uint supplies,
                         uint reproduction)
        {
            AddPlayer(new PlayerInfo(character, color, intelligence, supplies, reproduction));
        }

        public void RemoveAllPlayers()
        {
            players.Clear();
        }

        public GameInfo GetMission(uint mission)
        {
            if (mission >= GetMissionCount())
            {
                return null;
            }

            return new GameInfo(missions[(int)mission]);
        }

        public uint GetMissionCount()
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

        public Game Instantiate(Render.IRenderView renderView)
        {
            Game game = new Game(renderView);

            if (!game.Init(MapSize, RandomBase))
            {
                return null;
            }

            /* Initialize player and build initial castle */
            foreach (PlayerInfo playerInfo in players)
            {
                uint index = game.AddPlayer(playerInfo.Intelligence,
                                            playerInfo.Supplies,
                                            playerInfo.Reproduction);
                Player player = game.GetPlayer(index);
                player.InitView(playerInfo.Color, playerInfo.Face);

                PlayerInfo.Pos castle_pos = playerInfo.CastlePos;

                if (castle_pos.Column > -1 && castle_pos.Row > -1)
                {
                    uint pos = game.Map.Pos((uint)castle_pos.Column, (uint)castle_pos.Row);

                    game.BuildCastle(pos, player);
                }
            }

            return game;
        }
    }
}
