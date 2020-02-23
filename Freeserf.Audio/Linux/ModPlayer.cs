using Freeserf.Data;

namespace Freeserf.Audio.Linux
{
    internal class ModPlayer : IModPlayer
    {
        public ModPlayer(DataSource dataSource)
        {
            // TODO: implement
        }

        public bool Available => false;

        public bool Enabled { get => false; set { /* TODO: implement */ } }

        public bool Paused => false;

        public bool Running => false;

        public bool Looped => false;

        public void Pause()
        {
            // TODO: implement
        }

        public void Play(MOD mod, bool looped)
        {
            // TODO: implement
        }

        public void Resume()
        {
            // TODO: implement
        }

        public void Stop()
        {
            // TODO: implement
        }
    }
}
