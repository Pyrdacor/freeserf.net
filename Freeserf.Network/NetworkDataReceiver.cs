/*
 * NetworkDataReceiver.cs - Freeserf network data receiver
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
    internal class NetworkDataReceiver : INetworkDataReceiver
    {
        readonly List<Action<ReceivedNetworkDataHandler>> receivedNetworkDataHandlers = new List<Action<ReceivedNetworkDataHandler>>();

        public void ProcessReceivedData(ReceivedNetworkDataHandler dataHandler)
        {
            lock (receivedNetworkDataHandlers)
            {
                foreach (var handler in receivedNetworkDataHandlers)
                    handler?.Invoke(dataHandler);

                receivedNetworkDataHandlers.Clear();
            }
        }

        public void Receive(IRemote source, INetworkData data, ResponseHandler responseHandler)
        {
            void receivedDataHandler(ReceivedNetworkDataHandler dataHandler)
            {
                dataHandler(source, data, responseHandler);
            }

            lock (receivedNetworkDataHandlers)
            {
                receivedNetworkDataHandlers.Add(receivedDataHandler);
            }
        }
    }

    public class NetworkDataReceiverFactory : INetworkDataReceiverFactory
    {
        public INetworkDataReceiver CreateReceiver()
        {
            return new NetworkDataReceiver();
        }
    }
}
