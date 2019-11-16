using System;

namespace Freeserf
{
    using Render;
    using MapPos = UInt32;

    /* Space transformations.

       The game world space is a three dimensional space with the axes
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

        public Position TileSpaceToMapSpace(MapPos position)
        {
            int lwidth = (int)map.Columns * RenderMap.TILE_WIDTH;
            int lheight = (int)map.Rows * RenderMap.TILE_HEIGHT;

            int column = (int)map.PositionColumn(position);
            int row = (int)map.PositionRow(position);

            int x = RenderMap.TILE_WIDTH * column - (RenderMap.TILE_WIDTH / 2) * row;
            int y = row * RenderMap.TILE_HEIGHT;

            y -= 4 * (int)map.GetHeight(position);

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

        public Position TileSpaceToViewSpace(MapPos position)
        {
            return MapSpaceToViewSpace(TileSpaceToMapSpace(position));
        }

        public Position MapSpaceToViewSpace(int x, int y)
        {
            int lwidth = (int)map.Columns * RenderMap.TILE_WIDTH;
            int lheight = (int)map.Rows * RenderMap.TILE_HEIGHT;

            int renderX = (int)renderMap.ScrollX * RenderMap.TILE_WIDTH;
            int renderY = (int)renderMap.ScrollY * RenderMap.TILE_HEIGHT;

            x -= renderX;
            y -= renderY;

            int yDifference = Math.Min(0, -(lheight - Global.MAX_VIRTUAL_SCREEN_HEIGHT));

            while (y < yDifference)
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

        public Position MapSpaceToViewSpace(Position position)
        {
            return MapSpaceToViewSpace(position.X, position.Y);
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

        public Position ViewSpaceToMapSpace(Position position)
        {
            return ViewSpaceToMapSpace(position.X, position.Y);
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
            int lastDistance = int.MaxValue;
            var position = map.MoveUp(map.Position((uint)column, 0u));

            position = map.MoveDownN(position, row);
            var lastPosition = position;

            while (true)
            {
                int rowY = TileSpaceToMapSpace(position).Y;

                int distance = Math.Abs(rowY - y);

                if (distance >= lheight / 2)
                    distance = Math.Abs(distance - lheight);

                if (lastDistance < distance)
                {
                    var mapPosition = TileSpaceToMapSpace(lastPosition);

                    int xOffset = x - mapPosition.X;

                    if (xOffset >= lwidth)
                        xOffset -= lwidth;
                    else if (xOffset <= -lwidth)
                        xOffset += lwidth;

                    if (xOffset >= lwidth / 2)
                        xOffset -= lwidth / 2;
                    else if (xOffset <= -lwidth / 2)
                        xOffset += lwidth / 2;

                    if (xOffset >= lwidth / 4)
                        xOffset -= lwidth / 4;
                    else if (xOffset <= -lwidth / 4)
                        xOffset += lwidth / 4;

                    bool moved = true;

                    if (xOffset >= RenderMap.TILE_WIDTH / 2)
                        lastPosition = map.MoveRight(lastPosition);
                    else if (xOffset < -RenderMap.TILE_WIDTH / 2)
                        lastPosition = map.MoveLeft(lastPosition);
                    else
                        moved = false;

                    // re-check y distance
                    if (moved)
                    {
                        // TODO: as we moved to left or right, the height may lead to a wrong tile
                        /*rowY = TileSpaceToMapSpace(lastPosition).Y;

                        if (rowY < y - RenderMap.TILE_HEIGHT / 2)
                        {
                            if (xOffset < 0)
                                lastPosition = map.MoveUpLeft(lastPosition);
                            else
                                lastPosition = map.MoveUp(lastPosition);
                        }
                        else if (rowY > y + RenderMap.TILE_HEIGHT / 2)
                        {
                            if (xOffset < 0)
                                lastPosition = map.MoveDown(lastPosition);
                            else
                                lastPosition = map.MoveDownRight(lastPosition);
                        }*/
                    }

                    return lastPosition;
                }

                lastDistance = distance;
                lastPosition = position;

                if (row % 2 == 0)
                    position = map.MoveDown(position);
                else
                    position = map.MoveDownRight(position);

                ++row;
            }
        }

        public MapPos MapSpaceToTileSpace(Position position)
        {
            return MapSpaceToTileSpace(position.X, position.Y);
        }

        public MapPos ViewSpaceToTileSpace(uint renderColumn, uint renderRow)
        {
            return ViewSpaceToTileSpace((int)renderColumn * RenderMap.TILE_WIDTH, (int)renderRow * RenderMap.TILE_HEIGHT);
        }

        public MapPos ViewSpaceToTileSpace(int x, int y)
        {
            return MapSpaceToTileSpace(ViewSpaceToMapSpace(x, y));
        }

        public MapPos ViewSpaceToTileSpace(Position position)
        {
            return ViewSpaceToTileSpace(position.X, position.Y);
        }
    }
}
