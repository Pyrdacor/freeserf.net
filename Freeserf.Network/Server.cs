using System;
using System.Collections.Generic;
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

        public LocalServer(string name, GameInfo gameInfo)
        {
            Name = name;
            HostName = GetLocalIpAddress();
            GameInfo = gameInfo;
        }

        public string Name
        {
            get;
        } = "";

        public string HostName
        {
            get;
        } = "";

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
        CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

        public static string GetLocalIpAddress()
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

                    return address.Address.ToString();
                }
            }

            return mostSuitableIp != null
                ? mostSuitableIp.Address.ToString()
                : "";
        }

        public void Run(CancellationToken cancellationToken)
        {
            Error = "";

            var addresses = Dns.GetHostAddresses(HostName);

            if (addresses.Length == 0)
            {
                Error = "Invalid hostname.";
                return;
            }

            TcpListener listener = new TcpListener(addresses[0], Global.NetworkPort);

            listener.Start();

            cancellationToken.Register(listener.Stop);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();

                    uint playerIndex = (GameInfo.PlayerCount == 4) ? 0 : GameInfo.PlayerCount;

                    Clients.Add(new RemoteClient(playerIndex));
                    /*var clientTask = protocol.HandleClient(client, cancellationToken)
                        .ContinueWith((antecedent) => client.Dispose())
                        .ContinueWith((antecedent) => logger.LogInformation("Client disposed."));*/
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Error = "Error connecting client: " + ex.Message;
                }
            }
        }

        public void Close()
        {
            cancelTokenSource.Cancel();
        }

        public void ConnectClient(IRemoteClient client)
        {
            throw new NotImplementedException();
        }

        public void DisconnectClient(IRemoteClient client)
        {
            throw new NotImplementedException();
        }

        public void Init()
        {
            Run(cancelTokenSource.Token);
        }
    }

    public class RemoteServer : IRemoteServer
    {
        public RemoteServer(string name, string hostName)
        {
            Name = name;
            HostName = HostName;
        }

        public string Name
        {
            get;
        }

        public string HostName
        {
            get;
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
    }

    public class ServerFactory : IServerFactory
    {
        public ILocalServer CreateLocal(string name, GameInfo gameInfo)
        {
            return new LocalServer(name, gameInfo);
        }
    }
}
