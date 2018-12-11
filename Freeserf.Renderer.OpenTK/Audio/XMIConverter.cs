using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.Renderer.OpenTK.Audio
{
    internal class XMIConverter
    {
        static readonly float[] Tones = null;

        static XMIConverter()
        {
            Tones = new float[128];

            int a = 440; // a is 440 hz...

            for (int x = 0; x < 128; ++x)
            {
                Tones[x] = a / 32.0f * (float)Math.Pow((x - 9) / 12, 2);
            }
        }

        class MIDIEvent
        {
            public double Start;
            public double Duration;
            public byte Note;
        }

        class Wave
        {
            public double Start;
            public double Duration;
            public List<short> Data = new List<short>();
        }

        class MIDIChannel
        {
            public List<MIDIEvent> Events = new List<MIDIEvent>();

            public short[] Transform()
            {
                List<Wave> waves = new List<Wave>();

                // PCM frequency (1/s)
                const double pcmFreq = 44100.0;
                const double dt = 1000.0 / pcmFreq; // time span in milliseconds of each pcm value
                const double pi2 = 2.0 * Math.PI;

                // create waves from events
                foreach (var ev in Events)
                {
                    double toneFreq = Tones[ev.Note] / 1000.0;

                    Wave wave = new Wave()
                    {
                        Start = ev.Start,
                        Duration = ev.Duration
                    };

                    double eventTime = 0.0;

                    while (eventTime < ev.Duration)
                    {
                        wave.Data.Add((short)(Math.Sin(pi2 * toneFreq * eventTime) * short.MaxValue));
                        eventTime += dt;
                    }

                    waves.Add(wave);
                }

                waves.Sort((a, b) => a.Start.CompareTo(b.Start));

                double endTime = 0.0;

                foreach (var wave in waves)
                {
                    if (wave.Start + wave.Duration > endTime)
                        endTime = wave.Start + wave.Duration;
                }

                short[] data = new short[Misc.Ceiling(endTime / dt)];

                // merge waves
                foreach (var wave in waves)
                {
                    int offset = Misc.Floor(wave.Start / dt);
                    int waveOffset = 0;

                    while (wave.Duration > 0.0)
                    {
                        data[offset] = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, data[offset++] + wave.Data[waveOffset++]));
                        wave.Duration -= dt;
                    }
                }

                return data;
            }
        }

        // default is 120bpm = 2 beats per second = each quarter note (beat) has 500ms time
        int timePerQuarterNote = 500;
        double currentTime = 0.0;

        // we convert directly to 44.1kHz stereo PCM data
        // there may be up to 16 midi channels
        public List<short[]> ConvertToPCM(Buffer data)
        {
            // Note: Chunk length and so on are encoded as big endian.
            // But as we don't use them we use little endian because
            // the XMI data is encoded in little endian.
            data.SetEndianess(Endian.Endianess.Little);

            // Form chunk
            if (data.ToString(4) != "FORM")
                return null;

            data.Pop(4); // FORM
            data.Pop<uint>(); // FORM chunk length

            // format XDIR
            if (data.ToString(4) != "XDIR")
                return null;

            data.Pop(4); // XDIR

            if (data.ToString(4) != "INFO")
                return null;

            data.Pop(4); // INFO
            data.Pop<uint>(); // INFO chunk length

            int numTracks = data.Pop<ushort>();

            if (numTracks != 1)
                return null; // we only support one track per file

            if (data.ToString(4) != "CAT ")
                return null;

            data.Pop(4); // CAT_
            data.Pop<uint>(); // CAT chunk length

            // format XMID
            if (data.ToString(4) != "XMID")
                return null;

            data.Pop(4); // XMID

            // load the one track

            // Form chunk
            if (data.ToString(4) != "FORM")
                return null;

            data.Pop(4); // FORM
            data.Pop<uint>(); // FORM chunk length

            // format XMID
            if (data.ToString(4) != "XMID")
                return null;

            data.Pop(4); // XMID

            // TIMB chunk
            if (data.ToString(4) != "TIMB")
                return null;

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
                return null;

            data.Pop(4); // EVNT
            data.Pop<uint>(); // EVNT chunk length

            // read xmi/midi events
            Dictionary<int, MIDIChannel> soundData = new Dictionary<int, MIDIChannel>();

            while (data.Readable())
            {
                ParseEvent(data, soundData);
            }

            return soundData.Select(d => d.Value.Transform()).ToList();
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

                uint time = (uint)high << 16 | (uint)mid << 8 | low;

                timePerQuarterNote = (int)time / 1000; // in milliseconds
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

        void ParseEvent(Buffer data, Dictionary<int, MIDIChannel> soundData)
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
                    // Note: In XMI it has 3 parameters
                    {
                        byte note = data.Pop<byte>();
                        byte velocity = data.Pop<byte>();
                        uint length = ParseDeltaTime(data);

                        if (velocity != 0)
                            ProcessNote(soundData, channel, note, length);
                    }
                    break;
                case 0x8:
                case 0xA:
                case 0xB:
                case 0xE:
                    data.Pop(2);
                    break;
                case 0xC:
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
            int ticksPerQuarternote = freq * timePerQuarterNote / 1000;

            double time = (double)ticks / (double)ticksPerQuarternote;

            return time * timePerQuarterNote;
        }

        void ProcessNote(Dictionary<int, MIDIChannel> soundData, int channel, byte note, uint duration)
        {
            double noteDuration = ConvertTicksToTime(duration);

            if (!soundData.ContainsKey(channel))
            {
                soundData.Add(channel, new MIDIChannel());
            }

            var ev = new MIDIEvent()
            {
                Start = currentTime,
                Duration = noteDuration,
                Note = note
            };

            soundData[channel].Events.Add(ev);
        }
    }
}
