namespace Freeserf.Renderer.OpenTK.Audio
{
    internal interface IWavePlayer
    {
        bool Available { get; }
        bool Enabled { get; set; }
        bool Running { get; }

        void Play(short[] data);
        void Stop();
    }

    internal interface IWavePlayerFactory
    {
        IWavePlayer GetWavePlayer();
    }
}
