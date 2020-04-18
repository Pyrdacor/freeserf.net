/*
 * Client.cs - Freeserf clients
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
using System.Net.Sockets;
using System.Threading;

namespace Freeserf.Network
{
    public class LocalClient : ILocalClient
    {
        TcpClient client = null;
        RemoteServer server = null;
        DateTime lastServerHeartbeat = DateTime.MinValue;
        DateTime lastOwnHearbeat = DateTime.MinValue; // Every sent message counts as a heartbeat!
        ConnectionObserver connectionObserver = null;
        readonly CancellationTokenSource disconnectToken = new CancellationTokenSource();
        readonly List<Action<ResponseData>> registeredResponseHandlers = new List<Action<ResponseData>>();

        public LocalClient()
        {
            Ip = Host.GetLocalIpAddress();
        }

        public IPAddress Ip
        {
            get;
        }

        // TODO: get from server and set it here
        public uint PlayerIndex
        {
            get;
            private set;
        }

        // TODO: needed
        public Game Game
        {
            get;
            set;
        }

        public IRemoteServer Server => server;

        public bool Connected => client != null && client.Connected;

        public INetworkDataReceiver NetworkDataReceiver { get; set; }

        public LobbyData LobbyData
        {
            get;
            private set;
        } = null;

        public event EventHandler Disconnected;
        public event EventHandler LobbyDataUpdated;
        public event EventHandler GameStarted;
        public event EventHandler GamePaused;
        public event EventHandler GameResumed;
        public event EventHandler InputAllowed;
        public event EventHandler InputDisallowed;

        public void Disconnect()
        {
            try
            {
                SendDisconnect();
            }
            catch
            {
                // ignore
            }

            disconnectToken?.Cancel();

            if (connectionObserver != null)
            {
                connectionObserver.ConnectionLost -= ConnectionObserver_ConnectionLost;
                connectionObserver.DataRefreshNeeded -= ConnectionObserver_DataRefreshNeeded;
            }

            HandleDisconnect();
        }

        private void HandleDisconnect()
        {
            lock (registeredResponseHandlers)
            {
                registeredResponseHandlers.Clear();
            }
            client?.Close();
            server?.Close();
            client = null;
            server = null;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public bool JoinServer(string name, IPAddress ip)
        {
            // if already connected to another server, first disconnect
            if (server != null || client != null)
            {
                Disconnect();
            }

            try
            {
                client = new TcpClient(new IPEndPoint(Ip, 0));

                if (IPAddress.IsLoopback(ip))
                    client.Connect(Host.GetLocalIpAddress(), Global.NetworkPort);
                else
                    client.Connect(ip, Global.NetworkPort);

                server = new RemoteServer(name, ip, client);
                server.DataReceived += Server_DataReceived;
                lastServerHeartbeat = DateTime.UtcNow;

                connectionObserver = new ConnectionObserver(() => lastServerHeartbeat, 200, disconnectToken.Token);

                connectionObserver.DataRefreshNeeded += ConnectionObserver_DataRefreshNeeded;
                connectionObserver.ConnectionLost += ConnectionObserver_ConnectionLost;

                connectionObserver.Run();

                return true;
            }
            catch
            {
                return false;
            }
        }

        void ConnectionObserver_ConnectionLost()
        {
            HandleDisconnect();
        }

        void ConnectionObserver_DataRefreshNeeded()
        {
            // long response time -> data refresh from server is needed
            RequestGameStateUpdate();
        }

        public void UpdateNetworkEvents()
        {
            void handleReceivedData(IRemote source, INetworkData data, ResponseHandler responseHandler)
            {
                if (!(source is IRemoteServer server))
                {
                    Log.Error.Write(ErrorSystemType.Network, "Client received data from a non-server.");
                    responseHandler?.Invoke(ResponseType.BadDestination);
                    return;
                }

                ProcessData(server, data, responseHandler);
            }

            NetworkDataReceiver?.ProcessReceivedData(handleReceivedData);
        }

        void ProcessData(IRemoteServer server, INetworkData networkData, ResponseHandler responseHandler)
        {
            switch (networkData.Type)
            {
                case NetworkDataType.Request:
                    HandleRequest(networkData as RequestData, responseHandler);
                    break;
                case NetworkDataType.Heartbeat:
                    // Last heartbeat time was set before.
                    responseHandler?.Invoke(ResponseType.Ok);
                    break;
                case NetworkDataType.LobbyData:
                    responseHandler?.Invoke(ResponseType.Ok);
                    UpdateLobbyData(networkData as LobbyData);
                    break;
                case NetworkDataType.Response:
                    {
                        var responseData = networkData as ResponseData;
                        foreach (var registeredResponseHandler in registeredResponseHandlers.ToArray())
                            registeredResponseHandler?.Invoke(responseData);
                        break;
                    }
                case NetworkDataType.InSync:
                    // TODO: handle in sync
                    break;
                case NetworkDataType.SyncData:
                    // TODO: handle sync
                    break;
                default:
                    // Should be handled by Server_DataReceived already.
                    break;
            }
        }

        void Server_DataReceived(IRemoteServer server, byte[] data)
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Received {data.Length} byte(s) of data from server '{server.Ip}'.");

            // TODO: handle client states
            lastServerHeartbeat = DateTime.UtcNow;

            if (NetworkDataReceiver == null)
            {
                Log.Error.Write(ErrorSystemType.Application, "Network data receiver is not set up.");
                Disconnect();                
                return;
            }

            try
            {
                foreach (var parsedData in NetworkDataParser.Parse(data))
                {
                    Log.Verbose.Write(ErrorSystemType.Network, $"Received {parsedData.LogName} (message index {parsedData.MessageIndex}).");

                    switch (parsedData.Type)
                    {
                        case NetworkDataType.Heartbeat:
                            // Last heartbeat time was set above.
                            break;
                        case NetworkDataType.Request:
                        case NetworkDataType.LobbyData:
                        case NetworkDataType.SyncData:
                            NetworkDataReceiver.Receive(server, parsedData, (ResponseType responseType) => SendResponse(parsedData.MessageIndex, responseType));
                            break;
                        case NetworkDataType.Response:
                        case NetworkDataType.InSync:
                            NetworkDataReceiver.Receive(server, parsedData, null);
                            break;
                        case NetworkDataType.UserActionData:
                            Log.Error.Write(ErrorSystemType.Network, "User actions can't be send to a client.");
                            SendResponse(parsedData.MessageIndex, ResponseType.BadDestination);                                
                            break;
                        default:
                            Log.Error.Write(ErrorSystemType.Network, "Received unknown server data.");
                            SendResponse(parsedData.MessageIndex, ResponseType.BadRequest);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Network, "Error in receiving server data: " + ex.Message);
            }
        }

        private void HandleRequest(RequestData request, ResponseHandler responseHandler)
        {
            switch (request.Request)
            {
                case Request.Disconnect:
                    // server closed or kicked player
                    HandleDisconnect();
                    break;
                case Request.Heartbeat:
                    // Response is send below.
                    break;
                case Request.StartGame:
                    GameStarted?.Invoke(this, EventArgs.Empty);
                    SendStartGameRequest();
                    // Start game messages are sent asynchronously.
                    return;
                case Request.AllowUserInput:
                    InputAllowed?.Invoke(this, EventArgs.Empty);
                    break;
                case Request.DisallowUserInput:
                    InputDisallowed?.Invoke(this, EventArgs.Empty);
                    break;
                case Request.Pause:
                    GamePaused?.Invoke(this, EventArgs.Empty);
                    break;
                case Request.Resume:
                    GameResumed?.Invoke(this, EventArgs.Empty);
                    break;
                default:
                    // all other requests can not be send to a client
                    throw new ExceptionFreeserf(ErrorSystemType.Network, $"Request {request} can not be send to a client.");
            }

            RespondToRequest(request);
        }

        private void RespondToRequest(RequestData request)
        {
            if (request.MessageIndex == Global.SpontaneousMessage)
                return; // spontaneous messages don't need a response

            switch (request.Request)
            {
                case Request.Heartbeat:
                    SendHeartbeatAsResponse(request.MessageIndex);
                    break;
                default:
                    SendResponse(request.MessageIndex, ResponseType.Ok);
                    break;
            }
        }

        private void UpdateLobbyData(LobbyData data)
        {
            for (int i = 0; i < data.Players.Count; ++i)
            {
                if (!data.Players[i].IsHost && data.Players[i].Identification == Ip.ToString())
                {
                    PlayerIndex = (uint)i;
                    break;
                }
            }

            LobbyData = data;
            LobbyDataUpdated?.Invoke(this, EventArgs.Empty);
        }

        public byte RequestLobbyStateUpdate()
        {
            byte messageIndex = Global.GetNextMessageIndex();

            Log.Verbose.Write(ErrorSystemType.Network, $"Send request 'Lobby state update' with message index {messageIndex} to server.");

            new RequestData(messageIndex, Request.LobbyData).Send(server);

            return messageIndex;
        }

        public byte RequestGameStateUpdate()
        {
            byte messageIndex = Global.GetNextMessageIndex();

            Log.Verbose.Write(ErrorSystemType.Network, $"Send request 'Game state update' with message index {messageIndex} to server.");

            // Note: A game data request is always a full sync request.
            new RequestData(messageIndex, Request.GameData).Send(server);

            return messageIndex;
        }

        public void SendHeartbeatAsResponse(byte messageIndex)
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Send heartbeat to server.");
            lastOwnHearbeat = DateTime.UtcNow;
            new Heartbeat(messageIndex, (byte)PlayerIndex).Send(server);
        }

        public void SendHeartbeat()
        {
            // Only send heartbeat every second
            if ((DateTime.UtcNow - lastOwnHearbeat).TotalSeconds < 1.0)
                return;

            SendHeartbeatAsResponse(Global.GetNextMessageIndex());
        }

        public void SendDisconnect()
        {
            Log.Verbose.Write(ErrorSystemType.Network, "Send disconnect to server.");
            lastOwnHearbeat = DateTime.UtcNow;
            new RequestData(Global.GetNextMessageIndex(), Request.Disconnect).Send(server);
        }

        private void SendStartGameRequest()
        {
            Log.Verbose.Write(ErrorSystemType.Network, "Send request 'Start game' to server.");
            lastOwnHearbeat = DateTime.UtcNow;
            new RequestData(Global.SpontaneousMessage, Request.StartGame).Send(server);
        }

        public void SendResponse(byte messageIndex, ResponseType responseType)
        {
            if (messageIndex != Global.SpontaneousMessage)
            {
                Log.Verbose.Write(ErrorSystemType.Network, $"Send response {responseType} to server.");
                lastOwnHearbeat = DateTime.UtcNow;
                new ResponseData(messageIndex, responseType).Send(server);
            }
        }

        public void SendUserAction(UserActionData userAction)
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Send user action {userAction.UserAction} to server.");
            lastOwnHearbeat = DateTime.UtcNow;
            userAction.Send(server);
        }

        public void SendUserAction(UserActionData userAction, Action<ResponseType> responseAction)
        {
            RegisterResponse(userAction.MessageIndex, responseAction);
            SendUserAction(userAction);
        }

        private void RegisterResponse(byte messageIndex, Action<ResponseType> responseAction)
        {
            void responseHandler(ResponseData response)
            {
                if (response.MessageIndex == messageIndex)
                {
                    lock (registeredResponseHandlers)
                    {
                        registeredResponseHandlers.Remove(responseHandler);
                    }

                    responseAction?.Invoke(response.ResponseType);
                }
            }

            registeredResponseHandlers.Add(responseHandler);
        }
    }

    public class RemoteClient : IRemoteClient
    {
        readonly TcpClient client = new TcpClient();

        public RemoteClient(uint playerIndex, ILocalServer server, TcpClient client)
        {
            Ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
            PlayerIndex = playerIndex;
            Server = server;
            this.client = client;
        }

        public IPAddress Ip
        {
            get;
        }

        public uint PlayerIndex
        {
            get;
        }

        public ILocalServer Server
        {
            get;
        }

        public void SendResponse(byte messageIndex, ResponseType responseType)
        {
            new ResponseData(messageIndex, responseType).Send(this);
        }

        public void SendHeartbeat()
        {
            new Heartbeat(Global.GetNextMessageIndex(), (byte)PlayerIndex).Send(this);
        }

        public void SendDisconnect()
        {
            new RequestData(Global.GetNextMessageIndex(), Request.Disconnect).Send(this);
        }

        public void Send(byte[] rawData)
        {
            if (client != null && client.Connected)
            {
                try
                {
                    client.GetStream().Write(rawData, 0, rawData.Length);
                }
                catch (System.IO.IOException)
                {
                    if (client.Connected)
                        throw;
                }
            }
        }

        public void SendLobbyDataUpdate(byte messageIndex, LobbyServerInfo serverInfo, List<LobbyPlayerInfo> players)
        {
            new LobbyData(messageIndex, serverInfo, players).Send(this);
        }

        public void SendGameStateUpdate(Game game)
        {
            // TODO: distinguish between patch / full
            byte[] data = new byte[] { }; // TODO: get from game
            new SyncData(game.GameTime, data).Send(this);
        }

        public void SendInSyncMessage(UInt32 gameTime)
        {
            new InSyncData(gameTime).Send(this);
        }
    }

    public class ClientFactory : IClientFactory
    {
        public ILocalClient CreateLocal()
        {
            return new LocalClient();
        }
    }
}
