using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Freeserf.Network
{
    public interface IRemote
    {
        IPAddress GetIP();
        void Send(byte[] rawData);
    }

    public interface IResponse
    {
        IRemote GetSource();
        INetworkData GetData();
    }

    public enum NetworkDataType : UInt16
    {
        Request,
        Heartbeat,
        LobbyData,
        PlayerData,
        MapData,
        GameData,
        UserActionData, // setting, building, demolishing, etc
        // TODO ...
    }

    public interface INetworkData
    {
        NetworkDataType Type { get; }
        int GetSize();
        void Send(IRemote destination);
        INetworkData Parse(byte[] rawData);
    }

    public static class NetworkDataParser
    {
        public static INetworkData Parse(byte[] rawData)
        {
            if (rawData.Length < 2)
                throw new ExceptionFreeserf("Unknown network data.");

            NetworkDataType type = (NetworkDataType)BitConverter.ToUInt16(rawData, 2);

            switch (type)
            {
                case NetworkDataType.Request:
                    return new RequestData().Parse(rawData);
                case NetworkDataType.Heartbeat:
                    return new Heartbeat().Parse(rawData);
                case NetworkDataType.LobbyData:
                    return new LobbyData().Parse(rawData);
                // TODO ...
                default:
                    throw new ExceptionFreeserf("Unknown network data.");
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

            return obj;
        }
    }
}
