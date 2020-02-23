using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using Freeserf.Audio;
using Freeserf.Data;

namespace Freeserf.Audio.Windows
{
#if WINDOWS
    internal class WindowsWavePlayerFactory : IWavePlayerFactory
    {
        public WindowsWavePlayerFactory(DataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        DataSource dataSource = null;
        static IWavePlayer player = null;

        public IWavePlayer GetWavePlayer()
        {
            if (player == null)
                player = new WindowsWavePlayer(dataSource);

            return player;
        }
    }

    internal class WindowsWavePlayer : Audio.Player, Audio.IVolumeController, IWavePlayer, IDisposable
    {
        IntPtr handle = IntPtr.Zero;
        protected DataSource dataSource = null;
        WinMMNatives.Wave wave = null;

        public WindowsWavePlayer(DataSource dataSource)
        {
            this.dataSource = dataSource;

            var device = FindBestDevice();

            uint samplesPerSec = (this is WindowsModPlayer) ? 44100 : 8000u; // sfx uses 8kHz, mod uses 44.1kHz

            if (device == -1 || !WinMMNatives.OpenPlaybackDevice(out handle, (uint)device, samplesPerSec, 1))
                throw new ExceptionAudio("Unable to create wave output.");

            Available = true;
        }

        protected class WaveTrack : Audio.ITrack
        {
            short[] data = null;

            public WaveTrack(short[] data)
            {
                this.data = data;
            }

            public void Play(Freeserf.Audio.Audio.Player player)
            {
                if (player is WindowsWavePlayer)
                    (player as WindowsWavePlayer).Play(data);
            }
        }

        public bool Available
        {
            get;
            private set;
        } = false;

        public override bool Enabled
        {
            get;
            set;
        } = true;

        public bool Running
        {
            get;
            private set;
        } = false;

        public void Play(short[] data)
        {
            if (!Enabled)
                return;

            if (wave != null)
            {
                wave.Finished -= Wave_Finished;
                wave.Close();
            }

            wave = new WinMMNatives.Wave(handle, data);
            wave.Finished += Wave_Finished;
            Running = true;

            wave.Play();
        }

        private void Wave_Finished(object sender, EventArgs e)
        {
            Running = false;
        }

        protected override Audio.ITrack CreateTrack(int trackID)
        {
            int level = DataSource.DosSounds(dataSource) ? -32 : 0;

            return new WaveTrack(SFX.ConvertToWav(dataSource.GetSound((uint)trackID), level));
        }

        public override void Stop()
        {
            if (Running)
            {
                WinMMNatives.ResetPlaybackDevice(handle);
                Running = false;
            }
        }

        public override void Pause()
        {
            // sounds do not support pause
        }

        public override void Resume()
        {
            // do nothing
        }

        public override Audio.IVolumeController GetVolumeController()
        {
            return this;
        }

        public float Volume
        {
            get
            {
                uint volume = WinMMNatives.GetVolume(handle);
                uint left = volume & 0xffff;
                uint right = volume >> 16;
                float result = 0.0f;

                if (left != right)
                {
                    volume = Math.Max(left, right);
                    result = (float)volume / (float)0xffff;
                    SetVolume(result);
                }
                else
                {
                    result = (float)volume / (float)0xffff;
                }

                return result;
            }
        }

        public void SetVolume(float volume)
        {
            if (volume < 0.0f)
                volume = 0.0f;
            if (volume > 1.0f)
                volume = 1.0f;

            uint value = (uint)Misc.Round(volume * 0xffff);

            value |= (value << 16); // copy left volume to right volume

            WinMMNatives.SetVolume(handle, value);
        }

        public void VolumeDown()
        {
            SetVolume(Volume - 0.1f);
        }

        public void VolumeUp()
        {
            SetVolume(Volume + 0.1f);
        }

        const uint WAVECAPS_VOLUME = 1;

        int FindBestDevice()
        {
            int count = WinMMNatives.GetPlaybackDeviceCount();

            if (count == 0)
                return -1;

            for (uint i = 0; i < count; ++i)
            {
                var caps = WinMMNatives.GetPlaybackDeviceCapabilities(i);

                if (caps == null)
                    continue;

                const int WAVE_FORMAT_4M16 = 0x400;
                const int WAVECAPS_VOLUME = 0x4;

                // we need support for 44.1kHz 16 bit mono
                if ((caps.Value.Formats & WAVE_FORMAT_4M16) == 0)
                    continue;

                // we need volume support
                if ((caps.Value.Support & WAVECAPS_VOLUME) != 0)
                    return (int)i;
            }

            return -1;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
        public struct WAVEOUTCAPS
        {
            public ushort Mid;
            public ushort Pid;
            public uint DriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WinMMNatives.MaxPNameLen)]
            public string Name;
            public uint Formats;
            public ushort Channels;
            public ushort Reserved1;
            public uint Support;
        }

        public delegate void WaveOutProc(IntPtr waveOut, uint msg, IntPtr instance, IntPtr param1, IntPtr param2);

        public static class WinMMNatives
        {
            internal class Wave
            {
                IntPtr handle;
                IntPtr dataPointer;
                WAVEHDR data = new WAVEHDR();
                readonly Timer timer = new Timer();

                public event EventHandler Finished;

                public Wave(IntPtr handle, short[] data)
                {
                    timer.Interval = 20;
                    timer.AutoReset = true;
                    timer.Elapsed += Timer_Elapsed;

                    this.handle = handle;
                    dataPointer = Marshal.AllocHGlobal(Marshal.SizeOf(this.data));

                    this.data.Data = Marshal.AllocHGlobal(data.Length * 2);
                    Marshal.Copy(data, 0, this.data.Data, data.Length);
                    this.data.BufferLength = (uint)data.Length * 2;
                    this.data.Flags = 0;

                    Marshal.StructureToPtr(this.data, dataPointer, true);

                    if (waveOutPrepareHeader(handle, dataPointer, Marshal.SizeOf(this.data)) != 0)
                    {
                        throw new ExceptionAudio("Unable to play wave.");
                    }

                    this.data = (WAVEHDR)Marshal.PtrToStructure(dataPointer, typeof(WAVEHDR));
                }

                private void Timer_Elapsed(object sender, ElapsedEventArgs e)
                {
                    if ((data.Flags & WaveHdrFlags.WHDR_DONE) == 0) // still running
                    {
                        // updated managed data
                        data = (WAVEHDR)Marshal.PtrToStructure(dataPointer, typeof(WAVEHDR));
                    }
                    else // finished
                    {
                        Close();
                    }
                }

                public void Play()
                {
                    if (waveOutWrite(handle, dataPointer, Marshal.SizeOf(data)) != 0)
                    {
                        Close();
                        return;
                    }

                    timer.Start();
                }

                public void Close()
                {
                    timer.Stop();

                    waveOutUnprepareHeader(handle, dataPointer, Marshal.SizeOf(data));
                    waveOutReset(handle);

                    Marshal.FreeHGlobal(data.Data);
                    Marshal.FreeHGlobal(dataPointer);

                    dataPointer = IntPtr.Zero;
                    data.Data = IntPtr.Zero;

                    Finished?.Invoke(this, EventArgs.Empty);
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct WAVEFORMATEX
            {
                public ushort FormatTag;
                public ushort Channels;
                public uint SamplesPerSec;
                public uint AvgBytesPerSec;
                public ushort BlockAlign;
                public ushort BitsPerSample;
                public ushort Size;
            }

            [Flags]
            enum WaveHdrFlags : uint
            {
                WHDR_DONE = 1,
                WHDR_PREPARED = 2,
                WHDR_BEGINLOOP = 4,
                WHDR_ENDLOOP = 8,
                WHDR_INQUEUE = 16
            }

            [StructLayout(LayoutKind.Sequential)]
            struct WAVEHDR
            {
                public IntPtr Data; // pointer to locked data buffer
                public uint BufferLength; // length of data buffer
                public uint BytesRecorded; // used for input only
                public IntPtr User; // for client's use
                public WaveHdrFlags Flags; // assorted flags (see defines)
                public uint Loops; // loop control counter
                public IntPtr Next; // PWaveHdr, reserved for driver
                public IntPtr Reserved; // reserved for driver
            }

            public const string LibraryName = "winmm";
            public const int MaxPNameLen = 32;

            [DllImport(LibraryName)]
            static extern int waveOutGetNumDevs();

            [DllImport(LibraryName)]
            static extern int waveOutGetDevCaps(UIntPtr uDeviceID, out WAVEOUTCAPS waveOutCaps, uint sizeOfwaveOutCaps);

            [DllImport(LibraryName)]
            static extern int waveOutOpen(out IntPtr waveIn, uint deviceID, ref WAVEFORMATEX format, WaveOutProc callback, IntPtr callbackInstance, int flags);

            [DllImport(LibraryName)]
            static extern int waveOutClose(IntPtr waveIn);

            [DllImport(LibraryName)]
            static extern int waveOutReset(IntPtr waveIn);

            [DllImport(LibraryName)]
            static extern int waveOutGetVolume(IntPtr handle, out uint volume);

            [DllImport(LibraryName)]
            static extern int waveOutSetVolume(IntPtr handle, uint volume);

            [DllImport(LibraryName)]
            static extern int waveOutPrepareHeader(IntPtr handle, IntPtr data, int size);

            [DllImport(LibraryName)]
            static extern int waveOutUnprepareHeader(IntPtr handle, IntPtr data, int size);

            [DllImport(LibraryName)]
            static extern int waveOutWrite(IntPtr handle, IntPtr data, int size);

            [DllImport(LibraryName)]
            static extern int waveOutGetErrorText(int mmrError, StringBuilder message, int sizeOfMessage);

            [DllImport(LibraryName)]
            static extern int waveInGetErrorText(int mmrError, StringBuilder message, int sizeOfMessage);

            internal static bool OpenPlaybackDevice(out IntPtr handle, uint deviceId, uint samplesPerSec, int numChannels)
            {
                const int WAVE_FORMAT_PCM = 1;

                WAVEFORMATEX format = new WAVEFORMATEX();

                format.FormatTag = WAVE_FORMAT_PCM;
                format.Channels = (ushort)numChannels;
                format.SamplesPerSec = samplesPerSec;
                format.BitsPerSample = 16;
                format.BlockAlign = (ushort)(format.Channels * format.BitsPerSample / 8);
                format.AvgBytesPerSec = format.SamplesPerSec * format.BlockAlign;
                format.Size = (ushort)Marshal.SizeOf(format);

                return waveOutOpen(out handle, deviceId, ref format, null, IntPtr.Zero, 0) == 0;
            }

            internal static bool ClosePlaybackDevice(IntPtr handle)
            {
                return waveOutClose(handle) == 0;
            }

            internal static bool ResetPlaybackDevice(IntPtr handle)
            {
                return waveOutReset(handle) == 0;
            }

            internal static uint GetVolume(IntPtr handle)
            {
                if (waveOutGetVolume(handle, out uint volume) != 0)
                    return 0;

                return volume;
            }

            internal static bool SetVolume(IntPtr handle, uint volume)
            {
                return waveOutSetVolume(handle, volume) == 0;
            }

            internal static WAVEOUTCAPS? GetPlaybackDeviceCapabilities(uint device)
            {
                WAVEOUTCAPS caps = new WAVEOUTCAPS();
                UIntPtr deviceID = new UIntPtr(device);

                if (waveOutGetDevCaps(deviceID, out caps, (uint)Marshal.SizeOf(caps)) != 0)
                    return null;

                return caps;
            }

            internal static int GetPlaybackDeviceCount()
            {
                return waveOutGetNumDevs();
            }

            internal static string GetwaveOutErrorText(int code, int maxLength = 128)
            {
                StringBuilder errorMsg = new StringBuilder(maxLength);

                if (waveOutGetErrorText(code, errorMsg, maxLength) == 0)
                {
                    return errorMsg.ToString();
                }

                return "Unknown winmm wave output error";
            }

            internal static string GetwaveInErrorText(int code, int maxLength = 128)
            {
                StringBuilder errorMsg = new StringBuilder(maxLength);

                if (waveInGetErrorText(code, errorMsg, maxLength) == 0)
                {
                    return errorMsg.ToString();
                }

                return "Unknown winmm wave input error";
            }
        }

#region IDisposable Support

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (handle != null && handle != IntPtr.Zero)
                {
                    if (wave != null)
                    {
                        wave.Close();
                        wave = null;
                    }

                    WinMMNatives.ResetPlaybackDevice(handle);
                    WinMMNatives.ClosePlaybackDevice(handle);
                    handle = IntPtr.Zero;
                }

                disposed = true;
            }
        }

        ~WindowsWavePlayer()
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
#endif // WINDOWS
}
