/*
 * ConnectionObserver.cs - Observes the connection for timeouts
 *
 * Copyright (C) 2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
            this.cancellationToken = cancellationToken;
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
