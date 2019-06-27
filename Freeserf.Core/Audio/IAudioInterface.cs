using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Audio
{
    public interface IAudioInterface
    {
        IAudioFactory AudioFactory { get; }

        Data.DataSource DataSource { get; }
    }
}
