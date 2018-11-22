/*
 * Gui.cs - Base functions for the GUI hierarchy
 *
 * Copyright (C) 2012  Jon Lund Steffensen <jonlst@gmail.com>
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
using System.Collections.Generic;

namespace Freeserf
{
    internal abstract class GuiObject
    {
        public static Position GetTextureAtlasOffset(Data.Resource resourceType, uint spriteIndex)
        {
            var layer = (resourceType == Data.Resource.MapObject) ?
                Freeserf.Layer.GuiBuildings : Freeserf.Layer.Gui;

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

        readonly List<GuiObject> children = new List<GuiObject>();
        bool redraw = true;
        protected internal Render.IRenderLayer Layer { get; private set; } = null;
        static GuiObject FocusedObject = null;
        protected bool focused = false;
        protected bool displayed = false;
        GuiObject parent = null;

        public int X { get; private set; } = 0;
        public int Y { get; private set; } = 0;
        public int Width { get; private set; } = 0;
        public int Height { get; private set; } = 0;
        public bool Enabled { get; set; } = true;
        public bool Displayed
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
        public byte BaseDisplayLayer
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
            : this(interf.RenderView)
        {

        }

        protected GuiObject(Render.IRenderView renderView)
        {
            Layer = renderView.GetLayer(Freeserf.Layer.Gui);
        }

        protected GuiObject(Render.IRenderLayer renderLayer)
        {
            Layer = renderLayer;
        }

        protected internal virtual void UpdateParent()
        {
            foreach (var child in children)
                child.UpdateParent();
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

        protected virtual bool HandleDrag(int dx, int dy)
        {
            return false;
        }

        protected virtual bool HandleKeyPressed(char key, int modifier)
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

                foreach (var child in children)
                {
                    child.Draw();
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

        public void PlaySound(Audio.TypeSfx sound)
        {
            // TODO
            return;

            Audio audio = Audio.Instance;
            Audio.Player player = audio.GetSoundPlayer();

            if (player != null)
            {
                player.PlayTrack((int)sound);
            }
        }

        public static int GuiGetSliderClickValue(int x)
        {
            return 1310 * Misc.Clamp(0, x - 7, 50);
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

            /* Find the corresponding child element if any */
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
        readonly Interface interf = null;

        public Gui(Render.IRenderView renderView)
        {
            this.renderView = renderView;
            interf = new Interface(renderView);

            renderView.Click += RenderView_Click;
            renderView.DoubleClick += RenderView_DoubleClick;
            renderView.SpecialClick += RenderView_DoubleClick;
            renderView.Drag += RenderView_Drag;
            renderView.KeyPress += RenderView_KeyPress;
        }

        public bool Ingame => interf.Ingame;

        Position PositionToGui(Position position)
        {
            float factorX = 640.0f / (float)renderView.VirtualScreen.Size.Width;
            float factorY = 480.0f / (float)renderView.VirtualScreen.Size.Height;

            return new Position((int)Math.Floor(position.X * factorX), (int)Math.Floor(position.Y * factorY));
        }

        public static Position PositionToGame(Position position, Render.IRenderView renderView)
        {
            float zoomFactor = 1.0f + renderView.Zoom * 0.5f;

            position.X = Misc.Round(zoomFactor * position.X);
            position.Y = Misc.Round(zoomFactor * position.Y);

            return position;
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

            delta.Width = Misc.Round(delta.Width / zoomFactor);
            delta.Height = Misc.Round(delta.Height / zoomFactor);

            return delta;
        }

        private bool RenderView_KeyPress(object sender, Event.EventArgs args)
        {
            return HandleEvent(args);
        }

        private bool RenderView_Drag(object sender, Event.EventArgs args)
        {
            var position = PositionToGui(new Position(args.X, args.Y));
            var delta = DeltaToGui(new Size(args.Dx, args.Dy));

            args = Event.EventArgs.Transform(args, position.X, position.Y, delta.Width, delta.Height);

            return HandleEvent(args);
        }

        private bool RenderView_DoubleClick(object sender, Event.EventArgs args)
        {
            var position = PositionToGui(new Position(args.X, args.Y));

            args = Event.EventArgs.Transform(args, position.X, position.Y, args.Dy, args.Dy);

            return HandleEvent(args);
        }

        private bool RenderView_Click(object sender, Event.EventArgs args)
        {
            var position = PositionToGui(new Position(args.X, args.Y));

            args = Event.EventArgs.Transform(args, position.X, position.Y, args.Dy, args.Dy);

            return HandleEvent(args);
        }

        public void Draw()
        {
            interf.Update();
            interf.Draw();
        }

        bool HandleEvent(Event.EventArgs args)
        {
            if (!args.Done)
                args.Done = interf.HandleEvent(args);

            return args.Done;
        }
    }

    internal class Icon : GuiObject
    {
        Render.ILayerSprite sprite = null;
        readonly byte displayLayerOffset = 0;
        readonly Data.Resource resourceType = Data.Resource.None;

        public uint SpriteIndex { get; private set; } = 0u;
        public object Tag { get; set; }

        // only used by BuildingIcon
        protected Icon(Interface interf, int width, int height, uint spriteIndex, byte displayLayerOffset)
            : base(interf)
        {
            sprite = CreateSprite(interf.RenderView.SpriteFactory, width, height, Data.Resource.MapObject, spriteIndex, (byte)(BaseDisplayLayer + displayLayerOffset));
            this.displayLayerOffset = displayLayerOffset;
            resourceType = Data.Resource.MapObject;
            sprite.Layer = interf.RenderView.GetLayer(Freeserf.Layer.GuiBuildings);
            SpriteIndex = spriteIndex;

            SetSize(width, height);
        }

        public Icon(Interface interf, int width, int height, Data.Resource resourceType, uint spriteIndex, byte displayLayerOffset)
            : base(interf)
        {
            sprite = CreateSprite(interf.RenderView.SpriteFactory, width, height, resourceType, spriteIndex, (byte)(BaseDisplayLayer + displayLayerOffset));
            this.displayLayerOffset = displayLayerOffset;
            this.resourceType = resourceType;
            sprite.Layer = Layer;
            SpriteIndex = spriteIndex;

            SetSize(width, height);
        }

        protected override void InternalDraw()
        {
            sprite.X = TotalX;
            sprite.Y = TotalY;

            sprite.Visible = Displayed;
        }

        protected internal override void UpdateParent()
        {
            sprite.DisplayLayer = (byte)(BaseDisplayLayer + displayLayerOffset);
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            sprite.Visible = false;
        }

        public void SetSpriteIndex(uint spriteIndex)
        {
            sprite.TextureAtlasOffset = GetTextureAtlasOffset(resourceType, spriteIndex);
            SpriteIndex = spriteIndex;
        }

        public void Resize(int width, int height)
        {
            sprite.Resize(width, height);
            SetSize(width, height);
        }
    }

    internal class Button : Icon
    {
        public class ClickEventArgs : EventArgs
        {
            public int X { get; }
            public int Y { get; }

            public ClickEventArgs(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        public delegate void ClickEventHandler(object sender, ClickEventArgs args);

        public event ClickEventHandler Clicked;

        public Button(Interface interf, int width, int height, Data.Resource resourceType, uint spriteIndex, byte displayLayerOffset)
            : base(interf, width, height, resourceType, spriteIndex, displayLayerOffset)
        {

        }

        protected override bool HandleClickLeft(int x, int y)
        {
            Clicked?.Invoke(this, new ClickEventArgs(x - TotalX, y - TotalY));

            return true;
        }
    }

    internal class BuildingIcon : Icon
    {
        public BuildingIcon(Interface interf, int width, int height, uint spriteIndex, byte displayLayerOffset)
            : base(interf, width, height, spriteIndex, displayLayerOffset)
        {

        }
    }

    internal class BuildingButton : BuildingIcon
    {
        public delegate void ClickEventHandler(object sender, Button.ClickEventArgs args);

        public event ClickEventHandler Clicked;

        public BuildingButton(Interface interf, int width, int height, uint spriteIndex, byte displayLayerOffset)
            : base(interf, width, height, spriteIndex, displayLayerOffset)
        {

        }

        protected override bool HandleClickLeft(int x, int y)
        {
            Clicked?.Invoke(this, new Button.ClickEventArgs(x - TotalX, y - TotalY));

            return true;
        }
    }

    internal class TextField
    {
        readonly Render.TextRenderer textRenderer;
        int index = -1;
        string text = "";
        byte displayLayer = 0;
        bool useSpecialDigits = false;

        public int X { get; private set; } = 0;
        public int Y { get; private set; } = 0;

        public byte DisplayLayer
        {
            get => displayLayer;
            set
            {
                if (displayLayer == value)
                    return;

                displayLayer = value;

                if (index != -1)
                    textRenderer.ChangeDisplayLayer(index, displayLayer);
            }
        }

        public TextField(Render.TextRenderer textRenderer, bool useSpecialDigits = false)
        {
            this.textRenderer = textRenderer;
            this.useSpecialDigits = useSpecialDigits;
        }

        public void Destroy()
        {
            if (index != -1)
                textRenderer.DestroyText(index);

            text = "";
            index = -1;
        }

        public string Text
        {
            get => text;
            set
            {
                if (text == value)
                    return;

                text = value;

                if (index == -1)
                    index = textRenderer.CreateText(text, DisplayLayer, useSpecialDigits, new Position(X, Y));
                else
                    textRenderer.ChangeText(index, text, DisplayLayer);
            }
        }

        public bool Visible
        {
            get
            {
                if (index == -1)
                    return false;

                return textRenderer.IsVisible(index);
            }
            set
            {
                if (Visible == value)
                    return;

                if (index == -1 && !value)
                    return;

                if (index == -1 && value)
                    index = textRenderer.CreateText(text, DisplayLayer, useSpecialDigits, new Position(X, Y));

                textRenderer.ShowText(index, value);
            }
        }

        public void SetPosition(int x, int y)
        {
            if (index != -1)
                textRenderer.SetPosition(index, new Position(x, y));

            X = x;
            Y = y;
        }
    }
}
