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

namespace Freeserf.UI
{
    internal class ServerInfo
    {
        public string Name = "";
        public string HostName = "";
        public int CurrentPlayers = 0;
        public int MaxPlayers = 4;

        public override string ToString()
        {
            return $"{Name} | {HostName} | {CurrentPlayers}/{MaxPlayers}";
        }
    }

    internal class ListServers : ListBox<ServerInfo>
    {
        public ListServers(Interface interf)
            : base(interf, Render.TextRenderType.NewUI)
        {
            // TODO
            AddServer("Test Server", "localhost", 1, 4);

            Init(interf);
        }

        public void AddServer(string serverName, string hostName, int currentPlayers, int maxPlayers = 4)
        {
            items.Add(new ServerInfo()
            {
                Name = serverName,
                HostName = hostName,
                CurrentPlayers = currentPlayers,
                MaxPlayers = maxPlayers
            });
        }
    }
}
