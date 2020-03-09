using System;
using Freeserf.Data;

namespace Freeserf.Audio.Bass
{
    internal abstract class MusicPlayer : Audio.Player, Audio.IVolumeController
    {
        protected DataSource dataSource = null;
        bool enabled = true;
        int currentChannel = 0;
        float volume = 1.0f;

        public MusicPlayer(DataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        public override bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled == value)
                    return;

                enabled = value;

                if (enabled && currentChannel != 0)
                {
                    ManagedBass.Bass.ChannelPlay(currentChannel, true);
                    ManagedBass.Bass.Start();
                }
                else if (!enabled)
                    ManagedBass.Bass.ChannelStop(currentChannel);
            }
        }

        public float Volume
        {
            get => volume;
            set
            {
                volume = Misc.Clamp(0.0f, value, 1.0f);

                if (currentChannel != 0)
                    ManagedBass.Bass.ChannelSetAttribute(currentChannel, ManagedBass.ChannelAttribute.Volume, volume);
            }
        }

        public override Audio.IVolumeController GetVolumeController()
        {
            return this;
        }

        public void SetVolume(float volume)
        {
            Volume = volume;
        }

        public override void Stop()
        {
            if (enabled && currentChannel != 0)
                ManagedBass.Bass.ChannelStop(currentChannel);
        }

        internal void SetCurrentChannel(int channel)
        {
            currentChannel = channel;

            if (currentChannel != 0) // use set volume for the new channel
                ManagedBass.Bass.ChannelSetAttribute(currentChannel, ManagedBass.ChannelAttribute.Volume, Volume);
        }

        public void VolumeDown()
        {
            SetVolume(Volume - 0.1f);
        }

        public void VolumeUp()
        {
            SetVolume(Volume + 0.1f);
        }
    }
}
