using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Freeserf.Localization;
using Freeserf.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    [DoNotParallelize]
    public class PopupTextTests
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

        public static IEnumerable<object[]> ExpectedResources =>
        [
            ["Popup_ResourceAmountNotPresent", "Not Present"],
            ["Popup_ResourceAmountMinimum", "Minimum"],
            ["Popup_ResourceAmountVeryFew", "Very Few"],
            ["Popup_ResourceAmountFew", "Few"],
            ["Popup_ResourceAmountBelowAverage", "Below Average"],
            ["Popup_ResourceAmountAverage", "Average"],
            ["Popup_ResourceAmountAboveAverage", "Above Average"],
            ["Popup_ResourceAmountMuch", "Much"],
            ["Popup_ResourceAmountVeryMuch", "Very Much"],
            ["Popup_ResourceAmountPerfect", "Perfect"],
            ["Popup_GroundAnalysis", "GROUND-ANALYSIS:"],
            ["Popup_KnightLevelMinimum", "Minimum"],
            ["Popup_KnightLevelWeak", "Weak"],
            ["Popup_KnightLevelMedium", "Medium"],
            ["Popup_KnightLevelGood", "Good"],
            ["Popup_KnightLevelFull", "Full"],
            ["Popup_KnightLevelError", "ERROR"],
            ["Popup_QuitQuestion", "   Do you want\n     to quit\n   this game?"],
            ["Popup_QuitChoices", "  Yes        No"],
            ["Popup_UnsavedQuitQuestion", "The game has not\n   been saved\n   recently.\n    Are you\n     sure?"],
            ["Popup_UnsavedQuitChoices", "  Save  Yes  No"],
            ["Popup_FastMapclick", "Fast\nMapclick"],
            ["Popup_FastBuilding", "Fast\nBuilding"],
            ["Popup_MessagesAll", "All"],
            ["Popup_MessagesMost", "Most"],
            ["Popup_MessagesFew", "Few"],
            ["Popup_MessagesNone", "None"],
            ["Popup_Messages", "Messages"],
            ["Popup_PathwayScrolling", "Pathway-\nScrolling"],
            ["Popup_InvertScrolling", "Invert\nScrolling"],
            ["Popup_HideMouseWhileScrolling", "Hide mouse\nwhile scr."],
            ["Popup_ResetMouseAfterScrolling", "Reset mouse\nafter scr."],
            ["Popup_Music", "Music"],
            ["Popup_SoundEffects", "Sound\neffects"],
            ["Popup_Volume", "Volume"],
            ["Popup_Fullscreen", "Fullscreen"],
            ["Popup_Percentage", "{0}%"],
            ["Popup_MiningOutput", "Mining output:"],
            ["Popup_OrderedBuilding", "Ordered\nBuilding"],
            ["Popup_Defenders", "Defenders:"],
            ["Popup_State", "State:"],
            ["Popup_TransportInfo", "Transport Info:"],
            ["Popup_BuildingStock", "Stock of\nthis building:"],
            ["Popup_MissionWon", "CONGRATULATIONS.\n  YOUR ENEMIES\n  RESIGN. YOU\n HAVE WON THIS\n    MISSION."],
            ["Popup_MissionLost", "SORRY, ONLY ONE\nOF YOUR ENEMIES\nHAS GAINED THE\n SUPERIORITY IN\n  THIS MISSION"],
            ["Popup_TutorialCompleted", "   WELL DONE.\n YOU COMPLETED\n THIS TUTORIAL\n     GAME."],
            ["Popup_BlueVictory", "    THE BLUE\n SETTLERS HAVE\n   GAINED THE\n  SUPERIORITY"],
            ["Popup_RedVictory", "    THE RED\n SETTLERS HAVE\n   GAINED THE\n  SUPERIORITY"],
            ["Popup_VioletVictory", "  THE VIOLET\n SETTLERS HAVE\n   GAINED THE\n  SUPERIORITY"],
            ["Popup_YellowVictory", "   THE YELLOW\n SETTLERS HAVE\n   GAINED THE\n  SUPERIORITY"],
            ["Popup_TutorialOrder1", "Build a guard hut and a guard tower and a fortress."],
            ["Popup_TutorialOrder2", "Produce five planks and five stones."],
            ["Popup_TutorialOrder3", "Produce five fish and five meat and five bread."],
            ["Popup_TutorialOrder4", "Smelt five steel bars and five gold bars."],
            ["Popup_TutorialOrder5", "Make ten weapons and ten tools."],
            ["Popup_TutorialOrder6", "Conquer all the land of the enemy."],
            ["Popup_TutorialTitle", "   TUTORIAL {0}"],
            ["Popup_TutorialOrders", "    ORDERS:"],
            ["Popup_Demolish", "    Demolish:\n   Click here\n   if you are\n      sure"],
            ["Popup_SaveGame", "Save  Game"],
            ["Popup_SaveSuccess", "Game was saved\nsuccessfully."],
            ["Popup_SaveFail", "Game could not\nbe saved."],
            ["Popup_LoadSuccess", "Game was loaded\nsuccessfully."],
            ["Popup_LoadFail", "Game could not\nbe loaded."]
        ];

        [TestMethod]
        [DynamicData(nameof(ExpectedResources))]
        public void EnglishText_PreservesEveryPopupResource(string key, string expected)
        {
            var property = typeof(Strings).GetProperty(key, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.IsNotNull(property, $"Missing generated accessor: {key}");
            Assert.AreEqual(expected, property.GetValue(null), key);
        }

        [TestMethod]
        public void ExpectedResources_CoverEveryPopupAccessorExactlyOnce()
        {
            var expected = ExpectedResources.Select(row => (string)row[0]).ToArray();
            var actual = typeof(Strings).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(property => property.Name.StartsWith("Popup_", StringComparison.Ordinal))
                .Select(property => property.Name).ToArray();

            CollectionAssert.AreEquivalent(expected, actual);
            Assert.AreEqual(expected.Length, expected.Distinct().Count());
        }

        [TestMethod]
        [DataRow(0u, "Not Present")]
        [DataRow(1u, "Minimum")]
        [DataRow(99u, "Minimum")]
        [DataRow(100u, "Very Few")]
        [DataRow(179u, "Very Few")]
        [DataRow(180u, "Few")]
        [DataRow(239u, "Few")]
        [DataRow(240u, "Below Average")]
        [DataRow(299u, "Below Average")]
        [DataRow(300u, "Average")]
        [DataRow(399u, "Average")]
        [DataRow(400u, "Above Average")]
        [DataRow(499u, "Above Average")]
        [DataRow(500u, "Much")]
        [DataRow(599u, "Much")]
        [DataRow(600u, "Very Much")]
        [DataRow(799u, "Very Much")]
        [DataRow(800u, "Perfect")]
        [DataRow(uint.MaxValue, "Perfect")]
        public void ResourceAmount_PreservesEveryBoundary(uint amount, string expected)
        {
            Assert.AreEqual(expected, PopupBox.GetResourceAmountText(amount));
        }

        [TestMethod]
        public void ChoiceLabels_PreserveFixedClickColumns()
        {
            Assert.AreEqual(15, Strings.Popup_QuitChoices.Length);
            Assert.AreEqual("Yes", Strings.Popup_QuitChoices.Substring(16 / 8, 24 / 8));
            Assert.AreEqual("No", Strings.Popup_QuitChoices.Substring(104 / 8, 16 / 8));
            Assert.AreEqual(15, Strings.Popup_UnsavedQuitChoices.Length);
            Assert.AreEqual("Save", Strings.Popup_UnsavedQuitChoices.Substring(16 / 8, 32 / 8));
            Assert.AreEqual("Yes", Strings.Popup_UnsavedQuitChoices.Substring(64 / 8, 24 / 8));
            Assert.AreEqual("No", Strings.Popup_UnsavedQuitChoices.Substring(104 / 8, 16 / 8));
        }

        [TestMethod]
        public void TextLayout_PreservesIrregularPositionsAndLeadingSpaces()
        {
            CollectionAssert.AreEqual(
                new[]
                {
                    (8, 13, "CONGRATULATIONS."),
                    (8, 25, "  YOUR ENEMIES"),
                    (8, 35, "  RESIGN. YOU"),
                    (8, 45, " HAVE WON THIS"),
                    (8, 55, "    MISSION.")
                },
                PopupBox.GetTextLayout(Strings.Popup_MissionWon,
                    (8, 13), (8, 25), (8, 35), (8, 45), (8, 55)).ToArray());
            CollectionAssert.AreEqual(
                new[] { (8, 19, "    Demolish:"), (8, 39, "   Click here"), (8, 77, "   if you are"), (8, 95, "      sure") },
                PopupBox.GetTextLayout(Strings.Popup_Demolish, (8, 19), (8, 39), (8, 77), (8, 95)).ToArray());
            CollectionAssert.AreEqual(
                new[] { (11, 33, "Game was loaded"), (22, 74, "successfully.") },
                PopupBox.GetTextLayout(Strings.Popup_LoadSuccess, (11, 33), (22, 74)).ToArray());
        }

        [TestMethod]
        [DataRow("en-US")]
        [DataRow("de-DE")]
        [DataRow("fr-FR")]
        public void MissingTranslation_ResolvesAllPopupEnglishText(string culture)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

            foreach (var row in ExpectedResources)
                Assert.AreEqual((string)row[1], Strings.ResourceManager.GetString((string)row[0]), (string)row[0]);

            Assert.AreEqual("100%", PopupBox.FormatPercentage(100));
            Assert.AreEqual("1599%", PopupBox.FormatPercentage(1599));
            Assert.AreEqual("   TUTORIAL 6", PopupBox.FormatTutorialTitle(6));
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataRow(6)]
        public void TutorialTitle_PreservesPaddingAndLevel(int level)
        {
            Assert.AreEqual("   TUTORIAL " + level.ToString(), PopupBox.FormatTutorialTitle(level));
        }
    }
}
