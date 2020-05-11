/*
 * LobbyData.cs - Lobby network data
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Freeserf.Network
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct LobbyPlayerData
    {
        public byte Type; // 0: AI, 1: Server/Host, 2: Client
        public byte Face;
        public byte Supplies;
        public byte Reproduction;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct LobbyAIPlayerData
    {
        public byte Intelligence;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct LobbyHumanPlayerData
    {
        public fixed byte IP[4]; // 4 bytes
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct LobbyServerSettings
    {
        // Bit 0: Server Values
        // Bit 1: Same Values
        // Bit 2-7: Unused
        public byte Flags;
        public byte MapSize;
        public fixed byte MapSeed[16]; // 16 bytes
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct LobbyDataHeader
    {
        public byte DataSize; // including this byte
        public byte PlayerCount;
    }

    public class LobbyPlayerInfo
    {
        public LobbyPlayerInfo(string identification, bool isHost,
            int face, int supplies, int intelligence, int reproduction)
        {
            Identification = identification;
            IsHost = isHost;
            Face = face;
            Supplies = supplies;
            Intelligence = intelligence;
            Reproduction = reproduction;
        }

        public readonly string Identification; // IP or empty
        public readonly bool IsHost;
        public readonly int Face;
        public readonly int Supplies;
        public readonly int Intelligence;
        public readonly int Reproduction;

        internal int GetDataSize()
        {
            return Marshal.SizeOf(typeof(LobbyPlayerData)) +
                (Identification == null ?
                    Marshal.SizeOf(typeof(LobbyAIPlayerData)) :
                    Marshal.SizeOf(typeof(LobbyHumanPlayerData)));
        }
    }

    public class LobbyServerInfo
    {
        public LobbyServerInfo(bool useServerValues, bool useSameValues, uint mapSize, string mapSeed)
        {
            UseServerValues = useServerValues;
            UseSameValues = useSameValues;
            MapSize = mapSize;
            MapSeed = mapSeed;
        }

        public bool UseServerValues { get; set; }
        public bool UseSameValues { get; set; }
        public uint MapSize { get; set; }
        public string MapSeed { get; set; }
    }

    public class LobbyData : INetworkData
    {
        private static readonly int MIN_DATA_SIZE = 3 + Marshal.SizeOf(typeof(LobbyDataHeader)) + Marshal.SizeOf(typeof(LobbyServerSettings));
        private LobbyDataHeader header;
        public LobbyServerInfo ServerInfo { get; private set; } = null;
        private readonly List<LobbyPlayerInfo> players = new List<LobbyPlayerInfo>(4);
        public IReadOnlyList<LobbyPlayerInfo> Players => players.AsReadOnly();
        private byte[] sendData = null;

        public NetworkDataType Type => NetworkDataType.LobbyData;

        public byte Number
        {
            get;
            private set;
        } = 0;

        internal LobbyData()
        {
            // use when parsing the data
        }

        public LobbyData(byte number, LobbyServerInfo serverInfo, List<LobbyPlayerInfo> players)
        {
            Number = number;

            if (players.Count > 4)
                throw new ExceptionFreeserf("Player count must not exceed 4.");

            int dataSize = MIN_DATA_SIZE + (players.Count == 0 ? 0 : players.Select(p => p.GetDataSize()).Aggregate((a, b) => a + b));

            if (dataSize > 255)
                throw new ExceptionFreeserf("Lobby data length must not exceed 255.");

            header.DataSize = (byte)dataSize;
            header.PlayerCount = (byte)players.Count;

            var serverSettings = new LobbyServerSettings();

            if (serverInfo.UseServerValues)
                serverSettings.Flags |= 0x01;
            if (serverInfo.UseSameValues)
                serverSettings.Flags |= 0x02;

            serverSettings.MapSize = (byte)serverInfo.MapSize;

            byte[] mapSeed = Encoding.ASCII.GetBytes(serverInfo.MapSeed);

            if (mapSeed.Length != 16)
                throw new ExceptionFreeserf("Invalid map seed. Length must be 16.");

            unsafe
            {
                Marshal.Copy(mapSeed, 0, (IntPtr)serverSettings.MapSeed, 16);
            }

            var typeBytes = BitConverter.GetBytes((UInt16)Type);
            int dataOffset = 3;
            sendData = new byte[dataSize];
            sendData[0] = typeBytes[0];
            sendData[1] = typeBytes[1];
            sendData[2] = Number;

            NetworkDataConverter<LobbyDataHeader>.ToBytes(header, sendData, ref dataOffset);
            NetworkDataConverter<LobbyServerSettings>.ToBytes(serverSettings, sendData, ref dataOffset);

            foreach (var player in players)
            {
                var playerData = new LobbyPlayerData();

                playerData.Type = 0;

                if (player.Identification != null)
                    playerData.Type = (byte)(player.IsHost ? 1 : 2);

                playerData.Face = (byte)player.Face;
                playerData.Supplies = (byte)player.Supplies;
                playerData.Reproduction = (byte)player.Reproduction;

                NetworkDataConverter<LobbyPlayerData>.ToBytes(playerData, sendData, ref dataOffset);

                if (playerData.Type == 0) // AI
                {
                    var aiPlayerData = new LobbyAIPlayerData();

                    aiPlayerData.Intelligence = (byte)player.Intelligence;

                    NetworkDataConverter<LobbyAIPlayerData>.ToBytes(aiPlayerData, sendData, ref dataOffset);
                }
                else // Human
                {
                    var humanPlayerData = new LobbyHumanPlayerData();

                    var ipParts = player.Identification.Split('.');

                    if (ipParts.Length != 4)
                        throw new ExceptionFreeserf("Invalid IP address.");

                    byte[] ipBytes = ipParts.Select(p => (byte)int.Parse(p)).ToArray();

                    unsafe
                    {
                        Marshal.Copy(ipBytes, 0, (IntPtr)humanPlayerData.IP, 4);
                    }

                    NetworkDataConverter<LobbyHumanPlayerData>.ToBytes(humanPlayerData, sendData, ref dataOffset);
                }
            }
        }

        public int Size => header.DataSize;

        public void Send(IRemote destination)
        {
            if (sendData == null)
                throw new ExceptionFreeserf("No data to send.");

            destination.Send(sendData);
        }

        unsafe public INetworkData Parse(byte[] rawData, ref int offset)
        {
            sendData = rawData;

            if (rawData.Length - offset == 2)
                throw new ExceptionFreeserf("Empty lobby data received.");
            else if (rawData.Length - offset < MIN_DATA_SIZE)
                throw new ExceptionFreeserf("Invalid lobby data received.");

            Number = rawData[offset + 2];
            header.DataSize = rawData[offset + 3];
            header.PlayerCount = rawData[offset + 4];

            if (header.PlayerCount > 4)
                throw new ExceptionFreeserf("Lobby player count was > 4. Lobby data is corrupted.");

            int dataIndex = offset + 5;
            var serverSettings = NetworkDataConverter<LobbyServerSettings>.FromBytes(rawData, ref dataIndex);

            byte[] mapSeed = new byte[16];
            Marshal.Copy((IntPtr)serverSettings.MapSeed, mapSeed, 0, 16);
            ServerInfo = new LobbyServerInfo(
                (serverSettings.Flags & 0x01) != 0,
                (serverSettings.Flags & 0x02) != 0,
                serverSettings.MapSize,
                Encoding.ASCII.GetString(mapSeed)
            );

            for (int i = 0; i < header.PlayerCount; ++i)
            {
                var player = NetworkDataConverter<LobbyPlayerData>.FromBytes(rawData, ref dataIndex);

                LobbyPlayerInfo playerInfo;

                if (player.Type == 0) // AI
                {
                    var aiPlayer = NetworkDataConverter<LobbyAIPlayerData>.FromBytes(rawData, ref dataIndex);
                    playerInfo = new LobbyPlayerInfo(null, false, player.Face, player.Supplies, aiPlayer.Intelligence, player.Reproduction);
                }
                else // Human
                {
                    var humanPlayer = NetworkDataConverter<LobbyHumanPlayerData>.FromBytes(rawData, ref dataIndex);

                    byte[] ipBytes = new byte[4];
                    Marshal.Copy((IntPtr)humanPlayer.IP, ipBytes, 0, 4);
                    string ip = $"{ipBytes[0]}.{ipBytes[1]}.{ipBytes[2]}.{ipBytes[3]}";

                    playerInfo = new LobbyPlayerInfo(ip, player.Type == 1, player.Face, player.Supplies, 40, player.Reproduction);
                }

                players.Add(playerInfo);
            }

            offset = dataIndex;

            return this;
        }
    }
}
