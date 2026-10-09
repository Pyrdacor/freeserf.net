/*
 * UbisoftLogin.cs - Login against the Ubisoft Connect API
 *
 * Part of the Ubisoft Connect SPAE.PA download feature for freeserf.net.
 * Uses the (unofficial) public Ubisoft services API that the Ubisoft
 * Connect client itself uses.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Freeserf.Android.Ubisoft
{
    public static class UbisoftLogin
    {
        // App id used by the Ubisoft Connect downloader tooling (UplayDB).
        // Required by the API for every request.
        public const string AppId = "f68a4bb5-608a-4ff2-8123-be8ef797e0a6";
        public const string UserAgent = "Massgate";
        public const string SessionUrl = "https://public-ubiservices.ubi.com/v3/profiles/sessions";

        // Web login (used by the Ubisoft Connect web app and third-party
        // integrations). After a successful login the session (ticket +
        // sessionId) is stored in the browser's localStorage under
        // "PRODloginData" on the connect.ubisoft.com domain.
        public const string GenomeId = "954e66a0-be1b-4aa0-9690-fb75201e4e9e";
        public const string LoginUrl =
            "https://connect.ubisoft.com/login?appId=" + AppId +
            "&genomeId=" + GenomeId +
            "&lang=en-US&nextUrl=https:%2F%2Fconnect.ubisoft.com%2F";

        static readonly HttpClient httpClient = CreateHttpClient();

        static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            client.DefaultRequestHeaders.Add("Ubi-AppId", AppId);
            client.DefaultRequestHeaders.Add("Ubi-RequestedPlatformType", "uplay");
            return client;
        }

        // First login step with email and password. If the account has two
        // factor authentication enabled, the result contains a
        // TwoFactorAuthenticationTicket and RequiresTwoFactorAuthentication
        // is set; the caller must then call LoginWithTwoFactorCode.
        public static async Task<UbisoftLoginResult> LoginAsync(string email, string password,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            {
                return new UbisoftLoginResult
                {
                    Success = false,
                    ErrorMessage = "Bitte E-Mail und Passwort eingeben."
                };
            }

            string basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{password}"));
            return await PostSessionAsync($"Basic {basic}", null, null, cancellationToken).ConfigureAwait(false);
        }

        // Second login step for accounts with two factor authentication.
        public static async Task<UbisoftLoginResult> LoginWithTwoFactorCodeAsync(string twoFactorTicket,
            string code, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(twoFactorTicket) || string.IsNullOrEmpty(code))
            {
                return new UbisoftLoginResult
                {
                    Success = false,
                    ErrorMessage = "Bitte den 2FA-Code eingeben."
                };
            }

            return await PostSessionAsync($"ubi_2fa_v1 t={twoFactorTicket}", code, null, cancellationToken).ConfigureAwait(false);
        }

        // Login with a previously stored remember-me ticket (no password needed).
        public static async Task<UbisoftLoginResult> LoginWithRememberMeTicketAsync(string rememberMeTicket,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(rememberMeTicket))
            {
                return new UbisoftLoginResult
                {
                    Success = false,
                    ErrorMessage = "Kein gespeicherter Login vorhanden."
                };
            }

            return await PostSessionAsync($"rm_v1 t={rememberMeTicket}", null, null, cancellationToken).ConfigureAwait(false);
        }

        // Renews a session captured from the web login (browser localStorage)
        // under this app id. The browser ticket may be scoped to a different
        // app id; renewing it makes it usable for the download API.
        public static async Task<UbisoftLoginResult> RenewSessionAsync(string ticket, string sessionId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(ticket))
            {
                return new UbisoftLoginResult
                {
                    Success = false,
                    ErrorMessage = "Kein gültiger Login vorhanden."
                };
            }

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Put, SessionUrl))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", $"Ubi_v1 t={ticket}");
                    if (!string.IsNullOrEmpty(sessionId))
                        request.Headers.TryAddWithoutValidation("Ubi-SessionId", sessionId);

                    request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

                    using (var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        UbisoftLoginResponse loginResponse = null;
                        try
                        {
                            loginResponse = JsonSerializer.Deserialize<UbisoftLoginResponse>(body);
                        }
                        catch
                        {
                            // Non-JSON error body; handled below.
                        }

                        if (response.IsSuccessStatusCode && loginResponse != null && !string.IsNullOrEmpty(loginResponse.Ticket))
                        {
                            return new UbisoftLoginResult
                            {
                                Success = true,
                                Response = loginResponse
                            };
                        }

                        string message = loginResponse?.Message;
                        if (string.IsNullOrEmpty(message))
                            message = $"Login fehlgeschlagen (HTTP {(int)response.StatusCode}).";

                        return new UbisoftLoginResult
                        {
                            Success = false,
                            ErrorMessage = message
                        };
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new UbisoftLoginResult
                {
                    Success = false,
                    ErrorMessage = "Netzwerkfehler: " + ex.Message
                };
            }
        }

        static async Task<UbisoftLoginResult> PostSessionAsync(string authorization, string twoFactorCode,
            string rememberDeviceTicket, CancellationToken cancellationToken)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, SessionUrl))
                {
                    // Use TryAddWithoutValidation: the Ubisoft API uses
                    // non-standard auth schemes (e.g. "ubi_2fa_v1 t=...") that
                    // AuthenticationHeaderValue would reject.
                    request.Headers.TryAddWithoutValidation("Authorization", authorization);
                    if (!string.IsNullOrEmpty(twoFactorCode))
                        request.Headers.Add("Ubi-2FACode", twoFactorCode);
                    if (!string.IsNullOrEmpty(rememberDeviceTicket))
                        request.Headers.Add("Ubi-RememberDeviceTicket", rememberDeviceTicket);

                    request.Content = new StringContent("{\"rememberMe\": true}", Encoding.UTF8, "application/json");

                    using (var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        UbisoftLoginResponse loginResponse = null;
                        try
                        {
                            loginResponse = JsonSerializer.Deserialize<UbisoftLoginResponse>(body);
                        }
                        catch
                        {
                            // Non-JSON error body; handled below.
                        }

                        if (response.IsSuccessStatusCode && loginResponse != null && !string.IsNullOrEmpty(loginResponse.Ticket))
                        {
                            return new UbisoftLoginResult
                            {
                                Success = true,
                                Response = loginResponse
                            };
                        }

                        if (loginResponse != null && loginResponse.RequiresTwoFactorAuthentication)
                        {
                            return new UbisoftLoginResult
                            {
                                Success = false,
                                RequiresTwoFactorAuthentication = true,
                                Response = loginResponse
                            };
                        }

                        string message = loginResponse?.Message;
                        if (string.IsNullOrEmpty(message))
                            message = $"Login fehlgeschlagen (HTTP {(int)response.StatusCode}).";

                        return new UbisoftLoginResult
                        {
                            Success = false,
                            ErrorMessage = message
                        };
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new UbisoftLoginResult
                {
                    Success = false,
                    ErrorMessage = "Netzwerkfehler: " + ex.Message
                };
            }
        }
    }
}
