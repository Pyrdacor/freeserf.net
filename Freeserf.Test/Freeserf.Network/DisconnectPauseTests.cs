using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Freeserf.Network;
using global::Freeserf.Test.Freeserf.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Network
{
    [TestClass]
    [DoNotParallelize]
    public class DisconnectPauseTests
    {
        Game previousGame;

        [TestInitialize]
        public void RememberGame() => previousGame = GameManager.Instance.GetCurrentGame();

        [TestCleanup]
        public void RestoreGame()
        {
            if (previousGame != null)
                GameManager.Instance.StartMultiplayerGame(previousGame);
            else
                GameManager.Instance.CloseGame();
        }

        static Game CreateGame()
        {
            var map = new Map(new MapGeometry(3), null);
            map.InitTiles(new FlatGrassMapGenerator(map));
            var game = new Game(map);
            game.InsertPlayer(0, 0, 20, 20);
            game.InsertPlayer(1, 0, 20, 20);
            game.SetSpeed(2);
            return game;
        }

        sealed class Peer : IRemoteClient
        {
            public IPAddress Ip => IPAddress.Loopback;
            public uint PlayerIndex => 1;
            public ILocalServer Server => null;
            public readonly List<INetworkData> Messages = new();
            public Game Snapshot;
            public void Send(byte[] data) => Messages.AddRange(NetworkDataParser.Parse(data));
            public void SendResponse(byte index, ResponseType response) { }
            public void SendHeartbeat(byte index) { }
            public void SendDisconnect() { }
            public void SendLobbyDataUpdate(byte index, LobbyServerInfo server, List<LobbyPlayerInfo> players) { }
            public void SendGameStateUpdate(byte index, Game game, bool full) => Snapshot = game;
            public void SendInSyncMessage(uint tick, byte[] hash) { }
        }

        [TestMethod]
        public void HostDisconnect_ShouldPauseAndPreservePlayerForRejoin()
        {
            var game = CreateGame();
            var player = game.GetPlayer(1);
            GameManager.Instance.StartMultiplayerGame(game);
            var server = new LocalServer("test", null);
            try
            {
                server.EnterGame();
                server.DisconnectClient(new Peer(), false);

                Assert.IsTrue(game.IsPaused);
                Assert.AreSame(game, GameManager.Instance.GetCurrentGame());
                Assert.AreSame(player, game.GetPlayer(1));
                Assert.IsTrue(player.HasLeftGame);
                game.PlayerRejoinedGame(1);
                Assert.IsFalse(player.HasLeftGame);
                Assert.IsTrue(game.IsPaused, "Rejoining must not resume the host automatically.");
                Assert.IsTrue(server.GameDirty);
            }
            finally { server.Close(); }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ClientDisconnect_ShouldPauseOnlyWhenConnectionWasInterrupted(bool interrupted)
        {
            var game = CreateGame();
            var client = new LocalClient { Game = game };
            ClientDisconnectedEventArgs reason = null;
            client.Disconnected += (_, args) => reason = args as ClientDisconnectedEventArgs;

            typeof(LocalClient).GetMethod("HandleDisconnect", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(client, new object[] { interrupted });

            Assert.AreEqual(interrupted, game.IsPaused);
            Assert.AreSame(game, client.Game);
            Assert.IsNotNull(reason);
            Assert.AreEqual(interrupted, reason.ConnectionLost);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RejoinHandshake_ShouldRespectHostPause(bool paused)
        {
            var game = CreateGame();
            if (paused) game.Pause();
            GameManager.Instance.StartMultiplayerGame(game);
            var server = new LocalServer("test", null);
            var peer = new Peer();

            typeof(LocalServer).GetMethod("HandleGameRequest", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(server, new object[] { game, peer, (byte)255, Request.StartGame, null });

            var requests = peer.Messages.Cast<RequestData>().Select(message => message.Request).ToArray();
            CollectionAssert.AreEqual(new[] { paused ? Request.Pause : Request.Resume, Request.AllowUserInput }, requests);
            Assert.AreSame(game, peer.Snapshot);
            Assert.AreEqual(paused, game.IsPaused);
        }
    }
}
