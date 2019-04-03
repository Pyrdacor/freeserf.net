using System;

namespace Freeserf
{
    using Render;
    using MapPos = UInt32;

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
    internal class CoordinateSpace
    {
        readonly Map map = null;
        readonly RenderMap renderMap = null;
        readonly int columnRowFactor = 2;

        internal CoordinateSpace(Map map, RenderMap renderMap)
        {
            this.map = map;
            this.renderMap = renderMap;

            columnRowFactor = (map.Size % 2 == 0) ? 4 : 2;
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
                x += (columnRowFactor - 1) * (int)map.Rows * RenderMap.TILE_WIDTH / 2;
                y += lheight;
            }

            while (y >= lheight)
            {
                x -= (columnRowFactor - 1) * (int)map.Rows * RenderMap.TILE_WIDTH / 2;
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

            int mappedX = (x + (y * RenderMap.TILE_WIDTH) / (2 * RenderMap.TILE_HEIGHT));// % lwidth;
            int column = mappedX / RenderMap.TILE_WIDTH;
            int row = y / RenderMap.TILE_HEIGHT;
            int lastDist = int.MaxValue;
            var pos = map.MoveUp(map.Pos((uint)column, 0u));

            pos = map.MoveDownN(pos, row);
            MapPos lastPos = pos;

            while (true)
            {
                int rowY = TileSpaceToMapSpace(pos).Y;

                int dist = Math.Abs(rowY - y);

                if (dist >= lheight / 2)
                    dist = Math.Abs(dist - lheight);

                if (lastDist < dist)
                {
                    var mapPosition = TileSpaceToMapSpace(lastPos);

                    int xOff = x - mapPosition.X;

                    if (xOff >= lwidth)
                        xOff -= lwidth;
                    else if (xOff <= -lwidth)
                        xOff += lwidth;

                    if (xOff >= lwidth / 2)
                        xOff -= lwidth / 2;
                    else if (xOff <= -lwidth / 2)
                        xOff += lwidth / 2;

                    if (xOff >= lwidth / 4)
                        xOff -= lwidth / 4;
                    else if (xOff <= -lwidth / 4)
                        xOff += lwidth / 4;

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
                        /*rowY = TileSpaceToMapSpace(lastPos).Y;

                        if (rowY < y - RenderMap.TILE_HEIGHT / 2)
                        {
                            if (xOff < 0)
                                lastPos = map.MoveUpLeft(lastPos);
                            else
                                lastPos = map.MoveUp(lastPos);
                        }
                        else if (rowY > y + RenderMap.TILE_HEIGHT / 2)
                        {
                            if (xOff < 0)
                                lastPos = map.MoveDown(lastPos);
                            else
                                lastPos = map.MoveDownRight(lastPos);
                        }*/
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
