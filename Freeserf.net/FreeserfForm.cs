using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Freeserf.Renderer.OpenTK;
using Orientation = Freeserf.Renderer.OpenTK.Orientation;
using Freeserf.Data;

namespace Freeserf
{
    public partial class FreeserfForm : Form
    {
        GameView gameView = null;
        bool fullscreen = false;
        MouseButtons pressedMouseButtons = MouseButtons.None;
        int lastDragX = int.MinValue;
        int lastDragY = int.MinValue;
        static Timer clickWaitTimer = new Timer();
        Global.InitInfo initInfo = null;
        DebugConsole debugConsole = null;
        CrashHandlerForm crashHandlerForm = new CrashHandlerForm();

        public FreeserfForm(string[] args)
        {
            Log.SetStream(File.Create(Path.Combine(Program.ExecutablePath, "log.txt")));
            Log.SetLevel(Log.Level.Error);

            initInfo = Global.Init(args); // this may change the log level

            InitializeComponent();
        }

        void SetClientSize(int width, int height)
        {
            int diffX = Width - RenderControl.Width;
            int diffY = Height - RenderControl.Height;

            Size = new System.Drawing.Size(width + diffX, height + diffY);
        }

        private void FreeserfForm_Load(object sender, EventArgs e)
        {
            try
            {
                Network.Network.DefaultClientFactory = new Network.ClientFactory();
                Network.Network.DefaultServerFactory = new Network.ServerFactory();

                if (initInfo.ConsoleWindow)
                {
                    debugConsole = new DebugConsole();

                    debugConsole.Show();
                    debugConsole.AttachLog();
                }

                UserConfig.Load(FileSystem.Paths.UserConfigPath);

                var data = Data.Data.GetInstance();
                string dataPath = Program.ExecutablePath;// Path.Combine(Program.ExecutablePath, UserConfig.Game.DataFile);

                if (!data.Load(dataPath))
                {
                    MessageBox.Show(this, $"Error loading data from \"{dataPath}\".", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
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

                var screen = Screen.FromHandle(Handle);

                if (initInfo.ScreenWidth > screen.Bounds.Width)
                    initInfo.ScreenWidth = screen.Bounds.Width;
                if (initInfo.ScreenHeight > screen.Bounds.Height)
                    initInfo.ScreenHeight = screen.Bounds.Height;

                UserConfig.Video.ResolutionWidth = initInfo.ScreenWidth;
                UserConfig.Video.ResolutionHeight = initInfo.ScreenHeight;
                UserConfig.Video.Fullscreen = initInfo.Fullscreen.Value;

                SetClientSize(initInfo.ScreenWidth, initInfo.ScreenHeight);

                gameView = new GameView(dataSource, new Size(initInfo.ScreenWidth, initInfo.ScreenHeight), DeviceType.Desktop, SizingPolicy.FitRatio, OrientationPolicy.Fixed);
                gameView.FullscreenRequestHandler = FullscreenRequestHandler;

                gameView.Resize(RenderControl.Width, RenderControl.Height, Orientation.LandscapeLeftRight);

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

                RenderControl.MouseWheel += RenderControl_MouseWheel;

                clickWaitTimer.Tick += ClickWaitTimer_Tick;
                clickWaitTimer.Interval = 130;

                FrameTimer.Interval = Global.TICK_LENGTH;
                FrameTimer.Start();

                BringToFront();
            }
            catch (Exception ex)
            {
                ReportException("Load", ex);
            }
        }

        void GameView_Closed(object sender, EventArgs e)
        {
            Close();
        }

        void FrameTimer_Tick(object sender, EventArgs e)
        {
            RenderControl.MakeCurrent();

            try
            {
                gameView.Render();
            }
            catch (Exception ex)
            {
                FrameTimer.Stop();

                ReportException("FrameTimer", ex);
                
                return;
            }

            RenderControl.SwapBuffers();
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            FormWindowState previousWindowState = WindowState;

            base.WndProc(ref m);

            FormWindowState currentWindowState = WindowState;

            if (previousWindowState != currentWindowState)
            {
                if (currentWindowState == FormWindowState.Maximized)
                {
                    if (!SetFullscreen(true) && !fullscreen)
                    {
                        WindowState = previousWindowState;
                    }
                }
                else if (currentWindowState == FormWindowState.Normal)
                {
                    if (!SetFullscreen(false) && fullscreen)
                    {
                        WindowState = previousWindowState;
                    }
                }
            }
        }

        bool FullscreenRequestHandler(bool fullscreen)
        {
            if (this.fullscreen != fullscreen)
            {
                this.fullscreen = fullscreen;

                HandleFullscreenChange();
            }

            return true;
        }

        void HandleFullscreenChange()
        {
            try
            {
                if (fullscreen)
                {
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Maximized;
                    gameView.Resize(RenderControl.Width, RenderControl.Height, Orientation.LandscapeLeftRight);
                    BringToFront();
                }
                else
                {
                    WindowState = FormWindowState.Normal;
                    FormBorderStyle = FormBorderStyle.FixedDialog;
                    SetClientSize(initInfo.ScreenWidth, initInfo.ScreenHeight);
                    gameView.Resize(RenderControl.Width, RenderControl.Height, Orientation.LandscapeLeftRight);
                }
            }
            catch (Exception ex)
            {
                ReportException("FullscreenChange", ex);
            }
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

        static Event.Button ConvertMouseButton(MouseButtons buttons)
        {
            switch (buttons)
            {
                default:
                case MouseButtons.None:
                    return Event.Button.None;
                case MouseButtons.Left:
                    return Event.Button.Left;
                case MouseButtons.Middle:
                    return Event.Button.Middle;
                case MouseButtons.Right:
                    return Event.Button.Right;
            }
        }

        void RenderControl_MouseWheel(object sender, MouseEventArgs e)
        {
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

        void RenderControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (gameView == null)
                return;

            try
            {
                gameView.SetCursorPosition(e.X, e.Y);

                var button = e.Button & (MouseButtons.Left | MouseButtons.Right);

                pressedMouseButtons = button;

                if (button == MouseButtons.Left || button == MouseButtons.Right)
                {
                    if (lastDragX == int.MinValue)
                        return;

                    gameView.NotifyDrag(e.X, e.Y, lastDragX - e.X, lastDragY - e.Y, ConvertMouseButton(button));
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

        void RenderControl_MouseDown(object sender, MouseEventArgs e)
        {
            var button = e.Button & (MouseButtons.Left | MouseButtons.Right);

            if (button == MouseButtons.Left || button == MouseButtons.Right)
            {
                lastDragX = e.X;
                lastDragY = e.Y;
            }

            pressedMouseButtons |= button;

            if (pressedMouseButtons == (MouseButtons.Left | MouseButtons.Right))
            {
                try
                {
                    gameView?.NotifySpecialClick(e.X, e.Y);
                }
                catch (Exception ex)
                {
                    ReportException("MouseDown", ex);
                }

                pressedMouseButtons = MouseButtons.None;
            }
        }

        void RenderControl_MouseUp(object sender, MouseEventArgs e)
        {
            var button = e.Button & (MouseButtons.Left | MouseButtons.Right);

            if (button != MouseButtons.None)
                pressedMouseButtons &= ~button;
        }

        bool[] KeysDown = new bool[256];

        void HandleKeyDrag()
        {
            int dx = 0;
            int dy = 0;

            if (KeysDown[(int)Keys.Left])
                dx += 32;
            if (KeysDown[(int)Keys.Right])
                dx -= 32;
            if (KeysDown[(int)Keys.Up])
                dy -= 32;
            if (KeysDown[(int)Keys.Down])
                dy += 32;

            gameView?.NotifyDrag(0, 0, -dx, dy, Event.Button.Right);
        }

        private void RenderControl_KeyUp(object sender, KeyEventArgs e)
        {
            KeysDown[(int)e.KeyCode] = false;
        }

        void RenderControl_KeyDown(object sender, KeyEventArgs e)
        {
            KeysDown[(int)e.KeyCode] = true;

            try
            {
                if (e.Control && e.KeyCode == Keys.F)
                {
                    ToggleFullscreen();
                    e.Handled = true;
                    return;
                }

                switch (e.KeyCode)
                {
                    case Keys.Left:
                        gameView?.NotifyKeyPressed((char)e.KeyValue, 0);
                        HandleKeyDrag();
                        e.Handled = true;
                        break;
                    case Keys.Right:
                        gameView?.NotifyKeyPressed((char)e.KeyValue, 0);
                        HandleKeyDrag();
                        e.Handled = true;
                        break;
                    case Keys.Up:
                        gameView?.NotifyKeyPressed((char)e.KeyValue, 0);
                        HandleKeyDrag();
                        e.Handled = true;
                        break;
                    case Keys.Down:
                        gameView?.NotifyKeyPressed((char)e.KeyValue, 0);
                        HandleKeyDrag();
                        e.Handled = true;
                        break;
                    case Keys.F10:
                        gameView?.NotifyKeyPressed('n', 1);
                        e.Handled = true;
                        break;
                    case Keys.F11:
                        ToggleFullscreen();
                        e.Handled = true;
                        break;
                    case Keys.Enter:
                        gameView?.NotifyKeyPressed(Event.SystemKeys.Return, 0);
                        e.SuppressKeyPress = true;
                        e.Handled = true;
                        break;
                    case Keys.Back:
                        gameView?.NotifyKeyPressed(Event.SystemKeys.Backspace, 0);
                        e.Handled = true;
                        break;
                    case Keys.Delete:
                        gameView?.NotifyKeyPressed(Event.SystemKeys.Delete, 0);
                        e.Handled = true;
                        break;
                    default:
                        {
                            if (e.KeyValue < 128) // only valid ascii characters
                            {
                                byte modifier = 0;

                                if (e.Control)
                                    modifier |= 1;
                                if (e.Shift)
                                    modifier |= 2;
                                if (e.Alt)
                                    modifier |= 4;

                                gameView?.NotifyKeyPressed((char)e.KeyValue, modifier);
                                e.Handled = true;
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

        void FreeserfForm_KeyDown(object sender, KeyEventArgs e)
        {
            RenderControl_KeyDown(sender, e); // forward form key down to render control key down
        }

        void RenderControl_MouseClick(object sender, MouseEventArgs e)
        {
            clickWaitTimer.Tag = e;
            clickWaitTimer.Start();
        }

        void RenderControl_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            clickWaitTimer.Stop();

            try
            {
                gameView?.NotifyDoubleClick(e.X, e.Y, ConvertMouseButton(e.Button));
            }
            catch (Exception ex)
            {
                ReportException("MouseDoubleClick", ex);
            }
        }

        void ClickWaitTimer_Tick(object sender, EventArgs e)
        {
            MouseEventArgs args = clickWaitTimer.Tag as MouseEventArgs;

            clickWaitTimer.Stop();

            try
            {
                gameView?.NotifyClick(args.X, args.Y, ConvertMouseButton(args.Button));
            }
            catch (Exception ex)
            {
                ReportException("MouseClick", ex);
            }
        }

        void ReportException(string source, Exception exception)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate
                {
                    ReportException(source, exception);
                }));

                return;
            }

            if (clickWaitTimer != null && clickWaitTimer.Enabled)
                clickWaitTimer.Stop();

            if (FrameTimer != null && FrameTimer.Enabled)
                FrameTimer.Stop();

            Log.Error.Write(source, exception.Message);

            SetFullscreen(false, false);

            if (crashHandlerForm.RaiseException(exception) == UI.CrashReaction.Restart)
                Process.Start(Assembly.GetEntryAssembly().Location);

            Close();
        }

        void RenderControl_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                switch (e.KeyChar)
                {
                    case '<':
                        ZoomOut();
                        e.Handled = true;
                        break;
                    case '>':
                        ZoomIn();
                        e.Handled = true;
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
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                ReportException("KeyPress", ex);
            }
        }

        void FreeserfForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            RenderControl_KeyPress(sender, e); // forward form key press to render control key press
        }

        void RenderControl_MouseEnter(object sender, EventArgs e)
        {
            Cursor.Hide();
        }

        void RenderControl_MouseLeave(object sender, EventArgs e)
        {
            Cursor.Show();
        }

        private void FreeserfForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // TODO: Ask for saving?

            if (clickWaitTimer.Enabled)
                clickWaitTimer.Stop();

            if (FrameTimer.Enabled)
                FrameTimer.Stop();

            if (debugConsole != null)
                debugConsole.Close();

            Cursor.Show();

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

        private void RenderControl_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                    e.IsInputKey = true;
                    break;
            }
        }
    }
}
