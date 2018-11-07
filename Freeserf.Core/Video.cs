/*
 * Video.cs - Basic gaphics rendering interface
 *
 * Copyright (C) 2013-2015  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2015       Wicked_Digger <wicked_digger@mail.ru>
 * Copyright (C) 2018       Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    public class ExceptionVideo : ExceptionFreeserf 
    {
        public ExceptionVideo(string description)
            : base(description)
        {

        }

        public virtual string Platform => "Abstract";

        public override string System => "video";

        public override string Message => "[" + System + ":" + Platform + "]" + Description;
    }

    public interface IVideoFactory
    {
        IVideo GetInstance();
    }

    public class Frame
    {
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
        public uint Width { get; set; } = 0;
        public uint Height { get; set; } = 0;
    }

    public interface IImage
    {
        uint Width { get; }
        uint Height { get; }
        Texture Texture { get; }
    }

    public interface IVideo
    {
        IVideoFactory Factory { get; }

        void SetResolution(uint width, uint height, bool fullscreen);
        void GetResolution(out uint width, out uint height);
        void SetFullscreen(bool enable);
        bool IsFullscreen();

        Frame GetScreenFrame();
        Frame CreateFrame(uint width, uint height);
        void DestroyFrame(Frame frame);

        IImage CreateImage(byte[] data, uint width, uint height);
        void DestroyImage(IImage image);

        void WarpMouse(int x, int y);

        void DrawImage(IImage image, int x, int y, int y_offset, Frame dest);
        void DrawFrame(int dx, int dy, Frame dest, int sx, int sy, Frame src, int w, int h);
        void DrawRect(int x, int y, uint width, uint height, Render.Color color, Frame dest);
        void FillRect(int x, int y, uint width, uint height, Render.Color color, Frame dest);
        void DrawLine(int x, int y, int x1, int y1, Render.Color color, Frame dest);

        void SwapBuffers();

        void SetCursor(byte[] data, uint width, uint height);

        float GetZoomFactor();
        bool SetZoomFactor(float factor);
        void GetScreenFactor(out float fx, out float fy);
    }
}
