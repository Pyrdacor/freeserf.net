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
    class Server
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
    }

    public class LocalServer : ILocalServer
    {
        bool acceptClients = true;
        CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        Task listenerTask = null;
        TcpListener listener = null;
        LobbyServerInfo lobbyServerInfo = null;
        readonly List<LobbyPlayerInfo> lobbyPlayerInfo = new List<LobbyPlayerInfo>();
        readonly Dictionary<IRemoteClient, TcpClient> clients = new Dictionary<IRemoteClient, TcpClient>();

        public LocalServer(string name, GameInfo gameInfo)
        {
            Name = name;
            Ip = IPAddress.Loopback;
            // TODO switch to local ip
            // Ip = GetLocalIpAddress();
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

        public string Error
        {
            get;
            private set;
        } = "";

        public GameInfo GameInfo
        {
            get;
        }

        public List<IRemoteClient> Clients => clients.Keys.ToList();

        public static IPAddress GetLocalIpAddress()
        {
            UnicastIPAddressInformation mostSuitableIp = null;

            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var network in networkInterfaces)
            {
                if (network.OperationalStatus != OperationalStatus.Up)
                    continue;

                var properties = network.GetIPProperties();

                if (properties.GatewayAddresses.Count == 0)
                    continue;

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

            return mostSuitableIp != null
                ? mostSuitableIp.Address
                : null;
        }

        public void Run(bool useServerValues, bool useSameValues, string mapSeed,
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
            lobbyServerInfo = new LobbyServerInfo(useServerValues, useSameValues, mapSeed);
            lobbyPlayerInfo.Clear();
            bool isHost = true;
            foreach (var player in players)
            {
                lobbyPlayerInfo.Add(new LobbyPlayerInfo
                (
                    "127.0.0.1", // TODO: has to be a valid IP
                    isHost,
                    (int)player.Face,
                    (int)player.Supplies,
                    (int)player.Intelligence,
                    (int)player.Reproduction
                ));
                isHost = false;
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
                    else if (GameInfo.PlayerCount == 4) // TODO: spectators
                    {
                        RejectClient(client);
                        continue;
                    }

                    uint playerIndex = GameInfo.PlayerCount;
                    var remoteClient = new RemoteClient(playerIndex, this, client);

                    clients.Add(remoteClient, client);

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

        Task HandleClient(RemoteClient client, TcpClient tcpClient, CancellationToken cancellationToken)
        {
            if (State != ServerState.Lobby)
            {
                throw new ExceptionFreeserf("Expected server to be in lobby.");
            }

            // initially send the lobby data to the client
            lock (lobbyServerInfo)
            lock (lobbyPlayerInfo)
            {
                new LobbyData(Global.SpontaneousMessage, lobbyServerInfo, lobbyPlayerInfo).Send(client);
            }

            return Task.Run(() =>
            {
                byte[] buffer = new byte[1024];

                using (var stream = tcpClient.GetStream())
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (!stream.DataAvailable)
                        {
                            Thread.Sleep(5);
                            continue;
                        }

                        var data = Server.ReadData(stream);

                        if (data.Length > 0)
                        {
                            HandleData(client, data);
                        }
                    }
                }
            });
        }

        void HandleData(RemoteClient client, byte[] data)
        {
            var networkData = NetworkDataParser.Parse(data);

            if (networkData.Type == NetworkDataType.Heartbeat)
            {
                // TODO
                // Heartbeats are possible in all states
            }

            switch (State)
            {
                case ServerState.Lobby:
                    {
                        // TODO allow user actions in lobby? can clients do something in lobby?

                        if (networkData.Type != NetworkDataType.Request)
                            throw new ExceptionFreeserf("Request expected.");

                        var request = networkData as RequestData;

                        HandleLobbyRequest(client, request.Number, request.Request);

                        break;
                    }
                case ServerState.Loading:
                    throw new ExceptionFreeserf("No message expected during loading."); // TODO maybe just ignore?
                case ServerState.Game:
                    {
                        if (networkData.Type == NetworkDataType.Request)
                        {
                            var request = networkData as RequestData;

                            HandleGameRequest(client, request.Number, request.Request);
                        }
                        else if (networkData.Type == NetworkDataType.UserActionData)
                        {
                            /*var userAction = networkData as UserActionData;

                            HandleGameUserAction(client, userAction.Number, userAction...);*/
                        }
                        else
                        {
                            throw new ExceptionFreeserf("Request or user action expected.");
                        }

                        break;
                    }
                case ServerState.Outro:
                    // TODO
                    break;
                default:
                    break;
            }
        }

        void HandleLobbyRequest(RemoteClient client, byte messageIndex, Request request)
        {
            switch (request)
            {
                case Request.Disconnect:
                    // TODO
                    break;
                case Request.GameData:
                    // TODO
                    break;
                case Request.Heartbeat:
                    // TODO
                    break;
                case Request.LobbyData:
                    lock (lobbyServerInfo)
                    lock (lobbyPlayerInfo)
                    {
                        new LobbyData(messageIndex, lobbyServerInfo, lobbyPlayerInfo).Send(client);
                    }
                    break;
                case Request.MapData:
                    // TODO
                    break;
                case Request.PlayerData:
                    // TODO
                    break;
                default:
                    // TODO error?
                    break;
            }
        }

        void HandleGameRequest(RemoteClient client, byte messageIndex, Request request)
        {
            switch (request)
            {
                case Request.Disconnect:
                    // TODO
                    break;
                case Request.GameData:
                    // TODO
                    break;
                case Request.Heartbeat:
                    // TODO
                    break;
                case Request.LobbyData:
                    throw new ExceptionFreeserf("Lobby data should not be requested during game."); // maybe for spectators?
                case Request.MapData:
                    // TODO
                    break;
                case Request.PlayerData:
                    // TODO
                    break;
                default:
                    // TODO error?
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

        public void RejectClient(TcpClient client)
        {
            new RemoteClient(uint.MaxValue, this, client).SendDisconnect();
            client?.Close();
        }

        public void DisconnectClient(IRemoteClient client)
        {
            client.SendDisconnect();

            if (clients.ContainsKey(client))
            {
                clients[client]?.Close();
                clients.Remove(client);
            }

            switch (State)
            {
                case ServerState.Lobby:
                    GameInfo.RemovePlayer(client.PlayerIndex);
                    break;
                case ServerState.Game:
                    // TODO: let AI continue the players settlement or keep it at this state?
                    break;
                default:
                    break;
            }
        }

        public void Init(bool useServerValues, bool useSameValues, string mapSeed, IEnumerable<PlayerInfo> players)
        {
            State = ServerState.Offline;
            listenerTask = Task.Run(() => Run(useServerValues, useSameValues, mapSeed, players, cancelTokenSource.Token), cancelTokenSource.Token);
        }

        public void Update(bool useServerValues, bool useSameValues, string mapSeed, IEnumerable<PlayerInfo> players)
        {
            lock (lobbyServerInfo)
            {
                lobbyServerInfo.UseServerValues = useServerValues;
                lobbyServerInfo.UseSameValues = useSameValues;
                lobbyServerInfo.MapSeed = mapSeed;
            }

            lock (lobbyPlayerInfo)
            {
                lobbyPlayerInfo.Clear();
                bool isHost = true;

                foreach (var player in players.ToList())
                {
                    lobbyPlayerInfo.Add(new LobbyPlayerInfo(
                        "127.0.0.1", // TODO: has to be a valid IP
                        isHost,
                        (int)player.Face,
                        (int)player.Supplies,
                        (int)player.Intelligence,
                        (int)player.Reproduction
                    ));

                    isHost = false;
                }
            }
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
                        if (!stream.DataAvailable)
                        {
                            Thread.Sleep(5);
                            continue;
                        }

                        var data = Server.ReadData(stream);

                        if (data.Length > 0)
                        {
                            RequestReceived?.Invoke(this, data);
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

        public IPAddress GetIP()
        {
            return Ip;
        }

        public event ReceivedDataHandler RequestReceived;

        public void Send(byte[] rawData)
        {
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
