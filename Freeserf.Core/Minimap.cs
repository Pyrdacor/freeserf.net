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
    using Freeserf.Event;
    using MapPos = UInt32;

    // Note: The minimap is drawn as 128x128.
    internal class Minimap : GuiObject
    {
        const int MaxScale = 8;

        Interface interf = null;
        Render.ILayerSprite sprite = null;
        Map map = null;
        MapPos mapOffset = Global.BadMapPos;
        int scale = 1; // 1-8
        bool drawGrid = false;

        public Minimap(Interface interf, Map map = null)
            : base(interf)
        {
            this.interf = interf;
            sprite = interf.RenderView.SpriteFactory.Create(128, 128, 0, 0, false, true) as Render.ILayerSprite;
            sprite.Layer = interf.RenderView.GetLayer(Freeserf.Layer.Minimap);
            sprite.Visible = false;
            sprite.DisplayLayer = (byte)(BaseDisplayLayer + 1);

            SetMap(map);
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            sprite.Visible = false;
        }

        protected override void InternalDraw()
        {
            sprite.X = TotalX;
            sprite.Y = TotalY;
            sprite.Visible = Displayed;
        }

        public void SetMap(Map map)
        {
            if (this.map == map)
                return;

            this.map = map;

            UpdateMinimap();
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
            sprite.DisplayLayer = (byte)(BaseDisplayLayer + 1);
        }

        public void MoveToMapPos(MapPos pos)
        {
            // TODO: I guess we don't need this anymore as we take the offset directly from RenderMap
        }

        /* Initialize minimap data. */
        public void UpdateMinimap(bool force = false)
        {
            if (map == null)
                return;

            var offset = map.RenderMap.GetCenteredPosition();

            if (offset == mapOffset && !force)
                return;

            mapOffset = offset;

            var mapCoordinates = map.RenderMap.GetMapPosition(offset);
            mapCoordinates.X -= (64 / scale) * Render.RenderMap.TILE_WIDTH - Render.RenderMap.TILE_WIDTH;
            mapCoordinates.Y -= (64 / scale) * Render.RenderMap.TILE_HEIGHT - Render.RenderMap.TILE_HEIGHT / 2;
            offset = map.RenderMap.GetMapPosFromMapCoordinates(mapCoordinates.X, mapCoordinates.Y);

            byte[] minimapData = new byte[128 * 128 * 4];
            int visibleWidth = Math.Min(128, (int)map.Columns / scale);
            int visibleHeight = Math.Min(128, (int)map.Rows / scale);
            var pos = offset;
            Render.Color tileColor = null;

            if (visibleWidth * scale < 128)
                visibleWidth = 128 / scale;

            if (visibleHeight * scale < 128)
                visibleHeight = 128 / scale;

            for (int c = 0; c < visibleWidth; ++c)
            {
                var start = pos;

                for (int r = 0; r < visibleHeight; ++r)
                {
                    tileColor = GetTileColor(pos);

                    SetColor(minimapData, c, r, scale, tileColor);

                    if (map.PosRow(pos) % 2 == 0)
                        pos = map.MoveDownRight(pos);
                    else
                        pos = map.MoveDown(pos);
                }

                start = map.MoveRight(start);
                pos = start;
            }

            interf.RenderView.MinimapTextureFactory.ResizeMinimapTexture(128, 128);
            interf.RenderView.MinimapTextureFactory.FillMinimapTexture(minimapData);
        }

        void SetColor(byte[] data, int x, int y, int scale, Render.Color color)
        {
            int xOffset = x * scale;
            int yOffset = y * scale;
            int index = (yOffset * 128 + xOffset) * 4;
            int rowIndex = index;

            for (int r = 0; r < scale; ++r)
            {
                for (int c = 0; c < scale; ++c)
                {
                    data[index++] = color.B;
                    data[index++] = color.G;
                    data[index++] = color.R;
                    data[index++] = color.A;
                }

                rowIndex += 128 * 4;
                index = rowIndex;
            }
        }

        Render.Color GetTileColor(MapPos pos)
        {
            int typeOff = ColorOffset[(int)map.TypeUp(pos)];

            int h1 = (int)map.GetHeight(map.MoveRight(pos));
            int h2 = (int)map.GetHeight(map.MoveDown(pos));

            int hOff = h2 - h1 + 8;

            return Colors[typeOff + hOff];
        }

        protected override bool HandleClickLeft(int x, int y)
        {
            x -= TotalX;
            y -= TotalY;

            x /= scale;
            y /= scale;

            int visibleWidth = Math.Min(128, (int)map.Columns / scale);
            int visibleHeight = Math.Min(128, (int)map.Rows / scale);
            var mapPosition = map.RenderMap.GetMapPosition(mapOffset);

            mapPosition.X += Render.RenderMap.TILE_WIDTH / 2;
            mapPosition.Y += Render.RenderMap.TILE_HEIGHT / 2;

            if (visibleWidth * scale < 128)
                visibleWidth = 128 / scale;

            if (visibleHeight * scale < 128)
                visibleHeight = 128 / scale;

            int relX = x - visibleHeight / 2;
            int relY = y - visibleHeight / 2;

            mapPosition.X += relX * Render.RenderMap.TILE_WIDTH;
            mapPosition.Y += relY * Render.RenderMap.TILE_HEIGHT;

            var pos = map.RenderMap.GetMapPosFromMapCoordinates(mapPosition.X, mapPosition.Y);

            interf.GotoMapPos(pos);
            UpdateMinimap();

            return true;
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

        Interface interf = null;

        public MinimapGame(Interface interf, Game game)
            : base(interf, game.Map)
        {
            this.interf = interf;
        }

        protected override bool HandleDoubleClick(int x, int y, Event.Button button)
        {
            if (button == Event.Button.Left)
            {
                // jump to map pos
                HandleClickLeft(x, y);

                // close the minimap
                interf.ClosePopup();
            }

            return true;
        }
    }
}
