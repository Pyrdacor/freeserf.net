using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

namespace Freeserf.Renderer.OpenTK.Audio.Windows
{
    internal class WindowsMidiPlayerFactory : IMidiPlayerFactory
    {
        public WindowsMidiPlayerFactory(DataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        DataSource dataSource = null;
        static IMidiPlayer player = null;

        public IMidiPlayer GetMidiPlayer()
        {
            if (player == null)
                player = new WindowsMidiPlayer(dataSource);

            return player;
        }
    }

    internal class WindowsMidiPlayer : Audio.Player, Audio.IVolumeController, IMidiPlayer, IDisposable
    {
        IntPtr handle = IntPtr.Zero;
        readonly Timer eventTimer = new Timer();
        readonly Timer nextEventTimer = new Timer();
        int currentEventIndex = 0;
        readonly Queue<uint> messageQueue = new Queue<uint>();
        DateTime trackStartTime = DateTime.MinValue;
        DateTime pauseStartTime = DateTime.MinValue;
        bool looped = false;
        bool paused = false;
        bool enabled = true;
        DataSource dataSource = null;
        bool runningStateChanged = false;

        public WindowsMidiPlayer(DataSource dataSource)
        {
            this.dataSource = dataSource;

#if WINDOWS
            var device = FindBestDevice();

            if (device == -1 || !WinMMNatives.OpenPlaybackDevice(out handle, (uint)device))
                throw new ExceptionAudio("Unable to create midi output.");

            Available = true;

            Init();
#endif // WINDOWS

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

        public bool Available
        {
            get;
            private set;
        } = false;

        public override bool Enabled
        {
            get => enabled && Available;
            set
            {
                if (!Available)
                    value = false;

                if (enabled == value)
                    return;

                if (value) // just enabled
                {
                    enabled = true;

                    // restart the music
                    if (CurrentXMI != null)
                    {
                        currentEventIndex = 0;
                        trackStartTime = DateTime.Now;
                        Running = true;
                        runningStateChanged = true;
                        PlayNextEvent();
                    }
                }
                else
                {
                    if (Running)
                    {
                        Stop();
                    }

                    enabled = false;
                }
            }
        }

        public bool Paused
        {
            get => paused && Running && Enabled && CurrentXMI != null;
            private set
            {
                if (!Running || !Enabled || CurrentXMI == null)
                {
                    paused = false;
                    return;
                }

                if (paused == value)
                    return;

                paused = value;

                if (paused)
                    pauseStartTime = DateTime.Now;
            }
        }

        public bool Running
        {
            get;
            private set;
        } = false;

        public bool Looped
        {
            get => looped;
            private set
            {
                if (looped == value)
                    return;

                bool start = Enabled && !looped && !Running;

                looped = value;

                if (start)
                {
                    Running = true;
                    PlayNextEvent();
                }
            }
        }

        public XMI CurrentXMI
        {
            get;
            private set;
        } = null;

        public void Play(XMI xmi, bool looped)
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

                PlayNextEvent();
            }
        }

        void PlayNextEvent()
        {
            if (CurrentXMI == null || !Running || !Enabled)
            {
                Stop();
                return;
            }

            if (Paused)
                return;

            if (currentEventIndex == CurrentXMI.NumEvents)
            {
                if (Looped)
                {
                    currentEventIndex = 0;
                    trackStartTime = DateTime.Now;
                }
                else
                {
                    Stop();
                    return;
                }
            }

            var ev = CurrentXMI.GetEvent(currentEventIndex++);
            var currentTrackTime = CurrentTrackTime;

            if ((long)ev.StartTime > (long)currentTrackTime)
            {
                SendDelayedEvent((uint)(ev.StartTime - currentTrackTime), ev.ToMidiMessage());
            }
            else
            {
                SendEvent(ev.ToMidiMessage());
            }
        }

        protected override Audio.ITrack CreateTrack(int trackID)
        {
            if (dataSource is DataSourceAmiga)
                return new XMI(new MOD(dataSource.GetSound((uint)trackID))); // TODO: needs testing with amiga data
            else
                return new XMI(dataSource.GetMusic((uint)trackID));
        }

        public override void Stop()
        {
#if WINDOWS
            if (Running)
            {
                eventTimer.Stop();
                nextEventTimer.Stop();
                Running = false;
                paused = false;
                currentEventIndex = 0;
                WinMMNatives.ResetPlaybackDevice(handle);
                runningStateChanged = true;
            }
#endif // WINDOWS
        }

        public override void Pause()
        {
            Paused = true;
        }

        public override void Resume()
        {
            if (!Paused)
                return;

            Paused = false;

            if (Running && Enabled && CurrentXMI != null)
            {
                trackStartTime += DateTime.Now - pauseStartTime;

                PlayNextEvent();
            }
        }

        public override Audio.IVolumeController GetVolumeController()
        {
            return this;
        }

        public float GetVolume()
        {
#if WINDOWS
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
#else
            return 0.0f;
#endif // WINDOWS
        }

        public void SetVolume(float volume)
        {
#if WINDOWS
            if (volume < 0.0f)
                volume = 0.0f;
            if (volume > 1.0f)
                volume = 1.0f;

            uint value = (uint)Misc.Round(volume * 0xffff);

            value |= (value << 16); // copy left volume to right volume

            WinMMNatives.SetVolume(handle, value);
#endif // WINDOWS
        }

        public void VolumeDown()
        {
            SetVolume(GetVolume() - 0.1f);
        }

        public void VolumeUp()
        {
            SetVolume(GetVolume() + 0.1f);
        }

        void Init()
        {
            eventTimer.Elapsed += EventTimer_Elapsed;
            eventTimer.AutoReset = false;

            nextEventTimer.Interval = 0.1;
            nextEventTimer.Elapsed += NextEventTimer_Elapsed;
            nextEventTimer.AutoReset = false;
        }

        private void NextEventTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!Running)
                return;

            PlayNextEvent();
        }

        void SendEvent(uint message)
        {
            if (!Running)
                return;

            runningStateChanged = false;

#if WINDOWS
            WinMMNatives.SendPlaybackDeviceMessage(handle, message);

            if (!runningStateChanged)
                nextEventTimer.Start();
            else
                WinMMNatives.ResetPlaybackDevice(handle);

            runningStateChanged = false;

#endif // WINDOWS
        }

        void SendDelayedEvent(uint delay, uint message)
        {
            if (delay == 0)
            {
                SendEvent(message);
                return;
            }

            lock (messageQueue)
            {
                messageQueue.Enqueue(message);
            }

            eventTimer.Interval = delay;
            eventTimer.Start();
        }

        private void EventTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!Running)
                return;

            uint message = 0;

            lock (messageQueue)
            {
                if (!Running)
                    return;

                if (messageQueue.Count > 0)
                    message = messageQueue.Dequeue();
            }

            if (message != 0)
            {
                SendEvent(message);
            }
            else
            {
                PlayNextEvent();
            }
        }

        
#if WINDOWS

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
                Log.Debug.Write("audio", "MIDI Reset");

                return midiOutReset(handle) == 0;
            }

            internal static bool SendPlaybackDeviceMessage(IntPtr handle, uint message)
            {
                Log.Debug.Write("audio", string.Format("MIDI Message {0:x2} {1:x2} {2:x2}", message & 0xff, (message >> 8) & 0xff, (message >> 16) & 0xff));

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

#endif // WINDOWS


        #region IDisposable Support

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
#if WINDOWS
                if (handle != null && handle != IntPtr.Zero)
                {
                    WinMMNatives.ResetPlaybackDevice(handle);
                    WinMMNatives.ClosePlaybackDevice(handle);
                    handle = IntPtr.Zero;
                }
#endif // WINDOWS

                disposed = true;
            }
        }

         ~WindowsMidiPlayer()
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

}
