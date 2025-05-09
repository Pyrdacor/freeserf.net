/*
 * MainWindow.cs - Main game window
 *
 * Copyright (C) 2019-2025  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.IO;
using System.Linq;
using System.Numerics;
using Freeserf.Renderer;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Window;
using Silk.NET.Windowing;

namespace Freeserf
{
    class MainWindow : Silk.NET.Window.Window, IDisposable
    {
        enum MouseButtonIndex
        {
            Left = 0,
            Middle = 1,
            Right = 2
        }

        static MainWindow mainWindow = null;
        GameView gameView = null;
        readonly Network.INetworkDataReceiver networkDataReceiver;
        bool fullscreen = false;
        readonly bool[] pressedMouseButtons = new bool[3];
        readonly bool[] keysDown;
        int lastDragX = int.MinValue;
        int lastDragY = int.MinValue;
        static Global.InitInfo initInfo = null;
        static Data.DataSource dataSource = null;
        bool scrolled = false;
        Vector2D<int> clickPosition = Vector2D<int>.Zero;

        private MainWindow(WindowOptions options)
            : base(options)
        {
            var keyMaxValue = Enum.GetValues(typeof(Key)).Cast<int>().Max();
            keysDown = new bool[keyMaxValue + 1];
            networkDataReceiver = new Network.NetworkDataReceiverFactory().CreateReceiver();
            Load += MainWindow_Load;
        }

        private int Width
        {
            get => Size.X;
            set => Size = new Vector2D<int>(value, Size.Y);
        }
        private int Height
        {
            get => Size.Y;
            set => Size = new Vector2D<int>(Size.X, value);
        }


        public static MainWindow Create(string[] args)
        {
            if (mainWindow != null)
                throw new ExceptionFreeserf(ErrorSystemType.Application, "A main window can not be created twice.");

            string logDirectory = string.Empty;

            try
            {
#if !DEBUG
                logDirectory = FileSystem.Paths.IsWindows() ? Program.ExecutablePath : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/freeserf";
                string logPath = Path.Combine(logDirectory, UserConfig.DefaultLogFile);
                Directory.CreateDirectory(logDirectory);
                Log.SetStream(new LogFileStream(logPath));
                Log.MaxSize = UserConfig.DefaultMaxLogSize;
#else
                Log.MaxSize = null; // Console output is not limited
#endif
                Log.SetLevel(UserConfig.DefaultLogLevel);
            }
            catch (IOException ex)
            {
                // Logging not possible. We can just write to a console here.
                Console.WriteLine("Error initializing logging: " + ex.Message);
            }

            initInfo = Global.Init(args);

            try
            {
                Network.Network.DefaultClientFactory = new Network.ClientFactory();
                Network.Network.DefaultServerFactory = new Network.ServerFactory();

                UserConfig.Load(FileSystem.Paths.UserConfigPath);

#if !DEBUG
                if (initInfo.ConsoleWindow || UserConfig.Logging.LogToConsole)
                {
                    Log.SetStream(Console.OpenStandardOutput());
                    Log.MaxSize = null; // Console output is not limited
                }
                else
                {
                    string logFile = string.IsNullOrWhiteSpace(UserConfig.Logging.LogFileName) ? UserConfig.DefaultLogFile : UserConfig.Logging.LogFileName;

                    if (!Path.IsPathRooted(logFile))
                        logFile = Path.Combine(logDirectory, logFile);

                    Log.SetStream(new LogFileStream(logFile));
                    Log.MaxSize = UserConfig.Logging.MaxLogSize;
                }

                if (!initInfo.LogLevelSet && UserConfig.Logging.LogLevel != Log.LogLevel)
                    Log.SetLevel(UserConfig.Logging.LogLevel);
#endif

                var data = Data.Data.GetInstance();
                string dataPath = Program.ExecutablePath;

                if (!data.Load(dataPath, UserConfig.Game.GraphicDataUsage, UserConfig.Game.SoundDataUsage, UserConfig.Game.MusicDataUsage))
                {
                    Log.Error.Write(ErrorSystemType.Data, "Error loading game data.");
                    return null;
                }

                dataSource = data.GetDataSource();

                if (UserConfig.Video.ResolutionWidth < 640)
                    UserConfig.Video.ResolutionWidth = 640;
                if (UserConfig.Video.ResolutionWidth > Global.MAX_VIRTUAL_SCREEN_WIDTH)
                    UserConfig.Video.ResolutionWidth = Global.MAX_VIRTUAL_SCREEN_WIDTH;
                if (UserConfig.Video.ResolutionHeight < 480)
                    UserConfig.Video.ResolutionHeight = 480;
                if (UserConfig.Video.ResolutionHeight > Global.MAX_VIRTUAL_SCREEN_HEIGHT)
                    UserConfig.Video.ResolutionHeight = Global.MAX_VIRTUAL_SCREEN_HEIGHT;

                if (initInfo.ScreenWidth == -1)
                    initInfo.ScreenWidth = UserConfig.Video.ResolutionWidth;
                if (initInfo.ScreenHeight == -1)
                    initInfo.ScreenHeight = UserConfig.Video.ResolutionHeight;
                if (initInfo.Fullscreen == null)
                    initInfo.Fullscreen = UserConfig.Video.Fullscreen;

                if (initInfo.ScreenWidth < 640)
                    initInfo.ScreenWidth = 640;
                if (initInfo.ScreenHeight < 480)
                    initInfo.ScreenHeight = 480;

                /*float ratio = (float)initInfo.ScreenWidth / (float)initInfo.ScreenHeight;
                bool reducedWidth = false;
                bool reducedHeight = false;

                var screen = Screen.FromHandle(Handle);

                if (initInfo.ScreenWidth > screen.Bounds.Width)
                {
                    initInfo.ScreenWidth = screen.Bounds.Width;
                    reducedWidth = true;
                }

                if (initInfo.ScreenHeight > screen.Bounds.Height)
                {
                    initInfo.ScreenHeight = screen.Bounds.Height;
                    reducedHeight = true;
                }

                if (reducedHeight)
                {
                    initInfo.ScreenWidth = Misc.Round(initInfo.ScreenHeight * ratio);

                    if (initInfo.ScreenWidth > screen.Bounds.Width)
                    {
                        initInfo.ScreenWidth = screen.Bounds.Width;
                        reducedWidth = true;
                    }
                }

                if (reducedWidth)
                {
                    initInfo.ScreenHeight = Misc.Round(initInfo.ScreenWidth / ratio);
                }*/

                UserConfig.Video.ResolutionWidth = initInfo.ScreenWidth;
                UserConfig.Video.ResolutionHeight = initInfo.ScreenHeight;
                UserConfig.Video.Fullscreen = initInfo.Fullscreen.Value;

                var options = new WindowOptions(
                    true,
                    new Vector2D<int>(20, 40),
                    new Vector2D<int>(initInfo.ScreenWidth, initInfo.ScreenHeight),
                    50.0,
                    50.0,
                    GraphicsAPI.Default,
                    Global.VERSION,
                    WindowState.Normal,
                    WindowBorder.Fixed,
                    true,
                    false,
                    new VideoMode(),
                    24
                );

                return mainWindow = new MainWindow(options);
            }
            catch (Exception ex)
            {
                ReportException("Init", ex);
                return null;
            }
        }

        private void MainWindow_Load()
        {
            try
            {
                State.Init(this);
                gameView = new(dataSource, new Size(initInfo.ScreenWidth, initInfo.ScreenHeight),
                    DeviceType.Desktop, SizingPolicy.FitRatio, OrientationPolicy.Fixed);
                gameView.FullscreenRequestHandler = FullscreenRequestHandler;
                gameView.Resize(Width, Height);

                if (initInfo.Fullscreen == true)
                    SetFullscreen(true);

                CursorVisible = false; // hide cursor
                DoubleClickTime = 300;

                gameView.Closed += GameView_Closed;
                Closing += MainWindow_Closing;
                Render += MainWindow_Render;
                Update += MainWindow_Update;
                Resize += MainWindow_Resize;
                StateChanged += MainWindow_StateChanged;

                MakeCurrent();
            }
            catch (Exception ex)
            {
                ReportException("Load", ex);
            }
        }

        private static void Exit()
        {
            mainWindow?.Close();
        }

        private void GameView_Closed(object sender, EventArgs e)
        {
            if (gameView != null)
            {
                gameView = null;
                Exit();
            }
        }

        protected void MainWindow_Closing()
        {
            // TODO: Ask for saving?

            // TODO
            //if (debugConsole != null)
            //    debugConsole.Close();

            if (gameView != null)
            {
                var view = gameView;
                gameView = null;
                view.Close();
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FileSystem.Paths.UserConfigPath));
                UserConfig.Save(FileSystem.Paths.UserConfigPath);
            }
            catch
            {
                // ignore
            }
        }

        static void ReportException(string source, Exception exception)
        {
            Log.Error.Write(ErrorSystemType.Application, $"{source}: {exception.Message}");

            mainWindow?.SetFullscreen(false, false);

            // TODO: how to implement crash handle window?
            //if (crashHandlerForm.RaiseException(exception) == UI.CrashReaction.Restart)
            //    Process.Start(Assembly.GetEntryAssembly().Location);

            Exit();
        }

        bool FullscreenRequestHandler(bool fullscreen)
        {
            if (this.fullscreen != fullscreen)
            {
                this.fullscreen = fullscreen;

                WindowState = fullscreen ? WindowState.Fullscreen : WindowState.Normal;
            }

            return true;
        }

        bool SetFullscreen(bool fullscreen, bool save = true)
        {
            if (this.fullscreen == fullscreen)
                return false;

            if (gameView == null)
                return false;

            gameView.Fullscreen = fullscreen;

            if (save)
                UserConfig.Video.Fullscreen = gameView.Fullscreen;

            return gameView.Fullscreen == fullscreen; // dit it work?
        }

        bool ToggleFullscreen()
        {
            return SetFullscreen(!fullscreen);
        }

        void ZoomIn()
        {
            if (gameView.Zoom < 4.0f)
                gameView.Zoom += 0.5f;
        }

        void ZoomOut()
        {
            if (gameView.Zoom > 0.0f)
                gameView.Zoom -= 0.5f;
        }

        private void MainWindow_Resize(Vector2D<int> size)
        {
            gameView.Resize(size.X, size.Y);
        }

        private void MainWindow_Render(double delta)
        {
            if (!Initialized)
                return;

            try
            {
                gameView?.Render();
            }
            catch (Exception ex)
            {
                ReportException("Render", ex);
                return;
            }

            SwapBuffers();
        }

        private void MainWindow_Update(double delta)
        {
            if (gameView != null)
            {
                gameView.NetworkDataReceiver = networkDataReceiver;
                gameView.UpdateNetworkEvents();
            }
        }

        private float? preMinimizedVolume = null;

        private void MainWindow_StateChanged(WindowState state)
        {
            // Mute audio while minimized
            if (state == WindowState.Minimized)
            {
                var volumeControl = gameView.AudioFactory.GetAudio()?.GetVolumeController();

                preMinimizedVolume = volumeControl?.Volume;
                volumeControl?.SetVolume(0.0f);
            }
            else if (preMinimizedVolume != null)
            {
                var volumeControl = gameView.AudioFactory.GetAudio()?.GetVolumeController();

                preMinimizedVolume = null;
                volumeControl?.SetVolume(preMinimizedVolume.Value);
            }

            if (state == WindowState.Maximized)
            {
                SetFullscreen(true, true);
                return;
            }
        }

        static Event.Button ConvertMouseButtons(MouseButtons buttons)
        {
            if (buttons.HasFlag(MouseButtons.Left))
                return Event.Button.Left;
            else if (buttons.HasFlag(MouseButtons.Right))
                return Event.Button.Right;
            else if (buttons.HasFlag(MouseButtons.Middle))
                return Event.Button.Middle;
            else
                return Event.Button.None;
        }

        protected override void OnKeyUp(Key key, KeyModifiers modifiers)
        {
            keysDown[(int)key] = false;

            base.OnKeyUp(key, modifiers);
        }

        protected override void OnKeyDown(Key key, KeyModifiers modifiers)
        {
            try
            {
                keysDown[(int)key] = true;

                if (modifiers.HasFlag(KeyModifiers.Control) && key == Key.F)
                {
                    ToggleFullscreen();
                    return;
                }

                switch (key)
                {
                    case Key.Left:
                        gameView?.NotifySystemKeyPressed(Event.SystemKey.Left, 0);
                        break;
                    case Key.Right:
                        gameView?.NotifySystemKeyPressed(Event.SystemKey.Right, 0);
                        break;
                    case Key.Up:
                        gameView?.NotifySystemKeyPressed(Event.SystemKey.Up, 0);
                        break;
                    case Key.Down:
                        gameView?.NotifySystemKeyPressed(Event.SystemKey.Down, 0);
                        break;
                    case Key.PageUp:
                        gameView?.NotifySystemKeyPressed(Event.SystemKey.PageUp, (byte)modifiers);
                        break;
                    case Key.PageDown:
                        gameView?.NotifySystemKeyPressed(Event.SystemKey.PageDown, (byte)modifiers);
                        break;
                    case Key.Escape:
                        gameView?.NotifySystemKeyPressed(Event.SystemKey.Escape, 0);
                        break;
                    case Key.F5:
                        gameView?.NotifySystemKeyPressed(Event.SystemKey.F5, 0);
                        break;
                    case Key.F6:
                        gameView?.NotifySystemKeyPressed(Event.SystemKey.F6, 0);
                        break;
                    case Key.F11:
                        ToggleFullscreen();
                        break;
                    case Key.Enter:
                        gameView?.NotifyKeyPressed(Event.SystemKeys.Return, (byte)modifiers);
                        break;
                    case Key.Backspace:
                        gameView?.NotifyKeyPressed(Event.SystemKeys.Backspace, (byte)modifiers);
                        break;
                    case Key.Delete:
                        gameView?.NotifyKeyPressed(Event.SystemKeys.Delete, (byte)modifiers);
                        break;
                    case Key.Tab:
                        gameView?.NotifyKeyPressed(Event.SystemKeys.Tab, (byte)modifiers);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                ReportException("KeyDown", ex);
            }

            base.OnKeyDown(key, modifiers);
        }

        protected override void OnKeyChar(char character, KeyModifiers modifiers)
        {
            try
            {
                if (character >= 32 && character < 128)
                    gameView?.NotifyKeyPressed(character, (byte)modifiers);

                switch (character)
                {
                    case '<':
                        if (gameView.CanZoom)
                            ZoomOut();
                        else
                            gameView?.NotifyKeyPressed(character, (byte)modifiers);
                        break;
                    case '>':
                        if (gameView.CanZoom)
                            ZoomIn();
                        else
                            gameView?.NotifyKeyPressed(character, (byte)modifiers);
                        break;
                    case 'ä':
                    case 'Ä':
                    case 'ö':
                    case 'Ö':
                    case 'ü':
                    case 'Ü':
                        gameView?.NotifyKeyPressed(character, (byte)modifiers);
                        break;
                }
            }
            catch (Exception ex)
            {
                ReportException("KeyPress", ex);
            }

            base.OnKeyChar(character, modifiers);
        }

        protected override void OnMouseMoveDelta(Vector2D<int> position, MouseButtons buttons, Vector2D<int> delta)
        {
            if (gameView == null)
                return;

            try
            {
                if (delta.X == 0 && delta.Y == 0)
                    return;

                UpdateMouseState(buttons);

                if (buttons.HasFlag(MouseButtons.Right))
                {
                    if (lastDragX == int.MinValue)
                        return;

                    bool dragAllowed = gameView.NotifyDrag(position.X, position.Y, lastDragX - position.X, lastDragY - position.Y, ConvertMouseButtons(buttons));

                    // lock the mouse if dragging with right button
                    if (dragAllowed)
                    {
                        scrolled = true;
                    }
                    else if (buttons.HasFlag(MouseButtons.Left))
                    {
                        gameView.SetCursorPosition(position.X, position.Y);
                    }

                    lastDragX = position.X;
                    lastDragY = position.Y;
                }
                else
                {
                    lastDragX = int.MinValue;
                    lastDragY = int.MinValue;
                    gameView.SetCursorPosition(position.X, position.Y);
                }
            }
            catch (Exception ex)
            {
                ReportException("MouseMove", ex);
            }

            base.OnMouseMoveDelta(position, buttons, delta);
        }

        protected override void OnMouseUp(Vector2D<int> position, MouseButtons button)
        {
            UpdateMouseState(button, false);

            // restore cursor from successful locked dragging
            if (button.HasFlag(MouseButtons.Right))
            {
                lastDragX = int.MinValue;
                lastDragY = int.MinValue;
                CursorMode = CursorVisible ? CursorMode.Normal : CursorMode.Hidden;
                if (scrolled && UserConfig.Game.Options.HasFlag(Option.ResetCursorAfterScrolling))
                {
                    CursorPosition = new Vector2(Width / 2, Height / 2);
                    gameView.SetCursorPosition(Width / 2, Height / 2);
                }
                scrolled = false;                
                gameView.NotifyStopDrag();                
            }

            base.OnMouseUp(position, button);
        }

        protected override void OnMouseDown(Vector2D<int> position, MouseButtons button)
        {
            UpdateMouseState(button, true);

            clickPosition = position;

            // left + right = special click
            if (button.HasFlag(MouseButtons.Left) || button.HasFlag(MouseButtons.Right))
            {
                try
                {
                    if (pressedMouseButtons[(int)MouseButtonIndex.Left] &&
                        pressedMouseButtons[(int)MouseButtonIndex.Right])
                    {
                        gameView?.NotifySpecialClick(position.X, position.Y);
                    }
                    else
                    {
                        lastDragX = position.X;
                        lastDragY = position.Y;

                        if (button.HasFlag(MouseButtons.Right))
                            CursorMode = CursorMode.Disabled;

                        gameView?.NotifyClick(position.X, position.Y, ConvertMouseButtons(button), false);
                    }
                }
                catch (Exception ex)
                {
                    ReportException("MouseDown", ex);
                }
            }

            base.OnMouseDown(position, button);
        }

        protected override void OnClick(Vector2D<int> position, MouseButtons button)
        {
            // left + right = special click
            if (button.HasFlag(MouseButtons.Left) || button.HasFlag(MouseButtons.Right))
            {
                if (
                    pressedMouseButtons[(int)MouseButtonIndex.Left] &&
                    pressedMouseButtons[(int)MouseButtonIndex.Right]
                )
                return; // special clicks are handled in OnMouseDown
            }

            try
            {
                gameView?.NotifyClick(clickPosition.X, clickPosition.Y, ConvertMouseButtons(button), true);
            }
            catch (Exception ex)
            {
                ReportException("MouseClick", ex);
            }

            base.OnClick(position, button);
        }

        protected override void OnDoubleClick(Vector2D<int> position, MouseButtons button)
        {
            try
            {
                if (button == MouseButtons.Left || button == MouseButtons.Right)
                    gameView?.NotifyDoubleClick(position.X, position.Y, ConvertMouseButtons(button));
            }
            catch (Exception ex)
            {
                ReportException("MouseDoubleClick", ex);
            }

            base.OnDoubleClick(position, button);
        }

        protected override void OnMouseWheel(Vector2D<int> position, float delta)
        {
            try
            {
                if (delta < 0)
                    ZoomOut();
                else if (delta > 0)
                    ZoomIn();
            }
            catch (Exception ex)
            {
                ReportException("MouseWheel", ex);
            }

            base.OnMouseWheel(position, delta);
        }

        void UpdateMouseState(MouseButtons buttons, bool? pressed = null)
        {
            if (pressed.HasValue)
            {
                if (buttons.HasFlag(MouseButtons.Left))
                    pressedMouseButtons[(int)MouseButtonIndex.Left] = pressed.Value;
                if (buttons.HasFlag(MouseButtons.Right))
                    pressedMouseButtons[(int)MouseButtonIndex.Right] = pressed.Value;
                if (buttons.HasFlag(MouseButtons.Middle))
                    pressedMouseButtons[(int)MouseButtonIndex.Middle] = pressed.Value;
            }
            else
            {
                pressedMouseButtons[(int)MouseButtonIndex.Left] = buttons.HasFlag(MouseButtons.Left);
                pressedMouseButtons[(int)MouseButtonIndex.Right] = buttons.HasFlag(MouseButtons.Right);
                pressedMouseButtons[(int)MouseButtonIndex.Middle] = buttons.HasFlag(MouseButtons.Middle);
            }
        }

        protected override void OnMouseEnter()
        {
            //Cursor = MouseCursor.Empty;
            // CursorVisible = false;
        }

        protected override void OnMouseLeave()
        {
            //Cursor = MouseCursor.Default;
            // CursorVisible = true;
        }


#region IDisposable

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    gameView?.Dispose();

                    // TODO: dispose managed resources
                }

                // TODO: dispose unmanaged resources

                disposed = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

#endregion

    }
}
