using Freeserf.Data;
using System;

namespace Freeserf.Audio
{
    internal class AudioImpl : Audio, Audio.IVolumeController, IDisposable
    {
        Player musicPlayer = null;
        Player soundPlayer = null;

        static int RefCount = 0;

        internal AudioImpl(DataSource dataSource)
        {
            try
            {
                if (RefCount++ == 0)
                {
                    // Init Bass if it is used
                    Bass.BassLib.EnsureBass();
                }

                // If Bass should be used but it is not initialized, the sound is disabled
                if (!Bass.BassLib.Initialized)
                {
                    DisableSound();
                    return;
                }

                musicPlayer = DataSource.DosMusic(dataSource)
                    ? new MidiPlayerFactory(dataSource).GetMidiPlayer()
                    : new ModPlayerFactory(dataSource).GetModPlayer();
                soundPlayer = new WavePlayerFactory(dataSource).GetWavePlayer();
            }
            catch (Exception ex)
            {
                Log.Debug.Write(ErrorSystemType.Audio, "Unable to create BASS audio: " + ex.Message);
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
            if (--RefCount == 0)
            {
                // Free Bass resources
                Bass.BassLib.FreeBass();
            }
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
        private AudioImpl audio = null;
        private readonly DataSource dataSource = null;

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
