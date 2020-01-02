using Freeserf.Data;
using Freeserf.Audio;

namespace Freeserf.Renderer.OpenTK.Audio
{
    public class Audio : Freeserf.Audio.Audio, Freeserf.Audio.Audio.IVolumeController
    {
        Player musicPlayer = null;
        Player soundPlayer = null;

        internal Audio(DataSource dataSource)
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

        public override Freeserf.Audio.Audio.Player GetMusicPlayer()
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
        static Audio audio = null;
        DataSource dataSource = null;

        internal AudioFactory(DataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        public Freeserf.Audio.Audio GetAudio()
        {
            if (audio == null)
                audio = new Audio(dataSource);

            return audio;
        }
    }
}
