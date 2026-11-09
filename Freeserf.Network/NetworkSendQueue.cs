using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Freeserf.Network
{
    // A single writer preserves packet boundaries when several threads send to one peer.
    // The game thread only enqueues packets; a stalled peer cannot stop rendering or simulation.
    internal sealed class NetworkSendQueue : IDisposable
    {
        readonly Stream stream;
        readonly Action closeConnection;
        readonly Action<Exception> connectionLost;
        readonly TimeSpan sendTimeout;
        readonly long maxPendingBytes;
        readonly Channel<byte[]> packets = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = false
        });
        readonly CancellationTokenSource cancellation = new();
        readonly object sync = new();
        long pendingBytes;
        bool stopped;
        bool completing;

        internal Task Completion { get; }

        internal NetworkSendQueue(Stream stream, Action closeConnection, Action<Exception> connectionLost,
            TimeSpan? sendTimeout = null, long maxPendingBytes = 128 * 1024 * 1024)
        {
            this.stream = stream;
            this.closeConnection = closeConnection;
            this.connectionLost = connectionLost;
            this.sendTimeout = sendTimeout ?? TimeSpan.FromSeconds(10);
            this.maxPendingBytes = maxPendingBytes;
            Completion = Task.Run(SendPackets);
        }

        internal void Send(byte[] data)
        {
            lock (sync)
            {
                if (stopped || completing)
                    return;

                if (pendingBytes + data.LongLength + 8 <= maxPendingBytes)
                {
                    // Own the queued buffer; callers may reuse theirs after Send returns.
                    var copy = (byte[])data.Clone();
                    pendingBytes += copy.LongLength + 8;
                    packets.Writer.TryWrite(copy);
                    return;
                }
            }

            Stop(new IOException("Network send queue exceeded its size limit; peer is not keeping up."));
        }

        async Task SendPackets()
        {
            try
            {
                await foreach (var data in packets.Reader.ReadAllAsync(cancellation.Token))
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token);
                    timeout.CancelAfter(sendTimeout);
                    await Host.WriteDataAsync(stream, data, timeout.Token);
                    lock (sync)
                        pendingBytes -= data.LongLength + 8;
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // Normal shutdown.
            }
            catch (Exception ex) when (ex is IOException || ex is System.Net.Sockets.SocketException ||
                ex is ObjectDisposedException || ex is InvalidOperationException || ex is OperationCanceledException)
            {
                Stop(ex is OperationCanceledException
                    ? new IOException("Timed out sending a network packet.", ex) : ex);
            }
            finally
            {
                Stop(null);
                // Release large queued game states even if the peer remains referenced.
                while (packets.Reader.TryRead(out _)) { }
            }
        }

        void Stop(Exception error)
        {
            lock (sync)
            {
                if (stopped)
                    return;
                stopped = true;
                packets.Writer.TryComplete();
                cancellation.Cancel();
            }

            closeConnection(); // Also interrupts outstanding reads and writes.
            if (error != null)
                connectionLost(error);
        }

        internal void Complete()
        {
            lock (sync)
            {
                if (stopped || completing)
                    return;
                completing = true;
                packets.Writer.TryComplete();
                // Best-effort delivery of the final disconnect message, bounded in total time.
                cancellation.CancelAfter(sendTimeout);
            }
        }

        public void Dispose() => Stop(null);
    }
}
