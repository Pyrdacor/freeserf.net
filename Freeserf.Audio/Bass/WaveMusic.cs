using ManagedBass;

namespace Freeserf.Audio.Bass
{
    internal class WaveMusic : Music
    {
        internal WaveMusic(byte[] data)
            :  base(data, Type.Wave)
        {

        }
    }
}
