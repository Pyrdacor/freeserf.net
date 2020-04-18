/*
 * ServerViewer.cs - Viewer for multiplayer servers/hosts
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
    internal class ServerViewer : LocalPlayerViewer
    {
        readonly Network.ILocalServer server = null;
        internal override Interface MainInterface { get; }

        public ServerViewer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui)
            : base(renderView, previousViewer, gui, Type.Server)
        {
            // Note: It is ok if the only clients are spectators, but running a server without any connected client makes no sense.
            // Note: All clients must be setup at game start. Clients can not join during the game.
            // Note: There may be more than 3 clients because of spectators!
            server = previousViewer.MainInterface.Server;
            server.NetworkDataReceiver = previousViewer.MainInterface.NetworkDataHandler.NetworkDataReceiver;

            Init();
            MainInterface = new ServerInterface(renderView, audioInterface, this, server);
        }

        public override void OnEndGame(Game game)
        {
            base.OnEndGame(game);

            foreach (var client in server.Clients)
            {
                client.SendDisconnect();
            }

            server.Close();
        }

        public override void Update()
        {
            server.BroadcastHeartbeat();

            base.Update();
        }
    }

}
