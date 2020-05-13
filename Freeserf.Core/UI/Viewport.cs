/*
 * Viewport.cs - Viewport GUI component
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

using Freeserf.Data;
using Freeserf.Render;
using System;

namespace Freeserf.UI
{
    using Data = Data.Data;
    using MapPos = UInt32;

    // The viewport controls the visible part of the map.
    // It transforms and forwards mouse clicks to the game.
    internal class Viewport : GuiObject
    {
        static SpriteInfo[] buildSpriteInfos = null;

        readonly Interface interf = null;
        readonly Map map = null;
        int totalDragX = 0;
        int totalDragY = 0;
        readonly IRenderLayer buildsLayer = null;
        readonly ILayerSprite[,] builds = null;
        readonly ILayerSprite[] mapCursorSprites = new ILayerSprite[7];

        public bool ShowPossibleBuilds { get; set; } = false;

        public Viewport(Interface interf, Map map)
            : base(interf)
        {
            this.interf = interf;
            this.map = map;

            buildsLayer = interf.RenderView.GetLayer(Freeserf.Layer.Builds);

            builds = new ILayerSprite[map.RenderMap.NumVisibleColumns, map.RenderMap.NumVisibleRows];

            if (buildSpriteInfos == null)
            {
                buildSpriteInfos = new SpriteInfo[21];
                var data = interf.RenderView.DataSource;

                for (uint i = 0; i < 21u; ++i)
                {
                    buildSpriteInfos[i] = data.GetSpriteInfo(Data.Resource.GameObject, 31u + i);
                }
            }

            // create map cursor sprites
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Freeserf.Layer.Builds);
            var offset = textureAtlas.GetOffset(31u);
            mapCursorSprites[0] = interf.RenderView.SpriteFactory.Create(16, 9, offset.X, offset.Y, false, true) as ILayerSprite;
            mapCursorSprites[0].Layer = buildsLayer;
            mapCursorSprites[0].Visible = true;
            offset = textureAtlas.GetOffset(32u);

            for (int i = 1; i < 7; ++i)
            {
                mapCursorSprites[i] = interf.RenderView.SpriteFactory.Create(5, 5, offset.X, offset.Y, false, true) as ILayerSprite;
                mapCursorSprites[i].Layer = buildsLayer;
                mapCursorSprites[i].Visible = true;
            }
        }

        public void CleanUp()
        {
            // destroy build sprites
            for (uint row = 0; row < map.RenderMap.NumVisibleRows; ++row)
            {
                for (uint column = 0; column < map.RenderMap.NumVisibleColumns; ++column)
                {
                    builds[column, row]?.Delete();
                    builds[column, row] = null;
                }
            }

            // destroy map cursor sprites
            for (int i = 0; i < 7; ++i)
            {
                mapCursorSprites[i]?.Delete();
                mapCursorSprites[i] = null;
            }
        }

        // Called periodically when the game progresses. 
        public void Update()
        {
            // update map zoom factor
            if (map?.RenderMap != null && interf?.RenderView != null)
                map.RenderMap.ZoomFactor = 1.0f + interf.RenderView.Zoom * 0.5f;

            // view redraw the viewport permanently
            SetRedraw();
        }

        public MapPos GetCurrentMapPosition()
        {
            return map.RenderMap.GetMapOffset();
        }

        public void MoveToMapPosition(MapPos position, bool center)
        {
            if (center)
                map.RenderMap.CenterMapPosition(position);
            else
                map.RenderMap.ScrollToMapPosition(position);

            interf.UpdateMinimap();
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            // TODO
        }

        protected override void InternalDraw()
        {
            if (map.RenderMap == null) // no more viewport rendering
                return;

            DrawMapCursor();
        }


        #region Cursor and Builds

        void DrawMapCursor()
        {
            if (ShowPossibleBuilds)
                DrawMapCursorPossibleBuild();
            else
                ClearMapCursorPossibleBuild();

            var position = interf.GetMapCursorPosition();

            DrawMapCursorSprite(position, 0, interf.GetMapCursorSprite(0));

            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                DrawMapCursorSprite(map.Move(position, direction), 1 + (int)direction, interf.GetMapCursorSprite(1 + (int)direction));
            }
        }

        void DrawMapCursorSprite(MapPos position, int index, uint spriteIndex)
        {
            var renderPos = map.RenderMap.CoordinateSpace.TileSpaceToViewSpace(position);
            var spriteInfo = buildSpriteInfos[spriteIndex - 31u];
            var textureAtlas = Render.TextureAtlasManager.Instance.GetOrCreate(Freeserf.Layer.Builds);

            mapCursorSprites[index].Resize((int)spriteInfo.Width, (int)spriteInfo.Height);
            mapCursorSprites[index].X = TotalX + renderPos.X + spriteInfo.OffsetX;
            mapCursorSprites[index].Y = TotalY + renderPos.Y + spriteInfo.OffsetY;
            mapCursorSprites[index].TextureAtlasOffset = textureAtlas.GetOffset(spriteIndex);
        }

        void SetBuildSprite(uint column, uint row, int spriteIndex)
        {
            if (spriteIndex >= 0)
            {
                var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Freeserf.Layer.Builds);
                var offset = textureAtlas.GetOffset((uint)spriteIndex);
                var spriteInfo = buildSpriteInfos[spriteIndex - 31];

                if (builds[column, row] == null)
                {
                    builds[column, row] = interf.RenderView.SpriteFactory.Create((int)spriteInfo.Width, (int)spriteInfo.Height, offset.X, offset.Y, false, true) as ILayerSprite;
                    builds[column, row].Layer = buildsLayer;
                }
                else
                {
                    builds[column, row].Resize(spriteInfo.Width, spriteInfo.Height);
                    builds[column, row].TextureAtlasOffset = offset;
                }

                var renderPos = map.RenderMap.CoordinateSpace.TileSpaceToViewSpace(map.RenderMap.CoordinateSpace.ViewSpaceToTileSpace(column, row));

                builds[column, row].X = TotalX + renderPos.X + spriteInfo.OffsetX;
                builds[column, row].Y = TotalY + renderPos.Y + spriteInfo.OffsetY;
                builds[column, row].Visible = true;
            }
            else
            {
                if (builds[column, row] != null)
                    builds[column, row].Visible = false;
            }
        }

        void ClearMapCursorPossibleBuild()
        {
            for (uint row = 0; row < map.RenderMap.NumVisibleRows; ++row)
            {
                for (uint column = 0; column < map.RenderMap.NumVisibleColumns; ++column)
                {
                    SetBuildSprite(column, row, -1);
                }
            }
        }

        void DrawMapCursorPossibleBuild()
        {
            var game = interf.Game;

            for (uint row = 0; row < map.RenderMap.NumVisibleRows; ++row)
            {
                for (uint column = 0; column < map.RenderMap.NumVisibleColumns; ++column)
                {
                    var position = map.RenderMap.CoordinateSpace.ViewSpaceToTileSpace(column, row);

                    // Draw possible building 
                    int sprite = -1;

                    if (game.CanBuildCastle(position, interf.Player))
                    {
                        sprite = 49;
                    }
                    else if (game.CanPlayerBuild(position, interf.Player) &&
                             Map.MapSpaceFromObject[(int)map.GetObject(position)] == Map.Space.Open &&
                             (game.CanBuildFlag(map.MoveDownRight(position), interf.Player) || map.HasFlag(map.MoveDownRight(position))))
                    {
                        if (game.CanBuildMine(position))
                        {
                            sprite = 47;
                        }
                        else if (game.CanBuildLarge(position))
                        {
                            sprite = 49;
                        }
                        else if (game.CanBuildSmall(position))
                        {
                            sprite = 48;
                        }
                    }

                    SetBuildSprite(column, row, sprite);
                }
            }
        }

        #endregion


        public override bool HandleEvent(Event.EventArgs e)
        {
            if (!Enabled || !Displayed)
            {
                return false;
            }

            bool result = false;

            switch (e.Type)
            {
                case Event.Type.Click:
                    if (e.Button == Event.Button.Left)
                        result = HandleClickLeft(e.X, e.Y);
                    break;
                case Event.Type.Drag:
                    result = HandleDrag(e.X, e.Y, e.Dx, e.Dy, e.Button);
                    break;
                case Event.Type.DoubleClick:
                case Event.Type.SpecialClick:
                    result = HandleDoubleClick(e.X, e.Y, e.Button);
                    break;
                case Event.Type.KeyPressed:
                    result = HandleKeyPressed((char)e.Dx, e.Dy);
                    break;
                case Event.Type.SystemKeyPressed:
                    result = HandleSystemKeyPressed((Event.SystemKey)e.Dx, e.Dy);
                    break;
                default:
                    break;
            }

            return result;
        }

        protected override bool HandleClickLeft(int x, int y)
        {
            if (!interf.Ingame)
                return false;

            // if clicked into the viewport, close notifications and other popups
            interf.CloseMessage();
            interf.ClosePopup();

            var position = new Position(x, y);
            var mapPosition = map.RenderMap.CoordinateSpace.ViewSpaceToTileSpace(position);

            if (interf.IsBuildingRoad)
            {
                int distanceX = map.DistanceX(interf.GetMapCursorPosition(), mapPosition) + 1;
                int distanceY = map.DistanceY(interf.GetMapCursorPosition(), mapPosition) + 1;
                Direction direction;

                if (distanceX == 0)
                {
                    if (distanceY == 1)
                    {
                        direction = Direction.Left;
                    }
                    else if (distanceY == 0)
                    {
                        direction = Direction.UpLeft;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (distanceX == 1)
                {
                    if (distanceY == 2)
                    {
                        direction = Direction.Down;
                    }
                    else if (distanceY == 0)
                    {
                        direction = Direction.Up;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (distanceX == 2)
                {
                    if (distanceY == 1)
                    {
                        direction = Direction.Right;
                    }
                    else if (distanceY == 2)
                    {
                        direction = Direction.DownRight;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                if (interf.BuildRoadIsValidDirection(direction))
                {
                    var road = interf.GetBuildingRoad();

                    if (road.IsUndo(direction))
                    {
                        // Delete existing path 
                        int result = interf.RemoveRoadSegment();

                        if (result < 0)
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                        }
                        else
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);
                        }

                        if (!interf.GetBuildingRoad().Valid)
                            interf.BuildRoadBegin();
                    }
                    else
                    {
                        // Build new road segment 
                        int result = interf.BuildRoadSegment(direction, false);

                        if (result < 0)
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                        }
                        else if (result == 0)
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);
                        }
                        else
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
                        }
                    }
                }
            }
            else
            {
                // Fast building
                if (interf.AccessRights == Viewer.Access.Player &&
                    interf.GetOption(Option.FastBuilding) &&
                    interf.GetMapCursorPosition() == mapPosition)
                {
                    if (!interf.Player.HasCastle)
                    {
                        if (interf.Game.CanBuildCastle(mapPosition, interf.Player))
                            interf.BuildCastle();
                    }
                    else if (interf.Game.Map.HasFlag(mapPosition))
                        interf.BuildRoadBegin();
                    else if (interf.Game.CanBuildAnything(mapPosition, interf.Player))
                    {
                        if (!interf.Game.CanBuildSmall(mapPosition)) // only flags are possible
                            interf.BuildFlag();
                        else
                            interf.OpenPopup(interf.Game.CanBuildLarge(mapPosition) ? PopupBox.Type.BasicBldFlip : PopupBox.Type.BasicBld);
                    }
                }

                interf.UpdateMapCursorPosition(mapPosition);
                PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);
            }

            return true;
        }

        protected override bool HandleDoubleClick(int x, int y, Event.Button button)
        {
            if (!interf.Ingame)
                return false;

            if (button == Event.Button.Right && interf.GetOption(Option.FastMapClick))
            {
                // Perform fast map click
                interf.PanelBar.ToggleMiniMap();
                return true;
            }

            if (button != Event.Button.Left)
                return false;

            // if clicked into the viewport, close notifications and other popups
            interf.CloseMessage();
            interf.ClosePopup();

            var position = new Position(x, y);
            var mapPosition = map.RenderMap.CoordinateSpace.ViewSpaceToTileSpace(position);
            var player = interf.Player;

            if (interf.IsBuildingRoad)
            {
                if (mapPosition != interf.GetMapCursorPosition())
                {
                    var roadEndPosition = interf.GetBuildingRoad().EndPosition;
                    var road = Pathfinder.FindShortestPath(map, roadEndPosition, mapPosition, interf.GetBuildingRoad(), int.MaxValue, true);

                    if (road.Length != 0)
                    {
                        int result = interf.ExtendRoad(road);

                        if (result < 0)
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                        }
                        else if (result == 1)
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
                        }
                        else
                        {
                            if (interf.Game.BuildFlag(interf.GetMapCursorPosition(), player))
                            {
                                interf.BuildRoad();
                                PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
                            }
                            else
                            {
                                PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);
                            }
                        }
                    }
                    else
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                }
                else
                {
                    bool result = interf.Game.BuildFlag(interf.GetMapCursorPosition(), player);

                    if (result)
                    {
                        interf.BuildRoad();
                    }
                    else
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                    }
                }
            }
            else
            {
                interf.UpdateMapCursorPosition(mapPosition);

                if (map.GetObject(mapPosition) == Map.Object.None ||
                    map.GetObject(mapPosition) > Map.Object.Castle)
                {
                    PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);
                    return false;
                }

                if (map.GetObject(mapPosition) == Map.Object.Flag)
                {
                    if (map.GetOwner(mapPosition) == player.Index)
                    {
                        interf.OpenPopup(PopupBox.Type.TransportInfo);
                    }

                    player.SelectedObjectIndex = map.GetObjectIndex(mapPosition);
                    PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);
                }
                else
                {
                    // Building 
                    if (map.GetOwner(mapPosition) == player.Index || interf.AccessRights != Viewer.Access.Player)
                    {
                        PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);

                        var building = interf.Game.GetBuildingAtPosition(mapPosition);

                        if (building.BuildingType == Building.Type.Castle)
                        {
                            interf.OpenPopup(PopupBox.Type.CastleResources);
                        }
                        else if (!building.IsDone)
                        {
                            interf.OpenPopup(PopupBox.Type.OrderedBld);
                        }
                        else if (building.BuildingType == Building.Type.Stock)
                        {
                            if (!building.IsActive)
                                return false;

                            interf.OpenPopup(PopupBox.Type.CastleResources);
                        }
                        else if (building.BuildingType == Building.Type.Hut ||
                                 building.BuildingType == Building.Type.Tower ||
                                 building.BuildingType == Building.Type.Fortress)
                        {
                            interf.OpenPopup(PopupBox.Type.Defenders);
                        }
                        else if (building.BuildingType == Building.Type.StoneMine ||
                                 building.BuildingType == Building.Type.CoalMine ||
                                 building.BuildingType == Building.Type.IronMine ||
                                 building.BuildingType == Building.Type.GoldMine)
                        {
                            interf.OpenPopup(PopupBox.Type.MineOutput);
                        }
                        else
                        {
                            interf.OpenPopup(PopupBox.Type.BuildingStock);
                        }

                        player.SelectedObjectIndex = map.GetObjectIndex(mapPosition);
                    }
                    else
                    {
                        // Foreign building 
                        // TODO handle coop mode
                        if (player.PrepareAttack(mapPosition))
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
                            interf.OpenPopup(PopupBox.Type.StartAttack);
                        }
                        else
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                        }
                    }
                }
            }

            return false;
        }

        protected override bool HandleDrag(int x, int y, int dx, int dy, Event.Button button)
        {
            if (!interf.Ingame)
                return false;

            if (button != Event.Button.Right)
                return false;

            totalDragX += dx;
            totalDragY += dy;

            int scrollX = totalDragX / RenderMap.TILE_WIDTH;
            int scrollY = totalDragY / RenderMap.TILE_HEIGHT;

            if (interf.GetOption(Option.InvertScrolling)) // invert scrolling
                map.Scroll(-scrollX, -scrollY);
            else
                map.Scroll(scrollX, scrollY);

            interf.UpdateMinimap();

            totalDragX -= scrollX * RenderMap.TILE_WIDTH;
            totalDragY -= scrollY * RenderMap.TILE_HEIGHT;

            return true;
        }
    }
}
