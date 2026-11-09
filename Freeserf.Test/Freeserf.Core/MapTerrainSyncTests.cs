using System;
using Freeserf.Serialize;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    public class MapTerrainSyncTests
    {
        static Game CreateGame(Map.Terrain up, Map.Terrain down)
        {
            var map = new Map(new MapGeometry(3), null);
            var generator = new FlatGrassMapGenerator(map);
            var tile = generator.GetLandscape()[0];
            tile.TypeUp = up;
            tile.TypeDown = down;
            map.InitTiles(generator);
            return new Game(map);
        }

        [TestMethod]
        public void FullSync_ShouldReplaceClientTerrain()
        {
            var server = CreateGame(Map.Terrain.Tundra0, Map.Terrain.Water1);
            var client = CreateGame(Map.Terrain.Water0, Map.Terrain.Grass1);

            GameStateSerializer.DeserializeInto(client, GameStateSerializer.SerializeFrom(server, true), true, true);

            Assert.AreEqual(server.Map.TypeUp(0), client.Map.TypeUp(0));
            Assert.AreEqual(server.Map.TypeDown(0), client.Map.TypeDown(0));
            CollectionAssert.AreEqual(GameStateSerializer.ComputeHash(server), GameStateSerializer.ComputeHash(client));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void StateHash_ShouldDetectTerrainOnlyChanges(bool changeUp)
        {
            var game = CreateGame(Map.Terrain.Grass1, Map.Terrain.Grass1);
            var originalHash = GameStateSerializer.ComputeHash(game);
            var generator = new FlatGrassMapGenerator(game.Map);
            var tile = generator.GetLandscape()[0];
            if (changeUp)
                tile.TypeUp = Map.Terrain.Water0;
            else
                tile.TypeDown = Map.Terrain.Tundra0;
            game.Map.InitTiles(generator);

            Assert.IsFalse(originalHash.AsSpan().SequenceEqual(GameStateSerializer.ComputeHash(game)),
                "Terrain differences must be detected by multiplayer checkpoints.");
        }

        [TestMethod]
        public void FullSync_ShouldRejectPreviousStateFormat()
        {
            var game = CreateGame(Map.Terrain.Grass1, Map.Terrain.Grass1);
            var data = GameStateSerializer.SerializeFrom(game, true);
            data[3] = 0; // Previous format omitted the terrain types.

            Assert.Throws<ExceptionFreeserf>(() => GameStateSerializer.DeserializeInto(game, data, true, true));
        }
    }
}
