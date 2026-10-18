using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Freeserf.Localization;
using Freeserf.Render;
using Freeserf.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    [DoNotParallelize]
    public class GermanTextTests
    {
        CultureInfo originalResourceCulture;
        CultureInfo originalCulture;
        CultureInfo originalUICulture;

        [TestInitialize]
        public void SelectGerman()
        {
            originalResourceCulture = Strings.Culture;
            originalCulture = CultureInfo.CurrentCulture;
            originalUICulture = CultureInfo.CurrentUICulture;
            Strings.Culture = CultureInfo.GetCultureInfo("de");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
        }

        [TestCleanup]
        public void RestoreCulture()
        {
            Strings.Culture = originalResourceCulture;
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }

        [TestMethod]
        public void Catalog_HasAnExplicitNonemptyGermanEntryForEverySourceKey()
        {
            var source = Strings.ResourceManager.GetResourceSet(CultureInfo.GetCultureInfo("en"), true, true);
            var german = Strings.ResourceManager.GetResourceSet(CultureInfo.GetCultureInfo("de"), true, false);
            Assert.IsNotNull(source);
            Assert.IsNotNull(german);
            CollectionAssert.AreEquivalent(source.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).ToArray(),
                german.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).ToArray());

            foreach (DictionaryEntry entry in german)
                Assert.IsFalse(string.IsNullOrWhiteSpace((string)entry.Value), (string)entry.Key);
        }

        [TestMethod]
        public void LegacyUIText_UsesOnlyActuallySupportedGlyphs()
        {
            var german = Strings.ResourceManager.GetResourceSet(CultureInfo.GetCultureInfo("de"), true, false);
            var supported = TextRenderer.ValidCharacters(TextRenderType.Legacy).Select(value => (char)value).ToHashSet();
            supported.Add(' ');
            supported.Add('\n');

            foreach (DictionaryEntry entry in german)
            {
                string key = (string)entry.Key;
                if (!key.StartsWith("Vote_", StringComparison.Ordinal) &&
                    !key.StartsWith("Notification_", StringComparison.Ordinal) &&
                    !key.StartsWith("Setup_", StringComparison.Ordinal) &&
                    !key.StartsWith("Popup_", StringComparison.Ordinal))
                    continue;

                string text = string.Format(CultureInfo.InvariantCulture, (string)entry.Value, 1, 40, 4, 4);
                foreach (char character in text)
                    Assert.IsTrue(supported.Contains(character), $"{key}: unsupported U+{(int)character:X4}");
            }
        }

        [TestMethod]
        [DataRow(VoteType.Pause, "Spieler 3 will das Spiel anhalten.", "Sie wollen das Spiel anhalten.")]
        [DataRow(VoteType.Resume, "Spieler 3 will weiterspielen.", "Sie wollen weiterspielen.")]
        [DataRow(VoteType.GameSpeed, "Spieler 3 will das Tempo auf 7 setzen.", "Sie wollen das Tempo auf 7 setzen.")]
        [DataRow(VoteType.Save, "Spieler 3 will das Spiel speichern.", "Sie wollen das Spiel speichern.")]
        [DataRow((VoteType)255, "Spieler 3 will etwas.", "Sie wollen etwas.")]
        public void Votes_TranslateWholeTemplates(VoteType type, string question, string own)
        {
            Assert.AreEqual(question, Vote.GetQuestion(2, type, 7));
            Assert.AreEqual(own, Vote.GetOwnRequest(type, 7));
        }

        [TestMethod]
        public void AllVoteVariants_FitThreeRowsWithoutLosingWords()
        {
            foreach (var type in Enum.GetValues<VoteType>().Append((VoteType)255))
            {
                for (uint player = 0; player < Game.MAX_PLAYER_COUNT; ++player)
                {
                    for (int speed = 1; speed <= Global.MAX_GAME_SPEED; ++speed)
                    {
                        string question = Vote.GetQuestion(player, type, speed);
                        string own = Vote.GetOwnRequest(type, speed);
                        Assert.AreEqual(question, string.Join(" ", VoteBox.WrapMessage(question)));
                        Assert.AreEqual(own, string.Join(" ", VoteBox.WrapMessage(own)));
                    }
                }
            }

            CollectionAssert.AreEqual(new[] { "Spieler 1 will", "das Tempo auf 40", "setzen." },
                VoteBox.WrapMessage(Vote.GetQuestion(0, VoteType.GameSpeed, 40)));
        }

        [TestMethod]
        public void VoteStatus_UsesGermanAndFitsTheSingleStatusRow()
        {
            var now = new DateTime(2026, 10, 17, 13, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual("Angenommen.", VoteText.Status(VoteResult.Accepted, true, now, now));
            Assert.AreEqual("Abgelehnt.", VoteText.Status(VoteResult.Rejected, true, now, now));
            Assert.AreEqual("Jetzt unmöglich.", VoteText.Status(VoteResult.Refused, true, now, now));
            Assert.AreEqual("Bitte warten...", VoteText.Status(null, false, now, now));
            Assert.AreEqual("Noch 1 Sek.", VoteText.Status(null, true, now.AddSeconds(1), now));

            foreach (var result in new VoteResult?[] { null, VoteResult.Accepted, VoteResult.Rejected, VoteResult.Refused })
                foreach (bool canAnswer in new[] { true, false })
                    Assert.IsLessThanOrEqualTo(16, VoteText.Status(result, canAnswer, now.AddSeconds(60), now).Length);
        }

        public static IEnumerable<object[]> Notifications =>
        [
            [Notification.Type.UnderAttack, "Ihre Siedlung wird angegriffen"],
            [Notification.Type.LoseFight, "Ihre Ritter haben hier den Kampf verloren"],
            [Notification.Type.WinFight, "Sie haben einen Sieg errungen"],
            [Notification.Type.MineEmpty, "Dieses Bergwerk liefert keine Rohstoffe mehr"],
            [Notification.Type.CallToLocation, "Sie wollten an diese Stelle gerufen werden"],
            [Notification.Type.KnightOccupied, "Ein Ritter hat ein neues Gebäude besetzt"],
            [Notification.Type.NewStock, "Eine neue Lagerhalle ist fertig"],
            [Notification.Type.LostLand, "Ein Feindgebäude hat Sie Land gekostet"],
            [Notification.Type.EmergencyActive, "Das Notprogramm wurde aktiviert"],
            [Notification.Type.EmergencyNeutral, "Das Notprogramm wurde beendet"],
            [Notification.Type.FoundGold, "Ein Geologe hat Gold gefunden"],
            [Notification.Type.FoundIron, "Ein Geologe hat Eisen gefunden"],
            [Notification.Type.FoundCoal, "Ein Geologe hat Kohle gefunden"],
            [Notification.Type.FoundStone, "Ein Geologe hat Granit gefunden"],
            [Notification.Type.CallToMenu, "Sie wollten zu diesem Menü gerufen werden"],
            [Notification.Type.ThirtyMinutesSinceSave, "Sie haben vor 30 Minuten zuletzt gespeichert"],
            [Notification.Type.OneHourSinceSave, "Sie haben vor einer Stunde zuletzt gespeichert"],
            [Notification.Type.CallToStock, "Sie wollten zu diesem Lager gerufen werden"],
            [Notification.Type.PlayerLeft, "Dieser Spieler hat das Spiel verlassen"],
            [Notification.Type.PlayerRejoined, "Dieser Spieler ist wieder im Spiel"]
        ];

        [TestMethod]
        [DynamicData(nameof(Notifications))]
        public void Notifications_TranslateAndFitTheirMessageArea(Notification.Type type, string expected)
        {
            var view = NotificationBox.GetView(type);
            Assert.AreEqual(expected, view.Text);
            Assert.AreEqual(expected, string.Join(" ", NotificationBox.WrapMessage(view.Text)));
        }

        [TestMethod]
        public void TutorialOrders_FitAndUseGermanBuildingAndResourceTerms()
        {
            string[] orders =
            [
                Strings.Popup_TutorialOrder1, Strings.Popup_TutorialOrder2, Strings.Popup_TutorialOrder3,
                Strings.Popup_TutorialOrder4, Strings.Popup_TutorialOrder5, Strings.Popup_TutorialOrder6
            ];
            for (int index = 0; index < orders.Length; ++index)
            {
                var layout = PopupBox.GetParagraphLayout(orders[index], 8, 55, 9, $"German training {index + 1}").ToArray();
                Assert.AreEqual(orders[index], string.Join(" ", layout.Select(line => line.Text)));
                Assert.IsTrue(layout.All(line => line.X + line.Text.Length * 8 <= 136 && line.Y + 8 <= 143));
            }
            StringAssert.Contains(orders[0], "Wachhütte");
            StringAssert.Contains(orders[0], "Wachturm");
            StringAssert.Contains(orders[0], "Wachburg");
            StringAssert.Contains(orders[2], "je fünf Einheiten");
            StringAssert.Contains(orders[4], "je zehn");
        }

        [TestMethod]
        public void QuitParagraphs_FitBeforeUnchangedChoiceRows()
        {
            var quit = PopupBox.GetParagraphLayout(Strings.Popup_QuitQuestion, 8, 19, 3, "German quit").ToArray();
            var unsaved = PopupBox.GetParagraphLayout(Strings.Popup_UnsavedQuitQuestion, 8, 79, 5, "German unsaved quit").ToArray();
            Assert.IsTrue(quit.All(line => line.Y + 8 < 54 && line.X + line.Text.Length * 8 <= 136));
            Assert.IsTrue(unsaved.All(line => line.Y + 8 < 134 && line.X + line.Text.Length * 8 <= 136));
            Assert.AreEqual("Wollen Sie dieses Spiel beenden?", string.Join(" ", quit.Select(line => line.Text)));
        }

        [TestMethod]
        public void ChoiceLabels_UseShortGermanTranslations()
        {
            Assert.AreEqual("Ja", Strings.Popup_QuitYes);
            Assert.AreEqual("Nein", Strings.Popup_QuitNo);
            Assert.AreEqual("Sich.", Strings.Popup_QuitSave);
            Assert.AreEqual("Ja", Strings.Vote_Answer_Yes);
            Assert.AreEqual("Nein", Strings.Vote_Answer_No);
        }

        [TestMethod]
        public void IrregularFixedLayouts_PreserveRowCountsAndFitActualCoordinates()
        {
            CheckCenteredParagraph(Strings.Popup_MissionWon, 13, 25, 35, 45, 55);
            CheckCenteredParagraph(Strings.Popup_MissionLost, 15, 25, 35, 45, 55);
            CheckCenteredParagraph(Strings.Popup_TutorialCompleted, 13, 25, 35, 45);
            foreach (string text in new[] { Strings.Popup_BlueVictory, Strings.Popup_RedVictory,
                Strings.Popup_VioletVictory, Strings.Popup_YellowVictory })
                CheckCenteredParagraph(text, 15, 25, 35, 45);
            CheckCentered(Strings.Popup_Demolish, 19, 39, 77, 95);
            CheckCentered(Strings.Popup_SaveSuccess, 33, 74);
            CheckCentered(Strings.Popup_SaveFail, 33, 74);
            CheckCentered(Strings.Popup_LoadSuccess, 33, 74);
            CheckCentered(Strings.Popup_LoadFail, 33, 74);
        }

        static void CheckCenteredParagraph(string text, params int[] rows)
        {
            foreach (var line in PopupBox.GetCenteredParagraphLayout(text, rows))
            {
                Assert.IsGreaterThanOrEqualTo(8, line.X);
                Assert.IsLessThanOrEqualTo(136, line.X + line.Text.Length * 8);
                Assert.AreEqual(144, line.X * 2 + line.Text.Length * 8);
                Assert.IsLessThanOrEqualTo(65, line.Y + 8);
            }
        }

        static void CheckCentered(string text, params int[] rows)
        {
            foreach (var line in PopupBox.GetCenteredTextLayout(text, rows))
            {
                Assert.IsGreaterThanOrEqualTo(8, line.X);
                Assert.IsLessThanOrEqualTo(136, line.X + line.Text.Length * 8);
                Assert.AreEqual(144, line.X * 2 + line.Text.Length * 8);
            }
        }

        [TestMethod]
        public void OptionsAndBuildingLabels_FitTheirFixedRows()
        {
            foreach (string text in new[] { Strings.Popup_FastMapclick, Strings.Popup_FastBuilding,
                Strings.Popup_PathwayScrolling, Strings.Popup_InvertScrolling, Strings.Popup_HideMouseWhileScrolling,
                Strings.Popup_ResetMouseAfterScrolling, Strings.Popup_SoundEffects })
                Assert.IsTrue(PopupBox.GetWrappedLabelLayout(text, 16, 12, 20, 29)
                    .All(line => line.X + line.Text.Length * 8 <= 112));
            foreach (string text in new[] { Strings.Popup_OrderedBuilding, Strings.Popup_BuildingStock })
                Assert.IsTrue(PopupBox.GetWrappedLabelLayout(text, 16, 15, 13, 23)
                    .All(line => line.X + line.Text.Length * 8 <= 136));
        }

        [TestMethod]
        [DataRow("Popup_Music", 12)]
        [DataRow("Popup_Volume", 7)]
        [DataRow("Popup_Fullscreen", 12)]
        [DataRow("Popup_Language", 15)]
        [DataRow("Popup_LanguageSystem", 12)]
        [DataRow("Popup_LanguageEnglish", 12)]
        [DataRow("Popup_LanguageGerman", 12)]
        [DataRow("Popup_Messages", 10)]
        [DataRow("Popup_MessagesAll", 5)]
        [DataRow("Popup_MessagesMost", 5)]
        [DataRow("Popup_MessagesFew", 5)]
        [DataRow("Popup_MessagesNone", 5)]
        [DataRow("Popup_GroundAnalysis", 16)]
        [DataRow("Popup_MiningOutput", 15)]
        [DataRow("Popup_Defenders", 13)]
        [DataRow("Popup_State", 14)]
        [DataRow("Popup_TransportInfo", 16)]
        [DataRow("Popup_SaveGame", 13)]
        [DataRow("Popup_TutorialOrders", 16)]
        public void FixedLabels_RespectSpaceReservedForAdjacentControls(string key, int columns)
        {
            string value = Strings.ResourceManager.GetString(key, Strings.Culture);
            Assert.IsNotNull(value);
            Assert.IsLessThanOrEqualTo(columns, value.Length, key);
        }

        [TestMethod]
        public void AmountAndOccupationLabels_FitBesideTheirIconsAndButtons()
        {
            var german = Strings.ResourceManager.GetResourceSet(Strings.Culture, true, false);
            foreach (DictionaryEntry entry in german)
            {
                string key = (string)entry.Key;
                if (key.StartsWith("Popup_ResourceAmount", StringComparison.Ordinal))
                    Assert.IsLessThanOrEqualTo(13, ((string)entry.Value).Length, key);
                if (key.StartsWith("Popup_KnightLevel", StringComparison.Ordinal))
                    Assert.IsLessThanOrEqualTo(8, ((string)entry.Value).Length, key);
            }
        }

        [TestMethod]
        public void SetupLabels_FitBesideNumericValuesAndWithinTheSetupBox()
        {
            int labelX = GameInitBox.GetTextX(GameInitBox.HeaderTextColumn);
            int valueX = GameInitBox.GetTextX(GameInitBox.ValueTextColumn);
            foreach (string text in new[] { Strings.Setup_MapSizeLabel, Strings.Setup_MissionLabel, Strings.Setup_TutorialLabel })
                Assert.IsLessThanOrEqualTo(valueX, labelX + 8 + (text.Length - 1) * GameInitBox.TextCharacterAdvance);
            Assert.IsLessThanOrEqualTo(GameInitBox.SameValuesCheckBoxX,
                GameInitBox.ServerValuesCheckBoxX + 18 + Strings.Setup_ServerValues.Length * 8);
        }

        [TestMethod]
        public void Presets_TranslateTutorialsAndCharactersWithoutChangingMissionIdentity()
        {
            var source = Strings.ResourceManager.GetResourceSet(CultureInfo.GetCultureInfo("en"), true, true);
            var german = Strings.ResourceManager.GetResourceSet(Strings.Culture, true, false);
            foreach (DictionaryEntry entry in source)
            {
                string key = (string)entry.Key;
                if (key.StartsWith("Mission_", StringComparison.Ordinal) && !key.StartsWith("Mission_Tutorial", StringComparison.Ordinal))
                    Assert.AreEqual(entry.Value, german.GetString(key), key);
            }

            for (uint index = 0; index < GameInfo.GetTutorialCount(); ++index)
            {
                var tutorial = GameInfo.GetTutorial(index);
                Assert.AreEqual($"Training {index + 1}", tutorial.Name);
                Assert.AreEqual(index + 1, tutorial.Level);
            }
            Assert.AreEqual("Kumpy Einfinger", PlayerInfo.Characters[2].Name);
            Assert.AreEqual("Homen Doppelhorn", PlayerInfo.Characters[9].Name);
            StringAssert.Contains(PlayerInfo.Characters[7].Characterization, "Sichern Sie Ihre Lager");
        }

        [TestMethod]
        [DataRow(false, false, false, "host | nicht gefunden")]
        [DataRow(true, false, false, "Name | host | 2/4")]
        [DataRow(true, false, true, "Name | host | 2/4 | gespeichert")]
        [DataRow(true, true, false, "Name | host | 2/4 | läuft")]
        public void ServerStatuses_TranslateWithoutChangingNetworkValues(bool online, bool inGame, bool saved, string expected)
        {
            var server = new ServerInfo
            {
                Name = "Name", HostName = "host", SavedAddress = "host", Online = online,
                CurrentPlayers = 2, MaxPlayers = 4, InGame = inGame, AllowsRejoin = saved
            };
            Assert.AreEqual(expected, server.ToString());
        }

        [TestMethod]
        public void CommandLine_TranslatesDescriptionsButPreservesAcceptedTokens()
        {
            Assert.AreEqual("Aufruf: {0}", Strings.CommandLine_Usage);
            Assert.AreEqual("Im Vollbildmodus starten.", Strings.CommandLine_Fullscreen);
            Assert.AreEqual("host:CLIENTS[:AI] | join:ADDRESS", Strings.CommandLine_MultiplayerModes);
            StringAssert.Contains(Strings.CommandLine_MultiplayerTest, "host:CLIENTS[:AI]");
            StringAssert.Contains(Strings.CommandLine_MultiplayerTest, "join:ADDRESS");
            StringAssert.Contains(Strings.CommandLine_IncludeLogType, "-x");
            StringAssert.Contains(Strings.CommandLine_ExcludeLogType, "-o");
        }

    }
}
