/*
 * InSyncData.cs - In-sync message data
 *
 * Copyright (C) 2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
    /// Sent by the server at each game checkpoint (see <see cref="Game.CheckpointInterval"/>).
    /// It contains the hash of the server's game state at the given tick. A client with
    /// a different game state at that tick requests a full game state update.
    /// </summary>
    public class InSyncData : INetworkData
    {
        public const int HashSize = 20;

        public NetworkDataType Type => NetworkDataType.InSync;

        public byte MessageIndex => Global.SpontaneousMessage; // always async

        public UInt32 Tick
        {
            get;
            private set;
        } = 0u;

        public byte[] StateHash
        {
            get;
            private set;
        } = new byte[HashSize];

        public InSyncData()
        {
            // use when parsing the data
        }

        public InSyncData(uint tick, byte[] stateHash)
        {
            if (stateHash == null || stateHash.Length != HashSize)
                throw new ArgumentException($"State hash must have {HashSize} bytes.", nameof(stateHash));

            Tick = tick;
            StateHash = stateHash;
        }

        public int Size => 6 + HashSize;

        public string LogName => "In-sync message";

        public INetworkData Parse(byte[] rawData, ref int offset)
        {
            if (rawData.Length - offset == 2)
                throw new ExceptionFreeserf("Empty in-sync data received.");

            if (rawData.Length - offset < Size)
                throw new ExceptionFreeserf($"In-sync length must be {Size}.");

            Tick = BitConverter.ToUInt32(rawData, offset + 2);
            StateHash = new byte[HashSize];
            Buffer.BlockCopy(rawData, offset + 6, StateHash, 0, HashSize);

            offset += Size;

            return this;
        }

        public void Send(IRemote destination)
        {
            List<byte> rawData = new List<byte>(Size);

            rawData.AddRange(BitConverter.GetBytes((UInt16)Type));
            rawData.AddRange(BitConverter.GetBytes(Tick));
            rawData.AddRange(StateHash);

            destination.Send(rawData.ToArray());
        }
    }
}
