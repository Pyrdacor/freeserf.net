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

        public RandomInput(Interface interf)
            : base(interf)
        {
            SetFilter(TextInputFilter);
            SetSize(34, 34);
            MaxLength = 16;
        }

        public void SetRandom(Random rnd)
        {
            Text = rnd.ToString();
        }

        public Random GetRandom()
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

        GameType gameType = GameType.Custom;
        int gameMission = 0;

        GameInfo customMission = null;
        GameInfo mission = null;

        RandomInput randomInput = null;
        Map map = null;
        Minimap minimap = null;
        ListSavedFiles fileList = null;

        // rendering
        Render.ISprite background = null;
        Render.ISprite buttonStart = null;
        Render.ISprite buttonOptions = null;
        Render.ISprite iconGameType = null;

        public GameInitBox(Interface interf)
        {
            this.interf = interf;

            randomInput = new RandomInput(interf);

            var size = Global.TransformSizeFromOriginalSize(interf.RenderView, new Size(360, 254));
            SetSize(size.Width, size.Height);

            customMission = new GameInfo(new Random());
            customMission.AddPlayer(12, PlayerInfo.PlayerColors[0], 40, 40, 40);
            customMission.AddPlayer(1, PlayerInfo.PlayerColors[1], 20, 30, 40);
            mission = customMission;

            minimap.Displayed = true;
            size = Global.TransformSizeFromOriginalSize(interf.RenderView, new Size(150, 160));
            minimap.SetSize(size.Width, size.Height);
            var position = Global.TransformPositionFromOriginalPosition(interf.RenderView, new Position(190, 55));
            AddFloatWindow(minimap, position.X, position.Y);

            generate_map_preview();

            randomInput.SetRandom(customMission.RandomBase);
            randomInput.Displayed = true;
            position = Global.TransformPositionFromOriginalPosition(interf.RenderView, new Position(19 + 31 * 8, 15));
            AddFloatWindow(randomInput, position.X, position.Y);

            size = Global.TransformSizeFromOriginalSize(interf.RenderView, new Size(160, 160));
            fileList.SetSize(size.Width, size.Height);
            fileList.Displayed = false;
            fileList.SetSelectionHandler((string item) =>
            {
                Game game = new Game(interf.RenderView);

                if (GameStore.Instance.Load(item, game))
                {
                    map = game.Map;
                    minimap.SetMap(map);
                }
            });
            position = Global.TransformPositionFromOriginalPosition(interf.RenderView, new Position(20, 55));
            AddFloatWindow(fileList, position.X, position.Y);
        }

        protected override void InternalDraw()
        {
            throw new NotImplementedException();
        }
    }
}
