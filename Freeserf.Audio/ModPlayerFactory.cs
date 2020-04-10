using Freeserf.Data;

namespace Freeserf.Audio
{
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
                player = new Bass.ModPlayer(dataSource);

            return player;
        }
    }
}
