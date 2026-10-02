# Freeserf.net Android Port — Knowledge Base for LLMs

This document captures hard-won knowledge about building, debugging, and running the
Android port of freeserf.net. It is written for future LLM sessions (and humans) working
on this codebase so they don't have to rediscover these issues.

## Overview

- The Android host project is `FreeserfNet.Android/` (targets `net10.0-android`,
  minSdk 21, targetSdk 36, package `net.freeserf.android`).
- It uses **Silk.NET 2.23.0** (`Silk.NET.Windowing.Sdl`, `Silk.NET.Input.Sdl`,
  `Silk.NET.OpenGL`). The activity extends `SilkActivity` from
  `Silk.NET.Windowing.Sdl.Android`, which itself extends SDL's `SDLActivity`.
- The game data file `SPAE.PA` is **copyrighted and kept outside the repo**.
  Set the `FreeserfGameDataPath` MSBuild property to its location to bundle it;
  the project retains `C:\git\freeserf.net\SPAE.PA` as a fallback. It is
  extracted to app storage on first run (`ExtractBundledData()` in
  `MainActivity.cs`).
- The desktop host project is `FreeserfNet/`; `GameView.cs` is compiled directly into
  the Android project via a `<Compile Include="..\FreeserfNet\GameView.cs" Link=...>`.

## Building the Release APK

### Working build command (Windows, .NET SDK 10.0.400, Android workload 36.1.69)

```powershell
$env:MSBUILDDISABLENODEREUSE = 1
dotnet build FreeserfNet.Android\FreeserfNet.Android.csproj -c Release `
  -m:1 -nodeReuse:false -p:PublishTrimmed=false -p:RunAOTCompilation=false
```

To include an external `SPAE.PA`, add
`-p:FreeserfGameDataPath="D:\path\to\SPAE.PA"` to the build command.

Output APK: `FreeserfNet.Android\bin\Release\net10.0-android\net.freeserf.android-Signed.apk`
(~101 MB with trimming/AOT disabled; ~30 MB when trimmed).

### Why trimming and AOT are disabled (MSB4018)

- MSBuild on this machine **cannot spawn the out-of-proc task host**
  ("Zugriff verweigert" / access denied). The ILLink `ComputeManagedAssemblies` task
  hardcodes `TaskFactory="TaskHostFactory"` in `Microsoft.NET.ILLink.targets` (line ~325),
  which requires the out-of-proc host.
- Workaround: `-p:PublishTrimmed=false`. AOT requires trimming, so also pass
  `-p:RunAOTCompilation=false` (otherwise XA1030).
- Also use `-m:1 -nodeReuse:false` and `$env:MSBUILDDISABLENODEREUSE=1` to avoid
  node-reuse problems.

### `dotnet build` vs `dotnet publish`

- The reference project **Pyrdacor/Ambermoon.net** (a very similar C# game rewrite with
  an Android port, same Silk.NET 2.23.0 stack) builds with **`dotnet publish`**, not
  `dotnet build`:
  `dotnet publish -c ReleaseAndroid -p:AndroidSigningKeyStore=...`
- Ambermoon's csproj explicitly sets:
  ```xml
  <RunAOTCompilation>False</RunAOTCompilation>
  <PublishTrimmed>False</PublishTrimmed>
  <AndroidLinkTool>none</AndroidLinkTool>
  <AndroidLinkMode>None</AndroidLinkMode>
  <AndroidEnableProguard>false</AndroidEnableProguard>
  <AndroidEnableR8>false</AndroidEnableR8>
  <Optimize>false</Optimize>
  ```
- If the type-registration crash (below) ever comes back, switch to `dotnet publish`
  with the Ambermoon settings — `dotnet publish` regenerates the type-registration
  table (`libxamarin-app.so`) more reliably than incremental `dotnet build`.

## Widescreen Support (Android port)

### Virtual Screen Size

The virtual screen is computed from the actual device view, preserving aspect ratio,
capped at `MAX_VIRTUAL_SCREEN_WIDTH=1920`:

```csharp
int screenW = view.Size.X, screenH = view.Size.Y;
if (screenH > screenW) { int t = screenW; screenW = screenH; screenH = t; } // ensure landscape
int virtualWidth = Math.Min(screenW, Global.MAX_VIRTUAL_SCREEN_WIDTH);
int virtualHeight = Math.Max(1, (int)Math.Round(virtualWidth * (double)screenH / screenW));
gameView = new GameView(dataSource, new Size(virtualWidth, virtualHeight), ...);
```

Pixel 8a → 1920×864 (20:9). The map automatically gets more columns (60 vs 40),
filling the full screen with no black bars.

### GUI Scaling

The GUI (PanelBar, GameInitBox, minimap) is scaled **uniformly** (min ratio) and
**centered** instead of the previous non-uniform stretch:

```csharp
float scale = Math.Min((float)VirtualScreen.Size.Width / 640.0f, (float)VirtualScreen.Size.Height / 480.0f);
int offsetX = Misc.Round((VirtualScreen.Size.Width - 640.0f * scale) / 2.0f);
int offsetY = Misc.Round((VirtualScreen.Size.Height - 480.0f * scale) / 2.0f);
```

- Scale = min(1920/640, 864/480) = 1.8
- Center offset: offsetX=384, offsetY=0
- GUI on-screen size unchanged (2.25× design size) — no UX regression

### Input Transformation

`PositionToGui` and `DeltaToGui` use the inverse of the uniform+centered transformation:

```csharp
Position PositionToGui(Position position)
{
    float scale = Math.Min((float)renderView.VirtualScreen.Size.Width / 640.0f, (float)renderView.VirtualScreen.Size.Height / 480.0f);
    int offsetX = Misc.Round((renderView.VirtualScreen.Size.Width - 640.0f * scale) / 2.0f);
    int offsetY = Misc.Round((renderView.VirtualScreen.Size.Height - 480.0f * scale) / 2.0f);
    return new Position((int)Math.Floor((position.X - offsetX) / scale), (int)Math.Floor((position.Y - offsetY) / scale));
}

Size DeltaToGui(Size delta)
{
    float scale = Math.Min((float)renderView.VirtualScreen.Size.Width / 640.0f, (float)renderView.VirtualScreen.Size.Height / 480.0f);
    return new Size(Misc.Round(delta.Width / scale), Misc.Round(delta.Height / scale));
}
```

### Verification

- No black bars, map fills full width
- GUI (PanelBar, GameInitBox) undistorted and centered
- Input works: map tile selection, GUI buttons, scrolling
- Map cursor aligns with tiles

## Crash 1: `System.TypeInitializationException` at startup (FIXED)

- Symptom: `FATAL UNHANDLED EXCEPTION: System.TypeInitializationException` →
  `NullReferenceException` in `Freeserf.Global..cctor()`.
- Root cause: `Assembly.GetEntryAssembly()` returns **null on Android**.
  `Freeserf.Core\Freeserf.cs` (lines ~34-36) used it in three static field
  initializers (`Version`, `VERSION`, `EXTENDED_VERSION`).
- Fix: replace with `Assembly.GetExecutingAssembly()`.
- Note: `Freeserf.Core\FileSystem\Paths.cs` (line ~99) also uses `GetEntryAssembly()`
  but is guarded by `!OperatingSystem.IsAndroid()` — safe.

## Crash 2: `n_onResume` / `n_loadLibraries` UnsatisfiedLinkError (FIXED by clean rebuild)

- Symptom: `java.lang.UnsatisfiedLinkError: No implementation found for void
  crc64bcc776d209640335.MainActivity.n_onResume()` — app crashes immediately on launch.
  `n_loadLibraries` for `SilkActivity` (crc647ab54b95e567f95c) was also unregistered
  (that one is non-fatal, caught).
- Root cause: **stale/incremental build artifact**. The Mono type-registration table
  (`libxamarin-app.so`) did not match the generated Java stubs. The `n_*` native
  methods in the generated Java wrappers are registered via this table; when it is
  stale, the JVM cannot find the native implementation.
- Fix: **delete `FreeserfNet.Android\bin` and `FreeserfNet.Android\obj` and rebuild
  from scratch.** The clean rebuild produced a working APK (verified: `onResume()`
  runs, `Running main function SDL_main from library libmain.so`, no FATAL).
- Lesson: after ANY change to the Android project, prefer a clean build
  (`dotnet clean` or delete bin/obj) to avoid this class of failure. The crash is
  non-deterministic across incremental builds.

## Logging to logcat (FIXED)

- The game's `Log` class (`Freeserf.Core\Log.cs`) writes to a `Stream` (default
  `Console.OpenStandardOutput()`, which goes nowhere on Android). The desktop app
  calls `Log.SetStream(...)` in `FreeserfNet\MainWindow.cs`; the Android port never did.
- Fix in `FreeserfNet.Android\MainActivity.cs` constructor:
  ```csharp
  Console.SetOut(new AndroidConsole("Freeserf_Info"));
  Console.SetError(new AndroidConsole("Freeserf_Error"));
  Log.SetStream(new ConsoleStream(Console.Error));
  ```
- `AndroidConsole : TextWriter` routes `Console.WriteLine` to
  `Android.Util.Log.Debug(tag, value)`.
- `ConsoleStream : Stream` is a small adapter that forwards `Write(byte[],...)` to a
  `TextWriter` — required because `Log.SetStream` takes a `Stream`, not a `TextWriter`
  (passing `Console.Error` directly is a CS1503 compile error).
- Ambermoon.net does exactly the same pattern (`AndroidConsole` in its `MainActivity`
  constructor) — this approach is confirmed correct.

## adb / device workflow (Pixel 8a)

- adb is NOT on PATH: `C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe`.
- Device: `3C211JEKB03986` (Pixel 8a / akita). MainActivity:
  `net.freeserf.android/crc64bcc776d209640335.MainActivity`.
- Install / launch / log:
  ```powershell
  adb install -r <apk>
  adb logcat -c
  adb shell am force-stop net.freeserf.android
  adb shell am start -n net.freeserf.android/crc64bcc776d209640335.MainActivity
  adb logcat -d --pid=<pid>          # or: adb logcat -d -s Freeserf_Info:* Freeserf_Error:*
  ```
- `run-as` does NOT work (Release build, `Debuggable=false`).
- **Screenshot gotcha:** PowerShell `>` redirection writes UTF-16 and corrupts binary
  PNGs. Use `Start-Process -RedirectStandardOutput` or
  `[System.IO.File]::WriteAllBytes` with `adb exec-out screencap -p`. Valid PNG header:
  `137,80,78,71,13,10,26,10`.

## Audio libraries

- BASS core and BASSMIDI native libraries are bundled for `arm64-v8a`,
  `armeabi-v7a`, and `x86_64` under `FreeserfNet.Android\lib\<abi>\`. Keep both
  libraries together when adding an ABI: SFX use BASS core and DOS MIDI music
  also needs BASSMIDI.
- Binaries are from the official Un4seen Android packages; BASS is free for
  non-commercial use. See `sound_android.md` for sources and details.

## Known non-fatal warnings (ignore)
- `hidapi: One of RECEIVER_EXPORTED or RECEIVER_NOT_EXPORTED should be specified...`
  — Silk.NET/SDL hidapi receiver issue; harmless.
- `Assembly 'de/System.Private.CoreLib.resources' not found` — missing satellite
  resource assemblies; harmless.
- XA0141 16 KB page-size warnings for `libmain.so`/`libSDL2.so` — informational for
  Android 16; harmless on the Pixel 8a.

## Reference: Ambermoon.net (Pyrdacor/Ambermoon.net)

A nearly identical C# game rewrite with an Android port. Useful as a reference for
how a working Silk.NET-on-Android game is structured:

- `AmbermoonAndroid/MainActivity.cs` — extends `SilkActivity`, overrides
  `OnCreate`/`OnRun`/`OnPause`/`OnResume`/`OnStop`, sets up `AndroidConsole` logging
  in the constructor, registers `AppDomain.CurrentDomain.UnhandledException` and
  `AndroidEnvironment.UnhandledExceptionRaiser` handlers to show errors in an
  `AlertDialog` instead of silently dying.
- `AmbermoonAndroid/GameWindow.cs` — wraps the whole game loop; `OnRun()` calls
  `gameWindow.Run(...)` inside try/catch/finally.
- `AmbermoonAndroid/FileProvider.cs` — loads game data from APK **raw resources**
  (`Resources.OpenRawResource(id)`) instead of extracting assets to storage.
- `AmbermoonAndroid/libsilkdroid.c` — they build their own `libmain.so` (implements
  `sdSetMain` + `main`) and replace Silk.NET's `libSDL2.so`/`libmain.so` with custom
  builds (see `prepare_publish.sh`). Only relevant if the stock Silk.NET native libs
  misbehave.
- `PublishAndroid.ps1` — `dotnet publish -c ReleaseAndroid` with keystore signing.
- `install-release-on-phone.bat` — converts the AAB to a universal APKS with
  `bundletool` and installs it.

## Issue 3: `EGL_BAD_SURFACE` in render loop — black screen (FIXED)

### Symptom

- Shaders now compile (GLSL ES fixes in `db096be`), but the render loop fails with
  `EGL_BAD_SURFACE` from `eglSwapBuffersWithDamageKHR` — the EGL surface is lost, so
  the screen stays black.
- The ANR seen earlier was a symptom of the render errors and should disappear once
  rendering works.

### Root cause (confirmed)

The missing `DoEvents()` in the render loop prevented SDL events from being pumped,
so Android surface lifecycle events were never processed. Combined with no activity
state tracking, the EGL surface became invalid while the render loop continued calling
`eglSwapBuffers`. The fix is to add both `DoEvents()` and proper lifecycle handling.

### Fix applied (commit `16f3179`)

- Added `enum ActivityState { Active, Paused, Stopped }` + `static volatile ActivityState activityState = ActivityState.Active;`
- Added `view.DoEvents();` as the first line of the render loop in `OnRun()`
- `Window_Render` now returns early if `activityState != ActivityState.Active`, skipping both rendering and `SwapBuffers()` when inactive.
- Overrode lifecycle methods:
  - `OnPause`: sets `ActivityState.Paused`
  - `OnStop`: sets `ActivityState.Stopped`
  - `OnStart`: sets `ActivityState.Active`
  - `OnResume`: sets `ActivityState.Active`

### Silk.NET internals (resolved)

- **`SdlContext` mystery resolved**: The class is a plain `IGLContext` in `Silk.NET.SDL.dll`. It's not a custom type — it implements the standard `IGLContext` interface and owns the EGL surface/context. No special decompilation needed.
- `ViewImplementationBase.DoRender()`: makes the GL context current (if not already), invokes the `Render` event, then swaps buffers if `ShouldSwapAutomatically`.
- `ViewImplementationBase.Run(onFrame)`: `while (!IsClosing) onFrame();`.
- `SdlView.CoreGLContext => _ctx ??= new SdlContext(Sdl, SdlWindow, this)` — the `SdlContext` class owns the EGL surface/context and `SwapBuffers`.
- `SdlView.CoreReset()`: `CoreGLContext?.Dispose(); Sdl.DestroyWindow(SdlWindow); SdlWindow = null; _ctx = null;`
- `SilkActivity.Main()` (static, called from native `libmain.so`) → `Instance.Run()` → `OnRun()`.
- `SDLActivity` exposes static JNI callbacks `OnNativeSurfaceCreated/Changed/Destroyed` — this is how SDL notifies managed code about Android surface lifecycle.

### Java/C# lifecycle dispatch finding

The logcat shows `onCreate()`, `onStart()`, `onResume()`, `onPause()`, `onStop()` logged with tag "SDL" — these come from the Java `org.libsdl.app.SDLActivity` class in the AAR. So the Java SDLActivity lifecycle methods ARE reached through the C# override chain (scenario A confirmed). The decompiled `Activity.OnPause()` uses `InvokeVirtualVoidMethod("onPause.()V", this, null)` — virtual JNI dispatch — which explains how the chain reaches SDLActivity.onPause() without infinite recursion.

### Ambermoon.net comparison (the working reference)

Ambermoon.net (same Silk.NET 2.23.0 + SDL stack, working on Android) differs from freeserf.net in three important ways:

1. **`DoEvents()` in the render loop** — Ambermoon's `Run()` loop is:
   ```csharp
   window.Run(() => {
       DoEvents();
       if (!window.IsClosing) window.DoUpdate();
       if (!window.IsClosing) window.DoRender();
   });
   ```
   freeserf.net's loop omitted `DoEvents()`, so SDL events (including surface lifecycle events) were never pumped from the managed side.
2. **`ActivityState` tracking** — Ambermoon's `MainActivity` overrides `OnPause`/`OnStop`/`OnResume` to set `gameWindow.State` to `Paused`/`Stopped`/`Active`, and `Window_Render` skips rendering **and** `window.SwapBuffers()` when `State == ActivityState.Stopped`. freeserf.net had no lifecycle tracking and called `view.SwapBuffers()` unconditionally.
3. **Initial swap in `Window_Load`** — Ambermoon calls `GLContext.SwapBuffers()` once after setup, before the first `DoRender()`.

### Screen-off testing gotcha (important for future debugging)

When an app is launched while the device screen is OFF (`isSleeping=true`), the following happens:
- `onPause()` fires immediately after `onResume()` (device not interactive)
- The surface is skipped (orientation mismatch between requested LANDSCAPE and actual portrait surface) and destroyed
- `handleNativeState()` never reaches RESUMED → SDLThread never starts → `OnRun()` never runs

**Fix for testing**: Wake the screen before launching:
```powershell
adb shell input keyevent KEYCODE_WAKEUP
adb shell wm dismiss-keyguard
```

### Next steps (completed)

- [x] Add `DoEvents()` to render loop — commit `16f3179`
- [x] Add `ActivityState` tracking with lifecycle overrides — commit `16f3179`
- [x] Verify app runs on device — confirmed, no EGL_BAD_SURFACE when screen is awake
- [ ] Update SQL todo statuses as work progresses

### Current state / next steps

- [x] Startup crash fixed (`GetExecutingAssembly`), committed as `356a131`, pushed to `origin/android`.
- [x] Logging to logcat fixed (`Log.SetStream` + `ConsoleStream`), same commit.
- [x] `n_onResume` type-registration crash fixed by clean rebuild (bin/obj deleted).
- [x] GLSL ES shader compilation errors fixed (`db096be`).
- [x] **EGL_BAD_SURFACE in render loop** — shaders compile, but `eglSwapBuffers` fails with `EGL_BAD_SURFACE`; screen stays black. **FIXED**: Added `DoEvents()` and activity state tracking (commit `16f3179`). App now runs on device without EGL errors when the screen is awake.
- [ ] Consider switching the build to `dotnet publish` + Ambermoon's `AndroidLinkMode=None` settings for reproducible builds.

## Issue 4: App runs but background is black — map not rendering (OPEN)

### Symptom

- App launches cleanly: full init sequence logged (`OnRun` → `Window_Load` → `GameView`
  created → `Window_Render`), **no crashes, no EGL errors** (EGL_BAD_SURFACE fix works).
- Render loop is actively running (freeserf at ~60% CPU — no frame limiting, not frozen).
- **The screen is almost entirely black (0,0,0).** Only a small, sparse content area
  renders at screen coords x=958-1532, y=344-594 on the 2400x1080 Pixel 8a.
- Only ~1490 non-black pixels (sampled every 2px) exist in that 575x251 box — the
  content is thin lines and small solid bars, not a filled dialog or map.

### Content structure (from pixel analysis of `fs_clean.png`)

1. **Vertical line** at x≈958-960 spanning y=344-422 (3px wide, 78px tall).
2. **4 horizontal bands** at y=362, 382, 402, 422 — exactly **20px apart
   (= TILE_HEIGHT)**, spanning x≈958-997 (39px wide).
3. **5 solid-color vertical bars** at y=506-594 (growing from 4px to 52px wide):
   - x=972-978: bright green (107,171,59) — grass color, full height
   - x=1152-1172: bright green, from y≈540 down
   - x=1318-1326: teal (0,147,135) — water color, from y≈540 down
   - x=1498-1506: teal, from y≈530 down
   - plus thin vertical lines at x≈1138-1146 and x≈958-992

The bright-green/teal bars are Freeserf palette colors (grass/water), which is why the
earlier reading looked like "map tiles" — but they are solid bars, not diamond-shaped
tiles, and they sit inside the GameInitBox area (see geometry below).

### Geometry reconciliation (important)

- Content at window (960,344)-(1532,592) = **VirtualScreen (427,306)-(935,526)**.
- The GameInitBox is **centered** by `Interface.Layout()` (Interface.cs line 1453) at
  GUI (144,140)-(496,340) → window (804,315)-(1596,765).
- **The content is INSIDE the GameInitBox area** — it is a *partial render of the
  GameInitBox dialog* (right-center portion), NOT the game map.
- The GameInitBox **background** (which would cover the whole 352x200 box) is **not
  rendering** — only some elements (thin lines + a few solid bars) show.

### Root cause (CONFIRMED) — `GL_BGRA` texture format invalid on OpenGL ES

- **Every `glTexImage2D` call with `GL_BGRA` format fails with `GL_INVALID_OPERATION`**
  on this device (Pixel 8a, Mali-G715, GLES 3.2). `GL_BGRA` is **not core OpenGL ES**
  (only via the `GL_EXT_texture_format_BGRA8888` extension, which this device does not
  honor for `glTexImage2D`).
- All atlas textures are created as `PixelFormat.BGRA8` (`MutableTexture.Finish()` →
  `Texture.Create(BGRA8, ...)`), and `ToOpenGLPixelFormat(BGRA8)` returns `GLEnum.Bgra`
  → every atlas texture upload fails → textures are **incomplete** → sampling returns
  opaque black → map tiles and GUI sprites render black.
- **Evidence (per-layer `glGetError` instrumentation in `GameView.cs`):**
  ```
  GameView: layer Landscape texture 512x410 glError=InvalidOperation
  GameView: layer Waves texture 480x44 glError=InvalidOperation
  GameView: layer Paths texture 512x123 glError=InvalidOperation
  GameView: layer Objects texture 512x412 glError=InvalidOperation
  GameView: layer Serfs texture 512x631 glError=InvalidOperation
  GameView: layer Buildings texture 512x761 glError=InvalidOperation
  GameView: layer Builds texture 282x15 glError=InvalidOperation
  GameView: layer Gui texture 512x936 glError=InvalidOperation
  GameView: layer GuiBuildings texture 512x761 glError=NoError   ← reuses Buildings atlas, no new TexImage2D
  GameView: layer GuiFont texture 256x256 glError=InvalidOperation
  GameView: layer Minimap texture 128x128 glError=NoError        ← no Finish()/TexImage2D yet
  GameView: layer Cursor texture 16x16 glError=InvalidOperation
  ```
  The only `NoError` cases are exactly the two layers that do **not** call
  `TexImage2D` at that point (GuiBuildings aliases the already-created Buildings atlas;
  Minimap is created without `Finish()`). Every real texture upload fails.
- The small visible colored clusters (teal/green bars) come from **colored rects via
  `ColorShader`** (no texture needed) — not from atlas textures. They become irrelevant
  once the texture fix lands.

### Fix (APPLIED) — swizzle BGRA→RGBA in `Texture.Create()`

- `Freeserf.Renderer\Texture.cs` `Create()`: after `ConvertPixelData`, if the format is
  `BGRA8`/`BGR8`, the pixel data is swizzled in memory to `RGBA8`/`RGB8` and the format
  is changed accordingly, so `ToOpenGLPixelFormat` returns `GL_RGBA`/`GL_RGB` (core GLES).
- This is a one-time per-texture conversion, works on **both** desktop OpenGL (where
  `GL_BGRA` is valid) and OpenGL ES, and needs no extension checks.
- The minimap data is also stored BGRA (`Minimap.SetColor` writes B,G,R,A) and is
  covered by the same conversion.

### YouTube behind freeserf (CRITICAL for screenshots)

- `dumpsys window windows` shows freeserf's window is **`fmt=TRANSLUCENT`** — content
  behind it shows through wherever the game isn't rendering.
- **YouTube was running behind freeserf** (Window #9 `com.google.android.youtube`,
  `isOnScreen=true`) and its video showed through the translucent window — this
  explained the "video at top-left" that doesn't belong to the app.
- **For clean screenshots, force-stop YouTube first:**
  ```powershell
  adb shell am force-stop com.google.android.youtube
  ```
  After stopping it, freeserf is the focused window and screenshots show only the app.

### Analysis gotcha (fine-grid sampling bug)

- An earlier fine-grid analysis (sampling every ~8px) produced a misleading "map tiles
  with trees/water" reading. The bug: the row offset was **double-counted**
  (`$miny += $ch` AND `$y = $miny + $r*$ch`), so rows sampled far below their labels.
- **Reliable method: row/column histograms sampling every 2px** (count non-black
  pixels per row/column). Always verify suspicious pixels with direct `GetPixel`.

### Current state / next steps

- [x] Startup crash fixed (`GetExecutingAssembly`), committed as `356a131`, pushed to
      `origin/android`.
- [x] Logging to logcat fixed (`Log.SetStream` + `ConsoleStream`), same commit.
- [x] `n_onResume` type-registration crash fixed by clean rebuild (bin/obj deleted).
- [x] GLSL ES shader compilation errors fixed (`db096be`).
- [x] **EGL_BAD_SURFACE in render loop** — FIXED via `DoEvents()` + `ActivityState`
      tracking (commits `16f3179` + `4d32277`, pushed to `origin/android`). App runs
      without EGL errors; pause/resume verified working.
- [x] **Black background (Issue 4)** — ROOT CAUSE FOUND: `GL_BGRA` texture format is
      invalid on OpenGL ES → every atlas texture upload fails with
      `GL_INVALID_OPERATION` → all textures incomplete → black. FIXED by swizzling
      BGRA→RGBA in `Texture.Create()` (pending device verification).
- [ ] **Bug #2: activity recreation breaks init** — `TextureAtlasManager` is a static
      singleton; on activity recreation `AddAll()` throws "Texture atlas already
      created", the exception is caught in `Window_Load`, and `initialized` stays
      `false` → black screen. Needs a `Reset()`/idempotent `AddAll` + proper re-init
      (GPU textures are invalid after EGL context loss).
- [ ] Consider switching the build to `dotnet publish` + Ambermoon's
      `AndroidLinkMode=None` settings for reproducible builds.

## Pinch-to-zoom and finger panning (implemented)

Two-finger pinch zooms the map in/out while ingame, and single-finger drag pans
the map, matching the Windows mouse-wheel zoom (`gameView.Zoom`, range 0..4,
`zoomFactor = 1 + zoom * 0.5` applied around the screen center in
`Freeserf.Renderer/Context.cs`) and the desktop right-button drag scrolling.

### Why a custom implementation

Silk.NET 2.23.0 has **no touch abstraction** (no `ITouch`). SDL maps single-finger
touch to the left mouse button via `SDLSurface.onTouch` → `onNativeTouch`; two-finger
touch produces no mouse events at all. So all touch gestures must be captured from
the raw Android `MotionEvent` stream.

### Threading model (critical)

- `DispatchTouchEvent` runs on the **UI thread** and must NOT touch GL state
  (`gameView.Zoom` setter → `Context.ApplyMatrix` → GL matrix stack).
- Instead it records gesture state in **`volatile` static fields**:
  `pinchActive`, `pinchStartDistance`, `pinchStartZoom`, `pinchCurrentDistance`
  (plus the single-finger pan state fields).
- `Window_Update` runs on the **SDL thread** every frame and applies the zoom there.

### How it works (`MainActivity.cs`)

- `DispatchTouchEvent(MotionEvent e)`:
  - If `e.PointerCount >= 2 && gameView != null && gameView.CanZoom` → handle the
    pinch and **return `true`** (consume, so SDL never sees multi-touch).
  - If `e.PointerCount == 1 && (touchActive || gameView.CanZoom)` → handle the
    single-finger gesture (tap vs drag) and **return `true`**.
  - Otherwise → `base.DispatchTouchEvent(e)` (single-finger SDL mouse emulation
    unchanged, e.g. on the main menu or with popups open).
- **Pinch** (`ActionMasked` handling):
  - `PointerDown` (2nd finger down) → start pinch: record
    `pinchStartDistance` = distance between `(GetX(0),GetY(0))` and
    `(GetX(1),GetY(1))`, `pinchStartZoom = gameView.Zoom`, `pinchActive = true`.
  - `Move` → update `pinchCurrentDistance`.
  - `PointerUp` / `Up` / `Cancel` → `pinchActive = false`.
  - Zero distance is guarded with `Math.Max(distance, 1.0f)`.
- **Single-finger** (tap vs drag disambiguation):
  - `Down` → record start position, `touchActive = true`, `touchPanning = false`.
  - `Move` → if moved beyond `ViewConfiguration.ScaledTouchSlop`, the gesture
    becomes a pan: `gameView.NotifyDrag(x, y, lastX - x, lastY - y,
    Event.Button.Right)` (the game scrolls the map on right-button drags).
    Otherwise just `gameView.SetCursorPosition(x, y)` (hover).
  - `Up` → if it was a pan: `gameView.NotifyStopDrag()`; if it was a tap:
    `gameView.NotifyClick(x, y, Event.Button.Left, false)`.
  - `Cancel` → `NotifyStopDrag()` if panning.
- `Window_Update` (SDL thread), when `pinchActive`:
  ```csharp
  float startFactor = 1.0f + pinchStartZoom * 0.5f;
  float ratio = pinchCurrentDistance / pinchStartDistance;
  float newZoom = (startFactor * ratio - 1.0f) * 2.0f;
  gameView.Zoom = Math.Clamp(newZoom, 0.0f, 4.0f);
  ```
  Factor-space math (`zoomFactor = 1 + zoom * 0.5`) so zooming works correctly
  starting from `zoom = 0`.
- `CanZoom` (`Freeserf.Core/UI/Interface.cs:172`) = ingame && viewport enabled &&
  no notification/popup box — pinch and pan only work when that is true.
- `pinchActive`/`touchActive`/`touchPanning` are reset in `OnPause`/`OnStop` so a
  stale gesture can't cause a zoom jump or spurious click after resume.
- The Bluetooth-mouse `Mouse_Scroll` zoom handler and right-button drag are
  unchanged.

### Known minor edge cases (accepted)

- After a pinch ends with one finger still down, SDL may still think the left
  mouse button is held (it never saw the second finger's down/up). The remaining
  finger's moves are passed to SDL and can briefly act as a drag until the last
  finger lifts.
- After a pinch, the remaining finger continues as a pan (drag) until it lifts —
  no spurious click is sent.
- Taps activate on finger release (not press) while ingame; on the main menu and
  with popups open, taps still activate on press via SDL mouse emulation.

## Exit button closes the app (implemented)

The main menu's **Exit** button now closes the app. On desktop this already
worked: `GameInitBox` `Action.Close` → `interf.RenderView.Close()` →
`GameView.Close()` fires the `Closed` event, which `MainWindow` handles by
closing the window. On Android `MainActivity` never subscribed to
`GameView.Closed`, so the view was disposed but the app stayed open.

Fix in `FreeserfNet.Android/MainActivity.cs`:

- `MainActivity` keeps a static `instance` reference (set in the constructor).
- `Window_Load` subscribes `gameView.Closed += GameView_Closed`.
- `GameView_Closed` nulls `gameView`, calls `view.Close()` (stops the render
  loop; `Window_Closing` still saves the user config) and then
  `instance?.RunOnUiThread(() => instance.Finish())`.

Why `Finish()` is required: Silk.NET's `SdlView.Close()` only sets `IsClosing`
and raises `Closing` — it does not finish the Android activity. `Finish()` is
marshalled to the UI thread with `RunOnUiThread` because `Closed` fires on the
SDL thread. The `if (gameView != null)` guard prevents re-entrancy when
`Window_Closing` itself calls `gameView.Close()`.

The in-game menu's "Quit" (SettlerMenu → QuitConfirm) intentionally returns to
the main menu on all platforms and is unchanged.

## Open issue: crash when pressing in-game menu button (UNRESOLVED, under investigation)

- Symptom (Pixel 6 emulator, x86_64, Android 16): tapping an in-game panel/menu button
  kills the app. Logcat shows only the secondary error
  `InvalidOperationException: You cannot call Reset inside of the render loop!`
  (`ViewImplementationBase.Reset` <- `Dispose` <- `MainActivity.OnRun`), followed by
  `SIGABRT` (pthread_mutex_lock on a destroyed mutex). This is probably a follow-up
  failure during cleanup; the original exception is not yet known.
- Diagnostics added: `OnRun` now logs the full exception (`ex`, not `ex.Message`) and
  guards `view.Dispose()`; touch/mouse input is logged under tag `Freeserf_Input`
  (raw coordinates and the transformed view coordinates).
- Coordinates: `adb shell input tap` uses landscape coordinates (2400x1080 on the
  emulator). The virtual screen is 1920x864 (factor 0.8). Panel bar Settler button
  is at roughly view (1104..1162, 799..857), i.e. tap about (1420,1030).
  A tap at (1420,1030) was mapped to view (1136,824) with no crash in the last test,
  so the crash is not yet reliably reproduced with the diagnostic build.
- Next: reproduce with the diagnostic build, read `Freeserf_Error` / `Freeserf_Input`
  logs for the original exception, then fix.
- After changing the Android project, do a clean rebuild (delete bin/obj), otherwise
  `n_onStart` UnsatisfiedLinkError occurs (see Crash 2).
