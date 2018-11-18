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
        Game game = null;

        public FreeserfForm()
        {
            InitializeComponent();
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

            gameView = new GameView(dosData, new Size(1280, 960), DeviceType.Desktop, SizingPolicy.FitRatio, OrientationPolicy.Fixed);

            gameView.Resize(RenderControl.Width, RenderControl.Height, Orientation.LandscapeLeftRight);          

            // TODO: REMOVE initialization stuff
            var random = new Random();
            var gameInfo = new GameInfo(random);

            if (!GameManager.Instance.StartGame(gameInfo.GetMission(29), gameView))
                throw new ExceptionFreeserf("Failed to start game.");

            game = GameManager.Instance.GetCurrentGame();

            game.Map.AttachToRenderLayer(gameView.GetLayer(Layer.Landscape), dosData);

            var pos = game.GetPlayer(0u).CastlePos;
            uint column = game.Map.PosColumn(pos);
            uint row = game.Map.PosRow(pos);

            game.Map.ScrollTo(column, row);

            RenderControl.MouseWheel += RenderControl_MouseWheel;

            FrameTimer.Start();
        }

        private void FrameTimer_Tick(object sender, EventArgs e)
        {
            RenderControl.MakeCurrent();

            game.Update();
            gameView.Render();

            RenderControl.SwapBuffers();
        }

        void ToggleFullscreen()
        {
            // TODO
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

        private void RenderControl_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta < 0)
                ZoomOut();
            else if (e.Delta > 0)
                ZoomIn();
        }

        int lastX = int.MinValue;
        int lastY = int.MinValue;

        private void RenderControl_MouseMove(object sender, MouseEventArgs e)
        {
            /*Position pos = gameView.ScreenToView(new Position(e.X, e.Y));

            if (e.Button == MouseButtons.Right)
            {
                if (pos == null || lastX == int.MinValue)
                    return;

                int diffX = pos.X - lastX;
                int diffY = pos.Y - lastY;
                int scrollX = diffX / 32;
                int scrollY = diffY / 20;

                game.Map.Scroll(-scrollX, -scrollY);

                int remainingX = diffX % 32;
                int remainingY = diffY % 20;

                lastX = pos.X - remainingX;
                lastY = pos.Y - remainingY;
            }
            else
            {
                lastX = int.MinValue;
            }*/
        }

        private void RenderControl_MouseDown(object sender, MouseEventArgs e)
        {
            /*lastX = int.MinValue;

            Position pos = gameView.ScreenToView(new Position(e.X, e.Y));

            if (pos == null)
                return;

            lastX = pos.X;
            lastY = pos.Y;*/

            
        }

        private void RenderControl_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    gameView.NotifyDrag(0, 0, -32, 0, Event.Button.Left);
                    e.Handled = true;
                    break;
                case Keys.Right:
                    gameView.NotifyDrag(0, 0, 32, 0, Event.Button.Left);
                    e.Handled = true;
                    break;
                case Keys.Up:
                    gameView.NotifyDrag(0, 0, 0, -32, Event.Button.Left);
                    e.Handled = true;
                    break;
                case Keys.Down:
                    gameView.NotifyDrag(0, 0, 0, 32, Event.Button.Left);
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
                    gameView.NotifyKeyPressed('n', 1);
                    e.Handled = true;
                    break;
                case Keys.F11:
                    ToggleFullscreen();
                    e.Handled = true;
                    break;
                case Keys.Enter:
                    gameView.NotifyKeyPressed('\n', 0);
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

                            gameView.NotifyKeyPressed((char)e.KeyValue, modifier);
                            e.Handled = true;
                        }
                    }
                    break;
            }
        }

        private void FreeserfForm_KeyDown(object sender, KeyEventArgs e)
        {
            RenderControl_KeyDown(sender, e); // forward form key down to render control key down
        }

        private void RenderControl_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == (MouseButtons.Left | MouseButtons.Right))
                gameView.NotifySpecialClick(e.X, e.Y);
            else
                gameView.NotifyClick(e.X, e.Y, ConvertMouseButton(e.Button));
        }

        private void RenderControl_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            gameView.NotifyDoubleClick(e.X, e.Y, ConvertMouseButton(e.Button));
        }

        private void RenderControl_KeyPress(object sender, KeyPressEventArgs e)
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
                    gameView.NotifyKeyPressed(e.KeyChar, 0);
                    e.Handled = true;
                    break;
            }
        }

        private void FreeserfForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            RenderControl_KeyPress(sender, e); // forward form key press to render control key press
        }
    }
}
