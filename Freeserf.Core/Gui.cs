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
using System.Linq;
using Freeserf.Event;

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
        public Audio Audio { get; } = null;

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
            : this(interf.RenderView)
        {

        }

        protected GuiObject(Render.IRenderView renderView)
        {
            Layer = renderView.GetLayer(Freeserf.Layer.Gui);
            Audio = renderView.AudioFactory.GetAudio();
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

        public void PlaySound(Audio.TypeSfx sound)
        {
            Audio.Player player = Audio?.GetSoundPlayer();

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
        Viewer viewer = null;

        public Gui(Render.IRenderView renderView)
        {
            this.renderView = renderView;

            // At the beginning we start with a local player.
            // Depending on the chosen game mode the viewer may be changed.
            SetViewer(Viewer.CreateLocalPlayer(renderView, null, this));

            renderView.Click += RenderView_Click;
            renderView.DoubleClick += RenderView_DoubleClick;
            renderView.SpecialClick += RenderView_DoubleClick;
            renderView.Drag += RenderView_Drag;
            renderView.KeyPress += RenderView_KeyPress;
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

        private bool RenderView_Drag(object sender, Event.EventArgs args)
        {
            var position = PositionToGui(new Position(args.X, args.Y));
            var delta = DeltaToGui(new Size(args.Dx, args.Dy));

            args = Event.EventArgs.Transform(args, position.X, position.Y, delta.Width, delta.Height);

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

    internal class Icon : GuiObject
    {
        Render.ILayerSprite sprite = null;
        byte displayLayerOffset = 0;

        public uint SpriteIndex { get; private set; } = 0u;
        public Data.Resource ResourceType { get; private set; } = Data.Resource.None;
        public object Tag { get; set; }

        // only used by BuildingIcon
        protected Icon(Interface interf, int width, int height, uint spriteIndex, byte displayLayerOffset)
            : base(interf)
        {
            sprite = CreateSprite(interf.RenderView.SpriteFactory, width, height, Data.Resource.MapObject, spriteIndex, (byte)(BaseDisplayLayer + displayLayerOffset));
            this.displayLayerOffset = displayLayerOffset;
            ResourceType = Data.Resource.MapObject;
            sprite.Layer = interf.RenderView.GetLayer(Freeserf.Layer.GuiBuildings);
            SpriteIndex = spriteIndex;

            SetSize(width, height);
        }

        public Icon(Interface interf, int width, int height, Data.Resource resourceType, uint spriteIndex, byte displayLayerOffset)
            : base(interf)
        {
            sprite = CreateSprite(interf.RenderView.SpriteFactory, width, height, resourceType, spriteIndex, (byte)(BaseDisplayLayer + displayLayerOffset));
            this.displayLayerOffset = displayLayerOffset;
            this.ResourceType = resourceType;
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

        public void SetDisplayLayerOffset(byte offset)
        {
            if (displayLayerOffset == offset)
                return;

            displayLayerOffset = offset;

            sprite.DisplayLayer = (byte)(BaseDisplayLayer + displayLayerOffset);
        }

        public void SetResourceType(Data.Resource type)
        {
            ResourceType = type;
            sprite.TextureAtlasOffset = GetTextureAtlasOffset(ResourceType, SpriteIndex);
        }

        public void SetSpriteIndex(uint spriteIndex)
        {
            sprite.TextureAtlasOffset = GetTextureAtlasOffset(ResourceType, spriteIndex);
            SpriteIndex = spriteIndex;
        }

        public void SetSpriteIndex(Data.Resource type, uint spriteIndex)
        {
            ResourceType = type;
            SetSpriteIndex(spriteIndex);
        }

        public void Resize(int width, int height)
        {
            sprite.Resize(width, height);
            SetSize(width, height);
        }
    }

    internal class Button : Icon
    {
        public class ClickEventArgs : System.EventArgs
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
        public event ClickEventHandler DoubleClicked;

        public Button(Interface interf, int width, int height, Data.Resource resourceType, uint spriteIndex, byte displayLayerOffset)
            : base(interf, width, height, resourceType, spriteIndex, displayLayerOffset)
        {

        }

        protected override bool HandleClickLeft(int x, int y)
        {
            Clicked?.Invoke(this, new ClickEventArgs(x - TotalX, y - TotalY));

            return true;
        }

        protected override bool HandleDoubleClick(int x, int y, Event.Button button)
        {
            DoubleClicked?.Invoke(this, new ClickEventArgs(x - TotalX, y - TotalY));

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

    internal class TextField : GuiObject
    {
        readonly Render.TextRenderer textRenderer;
        int index = -1;
        string text = "";
        byte displayLayerOffset = 0;
        bool useSpecialDigits = false;
        int characterGapSize = 8;

        public TextField(Interface interf, byte displayLayerOffset, int characterGapSize = 8, bool useSpecialDigits = false)
            : base(interf)
        {
            textRenderer = interf.TextRenderer;
            this.useSpecialDigits = useSpecialDigits;
            this.displayLayerOffset = displayLayerOffset;
            this.characterGapSize = characterGapSize;
        }

        public void Destroy()
        {
            base.Displayed = false;

            if (index != -1)
                textRenderer.DestroyText(index);

            text = "";
            index = -1;

            if (Parent != null)
                Parent.DeleteChild(this);
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
                    index = textRenderer.CreateText(text, (byte)(BaseDisplayLayer + displayLayerOffset + 1), useSpecialDigits, new Position(TotalX, TotalY), characterGapSize);
                else
                    textRenderer.ChangeText(index, text, (byte)(BaseDisplayLayer + displayLayerOffset + 1), characterGapSize);

                if (text.Length == 0)
                    SetSize(0, 0);
                else if (text.Length == 1)
                    SetSize(8, 8);
                else
                    SetSize(8 + (text.Length - 1) * characterGapSize, 8);
            }
        }

        public override bool Displayed
        {
            get
            {
                if (Parent != null && !Parent.Displayed)
                    return false;

                if (index == -1)
                    return false;

                return textRenderer.IsVisible(index);
            }
            set
            {

                if (base.Displayed == value)
                    return;

                if (index == -1 && !value)
                {
                    base.Displayed = false;
                    return;
                }

                if (index == -1 && value)
                    index = textRenderer.CreateText(text, (byte)(BaseDisplayLayer + displayLayerOffset + 1), useSpecialDigits, new Position(TotalX, TotalY), characterGapSize);

                textRenderer.ShowText(index, value);

                base.Displayed = value;
            }
        }

        protected override void InternalDraw()
        {
            if (index != -1)
                textRenderer.SetPosition(index, new Position(TotalX, TotalY), characterGapSize);
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            if (index != -1)
                textRenderer.ShowText(index, false);
        }

        protected internal override void UpdateParent()
        {
            base.UpdateParent();

            if (index != -1)
                textRenderer.ChangeDisplayLayer(index, (byte)(BaseDisplayLayer + displayLayerOffset));
        }

        public void UseSpecialDigits(bool use)
        {
            if (useSpecialDigits == use)
                return;

            useSpecialDigits = use;

            if (index != -1)
                textRenderer.UseSpecialDigits(index, use);
        }
    }

    internal class SlideBar : GuiObject
    {
        readonly Icon icon = null;
        readonly Render.IColoredRect fillRect = null;
        int fill = 0;
        readonly byte displayLayerOffset;

        public SlideBar(Interface interf, byte displayLayerOffset)
            : base(interf)
        {
            this.displayLayerOffset = displayLayerOffset;

            icon = new Icon(interf, 64, 8, Data.Resource.Icon, 236u, (byte)(displayLayerOffset + 1));

            fillRect = interf.RenderView.ColoredRectFactory.Create(0, 4, new Render.Color(0x6b, 0xab, 0x3b), (byte)(displayLayerOffset + 2));
            fillRect.Layer = Layer;

            SetSize(64, 8);

            AddChild(icon, 0, 0, true);
        }

        public int Fill
        {
            get => fill;
            set
            {
                if (value < 0)
                    value = 0;

                if (value > 50)
                    value = 50;

                if (fill == value)
                    return;

                fill = value;

                fillRect.Resize(fill, 4);
                FillChanged?.Invoke(this, System.EventArgs.Empty);
            }
        }

        protected override void InternalDraw()
        {
            fillRect.X = TotalX + 7;
            fillRect.Y = TotalY + 2;

            fillRect.Visible = fill > 0 && Displayed;
            icon.Displayed = Displayed;
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            fillRect.Visible = false;
        }

        protected internal override void UpdateParent()
        {
            fillRect.DisplayLayer = (byte)(BaseDisplayLayer + displayLayerOffset + 2);
        }

        protected override bool HandleClickLeft(int x, int y)
        {
            int relX = x - TotalX;

            if (relX < 7)
                Fill = 0;
            else if (relX < 57)
                Fill = relX - 7;
            else
                Fill = 50;

            return true;
        }

        public event System.EventHandler FillChanged;
    }
}
