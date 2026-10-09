/*
 * DemuxClient.cs - TLS socket client for the Ubisoft Connect demux protocol
 *
 * Part of the Ubisoft Connect SPAE.PA download feature for freeserf.net.
 * Implements the protobuf-based demux protocol used by the Ubisoft Connect
 * client (see UplayDB/UplayKit for the reference implementation).
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */

using Google.Protobuf;
using Mg.Protocol.Demux;
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Freeserf.Android.Ubisoft
{
    public class DemuxClient : IDisposable
    {
        public const string Host = "dmx.upc.ubisoft.com";
        public const int Port = 443;
        public const uint ClientVersion = 12922;

        TcpClient tcpClient;
        SslStream sslStream;
        uint requestId = 1;

        public bool IsConnected
        {
            get => tcpClient != null && tcpClient.Connected;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(Host, Port).ConfigureAwait(false);

            sslStream = new SslStream(tcpClient.GetStream(), false, (sender, certificate, chain, errors) => true);
            await sslStream.AuthenticateAsClientAsync(Host).ConfigureAwait(false);

            // Handshake: check the patch version and announce the client
            // version (mirrors the Ubisoft Connect client).
            await VersionCheckAsync(cancellationToken).ConfigureAwait(false);
            await PushVersionAsync(cancellationToken).ConfigureAwait(false);
        }

        // Authenticates the connection with the login ticket.
        public async Task<bool> AuthenticateAsync(string ticket, CancellationToken cancellationToken = default)
        {
            var req = new Req
            {
                RequestId = requestId++,
                AuthenticateReq = new AuthenticateReq
                {
                    ClientId = "uplay_pc",
                    SendKeepAlive = false,
                    Token = new Token { UbiTicket = ticket }
                }
            };

            var rsp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
            return rsp != null && rsp.AuthenticateRsp != null && rsp.AuthenticateRsp.Success;
        }

        // Checks the current patch version (result is not enforced, matching
        // the reference client).
        async Task VersionCheckAsync(CancellationToken cancellationToken)
        {
            var req = new Req
            {
                RequestId = requestId++,
                GetPatchInfoReq = new GetPatchInfoReq
                {
                    PatchTrackId = "DEFAULT",
                    TestConfig = false,
                    TrackType = 0
                }
            };

            await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
        }

        // Announces the client version to the server.
        async Task PushVersionAsync(CancellationToken cancellationToken)
        {
            var upstream = new Upstream
            {
                Push = new Push
                {
                    ClientVersion = new ClientVersionPush { Version = ClientVersion }
                }
            };

            byte[] framed = FormatLengthPrefixed(upstream.ToByteArray());
            await sslStream.WriteAsync(framed, 0, framed.Length, cancellationToken).ConfigureAwait(false);
            await sslStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        // Opens a service connection (e.g. "ownership_service") and returns
        // the connection id used for service requests.
        public async Task<uint> OpenConnectionAsync(string serviceName, CancellationToken cancellationToken = default)
        {
            var req = new Req
            {
                RequestId = requestId++,
                OpenConnectionReq = new OpenConnectionReq { ServiceName = serviceName }
            };

            var rsp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
            if (rsp == null || rsp.OpenConnectionRsp == null || !rsp.OpenConnectionRsp.Success)
                throw new UbisoftException($"Konnte Verbindung zu '{serviceName}' nicht öffnen.");

            return rsp.OpenConnectionRsp.ConnectionId;
        }

        // Sends a service-specific request on an open connection and returns
        // the parsed response message.
        public async Task<TResponse> SendServiceRequestAsync<TRequest, TResponse>(uint connectionId,
            TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IMessage<TRequest>, new()
            where TResponse : IMessage<TResponse>, new()
        {
            var upstream = new Upstream
            {
                Push = new Push
                {
                    Data = new DataMessage
                    {
                        ConnectionId = connectionId,
                        Data = ByteString.CopyFrom(FormatLengthPrefixed(request.ToByteArray()))
                    }
                }
            };

            var downstream = await SendUpstreamAsync(upstream, cancellationToken).ConfigureAwait(false);
            if (downstream == null || downstream.Push == null || downstream.Push.Data == null || !downstream.Push.Data.HasData)
                throw new UbisoftException("Keine Antwort vom Ubisoft-Dienst erhalten.");

            byte[] data = downstream.Push.Data.Data.ToByteArray();
            if (data.Length < 4)
                throw new UbisoftException("Ungültige Antwort vom Ubisoft-Dienst.");

            int length = ReadBigEndianUInt32(data, 0);
            if (length <= 0 || 4 + length > data.Length)
                throw new UbisoftException("Ungültige Antwortlänge vom Ubisoft-Dienst.");

            var parser = new MessageParser<TResponse>(() => new TResponse());
            return parser.ParseFrom(data, 4, length);
        }

        // Sends a demux request and returns the matching response.
        async Task<Rsp> SendRequestAsync(Req req, CancellationToken cancellationToken)
        {
            var upstream = new Upstream { Request = req };
            var downstream = await SendUpstreamAsync(upstream, cancellationToken).ConfigureAwait(false);
            if (downstream == null || downstream.Response == null)
                return null;
            return downstream.Response;
        }

        // Sends an upstream message (4-byte big-endian length prefix) and
        // reads the downstream response, skipping/handling any pushes that
        // arrive in between.
        async Task<Downstream> SendUpstreamAsync(Upstream upstream, CancellationToken cancellationToken)
        {
            byte[] payload = upstream.ToByteArray();
            byte[] framed = FormatLengthPrefixed(payload);
            await sslStream.WriteAsync(framed, 0, framed.Length, cancellationToken).ConfigureAwait(false);
            await sslStream.FlushAsync(cancellationToken).ConfigureAwait(false);

            while (true)
            {
                Downstream downstream = await ReadMessageAsync(cancellationToken).ConfigureAwait(false);
                if (downstream == null)
                    continue;

                // Demux-level responses (Authenticate, OpenConnection) arrive
                // in Response; service responses arrive in Push.Data.
                if (downstream.Response != null || (downstream.Push != null && downstream.Push.Data != null))
                    return downstream;

                // Other pushes (e.g. keep-alive): answer keep-alive, ignore
                // the rest.
                if (downstream.Push != null && downstream.Push.KeepAlive != null)
                {
                    await SendKeepAliveAsync(cancellationToken).ConfigureAwait(false);
                }
                else if (downstream.Push != null && downstream.Push.ConnectionClosed != null)
                {
                    throw new UbisoftException("Die Ubisoft-Verbindung wurde geschlossen.");
                }
            }
        }

        async Task SendKeepAliveAsync(CancellationToken cancellationToken)
        {
            var upstream = new Upstream { Push = new Push { KeepAlive = new KeepAlivePush() } };
            byte[] framed = FormatLengthPrefixed(upstream.ToByteArray());
            await sslStream.WriteAsync(framed, 0, framed.Length, cancellationToken).ConfigureAwait(false);
            await sslStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        // Reads one demux message. Responses are length-prefixed; some pushes
        // are sent as raw protobuf (first byte 0x12 = Push field tag).
        async Task<Downstream> ReadMessageAsync(CancellationToken cancellationToken)
        {
            byte[] header = new byte[4];
            await ReadExactlyAsync(header, 0, 4, cancellationToken).ConfigureAwait(false);

            if (header[0] == 0x12)
            {
                // Raw push without length prefix: 0x12 (Push field tag) followed
                // by a varint length and the Push payload.
                byte[] raw = await ReadRawPushAsync(header, cancellationToken).ConfigureAwait(false);
                return Downstream.Parser.ParseFrom(raw);
            }

            int messageLength = ReadBigEndianUInt32(header, 0);
            if (messageLength <= 0 || messageLength > 16 * 1024 * 1024)
                throw new UbisoftException("Ungültige Nachrichtenlänge vom Ubisoft-Server.");

            byte[] message = new byte[messageLength];
            await ReadExactlyAsync(message, 0, messageLength, cancellationToken).ConfigureAwait(false);
            return Downstream.Parser.ParseFrom(message);
        }

        // Reads a raw (non length-prefixed) push message. The first byte is the
        // Push field tag (0x12); the varint length and payload follow.
        async Task<byte[]> ReadRawPushAsync(byte[] firstBytes, CancellationToken cancellationToken)
        {
            using (var buffer = new MemoryStream())
            {
                buffer.Write(firstBytes, 0, firstBytes.Length);

                // Byte 0 is the tag (0x12). Parse the length varint starting
                // at byte 1, using already-read bytes first, then the stream.
                int offset = 1;
                int length = 0;
                int shift = 0;
                while (true)
                {
                    byte b;
                    if (offset < firstBytes.Length)
                    {
                        b = firstBytes[offset++];
                    }
                    else
                    {
                        byte[] nb = new byte[1];
                        await ReadExactlyAsync(nb, 0, 1, cancellationToken).ConfigureAwait(false);
                        buffer.WriteByte(nb[0]);
                        b = nb[0];
                    }
                    length |= (b & 0x7F) << shift;
                    if ((b & 0x80) == 0)
                        break;
                    shift += 7;
                    if (shift > 35)
                        throw new UbisoftException("Ungültiges Protobuf-Format vom Ubisoft-Server.");
                }

                if (length <= 0 || length > 16 * 1024 * 1024)
                    throw new UbisoftException("Ungültige Push-Länge vom Ubisoft-Server.");

                // Some payload bytes may already be part of firstBytes.
                int alreadyHave = Math.Max(0, firstBytes.Length - offset);
                int toRead = length - alreadyHave;
                if (toRead > 0)
                {
                    byte[] payload = new byte[toRead];
                    await ReadExactlyAsync(payload, 0, toRead, cancellationToken).ConfigureAwait(false);
                    buffer.Write(payload, 0, toRead);
                }

                return buffer.ToArray();
            }
        }

        static byte[] FormatLengthPrefixed(byte[] payload)
        {
            byte[] framed = new byte[4 + payload.Length];
            WriteBigEndianUInt32(framed, 0, (uint)payload.Length);
            Buffer.BlockCopy(payload, 0, framed, 4, payload.Length);
            return framed;
        }

        static int ReadBigEndianUInt32(byte[] buffer, int offset)
        {
            return (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];
        }

        static void WriteBigEndianUInt32(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        async Task ReadExactlyAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int read = 0;
            while (read < count)
            {
                int n = await sslStream.ReadAsync(buffer, offset + read, count - read, cancellationToken).ConfigureAwait(false);
                if (n <= 0)
                    throw new UbisoftException("Die Ubisoft-Verbindung wurde unerwartet geschlossen.");
                read += n;
            }
        }

        public void Disconnect()
        {
            try { sslStream?.Dispose(); } catch { }
            try { tcpClient?.Dispose(); } catch { }
            sslStream = null;
            tcpClient = null;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }

    public class UbisoftException : Exception
    {
        public UbisoftException(string message) : base(message) { }
        public UbisoftException(string message, Exception inner) : base(message, inner) { }
    }
}
