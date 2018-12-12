using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Renderer.OpenTK.Audio
{
    public class Audio : Freeserf.Audio, Freeserf.Audio.IVolumeController
    {
        Player musicPlayer = null;
        Player soundPlayer = null;

        internal Audio(DataSource dataSource)
        {
            try
            {
                IMidiPlayerFactory midiPlayerFactory;
                IWavePlayerFactory wavePlayerFactory;

#if WINDOWS
                midiPlayerFactory = new Windows.WindowsMidiPlayerFactory(dataSource);
                wavePlayerFactory = new Windows.WindowsWavePlayerFactory(dataSource);
#else
                throw new ExceptionAudio("Unsupported platform.");
                // TODO: other platforms
#endif

                musicPlayer = midiPlayerFactory?.GetMidiPlayer() as Audio.Player;
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

            Log.Info.Write("audio", "No audio device available. Sound is deactivated.");
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

        public float GetVolume()
        {
            if (musicPlayer?.GetVolumeController() != null)
                return musicPlayer.GetVolumeController().GetVolume();

            if (soundPlayer?.GetVolumeController() != null)
                return soundPlayer.GetVolumeController().GetVolume();

            return 0.0f;
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
            SetVolume(GetVolume() + 0.1f);
        }

        public void VolumeDown()
        {
            SetVolume(GetVolume() - 0.1f);
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

        public Freeserf.Audio GetAudio()
        {
            if (audio == null)
                audio = new Audio(dataSource);

            return audio;
        }
    }
}
