namespace Freeserf.Audio.Linux
{
    internal class WavePlayer : IWavePlayer
    {
        public bool Available => false;

        public bool Enabled { get => false; set { /* TODO: implement */ } }

        public bool Running => false;

        public void Play(short[] data)
        {
            // TODO: implement
        }

        public void Stop()
        {
            // TODO: implement
        }
    }
}
