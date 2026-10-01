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

## Issue 3: `EGL_BAD_SURFACE` in render loop — black screen (OPEN)

### Symptom

- Shaders now compile (GLSL ES fixes in `db096be`), but the render loop fails with
  `EGL_BAD_SURFACE` from `eglSwapBuffersWithDamageKHR` — the EGL surface is lost, so
  the screen stays black.
- The ANR seen earlier is a symptom of the render errors and should disappear once
  rendering works.

### Current state of the code (`FreeserfNet.Android/MainActivity.cs`)

- `OnRun()`: registers `SdlWindowing`/`SdlInput` platforms, creates the view with
  `GraphicsAPI(ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default,
  new APIVersion(3, 0))`, attaches `Load`/`Render`/`Update`/`Resize`/`Closing` events,
  calls `view.Initialize()` then `view.Run(...)` with a custom loop:
  ```csharp
  view.Run(() => {
      if (!view.IsClosing) view.DoUpdate();
      if (!view.IsClosing) view.DoRender();
  });
  ```
  **No `DoEvents()` call in the loop.**
- `Window_Load`: `view.MakeCurrent()`, `Global.Init`, `ExtractBundledData()`,
  `data.Load(...)`, `State.Init(view)`, creates `GameView`, sets `initialized = true`.
- `Window_Render`: `if (!initialized) return;` then `gameView?.Render()` in try/catch,
  then **`view.SwapBuffers()` unconditionally**.
- `OnPause`/`OnResume`: only adjust audio volume — **no surface/EGL handling**.

### Silk.NET internals (decompiled from Silk.NET 2.23.0)

- `ViewImplementationBase.DoRender()`: makes the GL context current (if not already),
  invokes the `Render` event, then swaps buffers if `ShouldSwapAutomatically`.
- `ViewImplementationBase.Run(onFrame)`: `while (!IsClosing) onFrame();`.
- `SdlView.CoreGLContext => _ctx ??= new SdlContext(Sdl, SdlWindow, this)` — the
  `SdlContext` class owns the EGL surface/context and `SwapBuffers`.
- `SdlView.CoreReset()`: `CoreGLContext?.Dispose(); Sdl.DestroyWindow(SdlWindow);
  SdlWindow = null; _ctx = null;`
- `SilkActivity.Main()` (static, called from native `libmain.so`) → `Instance.Run()`
  → `OnRun()`.
- `SDLActivity` exposes static JNI callbacks `OnNativeSurfaceCreated/Changed/Destroyed`
  — this is how SDL notifies managed code about Android surface lifecycle.

### `SdlContext` mystery (unresolved)

- `SdlView.cs` references `new SdlContext(Sdl, SdlWindow, this)`, but the class was
  **not found** in any Silk.NET 2.23.0 assembly (`Silk.NET.Windowing.Sdl.dll` android +
  netstandard2.1, `Silk.NET.Core.dll`, `Silk.NET.SDL.dll`) nor in the GitHub source
  tree at tag v2.23.0. It presumably owns the EGL surface and
  `eglSwapBuffersWithDamageKHR` — locating it is a blocker for understanding the exact
  EGL surface lifecycle.

### Ambermoon.net comparison (the working reference)

Ambermoon.net (same Silk.NET 2.23.0 + SDL stack, working on Android) differs from
freeserf.net in three important ways:

1. **`DoEvents()` in the render loop** — Ambermoon's `Run()` loop is:
   ```csharp
   window.Run(() => {
       DoEvents();
       if (!window.IsClosing) window.DoUpdate();
       if (!window.IsClosing) window.DoRender();
   });
   ```
   freeserf.net's loop omits `DoEvents()`, so SDL events (including surface
   lifecycle events) are never pumped from the managed side.
2. **`ActivityState` tracking** — Ambermoon's `MainActivity` overrides
   `OnPause`/`OnStop`/`OnResume` to set `gameWindow.State` to `Paused`/`Stopped`/`Active`,
   and `Window_Render` skips rendering **and** `window.SwapBuffers()` when
   `State == ActivityState.Stopped`. freeserf.net has no lifecycle tracking and calls
   `view.SwapBuffers()` unconditionally.
3. **Initial swap in `Window_Load`** — Ambermoon calls `GLContext.SwapBuffers()` once
   after setup, before the first `DoRender()`.

### Working hypothesis (root cause not yet confirmed)

The EGL surface becomes invalid (`EGL_BAD_SURFACE`) because the render loop keeps
calling `eglSwapBuffers` with a stale surface — either because the Android surface was
recreated (activity lifecycle) without the EGL surface being recreated, or because the
surface is destroyed while the loop still renders. The missing `DoEvents()` and missing
lifecycle handling in `MainActivity.cs` are the most likely culprits.

### Next steps

- Locate `SdlContext` (check the decompiled `sdl-decomp/` output for a file containing
  it, or a nested class in `SdlView.cs`).
- Capture logcat around `SurfaceTexture`/`EGLContext`/`onSurfaceChanged` for the app
  process to confirm when the surface is lost.
- Port the Ambermoon pattern: add `DoEvents()` to the render loop, track
  `ActivityState` in `OnPause`/`OnStop`/`OnResume`, and skip `SwapBuffers()` when the
  surface is not valid.

## Current state / next steps

- [x] Startup crash fixed (`GetExecutingAssembly`), committed as `356a131`, pushed to
      `origin/android`.
- [x] Logging to logcat fixed (`Log.SetStream` + `ConsoleStream`), same commit.
- [x] `n_onResume` type-registration crash fixed by clean rebuild (bin/obj deleted).
- [x] GLSL ES shader compilation errors fixed (`db096be`).
- [ ] **EGL_BAD_SURFACE in render loop** — shaders compile, but `eglSwapBuffers` fails
      with `EGL_BAD_SURFACE`; screen stays black. See "Issue 3" above. Working
      hypothesis: missing `DoEvents()` in the render loop + missing surface/lifecycle
      handling in `MainActivity.cs` (Ambermoon.net handles both).
- [ ] Consider switching the build to `dotnet publish` + Ambermoon's
      `AndroidLinkMode=None` settings for reproducible builds.
