using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Freeserf.Network
{
    public class LocalClient : ILocalClient
    {
        public LocalClient(uint playerIndex)
        {
            PlayerIndex = playerIndex;
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

        public void RequestGameStateUpdate()
        {
            throw new NotImplementedException();
        }

        public void RequestMapStateUpdate()
        {
            throw new NotImplementedException();
        }

        public void RequestPlayerStateUpdate(uint playerIndex)
        {
            throw new NotImplementedException();
        }

        public void SendDisconnect()
        {
            throw new NotImplementedException();
        }

        public void SendKeepAlive()
        {
            throw new NotImplementedException();
        }
    }

    public class RemoteClient : IRemoteClient
    {
        readonly TcpClient client = new TcpClient();

        public RemoteClient(uint playerIndex)
        {
            PlayerIndex = playerIndex;
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

        public event EventHandler RequestReceived;

        public void HandleRequest()
        {
            throw new NotImplementedException();
        }

        public void Respond()
        {
            throw new NotImplementedException();
        }

        public void SendDisconnect()
        {
            throw new NotImplementedException();
        }

        public void SendGameStateUpdate()
        {
            throw new NotImplementedException();
        }

        public void SendKeepAlive()
        {
            throw new NotImplementedException();
        }

        public void SendMapStateUpdate()
        {
            throw new NotImplementedException();
        }

        public void SendPlayerStateUpdate(uint playerIndex)
        {
            throw new NotImplementedException();
        }
    }

    public class ClientFactory : IClientFactory
    {
        public ILocalClient CreateLocal(uint playerIndex)
        {
            return new LocalClient(playerIndex);
        }

        public IRemoteClient CreateRemote(uint playerIndex)
        {
            return new RemoteClient(playerIndex);
        }
    }
}
