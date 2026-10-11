/*
 * VoteData.cs - Messages of multiplayer votes
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
    public enum VoteMessage : byte
    {
        /// <summary>
        /// C2S: A player wants to start a vote.
        /// </summary>
        Request,
        /// <summary>
        /// S2C: A vote was started. All players except the requester have to answer it.
        /// </summary>
        Start,
        /// <summary>
        /// C2S: The answer of a player (value 1 = yes, 0 = no).
        /// </summary>
        Answer,
        /// <summary>
        /// S2C: The result of a vote (see <see cref="VoteResult"/>).
        /// </summary>
        Result
    }

    /// <summary>
    /// Multiplayer vote message. All votes are managed by the server.
    /// Each started vote has a unique id so answers can be assigned to it.
    /// </summary>
    public class VoteData : INetworkData
    {
        public NetworkDataType Type => NetworkDataType.Vote;

        public byte MessageIndex
        {
            get;
            private set;
        } = 0;

        public VoteMessage Message { get; private set; } = VoteMessage.Request;
        /// <summary>
        /// Id of the vote (not used for requests).
        /// </summary>
        public uint VoteId { get; private set; } = 0;
        /// <summary>
        /// The requesting player (or the answering player for answers).
        /// </summary>
        public byte PlayerIndex { get; private set; } = 0;
        public VoteType VoteType { get; private set; } = VoteType.Pause;
        public int Parameter { get; private set; } = 0;
        /// <summary>
        /// Answer (1 = yes, 0 = no) or result (see <see cref="VoteResult"/>).
        /// </summary>
        public byte Value { get; private set; } = 0;

        public VoteData()
        {
            // use when parsing the data
        }

        VoteData(VoteMessage message, uint voteId, uint playerIndex, VoteType voteType, int parameter, byte value)
        {
            MessageIndex = Global.SpontaneousMessage;
            Message = message;
            VoteId = voteId;
            PlayerIndex = (byte)playerIndex;
            VoteType = voteType;
            Parameter = parameter;
            Value = value;
        }

        public static VoteData CreateRequest(uint playerIndex, VoteType voteType, int parameter) =>
            new(VoteMessage.Request, 0, playerIndex, voteType, parameter, 0);

        public static VoteData CreateStart(Vote vote) =>
            new(VoteMessage.Start, vote.Id, vote.Requester, vote.Type, vote.Parameter, 0);

        public static VoteData CreateAnswer(uint voteId, uint playerIndex, bool yes) =>
            new(VoteMessage.Answer, voteId, playerIndex, VoteType.Pause, 0, (byte)(yes ? 1 : 0));

        public static VoteData CreateResult(uint voteId, uint requester, VoteType voteType, int parameter, VoteResult result) =>
            new(VoteMessage.Result, voteId, requester, voteType, parameter, (byte)result);

        public bool Yes => Value != 0;
        public VoteResult Result => (VoteResult)Value;

        public int Size => 15;

        public string LogName => $"Vote {Message} #{VoteId} ({VoteType} {Parameter})";

        public INetworkData Parse(byte[] rawData, ref int offset)
        {
            if (rawData.Length - offset < Size)
                throw new ExceptionFreeserf(ErrorSystemType.Network, $"Vote data length must be {Size}.");

            MessageIndex = rawData[offset + 2];
            Message = (VoteMessage)rawData[offset + 3];
            VoteId = BitConverter.ToUInt32(rawData, offset + 4);
            PlayerIndex = rawData[offset + 8];
            VoteType = (VoteType)rawData[offset + 9];
            Parameter = BitConverter.ToInt32(rawData, offset + 10);
            Value = rawData[offset + 14];

            offset += Size;

            return this;
        }

        public void Send(IRemote destination)
        {
            var rawData = new List<byte>(Size);

            rawData.AddRange(BitConverter.GetBytes((UInt16)Type));
            rawData.Add(MessageIndex);
            rawData.Add((byte)Message);
            rawData.AddRange(BitConverter.GetBytes(VoteId));
            rawData.Add(PlayerIndex);
            rawData.Add((byte)VoteType);
            rawData.AddRange(BitConverter.GetBytes(Parameter));
            rawData.Add(Value);

            destination.Send(rawData.ToArray());
        }
    }
}
