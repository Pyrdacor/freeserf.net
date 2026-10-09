/*
 * UbisoftModels.cs - JSON models for the Ubisoft Connect API
 *
 * Part of the Ubisoft Connect SPAE.PA download feature for freeserf.net.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */

using System.Text.Json.Serialization;

namespace Freeserf.Android.Ubisoft
{
    // Response of POST https://public-ubiservices.ubi.com/v3/profiles/sessions
    public class UbisoftLoginResponse
    {
        [JsonPropertyName("ticket")]
        public string Ticket { get; set; }

        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; }

        [JsonPropertyName("profileId")]
        public string ProfileId { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("nameOnPlatform")]
        public string NameOnPlatform { get; set; }

        [JsonPropertyName("rememberMeTicket")]
        public string RememberMeTicket { get; set; }

        // Present when the account requires two-factor authentication.
        [JsonPropertyName("twoFactorAuthenticationTicket")]
        public string TwoFactorAuthenticationTicket { get; set; }

        [JsonPropertyName("environment")]
        public string Environment { get; set; }

        [JsonPropertyName("expiration")]
        public string Expiration { get; set; }

        [JsonPropertyName("spaceId")]
        public string SpaceId { get; set; }

        [JsonPropertyName("clientIp")]
        public string ClientIp { get; set; }

        [JsonPropertyName("serverTime")]
        public string ServerTime { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("httpCode")]
        public int? HttpCode { get; set; }

        public bool RequiresTwoFactorAuthentication
        {
            get => !string.IsNullOrEmpty(TwoFactorAuthenticationTicket);
        }
    }

    // Result of a login attempt.
    public class UbisoftLoginResult
    {
        public bool Success { get; set; }
        public bool RequiresTwoFactorAuthentication { get; set; }
        public string ErrorMessage { get; set; }
        public UbisoftLoginResponse Response { get; set; }
    }
}
