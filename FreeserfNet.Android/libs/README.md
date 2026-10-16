# 16 KB page-size aligned Silk.NET native libraries

These `.so` files replace the stock native libraries shipped inside the
`Silk.NET.Windowing.Sdl` NuGet package (its `Silk.NET.Windowing.Sdl.aar`),
which are only aligned to 4 KB pages (`p_align = 0x1000`). Android 15+/16
devices with 16 KB memory pages reject such libraries, and the .NET Android
build flags them with warning XA0141.

The files here are built with `-Wl,-z,max-page-size=16384`, so every
`PT_LOAD` segment has `p_align = 0x4000` (16 KB) with the required
offset/virtual-address congruence, and load correctly on 16 KB-page devices.

## Contents

| ABI | File | Purpose |
|-----|------|---------|
| `arm64-v8a` | `libSDL2.so` | SDL 2.30.8 shared library (SilkDroid-style build) |
| `arm64-v8a` | `libmain.so` | App entry point implementing `sdSetMain` + `main` |
| `x86_64` | `libSDL2.so` | SDL 2.30.8 shared library (SilkDroid-style build) |
| `x86_64` | `libmain.so` | App entry point implementing `sdSetMain` + `main` |

`armeabi-v7a` is intentionally not replaced: 16 KB page sizes only apply to
64-bit devices, and the XA0141 check ignores 32-bit ELF images.

## Origin

These binaries are the prebuilt 16 KB-aligned libraries from the
[Ambermoon.net](https://github.com/Pyrdacor/Ambermoon.net) Android port
(`AmbermoonAndroid/libs/`), which uses the same Silk.NET 2.23.0 stack and is
accepted by Google Play with 16 KB page-size support:

- `libSDL2.so` — SDL 2.30.8 compiled for Android with
  `-Wl,-z,max-page-size=16384` (SDL is zlib-licensed).
- `libmain.so` — compiled from `libsilkdroid.c` (Silk.NET's SilkDroid main,
  MIT-licensed, as used by Ambermoon.net) with the same linker flag.

Note: the NuGet package bundles SDL 2.32.10; these libs are SDL 2.30.8. SDL 2.x
keeps API/ABI compatibility within the 2.x line, and Ambermoon.net ships this
exact combination with Silk.NET 2.23.0, so it is a drop-in replacement.

## How the replacement works

`FreeserfNet.Android.csproj` contains a `ReplaceSilkNativeLibraries` target
that runs after the `_GetLibraryImports` build target (the point where the
AAR's native libraries are added to the `AndroidNativeLibrary` item group). It
removes the stock `libSDL2.so`/`libmain.so` and re-adds the files from this
folder with explicit `<Abi>` metadata.

## Updating

If Silk.NET ever ships 16 KB-aligned native libraries, delete this folder and
the `ReplaceSilkNativeLibraries` target from the csproj. To refresh these
binaries, rebuild SDL 2.x and `libsilkdroid.c` for Android with
`-Wl,-z,max-page-size=16384` (see the Ambermoon.net repository for the build
setup), or copy the current prebuilt files from
`Pyrdacor/Ambermoon.net` → `AmbermoonAndroid/libs/`.
