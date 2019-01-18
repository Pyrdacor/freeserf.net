/*
 * SlideBar.cs - Slidebar GUI component
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

    internal class SlideBar : GuiObject
    {
        readonly Icon icon = null;
        readonly Render.IColoredRect fillRect = null;
        int fill = 0;
        readonly byte displayLayerOffset;

        public SlideBar(Interface interf, byte displayLayerOffset)
            : base(interf)
        {
            this.displayLayerOffset = displayLayerOffset;

            icon = new Icon(interf, 64, 8, Data.Resource.Icon, 236u, (byte)(displayLayerOffset + 1));

            fillRect = interf.RenderView.ColoredRectFactory.Create(0, 4, new Render.Color(0x6b, 0xab, 0x3b), (byte)(displayLayerOffset + 2));
            fillRect.Layer = Layer;

            SetSize(64, 8);

            AddChild(icon, 0, 0, true);
        }

        public int Fill
        {
            get => fill;
            set
            {
                if (value < 0)
                    value = 0;

                if (value > 50)
                    value = 50;

                if (fill == value)
                    return;

                fill = value;

                fillRect.Resize(fill, 4);
                FillChanged?.Invoke(this, System.EventArgs.Empty);
            }
        }

        protected override void InternalDraw()
        {
            fillRect.X = TotalX + 7;
            fillRect.Y = TotalY + 2;

            fillRect.Visible = fill > 0 && Displayed;
            icon.Displayed = Displayed;
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            fillRect.Visible = false;
        }

        protected internal override void UpdateParent()
        {
            fillRect.DisplayLayer = (byte)(BaseDisplayLayer + displayLayerOffset + 2);
        }

        protected override bool HandleClickLeft(int x, int y)
        {
            int relX = x - TotalX;

            if (relX < 7)
                Fill = 0;
            else if (relX < 57)
                Fill = relX - 7;
            else
                Fill = 50;

            return true;
        }

        public event System.EventHandler FillChanged;
    }
}
