using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    /// <summary>
    /// Map generator that produces flat grass without any map objects.
    /// This gives the tests a fully predictable landscape.
    /// </summary>
    internal class FlatGrassMapGenerator : MapGenerator
    {
        readonly Map.LandscapeTile[] tiles;

        public FlatGrassMapGenerator(Map map)
        {
            int tileCount = (int)map.Geometry.TileCount;

            tiles = new Map.LandscapeTile[tileCount];

            for (int i = 0; i < tileCount; ++i)
            {
                tiles[i] = new Map.LandscapeTile(null, i)
                {
                    TypeUp = Map.Terrain.Grass1,
                    TypeDown = Map.Terrain.Grass1
                };
            }
        }

        public override void Generate() { }
        public override uint GetHeight(uint position) => 0u;
        public override Map.Terrain GetTypeUp(uint position) => Map.Terrain.Grass1;
        public override Map.Terrain GetTypeDown(uint position) => Map.Terrain.Grass1;
        public override Map.Object GetObject(uint position) => Map.Object.None;
        public override Map.Minerals GetResourceType(uint position) => Map.Minerals.None;
        public override int GetResourceAmount(uint position) => 0;
        public override Map.LandscapeTile[] GetLandscape() => tiles;
    }

    /// <summary>
    /// Tests for ending a road on an existing road by placing a flag there
    /// which splits the existing road. This was possible in the original game.
    /// </summary>
    [TestClass]
    public class RoadBuildingTests
    {
        const uint PlayerIndex = 0u;
        const uint OtherPlayerIndex = 1u;

        /// <summary>
        /// Creates a flat map that is owned by the player and holds a single
        /// straight road running east along row 10 between two flags. The flags
        /// are far enough apart to leave flag-able positions in between.
        /// </summary>
        static Map CreateMapWithRoad()
        {
            var map = new Map(new MapGeometry(3), null);

            map.InitTiles(new FlatGrassMapGenerator(map));

            for (uint y = 5; y <= 20; ++y)
            {
                for (uint x = 5; x <= 20; ++x)
                {
                    map.SetOwner(map.Position(x, y), PlayerIndex);
                }
            }

            // Flags at both ends of the existing road.
            map.SetObject(map.Position(8, 10), Map.Object.Flag, 1);
            map.SetObject(map.Position(14, 10), Map.Object.Flag, 2);

            // Paths in between, connecting both flags.
            for (uint x = 8; x < 14; ++x)
            {
                map.AddPath(map.Position(x, 10), Direction.Right);
                map.AddPath(map.Position(x + 1, 10), Direction.Left);
            }

            return map;
        }

        /// <summary>
        /// A position in the middle of the existing road, far enough away
        /// from the flags at both ends to allow a flag there.
        /// </summary>
        static uint RoadPosition(Map map) => map.Position(11, 10);

        /// <summary>
        /// The clear position a road under construction would come from.
        /// Moving up from here leads onto the existing road.
        /// </summary>
        static uint ApproachPosition(Map map) => map.MoveDown(RoadPosition(map));

        [TestMethod]
        public void RoadOnExistingRoad_ShouldBeAllowedAsRoadEnd()
        {
            var map = CreateMapWithRoad();

            Assert.IsTrue(map.CanRoadEndOnExistingRoad(ApproachPosition(map), Direction.Up, PlayerIndex),
                "A road should be allowed to end on an existing road by placing a flag there.");
        }

        [TestMethod]
        public void RoadOnExistingRoad_ShouldNotBeAllowedAsPassThrough()
        {
            var map = CreateMapWithRoad();
            var approachPosition = ApproachPosition(map);

            // Only the destination of a road may have a flag (see Game.CanBuildRoad),
            // so a road must never run across an existing road. This is what keeps
            // the road network consistent and it must stay rejected.
            Assert.IsFalse(map.IsRoadSegmentValid(approachPosition, Direction.Up, false),
                "A road must never pass through an existing road.");
            Assert.IsFalse(map.IsRoadSegmentValid(approachPosition, Direction.Up, true),
                "A road must never pass through an existing road.");
        }

        [TestMethod]
        public void RoadNextToExistingFlag_ShouldNotBeAllowedAsRoadEnd()
        {
            var map = CreateMapWithRoad();

            // This position is part of the road as well but it is a direct
            // neighbor of the flag at the start of the existing road.
            var positionNextToFlag = map.Position(9, 10);

            Assert.IsTrue(map.Paths(positionNextToFlag) != 0, "Test setup is broken.");
            Assert.IsFalse(map.CanRoadEndOnExistingRoad(map.MoveDown(positionNextToFlag), Direction.Up, PlayerIndex),
                "A flag must not be built next to an existing flag.");
        }

        [TestMethod]
        public void RoadWithObject_ShouldNotBeAllowedAsRoadEnd()
        {
            var map = CreateMapWithRoad();

            map.SetObject(RoadPosition(map), Map.Object.Tree0, -1);

            Assert.IsFalse(map.CanRoadEndOnExistingRoad(ApproachPosition(map), Direction.Up, PlayerIndex),
                "A flag must not be built where the land is not clear.");
        }

        [TestMethod]
        public void RoadOnForeignLand_ShouldNotBeAllowedAsRoadEnd()
        {
            var map = CreateMapWithRoad();

            map.SetOwner(RoadPosition(map), OtherPlayerIndex);

            Assert.IsFalse(map.CanRoadEndOnExistingRoad(ApproachPosition(map), Direction.Up, PlayerIndex),
                "A flag must not be built on foreign land.");
        }

        [TestMethod]
        public void RoadOnUnownedLand_ShouldNotBeAllowedAsRoadEnd()
        {
            var map = CreateMapWithRoad();

            map.DeleteOwner(RoadPosition(map));

            Assert.IsFalse(map.CanRoadEndOnExistingRoad(ApproachPosition(map), Direction.Up, PlayerIndex),
                "A flag must not be built on unowned land.");
        }

        [TestMethod]
        public void RoadStartingOnForeignLand_ShouldNotBeAllowedAsRoadEnd()
        {
            var map = CreateMapWithRoad();

            map.SetOwner(ApproachPosition(map), OtherPlayerIndex);

            Assert.IsFalse(map.CanRoadEndOnExistingRoad(ApproachPosition(map), Direction.Up, PlayerIndex),
                "A road must be built on the player's own land.");
        }

        [TestMethod]
        public void PositionWithoutRoad_ShouldNotBeHandled()
        {
            var map = CreateMapWithRoad();

            // A clear position is covered by IsRoadSegmentValid, not by this rule.
            var clearPosition = map.Position(11, 15);

            Assert.AreEqual(0u, map.Paths(clearPosition), "Test setup is broken.");
            Assert.IsFalse(map.CanRoadEndOnExistingRoad(map.MoveDown(clearPosition), Direction.Up, PlayerIndex),
                "Positions without a road are not handled by this rule.");
            Assert.IsTrue(map.IsRoadSegmentValid(map.MoveDown(clearPosition), Direction.Up, true),
                "Clear positions should still be valid road segments.");
        }

        [TestMethod]
        public void ExistingFlagOnRoad_ShouldNotBeHandled()
        {
            var map = CreateMapWithRoad();

            // Connecting to an existing flag is covered by IsRoadSegmentValid,
            // no new flag is needed there.
            var flagPosition = map.Position(8, 10);

            Assert.IsFalse(map.CanRoadEndOnExistingRoad(map.MoveDown(flagPosition), Direction.Up, PlayerIndex),
                "Existing flags are not handled by this rule.");
            Assert.IsTrue(map.IsRoadSegmentValid(map.MoveDown(flagPosition), Direction.Up, true),
                "Connecting to an existing flag should still be a valid road segment.");
        }

        [TestMethod]
        public void ExistingRoad_ShouldBeAValidFlagPosition()
        {
            var map = CreateMapWithRoad();

            // Building the flag will split the existing road (see Game.BuildFlag).
            Assert.IsTrue(map.CanBuildFlag(RoadPosition(map), PlayerIndex),
                "A flag should be buildable on an existing road.");
        }

        [TestMethod]
        public void Pathfinder_ShouldFindRoadEndingOnExistingRoad()
        {
            var map = CreateMapWithRoad();

            // The double click during road construction routes through the
            // pathfinder (see Viewport.HandleDoubleClick), so it has to be
            // able to reach a position on an existing road.
            var road = Pathfinder.FindShortestPath(map, ApproachPosition(map), RoadPosition(map), null, int.MaxValue, true);

            Assert.AreEqual(1u, road.Length, "The pathfinder should reach a position on an existing road.");
        }
    }
}
