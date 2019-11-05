using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Freeserf.Network
{
    public enum Request : byte
    {
        None = 0,
        Heartbeat,
        Disconnect,
        LobbyData,
        PlayerData,
        MapData,
        GameData
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
        const int Size = 4;

        public NetworkDataType Type => NetworkDataType.Request;

        public byte Number
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
            Number = number;
            Request = request;
        }

        public int GetSize()
        {
            return Size;
        }

        public INetworkData Parse(byte[] rawData)
        {
            if (rawData.Length != Size)
                throw new ExceptionFreeserf($"Request length must be {Size}.");

            Number = rawData[2];

            var possibleValues = Enum.GetValues(typeof(Request));

            foreach (Request possibleValue in possibleValues)
            {
                if ((byte)possibleValue == rawData[3])
                {
                    Request = possibleValue;
                    return this;
                }
            }

            throw new ExceptionFreeserf("Invalid request.");
        }

        public void Send(IRemote destination)
        {
            List<byte> rawData = new List<byte>(GetSize());

            rawData.AddRange(BitConverter.GetBytes((UInt16)Type));
            rawData.Add(Number);
            rawData.Add((byte)Request);

            destination.Send(rawData.ToArray());
        }
    }
}
