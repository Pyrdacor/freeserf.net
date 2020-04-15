using System;
using System.Threading;
using System.Threading.Tasks;

namespace Freeserf.Network
{
    internal class ConnectionObserver
    {
        const double ConnectionTimeoutInSeconds = 10.0;
        const double NeedRefreshTimeoutInSeconds = 1.5;

        readonly Func<DateTime> lastHeartbeatTimeProvider;
        readonly int checkDelay;
        readonly CancellationToken cancellationToken;

        public ConnectionObserver(Func<DateTime> lastHeartbeatTimeProvider, int checkDelay, CancellationToken cancellationToken)
        {
            this.lastHeartbeatTimeProvider = lastHeartbeatTimeProvider;
            this.checkDelay = checkDelay;
        }

        public void Run()
        {
            Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    double noResponseTime = (DateTime.UtcNow - lastHeartbeatTimeProvider()).TotalSeconds;

                    if (noResponseTime > ConnectionTimeoutInSeconds)
                    {
                        ConnectionLost?.Invoke();
                        break;
                    }
                    else if (noResponseTime > NeedRefreshTimeoutInSeconds)
                    {
                        DataRefreshNeeded?.Invoke();
                    }

                    Thread.Sleep(checkDelay);
                }
            });
        }

        public event Action ConnectionLost;
        public event Action DataRefreshNeeded;
    }
}
