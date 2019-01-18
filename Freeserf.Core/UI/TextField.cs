/*
 * TextField.cs - Text field GUI component
 *
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

namespace Freeserf.UI
{
    internal class TextField : GuiObject
    {
        readonly Render.TextRenderer textRenderer;
        int index = -1;
        string text = "";
        byte displayLayerOffset = 0;
        bool useSpecialDigits = false;
        int characterGapSize = 8;

        public TextField(Interface interf, byte displayLayerOffset, int characterGapSize = 8, bool useSpecialDigits = false)
            : base(interf)
        {
            textRenderer = interf.TextRenderer;
            this.useSpecialDigits = useSpecialDigits;
            this.displayLayerOffset = displayLayerOffset;
            this.characterGapSize = characterGapSize;
        }

        public void Destroy()
        {
            base.Displayed = false;

            if (index != -1)
                textRenderer.DestroyText(index);

            text = "";
            index = -1;

            if (Parent != null)
                Parent.DeleteChild(this);
        }

        public string Text
        {
            get => text;
            set
            {
                if (text == value)
                    return;

                text = value;

                if (index == -1)
                    index = textRenderer.CreateText(text, (byte)(BaseDisplayLayer + displayLayerOffset + 1), useSpecialDigits, new Position(TotalX, TotalY), characterGapSize);
                else
                    textRenderer.ChangeText(index, text, (byte)(BaseDisplayLayer + displayLayerOffset + 1), characterGapSize);

                if (text.Length == 0)
                    SetSize(0, 0);
                else if (text.Length == 1)
                    SetSize(8, 8);
                else
                    SetSize(8 + (text.Length - 1) * characterGapSize, 8);
            }
        }

        public override bool Displayed
        {
            get => base.Displayed;
            set
            {

                if (Displayed == value)
                    return;

                base.Displayed = value;

                UpdateVisibility();
            }
        }

        public void UpdateVisibility()
        {
            if (Visible)
            {
                if (index == -1)
                    index = textRenderer.CreateText(text, (byte)(BaseDisplayLayer + displayLayerOffset + 1), useSpecialDigits, new Position(TotalX, TotalY), characterGapSize);

                textRenderer.ShowText(index, true);
            }
            else
            {
                if (index != -1)
                    textRenderer.ShowText(index, false);
            }
        }

        protected override void InternalDraw()
        {
            if (index != -1)
                textRenderer.SetPosition(index, new Position(TotalX, TotalY), characterGapSize);
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            if (index != -1)
                textRenderer.ShowText(index, false);
        }

        protected internal override void UpdateParent()
        {
            base.UpdateParent();

            if (index != -1)
                textRenderer.ChangeDisplayLayer(index, (byte)(BaseDisplayLayer + displayLayerOffset));
        }

        public void UseSpecialDigits(bool use)
        {
            if (useSpecialDigits == use)
                return;

            useSpecialDigits = use;

            if (index != -1)
                textRenderer.UseSpecialDigits(index, use);
        }
    }
}
