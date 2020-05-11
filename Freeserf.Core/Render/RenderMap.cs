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

using Freeserf.Data;
using System.Collections.Generic;

/*
 * The texture atlas for map tile graphics contains 155 sprites.
 * - 33 tile graphics
 * - 61 masks for up triangles
 * - 61 masks for down triangles
 */

namespace Freeserf.Render
{
    using Data = Data.Data;
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
        const int ADDITIONAL_X_TILES = 2;
        const uint WaveMaskFull = 16u;
        const uint WaveMaskUp = 17u;
        const uint WaveMaskDown = 18u;

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
            -1, -1, -1, 83, 84, 85, 86, 87, 88,
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
        readonly int columnRowFactor = 2;
        Map map = null;
        ITextureAtlas textureAtlasTiles = null;
        ITextureAtlas textureAtlasWaves = null;
        readonly List<ITriangle> triangles = null;
        readonly List<IMaskedSprite> waves = null;

        public uint ScrollX { get; private set; } = 0;
        public uint ScrollY { get; private set; } = 0;
        public uint NumVisibleColumns => numColumns + ADDITIONAL_X_TILES;
        public uint NumVisibleRows => numRows + ADDITIONAL_Y_TILES;
        public float ZoomFactor { get; set; } = 1.0f;
        public CoordinateSpace CoordinateSpace { get; } = null;

        public RenderMap(uint numColumns, uint numRows, Map map,
            ITriangleFactory triangleFactory, ISpriteFactory spriteFactory,
            ITextureAtlas textureAtlasTiles, ITextureAtlas textureAtlasWaves,
            DataSource dataSource)
        {
            CoordinateSpace = new CoordinateSpace(map, this);

            columnRowFactor = (map.Size % 2 == 0) ? 4 : 2;

            ScrollX = 0;
            ScrollY = 0;
            this.numColumns = numColumns;
            this.numRows = numRows;
            this.map = map;
            this.textureAtlasTiles = textureAtlasTiles;
            this.textureAtlasWaves = textureAtlasWaves;

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

            uint numTriangles = (numColumns + ADDITIONAL_X_TILES) * (numRows + ADDITIONAL_Y_TILES) * 2u;

            triangles = new List<ITriangle>((int)numTriangles);
            waves = new List<IMaskedSprite>((int)numTriangles / 2);

            for (uint column = 0; column < numColumns + ADDITIONAL_X_TILES; ++column)
            {
                for (int i = 0; i < 2; ++i) // up and down row
                {
                    for (uint row = 0; row < numRows + ADDITIONAL_Y_TILES; ++row)
                    {
                        // the triangles are created with the max mask height of 41.
                        // also see comments in TextureAtlasManager.AddAll for further details.

                        var triangle = triangleFactory.Create(TILE_WIDTH, TILE_RENDER_MAX_HEIGHT, 0, 0);

                        triangle.X = (int)(column * TILE_WIDTH) - TILE_WIDTH / 2 + i * TILE_WIDTH / 2;
                        triangle.Y = (int)(row * TILE_HEIGHT);
                        triangle.Visible = true;

                        triangles.Add(triangle);
                    }
                }
            }

            for (uint column = 0; column < numColumns + ADDITIONAL_X_TILES; ++column)
            {
                for (uint row = 0; row < numRows + ADDITIONAL_Y_TILES; ++row)
                {
                    var wave = spriteFactory.Create(48, 19, 0, 0, true, false) as IMaskedSprite;

                    wave.X = (int)(column * TILE_WIDTH) - TILE_WIDTH / 2;
                    wave.Y = (int)(row * TILE_HEIGHT);
                    wave.Visible = false;

                    waves.Add(wave);
                }
            }

            UpdatePosition();
        }

        void UpdateWave(MapPos position, int index, int tick, int x, int y)
        {
            int sprite = ((int)(position ^ 5) + (tick >> 3)) & 0xf;

            if (map.TypeUp(position) <= Map.Terrain.Water3 && map.TypeDown(position) <= Map.Terrain.Water3)
            {
                waves[index].X = x;
                waves[index].Y = y;
                waves[index].TextureAtlasOffset = textureAtlasWaves.GetOffset((uint)sprite);
                waves[index].MaskTextureAtlasOffset = textureAtlasWaves.GetOffset(WaveMaskFull);
                waves[index].Visible = true;
            }
            else if (map.TypeDown(position) <= Map.Terrain.Water3)
            {
                waves[index].X = x + maskOffsets[81 + 40].X + TILE_WIDTH / 2;
                waves[index].Y = y + maskOffsets[81 + 40].Y + TILE_HEIGHT;
                waves[index].TextureAtlasOffset = textureAtlasWaves.GetOffset((uint)sprite);
                waves[index].MaskTextureAtlasOffset = textureAtlasWaves.GetOffset(WaveMaskDown);
                waves[index].Visible = true;
            }
            else if (map.TypeUp(position) <= Map.Terrain.Water3)
            {
                waves[index].X = x + maskOffsets[40].X;
                waves[index].Y = y + maskOffsets[40].Y;
                waves[index].TextureAtlasOffset = textureAtlasWaves.GetOffset((uint)sprite);
                waves[index].MaskTextureAtlasOffset = textureAtlasWaves.GetOffset(WaveMaskUp);
                waves[index].Visible = true;
            }
            else
            {
                waves[index].Visible = false;
            }
        }

        void UpdateEvenWaveColumn(MapPos position, ref int index, int tick, int x, int y)
        {
            position = map.MoveDown(position);

            for (int i = 0; i < (numRows + ADDITIONAL_Y_TILES) / 2; ++i)
            {
                UpdateWave(map.MoveUp(position), index++, tick, x, y);

                y += TILE_HEIGHT;

                position = map.MoveDownRight(position);

                UpdateWave(map.MoveUpLeft(position), index++, tick, x - TILE_WIDTH / 2, y);

                y += TILE_HEIGHT;

                position = map.MoveDown(position);
            }

            if ((numRows + ADDITIONAL_Y_TILES) % 2 == 1)
            {
                UpdateWave(map.MoveUp(position), index++, tick, x, y);
            }
        }

        void UpdateOddWaveColumn(MapPos position, ref int index, int tick, int x, int y)
        {
            position = map.MoveDownRight(position);

            for (int i = 0; i < (numRows + ADDITIONAL_Y_TILES) / 2; ++i)
            {
                UpdateWave(map.MoveUpLeft(position), index++, tick, x - TILE_WIDTH / 2, y);

                y += TILE_HEIGHT;

                position = map.MoveDown(position);

                UpdateWave(map.MoveUp(position), index++, tick, x, y);

                y += TILE_HEIGHT;

                position = map.MoveDownRight(position);
            }

            if ((numRows + ADDITIONAL_Y_TILES) % 2 == 1)
            {
                UpdateWave(map.MoveUpLeft(position), index++, tick, x - TILE_WIDTH / 2, y);
            }
        }

        public void UpdateWaves(int tick)
        {
            bool odd = ScrollY % 2 == 1;
            int index = 0;
            var position = GetMapOffset();
            int x = -TILE_WIDTH / 2;

            for (uint c = 0; c < numColumns + ADDITIONAL_X_TILES; ++c)
            {
                if (odd)
                    UpdateOddWaveColumn(position, ref index, tick, x, 0);
                else
                    UpdateEvenWaveColumn(position, ref index, tick, x, 0);

                position = map.MoveRight(position);
                x += TILE_WIDTH;
            }
        }

        public void AttachToRenderLayer(IRenderLayer renderLayerTiles, IRenderLayer renderLayerWaves)
        {
            foreach (var triangle in triangles)
                triangle.Layer = renderLayerTiles;

            foreach (var wave in waves)
                wave.Layer = renderLayerWaves;
        }

        public void DetachFromRenderLayer()
        {
            foreach (var triangle in triangles)
                triangle.Layer = null;

            foreach (var wave in waves)
                wave.Layer = null;
        }

        public void Scroll(int x, int y)
        {
            int column = (int)ScrollX + x;
            int row = (int)ScrollY + y;

            if (column < 0)
                column += (int)map.Columns;

            if (row < 0)
                row += columnRowFactor * (int)map.Rows;

            ScrollTo((uint)column, (uint)row);
        }

        public void ScrollTo(uint x, uint y)
        {
            if (ScrollX == x && ScrollY == y)
                return;

            ScrollX = x;
            ScrollY = y;

            UpdatePosition();
        }

        public void ScrollToMapPosition(MapPos position)
        {
            ScrollY = map.PositionRow(position);
            ScrollX = map.PositionColumn(position) - ScrollY / 2;

            UpdatePosition();
        }

        public void CenterMapPosition(MapPos position)
        {
            var mapPosition = CoordinateSpace.TileSpaceToMapSpace(position);

            mapPosition.X -= (int)numColumns * TILE_WIDTH / 2;
            mapPosition.Y -= (int)numRows * TILE_HEIGHT / 2;

            int lheight = (int)map.Rows * RenderMap.TILE_HEIGHT;

            if (mapPosition.Y < 0)
            {
                mapPosition.Y += lheight;
                mapPosition.X -= (int)map.Rows * TILE_WIDTH / 2;
            }
            else if (mapPosition.Y >= lheight)
            {
                mapPosition.Y -= lheight;
                mapPosition.X += (int)map.Rows * TILE_WIDTH / 2;
            }

            ScrollToMapPosition(CoordinateSpace.MapSpaceToTileSpace(mapPosition));
        }

        void UpdateTriangleUp(int index, int yOffset, int height, int left, int right, MapPos position)
        {
            if (((left - height) < -4) || ((left - height) > 4))
            {
                throw new ExceptionFreeserf(ErrorSystemType.Render, "Failed to draw triangle up (1).");
            }
            if (((right - height) < -4) || ((right - height) > 4))
            {
                throw new ExceptionFreeserf(ErrorSystemType.Render, "Failed to draw triangle up (2).");
            }

            int mask = 4 + height - left + 9 * (4 + height - right);

            if (TileMaskUp[mask] < 0)
            {
                throw new ExceptionFreeserf(ErrorSystemType.Render, "Failed to draw triangle up (3).");
            }

            var terrain = map.TypeUp(map.MoveUp(position));
            int spriteIndex = (int)terrain * 8 + TileMaskUp[mask];
            uint sprite = TileSprites[spriteIndex];

            triangles[index].Y = yOffset + maskOffsets[(uint)mask].Y;
            triangles[index].TextureAtlasOffset = textureAtlasTiles.GetOffset(sprite);
            triangles[index].MaskTextureAtlasOffset = textureAtlasTiles.GetOffset((uint)MaskUpSprites[mask]);
        }

        void UpdateTriangleDown(int index, int yOffset, int height, int left, int right, MapPos position)
        {
            if (((left - height) < -4) || ((left - height) > 4))
            {
                throw new ExceptionFreeserf(ErrorSystemType.Render, "Failed to draw triangle down (1).");
            }
            if (((right - height) < -4) || ((right - height) > 4))
            {
                throw new ExceptionFreeserf(ErrorSystemType.Render, "Failed to draw triangle down (2).");
            }

            int mask = 4 + left - height + 9 * (4 + right - height);

            if (TileMaskDown[mask] < 0)
            {
                throw new ExceptionFreeserf(ErrorSystemType.Render, "Failed to draw triangle down (3).");
            }

            var terrain = map.TypeDown(map.MoveUpLeft(position));
            int spriteIndex = (int)terrain * 8 + TileMaskDown[mask];
            uint sprite = TileSprites[spriteIndex];

            triangles[index].Y = yOffset + TILE_HEIGHT + maskOffsets[81u + (uint)mask].Y;
            triangles[index].TextureAtlasOffset = textureAtlasTiles.GetOffset(sprite);
            triangles[index].MaskTextureAtlasOffset = textureAtlasTiles.GetOffset((uint)MaskDownSprites[mask]);
        }

        void UpdateUpTileColumn(MapPos position, ref int index, int yOffset)
        {
            int height = (int)map.GetHeight(position);

            position = map.MoveDown(position);

            int left = (int)map.GetHeight(position);
            int right = (int)map.GetHeight(map.MoveRight(position));

            for (int i = 0; i < (numRows + ADDITIONAL_Y_TILES) / 2; ++i)
            {
                UpdateTriangleUp(index++, yOffset - 4 * height, height, left, right, position);

                yOffset += TILE_HEIGHT;

                position = map.MoveDownRight(position);
                height = (int)map.GetHeight(position);

                UpdateTriangleDown(index++, yOffset - 4 * height, height, left, right, position);

                yOffset += TILE_HEIGHT;

                position = map.MoveDown(position);

                left = (int)map.GetHeight(position);
                right = (int)map.GetHeight(map.MoveRight(position));
            }

            if ((numRows + ADDITIONAL_Y_TILES) % 2 == 1)
            {
                UpdateTriangleUp(index++, yOffset - 4 * height, height, left, right, position);
            }
        }

        void UpdateDownTileColumn(MapPos position, ref int index, int yOffset)
        {
            int left = (int)map.GetHeight(position);
            int right = (int)map.GetHeight(map.MoveRight(position));

            position = map.MoveDownRight(position);

            int m = (int)map.GetHeight(position);

            for (int i = 0; i < (numRows + ADDITIONAL_Y_TILES) / 2; ++i)
            {
                UpdateTriangleDown(index++, yOffset - 4 * m, m, left, right, position);

                yOffset += TILE_HEIGHT;

                position = map.MoveDown(position);

                left = (int)map.GetHeight(position);
                right = (int)map.GetHeight(map.MoveRight(position));

                UpdateTriangleUp(index++, yOffset - 4 * m, m, left, right, position);

                yOffset += TILE_HEIGHT;

                position = map.MoveDownRight(position);

                m = (int)map.GetHeight(position);
            }

            if ((numRows + ADDITIONAL_Y_TILES) % 2 == 1)
            {
                UpdateTriangleDown(index++, yOffset - 4 * m, m, left, right, position);
            }
        }

        public MapPos GetMapOffset()
        {
            uint realColumn = (ScrollX + ScrollY / 2) & map.ColumnMask;
            uint realRow = ScrollY & map.RowMask;

            return map.Position(realColumn, realRow);
        }

        public MapPos GetCenteredPosition()
        {
            //return GetMapPosFromScreenPosition(new Position((int)numColumns * TILE_WIDTH / 2, (int)numRows * TILE_HEIGHT / 2));
            return CoordinateSpace.ViewSpaceToTileSpace((int)numColumns * TILE_WIDTH / 2, (int)numRows * TILE_HEIGHT / 2);
        }

        void UpdatePosition()
        {
            ScrollX &= map.ColumnMask;

            // cap at double rows as half rows have influence on the column
            if (ScrollY >= columnRowFactor * map.Rows)
                ScrollY &= map.RowMask;

            bool odd = ScrollY % 2 == 1;
            int index = 0;
            var position = GetMapOffset();

            for (uint column = 0; column < numColumns + ADDITIONAL_X_TILES; ++column)
            {
                if (column > 0 || !odd) // (1): this and (2) avoids x-change when scrolled to odd row numbers
                    UpdateUpTileColumn(position, ref index, 0);

                UpdateDownTileColumn(position, ref index, 0);

                position = map.MoveRight(position);

                if (column == numColumns + ADDITIONAL_X_TILES - 1 && odd) // (2): see (1)
                    UpdateUpTileColumn(position, ref index, 0);
            }
        }
    }
}
