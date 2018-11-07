/*
 * Node.cs - Basic render node implementation
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

using System;
using Freeserf.Render;

namespace Freeserf.Renderer.OpenTK
{
    public abstract class Node : IRenderNode
    {
        int x = 0;
        int y = 0;
        float scaleX = 1.0f;
        float scaleY = 1.0f;
        bool visible = false;
        IRenderLayer layer = null;
        bool visibleRequest = false;

        protected Node(Shape shape, int width, int height)
        {
            Shape = shape;
            Width = width;
            Height = height;
        }

        public Shape Shape { get; } = Shape.Rect;

        public bool Visible
        {
            get => visible;
            set
            {
                if (layer == null)
                {
                    visibleRequest = value;
                    visible = false;
                    return;
                }

                visibleRequest = false;

                if (visible == value)
                    return;

                visible = value;
                
                if (visible)
                    AddToLayer();
                else
                    RemoveFromLayer();
            }
        }

        public IRenderLayer Layer
        {
            get => layer;
            set
            {
                if (value != null && !(value is RenderLayer))
                    throw new InvalidCastException("The given layer is not valid for this renderer.");

                if (layer == value)
                    return;

                if (layer != null && visible)
                    RemoveFromLayer();

                layer = value;

                if (layer != null && visibleRequest)
                {
                    visible = true;
                    visibleRequest = false;
                }

                if (layer == null)
                {
                    visibleRequest = false;
                    visible = false;
                }

                if (layer != null && visible)
                    AddToLayer();
            }
        }

        public int Width { get; }

        public int Height { get; }

        protected virtual void AddToLayer()
        {
            layer.AddNode(this);
        }

        protected virtual void RemoveFromLayer()
        {
            layer.RemoveNode(this);
        }

        protected abstract void UpdatePosition();

        public int X
        {
            get => x;
            set
            {
                if (x == value)
                    return;

                x = value;
                UpdatePosition();
            }
        }

        public int Y
        {
            get => y;
            set
            {
                if (y == value)
                    return;

                y = value;
                UpdatePosition();
            }
        }

        public float ScaleX
        {
            get => scaleX;
            set
            {
                if (Misc.FloatEqual(scaleX, value))
                    return;

                scaleX = value;
                // TODO
            }
        }

        public float ScaleY
        {
            get => scaleY;
            set
            {
                if (Misc.FloatEqual(scaleY, value))
                    return;

                scaleY = value;
                // TODO
            }
        }
    }
}
