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

        public uint PlayerIndex
        {
            get;
        }

        public Game Game
        {
            get;
            set;
        }

        public IRemoteServer Server
        {
            get;
        }

        public bool JoinServer(string name, IPAddress ip)
        {
            try
            {
                client = new TcpClient();
                client.Connect(ip, Global.NetworkPort);

                server = new RemoteServer(name, ip, client);

                return true;
            }
            catch
            {
                return false;
            }
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

        public Game Game
        {
            get;
            set;
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
    }

    public class ClientFactory : IClientFactory
    {
        public ILocalClient CreateLocal()
        {
            return new LocalClient();
        }
    }
}
