using ManagedBass;

namespace Freeserf.Audio.Bass
{
    internal class ModMusic : Music
    {
        static ModMusic()
        {
            BassLib.EnsureBass();
        }

        internal ModMusic(byte[] data)
            : base(data, BassFlags.MusicPT1Mod, 8000)
        {

        }
    }
}
