using System;
using System.Globalization;
using System.Linq;
using Freeserf.Localization;
using Freeserf.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    [DoNotParallelize]
    public class MessageTextTests
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
        [DataRow("", "")]
        [DataRow("one two three", "one two|three")]
        [DataRow("1234567 8", "1234567|8")]
        [DataRow("1234567", "1234567")]
        [DataRow("12345678", "1234567|8")]
        [DataRow("12345678901234", "1234567|8901234")]
        [DataRow("a 123456789 b", "a|1234567|89 b")]
        [DataRow("  one \t two  ", "one two")]
        [DataRow("one\ntwo", "one|two")]
        [DataRow("one\r\ntwo\rthree", "one|two|three")]
        [DataRow("one\n\ntwo", "one||two")]
        [DataRow("\none\n", "|one|")]
        [DataRow("\u00e4\u00f6\u00fc \u00e9\u00e8\u00e0", "\u00e4\u00f6\u00fc \u00e9\u00e8\u00e0")]
        public void Wrap_HandlesBoundariesWhitespaceAndExplicitBreaks(string text, string expected)
        {
            var lines = MessageText.Wrap(text, 7, 10, "test");
            Assert.AreEqual(expected, string.Join("|", lines));
            Assert.IsTrue(lines.All(line => line.Length <= 7));
        }

        [TestMethod]
        public void EmptyMessage_HasNoLines()
        {
            Assert.IsEmpty(MessageText.Wrap("", 16, 1, "empty"));
        }

        [TestMethod]
        [DataRow(0, 3)]
        [DataRow(-1, 3)]
        [DataRow(16, 0)]
        [DataRow(16, -1)]
        public void InvalidBounds_AreRejected(int columns, int maxLines)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => MessageText.Wrap("text", columns, maxLines, "test"));
        }

        [TestMethod]
        public void NullMessage_IsRejected()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => MessageText.Wrap(null, 16, 3, "test"));
        }

        [TestMethod]
        public void Overflow_ReportsContextAndRequiredLayoutInsteadOfTruncating()
        {
            var error = Assert.ThrowsExactly<ExceptionFreeserf>(() => MessageText.Wrap("one two three", 7, 1, "example"));
            Assert.AreEqual(ErrorSystemType.UI, error.System);
            StringAssert.Contains(error.Description, "example");
            StringAssert.Contains(error.Description, "needs 2 lines");
            StringAssert.Contains(error.Description, "only 1 fit");
            StringAssert.Contains(error.Description, "explicit layout change");

            CollectionAssert.AreEqual(new[] { "one two", "three" }, MessageText.Wrap("one two three", 7, 2, "example"));
        }

        [TestMethod]
        public void EveryVoteVariant_FitsItsActualMessageArea()
        {
            foreach (var type in Enum.GetValues<VoteType>().Append((VoteType)255))
            {
                for (uint player = 0; player < Game.MAX_PLAYER_COUNT; ++player)
                {
                    for (int speed = 1; speed <= Global.MAX_GAME_SPEED; ++speed)
                    {
                        AssertFits(VoteBox.WrapMessage(Vote.GetQuestion(player, type, speed)), 3);
                        AssertFits(VoteBox.WrapMessage(Vote.GetOwnRequest(type, speed)), 3);
                    }
                }
            }

            CollectionAssert.AreEqual(
                new[] { "Player 1 wants", "to set the game", "speed to 10." },
                VoteBox.WrapMessage(Vote.GetQuestion(0, VoteType.GameSpeed, 10)));
        }

        [TestMethod]
        public void VoteAndNotification_RejectOverflow()
        {
            Assert.ThrowsExactly<ExceptionFreeserf>(() => VoteBox.WrapMessage(new string('x', 49)));
            Assert.ThrowsExactly<ExceptionFreeserf>(() => NotificationBox.WrapMessage(new string('x', 81)));
        }

        [TestMethod]
        public void EveryTutorialOrder_PreservesExistingWordWrap()
        {
            string[] orders =
            [
                Strings.Popup_TutorialOrder1, Strings.Popup_TutorialOrder2, Strings.Popup_TutorialOrder3,
                Strings.Popup_TutorialOrder4, Strings.Popup_TutorialOrder5, Strings.Popup_TutorialOrder6
            ];
            string[][] expected =
            [
                ["Build a guard", "hut and a guard", "tower and a", "fortress."],
                ["Produce five", "planks and five", "stones."],
                ["Produce five", "fish and five", "meat and five", "bread."],
                ["Smelt five steel", "bars and five", "gold bars."],
                ["Make ten weapons", "and ten tools."],
                ["Conquer all the", "land of the", "enemy."]
            ];

            for (int order = 0; order < orders.Length; ++order)
            {
                var layout = PopupBox.GetParagraphLayout(orders[order], 8, 55, 9, "tutorial orders").ToArray();
                CollectionAssert.AreEqual(expected[order], layout.Select(line => line.Text).ToArray());
                for (int line = 0; line < layout.Length; ++line)
                {
                    Assert.AreEqual(8, layout[line].X);
                    Assert.AreEqual(55 + line * 10, layout[line].Y);
                    Assert.IsLessThanOrEqualTo(143, layout[line].Y + 8);
                }
            }
        }

        [TestMethod]
        public void QuitParagraphs_WrapWithoutChangingChoiceRows()
        {
            var quit = PopupBox.GetParagraphLayout(Strings.Popup_QuitQuestion, 8, 19, 3, "quit").ToArray();
            var unsaved = PopupBox.GetParagraphLayout(Strings.Popup_UnsavedQuitQuestion, 8, 79, 5, "unsaved quit").ToArray();
            CollectionAssert.AreEqual(new[] { "Do you want to", "quit this game?" },
                quit.Select(line => line.Text).ToArray());
            CollectionAssert.AreEqual(new[] { "The game has not", "been saved", "recently. Are", "you sure?" },
                unsaved.Select(line => line.Text).ToArray());
            CollectionAssert.AreEqual(new[] { 19, 29 }, quit.Select(line => line.Y).ToArray());
            CollectionAssert.AreEqual(new[] { 79, 89, 99, 109 }, unsaved.Select(line => line.Y).ToArray());
            Assert.IsTrue(quit.All(line => line.X == 8 && line.X + line.Text.Length * 8 <= 136 && line.Y + 8 < 54));
            Assert.IsTrue(unsaved.All(line => line.X == 8 && line.X + line.Text.Length * 8 <= 136 && line.Y + 8 < 134));
            Assert.IsTrue(PopupBox.GetQuitChoices(false).All(choice => choice.Y == 54));
            Assert.IsTrue(PopupBox.GetQuitChoices(true).All(choice => choice.Y == 134));
        }

        [TestMethod]
        public void PopupParagraphAndFixedLayout_RejectOverflow()
        {
            Assert.ThrowsExactly<ExceptionFreeserf>(() =>
                PopupBox.GetParagraphLayout(new string('x', 145), 8, 55, 9, "tutorial").ToArray());
            Assert.ThrowsExactly<ExceptionFreeserf>(() =>
                PopupBox.GetTextLayout("a\nb", (8, 19)).ToArray());
            Assert.ThrowsExactly<ExceptionFreeserf>(() =>
                PopupBox.GetTextLayout("a", (8, 19), (8, 29)).ToArray());
        }

        static void AssertFits(string[] lines, int maxLines)
        {
            Assert.IsLessThanOrEqualTo(maxLines, lines.Length);
            Assert.IsTrue(lines.All(line => line.Length <= MessageText.Columns));
        }
    }
}
