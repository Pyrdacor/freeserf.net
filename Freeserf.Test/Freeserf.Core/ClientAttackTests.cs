using System;
using System.Linq;
using System.Reflection;
using Freeserf.Network;
using Freeserf.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    public class ClientAttackTests
    {
        static Building AddHut(Game game, uint owner, uint position, int knights)
        {
            var building = game.Buildings.Allocate();
            building.Position = position;
            building.Player = owner;
            building.StartBuilding(Building.Type.Hut);
            var state = (BuildingState)typeof(Building).GetField("state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(building);
            state.Constructing = false;
            state.Active = true;
            building.ThreatLevel = 3;
            game.Map.SetOwner(position, owner);
            game.Map.SetObject(position, Map.Object.SmallBuilding, (int)building.Index);
            for (int i = 0; i < knights; ++i)
            {
                var knight = game.Serfs.Allocate();
                knight.InitDefendingKnight(building);
                building.FirstKnight = knight.Index;
            }
            state.Stock[0].Available = (byte)knights;
            return building;
        }

        static Game CreateGame()
        {
            var map = new Map(new MapGeometry(3), null);
            map.InitTiles(new FlatGrassMapGenerator(map));
            var game = new Game(map);
            game.Buildings.Allocate(); // Index zero is reserved.
            game.Serfs.Allocate();
            game.InsertPlayer(0, 0, 20, 20);
            game.InsertPlayer(1, 0, 20, 20);
            game.GetPlayer(1).SetKnightOccupation(3, 0);
            AddHut(game, 0, map.Position(30, 30), 1);
            AddHut(game, 1, map.Position(34, 30), 3);
            return game;
        }

        sealed class Wire : IRemote
        {
            public UserActionData Attack;
            public void Send(byte[] data) => Attack = (UserActionData)NetworkDataParser.Parse(data).Single();
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        public void ClientAttack_ShouldDispatchSelectedKnightsOnBothPeers(int count)
        {
            var client = CreateGame();
            var server = CreateGame();
            GameStateSerializer.DeserializeInto(client, GameStateSerializer.SerializeFrom(server, true), true, true);
            uint target = client.Map.Position(30, 30);
            var player = client.GetPlayer(1);
            Assert.IsTrue(player.PrepareAttack(target, count));
            Assert.AreEqual(count, player.TotalKnightsAttacking);

            var command = PopupBox.StartAttackAndCreateUserAction(client, player);
            Assert.AreEqual(0, player.TotalKnightsAttacking, "Local prediction consumes the selected count.");
            var wire = new Wire();
            command.Send(wire);
            Assert.AreEqual((uint)count, BitConverter.ToUInt32(wire.Attack.Parameters, 12));
            Assert.AreEqual(ResponseType.Ok, wire.Attack.ApplyToGame(server, 1));
            uint source = client.Map.Position(34, 30);
            Assert.AreEqual((uint)(3 - count), server.GetBuildingAtPosition(source).KnightCount);
            Assert.AreEqual(client.GetBuildingAtPosition(source).KnightCount, server.GetBuildingAtPosition(source).KnightCount);
            CollectionAssert.AreEqual(GameStateSerializer.ComputeHash(client), GameStateSerializer.ComputeHash(server));
        }
    }
}
