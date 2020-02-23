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

    internal class WavePlayerFactory : IWavePlayerFactory
    {
        public WavePlayerFactory(DataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        readonly DataSource dataSource = null;
        IWavePlayer player = null;

        public IWavePlayer GetWavePlayer()
        {
            if (player == null)
                player = new os.WavePlayer(dataSource);

            return player;
        }
    }
}
