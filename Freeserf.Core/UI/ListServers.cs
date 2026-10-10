/*
 * ListServers.cs - Server list GUI component
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

using System.Collections.Generic;
using System.Linq;

namespace Freeserf.UI
{
    internal class ServerInfo
    {
        public string Name = "";
        public string HostName = "";
        public int CurrentPlayers = 0;
        public int MaxPlayers = Game.MAX_PLAYER_COUNT;
        /// <summary>
        /// The server has answered (otherwise it is a saved address without running server).
        /// </summary>
        public bool Online = false;
        /// <summary>
        /// The game was already started, so no player can join.
        /// </summary>
        public bool InGame = false;
        /// <summary>
        /// The manually entered address if the entry belongs to one (otherwise null).
        /// </summary>
        public string SavedAddress = null;

        public bool CanJoin => Online && !InGame && CurrentPlayers < MaxPlayers;

        public override string ToString()
        {
            if (!Online)
                return $"{SavedAddress} | not found";

            string text = $"{Name} | {HostName} | {CurrentPlayers}/{MaxPlayers}";

            return InGame ? text + " | running" : text;
        }
    }

    internal class ListServers : ListBox<ServerInfo>
    {
        readonly Interface interf = null;
        string lastContent = null;

        public ListServers(Interface interf)
            : base(interf, Render.TextRenderType.NewUI)
        {
            Init(interf);

            this.interf = interf;
        }

        /// <summary>
        /// Replaces the listed servers. The selected server stays selected.
        /// </summary>
        public void SetServers(IEnumerable<ServerInfo> servers)
        {
            var serverList = servers.ToList();
            string content = string.Join("\n", serverList);

            if (content == lastContent)
                return;

            lastContent = content;

            var selected = GetSelected();
            SetItems(interf, serverList);

            if (selected != null)
            {
                int index = serverList.FindIndex(server => (server.Online && selected.Online && server.HostName == selected.HostName) ||
                    (server.SavedAddress != null && server.SavedAddress == selected.SavedAddress));

                if (index >= 0)
                    Select(index);
            }
            else if (serverList.Count != 0)
            {
                Select(0);
            }
        }
    }
}
