/*
 * RenderMap.cs - Handles map rendering
 *
 * Copyright (C) 2018  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Collections.Generic;
using System.Linq;

/*
 * The texture atlas for map tile graphics contains 155 sprites.
 * - 33 tile graphics
 * - 61 masks for up triangles
 * - 61 masks for down triangles
 */

namespace Freeserf.Render
{
    using MapPos = System.UInt32;

    // Note: Scrolling is only possible by full tile columns or rows
    internal class RenderMap
    {
        internal const int TILE_WIDTH = 32;
        internal const int TILE_HEIGHT = 20;
        internal const int TILE_RENDER_MAX_HEIGHT = 41; // the heighest mask is 41 pixels height
        const int MAX_HEIGHT_OFFSET = 4 * 31; // 31 is the max height of a tile (this value is in negative y-direction)
        const int MAX_HEIGHT_OFFSET_ROWS = (MAX_HEIGHT_OFFSET + TILE_HEIGHT - 1) / TILE_HEIGHT;
        const int MAX_OVERLAP_Y = MAX_HEIGHT_OFFSET; // the last tile may have a negative offset of this
        const int ADDITIONAL_Y_TILES = MAX_HEIGHT_OFFSET_ROWS;
        const int ADDITIOANL_X_TILES = 2;

        static readonly int[] TileMaskUp = new int[81]
        {
            0,  1,  3,  6,  7, -1, -1, -1, -1,
            0,  1,  2,  5,  6,  7, -1, -1, -1,
            0,  1,  2,  3,  5,  6,  7, -1, -1,
            0,  1,  2,  3,  4,  5,  6,  7, -1,
            0,  1,  2,  3,  4,  4,  5,  6,  7,
            -1,  0,  1,  2,  3,  4,  5,  6,  7,
            -1, -1,  0,  1,  2,  4,  5,  6,  7,
            -1, -1, -1,  0,  1,  2,  5,  6,  7,
            -1, -1, -1, -1,  0,  1,  4,  6,  7
        };

        static readonly int[] TileMaskDown = new int[81]
        {
            0,  0,  0,  0,  0, -1, -1, -1, -1,
            1,  1,  1,  1,  1,  0, -1, -1, -1,
            3,  2,  2,  2,  2,  1,  0, -1, -1,
            6,  5,  3,  3,  3,  2,  1,  0, -1,
            7,  6,  5,  4,  4,  3,  2,  1,  0,
            -1,  7,  6,  5,  4,  4,  4,  2,  1,
            -1, -1,  7,  6,  5,  5,  5,  5,  4,
            -1, -1, -1,  7,  6,  6,  6,  6,  6,
            -1, -1, -1, -1,  7,  7,  7,  7,  7
        };

        static readonly uint[] TileSprites = new uint[]
        {
            32, 32, 32, 32, 32, 32, 32, 32,
            32, 32, 32, 32, 32, 32, 32, 32,
            32, 32, 32, 32, 32, 32, 32, 32,
            32, 32, 32, 32, 32, 32, 32, 32,
            0, 1, 2, 3, 4, 5, 6, 7,
            0, 1, 2, 3, 4, 5, 6, 7,
            0, 1, 2, 3, 4, 5, 6, 7,
            0, 1, 2, 3, 4, 5, 6, 7,
            24, 25, 26, 27, 28, 29, 30, 31,
            24, 25, 26, 27, 28, 29, 30, 31,
            24, 25, 26, 27, 28, 29, 30, 31,
            8, 9, 10, 11, 12, 13, 14, 15,
            8, 9, 10, 11, 12, 13, 14, 15,
            8, 9, 10, 11, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21, 22, 23,
            16, 17, 18, 19, 20, 21, 22, 23
        };

        // we put the masks in the same texture atlas as the tiles
        // up masks start after tiles at index 33. there are 61 valid up masks.
        static readonly int[] MaskUpSprites = new int[81]
        {
            33, 34, 35, 36, 37, -1, -1, -1, -1,
            38, 39, 40, 41, 42, 43, -1, -1, -1,
            44, 45, 46, 47, 48, 49, 50, -1, -1,
            51, 52, 53, 54, 55, 56, 57, 58, -1,
            59, 60, 61, 62, 63, 64, 65, 66, 67,
            -1, 68, 69, 70, 71, 72, 73, 74, 75,
            -1, -1, 76, 77, 78, 79, 80, 81, 82,
            -1, -1, -1, 83, 84, 84, 86, 87, 88,
            -1, -1, -1, -1, 89, 90, 91, 92, 93
        };

        // down masks start after up masks at index 33 + 61. there are 61 valid down masks.
        static readonly int[] MaskDownSprites = new int[81]
        {
             94,  95,  96,  97,  98,  -1,  -1,  -1,  -1,
             99, 100, 101, 102, 103, 104,  -1,  -1,  -1,
            105, 106, 107, 108, 109, 110, 111,  -1,  -1,
            112, 113, 114, 115, 116, 117, 118, 119,  -1,
            120, 121, 122, 123, 124, 125, 126, 127, 128,
             -1, 129, 130, 131, 132, 133, 134, 135, 136,
             -1,  -1, 137, 138, 139, 140, 141, 142, 143,
             -1,  -1,  -1, 144, 145, 146, 147, 148, 149,
             -1,  -1,  -1,  -1, 150, 151, 152, 153, 154
        };

        readonly Dictionary<uint, Position> maskOffsets = new Dictionary<MapPos, Position>(81 + 81);
        readonly uint numColumns = 0;
        readonly uint numRows = 0;
        Map map = null;
        ITextureAtlas textureAtlas = null;
        readonly List<ITriangle> triangles = null;

        public uint ScrollX { get; private set; } = 0;
        public uint ScrollY { get; private set; } = 0;
        public Rect RenderArea { get; private set; } = new Rect();
        public uint NumVisibleColumns => numColumns + ADDITIOANL_X_TILES;
        public uint NumVisibleRows => numRows + ADDITIONAL_Y_TILES;

        public RenderMap(uint numColumns, uint numRows, Map map,
            ITriangleFactory triangleFactory, ITextureAtlas textureAtlas,
            DataSource dataSource)
        {
            ScrollX = 0;
            ScrollY = 0;
            this.numColumns = numColumns;
            this.numRows = numRows;
            this.map = map;
            this.textureAtlas = textureAtlas;

            // store map sprite offsets
            for (uint i = 0; i < 81; ++i)
            {
                var spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapMaskUp, i);

                if (spriteInfo != null)
                    maskOffsets.Add(i, new Position(spriteInfo.OffsetX, spriteInfo.OffsetY));

                spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapMaskDown, i);

                if (spriteInfo != null)
                    maskOffsets.Add(81u + i, new Position(spriteInfo.OffsetX, spriteInfo.OffsetY));
            }

            uint numTriangles = (numColumns + ADDITIOANL_X_TILES) * (numRows + ADDITIONAL_Y_TILES) * 2u;

            triangles = new List<ITriangle>((int)numTriangles);

            for (uint c = 0; c < numColumns + ADDITIOANL_X_TILES; ++c)
            {
                for (int i = 0; i < 2; ++i) // up and down row
                {
                    for (uint r = 0; r < numRows + ADDITIONAL_Y_TILES; ++r)
                    {
                        // the triangles are created with the max mask height of 41.
                        // also see comments in TextureAtlasManager.AddAll for further details.

                        var triangle = triangleFactory.Create(TILE_WIDTH, TILE_RENDER_MAX_HEIGHT, 0, 0);

                        triangle.X = (int)(c * TILE_WIDTH) - TILE_WIDTH / 2 + i * TILE_WIDTH / 2;
                        triangle.Y = (int)(r * TILE_HEIGHT);
                        triangle.Visible = true;

                        triangles.Add(triangle);
                    }
                }
            }

            UpdatePosition();
        }

        public void AttachToRenderLayer(IRenderLayer renderLayer)
        {
            foreach (var triangle in triangles)
                triangle.Layer = renderLayer;
        }

        public void DetachFromRenderLayer()
        {
            foreach (var triangle in triangles)
                triangle.Layer = null;
        }

        public void Scroll(int x, int y)
        {
            int column = (int)this.ScrollX + x;
            int row = (int)this.ScrollY + y;

            if (column < 0)
                column += (int)map.Columns;

            if (row < 0)
                row += 2 * (int)map.Rows;

            ScrollTo((uint)column, (uint)row);
        }

        public void ScrollTo(uint x, uint y)
        {
            if (this.ScrollX == x && this.ScrollY == y)
                return;

            this.ScrollX = x;
            this.ScrollY = y;

            UpdatePosition();
        }

        public void CenterMapPos(MapPos pos)
        {
            int column = (int)map.PosColumn(pos) - (int)numColumns / 2;
            int row = (int)map.PosRow(pos) - (int)numRows / 2;

            if (column < 0)
                column += (int)map.Columns;

            if (row < 0)
                row += (int)map.Rows;

            ScrollTo((uint)column, (uint)row);
        }

        void UpdateTriangleUp(int index, int yOffset, int m, int left, int right, MapPos pos)
        {
            if (((left - m) < -4) || ((left - m) > 4))
            {
                throw new ExceptionFreeserf("Failed to draw triangle up (1).");
            }
            if (((right - m) < -4) || ((right - m) > 4))
            {
                throw new ExceptionFreeserf("Failed to draw triangle up (2).");
            }

            int mask = 4 + m - left + 9 * (4 + m - right);

            if (TileMaskUp[mask] < 0)
            {
                throw new ExceptionFreeserf("Failed to draw triangle up (3).");
            }

            var terrain = map.TypeUp(map.MoveUp(pos));
            int spriteIndex = (int)terrain * 8 + TileMaskUp[mask];
            uint sprite = TileSprites[spriteIndex];

            triangles[index].Y = yOffset + maskOffsets[(uint)mask].Y;
            triangles[index].TextureAtlasOffset = textureAtlas.GetOffset(sprite);
            triangles[index].MaskTextureAtlasOffset = textureAtlas.GetOffset((uint)MaskUpSprites[mask]);
        }

        void UpdateTriangleDown(int index, int yOffset, int m, int left, int right, MapPos pos)
        {
            if (((left - m) < -4) || ((left - m) > 4))
            {
                throw new ExceptionFreeserf("Failed to draw triangle down (1).");
            }
            if (((right - m) < -4) || ((right - m) > 4))
            {
                throw new ExceptionFreeserf("Failed to draw triangle down (2).");
            }

            int mask = 4 + left - m + 9 * (4 + right - m);

            if (TileMaskDown[mask] < 0)
            {
                throw new ExceptionFreeserf("Failed to draw triangle down (3).");
            }

            var terrain = map.TypeDown(map.MoveUpLeft(pos));
            int spriteIndex = (int)terrain * 8 + TileMaskDown[mask];
            uint sprite = TileSprites[spriteIndex];

            triangles[index].Y = yOffset + TILE_HEIGHT + maskOffsets[81u + (uint)mask].Y;
            triangles[index].TextureAtlasOffset = textureAtlas.GetOffset(sprite);
            triangles[index].MaskTextureAtlasOffset = textureAtlas.GetOffset((uint)MaskDownSprites[mask]);
        }

        void UpdateUpTileColumn(MapPos pos, ref int index, int yOffset)
        {
            int m = (int)map.GetHeight(pos);

            pos = map.MoveDown(pos);

            int left = (int)map.GetHeight(pos);
            int right = (int)map.GetHeight(map.MoveRight(pos));

            for (int i = 0; i < (numRows + ADDITIONAL_Y_TILES) / 2; ++i)
            {
                UpdateTriangleUp(index++, yOffset - 4 * m, m, left, right, pos);

                yOffset += TILE_HEIGHT;

                pos = map.MoveDownRight(pos);
                m = (int)map.GetHeight(pos);

                UpdateTriangleDown(index++, yOffset - 4 * m, m, left, right, pos);

                yOffset += TILE_HEIGHT;

                pos = map.MoveDown(pos);

                left = (int)map.GetHeight(pos);
                right = (int)map.GetHeight(map.MoveRight(pos));
            }
        }

        void UpdateDownTileColumn(MapPos pos, ref int index, int yOffset)
        {
            int left = (int)map.GetHeight(pos);
            int right = (int)map.GetHeight(map.MoveRight(pos));

            pos = map.MoveDownRight(pos);

            int m = (int)map.GetHeight(pos);

            for (int i = 0; i < (numRows + ADDITIONAL_Y_TILES) / 2; ++i)
            {
                UpdateTriangleDown(index++, yOffset - 4 * m, m, left, right, pos);

                yOffset += TILE_HEIGHT;

                pos = map.MoveDown(pos);

                left = (int)map.GetHeight(pos);
                right = (int)map.GetHeight(map.MoveRight(pos));

                UpdateTriangleUp(index++, yOffset - 4 * m, m, left, right, pos);

                yOffset += TILE_HEIGHT;

                pos = map.MoveDownRight(pos);

                m = (int)map.GetHeight(pos);
            }
        }

        /* Space transformations. */
        /* The game world space is a three dimensional space with the axes
           named "column", "row" and "height". The (column, row) coordinate
           can be encoded as a MapPos.

           The landscape is composed of a mesh of vertices in the game world
           space. There is one vertex for each integer position in the
           (column, row)-space. The height value of such a vertex is stored
           in the map data. Height values at non-integer (column, row)-points
           can be obtained by interpolation from the nearest three vertices.

           The game world can be projected onto a two-dimensional space called
           map pixel space by the following transformation:

            mx =  Tw*c  -(Tw/2)*r
            my =  Th*r  -4*h

           where (mx,my) is the coordinate in map pixel space; (c,r,h) is
           the coordinate in game world space; and (Tw,Th) is the width and
           height of a map tile in pixel units (these two values are defined
           as MAP_TILE_WIDTH and MAP_TILE_HEIGHT in the code).

           The map pixels space can be transformed into screen space by a
           simple translation.

           Note that the game world space wraps around when the edge is
           reached, i.e. decrementing the column by one when standing at
           (c,r,h) = (0,0,0) leads to the point (N-1,0,0) where N is the
           width of the map in columns. This also happens when crossing
           the row edge.

           The map pixel space also wraps around but the vertical wrap-around
           is a bit more tricky, so care must be taken when translating
           map pixel coordinates. When an edge is traversed vertically,
           the x-coordinate has to be offset by half the height of the map,
           because of the skew in the translation from game world space to
           map pixel space.
        */
        public Position GetScreenPositionFromMapPosition(Position mapPosition)
        {
            int lwidth = (int)map.Columns * TILE_WIDTH;
            int lheight = (int)map.Rows * TILE_HEIGHT;

            int x = mapPosition.X - RenderArea.Position.X;
            int y = mapPosition.Y - RenderArea.Position.Y;

            while (y < 0)
            {
                x -= ((int)map.Rows * TILE_WIDTH) / 2;
                y += lheight;
            }

            while (y >= lheight)
            {
                x += ((int)map.Rows * TILE_WIDTH) / 2;
                y -= lheight;
            }

            while (x < 0)
                x += lwidth;

            while (x >= lwidth)
                x -= lwidth;

            return new Position(x, y);
        }

        public Position GetMapPosition(MapPos pos)
        {
            int lwidth = (int)map.Columns * TILE_WIDTH;
            int lheight = (int)map.Rows * TILE_HEIGHT;

            int x = TILE_WIDTH * (int)map.PosColumn(pos) - (TILE_WIDTH / 2) * (int)map.PosRow(pos);
            int y = TILE_HEIGHT * (int)map.PosRow(pos) - 4 * (int)map.GetHeight(pos);

            if (y < 0)
            {
                x -= ((int)map.Rows * TILE_WIDTH) / 2;
                y += lheight;
            }

            if (x < 0)
                x += lwidth;
            else if (x >= lwidth)
                x -= lwidth;

            return new Position(x, y);
        }

        public Position GetScreenPosition(MapPos pos)
        {
            var mapPosition = GetMapPosition(pos);

            return GetScreenPositionFromMapPosition(mapPosition);
        }

        uint GetRow(uint column, uint startRow, int y, bool? down = null, int lastDiff = -1)
        {
            var pos = map.Pos(column, startRow);
            int realY = (int)startRow * TILE_HEIGHT - 4 * (int)map.GetHeight(pos);

            int lower = y - TILE_HEIGHT / 2;
            int upper = y + TILE_HEIGHT / 2;

            if (realY < lower)
            {
                if (down.HasValue && !down.Value) // previous was up
                {
                    if (lastDiff < lower - realY) // last row was closer
                        return startRow + 1;

                    return startRow;
                }

                pos = (startRow % 2 == 0) ? map.MoveDown(pos) : map.MoveDownRight(pos);
                down = false;
                lastDiff = lower - realY;
            }
            else if (realY > upper)
            {
                if (down.HasValue && down.Value) // previous was down
                {
                    if (lastDiff < realY - upper) // last row was closer
                    {
                        if (startRow == 0)
                            return map.Rows - 1;
                        else
                            return startRow - 1;
                    }

                    return startRow;
                }

                pos = (startRow % 2 == 1) ? map.MoveUp(pos) : map.MoveUpLeft(pos);
                down = true;
                lastDiff = realY - upper;
            }
            else
            {
                return startRow;
            }

            return GetRow(map.PosColumn(pos), map.PosRow(pos), y, down, lastDiff);
        }

        MapPos GetMapPosFromMapCoordinates(int x, int y)
        {
            int mappedX = x + (y * TILE_WIDTH) / (2 * TILE_HEIGHT);
            int column = mappedX / TILE_WIDTH;
            int row = y / TILE_HEIGHT;
            int lastDist = int.MaxValue;
            var pos = map.Pos((uint)column, 0u);
            MapPos lastPos = Global.BadMapPos;

            pos = map.MoveDownN(pos, row);

            while (true)
            {
                int rowY = row * TILE_HEIGHT - 4 * (int)map.GetHeight(pos);

                if (row % 2 == 0)
                    pos = map.MoveDown(pos);
                else
                    pos = map.MoveDownRight(pos);

                int nextRowY = (row + 1) * TILE_HEIGHT - 4 * (int)map.GetHeight(pos);

                int rowCenterY = rowY + (nextRowY - rowY) / 2;
                int dist = Math.Abs(rowCenterY - y);

                if (lastDist < dist)
                {
                    int xOff = mappedX % TILE_WIDTH;

                    if (xOff >= TILE_WIDTH / 2)
                        return map.MoveRight(lastPos);

                    return lastPos;
                }

                lastPos = pos;
                lastDist = dist;

                ++row;
            }

            /*int mappedX = x + (y * TILE_WIDTH) / (2 * TILE_HEIGHT);
            int column = mappedX / TILE_WIDTH;
            int xOff = mappedX % TILE_WIDTH;
            //int yOff = TILE_HEIGHT / 2;
            
            // transform to the nearest node
            if (mappedX % TILE_WIDTH > TILE_WIDTH / 2)
                ++column;

            //if (y % TILE_HEIGHT > TILE_HEIGHT / 2)
            //    yOff = -yOff;

            uint row = (uint)(y / TILE_HEIGHT);
            var pos = map.Pos((uint)column, row);

            // the row may have a negative y-offset because of its height so check rows below
            // key = row index, value = distance from y-position
            Dictionary<uint, int> rows = new Dictionary<MapPos, int>();
            
            for (uint i = 0; i < MAX_HEIGHT_OFFSET_ROWS; ++i)
            {
                int rowY = (int)row * TILE_HEIGHT - 4 * (int)map.GetHeight(pos);

                if (row % 2 == 0)
                    pos = map.MoveDown(pos);
                else
                    pos = map.MoveDownRight(pos);

                int nextRowY = (int)(row + 1) * TILE_HEIGHT - 4 * (int)map.GetHeight(pos);

                int rowCenterY = rowY + (nextRowY - rowY) / 2;

                rows.Add(row, Math.Abs(rowCenterY - y));

                ++row;
            }

            row = rows.OrderBy(r => r.Value).First().Key;

            //int startColumn = column - (y / TILE_HEIGHT) / 2;

            //var pos = map.Pos((uint)startColumn, row);

            mappedX = column * TILE_WIDTH + (int)row * TILE_WIDTH / 2;
            column = mappedX / TILE_WIDTH;

            return map.Pos((uint)column, row);//GetRow((uint)column, row, y));

            /*while (true)
            {
                int realY = baseY - 4 + (int)map.GetHeight(pos);

                if (realY + yOff > y)
                {
                    return pos;
                }

                if (row % 2 == 0)
                    pos = map.MoveDown(pos);
                else
                    pos = map.MoveDownRight(pos);

                baseY += TILE_HEIGHT;
                ++row;
            }*/

            // result column und row should be 1 bigger

            /*int mappedX = x + (y * TILE_WIDTH) / (2 * TILE_HEIGHT);
            uint column = (uint)(mappedX / TILE_WIDTH);
            uint row = (uint)(y / TILE_HEIGHT);
            var pos = map.Pos(column, row);

            int baseY = 0;

            while (true)
            {
                int realY = baseY - 4 + (int)map.GetHeight(pos);

                if (y < realY + TILE_HEIGHT / 2)
                {
                    if (row % 2 == 1 && (x % TILE_WIDTH) < TILE_WIDTH / 2)
                        return map.MoveLeft(pos);

                    return pos;
                }

                pos = map.MoveRight(pos);

                if (row % 2 == 0)
                    pos = map.MoveDown(pos);
                else
                    pos = map.MoveDownRight(pos);

                baseY += TILE_HEIGHT;
            }*/


            /*// axis-aligned map column and row
            uint column = (uint)((x + 8) / TILE_WIDTH);
            uint row = 0; // we start at row 0 and go down till we reach the y position

            int yBase = -4;

            if ((x + 8) % TILE_WIDTH < 16) // TODO: this is not perfect yet
            {
                ++row;
                yBase = 16;
            }

            column &= map.ColumnMask;

            // now consider tile height values
            int ly;
            int lastY = -100;

            while (true)
            {
                ly = yBase - 4 * (int)map.GetHeight(map.Pos(column, row));

                if (y < ly)
                    break;

                lastY = ly;
                column = (column + 1) & map.ColumnMask;
                row = (row + 2) & map.RowMask;
                yBase += 2 * TILE_HEIGHT;
            }

            if (y < (ly + lastY) / 2)
            {
                column = (column - 1) & map.ColumnMask;
                row = (row - 2) & map.RowMask;
            }

            return map.Pos(column, row);*/
        }

        public MapPos GetMapPosFromScreenPosition(uint renderColumn, uint renderRow)
        {
            // axis-aligned map column and row
            var column = ScrollX + renderColumn;
            var row = ScrollY + renderRow;

            column &= map.ColumnMask;
            row &= map.RowMask;

            return GetMapPosFromMapCoordinates((int)column * TILE_WIDTH, (int)row * TILE_HEIGHT);
        }

        public MapPos GetMapPosFromScreenPosition(Position position)
        {
            // position inside the map
            int x = RenderArea.Position.X + position.X;
            int y = RenderArea.Position.Y + position.Y;

            return GetMapPosFromMapCoordinates(x, y);
        }

        public MapPos GetMapOffset()
        {
            return GetMapPosFromMapCoordinates(RenderArea.Position.X, RenderArea.Position.Y);
        }

        public MapPos GetCenteredPosition()
        {
            return GetMapPosFromScreenPosition(new Position((int)numColumns * TILE_WIDTH / 2, (int)numRows * TILE_HEIGHT / 2));
        }

        void UpdatePosition()
        {
            ScrollX &= map.ColumnMask;

            // cap at double rows as half rows have influence on the column
            if (ScrollY >= 2 * map.Rows)
                ScrollY &= map.RowMask;

            RenderArea = new Rect((int)ScrollX * TILE_WIDTH + TILE_WIDTH / 2, (int)(ScrollY & map.RowMask) * TILE_HEIGHT,
                ((int)numColumns + ADDITIOANL_X_TILES) * TILE_WIDTH, ((int)numRows + ADDITIONAL_Y_TILES) * TILE_HEIGHT);

            bool odd = ScrollY % 2 == 1;
            int index = 0;
            uint realColumn = (ScrollX + ScrollY / 2) & map.ColumnMask;
            uint realRow = ScrollY & map.RowMask;

            MapPos pos = map.Pos(realColumn, realRow);

            for (uint c = 0; c < numColumns + ADDITIOANL_X_TILES; ++c)
            {
                if (c > 0 || !odd) // (1): this and (2) avoids x-change when scrolled to odd row numbers
                    UpdateUpTileColumn(pos, ref index, 0);

                UpdateDownTileColumn(pos, ref index, 0);

                pos = map.MoveRight(pos);

                if (c == numColumns && odd) // (2): see (1)
                    UpdateUpTileColumn(pos, ref index, 0);
            }
        }
    }
}
