/*
 * GameInitBox.cs - Game initialization GUI component
 *
 * Copyright (C) 2013-2016  Jon Lund Steffensen <jonlst@gmail.com>
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
using System.Text;

namespace Freeserf
{
    class RandomInput : TextInput
    {
        string savedText = "";

        public RandomInput()
        {
            SetFilter(TextInputFilter);
            SetSize(34, 34);
            MaxLength = 16;
        }

        void SetRandom(Random rnd)
        {
            Text = rnd.ToString();
        }

        Random GetRandom()
        {
            return new Random(Text);
        }

        static bool TextInputFilter(char key, TextInput textInput)
        {
            if (key < '1' || key > '8')
            {
                return false;
            }

            if (textInput.Text.Length > 16)
            {
                return false;
            }

            return true;
        }

        protected override bool HandleClickLeft(int x, int y)
        {
            base.HandleClickLeft(x, y);

            savedText = Text;
            Text = "";

            return true;
        }

        protected override bool HandleFocusLoose()
        {
            base.HandleFocusLoose();

            if (Text.Length < 16 && savedText.Length == 16)
            {
                Text = savedText;
                savedText = "";
            }

            return true;
        }
    }

    internal class GameInitBox : GuiObject
    {
        public enum Action
        {
            StartGame,
            ToggleGameType,
            ShowOptions,
            Increment,
            Decrement,
            Close,
            GenRandom,
            ApplyRandom
        }

        public enum GameType
        {
            Custom = 0,
            Mission = 1,
            Load = 2
        }

        Interface interf = null;

        GameType game_type;
        int game_mission;

        GameInfo custom_mission;
        GameInfo mission;

        RandomInput random_input;
        Map map;
        Minimap minimap;
        ListSavedFiles file_list;

        public GameInitBox(Interface interf)
        {
            this.interf = interf;
        }

        protected override void InternalDraw()
        {
            throw new NotImplementedException();
        }
    }
}
