using System;
using Freeserf.Data;

namespace Freeserf.Audio
{
    internal class AudioImpl : Audio, Audio.IVolumeController, IDisposable
    {
        Player musicPlayer = null;
        Player soundPlayer = null;

        static AudioImpl()
        {
#if !WINDOWS || !USE_WINMM
            // Init Bass if is is used
            Bass.BassLib.EnsureBass();
#endif
        }

        internal AudioImpl(DataSource dataSource)
        {
            try
            {
                var midiPlayerFactory = new MidiPlayerFactory(dataSource);
                var wavePlayerFactory = new WavePlayerFactory(dataSource);
                var modPlayerFactory = new ModPlayerFactory(dataSource);

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
            if (musicPlayer == null && soundPlayer == null)
                return null;

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

        public void Dispose()
        {
#if !WINDOWS || !USE_WINMM
            // Free Bass resources if is is used
            Bass.BassLib.FreeBass();
#endif
        }

        internal static Data.Buffer GetMusicTrackData(DataSource dataSource, int trackID)
        {
            var musicData = dataSource.GetMusic((uint)trackID);

            if (musicData == null)
                throw new ExceptionFreeserf(ErrorSystemType.Data, $"Error loading music track {trackID}");

            return musicData;
        }

        internal static Data.Buffer GetSoundTrackData(DataSource dataSource, int trackID)
        {
            var soundData = dataSource.GetSound((uint)trackID);

            if (soundData == null)
                throw new ExceptionFreeserf(ErrorSystemType.Data, $"Error loading sound track {trackID}");

            return soundData;
        }
    }

    public class AudioFactory : IAudioFactory
    {
        AudioImpl audio = null;
        readonly DataSource dataSource = null;

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
