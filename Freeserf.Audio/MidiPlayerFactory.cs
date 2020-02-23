using Freeserf.Data;

namespace Freeserf.Audio
{
#if WINDOWS
    using os = Windows;
#elif LINUX
    using os = Linux;
#else
    #error Unsupported platform.
#endif

    internal class MidiPlayerFactory : IMidiPlayerFactory
    {
        public MidiPlayerFactory(DataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        readonly DataSource dataSource = null;
        IMidiPlayer player = null;

        public IMidiPlayer GetMidiPlayer()
        {
            if (player == null)
                player = new os.MidiPlayer(dataSource);

            return player;
        }
    }
}
