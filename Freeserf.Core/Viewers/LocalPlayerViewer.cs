/*
 * LocalPlayerViewer.cs - Viewer for local players
 *
 * Copyright (C) 2018-2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
    internal class LocalPlayerViewer : LocalSpectatorViewer
    {
        public override Access AccessRights => Access.Player;
        internal override Interface MainInterface { get; }

        protected LocalPlayerViewer(Render.IRenderView renderView, Viewer previousViewer, Gui gui, Type type)
            : base(renderView, previousViewer, gui, type)
        {

        }

        public LocalPlayerViewer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui)
            : this(renderView, previousViewer, gui, Type.LocalPlayer)
        {
            if (previousViewer == null)
            {
                Init();
                MainInterface = new Interface(renderView, audioInterface, gui, this);
                MainInterface.OpenGameInit();
            }
            else
            {
                if (previousViewer.MainInterface.GetType() != typeof(Interface))
                    MainInterface = new Interface(renderView, audioInterface, gui, this);
                else
                    MainInterface = previousViewer.MainInterface;

                MainInterface.Viewer = this;
            }
        }
    }
}
