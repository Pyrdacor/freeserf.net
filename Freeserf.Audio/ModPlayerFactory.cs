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

    internal class ModPlayerFactory : IModPlayerFactory
    {
        public ModPlayerFactory(DataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        readonly DataSource dataSource = null;
        IModPlayer player = null;

        public IModPlayer GetModPlayer()
        {
            if (player == null)
                player = new os.ModPlayer(dataSource);

            return player;
        }
    }
}
