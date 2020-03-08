using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.Audio
{
    public class MOD
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

                if ((RepeatPointOffset != 0 || RepeatLength != 0) && index >= /*Data.Length*/RepeatPointOffset + RepeatLength)
                {
                    //if (RepeatLength > 0)
                    {
                        index -= Data.Length;
                        index %= RepeatLength;
                        index += RepeatPointOffset;
                    }
                }
                else if (index >= Data.Length)
                {
                    return 0;
                }
                //}

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

            Log.Info.Write(ErrorSystemType.Data, $"Loading MOD song: {System.Text.Encoding.ASCII.GetString(data.Pop(20).ReinterpretAsArray(20))}"); // songname

            // add dummy sample 0
            samples.Add(new Sample());

            for (int i = 1; i < 32; ++i) // samples 1-31
            {
                Log.Info.Write(ErrorSystemType.Data, $"Sample {i} name: {System.Text.Encoding.ASCII.GetString(data.Pop(22).ReinterpretAsArray(22))}"); // samplename

                samples.Add(new Sample()
                {
                    Length = data.Pop<UInt16>() * 2,
                    FineTune = ConvertFineTune(data.Pop<byte>() & 0xf),
                    Volume = data.Pop<byte>(), // 0-64, change in dB = 20*log10(Volume/64)
                    RepeatPointOffset = data.Pop<UInt16>() * 2,
                    RepeatLength = Math.Max(0, data.Pop<UInt16>() - 1) * 2
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
                    samples[i].Length -= 2;

                    uint length = (uint)samples[i].Length;

                    if (length > 0)
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
            double[] finetuneTable = new double[16];

            for (int t = 0; t < 16; t++)
                finetuneTable[t] = Math.Pow(2.0, (t - 8) / 12.0 / 8.0);

            var waveData = new List<short>();

            // Default values: 125 bpm, 6 ticks/division
            // divisions / minute = (24*bpm) / (ticks/division)
            // 500 divisions / minute -> 8.33 divisions / second -> 120ms per division
            const double timePerDivision = 120.0 + 60.0; // TODO: with an offset of 60 it sounds better
            const double clockRate = 7093789.2; // Hz (PAL), NTSC would be 7159090.5 Hz
            const double halfClockRate = 0.5 * clockRate;

            int[] activeSamples = new int[4] { 0, 0, 0, 0 };
            int[] activePeriods = new int[4] { 214, 214, 214, 214 };
            double[] activeSampleTimes = new double[4] { 0.0, 0.0, 0.0, 0.0 };
            List<short>[] channelData = new List<short>[4] { new List<short>(), new List<short>(), new List<short>(), new List<short>() };

            foreach (var pattern in song)
            {
                for (int i = 0; i < 4; ++i)
                {
                    for (int r = 0; r < 48; ++r) // TODO: should be 64 but works a bit better with 48 (but not the whole time)
                    {
                        if (r == 0)
                        {
                            activePeriods[i] = 214;
                            activeSamples[i] = 0;
                            activeSampleTimes[i] = 0.0;
                        }

                        var note = pattern.GetNextNote(i);
                        int sample = (note.SampleNumber == 0) ? activeSamples[i] : note.SampleNumber;

                        if (sample < 0 || sample > 31)
                            throw new ExceptionFreeserf($"Invalid sample number {sample} in MOD");

                        int pitchPeriod = (note.Period == 0) ? activePeriods[i] : note.Period;
                        double dataRatePerSecond = halfClockRate / pitchPeriod;
                        double sampleRate = dataRatePerSecond * finetuneTable[samples[sample].FineTune + 8] / 44100.0; // playback with 44.1kHz
                        double currentSampleTime = (sample == activeSamples[i]) ? activeSampleTimes[i] : 0.0;
                        double bytesPerDivision = dataRatePerSecond * finetuneTable[samples[sample].FineTune + 8] / (1000.0 / timePerDivision);

                        if (sample != activeSamples[i] || note.Period != 0)
                        {
                            activeSampleTimes[i] = 0.0;
                            currentSampleTime = 0.0;
                        }

                        if (note.SampleNumber != 0 && note.Period == 0 && currentSampleTime >= samples[note.SampleNumber].Length)
                        {
                            activeSampleTimes[i] = 0.0;
                            currentSampleTime = 0.0;
                        }

                        double target = currentSampleTime + bytesPerDivision;

                        while (currentSampleTime < target)
                        {
                            short data = 0;

                            if (samples[sample].Length == 0 || samples[sample].Volume == 0)
                                data = 0;
                            else
                                data = samples[sample].GetData((int)Math.Floor(currentSampleTime));

                            channelData[i].Add(data);

                            activeSampleTimes[i] += sampleRate;
                            currentSampleTime += sampleRate;
                        }

                        activeSamples[i] = sample;
                        activePeriods[i] = pitchPeriod;
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
                waveData.Add((short)((channelData[0][index] + channelData[1][index] + channelData[2][index] + channelData[3][index]) / 4));
            }

            return waveData.ToArray();
        }
    }
}
