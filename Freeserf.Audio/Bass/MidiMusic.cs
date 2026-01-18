using ManagedBass.Midi;
using System;

namespace Freeserf.Audio.Bass
{
    internal class MidiMusic : Music
    {
        internal MidiMusic(XMI xmi) : base(LoadMidi(xmi))
        {
        }

        static MidiEventType GetControllerEventType(byte controller)
        {
            return controller switch
            {
                // Standard GM controllers with dedicated event types
                0 => MidiEventType.Bank,             // Bank Select MSB
                1 => MidiEventType.Modulation,       // Modulation
                5 => MidiEventType.PortamentoTime,   // Portamento Time
                7 => MidiEventType.Volume,           // Channel Volume
                10 => MidiEventType.Pan,              // Pan
                11 => MidiEventType.Expression,       // Expression
                64 => MidiEventType.Sustain,          // Sustain Pedal
                65 => MidiEventType.Portamento,       // Portamento Switch
                66 => MidiEventType.Sostenuto,        // Sostenuto Pedal
                67 => MidiEventType.Soft,             // Soft Pedal
                71 => MidiEventType.Resonance,        // Filter Resonance
                72 => MidiEventType.Release,          // Release Time
                73 => MidiEventType.Attack,           // Attack Time
                74 => MidiEventType.CutOff,           // Filter Cutoff
                84 => MidiEventType.PortamentoNote,   // Portamento Control Key
                91 => MidiEventType.Reverb,           // Reverb Send
                93 => MidiEventType.Chorus,           // Chorus Send

                // Special GM controllers with dedicated event types
                120 => MidiEventType.SoundOff,         // All Sound Off
                121 => MidiEventType.Reset,            // Reset All Controllers
                123 => MidiEventType.NotesOff,         // All Notes Off
                126 => MidiEventType.Mode,             // Mono Mode
                127 => MidiEventType.Mode,             // Poly Mode

                // Everything else is a generic controller event
                _ => MidiEventType.Control
            };
        }

        static int LoadMidi(XMI xmi)
        {
            var events = new ManagedBass.Midi.MidiEvent[xmi.NumEvents + 1];

            for (int i = 0; i < xmi.NumEvents; ++i)
            {
                var evt = new ManagedBass.Midi.MidiEvent();
                var xmiEvent = xmi.GetEvent(i);

                evt.Ticks = (int)xmiEvent.Ticks;

                if (xmiEvent is XMI.PlayNoteEvent)
                {
                    var playNoteEvent = xmiEvent as XMI.PlayNoteEvent;
                    evt.EventType = MidiEventType.Note;
                    evt.Channel = playNoteEvent.Channel;
                    evt.Parameter = (playNoteEvent.Note | (playNoteEvent.Velocity << 8));
                }
                else if (xmiEvent is XMI.StopNoteEvent)
                {
                    var stopNoteEvent = xmiEvent as XMI.StopNoteEvent;
                    evt.EventType = MidiEventType.Note;
                    evt.Channel = stopNoteEvent.Channel;
                    evt.Parameter = stopNoteEvent.Note;
                }
                else if (xmiEvent is XMI.SetInstrumentEvent)
                {
                    var setInstrumentEvent = xmiEvent as XMI.SetInstrumentEvent;
                    evt.EventType = MidiEventType.Program;
                    evt.Channel = setInstrumentEvent.Channel;
                    evt.Parameter = setInstrumentEvent.Instrument;
                }
                else if (xmiEvent is XMI.SetControllerValueEvent)
                {
                    var setControllerValueEvent = xmiEvent as XMI.SetControllerValueEvent;
                    evt.EventType = GetControllerEventType(setControllerValueEvent.Controller);
                    evt.Channel = setControllerValueEvent.Channel;
                    evt.Parameter = setControllerValueEvent.Value;
                }
                else if (xmiEvent is XMI.SetTempoEvent)
                {
                    var setTempoEvent = xmiEvent as XMI.SetTempoEvent;
                    evt.EventType = MidiEventType.Tempo;
                    evt.Parameter = (int)setTempoEvent.MicroSecondsPerQuarterNote;
                }

                events[i] = evt;
            }

            int ticksPerQuarterNote = 100;
            if (events[0].EventType == MidiEventType.Tempo)
            {
                // Parameter: tempo in microseconds per quarter note
                // Midi sound uses 120 Hz
                // ticksPerQuarterNote / quarterNoteTimeInSeconds = frequency
                // ticksPerQuarterNote = frequency * quarterNoteTimeInSeconds
                // ticksPerQuarterNote = 120 Hz * quarterNoteTimeInMicroseconds / 1.000.000
                ticksPerQuarterNote = (int)Math.Round(120 * events[0].Parameter / 1_000_000.0);
            }

            events[xmi.NumEvents] = new ManagedBass.Midi.MidiEvent()
            {
                EventType = MidiEventType.End,
                Ticks = (int)(events[xmi.NumEvents - 1].Ticks + (uint)ticksPerQuarterNote * 5u), // Small pause between songs
            };


            return BassLib.LoadMidiMusic(events, ticksPerQuarterNote, 44100u);
        }

        protected override void FreeMusic(int channel)
        {
            BassLib.FreeMidiMusic(channel);
        }
    }
}
