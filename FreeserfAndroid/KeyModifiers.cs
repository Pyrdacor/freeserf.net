using System;

namespace FreeserfAndroid
{
    [Flags]
    public enum KeyModifiers
    {
        Shift = 1 << 0,
        Control = 1 << 1,
        Alt = 1 << 2,
    }
}
