using System;
using Freeserf.Data;

namespace Freeserf.Audio.Bass
{
    internal abstract class MusicPlayer : Audio.Player, Audio.IVolumeController
    {
        protected DataSource dataSource = null;
        bool enabled = true;
        int currentChannel = 0;

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
            get
            {
                if (currentChannel == 0)
                    return (float)ManagedBass.Bass.Volume;
                else
                    return (float)ManagedBass.Bass.ChannelGetAttribute(currentChannel, ManagedBass.ChannelAttribute.Volume);
            }
            set
            {
                if (currentChannel == 0)
                    ManagedBass.Bass.Volume = value;
                else
                    ManagedBass.Bass.ChannelSetAttribute(currentChannel, ManagedBass.ChannelAttribute.Volume, value);
            }
        }

        public override Audio.IVolumeController GetVolumeController()
        {
            return this;
        }

        public void SetVolume(float volume)
        {
            Volume = Math.Max(0.0f, Math.Min(1.0f, volume));
        }

        public override void Stop()
        {
            if (enabled && currentChannel != 0)
                ManagedBass.Bass.ChannelStop(currentChannel);
        }

        internal void SetCurrentChannel(int channel)
        {
            currentChannel = channel;
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
