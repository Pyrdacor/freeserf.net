using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using OpenTK;
using Freeserf.Renderer.OpenTK;
using Orientation = Freeserf.Renderer.OpenTK.Orientation;
using Freeserf.Data;
using System.ComponentModel;
using OpenTK.Input;

namespace Freeserf
{
    // TODO: The Render event is called independently of input events. This can cause problem if the input events change something while the render/update events work with something.
    // TODO: This has to be fixed in a way that this event behavior is valid (e.g. locking mutexes inside the viewers, gui or interface).
    class MainWindow : GameWindow
    {
        GameView gameView = null;
        bool fullscreen = false;
        bool[] pressedMouseButtons = new bool[3];
        bool[] keysDown = new bool[256];
        int lastDragX = int.MinValue;
        int lastDragY = int.MinValue;
        static Timer clickWaitTimer = new Timer();
        static MouseEventArgs clickWaitTimerArgs = null;
        Global.InitInfo initInfo = null;

        internal MainWindow(string[] args)
        {
            Log.SetStream(File.Create(Path.Combine(Program.ExecutablePath, "log.txt")));
            Log.SetLevel(Log.Level.Error);

            initInfo = Global.Init(args); // this may change the log level
        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);

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

                var dataSource = data.GetDataSource();

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

                // TODO
                /*var screen = Screen.FromHandle(Handle);

                if (initInfo.ScreenWidth > screen.Bounds.Width)
                    initInfo.ScreenWidth = screen.Bounds.Width;
                if (initInfo.ScreenHeight > screen.Bounds.Height)
                    initInfo.ScreenHeight = screen.Bounds.Height;*/

                UserConfig.Video.ResolutionWidth = initInfo.ScreenWidth;
                UserConfig.Video.ResolutionHeight = initInfo.ScreenHeight;
                UserConfig.Video.Fullscreen = initInfo.Fullscreen.Value;

                Width = initInfo.ScreenWidth;
                Height = initInfo.ScreenHeight;

                gameView = new GameView(dataSource, new Size(initInfo.ScreenWidth, initInfo.ScreenHeight), DeviceType.Desktop, SizingPolicy.FitRatio, OrientationPolicy.Fixed);
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

                gameView.Closed += GameView_Closed; ;

                clickWaitTimer.Elapsed += ClickWaitTimer_Elapsed;
                clickWaitTimer.Interval = 130;
            }
            catch (Exception ex)
            {
                ReportException("Load", ex);
            }
        }

        private void GameView_Closed(object sender, EventArgs e)
        {
            Exit();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            // TODO: Ask for saving?

            if (clickWaitTimer.Enabled)
                clickWaitTimer.Stop();

            // TODO
            //if (debugConsole != null)
            //    debugConsole.Close();

            Cursor = MouseCursor.Default;

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

            Log.Error.Write(source, exception.Message);

            SetFullscreen(false, false);

            // TODO: how to implement crash handle window?
            //if (crashHandlerForm.RaiseException(exception) == UI.CrashReaction.Restart)
            //    Process.Start(Assembly.GetEntryAssembly().Location);
            Log.Error.Write(source, exception.Message);

            Exit();
        }

        void ClickWaitTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            clickWaitTimer.Stop();

            try
            {
                gameView?.NotifyClick(clickWaitTimerArgs.X, clickWaitTimerArgs.Y, ConvertMouseButton(clickWaitTimerArgs.Mouse));
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

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            MakeCurrent();

            try
            {
                gameView.Render();
            }
            catch (Exception ex)
            {
                ReportException("Render", ex);
                return;
            }

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
        }

        protected override void OnWindowStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                SetFullscreen(true, true);
                return;
            }

            base.OnWindowStateChanged(e);
        }

        static Event.Button ConvertMouseButton(MouseState state)
        {
            if (state.LeftButton == ButtonState.Pressed)
                return Event.Button.Left;
            else if (state.MiddleButton == ButtonState.Pressed)
                return Event.Button.Middle;
            else if (state.RightButton == ButtonState.Pressed)
                return Event.Button.Right;
            else
                return Event.Button.None;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            try
            {
                if (e.Delta < 0)
                    ZoomOut();
                else if (e.Delta > 0)
                    ZoomIn();
            }
            catch (Exception ex)
            {
                ReportException("MouseWheel", ex);
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            if (gameView == null)
                return;

            try
            {
                gameView.SetCursorPosition(e.X, e.Y);

                UpdateMouseState(e.Mouse);

                if (e.Mouse.LeftButton == ButtonState.Pressed || e.Mouse.RightButton == ButtonState.Pressed)
                {
                    if (lastDragX == int.MinValue)
                        return;

                    gameView.NotifyDrag(e.X, e.Y, lastDragX - e.X, lastDragY - e.Y, ConvertMouseButton(e.Mouse));
                    lastDragX = e.X;
                    lastDragY = e.Y;
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

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Mouse.LeftButton == ButtonState.Pressed || e.Mouse.RightButton == ButtonState.Pressed)
            {
                lastDragX = e.X;
                lastDragY = e.Y;

                clickWaitTimerArgs = e;
            }

            UpdateMouseState(e.Mouse);

            if (pressedMouseButtons[0] && pressedMouseButtons[2]) // left + right
            {
                try
                {
                    gameView?.NotifySpecialClick(e.X, e.Y);
                }
                catch (Exception ex)
                {
                    ReportException("MouseDown", ex);
                }

                pressedMouseButtons[0] = false;
                pressedMouseButtons[2] = false;
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            UpdateMouseState(e.Mouse);

            OnMouseClick(e);
        }

        void OnMouseClick(MouseButtonEventArgs e)
        {
            if (clickWaitTimer.Enabled)
            {
                clickWaitTimer.Stop();

                try
                {
                    gameView?.NotifyDoubleClick(e.X, e.Y, ConvertMouseButton(clickWaitTimerArgs.Mouse));
                }
                catch (Exception ex)
                {
                    ReportException("MouseClick", ex);
                }

                return;
            }

            clickWaitTimer.Start();
        }

        void UpdateMouseState(MouseState state)
        {
            pressedMouseButtons[0] = state.LeftButton == ButtonState.Pressed;
            pressedMouseButtons[1] = state.MiddleButton == ButtonState.Pressed;
            pressedMouseButtons[2] = state.RightButton == ButtonState.Pressed;
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

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);

            keysDown[(int)e.Key] = false;
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            try
            {
                base.OnKeyDown(e);

                keysDown[(int)e.Key] = true;

                if (e.Control && e.Key == Key.F)
                {
                    ToggleFullscreen();
                    return;
                }

                switch (e.Key)
                {
                    case Key.Left:
                        gameView?.NotifyKeyPressed((char)e.ScanCode, 0);
                        HandleKeyDrag();
                        break;
                    case Key.Right:
                        gameView?.NotifyKeyPressed((char)e.ScanCode, 0);
                        HandleKeyDrag();
                        break;
                    case Key.Up:
                        gameView?.NotifyKeyPressed((char)e.ScanCode, 0);
                        HandleKeyDrag();
                        break;
                    case Key.Down:
                        gameView?.NotifyKeyPressed((char)e.ScanCode, 0);
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
                    case Key.BackSpace:
                        gameView?.NotifyKeyPressed(Event.SystemKeys.Backspace, 0);
                        break;
                    case Key.Delete:
                        gameView?.NotifyKeyPressed(Event.SystemKeys.Delete, 0);
                        break;
                    default:
                        {
                            if (e.ScanCode < 128) // only valid ascii characters
                            {
                                byte modifier = 0;

                                if (e.Control)
                                    modifier |= 1;
                                if (e.Shift)
                                    modifier |= 2;
                                if (e.Alt)
                                    modifier |= 4;

                                gameView?.NotifyKeyPressed((char)e.ScanCode, modifier);
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

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            try
            {
                base.OnKeyPress(e);

                switch (e.KeyChar)
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
                        gameView?.NotifyKeyPressed(e.KeyChar, 0);
                        break;
                }
            }
            catch (Exception ex)
            {
                ReportException("KeyPress", ex);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);

            //Cursor = MouseCursor.Empty;
            CursorVisible = false;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            //Cursor = MouseCursor.Default;
            CursorVisible = true;
        }
    }
}
