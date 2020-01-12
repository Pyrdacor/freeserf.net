﻿/*
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
using System.Net;

namespace Freeserf.Network
{
    public interface IClient
    {
        uint PlayerIndex { get; }
        
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

        byte RequestLobbyStateUpdate();
        byte RequestPlayerStateUpdate(uint playerIndex);
        byte RequestMapStateUpdate();
        byte RequestGameStateUpdate();

        bool JoinServer(string name, IPAddress ip);
        void Disconnect();
    }

    public interface IRemoteClient : IClient, IRemote
    {
        ILocalServer Server { get; }

        void SendGameStateUpdate(Game game);
        void SendPlayerStateUpdate(Player player);
        void SendMapStateUpdate(Map map);
    }

    public interface IClientFactory
    {
        ILocalClient CreateLocal();
    }
}
