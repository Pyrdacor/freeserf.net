using System;
using System.Collections.Generic;
using System.Text;

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
    public class RenderMap
    {
        internal const int TILE_WIDTH = 32;
        internal const int TILE_HEIGHT = 20;

        static readonly int[] TileMask = new int[81]
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

        uint x = 0; // in columns
        uint y = 0; // in rows
        uint numColumns = 0;
        uint numRows = 0;
        Map map = null;
        ITextureAtlas textureAtlas = null;
        readonly List<ITriangle> triangles = null;

        public RenderMap(uint numColumns, uint numRows, Map map,
            ITriangleFactory triangleFactory, ITextureAtlas textureAtlas)
        {
            x = 0;
            y = 0;
            this.numColumns = numColumns;
            this.numRows = numRows;
            this.map = map;
            this.textureAtlas = textureAtlas;

            uint numTriangles = 2 * (numColumns + 1) * (numRows + 1);

            triangles = new List<ITriangle>((int)numTriangles);

            for (uint i = 0; i < numTriangles; ++i)
                triangles.Add(triangleFactory.Create(i % 2 == 0, TILE_WIDTH, TILE_HEIGHT, 0, 0));

            UpdatePosition();
        }

        public void AttachToRenderLayer(IRenderLayer renderLayer)
        {
            foreach (var triangle in triangles)
                triangle.Layer = renderLayer;
        }

        public void Scroll(int x, int y)
        {
            int column = (int)this.x + x;
            int row = (int)this.y + y;

            if (column < 0)
                column += (int)map.Columns;

            if (row < 0)
                row += (int)map.Rows;

            ScrollTo((uint)column, (uint)row);
        }

        public void ScrollTo(uint x, uint y)
        {
            if (this.x == x && this.y == y)
                return;

            this.x = x;
            this.y = y;

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

        void UpdatePosition()
        {
            if (x >= map.Columns)
                x -= map.Columns;

            if (y >= map.Rows)
                y -= map.Rows;

            MapPos columnBegin = map.Pos(x, y);

            for (uint r = 0; r < numRows + 1; ++r)
            {
                MapPos pos = columnBegin;

                for (int i = 0; i < 2; ++i) // first up and then down columns
                {
                    for (uint c = 0; c < numColumns + 1; ++c)
                    {
                        int offset = (int)(c + r * (numColumns + 1));
                        Map.Terrain terrain;
                        MapPos left;
                        MapPos right;
                        MapPos m;

                        if (i == 0) // up
                        {
                            left = map.MoveDown(pos);
                            right = map.MoveRight(left);
                            m = pos;
                            terrain = map.TypeUp(pos);
                        }
                        else // down
                        {
                            left = pos;
                            right = map.MoveRight(left);
                            m = map.MoveDown(pos);
                            terrain = map.TypeDown(pos);
                        }

                        int hLeft = (int)map.GetHeight(left);
                        int hRight = (int)map.GetHeight(right);
                        int hM = (int)map.GetHeight(m);

                        if (((hLeft - hM) < -4) || ((hLeft - hM) > 4))
                        {
                            throw new ExceptionFreeserf("Failed to draw triangle (1).");
                        }
                        if (((hRight - hM) < -4) || ((hRight - hM) > 4))
                        {
                            throw new ExceptionFreeserf("Failed to draw triangle (2).");
                        }

                        int mask = 4 + hM - hLeft + 9 * (4 + hM - hRight);

                        if (TileMask[mask] < 0)
                        {
                            throw new ExceptionFreeserf("Failed to draw triangle (3).");
                        }

                        int spriteIndex = (int)terrain + TileMask[mask];
                        uint sprite = TileSprites[spriteIndex];

                        triangles[offset * 2 + i].TextureAtlasOffset = textureAtlas.GetOffset(sprite);

                        pos = map.MoveDown(pos);
                    }

                    pos = map.MoveRight(columnBegin);
                }

                columnBegin = map.MoveRight(columnBegin);
            }
        }
    }
}
