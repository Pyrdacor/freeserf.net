using System.Linq;
using System.Net;
using Freeserf.Network;
using Freeserf.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Network
{
    [TestClass]
    public class ServerDiscoveryTests
    {
        [TestMethod]
        [DataRow(false, false, false, "")]
        [DataRow(false, true, true, " | saved")]
        [DataRow(true, true, true, " | running")]
        public void FullServer_DiscoveryPreservesJoinCapability(bool inGame, bool allowsRejoin, bool canJoin, string label)
        {
            var answer = Discovery.CreateAnswer("Test", inGame, 4, 4, allowsRejoin);
            var found = Discovery.ParseAnswer(answer, IPAddress.Loopback, "localhost");

            Assert.IsNotNull(found);
            Assert.AreEqual(inGame, found.InGame);
            Assert.AreEqual(allowsRejoin, found.AllowsRejoin);
            Assert.AreEqual(IPAddress.Loopback, found.Ip);
            Assert.AreEqual("localhost", found.SearchedAddress);

            var server = new ServerInfo
            {
                Name = found.Name,
                HostName = found.Ip.ToString(),
                CurrentPlayers = found.CurrentPlayers,
                MaxPlayers = found.MaxPlayers,
                Online = true,
                InGame = found.InGame,
                AllowsRejoin = found.AllowsRejoin
            };

            Assert.AreEqual(canJoin, server.CanJoin);
            Assert.AreEqual("Test | 127.0.0.1 | 4/4" + label, server.ToString());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void LegacyAnswer_RejoinCapabilityDefaultsToRunningState(bool inGame)
        {
            var answer = Discovery.CreateAnswer("Legacy", inGame, 4, 4, true);
            var legacyAnswer = answer.Take(answer.Length - 1).ToArray();
            var found = Discovery.ParseAnswer(legacyAnswer, IPAddress.Loopback, null);

            Assert.IsNotNull(found);
            Assert.AreEqual("Legacy", found.Name);
            Assert.AreEqual(inGame, found.InGame);
            Assert.AreEqual(inGame, found.AllowsRejoin);
        }

        [TestMethod]
        public void ExtendedAnswer_PreservesLegacyLayout()
        {
            var answer = Discovery.CreateAnswer("Test", false, 4, 4, true);
            byte[] legacyAnswer = [(byte)'F', (byte)'S', (byte)'N', (byte)'A', 0, 0, 4, 4, 4,
                (byte)'T', (byte)'e', (byte)'s', (byte)'t'];

            CollectionAssert.AreEqual(legacyAnswer, answer.Take(answer.Length - 1).ToArray());
            Assert.AreEqual((byte)1, answer[^1]);
        }

        [TestMethod]
        public void LongName_RejoinCapabilityFollowsTruncatedName()
        {
            var answer = Discovery.CreateAnswer(new string('A', 300), false, 4, 4, true);
            var found = Discovery.ParseAnswer(answer, IPAddress.Loopback, null);

            Assert.IsNotNull(found);
            Assert.AreEqual(new string('A', 255), found.Name);
            Assert.IsTrue(found.AllowsRejoin);
        }

        [TestMethod]
        public void TruncatedName_AnswerIsRejected()
        {
            var answer = Discovery.CreateAnswer("Test", false, 4, 4, true);

            Assert.IsNull(Discovery.ParseAnswer(answer.Take(12).ToArray(), IPAddress.Loopback, null));
        }

        [TestMethod]
        public void OfflineServer_CannotJoinEvenWithRejoinCapability()
        {
            var server = new ServerInfo { AllowsRejoin = true, CurrentPlayers = 4 };

            Assert.IsFalse(server.CanJoin);
        }

        [TestMethod]
        public void NewLobbyWithFreeSlot_CanJoin()
        {
            var server = new ServerInfo { Online = true, CurrentPlayers = 3 };

            Assert.IsTrue(server.CanJoin);
        }
    }
}