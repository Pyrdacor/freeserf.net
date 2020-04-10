using Freeserf.Data;

namespace Freeserf.Audio
{
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
                player = new Bass.MidiPlayer(dataSource);

            return player;
        }
    }
}
