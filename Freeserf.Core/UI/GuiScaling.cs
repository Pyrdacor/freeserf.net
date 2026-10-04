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
}
