using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    public class SerfInventoryTests
    {
        [TestMethod]
        public void EscapeBuildingWithOccupiedExit_ShouldKeepWaiting()
        {
            var map = new Map(new MapGeometry(3), null);
            map.InitTiles(new FlatGrassMapGenerator(map));
            var game = new Game(map);
            var position = game.Map.Position(10, 10);
            var first = game.CreateSerf(1);
            first.Position = position;
            game.Map.SetSerfIndex(position, (int)first.Index);

            // The remaining escapees must keep retrying while the first occupies the exit.
            for (int i = 2; i <= 12; ++i)
            {
                var serf = game.CreateSerf(i);
                serf.Position = position;
                serf.GoOutFromInventory(0, 0, -3);
                serf.BuildingDeleted(position, true);

                for (int tick = 0; tick < 3; ++tick)
                {
                    serf.Update();
                    Assert.AreEqual(Serf.State.EscapeBuilding, serf.SerfState);
                    Assert.AreEqual((uint)first.Index, game.Map.GetSerfIndex(position));
                }
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReadyToLeaveDestroyedInventory_ShouldEscapeBuilding(bool flagOccupied)
        {
            var map = new Map(new MapGeometry(3), null);
            map.InitTiles(new FlatGrassMapGenerator(map));
            var game = new Game(map);
            var inventory = game.CreateInventory();
            var serf = game.CreateSerf(1);
            serf.Position = game.Map.Position(10, 10);
            serf.GoOutFromInventory(inventory.Index, 0, -3);

            if (flagOccupied)
                game.Map.SetSerfIndex(game.Map.MoveDownRight(serf.Position), (int)serf.Index);

            game.DeleteInventory(inventory);
            serf.Update();

            Assert.AreEqual(Serf.State.EscapeBuilding, serf.SerfState);
        }

        [TestMethod]
        public void ReadyToLeaveInventoryWithOccupiedFlag_ShouldWait()
        {
            var map = new Map(new MapGeometry(3), null);
            map.InitTiles(new FlatGrassMapGenerator(map));
            var game = new Game(map);
            var inventory = game.CreateInventory();
            var serf = game.CreateSerf(1);
            serf.Position = game.Map.Position(10, 10);
            serf.GoOutFromInventory(inventory.Index, 0, -3);
            game.Map.SetSerfIndex(game.Map.MoveDownRight(serf.Position), (int)serf.Index);

            serf.Update();

            Assert.AreEqual(Serf.State.ReadyToLeaveInventory, serf.SerfState);
            Assert.AreEqual(82, serf.Animation);
        }
    }
}
