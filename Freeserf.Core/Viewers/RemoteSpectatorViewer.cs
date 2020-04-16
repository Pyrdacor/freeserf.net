/*
 * RemoteSpectatorViewer.cs - Viewer for remote spectators (multiplayer)
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
    internal class RemoteSpectatorViewer : Viewer
    {
        public override Access AccessRights => ViewerType == Type.RestrictedRemoteSpectator ? Access.RestrictedSpectator : Access.Spectator;
        public override bool Ingame => MainInterface.Ingame;
        internal override Interface MainInterface { get; }

        protected RemoteSpectatorViewer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui, Type type)
            : base(renderView, gui, type)
        {
            Init();
            MainInterface = new RemoteInterface(renderView, audioInterface, this);
        }

        public RemoteSpectatorViewer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui, bool restricted)
            : this(renderView, audioInterface, previousViewer, gui, restricted ? Type.RestrictedRemoteSpectator : Type.RemoteSpectator)
        {

        }

        public override void Update()
        {
            var remoteInterface = MainInterface as RemoteInterface;

            if (remoteInterface.Game != null && remoteInterface.Player != null)
            {
                remoteInterface.GetMapUpdate();
                remoteInterface.GetGameUpdate();

                if (AccessRights == Access.Spectator)
                {
                    for (uint i = 0; i < remoteInterface.Game.PlayerCount; ++i)
                        remoteInterface.GetPlayerUpdate(i);
                }
                else
                {
                    remoteInterface.GetPlayerUpdate(remoteInterface.Player.Index);
                }

                remoteInterface.Update();
            }
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

            Gui.SetViewer(Viewer.CreateLocalPlayer(MainInterface.RenderView, MainInterface.AudioInterface, this, Gui));
        }
    }
}
