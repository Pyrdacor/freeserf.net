using System;
using System.IO;
using System.Windows.Forms;
using Freeserf.Renderer.OpenTK;
using Orientation = Freeserf.Renderer.OpenTK.Orientation;
using Freeserf.Render;

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

        public FreeserfForm()
        {
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
            Log.SetFile(File.Create(Path.Combine(Program.ExecutablePath, "log.txt")));
            Log.SetLevel(Log.Level.Verbose);

            // TODO: for now we just load DOS data (test path)
            DataSourceDos dosData = new DataSourceDos(Path.Combine(Program.ExecutablePath, "SPAE.PA"));

            if (!dosData.Load())
            {
                MessageBox.Show(this, "Error loading DOS data.", "Error");
                Close();
            }

            SetClientSize(1280, 960);

            gameView = new GameView(dosData, new Size(1280, 960), DeviceType.Desktop, SizingPolicy.FitRatio, OrientationPolicy.Fixed);

            gameView.Resize(RenderControl.Width, RenderControl.Height, Orientation.LandscapeLeftRight);

            gameView.Initialize();
            gameView.Closed += GameView_Closed;

            RenderControl.MouseWheel += RenderControl_MouseWheel;

            clickWaitTimer.Tick += ClickWaitTimer_Tick;
            clickWaitTimer.Interval = 130;

            FrameTimer.Interval = Global.TICK_LENGTH;
            FrameTimer.Start();
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
                Log.Error.Write("render", ex.Message);
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);                
                Close();
                return;
            }

            RenderControl.SwapBuffers();
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            FormWindowState previousWindowState = this.WindowState;

            base.WndProc(ref m);

            FormWindowState currentWindowState = this.WindowState;

            if (previousWindowState != currentWindowState)
            {
                if (currentWindowState == FormWindowState.Maximized)
                    SetFullscreen(true);
                else if (currentWindowState == FormWindowState.Normal)
                    SetFullscreen(false);
            }
        }

        void HandleFullscreenChange()
        {
            if (fullscreen)
            {
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                gameView.Resize(RenderControl.Width, RenderControl.Height, Orientation.LandscapeLeftRight);
                TopMost = true;
                BringToFront();
            }
            else
            {
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                SetClientSize(1280, 960);
                gameView.Resize(RenderControl.Width, RenderControl.Height, Orientation.LandscapeLeftRight);
                TopMost = false;
            }
        }

        void SetFullscreen(bool fullscreen)
        {
            this.fullscreen = fullscreen;
            HandleFullscreenChange();
        }

        void ToggleFullscreen()
        {
            fullscreen = !fullscreen;
            HandleFullscreenChange();
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
            if (e.Delta < 0)
                ZoomOut();
            else if (e.Delta > 0)
                ZoomIn();
        }

        void RenderControl_MouseMove(object sender, MouseEventArgs e)
        {
            pressedMouseButtons = e.Button;

            if (e.Button == MouseButtons.Right)
            {
                if (lastDragX == int.MinValue)
                    return;

                gameView.NotifyDrag(0, 0, lastDragX - e.X, lastDragY - e.Y, Event.Button.Right);
                lastDragX = e.X;
                lastDragY = e.Y;
            }
            else
            {
                lastDragX = int.MinValue;
                lastDragY = int.MinValue;
            }
        }

        void RenderControl_MouseDown(object sender, MouseEventArgs e)
        {
            /*lastX = int.MinValue;

            Position pos = gameView.ScreenToView(new Position(e.X, e.Y));

            if (pos == null)
                return;

            lastX = pos.X;
            lastY = pos.Y;*/

            if (e.Button == MouseButtons.Right)
            {
                lastDragX = e.X;
                lastDragY = e.Y;
            }

            pressedMouseButtons |= e.Button;

            if (pressedMouseButtons == (MouseButtons.Left | MouseButtons.Right))
            {
                gameView?.NotifySpecialClick(e.X, e.Y);
                pressedMouseButtons = MouseButtons.None;
            }
        }

        void RenderControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.None)
                pressedMouseButtons &= ~e.Button;
        }

        void RenderControl_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    gameView?.NotifyDrag(0, 0, -32, 0, Event.Button.Left);
                    e.Handled = true;
                    break;
                case Keys.Right:
                    gameView?.NotifyDrag(0, 0, 32, 0, Event.Button.Left);
                    e.Handled = true;
                    break;
                case Keys.Up:
                    gameView?.NotifyDrag(0, 0, 0, -32, Event.Button.Left);
                    e.Handled = true;
                    break;
                case Keys.Down:
                    gameView?.NotifyDrag(0, 0, 0, 32, Event.Button.Left);
                    e.Handled = true;
                    break;
                case Keys.F:
                    if (e.Control)
                    {
                        ToggleFullscreen();
                        e.Handled = true;
                    }
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
                    gameView?.NotifyKeyPressed('\n', 0);
                    e.SuppressKeyPress = true;
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

            gameView?.NotifyDoubleClick(e.X, e.Y, ConvertMouseButton(e.Button));
        }

        void ClickWaitTimer_Tick(object sender, EventArgs e)
        {
            MouseEventArgs args = clickWaitTimer.Tag as MouseEventArgs;

            clickWaitTimer.Stop();

            gameView?.NotifyClick(args.X, args.Y, ConvertMouseButton(args.Button));
        }

        void RenderControl_KeyPress(object sender, KeyPressEventArgs e)
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

        void FreeserfForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            RenderControl_KeyPress(sender, e); // forward form key press to render control key press
        }

        void FreeserfForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (clickWaitTimer.Enabled)
                clickWaitTimer.Stop();

            if (FrameTimer.Enabled)
                FrameTimer.Stop();
        }
    }
}
