using Freeserf.Data;

namespace Freeserf.Audio.Linux
{
    internal class MidiPlayer : IMidiPlayer
    {
        public MidiPlayer(DataSource dataSource)
        {
            // TODO: implement
        }

        public bool Available => false;

        public bool Enabled { get => false; set { /* TODO: implement */ } }

        public bool Paused => false;

        public bool Running => false;

        public bool Looped => false;

        public XMI CurrentXMI => null;

        public void Pause()
        {
            // TODO: implement
        }

        public void Play(XMI xmi, bool looped)
        {
            // TODO: implement
        }

        public void Resume()
        {
            // TODO: implement
        }

        public void Stop()
        {
            // TODO: implement
        }
    }
}
