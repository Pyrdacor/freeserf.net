
namespace Freeserf.Audio.Bass
{
    internal class WaveMusic : Music
    {
        internal WaveMusic(byte[] data)
            : base(data, Type.Sfx)
        {

        }


        protected override void FreeMusic(int channel)
        {
            BassLib.FreeSfxMusic(channel);
        }
    }
}
