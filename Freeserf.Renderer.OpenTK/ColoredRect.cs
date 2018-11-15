/*
 * ColoredRect.cs - Colored rectangle
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

using Freeserf.Render;

namespace Freeserf.Renderer.OpenTK
{
    public class ColoredRect : Node, IColoredRect
    {
        protected int drawIndex = -1;

        public ColoredRect(int width, int height, Color color, Rect virtualScreen)
            : base(Shape.Rect, width, height, virtualScreen)
        {
            Color = color;
        }

        public Color Color
        {
            get;
        }

        protected override void AddToLayer()
        {
            base.AddToLayer();

            drawIndex = (Layer as RenderLayer).GetColoredRectDrawIndex(this);
        }

        protected override void RemoveFromLayer()
        {
            base.RemoveFromLayer();

            (Layer as RenderLayer).FreeColoredRectDrawIndex(drawIndex);
            drawIndex = -1;
        }

        protected override void UpdatePosition()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateColoredRectPosition(drawIndex, this);
        }
    }

    public class ColoredRectFactory : IColoredRectFactory
    {
        readonly Rect virtualScreen = null;

        public ColoredRectFactory(Rect virtualScreen)
        {
            this.virtualScreen = virtualScreen;
        }

        public IColoredRect Create(int width, int height, Color color)
        {
            return new ColoredRect(width, height, color, virtualScreen);
        }
    }
}
