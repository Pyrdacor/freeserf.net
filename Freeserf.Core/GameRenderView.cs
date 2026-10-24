/*
 * GameRenderView.cs - Render data of one local player's view of the game
 *
 * Copyright (C) 2025  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using Freeserf.Audio;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf
{
    using MapPos = uint;

    // In split-screen mode each local player has his own view of the game: his own
    // render map (with its own scroll position) and his own set of render objects
    // (sprites) which are positioned based on that render map. This class holds all
    // render data of one such view. In single player mode there is exactly one view.
    internal class GameRenderView
    {
        readonly Game game = null;
        readonly Render.IRenderView renderView = null;
        readonly IAudioInterface audioInterface = null;
        readonly int playerIndex = 0;

        readonly ConcurrentDictionary<Serf, Render.RenderSerf> renderSerfs = new ConcurrentDictionary<Serf, Render.RenderSerf>();
        readonly ConcurrentDictionary<Building, Render.RenderBuilding> renderBuildings = new ConcurrentDictionary<Building, Render.RenderBuilding>();
        readonly ConcurrentDictionary<Flag, Render.RenderFlag> renderFlags = new ConcurrentDictionary<Flag, Render.RenderFlag>();
        readonly ConcurrentDictionary<MapPos, Render.RenderMapObject> renderObjects = new ConcurrentDictionary<MapPos, Render.RenderMapObject>();
        readonly ConcurrentDictionary<long, Render.RenderRoadSegment> renderRoadSegments = new ConcurrentDictionary<long, Render.RenderRoadSegment>();
        readonly ConcurrentDictionary<long, Render.RenderBorderSegment> renderBorderSegments = new ConcurrentDictionary<long, Render.RenderBorderSegment>();
        readonly List<Render.RenderBuilding> renderBuildingsInProgress = new List<Render.RenderBuilding>();
        // The render positions of roads, borders and map objects only change if the map is scrolled
        // (or the height of a map position changes, see OnHeightChanged).
        Render.RenderMap lastRenderMap = null;
        uint lastScrollX = 0;
        uint lastScrollY = 0;
        bool mapScrolled = true;
        // Map objects (trees, stones, ...) only get render objects inside the visible area of the map.
        // Large maps have hundreds of thousands of them which would slow down the game a lot.
        HashSet<MapPos> renderObjectArea = new HashSet<MapPos>();
        HashSet<MapPos> nextRenderObjectArea = new HashSet<MapPos>();
        // Reused for the updates to avoid allocations every tick.
        readonly List<Flag> flagUpdateList = new List<Flag>();
        readonly List<Building> buildingUpdateList = new List<Building>();
        readonly List<Serf> serfUpdateList = new List<Serf>();

        public Render.RenderMap RenderMap => game.Map?.GetRenderMap(playerIndex);

        public GameRenderView(Game game, Render.IRenderView renderView, IAudioInterface audioInterface, int playerIndex)
        {
            this.game = game;
            this.renderView = renderView;
            this.audioInterface = audioInterface;
            this.playerIndex = playerIndex;
        }

        public void Clear()
        {
            // delete all render objects

            foreach (var building in renderBuildings)
                building.Value.Delete();

            foreach (var serf in renderSerfs)
                serf.Value.Delete();

            foreach (var flag in renderFlags)
                flag.Value.Delete();

            foreach (var mapObject in renderObjects)
                mapObject.Value.Delete();

            renderObjectArea.Clear();
            lastRenderMap = null; // the render object area is set up again with the next update

            foreach (var roadSegment in renderRoadSegments)
                roadSegment.Value.Delete();

            foreach (var borderSegment in renderBorderSegments)
                borderSegment.Value.Delete();

            renderBuildings.Clear();
            renderBuildingsInProgress.Clear();
            renderSerfs.Clear();
            renderFlags.Clear();
            renderObjects.Clear();
            renderRoadSegments.Clear();
            renderBorderSegments.Clear();
        }

        // Updates the parts of the visuals which depend on the scroll position
        // (waves, roads, borders and map objects). The game logic updates of the
        // flags, buildings and serfs are done by the game itself (once per tick).
        public void UpdateVisuals(ushort tick)
        {
            if (RenderMap != null)
                RenderMap.UpdateWaves(tick);

            CheckMapScrolled();

            UpdateRoads();
            UpdateBorders();
            UpdateMapObjects();
        }

        void CheckMapScrolled()
        {
            var renderMap = RenderMap;

            if (renderMap == null)
            {
                lastRenderMap = null;
                mapScrolled = false;
                return;
            }

            mapScrolled = renderMap != lastRenderMap || renderMap.ScrollX != lastScrollX || renderMap.ScrollY != lastScrollY;
            lastRenderMap = renderMap;
            lastScrollX = renderMap.ScrollX;
            lastScrollY = renderMap.ScrollY;
        }

        void UpdateMapObjects()
        {
            if (mapScrolled)
                UpdateRenderObjectArea();

            // Note: Enumerating a concurrent dictionary is safe without copying it.
            foreach (var renderObject in renderObjects)
                renderObject.Value.Update(game.Tick, RenderMap, renderObject.Key, mapScrolled);
        }

        // Rows and columns around the visible map area in which map objects get render objects.
        // Sprites reach up to the tile above and high positions are drawn up to 7 rows higher.
        const int RenderObjectAreaMarginTop = 2;
        const int RenderObjectAreaMarginBottom = 8;
        const int RenderObjectAreaMarginX = 2;

        void UpdateRenderObjectArea()
        {
            var renderMap = RenderMap;
            int scrollX = (int)renderMap.ScrollX;
            int scrollY = (int)renderMap.ScrollY;
            int columns = (int)renderMap.NumVisibleColumns + RenderObjectAreaMarginX;
            int rows = (int)renderMap.NumVisibleRows + RenderObjectAreaMarginBottom;

            nextRenderObjectArea.Clear();

            for (int r = -RenderObjectAreaMarginTop; r < rows; ++r)
            {
                int row = scrollY + r;
                // The same as RenderMap.GetMapOffset: the first column moves right every second row.
                int firstColumn = scrollX + (int)System.Math.Floor(row / 2.0);

                for (int c = -RenderObjectAreaMarginX; c < columns; ++c)
                    nextRenderObjectArea.Add(game.Map.Position((uint)(firstColumn + c) & game.Map.ColumnMask, (uint)row & game.Map.RowMask));
            }

            foreach (var position in renderObjectArea)
            {
                if (!nextRenderObjectArea.Contains(position) && renderObjects.TryRemove(position, out var renderObject) && renderObject != null)
                    renderObject.Delete();
            }

            foreach (var position in nextRenderObjectArea)
            {
                if (!renderObjectArea.Contains(position))
                    AddRenderMapObject(position);
            }

            (renderObjectArea, nextRenderObjectArea) = (nextRenderObjectArea, renderObjectArea);
        }

        void AddRenderMapObject(MapPos position)
        {
            var obj = game.Map.GetObject(position);

            if (obj == Map.Object.None || obj == Map.Object.Flag || obj == Map.Object.SmallBuilding ||
                obj == Map.Object.LargeBuilding || obj == Map.Object.Castle || renderObjects.ContainsKey(position))
                return;

            var renderObject = new Render.RenderMapObject(obj, renderView.GetLayer(Layer.Objects), renderView.SpriteFactory, renderView.DataSource);

            renderObject.Visible = true;

            renderObjects.TryAdd(position, renderObject);
        }

        public void UpdateFlagRenders(ushort tick)
        {
            // flag.Update() may change the collection, so we use a copied list here.
            flagUpdateList.Clear();
            flagUpdateList.AddRange(game.Flags);

            foreach (var flag in flagUpdateList)
            {
                if (flag.Index > 0u && renderFlags.TryGetValue(flag, out var renderFlag))
                    renderFlag?.Update(tick, RenderMap, flag.Position);
            }

            flagUpdateList.Clear();
        }

        void UpdateRoads()
        {
            // New road segments are positioned when they are added.
            if (!mapScrolled)
                return;

            foreach (var renderRoadSegment in renderRoadSegments)
                renderRoadSegment.Value?.Update(RenderMap);
        }

        public void UpdateBorders()
        {
            // New border segments are positioned when they are added.
            if (!mapScrolled)
                return;

            foreach (var renderBorderSegment in renderBorderSegments)
                renderBorderSegment.Value?.Update(RenderMap);
        }

        public void UpdateBuildingRenders(ushort tick)
        {
            // Note: Do not use foreach here as building.Update()
            // may delete the building and therefore change the
            // collection while we iterate through it!

            // Therefore we use a copied list here.
            var buildingList = buildingUpdateList;
            buildingList.Clear();
            buildingList.AddRange(game.Buildings);

            for (int i = 0; i < buildingList.Count; ++i)
            {
                if (buildingList[i].Index > 0u)
                {
                    if (renderBuildings.TryGetValue(buildingList[i], out var renderBuilding))
                    {
                        // if a building burns we have to update its rendering so ensure that is is updated when visible
                        if (buildingList[i].IsBurning)
                        {
                            if (renderBuilding.Visible)
                            {
                                if (!renderBuildingsInProgress.Contains(renderBuilding))
                                    renderBuildingsInProgress.Add(renderBuilding);
                            }
                        }

                        renderBuilding.Update(tick, RenderMap, buildingList[i].Position);
                    }
                }
            }

            buildingList.Clear();

            for (int i = renderBuildingsInProgress.Count - 1; i >= 0; --i)
            {
                if (!renderBuildingsInProgress[i].UpdateProgress()) // no more updating needed
                    renderBuildingsInProgress.RemoveAt(i);
            }
        }

        public void UpdateSerfRenders(ushort tick)
        {
            // Note: Do not use foreach here as serf.Update()
            // may delete the serf and therefore change the
            // collection while we iterate through it!

            // Therefore we use a copied list here.
            var serfList = serfUpdateList;
            serfList.Clear();
            serfList.AddRange(game.Serfs);

            for (int i = 0; i < serfList.Count; ++i)
            {
                if (serfList[i].Index > 0u && renderSerfs.TryGetValue(serfList[i], out var renderSerf))
                {
                    renderSerf.Update(game, renderView.DataSource, tick, game.Map, serfList[i].Position, RenderMap);
                }
            }

            serfList.Clear();
        }

        public void UpdateBorders(MapPos position)
        {
            var cycle = new DirectionCycleCW(Direction.Right, 3);

            foreach (var direction in cycle)
            {
                long index = Render.RenderBorderSegment.CreateIndex(position, direction);

                if (game.Map.HasOwner(position) != game.Map.HasOwner(game.Map.Move(position, direction)) ||
                    game.Map.GetOwner(position) != game.Map.GetOwner(game.Map.Move(position, direction)))
                {
                    if (!renderBorderSegments.ContainsKey(index))
                    {
                        var renderBorderSegment = new Render.RenderBorderSegment(game.Map, position, direction,
                            renderView.GetLayer(Layer.Objects), renderView.SpriteFactory, renderView.DataSource);

                        if (!renderBorderSegments.TryAdd(index, renderBorderSegment))
                        {
                            throw new ExceptionFreeserf(ErrorSystemType.Application, "Unable to add render border segment.");
                        }

                        if (RenderMap != null)
                            renderBorderSegment.Update(RenderMap);
                    }
                }
                else
                {
                    if (renderBorderSegments.TryRemove(index, out var renderBorderSegment) && renderBorderSegment != null)
                        renderBorderSegment.Delete();
                }
            }
        }

        public void DeleteBuilding(Building building)
        {
            if (renderBuildings.TryRemove(building, out var renderBuilding) && renderBuilding != null)
            {
                renderBuildingsInProgress.Remove(renderBuilding);
                renderBuilding.Delete();
            }
        }

        public void DeleteFlag(Flag flag)
        {
            if (renderFlags.TryRemove(flag, out var renderFlag) && renderFlag != null)
                renderFlag.Delete();
        }

        public void OnObjectExchanged(MapPos position, Map.Object oldObject, Map.Object newObject)
        {
            if (oldObject != Map.Object.None && newObject < Map.Object.Tree0)
            {
                // we don't draw buildings etc with renderObjects
                if (renderObjects.TryRemove(position, out var renderObject) && renderObject != null)
                    renderObject.Delete();
            }
            else if (renderObjects.ContainsKey(position))
            {
                renderObjects[position].ChangeObjectType(newObject);
            }
        }

        public void OnObjectPlaced(MapPos position)
        {
            var obj = game.Map.GetObject(position);

            // rendering
            if (obj == Map.Object.Flag)
            {
                var flag = game.GetFlagAtPosition(position);

                if (!renderFlags.ContainsKey(flag))
                {
                    var renderFlag = new Render.RenderFlag(flag, renderView.GetLayer(Layer.Objects), renderView.SpriteFactory, renderView.DataSource);

                    renderFlag.Visible = true;

                    renderFlags.TryAdd(flag, renderFlag);
                }
            }
            else if (obj == Map.Object.SmallBuilding ||
                     obj == Map.Object.LargeBuilding ||
                     obj == Map.Object.Castle)
            {
                var building = game.GetBuildingAtPosition(position);

                if (!renderBuildings.ContainsKey(building))
                {
                    var renderBuilding = new Render.RenderBuilding(building, renderView.GetLayer(Layer.Buildings),
                        renderView.GetLayer(Layer.Objects), renderView.SpriteFactory, renderView.DataSource, audioInterface.AudioFactory.GetAudio());

                    renderBuilding.Visible = true;

                    if (!renderBuildings.TryAdd(building, renderBuilding))
                        throw new ExceptionFreeserf(ErrorSystemType.Application, "Unable to add render building.");

                    if (!building.IsDone || building.IsBurning)
                        renderBuildingsInProgress.Add(renderBuilding);
                }
            }
            else if (renderObjectArea.Contains(position)) // map object (only visible ones are drawn)
            {
                AddRenderMapObject(position);
            }
        }

        public void AddSerfForDrawing(Serf serf, MapPos position)
        {
            if (renderSerfs.ContainsKey(serf))
                return;

            var renderSerf = new Render.RenderSerf(serf, renderView.GetLayer(Layer.Serfs), renderView.SpriteFactory,
                renderView.DataSource, audioInterface.AudioFactory.GetAudio());

            renderSerf.Visible = true;

            if (!renderSerfs.TryAdd(serf, renderSerf))
                throw new ExceptionFreeserf(ErrorSystemType.Application, "Unable to add render serf.");
        }

        // Adds the render objects of all serfs which are visible on the map
        // (e.g. after a full game state sync which cleared all visuals).
        public void AddAllSerfsForDrawing()
        {
            foreach (var serf in game.Serfs.ToList())
            {
                if (serf.Index == 0)
                    continue;

                switch (serf.SerfState)
                {
                    case Serf.State.BuildingCastle:
                    case Serf.State.IdleInStock:
                    case Serf.State.DefendingCastle:
                    case Serf.State.DefendingFortress:
                    case Serf.State.DefendingHut:
                    case Serf.State.DefendingTower:
                    case Serf.State.Invalid:
                    case Serf.State.Null:
                    case Serf.State.WaitForResourceOut:
                        break;
                    default:
                        AddSerfForDrawing(serf, serf.Position);
                        break;
                }
            }
        }

        public void RemoveSerfFromDrawing(Serf serf)
        {
            if (renderSerfs.TryRemove(serf, out var renderSerf) && renderSerf != null)
                renderSerf.Delete();
        }

        public void AddRoadSegment(MapPos position, Direction direction)
        {
            if (direction < Direction.Right || direction > Direction.Down)
                return;

            long index = Render.RenderRoadSegment.CreateIndex(position, direction);

            var renderRoadSegment = new Render.RenderRoadSegment(game.Map, position, direction, renderView.GetLayer(Layer.Paths),
                renderView.SpriteFactory, renderView.DataSource);

            renderRoadSegment.Visible = true;

            if (!renderRoadSegments.TryAdd(index, renderRoadSegment))
                Log.Error.Write(ErrorSystemType.Game, $"Failed to add road segment at position {position} direction {direction}.");
            else if (RenderMap != null)
                renderRoadSegment.Update(RenderMap);
        }

        public void RemoveRoadSegment(MapPos position, Direction direction)
        {
            if (direction < Direction.Right)
                return;

            if (direction > Direction.Down)
            {
                RemoveRoadSegment(game.Map.Move(position, direction), direction.Reverse());
                return;
            }

            long index = Render.RenderRoadSegment.CreateIndex(position, direction);

            if (renderRoadSegments.TryRemove(index, out var renderRoadSegment) && renderRoadSegment != null)
                renderRoadSegment.Delete();
            else
                Log.Error.Write(ErrorSystemType.Game, $"Failed to remove road segment at position {position} direction {direction}.");
        }

        public void OnHeightChanged(MapPos position)
        {
            // Update road segments
            for (int i = 0; i < 7; ++i)
            {
                var checkPosition = game.Map.PositionAddSpirally(position, (uint)i);
                var cycle = new DirectionCycleCW(Direction.Right, 3u);

                foreach (var direction in cycle)
                {
                    if (game.Map.HasPath(checkPosition, direction))
                    {
                        long index = Render.RenderRoadSegment.CreateIndex(checkPosition, direction);

                        if (renderRoadSegments.TryGetValue(index, out var renderRoadSegment) && renderRoadSegment != null)
                        {
                            renderRoadSegment.UpdateAppearance();

                            if (RenderMap != null)
                                renderRoadSegment.Update(RenderMap);
                        }
                    }

                    // The render positions depend on the height.
                    if (RenderMap != null &&
                        renderBorderSegments.TryGetValue(Render.RenderBorderSegment.CreateIndex(checkPosition, direction), out var renderBorderSegment) &&
                        renderBorderSegment != null)
                    {
                        renderBorderSegment.Update(RenderMap);
                    }
                }

                if (RenderMap != null && renderObjects.TryGetValue(checkPosition, out var renderObject) && renderObject != null)
                    renderObject.Update(game.Tick, RenderMap, checkPosition, true);
            }
        }

        // This is called after loading a game.
        // As no roads are built manually in this case
        // we have to scan the map and add render objects
        // for all road segments.
        public void PostLoadRoads()
        {
            foreach (var position in game.Map.Geometry)
            {
                if (game.Map.Paths(position) != 0)
                {
                    var cycle = new DirectionCycleCW(Direction.Right, 3);

                    foreach (var direction in cycle)
                    {
                        if (game.Map.HasPath(position, direction))
                            AddRoadSegment(position, direction);
                    }
                }
            }
        }
    }
}
