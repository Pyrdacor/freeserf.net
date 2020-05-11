/*
 * GameView.cs - Implementation of a OpenTK-based render view
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

    public class GameView : RenderLayerFactory, IRenderView, IAudioInterface, IDisposable
    {
        // these two lines are fore the background map at start
        int mapScrollTicks = 0;
        readonly Random mapScrollRandom = new Random();

        bool disposed = false;
        Context context;
        Rect virtualScreenDisplay;
        readonly SizingPolicy sizingPolicy;
        readonly OrientationPolicy orientationPolicy;
        readonly DeviceType deviceType;
        readonly bool isLandscapeRatio = true;
        Rotation rotation = Rotation.None;
        readonly SortedDictionary<Layer, RenderLayer> layers = new SortedDictionary<Layer, RenderLayer>();
        readonly SpriteFactory spriteFactory = null;
        readonly TriangleFactory triangleFactory = null;
        readonly ColoredRectFactory coloredRectFactory = null;
        readonly MinimapTextureFactory minimapTextureFactory = null;
        readonly AudioFactory audioFactory = null;
        readonly UI.Gui gui = null;
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
        public FullscreenRequestHandler FullscreenRequestHandler { get; set; }

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

            foreach (Layer layer in Enum.GetValues(typeof(Layer)))
            {
                if (layer == Layer.None)
                    continue;

                // TODO: color keys?

                try
                {
                    var texture = (layer == Layer.Minimap) ? minimapTextureFactory.GetMinimapTexture() :
                        textureAtlas.GetOrCreate(layer).Texture as Texture;

                    var renderLayer = Create(layer, texture,
                        layer == Layer.Gui, // only the gui supports colored rects
                        null, // no color key for now
                        layer == Layer.GuiFont ? new Render.Color(115, 179, 67, 255) : null); // UI font uses green color overlay

                    if (layer == Layer.Gui || layer == Layer.GuiBuildings || layer == Layer.Minimap)
                    {
                        // the gui needs scaling
                        renderLayer.PositionTransformation = (Position position) =>
                        {
                            float factorX = (float)VirtualScreen.Size.Width / 640.0f;
                            float factorY = (float)VirtualScreen.Size.Height / 480.0f;

                            return new Position(Misc.Round(position.X * factorX), Misc.Round(position.Y * factorY));
                        };

                        renderLayer.SizeTransformation = (Size size) =>
                        {
                            float factorX = (float)VirtualScreen.Size.Width / 640.0f;
                            float factorY = (float)VirtualScreen.Size.Height / 480.0f;

                            // don't scale a dimension of 0
                            int width = (size.Width == 0) ? 0 : Misc.Round(size.Width * factorX);
                            int height = (size.Height == 0) ? 0 : Misc.Round(size.Height * factorY);

                            return new Size(width, height);
                        };
                    }
                    else if (layer == Layer.GuiFont) // UI Font needs different scaling
                    {
                        renderLayer.PositionTransformation = (Position position) =>
                        {
                            float factorX = (float)VirtualScreen.Size.Width / 640.0f;
                            float factorY = (float)VirtualScreen.Size.Height / 480.0f;

                            return new Position(Misc.Round(position.X * factorX), Misc.Round(position.Y * factorY));
                        };

                        renderLayer.SizeTransformation = (Size size) =>
                        {
                            // The UI expects 8x8 characters but we may use different sizes.
                            // So we adjust the scale factors accordingly.
                            float factorX = (8.0f / Global.UIFontCharacterWidth) * (float)VirtualScreen.Size.Width / 640.0f;
                            float factorY = (8.0f / Global.UIFontCharacterHeight) * (float)VirtualScreen.Size.Height / 480.0f;

                            // don't scale a dimension of 0
                            int width = (size.Width == 0) ? 0 : Misc.Round(size.Width * factorX);
                            int height = (size.Height == 0) ? 0 : Misc.Round(size.Height * factorY);

                            return new Size(width, height);
                        };
                    }

                    renderLayer.Visible = true;

                    AddLayer(renderLayer);
                }
                catch (Exception ex)
                {
                    throw new ExceptionFreeserf(ErrorSystemType.Render, $"Unable to create layer '{layer.ToString()}': {ex.Message}");
                }
            }

            gui = new UI.Gui(this, this);
        }

        public void Close()
        {
            var game = GameManager.Instance.GetCurrentGame();

            if (game != null)
                game.Close();

            Dispose();

            Closed?.Invoke(this, System.EventArgs.Empty);
        }

        public float Zoom
        {
            get => context.Zoom;
            set
            {
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
            context.Zoom = 0.0f;
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
            if (!(layer is RenderLayer))
                throw new InvalidCastException("The given layer is not valid for this renderer.");

            layers.Add(layer.Layer, layer as RenderLayer);
        }

        public IRenderLayer GetLayer(Layer layer)
        {
            return layers[layer];
        }

        public void ShowLayer(Layer layer, bool show)
        {
            layers[layer].Visible = show;
        }

        public void Render()
        {
            if (disposed)
                return;

            context.SetRotation(rotation);

            State.Gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);

            if (!gui.Ingame)
            {
                // if we did not start a game the map in the background is scrolled
                if (++mapScrollTicks >= 80)
                {
                    var game = GameManager.Instance.GetCurrentGame();

                    if (game != null && game.Map != null)
                        game.Map.ScrollTo(mapScrollRandom.Next() % game.Map.Columns, mapScrollRandom.Next() % game.Map.Rows);

                    mapScrollTicks = 0;
                }
            }

            if (layers[Layer.Gui].Visible)
            {
                // TODO gui.Draw draws/prepares everything at the moment
                // TODO separate rendering from game logic/update
                gui.Draw(); // this will prepare gui components for rendering
                gui.DrawCursor(cursorPosition.X, cursorPosition.Y);
            }

            foreach (var layer in layers)
                layer.Value.Render();
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
            if (!virtualScreenDisplay.Contains(position))
                return null;

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
            bool? h = handler?.Invoke(this, args);

            if (h.HasValue)
                args.Done = h.Value;

            return args.Done;
        }

        public bool NotifyClick(int x, int y, Button button)
        {
            // transform from screen to view
            var position = ScreenToView(new Position(x, y));

            if (position == null)
                return false;

            return RunHandler(Click, new EventArgs(EventType.Click, position.X, position.Y, 0, 0, button));
        }

        public bool NotifyDoubleClick(int x, int y, Button button)
        {
            // transform from screen to view
            var position = ScreenToView(new Position(x, y));

            if (position == null)
                return false;

            return RunHandler(DoubleClick, new EventArgs(EventType.DoubleClick, position.X, position.Y, 0, 0, button));
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
                    foreach (var layer in layers.Values)
                        layer?.Dispose();

                    layers.Clear();

                    disposed = true;
                }
            }
        }
    }
}
