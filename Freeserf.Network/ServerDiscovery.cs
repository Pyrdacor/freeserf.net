/*
 * ServerDiscovery.cs - Search for multiplayer servers via UDP
 *
 * Copyright (C) 2026  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Freeserf.Network
{
    /// <summary>
    /// UDP messages to find servers. They use the same port number as the game (TCP).
    ///
    /// Query:  'F' 'S' 'N' 'Q' version
    /// Answer: 'F' 'S' 'N' 'A' version inGame currentPlayers maxPlayers nameLength name (ASCII)
    /// </summary>
    internal static class Discovery
    {
        const byte ProtocolVersion = 0;
        static readonly byte[] QueryHeader = [(byte)'F', (byte)'S', (byte)'N', (byte)'Q'];
        static readonly byte[] AnswerHeader = [(byte)'F', (byte)'S', (byte)'N', (byte)'A'];

        public static byte[] CreateQuery()
        {
            return [.. QueryHeader, ProtocolVersion];
        }

        public static bool IsQuery(byte[] data)
        {
            return data.Length >= 5 && data.Take(4).SequenceEqual(QueryHeader) && data[4] == ProtocolVersion;
        }

        public static byte[] CreateAnswer(string name, bool inGame, int currentPlayers, int maxPlayers)
        {
            var nameBytes = Encoding.ASCII.GetBytes(name ?? "");

            if (nameBytes.Length > 255)
                nameBytes = nameBytes.Take(255).ToArray();

            return [.. AnswerHeader, ProtocolVersion, (byte)(inGame ? 1 : 0), (byte)currentPlayers, (byte)maxPlayers, (byte)nameBytes.Length, .. nameBytes];
        }

        public static FoundServer ParseAnswer(byte[] data, IPAddress ip, string searchedAddress)
        {
            if (data.Length < 9 || !data.Take(4).SequenceEqual(AnswerHeader) || data[4] != ProtocolVersion)
                return null;

            int nameLength = data[8];

            if (data.Length < 9 + nameLength)
                return null;

            return new FoundServer
            {
                Ip = ip,
                SearchedAddress = searchedAddress,
                InGame = data[5] != 0,
                CurrentPlayers = data[6],
                MaxPlayers = data[7],
                Name = Encoding.ASCII.GetString(data, 9, nameLength)
            };
        }

        // On Windows a UDP socket gets a connection reset error when a query was sent
        // to an address without listener. This disables it as it is no real error here.
        public static void IgnoreConnectionResets(UdpClient udpClient)
        {
            try
            {
                const int SIO_UDP_CONNRESET = -1744830452;
                udpClient.Client.IOControl(SIO_UDP_CONNRESET, [0], null);
            }
            catch
            {
                // not supported on this platform
            }
        }
    }

    /// <summary>
    /// Answers server queries. Used by the local server.
    /// </summary>
    internal class DiscoveryResponder : IDisposable
    {
        readonly UdpClient udpClient = null;
        readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        bool disposed = false;

        public DiscoveryResponder(Func<byte[]> answerProvider)
        {
            try
            {
                udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, Global.NetworkPort));
                Discovery.IgnoreConnectionResets(udpClient);
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Network, "Unable to answer server queries: " + ex.Message);
                return;
            }

            var cancellationToken = cancellationTokenSource.Token;

            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = await udpClient.ReceiveAsync(cancellationToken);

                        if (Discovery.IsQuery(result.Buffer))
                        {
                            var answer = answerProvider();
                            await udpClient.SendAsync(answer, answer.Length, result.RemoteEndPoint);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    catch (SocketException ex)
                    {
                        Log.Verbose.Write(ErrorSystemType.Network, "Error while answering server queries: " + ex.Message);
                    }
                }
            });
        }

        public void Dispose()
        {
            if (!disposed)
            {
                cancellationTokenSource.Cancel();
                udpClient?.Close();
                disposed = true;
            }
        }
    }

    /// <summary>
    /// Searches for servers in the local network (broadcast) and at given addresses.
    /// </summary>
    internal class ServerFinder : IServerFinder
    {
        const int SearchIntervalMilliseconds = 2000;
        const double ServerTimeoutSeconds = 7.0;

        readonly object serversLock = new object();
        readonly Dictionary<string, (FoundServer Server, DateTime LastSeen)> servers = new Dictionary<string, (FoundServer, DateTime)>();
        readonly Dictionary<string, string> searchedAddresses = new Dictionary<string, string>(); // key: IP, value: entered address
        List<string> addresses = new List<string>();
        UdpClient udpClient = null;
        CancellationTokenSource cancellationTokenSource = null;

        public IReadOnlyList<FoundServer> Servers
        {
            get
            {
                lock (serversLock)
                {
                    var now = DateTime.UtcNow;

                    return servers.Values
                        .Where(entry => (now - entry.LastSeen).TotalSeconds < ServerTimeoutSeconds)
                        .Select(entry => entry.Server)
                        .OrderBy(server => server.Name)
                        .ToList();
                }
            }
        }

        public void SetAddresses(IEnumerable<string> addresses)
        {
            lock (serversLock)
            {
                this.addresses = addresses.ToList();
            }
        }

        public void Start()
        {
            if (cancellationTokenSource != null)
                return;

            try
            {
                udpClient = new UdpClient(0) { EnableBroadcast = true };
                Discovery.IgnoreConnectionResets(udpClient);
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Network, "Unable to search for servers: " + ex.Message);
                udpClient = null;
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var client = udpClient;

            Task.Run(() => ReceiveAsync(client, cancellationToken));
            Task.Run(() => SearchAsync(client, cancellationToken));
        }

        public void Stop()
        {
            cancellationTokenSource?.Cancel();
            udpClient?.Close();
            cancellationTokenSource = null;
            udpClient = null;

            lock (serversLock)
            {
                servers.Clear();
            }
        }

        async Task SearchAsync(UdpClient client, CancellationToken cancellationToken)
        {
            var query = Discovery.CreateQuery();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var broadcastAddress in GetBroadcastAddresses())
                        await SendQueryAsync(client, query, broadcastAddress);

                    // The server of this machine (also if there is no network).
                    await SendQueryAsync(client, query, IPAddress.Loopback);

                    List<string> addressesToSearch;

                    lock (serversLock)
                    {
                        addressesToSearch = addresses.ToList();
                    }

                    foreach (var address in addressesToSearch)
                    {
                        var ip = await ResolveAsync(address, cancellationToken);

                        if (ip == null)
                            continue;

                        lock (serversLock)
                        {
                            searchedAddresses[ip.ToString()] = address;
                        }

                        await SendQueryAsync(client, query, ip);
                    }

                    await Task.Delay(SearchIntervalMilliseconds, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Log.Verbose.Write(ErrorSystemType.Network, "Error while searching for servers: " + ex.Message);
                }
            }
        }

        static async Task SendQueryAsync(UdpClient client, byte[] query, IPAddress ip)
        {
            try
            {
                await client.SendAsync(query, query.Length, new IPEndPoint(ip, Global.NetworkPort));
            }
            catch (SocketException)
            {
                // e.g. network unreachable
            }
        }

        static async Task<IPAddress> ResolveAsync(string address, CancellationToken cancellationToken)
        {
            if (IPAddress.TryParse(address, out var ip))
                return ip;

            try
            {
                var addresses = await Dns.GetHostAddressesAsync(address, cancellationToken);
                return addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            }
            catch (SocketException)
            {
                return null;
            }
        }

        static IEnumerable<IPAddress> GetBroadcastAddresses()
        {
            var broadcastAddresses = new HashSet<IPAddress> { IPAddress.Broadcast };

            try
            {
                foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (network.OperationalStatus != OperationalStatus.Up)
                        continue;

                    foreach (var address in network.GetIPProperties().UnicastAddresses)
                    {
                        if (address.Address.AddressFamily != AddressFamily.InterNetwork ||
                            IPAddress.IsLoopback(address.Address) || address.IPv4Mask == null)
                            continue;

                        // The broadcast address of the subnet: all host bits are set.
                        var ipBytes = address.Address.GetAddressBytes();
                        var maskBytes = address.IPv4Mask.GetAddressBytes();

                        for (int i = 0; i < ipBytes.Length; ++i)
                            ipBytes[i] |= (byte)~maskBytes[i];

                        broadcastAddresses.Add(new IPAddress(ipBytes));
                    }
                }
            }
            catch
            {
                // the general broadcast address is still used
            }

            return broadcastAddresses;
        }

        async Task ReceiveAsync(UdpClient client, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await client.ReceiveAsync(cancellationToken);
                    var ip = result.RemoteEndPoint.Address;
                    string searchedAddress;

                    lock (serversLock)
                    {
                        searchedAddresses.TryGetValue(ip.ToString(), out searchedAddress);
                    }

                    // The server of this machine listens on the local network IP. Use it
                    // so that the server is only listed once (it answers to the broadcast too).
                    if (IPAddress.IsLoopback(ip))
                        ip = Host.GetLocalIpAddress() ?? ip;

                    var server = Discovery.ParseAnswer(result.Buffer, ip, searchedAddress);

                    if (server == null)
                        continue;

                    lock (serversLock)
                    {
                        string key = ip.ToString();

                        // Keep the entered address if the server was found by the broadcast as well.
                        if (server.SearchedAddress == null && servers.TryGetValue(key, out var known) && known.Server.SearchedAddress != null)
                        {
                            server = new FoundServer
                            {
                                Ip = server.Ip,
                                SearchedAddress = known.Server.SearchedAddress,
                                Name = server.Name,
                                InGame = server.InGame,
                                CurrentPlayers = server.CurrentPlayers,
                                MaxPlayers = server.MaxPlayers
                            };
                        }

                        servers[key] = (server, DateTime.UtcNow);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    Log.Verbose.Write(ErrorSystemType.Network, "Error while receiving server answers: " + ex.Message);
                }
            }
        }
    }
}
