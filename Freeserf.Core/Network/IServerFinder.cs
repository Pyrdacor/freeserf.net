/*
 * IServerFinder.cs - Search for multiplayer servers
 *
 * Copyright (C) 2026  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using System.Collections.Generic;
using System.Net;

namespace Freeserf.Network
{
    public class FoundServer
    {
        public string Name { get; init; } = "";
        public IPAddress Ip { get; init; }
        /// <summary>
        /// The address under which the server was searched (only for manually entered addresses).
        /// </summary>
        public string SearchedAddress { get; init; }
        public int CurrentPlayers { get; init; } = 0;
        public int MaxPlayers { get; init; } = Game.MAX_PLAYER_COUNT;
        /// <summary>
        /// The game already started. So no players can join.
        /// </summary>
        public bool InGame { get; init; } = false;
    }

    /// <summary>
    /// Searches for multiplayer servers in the local network
    /// and at manually entered addresses.
    /// </summary>
    public interface IServerFinder
    {
        /// <summary>
        /// Starts the search. It is repeated every few seconds until it is stopped.
        /// </summary>
        void Start();
        void Stop();
        /// <summary>
        /// Sets the addresses (IPs or host names) which are checked in addition to the local network.
        /// </summary>
        void SetAddresses(IEnumerable<string> addresses);
        /// <summary>
        /// The servers which have answered recently.
        /// </summary>
        IReadOnlyList<FoundServer> Servers { get; }
    }
}
