using System;

namespace Silk.NET.Window
{
    [Flags]
    public enum MouseButtons
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Middle = 1 << 2,
    }
}
