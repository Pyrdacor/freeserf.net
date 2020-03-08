namespace Freeserf.Audio.Bass
{
    internal static class BassLib
    {
        public static bool Initialized { get; private set; } = false;

        public static void EnsureBass()
        {
            if (!Initialized)
                Initialized = ManagedBass.Bass.Init();
        }

        public static void FreeBass()
        {
            if (Initialized)
                ManagedBass.Bass.Free();
        }
    }
}
