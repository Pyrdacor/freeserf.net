using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Freeserf.Localization;
using Freeserf.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    [DoNotParallelize]
    public class SetupTextTests
    {
        CultureInfo originalCulture;
        CultureInfo originalUICulture;

        [TestInitialize]
        public void SetEnglishCulture()
        {
            originalCulture = CultureInfo.CurrentCulture;
            originalUICulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
        }

        [TestCleanup]
        public void RestoreCulture()
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }

        [TestMethod]
        [DataRow(false, false, false, "host | not found")]
        [DataRow(true, false, false, "Name | host | 2/4")]
        [DataRow(true, false, true, "Name | host | 2/4 | saved")]
        [DataRow(true, true, false, "Name | host | 2/4 | running")]
        [DataRow(true, true, true, "Name | host | 2/4 | running")]
        public void ServerList_PreservesAllStatuses(bool online, bool inGame, bool allowsRejoin, string expected)
        {
            var server = new ServerInfo
            {
                Name = "Name",
                HostName = "host",
                SavedAddress = "host",
                CurrentPlayers = 2,
                MaxPlayers = 4,
                Online = online,
                InGame = inGame,
                AllowsRejoin = allowsRejoin
            };

            Assert.AreEqual(expected, server.ToString());
        }

        [TestMethod]
        [DataRow("en")]
        [DataRow("de")]
        public void LocalizedLabels_StayBetweenMainMenuWoodenBars(string language)
        {
            var culture = CultureInfo.GetCultureInfo(language);
            (string Key, int Column)[] labels =
            [
                (nameof(Strings.Setup_NewGame), GameInitBox.HeaderTextColumn),
                (nameof(Strings.Setup_Demo), GameInitBox.HeaderTextColumn),
                (nameof(Strings.Setup_Multiplayer), GameInitBox.HeaderTextColumn),
                (nameof(Strings.Setup_HostGame), GameInitBox.HeaderTextColumn),
                (nameof(Strings.Setup_MultiplayerJoin), GameInitBox.HeaderTextColumn),
                (nameof(Strings.Setup_StartingGame), GameInitBox.HeaderTextColumn),
                (nameof(Strings.Setup_StartMission), GameInitBox.HeaderTextColumn),
                (nameof(Strings.Setup_StartTutorial), GameInitBox.HeaderTextColumn),
                (nameof(Strings.Setup_LoadGame), GameInitBox.HeaderTextColumn),
                (nameof(Strings.Setup_AddressLabel), GameInitBox.HeaderTextColumn),
                (nameof(Strings.Setup_MapSizeLabel), GameInitBox.HeaderTextColumn),
                (nameof(Strings.Setup_MissionLabel), GameInitBox.HeaderTextColumn),
                (nameof(Strings.Setup_TutorialLabel), GameInitBox.HeaderTextColumn),
                (nameof(Strings.Setup_NameLabel), GameInitBox.ServerNameTextColumn),
                (nameof(Strings.Setup_HostGame), GameInitBox.HostGameTextColumn),
                (nameof(Strings.Setup_ServerAddress), GameInitBox.FooterTextColumn)
            ];

            Assert.AreEqual(16, GameInitBox.TextContentLeft);
            Assert.AreEqual(336, GameInitBox.TextContentRight);
            foreach (var (key, column) in labels)
            {
                string template = Strings.ResourceManager.GetString(key, culture);
                Assert.IsNotNull(template, key);
                string text = string.Format(culture, template, "255.255.255.255");
                int left = GameInitBox.GetTextX(column);
                int right = left + 8 + (text.Length - 1) * GameInitBox.TextCharacterAdvance;
                Assert.IsGreaterThanOrEqualTo(GameInitBox.TextContentLeft, left, $"{language}: {key}");
                Assert.IsLessThanOrEqualTo(GameInitBox.TextContentRight, right, $"{language}: {key}");
            }

            (string Key, int X)[] checkBoxes =
            [
                (nameof(Strings.Setup_ServerValues), GameInitBox.ServerValuesCheckBoxX),
                (nameof(Strings.Setup_IdenticalValues), GameInitBox.SameValuesCheckBoxX)
            ];
            foreach (var (key, x) in checkBoxes)
            {
                string text = Strings.ResourceManager.GetString(key, culture);
                Assert.IsNotNull(text, key);
                int textLeft = x + 18;
                Assert.IsGreaterThanOrEqualTo(GameInitBox.TextContentLeft, textLeft, $"{language}: {key}");
                Assert.IsLessThanOrEqualTo(GameInitBox.TextContentRight, textLeft + text.Length * 8, $"{language}: {key}");
            }
        }

        [TestMethod]
        public void SetupLabels_PreserveWording()
        {
            string[] expected =
            [
                "Freeserf Server", "Server Values", "Identical Values", "New game",
                "Demo", "Multiplayer", "Host game", "Multiplayer Join", "Starting Game",
                "IP:", "Name:", "Mapsize:", "Start mission", "Mission:", "Start tutorial",
                "Tutorial:", "Load game", "IP: 127.0.0.1"
            ];
            string[] actual =
            [
                Strings.Setup_DefaultServerName, Strings.Setup_ServerValues, Strings.Setup_IdenticalValues,
                Strings.Setup_NewGame, Strings.Setup_Demo, Strings.Setup_Multiplayer, Strings.Setup_HostGame,
                Strings.Setup_MultiplayerJoin, Strings.Setup_StartingGame, Strings.Setup_AddressLabel,
                Strings.Setup_NameLabel, Strings.Setup_MapSizeLabel, Strings.Setup_StartMission,
                Strings.Setup_MissionLabel, Strings.Setup_StartTutorial, Strings.Setup_TutorialLabel,
                Strings.Setup_LoadGame, string.Format(CultureInfo.CurrentCulture, Strings.Setup_ServerAddress, "127.0.0.1")
            ];

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MissionNamesAndIdentity_PreservePresets()
        {
            string[] names =
            [
                "START", "STATION", "UNITY", "WAVE", "EXPORT", "OPTION", "RECORD", "SCALE",
                "SIGN", "ACORN", "CHOPPER", "GATE", "ISLAND", "LEGION", "PIECE", "RIVAL",
                "SAVAGE", "XAVER", "BLADE", "BEACON", "PASTURE", "OMNUS", "TRIBUTE",
                "FOUNTAIN", "CHUDE", "TRAILER", "CANYON", "REPRESS", "YOKI", "PASSIVE", "PYRDACOR"
            ];
            var introFlag = typeof(GameInfo).GetField("intro", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(introFlag);
            Assert.AreEqual((uint)names.Length, GameInfo.GetMissionCount());

            for (uint index = 0; index < names.Length; ++index)
            {
                var mission = GameInfo.GetMission(index);
                Assert.AreEqual(names[index], mission.Name);
                Assert.AreEqual((int)Game.GameType.Mission, mission.GameType);
                Assert.AreEqual(index, mission.Level);
                Assert.AreEqual(false, introFlag.GetValue(mission));
            }

            var intro = GameInfo.GetIntroMission();
            Assert.AreEqual("INTRO", intro.Name);
            Assert.AreEqual(true, introFlag.GetValue(intro));
            Assert.AreEqual((int)Game.GameType.Mission, intro.GameType);
            Assert.IsNull(GameInfo.GetMission((uint)names.Length));
        }

        [TestMethod]
        public void TutorialsAndRandomGame_PreserveNamesAndIdentity()
        {
            Assert.AreEqual(6u, GameInfo.GetTutorialCount());

            for (uint index = 0; index < 6; ++index)
            {
                var tutorial = GameInfo.GetTutorial(index);
                Assert.AreEqual($"Tutorial {index + 1}", tutorial.Name);
                Assert.AreEqual((int)Game.GameType.Tutorial, tutorial.GameType);
                Assert.AreEqual(index + 1, tutorial.Level);
            }

            Assert.IsNull(GameInfo.GetTutorial(6));
            var random = new global::Freeserf.Random("1234567812345678");
            var game = new GameInfo(random, false);
            Assert.AreEqual(random.ToString(), game.Name);
            Assert.AreEqual((int)Game.GameType.OnePlayer, game.GameType);
        }

        [TestMethod]
        [DataRow(0, "ERROR", "ERROR")]
        [DataRow(1, "Lady Amalie", "An inoffensive lady, reserved, who goes about her work peacefully.")]
        [DataRow(2, "Kumpy Onefinger", "A very hostile character, who loves gold above all else.")]
        [DataRow(3, "Balduin, a former monk", "A very discrete character, who worries chiefly about the protection of his lands and his important buildings.")]
        [DataRow(4, "Frollin", "His unpredictable behaviour will always take you by surprise. He will \"pilfer\" away lands that are not occupied.")]
        [DataRow(5, "Kallina", "She is a fighter who attempts to block the enemy’s food supply by using strategic tactics.")]
        [DataRow(6, "Rasparuk the druid", "His tactics consist in amassing large stocks of raw materials. But he attacks slyly.")]
        [DataRow(7, "Count Aldaba", "Protect your warehouses well, because he is aggressive and knows exactly where he must attack.")]
        [DataRow(8, "The King Rolph VII", "He is a prudent ruler, without any particular weakness. He will try to check the supply of construction materials of his adversaries.")]
        [DataRow(9, "Homen Doublehorn", "He is the most aggressive enemy. Watch your buildings carefully, otherwise he might take you by surprise.")]
        [DataRow(10, "Sollok the Joker", "A sly and repugnant adversary, he will try to stop the supply of raw materials of his enemies right from the beginning of the game.")]
        [DataRow(11, "Enemy", "Last enemy.")]
        [DataRow(12, "You", "You.")]
        [DataRow(13, "Friend", "Your partner.")]
        public void Characters_PreserveNamesAndDescriptions(int index, string name, string description)
        {
            var character = PlayerInfo.Characters[index];
            Assert.AreEqual(index, (int)character.Face);
            Assert.AreEqual(name, character.Name);
            Assert.AreEqual(description, character.Characterization);
        }

        [TestMethod]
        public void CommandLine_PreservesHelpAndUnknownArgument()
        {
            var output = new StringWriter(CultureInfo.InvariantCulture);
            var originalOutput = Console.Out;
            try
            {
                Console.SetOut(output);
                var commandLine = new CommandLine();
                commandLine.AddOption('f', Strings.CommandLine_Fullscreen);
                commandLine.SetComment(Strings.CommandLine_ReportBugs);
                commandLine.ShowHelp();
                Assert.IsTrue(commandLine.Process(["unknown"]));
            }
            finally
            {
                Console.SetOut(originalOutput);
            }

            string expected = "Usage:  [-f]" + Environment.NewLine + Environment.NewLine
                + "\t-f" + new string(' ', 14) + "Run in fullscreen mode." + Environment.NewLine
                + Environment.NewLine + "Please report bugs to <robert.schneckenhaus@web.de>" + Environment.NewLine
                + "Unknown command line parameter: \"unknown\"" + Environment.NewLine;
            Assert.AreEqual(expected, output.ToString());
        }
    }
}
