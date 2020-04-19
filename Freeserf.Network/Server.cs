/*
 * Server.cs - Freeserf servers
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
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Freeserf.Network
{
    internal class Host
    {
        internal static byte[] ReadData(NetworkStream stream)
        {
            List<byte> largeBuffer = new List<byte>(4);
            byte[] buffer = new byte[1024];
            int numRead;

            do
            {
                numRead = stream.Read(buffer, 0, buffer.Length);
                largeBuffer.AddRange(buffer.Take(numRead));
            } while (numRead == buffer.Length);

            return largeBuffer.ToArray();
        }

        internal static IPAddress GetLocalIpAddress()
        {
            UnicastIPAddressInformation mostSuitableIp = null;
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var network in networkInterfaces)
            {
                if (network.OperationalStatus != OperationalStatus.Up)
                    continue;

                var properties = network.GetIPProperties();

                // TODO: do we care?
                //if (properties.GatewayAddresses.Count == 0)
                //    continue;

                foreach (var address in properties.UnicastAddresses)
                {
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    if (IPAddress.IsLoopback(address.Address))
                        continue;

                    if (!address.IsDnsEligible)
                    {
                        if (mostSuitableIp == null)
                            mostSuitableIp = address;
                        continue;
                    }

                    // The best IP is the IP got from DHCP server
                    if (address.PrefixOrigin != PrefixOrigin.Dhcp)
                    {
                        if (mostSuitableIp == null || !mostSuitableIp.IsDnsEligible)
                            mostSuitableIp = address;
                        continue;
                    }

                    return address.Address;
                }
            }

            return mostSuitableIp?.Address;
        }
    }

    public class LocalServer : ILocalServer
    {
        readonly object gameReadyLock = new object();
        bool acceptClients = true;
        readonly CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        Task listenerTask = null;
        TcpListener listener = null;
        LobbyServerInfo lobbyServerInfo = null;
        readonly List<LobbyPlayerInfo> lobbyPlayerInfo = new List<LobbyPlayerInfo>();
        readonly Dictionary<IRemoteClient, TcpClient> clients = new Dictionary<IRemoteClient, TcpClient>();
        readonly Dictionary<uint, IRemoteClient> playerClients = new Dictionary<uint, IRemoteClient>();
        readonly Dictionary<IRemoteClient, MultiplayerStatus> clientStatus = new Dictionary<IRemoteClient, MultiplayerStatus>();
        readonly Dictionary<IRemoteClient, DateTime> lastClientHeartbeats = new Dictionary<IRemoteClient, DateTime>();
        DateTime lastOwnHearbeat = DateTime.MinValue; // Every sent message counts as a heartbeat but only the broadcasted ones so no client is missed out.
        uint lastUserActionOrInSyncGameTime = 0;

        public LocalServer(string name, GameInfo gameInfo)
        {
            Name = name;
            Ip = Host.GetLocalIpAddress();
            GameInfo = gameInfo;
        }

        public string Name
        {
            get;
        } = "";

        public IPAddress Ip
        {
            get;
        }

        public ServerState State
        {
            get;
            private set;
        } = ServerState.Offline;

        public bool AcceptClients
        {
            get => acceptClients;
            set
            {
                if (acceptClients == value)
                    return;

                acceptClients = value;

                if (!acceptClients)
                    cancelTokenSource.Cancel();
            }
        }

        public bool GameDirty { get; set; } = false;

        public string Error
        {
            get;
            private set;
        } = "";

        public GameInfo GameInfo
        {
            get;
        }

        public INetworkDataReceiver NetworkDataReceiver { get; set; }

        public List<IRemoteClient> Clients => clients.Keys.ToList();

        public event ClientJoinedHandler ClientJoined;
        public event ClientLeftHandler ClientLeft;
        public event GameReadyHandler GameReady;

        public void Run(bool useServerValues, bool useSameValues, uint mapSize, string mapSeed,
            IEnumerable<PlayerInfo> players, CancellationToken cancellationToken)
        {
            Error = "";

            /*var addresses = Dns.GetHostAddresses(HostName);

            if (addresses.Length == 0)
            {
                Error = "Invalid hostname.";
                return;
            }

            listener = new TcpListener(addresses[0], Global.NetworkPort);*/
            listener = new TcpListener(Ip, Global.NetworkPort);

            listener.Start();

            cancellationToken.Register(listener.Stop);

            State = ServerState.Lobby;
            lobbyServerInfo = new LobbyServerInfo(useServerValues, useSameValues, mapSize, mapSeed);
            lobbyPlayerInfo.Clear();
            uint playerIndex = 0u;

            foreach (var player in players)
            {
                if (player != null)
                {
                    string identification = null;

                    if (player.Face >= PlayerFace.You) // human
                    {
                        if (playerIndex == 0u) // host
                            identification = Ip.ToString();
                        else
                            identification = playerClients[playerIndex].Ip.ToString();
                    }

                    lobbyPlayerInfo.Add(new LobbyPlayerInfo
                    (
                        identification,
                        playerIndex,
                        (int)player.Face,
                        (int)player.Supplies,
                        (int)player.Intelligence,
                        (int)player.Reproduction
                    ));
                }

                ++playerIndex;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    // TODO: password protected games

                    // if someone connects during game or outside lobby, we will ignore it and disconnect the client
                    if (State != ServerState.Lobby)
                    {
                        RejectClient(client);
                        AcceptClients = false; // this will also cancel the listener (no more clients are able to connect)
                        return;
                    }
                    // Note: We can not just stop the listener if there are 4 players in the lobby
                    // because players may disconnect or may be removed by the server. If the listener
                    // would be stopped no other client may connect then.
                    else if (GameInfo.MultiplayerPlayerCount == Game.MAX_PLAYER_COUNT) // TODO: spectators
                    {
                        RejectClient(client);
                        continue;
                    }

                    var clientPlayerIndex = GameInfo.FirstFreeMultiplayerPlayerIndex;
                    var remoteClient = new RemoteClient(clientPlayerIndex, this, client);

                    playerClients.Add(clientPlayerIndex, remoteClient);
                    clients.Add(remoteClient, client);
                    clientStatus.Add(remoteClient, MultiplayerStatus.Unknown);
                    lastClientHeartbeats.Add(remoteClient, DateTime.UtcNow);

                    HandleClient(remoteClient, client, cancellationToken).ContinueWith((task) => client.Close());
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.Interrupted)
                    {
                        return;
                    }
                    else
                    {
                        Error = "Error connecting client: " + ex.Message;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Error = "Error connecting client: " + ex.Message;
                }
            }
        }

        public void LoadGame()
        {
            State = ServerState.Loading;
        }

        public void EnterGame()
        {
            State = ServerState.Game;
        }

        public void ShowOutro()
        {
            State = ServerState.Outro;
        }

        public void UpdateNetworkEvents()
        {
            void handleReceivedData(IRemote source, INetworkData data, ResponseHandler responseHandler)
            {
                if (!(source is IRemoteClient client))
                {
                    Log.Error.Write(ErrorSystemType.Network, "Server received data from a non-client.");
                    responseHandler?.Invoke(ResponseType.BadDestination);
                    return;
                }

                ProcessData(client, data, responseHandler);
            }

            NetworkDataReceiver?.ProcessReceivedData(handleReceivedData);

            // Send an in-sync message if necessary or update the game state
            if (State == ServerState.Game)
            {
                var currentGame = GameManager.Instance.GetCurrentGame();

                if (currentGame != null)
                {
                    if (GameDirty)
                    {
                        BroadcastGameStateUpdate(currentGame, false);
                        lastUserActionOrInSyncGameTime = currentGame.GameTime;
                    }
                    else if (lastUserActionOrInSyncGameTime - currentGame.GameTime >= InSyncData.SyncDelay * Freeserf.Global.TICKS_PER_SEC &&
                        InSyncData.TimeToSync(currentGame))
                    {
                        lastUserActionOrInSyncGameTime = currentGame.GameTime;

                        var gameTime = currentGame.GameTime;
                        var hours = gameTime / 3600;
                        gameTime -= hours * 3600;
                        var minutes = gameTime / 60;
                        gameTime -= minutes * 60;
                        var seconds = gameTime;
                        Log.Verbose.Write(ErrorSystemType.Network, $"Sending in-sync message to all clients at game time: {hours}:{minutes}:{seconds}.");

                        BroadcastInSync(currentGame.GameTime);
                    }

                    GameDirty = false;
                    currentGame.ResetDirtyFlag(); // TODO: if we use the dirty flag for local saving as well, we should auto-save now so unsaved changes tracking will work                    
                }
            }
        }

        Task HandleClient(RemoteClient client, TcpClient tcpClient, CancellationToken cancellationToken)
        {
            if (State != ServerState.Lobby)
            {
                throw new ExceptionFreeserf("Expected server to be in lobby.");
            }

            var clientReceiveTask = Task.Run(() =>
            {
                var server = client.Server as LocalServer;
                byte[] buffer = new byte[1024];
                using var stream = tcpClient.GetStream();

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (!stream.DataAvailable)
                        {
                            Thread.Sleep(5);
                            continue;
                        }

                        var data = Host.ReadData(stream);

                        if (data.Length > 0)
                        {
                            server.HandleData(client, data);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        if (tcpClient.Connected)
                        {
                            // TODO: connected but disposed? should not happen.
                        }

                        return;
                    }
                }
            });

            // This should add the player to the game in lobby.
            // Moreover it should trigger a server update and
            // therefore broadcasts the lobby data to all clients
            // (including this one).
            ClientJoined?.Invoke(this, client);

            return clientReceiveTask;
        }

        void HandleData(RemoteClient client, byte[] data)
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Received {data.Length} byte(s) of data from client '{client.Ip} (player {client.PlayerIndex})'.");

            if (NetworkDataReceiver == null)
                throw new ExceptionFreeserf(ErrorSystemType.Application, "Network data receiver is not set up.");

            try
            {
                foreach (var networkData in NetworkDataParser.Parse(data))
                {
                    Log.Verbose.Write(ErrorSystemType.Network, $"Received {networkData.LogName} (message index {networkData.MessageIndex}).");

                    // Whenever we receive something from the client we update the last heartbeat time.
                    lastClientHeartbeats[client] = DateTime.UtcNow;

                    if (networkData.Type == NetworkDataType.Heartbeat)
                    {
                        // Heartbeats are possible in all states but
                        // we have set the last heartbeat time above already.
                        return;
                    }

                    switch (State)
                    {
                        case ServerState.Lobby:
                            {
                                // TODO allow user actions in lobby? can clients do something in lobby?

                                if (networkData.Type != NetworkDataType.Request)
                                {
                                    client.SendResponse(networkData.MessageIndex, ResponseType.BadState);
                                    throw new ExceptionFreeserf("Request expected.");
                                }

                                NetworkDataReceiver.Receive(client, networkData, (ResponseType responseType) => SendResponse(client, networkData.MessageIndex, responseType));
                                break;
                            }
                        case ServerState.Loading:
                            {
                                if (networkData.Type != NetworkDataType.Request)
                                {
                                    client.SendResponse(networkData.MessageIndex, ResponseType.BadState);
                                    throw new ExceptionFreeserf("Request expected.");
                                }

                                var request = networkData as RequestData;

                                // client sends the start game request when he is ready
                                if (request.Request == Request.StartGame ||
                                    request.Request == Request.Disconnect)
                                {
                                    NetworkDataReceiver.Receive(client, request, null);                                    
                                    break;
                                }
                                else
                                    throw new ExceptionFreeserf("Unexpected request during loading."); // TODO maybe just ignore?
                            }
                        case ServerState.Game:
                            {
                                if (networkData.Type == NetworkDataType.Request || networkData.Type == NetworkDataType.UserActionData)
                                {
                                    if (networkData.Type == NetworkDataType.UserActionData)
                                    {
                                        var currentGame = GameManager.Instance.GetCurrentGame();

                                        if (currentGame == null)
                                        {
                                            Log.Error.Write(ErrorSystemType.Network, "User action received after game was closed.");
                                            Close();
                                            return;
                                        }

                                        lastUserActionOrInSyncGameTime = currentGame.GameTime;
                                    }

                                    NetworkDataReceiver.Receive(client, networkData, (ResponseType responseType) => SendResponse(client, networkData.MessageIndex, responseType));
                                }
                                else
                                {
                                    client.SendResponse(networkData.MessageIndex, ResponseType.BadState);
                                    throw new ExceptionFreeserf("Request or user action expected.");
                                }

                                break;
                            }
                        case ServerState.Outro:
                            // TODO
                            break;
                        default:
                            client.SendResponse(networkData.MessageIndex, ResponseType.BadRequest);
                            throw new ExceptionFreeserf(ErrorSystemType.Network, "Invalid server state.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Network, $"Error in receiving data from client {client.Ip}: " + ex.Message);
            }
        }

        void ProcessData(IRemoteClient client, INetworkData networkData, ResponseHandler responseHandler)
        {
            switch (State)
            {
                case ServerState.Lobby:
                    {
                        // TODO: assert that it is a request (checked before in HandleData)
                        var request = networkData as RequestData;

                        HandleLobbyRequest(client, request.MessageIndex, request.Request, responseHandler);

                        break;
                    }
                case ServerState.Loading:
                    {
                        // TODO: assert that it is a startgame request (checked before in HandleData)
                        var request = networkData as RequestData;

                        if (request.Request == Request.StartGame)
                        {
                            // client sends this when he is ready
                            ClientReady(client);
                        }
                        else if (request.Request == Request.Disconnect)
                        {
                            ClientLeft?.Invoke(this, client);
                            responseHandler?.Invoke(ResponseType.Ok);
                            DisconnectClient(client, false);
                        }
                    
                        break;
                    }
                case ServerState.Game:
                    {
                        // TODO: assert that it is a request or user action (checked before in HandleData)
                        Game game = GameManager.Instance.GetCurrentGame();

                        if (networkData.Type == NetworkDataType.Request)
                        {
                            var request = networkData as RequestData;

                            HandleGameRequest(game, client, request.MessageIndex, request.Request, responseHandler);
                        }
                        else if (networkData.Type == NetworkDataType.UserActionData)
                        {
                            var userAction = networkData as UserActionData;
                            var response = userAction.ApplyToGame(game, client.PlayerIndex);

                            if (clients.Count > 1)
                                GameDirty = true;

                            responseHandler?.Invoke(response);
                        }

                        break;
                    }
                case ServerState.Outro:
                    // TODO
                    break;
                default:
                    // Was handled in HandleData already.
                    break;
            }
        }

        void SendResponse(IRemoteClient client, byte messageIndex, ResponseType responseType)
        {
            if (messageIndex != Global.SpontaneousMessage)
                client.SendResponse(messageIndex, responseType);
        }

        void ClientReady(IRemoteClient client)
        {
            clientStatus[client] = MultiplayerStatus.Ready;
            CheckGameReady();
        }

        void CheckGameReady()
        {
            lock (gameReadyLock)
            {
                // all clients ready or disconnected?
                bool gameReady = clientStatus.Count(c => c.Value == MultiplayerStatus.Ready) == playerClients.Count;

                if (GameReady?.Invoke(gameReady) == true)
                {
                    EnterGame();
                }
            }
        }

        void HandleLobbyRequest(IRemoteClient client, byte messageIndex, Request request, ResponseHandler responseHandler)
        {
            switch (request)
            {
                case Request.Disconnect:
                    ClientLeft?.Invoke(this, client);
                    responseHandler?.Invoke(ResponseType.Ok);
                    DisconnectClient(client, false);
                    break;
                case Request.StartGame:
                    // This is not allowed while in lobby.
                    responseHandler?.Invoke(ResponseType.BadState);
                    break;
                case Request.Heartbeat:
                    // TODO: check if the client just requested it (bruteforce attacks should be avoided)
                    client.SendHeartbeat(messageIndex);
                    break;
                case Request.LobbyData:
                    // TODO: check if the client just requested it (bruteforce attacks should be avoided)
                    lock (lobbyServerInfo)
                        lock (lobbyPlayerInfo)
                        {
                            client.SendLobbyDataUpdate(messageIndex, lobbyServerInfo, lobbyPlayerInfo);
                        }
                    break;
                default:
                    responseHandler?.Invoke(ResponseType.BadRequest);
                    break;
            }
        }

        void HandleGameRequest(Game game, IRemoteClient client, byte messageIndex, Request request, ResponseHandler responseHandler)
        {
            switch (request)
            {
                case Request.Disconnect:
                    ClientLeft?.Invoke(this, client);
                    responseHandler?.Invoke(ResponseType.Ok);
                    DisconnectClient(client, false);
                    break;
                case Request.GameData:
                    // TODO: check if the client just requested it (bruteforce attacks should be avoided)
                    client.SendGameStateUpdate(messageIndex, game, true);
                    break;
                case Request.Heartbeat:
                    // TODO: check if the client just requested it (bruteforce attacks should be avoided)
                    client.SendHeartbeat(messageIndex);
                    break;
                case Request.LobbyData:
                    throw new ExceptionFreeserf("Lobby data should not be requested during game."); // maybe for spectators?
                default:
                    responseHandler?.Invoke(ResponseType.BadRequest);
                    break;
            }
        }

        public void Close()
        {
            State = ServerState.Offline;

            if (listener != null)
            {
                listener.Stop();
            }

            cancelTokenSource.Cancel();

            if (listenerTask != null)
            {
                listenerTask.Wait();
                listenerTask = null;
            }
        }

        public void StartGame(Game game)
        {
            BroadcastStartGameRequest();
            BroadcastGameStateUpdate(game, true);
            LoadGame();
        }

        public void RejectClient(TcpClient client)
        {
            new RemoteClient(uint.MaxValue, this, client).SendDisconnect();
            client?.Close();
        }

        public void DisconnectClient(IRemoteClient client, bool sendNotificationToClient = true)
        {
            if (sendNotificationToClient)
                client.SendDisconnect();

            if (clients.ContainsKey(client))
            {
                clients[client]?.Close();
                clients.Remove(client);
                playerClients.Remove(client.PlayerIndex);
                clientStatus.Remove(client);

                if (State == ServerState.Loading)
                {
                    CheckGameReady();
                }
            }

            switch (State)
            {
                case ServerState.Lobby:
                    // Note: GameInitBox removes the player
                    break;
                case ServerState.Game:
                    // TODO: let AI continue the players settlement or keep it at this state?
                    // Maybe even allow a reconnect?
                    break;
                default:
                    break;
            }
        }

        public void Init(bool useServerValues, bool useSameValues, uint mapSize, string mapSeed, IEnumerable<PlayerInfo> players)
        {
            State = ServerState.Offline;
            listenerTask = Task.Run(() => Run(
                useServerValues, useSameValues, mapSize, mapSeed, players, cancelTokenSource.Token), cancelTokenSource.Token
            );
        }

        public void Update(bool useServerValues, bool useSameValues, uint mapSize, string mapSeed, IEnumerable<PlayerInfo> players)
        {
            lock (lobbyServerInfo)
            {
                lobbyServerInfo.UseServerValues = useServerValues;
                lobbyServerInfo.UseSameValues = useSameValues;
                lobbyServerInfo.MapSize = mapSize;
                lobbyServerInfo.MapSeed = mapSeed;
            }

            lock (lobbyPlayerInfo)
            {
                lobbyPlayerInfo.Clear();
                uint playerIndex = 0u;

                foreach (var player in players.ToList())
                {
                    if (player != null)
                    {
                        string identification = null;

                        if (player.Face >= PlayerFace.You) // human
                        {
                            if (playerIndex == 0u) // host
                                identification = Ip.ToString();
                            else
                                identification = playerClients[playerIndex].Ip.ToString();
                        }

                        lobbyPlayerInfo.Add(new LobbyPlayerInfo(
                            identification,
                            playerIndex,
                            (int)player.Face,
                            (int)player.Supplies,
                            (int)player.Intelligence,
                            (int)player.Reproduction
                        ));
                    }

                    ++playerIndex;
                }
            }

            BroadcastLobbyData();
        }

        public void AllowUserInput(bool allow)
        {
            // Note: This is only a notification. A client might easily change its code
            // to ignore it. So this can't be used in a secure way to disable user input.
            // It's more like "all your user input from now on is processed or discarded".
            // If the client proceeds with user input without allowance its progress will
            // be overriden by the server anyway.
            if (allow)
                BroadcastAllowUserInputRequest();
            else
                BroadcastDisallowUserInputRequest();
        }

        public void PauseGame()
        {
            // Note: This is only a notification. A client might easily change its code
            // to ignore it. So this can't be used in a secure way to disable client's
            // game progress or even settings the client's game speed.
            // It's more like "all your game progress from now on is discarded".
            // If the client proceeds with game without allowance its progress will
            // be overriden by the server anyway.
            BroadcastPauseRequest();
        }

        public void ResumeGame()
        {
            // Note: This is only a notification. A client might easily change its code
            // to ignore it. So this can't be used in a secure way to enable client's
            // game progress or even settings the client's game speed.
            // It's more like "all your game progress from now on is processed".
            BroadcastResumeRequest();
        }

        private delegate void BroadcastMethod(IRemoteClient client);

        private void Broadcast(BroadcastMethod method)
        {
            lastOwnHearbeat = DateTime.UtcNow;

            foreach (var client in clients.ToList())
            {
                if (client.Value.Connected)
                    method(client.Key);
            }
        }

        private void BroadcastStartGameRequest()
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Broadcast request 'Start game' to {clients.Count} clients.");

            Broadcast((client) => new RequestData(Global.SpontaneousMessage, Request.StartGame).Send(client));
        }

        private void BroadcastAllowUserInputRequest()
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Broadcast request 'Allow user input' to {clients.Count} clients.");

            Broadcast((client) => new RequestData(Global.SpontaneousMessage, Request.AllowUserInput).Send(client));
        }

        private void BroadcastDisallowUserInputRequest()
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Broadcast request 'Disallow user input' to {clients.Count} clients.");

            Broadcast((client) => new RequestData(Global.SpontaneousMessage, Request.DisallowUserInput).Send(client));
        }

        private void BroadcastPauseRequest()
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Broadcast request 'Pause game' to {clients.Count} clients.");

            Broadcast((client) => new RequestData(Global.SpontaneousMessage, Request.Pause).Send(client));
        }

        private void BroadcastResumeRequest()
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Broadcast request 'Resume game' to {clients.Count} clients.");

            Broadcast((client) => new RequestData(Global.SpontaneousMessage, Request.Resume).Send(client));
        }

        private void BroadcastLobbyData()
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Broadcast lobby data to {clients.Count} clients.");

            Broadcast((client) =>
            {
                lock (lobbyServerInfo)
                    lock (lobbyPlayerInfo)
                    {
                        client.SendLobbyDataUpdate(Global.SpontaneousMessage, lobbyServerInfo, lobbyPlayerInfo);
                    }
            });
        }

        private void BroadcastDisconnect()
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Broadcast disconnect to {clients.Count} clients.");

            Broadcast((client) => client.SendDisconnect());
        }

        private void BroadcastInSync(uint gameTime)
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Broadcast in-sync message to {clients.Count} clients.");

            Broadcast((client) => client.SendInSyncMessage(gameTime));
        }

        private void BroadcastGameStateUpdate(Game game, bool fullState)
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Broadcast game state update to {clients.Count} clients.");

            Broadcast((client) => client.SendGameStateUpdate(Global.SpontaneousMessage, game, fullState));
        }

        public void BroadcastHeartbeat()
        {
            // Only send heartbeats every second.
            if ((DateTime.UtcNow - lastOwnHearbeat).TotalSeconds < 1.0)
                return;

            Log.Verbose.Write(ErrorSystemType.Network, $"Broadcast heartbeat to {clients.Count} clients.");

            Broadcast((client) => client.SendHeartbeat(Global.SpontaneousMessage));
        }
    }

    public class RemoteServer : IRemoteServer
    {
        private TcpClient localClient = null;
        private Task receiveTask = null;
        private CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

        public RemoteServer(string name, IPAddress ip, TcpClient localClient)
        {
            Name = name;
            Ip = ip;
            State = ServerState.Lobby; // when joining we are in lobby
            this.localClient = localClient;

            receiveTask = Task.Run(() => Run(cancelTokenSource.Token), cancelTokenSource.Token);
        }

        public void Close()
        {
            localClient = null;
            State = ServerState.Offline;

            cancelTokenSource.Cancel();

            if (receiveTask != null)
            {
                if (receiveTask.Status == TaskStatus.Running)
                    receiveTask.Wait();
                receiveTask = null;
            }
        }

        Task Run(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                byte[] buffer = new byte[1024];

                using (var stream = localClient.GetStream())
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            if (!stream.DataAvailable)
                            {
                                Thread.Sleep(5);
                                continue;
                            }

                            var data = Host.ReadData(stream);

                            if (data.Length > 0)
                            {
                                DataReceived?.Invoke(this, data);
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            if (localClient.Connected)
                            {
                                // TODO: connected but disposed? should not happen.
                            }
                        }
                    }
                }
            });
        }

        public string Name
        {
            get;
        }

        public IPAddress Ip
        {
            get;
        }

        public ServerState State
        {
            get;
            private set;
        }

        public event ReceivedDataHandler DataReceived;

        public void Send(byte[] rawData)
        {
            if (localClient != null && localClient.Connected)
                localClient.GetStream().Write(rawData, 0, rawData.Length);
        }
    }

    public class ServerFactory : IServerFactory
    {
        public ILocalServer CreateLocal(string name, GameInfo gameInfo)
        {
            return new LocalServer(name, gameInfo);
        }
    }
}
