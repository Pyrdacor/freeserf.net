/*
 * ClientViewer.cs - Viewer for multiplayer clients
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
    internal class ClientViewer : RemoteSpectatorViewer
    {
        readonly Network.ILocalClient client = null;
        public override Access AccessRights => Access.Player;

        public ClientViewer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui)
            : base(renderView, audioInterface, previousViewer, gui, Type.Client)
        {
            Log.Verbose.Write(ErrorSystemType.Application, "Creating client viewer.");

            client = previousViewer.MainInterface.Client;
            client.Disconnected += Client_Disconnected;
        }

        public void SendUserAction(Network.UserActionData userActionData)
        {
            client.SendUserAction(userActionData, response =>
            {
                if (response != Network.ResponseType.Ok)
                    client.RequestGameStateUpdate();
            });
        }

        private void Client_Disconnected(object sender, System.EventArgs e)
        {
            if (client == null || !client.Connected)
                return; // already disconnected

            // TODO: this may be raised on a different thread and cause all kind of problems (should be invoked during main update cycle somehow)
            GameManager.Instance.CloseGame();
            ActiveViewer.MainInterface.OpenGameInit();
        }

        public override void OnNewGame(Game game)
        {
            Log.Verbose.Write(ErrorSystemType.Application, "Client viewer: New game");

            var music = MainInterface.Audio?.GetMusicPlayer();

            if (music != null)
                music.PlayTrack((int)Audio.Audio.TypeMidi.Track0);

            MainInterface.SetGame(game);
            MainInterface.SetPlayer(client.PlayerIndex);
        }

        public override void OnEndGame(Game game)
        {
            Log.Verbose.Write(ErrorSystemType.Application, "Client viewer: End game");

            client.Disconnected -= Client_Disconnected;

            base.OnEndGame(game);

            if (client.Connected)
                client.Disconnect();
        }

        public override void Update()
        {
            if (client.Connected)
                client.SendHeartbeat(Network.Global.SpontaneousMessage);

            base.Update();
        }
    }
}
