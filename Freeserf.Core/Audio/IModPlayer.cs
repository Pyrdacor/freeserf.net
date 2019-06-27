namespace Freeserf.Audio
{
    public interface IModPlayer
    {
        bool Available { get; }
        bool Enabled { get; set; }
        bool Paused { get; }
        bool Running { get; }
        bool Looped { get; }

        void Play(MOD mod, bool looped);
        void Stop();
        void Pause();
        void Resume();
    }

    public interface IModPlayerFactory
    {
        IModPlayer GetModPlayer();
    }
}
