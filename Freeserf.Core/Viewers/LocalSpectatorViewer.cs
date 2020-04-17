/*
 * LocalSpectatorViewer.cs - Viewer for local spectators
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
    internal class LocalSpectatorViewer : Viewer
    {
        public override Access AccessRights => Access.Spectator;
        public override bool Ingame => MainInterface.Ingame;
        internal override Interface MainInterface { get; }

        protected LocalSpectatorViewer(Render.IRenderView renderView, Viewer previousViewer, Gui gui, Type type)
            : base(renderView, gui, type)
        {

        }

        public LocalSpectatorViewer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui)
            : this(renderView, previousViewer, gui, Type.LocalSpectator)
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

        public override void Update()
        {
            MainInterface.Update();
        }

        public override void Draw()
        {
            MainInterface.Draw();
        }

        public override void DrawCursor(int x, int y)
        {
            MainInterface.DrawCursor(x, y);
        }

        public override bool SendEvent(Event.EventArgs args)
        {
            if (!args.Done)
                args.Done = MainInterface.HandleEvent(args);

            return args.Done;
        }

        public override void OnNewGame(Game game)
        {
            var music = MainInterface.Audio?.GetMusicPlayer();

            if (music != null)
                music.PlayTrack((int)Audio.Audio.TypeMidi.Track0);

            MainInterface.SetGame(game);
            MainInterface.SetPlayer(0);
        }

        public override void OnEndGame(Game game)
        {
            MainInterface.SetGame(null);

            if (!(this is LocalPlayerViewer))
                Gui.SetViewer(Viewer.CreateLocalPlayer(MainInterface.RenderView, MainInterface.AudioInterface, this, Gui));

        }
    }

}
