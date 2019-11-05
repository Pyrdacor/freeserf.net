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
    public class LocalServer : ILocalServer
    {
        bool acceptClients = true;
        CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        Task listenerTask = null;
        TcpListener listener = null;
        LobbyServerInfo lobbyServerInfo = null;
        readonly List<LobbyPlayerInfo> lobbyPlayerInfo = new List<LobbyPlayerInfo>();

        public LocalServer(string name, GameInfo gameInfo)
        {
            Name = name;
            Ip = GetLocalIpAddress();
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

        public List<IRemoteClient> Clients { get; } = new List<IRemoteClient>();        

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

        public void Run(bool useServerValues, bool useSameValues, string mapSeed, CancellationToken cancellationToken)
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

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    uint playerIndex = (GameInfo.PlayerCount == 4) ? 0 : GameInfo.PlayerCount;
                    var remoteClient = new RemoteClient(playerIndex, this, client);

                    Clients.Add(remoteClient);
                    
                    HandleClient(remoteClient, client, cancellationToken).ContinueWith((task) => client.Dispose());
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

        byte[] ReadData(NetworkStream stream)
        {
            List<byte> largeBuffer = new List<byte>(4);
            byte[] buffer = new byte[1024];

            int numRead = 0;

            do
            {
                numRead = stream.Read(buffer, 0, buffer.Length);
                largeBuffer.AddRange(buffer.Take(numRead));
            } while (numRead == buffer.Length);

            return largeBuffer.ToArray();
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
                    break;
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

        Task HandleClient(RemoteClient client, TcpClient tcpClient, CancellationToken cancellationToken)
        {
            if (State != ServerState.Lobby)
            {
                throw new ExceptionFreeserf("Expected server to be in lobby.");
            }

            // initially send the lobby data to the client
            new LobbyData(Global.SpontaneousMessage, lobbyServerInfo, lobbyPlayerInfo).Send(client);

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

                        var data = ReadData(stream);                        

                        if (data.Length > 0)
                        {
                            HandleData(client, data);
                        }
                    }
                }
            });
        }

        public void Close()
        {
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

        public void ConnectClient(IRemoteClient client)
        {
            throw new NotImplementedException();
        }

        public void DisconnectClient(IRemoteClient client)
        {
            throw new NotImplementedException();
        }

        public void Init(bool useServerValues, bool useSameValues, string mapSeed)
        {
            listenerTask = Task.Run(() => Run(useServerValues, useSameValues, mapSeed, cancelTokenSource.Token), cancelTokenSource.Token);
        }

        public void Update(bool useServerValues, bool useSameValues, string mapSeed)
        {
            lock (lobbyServerInfo)
            {
                lobbyServerInfo.UseServerValues = useServerValues;
                lobbyServerInfo.UseSameValues = useSameValues;
                lobbyServerInfo.MapSeed = mapSeed;
            }
        }
    }

    public class RemoteServer : IRemoteServer
    {
        private TcpClient localClient = null;

        public RemoteServer(string name, IPAddress ip, TcpClient localClient)
        {
            Name = name;
            Ip = ip;
            State = ServerState.Lobby; // when joining we are in lobby
            this.localClient = localClient;
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
        }

        public IPAddress GetIP()
        {
            return Ip;
        }

        public event EventHandler RequestReceived;

        public void HandleRequest()
        {
            throw new NotImplementedException();
        }

        public void Respond()
        {
            throw new NotImplementedException();
        }

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
