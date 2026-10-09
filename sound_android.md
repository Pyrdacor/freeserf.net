# Android audio support

## Status

Android audio is enabled with the official Un4seen BASS 2.4 core and BASSMIDI
2.4 native libraries. `arm64-v8a`, `armeabi-v7a`, and `x86_64` are bundled,
covering Pixel phones, 32-bit ARM devices, and the Android emulator. SFX and
MIDI music were verified on the connected SM-A135F (`armeabi-v7a`).

BASS is free for non-commercial use; commercial distribution requires the
appropriate Un4seen licence. See the official [BASS page](https://www.un4seen.com/bass.html).

## Current audio architecture (what already exists)

- `Freeserf.Audio/` (net9.0, referenced by the Android project) wraps
  **ManagedBass 4.0.2** + **ManagedBass.Midi 4.0.2**.
- `Freeserf.Audio/Bass/BassLib.cs`:
  - `EnsureBass()` → `MBass.Init(-1, 44100, 0u, 0, IntPtr.Zero)`.
  - `LoadMidiMusic(...)` → `BassMidi.CreateStream(events, ...)` +
    `BassMidi.StreamSetFonts(...)`; the SoundFont is loaded **from memory**
    via `BassMidi.FontInit(FileProcedures, ...)` reading the embedded
    resource `Freeserf.Audio.Bass.ChoriumRevA.SF2` (28.9 MB, embedded in the
    assembly).
  - `LoadModMusic(byte[])` → `MBass.MusicLoad(...)` (MOD/XM/IT/S3M built into
    core BASS, no add-on).
  - `LoadSfxMusic(byte[])` → `MBass.CreateStream(...)` with a
    `StreamProcedure` feeding PCM (from `SFX.ConvertToWav`).
- `Freeserf.Audio/Audio.cs` (`AudioImpl`): creates the MIDI player
  (`DataSource.DosMusic(...)` → `MidiPlayerFactory`) or MOD player, plus the
  wave player; disables sound if BASS is not initialized.
- `FreeserfNet/GameView.cs` (compiled into the Android app) creates
  `AudioFactory` in its constructor and wires `musicPlayer.Enabled` /
  `soundPlayer.Enabled` from `UserConfig.Audio.*`.
- The Android project references `Freeserf.Audio.csproj`; its native
  `libbass.so` and `libbassmidi.so` libraries are in
  `FreeserfNet.Android/lib/<abi>/` and are included in the APK.

## Research findings (verified)

### BASS supports Android natively

- Un4seen ships an Android build of BASS: **`libbass.so`** for
  **armeabi-v7a / arm64-v8a / x86 / x86_64** (armv5 `armeabi` was dropped in
  2.4.16). Download: `https://www.un4seen.com/stuff/bass-android.zip`
  (contains `arm64-v8a/`, `armeabi-v7a/`, `x86/`).
- BASS is **free for non-commercial use** (same licence as desktop; commercial
  use requires a paid licence, per-platform).
- Output on Android: **AAudio** (default on Android 8.1+), **OpenSL ES**
  (older), or **AudioTrack** (`BASS_DEVICE_AUDIOTRACK`). The Pixel 8a runs
  Android 14 → AAudio is used by default.
- `BASS_Init(-1, 44100, 0, 0, IntPtr.Zero)` is correct on Android: device
  `-1` = default output device; the `win`/`dsguid` args are ignored on
  non-Windows. Real-world Android apps (e.g. osu!droid) call exactly this.
- Android-specific config exists: `BASS_CONFIG_ANDROID_SESSIONID` (62) and
  `BASS_CONFIG_ANDROID_AAUDIO` (67) — both exposed by ManagedBass. Defaults
  are fine; no changes needed unless audio-focus issues appear.

### ManagedBass needs no Android-specific code

- ManagedBass uses plain `[DllImport("bass")]` (and `"bassmidi"` for the MIDI
  add-on). On .NET Android the runtime maps `"bass"` → `libbass.so` in the
  APK's `lib/<abi>/` directory automatically (the pinvoke override prepends
  `lib` and appends `.so`). **No `DllImportResolver` or manual loading is
  needed.**
- ManagedBass targets `netstandard2.0`/`net8.0`/`net9.0`/`net10.0` and lists
  Android as a supported platform; the existing `ManagedBass 4.0.2` packages
  are consumable by `net10.0-android` as-is.
- The ManagedBass packages do **not** ship native libs — you download the
  `.so` files from un4seen.com yourself (same as the desktop project already
  does for `bass.dll`/`bassmidi.dll`).

### Add-ons needed

- **`libbassmidi.so`** — required for MIDI + SoundFont playback
  (`BassMidi.FontInit` / `CreateStream` / `StreamSetFonts`). Available on
  Android. `FontInit` (memory/file-procedure loading) **is** supported on
  Android; only `BASS_MIDI_FontPack` and MIDI *input* are unavailable — we
  use neither.
- **MOD music** (XM/IT/S3M/MOD/MTM/UMX) is built into core BASS — no add-on.
- **`libbassmix.so`** — NOT needed (single music channel + SFX streams go
  straight to the device).

### Bundling native libs in a .NET Android APK

- MSBuild item: `<AndroidNativeLibrary Include="..." />`.
- The ABI is taken from the **parent folder name** (`arm64-v8a`,
  `armeabi-v7a`, `x86`, `x86_64`) or from `%(Abi)` metadata.
- The libs land in the APK's `lib/<abi>/` and are found by DllImport
  automatically.

### Reference project comparison: Ambermoon.net

- **Ambermoon.net does NOT use BASS.** Desktop uses **Silk.NET.OpenAL**;
  Android uses **`Android.Media.AudioTrack`** directly. Its music is the
  Amiga **Sonic Arranger tracker format**, decoded to PCM in pure C# (the
  `SonicArranger` NuGet package) and streamed to `AudioTrack` — no native
  audio libs, no SoundFont, no MIDI.
- So Ambermoon.net is **not** a BASS-on-Android reference, but it *does*
  prove a working "no native audio library on Android" pattern: decode to
  PCM in managed code → `AudioTrack.Write(...)` on a background task.
- freeserf.net's DOS data uses **XMI (MIDI)** music, which needs a
  synthesizer + SoundFont — that is exactly what BASS+bassmidi provides, and
  why BASS is the natural primary path here.

## Bundled libraries

The native libraries are downloaded from the official Un4seen Android
archives:

- Core BASS 2.4: `https://www.un4seen.com/files/bass24-android.zip`
- BASSMIDI 2.4: `https://www.un4seen.com/files/bassmidi24-android.zip`

Supported ABIs are checked in:

```
FreeserfNet.Android/lib/arm64-v8a/libbass.so
FreeserfNet.Android/lib/arm64-v8a/libbassmidi.so
FreeserfNet.Android/lib/armeabi-v7a/libbass.so
FreeserfNet.Android/lib/armeabi-v7a/libbassmidi.so
FreeserfNet.Android/lib/x86_64/libbass.so
FreeserfNet.Android/lib/x86_64/libbassmidi.so
```

The .NET Android SDK includes these `.so` files from `lib/<abi>/` in the APK.
Add the matching core and MIDI libraries together when supporting another ABI.

## Runtime behavior

`BassLib.EnsureBass()` calls `MBass.Init(-1, 44100, 0u, 0, IntPtr.Zero)`,
which is the correct Android call and logs an error if initialization fails.
If the MIDI add-on is unavailable, MIDI playback logs a warning and degrades
gracefully; BASS core audio and SFX can still work.

(`Freeserf.Audio/Bass/BassLib.cs` — `Log` already routes to logcat on
Android via `Freeserf_Error`.)

### Step 4 — SoundFont handling on Android

Two options, both supported by bassmidi on Android:

- **Keep the current in-memory `FileProcedures` approach (no change).**
  `BassLib.LoadMidiMusic` reads the embedded `ChoriumRevA.SF2` (28.9 MB)
  through managed `FileProcedures` callbacks. This is proven on desktop and
  `FontInit` is supported on Android. Risk: 28.9 MB read through managed
  callbacks at first MIDI load — BASS caches the font, so it is a one-time
  cost. **Try this first.**
- **Fallback: extract the SF2 to app storage and use a file path.**
  `MainActivity.cs` already has `ExtractBundledData()` for `SPAE.PA`; the SF2
  is an embedded resource, so add a one-time write of the resource stream to
  `FileSystem.Paths` app storage, then `BassMidi.FontInit(path, 0)`. Avoids
  managed-callback overhead; costs ~29 MB of app storage.

### Step 5 — lifecycle / audio focus (Android-specific)

- **Pause/resume:** when the activity pauses (screen off, backgrounded), BASS
  should pause. In `MainActivity.OnPause()` call `BassLib.PauseAll()`
  (`MBass.Pause()`), in `OnResume()` call `BassLib.StartAll()`
  (`MBass.Start()`). Guard with `BassLib.Initialized`. This mirrors the
  existing `ActivityState` handling for rendering.
- **Audio focus:** BASS on Android uses the AAudio session
  (`BASS_CONFIG_ANDROID_SESSIONID`); if ducking/other-app behavior matters,
  request audio focus via `Android.Media.AudioManager` and pause/resume
  accordingly. Nice-to-have; not required for first working version.

### Step 6 — build & deploy (see Verification)

## Fallback (Option B): managed audio via Android AudioTrack

If BASS proves unusable on Android (init fails, AAudio issues, licensing
concerns), replace the Android audio backend with the Ambermoon.net pattern:
decode to PCM in managed code and stream via `Android.Media.AudioTrack`.

- **SFX:** already PCM — `SFX.ConvertToWav(...)` produces `short[]` samples.
  Play via `AudioTrack` (static mode for short sounds) or `SoundPool`.
- **MOD music:** decode the MOD/XM data in managed code (a pure-C# MOD
  decoder, e.g. a managed port) and stream PCM via `AudioTrack` (stream
  mode, background task, like Ambermoon's `MusicManager`).
- **MIDI music (the hard part):** XMI → MIDI events need a synthesizer +
  SoundFont. Options:
  1. A managed SoundFont synthesizer (C# SF2 synth) — significant work.
  2. Pre-render XMI → WAV on desktop and bundle the WAVs — large APK, but
     simple and robust.
  3. Keep BASS only for MIDI and use AudioTrack for everything else.
- This option is a **new `Freeserf.Audio.Android` backend** implementing the
  same `Audio`/`Player`/`ITrack` contracts, selected in `AudioImpl` when
  `OperatingSystem.IsAndroid()` and BASS is unavailable. It is a larger
  change and is only pursued if Option A fails.

## Verification

1. **Clean rebuild** (delete `FreeserfNet.Android\bin` + `obj`, then):
   ```powershell
   $env:MSBUILDDISABLENODEREUSE = 1
   dotnet build FreeserfNet.Android\FreeserfNet.Android.csproj -c Release `
     -m:1 -nodeReuse:false -p:PublishTrimmed=false -p:RunAOTCompilation=false
   ```
2. **Confirm the libs are in the APK** (unzip the APK and check
   `lib/arm64-v8a/libbass.so` and `libbassmidi.so` are present).
3. **Deploy to the Pixel 8a** (`adb` at
   `C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe`):
   ```powershell
   adb install -r <apk>
   adb logcat -c
   adb shell am force-stop net.freeserf.android
   adb shell am start -n net.freeserf.android/crc64bcc776d209640335.MainActivity
   ```
4. **logcat checks** (wait ~25-30 s after launch):
   - The old warning `Shared library 'bass' not loaded` must be **gone**.
   - `Freeserf_Error` must NOT contain `BASS init failed` or
     `No audio device available. Sound is deactivated.`
   - `Freeserf_Info` should show no audio-disable message.
5. **Audible check:** start a game — music should play (MIDI via
   ChoriumRevA.SF2 for DOS data), and SFX (clicks, digging, etc.) should be
   audible. Verify volume control (F-key volume up/down) works.
6. **Lifecycle check:** press Home / lock the screen → audio stops; return →
   audio resumes (Step 5).
7. **Regression:** desktop build still works (BASS desktop libs unchanged).

## Open questions / risks

- **BASS licensing:** free for non-commercial use; a commercial release needs
  a paid licence. Confirm before any store distribution.
- **un4seen.com download availability** from this machine (the site was
  unreachable during research — the `.so` files must be fetched from a
  network that can reach it).
- **SF2 in-memory loading on Android:** 28.9 MB through managed
  `FileProcedures` — verify no stutter at first MIDI load; fall back to
  extracting the SF2 to app storage if needed.
- **AAudio behavior on the Pixel 8a:** default output is AAudio; if init or
  playback misbehaves, try `BASS_DEVICE_AUDIOTRACK` (0x20000) or
  `BASS_CONFIG_ANDROID_AAUDIO` (67) to force a backend.
- **DllImport name resolution:** `"bass"` → `libbass.so` is the documented
  .NET Android behavior; if it ever fails, a `NativeLibrary.SetDllImportResolver`
  in `MainActivity` is the escape hatch.
- **Audio focus / other apps:** without explicit audio-focus handling, BASS
  may keep playing over other apps; acceptable for a first version.
- **APK size:** +~1-2 MB for the two arm64 `.so` files (negligible vs the
  ~101 MB untrimmed APK).
- **`Freeserf.Audio` TFM:** it targets `net9.0` and is already consumed by
  `net10.0-android`; no TFM change needed. If trimming is ever enabled, the
  `DllImport`-only usage must be kept (no reflection on BASS types).
