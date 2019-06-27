namespace Freeserf.Audio
{
    public interface IMidiPlayer
    {
        bool Available { get; }
        bool Enabled { get; set; }
        bool Paused { get; }
        bool Running { get; }
        bool Looped { get; }
        XMI CurrentXMI { get; }

        void Play(XMI xmi, bool looped);
        void Stop();
        void Pause();
        void Resume();
    }

    public interface IMidiPlayerFactory
    {
        IMidiPlayer GetMidiPlayer();
    }
}
