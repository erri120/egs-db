using System;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Scraper.Lib.ValueObjects;

namespace Scraper.Lib.Models;

[PublicAPI]
public record OAuthResponse(
    [property: JsonPropertyName("access_token")]
    OAuthToken AccessToken,

    [property: JsonPropertyName("expires_at")]
    DateTimeOffset ExpiresAt,

    [property: JsonPropertyName("refresh_token")]
    OAuthRefreshToken RefreshToken,

    [property: JsonPropertyName("refresh_expires_at")]
    DateTimeOffset RefreshExpiresAt
);
