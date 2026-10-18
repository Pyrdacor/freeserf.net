using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Freeserf.Localization;
using Freeserf.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    [DoNotParallelize]
    public class ChoiceTextTests
    {
        CultureInfo originalCulture;

        public static IEnumerable<object[]> Languages =>
            Directory.GetDirectories(Path.GetDirectoryName(typeof(Strings).Assembly.Location))
                .Where(directory => File.Exists(Path.Combine(directory,
                    typeof(Strings).Assembly.GetName().Name + ".resources.dll")))
                .Select(Path.GetFileName).Append("en").Append("fr-FR").Distinct()
                .Select(language => new object[] { language });

        [TestInitialize]
        public void SaveCulture() => originalCulture = Strings.Culture;

        [TestCleanup]
        public void RestoreCulture() => Strings.Culture = originalCulture;

        [TestMethod]
        [DynamicData(nameof(Languages))]
        public void LocalizedChoices_FitWithoutIntersectingAndMatchTheirClickTargets(string language)
        {
            Strings.Culture = CultureInfo.GetCultureInfo(language);
            foreach (bool unsaved in new[] { false, true })
            {
                var choices = PopupBox.GetQuitChoices(unsaved);
                CheckRow(choices);
                var paragraph = unsaved
                    ? PopupBox.GetParagraphLayout(Strings.Popup_UnsavedQuitQuestion, 8, 79, 5, "unsaved quit")
                    : PopupBox.GetParagraphLayout(Strings.Popup_QuitQuestion, 8, 19, 3, "quit");
                foreach (var line in paragraph)
                {
                    var lineBounds = new Rect(line.X, line.Y, line.Text.Length * 8, 8);
                    Assert.IsTrue(choices.All(choice => !lineBounds.IntersectsWith(choice.Target)));
                }
                PopupBox.Action[] actions = unsaved
                    ? [PopupBox.Action.SaveAndQuit, PopupBox.Action.NoSaveQuitConfirm, PopupBox.Action.QuitCancel]
                    : [PopupBox.Action.QuitConfirm, PopupBox.Action.QuitCancel];

                for (int y = choices[0].Target.Top - 1; y <= choices[0].Target.Bottom; ++y)
                    for (int x = 0; x < 144; ++x)
                    {
                        int index = ChoiceText.HitTest(choices, x, y);
                        PopupBox.Action? expected = index < 0 ? null : actions[index];
                        Assert.AreEqual(expected, PopupBox.GetQuitChoiceAction(unsaved, x, y),
                            $"{language}: ({x}, {y})");
                    }
            }
            var answers = VoteBox.GetAnswerLayout();
            CheckRow(answers);
            for (int y = 135; y <= 150; ++y)
                for (int x = 0; x < 144; ++x)
                {
                    int index = ChoiceText.HitTest(answers, x, y);
                    bool? expected = index < 0 ? null : index == 0;
                    Assert.AreEqual(expected, VoteBox.GetAnswerAt(x, y));
                }
            Assert.IsNull(PopupBox.GetQuitChoiceAction(true, 40, 54),
                "The earlier confirmation row must not act on the unsaved confirmation.");
        }

        static void CheckRow(ChoiceText[] choices)
        {
            for (int i = 0; i < choices.Length; ++i)
            {
                var choice = choices[i];
                Assert.IsGreaterThanOrEqualTo(8, choice.Target.Left);
                Assert.IsLessThanOrEqualTo(136, choice.Target.Right);
                Assert.IsGreaterThanOrEqualTo(8, choice.Target.Top);
                Assert.IsLessThanOrEqualTo(152, choice.Target.Bottom);
                if (i > 0)
                    Assert.IsGreaterThanOrEqualTo(4, choice.Target.Left - choices[i - 1].Target.Right);

                for (int character = 0; character < choice.Text.Length; ++character)
                    for (int dx = 0; dx < 8; ++dx)
                        for (int dy = 0; dy < 8; ++dy)
                        {
                            int x = choice.X + character * 8 + dx;
                            int y = choice.Y + dy;
                            Assert.AreEqual(i, ChoiceText.HitTest(choices, x, y));
                            Assert.AreEqual(1, choices.Count(other => x >= other.Target.Left &&
                                x < other.Target.Right && y >= other.Target.Top && y < other.Target.Bottom));
                        }
                Assert.AreEqual(i, ChoiceText.HitTest(choices, choice.Target.Left, choice.Target.Top));
                Assert.AreEqual(i, ChoiceText.HitTest(choices, choice.Target.Right - 1, choice.Target.Bottom - 1));
                Assert.AreEqual(-1, ChoiceText.HitTest(choices, choice.Target.Left - 1, choice.Y));
                Assert.AreEqual(-1, ChoiceText.HitTest(choices, choice.Target.Right, choice.Y));
            }
        }

        [TestMethod]
        public void SwitchingLanguages_RecalculatesBothTextAndTargets()
        {
            Strings.Culture = CultureInfo.GetCultureInfo("en");
            var english = PopupBox.GetQuitChoices(true);
            var englishVote = VoteBox.GetAnswerLayout();
            Strings.Culture = CultureInfo.GetCultureInfo("de");
            var german = PopupBox.GetQuitChoices(true);
            var germanVote = VoteBox.GetAnswerLayout();
            Assert.AreEqual("Save", english[0].Text);
            Assert.AreEqual("Sich.", german[0].Text);
            Assert.AreNotEqual(english[0].Target.Size.Width, german[0].Target.Size.Width);
            Assert.AreEqual("No", englishVote[1].Text);
            Assert.AreEqual("Nein", germanVote[1].Text);
            Assert.AreNotEqual(englishVote[1].Target.Size.Width, germanVote[1].Target.Size.Width);
            CheckRow(german);
            CheckRow(germanVote);
        }

        [TestMethod]
        public void InvalidOrOversizedChoices_ReportLayoutErrorsInsteadOfOverlapping()
        {
            Assert.ThrowsExactly<ExceptionFreeserf>(() => ChoiceText.Layout(54, "Yes"));
            Assert.ThrowsExactly<ExceptionFreeserf>(() => ChoiceText.Layout(54, "", "No"));
            Assert.ThrowsExactly<ExceptionFreeserf>(() => ChoiceText.Layout(54, "Two\nlines", "No"));
            Assert.ThrowsExactly<ExceptionFreeserf>(() => ChoiceText.Layout(54, new string('x', 16), "No"));
        }
    }
}
