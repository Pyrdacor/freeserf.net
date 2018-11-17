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

using System.Collections.Generic;

namespace Freeserf
{
    internal class TextInput : GuiObject
    {
        public delegate bool Filter(char key, TextInput textInput);

        string text = "";
        int maxLength = 0;
        Filter filter = null;
        Render.Color colorFocus = new Render.Color(0x00, 0x8b, 0x47);
        Render.Color colorText = Render.Color.Green;
        Render.Color colorBackground = Render.Color.Black;
        bool drawFocus = true;
        Render.IColoredRect background = null;
        readonly List<Render.TextField> textLines = new List<Render.TextField>();
        readonly Render.TextRenderer textRenderer = null;
        
        public TextInput(Interface interf)
            : base(interf)
        {
            background = interf.RenderView.ColoredRectFactory.Create(0, 0, colorBackground);
            textRenderer = interf.TextRenderer;
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
            get => maxLength;
            set
            {
                maxLength = value;

                if (maxLength != 0)
                {
                    if (text.Length > maxLength)
                    {
                        Text = Text.Substring(0, maxLength);
                    }
                }
            }
        }

        protected override void Layout()
        {
            base.Layout();

            background.Resize(Width, Height);
        }

        protected override void InternalHide()
        {
            background.Visible = false;

            foreach (var textLine in textLines)
                textLine.Visible = false;
        }

        protected override void InternalDraw()
        {
            background.Visible = Displayed;

            if (drawFocus && focused)
                background.Color = colorFocus;
            else
                background.Color = colorBackground;

            int numMaxCharsPerLine = Width / 8;
            string str = text;
            int cx = 0;
            int cy = 0;

            if (drawFocus)
            {
                cx = 1;
                cy = 1;
            }

            int textLineIndex = 0;

            while (str.Length > 0)
            {
                string substr = str.Substring(0, numMaxCharsPerLine);
                str.Remove(0, numMaxCharsPerLine);

                if (textLineIndex == textLines.Count)
                {
                    var newLine = new Render.TextField(textRenderer);

                    newLine.SetPosition(cx, cy);

                    textLines.Add(newLine);
                }

                textLines[textLineIndex].Text = substr; // TODO: we need a possibility to set a color for textured sprites
                textLines[textLineIndex].Visible = true;
                // TODO: set color to colorText

                ++textLineIndex;

                cy += 8; // TODO: adjust size
            }

            // ensure that the other lines are not visible (they can be reused later by just setting Visible to true)
            for (int i = textLineIndex; i < textLines.Count; ++i)
                textLines[i].Destroy();
        }

        public void SetFilter(Filter filter)
        {
            this.filter = filter;
        }

        protected override bool HandleClickLeft(int x, int y)
        {
            SetFocused();
            return true;
        }

        protected override bool HandleKeyPressed(char key, int modifier)
        {
            if (!focused)
            {
                return false;
            }

            if (MaxLength != 0 && text.Length >= MaxLength)
            {
                return true;
            }

            if (key == '\b' && text.Length > 0)
            {
                text = text.Substring(0, text.Length - 1);
                SetRedraw();
                return true;
            }

            if (filter != null)
            {
                if (!filter(key, this))
                {
                    return true;
                }
            }

            text += key;

            SetRedraw();

            return true;
        }

        protected override bool HandleFocusLoose()
        {
            focused = false;
            SetRedraw();
            return true;
        }
    }
}
