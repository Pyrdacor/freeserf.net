using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

namespace Freeserf.Renderer.OpenTK.Audio.Windows
{
    internal class WindowsMidiPlayerFactory : IMidiPlayerFactory
    {
        static IMidiPlayer player = null;

        public IMidiPlayer GetMidiPlayer()
        {
            if (player == null)
                player = new WindowsMidiPlayer();

            return player;
        }
    }

    internal class WindowsMidiPlayer : IMidiPlayer, IDisposable
    {
        readonly MidiOutCaps caps = new MidiOutCaps();
        IntPtr handle = IntPtr.Zero;
        readonly Timer eventTimer = new Timer();
        int currentEventIndex = 0;
        readonly Queue<uint> messageQueue = new Queue<uint>();
        DateTime trackStartTime = DateTime.MinValue;
        DateTime pauseStartTime = DateTime.MinValue;
        bool looped = false;
        bool paused = false;

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

        public bool Enabled
        {
            get;
            set;
        } = true;

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
            if (!Enabled)
                return;

            CurrentXMI = xmi;

            if (CurrentXMI == null)
            {
                Stop();
            }
            else
            {
                currentEventIndex = 0;
                trackStartTime = DateTime.Now;
                paused = false;

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
        }

        public void Stop()
        {
            Running = false;
        }

        public void Pause()
        {
            Paused = true;
        }

        public void Resume()
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

        public WindowsMidiPlayer()
        {

#if WINDOWS
            if (!WinMMNatives.OpenPlaybackDevice(out handle, 0u))
                throw new ExceptionAudio("Unable to create midi output.");

            Available = true;

            Init();
#endif

        }

        void Init()
        {
            eventTimer.Elapsed += EventTimer_Elapsed;
            eventTimer.AutoReset = true;
        }

        void SendEvent(uint message)
        {
            WinMMNatives.SendPlaybackDeviceMessage(handle, message);
            PlayNextEvent();
        }

        void SendDelayedEvent(uint delay, uint message)
        {
            lock (messageQueue)
            {
                messageQueue.Enqueue(message);
            }

            eventTimer.Interval = delay;
            eventTimer.Start();
        }

        private void EventTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (messageQueue)
            {
                if (messageQueue.Count > 0)
                {
                    SendEvent(messageQueue.Dequeue());
                }
            }
        }

        /*[StructLayout(LayoutKind.Sequential)]
        struct MidiShortMessage
        {
            const byte CHANNEL_MASK = 0x0F;
            const byte STATUS_MASK = 0xF0;

            byte[] data;

            public MidiShortMessage(MessageCommandMask command, byte midiChannel, byte value1, byte value2)
            {
                data = new byte[4];

                StatusCommand = command;
                Channel = midiChannel;
                Parameter1 = value1;
                Parameter2 = value2;
            }

            public MessageCommandMask StatusCommand
            {
                get => (MessageCommandMask)(data[0] >> 4);
                set => data[0] = (byte)((byte)value | (data[0] & CHANNEL_MASK));
            }

            public byte Channel
            {
                get => (byte)(data[0] & CHANNEL_MASK);
                set => data[0] = (byte)((data[0] & STATUS_MASK) | (value & CHANNEL_MASK));
            }

            public byte Parameter1
            {
                get => data[1];
                set => data[1] = value;
            }

            public byte Parameter2
            {
                get => data[2];
                set => data[2] = value;
            }

            public static explicit operator int(MidiShortMessage target)
            {
                return BitConverter.ToInt32(target.data, 0);
            }

            public enum MessageCommandMask : byte
            {
                NoteOff = 0x80,
                NoteOn = 0x90,
                PolyKeyPressure = 0xA0,
                ControllerChange = 0xB0,
                ProgramChange = 0xC0,
                ChannelPressure = 0xD0,
                PitchBend = 0xE0
            }
        }*/
        
#if WINDOWS

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
            static extern int midiOutGetDevCaps(IntPtr uDeviceID, out MidiOutCaps midiOutCaps, uint sizeOfMidiOutCaps);

            [DllImport(LibraryName)]
            static extern int midiOutOpen(out IntPtr midiIn, uint deviceID, MidiOutProc callback, IntPtr callbackInstance, int flags);

            [DllImport(LibraryName)]
            static extern int midiOutClose(IntPtr midiIn);

            [DllImport(LibraryName)]
            static extern int midiOutReset(IntPtr midiIn);

            [DllImport(LibraryName)]
            static extern int midiOutShortMsg(IntPtr handle, uint msg);

            [DllImport(LibraryName)]
            static extern int midiOutGetErrorText(int mmrError, StringBuilder message, int sizeOfMessage);

            [DllImport(LibraryName)]
            static extern int midiInGetErrorText(int mmrError, StringBuilder message, int sizeOfMessage);

            internal static bool OpenPlaybackDevice(out IntPtr handle, uint deviceId)
            {
                return midiOutOpen(out handle, deviceId, null, IntPtr.Zero, 0x30000) == 0;
            }

            internal static bool ClosePlaybackDevice(IntPtr handle)
            {
                return midiOutClose(handle) == 0;
            }

            internal static bool ResetPlaybackDevice(IntPtr handle)
            {
                return midiOutReset(handle) == 0;
            }

            internal static bool SendPlaybackDeviceMessage(IntPtr handle, uint message)
            {
                return midiOutShortMsg(handle, message) == 0;
            }

            internal static MidiOutCaps? GetPlaybackDeviceCapabilities(IntPtr device)
            {
                MidiOutCaps caps = new MidiOutCaps();

                if (midiOutGetDevCaps(device, out caps, (uint)Marshal.SizeOf(caps)) == 0)
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


#endif

    }

}
