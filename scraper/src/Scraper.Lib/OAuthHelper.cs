using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using OneOf;
using Scraper.Lib.Models;
using Scraper.Lib.ValueObjects;

namespace Scraper.Lib;

[PublicAPI]
public class OAuthHelper
{
    public const string OAuthUrlFormat = "https://www.epicgames.com/id/api/redirect?clientId={0}&responseType=code";
    public const string OAuthTokenUrl = "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/token";

    private readonly HttpClient _httpClient;
    private readonly OAuthClientId _clientId;
    private readonly OAuthClientSecret _clientSecret;

    /// <summary>
    /// URL for getting an authorization code.
    /// </summary>
    public string OAuthUrl => string.Format(
        CultureInfo.InvariantCulture,
        OAuthUrlFormat,
        _clientId.Value
    );

    /// <summary>
    /// Constructor using the default client ID and secret of the "launcherAppClient2" client.
    /// See https://github.com/MixV2/EpicResearch/blob/master/docs/auth/auth_clients.md for more
    /// information.
    /// </summary>
    public OAuthHelper(HttpMessageHandler httpMessageHandler) : this(
        httpMessageHandler,
        OAuthClientId.From("34a02cf8f4414e29b15921876da36f9a"),
        OAuthClientSecret.From("daafbccc737745039dffe53d94fc76cf"))
    { }

    /// <summary>
    /// Constructor for specifying the client ID and secret. See
    /// https://github.com/MixV2/EpicResearch/blob/master/docs/auth/auth_clients.md for a
    /// partial list of known clients and their secrets.
    /// </summary>
    /// <param name="httpMessageHandler"></param>
    /// <param name="clientId"></param>
    /// <param name="clientSecret"></param>
    public OAuthHelper(HttpMessageHandler httpMessageHandler, OAuthClientId clientId, OAuthClientSecret clientSecret)
    {
        _httpClient = new HttpClient(httpMessageHandler);

        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    /// <summary>
    /// Uses the provided authorization code or refresh token to get an OAuth token.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<OneOf<OAuthResponse, OAuthError>> GetOAuthTokenAsync(
        OneOf<AuthorizationCode, OAuthRefreshToken> token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, OAuthTokenUrl);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(
                "basic",
                ClientIdAndSecretToBase64(_clientId, _clientSecret)
            );

            requestMessage.Content = token.Match(
                authorizationCode =>
                {
                    return new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "authorization_code"),
                        new KeyValuePair<string, string>("code", authorizationCode.Value),
                        new KeyValuePair<string, string>("token_type", "eg1"),
                    });
                },
                refreshToken =>
                {
                    return new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                        new KeyValuePair<string, string>("refresh_token", refreshToken.Value),
                        new KeyValuePair<string, string>("token_type", "eg1"),
                    });
                });

            using var responseMessage = await _httpClient
                .SendAsync(requestMessage, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            responseMessage.EnsureSuccessStatusCode();

            var response = await responseMessage.Content
                .ReadFromJsonAsync<OAuthResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (response is null)
                return OAuthError.From($"Deserialization of request for \"{OAuthTokenUrl}\" returned null!");
            return response;
        }
        catch (Exception e)
        {
            return OAuthError.From($"Exception trying to contact endpoint \"{OAuthTokenUrl}\":\n{e}");
        }
    }

    internal static string ClientIdAndSecretToBase64(OAuthClientId clientId, OAuthClientSecret clientSecret)
    {
        var clientIdLength = clientId.Value.Length;
        var clientSecretLength = clientSecret.Value.Length;

        var byteCount = clientIdLength + 1 + clientSecretLength;

        Span<byte> bytes = stackalloc byte[byteCount];

        Encoding.ASCII.GetBytes(clientId.Value, bytes[..clientIdLength]);
        bytes[clientIdLength] = (byte)':';
        Encoding.ASCII.GetBytes(clientSecret.Value, bytes[(clientIdLength + 1)..]);

        return Convert.ToBase64String(bytes);
    }
}
