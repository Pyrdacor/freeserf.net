using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    public class SerfInventoryTests
    {
        [TestMethod]
        [DataRow((int)Building.Type.CoalMine, 125)]
        [DataRow((int)Building.Type.IronMine, 126)]
        [DataRow((int)Building.Type.GoldMine, 127)]
        [DataRow((int)Building.Type.StoneMine, 128)]
        public void MinerInBurnedMine_ShouldNotBlockItsOwnEscape(int mineType, int animation)
        {
            var map = new Map(new MapGeometry(3), null);
            map.InitTiles(new FlatGrassMapGenerator(map));
            var game = new Game(map);
            game.InsertPlayer(0, 0, 20, 20);
            var position = game.Map.Position(10, 10);
            var mine = game.CreateBuilding(1);
            mine.Position = position;
            mine.Player = 0;
            mine.StartBuilding((Building.Type)mineType);
            var buildingState = (BuildingState)typeof(Building)
                .GetField("state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mine);
            buildingState.Constructing = false;
            buildingState.Active = true;
            buildingState.Holder = true;
            game.Map.SetObject(position, Map.Object.SmallBuilding, (int)mine.Index);
            game.Map.SetOwner(position, 0);
            game.Map.SetOwner(game.Map.MoveDownRight(position), 0);
            var miner = game.CreateSerf(1);
            miner.Position = position;
            miner.Player = 0;
            miner.SerfType = Serf.Type.Miner;
            typeof(Serf).GetProperty("SerfState").SetValue(miner, Serf.State.Mining);
            typeof(Serf).GetProperty("Animation").SetValue(miner, animation);
            game.Map.SetSerfIndex(position, (int)miner.Index);

            Assert.IsTrue(mine.BurnUp());
            Assert.AreEqual(Serf.State.EscapeBuilding, miner.SerfState);
            game.DeleteBuilding(mine);
            miner.Update();

            Assert.AreSame(miner, game.GetSerf(miner.Index));
            Assert.AreEqual(Serf.Type.Miner, miner.SerfType);
            Assert.AreEqual(Serf.State.Lost, miner.SerfState);
            Assert.AreEqual(82, miner.Animation, "The elevator animation must be replaced.");
            Assert.AreEqual(0, miner.Counter);
            Assert.AreEqual(miner.Index, game.Map.GetSerfIndex(position));
            Assert.IsNull(game.GetBuilding(mine.Index));
        }

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
