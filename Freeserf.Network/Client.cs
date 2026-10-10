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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Freeserf.Network
{
    internal class LocalClient : ILocalClient
    {
        TcpClient client = null;
        RemoteServer server = null;
        ServerState serverState = ServerState.Offline;
        DateTime lastServerHeartbeat = DateTime.MinValue;
        DateTime lastOwnHearbeat = DateTime.MinValue; // Every sent message counts as a heartbeat!
        ConnectionObserver connectionObserver = null;
        CancellationTokenSource disconnectToken = new CancellationTokenSource();
        readonly List<Action<ResponseData>> registeredResponseHandlers = new List<Action<ResponseData>>();
        readonly List<Action<Heartbeat>> registeredHeartbeatHandlers = new List<Action<Heartbeat>>();
        // State hashes of the checkpoints (key: tick) of the own game and of the server's game.
        readonly Dictionary<uint, byte[]> ownStateHashes = new Dictionary<uint, byte[]>();
        readonly Dictionary<uint, byte[]> serverStateHashes = new Dictionary<uint, byte[]>();
        const int MaxStoredStateHashes = 16;
        // A game state update sent by the server (e.g. after a user action or an AI action
        // shortly before a checkpoint) may arrive after the checkpoint was compared. So an
        // update is only requested if none arrived within this delay after a mismatch.
        const double OutOfSyncGraceSeconds = 1.0;
        DateTime? outOfSyncTime = null;
        bool gameStateUpdateRequested = false;
        bool playerIndexKnown = false;
        volatile bool connectionLost = false;
        Game game = null;

        public LocalClient()
        {
            Ip = Host.GetLocalIpAddress();

            if (Ip == null)
            {
                Log.Error.Write(ErrorSystemType.Network, "Unable to retrieve local IP.");
                Ip = IPAddress.Loopback; // Is this ok or should we throw here?
            }
        }

        public IPAddress Ip
        {
            get;
        }

        public uint PlayerIndex
        {
            get;
            private set;
        }

        public Game Game
        {
            get => game;
            set
            {
                if (game == value)
                    return;

                game = value;
                ClearStateHashes();

                Freeserf.Game.CheckpointReached -= Game_CheckpointReached;

                if (game != null)
                    Freeserf.Game.CheckpointReached += Game_CheckpointReached;
            }
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
            Freeserf.Game.CheckpointReached -= Game_CheckpointReached;

            lock (registeredResponseHandlers)
            {
                registeredResponseHandlers.Clear();
            }
            lock (registeredHeartbeatHandlers)
            {
                registeredHeartbeatHandlers.Clear();
            }

            server?.Close();
            client?.Close();
            client = null;
            server = null;
            Disconnected?.Invoke(this, EventArgs.Empty);
            serverState = ServerState.Offline;
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
                    client.Connect(Ip, Global.NetworkPort);
                else
                    client.Connect(ip, Global.NetworkPort);

                // A previous connection may have cancelled the old token.
                disconnectToken = new CancellationTokenSource();
                connectionLost = false;
                server = new RemoteServer(name, ip, client);
                server.DataReceived += Server_DataReceived;
                server.ConnectionLost += ConnectionObserver_ConnectionLost;
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

        // This is called by the connection observer thread or while sending.
        // The disconnect is handled on the main thread (UpdateNetworkEvents).
        void ConnectionObserver_ConnectionLost()
        {
            connectionLost = true;
        }

        void ConnectionObserver_DataRefreshNeeded()
        {
            // long response time -> data refresh from server is needed

            switch (serverState)
            {
                case ServerState.Lobby:
                    RequestLobbyStateUpdate();
                    break;
                    // TODO: what to do while loading?
                case ServerState.Game:
                    RequestGameStateUpdate();
                    break;
            }
        }

        public void UpdateNetworkEvents()
        {
            void handleReceivedData(IRemote source, INetworkData data, ResponseHandler responseHandler)
            {
                if (source is not IRemoteServer server)
                {
                    Log.Error.Write(ErrorSystemType.Network, "Client received data from an unsupported server.");
                    responseHandler?.Invoke(ResponseType.BadDestination);
                    return;
                }

                ProcessData(server, data, responseHandler);
            }

            NetworkDataReceiver?.ProcessReceivedData(handleReceivedData);

            if (connectionLost)
            {
                connectionLost = false;

                if (server != null)
                {
                    Log.Verbose.Write(ErrorSystemType.Network, "Lost connection to the server.");
                    HandleDisconnect();
                }

                return;
            }

            if (outOfSyncTime != null && (DateTime.UtcNow - outOfSyncTime.Value).TotalSeconds >= OutOfSyncGraceSeconds)
            {
                outOfSyncTime = null;

                if (!gameStateUpdateRequested && Connected)
                {
                    Log.Verbose.Write(ErrorSystemType.Network, "Game state is still out of sync. Requesting game state update.");
                    gameStateUpdateRequested = true;
                    RequestGameStateUpdate();
                }
            }
        }

        void ClearStateHashes()
        {
            ownStateHashes.Clear();
            serverStateHashes.Clear();
            outOfSyncTime = null;
        }

        static void StoreStateHash(Dictionary<uint, byte[]> hashes, uint tick, byte[] hash)
        {
            hashes[tick] = hash;

            // Only keep the latest checkpoints.
            while (hashes.Count > MaxStoredStateHashes)
                hashes.Remove(hashes.Keys.Min());
        }

        void Game_CheckpointReached(Game game)
        {
            if (game != Game || serverState != ServerState.Game)
                return;

            var hash = GameStateSerializer.ComputeHash(game);

            StoreStateHash(ownStateHashes, game.ConstTick, hash);
            CompareStateHashes(game.ConstTick);
        }

        void CompareStateHashes(uint tick)
        {
            if (!ownStateHashes.TryGetValue(tick, out var ownHash) ||
                !serverStateHashes.TryGetValue(tick, out var serverHash))
                return; // compared when both are available

            if (ownHash.AsSpan().SequenceEqual(serverHash))
            {
                Log.Verbose.Write(ErrorSystemType.Network, $"Game state is in sync at tick {tick}.");
                return;
            }

            Log.Verbose.Write(ErrorSystemType.Network, $"Game state is out of sync at tick {tick}.");

            outOfSyncTime ??= DateTime.UtcNow;
        }

        void ProcessData(IRemoteServer server, INetworkData networkData, ResponseHandler responseHandler)
        {
            switch (networkData.Type)
            {
                case NetworkDataType.Request:
                    HandleRequest(networkData as RequestData, responseHandler);
                    break;
                case NetworkDataType.Heartbeat:
                    {
                        var heartbeat = networkData as Heartbeat;
                        // Last heartbeat time was set before.
                        // The server always sends our player index.
                        PlayerIndex = heartbeat.PlayerId;
                        playerIndexKnown = true;
                        foreach (var registeredHeartbeatHandler in registeredHeartbeatHandlers.ToArray())
                            registeredHeartbeatHandler?.Invoke(heartbeat);
                        responseHandler?.Invoke(ResponseType.Ok);
                        break;
                    }
                case NetworkDataType.LobbyData:
                    if (serverState != ServerState.Lobby)
                        responseHandler?.Invoke(ResponseType.BadState);
                    else
                    {
                        responseHandler?.Invoke(ResponseType.Ok);
                        UpdateLobbyData(networkData as LobbyData);
                    }
                    break;
                case NetworkDataType.Response:
                    {
                        var responseData = networkData as ResponseData;
                        foreach (var registeredResponseHandler in registeredResponseHandlers.ToArray())
                            registeredResponseHandler?.Invoke(responseData);
                        break;
                    }
                case NetworkDataType.InSync:
                    {
                        if (serverState != ServerState.Game)
                            break; // the game state is sent after loading anyway

                        if (Game == null)
                        {
                            serverState = ServerState.Offline;
                            return;
                        }

                        var inSyncData = networkData as InSyncData;

                        // The server may be ahead or behind. The hashes are compared
                        // as soon as both states of the checkpoint tick are known.
                        StoreStateHash(serverStateHashes, inSyncData.Tick, inSyncData.StateHash);
                        CompareStateHashes(inSyncData.Tick);
                        break;
                    }
                case NetworkDataType.SyncData:
                    {
                        if (serverState != ServerState.Game &&
                            serverState != ServerState.Loading)
                        {
                            responseHandler?.Invoke(ResponseType.BadState);
                        }
                        else
                        {
                            if (Game == null)
                            {
                                serverState = ServerState.Offline;
                                responseHandler?.Invoke(ResponseType.BadState);
                                return;
                            }

                            var syncData = networkData as SyncData;

                            if (!syncData.Full)
                            {
                                // The server only sends full states at the moment.
                                Log.Error.Write(ErrorSystemType.Network, "Partial game state updates are not supported. Requesting full update.");
                                RequestGameStateUpdate();
                                break;
                            }

                            try
                            {
#if DEBUG
                                var stopWatch = System.Diagnostics.Stopwatch.StartNew();
                                Log.Verbose.Write(ErrorSystemType.Network, "Processing sync ... ");
#endif
                                lock (Game)
                                {
                                    GameStateSerializer.DeserializeInto(Game, syncData.SerializedData, false, true);
                                }

                                // The own checkpoint hashes are no longer valid. The server's hashes of
                                // later checkpoints are kept to verify the new state when they are reached.
                                ownStateHashes.Clear();
                                outOfSyncTime = null;
                                gameStateUpdateRequested = false;

#if DEBUG
                                Log.Verbose.Write(ErrorSystemType.Network, $"Processing sync done in {stopWatch.ElapsedMilliseconds / 1000.0} seconds");
#endif
                            }
                            catch (Exception ex)
                            {
                                Log.Error.Write(ErrorSystemType.Network, "Failed to update game state: " + ex.Message);
                                Disconnect();
                                throw; // TODO: Close game instead of crash?
                            }
                        }
                        break;
                    }
                default:
                    // Should have been handled by Server_DataReceived already.
                    break;
            }
        }

        void Server_DataReceived(IRemoteServer server, byte[] data)
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Received {data.Length} byte(s) of data from server '{server.Ip}'.");

            lastServerHeartbeat = DateTime.UtcNow;

            if (NetworkDataReceiver == null)
            {
                Log.Error.Write(ErrorSystemType.Application, "Network data receiver is not set up.");
                Disconnect();                
                return;
            }

            if (serverState == ServerState.Offline)
                serverState = ServerState.Lobby;

            try
            {
                foreach (var parsedData in NetworkDataParser.Parse(data))
                {
                    Log.Verbose.Write(ErrorSystemType.Network, $"Received {parsedData.LogName} (message index {parsedData.MessageIndex}).");

                    switch (parsedData.Type)
                    {
                        case NetworkDataType.Heartbeat:
                            // Last heartbeat time was set above.
                            // Heartbeats contain our player index so they are always processed.
                            NetworkDataReceiver.Receive(server, parsedData, null);
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
                    if (Game != null)
                    {
                        responseHandler?.Invoke(ResponseType.BadState);
                        return;
                    }
                    serverState = ServerState.Loading;
                    if (!playerIndexKnown)
                    {
                        // We have to wait for first heartbeat to set the player index.
                        SendHeartbeatRequest(response =>
                        {
                            GameStarted?.Invoke(this, EventArgs.Empty);
                            SendStartGameRequest();
                        });
                    }
                    else
                    {
                        GameStarted?.Invoke(this, EventArgs.Empty);
                        SendStartGameRequest();
                    }
                    return;
                case Request.AllowUserInput:
                    if (serverState == ServerState.Loading)
                        serverState = ServerState.Game;
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
                    SendHeartbeat(request.MessageIndex);
                    break;
                default:
                    SendResponse(request.MessageIndex, ResponseType.Ok);
                    break;
            }
        }

        private void UpdateLobbyData(LobbyData data)
        {
            // The player index is normally known from the server's heartbeat.
            // The IP is only a fallback as several clients may share it.
            if (!playerIndexKnown)
            {
                foreach (var player in data.Players)
                {
                    if (player != null && player.PlayerIndex != 0u && player.Identification == Ip.ToString())
                    {
                        PlayerIndex = player.PlayerIndex;
                        break;
                    }
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

        public void SendHeartbeat(byte messageIndex)
        {
            // Only send heartbeat every second
            if ((DateTime.UtcNow - lastOwnHearbeat).TotalSeconds < 1.0)
                return;

            Log.Verbose.Write(ErrorSystemType.Network, $"Send heartbeat to server.");
            lastOwnHearbeat = DateTime.UtcNow;
            new Heartbeat(messageIndex, (byte)PlayerIndex).Send(server);
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

        public void SendHeartbeatRequest(Action<Heartbeat> responseAction)
        {
            var request = new RequestData(Global.GetNextMessageIndex(), Request.Heartbeat);
            RegisterHeartbeatResponse(request.MessageIndex, responseAction);
            request.Send(server);
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

        private void RegisterHeartbeatResponse(byte messageIndex, Action<Heartbeat> heartbeatAction)
        {
            void heartbeatHandler(Heartbeat heartbeat)
            {
                if (heartbeat.MessageIndex == messageIndex)
                {
                    lock (registeredHeartbeatHandlers)
                    {
                        registeredHeartbeatHandlers.Remove(heartbeatHandler);
                    }

                    heartbeatAction?.Invoke(heartbeat);
                }
            }

            registeredHeartbeatHandlers.Add(heartbeatHandler);
        }
    }

    internal class RemoteClient : ConnectionObserver, IRemoteClient
    {
        readonly TcpClient client = new TcpClient();
        public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
        readonly CancellationTokenSource disconnectToken = new CancellationTokenSource();

        public RemoteClient(uint playerIndex, ILocalServer server, TcpClient client)
            : base(200)
        {
            Ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
            PlayerIndex = playerIndex;
            Server = server;
            this.client = client;

            lastHeartbeatTimeProvider = () => LastHeartbeat;
            cancellationToken = disconnectToken.Token;

            Run();
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

        public void SendHeartbeat(byte messageIndex)
        {
            new Heartbeat(messageIndex, (byte)PlayerIndex).Send(this);
        }

        public void SendDisconnect()
        {
            new RequestData(Global.GetNextMessageIndex(), Request.Disconnect).Send(this);
        }

        public void CancelConnectionObserving()
        {
            disconnectToken.Cancel();
        }

        public void Send(byte[] rawData)
        {
            if (client != null && client.Connected)
            {
                try
                {
                    Host.WriteData(client.GetStream(), rawData);
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

        public void SendGameStateUpdate(byte messageIndex, Game game, bool fullState)
        {
            byte[] data = GameStateSerializer.SerializeFrom(game, fullState);
            new SyncData(messageIndex, game.GameTime, data, fullState).Send(this);
        }

        public void SendInSyncMessage(UInt32 tick, byte[] stateHash)
        {
            new InSyncData(tick, stateHash).Send(this);
        }
    }

    public class ClientFactory : IClientFactory
    {
        public ILocalClient CreateLocal()
        {
            return new LocalClient();
        }

        public IServerFinder CreateServerFinder()
        {
            return new ServerFinder();
        }
    }
}
