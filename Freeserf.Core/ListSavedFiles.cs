/*
 * ListSavedFiles.cs - String list GUI component
 *
 * Copyright (C) 2017  Wicked_Digger <wicked_digger@mail.ru>
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

using System;
using System.Collections.Generic;
using System.Linq;
using Freeserf.Render;

namespace Freeserf
{
    internal class ListSavedFiles : GuiObject
    {
        Color colorFocus = new Color(0x00, 0x8b, 0x47);
        Color colorText = Color.Green;
        Color colorBackground = Color.Black;

        GameStore saveGame = null;
        readonly List<GameStore.SaveInfo> items = null;
        int firstVisibleItem = 0;
        int selectedItem = -1;
        Action<string> selectionHandler = null;

        readonly IColoredRect background = null;
        readonly IColoredRect selectionBackground = null;
        readonly List<TextField> saveGameNames = new List<TextField>();

        public ListSavedFiles(Interface interf)
            : base(interf)
        {
            saveGame = GameStore.Instance;
            items = saveGame.GetSavedGames();

            background = interf.RenderView.ColoredRectFactory.Create(0, 0, colorBackground, BaseDisplayLayer);
            background.Layer = Layer;

            selectionBackground = interf.RenderView.ColoredRectFactory.Create(0, 0, Color.Green, (byte)(BaseDisplayLayer + 1));
            selectionBackground.Layer = Layer;

            int y = 3;

            foreach (var item in items)
            {
                var textField = new TextField(interf, 1);

                AddChild(textField, 3, y, true);
                saveGameNames.Add(textField);

                y += 9;
            }
        }

        public void SetSelectionHandler(Action<string> selectionHandler)
        {
            this.selectionHandler = selectionHandler;
        }

        public string GetSelected()
        {
            if (items.Count == 0 || selectedItem == -1)
            {
                return "";
            }

            return items[selectedItem].Path;
        }

        public string GetFolderPath()
        {
            return saveGame?.FolderPath;
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
            selectionBackground.Y = TotalY + 2 + (selectedItem - firstVisibleItem) * 9;

            for (int i = 0; i < saveGameNames.Count; ++i)
            {
                if (i < firstVisibleItem)
                {
                    saveGameNames[i].Displayed = false;
                }
                else
                {
                    int y = 3 + (i - firstVisibleItem) * 9;

                    saveGameNames[i].Displayed = y < (Height - 6);
                }

                saveGameNames[i].Text = items[i].Name;

                // TODO
                //saveGameNames[i].Color = (i == selectedItem) ? Color.Black : colorText;
            }

            selectionBackground.Visible = Displayed && selectedItem != -1 && saveGameNames[selectedItem].Displayed;
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
                    selectedItem = y;
                    SetRedraw();

                    selectionHandler?.Invoke(items[selectedItem].Path);
                }
            }

            return true;
        }

        protected override bool HandleDrag(int dx, int dy)
        {
            if (!focused)
            {
                return false;
            }

            int nextFirstVisibleItem = firstVisibleItem + dy / 32;

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

        protected override bool HandleKeyPressed(char key, int modifier)
        {
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
