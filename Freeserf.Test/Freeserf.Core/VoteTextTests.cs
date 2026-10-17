using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Freeserf.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    [DoNotParallelize]
    public class VoteTextTests
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
        [DataRow(0u, "Player 1")]
        [DataRow(3u, "Player 4")]
        public void PlayerName_PreservesOneBasedNumbering(uint player, string expected)
        {
            Assert.AreEqual(expected, Vote.GetPlayerName(player));
        }

        [TestMethod]
        [DataRow(VoteType.Pause, "Player 3 wants\nto pause the\ngame.")]
        [DataRow(VoteType.Resume, "Player 3 wants\nto resume the\ngame.")]
        [DataRow(VoteType.GameSpeed, "Player 3 wants\nto set the game\nspeed to 7.")]
        [DataRow(VoteType.Save, "Player 3 wants\nto save the\ngame.")]
        [DataRow((VoteType)255, "Player 3 wants\nsomething.")]
        public void Question_PreservesEveryMessage(VoteType type, string expected)
        {
            Assert.AreEqual(expected.Replace('\n', ' '), Vote.GetQuestion(2, type, 7));
        }

        [TestMethod]
        [DataRow(VoteType.Pause, "You want to\npause the game.")]
        [DataRow(VoteType.Resume, "You want to\nresume the game.")]
        [DataRow(VoteType.GameSpeed, "You want to set\nthe game speed\nto 7.")]
        [DataRow(VoteType.Save, "You want to\nsave the game.")]
        [DataRow((VoteType)255, "You want\nsomething.")]
        public void OwnRequest_PreservesEveryMessage(VoteType type, string expected)
        {
            Assert.AreEqual(expected.Replace('\n', ' '), Vote.GetOwnRequest(type, 7));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(40)]
        [DataRow(-1)]
        public void SpeedMessages_PreserveParameterFormatting(int speed)
        {
            Assert.AreEqual($"Player 1 wants to set the game speed to {speed}.",
                Vote.GetQuestion(0, VoteType.GameSpeed, speed));
            Assert.AreEqual($"You want to set the game speed to {speed}.",
                Vote.GetOwnRequest(VoteType.GameSpeed, speed));
        }

        [TestMethod]
        public void AnswerLabels_PreserveEnglish()
        {
            Assert.AreEqual("Yes", Strings.Vote_Answer_Yes);
            Assert.AreEqual("No", Strings.Vote_Answer_No);
        }

        [TestMethod]
        [DataRow(VoteResult.Accepted, true, "Accepted.")]
        [DataRow(VoteResult.Accepted, false, "Accepted.")]
        [DataRow(VoteResult.Rejected, true, "Rejected.")]
        [DataRow(VoteResult.Rejected, false, "Rejected.")]
        [DataRow(VoteResult.Refused, true, "Not possible now.")]
        [DataRow(VoteResult.Refused, false, "Not possible now.")]
        [DataRow((VoteResult)255, true, "Not possible now.")]
        [DataRow((VoteResult)255, false, "Not possible now.")]
        public void FinishedStatus_TakesPrecedenceOverWaitingAndCountdown(VoteResult result, bool canAnswer, string expected)
        {
            var now = new DateTime(2026, 10, 17, 10, 0, 0, DateTimeKind.Utc);

            Assert.AreEqual(expected, VoteText.Status(result, canAnswer, now.AddSeconds(5), now));
        }

        [TestMethod]
        [DataRow(5000)]
        [DataRow(0)]
        [DataRow(-1000)]
        public void UnansweredStatus_WhenCannotAnswer_Waits(int remainingMilliseconds)
        {
            var now = new DateTime(2026, 10, 17, 10, 0, 0, DateTimeKind.Utc);

            Assert.AreEqual("Waiting...",
                VoteText.Status(null, false, now.AddMilliseconds(remainingMilliseconds), now));
        }

        [TestMethod]
        [DataRow(5000, "5 seconds left")]
        [DataRow(1001, "2 seconds left")]
        [DataRow(1000, "1 seconds left")]
        [DataRow(1, "1 seconds left")]
        [DataRow(0, "0 seconds left")]
        [DataRow(-1, "0 seconds left")]
        [DataRow(-1000, "0 seconds left")]
        public void Countdown_PreservesRoundingClampingAndWording(int remainingMilliseconds, string expected)
        {
            var now = new DateTime(2026, 10, 17, 10, 0, 0, DateTimeKind.Utc);

            Assert.AreEqual(expected,
                VoteText.Status(null, true, now.AddMilliseconds(remainingMilliseconds), now));
        }

        [TestMethod]
        [DataRow("en-US")]
        [DataRow("de-DE")]
        [DataRow("fr-FR")]
        public void MissingTranslation_ResolvesCompleteSourceCatalog(string culture)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

            var properties = typeof(Strings).GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(property => property.PropertyType == typeof(string)).ToArray();
            var sourceResources = Strings.ResourceManager.GetResourceSet(CultureInfo.GetCultureInfo("en"), true, true);

            Assert.IsNotNull(sourceResources);
            Assert.IsNotEmpty(properties);
            Assert.AreEqual(sourceResources.Cast<DictionaryEntry>().Count(), properties.Length,
                "Every catalog entry should have a generated string accessor.");

            foreach (var property in properties)
            {
                string source = sourceResources.GetString(property.Name);
                Assert.IsFalse(string.IsNullOrEmpty(source), $"Missing source resource: {property.Name}");
                Assert.AreEqual(source, property.GetValue(null), $"Fallback failed: {property.Name}");
            }

            Assert.AreEqual("Player 2 wants to set the game speed to 12.",
                Vote.GetQuestion(1, VoteType.GameSpeed, 12));
        }
    }
}
