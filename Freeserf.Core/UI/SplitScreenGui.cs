/*
 * SplitScreenGui.cs - GUI for split-screen multiplayer (several local players)
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

using System.Collections.Generic;

namespace Freeserf.UI
{
    // Manages the viewers of all local players in split-screen mode. Each player has
    // his own viewer and interface which renders to his part of the screen. The input
    // events of the shared render view are routed to the player whose part of the
    // screen the input position is in.
    internal class SplitScreenGui : Gui
    {
        readonly Render.IRenderView sharedRenderView = null;
        readonly Audio.IAudioInterface audioInterface = null;
        readonly List<Viewer> viewers = new List<Viewer>();
        readonly List<Render.IRenderView> playerRenderViews = new List<Render.IRenderView>();
        readonly List<Rect> playerViewRects = new List<Rect>();
        // The shared layer the decorative pillar at the boundary is drawn on. It is
        // rendered unscissored on top of both players' content (see GameView).
        readonly Render.IRenderLayer pillarLayer = null;
        // The pillar sprites created by this gui. They are deleted when the gui is
        // destroyed (before the shared pillar layer is disposed by the game view).
        readonly List<Render.ISprite> pillarSprites = new List<Render.ISprite>();
        // The width of the custom pillar sprite (see CustomSprites.Pillar).
        const int PillarWidth = 53;
        int lastCursorViewX = 0;

        // Handler which is called when the split-screen game is closed and the
        // game view should return to the normal single-player mode (full screen
        // menu). Set by the game view.
        public System.Action DeactivateSplitScreenHandler { get; set; } = null;

        public SplitScreenGui(Render.IRenderView sharedRenderView, IEnumerable<Render.IRenderView> playerRenderViews,
            Audio.IAudioInterface audioInterface, Render.IRenderLayer pillarLayer)
            : base(sharedRenderView, audioInterface, false)
        {
            this.sharedRenderView = sharedRenderView;
            this.audioInterface = audioInterface;
            this.pillarLayer = pillarLayer;
            this.playerRenderViews.AddRange(playerRenderViews);

            int count = this.playerRenderViews.Count;
            int width = this.playerRenderViews[0].VirtualScreen.Size.Width;
            int height = this.playerRenderViews[0].VirtualScreen.Size.Height;

            for (int i = 0; i < count; ++i)
            {
                playerViewRects.Add(new Rect(i * width, 0, width, height));

                // The game is configured in the single setup screen before the
                // split-screen mode is activated, so no setup screen is opened here.
                var viewer = new SplitScreenPlayerViewer(this.playerRenderViews[i], audioInterface, this, (uint)i, false);
                viewers.Add(viewer);
            }

            // Only the first player drives the game updates. The other players only
            // update their own UI.
            for (int i = 1; i < count; ++i)
                viewers[i].MainInterface.DrivesGameUpdates = false;

            // Only the first player may change the game speed. The game speed
            // buttons are hidden for the other players.
            for (int i = 1; i < count; ++i)
                viewers[i].MainInterface.CanChangeGameSpeed = false;

            // Only the first player may change the game settings (quit, save,
            // options). The corresponding menu entries are hidden for the others.
            for (int i = 1; i < count; ++i)
                viewers[i].MainInterface.CanChangeGameSettings = false;

            // The custom pillar sprite is scaled to the full height of the screen
            // (the texture is sampled at its native size, the rendered quad is
            // stretched to the screen height).
            int screenHeight = playerViewRects[0].Size.Height;
            pillarLayer.SizeTransformation = (Size size) => new Size(size.Width, screenHeight);

            // Draw a decorative wooden pillar at each boundary between the players'
            // screen parts (like the History Edition).
            for (int i = 1; i < count; ++i)
                CreatePillar(i);

            // Route the input events of the shared render view to the players.
            sharedRenderView.Click += SplitScreen_Click;
            sharedRenderView.DoubleClick += SplitScreen_DoubleClick;
            sharedRenderView.SpecialClick += SplitScreen_DoubleClick;
            sharedRenderView.Drag += SplitScreen_Drag;
            sharedRenderView.KeyPress += SplitScreen_KeyPress;
            sharedRenderView.SystemKeyPress += SplitScreen_SystemKeyPress;
            sharedRenderView.StopDrag += SplitScreen_StopDrag;
            sharedRenderView.Wheel += SplitScreen_Wheel;
        }

        public IReadOnlyList<Viewer> Viewers => viewers;

        // Removes the event subscriptions of this gui. The base implementation only
        // removes the handlers of the base gui, so the split-screen handlers which
        // were subscribed in the constructor must be removed here as well. Otherwise
        // a click which arrives after the split-screen gui was replaced (e.g. the
        // delayed click after quitting the game) would be routed to the already
        // cleared viewer list and crash.
        public override void UnsubscribeEvents()
        {
            base.UnsubscribeEvents();

            sharedRenderView.Click -= SplitScreen_Click;
            sharedRenderView.DoubleClick -= SplitScreen_DoubleClick;
            sharedRenderView.SpecialClick -= SplitScreen_DoubleClick;
            sharedRenderView.Drag -= SplitScreen_Drag;
            sharedRenderView.KeyPress -= SplitScreen_KeyPress;
            sharedRenderView.SystemKeyPress -= SplitScreen_SystemKeyPress;
            sharedRenderView.StopDrag -= SplitScreen_StopDrag;
            sharedRenderView.Wheel -= SplitScreen_Wheel;
        }

        // Switches back to the normal single-player mode (full screen menu). Called
        // when the split-screen game is closed. The game view does the actual switch.
        public void DeactivateSplitScreen()
        {
            DeactivateSplitScreenHandler?.Invoke();
        }

        // Starts a split-screen game (2 local players sharing one game).
        public void StartSplitScreenGame(GameInfo gameInfo)
        {
            if (!GameManager.Instance.StartSplitScreenGame(gameInfo, playerRenderViews, audioInterface))
                Log.Error.Write(ErrorSystemType.Application, "Failed to start split-screen game.");
        }

        int GetPlayerIndex(int viewX)
        {
            for (int i = 0; i < playerViewRects.Count; ++i)
            {
                if (viewX < playerViewRects[i].Right)
                    return i;
            }

            return playerViewRects.Count - 1;
        }

        // Draws a decorative wooden pillar at the boundary between the given player
        // and the previous one (like the History Edition). The pillar is drawn on
        // the shared pillar layer which is rendered unscissored on top of both
        // players' content, so it can span the full height and be centered on the
        // boundary. All coordinates are in the shared virtual screen space. The
        // custom pillar sprite is scaled to the screen height by the pillar layer's
        // size transformation.
        void CreatePillar(int playerIndex)
        {
            const byte displayLayer = 250;

            // The boundary between the previous player and this one.
            int boundaryX = playerViewRects[playerIndex].Left;

            // The pillar is centered on the boundary. The sprite is created at its
            // native size and scaled to the screen height by the pillar layer's
            // size transformation.
            int x = boundaryX - PillarWidth / 2;

            AddPillarSprite(x, 0, PillarWidth, (int)Render.CustomSprites.Pillar.Height, displayLayer);
        }

        void AddPillarSprite(int x, int y, int width, int height, byte displayLayer)
        {
            var offset = Render.TextureAtlasManager.Instance.GetPillarOffset();
            var sprite = sharedRenderView.SpriteFactory.Create(width, height, offset.X, offset.Y, false, true, displayLayer) as Render.ILayerSprite;
            sprite.Layer = pillarLayer;
            sprite.X = x;
            sprite.Y = y;
            sprite.Visible = true;

            pillarSprites.Add(sprite);
        }

        // Transforms the given view position (in the shared virtual screen) to the
        // local view coordinates of the given player.
        Position ToPlayerViewPosition(int playerIndex, int viewX, int viewY)
        {
            var viewRect = playerViewRects[playerIndex];

            return new Position(viewX - viewRect.Left, viewY - viewRect.Top);
        }

        // The GUI is scaled to fill the player's part of the screen (the same
        // transformation which is used by the gui layers of the player).
        SplitScreenGuiScaling GetPlayerGuiScaling(int playerIndex)
        {
            return new SplitScreenGuiScaling(playerViewRects[playerIndex].Size);
        }

        Position ToPlayerGuiPosition(int playerIndex, Position localViewPosition)
        {
            return GetPlayerGuiScaling(playerIndex).ToGui(localViewPosition);
        }

        Size ToPlayerGuiDelta(int playerIndex, Size localViewDelta)
        {
            return GetPlayerGuiScaling(playerIndex).DeltaToGui(localViewDelta);
        }

        bool RouteClick(Event.EventArgs args)
        {
            int playerIndex = GetPlayerIndex(args.X);
            var position = ToPlayerViewPosition(playerIndex, args.X, args.Y);

            // First transform to the player's local view coordinates (the interface
            // needs them for the viewport handling).
            var localArgs = Event.EventArgs.Transform(args, position.X, position.Y, args.Dx, args.Dy);

            // Then transform to the GUI coordinates of the player.
            var guiPosition = ToPlayerGuiPosition(playerIndex, position);

            args = Event.EventArgs.Transform(localArgs, guiPosition.X, guiPosition.Y, args.Dx, args.Dy);

            return viewers[playerIndex].SendEvent(args);
        }

        bool RouteDrag(Event.EventArgs args)
        {
            int playerIndex = GetPlayerIndex(args.X);
            var position = ToPlayerViewPosition(playerIndex, args.X, args.Y);

            var localArgs = Event.EventArgs.Transform(args, position.X, position.Y, args.Dx, args.Dy);

            var guiPosition = ToPlayerGuiPosition(playerIndex, position);
            var guiDelta = ToPlayerGuiDelta(playerIndex, new Size(args.Dx, args.Dy));

            args = Event.EventArgs.Transform(localArgs, guiPosition.X, guiPosition.Y, guiDelta.Width, guiDelta.Height);

            return viewers[playerIndex].SendEvent(args);
        }

        bool RouteKey(Event.EventArgs args)
        {
            // Keyboard input is routed to the player whose part of the screen the
            // cursor is currently in.
            int playerIndex = GetPlayerIndex(lastCursorViewX);

            return viewers[playerIndex].SendEvent(args);
        }

        bool SplitScreen_Click(object sender, Event.EventArgs args)
        {
            return RouteClick(args);
        }

        bool SplitScreen_DoubleClick(object sender, Event.EventArgs args)
        {
            return RouteClick(args);
        }

        bool SplitScreen_Drag(object sender, Event.EventArgs args)
        {
            return RouteDrag(args);
        }

        bool SplitScreen_Wheel(object sender, Event.EventArgs args)
        {
            return RouteClick(args);
        }

        bool SplitScreen_KeyPress(object sender, Event.EventArgs args)
        {
            return RouteKey(args);
        }

        bool SplitScreen_SystemKeyPress(object sender, Event.EventArgs args)
        {
            return RouteKey(args);
        }

        bool SplitScreen_StopDrag(object sender, Event.EventArgs args)
        {
            int playerIndex = GetPlayerIndex(lastCursorViewX);

            return viewers[playerIndex].SendEvent(args);
        }

        protected override void RenderView_ZoomChanged(object sender, System.EventArgs e)
        {
            foreach (var viewer in viewers)
                viewer.MainInterface.HandleZoomChange();
        }

        public override void Draw()
        {
            foreach (var viewer in viewers)
            {
                viewer.Update();
                viewer.Draw();
            }
        }

        public override void DrawCursor(int x, int y)
        {
            lastCursorViewX = x;

            for (int i = 0; i < viewers.Count; ++i)
            {
                // Clamp the cursor to the player's part of the screen so it cannot
                // cross the boundary (the decorative pillar) into the other player's
                // part.
                int clampedX = System.Math.Max(playerViewRects[i].Left, System.Math.Min(playerViewRects[i].Right - 1, x));

                // The cursor layer is in view coordinates (identity for the first
                // player, offset for the others), so the player-local view position
                // is used here.
                var position = ToPlayerViewPosition(i, clampedX, y);

                // Only show the cursor of the player whose part of the screen the
                // cursor is in.
                if (i == GetPlayerIndex(x))
                    viewers[i].DrawCursor(position.X, position.Y);
            }
        }

        public override bool Ingame
        {
            get
            {
                foreach (var viewer in viewers)
                {
                    if (viewer.Ingame)
                        return true;
                }

                return false;
            }
        }

        public override bool CanZoom
        {
            get
            {
                // The zoom is applied per player, so the player whose part of the
                // screen the cursor is in can zoom.
                int playerIndex = GetPlayerIndex(lastCursorViewX);

                return viewers[playerIndex].MainInterface.CanZoom;
            }
        }

        public override void UpdateNetworkEvents()
        {
            foreach (var viewer in viewers)
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
                    var client = viewer.MainInterface?.Client;

                    if (client == null)
                        throw new ExceptionFreeserf(ErrorSystemType.Network, "Client viewer without client interface.");

                    client.NetworkDataReceiver = NetworkDataReceiver;
                    client.UpdateNetworkEvents();
                }
            }
        }

        public override void DisconnectNetwork()
        {
            foreach (var viewer in viewers)
            {
                if (viewer?.MainInterface == null)
                    continue;

                try
                {
                    viewer.MainInterface.Client?.Disconnect();
                }
                catch (System.Exception ex)
                {
                    Log.Error.Write(ErrorSystemType.Network, "Error disconnecting client: " + ex.Message);
                }

                try
                {
                    viewer.MainInterface.Server?.BroadcastDisconnect();
                    viewer.MainInterface.Server?.Close();
                }
                catch (System.Exception ex)
                {
                    Log.Error.Write(ErrorSystemType.Network, "Error closing server: " + ex.Message);
                }
            }
        }

        public override void DestroyViewer()
        {
            // Delete the pillar sprites before the shared pillar layer is disposed
            // by the game view (the layer is not part of the player layers).
            foreach (var sprite in pillarSprites)
                sprite.Delete();

            pillarSprites.Clear();

            foreach (var viewer in viewers)
                viewer.Destroy(true);

            viewers.Clear();
        }
    }
}
