/*
 * TextInput.cs - Text input GUI component
 *
 * Copyright (C) 2015  Wicked_Digger <wicked_digger@mail.ru>
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

namespace Freeserf
{
    internal class TextInput : GuiObject
    {
        public delegate bool Filter(char key, TextInput textInput);

        string text = "";
        Filter filter = null;
        Render.Color color_focus = new Render.Color(0x00, 0x8b, 0x47);
        Render.Color color_text = Render.Color.Green;
        Render.Color color_background = Render.Color.Black;
        bool draw_focus = true;
        Render.IColoredRect background = null;
        
        public TextInput(Interface interf)
        {
            background = interf.RenderView.ColoredRectFactory.Create(0, 0, color_background);
        }

        public string Text
        {
            get => text;
            set
            {
                if (text != value)
                {
                    text = value;
                    SetRedraw();
                }
            }
        }

        public int MaxLength
        {
            get;
            set;
        } = 0;

        protected override void Layout()
        {
            base.Layout();

            background.Resize(Width, Height);
        }

        protected override void InternalDraw()
        {
            frame->fill_rect(0, 0, width, height, color_background);
            if (draw_focus && focused)
            {
                frame->draw_rect(0, 0, width, height, color_focus);
            }
            int ch_width = width / 8;
            std::string str = text;
            int cx = 0;
            int cy = 0;
            if (draw_focus)
            {
                cx = 1;
                cy = 1;
            }
            while (str.length())
            {
                std::string substr = str.substr(0, ch_width);
                str.erase(0, ch_width);
                frame->draw_string(cx, cy, substr, color_text);
                cy += 8;
            }
        }

        public void SetFilter(Filter filter)
        {
            this.filter = filter;
        }

        protected virtual bool HandleClickLeft(int x, int y)
        {

        }

        protected virtual bool HandleKeyPressed(char key, int modifier)
        {

        }

        protected virtual bool HandleFocusLoose()
        {

        }
    }
}
