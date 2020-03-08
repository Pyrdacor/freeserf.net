using System;
using ManagedBass;

namespace Freeserf.Audio.Bass
{
    using BassImpl = ManagedBass.Bass;

    internal abstract class Music : Audio.ITrack, IDisposable
    {
        int channel;
        bool disposed = false;

        internal Music(byte[] data, BassFlags flags, int frequency)
        {
            channel = BassImpl.MusicLoad(data, 0L, data.Length, flags, frequency);

            if (channel == 0)
                Log.Warn.Write(ErrorSystemType.Audio, $"Failed to load music from data: {BassImpl.LastError}");
        }

        public void Play(Audio.Player player)
        {
            if (!disposed && channel != 0 && BassLib.Initialized)
            {
                BassImpl.ChannelPlay(channel);
                BassImpl.Start();
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
