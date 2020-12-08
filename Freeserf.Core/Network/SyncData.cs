﻿/*
 * SyncData.cs - Sync message data
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
    public class SyncData : INetworkData
    {
        public NetworkDataType Type => NetworkDataType.SyncData;

        public byte MessageIndex
        {
            get;
            private set;
        } = 0;

        public UInt32 GameTime
        {
            get;
            private set;
        } = 0u;

        public bool Full
        {
            get;
            private set;
        } = false;

        public byte[] SerializedData
        {
            get;
            private set;
        }

        public SyncData()
        {
            // use when parsing the data
        }

        public SyncData(byte messageIndex, uint gameTime, byte[] serializedData, bool full)
        {
            MessageIndex = messageIndex;
            GameTime = gameTime;
            SerializedData = serializedData;
            Full = full;
            Size = 12 + serializedData.Length;
        }

        public int Size
        {
            get;
            private set;
        }

        public string LogName => "Sync data";

        public INetworkData Parse(byte[] rawData, ref int offset)
        {
            if (rawData.Length - offset == 2)
                throw new ExceptionFreeserf("Empty sync data received.");

            MessageIndex = rawData[offset + 2];
            GameTime = BitConverter.ToUInt32(rawData, offset + 3);
            Full = rawData[offset + 7] != 0;
            int dataSize = (int)(BitConverter.ToUInt32(rawData, offset + 8) & 0x7fffffff);
            Size = 12 + dataSize;

            if (rawData.Length - offset < Size)
                throw new ExceptionFreeserf($"Sync data length must be {Size}.");

            SerializedData = new byte[dataSize];
            Buffer.BlockCopy(rawData, offset + 12, SerializedData, 0, SerializedData.Length);

            offset += Size;

            return this;
        }

        public void Send(IRemote destination)
        {
            List<byte> rawData = new List<byte>(Size);

            rawData.AddRange(BitConverter.GetBytes((UInt16)Type));
            rawData.Add(MessageIndex);
            rawData.AddRange(BitConverter.GetBytes(GameTime));
            rawData.Add((byte)(Full ? 1 : 0));
            rawData.AddRange(BitConverter.GetBytes((UInt32)SerializedData.Length));
            rawData.AddRange(SerializedData);

            destination.Send(rawData.ToArray());
        }
    }
}
