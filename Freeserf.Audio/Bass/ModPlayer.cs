using Freeserf.Data;

namespace Freeserf.Audio.Bass
{
    internal class ModPlayer : MusicPlayer
    {
        public ModPlayer(DataSource dataSource)
            : base(dataSource)
        {

        }

        unsafe protected override Audio.ITrack CreateTrack(int trackID)
        {
            return new ModMusic(dataSource.GetMusic((uint)trackID).Unfix());
        }
    }
}
