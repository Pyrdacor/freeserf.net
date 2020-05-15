using System.Collections.Generic;

namespace Freeserf.Audio
{
    public static class SFX
    {
        public static short[] ConvertToWav(Data.Buffer data, int level = 0, bool invert = false)
        {
            List<short> wavData = new List<short>();

            while (data.Readable())
            {
                int value = data.Pop<byte>();

                value += level;

                if (invert)
                {
                    value = 0xFF - value;
                }

                value *= 0xFF;

                wavData.Add((short)value);
            }

            return wavData.ToArray();
        }
    }
}
