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
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    using MapPos = UInt32;
    using Dirs = Stack<Direction>;
    using ChangeHandlers = List<Map.Handler>;

    public class Road
    {
        MapPos begin;
        Dirs dirs;

        const uint MaxLength = 256;

        public Road()
        {
            begin = Global.BadMapPos;
        }

        public bool Invalid => begin != Global.BadMapPos;
        public MapPos Source => begin;
        public Dirs Dirs => dirs;
        public int Length => dirs.Count;
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

            foreach (var d in dirs)
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

            foreach (var dir in dirs)
            {
                result = map.Move(result, dir);
            }

            return result;
        }

        public bool HasPos(Map map, MapPos pos)
        {
            MapPos result = begin;

            foreach (var dir in dirs)
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

    public class Map
    {
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
            eOpen = 0,
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
        }

        class LandscapeTile : IEquatable<LandscapeTile>
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

        class UpdateState : IEquatable<UpdateState>
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
        List<LandscapeTile> landscapeTiles;
        List<GameTile> gameTiles;
        ushort regions;
        UpdateState updateState;
        MapPos[] spiralPosPattern;

        /* Callback for map height changes */
        ChangeHandlers changeHandlers = new ChangeHandlers();

        public Map(MapGeometry geometry)
        {
            // Some code may still assume that map has at least size 3.
            if (geometry.Size < 3)
            {
                throw new ExceptionFreeserf("Failed to create map with size less than 3.");
            }

            Geometry = geometry;
            spiralPosPattern = new MapPos[295];

            landscapeTiles = new List<LandscapeTile>((int)geometry.TileCount);
            gameTiles = new List<GameTile>((int)geometry.TileCount);

            updateState.LastTick = 0;
            updateState.Counter = 0;
            updateState.RemoveSignsCounter = 0;
            updateState.InitialPos = 0;

            regions = (ushort)((geometry.Columns >> 5) * (geometry.Rows >> 5));

            InitSpiralPattern();
            InitSpiralPosPattern();
        }

        public uint Size => Geometry.Size;
        public uint Columns => Geometry.Columns;
        public uint Rows => Geometry.Rows;
        public uint ColumnMask => Geometry.ColumnMask;
        public uint RowMask => Geometry.RowMask;
        public uint RowShift => (uint)Geometry.RowShift;
        public uint RegionCount => regions;

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
        public MapPos Pos(int x, int y)
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
            int c = 0;
            int r = 0;

            GetRandomCoord(ref c, ref r, random);

            return Pos(c, r);
        }

        public void GetRandomCoord(ref int col, ref int row, Random random)
        {
            int c = random.Next() & (int)Geometry.ColumnMask;
            int r = random.Next() & (int)Geometry.RowMask;

            col = c;
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
        }

        public void DeletePath(MapPos pos, Direction dir)
        {
            gameTiles[(int)pos].Paths &= (byte)~Misc.BitU((int)dir);
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
        static readonly Space[] map_space_from_obj = new Space[128];

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
            generator.GetLandscape(landscapeTiles);
        }

        public void Update(uint tick, Random rnd)
        {

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

        }

        public void DeleteChangeHandler(Handler handler)
        {

        }

        /* Actually place road segments */
        public bool PlaceRoadSegments(Road road)
        {

        }

        public bool RemoveRoadBackrefUntilFlag(MapPos pos, Direction dir)
        {

        }

        public bool RemoveRoadBackrefs(MapPos pos)
        {

        }

        public Direction RemoveRoadSegment(MapPos* pos, Direction dir)
        {

        }

        public bool RoadSegmentInWater(MapPos pos, Direction dir)
        {

        }

        public bool IsRoadSegmentValid(MapPos pos, Direction dir)
        {

        }

        // TODO
        /*bool operator ==(Map& rhs)
        bool operator !=(Map& rhs)*/

        public void ReadFrom(SaveReaderBinary reader)
        {

        }

        public void ReadFrom(SaveReaderText reader)
        {

        }

        public void WriteTo(SaveWriterText writer)
        {

        }

        public MapPos PosFromSavedValue(uint val)
        {

        }

        /* Initialize spiral_pos_pattern from spiral_pattern. */
        void InitSpiralPosPattern()
        {
            for (int i = 0; i < 295; ++i)
            {
                int x = SpiralPattern[2 * i] & (int)Geometry.ColumnMask;
                int y = SpiralPattern[2 * i + 1] & (int)Geometry.RowMask;

                spiralPosPattern[i] = Pos(x, y);
            }
        }

        void UpdatePublic(MapPos pos, Random rnd)
        {

        }

        void UpdateHidden(MapPos pos, Random rnd)
        {

        }
    }
}
