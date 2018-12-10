using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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

        int timePerQuarterNote = 0;

        // we convert directly to 44.1kHz stereo PCM data
        public short[] ConvertToPCM(Buffer data)
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
            List<short> soundData = new List<short>();

            while (data.Readable())
            {
                ParseEvent(data, soundData);
            }

            return soundData.ToArray();
        }

        uint ParseDeltaTime(Buffer data)
        {
            uint deltaTime = 0;

            while (data.Readable())
            {
                byte b = data.PeekByte();

                if ((b & 0x80) != 0)
                    return deltaTime;

                deltaTime += b;

                data.Pop(1);
            }

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
            else
            {
                // skip event data
                data.Pop(len);
            }
        }

        void ParseEvent(Buffer data, List<short> soundData)
        {
            var deltaTime = ParseDeltaTime(data);
            byte status = data.Pop<byte>();

            if (status == 0xff) // meta event
            {
                ParseMetaEvent(data);
                return;
            }

            int channel = status & 0xf;
            int eventType = status >> 4;

            if (eventType < 0x8 || eventType > 0xE)
                throw new ExceptionAudio("Invalid MIDI event.");

            if (timePerQuarterNote == 0) // default is 120bpm = 2 beats per second = each quarter note (beat) has 500ms time
                timePerQuarterNote = 500;

            switch (eventType)
            {
                case 0x9: // Note on
                    // Note: In XMI it has 3 parameters
                    {
                        byte note = data.Pop<byte>();
                        byte velocity = data.Pop<byte>();
                        uint length = ParseDeltaTime(data);

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

        void ProcessNote(List<short> soundData, int channel, byte note, uint duration)
        {
            // ticks_per_quarter_note / quarternote_time_in_seconds = freq
            const int freq = 120; // ticks per second
            int ticksPerQuarternote = freq * timePerQuarterNote / 1000;

            // PCM frequency (1/s)
            const double pcmFreq = 44100.0;
            const double dt = 1000.0 / pcmFreq; // time span in milliseconds of each pcm value

            double noteDuration = (double)duration / (double)ticksPerQuarternote; // in number of quarter notes / beats
            noteDuration *= timePerQuarterNote; // now we have the time in milliseconds

            const double pi2 = 2.0 * Math.PI;
            double time = 0.0;
            double toneFreq = Tones[note];

            while (time < noteDuration)
            {
                short value = (short)(Math.Sin(pi2 * toneFreq * time / 1000.0) * short.MaxValue);

                soundData.Add(value);

                time += dt;
            }
        }
    }
}
