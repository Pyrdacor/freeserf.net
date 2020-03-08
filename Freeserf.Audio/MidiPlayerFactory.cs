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

    internal class MidiPlayerFactory
    {
        public MidiPlayerFactory(DataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        readonly DataSource dataSource = null;
        Audio.Player player = null;

        public Audio.Player GetMidiPlayer()
        {
            if (player == null)
                player = new Audiolib.MidiPlayer(dataSource);

            return player;
        }
    }
}
