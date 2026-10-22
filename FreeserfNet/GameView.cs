/*
 * GameView.cs - Implementation of a game render view
 *
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

using Freeserf.Audio;
using Freeserf.Data;
using Freeserf.Event;
using Freeserf.Network;
using Freeserf.Render;
using Freeserf.Renderer;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Freeserf
{
    using EventArgs = Event.EventArgs;
    using EventHandler = Event.EventHandler;
    using EventType = Event.Type;
    using Texture = Renderer.Texture;

    public delegate bool FullscreenRequestHandler(bool fullscreen);

    public class GameView : RenderLayerFactory, IRenderView, IAudioInterface, INetworkDataHandler, IDisposable
    {
        // this is for the background map at start
        int mapScrollTicks = 0;
        bool disposed = false;
        readonly Context context;
        Rect virtualScreenDisplay;
        readonly SizingPolicy sizingPolicy;
        readonly OrientationPolicy orientationPolicy;
        readonly DeviceType deviceType;
        readonly bool isLandscapeRatio = true;
        Rotation rotation = Rotation.None;
        // In split-screen mode there is one set of layers per local player. Each set
        // has position transformations which map the player's rendering to his part
        // of the screen.
        readonly List<SortedDictionary<Layer, RenderLayer>> playerLayers = new List<SortedDictionary<Layer, RenderLayer>>();
        readonly List<SplitScreenRenderView> playerRenderViews = new List<SplitScreenRenderView>();
        // The zoom of each local player (used in split-screen mode). In normal
        // mode there is only one entry which is not used (the context zoom is
        // used instead).
        readonly List<float> playerZooms = new List<float>();
        bool splitScreenActive = false;
        bool splitScreenPending = false;
        bool splitScreenDeactivatePending = false;
        GameInfo pendingSplitScreenGameInfo = null;
        // The shared layer the decorative pillar at the boundary between the
        // players' screen parts is drawn on. It is rendered unscissored on top of
        // both players' content so the pillar can span the full height and be
        // centered on the boundary.
        RenderLayer pillarLayer = null;
        readonly SpriteFactory spriteFactory = null;
        readonly TriangleFactory triangleFactory = null;
        readonly ColoredRectFactory coloredRectFactory = null;
        readonly MinimapTextureFactory minimapTextureFactory = null;
        readonly AudioFactory audioFactory = null;
        UI.Gui gui = null;
        bool fullscreen = false;

        float sizeFactorX = 1.0f;
        float sizeFactorY = 1.0f;
        Position cursorPosition = new Position();
        Position lastCursorPosition = new Position();

        public event System.EventHandler Closed;
        public event EventHandler Click;
        public event EventHandler DoubleClick;
        public event EventHandler SpecialClick;
        public event EventHandler Drag;
        public event EventHandler KeyPress;
        public event EventHandler SystemKeyPress;
        public event EventHandler StopDrag;
        public event EventHandler Wheel;
        public FullscreenRequestHandler FullscreenRequestHandler { get; set; }
        public INetworkDataReceiver NetworkDataReceiver { get; set; }

        public GameView(DataSource dataSource, Size virtualScreenSize,
            DeviceType deviceType = DeviceType.Desktop,
            SizingPolicy sizingPolicy = SizingPolicy.FitRatio,
            OrientationPolicy orientationPolicy = OrientationPolicy.Support180DegreeRotation)
        {
            VirtualScreen = new Rect(0, 0, Math.Min(virtualScreenSize.Width, Global.MAX_VIRTUAL_SCREEN_WIDTH), Math.Min(virtualScreenSize.Height, Global.MAX_VIRTUAL_SCREEN_HEIGHT));
            virtualScreenDisplay = new Rect(VirtualScreen);
            this.sizingPolicy = sizingPolicy;
            this.orientationPolicy = orientationPolicy;
            this.deviceType = deviceType;
            isLandscapeRatio = VirtualScreen.Size.Width > VirtualScreen.Size.Height;

            playerZooms.Add(0.0f);

            context = new Context(VirtualScreen.Size.Width, VirtualScreen.Size.Height);

            if (dataSource == null || !dataSource.IsLoaded)
                throw new ExceptionFreeserf(ErrorSystemType.Data, "Given data source is not useable.");

            DataSource = dataSource;

            // factories
            spriteFactory = new SpriteFactory(VirtualScreen);
            triangleFactory = new TriangleFactory(VirtualScreen);
            coloredRectFactory = new ColoredRectFactory(VirtualScreen);
            minimapTextureFactory = new MinimapTextureFactory();
            audioFactory = new AudioFactory(dataSource);
            var audio = audioFactory.GetAudio();

            if (audio != null)
            {
                var musicPlayer = audio.GetMusicPlayer();
                var soundPlayer = audio.GetSoundPlayer();
                var volumeController = audio.GetVolumeController();

                if (musicPlayer != null)
                    musicPlayer.Enabled = UserConfig.Audio.Music;
                if (soundPlayer != null)
                    soundPlayer.Enabled = UserConfig.Audio.Sound;
                if (volumeController != null)
                    volumeController.SetVolume(UserConfig.Audio.Volume);
            }

            TextureAtlasManager.RegisterFactory(new TextureAtlasBuilderFactory());

            var textureAtlas = TextureAtlasManager.Instance;

            textureAtlas.AddAll(dataSource);

            // The game starts with a single local player (full screen). Split-screen
            // mode is activated from the setup screen (like the History Edition).
            CreatePlayerLayers(0, VirtualScreen.Size.Width, 0, false);

            try
            {
                System.Console.WriteLine($"GameView: glError after layer creation = {Freeserf.Renderer.State.Gl.GetError()}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("GameView: glError query failed: " + ex.Message);
            }

            gui = new UI.Gui(this, this);
            gui.ActivateSplitScreenHandler = ActivateSplitScreen;
            gui.StartSplitScreenGameHandler = StartSplitScreenGame;
        }

        // Creates the render layers for one local player. In split-screen mode the
        // layers are transformed to the player's part of the screen.
        void CreatePlayerLayers(int playerIndex, int playerViewWidth, int viewOffsetX, bool splitScreen)
        {
            var layers = new SortedDictionary<Layer, RenderLayer>();
            playerLayers.Add(layers);

            var textureAtlas = TextureAtlasManager.Instance;

            foreach (Layer layer in Enum.GetValues(typeof(Layer)))
            {
                if (layer == Layer.None)
                    continue;

                // TODO: color keys?

                try
                {
                    var texture = (layer == Layer.Minimap) ? minimapTextureFactory.GetMinimapTexture() :
                        textureAtlas.GetOrCreate(layer).Texture as Texture;

#if DEBUG
                    try
                    {
                        System.Console.WriteLine($"GameView: layer {layer} texture {texture?.Width}x{texture?.Height} glError={Freeserf.Renderer.State.Gl.GetError()}");
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine("GameView: glError query failed: " + ex.Message);
                    }
#endif

                    var renderLayer = Create(layer, texture,
                        layer == Layer.Gui, // only the gui supports colored rects
                        null, // no color key for now
                        layer == Layer.GuiFont ? new Render.Color(115, 179, 67, 255) : null); // UI font uses green color overlay

                    SetLayerTransformation(renderLayer as RenderLayer, layer, playerIndex, playerViewWidth, viewOffsetX, splitScreen);

                    renderLayer.Visible = true;

                    layers.Add(layer, renderLayer as RenderLayer);
                }
                catch (Exception ex)
                {
                    throw new ExceptionFreeserf(ErrorSystemType.Render, $"Unable to create layer '{layer.ToString()}': {ex.Message}");
                }
            }

#if DEBUG
            try
            {
                System.Console.WriteLine($"GameView: glError after layer creation = {Freeserf.Renderer.State.Gl.GetError()}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("GameView: glError query failed: " + ex.Message);
            }
#endif
        }

        // Sets the position/size transformation of a render layer. In normal mode
        // (single player) the gui is scaled uniformly and centered (like the
        // original game). In split-screen mode the gui layers are scaled to the
        // player's part of the screen and the map layers of the other players are
        // offset.
        void SetLayerTransformation(RenderLayer layer, Layer layerType, int playerIndex, int playerViewWidth, int viewOffsetX, bool splitScreen)
        {
            if (splitScreen)
            {
                var guiScaling = new SplitScreenGuiScaling(new Size(playerViewWidth, VirtualScreen.Size.Height));

                if (layerType == Layer.Gui || layerType == Layer.GuiBuildings || layerType == Layer.Minimap)
                {
                    // the gui needs scaling to fit the player's part of the screen
                    float scaleX = guiScaling.ScaleX;
                    float scaleY = guiScaling.ScaleY;
                    int offsetX = guiScaling.OffsetX + viewOffsetX;
                    int offsetY = guiScaling.OffsetY;

                    layer.PositionTransformation = (Position position) =>
                    {
                        return new Position(Misc.Round(position.X * scaleX + offsetX), Misc.Round(position.Y * scaleY + offsetY));
                    };

                    layer.SizeTransformation = (Size size) =>
                    {
                        // don't scale a dimension of 0
                        int width = (size.Width == 0) ? 0 : Misc.Round(size.Width * scaleX);
                        int height = (size.Height == 0) ? 0 : Misc.Round(size.Height * scaleY);

                        return new Size(width, height);
                    };
                }
                else if (layerType == Layer.GuiFont) // UI Font needs different scaling
                {
                    // The UI expects 8x8 characters but we may use different sizes.
                    // So we adjust the scale factors accordingly.
                    float charScaleX = (8.0f / Global.UIFontCharacterWidth) * guiScaling.ScaleX;
                    float charScaleY = (8.0f / Global.UIFontCharacterHeight) * guiScaling.ScaleY;

                    layer.PositionTransformation = (Position position) =>
                    {
                        return new Position(Misc.Round(position.X * guiScaling.ScaleX + guiScaling.OffsetX + viewOffsetX),
                            Misc.Round(position.Y * guiScaling.ScaleY + guiScaling.OffsetY));
                    };

                    layer.SizeTransformation = (Size size) =>
                    {
                        // don't scale a dimension of 0
                        int width = (size.Width == 0) ? 0 : Misc.Round(size.Width * charScaleX);
                        int height = (size.Height == 0) ? 0 : Misc.Round(size.Height * charScaleY);

                        return new Size(width, height);
                    };
                }
                else // the map layers are offset to the player's part of the screen
                {
                    layer.PositionTransformation = (Position position) =>
                    {
                        return new Position(position.X + viewOffsetX, position.Y);
                    };
                }
            }
            else if (layerType == Layer.Gui || layerType == Layer.GuiBuildings || layerType == Layer.Minimap)
            {
                // the gui needs uniform scaling + centering
                var guiScaling = GuiScaling.For(VirtualScreen.Size);
                float scale = guiScaling.Scale;
                int offsetX = guiScaling.OffsetX;
                int offsetY = guiScaling.OffsetY;

                layer.PositionTransformation = (Position position) =>
                {
                    return new Position(Misc.Round(position.X * scale + offsetX), Misc.Round(position.Y * scale + offsetY));
                };

                layer.SizeTransformation = (Size size) =>
                {
                    // don't scale a dimension of 0
                    int width = (size.Width == 0) ? 0 : Misc.Round(size.Width * scale);
                    int height = (size.Height == 0) ? 0 : Misc.Round(size.Height * scale);

                    return new Size(width, height);
                };
            }
            else if (layerType == Layer.GuiFont) // UI Font needs different scaling
            {
                // The UI expects 8x8 characters but we may use different sizes.
                // So we adjust the scale factors accordingly.
                var guiScaling = GuiScaling.For(VirtualScreen.Size);
                float scale = guiScaling.Scale;
                int offsetX = guiScaling.OffsetX;
                int offsetY = guiScaling.OffsetY;

                float charScaleX = (8.0f / Global.UIFontCharacterWidth) * scale;
                float charScaleY = (8.0f / Global.UIFontCharacterHeight) * scale;

                layer.PositionTransformation = (Position position) =>
                {
                    return new Position(Misc.Round(position.X * scale + offsetX), Misc.Round(position.Y * scale + offsetY));
                };

                layer.SizeTransformation = (Size size) =>
                {
                    // don't scale a dimension of 0
                    int width = (size.Width == 0) ? 0 : Misc.Round(size.Width * charScaleX);
                    int height = (size.Height == 0) ? 0 : Misc.Round(size.Height * charScaleY);

                    return new Size(width, height);
                };
            }
        }

        // Switches the game view to split-screen mode (several local players) and
        // shows the setup screen for both players. Called when the split-screen
        // game type is selected in the setup screen. The actual switch is deferred
        // to the next render call so the setup screen's click handling can finish.
        public void ActivateSplitScreen()
        {
            splitScreenPending = true;
        }

        // Starts a split-screen game (2 local players sharing one game). Called when
        // the player clicks "Start" in the setup screen with the split-screen game
        // type selected. The switch to split-screen mode is deferred to the next
        // render call so the setup screen's click handling can finish.
        public void StartSplitScreenGame(GameInfo gameInfo)
        {
            pendingSplitScreenGameInfo = gameInfo;
            splitScreenPending = true;
        }

        void DoActivateSplitScreen()
        {
            splitScreenPending = false;

            if (splitScreenActive)
                return;

            splitScreenActive = true;

            int playerViewWidth = VirtualScreen.Size.Width / 2;

            // The second player starts with the default zoom.
            playerZooms.Add(0.0f);

            // The background game (intro mission) is closed so its render objects
            // do not stay on the re-transformed layers.
            GameManager.Instance.CloseGame();

            // Re-transform the first player's layers to the left half of the screen.
            foreach (var layer in playerLayers[0])
                SetLayerTransformation(layer.Value, layer.Key, 0, playerViewWidth, 0, true);

            // Create the layers of the second player.
            CreatePlayerLayers(1, playerViewWidth, playerViewWidth, true);

            // Create the render views of both players.
            playerRenderViews.Add(new SplitScreenRenderView(this, 0, new Rect(0, 0, playerViewWidth, VirtualScreen.Size.Height)));
            playerRenderViews.Add(new SplitScreenRenderView(this, 1, new Rect(playerViewWidth, 0, playerViewWidth, VirtualScreen.Size.Height)));

            // Create the shared layer for the decorative pillar at the boundary.
            // It uses the gui texture atlas and the identity transformation (shared
            // virtual screen coordinates) and is rendered unscissored on top of both
            // players' content.
            var textureAtlas = TextureAtlasManager.Instance;
            var guiTexture = textureAtlas.GetOrCreate(Layer.Gui).Texture as Texture;
            pillarLayer = Create(Layer.Gui, guiTexture, false, null, null) as RenderLayer;
            pillarLayer.Visible = true;

            // Replace the gui with the split-screen gui. The setup screen is shown
            // for both players so they can configure their characters.
            gui.UnsubscribeEvents();
            gui.DestroyViewer();
            gui = new UI.SplitScreenGui(this, playerRenderViews, this, pillarLayer);
            ((UI.SplitScreenGui)gui).DeactivateSplitScreenHandler = DeactivateSplitScreen;

            // If a game was started from the setup screen, start it now (the
            // split-screen gui is in place and its viewers are registered).
            if (pendingSplitScreenGameInfo != null)
            {
                var gameInfo = pendingSplitScreenGameInfo;
                pendingSplitScreenGameInfo = null;

                if (!GameManager.Instance.StartSplitScreenGame(gameInfo, playerRenderViews, this))
                    Log.Error.Write(ErrorSystemType.Application, "Failed to start split-screen game.");
            }
        }

        // Switches the game view back to the normal single-player mode (full screen
        // menu). Called when the split-screen game is closed. The actual switch is
        // deferred to the next render call so the quit handling can finish.
        public void DeactivateSplitScreen()
        {
            splitScreenDeactivatePending = true;
        }

        void DoDeactivateSplitScreen()
        {
            splitScreenDeactivatePending = false;

            if (!splitScreenActive)
                return;

            splitScreenActive = false;

            // Remove the second player's zoom and reset the shared context zoom
            // (it may have been left at the second player's zoom by the last
            // split-screen render).
            playerZooms.RemoveAt(1);
            context.Zoom = 0.0f;

            // The scissor test may still be enabled if the last split-screen
            // render was interrupted, so it is disabled here.
            context.DisableScissor();

            // Re-transform the first player's layers back to the full screen.
            foreach (var layer in playerLayers[0])
                SetLayerTransformation(layer.Value, layer.Key, 0, VirtualScreen.Size.Width, 0, false);

            // Remove the second player's layers and render views.
            playerLayers.RemoveAt(1);
            playerRenderViews.Clear();

            // Replace the split-screen gui with the normal gui. It creates a fresh
            // viewer which opens the setup screen (full screen menu).
            gui.UnsubscribeEvents();
            gui.DestroyViewer();
            gui = new UI.Gui(this, this);
            gui.ActivateSplitScreenHandler = ActivateSplitScreen;
            gui.StartSplitScreenGameHandler = StartSplitScreenGame;

            // Remove the shared pillar layer. Its sprites were deleted by the
            // split-screen gui's DestroyViewer above.
            pillarLayer?.Dispose();
            pillarLayer = null;
        }

        public void Close()
        {
            // Tell the other multiplayer participants that we are gone.
            gui.DisconnectNetwork();

            GameManager.Instance.GetCurrentGame()?.Close();

            Dispose();

            Closed?.Invoke(this, System.EventArgs.Empty);
        }

        // True if the game view is in split-screen mode (several local players).
        public bool IsSplitScreen => splitScreenActive;

        public bool CanZoom => gui.CanZoom;

        // Returns the index of the local player whose part of the screen the
        // cursor is currently in (used to route the zoom input in split-screen
        // mode).
        int GetActivePlayerIndex()
        {
            if (!splitScreenActive)
                return 0;

            int playerViewWidth = VirtualScreen.Size.Width / 2;

            return Math.Min(playerZooms.Count - 1, cursorPosition.X / playerViewWidth);
        }

        // Returns the zoom of the given local player (used in split-screen mode).
        public float GetPlayerZoom(int playerIndex) => playerZooms[playerIndex];

        // Sets the zoom of the given local player (used in split-screen mode).
        public void SetPlayerZoom(int playerIndex, float zoom)
        {
            if (zoom < 0.0f)
                zoom = 0.0f;

            if (Misc.FloatEqual(zoom, playerZooms[playerIndex]))
                return;

            playerZooms[playerIndex] = zoom;

            ZoomChanged?.Invoke(this, System.EventArgs.Empty);
        }

        public float Zoom
        {
            get
            {
                // In split-screen mode the zoom is per player, so the zoom of the
                // player whose part of the screen the cursor is in is returned.
                if (splitScreenActive)
                    return playerZooms[GetActivePlayerIndex()];

                return context.Zoom;
            }
            set
            {
                // In split-screen mode the zoom input is routed to the player
                // whose part of the screen the cursor is in.
                if (splitScreenActive)
                {
                    SetPlayerZoom(GetActivePlayerIndex(), value);
                    return;
                }

                float oldZoom = context.Zoom;

                if (gui.Ingame)
                    context.Zoom = value;
                else
                    context.Zoom = 0.0f;

                if (!Misc.FloatEqual(oldZoom, context.Zoom))
                    ZoomChanged?.Invoke(this, System.EventArgs.Empty);
            }
        }

        public void ResetZoom()
        {
            if (splitScreenActive)
            {
                for (int i = 0; i < playerZooms.Count; ++i)
                    SetPlayerZoom(i, 0.0f);
            }
            else
            {
                context.Zoom = 0.0f;
            }
        }

        public bool Fullscreen
        {
            get => fullscreen;
            set
            {
                if (fullscreen == value || FullscreenRequestHandler == null)
                    return;

                if (FullscreenRequestHandler(value))
                    fullscreen = value;
            }
        }

        public event System.EventHandler ZoomChanged;

        public DataSource DataSource { get; }

        public Rect VirtualScreen { get; }

        public ISpriteFactory SpriteFactory => spriteFactory;

        public ITriangleFactory TriangleFactory => triangleFactory;

        public IColoredRectFactory ColoredRectFactory => coloredRectFactory;

        public IMinimapTextureFactory MinimapTextureFactory => minimapTextureFactory;

        public IAudioFactory AudioFactory => audioFactory;

        void SetRotation(Orientation orientation)
        {
            if (deviceType == DeviceType.Desktop ||
                sizingPolicy == SizingPolicy.FitRatioKeepOrientation ||
                sizingPolicy == SizingPolicy.FitWindowKeepOrientation)
            {
                rotation = Rotation.None;
                return;
            }

            if (orientation == Orientation.Default)
                orientation = (deviceType == DeviceType.MobilePortrait) ? Orientation.PortraitTopDown : Orientation.LandscapeLeftRight;

            if (sizingPolicy == SizingPolicy.FitRatioForcePortrait ||
                sizingPolicy == SizingPolicy.FitWindowForcePortrait)
            {
                if (orientation == Orientation.LandscapeLeftRight)
                    orientation = Orientation.PortraitTopDown;
                else if (orientation == Orientation.LandscapeRightLeft)
                    orientation = Orientation.PortraitBottomUp;
            }
            else if (sizingPolicy == SizingPolicy.FitRatioForceLandscape ||
                     sizingPolicy == SizingPolicy.FitWindowForceLandscape)
            {
                if (orientation == Orientation.PortraitTopDown)
                    orientation = Orientation.LandscapeLeftRight;
                else if (orientation == Orientation.PortraitBottomUp)
                    orientation = Orientation.LandscapeRightLeft;
            }

            switch (orientation)
            {
                case Orientation.PortraitTopDown:
                    if (deviceType == DeviceType.MobilePortrait)
                        rotation = Rotation.None;
                    else
                        rotation = Rotation.Deg90;
                    break;
                case Orientation.LandscapeLeftRight:
                    if (deviceType == DeviceType.MobilePortrait)
                        rotation = Rotation.Deg270;
                    else
                        rotation = Rotation.None;
                    break;
                case Orientation.PortraitBottomUp:
                    if (deviceType == DeviceType.MobilePortrait)
                    {
                        if (orientationPolicy == OrientationPolicy.Support180DegreeRotation)
                            rotation = Rotation.Deg180;
                        else
                            rotation = Rotation.None;
                    }
                    else
                    {
                        if (orientationPolicy == OrientationPolicy.Support180DegreeRotation)
                            rotation = Rotation.Deg270;
                        else
                            rotation = Rotation.Deg90;
                    }
                    break;
                case Orientation.LandscapeRightLeft:
                    if (deviceType == DeviceType.MobilePortrait)
                    {
                        if (orientationPolicy == OrientationPolicy.Support180DegreeRotation)
                            rotation = Rotation.Deg270;
                        else
                            rotation = Rotation.Deg90;
                    }
                    else
                    {
                        if (orientationPolicy == OrientationPolicy.Support180DegreeRotation)
                            rotation = Rotation.Deg180;
                        else
                            rotation = Rotation.None;
                    }
                    break;
            }
        }

        public void Resize(int width, int height)
        {
            switch (deviceType)
            {
                default:
                case DeviceType.Desktop:
                case DeviceType.MobileLandscape:
                    Resize(width, height, Orientation.LandscapeLeftRight);
                    break;
                case DeviceType.MobilePortrait:
                    Resize(width, height, Orientation.PortraitTopDown);
                    break;
            }
        }

        public void Resize(int width, int height, Orientation orientation)
        {
            SetRotation(orientation);

            if ((width == VirtualScreen.Size.Width &&
                height == VirtualScreen.Size.Height) ||
                sizingPolicy == SizingPolicy.FitWindow ||
                sizingPolicy == SizingPolicy.FitWindowKeepOrientation ||
                sizingPolicy == SizingPolicy.FitWindowForcePortrait ||
                sizingPolicy == SizingPolicy.FitWindowForceLandscape)
            {
                virtualScreenDisplay = new Rect(0, 0, width, height);

                sizeFactorX = 1.0f;
                sizeFactorY = 1.0f;
            }
            else
            {
                float ratio = (float)width / (float)height;
                float virtualRatio = (float)VirtualScreen.Size.Width / (float)VirtualScreen.Size.Height;

                if (rotation == Rotation.Deg90 || rotation == Rotation.Deg270)
                    virtualRatio = 1.0f / virtualRatio;

                if (Misc.FloatEqual(ratio, virtualRatio))
                {
                    virtualScreenDisplay = new Rect(0, 0, width, height);
                }
                else if (ratio < virtualRatio)
                {
                    int newHeight = Misc.Round(width / virtualRatio);
                    virtualScreenDisplay = new Rect(0, (height - newHeight) / 2, width, newHeight);
                }
                else // ratio > virtualRatio
                {
                    int newWidth = Misc.Round(height * virtualRatio);
                    virtualScreenDisplay = new Rect((width - newWidth) / 2, 0, newWidth, height);
                }

                if (rotation == Rotation.Deg90 || rotation == Rotation.Deg270)
                {
                    sizeFactorX = (float)VirtualScreen.Size.Height / (float)virtualScreenDisplay.Size.Width;
                    sizeFactorY = (float)VirtualScreen.Size.Width / (float)virtualScreenDisplay.Size.Height;
                }
                else
                {
                    sizeFactorX = (float)VirtualScreen.Size.Width / (float)virtualScreenDisplay.Size.Width;
                    sizeFactorY = (float)VirtualScreen.Size.Height / (float)virtualScreenDisplay.Size.Height;
                }
            }

            State.Gl.Viewport(virtualScreenDisplay.Position.X, virtualScreenDisplay.Position.Y,
                (uint)virtualScreenDisplay.Size.Width, (uint)virtualScreenDisplay.Size.Height);
        }

        public void AddLayer(IRenderLayer layer)
        {
            AddLayer(0, layer);
        }

        public void AddLayer(int playerIndex, IRenderLayer layer)
        {
            if (!(layer is RenderLayer))
                throw new InvalidCastException("The given layer is not valid for this renderer.");

            playerLayers[playerIndex].Add(layer.Layer, layer as RenderLayer);
        }

        public IRenderLayer GetLayer(Layer layer)
        {
            return GetLayer(0, layer);
        }

        public IRenderLayer GetLayer(int playerIndex, Layer layer)
        {
            return playerLayers[playerIndex][layer];
        }

        public void ShowLayer(Layer layer, bool show)
        {
            foreach (var layers in playerLayers)
                layers[layer].Visible = show;
        }

        public void Render()
        {
            if (disposed)
                return;

            // A split-screen game was started from the setup screen. Switch to
            // split-screen mode now (the click handling has finished).
            if (splitScreenPending)
                DoActivateSplitScreen();

            // The split-screen game was closed. Switch back to the normal
            // single-player mode (full screen menu).
            if (splitScreenDeactivatePending)
                DoDeactivateSplitScreen();

            context.SetRotation(rotation);

            State.Gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);

            if (!gui.Ingame)
            {
                // if we did not start a game the map in the background is scrolled
                if (++mapScrollTicks >= 250)
                {
                    GameManager.Instance.GetCurrentGame()?.ScrollMapRandomly();
                    mapScrollTicks = 0;
                }
            }

            bool guiVisible = false;

            foreach (var layers in playerLayers)
            {
                if (layers[Layer.Gui].Visible)
                {
                    guiVisible = true;
                    break;
                }
            }

            if (guiVisible)
            {
                // TODO gui.Draw draws/prepares everything at the moment
                // TODO separate rendering from game logic/update
                gui.Draw(); // this will prepare gui components for rendering
                gui.DrawCursor(cursorPosition.X, cursorPosition.Y);
            }

            if (splitScreenActive)
            {
                int playerViewWidth = VirtualScreen.Size.Width / 2;

                for (int i = 0; i < playerLayers.Count; ++i)
                {
                    // Each player's part of the screen is zoomed around its own
                    // center (the gui layers are not zoomed, they use the
                    // unzoomed model view matrix).
                    float centerX = i * playerViewWidth + playerViewWidth * 0.5f;
                    float centerY = VirtualScreen.Size.Height * 0.5f;

                    context.SetZoom(playerZooms[i], centerX, centerY);

                    // Clip the player's rendering to his part of the screen so it
                    // does not bleed into the other player's part (e.g. when
                    // zooming).
                    SetPlayerScissor(i, playerViewWidth);

                    foreach (var layer in playerLayers[i])
                        layer.Value.Render();
                }

                context.DisableScissor();

                // The decorative pillar at the boundary is drawn on a shared layer
                // which is rendered unscissored so it can span both players' parts
                // and be centered on the boundary.
                if (pillarLayer != null)
                    pillarLayer.Render();
            }
            else
            {
                foreach (var layer in playerLayers[0])
                    layer.Value.Render();
            }
        }

        // Clips the rendering to the given player's part of the screen (used in
        // split-screen mode). The scissor rect is in window coordinates (origin at
        // the bottom-left, like the viewport).
        void SetPlayerScissor(int playerIndex, int playerViewWidth)
        {
            int viewportX = virtualScreenDisplay.Position.X;
            int viewportY = virtualScreenDisplay.Position.Y;
            int viewportWidth = virtualScreenDisplay.Size.Width;
            int viewportHeight = virtualScreenDisplay.Size.Height;

            int x, y, width, height;

            switch (rotation)
            {
                case Rotation.Deg90:
                    // The left half of the virtual screen is the top half of the viewport.
                    width = viewportWidth;
                    height = viewportHeight / 2;
                    x = viewportX;
                    y = viewportY + ((playerIndex == 0) ? height : 0);
                    break;
                case Rotation.Deg180:
                    // The left half of the virtual screen is the right half of the viewport.
                    width = viewportWidth / 2;
                    height = viewportHeight;
                    x = viewportX + ((playerIndex == 0) ? width : 0);
                    y = viewportY;
                    break;
                case Rotation.Deg270:
                    // The left half of the virtual screen is the bottom half of the viewport.
                    width = viewportWidth;
                    height = viewportHeight / 2;
                    x = viewportX;
                    y = viewportY + ((playerIndex == 0) ? 0 : height);
                    break;
                case Rotation.None:
                default:
                    {
                        float scaleX = (float)viewportWidth / (float)VirtualScreen.Size.Width;
                        width = Misc.Round(playerViewWidth * scaleX);
                        height = viewportHeight;
                        x = viewportX + Misc.Round(playerIndex * playerViewWidth * scaleX);
                        y = viewportY;
                    }
                    break;
            }

            context.SetScissorRect(x, y, width, height);
        }

        public void SetCursorPosition(int x, int y)
        {
            cursorPosition.X = x;
            cursorPosition.Y = y;

            cursorPosition = ScreenToView(cursorPosition);

            if (cursorPosition == null)
                cursorPosition = lastCursorPosition;
            else
                lastCursorPosition = cursorPosition;
        }

        public Position ScreenToView(Position position)
        {
            position.X = Math.Min(virtualScreenDisplay.Right, Math.Max(virtualScreenDisplay.Left, position.X));
            position.Y = Math.Min(virtualScreenDisplay.Bottom, Math.Max(virtualScreenDisplay.Top, position.Y));

            int relX = position.X - virtualScreenDisplay.Left;
            int relY = position.Y - virtualScreenDisplay.Top;
            int rotatedX;
            int rotatedY;

            switch (rotation)
            {
                case Rotation.None:
                default:
                    rotatedX = relX;
                    rotatedY = relY;
                    break;
                case Rotation.Deg90:
                    rotatedX = relY;
                    rotatedY = virtualScreenDisplay.Size.Width - relX;
                    break;
                case Rotation.Deg180:
                    rotatedX = virtualScreenDisplay.Size.Width - relX;
                    rotatedY = virtualScreenDisplay.Size.Height - relY;
                    break;
                case Rotation.Deg270:
                    rotatedX = virtualScreenDisplay.Size.Height - relY;
                    rotatedY = relX;
                    break;
            }

            int x = Misc.Round(sizeFactorX * rotatedX);
            int y = Misc.Round(sizeFactorY * rotatedY);

            return new Position(x, y);
        }

        public Size ScreenToView(Size size)
        {
            bool swapDimensions = rotation == Rotation.Deg90 || rotation == Rotation.Deg270;

            int width = (swapDimensions) ? size.Height : size.Width;
            int height = (swapDimensions) ? size.Width : size.Height;

            return new Size(Misc.Round(sizeFactorX * width), Misc.Round(sizeFactorY * height));
        }

        public Rect ScreenToView(Rect rect)
        {
            var clippedRect = new Rect(rect);

            clippedRect.Clip(virtualScreenDisplay);

            if (clippedRect.Empty)
                return null;

            var position = ScreenToView(clippedRect.Position);
            var size = ScreenToView(clippedRect.Size);

            return new Rect(position, size);
        }

        bool RunHandler(EventHandler handler, EventArgs args)
        {
            bool? handlerResult = handler?.Invoke(this, args);

            if (handlerResult.HasValue)
                args.Done = handlerResult.Value;

            return args.Done;
        }

        public bool NotifyClick(int x, int y, Button button, bool delayed)
        {
            // transform from screen to view
            var position = ScreenToView(new Position(x, y));

            if (position == null)
                return false;

            return RunHandler(Click, new EventArgs(delayed ? EventType.DelayedClick : EventType.Click, position.X, position.Y, 0, 0, button));
        }

        public bool NotifyDoubleClick(int x, int y, Button button)
        {
            // transform from screen to view
            var position = ScreenToView(new Position(x, y));

            if (position == null)
                return false;

            return RunHandler(DoubleClick, new EventArgs(EventType.DoubleClick, position.X, position.Y, 0, 0, button));
        }

        // Returns true if the wheel was used by the gui (e.g. to scroll a list).
        public bool NotifyWheel(int x, int y, int steps)
        {
            // transform from screen to view
            var position = ScreenToView(new Position(x, y));

            if (position == null)
                return false;

            return RunHandler(Wheel, new EventArgs(EventType.Wheel, position.X, position.Y, 0, steps, Button.None));
        }

        public bool NotifySpecialClick(int x, int y)
        {
            // transform from screen to view
            var position = ScreenToView(new Position(x, y));

            if (position == null)
                return false;

            // The special click is mapped to a double click with left mouse button
            return RunHandler(SpecialClick, new EventArgs(EventType.SpecialClick, position.X, position.Y, 0, 0, Button.Left));
        }

        public bool NotifyDrag(int x, int y, int dx, int dy, Button button)
        {
            // transform from screen to view
            var position = ScreenToView(new Position(x, y));
            var delta = ScreenToView(new Size(dx, dy));

            if (position == null)
                position = new Position();

            return RunHandler(Drag, new EventArgs(EventType.Drag, position.X, position.Y, delta.Width, delta.Height, button));
        }

        public bool NotifyStopDrag()
        {
            return RunHandler(StopDrag, new EventArgs(EventType.StopDrag, 0, 0, 0, 0));
        }

        public bool NotifyKeyPressed(char key, byte modifier)
        {
            return RunHandler(KeyPress, new EventArgs(EventType.KeyPressed, 0, 0, (byte)key, modifier));
        }

        public bool NotifySystemKeyPressed(SystemKey key, byte modifier)
        {
            return RunHandler(SystemKeyPress, new EventArgs(EventType.SystemKeyPressed, 0, 0, (int)key, modifier));
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Remove the viewer from the game manager so it no longer
                    // receives game events for this (now disposed) render view.
                    gui.DestroyViewer();

                    foreach (var layers in playerLayers)
                    {
                        foreach (var layer in layers.Values)
                            layer?.Dispose();

                        layers.Clear();
                    }

                    playerLayers.Clear();

                    pillarLayer?.Dispose();
                    pillarLayer = null;

                    // Free the audio (BASS) so playback stops when the game
                    // view is closed. On Android the process stays alive after
                    // the activity is finished, so without this the music
                    // keeps playing in the background.
                    audioFactory?.Dispose();

                    disposed = true;
                }
            }
        }

        public void UpdateNetworkEvents()
        {
            gui.NetworkDataReceiver = NetworkDataReceiver;
            gui.UpdateNetworkEvents();
        }

        public void DisconnectNetwork()
        {
            gui.DisconnectNetwork();
        }
    }
}
