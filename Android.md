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
- The game data file `SPAE.PA` is **copyrighted and kept outside the repo** at
  `C:\git\freeserf.net\SPAE.PA`. It is referenced in the csproj as an
  `<AndroidAsset>` and extracted to app storage on first run
  (`ExtractBundledData()` in `MainActivity.cs`).
- The desktop host project is `FreeserfNet/`; `GameView.cs` is compiled directly into
  the Android project via a `<Compile Include="..\FreeserfNet\GameView.cs" Link=...>`.

## Building the Release APK

### Working build command (Windows, .NET SDK 10.0.400, Android workload 36.1.69)

```powershell
$env:MSBUILDDISABLENODEREUSE = 1
dotnet build FreeserfNet.Android\FreeserfNet.Android.csproj -c Release `
  -m:1 -nodeReuse:false -p:PublishTrimmed=false -p:RunAOTCompilation=false
```

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

## Known non-fatal warnings (ignore)

- `Shared library 'bass' not loaded, p/invoke 'BASS_Init' may fail` — audio library
  not bundled; audio is unavailable but the game runs.
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

### Root cause hypothesis (OPEN)

- The intro mission map (Landscape layer, `RenderMap` triangles) is **not rendering at
  all** — the map should fill the viewport (VirtualScreen 1280x960) with tiles.
- The GameInitBox background sprites are also not rendering.
- Only a few GUI elements render. Possible causes to investigate:
  - Viewport clipping / letterboxing (virtualScreenDisplay Rect(480,0,1440,1080))
  - Texture atlas / sprite visibility issue (`Box.cs` BackgroundPattern.Draw line 106,
    sprite visibility check line 220)
  - Landscape layer not visible / map not attached (`EnsureViewport()` Interface.cs
    line 330)
  - Masked triangle shader issue on this platform (map uses `MaskedTriangleShader`)
- The map scrolls randomly when not ingame (`GameView.Render()` line ~445) — the
  content did not change between screenshots, suggesting the map layer truly isn't
  drawing.

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
- [ ] **Black background (Issue 4)** — map + GameInitBox background not rendering;
      only a partial dialog render shows. Root cause under investigation.
- [ ] Consider switching the build to `dotnet publish` + Ambermoon's
      `AndroidLinkMode=None` settings for reproducible builds.
