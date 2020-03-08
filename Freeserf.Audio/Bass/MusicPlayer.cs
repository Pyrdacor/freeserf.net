using System;
using Freeserf.Data;

namespace Freeserf.Audio.Bass
{
    internal abstract class MusicPlayer : Audio.Player, Audio.IVolumeController
    {
        protected DataSource dataSource = null;

        public MusicPlayer(DataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        public override bool Enabled
        {
            get;
            set;
        } = true;

        public float Volume
        {
            get => (float)ManagedBass.Bass.Volume;
            set => ManagedBass.Bass.Volume = value;
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
            ManagedBass.Bass.Stop();
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
