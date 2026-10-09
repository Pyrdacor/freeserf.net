/*
 * OwnershipService.cs - Ownership check against the Ubisoft Connect API
 *
 * Part of the Ubisoft Connect SPAE.PA download feature for freeserf.net.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */

using Mg.Protocol.Ownership;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Freeserf.Android.Ubisoft
{
    public class OwnershipService
    {
        const string ServiceName = "ownership_service";

        readonly DemuxClient demuxClient;
        uint connectionId;

        public OwnershipService(DemuxClient demuxClient)
        {
            this.demuxClient = demuxClient;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            connectionId = await demuxClient.OpenConnectionAsync(ServiceName, cancellationToken).ConfigureAwait(false);
        }

        // Initializes the ownership service and returns the owned games.
        public async Task<List<OwnedGame>> GetOwnedGamesAsync(string ticket, string sessionId,
            CancellationToken cancellationToken = default)
        {
            var request = new Upstream
            {
                Request = new Req
                {
                    RequestId = 1,
                    InitializeReq = new InitializeReq
                    {
                        GetAssociations = true,
                        ProtoVersion = 7,
                        UseStaging = false
                    },
                    UbiTicket = ticket,
                    UbiSessionId = sessionId
                }
            };

            var response = await demuxClient.SendServiceRequestAsync<Upstream, Downstream>(connectionId, request, cancellationToken).ConfigureAwait(false);
            if (response == null || response.Response == null || response.Response.InitializeRsp == null)
                throw new UbisoftException("Konnte die Ubisoft-Besitzprüfung nicht initialisieren.");

            var ownedGames = response.Response.InitializeRsp.OwnedGames;
            if (ownedGames == null || ownedGames.OwnedGames_ == null)
                return new List<OwnedGame>();

            return new List<OwnedGame>(ownedGames.OwnedGames_);
        }

        // Requests an ownership token for the given product id. This token is
        // required to initialize the download service.
        public async Task<(string Token, ulong Expiration)> GetOwnershipTokenAsync(uint productId,
            string ticket, string sessionId, CancellationToken cancellationToken = default)
        {
            var request = new Upstream
            {
                Request = new Req
                {
                    RequestId = 1,
                    OwnershipTokenReq = new OwnershipTokenReq { ProductId = productId },
                    UbiTicket = ticket,
                    UbiSessionId = sessionId
                }
            };

            var response = await demuxClient.SendServiceRequestAsync<Upstream, Downstream>(connectionId, request, cancellationToken).ConfigureAwait(false);
            if (response == null || response.Response == null || response.Response.OwnershipTokenRsp == null
                || !response.Response.OwnershipTokenRsp.Success)
                throw new UbisoftException("Kein Ownership-Token erhalten. Besitzen Sie die History Edition?");

            return (response.Response.OwnershipTokenRsp.Token, response.Response.OwnershipTokenRsp.Expiration);
        }
    }
}
