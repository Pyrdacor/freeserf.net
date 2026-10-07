/*
 * MainActivity.cs - Android host for freeserf.net
 *
 * Copyright (C) 2024  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Freeserf;
using Freeserf.Renderer;
using Silk.NET.Input;
using Silk.NET.Input.Sdl;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;
using Silk.NET.Windowing.Sdl.Android;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Numerics;
using System.Text;

#if DEBUG
[assembly: Application(Debuggable = true)]
#else
[assembly: Application(Debuggable = false)]
#endif

namespace Freeserf.Android
{
    [Activity(Label = "@string/app_name", MainLauncher = true,
        ScreenOrientation = ScreenOrientation.Landscape, Exported = true)]
    public class MainActivity : SilkActivity
    {
        static IView view;
        static IInputContext input;
        static bool textInputFocusSubscribed = false;
        static GameView gameView;
        static Data.DataSource dataSource;
        static Global.InitInfo initInfo;
        static bool initialized = false;
        static bool renderTraced = false;
        static bool firstFrameRendered = false;

        // Receives network data on the SDL thread (see Window_Update). Without
        // this the multiplayer server/client has no data receiver and crashes
        // when a remote participant sends data.
        static Network.INetworkDataReceiver networkDataReceiver;

        // Loading overlay shown while the game initializes (game data loading
        // and shader/atlas setup can take several seconds on slow devices).
        // It is a native Android view on top of the SDL surface and is hidden
        // once the first frame (main menu) has been rendered.
        static View loadingOverlay;
        static bool loadingOverlayVisible = false;

        // Game data import (first start without bundled data). The copyrighted
        // SPAE.PA file is not shipped with the APK, so on first start the user
        // is asked to pick their own data file via the system file picker.
        const int RequestImportData = 1001;
        static bool dataImported = false;

        // The activity instance, so static handlers (e.g. GameView.Closed)
        // can close the app via Finish().
        static MainActivity instance;

        // Tracks the Android activity lifecycle so the render loop can stop
        // swapping buffers while the EGL surface is being destroyed/recreated.
        enum ActivityState
        {
            Active,
            Paused,
            Stopped
        }

        static volatile ActivityState activityState = ActivityState.Active;

        // mouse emulation state (SDL maps single finger touch to left mouse button)
        static int lastDragX = int.MinValue;
        static int lastDragY = int.MinValue;
        static bool scrolled = false;

        // pinch-to-zoom state. Written on the UI thread in DispatchTouchEvent,
        // read on the SDL thread in Window_Update (GL state must only be
        // touched on the SDL thread).
        static volatile bool pinchActive = false;
        static volatile float pinchStartDistance = 0.0f;
        static volatile float pinchStartZoom = 0.0f;
        static volatile float pinchCurrentDistance = 0.0f;
        static volatile bool touchInputEnabled = false;
        static volatile float currentZoom = 0.0f;
        static readonly ConcurrentQueue<Action<GameView>> pendingTouchEvents = new();

        // single-finger pan state (UI thread). A tap (no significant movement)
        // becomes a left click; a drag pans the map like a right-button drag.
        static int touchStartX = 0;
        static int touchStartY = 0;
        static int touchLastX = 0;
        static int touchLastY = 0;
        static bool touchActive = false;
        static bool touchPanning = false;
        static bool touchPanAllowed = false;
        static bool longPressFired = false;
        const int LongPressDelayMs = 400;
        static Handler longPressHandler;
        static Handler delayedClickHandler;
        static Java.Lang.Runnable delayedClickRunnable;
        static bool delayedClickPending = false;
        static int lastTapX = 0;
        static int lastTapY = 0;
        static long lastTapTime = 0;
        static Java.Lang.Runnable longPressRunnable;

        public MainActivity()
        {
            instance = this;
            Console.SetOut(new AndroidConsole("Freeserf_Info"));
            Console.SetError(new AndroidConsole("Freeserf_Error"));
            Log.SetStream(new ConsoleStream(Console.Error));
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Show a loading indicator while the game initializes. On activity
            // recreation (e.g. configuration change) the game is already
            // running, so the overlay is only shown on the very first start.
            if (!initialized)
                ShowLoadingOverlay();
        }

        // Shows the loading overlay (spinner + app name) on top of the SDL
        // surface. Runs on the UI thread (RunOnUiThread is a no-op there).
        static void ShowLoadingOverlay()
        {
            if (loadingOverlayVisible)
                return;
            loadingOverlayVisible = true;

            instance?.RunOnUiThread(() =>
            {
                if (loadingOverlay != null || instance == null)
                    return;

                var layout = new LinearLayout(instance)
                {
                    Orientation = global::Android.Widget.Orientation.Vertical
                };
                layout.SetBackgroundColor(global::Android.Graphics.Color.Black);
                layout.SetGravity(GravityFlags.Center);

                var spinner = new ProgressBar(instance) { Indeterminate = true };
                layout.AddView(spinner, new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));

                var title = new TextView(instance)
                {
                    Text = "Freeserf",
                    TextSize = 28,
                    Gravity = GravityFlags.Center
                };
                title.SetTextColor(global::Android.Graphics.Color.White);
                var titleParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                titleParams.TopMargin = DpToPx(24);
                layout.AddView(title, titleParams);

                var subtitle = new TextView(instance)
                {
                    Text = "Lädt…",
                    TextSize = 16,
                    Gravity = GravityFlags.Center
                };
                subtitle.SetTextColor(global::Android.Graphics.Color.Argb(255, 180, 180, 180));
                var subtitleParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                subtitleParams.TopMargin = DpToPx(8);
                layout.AddView(subtitle, subtitleParams);

                loadingOverlay = layout;
                instance.AddContentView(layout, new FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
            });
        }

        // Removes the loading overlay from the view hierarchy. Runs on the UI
        // thread (RunOnUiThread is a no-op there).
        static void HideLoadingOverlay()
        {
            if (!loadingOverlayVisible)
                return;
            loadingOverlayVisible = false;

            instance?.RunOnUiThread(() =>
            {
                if (loadingOverlay == null)
                    return;
                (loadingOverlay.Parent as ViewGroup)?.RemoveView(loadingOverlay);
                loadingOverlay = null;
            });
        }

        static int DpToPx(int dp)
        {
            return (int)(dp * instance.Resources.DisplayMetrics.Density);
        }

        protected override void OnRun()
        {
            try
            {
                global::Android.Util.Log.Debug("Freeserf_Trace", "OnRun: start");
                SdlWindowing.RegisterPlatform();
                SdlInput.RegisterPlatform();
                SdlWindowing.Use();
                global::Android.Util.Log.Debug("Freeserf_Trace", "OnRun: platforms registered");

                var options = new WindowOptions(
                    true,
                    new Vector2D<int>(0, 0),
                    new Vector2D<int>(1280, 960),
                    60.0,
                    60.0,
                    new GraphicsAPI(ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default, new APIVersion(3, 0)),
                    Global.VERSION,
                    WindowState.Normal,
                    WindowBorder.Fixed,
                    true,
                    false,
                    new VideoMode(60),
                    24);

                view = Silk.NET.Windowing.Window.GetView(new ViewOptions(options));
                global::Android.Util.Log.Debug("Freeserf_Trace", "OnRun: view created");
                view.Load += Window_Load;
                view.Render += Window_Render;
                view.Update += Window_Update;
                view.Resize += Window_Resize;
                view.Closing += Window_Closing;

                global::Android.Util.Log.Debug("Freeserf_Trace", "OnRun: events attached, calling Initialize");
                view.Initialize();
                global::Android.Util.Log.Debug("Freeserf_Trace", "OnRun: initialized, calling Run");
                view.Run(() =>
                {
                    try
                    {
                        // Pump SDL events. On Android this drives the EGL surface
                        // lifecycle (pause/resume coordination with the Java side);
                        // without it the surface is destroyed while we keep
                        // swapping, causing EGL_BAD_SURFACE.
                        view.DoEvents();
                        if (!view.IsClosing)
                            view.DoUpdate();
                        if (!view.IsClosing)
                            view.DoRender();
                    }
                    catch (Exception ex)
                    {
                        global::Android.Util.Log.Debug("Freeserf_Trace", "Run loop EXCEPTION: " + ex);
                        Log.Error.Write(ErrorSystemType.Application, "Run loop: " + ex);
                        throw;
                    }
                });
                global::Android.Util.Log.Debug("Freeserf_Trace", "OnRun: Run returned");
                view.Reset();
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Application, "Run: " + ex);
            }
            finally
            {
                try
                {
                    view?.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error.Write(ErrorSystemType.Application, "View disposal: " + ex);
                }
            }
        }

        static void Window_Load()
        {
            try
            {
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: start");
                view.MakeCurrent();
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: MakeCurrent done");

                // The TextureAtlasManager is a static singleton that persists across
                // activity recreations. On recreation the EGL context is lost (GPU
                // textures invalid), so the atlases must be rebuilt from scratch.
                Freeserf.Render.TextureAtlasManager.Instance.Reset();
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: TextureAtlasManager.Reset done");

                initInfo = Global.Init(Array.Empty<string>());
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: Global.Init done");

                Network.Network.DefaultClientFactory = new Network.ClientFactory();
                Network.Network.DefaultServerFactory = new Network.ServerFactory();
                networkDataReceiver = new Network.NetworkDataReceiverFactory().CreateReceiver();

                UserConfig.Load(FileSystem.Paths.UserConfigPath);
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: UserConfig.Load done");

                // Extract bundled game data (e.g. SPAE.PA) from the APK assets
                // to the app's private storage so the data source can find it.
                ExtractBundledData();
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: ExtractBundledData done");

                if (!TryLoadGameData())
                {
                    // No game data available (not bundled, not imported yet).
                    // Ask the user to pick their own data file. The copyrighted
                    // data file is never shipped with the APK.
                    HideLoadingOverlay();
                    instance?.RunOnUiThread(() => instance.ShowDataImportDialog());
                    return;
                }

                InitializeAfterDataLoad();
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: EXCEPTION: " + ex);
                Log.Error.Write(ErrorSystemType.Application, "Load: " + ex.Message);
            }
        }

        static bool TryLoadGameData()
        {
            try
            {
                var data = Data.Data.GetInstance();
                if (!data.Load(FileSystem.Paths.GameDataFolder, UserConfig.Game.GraphicDataUsage,
                    UserConfig.Game.SoundDataUsage, UserConfig.Game.MusicDataUsage))
                {
                    Log.Error.Write(ErrorSystemType.Data, "Error loading game data.");
                    global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: data.Load FAILED");
                    return false;
                }
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: data.Load done");
                dataSource = data.GetDataSource();
                return true;
            }
            catch (Exception ex)
            {
                // e.g. UnauthorizedAccessException while scanning fallback paths
                // when no data file is present. Treat as "no data available" so
                // the user is asked to import their own data file.
                Log.Error.Write(ErrorSystemType.Data, "Error loading game data: " + ex.Message);
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: data.Load EXCEPTION: " + ex.Message);
                return false;
            }
        }

        // Runs on the SDL thread (called from Window_Load or Window_Update after
        // the user imported game data). Requires the GL context to be current.
        static void InitializeAfterDataLoad()
        {
            if (initialized)
                return;

            // Re-validate the game data (e.g. after the user imported a file).
            // If the imported file is not valid game data, ask again.
            if (!TryLoadGameData())
            {
                instance?.RunOnUiThread(() => instance.ShowDataImportDialog());
                return;
            }

            view.MakeCurrent();

            if (initInfo.ScreenWidth == -1)
                initInfo.ScreenWidth = UserConfig.Video.ResolutionWidth;
            if (initInfo.ScreenHeight == -1)
                initInfo.ScreenHeight = UserConfig.Video.ResolutionHeight;

            State.Init(view);
            global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: State.Init done");
            try
            {
                global::Android.Util.Log.Debug("Freeserf_Trace", $"Window_Load: glError after State.Init = {Freeserf.Renderer.State.Gl.GetError()}");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: glError query failed: " + ex.Message);
            }

            // Compute widescreen virtual screen size from actual view, preserving aspect ratio,
            // capped at MAX_VIRTUAL_SCREEN_WIDTH (1920 for Pixel 8a -> 1920x864).
            int screenW = view.Size.X, screenH = view.Size.Y;
            if (screenH > screenW) { int t = screenW; screenW = screenH; screenH = t; } // ensure landscape
            int virtualWidth = Math.Min(screenW, Global.MAX_VIRTUAL_SCREEN_WIDTH);
            int virtualHeight = Math.Max(1, (int)Math.Round(virtualWidth * (double)screenH / screenW));
            global::Android.Util.Log.Debug("Freeserf_Trace", $"Window_Load: virtual screen = {virtualWidth}x{virtualHeight}");

            GuiScaling.TouchMode = true; // larger GUI for touch screens

            gameView = new GameView(dataSource, new Size(virtualWidth, virtualHeight),
                DeviceType.MobileLandscape, SizingPolicy.FitRatio, OrientationPolicy.Support180DegreeRotation);
            gameView.Resize(view.Size.X, view.Size.Y);
            gameView.Closed += GameView_Closed;
            global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: GameView created");

            input = view.CreateInput();
            input.Mice[0].MouseDown += Mouse_MouseDown;
            input.Mice[0].MouseUp += Mouse_MouseUp;
            input.Mice[0].MouseMove += Mouse_MouseMove;
            input.Mice[0].Scroll += Mouse_Scroll;
            input.Keyboards[0].KeyDown += Keyboard_KeyDown;
            input.Keyboards[0].KeyChar += Keyboard_KeyChar;

            // Show/hide the on-screen keyboard when a text input (e.g. the save
            // game name field) gains/loses focus. Without this the user cannot
            // type a save game name on Android.
            if (!textInputFocusSubscribed)
            {
                Freeserf.UI.Gui.TextInputFocusChanged += OnTextInputFocusChanged;
                textInputFocusSubscribed = true;
            }

            initialized = true;
            global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: done, initialized=true");

            try
            {
                var glErr = Freeserf.Renderer.State.Gl.GetError();
                global::Android.Util.Log.Debug("Freeserf_Trace", $"Window_Load: glError after init = {glErr}");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: glError query failed: " + ex.Message);
            }
        }

        // Shows the system file picker so the user can import their own game
        // data file (e.g. SPAE.PA). Runs on the UI thread.
        void ShowDataImportDialog()
        {
            Toast.MakeText(this, "Bitte wählen Sie die Spieldatei (z.B. SPAE.PA) aus.", ToastLength.Long).Show();

            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");
            StartActivityForResult(intent, RequestImportData);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode != RequestImportData)
                return;

            if (resultCode == Result.Ok && data?.Data != null)
            {
                try
                {
                    CopyImportedData(data.Data);
                    dataImported = true;
                    // Loading the imported data and initializing the game can
                    // take a moment; show the overlay again until the first
                    // frame (main menu) is rendered.
                    ShowLoadingOverlay();
                }
                catch (Exception ex)
                {
                    Log.Error.Write(ErrorSystemType.Data, "Failed to import game data: " + ex.Message);
                    instance?.RunOnUiThread(() => instance.ShowDataImportDialog());
                }
            }
            else
            {
                // User cancelled the picker; ask again.
                instance?.RunOnUiThread(() => instance.ShowDataImportDialog());
            }
        }

        void CopyImportedData(global::Android.Net.Uri uri)
        {
            string fileName = GetFileName(uri);

            // The data loader only looks for the known file names, so fall back
            // to SPAE.PA if the user picked a differently named file.
            if (!IsKnownDataFileName(fileName))
                fileName = "SPAE.PA";

            string targetPath = Path.Combine(FileSystem.Paths.GameDataFolder, fileName);

            using (var input = ContentResolver.OpenInputStream(uri))
            using (var output = File.Create(targetPath))
            {
                input.CopyTo(output);
            }

            Log.Info.Write(ErrorSystemType.Data, $"Imported game data '{fileName}'.");
        }

        string GetFileName(global::Android.Net.Uri uri)
        {
            string name = "SPAE.PA";

            using (var cursor = ContentResolver.Query(uri, null, null, null, null))
            {
                if (cursor != null && cursor.MoveToFirst())
                {
                    int nameIndex = cursor.GetColumnIndex(IOpenableColumns.DisplayName);
                    if (nameIndex >= 0)
                        name = cursor.GetString(nameIndex);
                }
            }

            return name;
        }

        static bool IsKnownDataFileName(string name)
        {
            name = name.ToLowerInvariant();
            return name == "spae.pa" || name == "spad.pa" || name == "spaf.pa" || name == "spau.pa";
        }

        static void Window_Render(double delta)
        {
            if (!initialized)
                return;

            // While the activity is paused/stopped the EGL surface is being
            // destroyed (or is gone); rendering and swapping against it would
            // raise EGL_BAD_SURFACE. Skip until the activity is active again.
            if (activityState != ActivityState.Active)
                return;

            try
            {
                gameView?.Render();

                // The first rendered frame shows the main menu; the loading
                // overlay is no longer needed from here on.
                if (!firstFrameRendered)
                {
                    firstFrameRendered = true;
                    HideLoadingOverlay();
                }

                if (!renderTraced)
                {
                    renderTraced = true;
                    global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Render: first render call");
                    try
                    {
                        var game = GameManager.Instance.GetCurrentGame();
                        var landscape = gameView?.GetLayer(Freeserf.Layer.Landscape);
                        var guiLayer = gameView?.GetLayer(Freeserf.Layer.Gui);
                        int landscapeCount = (landscape is Freeserf.Renderer.RenderLayer rl) ? rl.GetDrawCount() : -1;
                        var glError = Freeserf.Renderer.State.Gl.GetError();
                        int maxTexSize = Freeserf.Renderer.State.Gl.GetInteger(Silk.NET.OpenGL.GLEnum.MaxTextureSize);
                        string atlasInfo = "n/a";
                        if (landscape is Freeserf.Renderer.RenderLayer rl2 && rl2.DebugTexture != null)
                        {
                            atlasInfo = $"{rl2.DebugTexture.Width}x{rl2.DebugTexture.Height}";
                        }
                        global::Android.Util.Log.Debug("Freeserf_Trace",
                            $"Window_Render: state game={(game == null ? "null" : "ok")} " +
                            $"landscapeVisible={landscape?.Visible} landscapeDrawCount={landscapeCount} " +
                            $"guiVisible={guiLayer?.Visible} viewSize={view?.Size.X}x{view?.Size.Y} " +
                            $"glError={glError} maxTexSize={maxTexSize} landscapeAtlas={atlasInfo} " +
                            $"gl={Freeserf.Renderer.State.OpenGLVersionMajor}.{Freeserf.Renderer.State.OpenGLVersionMinor} " +
                            $"gles={Freeserf.Renderer.State.IsOpenGLES}");
                    }
                    catch (Exception ex)
                    {
                        global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Render: state dump failed: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Application, "Render: " + ex.Message);
                return;
            }

            view.SwapBuffers();
        }
        static void Window_Update(double delta)
        {
            // If the user just imported game data, continue the initialization
            // that was deferred in Window_Load (runs on the SDL thread).
            if (dataImported && !initialized)
            {
                dataImported = false;
                InitializeAfterDataLoad();
            }

            var currentGameView = gameView;

            if (currentGameView != null)
            {
                touchInputEnabled = currentGameView.CanZoom;
                currentZoom = currentGameView.Zoom;

                while (pendingTouchEvents.TryDequeue(out var touchEvent))
                {
                    try
                    {
                        touchEvent(currentGameView);
                    }
                    catch (Exception ex)
                    {
                        global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Update: touch event EXCEPTION: " + ex);
                        Log.Error.Write(ErrorSystemType.Application, "Touch event: " + ex);
                    }
                }

                if (pinchActive)
                {
                    // Factor-space math (zoomFactor = 1 + zoom * 0.5) so that
                    // zooming works correctly starting from zoom = 0.
                    float startFactor = 1.0f + pinchStartZoom * 0.5f;
                    float ratio = pinchCurrentDistance / pinchStartDistance;
                    float newZoom = (startFactor * ratio - 1.0f) * 2.0f;
                    currentGameView.Zoom = Math.Clamp(newZoom, 0.0f, 4.0f);
                }

                currentGameView.NetworkDataReceiver = networkDataReceiver;
                currentGameView.UpdateNetworkEvents();
            }
        }

        static void Window_Resize(Vector2D<int> size)
        {
            if (gameView != null)
                gameView.Resize(size.X, size.Y);
        }

        // The game view was closed (e.g. the Exit button in the main menu).
        // Stop the render loop and finish the activity so the app actually
        // closes. view.Close() alone is not enough on Android: it only stops
        // the loop, so Finish() is required to close the activity. The Closed
        // event fires on the SDL thread, hence RunOnUiThread.
        static void GameView_Closed(object sender, EventArgs e)
        {
            if (gameView != null)
            {
                gameView = null;
                view?.Close();
                instance?.RunOnUiThread(() => instance.Finish());
            }
        }

        static void Window_Closing()
        {
            if (gameView != null)
            {
                var viewToClose = gameView;
                gameView = null;
                viewToClose.Close();
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

        // Copies bundled game data files (packaged as APK assets) to the app's
        // private storage on first run. Later this will be replaced by a file
        // picker that lets the user select their own data files.
        static void ExtractBundledData()
        {
            try
            {
                var context = global::Android.App.Application.Context;
                string[] assetNames = context.Assets.List("");

                foreach (var assetName in assetNames)
                {
                    if (!assetName.EndsWith(".PA", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string targetPath = Path.Combine(FileSystem.Paths.GameDataFolder, assetName);

                    if (File.Exists(targetPath))
                        continue;

                    using (var input = context.Assets.Open(assetName))
                    using (var output = File.Create(targetPath))
                    {
                        input.CopyTo(output);
                    }

                    Log.Info.Write(ErrorSystemType.Data, $"Extracted bundled game data '{assetName}'.");
                }
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Data, "Failed to extract bundled game data: " + ex.Message);
            }
        }

        static Event.Button ConvertMouseButtons(MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => Event.Button.Left,
                MouseButton.Right => Event.Button.Right,
                MouseButton.Middle => Event.Button.Middle,
                _ => Event.Button.None
            };
        }

        static void Mouse_MouseDown(IMouse mouse, MouseButton button)
        {
            if (gameView == null)
                return;

            try
            {
                var position = mouse.Position;

                if (button == MouseButton.Left || button == MouseButton.Right)
                {
                    global::Android.Util.Log.Debug("Freeserf_Input", $"Mouse down: {position.X},{position.Y} button={button}");
                    lastDragX = (int)position.X;
                    lastDragY = (int)position.Y;
                    gameView.NotifyClick((int)position.X, (int)position.Y, ConvertMouseButtons(button), false);
                }
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Application, "MouseDown: " + ex.Message);
            }
        }

        static void Mouse_MouseUp(IMouse mouse, MouseButton button)
        {
            if (gameView == null)
                return;

            try
            {
                if (button == MouseButton.Right)
                {
                    lastDragX = int.MinValue;
                    lastDragY = int.MinValue;
                    if (scrolled)
                    {
                        scrolled = false;
                        gameView.NotifyStopDrag();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Application, "MouseUp: " + ex.Message);
            }
        }

        static void Mouse_MouseMove(IMouse mouse, Vector2 position)
        {
            if (gameView == null)
                return;

            try
            {
                int x = (int)position.X;
                int y = (int)position.Y;

                if (mouse.IsButtonPressed(MouseButton.Right))
                {
                    if (lastDragX == int.MinValue)
                        return;

                    bool dragAllowed = gameView.NotifyDrag(x, y, lastDragX - x, lastDragY - y, Event.Button.Right);

                    if (dragAllowed)
                        scrolled = true;

                    lastDragX = x;
                    lastDragY = y;
                }
                else
                {
                    lastDragX = int.MinValue;
                    lastDragY = int.MinValue;
                    gameView.SetCursorPosition(x, y);
                }
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Application, "MouseMove: " + ex.Message);
            }
        }

        static void Mouse_Scroll(IMouse mouse, ScrollWheel scrollWheel)
        {
            if (gameView == null)
                return;

            try
            {
                if (scrollWheel.Y < 0)
                {
                    if (gameView.Zoom > 0.0f)
                        gameView.Zoom -= 0.5f;
                }
                else if (scrollWheel.Y > 0)
                {
                    if (gameView.Zoom < 4.0f)
                        gameView.Zoom += 0.5f;
                }
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Application, "Scroll: " + ex.Message);
            }
        }

        static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int value)
        {
            if (gameView == null)
                return;

            try
            {
                switch (key)
                {
                    case Key.Left:
                        gameView.NotifySystemKeyPressed(Event.SystemKey.Left, 0);
                        break;
                    case Key.Right:
                        gameView.NotifySystemKeyPressed(Event.SystemKey.Right, 0);
                        break;
                    case Key.Up:
                        gameView.NotifySystemKeyPressed(Event.SystemKey.Up, 0);
                        break;
                    case Key.Down:
                        gameView.NotifySystemKeyPressed(Event.SystemKey.Down, 0);
                        break;
                    case Key.PageUp:
                        gameView.NotifySystemKeyPressed(Event.SystemKey.PageUp, 0);
                        break;
                    case Key.PageDown:
                        gameView.NotifySystemKeyPressed(Event.SystemKey.PageDown, 0);
                        break;
                    case Key.Escape:
                        gameView.NotifySystemKeyPressed(Event.SystemKey.Escape, 0);
                        break;
                    case Key.F5:
                        gameView.NotifySystemKeyPressed(Event.SystemKey.F5, 0);
                        break;
                    case Key.F6:
                        gameView.NotifySystemKeyPressed(Event.SystemKey.F6, 0);
                        break;
                    case Key.Enter:
                        gameView.NotifyKeyPressed(Event.SystemKeys.Return, 0);
                        break;
                    case Key.Backspace:
                        gameView.NotifyKeyPressed(Event.SystemKeys.Backspace, 0);
                        break;
                    case Key.Delete:
                        gameView.NotifyKeyPressed(Event.SystemKeys.Delete, 0);
                        break;
                    case Key.Tab:
                        gameView.NotifyKeyPressed(Event.SystemKeys.Tab, 0);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Application, "KeyDown: " + ex.Message);
            }
        }

        static void Keyboard_KeyChar(IKeyboard keyboard, char character)
        {
            if (gameView == null)
                return;

            try
            {
                if (character >= 32 && character < 128)
                    gameView.NotifyKeyPressed(character, 0);
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Application, "KeyChar: " + ex.Message);
            }
        }

        // Fired on the SDL thread when a text input in the game GUI gains/loses
        // focus. BeginInput/EndInput map to SDL_StartTextInput/SDL_StopTextInput,
        // which show/hide the Android on-screen keyboard.
        static void OnTextInputFocusChanged(bool focused)
        {
            try
            {
                if (input == null || input.Keyboards.Count == 0)
                    return;

                if (focused)
                    input.Keyboards[0].BeginInput();
                else
                    input.Keyboards[0].EndInput();
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Application, "Text input focus: " + ex.Message);
            }
        }

        // Touch input runs on the UI thread. It queues GameView work for
        // Window_Update, because GUI event handlers must run on the SDL thread.
        // Tap = left click, long press = special click, drag = pan the map
        // (ingame only), two-finger pinch = zoom (ingame only).
        public override bool DispatchTouchEvent(MotionEvent e)
        {
            if (gameView == null)
                return base.DispatchTouchEvent(e);

            if (e.PointerCount >= 2 && (pinchActive || touchInputEnabled))
            {
                try
                {
                    switch (e.ActionMasked)
                    {
                        case MotionEventActions.PointerDown:
                            CancelLongPress();
                            pinchStartDistance = Math.Max(PinchDistance(e, 0, 1), 1.0f);
                            pinchStartZoom = currentZoom;
                            pinchCurrentDistance = pinchStartDistance;
                            pinchActive = true;
                            // The gesture is no longer a tap; don't click when it ends.
                            touchPanning = true;
                            touchLastX = (int)e.GetX(0);
                            touchLastY = (int)e.GetY(0);
                            break;
                        case MotionEventActions.Move:
                            pinchCurrentDistance = Math.Max(PinchDistance(e, 0, 1), 1.0f);
                            break;
                        case MotionEventActions.PointerUp:
                            pinchActive = e.PointerCount > 2;
                            // At least one finger remains. Keep the first
                            // non-lifted pointer as the drag anchor; this also
                            // handles a third finger lifting.
                            int remainingPointerIndex = e.ActionIndex == 0 ? 1 : 0;
                            touchLastX = (int)e.GetX(remainingPointerIndex);
                            touchLastY = (int)e.GetY(remainingPointerIndex);
                            break;
                        case MotionEventActions.Up:
                        case MotionEventActions.Cancel:
                            pinchActive = false;
                            CancelLongPress();
                            if (touchPanning)
                                pendingTouchEvents.Enqueue(view => view.NotifyStopDrag());
                            touchActive = false;
                            touchPanning = false;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error.Write(ErrorSystemType.Application, "Pinch: " + ex.Message);
                }

                return true;
            }

            // Single finger. Consumed so SDL's mouse emulation doesn't also fire.
            if (e.PointerCount == 1)
            {
                try
                {
                    switch (e.ActionMasked)
                    {
                        case MotionEventActions.Down:
                            touchStartX = touchLastX = (int)e.GetX();
                            touchStartY = touchLastY = (int)e.GetY();
                            global::Android.Util.Log.Debug("Freeserf_Input", $"Touch down: {touchStartX},{touchStartY}");
                            touchActive = true;
                            touchPanning = false;
                            touchPanAllowed = touchInputEnabled;
                            longPressFired = false;
                            StartLongPress();
                            int touchDownX = touchStartX;
                            int touchDownY = touchStartY;
                            pendingTouchEvents.Enqueue(view => view.SetCursorPosition(touchDownX, touchDownY));
                            break;
                        case MotionEventActions.Move:
                            if (!touchActive)
                                break;

                            int x = (int)e.GetX();
                            int y = (int)e.GetY();

                            if (longPressFired)
                                break;

                            if (!touchPanning && touchPanAllowed)
                            {
                                int slop = Math.Max(ViewConfiguration.Get(this).ScaledTouchSlop, 1);
                                if (Math.Abs(x - touchStartX) > slop || Math.Abs(y - touchStartY) > slop)
                                {
                                    touchPanning = true;
                                    CancelLongPress();
                                }
                            }

                            if (touchPanning)
                            {
                                int deltaX = touchLastX - x;
                                int deltaY = touchLastY - y;
                                pendingTouchEvents.Enqueue(view => view.NotifyDrag(x, y, deltaX, deltaY, Event.Button.Right));
                                touchLastX = x;
                                touchLastY = y;
                            }
                            else
                            {
                                pendingTouchEvents.Enqueue(view => view.SetCursorPosition(x, y));
                            }
                            break;
                        case MotionEventActions.Up:
                            CancelLongPress();

                            if (!touchActive)
                                break;

                            if (longPressFired)
                            {
                                // The special click was already sent.
                                longPressFired = false;
                                touchActive = false;
                                touchPanning = false;
                                break;
                            }

                            int touchUpX = (int)e.GetX();
                            int touchUpY = (int)e.GetY();
                            bool wasPanning = touchPanning;
                            pendingTouchEvents.Enqueue(view =>
                            {
                                var touchPosition = view.ScreenToView(new Freeserf.Position(touchUpX, touchUpY));
                                global::Android.Util.Log.Debug("Freeserf_Input", $"Touch up: {touchUpX},{touchUpY} -> {touchPosition.X},{touchPosition.Y} panning={wasPanning}");

                                if (wasPanning)
                                    view.NotifyStopDrag();
                                else
                                    view.NotifyClick(touchUpX, touchUpY, Event.Button.Left, false);
                            });
                            if (!wasPanning)
                                HandleTap(touchUpX, touchUpY);
                            touchActive = false;
                            touchPanning = false;
                            break;
                        case MotionEventActions.Cancel:
                            CancelLongPress();
                            longPressFired = false;
                            if (touchPanning)
                                pendingTouchEvents.Enqueue(view => view.NotifyStopDrag());
                            touchActive = false;
                            touchPanning = false;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error.Write(ErrorSystemType.Application, "Touch: " + ex.Message);
                }

                return true;
            }

            return base.DispatchTouchEvent(e);
        }

        // Like on the desktop, a click is repeated as delayed click once no second
        // tap follows (buttons with a double click handler only react to that).
        // A second tap in time becomes a double click.
        void HandleTap(int x, int y)
        {
            long now = SystemClock.ElapsedRealtime();
            int slop = Math.Max(ViewConfiguration.Get(this).ScaledDoubleTapSlop, 1);
            delayedClickHandler ??= new Handler(Looper.MainLooper);

            if (delayedClickPending)
            {
                delayedClickHandler.RemoveCallbacks(delayedClickRunnable);
                delayedClickPending = false;

                if (now - lastTapTime <= ViewConfiguration.DoubleTapTimeout &&
                    Math.Abs(x - lastTapX) <= slop && Math.Abs(y - lastTapY) <= slop)
                {
                    lastTapTime = 0;
                    pendingTouchEvents.Enqueue(view => view.NotifyDoubleClick(x, y, Event.Button.Left));
                    return;
                }
            }

            lastTapX = x;
            lastTapY = y;
            lastTapTime = now;
            delayedClickPending = true;
            delayedClickRunnable ??= new Java.Lang.Runnable(() =>
            {
                delayedClickPending = false;
                int dx = lastTapX;
                int dy = lastTapY;
                pendingTouchEvents.Enqueue(view => view.NotifyClick(dx, dy, Event.Button.Left, true));
            });
            delayedClickHandler.PostDelayed(delayedClickRunnable, ViewConfiguration.DoubleTapTimeout);
        }

        void StartLongPress()
        {
            longPressHandler ??= new Handler(Looper.MainLooper);
            longPressRunnable ??= new Java.Lang.Runnable(OnLongPress);
            longPressHandler.RemoveCallbacks(longPressRunnable);
            longPressHandler.PostDelayed(longPressRunnable, LongPressDelayMs);
        }

        static void CancelLongPress()
        {
            if (longPressHandler != null && longPressRunnable != null)
                longPressHandler.RemoveCallbacks(longPressRunnable);
        }

        static void CancelDelayedClick()
        {
            if (delayedClickHandler != null && delayedClickRunnable != null)
                delayedClickHandler.RemoveCallbacks(delayedClickRunnable);
            delayedClickPending = false;
        }

        // Runs on the UI thread after the finger rested for LongPressDelayMs.
        void OnLongPress()
        {
            if (!touchActive || touchPanning || longPressFired)
                return;

            longPressFired = true;
            int x = touchStartX;
            int y = touchStartY;
            global::Android.Util.Log.Debug("Freeserf_Input", $"Long press (special click): {x},{y}");
            // Same sequence as on the desktop (left press, then left + right).
            pendingTouchEvents.Enqueue(view =>
            {
                view.NotifyClick(x, y, Event.Button.Left, false);
                view.NotifySpecialClick(x, y);
            });
            Window?.DecorView?.PerformHapticFeedback(FeedbackConstants.LongPress);
        }
        static float PinchDistance(MotionEvent e, int index0, int index1)
        {
            float dx = e.GetX(index0) - e.GetX(index1);
            float dy = e.GetY(index0) - e.GetY(index1);
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        protected override void OnPause()
        {
            base.OnPause();
            activityState = ActivityState.Paused;
            pinchActive = false;
            touchActive = false;
            touchPanning = false;
            CancelLongPress();
            CancelDelayedClick();
            longPressFired = false;

            if (gameView != null)
            {
                var volumeControl = gameView.AudioFactory.GetAudio()?.GetVolumeController();
                volumeControl?.SetVolume(0.0f);
            }
        }

        protected override void OnStop()
        {
            base.OnStop();
            activityState = ActivityState.Stopped;
            pinchActive = false;
            touchActive = false;
            touchPanning = false;
            CancelLongPress();
            CancelDelayedClick();
            longPressFired = false;

            // Close any active multiplayer connection so no network threads
            // keep running while the app is backgrounded.
            try
            {
                gameView?.DisconnectNetwork();
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Debug("Freeserf_Trace", "OnStop: DisconnectNetwork EXCEPTION: " + ex);
                Log.Error.Write(ErrorSystemType.Application, "DisconnectNetwork: " + ex);
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            activityState = ActivityState.Active;
        }

        protected override void OnResume()
        {
            base.OnResume();
            activityState = ActivityState.Active;

            if (gameView != null)
            {
                var volumeControl = gameView.AudioFactory.GetAudio()?.GetVolumeController();
                volumeControl?.SetVolume(UserConfig.Audio.Volume);
            }

            // Pausing the activity hides the on-screen keyboard. If a text input
            // (e.g. the save game name field) is still focused, show it again.
            if (Freeserf.UI.Gui.IsTextInputFocused)
                OnTextInputFocusChanged(true);
        }
    }

    class AndroidConsole : TextWriter
    {
        readonly string tag;
        readonly StringBuilder lineBuilder = new();

        public AndroidConsole(string tag)
        {
            this.tag = tag;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string value)
        {
            value ??= "";

            if (lineBuilder.Length > 0)
            {
                value = lineBuilder.ToString() + value;
                lineBuilder.Clear();
            }

            global::Android.Util.Log.Debug(tag, value);
        }

        public override void Write(string value)
        {
            lineBuilder.Append(value);
        }
    }

    // Stream adapter that forwards writes to a TextWriter (e.g. the Android
    // logcat console) so the game's Log class can be routed to logcat.
    class ConsoleStream : Stream
    {
        readonly TextWriter writer;

        public ConsoleStream(TextWriter writer)
        {
            this.writer = writer;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => writer.Flush();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            writer.Write(Encoding.UTF8.GetString(buffer, offset, count));
        }
    }
}
