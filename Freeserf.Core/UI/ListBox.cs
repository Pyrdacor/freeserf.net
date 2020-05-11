/*
 * ListBox.cs - String list GUI component
 *
 * Copyright (C) 2017  Wicked_Digger <wicked_digger@mail.ru>
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

using Freeserf.Event;
using Freeserf.Render;
using System;
using System.Collections.Generic;

namespace Freeserf.UI
{
    internal class ListBox<T> : GuiObject
    {
        Render.Color colorFocus = new Render.Color(0x60, 0x70, 0x60);//new Color(0x00, 0x8b, 0x47);
        Render.Color colorText = Render.Color.Green;
        Render.Color colorBackground = Render.Color.Black;

        int firstVisibleItem = 0;
        int selectedItem = -1;
        Action<T> selectionHandler = null;
        protected readonly List<T> items = new List<T>();
        TextRenderType renderType = TextRenderType.NewUI;

        readonly IColoredRect background = null;
        readonly IColoredRect selectionBackground = null;
        readonly List<TextField> textEntries = new List<TextField>();

        public ListBox(Interface interf, TextRenderType renderType = TextRenderType.NewUI)
            : base(interf)
        {
            background = interf.RenderView.ColoredRectFactory.Create(0, 0, colorBackground, BaseDisplayLayer);
            background.Layer = Layer;

            selectionBackground = interf.RenderView.ColoredRectFactory.Create(0, 0, colorFocus, (byte)(BaseDisplayLayer + 1));
            selectionBackground.Layer = Layer;
            this.renderType = renderType;
        }

        public override bool Displayed
        {
            get => base.Displayed;
            set
            {
                base.Displayed = value;

                foreach (var entry in textEntries)
                    entry.UpdateVisibility();
            }
        }

        protected int CharacterGapSize
        {
            get;
            set;
        } = 6;

        protected void Init(Interface interf)
        {
            int y = 3;

            foreach (var item in items)
            {
                var textField = new TextField(interf, 1, CharacterGapSize, renderType);

                AddChild(textField, 3, y, true);
                textEntries.Add(textField);

                y += 9;
            }
        }

        protected void Update(Interface interf)
        {
            int y = 3;

            for (int i = 0; i < items.Count; ++i)
            {
                if (i == textEntries.Count)
                {
                    var textField = new TextField(interf, 1, CharacterGapSize, renderType);

                    AddChild(textField, 3, y, true);
                    textEntries.Add(textField);
                }

                y += 9;
            }

            SetRedraw();
        }

        public void SetSelectionHandler(Action<T> selectionHandler)
        {
            this.selectionHandler = selectionHandler;
        }

        public void Select(int index)
        {
            if (index < 0 || index >= items.Count)
                return;

            selectedItem = index;
            SetRedraw();

            selectionHandler?.Invoke(items[selectedItem]);
        }

        public T GetSelected()
        {
            if (items.Count == 0 || selectedItem == -1)
            {
                return default(T);
            }

            return items[selectedItem];
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            background.Visible = false;
            selectionBackground.Visible = false;
        }

        protected internal override void UpdateParent()
        {
            base.UpdateParent();

            background.DisplayLayer = BaseDisplayLayer;
            selectionBackground.DisplayLayer = (byte)(BaseDisplayLayer + 1);
        }

        protected override void InternalDraw()
        {
            background.Resize(Width, Height);
            background.X = TotalX;
            background.Y = TotalY;
            background.Visible = Displayed;

            selectionBackground.Resize(Width - 4, 9);
            selectionBackground.X = TotalX + 2;
            selectionBackground.Y = TotalY + 3 + (selectedItem - firstVisibleItem) * 9;

            for (int i = 0; i < textEntries.Count; ++i)
            {
                if (i < firstVisibleItem)
                {
                    textEntries[i].Displayed = false;
                }
                else
                {
                    int y = 3 + (i - firstVisibleItem) * 9;

                    textEntries[i].MoveTo(textEntries[i].X, y);
                    textEntries[i].Displayed = y < (Height - 6);
                }

                textEntries[i].Text = TrimText(items[i].ToString());

                // TODO
                //textEntries[i].Color = (i == selectedItem) ? Color.Black : colorText;
            }

            selectionBackground.Visible = Displayed && selectedItem != -1 && textEntries[selectedItem].Displayed;
        }

        string TrimText(string text)
        {
            int width = text.Length * CharacterGapSize;

            if (width <= Width - 3)
                return text;

            int maxLength = (Width - 3) / CharacterGapSize;

            return text.Substring(0, maxLength - 2) + "..";
        }

        protected override bool HandleClickLeft(int x, int y)
        {
            SetFocused();

            y -= TotalY;
            y -= 3;

            if (y >= 0)
            {
                y = firstVisibleItem + (y / 9);

                if (selectedItem != y && y >= 0 && y < items.Count)
                {
                    Select(y);
                }
            }

            return true;
        }

        protected override bool HandleDrag(int x, int y, int dx, int dy, Event.Button button)
        {
            if (!focused || button != Event.Button.Right)
            {
                return false;
            }

            int nextFirstVisibleItem = firstVisibleItem + dy;

            if (nextFirstVisibleItem < 0)
            {
                nextFirstVisibleItem = 0;
            }

            if (((items.Count - nextFirstVisibleItem) * 9) <= Height - 8)
            {
                return true;
            }

            if (firstVisibleItem != nextFirstVisibleItem)
            {
                firstVisibleItem = nextFirstVisibleItem;
                SetRedraw();
            }

            return true;
        }

        private void SelectAndScroll(int index)
        {
            int lastPossibleFirstVisibleItem = Math.Max(0, items.Count + 1 - (Height + 8) / 9);
            int newFirstVisibleItem = Math.Min(index, lastPossibleFirstVisibleItem);

            if (firstVisibleItem != newFirstVisibleItem)
            {
                firstVisibleItem = newFirstVisibleItem;
            }

            if (selectedItem != index)
                Select(index);
            else
                SetRedraw(); // we need a redraw cause we may have changed firstVisibleItem
        }

        protected override bool HandleSystemKeyPressed(SystemKey key, int modifier)
        {
            if (items.Count != 0)
            {
                if (key == SystemKey.PageUp) // page up
                {
                    SelectAndScroll(0);
                }
                else if (key == SystemKey.PageDown) // page down
                {
                    SelectAndScroll(items.Count - 1);
                }
                else if (key == SystemKey.Up) // up
                {
                    if (selectedItem > 0)
                    {
                        SelectAndScroll(selectedItem - 1);
                    }
                }
                else if (key == SystemKey.Down) // down
                {
                    if (selectedItem < items.Count - 1)
                    {
                        SelectAndScroll(selectedItem + 1);
                    }
                }
            }

            return focused;
        }

        protected override bool HandleFocusLoose()
        {
            focused = false;

            SetRedraw();

            return true;
        }
    }
}
