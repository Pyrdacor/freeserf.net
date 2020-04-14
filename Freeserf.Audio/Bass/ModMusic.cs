namespace Freeserf.Audio.Bass
{
    internal class ModMusic : Music
    {
        internal ModMusic(byte[] data)
            : base(data, Type.Mod)
        {

        }

        protected override void FreeMusic(int channel)
        {
            BassLib.FreeModMusic(channel);
        }
    }
}
