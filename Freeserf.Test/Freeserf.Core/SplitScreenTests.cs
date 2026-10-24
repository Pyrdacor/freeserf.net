using System;
using System.Collections.Generic;
using System.Reflection;
using Freeserf.Render;
using Freeserf.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    public class SplitScreenTests
    {
        [TestMethod]
        public void CreateSplitScreenGameInfo_HasTwoHumanPlayers()
        {
            var gameInfo = GameInfo.CreateSplitScreenGameInfo();

            Assert.IsTrue(gameInfo.PlayerCount >= 2u);
            Assert.IsTrue(gameInfo.GetPlayer(0).Face.IsHuman(), "Player 0 should be human.");
            Assert.IsTrue(gameInfo.GetPlayer(1).Face.IsHuman(), "Player 1 should be human.");
            Assert.AreEqual(PlayerFace.Friend, gameInfo.GetPlayer(1).Face, "Player 1 should be the Friend (red) character.");
            Assert.AreEqual(40u, gameInfo.GetPlayer(0).Intelligence, "Player 0 should have full intelligence.");
            Assert.AreEqual(40u, gameInfo.GetPlayer(1).Intelligence, "Player 1 should have full intelligence.");
        }

        [TestMethod]
        public void SplitScreen_BothHumanPlayersAreLocked()
        {
            // In split-screen mode the two human players have fixed player indices (0 and 1).
            // Only the CPU players (2 and 3) can be modified.
            Assert.IsTrue(GameInitBox.IsSplitScreenPlayerLocked(GameInitBox.GameType.SplitScreen, 0), "Player 0 must be locked in split-screen.");
            Assert.IsTrue(GameInitBox.IsSplitScreenPlayerLocked(GameInitBox.GameType.SplitScreen, 1), "Player 1 must be locked in split-screen.");
            Assert.IsFalse(GameInitBox.IsSplitScreenPlayerLocked(GameInitBox.GameType.SplitScreen, 2), "CPU player 2 must not be locked in split-screen.");
            Assert.IsFalse(GameInitBox.IsSplitScreenPlayerLocked(GameInitBox.GameType.SplitScreen, 3), "CPU player 3 must not be locked in split-screen.");
            Assert.IsFalse(GameInitBox.IsSplitScreenPlayerLocked(GameInitBox.GameType.Custom, 0), "Player 0 must not be locked in custom games.");
            Assert.IsFalse(GameInitBox.IsSplitScreenPlayerLocked(GameInitBox.GameType.Custom, 1), "Player 1 must not be locked in custom games.");
        }

        [TestMethod]
        public void Game_WithTwoRenderViews_CreatesTwoRenderViews()
        {
            var renderView0 = DispatchProxy.Create<IRenderView, RenderProxy>();
            var renderView1 = DispatchProxy.Create<IRenderView, RenderProxy>();

            var game = new Game(new[] { renderView0, renderView1 }, null);

            Assert.AreEqual(2, game.RenderViewCount);
        }

        [TestMethod]
        public void Game_WithOneRenderView_CreatesOneRenderView()
        {
            var renderView0 = DispatchProxy.Create<IRenderView, RenderProxy>();

            var game = new Game(renderView0, null);

            Assert.AreEqual(1, game.RenderViewCount);
        }

        [TestMethod]
        public void SplitScreenGuiScaling_ScalesUniformly()
        {
            // The gui must be scaled uniformly (like the normal single-player mode)
            // so the menus (e.g. the bottom panel bar) are not distorted.
            var scaling = new SplitScreenGuiScaling(new Size(960, 1080));

            Assert.AreEqual(scaling.ScaleX, scaling.ScaleY, 0.0001f, "The gui must be scaled uniformly.");
            Assert.AreEqual(640, scaling.LogicalWidth);
            Assert.AreEqual(480, scaling.LogicalHeight);
        }

        [TestMethod]
        public void SplitScreenGuiScaling_FitsIntoPlayerScreen()
        {
            // The scaled gui must fit into the player's part of the screen.
            var playerScreen = new Size(960, 1080);
            var scaling = new SplitScreenGuiScaling(playerScreen);

            int scaledWidth = Misc.Round(scaling.LogicalWidth * scaling.ScaleX);
            int scaledHeight = Misc.Round(scaling.LogicalHeight * scaling.ScaleY);

            Assert.IsTrue(scaledWidth <= playerScreen.Width, "The scaled gui width must fit into the player's screen.");
            Assert.IsTrue(scaledHeight <= playerScreen.Height, "The scaled gui height must fit into the player's screen.");
        }

        public class RenderProxy : DispatchProxy
        {
            readonly Dictionary<string, object> properties = new Dictionary<string, object>();

            protected override object Invoke(MethodInfo method, object[] args)
            {
                if (method.Name.StartsWith("set_", StringComparison.Ordinal))
                {
                    properties[method.Name.Substring(4)] = args[0];
                    return null;
                }

                if (method.Name.StartsWith("get_", StringComparison.Ordinal))
                {
                    if (properties.TryGetValue(method.Name.Substring(4), out var value))
                        return value;
                    return method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null;
                }

                if (method.Name == nameof(IRenderView.GetLayer))
                    return null;

                throw new NotSupportedException(method.Name);
            }
        }
    }
}
