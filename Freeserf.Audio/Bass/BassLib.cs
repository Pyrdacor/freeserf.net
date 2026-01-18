using ManagedBass;
using ManagedBass.Midi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using MBass = ManagedBass.Bass;

namespace Freeserf.Audio.Bass
{
    internal static class BassLib
    {
        public static bool Initialized { get; private set; } = false;
        private const string SoundFontResource = "Freeserf.Audio.Bass.ChoriumRevA.SF2";
        private static int soundFont = 0;
        private static Dictionary<int, Music.Type> createdChannels = new Dictionary<int, Music.Type>();
        private static Dictionary<int, StreamProcedure> streamProviders = new Dictionary<int, StreamProcedure>();
        private static FileProcedures soundFontProcs = null;

        public static void EnsureBass()
        {
            if (!Initialized)
            {
                Initialized = MBass.Init(-1, 44100, 0u, 0, IntPtr.Zero);
            }
        }

        public static void FreeBass()
        {
            if (Initialized)
            {
                foreach (var createdChannel in createdChannels.ToList())
                {
                    switch (createdChannel.Value)
                    {
                        case Music.Type.Mod:
                            FreeModMusic(createdChannel.Key);
                            break;
                        case Music.Type.Sfx:
                            FreeSfxMusic(createdChannel.Key);
                            break;
                        case Music.Type.Midi:
                            FreeMidiMusic(createdChannel.Key);
                            break;
                    }
                }

                streamProviders.Clear();

                MBass.Free();
            }
        }

        public static int LoadModMusic(byte[] data)
        {
            const BassFlags MusicPT1Mod = BassFlags.MusicPT1Mod;

            int music = MBass.MusicLoad(
                Memory: data,
                Offset: 0,
                Length: data.Length,
                Flags: MusicPT1Mod,
                Frequency: 44100
            );

            createdChannels.Add(music, Music.Type.Mod);
            return music;
        }

        public static int LoadSfxMusic(byte[] data)
        {
            // Your original flag 0x0002u = BASS_SAMPLE_MONO
            const BassFlags Mono = BassFlags.Mono;

            var streamProvider = new WaveStreamProvider(data);

            // ManagedBass uses the same delegate type
            StreamProcedure streamProc = streamProvider.StreamProcedure;

            int stream = MBass.CreateStream(
                Frequency: 8000,
                Channels: 1,
                Flags: Mono,
                Procedure: streamProc,
                User: IntPtr.Zero
            );

            streamProviders.Add(stream, streamProc);
            createdChannels.Add(stream, Music.Type.Sfx);

            return stream;
        }

        public static int LoadMidiMusic(ManagedBass.Midi.MidiEvent[] events, int pulsesPerQuarterNote, uint frequency)
        {
            // Initialize SoundFont once
            if (soundFont == 0)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream(SoundFontResource); // do NOT dispose

                soundFontProcs = new FileProcedures
                {
                    Close = user => { },
                    Length = user => stream.Length,
                    Read = (IntPtr buffer, int length, IntPtr user) =>
                    {
                        unsafe
                        {
                            var span = new Span<byte>((void*)buffer, length);
                            return stream.Read(span);
                        }
                    },
                    Seek = (offset, user) =>
                    {
                        stream.Seek((long)offset, SeekOrigin.Begin);
                        return true;
                    }
                };

                soundFont = ManagedBass.Midi.BassMidi.FontInit(soundFontProcs, IntPtr.Zero, 0);
            }

            // Loop flag for MIDI event streams
    

            // Create the MIDI stream from events
            int music = ManagedBass.Midi.BassMidi.CreateStream(
                events,
                pulsesPerQuarterNote, 
                BassFlags.Loop,
                (int)frequency
            );

            // Assign the SoundFont
            var fonts = new ManagedBass.Midi.MidiFont[]
            {
                new ManagedBass.Midi.MidiFont
                {
                    Handle = soundFont,
                    Preset = -1,
                    Bank = 0
                }
            };

            ManagedBass.Midi.BassMidi.StreamSetFonts(music, fonts, fonts.Length);

            return music;
        }

        public static void FreeModMusic(int music)
        {
            createdChannels.Remove(music);
            MBass.MusicFree(music);
        }


        public static void FreeSfxMusic(int music)
        {
            createdChannels.Remove(music);
            MBass.StreamFree(music);
        }

        public static void FreeMidiMusic(int music)
        {
            createdChannels.Remove(music);
            MBass.StreamFree(music);
        }

        public static void StartAll()
        {
            MBass.Start();
        }

        public static void StopAll()
        {
            MBass.Stop();
        }

        public static void PauseAll()
        {
            MBass.Pause();
        }

        public static void Play(int music, bool restart)
        {
            MBass.ChannelPlay(music, restart);
        }

        public static void Stop(int music)
        {
            MBass.ChannelStop(music);
        }

        public static void Pause(int music)
        {
            MBass.ChannelPause(music);
        }


        public static void SetVolume(int music, float volume)
        {
            MBass.ChannelSetAttribute(music, ChannelAttribute.Volume, volume);
        }

        public static string LastError => MBass.LastError.ToString();


        private class WaveStreamProvider
        {
            const int MAX_BUFFER_SIZE = 4096;
            const int STREAM_END = -2147483648;
            byte[] data;
            int offset = 0;

            public WaveStreamProvider(byte[] data)
            {
                this.data = data;
            }

            public int StreamProcedure(int handle, IntPtr buffer, int length, IntPtr user)
            {
                if (length > data.Length - offset)
                    length = data.Length - offset;

                if (length == 0)
                {
                    offset = 0;
                    return STREAM_END;
                }

                length = Math.Min(length, MAX_BUFFER_SIZE);
                Marshal.Copy(data, offset, buffer, length);
                offset += length;

                return length;
            }
        }  
    }
}
