using System;
using System.Collections.Generic;

namespace Freeserf.Network
{
    public enum ResponseType
    {
        /// <summary>
        /// The request was processed successfully.
        /// </summary>
        Ok,
        /// <summary>
        /// The request was invalid.
        /// </summary>
        BadRequest,
        /// <summary>
        /// The request was not possible in the current state.
        /// </summary>
        BadState,
        /// <summary>
        /// The request was not for this destination.
        /// For example if requests were send to a client
        /// which should be send to the server or when
        /// sending to the wrong client.
        /// </summary>
        BadDestination,
        /// <summary>
        /// Request could not be processed successfully.
        /// </summary>
        Failed,
        /// <summary>
        /// Invalid response code.
        /// </summary>
        Invalid = 0xff
    }

    /// <summary>
    /// General response with a simple status.
    /// </summary>
    public class ResponseData : INetworkData
    {
        public NetworkDataType Type => NetworkDataType.Response;

        public byte MessageIndex
        {
            get;
            private set;
        } = 0;

        public ResponseType ResponseType
        {
            get;
            private set;
        }

        public int Size => 4;

        public string LogName => $"Response '{ResponseType}'";

        public ResponseData()
        {
            // use when parsing the data
        }

        public ResponseData(byte number, ResponseType responseType)
        {
            MessageIndex = number;
            ResponseType = responseType;
        }

        public INetworkData Parse(byte[] rawData, ref int offset)
        {
            if (rawData.Length - offset != Size)
                throw new ExceptionFreeserf($"Response length must be {Size}.");

            MessageIndex = rawData[offset + 2];

            if (rawData[offset + 3] > (byte)ResponseType.Failed)
                ResponseType = ResponseType.Invalid;
            else
                ResponseType = (ResponseType)rawData[offset + 3];

            offset += Size;

            return this;
        }

        public void Send(IRemote destination)
        {
            List<byte> rawData = new List<byte>(Size);

            rawData.AddRange(BitConverter.GetBytes((UInt16)Type));
            rawData.Add(MessageIndex);
            rawData.Add((byte)ResponseType);

            destination?.Send(rawData.ToArray());
        }
    }
}
