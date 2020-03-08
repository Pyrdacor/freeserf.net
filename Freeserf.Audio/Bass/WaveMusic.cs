using ManagedBass;

namespace Freeserf.Audio.Bass
{
    internal class WaveMusic : Music
    {
        internal WaveMusic(byte[] data)
            : base(data, BassFlags.Default, 44100)
        {

        }
    }
}
