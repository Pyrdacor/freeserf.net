/*
 * ListSavedFiles.cs - Savegame list GUI component
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
    internal class ListSavedFiles : ListBox<GameStore.SaveInfo>
    {
        GameStore saveGame = null;
        Interface interf = null;

        public ListSavedFiles(Interface interf)
            : base(interf, Render.TextRenderType.NewUI)
        {
            saveGame = GameStore.Instance;

            foreach (var saveGame in saveGame.GetSavedGames())
                items.Add(saveGame);

            this.interf = interf;

            Init(interf);
        }

        public void Update()
        {
            items.Clear();

            foreach (var saveGame in saveGame.GetSavedGames())
                items.Add(saveGame);

            Update(interf);
        }

        public string GetFolderPath()
        {
            return saveGame?.FolderPath;
        }
    }
}
