/*
 * JoinData.cs - Identification of players for rejoining a game
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

using System;
using System.Collections.Generic;

namespace Freeserf.Network
{
    /// <summary>
    /// Identifies a player of a multiplayer game.
    ///
    /// S2C: The server tells a client which token belongs to its player.
    /// C2S: A client sends its token after connecting. If the game is already
    ///      running, the server uses it to give the client its player back.
    /// </summary>
    public class JoinData : INetworkData
    {
        public NetworkDataType Type => NetworkDataType.Join;

        public byte MessageIndex
        {
            get;
            private set;
        } = 0;

        /// <summary>
        /// Id of the server's game session (0 if unknown).
        /// </summary>
        public ulong SessionId { get; private set; } = 0;
        /// <summary>
        /// Token of the player (0 if unknown).
        /// </summary>
        public ulong Token { get; private set; } = 0;

        public JoinData()
        {
            // use when parsing the data
        }

        public JoinData(ulong sessionId, ulong token)
        {
            MessageIndex = Global.SpontaneousMessage;
            SessionId = sessionId;
            Token = token;
        }

        public int Size => 19;

        public string LogName => "Join data";

        public INetworkData Parse(byte[] rawData, ref int offset)
        {
            if (rawData.Length - offset < Size)
                throw new ExceptionFreeserf(ErrorSystemType.Network, $"Join data length must be {Size}.");

            MessageIndex = rawData[offset + 2];
            SessionId = BitConverter.ToUInt64(rawData, offset + 3);
            Token = BitConverter.ToUInt64(rawData, offset + 11);

            offset += Size;

            return this;
        }

        public void Send(IRemote destination)
        {
            var rawData = new List<byte>(Size);

            rawData.AddRange(BitConverter.GetBytes((UInt16)Type));
            rawData.Add(MessageIndex);
            rawData.AddRange(BitConverter.GetBytes(SessionId));
            rawData.AddRange(BitConverter.GetBytes(Token));

            destination.Send(rawData.ToArray());
        }

        public static ulong CreateRandomId()
        {
            ulong id = 0;

            while (id == 0)
                id = BitConverter.ToUInt64(System.Security.Cryptography.RandomNumberGenerator.GetBytes(8));

            return id;
        }
    }
}
