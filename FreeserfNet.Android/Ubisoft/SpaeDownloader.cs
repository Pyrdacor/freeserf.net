/*
 * SpaeDownloader.cs - Downloads the SPAE.PA data file from Ubisoft Connect
 *
 * Part of the Ubisoft Connect SPAE.PA download feature for freeserf.net.
 * Only downloads the manifest and the slices of the SPAE.PA file - not the
 * whole game. Requires the user to own "The Settlers - History Edition"
 * (product id 11662) on their Ubisoft Connect account.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */

using Mg.Protocol.Download;
using Mg.Protocol.Ownership;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZstdSharp;

namespace Freeserf.Android.Ubisoft
{
    public class SpaeDownloader
    {
        // "The Settlers - History Edition" (Siedler 1) on Ubisoft Connect.
        public const uint SettlersHistoryEditionProductId = 11662;
        public const string DataFileName = "SPAE.PA";

        static readonly HttpClient httpClient = CreateHttpClient();

        public delegate void ProgressCallback(string status, int percent);

        readonly UbisoftLoginResponse login;
        readonly string targetDirectory;

        public SpaeDownloader(UbisoftLoginResponse login, string targetDirectory)
        {
            this.login = login;
            this.targetDirectory = targetDirectory;
        }

        static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10);
            return client;
        }

        // Runs the whole download flow. Returns the path of the downloaded
        // SPAE.PA file.
        public async Task<string> DownloadAsync(ProgressCallback progress, CancellationToken cancellationToken = default)
        {
            // 1. Connect to the demux socket and authenticate
            progress?.Invoke("Verbinde mit Ubisoft…", 5);
            Console.Error.WriteLine("Ubisoft: connecting to demux socket...");
            using (var demux = new DemuxClient())
            {
                await demux.ConnectAsync(cancellationToken).ConfigureAwait(false);
                Console.Error.WriteLine("Ubisoft: demux connected, authenticating...");
                bool authenticated = await demux.AuthenticateAsync(login.Ticket, cancellationToken).ConfigureAwait(false);
                if (!authenticated)
                    throw new UbisoftException("Die Ubisoft-Authentifizierung ist fehlgeschlagen.");
                Console.Error.WriteLine("Ubisoft: authenticated.");

                // 2. Check ownership and get the ownership token
                progress?.Invoke("Prüfe Besitz der History Edition…", 10);
                var ownership = new OwnershipService(demux);
                await ownership.ConnectAsync(cancellationToken).ConfigureAwait(false);
                var ownedGames = await ownership.GetOwnedGamesAsync(login.Ticket, login.SessionId, cancellationToken).ConfigureAwait(false);
                Console.Error.WriteLine($"Ubisoft: got {ownedGames.Count} owned games.");

                OwnedGame settlers = null;
                foreach (var game in ownedGames)
                {
                    if (game.ProductId == SettlersHistoryEditionProductId)
                    {
                        settlers = game;
                        break;
                    }
                }

                if (settlers == null)
                    throw new UbisoftException("Sie besitzen die Siedler 1 History Edition nicht auf diesem Ubisoft-Konto.");

                if (string.IsNullOrEmpty(settlers.LatestManifest))
                    throw new UbisoftException("Für die History Edition ist kein Manifest verfügbar.");

                var (ownershipToken, _) = await ownership.GetOwnershipTokenAsync(SettlersHistoryEditionProductId,
                    login.Ticket, login.SessionId, cancellationToken).ConfigureAwait(false);

                // 3. Initialize the download service
                progress?.Invoke("Initialisiere Download…", 15);
                var download = new DownloadService(demux);
                await download.ConnectAsync(cancellationToken).ConfigureAwait(false);
                bool initialized = await download.InitializeAsync(ownershipToken, cancellationToken).ConfigureAwait(false);
                if (!initialized)
                    throw new UbisoftException("Der Download-Dienst konnte nicht initialisiert werden.");

                // 4. Download and parse the manifest
                progress?.Invoke("Lade Spielmanifest…", 20);
                Console.Error.WriteLine($"Ubisoft: downloading manifest {settlers.LatestManifest}...");
                byte[] manifestBytes = await DownloadManifestAsync(download, settlers.LatestManifest, cancellationToken).ConfigureAwait(false);
                Console.Error.WriteLine($"Ubisoft: manifest downloaded ({manifestBytes.Length} bytes), parsing...");
                var manifest = ManifestParser.Parse(manifestBytes);
                Console.Error.WriteLine($"Ubisoft: manifest parsed, version={manifest.Version}, compressed={manifest.IsCompressed}, method={manifest.CompressionMethod}.");

                // 5. Find the SPAE.PA file
                var slices = ManifestParser.FindFileSlices(manifest, DataFileName);
                if (slices == null || slices.Count == 0)
                {
                    // Log the manifest contents to help diagnosing the file layout.
                    int count = 0;
                    foreach (var chunk in manifest.Chunks)
                    {
                        foreach (var file in chunk.Files)
                        {
                            Console.Error.WriteLine($"Ubisoft: manifest file: '{file.Name}' (chunk lang='{chunk.Language}', {file.SliceList.Count} slices)");
                            if (++count >= 200)
                                break;
                        }
                        if (count >= 200)
                            break;
                    }
                    throw new UbisoftException($"Die Datei {DataFileName} wurde im Spielmanifest nicht gefunden.");
                }

                // 6. Download all slices of the SPAE.PA file
                var slicePaths = ManifestParser.GetSlicePaths(manifest, slices);
                progress?.Invoke("Fordere Download-URLs an…", 25);
                var urls = await download.GetUrlsAsync(SettlersHistoryEditionProductId, slicePaths, cancellationToken).ConfigureAwait(false);
                if (urls.Count != slices.Count)
                    throw new UbisoftException("Die Anzahl der Download-URLs passt nicht zu den Slices.");

                string targetPath = Path.Combine(targetDirectory, DataFileName);
                using (var output = System.IO.File.Create(targetPath))
                {
                    for (int i = 0; i < slices.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        int percent = 25 + (int)(70.0 * i / slices.Count);
                        progress?.Invoke($"Lade {DataFileName} herunter ({i + 1}/{slices.Count})…", percent);

                        byte[] sliceBytes = await DownloadSliceAsync(urls[i], cancellationToken).ConfigureAwait(false);
                        byte[] decompressed = DecompressSlice(manifest, sliceBytes, slices[i].Size);
                        output.Write(decompressed, 0, decompressed.Length);
                    }
                }

                progress?.Invoke($"{DataFileName} erfolgreich heruntergeladen.", 100);
                return targetPath;
            }
        }

        async Task<byte[]> DownloadManifestAsync(DownloadService download, string manifestId, CancellationToken cancellationToken)
        {
            var urls = await download.GetUrlsAsync(SettlersHistoryEditionProductId,
                new List<string> { $"manifests/{manifestId}.manifest" }, cancellationToken).ConfigureAwait(false);
            if (urls.Count == 0 || urls[0].Count == 0)
                throw new UbisoftException("Keine Manifest-URL erhalten.");

            return await DownloadFromUrlsAsync(urls[0], cancellationToken).ConfigureAwait(false);
        }

        async Task<byte[]> DownloadSliceAsync(List<string> urls, CancellationToken cancellationToken)
        {
            return await DownloadFromUrlsAsync(urls, cancellationToken).ConfigureAwait(false);
        }

        // Downloads from the first URL that works (the server provides
        // multiple mirror URLs per file).
        async Task<byte[]> DownloadFromUrlsAsync(List<string> urls, CancellationToken cancellationToken)
        {
            Exception lastError = null;
            foreach (var url in urls)
            {
                try
                {
                    using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Console.Error.WriteLine($"Ubisoft download failed for {url}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            throw new UbisoftException("Download fehlgeschlagen: " + (lastError?.Message ?? "unbekannter Fehler"));
        }

        static byte[] DecompressSlice(Manifest manifest, byte[] sliceBytes, ulong outputSize)
        {
            if (!manifest.IsCompressed)
                return sliceBytes;

            string method = manifest.CompressionMethod.ToString().ToLowerInvariant();
            if (method.StartsWith("compressionmethod_"))
                method = method.Substring("compressionmethod_".Length);

            switch (method)
            {
                case "zstd":
                    using (var decompressor = new Decompressor())
                    {
                        return decompressor.Unwrap(sliceBytes).ToArray();
                    }
                case "deflate":
                    using (var input = new MemoryStream(sliceBytes))
                    using (var zlib = new ZLibStream(input, CompressionMode.Decompress))
                    using (var output = new MemoryStream((int)outputSize))
                    {
                        zlib.CopyTo(output);
                        return output.ToArray();
                    }
                case "lzham":
                    throw new UbisoftException("Die lzham-Kompression wird auf Android nicht unterstützt.");
                default:
                    throw new UbisoftException($"Unbekannte Kompressionsmethode: {method}");
            }
        }
    }
}
