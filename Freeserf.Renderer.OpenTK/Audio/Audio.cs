using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Renderer.OpenTK.Audio
{
    public class Audio : Freeserf.Audio
    {
        Player musicPlayer = null;
        Player soundPlayer = null;
        IVolumeController volumeController = null;

        internal Audio(DataSource dataSource)
        {
            try
            {
                IMidiPlayerFactory midiPlayerFactory;

#if WINDOWS
                midiPlayerFactory = new Windows.WindowsMidiPlayerFactory(dataSource);
#else
                throw new ExceptionAudio("Unsupported platform.");
                // TODO: other platforms
#endif

                musicPlayer = midiPlayerFactory?.GetMidiPlayer() as Audio.Player;
                
                // TODO: sfx
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
            volumeController = null;

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
            return volumeController;
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
