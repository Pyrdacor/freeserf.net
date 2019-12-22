using System;

namespace Silk.NET.Window
{
    [Flags]
    public enum KeyModifiers
    {
        Shift = 1 << 0,
        Control = 1 << 1,
        Alt = 1 << 2,
    }
}
