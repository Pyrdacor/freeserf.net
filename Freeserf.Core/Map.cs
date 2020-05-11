/*
 * Map.cs - Map data and map update functions
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

/* Basically the map is constructed from a regular, square grid, with
  rows and columns, except that the grid is actually sheared like this:
  http://mathworld.wolfram.com/Polyrhomb.html
  This is the foundational 2D grid for the map, where each vertex can be
  identified by an integer column and row (commonly encoded as MapPos).

  Each tile has the shape of a rhombus:
     A ______ B
      /\    /
     /  \  /
  C /____\/ D

  but is actually composed of two triangles called "up" (a,c,d) and
  "down" (a,b,d). A serf can move on the perimeter of any of these
  triangles. Each vertex has various properties associated with it,
  among others a height value which means that the 3D landscape is
  defined by these points in (column, row, height)-space.

  Map elevation and type
  ----------------------
  The type of terrain is determined by either the elevation or the adjacency
  to other terrain types when the map is generated. The type is encoded
  separately from the elevation so it is only the map generator enforcing this
  correlation. The elevation (height) values range in 0-31 while the type
  ranges in 0-15.

  Terrain types:
  - 0-3: Water (range encodes adjacency to shore)
  - 4-7: Grass (4=adjacency to water, 5=only tile that allows large buildings,
                6-7=elevation based)
  - 8-10: Desert (range encodes adjacency to grass)
  - 11-13: Tundra (elevation based)
  - 14-15: Snow (elevation based)

  For water tiles, desert tiles and grass tile 4, the ranges of values are
  used to encode distance to other terrains. For example, type 4 grass is
  adjacent to at least one water tile and type 3 water is adjacent to at
  least one grass tile. Type 2 water is adjacent to at least one type 3 water,
  type 1 water is adjacent to at least one type 2 water, and so on. The lower
  desert tiles (8) are close to grass while the higher (10) are at the center
  of the desert. The higher grass tiles (5-7), tundra tiles, and snow tiles
  are determined by elevation and _not_ by adjacency.
*/

using Freeserf.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf
{
    using ChangeHandlers = List<Map.Handler>;
    using Directions = Stack<Direction>;
    using MapPos = UInt32;

    public class Road : IEquatable<Road>
    {
        const uint MaxLength = 256;

        public Road()
        {
            StartPosition = Global.INVALID_MAPPOS;
        }

        public Road Copy()
        {
            var copy = new Road();
            copy.StartPosition = StartPosition;
            copy.EndPosition = EndPosition;
            copy.Directions = new Directions(Directions.Reverse()); // enumerator of stack is reversed order
            copy.Cost = Cost;
            return copy;
        }

        public static Road CreateBuildingRoad(Map map, MapPos flagPosition)
        {
            var road = new Road();

            // these roads will have a cost of 0
            road.Start(flagPosition);
            road.Extend(map, Direction.UpLeft);

            return road;
        }

        public static Road CreateRoadFromMapPath(Map map, MapPos start, Direction startDirection)
        {
            if (!map.HasPath(start, startDirection))
                throw new ExceptionFreeserf(ErrorSystemType.Map, "Invalid map path.");

            var road = new Road();
            var position = start;
            var direction = startDirection;

            road.Start(start);

            do
            {
                road.Cost += Pathfinder.ActualCost(map, position, direction);
                road.Extend(map, direction);
                position = map.Move(position, direction);

                var cycle = DirectionCycleCW.CreateDefault();

                foreach (var nextDirection in cycle)
                {
                    if (nextDirection != direction.Reverse() && map.HasPath(position, nextDirection))
                    {
                        direction = nextDirection;
                        break;
                    }
                }
            }
            while (!map.HasFlag(position) && !map.HasBuilding(position));

            if (map.HasBuilding(position) || map.HasBuilding(start))
                road.Cost = 0;

            return road;
        }

        public Road Reverse(Map map)
        {
            var road = new Road();

            road.Start(EndPosition);

            foreach (var direction in Directions.ToList())
                road.Extend(map, direction.Reverse());

            road.Cost = Cost;

            return road;
        }

        public bool Valid => StartPosition != Global.INVALID_MAPPOS && EndPosition != Global.INVALID_MAPPOS;
        public MapPos StartPosition { get; private set; } = Global.INVALID_MAPPOS;
        public MapPos EndPosition { get; private set; } = Global.INVALID_MAPPOS;
        public Directions Directions { get; private set; } = new Directions();
        public uint Length => (uint)Directions.Count;
        public Direction Last => Directions.Peek();
        public bool Extendable => Length < MaxLength;
        public uint Cost { get; internal set; } = 0;

        public void Invalidate()
        {
            StartPosition = Global.INVALID_MAPPOS;
            EndPosition = Global.INVALID_MAPPOS;
            Directions.Clear();
            Cost = 0;
        }

        public void Start(MapPos start)
        {
            StartPosition = start;
            EndPosition = start;
            Cost = 0;
        }

        public bool IsValidExtension(Map map, Direction direction)
        {
            if (IsUndo(direction))
            {
                return false;
            }

            // Check that road does not cross itself. 
            var extendedEnd = map.Move(EndPosition, direction);
            return !HasPosition(map, extendedEnd);
        }

        public bool IsUndo(Direction direction)
        {
            return Length > 0 && Last == direction.Reverse();
        }

        public bool Extend(Map map, Direction direction)
        {
            if (StartPosition == Global.INVALID_MAPPOS)
            {
                return false;
            }

            if (EndPosition == Global.INVALID_MAPPOS)
            {
                if (Length != 0)
                    throw new ExceptionFreeserf(ErrorSystemType.Map, "Invalid road data");

                EndPosition = StartPosition;
            }

            EndPosition = map.Move(EndPosition, direction);
            Directions.Push(direction);

            return true;
        }

        public bool Extend(Map map, Road road)
        {
            if (StartPosition == Global.INVALID_MAPPOS || road == null || !road.Valid)
            {
                return false;
            }

            if (StartPosition == road.StartPosition) // road may be reversed
            {
                road = road.Reverse(map);
            }

            if (EndPosition != road.StartPosition)
            {
                return false;
            }

            foreach (var direction in road.Directions)
            {
                EndPosition = map.Move(EndPosition, direction);
                Directions.Push(direction);
            }

            return true;
        }

        public bool Undo(Map map)
        {
            if (StartPosition == Global.INVALID_MAPPOS)
            {
                return false;
            }

            EndPosition = map.Move(EndPosition, Directions.Pop().Reverse());

            if (Length == 0)
            {
                StartPosition = Global.INVALID_MAPPOS;
                EndPosition = Global.INVALID_MAPPOS;
            }

            return true;
        }

        public bool HasPosition(Map map, MapPos position)
        {
            var result = StartPosition;

            foreach (var direction in Directions.Reverse())
            {
                if (result == position)
                {
                    return true;
                }

                result = map.Move(result, direction);
            }

            return result == position;
        }

        public bool IsWaterPath(Map map)
        {
            if (!Valid || Directions.Count == 0)
                return false;

            var secondPosition = map.Move(StartPosition, Directions.Peek());

            return map.IsInWater(secondPosition);
        }

        public bool Equals(Road other)
        {
            if (other == null)
                return false;

            return StartPosition == other.StartPosition &&
                   EndPosition == other.EndPosition &&
                   Length == other.Length;
        }
    }

    public class Map : IEquatable<Map>
    {
        const int SAVE_MAP_TILE_SIZE = 16;

        public enum Object
        {
            None = 0,
            Flag,
            SmallBuilding,
            LargeBuilding,
            Castle,

            Tree0 = 8,
            Tree1,
            Tree2, // 10 
            Tree3,
            Tree4,
            Tree5,
            Tree6,
            Tree7, // 15 

            Pine0,
            Pine1,
            Pine2,
            Pine3,
            Pine4, // 20 
            Pine5,
            Pine6,
            Pine7,

            Palm0,
            Palm1, // 25 
            Palm2,
            Palm3,

            WaterTree0,
            WaterTree1,
            WaterTree2, // 30 
            WaterTree3,

            Stone0 = 72,
            Stone1,
            Stone2,
            Stone3, // 75 
            Stone4,
            Stone5,
            Stone6,
            Stone7,

            Sandstone0, // 80 
            Sandstone1,

            Cross,
            Stub,

            Stone,
            Sandstone3, // 85 

            Cadaver0,
            Cadaver1,

            WaterStone0,
            WaterStone1,

            Cactus0, // 90 
            Cactus1,

            DeadTree,

            FelledPine0,
            FelledPine1,
            FelledPine2, // 95 
            FelledPine3,
            FelledPine4,

            FelledTree0,
            FelledTree1,
            FelledTree2, // 100 
            FelledTree3,
            FelledTree4,

            NewPine,
            NewTree,

            Seeds0, // 105 
            Seeds1,
            Seeds2,
            Seeds3,
            Seeds4,
            Seeds5, // 110 
            FieldExpired,

            SignLargeGold,
            SignSmallGold,
            SignLargeIron,
            SignSmallIron, // 115 
            SignLargeCoal,
            SignSmallCoal,
            SignLargeStone,
            SignSmallStone,

            SignEmpty, // 120 

            Field0,
            Field1,
            Field2,
            Field3,
            Field4, // 125 
            Field5,
            Object127
        }

        public enum Minerals
        {
            None = 0,
            Gold,
            Iron,
            Coal,
            Stone
        }

        /* A map space can be OPEN which means that
           a building can be constructed in the space.
           A FILLED space can be passed by a serf, but
           nothing can be built in this space except roads.
           A SEMIPASSABLE space is like FILLED but no roads
           can be built. A IMPASSABLE space can neither be
           used for contructions nor passed by serfs. */
        public enum Space
        {
            Open = 0,
            Filled,
            Semipassable,
            Impassable,
        }

        public enum Terrain
        {
            Water0 = 0,
            Water1,
            Water2,
            Water3,
            Grass0,  // 4
            Grass1,
            Grass2,
            Grass3,
            Desert0,  // 8
            Desert1,
            Desert2,
            Tundra0,  // 11
            Tundra1,
            Tundra2,
            Snow0,  // 14
            Snow1
        }


        #region Spiral Pattern

        static readonly int[] SpiralPattern = new[]
        {
            0, 0,
            1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            3, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            4, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            4, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            4, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            5, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            5, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            5, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            5, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            6, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            6, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            6, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            6, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            6, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            7, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            7, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            7, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            7, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            7, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            7, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            8, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            8, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            8, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            8, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            8, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            8, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            8, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            9, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            9, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            9, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            9, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            9, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            9, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            9, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            16, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            24, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            24, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            24, 16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };
        static bool SpiralPatternInitialized = false;

        // Initialize the global spiral_pattern. 
        static void InitSpiralPattern()
        {
            if (SpiralPatternInitialized)
            {
                return;
            }

            int[] spiralMatrix = new[]
            {
                1,  0,  0,  1,
                1,  1, -1,  0,
                0,  1, -1, -1,
                -1,  0,  0, -1,
                -1, -1,  1,  0,
                0, -1,  1,  1
            };

            for (int i = 0; i < 49; i++)
            {
                int x = SpiralPattern[2 + 12 * i];
                int y = SpiralPattern[2 + 12 * i + 1];

                for (int j = 0; j < 6; j++)
                {
                    SpiralPattern[2 + 12 * i + 2 * j] = x * spiralMatrix[4 * j + 0] + y * spiralMatrix[4 * j + 2];
                    SpiralPattern[2 + 12 * i + 2 * j + 1] = x * spiralMatrix[4 * j + 1] + y * spiralMatrix[4 * j + 3];
                }
            }

            SpiralPatternInitialized = true;
        }

        public static int[] GetSpiralPattern()
        {
            return SpiralPattern;
        }


        #endregion


        public abstract class Handler
        {
            public abstract void OnHeightChanged(MapPos position);
            public abstract void OnObjectChanged(MapPos position);
            public abstract void OnObjectPlaced(MapPos position);
            public abstract void OnObjectExchanged(MapPos position, Map.Object oldObject, Map.Object newObject);
            public abstract void OnRoadSegmentPlaced(MapPos position, Direction direction);
            public abstract void OnRoadSegmentDeleted(MapPos position, Direction direction);
        }

        public class LandscapeTile : IEquatable<LandscapeTile>
        {
            // Landscape filds
            public uint Height = 0;
            public Terrain TypeUp = Terrain.Water0;
            public Terrain TypeDown = Terrain.Water0;
            public Minerals Mineral = Minerals.None;
            public int ResourceAmount = 0;
            // Mingled fields
            public Object Object = Object.None;

            public bool Equals(LandscapeTile other)
            {
                return this == other;
            }

            public static bool operator ==(LandscapeTile self, LandscapeTile other)
            {
                return self.Height == other.Height &&
                    self.TypeUp == other.TypeUp &&
                    self.TypeDown == other.TypeDown &&
                    self.Mineral == other.Mineral &&
                    self.ResourceAmount == other.ResourceAmount &&
                    self.Object == other.Object;
            }

            public static bool operator !=(LandscapeTile self, LandscapeTile other)
            {
                return !(self == other);
            }

            public override bool Equals(object obj)
            {
                return Equals((LandscapeTile)obj);
            }

            public override int GetHashCode()
            {
                unchecked // Overflow is fine, just wrap
                {
                    int hash = 17;

                    hash = hash * 23 + Height.GetHashCode();
                    hash = hash * 23 + TypeUp.GetHashCode();
                    hash = hash * 23 + TypeDown.GetHashCode();
                    hash = hash * 23 + Mineral.GetHashCode();
                    hash = hash * 23 + ResourceAmount.GetHashCode();
                    hash = hash * 23 + Object.GetHashCode();

                    return hash;
                }
            }
        }

        public class UpdateState : IEquatable<UpdateState>
        {
            public int RemoveSignsCounter = 0;
            public ushort LastTick = 0;
            public int Counter = 0;
            public MapPos InitialPosition = 0;

            public bool Equals(UpdateState other)
            {
                return this == other;
            }

            public static bool operator ==(UpdateState self, UpdateState other)
            {
                return self.RemoveSignsCounter == other.RemoveSignsCounter &&
                    self.LastTick == other.LastTick &&
                    self.Counter == other.Counter &&
                    self.InitialPosition == other.InitialPosition;
            }

            public static bool operator !=(UpdateState self, UpdateState other)
            {
                return !(self == other);
            }

            public override bool Equals(object obj)
            {
                return Equals((UpdateState)obj);
            }

            public override int GetHashCode()
            {
                unchecked // Overflow is fine, just wrap
                {
                    int hash = 17;

                    hash = hash * 23 + RemoveSignsCounter.GetHashCode();
                    hash = hash * 23 + LastTick.GetHashCode();
                    hash = hash * 23 + Counter.GetHashCode();
                    hash = hash * 23 + InitialPosition.GetHashCode();

                    return hash;
                }
            }
        }

        class GameTile : IEquatable<GameTile>
        {
            public uint Serf = 0;
            public uint Owner = 0;
            public uint ObjectIndex = 0;
            public byte Paths = 0;
            public bool IdleSerf = false;

            public bool Equals(GameTile other)
            {
                return this == other;
            }

            public static bool operator ==(GameTile self, GameTile other)
            {
                return self.Serf == other.Serf &&
                    self.Owner == other.Owner &&
                    self.ObjectIndex == other.ObjectIndex &&
                    self.Paths == other.Paths &&
                    self.IdleSerf == other.IdleSerf;
            }

            public static bool operator !=(GameTile self, GameTile other)
            {
                return !(self == other);
            }

            public override bool Equals(object obj)
            {
                return Equals((GameTile)obj);
            }

            public override int GetHashCode()
            {
                unchecked // Overflow is fine, just wrap
                {
                    int hash = 17;

                    hash = hash * 23 + Serf.GetHashCode();
                    hash = hash * 23 + Owner.GetHashCode();
                    hash = hash * 23 + ObjectIndex.GetHashCode();
                    hash = hash * 23 + Paths.GetHashCode();
                    hash = hash * 23 + IdleSerf.GetHashCode();

                    return hash;
                }
            }
        }

        public MapGeometry Geometry { get; }
        LandscapeTile[] landscapeTiles;
        readonly GameTile[] gameTiles;
        readonly ushort regions;
        UpdateState updateState = new UpdateState();
        readonly MapPos[] spiralPosPattern;

        // Rendering
        readonly Render.IRenderView renderView = null;
        internal Render.RenderMap RenderMap { get; private set; } = null;

        // Callback for map height changes 
        readonly ChangeHandlers changeHandlers = new ChangeHandlers();

        public Map(MapGeometry geometry, Render.IRenderView renderView)
        {
            // Some code may still assume that map has at least size 3.
            if (geometry.Size < 3)
            {
                throw new ExceptionFreeserf(ErrorSystemType.Map, "Failed to create map with size less than 3.");
            }

            Geometry = geometry;
            this.renderView = renderView;

            spiralPosPattern = new MapPos[295];

            landscapeTiles = new LandscapeTile[(int)geometry.TileCount];
            gameTiles = new GameTile[(int)geometry.TileCount];

            for (int i = 0; i < (int)geometry.TileCount; ++i)
            {
                landscapeTiles[i] = new LandscapeTile();
                gameTiles[i] = new GameTile();
            }

            updateState.LastTick = 0;
            updateState.Counter = 0;
            updateState.RemoveSignsCounter = 0;
            updateState.InitialPosition = 0;

            regions = (ushort)((geometry.Columns >> 5) * (geometry.Rows >> 5));

            InitSpiralPattern();
            InitSpiralPosPattern();
        }

        public void AttachToRenderLayer(Render.IRenderLayer renderLayerTiles,
            Render.IRenderLayer renderLayerWaves, DataSource dataSource)
        {
            if (RenderMap == null)
            {
                int virtualScreenWidth = renderView.VirtualScreen.Size.Width;
                int virtualScreenHeight = renderView.VirtualScreen.Size.Height;
                int tileWidth = Render.RenderMap.TILE_WIDTH;
                int tileHeight = Render.RenderMap.TILE_HEIGHT;

                RenderMap = new Render.RenderMap(
                    (uint)(virtualScreenWidth / tileWidth),
                    (uint)(virtualScreenHeight / tileHeight),
                    this, renderView.TriangleFactory, renderView.SpriteFactory,
                    Render.TextureAtlasManager.Instance.GetOrCreate(Layer.Landscape),
                    Render.TextureAtlasManager.Instance.GetOrCreate(Layer.Waves),
                    dataSource);
            }

            RenderMap.AttachToRenderLayer(renderLayerTiles, renderLayerWaves);
        }

        public void Close()
        {
            if (RenderMap != null)
            {
                RenderMap.DetachFromRenderLayer();
                RenderMap = null;
            }
        }

        public uint Size => Geometry.Size;
        public uint Columns => Geometry.Columns;
        public uint Rows => Geometry.Rows;
        public uint ColumnMask => Geometry.ColumnMask;
        public uint RowMask => Geometry.RowMask;
        public int RowShift => Geometry.RowShift;
        public uint RegionCount => regions;
        public uint TileCount => Geometry.TileCount;

        public void Scroll(int x, int y)
        {
            RenderMap?.Scroll(x, y);
        }

        public void ScrollTo(uint x, uint y)
        {
            RenderMap?.ScrollTo(x, y);
        }

        public void CenterMapPosition(MapPos position)
        {
            RenderMap?.CenterMapPosition(position);
        }

        // Extract column and row from MapPos
        public MapPos PositionColumn(MapPos position)
        {
            return Geometry.PositionColumn(position);
        }

        public MapPos PositionRow(MapPos position)
        {
            return Geometry.PositionRow(position);
        }

        // Translate column, row coordinate to MapPos value. */
        public MapPos Position(uint x, uint y)
        {
            return Geometry.Position(x, y);
        }

        // Addition of two map positions.
        public MapPos PositionAdd(MapPos position, int x, int y)
        {
            return Geometry.PositionAdd(position, x, y);
        }

        public MapPos PositionAdd(MapPos position, MapPos offset)
        {
            return Geometry.PositionAdd(position, offset);
        }

        public MapPos PositionAddSpirally(MapPos position, uint offset)
        {
            return PositionAdd(position, spiralPosPattern[offset]);
        }

        // Shortest distance between map positions.
        public int DistanceX(MapPos position1, MapPos position2)
        {
            return Geometry.DistanceX(position1, position2);
        }

        public int DistanceY(MapPos position1, MapPos position2)
        {
            return Geometry.DistanceY(position1, position2);
        }

        public int Distance(MapPos position1, MapPos position2)
        {
            int distanceColumn = DistanceX(position1, position2);
            int distanceRow = DistanceY(position1, position2);

            if ((distanceColumn > 0 && distanceRow > 0) || (distanceColumn < 0 && distanceRow < 0))
            {
                return Math.Max(Math.Abs(distanceColumn), Math.Abs(distanceRow));
            }
            else
            {
                return Math.Abs(distanceColumn) + Math.Abs(distanceRow);
            }
        }

        /// <summary>
        /// Get random position
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public MapPos GetRandomCoordinate(Random random)
        {
            uint column = 0;
            uint row = 0;

            GetRandomCoordinate(ref column, ref row, random);

            return Position(column, row);
        }

        /// <summary>
        /// Get random position
        /// </summary>
        /// <param name="column"></param>
        /// <param name="row"></param>
        /// <param name="random"></param>
        public void GetRandomCoordinate(ref uint column, ref uint row, Random random)
        {
            uint randomColumn = random.Next() & Geometry.ColumnMask;
            uint randomRow = random.Next() & Geometry.RowMask;

            column = randomColumn;
            row = randomRow;
        }

        // 
        /// <summary>
        /// Movement of map position according to directions.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public MapPos Move(MapPos position, Direction direction)
        {
            return Geometry.Move(position, direction);
        }

        public MapPos MoveRight(MapPos position)
        {
            return Geometry.MoveRight(position);
        }

        public MapPos MoveDownRight(MapPos position)
        {
            return Geometry.MoveDownRight(position);
        }

        public MapPos MoveDown(MapPos position)
        {
            return Geometry.MoveDown(position);
        }

        public MapPos MoveLeft(MapPos position)
        {
            return Geometry.MoveLeft(position);
        }

        public MapPos MoveUpLeft(MapPos position)
        {
            return Geometry.MoveUpLeft(position);
        }

        public MapPos MoveUp(MapPos position)
        {
            return Geometry.MoveUp(position);
        }

        public MapPos MoveRightN(MapPos position, int count)
        {
            return Geometry.MoveRightN(position, count);
        }

        public MapPos MoveDownN(MapPos position, int count)
        {
            return Geometry.MoveDownN(position, count);
        }

        // Extractors for map data. 
        public uint Paths(MapPos position)
        {
            return (uint)(gameTiles[(int)position].Paths & 0x3f);
        }

        public bool HasPath(MapPos position, Direction direction)
        {
            return Misc.BitTest(gameTiles[(int)position].Paths, (int)direction);
        }

        public void AddPath(MapPos position, Direction direction)
        {
            gameTiles[(int)position].Paths |= (byte)Misc.BitU((int)direction);

            foreach (var handler in changeHandlers)
                handler.OnRoadSegmentPlaced(position, direction);
        }

        public void DeletePath(MapPos position, Direction direction)
        {
            gameTiles[(int)position].Paths &= (byte)~Misc.BitU((int)direction);

            foreach (var handler in changeHandlers)
                handler.OnRoadSegmentDeleted(position, direction);
        }

        public bool HasOwner(MapPos position)
        {
            return gameTiles[(int)position].Owner != 0;
        }

        public uint GetOwner(MapPos position)
        {
            return gameTiles[(int)position].Owner - 1;
        }

        public void SetOwner(MapPos position, uint owner)
        {
            gameTiles[(int)position].Owner = owner + 1;
        }

        public void DeleteOwner(MapPos position)
        {
            gameTiles[(int)position].Owner = 0u;
        }

        public uint GetHeight(MapPos position)
        {
            return landscapeTiles[(int)position].Height;
        }

        public Terrain TypeUp(MapPos position)
        {
            return landscapeTiles[(int)position].TypeUp;
        }

        public Terrain TypeDown(MapPos position)
        {
            return landscapeTiles[(int)position].TypeDown;
        }

        public bool TypesWithin(MapPos position, Terrain low, Terrain high)
        {
            return TypeUp(position) >= low &&
                    TypeUp(position) <= high &&
                    TypeDown(position) >= low &&
                    TypeDown(position) <= high &&
                    TypeDown(MoveLeft(position)) >= low &&
                    TypeDown(MoveLeft(position)) <= high &&
                    TypeUp(MoveUpLeft(position)) >= low &&
                    TypeUp(MoveUpLeft(position)) <= high &&
                    TypeDown(MoveUpLeft(position)) >= low &&
                    TypeDown(MoveUpLeft(position)) <= high &&
                    TypeUp(MoveUp(position)) >= low &&
                    TypeUp(MoveUp(position)) <= high;
        }

        public class FindData
        {
            public bool Success;
            public object Data;
        }

        /// <summary>
        /// Searches the whole territory of a player.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="searchFunc"></param>
        /// <returns></returns>
        public List<object> FindInTerritory(uint owner, Func<Map, MapPos, FindData> searchFunc)
        {
            List<object> findings = new List<object>();

            foreach (var position in Geometry)
            {
                if (GetOwner(position) == owner)
                {
                    var data = searchFunc(this, position);

                    if (data != null & data.Success)
                        findings.Add(data.Data);
                }
            }

            return findings;
        }

        /// <summary>
        /// Searches the whole territory of a player and stops the search after finding the first.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="searchFunc"></param>
        /// <returns></returns>
        public object FindFirstInTerritory(uint owner, Func<Map, MapPos, FindData> searchFunc)
        {
            foreach (var position in Geometry)
            {
                if (GetOwner(position) == owner)
                {
                    var data = searchFunc(this, position);

                    if (data != null & data.Success)
                        return data.Data;
                }
            }

            return null;
        }

        public MapPos MoveTowards(MapPos origin, MapPos destination)
        {
            int distanceX = DistanceX(origin, destination);
            int distanceY = DistanceY(origin, destination);

            if (distanceX < 0) // origin right of destination
            {
                if (distanceY > 0)
                {
                    return MoveDown(MoveLeft(origin));
                }
                else if (distanceY < 0)
                {
                    return MoveUpLeft(origin);
                }
                else
                {
                    return MoveLeft(origin);
                }
            }
            else if (distanceX > 0) // origin left of destination
            {
                if (distanceY > 0)
                {
                    return MoveDownRight(origin);
                }
                else if (distanceY < 0)
                {
                    return MoveUp(MoveRight(origin));
                }
                else
                {
                    return MoveRight(origin);
                }
            }
            else
            {
                if (distanceY > 0)
                {
                    return MoveDown(origin);
                }
                else if (distanceY < 0)
                {
                    return MoveUp(origin);
                }
            }

            return origin;
        }

        public MapPos FindFarthestTerritorySpotTowards(uint basePosition, Func<Map, MapPos, bool> targetSearchFunc, int minDistance = 1)
        {
            uint owner = GetOwner(basePosition);
            uint minSum = (uint)(minDistance * minDistance + minDistance) / 2;
            uint spiralOffset = (minSum == 0) ? 0 : 1 + (minSum - 1) * 6;

            for (uint i = spiralOffset; i < 295; ++i)
            {
                var position = PositionAddSpirally(basePosition, i);

                if (targetSearchFunc(this, position))
                {
                    if (GetOwner(position) == owner)
                        return position;
                    else
                    {
                        int numTries = 0;
                        uint checkPosition = position;
                        uint lastCheckPosition;

                        do
                        {
                            lastCheckPosition = checkPosition;
                            checkPosition = MoveTowards(checkPosition, basePosition);
                        } while (GetOwner(checkPosition) != owner && checkPosition != lastCheckPosition && ++numTries < 10);

                        if (GetOwner(position) == owner)
                            return position;
                    }
                }
            }

            return Global.INVALID_MAPPOS;
        }

        public MapPos FindNearest(MapPos basePosition, int searchRange, Func<Map, MapPos, bool> searchFunc, int minDistance)
        {
            if (searchRange <= 0)
                return Global.INVALID_MAPPOS;

            if (searchRange > 9)
                searchRange = 9;

            int minSum = (minDistance * minDistance + minDistance) / 2;
            int sum = (searchRange * searchRange + searchRange) / 2;
            int spiralOffset = (minSum == 0) ? 0 : 1 + (minSum - 1) * 6;
            int spiralNum = 1 + sum * 6;

            for (int i = spiralOffset; i < spiralNum; ++i)
            {
                var position = PositionAddSpirally(basePosition, (uint)i);

                // as the spiral goes from inner to outer, the first found position is the nearest one
                if (searchFunc(this, position))
                    return position;
            }

            return Global.INVALID_MAPPOS;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="basePosition"></param>
        /// <param name="searchRange"></param>
        /// <param name="searchFunc"></param>
        /// <param name="rateFunc">The lower the value, the better the position. Zero is the best value.</param>
        /// <param name="minDistance"></param>
        /// <returns></returns>
        public MapPos FindBest(MapPos basePosition, int searchRange, Func<Map, MapPos, bool> searchFunc, Func<Map, MapPos, int> rateFunc, int minDistance)
        {
            if (searchRange <= 0)
                return Global.INVALID_MAPPOS;

            if (searchRange > 9)
                searchRange = 9;

            int minSum = (minDistance * minDistance + minDistance) / 2;
            int sum = (searchRange * searchRange + searchRange) / 2;
            int spiralOffset = (minSum == 0) ? 0 : 1 + (minSum - 1) * 6;
            int spiralNum = 1 + sum * 6;

            var bestPosition = Global.INVALID_MAPPOS;
            int bestValue = int.MaxValue;

            for (int i = spiralOffset; i < spiralNum; ++i)
            {
                var position = PositionAddSpirally(basePosition, (uint)i);

                if (searchFunc(this, position))
                {
                    int rating = rateFunc(this, position);

                    if (rating == 0)
                        return position;

                    if (rating < bestValue)
                    {
                        bestPosition = position;
                        bestValue = rating;
                    }
                }
            }

            return bestPosition;
        }

        /// <summary>
        /// Searches the spiral around the base position. Stops after first finding.
        /// </summary>
        /// <param name="basePosition">Base position</param>
        /// <param name="searchRange">Search distance</param>
        /// <param name="searchFunc">Search function</param>
        /// <param name="minDistance">Minimum distance required</param>
        /// <returns></returns>
        public bool HasAnyInArea(MapPos basePosition, int searchRange, Func<Map, MapPos, FindData> searchFunc, int minDistance = 0)
        {
            if (searchRange <= 0)
                return false;

            if (searchRange > 9)
                searchRange = 9;

            int minSum = (minDistance * minDistance + minDistance) / 2;
            int sum = (searchRange * searchRange + searchRange) / 2;
            int spiralOffset = (minSum == 0) ? 0 : 1 + (minSum - 1) * 6;
            int spiralNum = 1 + sum * 6;

            for (int i = spiralOffset; i < spiralNum; ++i)
            {
                var position = PositionAddSpirally(basePosition, (uint)i);
                var data = searchFunc(this, position);

                if (data != null & data.Success)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Searches the spiral around the base position.
        /// 
        /// The distances can range from 0 to 9.
        /// </summary>
        /// <param name="basePosition">Base position</param>
        /// <param name="searchRange">Search distance</param>
        /// <param name="searchFunc">Search function</param>
        /// <param name="minDistance">Minimum distance required</param>
        /// <returns></returns>
        public List<object> FindInArea(MapPos basePosition, int searchRange,
            Func<Map, MapPos, FindData> searchFunc, int minDistance = 0)
        {
            if (searchRange <= 0)
                return new List<object>();

            List<object> findings = new List<object>();

            if (searchRange > 9)
                searchRange = 9;

            int minSum = (minDistance * minDistance + minDistance) / 2;
            int sum = (searchRange * searchRange + searchRange) / 2;
            int spiralOffset = (minSum == 0) ? 0 : 1 + (minSum - 1) * 6;
            int spiralNum = 1 + sum * 6;

            for (int i = spiralOffset; i < spiralNum; ++i)
            {
                var position = PositionAddSpirally(basePosition, (uint)i);
                var data = searchFunc(this, position);

                if (data != null & data.Success)
                    findings.Add(data.Data);
            }

            return findings;
        }

        public MapPos FindSpotNear(MapPos basePosition, int searchRange, Func<Map, MapPos, bool> searchFunc,
            Func<Map, MapPos, int> rateFunc, int minDistance = 0)
        {
            if (searchRange <= 0)
                return Global.INVALID_MAPPOS;

            if (searchRange > 9)
                searchRange = 9;

            int minSum = (minDistance * minDistance + minDistance) / 2;
            int sum = (searchRange * searchRange + searchRange) / 2;
            int spiralOffset = (minSum == 0) ? 0 : 1 + (minSum - 1) * 6;
            int spiralNum = 1 + sum * 6;
            var spots = new List<MapPos>();

            for (int i = spiralOffset; i < spiralNum; ++i)
            {
                var position = PositionAddSpirally(basePosition, (uint)i);

                if (searchFunc(this, position))
                    spots.Add(position);
            }

            if (spots.Count == 0)
                return Global.INVALID_MAPPOS;

            var bestSpot = Global.INVALID_MAPPOS;
            int bestRating = int.MaxValue;

            foreach (var spot in spots)
            {
                int rating = rateFunc(this, spot);

                if (rating < bestRating)
                {
                    bestSpot = spot;
                    bestRating = rating;
                }
            }

            if (bestSpot == Global.INVALID_MAPPOS) // all spots have worst rating -> return the first one
                return spots[0];

            return bestSpot;
        }

        public MapPos FindSpotNear(MapPos basePosition, int searchRange,
            Func<Map, MapPos, bool> searchFunc, Random random, int minDistance = 0)
        {
            if (searchRange <= 0)
                return Global.INVALID_MAPPOS;

            if (searchRange > 9)
                searchRange = 9;

            int minSum = (minDistance * minDistance + minDistance) / 2;
            int sum = (searchRange * searchRange + searchRange) / 2;
            int spiralOffset = (minSum == 0) ? 0 : 1 + (minSum - 1) * 6;
            int spiralNum = 1 + sum * 6;
            var spots = new List<MapPos>();

            for (int i = spiralOffset; i < spiralNum; ++i)
            {
                var position = PositionAddSpirally(basePosition, (uint)i);

                if (searchFunc(this, position))
                    spots.Add(position);
            }

            if (spots.Count == 0)
                return Global.INVALID_MAPPOS;

            return spots[random.Next() % spots.Count];
        }

        public Object GetObject(MapPos position)
        {
            return landscapeTiles[(int)position].Object;
        }

        public bool GetIdleSerf(MapPos position)
        {
            return gameTiles[(int)position].IdleSerf;
        }

        public void SetIdleSerf(MapPos position)
        {
            gameTiles[(int)position].IdleSerf = true;
        }

        public void ClearIdleSerf(MapPos position)
        {
            gameTiles[(int)position].IdleSerf = false;
        }

        public uint GetObjectIndex(MapPos position)
        {
            return gameTiles[(int)position].ObjectIndex;
        }

        public void SetObjectIndex(MapPos position, uint index)
        {
            gameTiles[(int)position].ObjectIndex = index;
        }

        public Minerals GetResourceType(MapPos position)
        {
            return landscapeTiles[(int)position].Mineral;
        }

        public uint GetResourceAmount(MapPos position)
        {
            return (uint)landscapeTiles[(int)position].ResourceAmount;
        }

        public uint GetResourceFish(MapPos position)
        {
            return GetResourceAmount(position);
        }

        public uint GetSerfIndex(MapPos position)
        {
            return gameTiles[(int)position].Serf;
        }

        public bool HasSerf(MapPos position)
        {
            return gameTiles[(int)position].Serf != 0;
        }

        public bool HasOtherSerf(MapPos position, Serf serf)
        {
            return HasSerf(position) && GetSerfIndex(position) != serf.Index;
        }

        public bool HasFlag(MapPos position)
        {
            return (GetObject(position) == Object.Flag);
        }

        public bool HasBuilding(MapPos position)
        {
            return (GetObject(position) >= Object.SmallBuilding &&
                    GetObject(position) <= Object.Castle);
        }

        /// <summary>
        /// Whether any of the two up/down tiles at this position are water.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool IsWaterTile(MapPos position)
        {
            return (TypeDown(position) <= Terrain.Water3 &&
                    TypeUp(position) <= Terrain.Water3);
        }

        /// <summary>
        /// Whether the position is completely surrounded by water.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool IsInWater(MapPos position)
        {
            return (IsWaterTile(position) &&
                    IsWaterTile(MoveUpLeft(position)) &&
                    TypeDown(MoveLeft(position)) <= Terrain.Water3 &&
                    TypeUp(MoveUp(position)) <= Terrain.Water3);
        }

        // Mapping from Object to Space. 
        internal static readonly Space[] MapSpaceFromObject = new Space[128]
        {
            Space.Open,             // Object.None = 0,
            Space.Filled,           // Object.Flag,
            Space.Impassable,       // Object.SmallBuilding,
            Space.Impassable,       // Object.LargeBuilding,
            Space.Impassable,       // Object.Castle,
            Space.Open,
            Space.Open,
            Space.Open,

            Space.Filled,           // Object.Tree0 = 8,
            Space.Filled,           // Object.Tree1,
            Space.Filled,           // Object.Tree2, // 10 
            Space.Filled,           // Object.Tree3,
            Space.Filled,           // Object.Tree4,
            Space.Filled,           // Object.Tree5,
            Space.Filled,           // Object.Tree6,
            Space.Filled,           // Object.Tree7, // 15 

            Space.Filled,           // Object.Pine0,
            Space.Filled,           // Object.Pine1,
            Space.Filled,           // Object.Pine2,
            Space.Filled,           // Object.Pine3,
            Space.Filled,           // Object.Pine4, // 20 
            Space.Filled,           // Object.Pine5,
            Space.Filled,           // Object.Pine6,
            Space.Filled,           // Object.Pine7,

            Space.Filled,           // Object.Palm0,
            Space.Filled,           // Object.Palm1, // 25 
            Space.Filled,           // Object.Palm2,
            Space.Filled,           // Object.Palm3,

            Space.Impassable,       // Object.WaterTree0,
            Space.Impassable,       // Object.WaterTree1,
            Space.Impassable,       // Object.WaterTree2, // 30 
            Space.Impassable,       // Object.WaterTree3,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,
            Space.Open,

            Space.Impassable,       // Object.Stone0 = 72,
            Space.Impassable,       // Object.Stone1,
            Space.Impassable,       // Object.Stone2,
            Space.Impassable,       // Object.Stone3, // 75 
            Space.Impassable,       // Object.Stone4,
            Space.Impassable,       // Object.Stone5,
            Space.Impassable,       // Object.Stone6,
            Space.Impassable,       // Object.Stone7,

            Space.Impassable,       // Object.Sandstone0, // 80 
            Space.Impassable,       // Object.Sandstone1,

            Space.Filled,           // Object.Cross,
            Space.Open,             // Object.Stub,

            Space.Open,             // Object.Stone,
            Space.Open,             // Object.Sandstone3, // 85 

            Space.Open,             // Object.Cadaver0,
            Space.Open,             // Object.Cadaver1,

            Space.Impassable,       // Object.WaterStone0,
            Space.Impassable,       // Object.WaterStone1,

            Space.Filled,           // Object.Cactus0, // 90 
            Space.Filled,           // Object.Cactus1,

            Space.Filled,           // Object.DeadTree,

            Space.Filled,           // Object.FelledPine0,
            Space.Filled,           // Object.FelledPine1,
            Space.Filled,           // Object.FelledPine2, // 95 
            Space.Filled,           // Object.FelledPine3,
            Space.Open,             // Object.FelledPine4,

            Space.Filled,           // Object.FelledTree0,
            Space.Filled,           // Object.FelledTree1,
            Space.Filled,           // Object.FelledTree2, // 100 
            Space.Filled,           // Object.FelledTree3,
            Space.Open,             // Object.FelledTree4,

            Space.Filled,           // Object.NewPine,
            Space.Filled,           // Object.NewTree,

            Space.Semipassable,     // Object.Seeds0, // 105 
            Space.Semipassable,     // Object.Seeds1,
            Space.Semipassable,     // Object.Seeds2,
            Space.Semipassable,     // Object.Seeds3,
            Space.Semipassable,     // Object.Seeds4,
            Space.Semipassable,     // Object.Seeds5, // 110 
            Space.Open,             // Object.FieldExpired,

            Space.Open,             // Object.SignLargeGold,
            Space.Open,             // Object.SignSmallGold,
            Space.Open,             // Object.SignLargeIron,
            Space.Open,             // Object.SignSmallIron, // 115 
            Space.Open,             // Object.SignLargeCoal,
            Space.Open,             // Object.SignSmallCoal,
            Space.Open,             // Object.SignLargeStone,
            Space.Open,             // Object.SignSmallStone,

            Space.Open,             // Object.SignEmpty, // 120 

            Space.Semipassable,     // Object.Field0,
            Space.Semipassable,     // Object.Field1,
            Space.Semipassable,     // Object.Field2,
            Space.Semipassable,     // Object.Field3,
            Space.Semipassable,     // Object.Field4, // 125 
            Space.Semipassable,     // Object.Field5,
            Space.Open,             // Object.Object127
        };

        /// <summary>
        /// Change the height of a map position.
        /// </summary>
        public void SetHeight(MapPos position, uint height)
        {
            landscapeTiles[(int)position].Height = height;

            // Mark landscape dirty
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                foreach (var handler in changeHandlers)
                {
                    handler.OnHeightChanged(Move(position, direction));
                }
            }
        }

        /// <summary>
        /// Change the object at a map position. If index is non-negative
        /// also change this. The index should be reset to zero when a flag or
        /// building is removed.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="obj"></param>
        /// <param name="index"></param>
        public void SetObject(MapPos position, Object obj, int index)
        {
            var oldObject = landscapeTiles[(int)position].Object;

            landscapeTiles[(int)position].Object = obj;

            if (index >= 0)
                gameTiles[(int)position].ObjectIndex = (uint)index;

            // Notify about object change
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                foreach (var handler in changeHandlers)
                {
                    handler.OnObjectChanged(Move(position, direction));
                }
            }

            if ((oldObject == Object.None && obj != Object.None) ||
                (obj < Object.Tree0 && obj != Object.None)) // e.g. placing flags/buildings on some removable map objects
            {
                if (oldObject != Object.None && obj < Object.Tree0 && obj != Object.None)
                {
                    // this will remove an existing map object when a building or flag is placed
                    foreach (var handler in changeHandlers)
                    {
                        handler.OnObjectExchanged(position, oldObject, obj);
                    }
                }

                foreach (var handler in changeHandlers)
                {
                    handler.OnObjectPlaced(position);
                }
            }
            else if (oldObject != Object.None)
            {
                foreach (var handler in changeHandlers)
                {
                    handler.OnObjectExchanged(position, oldObject, obj);
                }
            }
        }

        /// <summary>
        /// Remove resources from the ground at a map position.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="amount"></param>
        public void RemoveGroundDeposit(MapPos position, int amount)
        {
            landscapeTiles[(int)position].ResourceAmount -= amount;

            if (landscapeTiles[(int)position].ResourceAmount <= 0)
            {
                // Also sets the ground deposit type to none. 
                landscapeTiles[(int)position].Mineral = Minerals.None;
            }
        }

        // Remove fish at a map position (must be water). 
        public void RemoveFish(MapPos position, int amount)
        {
            landscapeTiles[(int)position].ResourceAmount -= amount;
        }

        // Set the index of the serf occupying map position. 
        public void SetSerfIndex(MapPos position, int index)
        {
            gameTiles[(int)position].Serf = (uint)index;

            // TODO Mark dirty in viewport. 
        }

        // Get count of gold mineral deposits in the map.
        public uint GetGoldDeposit()
        {
            uint count = 0;

            foreach (var position in Geometry)
            {
                if (GetResourceType(position) == Minerals.Gold)
                {
                    count += GetResourceAmount(position);
                }
            }

            return count;
        }

        // Copy tile data from map generator into map tile data. 
        public void InitTiles(MapGenerator generator)
        {
            landscapeTiles = generator.GetLandscape();

            MapPos position = 0;

            foreach (var tile in landscapeTiles)
            {
                if (tile.Object != Object.None)
                {
                    foreach (var handler in changeHandlers)
                        handler.OnObjectPlaced(position);
                }

                ++position;
            }
        }

        // Update map data as part of the game progression. 
        public void Update(ushort tick, Random random)
        {
            ushort delta = (ushort)(tick - updateState.LastTick);
            updateState.LastTick = tick;
            updateState.Counter -= delta;

            int iterations = 0;

            while (updateState.Counter < 0)
            {
                iterations += regions;
                updateState.Counter += 20;
            }

            var position = updateState.InitialPosition;

            for (int i = 0; i < iterations; ++i)
            {
                --updateState.RemoveSignsCounter;

                if (updateState.RemoveSignsCounter < 0)
                {
                    updateState.RemoveSignsCounter = 16;
                }

                // Test if moving 23 positions right crosses map boundary. 
                if (PositionColumn(position) + 23 < Geometry.Columns)
                {
                    position = MoveRightN(position, 23);
                }
                else
                {
                    position = MoveRightN(position, 23);
                    position = MoveDown(position);
                }

                // Update map at position. 
                UpdateHidden(position, random);
                UpdatePublic(position, random);
            }

            updateState.InitialPosition = position;
        }

        public UpdateState GetUpdateState()
        {
            return updateState;
        }

        public void SetUpdateState(UpdateState updateState)
        {
            this.updateState = updateState;
        }

        public void AddChangeHandler(Handler handler)
        {
            changeHandlers.Add(handler);
        }

        public void DeleteChangeHandler(Handler handler)
        {
            changeHandlers.Remove(handler);
        }

        // Actually place road segments 
        public bool PlaceRoadSegments(Road road)
        {
            var position = road.StartPosition;
            var directions = road.Directions.Reverse().ToList();

            for (int i = 0; i < directions.Count; ++i)
            {
                var direction = directions[i];
                var reverseDirection = direction.Reverse();

                if (!IsRoadSegmentValid(position, direction))
                {
                    // Not valid after all. Backtrack and abort.
                    // This is needed to check that the road
                    // does not cross itself.
                    for (int j = i - 1; j >= 0; --j)
                    {
                        direction = directions[j];
                        reverseDirection = direction.Reverse();

                        gameTiles[(int)position].Paths &= (byte)~Misc.BitU((int)direction);
                        gameTiles[(int)Move(position, direction)].Paths &= (byte)~Misc.BitU((int)reverseDirection);

                        foreach (var handler in changeHandlers)
                            handler.OnRoadSegmentDeleted(position, direction);

                        position = Move(position, direction);
                    }

                    return false;
                }

                gameTiles[(int)position].Paths |= (byte)Misc.BitU((int)direction);
                gameTiles[(int)Move(position, direction)].Paths |= (byte)Misc.BitU((int)reverseDirection);

                foreach (var handler in changeHandlers)
                    handler.OnRoadSegmentPlaced(position, direction);
                foreach (var handler in changeHandlers)
                    handler.OnRoadSegmentPlaced(Move(position, direction), direction.Reverse());

                position = Move(position, direction);
            }

            return true;
        }

        public bool RemoveRoadBackrefUntilFlag(MapPos position, Direction direction)
        {
            while (true)
            {
                position = Move(position, direction);

                // Clear backreference 
                gameTiles[(int)position].Paths &= (byte)~Misc.Bit((int)direction.Reverse());

                if (GetObject(position) == Object.Flag)
                    break;

                // Find next direction of path. 
                direction = Direction.None;
                var cycle = DirectionCycleCW.CreateDefault();

                foreach (var nextDirection in cycle)
                {
                    if (HasPath(position, nextDirection))
                    {
                        direction = nextDirection;
                        break;
                    }
                }

                if (direction == Direction.None)
                    return false;
            }

            return true;
        }

        public bool RemoveRoadBackrefs(MapPos position)
        {
            if (Paths(position) == 0)
                return false;

            // Find directions of path segments to be split. 
            Direction path1Direction = Direction.None;
            var cycle = DirectionCycleCW.CreateDefault();
            var iter = cycle.Begin() as Iterator<Direction>;

            for (; iter != cycle.End(); ++iter)
            {
                if (HasPath(position, iter.Current))
                {
                    path1Direction = iter.Current;
                    break;
                }
            }

            Direction path2Direction = Direction.None;
            ++iter;

            for (; iter != cycle.End(); ++iter)
            {
                if (HasPath(position, iter.Current))
                {
                    path2Direction = iter.Current;
                    break;
                }
            }

            if (path1Direction == Direction.None || path2Direction == Direction.None)
                return false;

            if (!RemoveRoadBackrefUntilFlag(position, path1Direction))
                return false;
            if (!RemoveRoadBackrefUntilFlag(position, path2Direction))
                return false;

            return true;
        }

        public Direction RemoveRoadSegment(ref MapPos position, Direction direction)
        {
            // Clear forward reference. 
            gameTiles[(int)position].Paths &= (byte)~Misc.BitU((int)direction);
            position = Move(position, direction);

            // Clear backreference. 
            gameTiles[(int)position].Paths &= (byte)~Misc.BitU((int)direction.Reverse());

            // Find next direction of path. 
            direction = Direction.None;
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var nextDirection in cycle)
            {
                if (HasPath(position, nextDirection))
                {
                    direction = nextDirection;
                    break;
                }
            }

            return direction;
        }

        public bool RoadSegmentInWater(MapPos position, Direction direction)
        {
            if (direction > Direction.Down)
            {
                position = Move(position, direction);
                direction = direction.Reverse();
            }

            bool water = false;

            switch (direction)
            {
                case Direction.Right:
                    if (TypeDown(position) <= Terrain.Water3 &&
                        TypeUp(MoveUp(position)) <= Terrain.Water3)
                    {
                        water = true;
                    }
                    break;
                case Direction.DownRight:
                    if (TypeUp(position) <= Terrain.Water3 &&
                        TypeDown(position) <= Terrain.Water3)
                    {
                        water = true;
                    }
                    break;
                case Direction.Down:
                    if (TypeUp(position) <= Terrain.Water3 &&
                        TypeDown(MoveLeft(position)) <= Terrain.Water3)
                    {
                        water = true;
                    }
                    break;
                default:
                    Debug.NotReached();
                    break;
            }

            return water;
        }

        /// <summary>
        /// Returns true if the road segment from position in the given
        /// direction can be successfully constructed at the current time.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <param name="endThere"></param>
        /// <returns></returns>
        public bool IsRoadSegmentValid(MapPos position, Direction direction, bool endThere = false)
        {
            var otherPosition = Move(position, direction);
            var obj = GetObject(otherPosition);

            if ((Paths(otherPosition) != 0 && obj != Object.Flag) ||
                MapSpaceFromObject[(int)obj] >= Space.Semipassable)
            {
                return false;
            }

            if (!HasOwner(otherPosition) || GetOwner(otherPosition) != GetOwner(position))
            {
                return false;
            }

            if (IsInWater(position) != IsInWater(otherPosition) &&
                !(endThere || HasFlag(position) || HasFlag(otherPosition)))
            {
                return false;
            }

            return true;
        }

        public void ReadFrom(SaveReaderBinary reader)
        {
            var geometry = Geometry;

            for (uint y = 0; y < geometry.Rows; ++y)
            {
                for (uint x = 0; x < geometry.Columns; ++x)
                {
                    var position = Position(x, y);
                    var gameTile = gameTiles[(int)position];
                    var landscapeTile = landscapeTiles[(int)position];

                    gameTile.Paths = (byte)(reader.ReadByte() & 0x3f);
                    byte heightAndOwner = reader.ReadByte();
                    landscapeTile.Height = (byte)(heightAndOwner & 0x1f);
                    if ((heightAndOwner >> 7) == 0x01)
                    {
                        gameTile.Owner = (uint)((heightAndOwner >> 5) & 0x03) + 1;
                    }

                    byte terrain = reader.ReadByte();
                    landscapeTile.TypeUp = (Terrain)((terrain >> 4) & 0x0f);
                    landscapeTile.TypeDown = (Terrain)(terrain & 0x0f);

                    landscapeTile.Object = (Object)(reader.ReadByte() & 0x7f);
                    gameTile.IdleSerf = false;  // (Misc.BitTest(<the byte that was read before>, 7) != 0);
                }

                for (uint x = 0; x < geometry.Columns; ++x)
                {
                    var position = Position(x, y);
                    var gameTile = gameTiles[(int)position];
                    var landscapeTile = landscapeTiles[(int)position];

                    if (GetObject(position) >= Object.Flag &&
                        GetObject(position) <= Object.Castle)
                    {
                        landscapeTile.Mineral = Minerals.None; // Todo: What about mines? Will mines erase the resources in the ground?
                        landscapeTile.ResourceAmount = 0;
                        gameTile.ObjectIndex = reader.ReadWord();
                    }
                    else
                    {
                        byte resource = reader.ReadByte();
                        landscapeTile.Mineral = (Minerals)((resource >> 5) & 7);
                        landscapeTile.ResourceAmount = resource & 0x1f;
                        gameTile.ObjectIndex = reader.ReadByte();
                    }

                    gameTile.Serf = reader.ReadWord();
                }
            }
        }

        public void ReadFrom(SaveReaderText reader)
        {
            uint x = 0;
            uint y = 0;
            x = reader.Value("pos")[0].ReadUInt();
            y = reader.Value("pos")[1].ReadUInt();

            var position = Position(x, y);

            for (y = 0; y < SAVE_MAP_TILE_SIZE; ++y)
            {
                for (x = 0; x < SAVE_MAP_TILE_SIZE; ++x)
                {
                    var mapPosition = PositionAdd(position, Position(x, y));
                    var gameTile = gameTiles[(int)mapPosition];
                    var landscapeTile = landscapeTiles[(int)mapPosition];
                    int index = (int)(y * SAVE_MAP_TILE_SIZE + x);

                    gameTile.Paths = (byte)(reader.Value("paths")[index].ReadUInt() & 0x3f);
                    landscapeTile.Height = reader.Value("height")[index].ReadUInt() & 0x1f;
                    landscapeTile.TypeUp = (Terrain)reader.Value("type.up")[index].ReadInt();
                    landscapeTile.TypeDown = (Terrain)reader.Value("type.down")[index].ReadInt();

                    try
                    {
                        gameTile.IdleSerf = reader.Value("idle_serf")[index].ReadBool();
                        landscapeTile.Object = (Object)reader.Value("object")[index].ReadInt();
                    }
                    catch
                    {
                        uint value = reader.Value("object")[index].ReadUInt();

                        landscapeTile.Object = (Object)(value & 0x7f);
                        gameTile.IdleSerf = Misc.BitTest(value, 7);
                    }

                    gameTile.Serf = reader.Value("serf")[index].ReadUInt();
                    landscapeTile.Mineral = (Minerals)reader.Value("resource.type")[index].ReadInt();
                    landscapeTile.ResourceAmount = reader.Value("resource.amount")[index].ReadInt();
                }
            }
        }

        public void WriteTo(SaveWriterText writer)
        {
            uint i = 0;

            for (uint ty = 0; ty < Rows; ty += SAVE_MAP_TILE_SIZE)
            {
                for (uint tx = 0; tx < Columns; tx += SAVE_MAP_TILE_SIZE)
                {
                    var mapWriter = writer.AddSection("map", i++);

                    mapWriter.Value("pos").Write(tx);
                    mapWriter.Value("pos").Write(ty);

                    for (uint y = 0; y < SAVE_MAP_TILE_SIZE; ++y)
                    {
                        for (uint x = 0; x < SAVE_MAP_TILE_SIZE; ++x)
                        {
                            var position = Position(tx + x, ty + y);

                            mapWriter.Value("height").Write(GetHeight(position));
                            mapWriter.Value("type.up").Write((int)TypeUp(position));
                            mapWriter.Value("type.down").Write((int)TypeDown(position));
                            mapWriter.Value("paths").Write(Paths(position));
                            mapWriter.Value("object").Write((int)GetObject(position));
                            mapWriter.Value("serf").Write(GetSerfIndex(position));
                            mapWriter.Value("idle_serf").Write(GetIdleSerf(position));

                            if (IsInWater(position))
                            {
                                mapWriter.Value("resource.type").Write(0);
                                mapWriter.Value("resource.amount").Write(GetResourceFish(position));
                            }
                            else
                            {
                                mapWriter.Value("resource.type").Write((int)GetResourceType(position));
                                mapWriter.Value("resource.amount").Write(GetResourceAmount(position));
                            }
                        }
                    }
                }
            }
        }

        public MapPos PositionFromSavedValue(uint val)
        {
            val >>= 2;

            uint x = val & ColumnMask;

            val >>= (RowShift + 1);

            uint y = val & RowMask;

            return Position(x, y);
        }

        // Initialize spiral_pos_pattern from spiral_pattern. 
        void InitSpiralPosPattern()
        {
            for (int i = 0; i < 295; ++i)
            {
                uint x = (uint)(SpiralPattern[2 * i] & (int)Geometry.ColumnMask);
                uint y = (uint)(SpiralPattern[2 * i + 1] & (int)Geometry.RowMask);

                spiralPosPattern[i] = Position(x, y);
            }
        }

        // Update public parts of the map data. 
        void UpdatePublic(MapPos position, Random random)
        {
            // Update other map objects 
            int randomValue;

            switch (GetObject(position))
            {
                case Object.Stub:
                    if ((random.Next() & 3) == 0)
                    {
                        SetObject(position, Object.None, -1);
                    }
                    break;
                case Object.FelledPine0:
                case Object.FelledPine1:
                case Object.FelledPine2:
                case Object.FelledPine3:
                case Object.FelledPine4:
                case Object.FelledTree0:
                case Object.FelledTree1:
                case Object.FelledTree2:
                case Object.FelledTree3:
                case Object.FelledTree4:
                    SetObject(position, Object.Stub, -1);
                    break;
                case Object.NewPine:
                    randomValue = random.Next();
                    if ((randomValue & 0x300) == 0)
                    {
                        SetObject(position, Object.Pine0 + (randomValue & 7), -1);
                    }
                    break;
                case Object.NewTree:
                    randomValue = random.Next();
                    if ((randomValue & 0x300) == 0)
                    {
                        SetObject(position, Object.Tree0 + (randomValue & 7), -1);
                    }
                    break;
                case Object.Seeds0:
                case Object.Seeds1:
                case Object.Seeds2:
                case Object.Seeds3:
                case Object.Seeds4:
                case Object.Field0:
                case Object.Field1:
                case Object.Field2:
                case Object.Field3:
                case Object.Field4:
                    SetObject(position, GetObject(position) + 1, -1);
                    break;
                case Object.Seeds5:
                    SetObject(position, Object.Field0, -1);
                    break;
                case Object.FieldExpired:
                    SetObject(position, Object.None, -1);
                    break;
                case Object.SignLargeGold:
                case Object.SignSmallGold:
                case Object.SignLargeIron:
                case Object.SignSmallIron:
                case Object.SignLargeCoal:
                case Object.SignSmallCoal:
                case Object.SignLargeStone:
                case Object.SignSmallStone:
                case Object.SignEmpty:
                    if (updateState.RemoveSignsCounter == 0)
                    {
                        SetObject(position, Object.None, -1);
                    }
                    break;
                case Object.Field5:
                    SetObject(position, Object.FieldExpired, -1);
                    break;
                default:
                    break;
            }
        }

        // Update hidden parts of the map data. 
        void UpdateHidden(MapPos position, Random random)
        {
            // Update fish resources in water 
            if (IsInWater(position) && landscapeTiles[(int)position].ResourceAmount > 0)
            {
                int randomValue = random.Next();

                if (landscapeTiles[(int)position].ResourceAmount < 10 && (randomValue & 0x3f00) != 0)
                {
                    // Spawn more fish. 
                    ++landscapeTiles[(int)position].ResourceAmount;
                }

                // Move in a random direction of: right, down right, left, up left 
                var adjacentPosition = position;

                switch ((randomValue >> 2) & 3)
                {
                    case 0: adjacentPosition = MoveRight(adjacentPosition); break;
                    case 1: adjacentPosition = MoveDownRight(adjacentPosition); break;
                    case 2: adjacentPosition = MoveLeft(adjacentPosition); break;
                    case 3: adjacentPosition = MoveUpLeft(adjacentPosition); break;
                    default: Debug.NotReached(); break;
                }

                if (IsInWater(adjacentPosition))
                {
                    // Migrate a fish to adjacent water space. 
                    --landscapeTiles[(int)position].ResourceAmount;
                    ++landscapeTiles[(int)adjacentPosition].ResourceAmount;
                }
            }
        }

        public bool Equals(Map other)
        {
            return this == other;
        }

        public static bool operator ==(Map self, Map other)
        {
            if (ReferenceEquals(self, null))
                return ReferenceEquals(other, null);

            if (ReferenceEquals(other, null))
                return false;

            // Check fundamental properties
            if (self.Geometry != other.Geometry ||
                self.regions != other.regions ||
                self.updateState != other.updateState)
                return false;

            // Check all tiles
            foreach (var position in self.Geometry)
            {
                if (self.landscapeTiles[(int)position] != other.landscapeTiles[(int)position])
                {
                    return false;
                }

                if (self.gameTiles[(int)position] != other.gameTiles[(int)position])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool operator !=(Map self, Map other)
        {
            return !(self == other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Map);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;

                hash = hash * 23 + Geometry.GetHashCode();
                hash = hash * 23 + regions.GetHashCode();
                hash = hash * 23 + updateState.GetHashCode();

                foreach (var landscapeTile in landscapeTiles)
                    hash = hash * 23 + landscapeTile.GetHashCode();

                foreach (var gameTile in gameTiles)
                    hash = hash * 23 + gameTile.GetHashCode();

                return hash;
            }
        }
    }
}
