/*
 * NetworkData.cs - Basic network data interfaces
 *
 * Copyright (C) 2019-2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Net;
using System.Runtime.InteropServices;

namespace Freeserf.Network
{
    public interface IRemote
    {
        void Send(byte[] rawData);
    }

    public enum NetworkDataType : UInt16
    {
        Request,
        Response,
        Heartbeat,
        InSync,
        LobbyData,
        SyncData,
        UserActionData // setting, building, demolishing, etc
    }

    public interface INetworkData
    {
        NetworkDataType Type { get; }
        byte MessageIndex { get; }
        int Size { get; }
        void Send(IRemote destination);
        INetworkData Parse(byte[] rawData, ref int offset);
        string LogName { get; }
    }

    public static class NetworkDataParser
    {
        public static IEnumerable<INetworkData> Parse(byte[] rawData)
        {
            if (rawData.Length < 2)
            {
                Log.Error.Write(ErrorSystemType.Network, $"Invalid network data received: {rawData}");
                throw new ExceptionFreeserf("Invalid network data received.");
            }

            int offset = 0;

            while (offset < rawData.Length)
            {
                NetworkDataType type = (NetworkDataType)BitConverter.ToUInt16(rawData, offset);

                switch (type)
                {
                    case NetworkDataType.Request:
                        yield return new RequestData().Parse(rawData, ref offset);
                        break;
                    case NetworkDataType.Response:
                        yield return new ResponseData().Parse(rawData, ref offset);
                        break;
                    case NetworkDataType.Heartbeat:
                        yield return new Heartbeat().Parse(rawData, ref offset);
                        break;
                    case NetworkDataType.LobbyData:
                        yield return new LobbyData().Parse(rawData, ref offset);
                        break;
                    case NetworkDataType.InSync:
                        yield return new InSyncData().Parse(rawData, ref offset);
                        break;
                    case NetworkDataType.SyncData:
                        yield return new SyncData().Parse(rawData, ref offset);
                        break;
                    case NetworkDataType.UserActionData:
                        yield return new UserActionData().Parse(rawData, ref offset);
                        break;
                    // TODO ...
                    default:
                        Log.Error.Write(ErrorSystemType.Network, $"Unknown network data received: {rawData}");
                        throw new ExceptionFreeserf("Unknown network data.");
                }
            }
        }
    }

    public static class NetworkDataConverter<T> where T : struct
    {
        public static byte[] ToBytes(T data)
        {
            int size = Marshal.SizeOf(data);
            byte[] buffer = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, buffer, 0, size);
            Marshal.FreeHGlobal(ptr);

            return buffer;
        }

        public static void ToBytes(T data, byte[] buffer, ref int offset)
        {
            int size = Marshal.SizeOf(data);

            if (offset + size > buffer.Length)
                throw new ExceptionFreeserf("Buffer is too small for network data.");

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, buffer, offset, size);
            Marshal.FreeHGlobal(ptr);

            offset += size;
        }

        public static T FromBytes(byte[] data, ref int offset)
        {
            T obj = new T();

            int size = Marshal.SizeOf(obj);

            if (offset + size > data.Length)
                throw new ExceptionFreeserf("Network data is too short or invalid.");

            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(data, offset, ptr, size);

            obj = (T)Marshal.PtrToStructure(ptr, obj.GetType());

            Marshal.FreeHGlobal(ptr);

            offset += size;

            return obj;
        }
    }
}
