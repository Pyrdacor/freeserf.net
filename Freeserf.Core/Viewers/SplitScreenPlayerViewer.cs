/*
 * SplitScreenPlayerViewer.cs - Viewer for a local player in split-screen mode
 *
 * Copyright (C) 2025  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using Freeserf.UI;

namespace Freeserf
{
    // A local player viewer which controls a fixed player index. In split-screen
    // mode each local player has his own viewer and his own interface.
    internal class SplitScreenPlayerViewer : LocalPlayerViewer
    {
        readonly uint playerIndex = 0;

        public SplitScreenPlayerViewer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Gui gui, uint playerIndex, bool openGameInit = false)
            : base(renderView, audioInterface, null, gui, openGameInit)
        {
            this.playerIndex = playerIndex;
        }

        public override void OnNewGame(Game game)
        {
            base.OnNewGame(game);

            MainInterface.SetPlayer(playerIndex);
        }
    }
}
