using Freeserf.Renderer;
using Silk.NET.Input.Common;
using Silk.NET.Window;
using Silk.NET.Windowing.Common;
using System;
using System.Drawing;
using System.IO;

namespace Freeserf
{
    // TODO: The Render event is called independently of input events. This can cause problem if the input events change something while the render/update events work with something.
    // TODO: This has to be fixed in a way that this event behavior is valid (e.g. locking mutexes inside the viewers, gui or interface).
    class MainWindow : Window, IDisposable
    {
        enum MouseButtonIndex
        {
            Left = 0,
            Middle = 1,
            Right = 2
        }

        static MainWindow mainWindow = null;
        GameView gameView = null;
        bool fullscreen = false;
        readonly bool[] pressedMouseButtons = new bool[3];
        readonly bool[] keysDown = new bool[(int)Key.LastKey + 1];
        int lastDragX = int.MinValue;
        int lastDragY = int.MinValue;
        static Global.InitInfo initInfo = null;
        static Data.DataSource dataSource = null;
        bool scrolled = false;
        Point clickPosition = Point.Empty;

        private MainWindow(WindowOptions options)
            : base(options)
        {
            Load += MainWindow_Load;
        }

        private int Width
        {
            get => Size.Width;
            set => Size = new System.Drawing.Size(value, Size.Height);
        }
        private int Height
        {
            get => Size.Height;
            set => Size = new System.Drawing.Size(Size.Width, value);
        }


        public static MainWindow Create(string[] args)
        {
            if (mainWindow != null)
                throw new ExceptionFreeserf(ErrorSystemType.Application, "A main window can not be created twice.");

            try
            {
#if !DEBUG
                Log.SetStream(File.Create(Path.Combine(Program.ExecutablePath, "log.txt")));
#endif
                Log.SetLevel(Log.Level.Error);
            }
            catch (IOException)
            {
                // TODO: logging not possible
            }

            initInfo = Global.Init(args); // this may change the log level

            try
            {
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
                    Log.Error.Write(ErrorSystemType.Data, "Error: Error loading DOS data.");
                    return null;
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

                var state = (initInfo.Fullscreen.HasValue && initInfo.Fullscreen.Value) ? WindowState.Fullscreen : WindowState.Normal;
                var options = new WindowOptions(
                    true,
                    true,
                    new Point(20, 40),
                    new System.Drawing.Size(initInfo.ScreenWidth, initInfo.ScreenHeight),
                    50.0,
                    50.0,
                    GraphicsAPI.Default,
                    Global.VERSION,
                    state,
                    state == WindowState.Normal ? WindowBorder.Fixed : WindowBorder.Hidden,
                    VSyncMode.Off,
                    10,
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
                gameView = new GameView(dataSource, new Size(initInfo.ScreenWidth, initInfo.ScreenHeight),
                    DeviceType.Desktop, SizingPolicy.FitRatio, OrientationPolicy.Fixed);
                gameView.FullscreenRequestHandler = FullscreenRequestHandler;
                gameView.Resize(Width, Height);

                if (initInfo.Fullscreen == true)
                    SetFullscreen(true);

                CursorVisible = false; // hide cursor
                DoubleClickTime = 150;

                gameView.Closed += GameView_Closed;
                Closing += MainWindow_Closing;
                Render += MainWindow_Render;
                Update += MainWindow_Update;
                Resize += MainWindow_Resize;
                StateChanged += MainWindow_StateChanged;
            }
            catch (Exception ex)
            {
                ReportException("Load", ex);
            }
        }

        private static void Exit()
        {
            mainWindow?.Close();
            // TODO: Silk has still problems to trigger the closing event on manual close.
            mainWindow?.MainWindow_Closing(); // Remove this later if Silk is fixed.
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

            // Cursor = MouseCursor.Default;

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

                WindowState = (fullscreen) ? WindowState.Fullscreen : WindowState.Normal;
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

        private void MainWindow_Resize(System.Drawing.Size size)
        {
            gameView.Resize(size.Width, size.Height);
        }

        private void MainWindow_Render(double delta)
        {
            if (!Initialized)
                return;

            MakeCurrent();

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
            // not used for now
        }

        private void MainWindow_StateChanged(WindowState state)
        {
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
                        HandleKeyDrag();
                        break;
                    case Key.Right:
                        gameView?.NotifySystemKeyPressed(Event.SystemKey.Right, 0);
                        HandleKeyDrag();
                        break;
                    case Key.Up:
                        gameView?.NotifySystemKeyPressed(Event.SystemKey.Up, 0);
                        HandleKeyDrag();
                        break;
                    case Key.Down:
                        gameView?.NotifySystemKeyPressed(Event.SystemKey.Down, 0);
                        HandleKeyDrag();
                        break;
                    case Key.PageUp:
                        gameView?.NotifySystemKeyPressed(Event.SystemKey.PageUp, 0);
                        break;
                    case Key.PageDown:
                        gameView?.NotifySystemKeyPressed(Event.SystemKey.PageDown, 0);
                        break;
                    case Key.Escape:
                        gameView?.NotifySystemKeyPressed(Event.SystemKey.Escape, 0);
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
                            if ((int)key >= 32 && (int)key < 128) // only valid ascii characters
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

            base.OnKeyDown(key, modifiers);
        }

        protected override void OnKeyChar(char character)
        {
            try
            {
                switch (character)
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
                        gameView?.NotifyKeyPressed(character, 0);
                        break;
                }
            }
            catch (Exception ex)
            {
                ReportException("KeyPress", ex);
            }

            base.OnKeyChar(character);
        }

        protected override void OnMouseMoveDelta(Point position, MouseButtons buttons, Point delta)
        {
            if (gameView == null)
                return;

            try
            {
                if (delta.X == 0 && delta.Y == 0)
                    return;

                UpdateMouseState(buttons);

                if (buttons.HasFlag(MouseButtons.Left) || buttons.HasFlag(MouseButtons.Right))
                {
                    if (lastDragX == int.MinValue)
                        return;

                    bool dragAllowed = gameView.NotifyDrag(position.X, position.Y, lastDragX - position.X, lastDragY - position.Y, ConvertMouseButtons(buttons));

                    // lock the mouse if dragging with right button
                    if (dragAllowed)
                    {
                        CursorMode = CursorMode.Disabled;
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

        protected override void OnMouseUp(Point position, MouseButtons button)
        {
            UpdateMouseState(button, false);

            // restore cursor from successful locked dragging
            if (button.HasFlag(MouseButtons.Right))
            {
                CursorMode = CursorVisible ? CursorMode.Normal : CursorMode.Hidden;
                if (scrolled && (UserConfig.Game.Options & (int)Option.ResetCursorAfterScrolling) != 0)
                {
                    CursorPosition = new PointF(Width / 2, Height / 2);
                    gameView.SetCursorPosition(Width / 2, Height / 2);
                }
                scrolled = false;
                gameView.NotifyStopDrag();
            }

            base.OnMouseUp(position, button);
        }

        protected override void OnMouseDown(Point position, MouseButtons button)
        {
            UpdateMouseState(button, true);

            clickPosition = position;

            // left + right = special click
            if (button.HasFlag(MouseButtons.Left) || button.HasFlag(MouseButtons.Right))
            {
                if (
                    pressedMouseButtons[(int)MouseButtonIndex.Left] &&
                    pressedMouseButtons[(int)MouseButtonIndex.Right]
                )
                {
                    try
                    {
                        gameView?.NotifySpecialClick(position.X, position.Y);
                    }
                    catch (Exception ex)
                    {
                        ReportException("MouseDown", ex);
                    }
                }
                else
                {
                    lastDragX = position.X;
                    lastDragY = position.Y;
                }
            }

            base.OnMouseDown(position, button);
        }

        protected override void OnClick(Point position, MouseButtons button)
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
                gameView?.NotifyClick(clickPosition.X, clickPosition.Y, ConvertMouseButtons(button));
            }
            catch (Exception ex)
            {
                ReportException("MouseClick", ex);
            }

            base.OnClick(position, button);
        }

        protected override void OnDoubleClick(Point position, MouseButtons button)
        {
            try
            {
                if (button == MouseButtons.Left)
                    gameView?.NotifyDoubleClick(position.X, position.Y, ConvertMouseButtons(button));
            }
            catch (Exception ex)
            {
                ReportException("MouseDoubleClick", ex);
            }

            base.OnDoubleClick(position, button);
        }

        protected override void OnMouseWheel(Point position, float delta)
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
