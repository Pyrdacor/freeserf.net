/*
 * Button.cs - Button GUI components
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
    using Data = Data.Data;

    internal class Button : Icon
    {
        public class ClickEventArgs : System.EventArgs
        {
            public int X { get; }
            public int Y { get; }

            public ClickEventArgs(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        public delegate void ClickEventHandler(object sender, ClickEventArgs args);

        public event ClickEventHandler Clicked;
        public event ClickEventHandler DoubleClicked;

        public Button(Interface interf, int width, int height, Data.Resource resourceType, uint spriteIndex, byte displayLayerOffset)
            : base(interf, width, height, resourceType, spriteIndex, displayLayerOffset)
        {

        }

        protected override bool HandleClickLeft(int x, int y)
        {
            Clicked?.Invoke(this, new ClickEventArgs(x - TotalX, y - TotalY));

            return true;
        }

        protected override bool HandleDoubleClick(int x, int y, Event.Button button)
        {
            DoubleClicked?.Invoke(this, new ClickEventArgs(x - TotalX, y - TotalY));

            return true;
        }
    }

    internal class BuildingButton : BuildingIcon
    {
        public delegate void ClickEventHandler(object sender, Button.ClickEventArgs args);

        public event ClickEventHandler Clicked;

        public BuildingButton(Interface interf, int width, int height, uint spriteIndex, byte displayLayerOffset)
            : base(interf, width, height, spriteIndex, displayLayerOffset)
        {

        }

        protected override bool HandleClickLeft(int x, int y)
        {
            Clicked?.Invoke(this, new Button.ClickEventArgs(x - TotalX, y - TotalY));

            return true;
        }
    }
}
