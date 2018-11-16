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
        int x = short.MaxValue;
        int y = short.MaxValue;
        float scaleX = 1.0f;
        float scaleY = 1.0f;
        bool visible = false;
        IRenderLayer layer = null;
        bool visibleRequest = false;
        bool deleted = false;
        bool notOnScreen = true;
        readonly Rect virtualScreen = null;

        protected Node(Shape shape, int width, int height, Rect virtualScreen)
        {
            Shape = shape;
            Width = width;
            Height = height;
            this.virtualScreen = virtualScreen;
        }

        public Shape Shape { get; } = Shape.Rect;

        public bool Visible
        {
            get => visible && !deleted && !notOnScreen;
            set
            {
                if (deleted)
                    return;

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
                
                if (Visible)
                    AddToLayer();
                else if (!visible)
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

                if (layer != null && Visible)
                    RemoveFromLayer();

                layer = value;

                if (layer != null && visibleRequest && !deleted)
                {
                    visible = true;
                    visibleRequest = false;
                    CheckOnScreen();
                }

                if (layer == null)
                {
                    visibleRequest = false;
                    visible = false;
                    notOnScreen = true;
                }

                if (layer != null && Visible)
                    AddToLayer();
            }
        }

        public int Width { get; private set; }

        public int Height { get; private set; }

        internal Rect ScaledRect
        {
            get
            {
                float scaledWidth = scaleX * Width;
                float scaledHeight = scaleY * Height;
                int xSub = Misc.Round(0.5f * (scaledWidth - Width));
                int ySub = Misc.Round(0.5f * (scaledHeight - Height));

                return new Rect(X - xSub, Y - ySub, Misc.Round(scaledWidth), Misc.Round(scaledHeight));
            }
        }

        public virtual void Resize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        protected abstract void AddToLayer();

        protected abstract void RemoveFromLayer();

        protected abstract void UpdatePosition();

        bool CheckOnScreen()
        {
            bool oldNotOnScreen = notOnScreen;
            bool oldVisible = Visible;

            notOnScreen = !virtualScreen.IntersectsWith(ScaledRect);

            if (oldNotOnScreen != notOnScreen)
            {
                if (oldVisible != Visible)
                {
                    if (Visible)
                        AddToLayer();
                    else
                        RemoveFromLayer();

                    return true; // handled
                }
            }

            return false;
        }

        public void Delete()
        {
            RemoveFromLayer();
            deleted = true;
            visible = false;
            visibleRequest = false;            
        }

        public int X
        {
            get => x;
            set
            {
                if (x == value)
                    return;

                x = value;

                if (!deleted)
                {
                    if (!CheckOnScreen())
                        UpdatePosition();
                }
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

                if (!deleted)
                {
                    if (!CheckOnScreen())
                        UpdatePosition();
                }
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

                if (!CheckOnScreen())
                    UpdatePosition();
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

                if (!CheckOnScreen())
                    UpdatePosition();
            }
        }
    }
}
