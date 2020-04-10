using Freeserf.Data;

namespace Freeserf.Audio
{
    internal class WavePlayerFactory
    {
        public WavePlayerFactory(DataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        readonly DataSource dataSource = null;
        Audio.Player player = null;

        public Audio.Player GetWavePlayer()
        {
            if (player == null)
                player = new Bass.WavePlayer(dataSource);

            return player;
        }
    }
}
