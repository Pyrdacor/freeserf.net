using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.Audio
{
    public class MOD : Audio.ITrack
    {
        class Sample
        {
            public int Length;
            public int FineTune;
            public int Volume;
            public int RepeatPointOffset;
            public int RepeatLength;
            public byte[] Data;

            public void SetData(byte[] data)
            {
                Data = data;
            }

            public short GetData(int index)
            {
                if (Volume == 0)
                    return 0;

                double factor = Math.Min(1.0, Volume / 64.0);

                index %= Data.Length;

                return (short)Misc.Round(((sbyte)Data[index]) * 256.0 * factor);
            }
        }

        class Note
        {
            public Note(Data.Buffer data)
            {
                byte b1 = data.Pop<byte>();
                byte b2 = data.Pop<byte>();
                byte b3 = data.Pop<byte>();
                byte b4 = data.Pop<byte>();

                SampleNumber = (byte)((b1 & 0xf0) | (b3 >> 4));
                Period = b2 | ((b1 & 0x0f) << 8);
                EffectCommand = new Effect((uint)(b4 | ((b3 & 0x0f) << 8)));
            }

            public byte SampleNumber;
            public int Period;
            public Effect EffectCommand;
        }

        class Channel
        {
            readonly List<Note> notes = new List<Note>();
            int activeNoteIndex = 0;

            public void AddNote(Note note)
            {
                notes.Add(note);
            }

            public Note GetNextNote()
            {
                if (activeNoteIndex == NoteCount)
                    activeNoteIndex = 0;

                return notes[activeNoteIndex++];
            }

            public int NoteCount => notes.Count;
        }

        class Pattern
        {
            readonly Channel[] channels = new Channel[4]
            {
                new Channel(),
                new Channel(),
                new Channel(),
                new Channel()
            };

            public void AddNote(int channel, Note note)
            {
                channels[channel].AddNote(note);
            }

            public Note GetNextNote(int channel)
            {
                return channels[channel].GetNextNote();
            }

            public int GetNoteCount(int channel)
            {
                return channels[channel].NoteCount;
            }
        }

        enum Effects
        {
            NormalPlay = 0x00,
            SlideUp,
            SlideDown,
            TonePortamento,
            Vibrato,
            TonePortamentoPlusVolumeSlide,
            VibratoPlusVolumeSlide,
            Tremolo,
            Unused1,
            SetSampleOffset,
            VolumeSlide,
            PositionJump,
            SetVolume,
            PatternBreak,
            ECommand,
            SetSpeed,
            SetFilter = 0xE0,
            FineSlideUp,
            FineSlideDown,
            GlissandoControl,
            SetVibratoWaveform,
            SetLoop,
            JumpToLoop,
            SetTremoloWaveform,
            Unused2,
            RetrigNote,
            FineVolumeSlideUp,
            FineVolumeSlideDown,
            NoteCut,
            NoteDelay,
            PatternDelay,
            InvertLoop
        }

        readonly List<Pattern> song = new List<Pattern>();
        readonly List<Sample> samples = new List<Sample>();

        static int ConvertFineTune(int fineTune)
        {
            if (fineTune < 8)
                return fineTune;
            else
                return fineTune - 16;
        }

        public MOD(Data.Buffer data)
        {
            data.SetEndianess(Endian.Endianess.Big);

            data.Skip(20); // songname

            for (int i = 1; i < 32; ++i) // samples 1-31
            {
                data.Skip(22); // samplename

                samples.Add(new Sample()
                {
                    Length = data.Pop<UInt16>() * 2,
                    FineTune = ConvertFineTune(data.Pop<byte>() & 0xf),
                    Volume = data.Pop<byte>(), // 0-64, change in dB = 20*log10(Volume/64)
                    RepeatPointOffset = data.Pop<UInt16>() * 2,
                    RepeatLength = data.Pop<UInt16>() * 2
                });
            }

            int songLength = data.Pop<byte>();

            if (songLength < 1 || songLength > 128)
                throw new ExceptionAudio("Invalid MOD format.");

            data.Skip(1);

            byte[] songPatterns = data.Pop(128).ReinterpretAsArray(128);

            var magic = data.Pop(4).ToString();

            if (magic != "M!K!" && magic != "M.K.")
                throw new ExceptionAudio("Invalid or unsupported MOD format.");

            int numPatterns = songPatterns.Max() + 1;
            List<Pattern> patterns = new List<Pattern>(numPatterns);

            for (int i = 0; i < numPatterns; ++i)
            {
                var pattern = new Pattern();

                for (int div = 0; div < 64; ++div)
                {
                    for (int chan = 0; chan < 4; ++chan)
                    {
                        pattern.AddNote(chan, new Note(data));
                    }
                }

                patterns.Add(pattern);
            }

            for (int i = 0; i < samples.Count; ++i)
            {
                if (samples[i].Length > 0)
                {
                    data.Skip(2); // ignore tracker word
                    uint length = (uint)Math.Max(0, samples[i].Length - 2);
                    samples[i].SetData(data.Pop(length).ReinterpretAsArray(length));
                }
            }

            for (int i = 0; i < songLength; ++i)
            {
                song.Add(patterns[songPatterns[i]]);
            }
        }

        class Effect
        {
            public Effects Type { get; }
            public uint Param1 { get; }
            public uint Param2 { get; }

            public Effect(uint value)
            {
                Type = (Effects)(value >> 16);

                if (Type == Effects.ECommand)
                {
                    Type = Effects.SetFilter + (int)((value & 0xF0) >> 8);
                    Param1 = 0;
                }
                else
                {
                    Param1 = (value & 0xF0) >> 8;
                }

                Param2 = value & 0x0F;
            }
        }

        public short[] ConvertToWav()
        {
            var waveData = new List<short>();

            const double tickInterval = 20.0;
            const double clockRate = 7093789.2; // Hz (PAL), NTSC would be 7159090.5 Hz
            const double halfClockRate = 0.5 * clockRate;

            int[] activeSamples = new int[4] { 0, 0, 0, 0 };
            int[] activePeriods = new int[4] { 0, 0, 0, 0 };
            double[] activeSampleTimes = new double[4] { 0.0, 0.0, 0.0, 0.0 };
            List<short>[] channelData = new List<short>[4] { new List<short>(), new List<short>(), new List<short>(), new List<short>() };

            foreach (var pattern in song)
            {
                for (int r = 0; r < 64; ++r)
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        var note = pattern.GetNextNote(i);
                        int sample = (note.SampleNumber == 0) ? activeSamples[i] : note.SampleNumber;

                        int pitchPeriod = (note.Period == 0) ? activePeriods[i] : note.Period;

                        double dataRatePerSecond = halfClockRate / pitchPeriod;
                        double currentSampleTime = (sample == activeSamples[i]) ? activeSampleTimes[i] : 0.0;

                        // a tick is 20 ms
                        int bytesPerTick = (int)Math.Floor(dataRatePerSecond / 50.0);
                        int currentTick = (int)Math.Floor(currentSampleTime / tickInterval);
                        int startbyte = currentTick * bytesPerTick;

                        int multiplier = Misc.Round(44100.0 / dataRatePerSecond);

                        for (int n = 0; n < bytesPerTick; ++n)
                        {
                            for (int m = 0; m < multiplier; ++m)
                            {
                                if (samples[sample].Length <= 2)
                                    channelData[i].Add(0);
                                else
                                    channelData[i].Add(samples[sample].GetData(startbyte + n));
                            }
                        }

                        activeSamples[i] = sample;
                        activePeriods[i] = pitchPeriod;
                        activeSampleTimes[i] += tickInterval; // milliseconds
                    }
                }
            }

            int maxLength = channelData.Max(c => c.Count);

            for (int i = 0; i < 4; ++i)
            {
                if (channelData[i].Count < maxLength)
                {
                    channelData[i].AddRange(new short[maxLength - channelData[i].Count]);
                }
            }

            for (int index = 0; index < maxLength; ++index)
            {
                for (int i = 0; i < 4; ++i)
                {
                    waveData.Add(channelData[i][index]);
                }
            }

            return waveData.ToArray();
        }

        public void Play(Audio.Player player)
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
