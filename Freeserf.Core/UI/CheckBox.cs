/*
 * CheckBox.cs - Check box GUI component
 *
 * Copyright (C) 2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
    using Data = Data.Data;

    internal class CheckBox : GuiObject
    {
        readonly TextField textField = null;
        readonly Button checkButton = null;
        bool active = false;

        public event System.EventHandler CheckedChanged;

        public CheckBox(Interface interf)
            : base(interf)
        {
            checkButton = new Button(interf, 16, 16, Data.Resource.Icon, 220u, 1);
            checkButton.Clicked += CheckButton_Clicked;
            AddChild(checkButton, 0, 0);

            textField = new TextField(interf, 1);
            AddChild(textField, 18, 4);

            SetSize(18, 16);
        }

        private void CheckButton_Clicked(object sender, Button.ClickEventArgs args)
        {
            Checked = !Checked;
        }

        public string Text
        {
            get => textField.Text;
            set
            {
                textField.Text = value;

                SetSize(18 + textField.Text.Length * 8, 16);
            }
        }

        public bool Checked
        {
            get => active;
            set
            {
                if (active == value)
                    return;

                active = value;

                checkButton.SetSpriteIndex(active ? 288u : 220u);

                CheckedChanged?.Invoke(this, System.EventArgs.Empty);
            }
        }

        public override bool Displayed
        {
            get => base.Displayed;
            set
            {
                base.Displayed = value;

                textField.UpdateVisibility();
            }
        }

        protected override void InternalDraw()
        {
            // nothing to do here
        }
    }
}
