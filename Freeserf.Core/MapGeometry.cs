/*
 * MapGeometry.cs - Map geometry functions
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

using System;
using System.Collections;
using System.Collections.Generic;

namespace Freeserf
{
    // MapPos is a compact composition of col and row values that
    // uniquely identifies a vertex in the map space. It is also used
    // directly as index to map data arrays.
    using MapPos = UInt32;

    public static partial class Global
    {
        public const MapPos BadMapPos = MapPos.MaxValue;
    }

    // Map directions
    //
    //    A ______ B
    //     /\    /
    //    /  \  /
    // C /____\/ D
    //
    // Six standard directions:
    // RIGHT: A to B
    // DOWN_RIGHT: A to D
    // DOWN: A to C
    // LEFT: D to C
    // UP_LEFT: D to A
    // UP: D to B
    //
    // Non-standard directions:
    // UP_RIGHT: C to B
    // DOWN_LEFT: B to C
    public enum Direction
    {
        None = -1,

        Right = 0,
        DownRight,
        Down,
        Left,
        UpLeft,
        Up
    }

    public static class DirectionExtensions
    {
        // Return the given direction turned clockwise a number of times.
        //
        // Return the resulting direction from turning the given direction
        // clockwise in 60 degree increment the specified number of times.
        // If times is a negative number the direction will be turned counter
        // clockwise.
        public static Direction Turn(this Direction d, int times)
        {
            if (d == Direction.None)
            {
                throw new ExceptionFreeserf("Failed to turn uninitialised direction");
            }

            int td = ((int)d + times) % 6;

            if (td < 0)
                td += 6;

            return (Direction)td;
        }

        // Return the given direction reversed.
        public static Direction Reverse(this Direction d)
        {
            return Turn(d, 3);
        }
    }

    // Cycle direction (clockwise or counter-clockwise)
    public enum Cycle
    {
        CW,
        CCW
    }

    public abstract class DirectionCycle : IEquatable<DirectionCycle>, IEqualityComparer<DirectionCycle>, IEnumerable<Direction>
    {
        public abstract class Iterator : Iterator<Direction>
        {
            protected DirectionCycle cycle;
            protected int offset;

            protected Iterator(DirectionCycle cycle, int offset)
            {
                this.cycle = cycle;
                this.offset = offset;
            }

            protected Iterator(Iterator other)
            {
                if (other == null)
                    throw new NullReferenceException("Iterator construction with null reference.");

                cycle = other.cycle;
                offset = other.offset;
            }

            public override bool Equals(Iterator<Direction> other)
            {
                if (!(other is Iterator))
                    return false;

                var iter = other as Iterator;

                return cycle == iter.cycle &&
                    offset == iter.offset;
            }

            protected override void Increment()
            {
                if (offset < (int)cycle.length)
                    ++offset;
            }
        }

        protected Direction start;
        protected uint length;

        public DirectionCycle(Direction start, uint length)
        {
            if (start == Direction.None)
            {
                throw new ExceptionFreeserf("Failed to init DirectionCycle with uninitialised direction");
            }

            this.start = start;
            this.length = length;
        }

        public DirectionCycle(DirectionCycle other)
        {
            start = other.start;
            length = other.length;
        }

        public bool Equals(DirectionCycle other)
        {
            return this == other;
        }

        public static bool operator ==(DirectionCycle self, DirectionCycle rhs)
        {
#pragma warning disable IDE0041
            if (ReferenceEquals(self, null))
                return ReferenceEquals(rhs, null);

            if (ReferenceEquals(rhs, null))
                return false;
#pragma warning restore IDE0041

            return self.start == rhs.start && self.length == rhs.length;
        }

        public static bool operator !=(DirectionCycle self, DirectionCycle rhs)
        {
            return !(self == rhs);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DirectionCycle);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;

                hash = hash * 23 + start.GetHashCode();
                hash = hash * 23 + length.GetHashCode();

                return hash;
            }
        }

        public bool Equals(DirectionCycle x, DirectionCycle y)
        {
            return x == y;
        }

        public int GetHashCode(DirectionCycle obj)
        {
            return (obj == null) ? 0 : obj.GetHashCode();
        }

        public abstract IEnumerator<Direction> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class DirectionCycleCW : DirectionCycle
    {
        Direction Start => base.start;
        uint Length => base.length;

        public class IteratorCW : Iterator
        {
            internal IteratorCW(DirectionCycle cycle, int offset)
                : base(cycle, offset)
            {

            }

            public IteratorCW(Iterator other)
                : base(other as IteratorCW)
            {

            }

            public override Direction Current => (Direction)(((int)(cycle as DirectionCycleCW).Start + offset) % 6);
        }

        public DirectionCycleCW(Direction start, MapPos length)
            : base(start, length)
        {

        }

        public DirectionCycleCW(DirectionCycleCW other)
            : base(other)
        {

        }

        public static DirectionCycleCW CreateDefault()
        {
            return new DirectionCycleCW(Direction.Right, 6);
        }

        public IteratorCW Begin()
        {
            return new IteratorCW(this, 0);
        }

        public IteratorCW End()
        {
            return new IteratorCW(this, (int)length);
        }

        public override IEnumerator<Direction> GetEnumerator()
        {
            var iter = Begin() as Iterator<Direction>;
            var end = End() as Iterator<Direction>;

            while (iter != end)
            {
                yield return iter.Current;

                ++iter;
            }
        }
    }

    public class DirectionCycleCCW : DirectionCycle
    {
        Direction Start => base.start;
        uint Length => base.length;

        public class IteratorCCW : Iterator
        {
            internal IteratorCCW(DirectionCycle cycle, int offset)
                : base(cycle, offset)
            {

            }

            public IteratorCCW(Iterator other)
                : base(other as IteratorCCW)
            {

            }

            public override Direction Current => (Direction)((((int)(cycle as DirectionCycleCCW).Start - offset) % 6 + 6) % 6);
        }

        public DirectionCycleCCW(Direction start, MapPos length)
            : base(start, length)
        {

        }

        public DirectionCycleCCW(DirectionCycleCCW other)
            : base(other)
        {

        }

        public static DirectionCycleCCW CreateDefault()
        {
            return new DirectionCycleCCW(Direction.Up, 6);
        }

        public IteratorCCW Begin()
        {
            return new IteratorCCW(this, 0);
        }

        public IteratorCCW End()
        {
            return new IteratorCCW(this, (int)length);
        }

        public override IEnumerator<Direction> GetEnumerator()
        {
            var iter = Begin() as Iterator<Direction>;
            var end = End() as Iterator<Direction>;

            while (iter != end)
            {
                yield return iter.Current;

                ++iter;
            }
        }
    }

    public class MapGeometry : IEquatable<MapGeometry>, IEqualityComparer<MapGeometry>, IEnumerable<MapPos>
    {
        public class Iterator : Iterator<MapPos>
        {
            MapGeometry mapGeometry;
            MapPos pos;

            internal Iterator(MapGeometry mapGeometry, MapPos pos)
            {
                this.mapGeometry = mapGeometry;
                this.pos = pos;
            }

            public Iterator(Iterator other)
            {
                mapGeometry = other.mapGeometry;
                pos = other.pos;
            }

            public override MapPos Current => pos;

            public override bool Equals(Iterator<MapPos> other)
            {
                if (!(other is Iterator))
                    return false;

                var iter = other as Iterator;

                return mapGeometry == iter.mapGeometry &&
                    pos == iter.pos;
            }

            protected override void Increment()
            {
                if (pos < mapGeometry.TileCount)
                    ++pos;
            }
        }

        // Derived members
        protected MapPos[] dirs = new MapPos[6];

        public uint Size { get; protected set; }
        public uint ColumnSize { get; protected set; }
        public uint RowSize { get; protected set; }
        public uint Columns { get; protected set; }
        public uint Rows { get; protected set; }
        public uint ColumnMask { get; protected set; }
        public uint RowMask { get; protected set; }
        public int RowShift { get; protected set; }
        public uint TileCount => Columns * Rows;

        public MapGeometry(uint size)
        {
            Size = size;

            Init();
        }

        public MapGeometry(MapGeometry other)
            : this(other.Size)
        {

        }

        /* Extract col and row from MapPos */
        public uint PosColumn(uint pos)
        {
            return pos & ColumnMask;

        }

        public uint PosRow(uint pos)
        {
            return (pos >> RowShift) & RowMask;
        }

        /* Translate col, row coordinate to MapPos value. */
        public MapPos Pos(uint x, uint y)
        {
            return (y << RowShift) | x;
        }

        /* Addition of two map positions. */
        public MapPos PosAdd(MapPos pos, int x, int y)
        {
            return Pos((uint)((int)Columns + (int)PosColumn(pos) + x) & ColumnMask, (uint)((int)Rows + (int)PosRow(pos) + y) & RowMask);
        }

        public MapPos PosAdd(MapPos pos, MapPos off)
        {
            return Pos((PosColumn(pos) + PosColumn(off)) & ColumnMask,
                    (PosRow(pos) + PosRow(off)) & RowMask);
        }

        // Shortest signed distance between map positions.
        public int DistX(MapPos pos1, MapPos pos2)
        {
            return (int)Columns/2 - (int)(((int)Columns / 2 + (int)PosColumn(pos1) - (int)PosColumn(pos2)) & ColumnMask);
        }

        public int DistY(MapPos pos1, MapPos pos2)
        {
            return (int)Rows/2 - (int)(((int)Rows/2 + (int)PosRow(pos1) - (int)PosRow(pos2)) & RowMask);
        }

        /* Movement of map position according to directions. */
        public MapPos Move(MapPos pos, Direction dir)
        {
            return PosAdd(pos, dirs[(int)dir]);
        }

        public MapPos MoveRight(MapPos pos)
        {
            return Move(pos, Direction.Right);
        }

        public MapPos MoveDownRight(MapPos pos)
        {
            return Move(pos, Direction.DownRight);
        }

        public MapPos MoveDown(MapPos pos)
        {
            return Move(pos, Direction.Down);
        }

        public MapPos MoveLeft(MapPos pos)
        {
            return Move(pos, Direction.Left);
        }

        public MapPos MoveUpLeft(MapPos pos)
        {
            return Move(pos, Direction.UpLeft);
        }

        public MapPos MoveUp(MapPos pos)
        {
            return Move(pos, Direction.Up);
        }

        public MapPos MoveRightN(MapPos pos, int n)
        {
            return PosAdd(pos, (MapPos)(dirs[(int)Direction.Right] * n));
        }

        public MapPos MoveDownN(MapPos pos, int n)
        {
            return PosAdd(pos, (MapPos)(dirs[(int)Direction.Down] * n));
        }

        public Iterator Begin()
        {
            return new Iterator(this, 0);
        }

        public Iterator End()
        {
            return new Iterator(this, TileCount);
        }

        protected void Init()
        {
            if (Size > 20)
            {
                throw new ExceptionFreeserf("Above size 20 the map positions can no longer fit in a 32-bit integer.");
            }

            ColumnSize = 5 + Size / 2;
            RowSize = 5 + (Size - 1) / 2;
            Columns = 1u << (int)ColumnSize;
            Rows = 1u << (int)RowSize;

            ColumnMask = Columns - 1;
            RowMask = Rows - 1;
            RowShift = (int)ColumnSize;

            // Setup direction offsets
            dirs[(int)Direction.Right] = 1u & ColumnMask;
            dirs[(int)Direction.Left] = (MapPos)(-1 & ColumnMask);
            dirs[(int)Direction.Down] = (1 & RowMask) << RowShift;
            dirs[(int)Direction.Up] = (MapPos)(-1 & RowMask) << RowShift;

            dirs[(int)Direction.DownRight] = dirs[(int)Direction.Right] | dirs[(int)Direction.Down];
            dirs[(int)Direction.UpLeft] = dirs[(int)Direction.Left] | dirs[(int)Direction.Up];
        }

        public bool Equals(MapGeometry other)
        {
            return this == other;
        }

        public static bool operator ==(MapGeometry self, MapGeometry rhs)
        {
#pragma warning disable IDE0041
            if (ReferenceEquals(self, null))
                return ReferenceEquals(rhs, null);

            if (ReferenceEquals(rhs, null))
                return false;
#pragma warning restore IDE0041

            return self.Size == rhs.Size;
        }

        public static bool operator !=(MapGeometry self, MapGeometry rhs)
        {
            return !(self == rhs);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MapGeometry);
        }

        public override int GetHashCode()
        {
            return Size.GetHashCode();
        }

        public bool Equals(MapGeometry x, MapGeometry y)
        {
            return x == y;
        }

        public int GetHashCode(MapGeometry obj)
        {
            return (obj == null) ? 0 : obj.GetHashCode();
        }

        public IEnumerator<uint> GetEnumerator()
        {
            var iter = Begin() as Iterator<MapPos>;
            var end = End() as Iterator<MapPos>;

            while (iter != end)
            {
                yield return iter.Current;

                ++iter;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
