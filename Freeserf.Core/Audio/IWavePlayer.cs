namespace Freeserf.Audio
{
    public interface IWavePlayer
    {
        bool Available { get; }
        bool Enabled { get; set; }
        bool Running { get; }

        void Play(short[] data);
        void Stop();
    }

    public interface IWavePlayerFactory
    {
        IWavePlayer GetWavePlayer();
    }
}
