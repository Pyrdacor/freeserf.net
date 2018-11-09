using System;
using System.Windows.Forms;
using Freeserf.Renderer.OpenTK;
using Orientation = Freeserf.Renderer.OpenTK.Orientation;
using Freeserf.Render;

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
            Log.SetFile(System.IO.File.Create(@"D:\Programmierung\C#\Projects\freeserf.net\Freeserf.net\log.txt"));
            Log.SetLevel(Log.Level.Verbose);

            // TODO: for now we just load DOS data (test path)
            DataSourceDos dosData = new DataSourceDos(@"D:\Programmierung\C#\Projects\freeserf.net\Freeserf.net\SPAE.PA");

            if (!dosData.Load())
            {
                MessageBox.Show(this, "Error loading DOS data.", "Error");
                Close();
            }

            gameView = new GameView(new Size(1024, 768), DeviceType.Desktop, SizingPolicy.FitRatio, OrientationPolicy.Fixed);

            gameView.Resize(RenderControl.Width, RenderControl.Height, Orientation.LandscapeLeftRight);

            TextureAtlasManager.RegisterFactory(new TextureAtlasBuilderFactory());

            // add all normal landscape tile sprites
            var color = new Sprite.Color() { Red = 0, Green = 0, Blue = 0, Alpha = 255 };
            for (uint i = 0; i < 33; ++i) // 33 map tile sprites
                TextureAtlasManager.Instance.AddSprite((int)Layer.Landscape, i, dosData.GetSprite(Data.Resource.MapGround, i, color));

            // TODO: create texture atlas for every layer
            Renderer.OpenTK.Texture textureDummy = null; // dummy

            // TODO: color keys?
            var layerLandscape = new RenderLayer(Layer.Landscape, Shape.Triangle, TextureAtlasManager.Instance.GetOrCreate((int)Layer.Landscape).Texture as Renderer.OpenTK.Texture);
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

            Game game = new Game(gameView);

            game.Init(10, new Random("3762665523225478")); // mission 1

            game.Map.AttachToRenderLayer(layerLandscape);

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
