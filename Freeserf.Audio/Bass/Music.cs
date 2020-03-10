using System;
using ManagedBass;

namespace Freeserf.Audio.Bass
{
    using BassImpl = ManagedBass.Bass;

    internal abstract class Music : Audio.ITrack, IDisposable
    {
        public enum Type
        {
            Midi,
            Mod,
            Wave
        }

        int channel = 0;
        bool disposed = false;

        internal Music(byte[] data, Type type)
        {
            switch (type)
            {
                case Type.Midi:
                    // TODO
                    break;
                case Type.Mod:
                    channel = BassImpl.MusicLoad(data, 0L, data.Length, BassFlags.Default | BassFlags.MusicPT1Mod, 44100);
                    break;
                case Type.Wave:
                    channel = BassImpl.CreateStream(8000, 1, BassFlags.Default | BassFlags.Mono,
                        new WaveMusic.WaveStreamProvider(data).StreamProcedure);
                    break;
            }

            if (channel == 0)
                Log.Warn.Write(ErrorSystemType.Audio, $"Failed to load music from data: {BassImpl.LastError}");
        }

        public void Play(Audio.Player player)
        {
            if (!disposed && channel != 0 && BassLib.Initialized)
            {
                (player as MusicPlayer).SetCurrentChannel(channel);

                if (player.Enabled)
                    BassImpl.ChannelPlay(channel, true);
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (channel != 0)
                {
                    BassImpl.MusicFree(channel);
                    channel = 0;
                }

                disposed = true;
            }
        }
    }
}
