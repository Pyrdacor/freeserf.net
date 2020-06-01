using System;

namespace Freeserf
{
    using Render;
    using System.Collections.Generic;
    using System.Linq;
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
            int column = (int)map.PositionColumn(position);
            int row = (int)map.PositionRow(position);

            int x = column * RenderMap.TILE_WIDTH - row * RenderMap.TILE_WIDTH / 2;
            int y = row * RenderMap.TILE_HEIGHT;

            y -= 4 * (int)map.GetHeight(position);

            NormalizeMapPosition(ref x, ref y);

            return new Position(x, y);
        }

        public Position TileSpaceToViewSpace(MapPos position)
        {
            return MapSpaceToViewSpace(TileSpaceToMapSpace(position));
        }

        private int ScrollX => renderMap == null ? 0 : (int)renderMap.ScrollX;
        private int ScrollY => renderMap == null ? 0 : (int)renderMap.ScrollY;

        public Position MapSpaceToViewSpace(int x, int y)
        {
            int mapWidth = (int)map.Columns * RenderMap.TILE_WIDTH;
            int mapHeight = (int)map.Rows * RenderMap.TILE_HEIGHT;

            int renderX = ScrollX * RenderMap.TILE_WIDTH;
            int renderY = ScrollY * RenderMap.TILE_HEIGHT;

            x -= renderX;
            y -= renderY;

            /*int yDifference = Math.Min(0, -(mapHeight - Global.MAX_VIRTUAL_SCREEN_HEIGHT));

            while (y < yDifference)
            {
                x -= mapWidth / columnRowFactor;
                y += mapHeight;
            }*/

            while (y < 0)
            {
                x -= mapWidth / columnRowFactor;
                y += mapHeight;
            }

            while (y >= mapHeight)
            {
                x += mapWidth / columnRowFactor;
                y -= mapHeight;
            }

            while (x < 0)
                x += mapWidth;

            while (x >= mapWidth)
                x -= mapWidth;

            return new Position(x, y);
        }

        public Position MapSpaceToViewSpace(Position position)
        {
            return MapSpaceToViewSpace(position.X, position.Y);
        }

        public Position ViewSpaceToMapSpace(int x, int y)
        {
            int mapWidth = (int)map.Columns * RenderMap.TILE_WIDTH;
            int mapHeight = (int)map.Rows * RenderMap.TILE_HEIGHT;

            int renderX = ScrollX * RenderMap.TILE_WIDTH;
            int renderY = ScrollY * RenderMap.TILE_HEIGHT;

            x += renderX;
            y += renderY;

            while (y < 0)
            {
                x += (columnRowFactor - 1) * mapWidth / columnRowFactor;
                y += mapHeight;
            }

            while (y >= mapHeight)
            {
                x -= (columnRowFactor - 1) * mapWidth / columnRowFactor;
                y -= mapHeight;
            }

            while (x < 0)
                x += mapWidth;

            while (x >= mapWidth)
                x -= mapWidth;

            return new Position(x, y);
        }

        public Position ViewSpaceToMapSpace(Position position)
        {
            return ViewSpaceToMapSpace(position.X, position.Y);
        }

        void NormalizeMapPosition(ref int x, ref int y)
        {
            int mapWidth = (int)map.Columns * RenderMap.TILE_WIDTH;
            int mapHeight = (int)map.Rows * RenderMap.TILE_HEIGHT;

            while (y < 0)
            {
                x -= mapWidth / columnRowFactor;
                y += mapHeight;
            }

            while (y >= mapHeight)
            {
                x += mapWidth / columnRowFactor;
                y -= mapHeight;
            }

            while (x < 0)
                x += mapWidth;

            while (x >= mapWidth)
                x -= mapWidth;
        }

        double SquaredDistanceToMapPosition(MapPos position, int x, int y)
        {
            int mapWidth = (int)map.Columns * RenderMap.TILE_WIDTH;
            int mapHeight = (int)map.Rows * RenderMap.TILE_HEIGHT;
            var mapPosition = TileSpaceToMapSpace(position);
            int distanceX = Math.Abs(x - mapPosition.X);
            int distanceY = Math.Abs(y - mapPosition.Y);

            if (distanceY > mapHeight / 2)
            {
                distanceY = mapHeight - distanceY;

                mapPosition.X += mapWidth / columnRowFactor;
                mapPosition.X -= mapWidth;

                distanceX = Math.Abs(x - mapPosition.X);
            }

            if (distanceX > mapWidth / 2)
                distanceX = mapWidth - distanceX;

            // We only use the distances for comparing so the squared values are sufficient.
            // Square roots will waste too much performance.
            return distanceX * distanceX + distanceY * distanceY;
        }

        public MapPos MapSpaceToTileSpace(int x, int y)
        {
            NormalizeMapPosition(ref x, ref y);

            int row = (y / RenderMap.TILE_HEIGHT) % (int)map.Rows;
            int column = ((x + row * RenderMap.TILE_WIDTH / 2) / RenderMap.TILE_WIDTH) % (int)map.Columns;
            var position = map.Position((uint)column, (uint)row);
            var mapPosition = TileSpaceToMapSpace(position);
            bool down = map.RenderMap == null ? true : map.RenderMap.ScrollY % 2 == 0;

            if (mapPosition.Y > y)
                mapPosition.Y -= (int)map.Rows * RenderMap.TILE_HEIGHT;

            while (mapPosition.Y < y)
            {
                int height = (int)map.GetHeight(position);

                if (down)
                    position = map.MoveDown(position);
                else
                    position = map.MoveDownRight(position);

                mapPosition.Y += RenderMap.TILE_HEIGHT - ((int)map.GetHeight(position) - height) * 4;

                down = !down;
            }

            // Search the 6 spots around the position and consider the position itself too.
            return Enumerable.Range(0, 7).Select(offset => map.PositionAddSpirally(position, (uint)offset))
                .OrderBy(tile => SquaredDistanceToMapPosition(tile, x, y)).First();
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
