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
            var soundData = dataSource.GetSound((uint)trackID);

            if (trackID == 62 && DataSource.DosSounds(dataSource))
            {
                // The smelter clip is the only one not centred on 32: its samples
                // lie in 0..15 around 8, followed by silence padding at 32. Played
                // as is, the offset and the jump to the padding give a loud click on
                // every repeat. Centre it on 8 and drop the padding.
                level = -8;
                uint size = soundData.Size;
                while (size > 0 && soundData.PeekByte(size - 1) == 32)
                {
                    --size;
                }
                soundData = soundData.GetSubBuffer(0, size);
            }

            short[] data = SFX.ConvertToWav(soundData, level);
            byte[] byteData = new byte[data.Length * sizeof(short)];
            System.Buffer.BlockCopy(data, 0, byteData, 0, byteData.Length);

            return new WaveMusic(byteData);
        }
    }
}
