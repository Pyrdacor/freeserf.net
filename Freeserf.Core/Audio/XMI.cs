using System;
using System.Collections;
using System.Collections.Generic;

namespace Freeserf.Audio
{
    public class XMI : IEnumerable<XMI.Event>
    {
        const int freq = 120; // ticks per second
        uint tempo = 500000;
        readonly List<Event> events = new List<Event>();
        double currentTime = 0.0;
        uint currentTick = 0;

        public int NumEvents => events.Count;

        public Event GetEvent(int index)
        {
            return events[index];
        }

        public XMI(Data.Buffer data)
        {
            // Note: Chunk length and so on are encoded as big endian.
            // But as we don't use them we use little endian because
            // the XMI data is encoded in little endian.
            data.SetEndianess(Endian.Endianess.Little);

            // Form chunk
            if (data.ToString(4) != "FORM")
                return;

            data.Pop(4); // FORM
            data.Pop<uint>(); // FORM chunk length

            // format XDIR
            if (data.ToString(4) != "XDIR")
                return;

            data.Pop(4); // XDIR

            if (data.ToString(4) != "INFO")
                return;

            data.Pop(4); // INFO
            data.Pop<uint>(); // INFO chunk length

            int numTracks = data.Pop<ushort>();

            if (numTracks != 1)
                return; // we only support one track per file

            if (data.ToString(4) != "CAT ")
                return;

            data.Pop(4); // CAT_
            data.Pop<uint>(); // CAT chunk length

            // format XMID
            if (data.ToString(4) != "XMID")
                return;

            data.Pop(4); // XMID

            // load the one track

            // Form chunk
            if (data.ToString(4) != "FORM")
                return;

            data.Pop(4); // FORM
            data.Pop<uint>(); // FORM chunk length

            // format XMID
            if (data.ToString(4) != "XMID")
                return;

            data.Pop(4); // XMID

            // TIMB chunk
            if (data.ToString(4) != "TIMB")
                return;

            data.Pop(4); // TIMB
            data.Pop<uint>(); // TIMB chunk length

            int count = data.Pop<ushort>();

            for (int j = 0; j < count; ++j)
            {
                // we don't need the TIMB information, just skip it
                data.Pop(2);
            }

            // EVNT chunk
            if (data.ToString(4) != "EVNT")
                return;

            data.Pop(4); // EVNT
            data.Pop<uint>(); // EVNT chunk length

            // read xmi/midi events
            while (data.Readable())
            {
                ParseEvent(data);
            }

            var eventIndices = new Dictionary<Event, int>();

            for (int i = 0; i < events.Count; ++i)
                eventIndices.Add(events[i], i);

            events.Sort((a, b) =>
            {
                int result = a.StartTime.CompareTo(b.StartTime);

                if (result == 0)
                {
                    return eventIndices[a].CompareTo(eventIndices[b]);
                }

                return result;
            });
        }

        uint ParseDeltaTime(Data.Buffer data)
        {
            uint deltaTime = 0;

            byte b = data.Pop<byte>();

            while ((b & 0x80) != 0)
            {
                deltaTime = (deltaTime << 7) | (uint)(b & 0x7f);
                b = data.Pop<byte>();
            }

            deltaTime = (deltaTime << 7) | (uint)(b & 0x7f);

            return deltaTime;
        }

        void ParseMetaEvent(Data.Buffer data)
        {
            // we ignore most of them
            // we only need the "set tempo" event
            byte type = data.Pop<byte>();
            byte len = data.Pop<byte>();

            if (type == 0x51) // set tempo
            {
                if (len != 3)
                    throw new ExceptionAudio("Invalid event length.");

                // 24 bit value: microseconds per quarternote
                byte high = data.Pop<byte>();
                byte mid = data.Pop<byte>();
                byte low = data.Pop<byte>();

                tempo = (uint)high << 16 | (uint)mid << 8 | low;

                events.Add(new SetTempoEvent(tempo, currentTime, currentTick));

                Log.Verbose.Write(ErrorSystemType.Audio, $"XMI Tempo Changed: {tempo} microseconds per quarter note.");
            }
            else if (type == 0x58)
            {
                if (len != 4)
                    throw new ExceptionAudio("Invalid event length.");

                int numerator = data.Pop<byte>();
                int denominator = 1 << data.Pop<byte>();
                int numMidiClocks = data.Pop<byte>();
                int num32InQuarterNote = data.Pop<byte>();

                Log.Verbose.Write(ErrorSystemType.Audio, $"XMI Time Signature: {numerator}/{denominator} ({numMidiClocks} MIDI Clocks, {num32InQuarterNote} 32nd-notes in quarter note.");
            }
            else
            {
                // skip event data
                data.Pop(len);
            }
        }

        void ParseEvent(Data.Buffer data)
        {
            byte status = data.PeekByte();

            if (status == 0xff) // meta event
            {
                data.Pop(1);
                ParseMetaEvent(data);
                return;
            }

            int channel = status & 0xf;
            int eventType = status >> 4;

            if (eventType < 0x8)
            {
                uint ticks = data.Pop<byte>();
                currentTime += ConvertTicksToTime(ticks);
                currentTick += ticks;
                return;
            }
            else if (eventType < 0xF)
            {
                data.Pop(1);
            }

            switch (eventType)
            {
                case 0x9: // Note on
                    // Note: In XMI it has 3 parameters (last for duration).
                    // But we create two events (note on and off).
                    {
                        byte note = data.Pop<byte>();
                        byte velocity = data.Pop<byte>();
                        uint length = ParseDeltaTime(data);

                        if (velocity != 0)
                        {
                            var onEvent = new PlayNoteEvent((byte)channel, note, velocity, currentTime, currentTick);
                            var offEvent = new StopNoteEvent((byte)channel, note, currentTime + ConvertTicksToTime(length), currentTick + length);

                            events.Add(onEvent);
                            events.Add(offEvent);
                        }
                    }
                    break;
                case 0xB: // Control change
                    {
                        byte controller = (byte)(data.Pop<byte>() & 0x7f);
                        byte value = (byte)(data.Pop<byte>() & 0x7f);

                        if (controller < 120) // ignore reserved controller events >= 120
                        {
                            events.Add(new SetControllerValueEvent((byte)channel, controller, value, currentTime, currentTick));
                        }
                    }
                    break;
                case 0x8:
                case 0xA:
                case 0xE:
                    data.Pop(2);
                    break;
                case 0xC:
                    events.Add(new SetInstrumentEvent((byte)channel, data.Pop<byte>(), currentTime, currentTick));
                    break;
                case 0xD:
                    data.Pop(1);
                    break;
                default:
                    throw new ExceptionAudio("Unsupported xmi/midi event type.");
            }
        }

        public double TicksPerQuarternote
        {
            get
            {
                // ticks_per_quarter_note / quarternote_time_in_seconds = freq
                return Math.Floor(freq * tempo / 1000000.0);
            }
        }

        // in milliseconds
        double ConvertTicksToTime(uint ticks)
        {
            double time = (double)ticks / TicksPerQuarternote;

            return time * tempo / 1000.0;
        }

        public IEnumerator<Event> GetEnumerator()
        {
            return events.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public abstract class Event
        {
            public double StartTime
            {
                get;
            }

            public uint Ticks
            {
                get;
            }

            protected Event(double startTime, uint ticks)
            {
                StartTime = startTime;
                Ticks = ticks;
            }

            public abstract uint ToMidiMessage();
        }

        public class PlayNoteEvent : Event
        {
            public byte Channel { get; } = 0;
            public byte Note { get; } = 0;
            public byte Velocity { get; } = 0;

            public PlayNoteEvent(byte channel, byte note, byte velocity, double startTime, uint ticks)
                : base(startTime, ticks)
            {
                Channel = channel;
                Note = note;
                Velocity = velocity;
            }

            public override uint ToMidiMessage()
            {
                byte code = (byte)(0x90 | Channel);

                return BitConverter.ToUInt32(new byte[4] { code, Note, Velocity, 0x00 }, 0);
            }
        }

        public class StopNoteEvent : Event
        {
            public byte Channel { get; } = 0;
            public byte Note { get; } = 0;

            public StopNoteEvent(byte channel, byte note, double startTime, uint ticks)
                : base(startTime, ticks)
            {
                Channel = channel;
                Note = note;
            }

            public override uint ToMidiMessage()
            {
                byte code = (byte)(0x90 | Channel);

                return BitConverter.ToUInt32(new byte[4] { code, Note, 0x00, 0x00 }, 0);
            }
        }

        public class SetControllerValueEvent : Event
        {
            public byte Channel { get; } = 0;
            public byte Controller { get; } = 0;
            public byte Value { get; } = 0;

            public SetControllerValueEvent(byte channel, byte controller, byte value, double startTime, uint ticks)
                : base(startTime, ticks)
            {
                Channel = channel;
                Controller = controller;
                Value = value;
            }

            public override uint ToMidiMessage()
            {
                byte code = (byte)(0xB0 | Channel);

                return BitConverter.ToUInt32(new byte[4] { code, Controller, Value, 0x00 }, 0);
            }
        }

        public class SetTempoEvent : Event
        {
            public UInt32 MicroSecondsPerQuarterNote { get; } = 0;

            public SetTempoEvent(UInt32 microSecondsPerQuarterNote, double startTime, uint ticks)
                : base(startTime, ticks)
            {
                MicroSecondsPerQuarterNote = microSecondsPerQuarterNote;
            }

            public override uint ToMidiMessage()
            {
                throw new ExceptionAudio("Tempo event can not be converted to MIDI message.");
            }
        }

        public class SetInstrumentEvent : Event
        {
            public byte Channel { get; } = 0;
            public byte Instrument { get; } = 0;

            public SetInstrumentEvent(byte channel, byte instrument, double startTime, uint ticks)
                : base(startTime, ticks)
            {
                Channel = channel;
                Instrument = instrument;
            }

            public override uint ToMidiMessage()
            {
                byte code = (byte)(0xC0 | Channel);

                return BitConverter.ToUInt32(new byte[4] { code, Instrument, 0x00, 0x00 }, 0);
            }
        }
    }
}
