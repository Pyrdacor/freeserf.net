/*
 * Heartbeat.cs - Heartbeat message
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

namespace Freeserf.Network
{
    public class Heartbeat : INetworkData
    {
        const int Size = 12;

        public NetworkDataType Type => NetworkDataType.Heartbeat;

        public byte Number
        {
            get;
            private set;
        } = 0;

        public byte PlayerId
        {
            get;
            private set;
        } = 0;

        public DateTime Last
        {
            get;
            private set;
        } = DateTime.MinValue;

        public Heartbeat()
        {
            // use when parsing the data
        }

        public Heartbeat(byte number, byte playerId)
        {
            Number = number;
            PlayerId = playerId;
        }

        public int GetSize()
        {
            return Size;
        }

        public INetworkData Parse(byte[] rawData)
        {
            if (rawData.Length == 2)
                throw new ExceptionFreeserf("Empty heartbeat data received.");

            if (rawData.Length != Size)
                throw new ExceptionFreeserf($"Heartbeat length must be {Size}.");

            Number = rawData[2];
            Last = DateTime.FromBinary(BitConverter.ToInt64(rawData, 3));
            PlayerId = rawData[Size - 1];

            return this;
        }

        public void Send(IRemote destination)
        {
            List<byte> rawData = new List<byte>(Size);

            rawData.AddRange(BitConverter.GetBytes((UInt16)Type));
            rawData.Add(Number);
            rawData.AddRange(BitConverter.GetBytes(DateTime.UtcNow.ToBinary()));
            rawData.Add(PlayerId);

            destination.Send(rawData.ToArray());
        }
    }
}
