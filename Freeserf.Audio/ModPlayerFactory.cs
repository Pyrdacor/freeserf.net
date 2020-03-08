using Freeserf.Data;

namespace Freeserf.Audio
{
#if WINDOWS
    #if USE_WINMM
        using Audiolib = Windows;
    #else
        using Audiolib = Bass;
    #endif
#else
    using Audiolib = Bass;
#endif

    internal class ModPlayerFactory
    {
        public ModPlayerFactory(DataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        readonly DataSource dataSource = null;
        Audio.Player player = null;

        public Audio.Player GetModPlayer()
        {
            if (player == null)
                player = new Audiolib.ModPlayer(dataSource);

            return player;
        }
    }
}
