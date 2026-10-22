/*
 * SplitScreenRenderView.cs - Render view of one local player in split-screen mode
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

using Freeserf.Event;
using Freeserf.Render;
using Freeserf.Renderer;
using System;

namespace Freeserf
{
    using EventHandler = Freeserf.Event.EventHandler;
    using Texture = Freeserf.Renderer.Texture;
    // The render view of one local player in split-screen mode. It provides access to
    // the player's layers and his part of the virtual screen. The input events are
    // handled by the shared game view and routed to the players by the split-screen gui.
    class SplitScreenRenderView : IRenderView
    {
        readonly GameView gameView = null;
        readonly int playerIndex = 0;
        readonly Rect viewRect = null; // the player's part of the virtual screen

        public SplitScreenRenderView(GameView gameView, int playerIndex, Rect viewRect)
        {
            this.gameView = gameView;
            this.playerIndex = playerIndex;
            this.viewRect = viewRect;
        }

        public Rect VirtualScreen => new Rect(0, 0, viewRect.Size.Width, viewRect.Size.Height);

        public IRenderLayer GetLayer(Layer layer)
        {
            return gameView.GetLayer(playerIndex, layer);
        }

        public void AddLayer(IRenderLayer layer)
        {
            gameView.AddLayer(playerIndex, layer);
        }

        public IRenderLayer Create(Layer layer, Freeserf.Render.Texture texture, bool supportColoredRects = false, Freeserf.Render.Color colorKey = null, Freeserf.Render.Color colorOverlay = null)
        {
            return gameView.Create(layer, texture, supportColoredRects, colorKey, colorOverlay);
        }

        public void Render()
        {
            // The shared game view renders all layers of all players.
        }

        public void Resize(int width, int height)
        {
            gameView.Resize(width, height);
        }

        public void Close()
        {
            gameView.Close();
        }

        public void SetCursorPosition(int x, int y)
        {
            gameView.SetCursorPosition(x, y);
        }

        public Position ScreenToView(Position position)
        {
            var viewPosition = gameView.ScreenToView(position);

            return new Position(viewPosition.X - viewRect.Left, viewPosition.Y - viewRect.Top);
        }

        public Size ScreenToView(Size size)
        {
            return gameView.ScreenToView(size);
        }

        public Rect ScreenToView(Rect rect)
        {
            var viewRect = gameView.ScreenToView(rect);

            if (viewRect == null)
                return null;

            return new Rect(viewRect.Position.X - this.viewRect.Left, viewRect.Position.Y - this.viewRect.Top,
                viewRect.Size.Width, viewRect.Size.Height);
        }

        public float Zoom
        {
            get => gameView.GetPlayerZoom(playerIndex);
            set => gameView.SetPlayerZoom(playerIndex, value);
        }

        public void ResetZoom()
        {
            gameView.SetPlayerZoom(playerIndex, 0.0f);
        }

        public bool Fullscreen
        {
            get => gameView.Fullscreen;
            set => gameView.Fullscreen = value;
        }

        public event System.EventHandler ZoomChanged
        {
            add => gameView.ZoomChanged += value;
            remove => gameView.ZoomChanged -= value;
        }

        public ISpriteFactory SpriteFactory => gameView.SpriteFactory;

        public ITriangleFactory TriangleFactory => gameView.TriangleFactory;

        public IColoredRectFactory ColoredRectFactory => gameView.ColoredRectFactory;

        public IMinimapTextureFactory MinimapTextureFactory => gameView.MinimapTextureFactory;

        public Data.DataSource DataSource => gameView.DataSource;

        // The input events are fired by the shared game view and routed to the
        // players by the split-screen gui. So they are not fired here.
        public event EventHandler Click;
        public event EventHandler DoubleClick;
        public event EventHandler SpecialClick;
        public event EventHandler Drag;
        public event EventHandler KeyPress;
        public event EventHandler SystemKeyPress;
        public event EventHandler StopDrag;
        public event EventHandler Wheel;
    }
}
