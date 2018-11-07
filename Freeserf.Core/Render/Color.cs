/*
 * Color.cs - Basic color implementation
 *
 * Copyright (C) 2018  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net. freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with freeserf.net. If not, see <http://www.gnu.org/licenses/>.
 */

namespace Freeserf.Render
{
    public class Color
    {
        public float R;
        public float G;
        public float B;
        public float A;

        public Color()
        {
            R = 0.0f;
            G = 0.0f;
            B = 0.0f;
            A = 1.0f;
        }

        public Color(byte r, byte g, byte b, byte a = 255)
        {
            R = r / 255.0f;
            G = g / 255.0f;
            B = b / 255.0f;
            A = a / 255.0f;
        }

        public Color(int r, int g, int b, int a = 255)
            : this((byte)r, (byte)g, (byte)b, (byte)a)
        {

        }

        public Color(float r, float g, float b, float a = 1.0f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static readonly Color Transparent = new Color(0.0f, 0.0f, 0.0f, 0.0f);
    }
}
