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
        internal static bool IsTextInputFocused => FocusedObject?.IsTextInput == true;
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
            lock (children)
            {
                foreach (var child in children)
                    child.UpdateParent();

                children.Sort(childComparer);
            }
        }

        protected abstract void InternalDraw();
        protected virtual void InternalHide()
        {
            if (!Displayed)
            {
                lock (children)
                {
                    foreach (var child in children)
                        child.Displayed = false;
                }
            }
        }

        protected virtual void Layout()
        {
            // empty
        }

        // Extra clickable border (in GUI units) used in touch mode
        protected virtual int HitSlop => 0;

        protected virtual bool HandleClickLeft(int x, int y, bool delayed)
        {
            return false;
        }

        /* The original's special click (right button held). */
        protected virtual bool HandleClickRight(int x, int y)
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

        protected virtual bool HandleWheel(int x, int y, int steps)
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

                lock (children)
                {
                    for (int i = children.Count - 1; i >= 0; --i)
                    {
                        // The collection might change while drawing (e.g. through key events).
                        // In worst case we draw a child twice.
                        children[i].Draw();
                    }
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

        internal virtual void RefreshLanguage()
        {
            lock (children)
            {
                foreach (var child in children)
                    child.RefreshLanguage();
            }
            SetRedraw();
        }

        public bool HasChild(GuiObject obj)
        {
            return children.Contains(obj);
        }

        public void AddChild(GuiObject obj, int x, int y, bool displayed = true)
        {
            lock (children)
            {
                obj.Parent = this;
                children.Add(obj);
                obj.MoveTo(x, y); // will call SetRedraw
                obj.Displayed = displayed;

                children.Sort(childComparer);
            }
        }

        public void DeleteChild(GuiObject obj)
        {
            obj.Parent = null;
            lock (children)
            {
                children.Remove(obj);
            }
            SetRedraw();
        }

        // True for GUI objects that need text input (e.g. TextInput). Platform
        // hosts use this to show/hide the on-screen keyboard on focus changes.
        public virtual bool IsTextInput => false;
        public bool Focused => focused;

        public void SetFocused()
        {
            if (FocusedObject != this)
            {
                if (FocusedObject != null)
                {
                    bool wasTextInput = FocusedObject.IsTextInput;
                    FocusedObject.focused = false;
                    FocusedObject.HandleFocusLoose();
                    FocusedObject.SetRedraw();
                    if (wasTextInput)
                        Gui.NotifyTextInputFocusChanged(false);
                }

                focused = true;
                FocusedObject = this;
                SetRedraw();
                if (IsTextInput)
                    Gui.NotifyTextInputFocusChanged(true);
            }
        }

        public static void LooseFocus()
        {
            if (FocusedObject != null)
            {
                bool wasTextInput = FocusedObject.IsTextInput;
                FocusedObject.focused = false;
                FocusedObject.HandleFocusLoose();
                FocusedObject.SetRedraw();
                if (wasTextInput)
                    Gui.NotifyTextInputFocusChanged(false);
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
            lock (children)
            {
                foreach (var child in children)
                {
                    child.HandleZoomChange();
                }
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
            // An object which gets the focus while handling the event (e.g. a text input
            // of a popup opened by a button) must keep it. The delayed click which follows
            // each click must not change the focus as well (the click already did it).
            var focusedObjectBefore = FocusedObject;

            if (e.Type == Event.Type.Click ||
                e.Type == Event.Type.DelayedClick ||
                e.Type == Event.Type.DoubleClick ||
                e.Type == Event.Type.SpecialClick ||
                e.Type == Event.Type.Drag ||
                e.Type == Event.Type.Wheel)
            {
                int objectX = e.X - TotalX;
                int objectY = e.Y - TotalY;

                int slop = GuiScaling.TouchMode ? HitSlop : 0;

                if (objectX < -slop || objectY < -slop || objectX > Width + slop || objectY > Height + slop)
                {
                    return false;
                }
            }

            // Find the corresponding child element if any
            lock (children)
            {
                foreach (var child in children)
                {
                    if (child.HandleEvent(e))
                    {
                        return true;
                    }
                }
            }

            bool result = false;

            switch (e.Type)
            {
                case Event.Type.Click:
                    if (e.Button == Event.Button.Left)
                        result = HandleClickLeft(eventX, eventY, false);
                    break;
                case Event.Type.DelayedClick:
                    if (e.Button == Event.Button.Left)
                        result = HandleClickLeft(eventX, eventY, true);
                    break;
                case Event.Type.Drag:
                    result = HandleDrag(e.X, e.Y, e.Dx, e.Dy, e.Button);
                    break;
                case Event.Type.DoubleClick:
                    result = HandleDoubleClick(e.X, e.Y, e.Button);
                    break;
                case Event.Type.SpecialClick:
                    result = HandleClickRight(e.X, e.Y);
                    break;
                case Event.Type.Wheel:
                    result = HandleWheel(eventX, eventY, e.Dy);
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

            if (result && FocusedObject != this && FocusedObject == focusedObjectBefore && e.Type != Event.Type.DelayedClick && e.Type != Event.Type.Wheel)
            {
                if (FocusedObject != null)
                {
                    bool wasTextInput = FocusedObject.IsTextInput;
                    FocusedObject.focused = false;
                    FocusedObject.HandleFocusLoose();
                    FocusedObject.SetRedraw();
                    if (wasTextInput)
                        Gui.NotifyTextInputFocusChanged(false);
                    FocusedObject = null;
                }
            }

            return result;
        }
    }

    public class Gui : Network.INetworkDataHandler
    {
        // Fired when a text input gains (true) or loses (false) focus. Platform
        // hosts (e.g. Android) use this to show/hide the on-screen keyboard.
        public static event Action<bool> TextInputFocusChanged;

        internal static void NotifyTextInputFocusChanged(bool focused)
        {
            TextInputFocusChanged?.Invoke(focused);
        }

        public static bool IsTextInputFocused => GuiObject.IsTextInputFocused;

        protected readonly Render.IRenderView renderView = null;
        protected Viewer viewer = null;

        // Handler which is called when a split-screen game is started from the
        // setup screen. The game view uses it to switch to split-screen mode.
        public System.Action<GameInfo> StartSplitScreenGameHandler { get; set; } = null;

        // Handler which is called when the split-screen game type is selected in
        // the setup screen. The game view uses it to switch to split-screen mode
        // (showing the setup screen for both players).
        public System.Action ActivateSplitScreenHandler { get; set; } = null;

        // Starts a split-screen game. The default gui delegates to the handler
        // which is set by the game view (it switches to split-screen mode first).
        public virtual void StartSplitScreenGame(GameInfo gameInfo)
        {
            StartSplitScreenGameHandler?.Invoke(gameInfo);
        }

        // Switches to split-screen mode (showing the setup screen for both players).
        public virtual void ActivateSplitScreen()
        {
            ActivateSplitScreenHandler?.Invoke();
        }

        public Gui(Render.IRenderView renderView, Audio.IAudioInterface audioInterface)
            : this(renderView, audioInterface, true)
        {
        }

        // Subclasses (e.g. the split-screen gui) may skip the creation of the initial
        // viewer and the event subscription and do it themselves.
        protected Gui(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, bool createViewer)
        {
            if (renderView.VirtualScreen.Size.Width > Global.MAX_VIRTUAL_SCREEN_WIDTH ||
                renderView.VirtualScreen.Size.Height > Global.MAX_VIRTUAL_SCREEN_HEIGHT)
                throw new ExceptionFreeserf(ErrorSystemType.Render, $"The virtual screen must not be larger than {Global.MAX_VIRTUAL_SCREEN_WIDTH}x{Global.MAX_VIRTUAL_SCREEN_HEIGHT}.");

            this.renderView = renderView;

            renderView.ZoomChanged += RenderView_ZoomChanged;

            if (!createViewer)
                return;

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
            renderView.Wheel += RenderView_Wheel;
        }

        // Removes the event subscriptions of this gui. Must be called when the gui
        // is replaced (e.g. when switching to split-screen mode) so the old gui does
        // not receive events anymore.
        public virtual void UnsubscribeEvents()
        {
            renderView.ZoomChanged -= RenderView_ZoomChanged;
            renderView.Click -= RenderView_Click;
            renderView.DoubleClick -= RenderView_DoubleClick;
            renderView.SpecialClick -= RenderView_DoubleClick;
            renderView.Drag -= RenderView_Drag;
            renderView.KeyPress -= RenderView_KeyPress;
            renderView.SystemKeyPress -= RenderView_SystemKeyPress;
            renderView.StopDrag -= RenderView_StopDrag;
            renderView.Wheel -= RenderView_Wheel;
        }

        public Network.INetworkDataReceiver NetworkDataReceiver { get; set; }

        public virtual void UpdateNetworkEvents()
        {
            if (viewer != null)
            {
                if (viewer.MainInterface?.Server != null || viewer.ViewerType == Viewer.Type.Server)
                {
                    var server = viewer.MainInterface?.Server;

                    if (server == null)
                        throw new ExceptionFreeserf(ErrorSystemType.Network, "Server viewer without server interface.");

                    server.NetworkDataReceiver = NetworkDataReceiver;
                    server.UpdateNetworkEvents();
                }
                else if (viewer.MainInterface?.Client != null ||
                         viewer.ViewerType == Viewer.Type.Client ||
                         viewer.ViewerType == Viewer.Type.RemoteSpectator ||
                         viewer.ViewerType == Viewer.Type.RestrictedRemoteSpectator)
                {
                    // TODO: spectators
                    var client = viewer.MainInterface?.Client;

                    if (client == null)
                        throw new ExceptionFreeserf(ErrorSystemType.Network, "Client viewer without client interface.");

                    // Note: This is also needed if the connection was lost (to handle it).
                    client.NetworkDataReceiver = NetworkDataReceiver;
                    client.UpdateNetworkEvents();
                }
            }
        }

        // Closes any active multiplayer connection (e.g. when the app is
        // backgrounded) so no network threads keep running.
        public virtual void DisconnectNetwork()
        {
            if (viewer?.MainInterface == null)
                return;

            try
            {
                viewer.MainInterface.Client?.Disconnect();
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Network, "Error disconnecting client: " + ex.Message);
            }

            try
            {
                // Tell the clients that the game is over for them.
                viewer.MainInterface.Server?.BroadcastDisconnect();
                viewer.MainInterface.Server?.Close();
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Network, "Error closing server: " + ex.Message);
            }
        }

        protected virtual void RenderView_ZoomChanged(object sender, EventArgs e)
        {
            viewer.MainInterface.HandleZoomChange();
        }

        internal void SetViewer(Viewer viewer)
        {
            if (this.viewer == viewer)
                return;

            if (this.viewer != null)
                this.viewer.Destroy(viewer == null || this.viewer.MainInterface != viewer.MainInterface);

            this.viewer = viewer;

            if (this.viewer != null)
                this.viewer.Init();
        }

        // Removes the current viewer from the game manager. Must be called when
        // the render view is closed (e.g. on Android, where the process stays
        // alive after the activity is finished): otherwise the stale viewer
        // keeps receiving game events and accesses the disposed render view's
        // layers, which throws (e.g. "The given key 'Landscape' was not present
        // in the dictionary") when the next game starts.
        public virtual void DestroyViewer()
        {
            if (viewer != null)
            {
                viewer.Destroy(true);
                viewer = null;
            }
        }

        public Viewer ActiveViewer => viewer;

        public virtual bool Ingame => viewer.Ingame;
        public virtual bool CanZoom => viewer.MainInterface.CanZoom;

        public virtual void DrawCursor(int x, int y)
        {
            viewer.DrawCursor(x, y);
        }

        protected Position PositionToGui(Position position) => GuiScaling.For(renderView.VirtualScreen.Size).ToGui(position);

        public static Position PositionToGame(Position position, Render.IRenderView renderView)
        {
            float zoomFactor = 1.0f + renderView.Zoom * 0.5f;

            var size = DeltaToGame(renderView.VirtualScreen.Size, renderView);

            var x = Misc.Round((renderView.VirtualScreen.Size.Width - size.Width) * 0.5f + position.X / zoomFactor);
            var y = Misc.Round((renderView.VirtualScreen.Size.Height - size.Height) * 0.5f + position.Y / zoomFactor);

            return new Position(x, y);
        }

        protected Size DeltaToGui(Size delta) => GuiScaling.For(renderView.VirtualScreen.Size).DeltaToGui(delta);

        public static Size DeltaToGame(Size delta, Render.IRenderView renderView)
        {
            float zoomFactor = 1.0f + renderView.Zoom * 0.5f;

            var width = Misc.Round(delta.Width / zoomFactor);
            var height = Misc.Round(delta.Height / zoomFactor);

            return new Size(width, height);
        }

        protected virtual bool RenderView_KeyPress(object sender, Event.EventArgs args)
        {
            return viewer.SendEvent(args);
        }

        protected virtual bool RenderView_SystemKeyPress(object sender, Event.EventArgs args)
        {
            return viewer.SendEvent(args);
        }

        protected virtual bool RenderView_Drag(object sender, Event.EventArgs args)
        {
            var position = PositionToGui(new Position(args.X, args.Y));
            var delta = DeltaToGui(new Size(args.Dx, args.Dy));

            args = Event.EventArgs.Transform(args, position.X, position.Y, delta.Width, delta.Height);

            return viewer.SendEvent(args);
        }

        protected virtual bool RenderView_StopDrag(object sender, Event.EventArgs args)
        {
            return viewer.SendEvent(args);
        }

        protected virtual bool RenderView_DoubleClick(object sender, Event.EventArgs args)
        {
            var position = PositionToGui(new Position(args.X, args.Y));

            args = Event.EventArgs.Transform(args, position.X, position.Y, args.Dy, args.Dy);

            return viewer.SendEvent(args);
        }

        private bool RenderView_Wheel(object sender, Event.EventArgs args)
        {
            var position = PositionToGui(new Position(args.X, args.Y));

            args = Event.EventArgs.Transform(args, position.X, position.Y, args.Dx, args.Dy);

            return viewer.SendEvent(args);
        }

        protected virtual bool RenderView_Click(object sender, Event.EventArgs args)
        {
            var position = PositionToGui(new Position(args.X, args.Y));

            args = Event.EventArgs.Transform(args, position.X, position.Y, args.Dy, args.Dy);

            return viewer.SendEvent(args);
        }

        public virtual void Draw()
        {
            MultiplayerTestDriver.PreUpdate(this);
            viewer.Update();
            MultiplayerTestDriver.Update(this);
            viewer.Draw();
        }
    }
}
