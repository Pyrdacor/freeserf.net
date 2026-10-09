/*
 * DownloadService.cs - Download URL requests against the Ubisoft Connect API
 *
 * Part of the Ubisoft Connect SPAE.PA download feature for freeserf.net.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */

using Mg.Protocol.DownloadService;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Freeserf.Android.Ubisoft
{
    public class DownloadService
    {
        const string ServiceName = "download_service";

        readonly DemuxClient demuxClient;
        uint connectionId;
        bool initialized;

        public DownloadService(DemuxClient demuxClient)
        {
            this.demuxClient = demuxClient;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            connectionId = await demuxClient.OpenConnectionAsync(ServiceName, cancellationToken).ConfigureAwait(false);
        }

        // Initializes the download service with the ownership token obtained
        // from the ownership service.
        public async Task<bool> InitializeAsync(string ownershipToken, CancellationToken cancellationToken = default)
        {
            var request = new Upstream
            {
                Request = new Req
                {
                    RequestId = 1,
                    InitializeReq = new InitializeReq
                    {
                        OwnershipToken = ownershipToken,
                        NetworkId = string.Empty
                    }
                }
            };

            var response = await demuxClient.SendServiceRequestAsync<Upstream, Downstream>(connectionId, request, cancellationToken).ConfigureAwait(false);
            if (response == null || response.Response == null || response.Response.InitializeRsp == null)
                return false;

            initialized = response.Response.InitializeRsp.Ok;
            return initialized;
        }

        // Returns the download URLs for the given relative file paths (e.g.
        // "manifests/xxx.manifest" or "slices/abc123"). The result contains
        // one entry per requested path, each with the mirror URLs for that
        // file.
        public async Task<List<List<string>>> GetUrlsAsync(uint productId, List<string> relativePaths,
            CancellationToken cancellationToken = default)
        {
            if (!initialized)
                throw new UbisoftException("Der Download-Dienst ist nicht initialisiert.");

            var urlRequest = new UrlReq.Types.Request();
            urlRequest.ProductId = productId;
            urlRequest.RelativeFilePath.AddRange(relativePaths);

            var request = new Upstream
            {
                Request = new Req
                {
                    RequestId = 1,
                    UrlReq = new UrlReq
                    {
                        UrlRequests = { urlRequest }
                    }
                }
            };

            var response = await demuxClient.SendServiceRequestAsync<Upstream, Downstream>(connectionId, request, cancellationToken).ConfigureAwait(false);
            if (response == null || response.Response == null || response.Response.UrlRsp == null
                || response.Response.UrlRsp.UrlResponses.Count == 0)
                throw new UbisoftException("Keine Download-URLs erhalten.");

            var urlResponse = response.Response.UrlRsp.UrlResponses[0];
            if (urlResponse.Result != UrlRsp.Types.Result.Success || urlResponse.DownloadUrls.Count == 0)
                throw new UbisoftException("Download-URLs wurden verweigert (nicht besessen?).");

            var result = new List<List<string>>(urlResponse.DownloadUrls.Count);
            foreach (var downloadUrls in urlResponse.DownloadUrls)
            {
                var mirrors = new List<string>();
                foreach (var url in downloadUrls.Urls)
                {
                    if (!string.IsNullOrEmpty(url))
                        mirrors.Add(url);
                }
                result.Add(mirrors);
            }

            return result;
        }
    }
}
