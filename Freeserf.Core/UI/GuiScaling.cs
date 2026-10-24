using System;

namespace Freeserf
{
    /// <summary>
    /// Central place for the mapping between GUI space and virtual screen space.
    /// Without touch mode the 640x480 GUI is scaled uniformly to fit and centered.
    /// In touch mode the GUI is scaled up and its logical size follows the screen.
    /// </summary>
    public readonly struct GuiScaling
    {
        public const int DesignWidth = 640;
        public const int DesignHeight = 480;

        // Logical GUI height in touch mode. Fits a popup (160) plus the panel bar (40).
        const float TouchLogicalHeight = 208.0f;
        // Smallest logical width in touch mode (game init box is 352 wide).
        const float TouchMinLogicalWidth = 360.0f;

        // Set once by the platform (Android) before the game view is created.
        public static bool TouchMode { get; set; } = false;

        public float Scale { get; }
        public int OffsetX { get; }
        public int OffsetY { get; }
        public int LogicalWidth { get; }
        public int LogicalHeight { get; }

        public GuiScaling(Size virtualScreen, bool touchMode)
        {
            float width = virtualScreen.Width;
            float height = virtualScreen.Height;
            float fitScale = Math.Min(width / DesignWidth, height / DesignHeight);

            if (!touchMode)
            {
                Scale = fitScale;
                LogicalWidth = DesignWidth;
                LogicalHeight = DesignHeight;
            }
            else
            {
                float scale = Math.Min(height / TouchLogicalHeight, width / TouchMinLogicalWidth);
                Scale = Math.Max(scale, fitScale);
                LogicalWidth = Math.Max(1, (int)(width / Scale));
                LogicalHeight = Math.Max(1, (int)(height / Scale));
            }

            OffsetX = Misc.Round((width - LogicalWidth * Scale) / 2.0f);
            OffsetY = Misc.Round((height - LogicalHeight * Scale) / 2.0f);
        }

        public static GuiScaling For(Size virtualScreen) => new GuiScaling(virtualScreen, TouchMode);

        public Position ToGui(Position position)
        {
            return new Position((int)Math.Floor((position.X - OffsetX) / Scale), (int)Math.Floor((position.Y - OffsetY) / Scale));
        }

        public Size DeltaToGui(Size delta)
        {
            return new Size(Misc.Round(delta.Width / Scale), Misc.Round(delta.Height / Scale));
        }
    }

    // The gui scaling for one player's part of the screen in split-screen mode.
    // In touch mode the gui uses the touch scaling (larger logical size, uniform
    // scale). Otherwise the gui is scaled non-uniformly to fill the player's part
    // of the screen (like the History Edition).
    public readonly struct SplitScreenGuiScaling
    {
        public float ScaleX { get; }
        public float ScaleY { get; }
        public int OffsetX { get; }
        public int OffsetY { get; }
        public int LogicalWidth { get; }
        public int LogicalHeight { get; }

        public SplitScreenGuiScaling(Size playerScreen)
        {
            if (GuiScaling.TouchMode)
            {
                var guiScaling = GuiScaling.For(playerScreen);
                ScaleX = guiScaling.Scale;
                ScaleY = guiScaling.Scale;
                OffsetX = guiScaling.OffsetX;
                OffsetY = guiScaling.OffsetY;
                LogicalWidth = guiScaling.LogicalWidth;
                LogicalHeight = guiScaling.LogicalHeight;
            }
            else
            {
                // Scale the gui uniformly to fit the player's part of the screen
                // (like the normal single-player mode). Non-uniform scaling would
                // distort the gui (e.g. the bottom panel bar). The gui is anchored
                // to the bottom edge so the bottom panel bar stays at the bottom
                // of the player's part of the screen.
                float scale = Math.Min(playerScreen.Width / (float)GuiScaling.DesignWidth,
                    playerScreen.Height / (float)GuiScaling.DesignHeight);
                ScaleX = scale;
                ScaleY = scale;
                OffsetX = Misc.Round((playerScreen.Width - GuiScaling.DesignWidth * scale) / 2.0f);
                OffsetY = Misc.Round(playerScreen.Height - GuiScaling.DesignHeight * scale);
                LogicalWidth = GuiScaling.DesignWidth;
                LogicalHeight = GuiScaling.DesignHeight;
            }
        }

        public Position ToGui(Position position)
        {
            return new Position(Misc.Round((position.X - OffsetX) / ScaleX), Misc.Round((position.Y - OffsetY) / ScaleY));
        }

        public Size DeltaToGui(Size delta)
        {
            return new Size(Misc.Round(delta.Width / ScaleX), Misc.Round(delta.Height / ScaleY));
        }
    }
}
