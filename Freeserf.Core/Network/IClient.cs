/*
 * IClient.cs - Interface for network clients
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

using System;
using System.Collections.Generic;
using System.Net;

namespace Freeserf.Network
{
    public interface IClient
    {
        IPAddress Ip { get; }
        uint PlayerIndex { get; }

        void SendResponse(byte messageIndex, ResponseType responseType);
        void SendHeartbeat();
        void SendDisconnect();
    }

    public interface ILocalClient : IClient
    {
        Game Game { get; set; }
        LobbyData LobbyData { get; }
        IRemoteServer Server { get; }

        event EventHandler Disconnected;
        event EventHandler LobbyDataUpdated;
        event EventHandler GameStarted;
        event EventHandler GamePaused;
        event EventHandler GameResumed;
        event EventHandler InputAllowed;
        event EventHandler InputDisallowed;

        byte RequestLobbyStateUpdate();
        byte RequestGameStateUpdate();
        void SendUserAction(UserActionData userAction);
        void SendUserAction(UserActionData userAction, Action<ResponseType> responseAction);

        bool JoinServer(string name, IPAddress ip);
        void Disconnect();
    }

    public interface IRemoteClient : IClient, IRemote
    {
        ILocalServer Server { get; }

        void SendLobbyDataUpdate(byte messageIndex, LobbyServerInfo serverInfo, List<LobbyPlayerInfo> players);
        void SendGameStateUpdate(Game game);
        void SendInSyncMessage(UInt32 gameTime);
    }

    public interface IClientFactory
    {
        ILocalClient CreateLocal();
    }
}
