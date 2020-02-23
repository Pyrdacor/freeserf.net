using Freeserf.Data;

namespace Freeserf.Audio
{
    internal class AudioImpl : Audio, Audio.IVolumeController
    {
        Player musicPlayer = null;
        Player soundPlayer = null;

        internal AudioImpl(DataSource dataSource)
        {
            try
            {
                IMidiPlayerFactory midiPlayerFactory;
                IWavePlayerFactory wavePlayerFactory;
                IModPlayerFactory modPlayerFactory;

#if WINDOWS
                midiPlayerFactory = new Windows.WindowsMidiPlayerFactory(dataSource);
                wavePlayerFactory = new Windows.WindowsWavePlayerFactory(dataSource);
                modPlayerFactory = new Windows.WindowsModPlayerFactory(dataSource);
#else
                throw new ExceptionAudio("Unsupported platform.");
                // TODO: other platforms
#endif

                musicPlayer = DataSource.DosMusic(dataSource) ? midiPlayerFactory?.GetMidiPlayer() as Audio.Player : modPlayerFactory?.GetModPlayer() as Audio.Player;
                soundPlayer = wavePlayerFactory?.GetWavePlayer() as Audio.Player;
            }
            catch
            {
                DisableSound();
            }
        }

        void DisableSound()
        {
            musicPlayer = null;
            soundPlayer = null;

            Log.Info.Write(ErrorSystemType.Audio, "No audio device available. Sound is deactivated.");
        }

        public override Audio.Player GetMusicPlayer()
        {
            return musicPlayer;
        }

        public override Audio.Player GetSoundPlayer()
        {
            return soundPlayer;
        }

        public override IVolumeController GetVolumeController()
        {
            return this;
        }

        public float Volume
        {
            get
            {
                if (musicPlayer?.GetVolumeController() != null)
                    return musicPlayer.GetVolumeController().Volume;

                if (soundPlayer?.GetVolumeController() != null)
                    return soundPlayer.GetVolumeController().Volume;

                return 0.0f;
            }
        }

        public void SetVolume(float volume)
        {
            if (musicPlayer != null)
                musicPlayer.GetVolumeController()?.SetVolume(volume);

            if (soundPlayer != null)
                soundPlayer.GetVolumeController()?.SetVolume(volume);
        }

        public void VolumeUp()
        {
            SetVolume(Volume + 0.1f);
        }

        public void VolumeDown()
        {
            SetVolume(Volume - 0.1f);
        }
    }

    public class AudioFactory : IAudioFactory
    {
        AudioImpl audio = null;
        DataSource dataSource = null;

        public AudioFactory(DataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        public Audio GetAudio()
        {
            if (audio == null)
                audio = new AudioImpl(dataSource);

            return audio;
        }
    }
}
