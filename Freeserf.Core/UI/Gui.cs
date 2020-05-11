/*
 * Gui.cs - Base functions for the GUI hierarchy
 *
 * Copyright (C) 2012       Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Collections.Generic;

namespace Freeserf.UI
{
    using Data = Data.Data;

    internal abstract class GuiObject
    {
        public static Position GetTextureAtlasOffset(Data.Resource resourceType, uint spriteIndex)
        {
            var layer = Freeserf.Layer.Gui;

            if (resourceType == Data.Resource.MapObject)
                layer = Freeserf.Layer.GuiBuildings;
            else if (resourceType == Data.Resource.UIText)
                layer = Freeserf.Layer.GuiFont;

            var textureAtlasManager = Render.TextureAtlasManager.Instance;
            var textureAtlas = textureAtlasManager.GetOrCreate(layer);
            var offset = (resourceType == Data.Resource.MapObject) ?
                0 : textureAtlasManager.GetGuiTypeOffset(resourceType);

            return textureAtlas.GetOffset(offset + spriteIndex);
        }

        public static Render.ILayerSprite CreateSprite(Render.ISpriteFactory spriteFactory, int width, int height,
            Data.Resource resourceType, uint spriteIndex, byte displayLayer)
        {
            var offset = GetTextureAtlasOffset(resourceType, spriteIndex);

            return spriteFactory.Create(width, height, offset.X, offset.Y, false, true, displayLayer) as Render.ILayerSprite;
        }

        // sort so that the objects with heighest display layer come first
        class ChildComparer : IComparer<GuiObject>
        {
            public int Compare(GuiObject x, GuiObject y)
            {
                return y.BaseDisplayLayer.CompareTo(x.BaseDisplayLayer);
            }
        }

        readonly List<GuiObject> children = new List<GuiObject>();
        static readonly ChildComparer childComparer = new ChildComparer();
        bool redraw = true;
        protected internal Render.IRenderLayer Layer { get; private set; } = null;
        static GuiObject FocusedObject = null;
        protected bool focused = false;
        protected bool displayed = false;
        GuiObject parent = null;
        public Audio.Audio Audio { get; } = null;

        public int X { get; private set; } = 0;
        public int Y { get; private set; } = 0;
        public int Width { get; private set; } = 0;
        public int Height { get; private set; } = 0;
        public bool Enabled { get; set; } = true;
        public virtual bool Displayed
        {
            get => displayed;
            set
            {
                if (displayed != value)
                {
                    displayed = value;

                    if (!displayed)
                        InternalHide();

                    SetRedraw();
                }
            }
        }
        public virtual bool Visible
        {
            get
            {
                if (!Displayed)
                    return false;

                if (Parent == null)
                    return true;

                return Parent.Visible;
            }
        }
        public GuiObject Parent
        {
            get => parent;
            set
            {
                if (parent == value)
                    return;

                parent = value;

                UpdateParent();
                SetRedraw();
            }
        }
        // This is the base display layer from the gui hierarchy.
        // The distance is 10 per parent-child relation.
        // Inside the 10 layers the controls can be freely assigned.
        public virtual byte BaseDisplayLayer
        {
            get
            {
                if (Parent == null)
                    return 0;

                return (byte)Math.Min(255, Parent.BaseDisplayLayer + 10);
            }
        }
        public int TotalX
        {
            get
            {
                if (Parent == null)
                    return X;

                return Parent.TotalX + X;
            }
        }
        public int TotalY
        {
            get
            {
                if (Parent == null)
                    return Y;

                return Parent.TotalY + Y;
            }
        }

        protected GuiObject(Interface interf)
            : this(interf.RenderView, interf.AudioInterface)
        {

        }

        protected GuiObject(Render.IRenderView renderView, Audio.IAudioInterface audioInterface)
        {
            Layer = renderView.GetLayer(Freeserf.Layer.Gui);
            Audio = audioInterface.AudioFactory.GetAudio();
        }

        protected internal virtual void UpdateParent()
        {
            foreach (var child in children)
                child.UpdateParent();

            children.Sort(childComparer);
        }

        protected abstract void InternalDraw();
        protected virtual void InternalHide()
        {
            if (!Displayed)
            {
                foreach (var child in children)
                    child.Displayed = false;
            }
        }

        protected virtual void Layout()
        {
            // empty
        }

        protected virtual bool HandleClickLeft(int x, int y)
        {
            return false;
        }

        protected virtual bool HandleDoubleClick(int x, int y, Event.Button button)
        {
            return false;
        }

        protected virtual bool HandleDrag(int x, int y, int dx, int dy, Event.Button button)
        {
            return false;
        }

        protected virtual bool HandleKeyPressed(char key, int modifier)
        {
            return false;
        }

        protected virtual bool HandleSystemKeyPressed(Event.SystemKey key, int modifier)
        {
            return false;
        }

        protected virtual bool HandleFocusLoose()
        {
            return false;
        }

        public void Draw()
        {
            if (!Displayed)
            {
                return;
            }

            if (redraw)
            {
                InternalDraw();

                for (int i = children.Count - 1; i >= 0; --i)
                {
                    // The collection might change while drawing (e.g. through key events).
                    // In worst case we draw a child twice.
                    children[i].Draw();
                }

                redraw = false;
            }
        }

        public void MoveTo(int x, int y)
        {
            X = x;
            Y = y;

            SetRedraw();
        }

        public void SetSize(int width, int height)
        {
            Width = width;
            Height = height;
            Layout();
            SetRedraw();
        }

        public void SetRedraw()
        {
            redraw = true;

            if (Parent != null)
                Parent.SetRedraw();
        }

        public void AddChild(GuiObject obj, int x, int y, bool displayed = true)
        {
            obj.Parent = this;
            children.Add(obj);
            obj.MoveTo(x, y); // will call SetRedraw
            obj.Displayed = displayed;

            children.Sort(childComparer);
        }

        public void DeleteChild(GuiObject obj)
        {
            obj.Parent = null;
            children.Remove(obj);
            SetRedraw();
        }

        public void SetFocused()
        {
            if (FocusedObject != this)
            {
                if (FocusedObject != null)
                {
                    FocusedObject.focused = false;
                    FocusedObject.HandleFocusLoose();
                    FocusedObject.SetRedraw();
                }

                focused = true;
                FocusedObject = this;
                SetRedraw();
            }
        }

        public static void LooseFocus()
        {
            if (FocusedObject != null)
            {
                FocusedObject.focused = false;
                FocusedObject.HandleFocusLoose();
                FocusedObject.SetRedraw();
            }

            FocusedObject = null;
        }

        public void PlaySound(Audio.Audio.TypeSfx sound)
        {
            Audio?.GetSoundPlayer()?.PlayTrack((int)sound);
        }

        public static int GuiGetSliderClickValue(int x)
        {
            return 1310 * Misc.Clamp(0, x - 7, 50);
        }

        public virtual void HandleZoomChange()
        {
            foreach (var child in children)
            {
                child.HandleZoomChange();
            }
        }

        public virtual bool HandleEvent(Event.EventArgs e)
        {
            if (!Enabled || !Displayed)
            {
                return false;
            }

            int eventX = e.X;
            int eventY = e.Y;

            if (e.Type == Event.Type.Click ||
                e.Type == Event.Type.DoubleClick ||
                e.Type == Event.Type.SpecialClick ||
                e.Type == Event.Type.Drag)
            {
                int objectX = e.X - TotalX;
                int objectY = e.Y - TotalY;

                if (objectX < 0 || objectY < 0 || objectX > Width || objectY > Height)
                {
                    return false;
                }
            }

            // Find the corresponding child element if any
            foreach (var child in children)
            {
                if (child.HandleEvent(e))
                {
                    return true;
                }
            }

            bool result = false;

            switch (e.Type)
            {
                case Event.Type.Click:
                    if (e.Button == Event.Button.Left)
                        result = HandleClickLeft(eventX, eventY);
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

            if (result && FocusedObject != this)
            {
                if (FocusedObject != null)
                {
                    FocusedObject.focused = false;
                    FocusedObject.HandleFocusLoose();
                    FocusedObject.SetRedraw();
                    FocusedObject = null;
                }
            }

            return result;
        }
    }

    public class Gui
    {
        readonly Render.IRenderView renderView = null;
        Viewer viewer = null;

        public Gui(Render.IRenderView renderView, Audio.IAudioInterface audioInterface)
        {
            if (renderView.VirtualScreen.Size.Width > Global.MAX_VIRTUAL_SCREEN_WIDTH ||
                renderView.VirtualScreen.Size.Height > Global.MAX_VIRTUAL_SCREEN_HEIGHT)
                throw new ExceptionFreeserf(ErrorSystemType.Render, $"The virtual screen must not be larger than {Global.MAX_VIRTUAL_SCREEN_WIDTH}x{Global.MAX_VIRTUAL_SCREEN_HEIGHT}.");

            this.renderView = renderView;

            renderView.ZoomChanged += RenderView_ZoomChanged;

            // At the beginning we start with a local player.
            // Depending on the chosen game mode the viewer may be changed.
            SetViewer(Viewer.CreateLocalPlayer(renderView, audioInterface, null, this));

            renderView.Click += RenderView_Click;
            renderView.DoubleClick += RenderView_DoubleClick;
            renderView.SpecialClick += RenderView_DoubleClick;
            renderView.Drag += RenderView_Drag;
            renderView.KeyPress += RenderView_KeyPress;
            renderView.SystemKeyPress += RenderView_SystemKeyPress;
            renderView.StopDrag += RenderView_StopDrag;
        }

        void RenderView_ZoomChanged(object sender, EventArgs e)
        {
            viewer.MainInterface.HandleZoomChange();
        }

        internal void SetViewer(Viewer viewer)
        {
            if (this.viewer == viewer)
                return;

            if (this.viewer != null)
                this.viewer.Destroy();

            this.viewer = viewer;

            if (this.viewer != null)
                this.viewer.Init();
        }

        public bool Ingame => viewer.Ingame;

        public void DrawCursor(int x, int y)
        {
            viewer.DrawCursor(x, y);
        }

        Position PositionToGui(Position position)
        {
            float factorX = 640.0f / (float)renderView.VirtualScreen.Size.Width;
            float factorY = 480.0f / (float)renderView.VirtualScreen.Size.Height;

            return new Position((int)Math.Floor(position.X * factorX), (int)Math.Floor(position.Y * factorY));
        }

        public static Position PositionToGame(Position position, Render.IRenderView renderView)
        {
            float zoomFactor = 1.0f + renderView.Zoom * 0.5f;

            var size = DeltaToGame(renderView.VirtualScreen.Size, renderView);

            var x = Misc.Round((renderView.VirtualScreen.Size.Width - size.Width) * 0.5f + position.X / zoomFactor);
            var y = Misc.Round((renderView.VirtualScreen.Size.Height - size.Height) * 0.5f + position.Y / zoomFactor);

            return new Position(x, y);
        }

        Size DeltaToGui(Size delta)
        {
            float factorX = 640.0f / (float)renderView.VirtualScreen.Size.Width;
            float factorY = 480.0f / (float)renderView.VirtualScreen.Size.Height;

            return new Size(Misc.Round(delta.Width * factorX), Misc.Round(delta.Height * factorY));
        }

        public static Size DeltaToGame(Size delta, Render.IRenderView renderView)
        {
            float zoomFactor = 1.0f + renderView.Zoom * 0.5f;

            var width = Misc.Round(delta.Width / zoomFactor);
            var height = Misc.Round(delta.Height / zoomFactor);

            return new Size(width, height);
        }

        private bool RenderView_KeyPress(object sender, Event.EventArgs args)
        {
            return viewer.SendEvent(args);
        }

        private bool RenderView_SystemKeyPress(object sender, Event.EventArgs args)
        {
            return viewer.SendEvent(args);
        }

        private bool RenderView_Drag(object sender, Event.EventArgs args)
        {
            var position = PositionToGui(new Position(args.X, args.Y));
            var delta = DeltaToGui(new Size(args.Dx, args.Dy));

            args = Event.EventArgs.Transform(args, position.X, position.Y, delta.Width, delta.Height);

            return viewer.SendEvent(args);
        }

        private bool RenderView_StopDrag(object sender, Event.EventArgs args)
        {
            return viewer.SendEvent(args);
        }

        private bool RenderView_DoubleClick(object sender, Event.EventArgs args)
        {
            var position = PositionToGui(new Position(args.X, args.Y));

            args = Event.EventArgs.Transform(args, position.X, position.Y, args.Dy, args.Dy);

            return viewer.SendEvent(args);
        }

        private bool RenderView_Click(object sender, Event.EventArgs args)
        {
            var position = PositionToGui(new Position(args.X, args.Y));

            args = Event.EventArgs.Transform(args, position.X, position.Y, args.Dy, args.Dy);

            return viewer.SendEvent(args);
        }

        public void Draw()
        {
            viewer.Update();
            viewer.Draw();
        }
    }
}
