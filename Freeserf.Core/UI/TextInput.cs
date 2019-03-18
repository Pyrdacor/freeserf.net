/*
 * TextInput.cs - Text input GUI component
 *
 * Copyright (C) 2015       Wicked_Digger <wicked_digger@mail.ru>
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Freeserf.UI
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
        readonly List<TextField> textLines = new List<TextField>();
        readonly Interface interf = null;
        bool useSpecialDigits = false;
        
        public TextInput(Interface interf, bool useSpecialDigits = false)
            : base(interf)
        {
            background = interf.RenderView.ColoredRectFactory.Create(0, 0, colorBackground, BaseDisplayLayer);
            background.Layer = Layer;
            this.interf = interf;
            this.useSpecialDigits = useSpecialDigits;
        }

        public Render.Color BackgroundColor
        {
            get => colorBackground;
            set
            {
                colorBackground = value;

                if (!drawFocus || !focused)
                background.Color = value;
            }
        }

        public Render.Color BackgroundFocusColor
        {
            get => colorFocus;
            set
            {
                colorFocus = value;

                if (drawFocus && focused)
                    background.Color = value;
            }
        }

        public Render.Color TextColor
        {
            get => colorText;
            set
            {
                colorText = value;
                
                // TODO
            }
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

        protected internal override void UpdateParent()
        {
            base.UpdateParent();

            background.DisplayLayer = BaseDisplayLayer;
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            background.Visible = false;
        }

        protected override void InternalDraw()
        {
            background.X = TotalX;
            background.Y = TotalY;
            background.Visible = Displayed;

            if (drawFocus && focused)
                background.Color = colorFocus;
            else
                background.Color = colorBackground;

            int numMaxCharsPerLine = (Width < 8) ? 0 : 1 + (Width - 8) / 9;
            string str = text;
            int cx = 0;
            int cy = 0;

            int textLineIndex = 0;

            while (str.Length > 0)
            {
                int numChars = Math.Min(str.Length, numMaxCharsPerLine);

                string substr = str.Substring(0, numChars);
                str = str.Remove(0, numChars);

                if (textLineIndex == textLines.Count)
                {
                    var newLine = new TextField(interf, 2, 9, useSpecialDigits);

                    textLines.Add(newLine);
                    AddChild(newLine, cx, cy);
                }
                else
                {
                    AddChild(textLines[textLineIndex], cx, cy);
                }

                textLines[textLineIndex].Text = substr; // TODO: we need a possibility to set a color for textured sprites
                textLines[textLineIndex].Displayed = true;
                // TODO: set color to colorText

                ++textLineIndex;

                cy += 9;
            }

            // ensure that the other lines are not visible (they can be reused later by just setting Displayed to true)
            for (int i = textLines.Count - 1; i >= textLineIndex; --i)
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

            if (key == Event.SystemKeys.Backspace && text.Length > 0)
            {
                text = text.Substring(0, text.Length - 1);
                SetRedraw();
                return true;
            }

            if (key == Event.SystemKeys.Return)
            {
                LooseFocus();
                return true;
            }

            // invalid character
            if (key != ' ' && !Render.TextRenderer.ValidCharacters.Any(c => c == key))
            {
                return true;
            }

            if (MaxLength != 0 && text.Length >= MaxLength)
            {
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
