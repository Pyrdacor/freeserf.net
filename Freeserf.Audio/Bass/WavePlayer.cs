using Freeserf.Data;

namespace Freeserf.Audio.Bass
{
    internal class WavePlayer : MusicPlayer
    {
        public WavePlayer(DataSource dataSource)
            : base(dataSource)
        {

        }

        unsafe protected override Audio.ITrack CreateTrack(int trackID)
        {
            int level = DataSource.DosSounds(dataSource) ? -32 : 0;
            short[] data = SFX.ConvertToWav(dataSource.GetSound((uint)trackID), level);
            byte[] byteData = new byte[data.Length * sizeof(short)];
            System.Buffer.BlockCopy(data, 0, byteData, 0, byteData.Length);

            return new WaveMusic(byteData);
        }
    }
}
