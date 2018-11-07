using System;
using System.Collections.Generic;
using System.Text;
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
