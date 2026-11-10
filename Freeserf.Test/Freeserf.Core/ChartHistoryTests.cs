using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    public class ChartHistoryTests
    {
        static Game CreateGame()
        {
            var map = new Map(new MapGeometry(3), null);
            map.InitTiles(new FlatGrassMapGenerator(map));
            var game = new Game(map);
            game.InsertPlayer(0, 0, 20, 20);
            return game;
        }

        static void FillHistory(Player player)
        {
            for (int mode = 0; mode < 16; ++mode)
                for (int i = 0; i < 112; ++i)
                    player.SetPlayerStatHistory(mode, i, (uint)((mode + i) % 101));
            for (int resource = 0; resource < 26; ++resource)
                for (int i = 0; i < 120; ++i)
                {
                    player.IncreaseResourceCount((Resource.Type)resource);
                    player.UpdateStats(resource, i);
                }
        }

        static void AssertHistory(Player expected, Player actual)
        {
            for (int mode = 0; mode < 16; ++mode)
                CollectionAssert.AreEqual(expected.GetPlayerStatHistory(mode), actual.GetPlayerStatHistory(mode));
            for (int resource = 0; resource < 26; ++resource)
                CollectionAssert.AreEqual(expected.GetResourceCountHistory((Resource.Type)resource), actual.GetResourceCountHistory((Resource.Type)resource));
        }

        [TestMethod]
        public void SavedPlayerHistory_ShouldSurviveLoadAndMultiplayerTransfer()
        {
            var source = CreateGame();
            FillHistory(source.GetPlayer(0));
            var writer = new SaveWriterTextSection("player", 0);
            source.GetPlayer(0).WriteTo(writer);
            using var stream = new MemoryStream();
            using (var text = new StreamWriter(stream, Encoding.ASCII, 1024, true))
                Assert.IsTrue(writer.Write(text));
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var loaded = CreateGame();
            loaded.GetPlayer(0).ReadFrom(new SaveReaderTextFile(reader).GetSections("player")[0]);
            AssertHistory(source.GetPlayer(0), loaded.GetPlayer(0));
            var server = CreateGame();
            GameStateSerializer.DeserializeInto(server, GameStateSerializer.SerializeFrom(loaded, true), true, true);
            var client = CreateGame();
            GameStateSerializer.DeserializeInto(client, GameStateSerializer.SerializeFrom(server, true), true, true);
            AssertHistory(source.GetPlayer(0), server.GetPlayer(0));
            AssertHistory(source.GetPlayer(0), client.GetPlayer(0));
        }

        [TestMethod]
        public void IncrementalSync_ShouldTransferNewHistorySamples()
        {
            var server = CreateGame();
            var client = CreateGame();
            GameStateSerializer.DeserializeInto(client, GameStateSerializer.SerializeFrom(server, true), true, true);
            server.ResetDirtyFlag();
            FillHistory(server.GetPlayer(0));
            GameStateSerializer.DeserializeInto(client, GameStateSerializer.SerializeFrom(server, false), true, false);
            AssertHistory(server.GetPlayer(0), client.GetPlayer(0));
            CollectionAssert.AreEqual(GameStateSerializer.ComputeHash(server), GameStateSerializer.ComputeHash(client));
        }
    }
}
