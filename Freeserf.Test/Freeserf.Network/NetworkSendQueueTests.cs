using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Freeserf.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Network
{
    [TestClass]
    public class NetworkSendQueueTests
    {
        sealed class StalledStream : MemoryStream
        {
            internal readonly TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                Started.TrySetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }

        sealed class RecordingStream : MemoryStream
        {
            int writes;
            internal readonly TaskCompletionSource Written = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                await base.WriteAsync(buffer, cancellationToken);
                if (++writes == 300) // Three writes per packet, 100 packets.
                    Written.TrySetResult();
            }
        }

        sealed class FailingStream : MemoryStream
        {
            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
                => ValueTask.FromException(new IOException("Connection timed out"));
        }

        [TestMethod]
        public async Task StalledPeer_ShouldNotBlockCallerAndShouldTimeOut()
        {
            using var stream = new StalledStream();
            var failure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
            int closed = 0;
            using var sender = new NetworkSendQueue(stream, () => Interlocked.Increment(ref closed),
                ex => failure.TrySetResult(ex), TimeSpan.FromMilliseconds(200));

            await Task.Run(() => sender.Send(new byte[1024])).WaitAsync(TimeSpan.FromSeconds(2));
            await stream.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            var error = await failure.Task.WaitAsync(TimeSpan.FromSeconds(3));
            await sender.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            Assert.IsInstanceOfType<IOException>(error);
            Assert.AreEqual(1, closed);
        }

        [TestMethod]
        public async Task Packets_ShouldKeepOrderAndFraming()
        {
            using var stream = new RecordingStream();
            using var sender = new NetworkSendQueue(stream, () => { }, ex => Assert.Fail(ex.ToString()));
            for (int i = 0; i < 100; ++i)
                sender.Send(BitConverter.GetBytes(i));

            await stream.Written.Task.WaitAsync(TimeSpan.FromSeconds(3));
            sender.Dispose();
            await sender.Completion.WaitAsync(TimeSpan.FromSeconds(2));
            using var reader = new BinaryReader(new MemoryStream(stream.ToArray()));
            for (int i = 0; i < 100; ++i)
            {
                CollectionAssert.AreEqual(new byte[] { (byte)'F', (byte)'S', (byte)'N', 0 }, reader.ReadBytes(4));
                Assert.AreEqual(4u, reader.ReadUInt32());
                Assert.AreEqual(i, reader.ReadInt32());
            }
        }

        [TestMethod]
        public async Task GracefulClose_ShouldFlushQueuedPacketsBeforeClosing()
        {
            using var stream = new MemoryStream();
            byte[] delivered = null;
            int failures = 0;
            using var sender = new NetworkSendQueue(stream, () => delivered = stream.ToArray(),
                ex => Interlocked.Increment(ref failures));
            sender.Send(new byte[] { 1, 2, 3 });
            sender.Complete();
            sender.Send(new byte[] { 99 }); // No new packets after closing starts.
            await sender.Completion.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.AreEqual(11, delivered.Length);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, delivered[8..]);
            Assert.AreEqual(0, failures);
        }

        [TestMethod]
        public async Task WriteFailure_ShouldClosePeerAndNotifyOnlyOnce()
        {
            using var stream = new FailingStream();
            int failures = 0, closed = 0;
            using var sender = new NetworkSendQueue(stream, () => Interlocked.Increment(ref closed),
                ex => Interlocked.Increment(ref failures));
            sender.Send(new byte[1]);
            await sender.Completion.WaitAsync(TimeSpan.FromSeconds(2));
            sender.Send(new byte[1]);
            sender.Dispose();
            Assert.AreEqual(1, failures);
            Assert.AreEqual(1, closed);
        }

        [TestMethod]
        public async Task QueueLimit_ShouldDisconnectSlowPeerWithoutGrowingIndefinitely()
        {
            using var stream = new StalledStream();
            int failures = 0;
            using var sender = new NetworkSendQueue(stream, () => { }, ex => Interlocked.Increment(ref failures),
                TimeSpan.FromSeconds(10), maxPendingBytes: 32);
            sender.Send(new byte[8]);
            await stream.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            sender.Send(new byte[32]);
            await sender.Completion.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.AreEqual(1, failures);
        }

        [TestMethod]
        public async Task Shutdown_ShouldCancelPendingWriteWithoutReportingFailure()
        {
            using var stream = new StalledStream();
            int failures = 0;
            using var sender = new NetworkSendQueue(stream, () => { }, ex => Interlocked.Increment(ref failures));
            sender.Send(new byte[8]);
            await stream.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            sender.Dispose();
            await sender.Completion.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.AreEqual(0, failures);
        }

        [TestMethod]
        public async Task RealSocketWithNonReadingPeer_ShouldTimeOutWithoutBlockingCaller()
        {
            using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            // Set the receive window before the TCP handshake. Changing only
            // the accepted socket can leave a large negotiated/autotuned window
            // on Windows, allowing the entire payload to be buffered.
            listener.Server.ReceiveBufferSize = 1024;
            listener.Start();
            using var client = new System.Net.Sockets.TcpClient();
            client.SendBufferSize = 1024;
            var accept = listener.AcceptTcpClientAsync();
            await client.ConnectAsync((System.Net.IPEndPoint)listener.LocalEndpoint);
            using var peer = await accept;
            peer.ReceiveBufferSize = 1024;
            var failure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var sender = new NetworkSendQueue(client.GetStream(), client.Close,
                ex => failure.TrySetResult(ex), TimeSpan.FromMilliseconds(300));

            await Task.Run(() => sender.Send(new byte[8 * 1024 * 1024])).WaitAsync(TimeSpan.FromSeconds(2));
            var error = await failure.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await sender.Completion.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.IsInstanceOfType<IOException>(error);
            Assert.IsInstanceOfType<OperationCanceledException>(error.InnerException);
        }

        [TestMethod]
        public async Task ReceiveCallback_ShouldNotHoldReceiveQueueLock()
        {
            var receiver = new NetworkDataReceiver();
            receiver.Receive(null, null, null);
            Task enqueue = null;
            receiver.ProcessReceivedData((source, data, response) =>
            {
                enqueue = Task.Run(() => receiver.Receive(null, null, null));
                Assert.IsTrue(enqueue.Wait(TimeSpan.FromSeconds(2)), "Receive was blocked by a game-thread callback.");
            });
            await enqueue;
            int received = 0;
            receiver.ProcessReceivedData((source, data, response) => ++received);
            Assert.AreEqual(1, received);
        }
    }
}
