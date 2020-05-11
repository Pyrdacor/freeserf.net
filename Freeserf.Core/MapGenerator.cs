/*
 * MapGenerator.cs - Map generator
 *
 * Copyright (C) 2013-2016  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net. freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with freeserf.net. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Freeserf
{
    using MapPos = UInt32;

    public abstract class MapGenerator
    {
        public enum HeightGenerator
        {
            Midpoints = 0,
            DiamondSquare
        }

        public abstract void Generate();

        public abstract uint GetHeight(MapPos position);
        public abstract Map.Terrain GetTypeUp(MapPos position);
        public abstract Map.Terrain GetTypeDown(MapPos position);
        public abstract Map.Object GetObject(MapPos position);
        public abstract Map.Minerals GetResourceType(MapPos position);
        public abstract int GetResourceAmount(MapPos position);
        public abstract Map.LandscapeTile[] GetLandscape();
    }

    // Classic map generator as in original game. 
    public class ClassicMapGenerator : MapGenerator
    {
        const uint DefaultMaxLakeArea = 14;
        const uint DefaultWaterLevel = 20;
        const int DefaultTerrainSpikyness = 0x9999;

        public ClassicMapGenerator(Map map, Random random)
        {
            this.random = random;
            int tileCount = (int)map.Geometry.TileCount;

            tiles = new Map.LandscapeTile[tileCount];
            tags = new int[tileCount];

            for (int i = 0; i < tileCount; ++i)
                tiles[i] = new Map.LandscapeTile();

            this.map = map;
        }

        public void Init(HeightGenerator heightGenerator, bool preserveBugs,
            uint maxLakeArea = DefaultMaxLakeArea,
            uint waterLevel = DefaultWaterLevel,
            int terrainSpikyness = DefaultTerrainSpikyness)
        {
            this.heightGenerator = heightGenerator;
            this.preserveBugs = preserveBugs;
            this.maxLakeArea = maxLakeArea;
            this.waterLevel = waterLevel;
            this.terrainSpikyness = terrainSpikyness;
        }

        public override void Generate()
        {
            random ^= new Random(0x5a5a, 0xa5a5, 0xc3c3);

            RandomInt();
            RandomInt();

            InitHeightsSquares();

            switch (heightGenerator)
            {
                case HeightGenerator.Midpoints:
                    InitHeightsMidpoints(); // Midpoint displacement algorithm 
                    break;
                case HeightGenerator.DiamondSquare:
                    InitHeightsDiamondSquare(); // Diamond square algorithm 
                    break;
                default:
                    Debug.NotReached();
                    break;
            }

            ClampHeights();
            CreateWaterBodies();
            HeightsRebase();
            InitTypes();
            RemoveIslands();
            HeightsRescale();

            // Adjust terrain types on shores
            ChangeShoreWaterType();
            ChangeShoreGrassType();

            // Create deserts
            CreateDeserts();

            // Create map objects (trees, boulders, etc.)
            CreateObjects();

            CreateMineralDeposits();

            CleanUp();
        }

        public override uint GetHeight(MapPos position)
        {
            return tiles[(int)position].Height;
        }

        public override Map.LandscapeTile[] GetLandscape()
        {
            return tiles;
        }

        public override Map.Object GetObject(MapPos position)
        {
            return tiles[(int)position].Object;
        }

        public override int GetResourceAmount(MapPos position)
        {
            return tiles[(int)position].ResourceAmount;
        }

        public override Map.Minerals GetResourceType(MapPos position)
        {
            return tiles[(int)position].Mineral;
        }

        public override Map.Terrain GetTypeDown(MapPos position)
        {
            return tiles[(int)position].TypeDown;
        }

        public override Map.Terrain GetTypeUp(MapPos position)
        {
            return tiles[(int)position].TypeUp;
        }

        Map map;
        Random random = new Random();

        readonly Map.LandscapeTile[] tiles;
        readonly int[] tags;
        HeightGenerator heightGenerator;
        bool preserveBugs;

        uint waterLevel = DefaultWaterLevel;
        uint maxLakeArea = DefaultMaxLakeArea;
        int terrainSpikyness = DefaultTerrainSpikyness;

        ushort RandomInt()
        {
            return random.Next();
        }

        /// <summary>
        /// Get a random position in the spiral pattern based at column, row. 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        MapPos PosAddSpirallyRandom(MapPos position, int mask)
        {
            return map.PositionAddSpirally(position, (uint)(RandomInt() & mask));
        }

        bool IsWaterTile(MapPos position)
        {
            return tiles[(int)position].TypeDown <= Map.Terrain.Water3 && tiles[(int)position].TypeUp <= Map.Terrain.Water3;
        }

        bool IsInWater(MapPos position)
        {
            return IsWaterTile(position) &&
                    IsWaterTile(map.MoveUpLeft(position)) &&
                    tiles[(int)map.MoveLeft(position)].TypeDown <= Map.Terrain.Water3 &&
                    tiles[(int)map.MoveUp(position)].TypeUp <= Map.Terrain.Water3;
        }

        /// <summary>
        /// Midpoint displacement map generator. This function initialises the height
        /// values in the corners of 16x16 squares.
        /// </summary>
        void InitHeightsSquares()
        {
            for (uint y = 0; y < map.Rows; y += 16)
            {
                for (uint x = 0; x < map.Columns; x += 16)
                {
                    int rndl = RandomInt() & 0xff;

                    tiles[(int)map.Position(x, y)].Height = Math.Min((uint)rndl, 250u);
                }
            }
        }

        int CalcHeightDisplacement(int avg, int base_, int offset)
        {
            int height = ((RandomInt() * base_) >> 16) - offset + avg;

            return Math.Max(0, Math.Min(height, 250));
        }

        /// <summary>
        /// Calculate height values of the subdivisions in the
        /// midpoint displacement algorithm.
        /// </summary>
        void InitHeightsMidpoints()
        {
            /*   This is the central part of the midpoint displacement algorithm.
                 The initial 16x16 squares are subdivided into 8x8 then 4x4 and so on,
                 until all positions in the map have a height value.

                 The random offset applied to the midpoints is based on r1 and r2.
                 The offset is a random value in [-r2; r1-r2). r1 controls the roughness of
                 the terrain; a larger value of r1 will result in rough terrain
                 while a smaller value will generate smoother terrain.

                 A high spikyness will result in sharp mountains and smooth valleys. A low
                 spikyness will result in smooth mountains and sharp valleys.
              */

            int randomValue = RandomInt();
            int r1 = 0x80 + (randomValue & 0x7f);
            int r2 = (r1 * terrainSpikyness) >> 16;

            for (uint i = 8; i > 0; i >>= 1)
            {
                for (uint y = 0; y < map.Rows; y += 2 * i)
                {
                    for (uint x = 0; x < map.Columns; x += 2 * i)
                    {
                        var position = map.Position(x, y);
                        uint height = tiles[(int)position].Height;

                        var positionRight = map.MoveRightN(position, 2 * (int)i);
                        var positionMidRight = map.MoveRightN(position, (int)i);
                        uint heightRight = tiles[(int)positionRight].Height;

                        if (preserveBugs)
                        {
                            // The intention was probably just to set hRight to the map height value,
                            // but the upper bits of rnd must be preserved in hRight in the first
                            // iteration to generate the same maps as the original game.
                            if (x == 0 && y == 0 && i == 8)
                                heightRight |= (uint)randomValue & 0xff00u;
                        }

                        tiles[(int)positionMidRight].Height = (uint)CalcHeightDisplacement((int)(height + heightRight) / 2, r1, r2);

                        var positionDown = map.MoveDownN(position, 2 * (int)i);
                        var positionMidDown = map.MoveDownN(position, (int)i);
                        uint heightDown = tiles[(int)positionDown].Height;
                        tiles[(int)positionMidDown].Height = (uint)CalcHeightDisplacement((int)(height + heightDown) / 2, r1, r2);

                        var positionDownRight = map.MoveRightN(map.MoveDownN(position, 2 * (int)i), 2 * (int)i);
                        var positionMidDownRight = map.MoveRightN(map.MoveDownN(position, (int)i), (int)i);
                        uint heightDownRight = tiles[(int)positionDownRight].Height;
                        tiles[(int)positionMidDownRight].Height = (uint)CalcHeightDisplacement((int)(height + heightDownRight) / 2, r1, r2);
                    }
                }

                r1 >>= 1;
                r2 >>= 1;
            }
        }

        void InitHeightsDiamondSquare()
        {
            /*   This is the central part of the diamond-square algorithm.
                 The squares are first subdivided into four new squares and
                 the height of the midpoint is calculated by averaging the corners and
                 adding a random offset. Each "diamond" that appears is then processed
                 in the same way.

                 The random offset applied to the midpoints is based on r1 and r2.
                 The offset is a random value in [-r2; r1-r2). r1 controls the roughness of
                 the terrain; a larger value of r1 will result in rough terrain
                 while a smaller value will generate smoother terrain.

                 A high spikyness will result in sharp mountains and smooth valleys. A low
                 spikyness will result in smooth mountains and sharp valleys.
              */

            int randomValue = RandomInt();
            int r1 = 0x80 + (randomValue & 0x7f);
            int r2 = (r1 * terrainSpikyness) >> 16;

            for (uint i = 8; i > 0; i >>= 1)
            {
                // Diamond step 
                for (uint y = 0; y < map.Rows; y += 2 * i)
                {
                    for (uint x = 0; x < map.Columns; x += 2 * i)
                    {
                        var position = map.Position(x, y);
                        uint height = tiles[(int)position].Height;

                        var positionRight = map.MoveRightN(position, 2 * (int)i);
                        int heightRight = (int)tiles[(int)positionRight].Height;

                        var positionDown = map.MoveDownN(position, 2 * (int)i);
                        int heightDown = (int)tiles[(int)positionDown].Height;

                        var positionDownRight = map.MoveRightN(map.MoveDownN(position, 2 * (int)i), 2 * (int)i);
                        int heightDownRight = (int)tiles[(int)positionDownRight].Height;

                        var positionMidDownRight = map.MoveRightN(map.MoveDownN(position, (int)i), (int)i);
                        int average = (int)(height + heightRight + heightDown + heightDownRight) / 4;
                        tiles[(int)positionMidDownRight].Height = (uint)CalcHeightDisplacement(average, r1, r2);
                    }
                }

                // Square step 
                for (uint y = 0; y < map.Rows; y += 2 * i)
                {
                    for (uint x = 0; x < map.Columns; x += 2 * i)
                    {
                        var position = map.Position(x, y);
                        int height = (int)tiles[(int)position].Height;

                        var positionRight = map.MoveRightN(position, 2 * (int)i);
                        int heightRight = (int)tiles[(int)positionRight].Height;

                        var positionDown = map.MoveDownN(position, 2 * (int)i);
                        int heightDown = (int)tiles[(int)positionDown].Height;

                        var positionUpRight = map.MoveRightN(map.MoveDownN(position, -(int)i), (int)i);
                        int heightUpRight = (int)tiles[(int)positionUpRight].Height;

                        var positionDownRight = map.MoveRightN(map.MoveDownN(position, (int)i), (int)i);
                        int heightDownRight = (int)tiles[(int)positionDownRight].Height;

                        var positionDownLeft = map.MoveRightN(map.MoveDownN(position, (int)i), -(int)i);
                        int heightDownLeft = (int)tiles[(int)positionDownLeft].Height;

                        var positionMidRight = map.MoveRightN(position, (int)i);
                        int averageRight = (height + heightRight + heightUpRight + heightDownRight) / 4;
                        tiles[(int)positionMidRight].Height = (uint)CalcHeightDisplacement(averageRight, r1, r2);

                        var positionMidDown = map.MoveDownN(position, (int)i);
                        int averageDown = (height + heightDown + heightDownLeft + heightDownRight) / 4;
                        tiles[(int)positionMidDown].Height = (uint)CalcHeightDisplacement(averageDown, r1, r2);
                    }
                }

                r1 >>= 1;
                r2 >>= 1;
            }
        }

        bool AdjustMapHeight(int height1, int height2, MapPos position)
        {
            if (Math.Abs(height1 - height2) > 32)
            {
                tiles[(int)position].Height = (uint)(height1 + ((height1 < height2) ? 32 : -32));
                return true;
            }

            return false;
        }

        // Ensure that map heights of adjacent fields are not too far apart. 
        void ClampHeights()
        {
            bool changed = true;

            while (changed)
            {
                changed = false;

                foreach (var position in map.Geometry)
                {
                    int height = (int)tiles[(int)position].Height;

                    var positionDown = map.MoveDown(position);
                    int heightDown = (int)tiles[(int)positionDown].Height;
                    changed |= AdjustMapHeight(height, heightDown, positionDown);

                    var positionDownRight = map.MoveDownRight(position);
                    int heightDownRight = (int)tiles[(int)positionDownRight].Height;
                    changed |= AdjustMapHeight(height, heightDownRight, positionDownRight);

                    var positionRight = map.MoveRight(position);
                    int heightRight = (int)tiles[(int)positionRight].Height;
                    changed |= AdjustMapHeight(height, heightRight, positionRight);
                }
            }
        }

        // Expand water around position.
        //
        // Expand water area by marking shores with 254 and water positions with 255.
        // Water (255) can only be expanded to a position where all six adjacent
        // positions are at or lower than the water level. When a position is marked
        // as water (255) the surrounding positions, that are not yet marked, are
        // changed to shore (254). Returns true only if the given position was
        // converted to water.
        bool ExpandWaterPosition(MapPos position)
        {
            bool expanding = false;
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                var newPosition = map.Move(position, direction);
                uint height = tiles[(int)newPosition].Height;

                if (waterLevel < height && height < 254)
                {
                    return false;
                }
                else if (height == 255)
                {
                    expanding = true;
                }
            }

            if (expanding)
            {
                tiles[(int)position].Height = 255;

                cycle = DirectionCycleCW.CreateDefault();

                foreach (var direction in cycle)
                {
                    var newPosition = map.Move(position, direction);

                    if (tiles[(int)newPosition].Height != 255)
                        tiles[(int)newPosition].Height = 254;
                }
            }

            return expanding;
        }

        // Try to expand area around position into a water body.
        //
        // After expanding, the water body will be tagged with the heights 253 for
        // positions in water and 252 for positions on the shore.
        void ExpandWaterBody(MapPos position)
        {
            // Check whether it is possible to expand from this position.
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                var newPosition = map.Move(position, direction);

                if (tiles[(int)newPosition].Height > waterLevel)
                {
                    // Expanding water from this position was not possible. Just raise the
                    // height to one above sea level.
                    tiles[(int)position].Height = 0;
                    return;
                }
            }

            // Initialize expansion
            tiles[(int)position].Height = 255;
            cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                var newPosition = map.Move(position, direction);

                tiles[(int)newPosition].Height = 254;
            }

            // Expand water until we are unable to expand any more or until the max
            // lake area limit has been reached.
            for (uint i = 0; i < maxLakeArea; ++i)
            {
                bool expanded = false;
                var newPosition = map.MoveRightN(position, (int)i + 1);

                cycle = new DirectionCycleCW(Direction.Down, 6);

                foreach (var direction in cycle)
                {
                    for (uint j = 0; j <= i; ++j)
                    {
                        expanded |= ExpandWaterPosition(newPosition);
                        newPosition = map.Move(newPosition, direction);
                    }
                }

                if (!expanded)
                    break;
            }

            // Change the water encoding from 255,254 to 253,252. This change means that
            // when expanding another lake, this area will look like an elevated plateau
            // at heights 252/253 and the other lake will not be able to expand into this
            // area. This keeps water bodies from growing larger than the max lake area.
            tiles[(int)position].Height -= 2;

            for (uint i = 0; i < maxLakeArea + 1; ++i)
            {
                var newPosition = map.MoveRightN(position, (int)i + 1);
                cycle = new DirectionCycleCW(Direction.Down, 6);

                foreach (var direction in cycle)
                {
                    for (uint j = 0; j <= i; ++j)
                    {
                        if (tiles[(int)newPosition].Height > 253)
                            tiles[(int)newPosition].Height -= 2;

                        newPosition = map.Move(newPosition, direction);
                    }
                }
            }
        }

        // Create water bodies on the map.
        //
        // Try to expand every position that is at or below the water level into a
        // body of water. After expanding bodies of water, the height of the positions
        // are changed such that the lowest points on the map are at water_level - 1
        // (marking water) and just above that the height is at water_level (marking
        // shore).
        void CreateWaterBodies()
        {
            for (uint height = 0; height <= waterLevel; ++height)
            {
                foreach (var position in map.Geometry)
                {
                    if (tiles[(int)position].Height == height)
                    {
                        ExpandWaterBody(position);
                    }
                }
            }

            // Map positions are marked in the previous loop.
            // 0: Above water level.
            // 252: Land at water level.
            // 253: Water.
            foreach (var position in map.Geometry)
            {
                int height = (int)tiles[(int)position].Height;

                switch (height)
                {
                    case 0:
                        tiles[(int)position].Height = waterLevel + 1;
                        break;
                    case 252:
                        tiles[(int)position].Height = waterLevel;
                        break;
                    case 253:
                        tiles[(int)position].Height = waterLevel - 1;
                        tiles[(int)position].Mineral = Map.Minerals.None;
                        tiles[(int)position].ResourceAmount = RandomInt() & 7; // Fish 
                        break;
                }
            }
        }

        // Adjust heights so zero height is sea level. 
        void HeightsRebase()
        {
            int height = (int)waterLevel - 1;

            foreach (var position in map.Geometry)
            {
                tiles[(int)position].Height = (uint)(tiles[(int)position].Height - height);
            }
        }

        static Map.Terrain CalcMapType(int hSum)
        {
            if (hSum < 3) return Map.Terrain.Water0;
            else if (hSum < 384) return Map.Terrain.Grass1;
            else if (hSum < 416) return Map.Terrain.Grass2;
            else if (hSum < 448) return Map.Terrain.Tundra0;
            else if (hSum < 480) return Map.Terrain.Tundra1;
            else if (hSum < 528) return Map.Terrain.Tundra2;
            else if (hSum < 560) return Map.Terrain.Snow0;
            return Map.Terrain.Snow1;
        }

        // Set type of map fields based on the height value. 
        void InitTypes()
        {
            foreach (var position in map.Geometry)
            {
                int h1 = (int)tiles[(int)position].Height;
                int h2 = (int)tiles[(int)map.MoveRight(position)].Height;
                int h3 = (int)tiles[(int)map.MoveDownRight(position)].Height;
                int h4 = (int)tiles[(int)map.MoveDown(position)].Height;
                tiles[(int)position].TypeUp = CalcMapType(h1 + h3 + h4);
                tiles[(int)position].TypeDown = CalcMapType(h1 + h2 + h3);
            }
        }

        void ClearAllTags()
        {
            foreach (var position in map.Geometry)
            {
                tags[(int)position] = 0;
            }
        }

        // Remove islands.
        //
        // Pick an initial map position, then search from there to see which other
        // positions on the map are reachable (over land) from that position. If the
        // reachable positions cover at least 1/4 of the map, then stop and convert any
        // position that was _not_ reached to water. Otherwise, keep trying new initial
        // positions.
        //
        // In most cases this will eliminate any island that covers less than 1/4 of
        // the map. However, since the markings are not reset after an initial
        // position has failed to expand to 1/4 of the map, it is still possible for
        // islands to survive if they by change happen to be in the area where the
        // first initial positions are chosen (around 0, 0).
        void RemoveIslands()
        {
            // Initially all positions are tagged with 0. When reached from another
            // position the tag is changed to 1, and later when that position is
            // itself expanded the tag is changed to 2.
            ClearAllTags();

            foreach (var position in map.Geometry)
            {
                if (tiles[(int)position].Height > 0 && tags[(int)position] == 0)
                {
                    tags[(int)position] = 1;

                    uint num = 0;
                    bool changed = true;

                    while (changed)
                    {
                        changed = false;

                        foreach (var otherPosition in map.Geometry)
                        {
                            if (tags[(int)otherPosition] == 1)
                            {
                                ++num;
                                tags[(int)otherPosition] = 2;

                                // The i'th flag will indicate whether a path on land from
                                // pos_in direction i is possible.
                                int flags = 0;
                                if (tiles[(int)otherPosition].TypeDown >= Map.Terrain.Grass0)
                                {
                                    flags |= 3;
                                }
                                if (tiles[(int)otherPosition].TypeUp >= Map.Terrain.Grass0)
                                {
                                    flags |= 6;
                                }
                                if (tiles[(int)map.MoveLeft(otherPosition)].TypeDown >= Map.Terrain.Grass0)
                                {
                                    flags |= 0xc;
                                }
                                if (tiles[(int)map.MoveUpLeft(otherPosition)].TypeUp >= Map.Terrain.Grass0)
                                {
                                    flags |= 0x18;
                                }
                                if (tiles[(int)map.MoveUpLeft(otherPosition)].TypeDown >= Map.Terrain.Grass0)
                                {
                                    flags |= 0x30;
                                }
                                if (tiles[(int)map.MoveUp(otherPosition)].TypeUp >= Map.Terrain.Grass0)
                                {
                                    flags |= 0x21;
                                }

                                // Mark positions following any valid direction on land.
                                var cycle = DirectionCycleCW.CreateDefault();

                                foreach (var direction in cycle)
                                {
                                    if (Misc.BitTest(flags, (int)direction))
                                    {
                                        if (tags[(int)map.Move(otherPosition, direction)] == 0)
                                        {
                                            tags[(int)map.Move(otherPosition, direction)] = 1;
                                            changed = true;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (4 * num >= map.Geometry.TileCount)
                        break;
                }
            }

            // Change every position that was not tagged (i.e. tag is 0) to water.
            foreach (var position in map.Geometry)
            {
                if (tiles[(int)position].Height > 0 && tags[(int)position] == 0)
                {
                    tiles[(int)position].Height = 0;
                    tiles[(int)position].TypeUp = Map.Terrain.Water0;
                    tiles[(int)position].TypeUp = Map.Terrain.Water0;

                    tiles[(int)map.MoveLeft(position)].TypeDown = Map.Terrain.Water0;
                    tiles[(int)map.MoveUpLeft(position)].TypeUp = Map.Terrain.Water0;
                    tiles[(int)map.MoveUpLeft(position)].TypeDown = Map.Terrain.Water0;
                    tiles[(int)map.MoveUp(position)].TypeUp = Map.Terrain.Water0;
                }
            }
        }

        // Rescale height values to be in [0;31]. 
        void HeightsRescale()
        {
            foreach (var position in map.Geometry)
            {
                tiles[(int)position].Height = (tiles[(int)position].Height + 6) >> 3;
            }
        }

        // Change terrain types based on a seed type in adjacent tiles.
        //
        // For every triangle, if the current type is old and any adjacent triangle
        // has type seed, then the triangle is changed into the new_ terrain type.
        void SeedTerrainType(Map.Terrain old, Map.Terrain seed, Map.Terrain new_)
        {
            foreach (var position in map.Geometry)
            {
                // Check that the central triangle is of type old (*), and that any
                // adjacent triangle is of type seed:
                //     ____
                //    /\  /\
                //   /__\/__\
                //  /\  /\  /\
                // /__\/*_\/__\
                // \  /\  /\  /
                //  \/__\/__\/
                //
                if (tiles[(int)position].TypeUp == old &&
                    (seed == tiles[(int)map.MoveUpLeft(position)].TypeDown ||
                     seed == tiles[(int)map.MoveUpLeft(position)].TypeUp ||
                     seed == tiles[(int)map.MoveUp(position)].TypeUp ||
                     seed == tiles[(int)map.MoveLeft(position)].TypeDown ||
                     seed == tiles[(int)map.MoveLeft(position)].TypeUp ||
                     seed == tiles[(int)position].TypeDown ||
                     seed == tiles[(int)map.MoveRight(position)].TypeUp ||
                     seed == tiles[(int)map.MoveLeft(map.MoveDown(position))].TypeDown ||
                     seed == tiles[(int)map.MoveDown(position)].TypeDown ||
                     seed == tiles[(int)map.MoveDown(position)].TypeUp ||
                     seed == tiles[(int)map.MoveDownRight(position)].TypeDown ||
                     seed == tiles[(int)map.MoveDownRight(position)].TypeUp))
                {
                    tiles[(int)position].TypeUp = new_;
                }

                // Check that the central triangle is of type old (*), and that any
                // adjacent triangle is of type seed:
                //   ________
                //  /\  /\  /\
                // /__\/__\/__\
                // \  /\* /\  /
                //  \/__\/__\/
                //   \  /\  /
                //    \/__\/
                //
                if (tiles[(int)position].TypeDown == old &&
                    (seed == tiles[(int)map.MoveUpLeft(position)].TypeDown ||
                     seed == tiles[(int)map.MoveUpLeft(position)].TypeUp ||
                     seed == tiles[(int)map.MoveUp(position)].TypeDown ||
                     seed == tiles[(int)map.MoveUp(position)].TypeUp ||
                     seed == tiles[(int)map.MoveRight(map.MoveUp(position))].TypeUp ||
                     seed == tiles[(int)map.MoveLeft(position)].TypeDown ||
                     seed == tiles[(int)position].TypeUp ||
                     seed == tiles[(int)map.MoveRight(position)].TypeDown ||
                     seed == tiles[(int)map.MoveRight(position)].TypeUp ||
                     seed == tiles[(int)map.MoveDown(position)].TypeDown ||
                     seed == tiles[(int)map.MoveDownRight(position)].TypeDown ||
                     seed == tiles[(int)map.MoveDownRight(position)].TypeUp))
                {
                    tiles[(int)position].TypeDown = new_;
                }
            }
        }

        // Change water type based on closeness to shore.
        //
        // Change type from TerrainWater0 to higher water (1-3) types based on
        // closeness to the shore. The water closest to the shore will become
        // TerrainWater3.
        void ChangeShoreWaterType()
        {
            SeedTerrainType(Map.Terrain.Water0, Map.Terrain.Grass1, Map.Terrain.Water3);
            SeedTerrainType(Map.Terrain.Water0, Map.Terrain.Water3, Map.Terrain.Water2);
            SeedTerrainType(Map.Terrain.Water0, Map.Terrain.Water2, Map.Terrain.Water1);
        }

        // Change grass type of shore to TerrainGrass0.
        //
        // Change type from TerrainGrass1 to TerrainGrass0 where the tiles are
        // adjacent to water.
        void ChangeShoreGrassType()
        {
            SeedTerrainType(Map.Terrain.Grass1, Map.Terrain.Water3, Map.Terrain.Grass0);
        }

        // Check whether large down-triangle is suitable for desert.
        //
        // The large down-triangle at position A is made up of the following
        // triangular pieces. The method returns true only if all terrain types
        // within the triangle are either TerrainGrass1 or TerrainDesert2.
        //
        // __ A ___
        // \  /\  /
        //  \/__\/
        //   \  /
        //    \/
        //
        bool CheckDesertDownTriangle(MapPos position)
        {
            var typeDown = tiles[(int)position].TypeDown;
            var typeUp = tiles[(int)position].TypeUp;

            if (typeDown != Map.Terrain.Grass1 && typeDown != Map.Terrain.Desert2)
            {
                return false;
            }

            if (typeUp != Map.Terrain.Grass1 && typeUp != Map.Terrain.Desert2)
            {
                return false;
            }

            typeDown = tiles[(int)map.MoveLeft(position)].TypeDown;

            if (typeDown != Map.Terrain.Grass1 && typeDown != Map.Terrain.Desert2)
            {
                return false;
            }

            typeDown = tiles[(int)map.MoveDown(position)].TypeDown;

            if (typeDown != Map.Terrain.Grass1 && typeDown != Map.Terrain.Desert2)
            {
                return false;
            }

            return true;
        }

        // Check whether large up-triangle is suitable for desert.
        //
        // The large up-triangle at position A is made up of the following
        // triangular pieces. The method returns true only if all terrain types
        // within the triangle are either TerrainGrass1 or TerrainDesert2.
        //
        //      /\
        //   A /__\
        //    /\  /\
        //   /__\/__\
        //
        bool CheckDesertUpTriangle(MapPos position)
        {
            var typeDown = tiles[(int)position].TypeDown;
            var typeUp = tiles[(int)position].TypeUp;

            if (typeDown != Map.Terrain.Grass1 && typeDown != Map.Terrain.Desert2)
            {
                return false;
            }

            if (typeUp != Map.Terrain.Grass1 && typeUp != Map.Terrain.Desert2)
            {
                return false;
            }

            typeUp = tiles[(int)map.MoveRight(position)].TypeUp;

            if (typeUp != Map.Terrain.Grass1 && typeUp != Map.Terrain.Desert2)
            {
                return false;
            }

            typeUp = tiles[(int)map.MoveUp(position)].TypeUp;

            if (typeUp != Map.Terrain.Grass1 && typeUp != Map.Terrain.Desert2)
            {
                return false;
            }

            return true;
        }

        // Create deserts.
        void CreateDeserts()
        {
            // Initialize random areas of desert based on spiral pattern.
            // Only TerrainGrass1 triangles will be converted to desert.
            for (uint i = 0; i < map.RegionCount; ++i)
            {
                for (int tryIndex = 0; tryIndex < 200; ++tryIndex)
                {
                    var randomPosition = map.GetRandomCoordinate(random);

                    if (tiles[(int)randomPosition].TypeUp == Map.Terrain.Grass1 &&
                        tiles[(int)randomPosition].TypeDown == Map.Terrain.Grass1)
                    {
                        for (int index = 255; index >= 0; index--)
                        {
                            var position = map.PositionAddSpirally(randomPosition, (uint)index);

                            if (CheckDesertDownTriangle(position))
                            {
                                tiles[(int)position].TypeUp = Map.Terrain.Desert2;
                            }

                            if (CheckDesertUpTriangle(position))
                            {
                                tiles[(int)position].TypeDown = Map.Terrain.Desert2;
                            }
                        }

                        break;
                    }
                }
            }

            // Convert outer triangles in the desert areas into a gradual transition
            // through TerrainGrass3, TerrainDesert0, TerrainDesert1 to TerrainDesert2.
            SeedTerrainType(Map.Terrain.Desert2, Map.Terrain.Grass1, Map.Terrain.Grass3);
            SeedTerrainType(Map.Terrain.Desert2, Map.Terrain.Grass3, Map.Terrain.Desert0);
            SeedTerrainType(Map.Terrain.Desert2, Map.Terrain.Desert0, Map.Terrain.Desert1);

            // Convert all triangles in the TerrainGrass3 - TerrainDesert1 range to
            // TerrainGrass1. This reduces the size of the desert areas to the core
            // that was made up of TerrainDesert2.
            foreach (var position in map.Geometry)
            {
                var typeDown = tiles[(int)position].TypeDown;
                var typeUp = tiles[(int)position].TypeUp;

                if (typeDown >= Map.Terrain.Grass3 && typeDown <= Map.Terrain.Desert1)
                {
                    tiles[(int)position].TypeDown = Map.Terrain.Grass1;
                }

                if (typeUp >= Map.Terrain.Grass3 && typeUp <= Map.Terrain.Desert1)
                {
                    tiles[(int)position].TypeUp = Map.Terrain.Grass1;
                }
            }

            // Restore the gradual transition from TerrainGrass3 to TerrainDesert2 around
            // the desert.
            SeedTerrainType(Map.Terrain.Grass1, Map.Terrain.Desert2, Map.Terrain.Desert1);
            SeedTerrainType(Map.Terrain.Grass1, Map.Terrain.Desert1, Map.Terrain.Desert0);
            SeedTerrainType(Map.Terrain.Grass1, Map.Terrain.Desert0, Map.Terrain.Grass3);
        }

        // Put crosses on top of mountains. 
        void CreateCrosses()
        {
            foreach (var position in map.Geometry)
            {
                var height = tiles[(int)position].Height;

                if (height >= 26 &&
                    height >= tiles[(int)map.MoveRight(position)].Height &&
                    height >= tiles[(int)map.MoveDownRight(position)].Height &&
                    height >= tiles[(int)map.MoveDown(position)].Height &&
                    height > tiles[(int)map.MoveLeft(position)].Height &&
                    height > tiles[(int)map.MoveUpLeft(position)].Height &&
                    height > tiles[(int)map.MoveUp(position)].Height)
                {
                    tiles[(int)position].Object = Map.Object.Cross;
                }
            }
        }

        void CreateObjects()
        {
            int regions = (int)map.RegionCount;

            CreateCrosses();

            // Add either tree or pine.
            CreateRandomObjectClusters(
              regions * 8, 10, 0xff, Map.Terrain.Grass1, Map.Terrain.Grass2,
              Map.Object.Tree0, 0xf);

            // Add only trees.
            CreateRandomObjectClusters(
              regions, 45, 0x3f, Map.Terrain.Grass1, Map.Terrain.Grass2,
              Map.Object.Tree0, 0x7);

            // Add only pines.
            CreateRandomObjectClusters(
              regions, 30, 0x3f, Map.Terrain.Grass0, Map.Terrain.Grass2,
              Map.Object.Pine0, 0x7);

            // Add either tree or pine.
            CreateRandomObjectClusters(
              regions, 20, 0x7f, Map.Terrain.Grass1, Map.Terrain.Grass2,
              Map.Object.Tree0, 0xf);

            // Create dense clusters of stone.
            CreateRandomObjectClusters(
              regions, 40, 0x3f, Map.Terrain.Grass1, Map.Terrain.Grass2,
              Map.Object.Stone0, 0x7);

            // Create sparse clusters.
            CreateRandomObjectClusters(
              regions, 15, 0xff, Map.Terrain.Grass1, Map.Terrain.Grass2,
              Map.Object.Stone0, 0x7);

            // Create dead trees.
            CreateRandomObjectClusters(
              regions, 2, 0xff, Map.Terrain.Grass1, Map.Terrain.Grass2,
              Map.Object.DeadTree, 0);

            // Create sandstone boulders.
            CreateRandomObjectClusters(
              regions, 6, 0xff, Map.Terrain.Grass1, Map.Terrain.Grass2,
              Map.Object.Sandstone0, 0x1);

            // Create trees submerged in water.
            CreateRandomObjectClusters(
              regions, 50, 0x7f, Map.Terrain.Water2, Map.Terrain.Water3,
              Map.Object.WaterTree0, 0x3);

            // Create tree stubs.
            CreateRandomObjectClusters(
              regions, 5, 0xff, Map.Terrain.Grass1, Map.Terrain.Grass2,
              Map.Object.Stub, 0);

            // Create small boulders.
            CreateRandomObjectClusters(
              regions, 10, 0xff, Map.Terrain.Grass1, Map.Terrain.Grass2,
              Map.Object.Stone, 0x1);

            // Create animal cadavers in desert.
            CreateRandomObjectClusters(
              regions, 2, 0xf, Map.Terrain.Desert2, Map.Terrain.Desert2,
              Map.Object.Cadaver0, 0x1);

            // Create cacti in desert.
            CreateRandomObjectClusters(
              regions, 6, 0x7f, Map.Terrain.Desert0, Map.Terrain.Desert2,
              Map.Object.Cactus0, 0x1);

            // Create boulders submerged in water.
            CreateRandomObjectClusters(
              regions, 8, 0x7f, Map.Terrain.Water0, Map.Terrain.Water2,
              Map.Object.WaterStone0, 0x1);

            // Create palm trees in desert.
            CreateRandomObjectClusters(
              regions, 6, 0x3f, Map.Terrain.Desert2, Map.Terrain.Desert2,
              Map.Object.Palm0, 0x3);
        }

        /* Check that hexagon has tile types in range.

           Check whether the hexagon at position has triangles of types between min and max,
           both inclusive. Return false if not all triangles are in this range,
           otherwise true.

           NOTE: This function has a quirk which is enabled by preserve_bugs. When this
           quirk is enabled, one of the tiles that is checked is not in the hexagon but
           is instead an adjacent tile. This is necessary to generate original game
           maps. */
        bool HexagonTypesInRange(MapPos position, Map.Terrain min, Map.Terrain max)
        {
            var typeDown = tiles[(int)position].TypeDown;
            var typeUp = tiles[(int)position].TypeUp;

            if (typeDown < min || typeDown > max)
                return false;
            if (typeUp < min || typeUp > max)
                return false;

            typeDown = tiles[(int)map.MoveLeft(position)].TypeDown;

            if (typeDown < min || typeDown > max)
                return false;

            typeDown = tiles[(int)map.MoveUpLeft(position)].TypeDown;
            typeUp = tiles[(int)map.MoveUpLeft(position)].TypeUp;

            if (typeDown < min || typeDown > max)
                return false;
            if (typeUp < min || typeUp > max)
                return false;

            // Should be checkeing the up tri type. 
            if (preserveBugs)
            {
                typeDown = tiles[(int)map.MoveUp(position)].TypeDown;

                if (typeDown < min || typeDown > max)
                    return false;
            }
            else
            {
                typeUp = tiles[(int)map.MoveUp(position)].TypeUp;

                if (typeUp < min || typeUp > max)
                    return false;
            }

            return true;
        }

        /* Create clusters of map objects.

           Tries to create num_clusters of objects in random locations on the map.
           Each cluster has up to objs_in_cluster objects. The pos_mask is used in
           the call to pos_add_spirally_random to determine the max cluster size. The
           type_min and type_max determine the range (both inclusive) of terrain
           types that must appear around a position to be elegible for placement of
           an object. The obj_base determines the first object type that can be placed
           and the obj_mask specifies a mask on a random integer that is added to the
           base to obtain the final object type.
        */
        void CreateRandomObjectClusters(int numClusters, int objectsInCluster,
                                        int positionMaks, Map.Terrain typeMin,
                                        Map.Terrain typeMax, Map.Object objectBase,
                                        int objectMask)
        {
            for (int i = 0; i < numClusters; ++i)
            {
                for (int tryIndex = 0; tryIndex < 100; ++tryIndex)
                {
                    var randomPosition = map.GetRandomCoordinate(random);

                    if (HexagonTypesInRange(randomPosition, typeMin, typeMax))
                    {
                        for (int j = 0; j < objectsInCluster; j++)
                        {
                            var position = PosAddSpirallyRandom(randomPosition, positionMaks);

                            if (HexagonTypesInRange(position, typeMin, typeMax) &&
                                tiles[(int)position].Object == Map.Object.None)
                            {
                                tiles[(int)position].Object = objectBase + (RandomInt() & objectMask);
                            }
                        }
                        break;
                    }
                }
            }
        }

        // Expand a cluster of minerals.
        void ExpandMineralCluster(int iterations, MapPos initialPosition, ref int index, int amount, Map.Minerals type)
        {
            for (int i = 0; i < iterations; ++i)
            {
                var position = map.PositionAddSpirally(initialPosition, (uint)index);

                ++index;

                if (tiles[(int)position].Mineral == Map.Minerals.None ||
                    tiles[(int)position].ResourceAmount < amount)
                {
                    tiles[(int)position].Mineral = type;
                    tiles[(int)position].ResourceAmount = amount;
                }
            }
        }

        static readonly int[] Iterations = new int[] { 1, 6, 12, 18, 24, 30 };

        // Create random clusters of mineral deposits.
        //
        // Tries to create num_clusters of minerals of the given type. The terrain type
        // around a position must be in the min, max range (both inclusive) for a
        // resource to be deposited.
        void CreateRandomMineralClusters(uint numClusters, Map.Minerals type, Map.Terrain min, Map.Terrain max)
        {
            for (uint i = 0; i < numClusters; ++i)
            {
                for (int tryIndex = 0; tryIndex < 100; ++tryIndex)
                {
                    var position = map.GetRandomCoordinate(random);

                    if (HexagonTypesInRange(position, min, max))
                    {
                        int index = 0;
                        int count = 2 + ((RandomInt() >> 2) & 3);

                        for (int j = 0; j < count; j++)
                        {
                            int amount = 4 * (count - j);

                            ExpandMineralCluster(Iterations[j], position, ref index, amount, type);
                        }

                        break;
                    }
                }
            }
        }

        struct Deposit
        {
            public uint Mult;
            public Map.Minerals Mineral;
        }

        static readonly Deposit[] Deposits = new Deposit[4]
        {
            new Deposit { Mult = 9, Mineral = Map.Minerals.Coal },
            new Deposit { Mult = 4, Mineral = Map.Minerals.Iron },
            new Deposit { Mult = 2, Mineral = Map.Minerals.Gold },
            new Deposit { Mult = 2, Mineral = Map.Minerals.Stone }
        };

        // Initialize mineral deposits in the ground.
        void CreateMineralDeposits()
        {
            uint regions = map.RegionCount;

            foreach (var deposit in Deposits)
            {
                CreateRandomMineralClusters(regions * deposit.Mult, deposit.Mineral, Map.Terrain.Tundra0, Map.Terrain.Snow0);
            }
        }

        void CleanUp()
        {
            /* Make sure that it is always possible to walk around
               any impassable objects. This also clears water obstacles
               except in certain positions near the shore. */
            foreach (var position in map.Geometry)
            {
                if (Map.MapSpaceFromObject[(int)tiles[(int)position].Object] >= Map.Space.Impassable)
                {
                    // Due to a quirk in the original game the three adjacent positions
                    // were not checked directly whether they were impassable but instead
                    // another flag was used to mark the position impassable. This flag
                    // was only initialzed for water positions before this loop and was
                    // initialized as part of this same loop for non-water positions. For
                    // this reason, the check for impassable spaces would never succeed
                    // under two particular conditions at the map edge:
                    // 1) x == 0 && d == DirectionLeft
                    // 2) y == 0 && (d == DirectionUp || d == DirectionUpLeft)
                    var cycle = new DirectionCycleCW(Direction.Left, 3);

                    foreach (var direction in cycle)
                    {
                        var otherPosition = map.Move(position, direction);
                        var space = Map.MapSpaceFromObject[(int)tiles[(int)otherPosition].Object];

                        bool check_impassable = false;
                        if (!(map.PositionColumn(position) == 0 && direction == Direction.Left) &&
                            !((direction == Direction.Up || direction == Direction.UpLeft) &&
                              map.PositionRow(position) == 0))
                        {
                            check_impassable = space >= Map.Space.Impassable;
                        }

                        if (IsInWater(otherPosition) || check_impassable)
                        {
                            tiles[(int)position].Object = Map.Object.None;
                            break;
                        }
                    }
                }
            }
        }
    }

    // Classic map generator that generates identical maps for missions. 
    public class ClassicMissionMapGenerator : ClassicMapGenerator
    {
        public ClassicMissionMapGenerator(Map map, Random random)
            : base(map, random)
        {

        }

        public void Init()
        {
            Init(HeightGenerator.Midpoints, true);
        }
    }
}
