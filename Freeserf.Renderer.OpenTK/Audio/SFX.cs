using System.Collections.Generic;

namespace Freeserf.Renderer.OpenTK.Audio
{
    class SFX
    {
        public static short[] ConvertToWav(Buffer data, int level = 0, bool invert = false)
        {
            List<short> wavData = new List<short>();

            while (data.Readable())
            {
                int value = data.Pop<byte>();

                value = value + level;

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
