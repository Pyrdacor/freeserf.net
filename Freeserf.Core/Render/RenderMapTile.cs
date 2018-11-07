using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Render
{
    using MapPos = UInt32;

    public class RenderMapTile
    {
        MapPos mapPos = 0;
        Map.Terrain terrain = Map.Terrain.Grass0;
        ITriangle triangle = null;

        public RenderMapTile(MapPos mapPos, Map.Terrain terrain, ITriangle triangle)
        {
            this.mapPos = mapPos;
            this.terrain = terrain;
            this.triangle = triangle;
        }

        public void Delete()
        {
            triangle.Delete();
            triangle = null;
        }

        public void Draw(Rect visibleMapArea)
        {
            if (triangle == null)
                return;

            // TODO: if tile is outside the visible map area -> return
            // TODO: set position and texture coords based on:
            // mapPos
            // terrain
        }
    }
}
