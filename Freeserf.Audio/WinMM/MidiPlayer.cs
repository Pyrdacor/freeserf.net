using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using Freeserf.Data;

namespace Freeserf.Audio.Windows
{
#if WINDOWS
    internal class MidiPlayer : Audio.Player, Audio.IVolumeController, IDisposable
    {
        IntPtr handle = IntPtr.Zero;
        readonly Timer eventTimer = new Timer();
        int currentEventIndex = 0;
        readonly Queue<uint> messageQueue = new Queue<uint>();
        DateTime trackStartTime = DateTime.MinValue;
        DateTime pauseStartTime = DateTime.MinValue;
        bool looped = false;
        bool paused = false;
        readonly bool available = false;
        bool enabled = false;
        DataSource dataSource = null;
        bool runningStateChanged = false;
        bool playingEvents = false;

        public MidiPlayer(DataSource dataSource)
        {
            this.dataSource = dataSource;

            var device = FindBestDevice();

            if (device == -1 || !WinMMNatives.OpenPlaybackDevice(out handle, (uint)device))
                throw new ExceptionAudio("Unable to create midi output.");

            available = true;
            enabled = true;

            Init();

        }

        double CurrentTrackTime
        {
            get
            {
                if (paused)
                    return (pauseStartTime - trackStartTime).TotalMilliseconds;
                else
                    return (DateTime.Now - trackStartTime).TotalMilliseconds;
            }
        }

        public override bool Enabled
        {
            get => enabled && available;
            set
            {
                if (enabled == value)
                    return;

                enabled = value;

                if (available)
                {
                    if (enabled) // just enabled
                    {
                        // restart the music
                        if (CurrentXMI != null)
                        {
                            currentEventIndex = 0;
                            trackStartTime = DateTime.Now;
                            Running = true;
                            runningStateChanged = true;
                            Play(CurrentXMI);
                        }
                    }
                    else
                    {
                        if (Running)
                        {
                            Stop();
                        }
                    }
                }
            }
        }

        public bool Running
        {
            get;
            private set;
        } = false;

        public XMI CurrentXMI
        {
            get;
            private set;
        } = null;

        void Play(XMI xmi)
        {
            if (Running)
                Stop();

            CurrentXMI = xmi;

            if (!Enabled)
                return;

            if (CurrentXMI != null)
            {
                currentEventIndex = 0;
                trackStartTime = DateTime.Now;
                paused = false;

                runningStateChanged = true;
                Running = true;

                eventTimer.Start();
            }
        }

        void PlayEvents()
        {
            if (playingEvents)
                return;

            playingEvents = true;

            try
            {
                if (CurrentXMI == null || !Running || !Enabled)
                {
                    Stop();
                    return;
                }

                if (currentEventIndex == CurrentXMI.NumEvents)
                {
                    currentEventIndex = 0;
                    trackStartTime = DateTime.Now;
                }

                var currentTrackTime = CurrentTrackTime;

                while (true)
                {
                    var ev = CurrentXMI.GetEvent(currentEventIndex);

                    if (ev.StartTime < currentTrackTime + 5.0)
                    {
                        SendEvent(ev.ToMidiMessage());
                        ++currentEventIndex;

                        if (currentEventIndex == CurrentXMI.NumEvents)
                        {
                            currentEventIndex = 0;
                            trackStartTime = DateTime.Now;
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            finally
            {
                playingEvents = false;
            }
        }

        class MidiTrack : Audio.ITrack
        {
            private XMI xmi = null;

            public MidiTrack(XMI xmi)
            {
                this.xmi = xmi;
            }

            public void Play(Audio.Player player)
            {
                (player as MidiPlayer).Play(xmi);
            }
        }

        protected override Audio.ITrack CreateTrack(int trackID)
        {
            var music = AudioImpl.GetMusicTrackData(dataSource, trackID);

            if (DataSource.DosMusic(dataSource))
                return new MidiTrack(new XMI(music));

            throw new ExceptionFreeserf(ErrorSystemType.Data, $"Only DOS data uses MIDI music");
        }

        public override void Stop()
        {
            if (Running)
            {
                eventTimer.Stop();
                Running = false;
                paused = false;
                currentEventIndex = 0;
                WinMMNatives.ResetPlaybackDevice(handle);
                runningStateChanged = true;
            }
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
                    volume = left;
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

        void Init()
        {
            eventTimer.Elapsed += EventTimer_Elapsed;
            eventTimer.AutoReset = true;
            eventTimer.Interval = 5;
        }

        void SendEvent(uint message)
        {
            if (!Running)
                return;

            runningStateChanged = false;

            WinMMNatives.SendPlaybackDeviceMessage(handle, message);

            if (runningStateChanged)
                WinMMNatives.ResetPlaybackDevice(handle);

            runningStateChanged = false;
        }

        private void EventTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!Running)
                return;

            PlayEvents();
        }

        const uint MIDICAPS_VOLUME = 1;

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

                // we need volume support
                if ((caps.Value.Support & MIDICAPS_VOLUME) != 0)
                     return (int)i;
            }

            return -1;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct MidiOutCaps
        {
            public short Mid;
            public short Pid;
            public int DriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WinMMNatives.MaxPNameLen)]
            public string Name;
            public short Technology;
            public short Voices;
            public short Notes;
            public short ChannelMask;
            public int Support;
        }

        public delegate void MidiOutProc(IntPtr midiOut, uint msg, IntPtr instance, IntPtr param1, IntPtr param2);

        public static class WinMMNatives
        {
            public const string LibraryName = "winmm";
            public const int MaxPNameLen = 32;

            [DllImport(LibraryName)]
            static extern int midiOutGetNumDevs();

            [DllImport(LibraryName)]
            static extern int midiOutGetDevCaps(UIntPtr uDeviceID, out MidiOutCaps midiOutCaps, uint sizeOfMidiOutCaps);

            [DllImport(LibraryName)]
            static extern int midiOutOpen(out IntPtr midiIn, uint deviceID, MidiOutProc callback, IntPtr callbackInstance, int flags);

            [DllImport(LibraryName)]
            static extern int midiOutClose(IntPtr midiIn);

            [DllImport(LibraryName)]
            static extern int midiOutReset(IntPtr midiIn);

            [DllImport(LibraryName)]
            static extern int midiOutShortMsg(IntPtr handle, uint msg);

            [DllImport(LibraryName)]
            static extern int midiOutGetVolume(IntPtr handle, out uint volume);

            [DllImport(LibraryName)]
            static extern int midiOutSetVolume(IntPtr handle, uint volume);

            [DllImport(LibraryName)]
            static extern int midiOutGetErrorText(int mmrError, StringBuilder message, int sizeOfMessage);

            [DllImport(LibraryName)]
            static extern int midiInGetErrorText(int mmrError, StringBuilder message, int sizeOfMessage);

            internal static bool OpenPlaybackDevice(out IntPtr handle, uint deviceId)
            {
                return midiOutOpen(out handle, deviceId, null, IntPtr.Zero, 0) == 0;
            }

            internal static bool ClosePlaybackDevice(IntPtr handle)
            {
                return midiOutClose(handle) == 0;
            }

            internal static bool ResetPlaybackDevice(IntPtr handle)
            {
                Log.Verbose.Write(ErrorSystemType.Audio, "MIDI Reset");

                return midiOutReset(handle) == 0;
            }

            internal static bool SendPlaybackDeviceMessage(IntPtr handle, uint message)
            {
                Log.Verbose.Write(ErrorSystemType.Audio, string.Format("MIDI Message {0:x2} {1:x2} {2:x2}", message & 0xff, (message >> 8) & 0xff, (message >> 16) & 0xff));

                return midiOutShortMsg(handle, message) == 0;
            }

            internal static uint GetVolume(IntPtr handle)
            {
                if (midiOutGetVolume(handle, out uint volume) != 0)
                    return 0;

                return volume;
            }

            internal static bool SetVolume(IntPtr handle, uint volume)
            {
                return midiOutSetVolume(handle, volume) == 0;
            }

            internal static MidiOutCaps? GetPlaybackDeviceCapabilities(uint device)
            {
                MidiOutCaps caps = new MidiOutCaps();
                UIntPtr deviceID = new UIntPtr(device);

                if (midiOutGetDevCaps(deviceID, out caps, (uint)Marshal.SizeOf(caps)) != 0)
                    return null;

                return caps;
            }

            internal static int GetPlaybackDeviceCount()
            {
                return midiOutGetNumDevs();
            }

            internal static string GetMidiOutErrorText(int code, int maxLength = 128)
            {
                StringBuilder errorMsg = new StringBuilder(maxLength);

                if (midiOutGetErrorText(code, errorMsg, maxLength) == 0)
                {
                    return errorMsg.ToString();
                }

                return "Unknown winmm midi output error";
            }

            internal static string GetMidiInErrorText(int code, int maxLength = 128)
            {
                StringBuilder errorMsg = new StringBuilder(maxLength);

                if (midiInGetErrorText(code, errorMsg, maxLength) == 0)
                {
                    return errorMsg.ToString();
                }

                return "Unknown winmm midi input error";
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
                    WinMMNatives.ResetPlaybackDevice(handle);
                    WinMMNatives.ClosePlaybackDevice(handle);
                    handle = IntPtr.Zero;
                }

                disposed = true;
            }
        }

         ~MidiPlayer()
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
