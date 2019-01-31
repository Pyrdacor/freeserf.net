using System;

namespace Freeserf
{
    using Render;
    using MapPos = UInt32;

    internal class CoordinateSpace
    {
        readonly Map map = null;
        readonly RenderMap renderMap = null;

        internal CoordinateSpace(Map map, RenderMap renderMap)
        {
            this.map = map;
            this.renderMap = renderMap;
        }

        public Position TileSpaceToMapSpace(MapPos pos)
        {
            int lwidth = (int)map.Columns * RenderMap.TILE_WIDTH;
            int lheight = (int)map.Rows * RenderMap.TILE_HEIGHT;

            int column = (int)map.PosColumn(pos);
            int row = (int)map.PosRow(pos);

            int x = RenderMap.TILE_WIDTH * column - (RenderMap.TILE_WIDTH / 2) * row;
            int y = row * RenderMap.TILE_HEIGHT;

            y -= 4 * (int)map.GetHeight(pos);

            /*if (y < 0)
            {
                x -= (int)map.Rows * RenderMap.TILE_WIDTH / 2;
                y += lheight;
            }*/

            if (x < 0)
                x += lwidth;
            /*else if (x >= lwidth)
                x -= lwidth;*/

            return new Position(x, y);
        }

        public Position TileSpaceToViewSpace(MapPos pos)
        {
            return MapSpaceToViewSpace(TileSpaceToMapSpace(pos));
        }

        public Position MapSpaceToViewSpace(int x, int y)
        {
            int lwidth = (int)map.Columns * RenderMap.TILE_WIDTH;
            int lheight = (int)map.Rows * RenderMap.TILE_HEIGHT;

            int renderX = (int)renderMap.ScrollX * RenderMap.TILE_WIDTH;
            int renderY = (int)renderMap.ScrollY * RenderMap.TILE_HEIGHT;

            x -= renderX;
            y -= renderY;

            int yDiff = Math.Min(0, -(lheight - Global.MAX_VIRTUAL_SCREEN_HEIGHT));

            while (y < yDiff)
            {
                x -= (int)map.Rows * RenderMap.TILE_WIDTH / 2;
                y += lheight;
            }

            while (y >= lheight)
            {
                x += (int)map.Rows * RenderMap.TILE_WIDTH / 2;
                y -= lheight;
            }

            while (x < 0)
                x += lwidth;

            while (x >= lwidth)
                x -= lwidth;

            return new Position(x, y);
        }

        public Position MapSpaceToViewSpace(Position pos)
        {
            return MapSpaceToViewSpace(pos.X, pos.Y);
        }

        public Position ViewSpaceToMapSpace(int x, int y)
        {
            int lwidth = (int)map.Columns * RenderMap.TILE_WIDTH;
            int lheight = (int)map.Rows * RenderMap.TILE_HEIGHT;

            int renderX = (int)renderMap.ScrollX * RenderMap.TILE_WIDTH;
            int renderY = (int)renderMap.ScrollY * RenderMap.TILE_HEIGHT;

            x += renderX;
            y += renderY;

            while (y < 0)
            {
                x += (int)map.Rows * RenderMap.TILE_WIDTH / 2;
                y += lheight;
            }

            while (y >= lheight)
            {
                x -= (int)map.Rows * RenderMap.TILE_WIDTH / 2;
                y -= lheight;
            }

            while (x < 0)
                x += lwidth;

            while (x >= lwidth)
                x -= lwidth;

            return new Position(x, y);
        }

        public Position ViewSpaceToMapSpace(Position pos)
        {
            return ViewSpaceToMapSpace(pos.X, pos.Y);
        }

        public MapPos MapSpaceToTileSpace(int x, int y)
        {
            int lwidth = (int)map.Columns * RenderMap.TILE_WIDTH;
            int lheight = (int)map.Rows * RenderMap.TILE_HEIGHT;

            while (y < 0)
                y += lheight;

            while (y >= lheight)
                y -= lheight;

            while (x < 0)
                x += lwidth;

            while (x >= lwidth)
                x -= lwidth;

            int mappedX = x + (y * RenderMap.TILE_WIDTH) / (2 * RenderMap.TILE_HEIGHT);
            int column = mappedX / RenderMap.TILE_WIDTH;
            int row = y / RenderMap.TILE_HEIGHT;
            int lastDist = int.MaxValue;
            var pos = map.Pos((uint)column, 0u);
            MapPos lastPos = pos;

            pos = map.MoveDownN(pos, row);

            while (true)
            {
                int rowY = TileSpaceToMapSpace(pos).Y;

                int dist = Math.Abs(rowY - y);

                if (lastDist < dist)
                {
                    var mapPosition = TileSpaceToMapSpace(lastPos);

                    int xOff = x - mapPosition.X;
                    bool moved = true;

                    if (xOff >= RenderMap.TILE_WIDTH / 2)
                        lastPos = map.MoveRight(lastPos);
                    else if (xOff < -RenderMap.TILE_WIDTH / 2)
                        lastPos = map.MoveLeft(lastPos);
                    else
                        moved = false;

                    // re-check y dist
                    if (moved)
                    {
                        // TODO: as we moved to left or right, the height may lead to a wrong tile
                    }

                    return lastPos;
                }

                lastDist = dist;
                lastPos = pos;

                if (row % 2 == 0)
                    pos = map.MoveDown(pos);
                else
                    pos = map.MoveDownRight(pos);

                ++row;
            }
        }

        public MapPos MapSpaceToTileSpace(Position pos)
        {
            return MapSpaceToTileSpace(pos.X, pos.Y);
        }

        public MapPos ViewSpaceToTileSpace(uint renderColumn, uint renderRow)
        {
            return ViewSpaceToTileSpace((int)renderColumn * RenderMap.TILE_WIDTH, (int)renderRow * RenderMap.TILE_HEIGHT);
        }

        public MapPos ViewSpaceToTileSpace(int x, int y)
        {
            return MapSpaceToTileSpace(ViewSpaceToMapSpace(x, y));
        }

        public MapPos ViewSpaceToTileSpace(Position pos)
        {
            return ViewSpaceToTileSpace(pos.X, pos.Y);
        }
    }
}
