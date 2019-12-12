using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Timers;
using Freeserf.Renderer;
using Orientation = Freeserf.Renderer.Orientation;
using Silk.NET.Input;
using Silk.NET.Input.Common;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Common;
using KeyModifiers = Silk.NET.GLFW.KeyModifiers;

namespace Freeserf
{
    // TODO: The Render event is called independently of input events. This can cause problem if the input events change something while the render/update events work with something.
    // TODO: This has to be fixed in a way that this event behavior is valid (e.g. locking mutexes inside the viewers, gui or interface).
    class MainWindow : IDisposable
    {
        enum MouseButtonIndex
        {
            Left = 0,
            Middle = 1,
            Right = 2
        }

        IWindow window = null;
        GameView gameView = null;
        bool fullscreen = false;
        bool[] pressedMouseButtons = new bool[3];
        bool[] keysDown = new bool[256];
        PointF lastMousePosition = PointF.Empty;
        int lastDragX = int.MinValue;
        int lastDragY = int.MinValue;
        float lastWheelValue = 0.0f;
        static Timer clickWaitTimer = new Timer();
        static IMouse clickWaitTimerArgs = null;
        Global.InitInfo initInfo = null;
        Data.DataSource dataSource = null;

        internal MainWindow(string[] args)
        {
            Log.SetStream(File.Create(Path.Combine(Program.ExecutablePath, "log.txt")));
            Log.SetLevel(Log.Level.Error);

            initInfo = Global.Init(args); // this may change the log level

            Init();
        }

        private string Title { get; set; } = "";
        private Point Position { get; set; } = Point.Empty;
        private int Width { get; set; } = 0;
        private int Height { get; set; } = 0;
        private WindowState State { get; set; } = WindowState.Normal;


        private void Init()
        {
            try
            {
                Title = Global.VERSION;

                Network.Network.DefaultClientFactory = new Network.ClientFactory();
                Network.Network.DefaultServerFactory = new Network.ServerFactory();

                if (initInfo.ConsoleWindow)
                {
                    // TODO
                }

                UserConfig.Load(FileSystem.Paths.UserConfigPath);

                var data = Data.Data.GetInstance();
                string dataPath = Program.ExecutablePath;// Path.Combine(Program.ExecutablePath, UserConfig.Game.DataFile);

                if (!data.Load(dataPath, UserConfig.Game.GraphicDataUsage, UserConfig.Game.SoundDataUsage, UserConfig.Game.MusicDataUsage))
                {
                    Console.WriteLine("Error: Error loading DOS data.");
                    Exit();
                    return;
                }

                dataSource = data.GetDataSource();

                // TODO: use the rest of the command line and maybe extend the command line

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

                State = (initInfo.Fullscreen.HasValue && initInfo.Fullscreen.Value) ? WindowState.Fullscreen : WindowState.Normal;

                Window.Init();

                var options = new WindowOptions(
                    true,
                    true,
                    Position,
                    new System.Drawing.Size(initInfo.ScreenWidth, initInfo.ScreenHeight),
                    50.0,
                    50.0,
                    GraphicsAPI.Default,
                    Title,
                    State,
                    State == WindowState.Normal ? WindowBorder.Fixed : WindowBorder.Hidden,
                    VSyncMode.Off,
                    10,
                    false
                );

                window = Window.Create(options);
                window.Load += Window_Load;

                Width = initInfo.ScreenWidth;
                Height = initInfo.ScreenHeight;                
            }
            catch (Exception ex)
            {
                ReportException("Init", ex);
            }
        }

        public void Run()
        {
            window?.Run();
        }

        private void Window_Load()
        {
            try
            {
                gameView = new GameView(dataSource, new Size(initInfo.ScreenWidth, initInfo.ScreenHeight), Program.ExecutablePath + "/assets", DeviceType.Desktop, SizingPolicy.FitRatio, OrientationPolicy.Fixed);
                gameView.FullscreenRequestHandler = FullscreenRequestHandler;

                gameView.Resize(Width, Height, Orientation.LandscapeLeftRight);

                if (initInfo.Fullscreen == true)
                    SetFullscreen(true);

                var audio = gameView.AudioFactory?.GetAudio();

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
                        volumeController.SetVolume(Misc.Clamp(0.0f, UserConfig.Audio.Volume, 1.0f));
                }

                gameView.Closed += GameView_Closed;
                window.Closing += Window_Closing;
                window.Render += Window_Render;
                window.Update += Window_Update;
                window.Resize += Window_Resize;
                window.StateChanged += Window_StateChanged;

                InitInputEvents();

                clickWaitTimer.Elapsed += ClickWaitTimer_Elapsed;
                clickWaitTimer.Interval = 130;
            }
            catch (Exception ex)
            {
                ReportException("Load", ex);
            }
        }

        private void Exit()
        {
            window?.Close();
        }

        private void InitInputEvents()
        {
            var input = window.GetInput();
            var mouse = input.Mice.FirstOrDefault(m => m.IsConnected);
            var keyboard = input.Keyboards.FirstOrDefault(k => k.IsConnected);

            if (mouse != null)
            {
                lastWheelValue = mouse.ScrollWheels.FirstOrDefault().Y;
                lastMousePosition = mouse.Position;

                mouse.MouseDown += GameView_MouseDown;
                mouse.MouseUp += GameView_MouseUp;
                mouse.MouseMove += GameView_MouseMove;
                mouse.Scroll += GameView_Scroll;
            }

            if (keyboard != null)
            {
                keyboard.KeyDown += GameView_KeyDown;
                keyboard.KeyUp += GameView_KeyUp;
                keyboard.KeyChar += GameView_KeyChar;
            }
        }

        private void GameView_Closed(object sender, EventArgs e)
        {
            Exit();
        }

        protected void Window_Closing()
        {
            // TODO: Ask for saving?

            if (clickWaitTimer.Enabled)
                clickWaitTimer.Stop();

            // TODO
            //if (debugConsole != null)
            //    debugConsole.Close();

            // Cursor = MouseCursor.Default;

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

        void ReportException(string source, Exception exception)
        {
            if (clickWaitTimer != null && clickWaitTimer.Enabled)
                clickWaitTimer.Stop();

            Log.Error.Write(ErrorSystemType.Application, $"{source}: {exception.Message}");

            SetFullscreen(false, false);

            // TODO: how to implement crash handle window?
            //if (crashHandlerForm.RaiseException(exception) == UI.CrashReaction.Restart)
            //    Process.Start(Assembly.GetEntryAssembly().Location);

            Exit();
        }

        void ClickWaitTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            clickWaitTimer.Stop();

            try
            {
                int x = (int)Math.Round(clickWaitTimerArgs.Position.X);
                int y = (int)Math.Round(clickWaitTimerArgs.Position.Y);

                gameView?.NotifyClick(x, y, ConvertMouseButton(clickWaitTimerArgs));
            }
            catch (Exception ex)
            {
                ReportException("MouseClick", ex);
            }
        }

        bool FullscreenRequestHandler(bool fullscreen)
        {
            if (this.fullscreen != fullscreen)
            {
                this.fullscreen = fullscreen;

                State = (fullscreen) ? WindowState.Fullscreen : WindowState.Normal;
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
            gameView.Resize(Width, Height, Orientation.LandscapeLeftRight);

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

        private void Window_Resize(System.Drawing.Size size)
        {
            // not used for now, window should not be resizable
        }

        private void Window_Render(double delta)
        {
            window.MakeCurrent();

            try
            {
                gameView.Render();
            }
            catch (Exception ex)
            {
                ReportException("Render", ex);
                return;
            }

            window.SwapBuffers();
        }

        private void Window_Update(double delta)
        {
            // not used for now
        }

        private void Window_StateChanged(WindowState state)
        {
            State = state;

            if (State == WindowState.Maximized)
            {
                SetFullscreen(true, true);
                return;
            }
        }

        static Event.Button ConvertMouseButton(IMouse mouse)
        {
            if (mouse.IsButtonPressed(MouseButton.Left))
                return Event.Button.Left;
            else if (mouse.IsButtonPressed(MouseButton.Middle))
                return Event.Button.Middle;
            else if (mouse.IsButtonPressed(MouseButton.Right))
                return Event.Button.Right;
            else
                return Event.Button.None;
        }

        private void GameView_KeyUp(IKeyboard keyboard, Key key, int mods)
        {
            keysDown[(int)key] = false;
        }

        private void GameView_KeyDown(IKeyboard keyboard, Key key, int mods)
        {
            var modifiers = (KeyModifiers)mods;

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
                        gameView?.NotifyKeyPressed((char)key, 0);
                        HandleKeyDrag();
                        break;
                    case Key.Right:
                        gameView?.NotifyKeyPressed((char)key, 0);
                        HandleKeyDrag();
                        break;
                    case Key.Up:
                        gameView?.NotifyKeyPressed((char)key, 0);
                        HandleKeyDrag();
                        break;
                    case Key.Down:
                        gameView?.NotifyKeyPressed((char)key, 0);
                        HandleKeyDrag();
                        break;
                    case Key.F10:
                        gameView?.NotifyKeyPressed('n', 1);
                        break;
                    case Key.F11:
                        ToggleFullscreen();
                        break;
                    case Key.Enter:
                        gameView?.NotifyKeyPressed(Event.SystemKeys.Return, 0);
                        break;
                    case Key.Backspace:
                        gameView?.NotifyKeyPressed(Event.SystemKeys.Backspace, 0);
                        break;
                    case Key.Delete:
                        gameView?.NotifyKeyPressed(Event.SystemKeys.Delete, 0);
                        break;
                    default:
                        {
                            if ((int)key < 128) // only valid ascii characters
                            {
                                byte modifier = 0;

                                if (modifiers.HasFlag(KeyModifiers.Control))
                                    modifier |= 1;
                                if (modifiers.HasFlag(KeyModifiers.Shift))
                                    modifier |= 2;
                                if (modifiers.HasFlag(KeyModifiers.Alt))
                                    modifier |= 4;

                                gameView?.NotifyKeyPressed((char)key, modifier);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ReportException("KeyDown", ex);
            }
        }

        private void GameView_KeyChar(IKeyboard keyboard, char keyChar)
        {
            try
            {
                switch (keyChar)
                {
                    case '<':
                        ZoomOut();
                        break;
                    case '>':
                        ZoomIn();
                        break;
                    case '+':
                    case '-':
                    case 'ä':
                    case 'Ä':
                    case 'ö':
                    case 'Ö':
                    case 'ü':
                    case 'Ü':
                        // TODO: Encoding
                        gameView?.NotifyKeyPressed(keyChar, 0);
                        break;
                }
            }
            catch (Exception ex)
            {
                ReportException("KeyPress", ex);
            }
        }

        private void GameView_MouseMove(IMouse mouse, PointF position)
        {
            if (gameView == null)
                return;

            try
            {
                int lastX = (int)Math.Round(lastMousePosition.X);
                int lastY = (int)Math.Round(lastMousePosition.Y);
                int x = (int)Math.Round(position.X);
                int y = (int)Math.Round(position.Y);

                if (x == lastX && y == lastY)
                    return;

                CheckMouseEnterOrLeave(
                    window.PointToClient(new Point(x, y)),
                    window.PointToClient(new Point(lastX, lastY))
                );

                gameView.SetCursorPosition(x, y);

                UpdateMouseState(mouse);

                if (mouse.IsButtonPressed(MouseButton.Left) || mouse.IsButtonPressed(MouseButton.Right))
                {
                    if (lastDragX == int.MinValue)
                        return;

                    gameView.NotifyDrag(x, y, lastDragX - x, lastDragY - y, ConvertMouseButton(mouse));
                    lastDragX = x;
                    lastDragY = y;
                }
                else
                {
                    lastDragX = int.MinValue;
                    lastDragY = int.MinValue;
                }
            }
            catch (Exception ex)
            {
                ReportException("MouseMove", ex);
            }
        }

        private void GameView_MouseUp(IMouse mouse, MouseButton button)
        {
            UpdateMouseState(mouse);
            OnMouseClick(mouse);
        }

        private void GameView_MouseDoubleClick(int x, int y, Event.Button button)
        {
            clickWaitTimer.Stop();

            try
            {
                gameView?.NotifyDoubleClick(x, y, button);
            }
            catch (Exception ex)
            {
                ReportException("MouseDoubleClick", ex);
            }
        }

        private void GameView_MouseDown(IMouse mouse, MouseButton button)
        {
            int x = (int)Math.Round(mouse.Position.X);
            int y = (int)Math.Round(mouse.Position.Y);

            if (button == MouseButton.Left || button == MouseButton.Right)
            {
                lastDragX = x;
                lastDragY = y;

                if (button == MouseButton.Left)
                {
                    if (clickWaitTimer.Enabled) // click timer is still running -> waiting for a double-click
                    {
                        GameView_MouseDoubleClick(x, y, Event.Button.Left);
                    }
                    else
                    {
                        // normal click, start the wait timer
                        clickWaitTimerArgs = mouse;
                        clickWaitTimer.Start();
                    }

                }
                else // right
                {
                    gameView?.NotifyClick(x, y, Event.Button.Right);
                }
            }

            UpdateMouseState(mouse);

            if (pressedMouseButtons[(int)MouseButtonIndex.Left] && pressedMouseButtons[(int)MouseButtonIndex.Right]) // left + right
            {
                try
                {
                    gameView?.NotifySpecialClick(x, y);
                }
                catch (Exception ex)
                {
                    ReportException("MouseDown", ex);
                }

                pressedMouseButtons[(int)MouseButtonIndex.Left] = false;
                pressedMouseButtons[(int)MouseButtonIndex.Right] = false;
            }
        }

        private void GameView_Scroll(IMouse mouse, ScrollWheel wheel)
        {
            try
            {
                float delta = wheel.Y - lastWheelValue;
                lastWheelValue = wheel.Y;

                if (delta < 0)
                    ZoomOut();
                else if (delta > 0)
                    ZoomIn();
            }
            catch (Exception ex)
            {
                ReportException("MouseWheel", ex);
            }
        }

        void OnMouseClick(IMouse mouse)
        {
            if (clickWaitTimer.Enabled)
            {
                clickWaitTimer.Stop();

                try
                {
                    int x = (int)Math.Round(mouse.Position.X);
                    int y = (int)Math.Round(mouse.Position.Y);

                    gameView?.NotifyDoubleClick(x, y, ConvertMouseButton(clickWaitTimerArgs));
                }
                catch (Exception ex)
                {
                    ReportException("MouseClick", ex);
                }

                return;
            }

            clickWaitTimer.Start();
        }

        void UpdateMouseState(IMouse state)
        {
            pressedMouseButtons[(int)MouseButtonIndex.Left] = state.IsButtonPressed(MouseButton.Left);
            pressedMouseButtons[(int)MouseButtonIndex.Middle] = state.IsButtonPressed(MouseButton.Middle);
            pressedMouseButtons[(int)MouseButtonIndex.Right] = state.IsButtonPressed(MouseButton.Right);
        }

        void HandleKeyDrag()
        {
            int dx = 0;
            int dy = 0;

            if (keysDown[(int)Key.Left])
                dx += 32;
            if (keysDown[(int)Key.Right])
                dx -= 32;
            if (keysDown[(int)Key.Up])
                dy -= 32;
            if (keysDown[(int)Key.Down])
                dy += 32;

            try
            {
                gameView?.NotifyDrag(0, 0, -dx, dy, Event.Button.Right);
            }
            catch (Exception ex)
            {
                ReportException("KeyDrag", ex);
            }
        }

        // Positions are in client coordinates
        private void CheckMouseEnterOrLeave(Point currentPosition, Point lastPosition)
        {
            bool currentOutside =   currentPosition.X < 0 || currentPosition.X >= Width ||
                                    currentPosition.Y < 0 || currentPosition.Y >= Height;
            bool lastOutside =      lastPosition.X < 0 || lastPosition.X >= Width ||
                                    lastPosition.Y < 0 || lastPosition.Y >= Height;

            if (currentOutside && !lastOutside) // leave
            {
                OnMouseLeave();
            }
            else if (!currentOutside && lastOutside) // enter
            {
                OnMouseEnter();
            }
        }

        private void OnMouseEnter()
        {
            //Cursor = MouseCursor.Empty;
            // CursorVisible = false;
        }

        private void OnMouseLeave()
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
