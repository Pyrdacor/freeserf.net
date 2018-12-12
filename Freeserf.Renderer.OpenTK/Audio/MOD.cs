using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.Renderer.OpenTK.Audio
{
    internal class MOD
    {
        struct Sample
        {
            public int Length;
            public int FineTune;
            public int Volume;
            public int RepeatPointOffset;
            public int RepeatLength;
        }

        class Note
        {
            public Note(Buffer data)
            {
                byte b1 = data.Pop<byte>();
                byte b2 = data.Pop<byte>();
                byte b3 = data.Pop<byte>();
                byte b4 = data.Pop<byte>();

                SampleNumber = (byte)((b1 & 0xf0) | (b3 >> 4));
                Period = b2 | ((b1 & 0x0f) << 8);
                EffectCommand = b4 | ((b3 & 0x0f) << 8);
            }

            public byte SampleNumber;
            public int Period;
            public int EffectCommand;
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
            readonly Channel[] channels = new Channel[4];

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

        public MOD(Buffer data)
        {
            data.SetEndianess(Endian.Endianess.Big);

            data.Pop(20); // songname

            for (int i = 1; i < 32; ++i) // samples 1-31
            {
                data.Pop(22); // samplename

                samples.Add(new Sample()
                {
                    Length = data.Pop<UInt16>() * 2,
                    FineTune = ConvertFineTune(data.Pop<byte>() & 0xf),
                    Volume = data.Pop<byte>(), // 0-64
                    RepeatPointOffset = data.Pop<UInt16>() * 2,
                    RepeatLength = data.Pop<UInt16>() * 2
                });
            }

            int songLength = data.Pop<byte>();

            if (songLength > 128)
                throw new ExceptionAudio("Invalid MOD format.");

            data.Pop(1);

            byte[] songPatterns = data.Pop(128).ReinterpretAsArray(128);

            if (data.Pop(4).ToString() != "M!K!")
                throw new ExceptionAudio("Invalid MOD format.");

            int numPatterns = songPatterns.Max();
            List<Pattern> patterns = new List<Pattern>(numPatterns);

            for (int i = 0; i < numPatterns; ++i)
            {
                var pattern = new Pattern();

                for (int n = 0; n < 256; ++n)
                {
                    pattern.AddNote(n % 4, new Note(data));
                }

                patterns.Add(pattern);
            }

            for (int i = 0; i < songLength; ++i)
            {
                song.Add(patterns[songPatterns[i]]);
            }
        }

        static readonly int[] NoteConversionTable = new int[]
        {
            /*
            +-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+
            I    1I    2I    3I    4I    5I    6I    7I    8I    9I   10I   11I   12I
            I 1712I 1616I 1524I 1440I 1356I 1280I 1208I 1140I 1076I 1016I  960I  906I
            I  C-0I  C#0I  D-0I  D#0I  E-0I  F-0I  F#0I  G-0I  G#0I  A-0I  A#0I  B-0I
            +-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+
            I   13I   14I   15I   16I   17I   18I   19I   20I   21I   22I   23I   24I
            I  856I  808I  762I  720I  678I  640I  604I  570I  538I  508I  480I  453I
            I  C-1I  C#1I  D-1I  D#1I  E-1I  F-1I  F#1I  G-1I  G#1I  A-1I  A#1I  B-1I
            +-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+
            I   25I   26I   27I   28I   29I   30I   31I   32I   33I   34I   35I   36I
            I  428I  404I  381I  360I  339I  320I  302I  285I  269I  254I  240I  226I
            I  C-2I  C#2I  D-2I  D#2I  E-2I  F-2I  F#2I  G-2I  G#2I  A-2I  A#2I  B-2I
            +-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+
            I   37I   38I   39I   40I   41I   42I   43I   44I   45I   46I   47I   48I
            I  214I  202I  190I  180I  170I  160I  151I  143I  135I  127I  120I  113I
            I  C-3I  C#3I  D-3I  D#3I  E-3I  F-3I  F#3I  G-3I  G#3I  A-3I  A#3I  B-3I
            +-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+
            I   49I   50I   51I   52I   53I   54I   55I   56I   57I   58I   59I   60I
            I  107I  101I   95I   90I   85I   80I   75I   71I   67I   63I   60I   56I
            I  C-4I  C#4I  D-4I  D#4I  E-4I  F-4I  F#4I  G-4I  G#4I  A-4I  A#4I  B-4I
            +-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+
            */
            1712, 1616, 1524, 1440, 1356, 1280, 1208, 1140, 1076, 1016,  960,  906,
             856,  808,  762,  720,  678,  640,  604,  570,  538,  508,  480,  453,
             428,  404,  381,  360,  339,  320,  302,  285,  269,  254,  240,  226,
             214,  202,  190,  180,  170,  160,  151,  143,  135,  127,  120,  113,
             107,  101,   95,   90,   85,   80,   75,   71,   67,   63,   60,   56
        };

        static int GetMidiNoteIndex(Note note)
        {
            for (int i = NoteConversionTable.Length - 1; i >= 0; --i)
            {
                if (note.Period >= NoteConversionTable[i])
                    return 72 + i; // the midi C-5 is the mod C-0
            }

            return -1;
        }

        public void FillXMIEvents(List<XMI.Event> events)
        {
            events.Clear();

            double currentTime = 0.0;
            const double interval = 20.0;

            int[] activeNotes = new int[4];

            foreach (var pattern in song)
            {
                for (int i = 0; i < 4; ++i)
                {
                    var note = pattern.GetNextNote(i);
                    int noteIndex = GetMidiNoteIndex(note);

                    if (noteIndex == activeNotes[i])
                        continue;

                    if (noteIndex == -1) // change to off
                    {
                        events.Add(new XMI.StopNoteEvent((byte)i, (byte)activeNotes[i], currentTime));
                    }
                    else if (activeNotes[i] != -1) // changed note
                    {
                        events.Add(new XMI.StopNoteEvent((byte)i, (byte)activeNotes[i], currentTime));
                        events.Add(new XMI.PlayNoteEvent((byte)i, (byte)noteIndex, currentTime));
                    }
                    else // change to on
                    {
                        events.Add(new XMI.PlayNoteEvent((byte)i, (byte)noteIndex, currentTime));
                    }

                    activeNotes[i] = noteIndex;
                }

                currentTime += interval;
            }
        }
    }
}
