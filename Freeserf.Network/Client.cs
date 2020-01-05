using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Freeserf.Network
{
    public class LocalClient : ILocalClient
    {
        TcpClient client = null;
        RemoteServer server = null;

        public LocalClient()
        {

        }

        // TODO: get from server and set it here
        public uint PlayerIndex
        {
            get;
        }

        // TODO: needed
        public Game Game
        {
            get;
            set;
        }

        public IRemoteServer Server => server;

        public LobbyData LobbyData
        {
            get;
            private set;
        } = null;

        public event EventHandler LobbyDataUpdated;

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

            client?.Close();
            server?.Close();
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
                client = new TcpClient();
                client.Connect(ip, Global.NetworkPort);

                server = new RemoteServer(name, ip, client);
                server.RequestReceived += Server_RequestReceived;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Server_RequestReceived(IRemoteServer server, byte[] data)
        {
            var request = NetworkDataParser.Parse(data);
            
            switch (request.Type)
            {
                case NetworkDataType.Heartbeat:
                    break;
                case NetworkDataType.LobbyData:
                    UpdateLobbyData(request as LobbyData);
                    break;
                // TODO: other data
                default:
                    // TODO: ignore? track safely?
                    throw new ExceptionFreeserf("Unknown server data");
            }
        }

        private void UpdateLobbyData(LobbyData data)
        {
            LobbyData = data;
            LobbyDataUpdated?.Invoke(this, EventArgs.Empty);
        }

        public byte RequestLobbyStateUpdate()
        {
            byte messageIndex = Global.GetNextMessageIndex();

            new RequestData(messageIndex, Request.LobbyData).Send(server);

            return messageIndex;
        }

        public byte RequestGameStateUpdate()
        {
            byte messageIndex = Global.GetNextMessageIndex();

            throw new NotImplementedException();

            return messageIndex;
        }

        public byte RequestMapStateUpdate()
        {
            byte messageIndex = Global.GetNextMessageIndex();

            throw new NotImplementedException();

            return messageIndex;
        }

        public byte RequestPlayerStateUpdate(uint playerIndex)
        {
            byte messageIndex = Global.GetNextMessageIndex();

            throw new NotImplementedException();

            return messageIndex;
        }

        public void SendHeartbeat()
        {
            new Heartbeat(Global.GetNextMessageIndex(), (byte)PlayerIndex).Send(server);
        }

        public void SendDisconnect()
        {
            new RequestData(Global.GetNextMessageIndex(), Request.Disconnect).Send(server);
        }
    }

    public class RemoteClient : IRemoteClient
    {
        readonly TcpClient client = new TcpClient();

        public RemoteClient(uint playerIndex, ILocalServer server, TcpClient client)
        {
            PlayerIndex = playerIndex;
            Server = server;
            this.client = client;
        }

        public uint PlayerIndex
        {
            get;
        }

        public ILocalServer Server
        {
            get;
        }

        public IPAddress GetIP()
        {
            throw new NotImplementedException();
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
            client.GetStream().Write(rawData, 0, rawData.Length);
        }

        public void SendGameStateUpdate(Game game)
        {
            throw new NotImplementedException();
        }

        public void SendPlayerStateUpdate(Player player)
        {
            throw new NotImplementedException();
        }

        public void SendMapStateUpdate(Map map)
        {
            throw new NotImplementedException();
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
