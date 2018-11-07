/*
 * RenderMapTile.cs - Handles map tile rendering
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

namespace Freeserf.Render
{
    using MapPos = System.UInt32;

    public class RenderMapTile
    {
        MapPos mapPos = 0;
        Map.Terrain terrain = Map.Terrain.Grass0;
        ITriangle triangle = null;

        public RenderMapTile(MapPos mapPos, Map.Terrain terrain, ITriangleFactory triangleFactory)
        {
            this.mapPos = mapPos;
            this.terrain = terrain;

            Create(triangleFactory);
        }

        void Create(ITriangleFactory tirangleFactory)
        {
            // TODO
            // triangle = triangleFactory.Create(...);
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
