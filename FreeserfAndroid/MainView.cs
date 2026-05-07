/*
 * MainView.cs - Main game window
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

using Freeserf;
using Freeserf.Renderer;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System;
using System.IO;
using System.Linq;


namespace FreeserfAndroid
{
    class MainView : Window, IDisposable
    {
        enum MouseButtonIndex
        {
            Left = 0,
            Middle = 1,
            Right = 2
        }

        static MainView mainWindow = null;
        GameView gameView = null;
        readonly Freeserf.Network.INetworkDataReceiver networkDataReceiver;
        bool fullscreen = false;
        readonly bool[] pressedMouseButtons = new bool[3];
        readonly bool[] keysDown;
        int lastDragX = int.MinValue;
        int lastDragY = int.MinValue;
        static Global.InitInfo initInfo = null;
        static Freeserf.Data.DataSource dataSource = null;
        bool scrolled = false;
        Vector2D<int> clickPosition = Vector2D<int>.Zero;

        private MainView() : base()
        {
            var keyMaxValue = Enum.GetValues(typeof(Key)).Cast<int>().Max();
            keysDown = new bool[keyMaxValue + 1];
            networkDataReceiver = new Freeserf.Network.NetworkDataReceiverFactory().CreateReceiver();
            Load += MainWindow_Load;
        }

        public static MainView Create(string[] args)
        {
            if (mainWindow != null)
            {
                return mainWindow;
            }

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
                Freeserf.Network.Network.DefaultClientFactory = new Freeserf.Network.ClientFactory();
                Freeserf.Network.Network.DefaultServerFactory = new Freeserf.Network.ServerFactory();

                UserConfig.Load(Freeserf.FileSystem.Paths.UserConfigPath);

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

                var data = Freeserf.Data.Data.GetInstance();

                using var input = FileManager.OpenStream("SPAE.PA");
                var dataPath = Path.Combine(Path.GetTempPath(), "SPAE.PA");
                using var output = System.IO.File.Create(dataPath);
                input.CopyTo(output);
                output.Position = 0;
                output.Close();
                
                dataPath = Path.GetDirectoryName(dataPath);

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

                if (initInfo.MonitorIndex == -1)
                    initInfo.MonitorIndex = UserConfig.Video.MonitorIndex;
                if (initInfo.WindowX == -1)
                    initInfo.WindowX = UserConfig.Video.WindowX;
                if (initInfo.WindowY == -1)
                    initInfo.WindowY = UserConfig.Video.WindowY;
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

                UserConfig.Video.MonitorIndex = initInfo.MonitorIndex;
                UserConfig.Video.WindowX = initInfo.WindowX;
                UserConfig.Video.WindowY = initInfo.WindowY;
                UserConfig.Video.ResolutionWidth = initInfo.ScreenWidth;
                UserConfig.Video.ResolutionHeight = initInfo.ScreenHeight;
                UserConfig.Video.Fullscreen = initInfo.Fullscreen.Value;

 
                return mainWindow = new MainView();
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
                //gameView.Resize(Width, Height);

                DoubleClickTime = 300;

                gameView.Closed += GameView_Closed;
                Closing += MainWindow_Closing;
                Render += MainWindow_Render;
                Update += MainWindow_Update;
                Resize += MainWindow_Resize;
                WindowAreaChanged += MainWindow_WindowAreaChanged;

                MakeCurrent();

                
            }
            catch (Exception ex)
            {
                ReportException("Load", ex);
            }
        }

        private void MainWindow_WindowAreaChanged(IMonitor monitor, Vector2D<int> position, Vector2D<int> size, bool fullscreen)
        {
            if (monitor != null)
                UserConfig.Video.MonitorIndex = monitor.Index;

            UserConfig.Video.WindowX = position.X;
            UserConfig.Video.WindowY = position.Y;
            UserConfig.Video.ResolutionWidth = size.X;
            UserConfig.Video.ResolutionHeight = size.Y;
            UserConfig.Video.Fullscreen = fullscreen;
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
                Directory.CreateDirectory(Path.GetDirectoryName(Freeserf.FileSystem.Paths.UserConfigPath));
                UserConfig.Save(Freeserf.FileSystem.Paths.UserConfigPath);
            }
            catch
            {
                // ignore
            }
        }

        static void ReportException(string source, Exception exception)
        {
            Log.Error.Write(ErrorSystemType.Application, $"{source}: {exception.Message}");

            // TODO: how to implement crash handle window?
            //if (crashHandlerForm.RaiseException(exception) == UI.CrashReaction.Restart)
            //    Process.Start(Assembly.GetEntryAssembly().Location);

            Exit();
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


        static Freeserf.Event.Button ConvertMouseButtons(MouseButtons buttons)
        {
            if (buttons.HasFlag(MouseButtons.Left))
                return Freeserf.Event.Button.Left;
            else if (buttons.HasFlag(MouseButtons.Right))
                return Freeserf.Event.Button.Right;
            else if (buttons.HasFlag(MouseButtons.Middle))
                return Freeserf.Event.Button.Middle;
            else
                return Freeserf.Event.Button.None;
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

                switch (key)
                {
                    case Key.Left:
                        gameView?.NotifySystemKeyPressed(Freeserf.Event.SystemKey.Left, 0);
                        break;
                    case Key.Right:
                        gameView?.NotifySystemKeyPressed(Freeserf.Event.SystemKey.Right, 0);
                        break;
                    case Key.Up:
                        gameView?.NotifySystemKeyPressed(Freeserf.Event.SystemKey.Up, 0);
                        break;
                    case Key.Down:
                        gameView?.NotifySystemKeyPressed(Freeserf.Event.SystemKey.Down, 0);
                        break;
                    case Key.PageUp:
                        gameView?.NotifySystemKeyPressed(Freeserf.Event.SystemKey.PageUp, (byte)modifiers);
                        break;
                    case Key.PageDown:
                        gameView?.NotifySystemKeyPressed(Freeserf.Event.SystemKey.PageDown, (byte)modifiers);
                        break;
                    case Key.Escape:
                        gameView?.NotifySystemKeyPressed(Freeserf.Event.SystemKey.Escape, 0);
                        break;
                    case Key.F5:
                        gameView?.NotifySystemKeyPressed(Freeserf.Event.SystemKey.F5, 0);
                        break;
                    case Key.F6:
                        gameView?.NotifySystemKeyPressed(Freeserf.Event.SystemKey.F6, 0);
                        break;
                    case Key.F11:
                        break;
                    case Key.Enter:
                        gameView?.NotifyKeyPressed(Freeserf.Event.SystemKeys.Return, (byte)modifiers);
                        break;
                    case Key.Backspace:
                        gameView?.NotifyKeyPressed(Freeserf.Event.SystemKeys.Backspace, (byte)modifiers);
                        break;
                    case Key.Delete:
                        gameView?.NotifyKeyPressed(Freeserf.Event.SystemKeys.Delete, (byte)modifiers);
                        break;
                    case Key.Tab:
                        gameView?.NotifyKeyPressed(Freeserf.Event.SystemKeys.Tab, (byte)modifiers);
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
