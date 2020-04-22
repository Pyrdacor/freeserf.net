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
    public class InSyncData : INetworkData
    {
        public NetworkDataType Type => NetworkDataType.InSync;

        public byte MessageIndex => Global.SpontaneousMessage; // always async

        public UInt32 GameTime
        {
            get;
            private set;
        } = 0u;

        public InSyncData()
        {
            // use when parsing the data
        }

        public InSyncData(uint gameTime)
        {
            GameTime = gameTime;
        }

        public int Size => 6;

        public string LogName => "In-sync message";

        public INetworkData Parse(byte[] rawData, ref int offset)
        {
            if (rawData.Length - offset == 2)
                throw new ExceptionFreeserf("Empty in-sync data received.");

            if (rawData.Length - offset < Size)
                throw new ExceptionFreeserf($"In-sync length must be {Size}.");

            GameTime = BitConverter.ToUInt32(rawData, offset + 2);

            offset += Size;

            return this;
        }

        public void Send(IRemote destination)
        {
            List<byte> rawData = new List<byte>(Size);

            rawData.AddRange(BitConverter.GetBytes((UInt16)Type));
            rawData.AddRange(BitConverter.GetBytes(GameTime));

            destination.Send(rawData.ToArray());
        }
    }
}
