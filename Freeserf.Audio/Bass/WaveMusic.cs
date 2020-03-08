using ManagedBass;

namespace Freeserf.Audio.Bass
{
    internal class WaveMusic : Music
    {
        internal WaveMusic(byte[] data)
            : base(data, BassFlags.Default | BassFlags.Loop, 44100)
        {

        }
    }
}
