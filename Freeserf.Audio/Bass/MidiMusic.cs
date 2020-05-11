using System;

namespace Freeserf.Audio.Bass
{
    internal class MidiMusic : Music
    {
        internal MidiMusic(XMI xmi)
           : base(LoadMidi(xmi))
        {

        }

        static uint GetControllerEventType(byte controller)
        {
            return controller switch
            {
                0 => 10,    // Bank select
                1 => 11,    // Modulation
                5 => 20,    // Portamento time
                7 => 12,    // Volume level
                10 => 13,   // Pan
                11 => 14,   // Expression
                64 => 15,   // Sustain
                65 => 19,   // Portamento
                66 => 76,   // Sostenuto
                67 => 60,   // Soft pedal
                71 => 26,   // Low-pass resonance
                72 => 27,   // Release time
                73 => 28,   // Attack time
                74 => 25,   // Low-pass cutoff
                84 => 21,   // Portamento start key
                91 => 23,   // Reverb
                93 => 24,   // Chorus
                _ => 42,    // Map everything else to user effect to ignore it.
            };
        }

        static int LoadMidi(XMI xmi)
        {
            const int MidiEnd = 0;
            const int MidiNote = 1;
            const int MidiProgram = 2;
            const int MidiTempo = 62;

            var events = new BassLib.MidiEvent[xmi.NumEvents + 1];

            for (int i = 0; i < xmi.NumEvents; ++i)
            {
                var evt = new BassLib.MidiEvent();
                var xmiEvent = xmi.GetEvent(i);

                evt.Ticks = xmiEvent.Ticks;

                if (xmiEvent is XMI.PlayNoteEvent)
                {
                    var playNoteEvent = xmiEvent as XMI.PlayNoteEvent;
                    evt.Event = MidiNote;
                    evt.Channel = playNoteEvent.Channel;
                    evt.Parameter = (uint)(playNoteEvent.Note | (playNoteEvent.Velocity << 8));
                }
                else if (xmiEvent is XMI.StopNoteEvent)
                {
                    var stopNoteEvent = xmiEvent as XMI.StopNoteEvent;
                    evt.Event = MidiNote;
                    evt.Channel = stopNoteEvent.Channel;
                    evt.Parameter = stopNoteEvent.Note;
                }
                else if (xmiEvent is XMI.SetInstrumentEvent)
                {
                    var setInstrumentEvent = xmiEvent as XMI.SetInstrumentEvent;
                    evt.Event = MidiProgram;
                    evt.Channel = setInstrumentEvent.Channel;
                    evt.Parameter = setInstrumentEvent.Instrument;
                }
                else if (xmiEvent is XMI.SetControllerValueEvent)
                {
                    var setControllerValueEvent = xmiEvent as XMI.SetControllerValueEvent;
                    evt.Event = GetControllerEventType(setControllerValueEvent.Controller);
                    evt.Channel = setControllerValueEvent.Channel;
                    evt.Parameter = setControllerValueEvent.Value;
                }
                else if (xmiEvent is XMI.SetTempoEvent)
                {
                    var setTempoEvent = xmiEvent as XMI.SetTempoEvent;
                    evt.Event = MidiTempo;
                    evt.Parameter = setTempoEvent.MicroSecondsPerQuarterNote;
                }

                events[i] = evt;
            }

            int ticksPerQuarterNote = 100;
            if (events[0].Event == MidiTempo)
            {
                // Parameter: tempo in microseconds per quarter note
                // Midi sound uses 120 Hz
                // ticksPerQuarterNote / quarterNoteTimeInSeconds = frequency
                // ticksPerQuarterNote = frequency * quarterNoteTimeInSeconds
                // ticksPerQuarterNote = 120 Hz * quarterNoteTimeInMicroseconds / 1.000.000
                ticksPerQuarterNote = (int)Math.Round(120 * events[0].Parameter / 1_000_000.0);
            }

            events[xmi.NumEvents] = new BassLib.MidiEvent()
            {
                Event = MidiEnd,
                Ticks = events[xmi.NumEvents - 1].Ticks + (uint)ticksPerQuarterNote * 5u, // Small pause between songs
            };

            return BassLib.LoadMidiMusic(events, ticksPerQuarterNote, 44100u);
        }

        protected override void FreeMusic(int channel)
        {
            BassLib.FreeMidiMusic(channel);
        }
    }
}
