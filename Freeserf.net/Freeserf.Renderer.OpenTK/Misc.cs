using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Renderer.OpenTK
{
    internal static class Misc
    {
        public static bool FloatEqual(float f1, float f2)
        {
            return Math.Abs(f1 - f2) < 0.00001f;
        }

        public static int Round(float f)
        {
            return (int)Math.Round(f);
        }

        public static int Round(double d)
        {
            return (int)Math.Round(d);
        }

        public static float Min(float firstValue, float secondValue, params float[] values)
        {
            float min = Math.Min(firstValue, secondValue);

            foreach (var value in values)
            {
                if (value < min)
                    min = value;
            }

            return min;
        }

        public static float Max(float firstValue, float secondValue, params float[] values)
        {
            float max = Math.Max(firstValue, secondValue);

            foreach (var value in values)
            {
                if (value > max)
                    max = value;
            }

            return max;
        }
    }
}
