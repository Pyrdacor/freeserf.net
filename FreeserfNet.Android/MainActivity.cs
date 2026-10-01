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
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Freeserf;
using Freeserf.Renderer;
using Silk.NET.Input;
using Silk.NET.Input.Sdl;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;
using Silk.NET.Windowing.Sdl.Android;
using System;
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
        static GameView gameView;
        static Data.DataSource dataSource;
        static Global.InitInfo initInfo;
        static bool initialized = false;
        static bool renderTraced = false;

        // mouse emulation state (SDL maps single finger touch to left mouse button)
        static int lastDragX = int.MinValue;
        static int lastDragY = int.MinValue;
        static bool scrolled = false;

        public MainActivity()
        {
            Console.SetOut(new AndroidConsole("Freeserf_Info"));
            Console.SetError(new AndroidConsole("Freeserf_Error"));
            Log.SetStream(new ConsoleStream(Console.Error));
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
                    if (!view.IsClosing)
                        view.DoUpdate();
                    if (!view.IsClosing)
                        view.DoRender();
                });
                global::Android.Util.Log.Debug("Freeserf_Trace", "OnRun: Run returned");
                view.Reset();
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Application, "Run: " + ex.Message);
            }
            finally
            {
                view?.Dispose();
            }
        }

        static void Window_Load()
        {
            try
            {
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: start");
                view.MakeCurrent();
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: MakeCurrent done");

                initInfo = Global.Init(Array.Empty<string>());
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: Global.Init done");

                Network.Network.DefaultClientFactory = new Network.ClientFactory();
                Network.Network.DefaultServerFactory = new Network.ServerFactory();

                UserConfig.Load(FileSystem.Paths.UserConfigPath);
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: UserConfig.Load done");

                // Extract bundled game data (e.g. SPAE.PA) from the APK assets
                // to the app's private storage so the data source can find it.
                ExtractBundledData();
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: ExtractBundledData done");

                var data = Data.Data.GetInstance();
                if (!data.Load(FileSystem.Paths.GameDataFolder, UserConfig.Game.GraphicDataUsage,
                    UserConfig.Game.SoundDataUsage, UserConfig.Game.MusicDataUsage))
                {
                    Log.Error.Write(ErrorSystemType.Data, "Error loading game data.");
                    global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: data.Load FAILED");
                    return;
                }
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: data.Load done");
                dataSource = data.GetDataSource();

                if (initInfo.ScreenWidth == -1)
                    initInfo.ScreenWidth = UserConfig.Video.ResolutionWidth;
                if (initInfo.ScreenHeight == -1)
                    initInfo.ScreenHeight = UserConfig.Video.ResolutionHeight;

                State.Init(view);
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: State.Init done");

                gameView = new GameView(dataSource, new Size(initInfo.ScreenWidth, initInfo.ScreenHeight),
                    DeviceType.MobileLandscape, SizingPolicy.FitRatio, OrientationPolicy.Support180DegreeRotation);
                gameView.Resize(view.Size.X, view.Size.Y);
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: GameView created");

                input = view.CreateInput();
                input.Mice[0].MouseDown += Mouse_MouseDown;
                input.Mice[0].MouseUp += Mouse_MouseUp;
                input.Mice[0].MouseMove += Mouse_MouseMove;
                input.Mice[0].Scroll += Mouse_Scroll;
                input.Keyboards[0].KeyDown += Keyboard_KeyDown;
                input.Keyboards[0].KeyChar += Keyboard_KeyChar;

                initialized = true;
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: done, initialized=true");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Load: EXCEPTION: " + ex);
                Log.Error.Write(ErrorSystemType.Application, "Load: " + ex.Message);
            }
        }

        static void Window_Render(double delta)
        {
            if (!initialized)
                return;

            try
            {
                if (!renderTraced)
                {
                    renderTraced = true;
                    global::Android.Util.Log.Debug("Freeserf_Trace", "Window_Render: first render call");
                }
                gameView?.Render();
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
            if (gameView != null)
                gameView.UpdateNetworkEvents();
        }

        static void Window_Resize(Vector2D<int> size)
        {
            if (gameView != null)
                gameView.Resize(size.X, size.Y);
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

        protected override void OnPause()
        {
            base.OnPause();

            if (gameView != null)
            {
                var volumeControl = gameView.AudioFactory.GetAudio()?.GetVolumeController();
                volumeControl?.SetVolume(0.0f);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (gameView != null)
            {
                var volumeControl = gameView.AudioFactory.GetAudio()?.GetVolumeController();
                volumeControl?.SetVolume(UserConfig.Audio.Volume);
            }
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
