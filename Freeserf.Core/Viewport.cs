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

namespace Freeserf
{
    using MapPos = UInt32;

    // The viewport controls the visible part of the map.
    // It transforms and forwards mouse clicks to the game.
    internal class Viewport : GuiObject
    {
        Interface interf = null;
        Map map = null;
        int totalDragX = 0;
        int totalDragY = 0;

        public Viewport(Interface interf, Map map)
            : base(interf)
        {
            this.interf = interf;
            this.map = map;
        }

        public void Update()
        {

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
            // TODO
        }

        protected internal override void UpdateParent()
        {
            // TODO
        }

        protected override bool HandleClickLeft(int x, int y)
        {
            if (!interf.Ingame)
                return false;

            // the game may be zoomed so transform gui position to game position
            var position = Gui.PositionGuiToGame(new Position(x, y), interf.RenderView);

            var mapPos = map.RenderMap.GetMapPosFromMousePosition(position);

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
                    else if (dy == 0) {
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

                        if (r< 0)
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

            // the game may be zoomed so transform gui position to game position
            var position = Gui.PositionGuiToGame(new Position(x, y), interf.RenderView);

            var mapPos = map.RenderMap.GetMapPosFromMousePosition(position);
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

            // the game may be zoomed so transform gui delta to game delta
            var delta = Gui.DeltaGuiToGame(new Size(dx, dy), interf.RenderView);

            totalDragX += delta.Width;
            totalDragY += delta.Height;

            int scrollX = totalDragX / Render.RenderMap.TILE_WIDTH;
            int scrollY = totalDragY / Render.RenderMap.TILE_HEIGHT;

            map.Scroll(scrollX, scrollY);

            totalDragX -= scrollX * Render.RenderMap.TILE_WIDTH;
            totalDragY -= scrollY * Render.RenderMap.TILE_HEIGHT;

            return true;
        }
    }
}
