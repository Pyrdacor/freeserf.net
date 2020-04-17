/*
 * RequestData.cs - Data for network requests
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
    public enum Request : byte
    {
        None = 0,
        Heartbeat,
        StartGame,
        Disconnect,
        LobbyData,
        GameData,
        AllowUserInput,
        DisallowUserInput,
        Pause,
        Resume
    }

    public partial class Global
    {
        public const byte SpontaneousMessage = 0xff;

        static byte CurrentMessageIndex = 0;
        static object MessageIndexLock = new object();

        public static byte GetNextMessageIndex()
        {
            lock (MessageIndexLock)
            {
                byte currentMessageIndex = CurrentMessageIndex;

                if (CurrentMessageIndex == SpontaneousMessage - 1)
                    CurrentMessageIndex = 0;
                else
                    ++CurrentMessageIndex;

                return currentMessageIndex;
            }
        }
    }

    public class RequestData : INetworkData
    {
        public NetworkDataType Type => NetworkDataType.Request;

        public byte MessageIndex
        {
            get;
            private set;
        } = 0;

        public Request Request
        {
            get;
            private set;
        } = Request.None;

        public RequestData()
        {
            // use when parsing the data
        }

        public RequestData(byte number, Request request)
        {
            MessageIndex = number;
            Request = request;
        }

        public int Size => 4;

        public INetworkData Parse(byte[] rawData, ref int offset)
        {
            if (rawData.Length - offset < Size)
                throw new ExceptionFreeserf($"Request length must be {Size}.");

            MessageIndex = rawData[offset + 2];

            var possibleValues = Enum.GetValues(typeof(Request));

            foreach (Request possibleValue in possibleValues)
            {
                if ((byte)possibleValue == rawData[offset + 3])
                {
                    Request = possibleValue;
                    offset += Size;
                    return this;
                }
            }

            throw new ExceptionFreeserf("Invalid request.");
        }

        public void Send(IRemote destination)
        {
            List<byte> rawData = new List<byte>(Size);

            rawData.AddRange(BitConverter.GetBytes((UInt16)Type));
            rawData.Add(MessageIndex);
            rawData.Add((byte)Request);

            destination?.Send(rawData.ToArray());
        }
    }
}
