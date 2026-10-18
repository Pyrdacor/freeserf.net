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
            ["Popup_QuitQuestion", "Do you want to quit this game?"],
            ["Popup_QuitNo", "No"],
            ["Popup_QuitSave", "Save"],
            ["Popup_QuitYes", "Yes"],
            ["Popup_UnsavedQuitQuestion", "The game has not been saved recently. Are you sure?"],
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
            ["Popup_Language", "Language"],
            ["Popup_LanguageSystem", "System"],
            ["Popup_LanguageEnglish", "English"],
            ["Popup_LanguageGerman", "German"],
            ["Popup_Percentage", "{0}%"],
            ["Popup_MiningOutput", "Mining output:"],
            ["Popup_OrderedBuilding", "Ordered\nBuilding"],
            ["Popup_Defenders", "Defenders:"],
            ["Popup_State", "State:"],
            ["Popup_TransportInfo", "Transport Info:"],
            ["Popup_BuildingStock", "Stock of\nthis building:"],
            ["Popup_MissionWon", "CONGRATULATIONS.\nYOUR ENEMIES\nRESIGN. YOU\nHAVE WON THIS\nMISSION."],
            ["Popup_MissionLost", "SORRY, ONLY ONE\nOF YOUR ENEMIES\nHAS GAINED THE\nSUPERIORITY IN\nTHIS MISSION"],
            ["Popup_TutorialCompleted", "WELL DONE.\nYOU COMPLETED\nTHIS TUTORIAL\nGAME."],
            ["Popup_BlueVictory", "THE BLUE\nSETTLERS HAVE\nGAINED THE\nSUPERIORITY"],
            ["Popup_RedVictory", "THE RED\nSETTLERS HAVE\nGAINED THE\nSUPERIORITY"],
            ["Popup_VioletVictory", "THE VIOLET\nSETTLERS HAVE\nGAINED THE\nSUPERIORITY"],
            ["Popup_YellowVictory", "THE YELLOW\nSETTLERS HAVE\nGAINED THE\nSUPERIORITY"],
            ["Popup_TutorialOrder1", "Build a guard hut and a guard tower and a fortress."],
            ["Popup_TutorialOrder2", "Produce five planks and five stones."],
            ["Popup_TutorialOrder3", "Produce five fish and five meat and five bread."],
            ["Popup_TutorialOrder4", "Smelt five steel bars and five gold bars."],
            ["Popup_TutorialOrder5", "Make ten weapons and ten tools."],
            ["Popup_TutorialOrder6", "Conquer all the land of the enemy."],
            ["Popup_TutorialTitle", "TUTORIAL {0}"],
            ["Popup_TutorialOrders", "ORDERS:"],
            ["Popup_Demolish", "Demolish:\nClick here\nif you are\nsure"],
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
        public void ChoiceLabels_AreSeparateResources()
        {
            Assert.AreEqual("Yes", Strings.Popup_QuitYes);
            Assert.AreEqual("No", Strings.Popup_QuitNo);
            Assert.AreEqual("Save", Strings.Popup_QuitSave);
        }

        [TestMethod]
        public void CenteredTextLayout_PreservesIrregularRowsWithoutPadding()
        {
            CollectionAssert.AreEqual(
                new[]
                {
                    (8, 13, "CONGRATULATIONS."),
                    (24, 25, "YOUR ENEMIES"),
                    (28, 35, "RESIGN. YOU"),
                    (20, 45, "HAVE WON THIS"),
                    (40, 55, "MISSION.")
                },
                PopupBox.GetCenteredTextLayout(Strings.Popup_MissionWon, 13, 25, 35, 45, 55).ToArray());
            CollectionAssert.AreEqual(
                new[] { (36, 19, "Demolish:"), (32, 39, "Click here"), (32, 77, "if you are"), (56, 95, "sure") },
                PopupBox.GetCenteredTextLayout(Strings.Popup_Demolish, 19, 39, 77, 95).ToArray());
            CollectionAssert.AreEqual(
                new[] { (12, 33, "Game was loaded"), (20, 74, "successfully.") },
                PopupBox.GetCenteredTextLayout(Strings.Popup_LoadSuccess, 33, 74).ToArray());
            CollectionAssert.AreEqual(new[] { (16, 20, "  Left aligned") },
                PopupBox.GetTextLayout("  Left aligned", (16, 20)).ToArray());
        }

        [TestMethod]
        [DataRow("en")]
        [DataRow("de")]
        [DataRow("fr-FR")]
        public void CenteredResources_HaveNoPaddingAndFitTheContentArea(string language)
        {
            var culture = CultureInfo.GetCultureInfo(language);
            (string Key, int[] Rows)[] resources =
            [
                ("Popup_MissionWon", [13, 25, 35, 45, 55]),
                ("Popup_MissionLost", [15, 25, 35, 45, 55]),
                ("Popup_TutorialCompleted", [13, 25, 35, 45]),
                ("Popup_BlueVictory", [15, 25, 35, 45]),
                ("Popup_RedVictory", [15, 25, 35, 45]),
                ("Popup_VioletVictory", [15, 25, 35, 45]),
                ("Popup_YellowVictory", [15, 25, 35, 45]),
                ("Popup_Demolish", [19, 39, 77, 95]),
                ("Popup_SaveSuccess", [33, 74]),
                ("Popup_SaveFail", [33, 74]),
                ("Popup_LoadSuccess", [33, 74]),
                ("Popup_LoadFail", [33, 74]),
                ("Popup_TutorialTitle", [15]),
                ("Popup_TutorialOrders", [35])
            ];
            foreach (var (key, rows) in resources)
            {
                string template = Strings.ResourceManager.GetString(key, culture);
                Assert.IsNotNull(template, key);
                for (int level = 1; level <= 6; ++level)
                {
                    string text = string.Format(culture, template, level);
                    var layout = PopupBox.GetCenteredTextLayout(text, rows).ToArray();
                    CollectionAssert.AreEqual(rows, layout.Select(line => line.Y).ToArray(), key);
                    CollectionAssert.AreEqual(text.Split('\n'), layout.Select(line => line.Text).ToArray(), key);
                    foreach (var line in layout)
                    {
                        Assert.AreEqual(line.Text.Trim(), line.Text, key);
                        Assert.IsGreaterThanOrEqualTo(8, line.X, key);
                        Assert.IsLessThanOrEqualTo(136, line.X + line.Text.Length * 8, key);
                        Assert.AreEqual(144, line.X * 2 + line.Text.Length * 8, key);
                    }
                }
            }
        }

        [TestMethod]
        public void CenteredTextLayout_RejectsOverflowAndIncorrectRowCounts()
        {
            Assert.ThrowsExactly<ExceptionFreeserf>(() =>
                PopupBox.GetCenteredTextLayout(new string('X', 17), 15).ToArray());
            Assert.ThrowsExactly<ExceptionFreeserf>(() =>
                PopupBox.GetCenteredTextLayout("First\nSecond", 15).ToArray());
            CollectionAssert.AreEqual(new[] { (8, 15, new string('X', 16)), (72, 25, "") },
                PopupBox.GetCenteredTextLayout(new string('X', 16) + "\n", 15, 25).ToArray());
        }

        [TestMethod]
        [DataRow("en-US")]
        [DataRow("de-DE")]
        [DataRow("fr-FR")]
        public void Catalog_ResolvesPopupTranslationOrEnglishFallback(string culture)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

            var german = Strings.ResourceManager.GetResourceSet(CultureInfo.GetCultureInfo("de"), true, false);
            foreach (var row in ExpectedResources)
            {
                string key = (string)row[0];
                string expected = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de"
                    ? german?.GetString(key) ?? (string)row[1]
                    : (string)row[1];
                Assert.AreEqual(expected, Strings.ResourceManager.GetString(key), key);
            }

            Assert.AreEqual("100%", PopupBox.FormatPercentage(100));
            Assert.AreEqual("1599%", PopupBox.FormatPercentage(1599));
            Assert.AreEqual(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de"
                ? "TRAINING 6" : "TUTORIAL 6", PopupBox.FormatTutorialTitle(6));
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataRow(6)]
        public void TutorialTitle_PreservesLevelWithoutPadding(int level)
        {
            Assert.AreEqual("TUTORIAL " + level.ToString(), PopupBox.FormatTutorialTitle(level));
        }
    }
}
