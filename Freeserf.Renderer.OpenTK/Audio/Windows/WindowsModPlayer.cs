using System;
using Freeserf.Audio;
using Freeserf.Data;

namespace Freeserf.Renderer.OpenTK.Audio.Windows
{
    internal class WindowsModPlayerFactory : IModPlayerFactory
    {
        public WindowsModPlayerFactory(DataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        DataSource dataSource = null;
        static IModPlayer player = null;

        public IModPlayer GetModPlayer()
        {
            if (player == null)
                player = new WindowsModPlayer(dataSource);

            return player;
        }
    }

    internal class WindowsModPlayer : WindowsWavePlayer, IModPlayer
    {
        public WindowsModPlayer(DataSource dataSource)
            : base(dataSource)
        {

        }

        public bool Paused => throw new NotImplementedException();

        public bool Looped => throw new NotImplementedException();

        public void Play(MOD mod, bool looped)
        {
            throw new NotImplementedException();
        }

        protected override Audio.ITrack CreateTrack(int trackID)
        {
            return new WaveTrack(new MOD(dataSource.GetMusic((uint)trackID)).ConvertToWav());
        }
    }
}
