using System;

namespace Freeserf.Audio.Bass
{
    internal abstract class Music : Audio.ITrack, IDisposable
    {
        public enum Type
        {
            Midi,
            Mod,
            Sfx
        }

        int channel = 0;
        bool disposed = false;

        protected Music(byte[] data, Type type)
        {
            switch (type)
            {
                case Type.Mod:
                    channel = BassLib.LoadModMusic(data);
                    break;
                case Type.Sfx:
                    channel = BassLib.LoadSfxMusic(data);
                    break;
            }

            if (channel == 0)
                Log.Warn.Write(ErrorSystemType.Audio, $"Failed to load music from data: {BassLib.LastError}");
        }

        protected Music(int channel)
        {
            this.channel = channel;
        }

        public void Play(Audio.Player player)
        {
            if (!disposed && channel != 0 && BassLib.Initialized)
            {
                (player as MusicPlayer).SetCurrentChannel(channel);

                if (player.Enabled)
                    BassLib.Play(channel, true);
            }
        }

        protected abstract void FreeMusic(int channel);

        public void Dispose()
        {
            if (!disposed)
            {
                if (channel != 0)
                {
                    FreeMusic(channel);
                    channel = 0;
                }

                disposed = true;
            }
        }
    }
}
