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
- The game data file `SPAE.PA` is **copyrighted and kept outside the repo** and
  is **NOT bundled by default**. On first start the user can either pick their
  own data file via the system file picker (`MainActivity.ShowFilePicker`),
  which copies it to app storage, or download it directly from **Ubisoft
  Connect** (`MainActivity.ShowUbisoftLoginDialog` + `Ubisoft/SpaeDownloader.cs`):
  the user logs in with their Ubisoft account, the app verifies ownership of
  "The Settlers - History Edition" (product id 11662) and downloads only the
  `SPAE.PA` file (manifest + slices, not the whole game). The download uses the
  unofficial Ubisoft Connect API (REST login at `public-ubiservices.ubi.com`,
  protobuf/TLS demux socket at `dmx.upc.ubisoft.com`); the protobuf definitions
  live in `FreeserfNet.Android/Ubisoft/Protobuf/` and are generated at build
  time via `Grpc.Tools`. To bundle a data file anyway (e.g. for a private
  test build), pass `-p:FreeserfGameDataPath="path\to\SPAE.PA"`; it is then
  extracted to app storage on first run (`ExtractBundledData()` in
  `MainActivity.cs`).
- The desktop host project is `FreeserfNet/`; `GameView.cs` is compiled directly into
  the Android project via a `<Compile Include="..\FreeserfNet\GameView.cs" Link=...>`.

## Building the Release APK

### Canonical build (recommended)

Use the build script — it wraps the working build command with all required flags:

```powershell
.\build-android.ps1                 # incremental build (safe against Crash 2)
.\build-android.ps1 -Clean          # full clean rebuild (delete bin/obj first)
.\build-android.ps1 -FreeserfGameDataPath "D:\path\to\SPAE.PA"   # bundle game data
```

Smoke-test the result on a connected device/emulator:

```powershell
.\test-android.ps1                  # install, launch, grep logcat for Crash 2
```

### Working build command (Windows, .NET SDK 10.0.400, Android workload 36.1.69)

```powershell
$env:MSBUILDDISABLENODEREUSE = 1
dotnet build FreeserfNet.Android\FreeserfNet.Android.csproj -c Release `
  -m:1 -nodeReuse:false -p:PublishTrimmed=false -p:RunAOTCompilation=false
```

`PublishTrimmed`, `RunAOTCompilation`, and the Ambermoon settings
(`AndroidLinkMode=None`, `AndroidLinkTool=none`, `AndroidEnableProguard=false`,
`AndroidEnableR8=false`) are now baked into `FreeserfNet.Android.csproj`, so the
`-p:` flags are optional. The csproj also force-regenerates the type-registration
table on every build (see Crash 2 below), so incremental builds are safe.

To include an external `SPAE.PA` (bundled into the APK), add
`-p:FreeserfGameDataPath="D:\path\to\SPAE.PA"` to the build command. Without
it, the APK ships without game data and the user imports it via the file
picker on first start.

Output APK: `FreeserfNet.Android\bin\Release\net10.0-android\net.freeserf.android-Signed.apk`
(~101 MB with trimming/AOT disabled and data bundled; ~30 MB when trimmed;
~4 MB without bundled data).

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
- The type-registration crash (below) is now **prevented automatically**: the
  `ForceFreshTypeRegistration` target in `FreeserfNet.Android.csproj` regenerates the
  type-registration table on every build, and the Ambermoon settings are baked into the
  csproj. `dotnet publish` remains an option if a switch is ever wanted, but is not
  required.

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

## Loading indicator at startup (implemented)

On slow devices the app used to show a black screen for several seconds between
launch and the main menu, because `Window_Load` (SDL thread) runs
`Global.Init()`, `UserConfig.Load()`, game data loading (`SPAE.PA`) and
`InitializeAfterDataLoad()` (GameView, State.Init, shader/atlas setup) before the
first frame is rendered.

A native Android loading overlay (spinner + "Freeserf" + "Lädt…" on a black
background) is now shown on top of the SDL surface from activity start until the
first frame (main menu) is rendered:

- `MainActivity.OnCreate` shows the overlay (only when `!initialized`, so an
  activity recreation does not re-show it).
- `Window_Render` hides it after the first rendered frame (`firstFrameRendered`).
- `Window_Load` hides it when no game data is available and the data import
  dialog is shown, so the file picker stays usable.
- After the user imports a data file, the overlay is shown again while the
  imported data is loaded and the game initializes.

Implementation notes:

- The overlay is a plain Android view (`LinearLayout` with `ProgressBar` +
  `TextView`s) added via `AddContentView`, i.e. on top of the SDL surface. It
  does not depend on the GL renderer, which is not available yet during the slow
  phase.
- While visible it intercepts touches (intended – no interaction with a
  half-initialized game).
- Android 12+ additionally shows the system splash screen (app icon) before the
  activity; the overlay takes over from there.

## Ubisoft Connect SPAE.PA download (implemented)

Users who own **The Settlers 1 History Edition** on Ubisoft Connect can download
the copyrighted `SPAE.PA` data file directly in the app instead of picking it
via the file picker. The data import dialog (`MainActivity.ShowDataImportDialog`)
now offers both options.

Flow (all in `FreeserfNet.Android/Ubisoft/`):

1. **Web login** (`MainActivity.ShowUbisoftLoginDialog`): a full-screen WebView
   opens Ubisoft's official login page
   (`https://connect.ubisoft.com/login?appId=...&genomeId=...`). The user logs
   in on Ubisoft's own page — no password is ever typed into the app. After
   login the session (ticket + sessionId) is read from the page's localStorage
   (`PRODloginData`) and renewed under the app id
   (`UbisoftLogin.RenewSessionAsync`, `PUT /v3/profiles/sessions`).
2. **Demux socket** (`DemuxClient.cs`): TLS 1.2 to `dmx.upc.ubisoft.com:443`,
   protobuf framing (4-byte big-endian length prefix; some pushes arrive raw
   with first byte `0x12`). Authenticates with the login ticket and opens
   service connections.
3. **Ownership** (`OwnershipService.cs`): `ownership_service` returns the owned
   games; the app checks for product id **11662** ("The Settlers - History
   Edition") and requests an ownership token.
4. **Download URLs** (`DownloadService.cs`): `download_service` returns CDN URLs
   for the manifest and for individual slices.
5. **Manifest** (`ManifestParser.cs`): the manifest file is downloaded, the
   356-byte header skipped, zlib-decompressed and parsed as protobuf
   `Mg.Protocol.Download.Manifest`. Only the `SPAE.PA` file entry is used
   (matched by name, also with a path prefix like `loca/SPAE.PA`).
6. **Slices** (`SpaeDownloader.cs`): the slices of `SPAE.PA` are downloaded
   (path `slices/{sha1}` or `slices_v3/{dir}/{sha1}` for manifest version 3;
   uppercase hex, the CDN is case-sensitive), decompressed (zstd via
   `ZstdSharp.Port`, deflate via `ZLibStream`; lzham is not supported on
   Android) and concatenated into `SPAE.PA` in the game data folder.

Protobuf definitions are in `FreeserfNet.Android/Ubisoft/Protobuf/*.proto`
(from UplayDB/Protobufs) and are generated at build time by `protoc` via the
`Grpc.Tools` package. NuGet packages added: `Google.Protobuf`, `Grpc.Tools`,
`ZstdSharp.Port`.

Notes:

- This uses the **unofficial** Ubisoft Connect API (same endpoints the Ubisoft
  Connect client uses). Ownership is verified server-side; users without the
  History Edition get a clear error.
- The login happens on **Ubisoft's official page** in a WebView — the app never
  sees the password. Two-factor authentication is handled by Ubisoft's page.
- Only the manifest and the `SPAE.PA` slices are downloaded — not the whole game.
- The `INTERNET` permission was already present in the manifest.

## Crash 1: `System.TypeInitializationException` at startup (FIXED)

- Symptom: `FATAL UNHANDLED EXCEPTION: System.TypeInitializationException` →
  `NullReferenceException` in `Freeserf.Global..cctor()`.
- Root cause: `Assembly.GetEntryAssembly()` returns **null on Android**.
  `Freeserf.Core\Freeserf.cs` (lines ~34-36) used it in three static field
  initializers (`Version`, `VERSION`, `EXTENDED_VERSION`).
- Fix: replace with `Assembly.GetExecutingAssembly()`.
- Note: `Freeserf.Core\FileSystem\Paths.cs` (line ~99) also uses `GetEntryAssembly()`
  but is guarded by `!OperatingSystem.IsAndroid()` — safe.

## Crash 2: `n_onResume` / `n_loadLibraries` UnsatisfiedLinkError (FIXED + PREVENTED)

- Symptom: `java.lang.UnsatisfiedLinkError: No implementation found for void
  crc64bcc776d209640335.MainActivity.n_onResume()` — app crashes immediately on launch.
  `n_loadLibraries` for `SilkActivity` (crc647ab54b95e567f95c) was also unregistered
  (that one is non-fatal, caught).
- Root cause: **stale/incremental build artifact**. The Mono type-registration table
  (`libxamarin-app.so`) did not match the generated Java stubs. The `n_*` native
  methods in the generated Java wrappers are registered via this table; when it is
  stale, the JVM cannot find the native implementation.
- Original fix: **delete `FreeserfNet.Android\bin` and `FreeserfNet.Android\obj` and rebuild
  from scratch.** The clean rebuild produced a working APK (verified: `onResume()`
  runs, `Running main function SDL_main from library libmain.so`, no FATAL).
- **Permanent prevention (implemented):** the `ForceFreshTypeRegistration` MSBuild
  target in `FreeserfNet.Android.csproj` deletes the `_CleanIntermediateIfNeeded.stamp`
  before the SDK's `_CleanIntermediateIfNeeded` target on **every** build. This makes
  the SDK's own `_CleanMonoAndroidIntermediateDir` target run, which cleans the Android
  intermediate (`android\`, `stamp\`, `app_shared_libraries\`, flags) while keeping the
  C# compile output and resolved assemblies. The whole stub → typemap → marshal_methods
  → `.o` → `libxamarin-app.so` chain is then regenerated consistently — exactly what a
  clean build does for these files. Incremental builds are now safe; no more manual
  bin/obj deletion. Disable with `-p:ForceFreshTypeRegistration=false` if ever needed.
  Use `.\build-android.ps1` and `.\test-android.ps1` for the canonical
  build + smoke-test workflow.
- **Why a plain incremental build is not safe (two .NET Android bugs):** (1) the linked
  `libxamarin-app.so` is not relinked when the native-assembly objects change, and
  (2) regenerating the marshal methods table without a build-properties change produces
  an **incomplete** table (only the Java.Interop runtime class, no user types) — the
  `n_*` methods then have no native implementation. Both are avoided by the forced
  intermediate clean above.

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

- adb: `C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe`. Added to the
  **user PATH** (2026-10-05) so `adb` works in newly opened terminals. Existing shells
  still need the full path or a PATH refresh (`$env:Path += ";C:\Program Files (x86)\Android\android-sdk\platform-tools"`).
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
- [x] `n_onResume` type-registration crash fixed by clean rebuild (bin/obj deleted);
      **permanently prevented** by the `ForceFreshTypeRegistration` csproj target.
- [x] GLSL ES shader compilation errors fixed (`db096be`).
- [x] **EGL_BAD_SURFACE in render loop** — shaders compile, but `eglSwapBuffers` fails with `EGL_BAD_SURFACE`; screen stays black. **FIXED**: Added `DoEvents()` and activity state tracking (commit `16f3179`). App now runs on device without EGL errors when the screen is awake.
- [x] Ambermoon's reproducible build settings (`AndroidLinkMode=None`, etc.) baked into
      `FreeserfNet.Android.csproj`; `dotnet publish` switch no longer needed.

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
- [x] `n_onResume` type-registration crash fixed by clean rebuild (bin/obj deleted);
      **permanently prevented** by the `ForceFreshTypeRegistration` csproj target.
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
- [x] Ambermoon's reproducible build settings (`AndroidLinkMode=None`, etc.) baked into
      `FreeserfNet.Android.csproj`; `dotnet publish` switch no longer needed.

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

## Touchscreen special click (implemented)

### Goal and current semantics

On desktop a simultaneous left and right mouse button press invokes
`GameView.NotifySpecialClick`. `Gui` handles `Event.Type.SpecialClick` through
the same path as a left-button double click. On Android, a one-finger tap is a
left click, a one-finger drag pans the map, a moving two-finger gesture pinches,
and a stationary two-finger tap produces the special click.

### Implemented gesture: short, stationary two-finger tap

A **two-finger tap** triggers a special click at the first finger's original
position. The first finger selects the target and the second finger acts as the
modifier, matching the desktop "left button, then right button" sequence. This
is preferable to using the midpoint because it gives the user a predictable
target even when the two fingers are not exactly colocated.

Gesture recognition applies only while `touchInputEnabled` is true (the same
ingame, non-popup condition used by pan and pinch):

| Gesture | Recognition | Result |
|---|---|---|
| One-finger tap | Lift without moving beyond `ScaledTouchSlop` | Left click |
| One-finger drag | Movement beyond `ScaledTouchSlop` | Right-button map pan |
| Two-finger pinch | Either tracked finger moves beyond `ScaledTouchSlop` | Existing pinch zoom |
| Two-finger tap | The first finger lifts within `ViewConfiguration.DoubleTapTimeout` and neither finger moves beyond `ScaledTouchSlop` | `NotifySpecialClick(firstDownX, firstDownY)` |

The two-finger tap takes precedence only while it remains stationary. As soon
as either finger crosses the movement threshold, discard the candidate and
continue with the existing pinch logic. A third finger always discards the
candidate and remains a pinch/multi-touch gesture; it must never trigger a
special click.

### Implementation outline

`DispatchTouchEvent` maintains:

- `specialTapCandidate`: set when the second pointer goes down.
- `specialTapStartTime`, `specialTapX`, and `specialTapY`: capture
  `SystemClock.ElapsedRealtime()` and the primary pointer position at that
  moment.
- Per-pointer start positions for the first two fingers, used to compare all
  movement with `ScaledTouchSlop`.
- `suppressNextPrimaryUp`: prevents the remaining primary finger's final `Up`
  from becoming a normal tap or a `NotifyStopDrag` after a recognized
  two-finger tap.

Process the Android actions as follows:

1. On `PointerDown` for pointer two, start both the existing pinch tracking and
   the special-tap candidate. Do not emit any game event yet.
2. On `Move`, cancel the special candidate when either pointer has moved farther
   than the slop. The existing pinch distance update continues unchanged.
3. On `PointerUp`, recognize the special tap only if it is still a candidate,
   exactly two pointers participated, and the first lift is within the
   double-tap timeout. Queue `view.NotifySpecialClick(specialTapX,
   specialTapY)` into `pendingTouchEvents`, then set
   `suppressNextPrimaryUp`.
4. On the final `Up`, consume and clear the suppression flag. On `Cancel`,
   `OnPause`, and `OnStop`, clear every special-tap field without emitting an
   event.

`NotifySpecialClick` must be enqueued through `pendingTouchEvents`; it must
never be called by `DispatchTouchEvent` directly. This preserves the fix for
the in-game-menu race: all Core GUI changes occur on the SDL thread in
`Window_Update`.

### Acceptance tests

1. In a running game, a short two-finger tap on a map tile causes the same game
   behavior as a desktop simultaneous left/right click at that tile.
2. A two-finger spread, pinch, or any two-finger movement beyond slop changes
   zoom only and never invokes `NotifySpecialClick`.
3. A normal one-finger tap and pan retain their current behavior.
4. After a special tap, lifting the first finger produces neither an additional
   left click nor a stray stop-drag event.
5. Repeated special taps on panel controls and map tiles leave the process
   alive, with no `Freeserf_Error`, `AndroidRuntime`, or render-loop reset
   error in logcat.

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

## In-game menu button crash (FIXED)

- Cause: `DispatchTouchEvent` runs on Android's UI thread, but directly invoked
  `GameView.NotifyClick`, `NotifyDrag`, and cursor methods. A panel/menu tap therefore
  changed the Core GUI object graph on the UI thread while the SDL render thread was
  traversing it. The `ViewImplementationBase.Reset` exception in logcat was only a
  secondary cleanup failure after that race.
- Fix: touch actions are queued in a `ConcurrentQueue` and drained by
  `Window_Update` on the SDL thread. `CanZoom` and the current zoom are mirrored by
  that thread for gesture recognition, so `DispatchTouchEvent` no longer reads or
  writes `GameView`/GUI state directly.
- Diagnostics remain enabled: `OnRun` logs complete exceptions and
  `Freeserf_Input` logs the touch coordinates after their queued action is processed.
- No clean rebuild needed after Android project changes: the
  `ForceFreshTypeRegistration` csproj target regenerates the type-registration table
  on every build, so the `n_onStart`/`n_onResume` UnsatisfiedLinkError (Crash 2)
  cannot occur. Use `.\build-android.ps1` + `.\test-android.ps1`.

## Android device test: 2026-10-02

### Test environment and passed checks

- Device: Android 16 x86_64 emulator (`emulator-5554`, 2400x1080 landscape).
- Build: Release APK built with
  `-p:FreeserfGameDataPath="D:\freeserf.net\SPAE.PA"`. The data file must be
  supplied explicitly because it is not committed to the repository.
- Startup loaded game data, initialized `GameView`, and rendered the main menu
  and background map correctly. Logcat reported no `Freeserf_Error` or
  `AndroidRuntime` error.
- Background/foreground lifecycle (Home followed by relaunch), raw tap, and
  raw swipe left the process alive without a crash.

### Reproducible bugs

#### Main-menu buttons ignore Android touch input (BLOCKER)

- **Symptom:** The visible **Start**, **Options**, and **Exit** buttons do not
  react to touchscreen input. The application stays on the new-game screen;
  **Exit** also does not close the activity.
- **Evidence:** `adb shell input tap 615 270`, `1050 270`, and `1056 493`
  respectively emit `Freeserf_Input: Mouse down: <x>,<y> button=Left`, but
  produce no UI transition, no popup, and no process exit. The coordinates
  target the visible centers of the three buttons on the 2400x1080 emulator.
- **Impact:** A player cannot start a game or open options on Android. This also
  blocks an end-to-end validation of the in-game menu fix and touchscreen
  special click.
- **Investigation starting point:** `Mouse_MouseDown` in
  `FreeserfNet.Android\MainActivity.cs` receives the SDL mouse event and calls
  `GameView.NotifyClick`; trace the transformed coordinates and `GameInitBox`
  button hit-testing from that call to identify why the click is not consumed.

#### Missing-data startup fails unclearly (MEDIUM)

- **Symptom:** A Release APK built without `FreeserfGameDataPath` stays black
  after launch rather than reporting that copyrighted game data is required.
- **Evidence:** The APK contains no `SPAE.PA`; `Window_Load` logs
  `UnauthorizedAccessException: Access to the path '/' is denied` from
  `DataSourceAmiga.CheckFiles` while `Data.Load` searches fallback paths.
- **Impact:** The app does not provide a usable failure screen or actionable
  message when its required external asset was omitted.
- **Expected behavior:** Catch a missing-data condition before the fallback
  filesystem scan and show a clear in-app or Android error explaining how to
  rebuild with `FreeserfGameDataPath`.

### Compatibility risk detected during the build

- The Android SDK emits `XA0141` for `libmain.so` and `libSDL2.so` from
  `Silk.NET.Windowing.Sdl` 2.23.0: they are not built for Android 16's required
  16-KB page size. The x86_64 emulator used above runs the APK, so this was not
  reproduced as a runtime failure; it remains a deployment risk for devices
  that enforce 16-KB pages. Update or rebuild the upstream SDL dependency
  before treating Android 16 / 16-KB-page hardware as supported.

## Android device test: 2026-10-04 (physical Pixel 8a)

### Test environment

- Device: **Pixel 8a** (`3C211JEKB03986`, arm64, Android 17, 2400x1080
  landscape), connected via USB, reachable via `adb`.
- Build: the APK installed on the device (installed 2026-10-02 22:38) contains
  the touch-queueing fix (`3fad9cb`) and the touchscreen special click
  (`18afac8`).
- Device locale is **German (de-DE)** — this matters for the crash signature
  below.

### In-game menu crash (FIXED - KeyNotFoundException)

The in-game menu crash reported by the user is **fixed**. The crash occurred
when the **SettlerMenu** was opened by tapping the **Sett button** in the
in-game PanelBar. Root cause: a **`KeyNotFoundException`** thrown by
`TextureAtlas.GetOffset` when a sprite index was missing from the texture
atlas (the game data does not contain every sprite the SettlerMenu box tries
to draw). The exception propagated out of the SDL run loop and the app closed
without logging it (the `de-DE/System.Private.CoreLib.resources` warnings in
logcat were the only signature, emitted while formatting the exception
message for the German locale).

#### Reproduction

- Start a game, then tap the Sett button (the settler/people icon) in the
  bottom PanelBar. The app closes immediately.
- On the 2400x1080 device the Sett button is at physical screen
  `(1433, 1034)` (tap center). Coordinate chain:
  physical `(1433, 1034)` → virtual `(1146, 827)` (scale 0.8) → GUI
  `(423, 459)` (scale 1.8, offsetX 384). The Sett button's GUI rect is
  `(400, 444)-(432, 476)` (PanelBar at `(144, 440)-(496, 480)`, button 4 at
  `(256, 4)` + 32x32), so the tap hits it.

#### Logcat evidence (crash at 14:24:31)

```
Freeserf_Input: Touch down: 1433,1034
Freeserf_Input: Touch up: 1433,1034 -> 1146,827 panning=False   <- tap, becomes left click
monodroid-assembly: Assembly 'de-DE/System.Private.CoreLib.resources' (hash ...) not found   (x4)
monodroid-assembly: Assembly 'de/System.Private.CoreLib.resources' (hash ...) not found      (x4)
SDL: Finished main function
WindowManagerShell: Transition type = CLOSE (activity closing)
SDL: onPause() / onStop() / onDestroy()
SDL: SDLActivity thread ends (error=Try to release egl_surface with context probably still active)
```

- **No** `FATAL EXCEPTION`, **no** `AndroidRuntime` error, **no** tombstone in
  `/data/tombstones/`, **no** `Freeserf_Error` / `Run:` / `Render:` log.
- The `de-DE/System.Private.CoreLib.resources` warnings are the key signature:
  they are emitted when the .NET runtime performs a culture-specific operation
  for the German locale — most commonly **formatting an exception message**.

#### Root cause hypothesis

- An exception is thrown while the SettlerMenu is opened
  (`Interface.OpenPopup` → `PopupBox.Show(SettlerMenu)` →
  `SetBox` → `DrawSettlerMenuBox`).
- Formatting that exception's message triggers the German satellite-resource
  assembly load (`de-DE/System.Private.CoreLib.resources`), which is not
  bundled → the `monodroid-assembly` warnings.
- The exception propagates out of the run loop (`view.Run` → `OnRun`) and the
  app closes **without logging the exception** (no `Run:`/`Render:`/
  `Freeserf_Error`). The exact exception type is still unknown.
- The earlier touch-queueing fix (`3fad9cb`) addressed the UI-thread race, but
  this crash is a different failure: it happens on the SDL thread while the
  menu is being opened, not on the UI thread.

#### Fix

1. `Freeserf.Renderer/TextureAtlas.cs` - `GetOffset` now uses
   `TryGetValue` and returns the atlas origin `(0, 0)` instead of throwing
   `KeyNotFoundException` when a sprite index is missing. This is the safety
   net for any sprite lookup.
2. `Freeserf.Core/Render/TextureAtlasManager.cs` - `AddGuiElements` now adds
   a 1x1 placeholder sprite for missing sprites instead of skipping the
   index, keeping the sprite index space contiguous so later lookups never
   hit a gap.
3. `Freeserf.Core/UI/Interface.cs` - `OpenPopup` is wrapped in a try/catch
   that logs the full exception (`Log.Error.Write`) before rethrowing, so a
   future failure in the popup path is visible in logcat instead of silently
   killing the app.
4. `FreeserfNet.Android/MainActivity.cs` - the run loop and the
   `pendingTouchEvents` drain in `Window_Update` are wrapped in try/catch
   that log the full exception, so any future exception on the SDL thread is
   captured in logcat before the app closes.

#### Verification

- Rebuilt the APK, installed on the physical device (2400x1080, German
  locale), started a game and tapped the Sett button: the SettlerMenu opens
  without a crash.
- No `monodroid-assembly` `de-DE/System.Private.CoreLib.resources` warnings
  and no app close on menu open.

### Main-menu buttons ignore Android touch input (BLOCKER, confirmed on device)

- **Symptom:** The **Start**, **Options**, and **Exit** buttons on the main
  menu do not react to touch. The app stays on the new-game screen.
- **Evidence (physical device):** `adb shell input tap 885 387` (Start button,
  GUI `(180, 172)`) emits `Freeserf_Input: Mouse down: 885,387 button=Left`
  but produces no UI transition and no `Freeserf_Error`.
- **Analysis:** The click chain looks correct in code:
  `Mouse_MouseDown` → `GameView.NotifyClick` → `ScreenToView` (physical →
  virtual) → `Gui.RenderView_Click` → `PositionToGui` (virtual → GUI) →
  `viewer.SendEvent` → `Interface.HandleEvent` → `GameInitBox` hit-test. The
  tap coordinates map exactly onto the Start button rect, yet the click is not
  consumed. The break is somewhere in this chain and needs diagnostics
  (log the transformed coordinates at each step) to locate.
- **Impact:** Blocks starting a game from the touchscreen, which also blocks
  end-to-end validation of the in-game menu fix. The crash above was
  reproduced because the game was already running (started before this test
  session).

## Multiplayer on Android (crash FIXED + cross-platform groundwork)

### Crash cause (two independent bugs)

1. **Missing `NetworkDataReceiver` on Android.** The desktop host
   (`FreeserfNet/MainWindow.cs`) creates a `NetworkDataReceiver` and assigns it to
   `gameView.NetworkDataReceiver` every frame in `MainWindow_Update`. The Android host
   (`FreeserfNet.Android/MainActivity.cs`) only called `gameView.UpdateNetworkEvents()`
   without ever assigning the receiver, so `Gui.NetworkDataReceiver` (and therefore
   `Server/Client.NetworkDataReceiver`) stayed `null`. When a remote participant sent
   data, `Server.HandleData` threw `ExceptionFreeserf("Network data receiver is not set up.")`
   *outside* its try/catch, on a background `Task.Run` thread that only catches
   `ObjectDisposedException` -> unhandled exception on a background thread -> app crash.
   - Fix: `MainActivity` now creates a receiver in `Window_Load` and assigns it in
     `Window_Update` (mirrors desktop). `Server.HandleData` now logs + drops instead of throwing.

2. **`Host.GetLocalIpAddress()` used Windows-only APIs.** `UnicastIPAddressInformation.IsDnsEligible`
   and `PrefixOrigin` are `[SupportedOSPlatform("windows")]` and throw
   `PlatformNotSupportedException` on Android (visible as CA1416 build warnings). This crashed
   already when creating `LocalClient` (join) or `LocalServer` (host).
   - Fix: guard both property accesses with try/catch and fall back to "first usable address".

### Cross-platform multiplayer (PC <-> Android)

- The protocol is plain TCP on port 5067 with custom `FSN` framing, shared `Freeserf.Network`
  code -> already platform-neutral.
- `GameInitBox` now has a "Server IP:" text input (default `localhost`, hostname resolution
  supported) for joining, an editable server name, and shows the host IP in the lobby.
- `AndroidManifest.xml` gained `ACCESS_NETWORK_STATE` + `ACCESS_WIFI_STATE`.
- `MainActivity.OnStop` calls `GameView.DisconnectNetwork()` (new, delegates to `Gui`) so
  connections are closed when the app is backgrounded.
- LAN play works; internet play (NAT/port forwarding) is out of scope.

### Device test results (Samsung Galaxy A13, 2026-10-07; retested 2026-10-08)

- APK installed and launched; no crash. Server creation works: `LocalServer` binds to the
  WiFi IP (192.168.178.151:5067) and answers a `LobbyData` request with valid lobby data
  (verified with `nc` from the device itself).
- **Touch-mode layout overlap (FIXED):** in touch mode the GUI scale is 4.255 (not 1.84),
  so the first version of the server name input and host IP text overlapped the map seed
  input and the map size button. Fix: for the MultiplayerServer screen the map seed input
  is hidden (it is fixed at server creation), the map size button is moved into its place,
  and the server name input + host IP are shown in the freed rows above the player boxes.
- **Multiplayer join IP input overlap (FIXED):** the 15-character address field extended
  underneath the Options button. The client screen now uses a compact `IP:` label and moves
  the field left; the full `192.168.178.151` value was verified on the Galaxy A13 via ADB.
- **PC <-> Android over LAN:** the code works, but the test network blocked PC<->device
  traffic (AP client isolation: the device reached the gateway, the PC did not). This is a
  router configuration issue, not a code issue. For LAN play, both devices must be on the
  same network without client isolation (or the PC on the same WiFi as the phone).
