/*
 * IRenderView.cs - Render view interface
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
    public interface IRenderView : IRenderLayerFactory, Event.IEventHandlers
    {
        void Render();
        void AddLayer(IRenderLayer layer);
        IRenderLayer GetLayer(Layer layer);
        void Resize(int width, int height);
        void Close();

        void SetCursorPosition(int x, int y);

        Position ScreenToView(Position position);
        Size ScreenToView(Size size);
        Rect ScreenToView(Rect rect);

        Rect VirtualScreen { get; }
        float Zoom { get; }
        void ResetZoom();
        bool Fullscreen { get; set; }

        ISpriteFactory SpriteFactory { get; }
        ITriangleFactory TriangleFactory { get; }
        IColoredRectFactory ColoredRectFactory { get; }
        IMinimapTextureFactory MinimapTextureFactory { get; }
        IAudioFactory AudioFactory { get; }

        Data.DataSource DataSource { get; }
    }
}
