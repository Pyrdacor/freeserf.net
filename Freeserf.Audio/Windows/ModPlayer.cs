using System;
using Freeserf.Audio;
using Freeserf.Data;

namespace Freeserf.Audio.Windows
{
#if WINDOWS
    internal class ModPlayer : WavePlayer, IModPlayer
    {
        public ModPlayer(DataSource dataSource)
            : base(dataSource)
        {

        }

        public bool Paused => throw new NotImplementedException();

        public bool Looped => throw new NotImplementedException();

        public void Play(MOD mod, bool looped)
        {
            throw new NotImplementedException();
        }

        protected override Audio.ITrack CreateTrack(int trackID)
        {
            return new WaveTrack(new MOD(dataSource.GetMusic((uint)trackID)).ConvertToWav());
        }
    }
#endif // WINDOWS
}
