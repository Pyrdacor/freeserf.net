# Widescreen Support Plan (Android port)

## Problem

On the Pixel 8a (2400x1080, 20:9), the game renders in a 4:3 letterboxed area:
- Virtual screen is hardcoded to **1280x960 (4:3)** in `MainActivity.cs` (via `UserConfig.Video.ResolutionWidth/Height` defaults).
- `GameView.Resize` letterboxes it to **1440x1080**, leaving **480px black bars** on each side.
- The map only shows **40 tile columns** (1280/32).

Goal: map fills the full screen (no black bars, more map visible), GUI stays **undistorted and centered**.

## Approach (Option A): widescreen virtual screen + uniform centered GUI scaling

Make the virtual screen match the device aspect ratio (capped at `MAX_VIRTUAL_SCREEN_WIDTH=1920`), so `virtualScreenDisplay` = full screen -> no letterboxing. The map automatically gets more columns. The 640x480 GUI is scaled **uniformly** (min ratio) and **centered** instead of the current non-uniform stretch.

For the Pixel 8a: virtual screen = **1920x864 (20:9)**. GUI scale = min(1920/640, 864/480) = **1.8**, centered at offsetX=384, offsetY=0. GUI on-screen size stays identical to today (2.25x design size) - no UX regression.

### Why this is safe (verified in code)
- **Map**: `Map.AttachToRenderLayer` sizes the RenderMap from the virtual screen -> 60 columns (vs 40) automatically. `Context` projection (`CreateOrtho2D(0, w, 0, h, 0, 1)`) covers the widescreen area.
- **Cursor alignment**: map cursor sprites are on the **Builds layer** (no transformation), positioned at `TotalX + renderPos.X` with `TotalX = 0` (Viewport at (0,0) in Interface at (0,0)) -> independent of GUI transformation. **No change needed.**
- **Viewport input**: receives events via `e.UntransformedArgs` (virtual screen coords) + `PositionToGame` - no GUI-coord dependency. **No change needed.**
- **Mouse cursor sprite**: on the `Cursor` layer (no transformation), in virtual screen coords. **No change needed.**
- **Interface.Layout()**: elements center within the 640x480 space, which maps to the centered GUI area. **No change needed.**
- **ScreenToView**: with matching aspect ratio, `virtualScreenDisplay` = full screen, `sizeFactorX/Y` = virtual/physical -> correct input mapping.

## Changes

### 1. `FreeserfNet.Android\MainActivity.cs` - widescreen virtual screen size
Replace the hardcoded `new Size(initInfo.ScreenWidth, initInfo.ScreenHeight)` with a size computed from the actual view, preserving aspect ratio, capped at MAX_VIRTUAL_SCREEN:
```csharp
int screenW = view.Size.X, screenH = view.Size.Y;
if (screenH > screenW) { int t = screenW; screenW = screenH; screenH = t; } // ensure landscape
int virtualWidth = Math.Min(screenW, Global.MAX_VIRTUAL_SCREEN_WIDTH);
int virtualHeight = Math.Max(1, (int)Math.Round(virtualWidth * (double)screenH / screenW));
gameView = new GameView(dataSource, new Size(virtualWidth, virtualHeight), ...);
```
Pixel 8a -> 1920x864. Works for any device aspect ratio (worst case 4:3 -> 1920x1440, exactly at the cap).

### 2. `FreeserfNet\GameView.cs` - uniform + centered GUI transformation
In the layer-creation loop, replace the per-axis `factorX/factorY` for the `Gui`, `GuiBuildings`, `Minimap` layers (and the `GuiFont` layer) with a single uniform scale + center offset:
```csharp
float scale = Math.Min((float)VirtualScreen.Size.Width / 640.0f, (float)VirtualScreen.Size.Height / 480.0f);
int offsetX = Misc.Round((VirtualScreen.Size.Width - 640.0f * scale) / 2.0f);
int offsetY = Misc.Round((VirtualScreen.Size.Height - 480.0f * scale) / 2.0f);
```
- `PositionTransformation`: `(position.X * scale + offsetX, position.Y * scale + offsetY)`
- `SizeTransformation`: `(size.Width * scale, size.Height * scale)` (keep the 0-dimension guard)
- `GuiFont` SizeTransformation: multiply scale by `8.0f/Global.UIFontCharacterWidth` and `8.0f/Global.UIFontCharacterHeight` (keep existing font adjustment).

### 3. `Freeserf.Core\UI\Gui.cs` - inverse input transformation
Update `PositionToGui` and `DeltaToGui` to the inverse of the uniform + centered transformation (same scale/offset formula):
- `PositionToGui`: `((position.X - offsetX) / scale, (position.Y - offsetY) / scale)` with `Math.Floor` (keep existing behavior).
- `DeltaToGui`: `(delta.Width / scale, delta.Height / scale)`.

## Verification

1. Clean rebuild (delete `FreeserfNet.Android\bin` + `obj`, then `dotnet build FreeserfNet.Android\FreeserfNet.Android.csproj -c Release -m:1 -nodeReuse:false -p:PublishTrimmed=false -p:RunAOTCompilation=false`).
2. Deploy to Pixel 8a (`adb install -r`), wait ~25-30s after `am start`.
3. Screenshot -> verify: no black bars, map fills full width, GUI (PanelBar, GameInitBox) undistorted and centered. Analyze via qwen3.6-27b:coding sub-agent (standing instruction).
4. Input test: tap map (tile selection works), tap GUI elements (PanelBar buttons, dialog buttons work), drag to scroll map.
5. Cursor alignment: map cursor aligns with tiles.

## Notes / considerations

- **MAX_VIRTUAL_SCREEN_WIDTH stays 1920** for now (1920x864 fits). Optional follow-up: raise to 2400 for native-resolution map rendering on the Pixel 8a (crisper tiles, more GPU work). Same on-screen GUI size either way.
- **GUI on-screen size is unchanged** (2.25x design size in all cases) - only the map area widens.
- `NuGet.config` stays untracked/excluded.
- Update `Android.md` with the widescreen changes; commit + push to `origin/android`.
