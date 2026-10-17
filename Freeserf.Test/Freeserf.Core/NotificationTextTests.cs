using System;
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
    public class NotificationTextTests
    {
        CultureInfo originalUICulture;

        [TestInitialize]
        public void SetEnglishCulture()
        {
            originalUICulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
        }

        [TestCleanup]
        public void RestoreCulture()
        {
            CultureInfo.CurrentUICulture = originalUICulture;
        }

        public static IEnumerable<object[]> ExpectedViews =>
        [
            [Notification.Type.UnderAttack, Decoration.Opponent, 0u, "Your settlement\nis under attack"],
            [Notification.Type.LoseFight, Decoration.Opponent, 0u, "Your knights\njust lost the\nfight"],
            [Notification.Type.WinFight, Decoration.Opponent, 0u, "You gained\na victory here"],
            [Notification.Type.MineEmpty, Decoration.Mine, 0u, "This mine hauls\nno more raw\nmaterials"],
            [Notification.Type.CallToLocation, Decoration.MapObject, 0x90u, "You wanted me\nto call you to\nthis location"],
            [Notification.Type.KnightOccupied, Decoration.Building, 0u, "A knight has\noccupied this\nnew building"],
            [Notification.Type.NewStock, Decoration.MapObject, RenderBuilding.MapBuildingSprite[(int)Building.Type.Stock],
                "A new stock\nhas been built"],
            [Notification.Type.LostLand, Decoration.Opponent, 0u, "Because of this\nenemy building\nyou lost some\nland"],
            [Notification.Type.EmergencyActive, Decoration.MapObject, RenderBuilding.MapBuildingFrameSprite[(int)Building.Type.Stock],
                "Emergency\nprogram\nactivated"],
            [Notification.Type.EmergencyNeutral, Decoration.MapObject, RenderBuilding.MapBuildingSprite[(int)Building.Type.Castle],
                "Emergency\nprogram\nneutralized"],
            [Notification.Type.FoundGold, Decoration.Icon, 0x2fu, "A geologist\nhas found gold"],
            [Notification.Type.FoundIron, Decoration.Icon, 0x2cu, "A geologist\nhas found iron"],
            [Notification.Type.FoundCoal, Decoration.Icon, 0x2eu, "A geologist\nhas found coal"],
            [Notification.Type.FoundStone, Decoration.Icon, 0x2bu, "A geologist\nhas found stone"],
            [Notification.Type.CallToMenu, Decoration.Menu, 0u, "You wanted me\nto call you\nto this menu"],
            [Notification.Type.ThirtyMinutesSinceSave, Decoration.Icon, 0x5du, "30 min. passed\nsince the last\nsaving"],
            [Notification.Type.OneHourSinceSave, Decoration.Icon, 0x5du, "One hour passed\nsince the last\nsaving"],
            [Notification.Type.CallToStock, Decoration.MapObject, RenderBuilding.MapBuildingSprite[(int)Building.Type.Stock],
                "You wanted me\nto call you\nto this stock"],
            [Notification.Type.PlayerLeft, Decoration.Opponent, 0u, "This player\nhas left\nthe game"],
            [Notification.Type.PlayerRejoined, Decoration.Opponent, 0u, "This player\nhas rejoined\nthe game"]
        ];

        [TestMethod]
        [DynamicData(nameof(ExpectedViews))]
        public void Notification_PreservesMessageAndDecoration(Notification.Type type, object decoration, uint icon, string expected)
        {
            var view = NotificationBox.GetView(type);

            Assert.AreEqual(type, view.Type);
            Assert.AreEqual((Decoration)decoration, view.Decoration);
            Assert.AreEqual(icon, view.Icon);
            Assert.AreEqual(expected.Replace('\n', ' '), view.Text);
            var lines = NotificationBox.WrapMessage(view.Text);
            Assert.IsLessThanOrEqualTo(5, lines.Length);
            Assert.IsTrue(lines.All(line => line.Length <= MessageText.Columns));
        }

        [TestMethod]
        public void Notification_WrapsWholeParagraphNaturally()
        {
            CollectionAssert.AreEqual(new[] { "A new stock has", "been built" },
                NotificationBox.WrapMessage(NotificationBox.GetView(Notification.Type.NewStock).Text));
        }

        [TestMethod]
        public void ExpectedViews_CoverEveryDisplayableNotificationExactlyOnce()
        {
            var types = ExpectedViews.Select(row => (Notification.Type)row[0]).ToArray();
            var expected = Enum.GetValues<Notification.Type>().Where(type => type != Notification.Type.None).ToArray();

            CollectionAssert.AreEquivalent(expected, types);
            Assert.AreEqual(types.Length, types.Distinct().Count());
        }

        [TestMethod]
        public void None_PreservesEmptyView()
        {
            var view = NotificationBox.GetView(Notification.Type.None);

            Assert.AreEqual(Notification.Type.None, view.Type);
            Assert.AreEqual(Decoration.Opponent, view.Decoration);
            Assert.AreEqual(0u, view.Icon);
            Assert.IsNull(view.Text);
        }

        [TestMethod]
        public void UnknownType_PreservesLookupFailure()
        {
            Assert.ThrowsExactly<KeyNotFoundException>(() => NotificationBox.GetView((Notification.Type)9));
        }

        [TestMethod]
        [DataRow("en-US")]
        [DataRow("de-DE")]
        [DataRow("fr-FR")]
        public void ExistingViews_ResolveCurrentUICulture(string culture)
        {
            var views = Enum.GetValues<Notification.Type>()
                .Where(type => type != Notification.Type.None)
                .Select(NotificationBox.GetView).ToArray();

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

            foreach (var view in views)
            {
                string expected = Strings.ResourceManager.GetString("Notification_" + view.Type);
                Assert.IsFalse(string.IsNullOrEmpty(expected), $"Missing resource for {view.Type}");
                Assert.AreEqual(expected, view.Text, $"Resource lookup failed for {view.Type}");
            }
        }
    }
}
