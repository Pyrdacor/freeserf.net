using System;
using System.Collections.Generic;
using System.Text;
using OpenAL;

namespace Freeserf.Renderer.OpenTK.Audio
{
    public class Audio : Freeserf.Audio
    {
        private class MusicTrack : Track, IDisposable
        {
            Player player = null;
            uint[] bufferIndices = null;
            int index = -1;
            bool disposed = false;

            public MusicTrack(Player player, int index, List<short[]> data)
            {
                this.player = player;
                this.index = index;

                bufferIndices = new uint[data.Count];

                AL10.alGenBuffers(data.Count, bufferIndices);

                for (int i = 0; i < data.Count; ++i)
                    AL10.alBufferData(bufferIndices[i], AL10.AL_FORMAT_MONO16, data[i], data[i].Length * 2, 44100);
            }

            public override void Play()
            {
                for (int i = 0; i < bufferIndices.Length; ++i)
                {
                    AL10.alSourcei(player.Sources[i], AL10.AL_BUFFER, (int)bufferIndices[i]);
                    AL10.alSourcePlay(player.Sources[i]);
                }
            }


            #region IDisposable Support

            protected virtual void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (bufferIndices != null && bufferIndices.Length != 0)
                    {
                        AL10.alDeleteBuffers(bufferIndices.Length, bufferIndices);
                        bufferIndices = null;
                    }

                    disposed = true;
                }
            }

            ~MusicTrack()
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

        private class SoundTrack : Track, IDisposable
        {
            Player player = null;
            uint bufferIndex = 0;
            int index = -1;
            bool disposed = false;

            public SoundTrack(Player player, int index, short[] data)
            {
                this.player = player;
                this.index = index;

                AL10.alGenBuffers(1, out bufferIndex);
                AL10.alBufferData(bufferIndex, AL10.AL_FORMAT_MONO16, data, data.Length * 2, 44100);
            }

            public override void Play()
            {
                AL10.alSourceQueueBuffers(player.Sources[0], 1, ref bufferIndex);
                AL10.alSourcePlay(player.Sources[0]);
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

            ~SoundTrack()
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
            int format = -1;
            bool playing = false;
            bool music = false;

            public uint[] Sources { get; } = null;

            public Player(VolumeController volumeController, DataSource dataSource, bool music, params uint[] sources)
            {
                Sources = sources;

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

            protected override Track CreateTrack(int trackID)
            {
                List<short[]> pcmData;

                if (music)
                {
                    XMIConverter converter = new XMIConverter();

                    pcmData = converter.ConvertToPCM(dataSource.GetMusic((uint)trackID));

                    return new MusicTrack(this, trackID, pcmData);
                }
                else
                {
                    // TODO
                    return null; // dataSource.GetSound((uint)trackID);
                    //return new SoundTrack(this, trackID, pcmData[0]);
                }
            }

            protected override void Stop()
            {
                foreach (var source in Sources)
                {
                    AL10.alSourceStop(source);
                    AL10.alGetSourcei(source, AL10.AL_BUFFERS_PROCESSED, out int numActiveBuffers);

                    if (numActiveBuffers > 0)
                        AL10.alSourceUnqueueBuffers(source, numActiveBuffers, null);
                }
            }
        }

        IntPtr device = IntPtr.Zero;
        IntPtr context = IntPtr.Zero;
        Player musicPlayer = null;
        Player soundPlayer = null;
        VolumeController volumeController = null;
        uint soundSource = 0;
        uint[] musicSources = new uint[16];

        internal Audio(DataSource dataSource)
        {
            try
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

                AL10.alGenSources(1, out soundSource);

                if (soundSource == 0 || AL10.alGetError() != AL10.AL_NO_ERROR)
                {
                    DisableSound();
                    return;
                }

                AL10.alGenSources(16, musicSources);

                if (musicSources[0] == 0 || AL10.alGetError() != AL10.AL_NO_ERROR)
                {
                    DisableSound();
                    return;
                }

                VolumeController volumeController = null; // TODO

                musicPlayer = new Player(volumeController, dataSource, true, musicSources);
                soundPlayer = new Player(volumeController, dataSource, false, soundSource);
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
