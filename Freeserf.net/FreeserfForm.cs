using System;
using System.Windows.Forms;
using Freeserf.Renderer.OpenTK;
using Orientation = Freeserf.Renderer.OpenTK.Orientation;

namespace Freeserf
{
    public partial class FreeserfForm : Form
    {
        GameView gameView = null;   

        public FreeserfForm()
        {
            InitializeComponent();
        }

        private void FreeserfForm_Load(object sender, EventArgs e)
        {
            gameView = new GameView(new Size(1024, 768), DeviceType.Desktop, SizingPolicy.FitRatio, OrientationPolicy.Fixed);

            gameView.Resize(RenderControl.Width, RenderControl.Height, Orientation.LandscapeLeftRight);

            // TODO: create texture atlas for every layer
            Renderer.OpenTK.Texture textureDummy = null; // dummy

            // TODO: color keys?
            var layerLandscape = new RenderLayer(Layer.Landscape, Shape.Triangle, textureDummy);
            var layerGrid = new RenderLayer(Layer.Grid, Shape.Triangle, textureDummy);
            var layerPaths = new RenderLayer(Layer.Paths, Shape.Rect, textureDummy);
            var layerSerfsBehind = new RenderLayer(Layer.SerfsBehind, Shape.Rect, textureDummy);
            var layerObjects = new RenderLayer(Layer.Objects, Shape.Rect, textureDummy);
            var layerSerfs = new RenderLayer(Layer.Serfs, Shape.Rect, textureDummy);
            var layerBuilds = new RenderLayer(Layer.Builds, Shape.Rect, textureDummy);
            var layerCursor = new RenderLayer(Layer.Cursor, Shape.Rect, textureDummy);

            gameView.AddLayer(layerLandscape);
            gameView.AddLayer(layerGrid);
            gameView.AddLayer(layerPaths);
            gameView.AddLayer(layerSerfsBehind);
            gameView.AddLayer(layerObjects);
            gameView.AddLayer(layerSerfs);
            gameView.AddLayer(layerBuilds);
            gameView.AddLayer(layerCursor);

            // Example for adding a sprite
            // var serfSprite = new Sprite(32, 34, 0, 0);
            // serfSprite.Visible = true;
            // serfSprite.Layer = layerSerfs;

            FrameTimer.Start();
        }

        private void FrameTimer_Tick(object sender, EventArgs e)
        {
            RenderControl.MakeCurrent();

            gameView.Render();

            RenderControl.SwapBuffers();
        }
    }
}
