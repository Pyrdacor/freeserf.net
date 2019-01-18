/*
 * Map.cs - Map data and map update functions
 *
 * Copyright (C) 2013-2016  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018       Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
  identified by an integer col and row (commonly encoded as MapPos).

  Each tile has the shape of a rhombus:
     A ______ B
      /\    /
     /  \  /
  C /____\/ D

  but is actually composed of two triangles called "up" (a,c,d) and
  "down" (a,b,d). A serf can move on the perimeter of any of these
  triangles. Each vertex has various properties associated with it,
  among others a height value which means that the 3D landscape is
  defined by these points in (col, row, height)-space.

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

using System;
using System.Collections.Generic;
using System.Linq;
using Freeserf.Data;

namespace Freeserf
{
    using MapPos = UInt32;
    using Dirs = Stack<Direction>;
    using ChangeHandlers = List<Map.Handler>;

    public class Road
    {
        MapPos begin = 0u;
        Dirs dirs = new Dirs();

        const uint MaxLength = 256;

        public Road()
        {
            begin = Global.BadMapPos;
        }

        public bool Valid => begin != Global.BadMapPos;
        public MapPos Source => begin;
        public Dirs Dirs => dirs;
        public uint Length => (uint)dirs.Count;
        public Direction Last => dirs.Peek();
        public bool Extendable => Length < MaxLength;

        public void Invalidate()
        {
            begin = Global.BadMapPos;
            dirs.Clear();
        }

        public void Start(MapPos start)
        {
            begin = start;
        }
        
        public bool IsValidExtension(Map map, Direction dir)
        {
            if (IsUndo(dir))
            {
                return false;
            }

            /* Check that road does not cross itself. */
            MapPos extendedEnd = map.Move(GetEnd(map), dir);
            MapPos pos = begin;
            bool valid = true;

            foreach (var d in dirs.Reverse())
            {
                pos = map.Move(pos, d);

                if (pos == extendedEnd)
                {
                    valid = false;
                    break;
                }
            }

            return valid;
        }

        public bool IsUndo(Direction dir)
        {
            return Length > 0 && Last == dir.Reverse();
        }

        public bool Extend(Direction dir)
        {
            if (begin == Global.BadMapPos)
            {
                return false;
            }

            dirs.Push(dir);

            return true;
        }

        public bool Undo()
        {
            if (begin == Global.BadMapPos)
            {
                return false;
            }

            dirs.Pop();

            if (Length == 0)
            {
                begin = Global.BadMapPos;
            }

            return true;
        }

        public MapPos GetEnd(Map map)
        {
            MapPos result = begin;

            foreach (var dir in dirs.Reverse())
            {
                result = map.Move(result, dir);
            }

            return result;
        }

        public bool HasPos(Map map, MapPos pos)
        {
            MapPos result = begin;

            foreach (var dir in dirs.Reverse())
            {
                if (result == pos)
                {
                    return true;
                }

                result = map.Move(result, dir);
            }

            return result == pos;
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
            Tree2, /* 10 */
            Tree3,
            Tree4,
            Tree5,
            Tree6,
            Tree7, /* 15 */

            Pine0,
            Pine1,
            Pine2,
            Pine3,
            Pine4, /* 20 */
            Pine5,
            Pine6,
            Pine7,

            Palm0,
            Palm1, /* 25 */
            Palm2,
            Palm3,

            WaterTree0,
            WaterTree1,
            WaterTree2, /* 30 */
            WaterTree3,

            Stone0 = 72,
            Stone1,
            Stone2,
            Stone3, /* 75 */
            Stone4,
            Stone5,
            Stone6,
            Stone7,

            Sandstone0, /* 80 */
            Sandstone1,

            Cross,
            Stub,

            Stone,
            Sandstone3, /* 85 */

            Cadaver0,
            Cadaver1,

            WaterStone0,
            WaterStone1,

            Cactus0, /* 90 */
            Cactus1,

            DeadTree,

            FelledPine0,
            FelledPine1,
            FelledPine2, /* 95 */
            FelledPine3,
            FelledPine4,

            FelledTree0,
            FelledTree1,
            FelledTree2, /* 100 */
            FelledTree3,
            FelledTree4,

            NewPine,
            NewTree,

            Seeds0, /* 105 */
            Seeds1,
            Seeds2,
            Seeds3,
            Seeds4,
            Seeds5, /* 110 */
            FieldExpired,

            SignLargeGold,
            SignSmallGold,
            SignLargeIron,
            SignSmallIron, /* 115 */
            SignLargeCoal,
            SignSmallCoal,
            SignLargeStone,
            SignSmallStone,

            SignEmpty, /* 120 */

            Field0,
            Field1,
            Field2,
            Field3,
            Field4, /* 125 */
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

        /* Initialize the global spiral_pattern. */
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
            public abstract void OnHeightChanged(MapPos pos);
            public abstract void OnObjectChanged(MapPos pos);
            public abstract void OnObjectPlaced(MapPos pos);
            public abstract void OnObjectExchanged(MapPos pos, Map.Object oldObject, Map.Object newObject);
            public abstract void OnRoadSegmentPlaced(MapPos pos, Direction dir);
            public abstract void OnRoadSegmentDeleted(MapPos pos, Direction dir);
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
            public MapPos InitialPos = 0;

            public bool Equals(UpdateState other)
            {
                return this == other;
            }

            public static bool operator ==(UpdateState self, UpdateState other)
            {
                return self.RemoveSignsCounter == other.RemoveSignsCounter &&
                    self.LastTick == other.LastTick &&
                    self.Counter == other.Counter &&
                    self.InitialPos == other.InitialPos;
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
                    hash = hash * 23 + InitialPos.GetHashCode();

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
        GameTile[] gameTiles;
        ushort regions;
        UpdateState updateState = new UpdateState();
        MapPos[] spiralPosPattern;

        // Rendering
        Render.IRenderView renderView = null;
        // TODO: road segments
        internal Render.RenderMap RenderMap { get; private set; } = null;

        /* Callback for map height changes */
        ChangeHandlers changeHandlers = new ChangeHandlers();

        public Map(MapGeometry geometry, Render.IRenderView renderView)
        {
            // Some code may still assume that map has at least size 3.
            if (geometry.Size < 3)
            {
                throw new ExceptionFreeserf("Failed to create map with size less than 3.");
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
            updateState.InitialPos = 0;

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

        public void CenterMapPos(MapPos pos)
        {
            RenderMap?.CenterMapPos(pos);
        }

        // Extract col and row from MapPos
        public MapPos PosColumn(MapPos pos)
        {
            return Geometry.PosColumn(pos);
        }

        public MapPos PosRow(MapPos pos)
        {
            return Geometry.PosRow(pos);
        }

        // Translate col, row coordinate to MapPos value. */
        public MapPos Pos(uint x, uint y)
        {
            return Geometry.Pos(x, y);
        }

        // Addition of two map positions.
        public MapPos PosAdd(MapPos pos, int x, int y)
        {
            return Geometry.PosAdd(pos, x, y);
        }

        public MapPos PosAdd(MapPos pos, MapPos off)
        {
            return Geometry.PosAdd(pos, off);
        }

        public MapPos PosAddSpirally(MapPos pos, uint off)
        {
            return PosAdd(pos, spiralPosPattern[off]);
        }

        // Shortest distance between map positions.
        public int DistX(MapPos pos1, MapPos pos2)
        {
            return Geometry.DistX(pos1, pos2);
        }

        public int DistY(MapPos pos1, MapPos pos2)
        {
            return Geometry.DistY(pos1, pos2);
        }

        // Get random position
        public MapPos GetRandomCoord(Random random)
        {
            uint column = 0;
            uint row = 0;

            GetRandomCoord(ref column, ref row, random);

            return Pos(column, row);
        }

        public void GetRandomCoord(ref uint column, ref uint row, Random random)
        {
            uint c = random.Next() & Geometry.ColumnMask;
            uint r = random.Next() & Geometry.RowMask;

            column = c;
            row = r;
        }

        // Movement of map position according to directions.
        public MapPos Move(MapPos pos, Direction dir)
        {
            return Geometry.Move(pos, dir);
        }

        public MapPos MoveRight(MapPos pos)
        {
            return Geometry.MoveRight(pos);
        }

        public MapPos MoveDownRight(MapPos pos)
        {
            return Geometry.MoveDownRight(pos);
        }

        public MapPos MoveDown(MapPos pos)
        {
            return Geometry.MoveDown(pos);
        }

        public MapPos MoveLeft(MapPos pos)
        {
            return Geometry.MoveLeft(pos);
        }

        public MapPos MoveUpLeft(MapPos pos)
        {
            return Geometry.MoveUpLeft(pos);
        }

        public MapPos MoveUp(MapPos pos)
        {
            return Geometry.MoveUp(pos);
        }

        public MapPos MoveRightN(MapPos pos, int n)
        {
            return Geometry.MoveRightN(pos, n);
        }

        public MapPos MoveDownN(MapPos pos, int n)
        {
            return Geometry.MoveDownN(pos, n);
        }

        /* Extractors for map data. */
        public uint Paths(MapPos pos)
        {
            return (uint)(gameTiles[(int)pos].Paths & 0x3f);
        }

        public bool HasPath(MapPos pos, Direction dir)
        {
            return Misc.BitTest(gameTiles[(int)pos].Paths, (int)dir);
        }

        public void AddPath(MapPos pos, Direction dir)
        {
            gameTiles[(int)pos].Paths |= (byte)Misc.BitU((int)dir);

            foreach (var handler in changeHandlers)
                handler.OnRoadSegmentPlaced(pos, dir);
        }

        public void DeletePath(MapPos pos, Direction dir)
        {
            gameTiles[(int)pos].Paths &= (byte)~Misc.BitU((int)dir);

            foreach (var handler in changeHandlers)
                handler.OnRoadSegmentDeleted(pos, dir);
        }

        public bool HasOwner(MapPos pos)
        {
            return gameTiles[(int)pos].Owner != 0;
        }

        public uint GetOwner(MapPos pos)
        {
            return gameTiles[(int)pos].Owner - 1;
        }

        public void SetOwner(MapPos pos, uint owner)
        {
            gameTiles[(int)pos].Owner = owner + 1;
        }

        public void DeleteOwner(MapPos pos)
        {
            gameTiles[(int)pos].Owner = 0u;
        }

        public uint GetHeight(MapPos pos)
        {
            return landscapeTiles[(int)pos].Height;
        }

        public Terrain TypeUp(MapPos pos)
        {
            return landscapeTiles[(int)pos].TypeUp;
        }

        public Terrain TypeDown(MapPos pos)
        {
            return landscapeTiles[(int)pos].TypeDown;
        }

        public bool TypesWithin(MapPos pos, Terrain low, Terrain high)
        {
            return  TypeUp(pos) >= low &&
                    TypeUp(pos) <= high &&
                    TypeDown(pos) >= low &&
                    TypeDown(pos) <= high &&
                    TypeDown(MoveLeft(pos)) >= low &&
                    TypeDown(MoveLeft(pos)) <= high &&
                    TypeUp(MoveUpLeft(pos)) >= low &&
                    TypeUp(MoveUpLeft(pos)) <= high &&
                    TypeDown(MoveUpLeft(pos)) >= low &&
                    TypeDown(MoveUpLeft(pos)) <= high &&
                    TypeUp(MoveUp(pos)) >= low &&
                    TypeUp(MoveUp(pos)) <= high;
        }

        public class FindData
        {
            public bool Success;
            public object Data;
        }

        /// <summary>
        /// Searches the spiral around the base position.
        /// 
        /// The distances can range from 0 to 9.
        /// </summary>
        /// <param name="basePos">Base position</param>
        /// <param name="searchRange">Search distance</param>
        /// <param name="searchFunc">Search function</param>
        /// <param name="minDist">Minimum distance required</param>
        /// <returns></returns>
        public List<object> FindInArea(MapPos basePos, int searchRange, Func<Map, MapPos, FindData> searchFunc, int minDist = 0)
        {
            if (searchRange < 0)
                return new List<object>();

            List<object> findings = new List<object>();

            if (searchRange > 9)
                searchRange = 9;

            int minSum = (minDist * minDist + minDist) / 2;
            int sum = (searchRange * searchRange + searchRange) / 2;
            int spiralOffset = (minSum == 0) ? 0 : 1 + (minSum - 1) * 6;
            int spiralNum = 1 + sum * 6;

            for (int i = spiralOffset; i < spiralNum; ++i)
            {
                MapPos pos = PosAddSpirally(basePos, (uint)i);

                var data = searchFunc(this, pos);

                if (data != null & data.Success)
                    findings.Add(data.Data);
            }

            return findings;
        }

        public MapPos FindSpotNear(MapPos basePos, int searchRange, Func<Map, MapPos, bool> searchFunc, Random random, int minDist = 0)
        {
            if (searchRange < 0)
                return Global.BadMapPos;

            if (searchRange > 9)
                searchRange = 9;

            int minSum = (minDist * minDist + minDist) / 2;
            int sum = (searchRange * searchRange + searchRange) / 2;
            int spiralOffset = (minSum == 0) ? 0 : 1 + (minSum - 1) * 6;
            int spiralNum = 1 + sum * 6;

            List<MapPos> spots = new List<MapPos>();

            for (int i = spiralOffset; i < spiralNum; ++i)
            {
                MapPos pos = PosAddSpirally(basePos, (uint)i);

                if (searchFunc(this, pos))
                    spots.Add(pos);
            }

            if (spots.Count == 0)
                return Global.BadMapPos;

            return spots[random.Next() % spots.Count];
        }

        public Object GetObject(MapPos pos)
        {
            return landscapeTiles[(int)pos].Object;
        }

        public bool GetIdleSerf(MapPos pos)
        {
            return gameTiles[(int)pos].IdleSerf;
        }

        public void SetIdleSerf(MapPos pos)
        {
            gameTiles[(int)pos].IdleSerf = true;
        }

        public void ClearIdleSerf(MapPos pos)
        {
            gameTiles[(int)pos].IdleSerf = false;
        }

        public uint GetObjectIndex(MapPos pos)
        {
            return gameTiles[(int)pos].ObjectIndex;
        }

        public void SetObjectIndex(MapPos pos, uint index)
        {
            gameTiles[(int)pos].ObjectIndex = index;
        }

        public Minerals GetResourceType(MapPos pos)
        {
            return landscapeTiles[(int)pos].Mineral;
        }

        public uint GetResourceAmount(MapPos pos)
        {
            return (uint)landscapeTiles[(int)pos].ResourceAmount;
        }

        public uint GetResourceFish(MapPos pos)
        {
            return GetResourceAmount(pos);
        }

        public uint GetSerfIndex(MapPos pos)
        {
            return gameTiles[(int)pos].Serf;
        }

        public bool HasSerf(MapPos pos)
        {
            return gameTiles[(int)pos].Serf != 0;
        }

        public bool HasFlag(MapPos pos)
        {
            return (GetObject(pos) == Object.Flag);
        }

        public bool HasBuilding(MapPos pos)
        {
            return (GetObject(pos) >= Object.SmallBuilding &&
                    GetObject(pos) <= Object.Castle);
        }

        /* Whether any of the two up/down tiles at this pos are water. */
        public bool IsWaterTile(MapPos pos)
        {
            return (TypeDown(pos) <= Terrain.Water3 &&
                    TypeUp(pos) <= Terrain.Water3);
        }

        /* Whether the position is completely surrounded by water. */
        public bool IsInWater(MapPos pos)
        {
            return (IsWaterTile(pos) &&
                    IsWaterTile(MoveUpLeft(pos)) &&
                    TypeDown(MoveLeft(pos)) <= Terrain.Water3 &&
                    TypeUp(MoveUp(pos)) <= Terrain.Water3);
        }

        /* Mapping from Object to Space. */
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
            Space.Filled,           // Object.Tree2, /* 10 */
            Space.Filled,           // Object.Tree3,
            Space.Filled,           // Object.Tree4,
            Space.Filled,           // Object.Tree5,
            Space.Filled,           // Object.Tree6,
            Space.Filled,           // Object.Tree7, /* 15 */

            Space.Filled,           // Object.Pine0,
            Space.Filled,           // Object.Pine1,
            Space.Filled,           // Object.Pine2,
            Space.Filled,           // Object.Pine3,
            Space.Filled,           // Object.Pine4, /* 20 */
            Space.Filled,           // Object.Pine5,
            Space.Filled,           // Object.Pine6,
            Space.Filled,           // Object.Pine7,

            Space.Filled,           // Object.Palm0,
            Space.Filled,           // Object.Palm1, /* 25 */
            Space.Filled,           // Object.Palm2,
            Space.Filled,           // Object.Palm3,

            Space.Impassable,       // Object.WaterTree0,
            Space.Impassable,       // Object.WaterTree1,
            Space.Impassable,       // Object.WaterTree2, /* 30 */
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
            Space.Impassable,       // Object.Stone3, /* 75 */
            Space.Impassable,       // Object.Stone4,
            Space.Impassable,       // Object.Stone5,
            Space.Impassable,       // Object.Stone6,
            Space.Impassable,       // Object.Stone7,

            Space.Impassable,       // Object.Sandstone0, /* 80 */
            Space.Impassable,       // Object.Sandstone1,

            Space.Filled,           // Object.Cross,
            Space.Open,             // Object.Stub,

            Space.Open,             // Object.Stone,
            Space.Open,             // Object.Sandstone3, /* 85 */

            Space.Open,             // Object.Cadaver0,
            Space.Open,             // Object.Cadaver1,

            Space.Impassable,       // Object.WaterStone0,
            Space.Impassable,       // Object.WaterStone1,

            Space.Filled,           // Object.Cactus0, /* 90 */
            Space.Filled,           // Object.Cactus1,

            Space.Filled,           // Object.DeadTree,

            Space.Filled,           // Object.FelledPine0,
            Space.Filled,           // Object.FelledPine1,
            Space.Filled,           // Object.FelledPine2, /* 95 */
            Space.Filled,           // Object.FelledPine3,
            Space.Open,             // Object.FelledPine4,

            Space.Filled,           // Object.FelledTree0,
            Space.Filled,           // Object.FelledTree1,
            Space.Filled,           // Object.FelledTree2, /* 100 */
            Space.Filled,           // Object.FelledTree3,
            Space.Open,             // Object.FelledTree4,

            Space.Filled,           // Object.NewPine,
            Space.Filled,           // Object.NewTree,

            Space.Semipassable,     // Object.Seeds0, /* 105 */
            Space.Semipassable,     // Object.Seeds1,
            Space.Semipassable,     // Object.Seeds2,
            Space.Semipassable,     // Object.Seeds3,
            Space.Semipassable,     // Object.Seeds4,
            Space.Semipassable,     // Object.Seeds5, /* 110 */
            Space.Open,             // Object.FieldExpired,

            Space.Open,             // Object.SignLargeGold,
            Space.Open,             // Object.SignSmallGold,
            Space.Open,             // Object.SignLargeIron,
            Space.Open,             // Object.SignSmallIron, /* 115 */
            Space.Open,             // Object.SignLargeCoal,
            Space.Open,             // Object.SignSmallCoal,
            Space.Open,             // Object.SignLargeStone,
            Space.Open,             // Object.SignSmallStone,

            Space.Open,             // Object.SignEmpty, /* 120 */

            Space.Semipassable,     // Object.Field0,
            Space.Semipassable,     // Object.Field1,
            Space.Semipassable,     // Object.Field2,
            Space.Semipassable,     // Object.Field3,
            Space.Semipassable,     // Object.Field4, /* 125 */
            Space.Semipassable,     // Object.Field5,
            Space.Open,             // Object.Object127
        };

        /* Change the height of a map position. */
        public void SetHeight(MapPos pos, uint height)
        {
            landscapeTiles[(int)pos].Height = height;

            /* Mark landscape dirty */
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction d in cycle)
            {
                foreach (Handler handler in changeHandlers)
                {
                    handler.OnHeightChanged(Move(pos, d));
                }
            }
        }

        /* Change the object at a map position. If index is non-negative
           also change this. The index should be reset to zero when a flag or
           building is removed. */
        public void SetObject(MapPos pos, Object obj, int index)
        {
            var oldObject = landscapeTiles[(int)pos].Object;

            landscapeTiles[(int)pos].Object = obj;

            if (index >= 0)
                gameTiles[(int)pos].ObjectIndex = (uint)index;

            /* Notify about object change */
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction d in cycle)
            {
                foreach (Handler handler in changeHandlers)
                {
                    handler.OnObjectChanged(Move(pos, d));
                }
            }

            if ((oldObject == Object.None && obj != Object.None) ||
                (obj < Object.Tree0 && obj != Object.None)) // e.g. placing flags/buildings on some removable map objects
            {
                if (oldObject != Object.None && obj < Object.Tree0 && obj != Object.None)
                {
                    // this will remove an existing map object when a building or flag is placed
                    foreach (Handler handler in changeHandlers)
                    {
                        handler.OnObjectExchanged(pos, oldObject, obj);
                    }
                }

                foreach (Handler handler in changeHandlers)
                {
                    handler.OnObjectPlaced(pos);
                }
            }
            else if (oldObject != Object.None)
            {
                foreach (Handler handler in changeHandlers)
                {
                    handler.OnObjectExchanged(pos, oldObject, obj);
                }
            }
        }

        /* Remove resources from the ground at a map position. */
        public void RemoveGroundDeposit(MapPos pos, int amount)
        {
            landscapeTiles[(int)pos].ResourceAmount -= amount;

            if (landscapeTiles[(int)pos].ResourceAmount <= 0)
            {
                /* Also sets the ground deposit type to none. */
                landscapeTiles[(int)pos].Mineral = Minerals.None;
            }
        }

        /* Remove fish at a map position (must be water). */
        public void RemoveFish(MapPos pos, int amount)
        {
            landscapeTiles[(int)pos].ResourceAmount -= amount;
        }

        /* Set the index of the serf occupying map position. */
        public void SetSerfIndex(MapPos pos, int index)
        {
            gameTiles[(int)pos].Serf = (uint)index;

            /* TODO Mark dirty in viewport. */
        }

        // Get count of gold mineral deposits in the map.
        public uint GetGoldDeposit()
        {
            uint count = 0;

            foreach (MapPos pos in Geometry)
            {
                if (GetResourceType(pos) == Minerals.Gold)
                {
                    count += GetResourceAmount(pos);
                }
            }

            return count;
        }

        /* Copy tile data from map generator into map tile data. */
        public void InitTiles(MapGenerator generator)
        {
            landscapeTiles = generator.GetLandscape();

            MapPos pos = 0;

            foreach (var tile in landscapeTiles)
            {
                if (tile.Object != Object.None)
                {
                    foreach (var handler in changeHandlers)
                        handler.OnObjectPlaced(pos);
                }

                ++pos;
            }
        }

        /* Update map data as part of the game progression. */
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

            MapPos pos = updateState.InitialPos;

            for (int i = 0; i < iterations; ++i)
            {
                --updateState.RemoveSignsCounter;

                if (updateState.RemoveSignsCounter < 0)
                {
                    updateState.RemoveSignsCounter = 16;
                }

                /* Test if moving 23 positions right crosses map boundary. */
                if (PosColumn(pos) + 23 < Geometry.Columns)
                {
                    pos = MoveRightN(pos, 23);
                }
                else
                {
                    pos = MoveRightN(pos, 23);
                    pos = MoveDown(pos);
                }

                /* Update map at position. */
                UpdateHidden(pos, random);
                UpdatePublic(pos, random);
            }

            updateState.InitialPos = pos;
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

        /* Actually place road segments */
        public bool PlaceRoadSegments(Road road)
        {
            MapPos pos = road.Source;
            var dirs = road.Dirs.Reverse().ToList();

            for (int i = 0; i < dirs.Count; ++i)
            {
                Direction dir = dirs[i];
                Direction revDir = dir.Reverse();

                if (!IsRoadSegmentValid(pos, dir))
                {
                    /* Not valid after all. Backtrack and abort.
                     This is needed to check that the road
                     does not cross itself. */
                    for (int j = i - 1; j >= 0; --j)
                    {
                        dir = dirs[j];
                        revDir = dir.Reverse();

                        gameTiles[(int)pos].Paths &= (byte)~Misc.BitU((int)dir);
                        gameTiles[(int)Move(pos, dir)].Paths &= (byte)~Misc.BitU((int)revDir);

                        foreach (var handler in changeHandlers)
                            handler.OnRoadSegmentDeleted(pos, dir);

                        pos = Move(pos, dir);
                    }

                    return false;
                }

                gameTiles[(int)pos].Paths |= (byte)Misc.BitU((int)dir);
                gameTiles[(int)Move(pos, dir)].Paths |= (byte)Misc.BitU((int)revDir);

                foreach (var handler in changeHandlers)
                    handler.OnRoadSegmentPlaced(pos, dir);
                foreach (var handler in changeHandlers)
                    handler.OnRoadSegmentPlaced(Move(pos, dir), dir.Reverse());

                pos = Move(pos, dir);
            }

            return true;
        }

        public bool RemoveRoadBackrefUntilFlag(MapPos pos, Direction dir)
        {
            while (true)
            {
                pos = Move(pos, dir);

                /* Clear backreference */
                gameTiles[(int)pos].Paths &= (byte)~Misc.Bit((int)dir.Reverse());

                if (GetObject(pos) == Object.Flag)
                    break;

                /* Find next direction of path. */
                dir = Direction.None;
                var cycle = DirectionCycleCW.CreateDefault();

                foreach (Direction d in cycle)
                {
                    if (HasPath(pos, d))
                    {
                        dir = d;
                        break;
                    }
                }

                if (dir == Direction.None)
                    return false;
            }

            return true;
        }

        public bool RemoveRoadBackrefs(MapPos pos)
        {
            if (Paths(pos) == 0)
                return false;

            /* Find directions of path segments to be split. */
            Direction path1Dir = Direction.None;
            var cycle = DirectionCycleCW.CreateDefault();
            var it = cycle.Begin() as Iterator<Direction>;

            for (; it != cycle.End(); ++it)
            {
                if (HasPath(pos, it.Current))
                {
                    path1Dir = it.Current;
                    break;
                }
            }

            Direction path2Dir = Direction.None;
            ++it;

            for (; it != cycle.End(); ++it)
            {
                if (HasPath(pos, it.Current))
                {
                    path2Dir = it.Current;
                    break;
                }
            }

            if (path1Dir == Direction.None || path2Dir == Direction.None)
                return false;

            if (!RemoveRoadBackrefUntilFlag(pos, path1Dir))
                return false;
            if (!RemoveRoadBackrefUntilFlag(pos, path2Dir))
                return false;

            return true;
        }

        public Direction RemoveRoadSegment(ref MapPos pos, Direction dir)
        {
            /* Clear forward reference. */
            gameTiles[(int)pos].Paths &= (byte)~Misc.BitU((int)dir);
            pos = Move(pos, dir);

            /* Clear backreference. */
            gameTiles[(int)pos].Paths &= (byte)~Misc.BitU((int)dir.Reverse());

            /* Find next direction of path. */
            dir = Direction.None;
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction d in cycle)
            {
                if (HasPath(pos, d))
                {
                    dir = d;
                    break;
                }
            }

            return dir;
        }

        public bool RoadSegmentInWater(MapPos pos, Direction dir)
        {
            if (dir > Direction.Down)
            {
                pos = Move(pos, dir);
                dir = dir.Reverse();
            }

            bool water = false;

            switch (dir)
            {
                case Direction.Right:
                    if (TypeDown(pos) <= Terrain.Water3 &&
                        TypeUp(MoveUp(pos)) <= Terrain.Water3)
                    {
                        water = true;
                    }
                    break;
                case Direction.DownRight:
                    if (TypeUp(pos) <= Terrain.Water3 &&
                        TypeDown(pos) <= Terrain.Water3)
                    {
                        water = true;
                    }
                    break;
                case Direction.Down:
                    if (TypeUp(pos) <= Terrain.Water3 &&
                        TypeDown(MoveLeft(pos)) <= Terrain.Water3)
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

        /* Return true if the road segment from pos in direction dir
           can be successfully constructed at the current time. */
        public bool IsRoadSegmentValid(MapPos pos, Direction dir)
        {
            MapPos otherPos = Move(pos, dir);
            Object obj = GetObject(otherPos);

            if ((Paths(otherPos) != 0 && obj != Object.Flag) ||
                MapSpaceFromObject[(int)obj] >= Space.Semipassable)
            {
                return false;
            }

            if (!HasOwner(otherPos) || GetOwner(otherPos) != GetOwner(pos))
            {
                return false;
            }

            if (IsInWater(pos) != IsInWater(otherPos) &&
                !(HasFlag(pos) || HasFlag(otherPos)))
            {
                return false;
            }

            return true;
        }

        public void ReadFrom(SaveReaderBinary reader)
        {
            var geom = Geometry;

            for (uint y = 0; y < geom.Rows; ++y)
            {
                for (uint x = 0; x < geom.Columns; ++x)
                {
                    MapPos pos = Pos(x, y);
                    GameTile gameTile = gameTiles[(int)pos];
                    LandscapeTile landscapeTile = landscapeTiles[(int)pos];

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

                for (uint x = 0; x < geom.Columns; ++x)
                {
                    MapPos pos = Pos(x, y);
                    GameTile gameTile = gameTiles[(int)pos];
                    LandscapeTile landscapeTile = landscapeTiles[(int)pos];

                    if (GetObject(pos) >= Object.Flag &&
                        GetObject(pos) <= Object.Castle)
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

            MapPos pos = Pos(x, y);

            for (y = 0; y < SAVE_MAP_TILE_SIZE; ++y)
            {
                for (x = 0; x < SAVE_MAP_TILE_SIZE; ++x)
                {
                    MapPos p = PosAdd(pos, Pos(x, y));
                    GameTile gameTile = gameTiles[(int)p];
                    LandscapeTile landscapeTile = landscapeTiles[(int)p];
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
                        uint val = reader.Value("object")[index].ReadUInt();

                        landscapeTile.Object = (Object)(val & 0x7f);
                        gameTile.IdleSerf = Misc.BitTest(val, 7);
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
                    SaveWriterText mapWriter = writer.AddSection("map", i++);

                    mapWriter.Value("pos").Write(tx);
                    mapWriter.Value("pos").Write(ty);

                    for (uint y = 0; y < SAVE_MAP_TILE_SIZE; ++y)
                    {
                        for (uint x = 0; x < SAVE_MAP_TILE_SIZE; ++x)
                        {
                            MapPos pos = Pos(tx + x, ty + y);

                            mapWriter.Value("height").Write(GetHeight(pos));
                            mapWriter.Value("type.up").Write((int)TypeUp(pos));
                            mapWriter.Value("type.down").Write((int)TypeDown(pos));
                            mapWriter.Value("paths").Write(Paths(pos));
                            mapWriter.Value("object").Write((int)GetObject(pos));
                            mapWriter.Value("serf").Write(GetSerfIndex(pos));
                            mapWriter.Value("idle_serf").Write(GetIdleSerf(pos));

                            if (IsInWater(pos))
                            {
                                mapWriter.Value("resource.type").Write(0);
                                mapWriter.Value("resource.amount").Write(GetResourceFish(pos));
                            }
                            else
                            {
                                mapWriter.Value("resource.type").Write((int)GetResourceType(pos));
                                mapWriter.Value("resource.amount").Write(GetResourceAmount(pos));
                            }
                        }
                    }
                }
            }
        }

        public MapPos PosFromSavedValue(uint val)
        {
            val >>= 2;

            uint x = val & ColumnMask;

            val >>= (RowShift + 1);

            uint y = val & RowMask;

            return Pos(x, y);
        }

        /* Initialize spiral_pos_pattern from spiral_pattern. */
        void InitSpiralPosPattern()
        {
            for (int i = 0; i < 295; ++i)
            {
                uint x = (uint)(SpiralPattern[2 * i] & (int)Geometry.ColumnMask);
                uint y = (uint)(SpiralPattern[2 * i + 1] & (int)Geometry.RowMask);

                spiralPosPattern[i] = Pos(x, y);
            }
        }

        /* Update public parts of the map data. */
        void UpdatePublic(MapPos pos, Random random)
        {
            /* Update other map objects */
            int r;

            switch (GetObject(pos))
            {
                case Object.Stub:
                    if ((random.Next() & 3) == 0)
                    {
                        SetObject(pos, Object.None, -1);
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
                    SetObject(pos, Object.Stub, -1);
                    break;
                case Object.NewPine:
                    r = random.Next();
                    if ((r & 0x300) == 0)
                    {
                        SetObject(pos, Object.Pine0 + (r & 7), -1);
                    }
                    break;
                case Object.NewTree:
                    r = random.Next();
                    if ((r & 0x300) == 0)
                    {
                        SetObject(pos, Object.Tree0 + (r & 7), -1);
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
                    SetObject(pos, GetObject(pos) + 1, -1);
                    break;
                case Object.Seeds5:
                    SetObject(pos, Object.Field0, -1);
                    break;
                case Object.FieldExpired:
                    SetObject(pos, Object.None, -1);
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
                        SetObject(pos, Object.None, -1);
                    }
                    break;
                case Object.Field5:
                    SetObject(pos, Object.FieldExpired, -1);
                    break;
                default:
                    break;
            }
        }

        /* Update hidden parts of the map data. */
        void UpdateHidden(MapPos pos, Random random)
        {
            /* Update fish resources in water */
            if (IsInWater(pos) && landscapeTiles[(int)pos].ResourceAmount > 0)
            {
                int r = random.Next();

                if (landscapeTiles[(int)pos].ResourceAmount < 10 && (r & 0x3f00) != 0)
                {
                    /* Spawn more fish. */
                    ++landscapeTiles[(int)pos].ResourceAmount;
                }

                /* Move in a random direction of: right, down right, left, up left */
                MapPos adjPos = pos;
                switch ((r >> 2) & 3)
                {
                    case 0: adjPos = MoveRight(adjPos); break;
                    case 1: adjPos = MoveDownRight(adjPos); break;
                    case 2: adjPos = MoveLeft(adjPos); break;
                    case 3: adjPos = MoveUpLeft(adjPos); break;
                    default: Debug.NotReached(); break;
                }

                if (IsInWater(adjPos))
                {
                    /* Migrate a fish to adjacent water space. */
                    --landscapeTiles[(int)pos].ResourceAmount;
                    ++landscapeTiles[(int)adjPos].ResourceAmount;
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
            foreach (MapPos pos in self.Geometry)
            {
                if (self.landscapeTiles[(int)pos] != other.landscapeTiles[(int)pos])
                {
                    return false;
                }

                if (self.gameTiles[(int)pos] != other.gameTiles[(int)pos])
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
