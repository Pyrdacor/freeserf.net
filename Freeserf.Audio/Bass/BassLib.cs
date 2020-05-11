using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Freeserf.Audio.Bass
{
    using DWORD = UInt32;
    using QWORD = UInt64;

    internal static class BassLib
    {
        public static bool Initialized { get; private set; } = false;
        private const string SoundFontResource = "Freeserf.Audio.Bass.ChoriumRevA.SF2";
        private static int soundFont = 0;
        private static Dictionary<int, Music.Type> createdChannels = new Dictionary<int, Music.Type>();
        private static Dictionary<int, NativeBass.StreamProcedure> streamProviders = new Dictionary<int, NativeBass.StreamProcedure>();
        private static NativeBass.SoundFontFileProcs soundFontProcs = null;

        public static void EnsureBass()
        {
            if (!Initialized)
            {
                Initialized = NativeBass.BASS_Init(-1, 44100u, 0u, 0, IntPtr.Zero);
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

                NativeBass.BASS_Free();
            }
        }

        public static int LoadModMusic(byte[] data)
        {
            unsafe
            {
                fixed (byte* ptr = data)
                {
                    const DWORD MusicPT1Mod = 0x4000u;
                    int music = NativeBass.BASS_MusicLoad(true, ptr, 0ul, (DWORD)data.Length, MusicPT1Mod, 44100u);
                    createdChannels.Add(music, Music.Type.Mod);
                    return music;
                }
            }
        }

        public static int LoadSfxMusic(byte[] data)
        {
            const DWORD Mono = 0x0002u;
            var streamProvider = new WaveStreamProvider(data);
            NativeBass.StreamProcedure streamProc = streamProvider.StreamProcedure;
            int music = NativeBass.BASS_StreamCreate(8000u, 1u, Mono, streamProc, IntPtr.Zero);
            streamProviders.Add(music, streamProc);
            createdChannels.Add(music, Music.Type.Sfx);
            return music;
        }

        public static int LoadMidiMusic(MidiEvent[] events, int pulsesPerQuarterNode, uint frequency)
        {
            if (soundFont == 0)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream(SoundFontResource); // Don't dispose it!
                soundFontProcs = SoundFontStream.CreateSoundFontFileProcs(stream);
                soundFont = NativeBass.BASS_MIDI_FontInitUser(soundFontProcs, IntPtr.Zero, 0u);
            }

            const DWORD loop = 0x04u;
            int music = NativeBass.BASS_MIDI_StreamCreateEvents(events, (DWORD)pulsesPerQuarterNode, loop, frequency);

            var fonts = new NativeBass.MidiFont[]
            {
                new NativeBass.MidiFont()
                {
                    Font = soundFont,
                    Preset = -1,
                    Bank = 0
                }
            };

            NativeBass.BASS_MIDI_StreamSetFonts(music, fonts, 1u);

            return music;
        }

        public static void FreeModMusic(int music)
        {
            createdChannels.Remove(music);
            NativeBass.BASS_MusicFree(music);
        }

        public static void FreeSfxMusic(int music)
        {
            createdChannels.Remove(music);
            NativeBass.BASS_StreamFree(music);
        }

        public static void FreeMidiMusic(int music)
        {
            createdChannels.Remove(music);
            NativeBass.BASS_StreamFree(music);
        }

        public static void StartAll()
        {
            NativeBass.BASS_Start();
        }

        public static void StopAll()
        {
            NativeBass.BASS_Stop();
        }

        public static void PauseAll()
        {
            NativeBass.BASS_Pause();
        }

        public static void Play(int music, bool restart)
        {
            NativeBass.BASS_ChannelPlay((DWORD)music, restart);
        }

        public static void Stop(int music)
        {
            NativeBass.BASS_ChannelStop((DWORD)music);
        }

        public static void Pause(int music)
        {
            NativeBass.BASS_ChannelPause((DWORD)music);
        }

        public static void SetVolume(int music, float volume)
        {
            const DWORD Volume = 0x02u;
            NativeBass.BASS_ChannelSetAttribute((DWORD)music, Volume, volume);
        }

        public static string LastError => NativeBass.LastError.ToString();

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MidiEvent
        {
            public DWORD Event;
            public DWORD Parameter;
            public DWORD Channel;
            public DWORD Ticks;
            public DWORD Pos;
        }

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

        private class SoundFontStream
        {
            private readonly Stream stream;

            private SoundFontStream(Stream stream)
            {
                this.stream = stream;
            }

            public unsafe static NativeBass.SoundFontFileProcs CreateSoundFontFileProcs(Stream stream)
            {
                var soundFontStream = new SoundFontStream(stream);

                return new NativeBass.SoundFontFileProcs()
                {
                    Close = new NativeBass.SoundFontCloseProcedure(soundFontStream.Close),
                    Length = new NativeBass.SoundFontLengthProcedure(soundFontStream.Length),
                    Read = new NativeBass.SoundFontReadProcedure(soundFontStream.Read),
                    Seek = new NativeBass.SoundFontSeekProcedure(soundFontStream.Seek)
                };
            }

            public void Close(IntPtr user)
            {
                stream.Close();
            }

            public QWORD Length(IntPtr user)
            {
                return (QWORD)stream.Length;
            }

            public unsafe DWORD Read(void* buffer, DWORD length, IntPtr user)
            {
                const DWORD eof = 0xffffffffu;

                if (length == 0u)
                    return 0u;

                int numRead = 0;

                try
                {
                    byte[] byteBuffer = new byte[length];
                    numRead = stream.Read(byteBuffer, 0, (int)length);

                    if (numRead > 0)
                    {
                        var ptr = new IntPtr(buffer);
                        Marshal.Copy(byteBuffer, 0, ptr, numRead);
                    }
                    else
                    {
                        return eof;
                    }
                }
                catch
                {
                    return 0u;
                }

                return (DWORD)numRead;
            }

            public bool Seek(QWORD offset, IntPtr user)
            {
                if (!stream.CanSeek)
                    return false;

                return stream.Seek((long)offset, SeekOrigin.Begin) == (long)offset;
            }
        }

        private static class NativeBass
        {
            private const string BassLib = "bass";
            private const string BassMidiLib = "bassmidi";
            public delegate int StreamProcedure(int handle, IntPtr buffer, int length, IntPtr user);
            public delegate void SoundFontCloseProcedure(IntPtr user);
            public delegate QWORD SoundFontLengthProcedure(IntPtr user);
            public unsafe delegate DWORD SoundFontReadProcedure(void* buffer, DWORD length, IntPtr user);
            public delegate bool SoundFontSeekProcedure(QWORD offset, IntPtr user);

            [DllImport(BassLib)]
            private static extern int BASS_ErrorGetCode();
            [DllImport(BassLib)]
            public static extern bool BASS_Init(int device, DWORD freq, DWORD flags, int win, IntPtr clsid);
            [DllImport(BassLib)]
            public static extern bool BASS_Free();
            [DllImport(BassLib)]
            public static extern bool BASS_Start();
            [DllImport(BassLib)]
            public static extern bool BASS_Stop();
            [DllImport(BassLib)]
            public static extern bool BASS_Pause();
            [DllImport(BassLib)]
            public static extern bool BASS_IsStarted();
            [DllImport(BassLib)]
            public static extern float BASS_GetVolume();
            [DllImport(BassLib)]
            public static extern bool BASS_SetVolume(float volume);
            [DllImport(BassLib)]
            public unsafe static extern int BASS_MusicLoad(bool mem, void* file, QWORD offset, DWORD length, DWORD flags, DWORD freq);
            [DllImport(BassLib)]
            public static extern bool BASS_MusicFree(int handle);
            [DllImport(BassLib)]
            public static extern int BASS_StreamCreate(DWORD freq, DWORD chans, DWORD flags, StreamProcedure proc, IntPtr user);
            [DllImport(BassLib)]
            public static extern bool BASS_StreamFree(int handle);
            [DllImport(BassLib)]
            public static extern bool BASS_ChannelPlay(DWORD handle, bool restart);
            [DllImport(BassLib)]
            public static extern bool BASS_ChannelStop(DWORD handle);
            [DllImport(BassLib)]
            public static extern bool BASS_ChannelPause(DWORD handle);
            [DllImport(BassLib)]
            public static extern bool BASS_ChannelSetAttribute(DWORD handle, DWORD attrib, float value);
            [DllImport(BassMidiLib)]
            public static extern int BASS_MIDI_StreamCreateEvents([In, Out] MidiEvent[] events, DWORD ppqn, DWORD flags, DWORD freq);
            [DllImport(BassMidiLib, CallingConvention = CallingConvention.Cdecl)]
            public static extern int BASS_MIDI_FontInitUser(SoundFontFileProcs procs, IntPtr user, DWORD flags);
            [DllImport(BassMidiLib)]
            public static extern bool BASS_MIDI_StreamSetFonts(int handle, [In, Out] MidiFont[] fonts, DWORD count);

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct MidiFont
            {
                public int Font;
                public int Preset;
                public int Bank;
            }

            [Serializable]
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public sealed class SoundFontFileProcs
            {
                public SoundFontCloseProcedure Close;
                public SoundFontLengthProcedure Length;
                public SoundFontReadProcedure Read;
                public SoundFontSeekProcedure Seek;
            }

            public static BassErrorCode LastError => (BassErrorCode)BASS_ErrorGetCode();

            #region Error Codes

            public enum BassErrorCode
            {
                BASS_OK = 0,
                BASS_ERROR_MEM = 1,
                BASS_ERROR_FILEOPEN = 2,
                BASS_ERROR_DRIVER = 3,
                BASS_ERROR_BUFLOST = 4,
                BASS_ERROR_HANDLE = 5,
                BASS_ERROR_FORMAT = 6,
                BASS_ERROR_POSITION = 7,
                BASS_ERROR_INIT = 8,
                BASS_ERROR_START = 9,
                BASS_ERROR_SSL = 10,
                BASS_ERROR_ALREADY = 14,
                BASS_ERROR_NOTAUDIO = 17,
                BASS_ERROR_NOCHAN = 18,
                BASS_ERROR_ILLTYPE = 19,
                BASS_ERROR_ILLPARAM = 20,
                BASS_ERROR_NO3D = 21,
                BASS_ERROR_NOEAX = 22,
                BASS_ERROR_DEVICE = 23,
                BASS_ERROR_NOPLAY = 24,
                BASS_ERROR_FREQ = 25,
                BASS_ERROR_NOTFILE = 27,
                BASS_ERROR_NOHW = 29,
                BASS_ERROR_EMPTY = 31,
                BASS_ERROR_NONET = 32,
                BASS_ERROR_CREATE = 33,
                BASS_ERROR_NOFX = 34,
                BASS_ERROR_NOTAVAIL = 37,
                BASS_ERROR_DECODE = 38,
                BASS_ERROR_DX = 39,
                BASS_ERROR_TIMEOUT = 40,
                BASS_ERROR_FILEFORM = 41,
                BASS_ERROR_SPEAKER = 42,
                BASS_ERROR_VERSION = 43,
                BASS_ERROR_CODEC = 44,
                BASS_ERROR_ENDED = 45,
                BASS_ERROR_BUSY = 46,
                BASS_ERROR_UNSTREAMABLE = 47,
                BASS_ERROR_UNKNOWN = -1
            }

            #endregion
        }
    }
}
