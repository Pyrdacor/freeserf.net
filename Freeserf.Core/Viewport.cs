/*
 * Viewport.cs - Viewport GUI component
 *
 * Copyright (C) 2013  Jon Lund Steffensen <jonlst@gmail.com>
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

using System;
using Freeserf.Render;

namespace Freeserf
{
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
        readonly IRenderLayer cursorLayer = null;
        readonly IRenderLayer buildsLayer = null;
        readonly ILayerSprite[,] builds = null;
        readonly ILayerSprite[] mapCursorSprites = new ILayerSprite[7];
        bool showPossibleBuilds = false;

        public Viewport(Interface interf, Map map)
            : base(interf)
        {
            this.interf = interf;
            this.map = map;

            cursorLayer = interf.RenderView.GetLayer(Freeserf.Layer.Cursor);
            buildsLayer = interf.RenderView.GetLayer(Freeserf.Layer.Builds);

            builds = new ILayerSprite[map.RenderMap.NumVisibleColumns, map.RenderMap.NumVisibleRows];

            if (buildSpriteInfos == null)
            {
                buildSpriteInfos = new SpriteInfo[20];
                var data = interf.RenderView.DataSource;

                for (uint i = 0; i < 20u; ++i)
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
            for (uint r = 0; r < map.RenderMap.NumVisibleRows; ++r)
            {
                for (uint c = 0; c < map.RenderMap.NumVisibleColumns; ++c)
                {
                    if (builds[c, r] != null)
                        builds[c, r].Delete();
                }
            }

            // destroy map cursor sprites
            for (int i = 0; i < 7; ++i)
            {
                if (mapCursorSprites[i] != null)
                    mapCursorSprites[i].Delete();
            }
        }

        /* Called periodically when the game progresses. */
        public void Update()
        {
            // update map zoom factor
            map.RenderMap.ZoomFactor = 1.0f + interf.RenderView.Zoom * 0.5f;

            // view redraw the viewport permanently
            SetRedraw();
        }

        public MapPos GetCurrentMapPos()
        {
            return 0;
        }

        public void MoveToMapPos(MapPos pos)
        {

        }

        public void RedrawMapPos(MapPos pos)
        {

        }

        public void SwitchLayer(Layer layer)
        {

        }

        protected override void InternalHide()
        {
            base.InternalHide();

            // TODO
        }

        protected override void InternalDraw()
        {
            DrawMapCursor();
        }


        #region Cursor and Builds

        void DrawMapCursor()
        {
            if (showPossibleBuilds)
                DrawMapCursorPossibleBuild();

            MapPos pos = interf.GetMapCursorPos();

            DrawMapCursorSprite(pos, 0, interf.GetMapCursorSprite(0));

            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction d in cycle)
            {
                DrawMapCursorSprite(map.Move(pos, d), 1 + (int)d, interf.GetMapCursorSprite(1 + (int)d));
            }
        }

        void DrawMapCursorSprite(MapPos pos, int index, uint spriteIndex)
        {
            var renderPos = map.RenderMap.GetScreenPosition(pos);
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
                var textureAtlas = Render.TextureAtlasManager.Instance.GetOrCreate(Freeserf.Layer.Builds);
                var offset = textureAtlas.GetOffset((uint)spriteIndex);
                var spriteInfo = buildSpriteInfos[spriteIndex - 31];

                if (builds[column, row] == null)
                {
                    builds[column, row] = interf.RenderView.SpriteFactory.Create((int)spriteInfo.Width, (int)spriteInfo.Height, offset.X, offset.Y, false, true) as ILayerSprite;
                    builds[column, row].Layer = buildsLayer;
                }
                else
                {
                    builds[column, row].Resize((int)spriteInfo.Width, (int)spriteInfo.Height);
                    builds[column, row].TextureAtlasOffset = offset;
                }

                var renderPos = map.RenderMap.GetScreenPosition(map.RenderMap.GetMapPosFromScreenPosition(column, row, false));

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

        void DrawMapCursorPossibleBuild()
        {
            Game game = interf.Game;

            for (uint r = 0; r < map.RenderMap.NumVisibleRows; ++r)
            {
                for (uint c = 0; c < map.RenderMap.NumVisibleColumns; ++c)
                {
                    var pos = map.RenderMap.GetMapPosFromScreenPosition(c, r);

                    /* Draw possible building */
                    int sprite = -1;

                    if (game.CanBuildCastle(pos, interf.GetPlayer()))
                    {
                        sprite = 49;
                    }
                    else if (game.CanPlayerBuild(pos, interf.GetPlayer()) &&
                             Map.MapSpaceFromObject[(int)map.GetObject(pos)] == Map.Space.Open &&
                             (game.CanBuildFlag(map.MoveDownRight(pos), interf.GetPlayer()) || map.HasFlag(map.MoveDownRight(pos))))
                    {
                        if (game.CanBuildMine(pos))
                        {
                            sprite = 47;
                        }
                        else if (game.CanBuildLarge(pos))
                        {
                            sprite = 49;
                        }
                        else if (game.CanBuildSmall(pos))
                        {
                            sprite = 48;
                        }
                    }

                    SetBuildSprite(c, r, sprite);
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
                    result = HandleDrag(e.Dx, e.Dy);
                    break;
                case Event.Type.DoubleClick:
                case Event.Type.SpecialClick:
                    result = HandleDoubleClick(e.X, e.Y, e.Button);
                    break;
                case Event.Type.KeyPressed:
                    result = HandleKeyPressed((char)e.Dx, e.Dy);
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

            var position = new Position(x, y);

            var mapPos = map.RenderMap.GetMapPosFromScreenPosition(position);

            if (interf.IsBuildingRoad())
            {
                int dx = map.DistX(interf.GetMapCursorPos(), mapPos) + 1;
                int dy = map.DistY(interf.GetMapCursorPos(), mapPos) + 1;
                Direction dir = Direction.None;

                if (dx == 0)
                {
                    if (dy == 1)
                    {
                        dir = Direction.Left;
                    }
                    else if (dy == 0)
                    {
                        dir = Direction.UpLeft;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (dx == 1)
                {
                    if (dy == 2)
                    {
                        dir = Direction.Down;
                    }
                    else if (dy == 0)
                    {
                        dir = Direction.Up;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (dx == 2)
                {
                    if (dy == 1)
                    {
                        dir = Direction.Right;
                    }
                    else if (dy == 2)
                    {
                        dir = Direction.DownRight;
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

                if (interf.BuildRoadIsValidDir(dir))
                {
                    Road road = interf.GetBuildingRoad();

                    if (road.IsUndo(dir))
                    {
                        /* Delete existing path */
                        int r = interf.RemoveRoadSegment();

                        if (r < 0)
                        {
                            PlaySound(Audio.TypeSfx.NotAccepted);
                        }
                        else
                        {
                            PlaySound(Audio.TypeSfx.Click);
                        }
                    }
                    else
                    {
                        /* Build new road segment */
                        int r = interf.BuildRoadSegment(dir);

                        if (r< 0)
                        {
                            PlaySound(Audio.TypeSfx.NotAccepted);
                        }
                        else if (r == 0)
                        {
                            PlaySound(Audio.TypeSfx.Click);
                        }
                        else
                        {
                            PlaySound(Audio.TypeSfx.Accepted);
                        }
                    }
                }
            }
            else
            {
                interf.UpdateMapCursorPos(mapPos);
                PlaySound(Audio.TypeSfx.Click);
            }

            return true;
        }

        protected override bool HandleDoubleClick(int x, int y, Event.Button button)
        {
            if (!interf.Ingame)
                return false;

            if (button != Event.Button.Left)
                return false;

            var position = new Position(x, y);

            var mapPos = map.RenderMap.GetMapPosFromScreenPosition(position);
            Player player = interf.GetPlayer();

            if (interf.IsBuildingRoad())
            {
                if (mapPos != interf.GetMapCursorPos())
                {
                    MapPos pos = interf.GetBuildingRoad().GetEnd(map);
                    Road road = Pathfinder.Map(map, pos, mapPos, interf.GetBuildingRoad());

                    if (road.Length != 0)
                    {
                        int r = interf.ExtendRoad(road);

                        if (r < 0)
                        {
                            PlaySound(Audio.TypeSfx.NotAccepted);
                        }
                        else if (r == 1)
                        {
                            PlaySound(Audio.TypeSfx.Accepted);
                        }
                        else
                        {
                            PlaySound(Audio.TypeSfx.Click);
                        }
                    }
                    else
                    {
                        PlaySound(Audio.TypeSfx.NotAccepted);
                    }
                }
                else
                {
                    bool r = interf.Game.BuildFlag(interf.GetMapCursorPos(), player);

                    if (r)
                    {
                        interf.BuildRoad();
                    }
                    else
                    {
                        PlaySound(Audio.TypeSfx.NotAccepted);
                    }
                }
            }
            else
            {
                if (map.GetObject(mapPos) == Map.Object.None ||
                    map.GetObject(mapPos) > Map.Object.Castle)
                {
                    return false;
                }

                if (map.GetObject(mapPos) == Map.Object.Flag)
                {
                    if (map.GetOwner(mapPos) == player.Index)
                    {
                        interf.OpenPopup(PopupBox.Type.TransportInfo);
                    }

                    player.tempIndex = map.GetObjectIndex(mapPos);
                }
                else
                { 
                    /* Building */
                    if (map.GetOwner(mapPos) == player.Index)
                    {
                        Building building = interf.Game.GetBuildingAtPos(mapPos);

                        if (!building.IsDone())
                        {
                            interf.OpenPopup(PopupBox.Type.OrderedBld);
                        }
                        else if (building.BuildingType == Building.Type.Castle)
                        {
                            interf.OpenPopup(PopupBox.Type.CastleRes);
                        }
                        else if (building.BuildingType == Building.Type.Stock)
                        {
                            if (!building.IsActive())
                                return false;

                            interf.OpenPopup(PopupBox.Type.CastleRes);
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
                            interf.OpenPopup(PopupBox.Type.BldStock);
                        }

                        player.tempIndex = map.GetObjectIndex(mapPos);
                    }
                    else
                    { 
                        /* Foreign building */
                        /* TODO handle coop mode*/
                        Building building = interf.Game.GetBuildingAtPos(mapPos);

                        player.buildingAttacked = (int)building.Index;

                        if (building.IsDone() &&
                            building.IsMilitary())
                        {
                            if (!building.IsActive() ||
                                building.GetThreatLevel() != 3)
                            {
                                /* It is not allowed to attack
                                   if currently not occupied or
                                   is too far from the border. */
                                PlaySound(Audio.TypeSfx.NotAccepted);
                                return false;
                            }

                            bool found = false;

                            for (int i = 257; i >= 0; --i)
                            {
                                MapPos pos = map.PosAddSpirally(building.Position, (uint)(7 + 257 - i));

                                if (map.HasOwner(pos) && map.GetOwner(pos) == player.Index)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                PlaySound(Audio.TypeSfx.NotAccepted);
                                return false;
                            }

                            /* Action accepted */
                            PlaySound(Audio.TypeSfx.Click);

                            int maxKnights = 0;

                            switch (building.BuildingType)
                            {
                                case Building.Type.Hut: maxKnights = 3; break;
                                case Building.Type.Tower: maxKnights = 6; break;
                                case Building.Type.Fortress: maxKnights = 12; break;
                                case Building.Type.Castle: maxKnights = 20; break;
                                default: Debug.NotReached(); break;
                            }

                            int knights = player.KnightsAvailableForAttack(building.Position);
                            player.knightsAttacking = Math.Min(knights, maxKnights);
                            interf.OpenPopup(PopupBox.Type.StartAttack);
                        }
                    }
                }
            }

            return false;
        }

        protected override bool HandleDrag(int dx, int dy)
        {
            if (!interf.Ingame)
                return false;

            totalDragX += dx;
            totalDragY += dy;

            int scrollX = totalDragX / Render.RenderMap.TILE_WIDTH;
            int scrollY = totalDragY / Render.RenderMap.TILE_HEIGHT;

            map.Scroll(scrollX, scrollY);

            totalDragX -= scrollX * Render.RenderMap.TILE_WIDTH;
            totalDragY -= scrollY * Render.RenderMap.TILE_HEIGHT;

            return true;
        }
    }
}
