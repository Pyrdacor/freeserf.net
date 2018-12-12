using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Renderer.OpenTK.Audio
{
    internal class XMI : Audio.ITrack, IEnumerable<XMI.Event>
    {
        uint tempo = 500000;
        readonly List<Event> events = new List<Event>();
        double currentTime = 0.0;

        public int NumEvents => events.Count;

        public Event GetEvent(int index)
        {
            return events[index];
        }

        public XMI(Buffer data)
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

            events.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
        }

        public void Play(Audio.Player player)
        {
            if (player is IMidiPlayer)
                (player as IMidiPlayer).Play(this, true);
            // TODO: sfx
        }

        uint ParseDeltaTime(Buffer data)
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

        void ParseMetaEvent(Buffer data)
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

                Log.Verbose.Write("audio", $"XMI Tempo Changed: {tempo} microseconds per quarter note.");
            }
            else if (type == 0x58)
            {
                if (len != 4)
                    throw new ExceptionAudio("Invalid event length.");

                int numerator = data.Pop<byte>();
                int denominator = 1 << data.Pop<byte>();
                int numMidiClocks = data.Pop<byte>();
                int num32InQuarterNote = data.Pop<byte>();

                Log.Verbose.Write("audio", $"XMI Time Signature: {numerator}/{denominator} ({numMidiClocks} MIDI Clocks, {num32InQuarterNote} 32nd-notes in quarter note.");
            }
            else
            {
                // skip event data
                data.Pop(len);
            }
        }

        void ParseEvent(Buffer data)
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
                currentTime += ConvertTicksToTime(data.Pop<byte>());
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
                            var onEvent = new PlayNoteEvent((byte)channel, note, currentTime);
                            var offEvent = new StopNoteEvent((byte)channel, note, currentTime + ConvertTicksToTime(length));

                            events.Add(onEvent);
                            events.Add(offEvent);
                        }
                    }
                    break;
                case 0x8:
                case 0xA:
                case 0xB:
                case 0xE:
                    data.Pop(2);
                    break;
                case 0xC:
                    {
                        var patchEvent = new SetInstrumentEvent((byte)channel, data.Pop<byte>(), currentTime);

                        events.Add(patchEvent);
                    }
                    break;
                case 0xD:
                    data.Pop(1);
                    break;
                default:
                    throw new ExceptionAudio("Unsupported xmi/midi event type.");
            }
        }

        // in milliseconds
        double ConvertTicksToTime(uint ticks)
        {
            // ticks_per_quarter_note / quarternote_time_in_seconds = freq
            const int freq = 120; // ticks per second
            double ticksPerQuarternote = Math.Floor(freq * tempo / 1000000.0);

            double time = (double)ticks / (double)ticksPerQuarternote;

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

            protected Event(double startTime)
            {
                StartTime = startTime;
            }

            public abstract uint ToMidiMessage();
        }

        public class PlayNoteEvent : Event
        {
            byte channel = 0;
            byte note = 0;

            public PlayNoteEvent(byte channel, byte note, double startTime)
                : base(startTime)
            {
                this.channel = channel;
                this.note = note;
            }

            public override uint ToMidiMessage()
            {
                byte code = (byte)(0x90 | channel);

                return BitConverter.ToUInt32(new byte[4] { code, note, 0x40, 0x00 }, 0);
            }
        }

        public class StopNoteEvent : Event
        {
            byte channel = 0;
            byte note = 0;

            public StopNoteEvent(byte channel, byte note, double startTime)
                : base(startTime)
            {
                this.channel = channel;
                this.note = note;
            }

            public override uint ToMidiMessage()
            {
                byte code = (byte)(0x90 | channel);

                return BitConverter.ToUInt32(new byte[4] { code, note, 0x00, 0x00 }, 0);
            }
        }

        public class SetInstrumentEvent : Event
        {
            byte channel = 0;
            byte instrument = 0;

            public SetInstrumentEvent(byte channel, byte instrument, double startTime)
                : base(startTime)
            {
                this.channel = channel;
                this.instrument = instrument;
            }

            public override uint ToMidiMessage()
            {
                byte code = (byte)(0xC0 | channel);

                return BitConverter.ToUInt32(new byte[4] { code, instrument, 0x00, 0x00 }, 0);
            }
        }
    }
}
