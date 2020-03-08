using System;
using Freeserf.Audio;
using Freeserf.Data;

namespace Freeserf.Audio.Windows
{
#if WINDOWS
    internal class ModPlayer : WavePlayer
    {
        public ModPlayer(DataSource dataSource)
            : base(dataSource)
        {

        }

        protected override Audio.ITrack CreateTrack(int trackID)
        {
            return new WaveTrack(new MOD(dataSource.GetMusic((uint)trackID)).ConvertToWav());
        }
    }
#endif // WINDOWS
}
