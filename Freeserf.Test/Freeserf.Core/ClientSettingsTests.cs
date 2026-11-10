using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Freeserf.Network;
using Freeserf.Render;
using Freeserf.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    public class ClientSettingsTests
    {
        static Game CreateGame()
        {
            var map = new Map(new MapGeometry(3), null);
            map.InitTiles(new FlatGrassMapGenerator(map));
            var game = new Game(map);
            game.InsertPlayer(0, 0, 20, 20);
            game.InsertPlayer(1, 0, 20, 20);
            for (int i = 0; i < 4; ++i)
            {
                game.GetPlayer(0).SetKnightOccupation(i, 0x44);
                game.GetPlayer(1).SetKnightOccupation(i, new byte[] { 0x21, 0x31, 0x32, 0x21 }[i]);
            }
            return game;
        }

        sealed class Wire : IRemote
        {
            public UserActionData Action;
            public void Send(byte[] bytes) => Action = (UserActionData)NetworkDataParser.Parse(bytes).Single();
        }

        public static IEnumerable<object[]> KnightChanges
        {
            get
            {
                for (int level = 0; level < 4; ++level)
                    foreach (bool max in new[] { false, true })
                        foreach (int delta in new[] { -1, 1 })
                            yield return new object[] { level, max, delta };
            }
        }

        [TestMethod]
        [DynamicData(nameof(KnightChanges))]
        public void KnightChanges_ShouldRoundTripWithoutChangingOtherRowsOrPlayers(int level, bool max, int delta)
        {
            var clientGame = CreateGame();
            var serverGame = CreateGame();
            var player = clientGame.GetPlayer(1);
            Assert.IsTrue(player.ChangeKnightOccupation(level, max, delta));
            var wire = new Wire();
            UserActionData.CreateChangeSettingUserAction(1, clientGame,
                UserActionGameSetting.KnightOccupation, player.GetKnightOccupations()).Send(wire);

            Assert.AreEqual(ResponseType.Ok, wire.Action.ApplyToGame(serverGame, 1));
            CollectionAssert.AreEqual(player.GetKnightOccupations(), serverGame.GetPlayer(1).GetKnightOccupations());
            CollectionAssert.AreEqual(new byte[] { 0x44, 0x44, 0x44, 0x44 }, serverGame.GetPlayer(0).GetKnightOccupations());
        }

        static readonly UserActionGameSetting[] DistributionSettings = Enum.GetValues<UserActionGameSetting>()
            .Where(setting => setting >= UserActionGameSetting.FoodStonemine && setting <= UserActionGameSetting.WheatMill)
            .Append(UserActionGameSetting.SerfToKnightRate).ToArray();

        public static IEnumerable<object[]> DistributionChanges => DistributionSettings.Select(setting => new object[] { setting });

        [TestMethod]
        [DynamicData(nameof(DistributionChanges))]
        public void DistributionChange_ShouldOnlyChangeRequestedSetting(UserActionGameSetting setting)
        {
            var game = CreateGame();
            var player = game.GetPlayer(1);
            var originals = DistributionSettings.ToDictionary(value => value,
                value => typeof(Player).GetProperty(value.ToString()).GetValue(player));
            var otherPlayerOriginal = typeof(Player).GetProperty(setting.ToString()).GetValue(game.GetPlayer(0));
            var wire = new Wire();
            UserActionData.CreateChangeSettingUserAction(1, game, setting, (ushort)12345).Send(wire);

            Assert.AreEqual(ResponseType.Ok, wire.Action.ApplyToGame(game, 1));
            foreach (var value in DistributionSettings)
            {
                var property = typeof(Player).GetProperty(value.ToString());
                var actual = property.GetValue(player);
                Assert.AreEqual(value == setting ? 12345u : Convert.ToUInt32(originals[value]), Convert.ToUInt32(actual), value.ToString());
            }
            Assert.AreEqual(otherPlayerOriginal, typeof(Player).GetProperty(setting.ToString()).GetValue(game.GetPlayer(0)));
        }

        public class RectProxy : DispatchProxy
        {
            protected override object Invoke(MethodInfo method, object[] args)
            {
                if (method.ReturnType == typeof(void)) return null;
                return method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null;
            }
        }

        static SlideBar CreateSlider()
        {
            // Exercise UI state without a graphics context, as in CheckBoxTests.
            var slider = (SlideBar)RuntimeHelpers.GetUninitializedObject(typeof(SlideBar));
            typeof(SlideBar).GetField("fillRect", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(slider, DispatchProxy.Create<IColoredRect, RectProxy>());
            return slider;
        }

        [TestMethod]
        public void DisplayRefresh_ShouldNotIssueSettingChanges()
        {
            var slider = CreateSlider();
            int changes = 0;
            slider.FillChanged += (_, _) => ++changes;
            // The same controls are reused when switching between building settings pages.
            slider.Fill = 50;
            slider.Fill = 2;
            slider.Fill = 37;
            Assert.AreEqual(37, slider.Fill);
            Assert.AreEqual(0, changes);
        }

        [TestMethod]
        public void UserClick_ShouldIssueOneChangeAndIgnoreDelayedOrUnchangedClicks()
        {
            var slider = CreateSlider();
            slider.Fill = 10;
            int changes = 0;
            slider.FillChanged += (_, _) => ++changes;
            var click = typeof(SlideBar).GetMethod("HandleClickLeft", BindingFlags.NonPublic | BindingFlags.Instance);
            click.Invoke(slider, new object[] { 32, 0, false });
            Assert.AreEqual(25, slider.Fill);
            Assert.AreEqual(1, changes);
            click.Invoke(slider, new object[] { 32, 0, false });
            click.Invoke(slider, new object[] { 40, 0, true });
            Assert.AreEqual(25, slider.Fill);
            Assert.AreEqual(1, changes);
        }
    }
}
