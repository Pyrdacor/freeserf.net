/*
 * Minimap.cs - Minimap GUI component
 *
 * Copyright (C) 2013       Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018-2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Freeserf.UI
{
    using Freeserf.Render;
    using MapPos = UInt32;

    // Note: The minimap is drawn as 128x128.
    internal class Minimap : GuiObject
    {
        const int MinScale = 1;
        const int MaxScale = 8;

        protected readonly Interface interf = null;
        readonly ILayerSprite sprite = null;
        protected Map map = null;
        MapPos mapOffset = Global.INVALID_MAPPOS;
        int scale = MinScale;
        protected static readonly Color GridColor = new Color(0x01, 0x01, 0x01);

        public Minimap(Interface interf, Map map = null)
            : base(interf)
        {
            this.interf = interf;
            sprite = interf.RenderView.SpriteFactory.Create(128, 128, 0, 0, false, true) as ILayerSprite;
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

        public void SetScale(int scale)
        {
            if (scale < MinScale)
                scale = MinScale;
            else if (scale > MaxScale)
                scale = MaxScale;

            if (this.scale == scale)
                return;

            this.scale = scale;
            UpdateMinimap(true);
        }

        public int GetScale()
        {
            return scale;
        }

        public void SetMap(Map map)
        {
            if (this.map == map)
                return;

            this.map = map;

            UpdateMinimap(true);
        }

        protected internal override void UpdateParent()
        {
            base.UpdateParent();

            sprite.DisplayLayer = (byte)(BaseDisplayLayer + 1);
        }

        public override void HandleZoomChange()
        {
            base.HandleZoomChange();

            UpdateMinimap(true);
        }

        // Initialize minimap data. 
        public void UpdateMinimap(bool force = false)
        {
            if (map == null)
                return;

            var offset = map.RenderMap.GetCenteredPosition();

            if (offset == mapOffset && !force)
                return;

            mapOffset = offset;

            var mapCoordinates = map.RenderMap.CoordinateSpace.TileSpaceToViewSpace(offset);
            mapCoordinates.X -= (64 / scale) * RenderMap.TILE_WIDTH + RenderMap.TILE_WIDTH;
            mapCoordinates.Y -= (64 / scale) * RenderMap.TILE_HEIGHT - RenderMap.TILE_HEIGHT;

            offset = map.RenderMap.CoordinateSpace.ViewSpaceToTileSpace(mapCoordinates.X, mapCoordinates.Y);

            byte[] minimapData = new byte[128 * 128 * 4];
            int visibleWidth = Math.Min(128, (int)map.Columns) / scale;
            int visibleHeight = Math.Min(128, (int)map.Rows) / scale;
            var position = offset;
            Color tileColor;
            int index = 0;

            if (visibleWidth * scale < 128)
                visibleWidth = 128 / scale;

            if (visibleHeight * scale < 128)
                visibleHeight = 128 / scale;

            var virtualScreenSize = interf.RenderView.VirtualScreen.Size;
            var zoom = map.RenderMap.ZoomFactor;
            int viewRectWidth = Misc.Round(virtualScreenSize.Width / zoom) / RenderMap.TILE_WIDTH;
            int viewRectHeight = Misc.Round(virtualScreenSize.Height / zoom) / RenderMap.TILE_HEIGHT;
            int viewRectX = (visibleWidth - viewRectWidth) / 2;
            int viewRectY = (visibleHeight - viewRectHeight) / 2;

            for (int column = 0; column < visibleWidth; ++column)
            {
                var start = position;

                for (int row = 0; row < visibleHeight; ++row)
                {
                    if (((column == viewRectX || column == viewRectX + viewRectWidth) && row >= viewRectY && row < viewRectY + viewRectHeight) ||
                        ((row == viewRectY || row == viewRectY + viewRectHeight) && column >= viewRectX && column < viewRectX + viewRectWidth))
                    {
                        // Draw the view area box
                        SetGridColor(minimapData, column, row, scale, false, map.PositionColumn(position), map.PositionRow(position));
                    }
                    else
                    {
                        tileColor = GetTileColor(position, index++);

                        if (tileColor == GridColor) // Grid
                            SetGridColor(minimapData, column, row, scale, true, map.PositionColumn(position), map.PositionRow(position));
                        else
                            SetColor(minimapData, column, row, scale, tileColor);
                    }

                    if (map.PositionRow(position) % 2 == 0)
                        position = map.MoveDownRight(position);
                    else
                        position = map.MoveDown(position);
                }

                start = map.MoveRight(start);
                position = start;
            }

            interf.RenderView.MinimapTextureFactory.ResizeMinimapTexture(128, 128);
            interf.RenderView.MinimapTextureFactory.FillMinimapTexture(minimapData);
        }

        static void SetColor(byte[] data, int x, int y, int scale, Color color)
        {
            int xOffset = x * scale;
            int yOffset = y * scale;
            int index = (yOffset * 128 + xOffset) * 4;
            int rowIndex = index;

            for (int row = 0; row < scale; ++row)
            {
                for (int column = 0; column < scale; ++column)
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

        void SetGridColor(byte[] data, int x, int y, int scale, bool mapGrid, uint mapColumn, uint mapRow)
        {
            int xOffset = x * scale;
            int yOffset = y * scale;
            int index = (yOffset * 128 + xOffset) * 4;
            int rowIndex = index;
            Color color;

            for (int row = 0; row < scale; ++row)
            {
                for (int column = 0; column < scale; ++column)
                {
                    if (mapGrid)
                    {
                        if (mapColumn == 0)
                        {
                            if (mapRow % 2 == 1)
                                color = Color.White;
                            else
                                color = GridColor;
                        }
                        else
                        {
                            if ((mapColumn + mapRow) % 2 == 0)
                                color = Color.White;
                            else
                                color = GridColor;
                        }
                    }
                    else
                    {
                        if ((x + y) % 2 == 0)
                            color = Color.White;
                        else
                            color = GridColor;
                    }

                    data[index++] = color.B;
                    data[index++] = color.G;
                    data[index++] = color.R;
                    data[index++] = color.A;
                }

                rowIndex += 128 * 4;
                index = rowIndex;
            }
        }

        protected virtual Color GetTileColor(MapPos position, int index)
        {
            int typeOff = ColorOffset[(int)map.TypeUp(position)];

            int h1 = (int)map.GetHeight(map.MoveRight(position));
            int h2 = (int)map.GetHeight(map.MoveDown(position));

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
            var mapPosition = map.RenderMap.CoordinateSpace.TileSpaceToMapSpace(mapOffset);

            mapPosition.X -= RenderMap.TILE_WIDTH / 2;
            mapPosition.Y -= RenderMap.TILE_HEIGHT;

            if (visibleWidth * scale < 128)
                visibleWidth = 128 / scale;

            if (visibleHeight * scale < 128)
                visibleHeight = 128 / scale;

            int relX = x - visibleWidth / 2;
            int relY = y - visibleHeight / 2;

            mapPosition.X += relX * RenderMap.TILE_WIDTH;
            mapPosition.Y += relY * RenderMap.TILE_HEIGHT;

            int lheight = (int)map.Rows * RenderMap.TILE_HEIGHT;

            if (mapPosition.Y < 0)
            {
                mapPosition.Y += lheight;
                mapPosition.X -= (int)map.Rows * RenderMap.TILE_WIDTH / 2;
            }
            else if (mapPosition.Y >= lheight)
            {
                mapPosition.Y -= lheight;
                mapPosition.X += (int)map.Rows * RenderMap.TILE_WIDTH / 2;
            }

            // TODO: y regarding the grid seems to be 3 pixels to high (with scale 1). Maybe the grid is out of place as other positions work.
            var position = map.RenderMap.CoordinateSpace.MapSpaceToTileSpace(mapPosition.X, mapPosition.Y);

            interf.GotoMapPosition(position);

            return true;
        }

        protected override void Layout()
        {
            base.Layout();

            sprite.Resize(Width, Height);
        }

        static readonly int[] ColorOffset = new int[]
        {
            0, 85, 102, 119, 17, 17, 17, 17,
            34, 34, 34, 51, 51, 51, 68, 68
        };

        static readonly Color[] Colors = new Color[]
        {
            new Color(0x00, 0x00, 0xaf), new Color(0x00, 0x00, 0xaf), new Color(0x00, 0x00, 0xaf),
            new Color(0x00, 0x00, 0xaf), new Color(0x00, 0x00, 0xaf), new Color(0x00, 0x00, 0xaf),
            new Color(0x00, 0x00, 0xaf), new Color(0x00, 0x00, 0xaf), new Color(0x00, 0x00, 0xaf),
            new Color(0x00, 0x00, 0xaf), new Color(0x00, 0x00, 0xaf), new Color(0x00, 0x00, 0xaf),
            new Color(0x00, 0x00, 0xaf), new Color(0x00, 0x00, 0xaf), new Color(0x00, 0x00, 0xaf),
            new Color(0x00, 0x00, 0xaf), new Color(0x00, 0x00, 0xaf), new Color(0x73, 0xb3, 0x43),
            new Color(0x73, 0xb3, 0x43), new Color(0x6b, 0xab, 0x3b), new Color(0x63, 0xa3, 0x33),
            new Color(0x5f, 0x9b, 0x2f), new Color(0x57, 0x93, 0x27), new Color(0x53, 0x8b, 0x23),
            new Color(0x4f, 0x83, 0x1b), new Color(0x47, 0x7f, 0x17), new Color(0x3f, 0x73, 0x13),
            new Color(0x3b, 0x6b, 0x13), new Color(0x33, 0x63, 0x0f), new Color(0x2f, 0x57, 0x0b),
            new Color(0x2b, 0x4f, 0x0b), new Color(0x23, 0x43, 0x0b), new Color(0x1f, 0x3b, 0x07),
            new Color(0x1b, 0x33, 0x07), new Color(0xef, 0xcf, 0xaf), new Color(0xef, 0xcf, 0xaf),
            new Color(0xe3, 0xbf, 0x9f), new Color(0xd7, 0xb3, 0x8f), new Color(0xd7, 0xb3, 0x8f),
            new Color(0xcb, 0xa3, 0x7f), new Color(0xbf, 0x97, 0x73), new Color(0xbf, 0x97, 0x73),
            new Color(0xb3, 0x87, 0x67), new Color(0xab, 0x7b, 0x5b), new Color(0xab, 0x7b, 0x5b),
            new Color(0x9f, 0x6f, 0x4f), new Color(0x93, 0x63, 0x43), new Color(0x93, 0x63, 0x43),
            new Color(0x87, 0x57, 0x3b), new Color(0x7b, 0x4f, 0x33), new Color(0x7b, 0x4f, 0x33),
            new Color(0xd7, 0xb3, 0x8f), new Color(0xd7, 0xb3, 0x8f), new Color(0xcb, 0xa3, 0x7f),
            new Color(0xcb, 0xa3, 0x7f), new Color(0xbf, 0x97, 0x73), new Color(0xbf, 0x97, 0x73),
            new Color(0xb3, 0x87, 0x67), new Color(0xab, 0x7b, 0x5b), new Color(0x9f, 0x6f, 0x4f),
            new Color(0x93, 0x63, 0x43), new Color(0x87, 0x57, 0x3b), new Color(0x7b, 0x4f, 0x33),
            new Color(0x73, 0x43, 0x2b), new Color(0x67, 0x3b, 0x23), new Color(0x5b, 0x33, 0x1b),
            new Color(0x4f, 0x2b, 0x17), new Color(0x43, 0x23, 0x13), new Color(0xff, 0xff, 0xff),
            new Color(0xff, 0xff, 0xff), new Color(0xef, 0xef, 0xef), new Color(0xef, 0xef, 0xef),
            new Color(0xdf, 0xdf, 0xdf), new Color(0xd3, 0xd3, 0xd3), new Color(0xc3, 0xc3, 0xc3),
            new Color(0xb3, 0xb3, 0xb3), new Color(0xa7, 0xa7, 0xa7), new Color(0x97, 0x97, 0x97),
            new Color(0x87, 0x87, 0x87), new Color(0x7b, 0x7b, 0x7b), new Color(0x6b, 0x6b, 0x6b),
            new Color(0x5b, 0x5b, 0x5b), new Color(0x4f, 0x4f, 0x4f), new Color(0x3f, 0x3f, 0x3f),
            new Color(0x2f, 0x2f, 0x2f), new Color(0x07, 0x07, 0xb3), new Color(0x07, 0x07, 0xb3),
            new Color(0x07, 0x07, 0xb3), new Color(0x07, 0x07, 0xb3), new Color(0x07, 0x07, 0xb3),
            new Color(0x07, 0x07, 0xb3), new Color(0x07, 0x07, 0xb3), new Color(0x07, 0x07, 0xb3),
            new Color(0x07, 0x07, 0xb3), new Color(0x07, 0x07, 0xb3), new Color(0x07, 0x07, 0xb3),
            new Color(0x07, 0x07, 0xb3), new Color(0x07, 0x07, 0xb3), new Color(0x07, 0x07, 0xb3),
            new Color(0x07, 0x07, 0xb3), new Color(0x07, 0x07, 0xb3), new Color(0x07, 0x07, 0xb3),
            new Color(0x0b, 0x0b, 0xb7), new Color(0x0b, 0x0b, 0xb7), new Color(0x0b, 0x0b, 0xb7),
            new Color(0x0b, 0x0b, 0xb7), new Color(0x0b, 0x0b, 0xb7), new Color(0x0b, 0x0b, 0xb7),
            new Color(0x0b, 0x0b, 0xb7), new Color(0x0b, 0x0b, 0xb7), new Color(0x0b, 0x0b, 0xb7),
            new Color(0x0b, 0x0b, 0xb7), new Color(0x0b, 0x0b, 0xb7), new Color(0x0b, 0x0b, 0xb7),
            new Color(0x0b, 0x0b, 0xb7), new Color(0x0b, 0x0b, 0xb7), new Color(0x0b, 0x0b, 0xb7),
            new Color(0x0b, 0x0b, 0xb7), new Color(0x0b, 0x0b, 0xb7), new Color(0x13, 0x13, 0xbb),
            new Color(0x13, 0x13, 0xbb), new Color(0x13, 0x13, 0xbb), new Color(0x13, 0x13, 0xbb),
            new Color(0x13, 0x13, 0xbb), new Color(0x13, 0x13, 0xbb), new Color(0x13, 0x13, 0xbb),
            new Color(0x13, 0x13, 0xbb), new Color(0x13, 0x13, 0xbb), new Color(0x13, 0x13, 0xbb),
            new Color(0x13, 0x13, 0xbb), new Color(0x13, 0x13, 0xbb), new Color(0x13, 0x13, 0xbb),
            new Color(0x13, 0x13, 0xbb), new Color(0x13, 0x13, 0xbb), new Color(0x13, 0x13, 0xbb),
            new Color(0x13, 0x13, 0xbb)
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

        private OwnershipMode ownershipMode = OwnershipMode.None;

        public MinimapGame(Interface interf, Game game)
            : base(interf, game.Map)
        {

        }

        protected override bool HandleDoubleClick(int x, int y, Event.Button button)
        {
            if (button == Event.Button.Left)
            {
                // close the minimap
                interf.ClosePopup();

                // jump to map position
                HandleClickLeft(x, y);
            }

            return true;
        }

        public void SetOwnershipMode(OwnershipMode mode)
        {
            if (ownershipMode == mode)
                return;

            ownershipMode = mode;

            UpdateMinimap(true);
        }

        public OwnershipMode GetOwnershipMode()
        {
            return ownershipMode;
        }

        public void SetDrawRoads(bool draw)
        {
            if (DrawRoads == draw)
                return;

            DrawRoads = draw;

            UpdateMinimap(true);
        }

        public void SetDrawBuildings(bool draw)
        {
            if (DrawBuildings == draw)
                return;

            DrawBuildings = draw;

            UpdateMinimap(true);
        }

        public void SetDrawGrid(bool draw)
        {
            if (DrawGrid == draw)
                return;

            DrawGrid = draw;

            UpdateMinimap(true);
        }

        public bool DrawRoads { get; private set; } = false;
        public bool DrawBuildings { get; private set; } = true;
        public bool DrawGrid { get; private set; } = false;

        protected override Color GetTileColor(MapPos position, int index)
        {
            var column = map.PositionColumn(position);
            var row = map.PositionRow(position);

            if (DrawGrid && (column == 0 || row == 0))
                return GridColor;

            if (DrawBuildings)
            {
                if (map.GetObject(position) > Map.Object.Flag && map.GetObject(position) <= Map.Object.Castle)
                {
                    if (ownershipMode == OwnershipMode.None)
                    {
                        var playerColor = interf.GetPlayerColor(map.GetOwner(position));

                        return new Color(playerColor.Red, playerColor.Green, playerColor.Blue);
                    }
                    else
                    {
                        return Color.White;
                    }
                }
            }

            if (DrawRoads)
            {
                if (map.Paths(position) > 0)
                {
                    return Color.Black;
                }
            }

            if (ownershipMode == OwnershipMode.Solid ||
                (ownershipMode == OwnershipMode.Mixed && (index % 2 == 0) == ((index / 128) % 2 == 0)))
            {
                if (!map.HasOwner(position))
                {
                    if (ownershipMode == OwnershipMode.Solid)
                        return Color.Black;
                }
                else
                {
                    var playerColor = interf.GetPlayerColor(map.GetOwner(position));

                    return new Color(playerColor.Red, playerColor.Green, playerColor.Blue);
                }
            }

            return base.GetTileColor(position, index);
        }
    }
}
