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
        const byte ProtocolVersion = 0;

        internal static byte[] ReadData(NetworkStream stream, int length)
        {
            List<byte> largeBuffer = new List<byte>(1024);
            byte[] buffer = new byte[1024];
            int numRead;

            do
            {
                while (!stream.DataAvailable)
                    Thread.Sleep(5);

                numRead = stream.Read(buffer, 0, Math.Min(buffer.Length, length));
                largeBuffer.AddRange(buffer.Take(numRead));
                length -= numRead;
            } while (length != 0);

            return largeBuffer.ToArray();
        }

        internal static byte[] ReadData(NetworkStream stream)
        {
            // 'F' 'S' 'N' 0xVV 0xSSSSSSSS where the last 4 bytes give the length of the arriving data.
            // VV is the protocol version (0 for now).
            var header = ReadData(stream, 8);

            if (header[0] != 'F' || header[1] != 'S' || header[2] != 'N')
            {
                Log.Debug.Write(ErrorSystemType.Network, $"Package with unknown header arrived and was discarded.");
                return null; // Not for freeserf.net
            }

            if (ProtocolVersion < header[3])
            {
                Log.Debug.Write(ErrorSystemType.Network, $"Package with unsupported protocol version 0x{header[3]:x2} arrived and was discarded.");
                return null; // We do not support the given protocol version
            }

            uint length = BitConverter.ToUInt32(header, 4);

            if (length == 0)
            {
                Log.Debug.Write(ErrorSystemType.Network, $"Package with a size of 0 bytes arrived and was discarded.");
                return null;
            }

            if (length > int.MaxValue)
            {
                Log.Debug.Write(ErrorSystemType.Network, $"Package with an out-of-bounds size of {length} bytes arrived and was discarded.");
                return null;
            }

            return ReadData(stream, (int)(length & 0x7fffffff));
        }

        internal static void WriteData(NetworkStream stream, byte[] data)
        {
            byte[] header = new byte[]
            {
                (byte)'F', (byte)'S', (byte)'N', ProtocolVersion
            };
            stream.Write(header);
            stream.Write(BitConverter.GetBytes((uint)data.Length));
            stream.Write(data);
        }

        internal static IPAddress GetLocalIpAddress()
        {
            IPAddress mostSuitableIp = null;
            bool mostSuitableIpIsDnsEligible = false;

            try
            {
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

                        // Note: IsDnsEligible and PrefixOrigin are only supported
                        // on Windows. On other platforms (e.g. Android) accessing
                        // them throws PlatformNotSupportedException, so we guard
                        // them and fall back to a simple "first usable address".
                        bool isDnsEligible = false;
                        PrefixOrigin prefixOrigin = PrefixOrigin.Other;

                        try
                        {
                            isDnsEligible = address.IsDnsEligible;
                            prefixOrigin = address.PrefixOrigin;
                        }
                        catch (PlatformNotSupportedException)
                        {
                            // Not available on this platform.
                        }

                        if (!isDnsEligible)
                        {
                            if (mostSuitableIp == null)
                            {
                                mostSuitableIp = address.Address;
                                mostSuitableIpIsDnsEligible = false;
                            }
                            continue;
                        }

                        // The best IP is the IP got from DHCP server
                        if (prefixOrigin != PrefixOrigin.Dhcp)
                        {
                            if (mostSuitableIp == null || !mostSuitableIpIsDnsEligible)
                            {
                                mostSuitableIp = address.Address;
                                mostSuitableIpIsDnsEligible = true;
                            }
                            continue;
                        }

                        return address.Address;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Network, "Error retrieving local IP: " + ex.Message);
            }

            return mostSuitableIp;
        }
    }

    internal class LocalServer : ILocalServer
    {
        readonly object gameReadyLock = new object();
        bool acceptClients = true;
        readonly CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        Task listenerTask = null;
        readonly List<RemoteClient> lostClients = new List<RemoteClient>();
        DiscoveryResponder discoveryResponder = null;
        TcpListener listener = null;
        LobbyServerInfo lobbyServerInfo = null;
        readonly List<LobbyPlayerInfo> lobbyPlayerInfo = new List<LobbyPlayerInfo>();
        readonly Dictionary<IRemoteClient, TcpClient> clients = new Dictionary<IRemoteClient, TcpClient>();
        readonly Dictionary<uint, IRemoteClient> playerClients = new Dictionary<uint, IRemoteClient>();
        readonly Dictionary<IRemoteClient, MultiplayerStatus> clientStatus = new Dictionary<IRemoteClient, MultiplayerStatus>();
        readonly Dictionary<string, uint> disconnectedClients = new Dictionary<string, uint>();
        // Rejoining: each player of a client has a token. Clients which connect during the game
        // are pending until they sent their token (see JoinData).
        ulong sessionId = JoinData.CreateRandomId();
        readonly Dictionary<uint, ulong> playerTokens = new Dictionary<uint, ulong>();
        readonly HashSet<uint> humanPlayers = new HashSet<uint>();
        readonly Dictionary<RemoteClient, (TcpClient TcpClient, DateTime ConnectTime)> pendingClients = new();
        readonly List<(RemoteClient Client, JoinData Data)> pendingJoins = new List<(RemoteClient, JoinData)>();
        static readonly TimeSpan PendingClientTimeout = TimeSpan.FromSeconds(5);
        static readonly TimeSpan ActiveClientTime = TimeSpan.FromSeconds(2);
        readonly Dictionary<RemoteClient, Action> connectionLossHandlers = new Dictionary<RemoteClient, Action>();
        DateTime lastOwnHearbeat = DateTime.MinValue; // Every sent message counts as a heartbeat but only the broadcasted ones so no client is missed out.

        public LocalServer(string name, GameInfo gameInfo)
        {
            Name = name;
            Ip = Host.GetLocalIpAddress();

            if (Ip == null)
            {
                Log.Error.Write(ErrorSystemType.Network, "Unable to retrieve local IP.");
                Ip = IPAddress.Loopback; // Is this ok or should we throw here?
            }

            GameInfo = gameInfo;
        }

        public string Name
        {
            get;
            set;
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
        public event ClientChangedFaceHandler ClientChangedFace;
        public event ClientChangedValuesHandler ClientChangedValues;
        public event ClientVoteHandler ClientVote;

        void Run(CancellationToken cancellationToken)
        {
            Error = "";

            listener = new TcpListener(Ip, Global.NetworkPort);

            listener.Start();

            cancellationToken.Register(listener.Stop);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    var ip = ((IPEndPoint)client?.Client?.RemoteEndPoint)?.Address?.ToString();
                    // TODO: password protected games

                    // Clients which connect during the game may get their player back (rejoin).
                    // This is also true for the lobby of a loaded game.
                    // They are pending until they sent their token (see CompleteRejoin).
                    if (State == ServerState.Game || (State == ServerState.Lobby && LoadedGame))
                    {
                        Log.Verbose.Write(ErrorSystemType.Network, $"Client with IP '{ip}' connected during the game.");

                        var pendingClient = new RemoteClient(uint.MaxValue, this, client);

                        lock (pendingClients)
                            pendingClients.Add(pendingClient, (client, DateTime.UtcNow));

                        ReceiveData(pendingClient, client, cancellationToken).ContinueWith((task) => client.Close());
                        continue;
                    }
                    // Nobody can join while the game is loading or after it.
                    else if (State != ServerState.Lobby)
                    {
                        RejectClient(client);
                        continue;
                    }
                    // Note: We can not just stop the listener if there are 4 players in the lobby
                    // because players may disconnect or may be removed by the server. If the listener
                    // would be stopped no other client may connect then.
                    else if (GameInfo.MultiplayerPlayerCount == Game.MAX_PLAYER_COUNT) // TODO: spectators
                    {
                        Log.Verbose.Write(ErrorSystemType.Network, $"Rejected client '{ip ?? "no ip"}': Game is full.");
                        RejectClient(client);
                        continue;
                    }

                    Log.Verbose.Write(ErrorSystemType.Network, $"New client with IP '{ip}' connected.");

                    var clientPlayerIndex = GameInfo.FirstFreeMultiplayerPlayerIndex;
                    var remoteClient = new RemoteClient(clientPlayerIndex, this, client);

                    playerClients.Add(clientPlayerIndex, remoteClient);
                    clients.Add(remoteClient, client);
                    clientStatus.Add(remoteClient, MultiplayerStatus.Unknown);

                    SubscribeConnectionEvents(remoteClient);

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
                        Log.Verbose.Write(ErrorSystemType.Network, Error);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Error = "Error connecting client: " + ex.Message;
                    Log.Verbose.Write(ErrorSystemType.Network, Error);
                }
            }
        }

        // This is called by the connection observer thread. The clients
        // are disconnected later on the main thread (UpdateNetworkEvents).
        void ClientConnectionLost(RemoteClient client)
        {
            lock (lostClients)
            {
                if (!lostClients.Contains(client))
                    lostClients.Add(client);
            }
        }

        void SubscribeConnectionEvents(RemoteClient remoteClient)
        {
            void connectionLost()
            {
                ClientConnectionLost(remoteClient);
            }

            connectionLossHandlers.Add(remoteClient, connectionLost);
            remoteClient.ConnectionLost += connectionLossHandlers[remoteClient];
        }

        void UnsubscribeConnectionEvents(RemoteClient remoteClient)
        {
            remoteClient.CancelConnectionObserving();
            remoteClient.ConnectionLost -= connectionLossHandlers[remoteClient];
            connectionLossHandlers.Remove(remoteClient);
        }

        public void LoadGame()
        {
            State = ServerState.Loading;
        }

        public bool LoadedGame { get; private set; } = false;

        public MultiplayerSaveInfo GetSaveInfo()
        {
            var info = new MultiplayerSaveInfo
            {
                SessionId = sessionId,
                ServerName = Name
            };

            info.HumanPlayers.UnionWith(State == ServerState.Game || LoadedGame ? humanPlayers : playerClients.Keys);

            lock (playerTokens)
            {
                foreach (var playerToken in playerTokens)
                {
                    if (info.HumanPlayers.Contains(playerToken.Key))
                        info.PlayerTokens[playerToken.Key] = playerToken.Value;
                }
            }

            return info;
        }

        public void InitFromSave(MultiplayerSaveInfo saveInfo)
        {
            LoadedGame = true;
            sessionId = saveInfo.SessionId != 0 ? saveInfo.SessionId : sessionId;

            humanPlayers.Clear();
            humanPlayers.UnionWith(saveInfo.HumanPlayers.Where(player => player != 0)); // 0 is the host

            lock (playerTokens)
            {
                playerTokens.Clear();

                foreach (var playerToken in saveInfo.PlayerTokens)
                    playerTokens[playerToken.Key] = playerToken.Value;
            }
        }

        public void EnterGame()
        {
            State = ServerState.Game;

            // Only these players can be taken over by rejoining clients.
            // For loaded games these are all players of clients in the save game.
            if (!LoadedGame)
            {
                humanPlayers.Clear();
                humanPlayers.UnionWith(playerClients.Keys);
            }

            // The clients have created their games from the lobby data but some
            // parts (e.g. the random state) are not equal. So start with a full sync.
            GameDirty = true;

            Game.CheckpointReached -= Game_CheckpointReached;
            Game.CheckpointReached += Game_CheckpointReached;
        }

        void Game_CheckpointReached(Game game)
        {
            if (State != ServerState.Game || game != GameManager.Instance.GetCurrentGame())
                return;

            BroadcastInSync(game.ConstTick, GameStateSerializer.ComputeHash(game));
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

            List<RemoteClient> clientsToDisconnect;

            lock (lostClients)
            {
                clientsToDisconnect = lostClients.ToList();
                lostClients.Clear();
            }

            foreach (var lostClient in clientsToDisconnect)
            {
                if (clients.ContainsKey(lostClient))
                {
                    Log.Verbose.Write(ErrorSystemType.Network, $"Lost connection to client {lostClient.Ip} (player {lostClient.PlayerIndex}).");
                    DisconnectClient(lostClient, false);
                }
            }

            UpdatePendingClients();

            // Send the game state if something has changed by a user action.
            // Otherwise the clients compare their states with the state hash
            // which is sent at each checkpoint (see Game_CheckpointReached).
            if (State == ServerState.Game)
            {
                var currentGame = GameManager.Instance.GetCurrentGame();

                if (currentGame != null)
                {
                    if (GameDirty || currentGame.StateChangedByAI)
                        BroadcastGameStateUpdate(currentGame, true);

                    GameDirty = false;
                    currentGame.StateChangedByAI = false;
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

            var clientReceiveTask = ReceiveData(client, tcpClient, cancellationToken);

            // The heartbeat tells the client its player index. Clients must not
            // derive it from the lobby data as several clients may share an IP
            // (same machine or same router).
            client.SendHeartbeat(Global.SpontaneousMessage);

            // The token identifies the player when the client rejoins a running game.
            var token = JoinData.CreateRandomId();

            lock (playerTokens)
                playerTokens[client.PlayerIndex] = token;

            new JoinData(sessionId, token).Send(client);

            // This should add the player to the game in lobby.
            // Moreover it should trigger a server update and
            // therefore broadcasts the lobby data to all clients
            // (including this one).
            ClientJoined?.Invoke(this, client);

            return clientReceiveTask;
        }

        Task ReceiveData(RemoteClient client, TcpClient tcpClient, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
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

                        if (data != null && data.Length > 0)
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
        }

        // Main thread: completes the rejoins of the clients which sent their token
        // and disconnects pending clients which did not identify in time.
        void UpdatePendingClients()
        {
            List<(RemoteClient Client, JoinData Data)> joins;

            lock (pendingClients)
            {
                joins = pendingJoins.ToList();
                pendingJoins.Clear();
            }

            foreach (var join in joins)
                CompleteRejoin(join.Client, join.Data);

            lock (pendingClients)
            {
                foreach (var pendingClient in pendingClients.Where(c => DateTime.UtcNow - c.Value.ConnectTime > PendingClientTimeout).ToList())
                {
                    Log.Verbose.Write(ErrorSystemType.Network, $"Client with IP '{pendingClient.Key.Ip}' did not identify. Disconnecting it.");
                    pendingClients.Remove(pendingClient.Key);
                    RejectPendingClient(pendingClient.Key, pendingClient.Value.TcpClient);
                }
            }
        }

        static void RejectPendingClient(RemoteClient client, TcpClient tcpClient)
        {
            client.CancelConnectionObserving();

            try
            {
                client.SendDisconnect();
            }
            catch
            {
                // ignore
            }

            tcpClient?.Close();
        }

        // Returns the player which a rejoining client gets or null if there is none.
        // A player is free if no client uses it or if its client sent nothing for a while
        // (e.g. the app crashed and the lost connection was not detected yet).
        uint? FindRejoinPlayer(RemoteClient client, JoinData joinData)
        {
            var now = DateTime.UtcNow;

            bool IsFree(uint player) => humanPlayers.Contains(player) &&
                (!playerClients.TryGetValue(player, out var playerClient) ||
                 (playerClient is RemoteClient remoteClient && now - remoteClient.LastHeartbeat > ActiveClientTime));

            uint Take(uint player)
            {
                if (playerClients.TryGetValue(player, out var oldClient))
                    DisconnectClient(oldClient, false);

                return player;
            }

            // 1. The player of the token.
            if (joinData.SessionId == sessionId && joinData.Token != 0)
            {
                uint? tokenPlayer = null;

                lock (playerTokens)
                {
                    foreach (var playerToken in playerTokens)
                    {
                        if (playerToken.Value == joinData.Token)
                            tokenPlayer = playerToken.Key;
                    }
                }

                // If its client is still active, another client uses the same token
                // (e.g. two instances with the same config). Then another player is searched.
                if (tokenPlayer != null && IsFree(tokenPlayer.Value))
                    return Take(tokenPlayer.Value);
            }

            var freePlayers = humanPlayers.Where(IsFree).OrderBy(player => player).ToList();
            string ip = client.Ip.ToString();

            // 2. A free player which was used by the same IP before.
            foreach (var player in freePlayers)
            {
                bool sameIp = playerClients.TryGetValue(player, out var oldClient)
                    ? oldClient.Ip.ToString() == ip
                    : disconnectedClients.TryGetValue(ip, out uint ipPlayer) && ipPlayer == player;

                if (sameIp)
                    return Take(player);
            }

            // 3. Any free player of a client which has left the game.
            return freePlayers.Count == 0 ? null : Take(freePlayers[0]);
        }

        void CompleteRejoin(RemoteClient client, JoinData joinData)
        {
            TcpClient tcpClient;

            lock (pendingClients)
            {
                if (!pendingClients.TryGetValue(client, out var pendingClient))
                    return;

                pendingClients.Remove(client);
                tcpClient = pendingClient.TcpClient;
            }

            var game = GameManager.Instance.GetCurrentGame();
            bool lobby = State == ServerState.Lobby && LoadedGame;
            uint? player = (State == ServerState.Game && game != null) || lobby ? FindRejoinPlayer(client, joinData) : null;

            if (player == null)
            {
                Log.Info.Write(ErrorSystemType.Network, $"Client with IP '{client.Ip}' can't rejoin the game (no free player).");
                RejectPendingClient(client, tcpClient);
                return;
            }

            client.PlayerIndex = player.Value;
            Log.Info.Write(ErrorSystemType.Network, $"Client with IP '{client.Ip}' rejoined the {(lobby ? "lobby of the loaded game" : "game")} as player {player.Value}.");

            playerClients[player.Value] = client;
            clients[client] = tcpClient;
            clientStatus[client] = MultiplayerStatus.Unknown;
            SubscribeConnectionEvents(client);

            // A new token is used for the next rejoin.
            var token = JoinData.CreateRandomId();

            lock (playerTokens)
                playerTokens[player.Value] = token;

            client.SendHeartbeat(Global.SpontaneousMessage); // tells the player index
            new JoinData(sessionId, token).Send(client);

            if (lobby)
            {
                // The lobby adds the client to its player (and sends the lobby data).
                ClientJoined?.Invoke(this, client);
                return;
            }

            // The client creates the game from the lobby data. Then it sends the start game
            // request back and gets the full game state (see HandleGameRequest).
            lock (lobbyServerInfo)
            lock (lobbyPlayerInfo)
            {
                client.SendLobbyDataUpdate(Global.SpontaneousMessage, lobbyServerInfo, lobbyPlayerInfo);
            }
            new RequestData(Global.SpontaneousMessage, Request.StartGame).Send(client);

            game.PlayerRejoinedGame(player.Value);
            GameDirty = true; // tell the other clients
        }

        void HandleData(RemoteClient client, byte[] data)
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Received {data.Length} byte(s) of data from client '{client.Ip} (player {client.PlayerIndex})'.");

            if (NetworkDataReceiver == null)
            {
                // A missing receiver must never crash the app on a background
                // thread. Log it and drop the data instead.
                Log.Error.Write(ErrorSystemType.Network, "Network data receiver is not set up. Dropping received data.");
                return;
            }

            try
            {
                foreach (var networkData in NetworkDataParser.Parse(data))
                {
                    Log.Verbose.Write(ErrorSystemType.Network, $"Received {networkData.LogName} (message index {networkData.MessageIndex}).");

                    // Whenever we receive something from the client we update the last heartbeat time.
                    client.LastHeartbeat = DateTime.UtcNow;

                    if (networkData.Type == NetworkDataType.Heartbeat)
                    {
                        // Heartbeats are possible in all states but
                        // we have set the last heartbeat time above already.
                        continue;
                    }

                    if (networkData.Type == NetworkDataType.Response)
                    {
                        // Responses are possible in all states
                        // but we won't handle it here.
                        continue;
                    }

                    bool pending;

                    lock (pendingClients)
                        pending = pendingClients.ContainsKey(client);

                    if (networkData is JoinData joinData)
                    {
                        // Clients send their token after connecting. It is only needed
                        // by clients which rejoin a running game (handled on the main thread).
                        if (pending)
                        {
                            lock (pendingClients)
                                pendingJoins.Add((client, joinData));
                        }

                        continue;
                    }

                    if (pending)
                        continue; // nothing else is accepted before the client is identified

                    switch (State)
                    {
                        case ServerState.Lobby:
                            {
                                if (networkData.Type != NetworkDataType.Request && networkData.Type != NetworkDataType.UserActionData)
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
                                    request.Request == Request.Disconnect ||
                                    request.Request == Request.Heartbeat ||
                                    request.Request == Request.LobbyData)
                                {
                                    NetworkDataReceiver.Receive(client, request, null);
                                    break;
                                }
                                else
                                {
                                    client.SendResponse(networkData.MessageIndex, ResponseType.BadState);
                                    throw new ExceptionFreeserf("Unexpected request during loading.");
                                }
                            }
                        case ServerState.Game:
                            {
                                if (networkData.Type == NetworkDataType.Request || networkData.Type == NetworkDataType.UserActionData ||
                                    networkData.Type == NetworkDataType.Vote)
                                {
                                    if (networkData.Type == NetworkDataType.UserActionData &&
                                        GameManager.Instance.GetCurrentGame() == null)
                                    {
                                        Log.Error.Write(ErrorSystemType.Network, "User action received after game was closed.");
                                        Close();
                                        return;
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
                        if (networkData is RequestData request)
                            HandleLobbyRequest(client, request.MessageIndex, request.Request, responseHandler);
                        else if (networkData is UserActionData userAction)
                        {
                            if (userAction.UserAction == UserAction.ChangeFace && userAction.Parameters?.Length >= 1)
                            {
                                ClientChangedFace?.Invoke(this, client, (PlayerFace)userAction.Parameters[0]);
                            }
                            else if (userAction.UserAction == UserAction.ChangeValues && userAction.Parameters?.Length >= 2)
                            {
                                ClientChangedValues?.Invoke(this, client, userAction.Parameters[0], userAction.Parameters[1]);
                            }
                            else
                            {
                                Log.Error.Write(ErrorSystemType.Network, $"Received user action {userAction.UserAction} in lobby.");
                                responseHandler?.Invoke(ResponseType.BadState);
                                return;
                            }
                        }

                        break;
                    }
                case ServerState.Loading:
                    {
                        // Only these requests are passed in HandleData.
                        var request = networkData as RequestData;

                        switch (request.Request)
                        {
                            case Request.StartGame:
                                // client sends this when he is ready
                                ClientReady(client);
                                break;
                            case Request.Disconnect:
                                ClientLeft?.Invoke(this, client);
                                responseHandler?.Invoke(ResponseType.Ok);
                                DisconnectClient(client, false);
                                break;
                            case Request.Heartbeat:
                                client.SendHeartbeat(request.MessageIndex);
                                break;
                            case Request.LobbyData:
                                lock (lobbyServerInfo)
                                lock (lobbyPlayerInfo)
                                {
                                    client.SendLobbyDataUpdate(request.MessageIndex, lobbyServerInfo, lobbyPlayerInfo);
                                }
                                break;
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
                        else if (networkData is VoteData vote)
                        {
                            // The player index of the message is not trusted. The client's one is used.
                            ClientVote?.Invoke(this, client, vote);
                        }
                        else if (networkData.Type == NetworkDataType.UserActionData)
                        {
                            var userAction = networkData as UserActionData;

                            if (userAction.UserAction == UserAction.Surrender)
                            {
                                // TODO
                            }

                            var response = userAction.ApplyToGame(game, client.PlayerIndex);

                            Log.Verbose.Write(ErrorSystemType.Network, $"User action {userAction.UserAction} of player {client.PlayerIndex} applied with result {response}.");

                            // The client applied the action at another game time than the server.
                            // So even the client itself needs the resulting game state.
                            if (response == ResponseType.Ok)
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
                    // Reconnected clients may request it.
                    // TODO: check if the client just requested it (bruteforce attacks should be avoided)
                    lock (lobbyServerInfo)
                    lock (lobbyPlayerInfo)
                    {
                        client.SendLobbyDataUpdate(messageIndex, lobbyServerInfo, lobbyPlayerInfo);
                    }
                    break;
                case Request.StartGame:
                    // This may be send by reconnected clients.
                    new RequestData(Global.SpontaneousMessage, Request.Resume).Send(client);
                    new RequestData(Global.SpontaneousMessage, Request.AllowUserInput).Send(client);
                    client.SendGameStateUpdate(Global.SpontaneousMessage, GameManager.Instance.GetCurrentGame(), true);
                    break;
                default:
                    responseHandler?.Invoke(ResponseType.BadRequest);
                    break;
            }
        }

        public void Close()
        {
            State = ServerState.Offline;
            Game.CheckpointReached -= Game_CheckpointReached;
            discoveryResponder?.Dispose();
            discoveryResponder = null;

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
            // Clients create their game from the lobby data so they
            // must have the final state before the game is started.
            BroadcastLobbyData();
            BroadcastStartGameRequest();
            LoadGame();
        }

        public void RejectClient(TcpClient client)
        {
            new RemoteClient(uint.MaxValue, this, client).SendDisconnect();
            client?.Close();
        }

        public void DisconnectClient(IRemoteClient client, bool sendNotificationToClient = true)
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Client with IP '{client.Ip.ToString()}' disconnected.");

            disconnectedClients[client.Ip.ToString()] = client.PlayerIndex; // the IP may be used by several clients

            if (sendNotificationToClient)
                client.SendDisconnect();

            if (clients.ContainsKey(client))
            {
                clients[client]?.Close();
                clients.Remove(client);
                playerClients.Remove(client.PlayerIndex);
                clientStatus.Remove(client);
                UnsubscribeConnectionEvents(client as RemoteClient);

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
                    GameManager.Instance.GetCurrentGame()?.PlayerLeftGame(client.PlayerIndex);
                    GameDirty = true; // tell the other clients
                    break;
                default:
                    break;
            }
        }

        public void Init(bool useServerValues, bool useSameValues, uint mapSize, string mapSeed, IEnumerable<PlayerInfo> players)
        {
            // The lobby data must exist before the server is used (e.g. updated by the lobby)
            // so it is not created by the listener task.
            lobbyServerInfo = new LobbyServerInfo(useServerValues, useSameValues, mapSize, mapSeed);
            lobbyPlayerInfo.Clear();
            uint playerIndex = 0u;

            foreach (var player in players)
            {
                if (player != null)
                {
                    lobbyPlayerInfo.Add(new LobbyPlayerInfo
                    (
                        player.Face >= PlayerFace.You ? Ip.ToString() : null, // only the host can be human here
                        playerIndex,
                        (int)player.Face,
                        (int)player.Supplies,
                        (int)player.Intelligence,
                        (int)player.Reproduction
                    ));
                }

                ++playerIndex;
            }

            State = ServerState.Lobby;
            listenerTask = Task.Run(() => Run(cancelTokenSource.Token), cancelTokenSource.Token);

            // Answer server searches of other players.
            discoveryResponder = new DiscoveryResponder(() => Discovery.CreateAnswer(Name,
                State != ServerState.Lobby, (int)GameInfo.MultiplayerPlayerCount, Game.MAX_PLAYER_COUNT));
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
                            else if (playerClients.TryGetValue(playerIndex, out var playerClient))
                                identification = playerClient.Ip.ToString();
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

        public void BroadcastVote(VoteData vote)
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Broadcast {vote.LogName} to {clients.Count} clients.");

            Broadcast((client) => vote.Send(client));
        }

        public void SendVote(IRemoteClient client, VoteData vote)
        {
            if (clients.TryGetValue(client as RemoteClient, out var status) && status.Connected)
                vote.Send(client);
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

        public void BroadcastLobbyData()
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

        public void BroadcastDisconnect()
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Broadcast disconnect to {clients.Count} clients.");

            Broadcast((client) => client.SendDisconnect());
        }

        private void BroadcastInSync(uint tick, byte[] stateHash)
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Broadcast in-sync message to {clients.Count} clients for tick {tick}.");

            Broadcast((client) => client.SendInSyncMessage(tick, stateHash));
        }

        private void BroadcastGameStateUpdate(Game game, bool fullState)
        {
            Log.Verbose.Write(ErrorSystemType.Network, $"Broadcast game state update to {clients.Count} clients with game time {Misc.SecondsToTime(game.GameTime)}");

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

    internal class RemoteServer : IRemoteServer
    {
        private TcpClient localClient = null;
        private Task receiveTask = null;
        private readonly CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

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
                    while (!cancellationToken.IsCancellationRequested && localClient.Connected)
                    {
                        try
                        {
                            if (!stream.DataAvailable)
                            {
                                Thread.Sleep(5);
                                continue;
                            }

                            var data = Host.ReadData(stream);

                            if (data != null && data.Length > 0)
                            {
                                DataReceived?.Invoke(this, data);
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            if (localClient?.Connected == true)
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
            try
            {
                if (localClient != null && localClient.Connected)
                    Host.WriteData(localClient.GetStream(), rawData);
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is SocketException ||
                ex is ObjectDisposedException || ex is InvalidOperationException)
            {
                Log.Verbose.Write(ErrorSystemType.Network, "Unable to send data to the server: " + ex.Message);
                ConnectionLost?.Invoke();
            }
        }

        /// <summary>
        /// Raised when data could not be sent.
        /// </summary>
        public event Action ConnectionLost;
    }

    public class ServerFactory : IServerFactory
    {
        public ILocalServer CreateLocal(string name, GameInfo gameInfo)
        {
            return new LocalServer(name, gameInfo);
        }
    }
}
