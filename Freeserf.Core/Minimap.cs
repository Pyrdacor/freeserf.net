/*
 * Minimap.cs - Minimap GUI component
 *
 * Copyright (C) 2013   Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018   Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Freeserf
{
    using MapPos = UInt32;

    // Note: The minimap in GameInitBox is drawn as 128x128.
    internal class Minimap : GuiObject
    {
        const int MaxScale = 8;

        Interface interf = null;
        Map map = null;
        int offsetX = 0;
        int offsetY = 0;
        int scale = 1; // 1-8
        bool drawGrid = false;
        readonly List<Render.Color> minimap = new List<Render.Color>();

        public Minimap(Interface interf, Map map = null)
            : base(interf)
        {
            this.interf = interf;

            SetMap(map);
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            // TODO
        }

        protected override void InternalDraw()
        {
            // TODO
        }

        public void SetMap(Map map)
        {
            this.map = map;
        }

        public void SetDrawGrid(bool draw)
        {
            if (drawGrid == draw)
                return;

            drawGrid = draw;

            SetRedraw();
        }

        protected internal override void UpdateParent()
        {
            // TODO
        }

        public void MoveToMapPos(MapPos pos)
        {

        }

        /* Initialize minimap data. */
        void InitMinimap()
        {
            if (map == null)
                return;

            byte[] minimapData = new byte[128 * 128 * 4];

            foreach (MapPos pos in map.Geometry)
            {
                int typeOff = ColorOffset[(int)map.TypeUp(pos)];

                int h1 = (int)map.GetHeight(map.MoveRight(pos));
                int h2 = (int)map.GetHeight(map.MoveLeft(map.MoveDown(pos)));

                int hOff = h2 - h1 + 8;

                minimap.Add(Colors[typeOff + hOff]);
            }

            UpdateTexture();
        }

        void UpdateTexture()
        {
            //interf.RenderView.MinimapTextureFactory.FillMinimapTexture()
        }

        static readonly int[] ColorOffset = new int[]
        {
            0, 85, 102, 119, 17, 17, 17, 17,
            34, 34, 34, 51, 51, 51, 68, 68
        };

        static readonly Render.Color[] Colors = new Render.Color[]
        {
            new Render.Color(0x00, 0x00, 0xaf), new Render.Color(0x00, 0x00, 0xaf), new Render.Color(0x00, 0x00, 0xaf),
            new Render.Color(0x00, 0x00, 0xaf), new Render.Color(0x00, 0x00, 0xaf), new Render.Color(0x00, 0x00, 0xaf),
            new Render.Color(0x00, 0x00, 0xaf), new Render.Color(0x00, 0x00, 0xaf), new Render.Color(0x00, 0x00, 0xaf),
            new Render.Color(0x00, 0x00, 0xaf), new Render.Color(0x00, 0x00, 0xaf), new Render.Color(0x00, 0x00, 0xaf),
            new Render.Color(0x00, 0x00, 0xaf), new Render.Color(0x00, 0x00, 0xaf), new Render.Color(0x00, 0x00, 0xaf),
            new Render.Color(0x00, 0x00, 0xaf), new Render.Color(0x00, 0x00, 0xaf), new Render.Color(0x73, 0xb3, 0x43),
            new Render.Color(0x73, 0xb3, 0x43), new Render.Color(0x6b, 0xab, 0x3b), new Render.Color(0x63, 0xa3, 0x33),
            new Render.Color(0x5f, 0x9b, 0x2f), new Render.Color(0x57, 0x93, 0x27), new Render.Color(0x53, 0x8b, 0x23),
            new Render.Color(0x4f, 0x83, 0x1b), new Render.Color(0x47, 0x7f, 0x17), new Render.Color(0x3f, 0x73, 0x13),
            new Render.Color(0x3b, 0x6b, 0x13), new Render.Color(0x33, 0x63, 0x0f), new Render.Color(0x2f, 0x57, 0x0b),
            new Render.Color(0x2b, 0x4f, 0x0b), new Render.Color(0x23, 0x43, 0x0b), new Render.Color(0x1f, 0x3b, 0x07),
            new Render.Color(0x1b, 0x33, 0x07), new Render.Color(0xef, 0xcf, 0xaf), new Render.Color(0xef, 0xcf, 0xaf),
            new Render.Color(0xe3, 0xbf, 0x9f), new Render.Color(0xd7, 0xb3, 0x8f), new Render.Color(0xd7, 0xb3, 0x8f),
            new Render.Color(0xcb, 0xa3, 0x7f), new Render.Color(0xbf, 0x97, 0x73), new Render.Color(0xbf, 0x97, 0x73),
            new Render.Color(0xb3, 0x87, 0x67), new Render.Color(0xab, 0x7b, 0x5b), new Render.Color(0xab, 0x7b, 0x5b),
            new Render.Color(0x9f, 0x6f, 0x4f), new Render.Color(0x93, 0x63, 0x43), new Render.Color(0x93, 0x63, 0x43),
            new Render.Color(0x87, 0x57, 0x3b), new Render.Color(0x7b, 0x4f, 0x33), new Render.Color(0x7b, 0x4f, 0x33),
            new Render.Color(0xd7, 0xb3, 0x8f), new Render.Color(0xd7, 0xb3, 0x8f), new Render.Color(0xcb, 0xa3, 0x7f),
            new Render.Color(0xcb, 0xa3, 0x7f), new Render.Color(0xbf, 0x97, 0x73), new Render.Color(0xbf, 0x97, 0x73),
            new Render.Color(0xb3, 0x87, 0x67), new Render.Color(0xab, 0x7b, 0x5b), new Render.Color(0x9f, 0x6f, 0x4f),
            new Render.Color(0x93, 0x63, 0x43), new Render.Color(0x87, 0x57, 0x3b), new Render.Color(0x7b, 0x4f, 0x33),
            new Render.Color(0x73, 0x43, 0x2b), new Render.Color(0x67, 0x3b, 0x23), new Render.Color(0x5b, 0x33, 0x1b),
            new Render.Color(0x4f, 0x2b, 0x17), new Render.Color(0x43, 0x23, 0x13), new Render.Color(0xff, 0xff, 0xff),
            new Render.Color(0xff, 0xff, 0xff), new Render.Color(0xef, 0xef, 0xef), new Render.Color(0xef, 0xef, 0xef),
            new Render.Color(0xdf, 0xdf, 0xdf), new Render.Color(0xd3, 0xd3, 0xd3), new Render.Color(0xc3, 0xc3, 0xc3),
            new Render.Color(0xb3, 0xb3, 0xb3), new Render.Color(0xa7, 0xa7, 0xa7), new Render.Color(0x97, 0x97, 0x97),
            new Render.Color(0x87, 0x87, 0x87), new Render.Color(0x7b, 0x7b, 0x7b), new Render.Color(0x6b, 0x6b, 0x6b),
            new Render.Color(0x5b, 0x5b, 0x5b), new Render.Color(0x4f, 0x4f, 0x4f), new Render.Color(0x3f, 0x3f, 0x3f),
            new Render.Color(0x2f, 0x2f, 0x2f), new Render.Color(0x07, 0x07, 0xb3), new Render.Color(0x07, 0x07, 0xb3),
            new Render.Color(0x07, 0x07, 0xb3), new Render.Color(0x07, 0x07, 0xb3), new Render.Color(0x07, 0x07, 0xb3),
            new Render.Color(0x07, 0x07, 0xb3), new Render.Color(0x07, 0x07, 0xb3), new Render.Color(0x07, 0x07, 0xb3),
            new Render.Color(0x07, 0x07, 0xb3), new Render.Color(0x07, 0x07, 0xb3), new Render.Color(0x07, 0x07, 0xb3),
            new Render.Color(0x07, 0x07, 0xb3), new Render.Color(0x07, 0x07, 0xb3), new Render.Color(0x07, 0x07, 0xb3),
            new Render.Color(0x07, 0x07, 0xb3), new Render.Color(0x07, 0x07, 0xb3), new Render.Color(0x07, 0x07, 0xb3),
            new Render.Color(0x0b, 0x0b, 0xb7), new Render.Color(0x0b, 0x0b, 0xb7), new Render.Color(0x0b, 0x0b, 0xb7),
            new Render.Color(0x0b, 0x0b, 0xb7), new Render.Color(0x0b, 0x0b, 0xb7), new Render.Color(0x0b, 0x0b, 0xb7),
            new Render.Color(0x0b, 0x0b, 0xb7), new Render.Color(0x0b, 0x0b, 0xb7), new Render.Color(0x0b, 0x0b, 0xb7),
            new Render.Color(0x0b, 0x0b, 0xb7), new Render.Color(0x0b, 0x0b, 0xb7), new Render.Color(0x0b, 0x0b, 0xb7),
            new Render.Color(0x0b, 0x0b, 0xb7), new Render.Color(0x0b, 0x0b, 0xb7), new Render.Color(0x0b, 0x0b, 0xb7),
            new Render.Color(0x0b, 0x0b, 0xb7), new Render.Color(0x0b, 0x0b, 0xb7), new Render.Color(0x13, 0x13, 0xbb),
            new Render.Color(0x13, 0x13, 0xbb), new Render.Color(0x13, 0x13, 0xbb), new Render.Color(0x13, 0x13, 0xbb),
            new Render.Color(0x13, 0x13, 0xbb), new Render.Color(0x13, 0x13, 0xbb), new Render.Color(0x13, 0x13, 0xbb),
            new Render.Color(0x13, 0x13, 0xbb), new Render.Color(0x13, 0x13, 0xbb), new Render.Color(0x13, 0x13, 0xbb),
            new Render.Color(0x13, 0x13, 0xbb), new Render.Color(0x13, 0x13, 0xbb), new Render.Color(0x13, 0x13, 0xbb),
            new Render.Color(0x13, 0x13, 0xbb), new Render.Color(0x13, 0x13, 0xbb), new Render.Color(0x13, 0x13, 0xbb),
            new Render.Color(0x13, 0x13, 0xbb)
        };
    }

    internal class MinimapGame : Minimap
    {
        public enum OwnershipMode
        {
            None = 0,
            Mixed = 1,
            Solid = 2,
            Last = Solid
        }

        public MinimapGame(Interface interf, Game game)
            : base(interf)
        {

        }
    }
}
