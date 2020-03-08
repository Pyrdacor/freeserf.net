using Freeserf.Data;

namespace Freeserf.Audio.Bass
{
    internal class MidiPlayer : Audio.Player
    {
        public MidiPlayer(DataSource dataSource)
        {
            // TODO
        }

        // TODO
        public override bool Enabled { get => false; set { } }

        public override Audio.IVolumeController GetVolumeController()
        {
            // TODO
            return null;
        }

        public override void Stop()
        {
            // TODO
        }

        protected override Audio.ITrack CreateTrack(int trackID)
        {
            // TODO
            return null;
        }
    }
}
