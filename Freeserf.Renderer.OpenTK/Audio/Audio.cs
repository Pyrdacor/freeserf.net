using System;
using System.Collections.Generic;
using System.Text;
using OpenAL;

namespace Freeserf.Renderer.OpenTK.Audio
{
    public class Audio : Freeserf.Audio
    {
        private new class Track : Freeserf.Audio.Track, IDisposable
        {
            Player player = null;
            uint bufferIndex = 0;
            int index = -1;
            Buffer data = null;
            bool disposed = false;

            public Track(Player player, int index, short[] data)
            {
                this.player = player;
                this.index = index;

                AL10.alGenBuffers(1, out bufferIndex);
                AL10.alBufferData(bufferIndex, AL10.AL_FORMAT_MONO16, data, data.Length * 2, 44100);
            }

            public override void Play()
            {
                AL10.alSourceQueueBuffers(player.Source, 1, ref bufferIndex);
                AL10.alSourcePlay(player.Source);
            }


            #region IDisposable Support

            protected virtual void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (bufferIndex != 0)
                    {
                        AL10.alDeleteBuffers(1, ref bufferIndex);
                        bufferIndex = 0;
                    }

                    disposed = true;
                }
            }

            ~Track()
            {

               Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            #endregion

        }

        private new class Player : Freeserf.Audio.Player
        {
            readonly VolumeController volumeController = null;
            readonly DataSource dataSource = null;
            uint index = uint.MaxValue;
            int format = -1;
            bool playing = false;
            bool music = false;

            public uint Source { get; } = uint.MaxValue;

            public Player(uint source, VolumeController volumeController, DataSource dataSource, bool music)
            {
                Source = source;

                AL10.alGenBuffers(1, out index);

                if (AL10.alGetError() != AL10.AL_NO_ERROR)
                {
                    throw new ExceptionAudio("Unable to create sound buffer.");
                }

                this.volumeController = volumeController;
                this.dataSource = dataSource;
                this.music = music;
            }

            public override void Enable(bool enable)
            {
                if (enabled == enable)
                    return;

                enabled = enable;

                if (playing && !enabled)
                    Stop();
            }

            public override VolumeController GetVolumeController()
            {
                return volumeController;
            }

            protected override Freeserf.Audio.Track CreateTrack(int trackID)
            {
                short[] pcmData;

                if (music)
                {
                    XMIConverter converter = new XMIConverter();

                    pcmData = converter.ConvertToPCM(dataSource.GetMusic((uint)trackID));
                }
                else
                {
                    // TODO
                    pcmData = null; // dataSource.GetSound((uint)trackID);
                }

                return new Track(this, trackID, pcmData);
            }

            protected override void Stop()
            {
                AL10.alSourceStop(Source);
                AL10.alGetSourcei(Source, AL10.AL_BUFFERS_PROCESSED, out int numActiveBuffers);

                if (numActiveBuffers > 0)
                    AL10.alSourceUnqueueBuffers(Source, numActiveBuffers, null);
            }
        }

        IntPtr device = IntPtr.Zero;
        IntPtr context = IntPtr.Zero;
        Player musicPlayer = null;
        Player soundPlayer = null;
        VolumeController volumeController = null;

        internal Audio()
        {
            device = ALC10.alcOpenDevice(null);

            if (device == null || device == IntPtr.Zero)
            {
                DisableSound();
                return;
            }

            context = ALC10.alcCreateContext(device, null);

            ALC10.alcMakeContextCurrent(context);

            if (context == null || context == IntPtr.Zero)
            {
                DisableSound();
                return;
            }
        }

        void DisableSound()
        {
            musicPlayer = null;
            soundPlayer = null;
            volumeController = null;

            Log.Info.Write("audio", "No audio device available. Sound is deactivated.");
        }

        public override Freeserf.Audio.Player GetMusicPlayer()
        {
            return musicPlayer;
        }

        public override Freeserf.Audio.Player GetSoundPlayer()
        {
            return soundPlayer;
        }

        public override VolumeController GetVolumeController()
        {
            return volumeController;
        }
    }

    public class AudioFactory : IAudioFactory
    {
        static Audio audio = null;

        public Freeserf.Audio GetAudio()
        {
            if (audio == null)
                audio = new Audio();

            return audio;
        }
    }
}
