using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    public class AudioDummy : Audio
    {
        public override VolumeController GetVolumeController()
        {
            return null;
        }

        public override Player GetSoundPlayer()
        {
            return null;
        }

        public override Player GetMusicPlayer()
        {
            return null;
        }
    }
}
