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
        }

        public void Play(Audio.Player player)
        {
            if (!disposed)
                BassImpl.ChannelPlay(channel);

            BassImpl.Start();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                BassImpl.MusicFree(channel);
                channel = -1;

                disposed = true;
            }
        }
    }
}
