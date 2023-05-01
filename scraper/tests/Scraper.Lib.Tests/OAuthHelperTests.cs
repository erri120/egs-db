using System.Text;
using System.Web;
using Moq.Contrib.HttpClient;
using Scraper.Lib.Models;
using Scraper.Lib.Tests.Setup;
using Scraper.Lib.ValueObjects;

namespace Scraper.Lib.Tests;

public class OAuthHelperTests
{
    [Theory, CustomAutoData]
    public void Test_ToBase64(OAuthClientId clientId, OAuthClientSecret clientSecret)
    {
        var sb = new StringBuilder(clientId.Value.Length + 1 + clientSecret.Value.Length);
        sb.Append(clientId.Value);
        sb.Append(':');
        sb.Append(clientSecret.Value);

        var tmp = sb.ToString();
        var bytes = Encoding.ASCII.GetBytes(tmp);
        var expectedResult = Convert.ToBase64String(bytes);

        var result = OAuthHelper.ClientIdAndSecretToBase64(clientId, clientSecret);
        result.Should().Be(expectedResult);
    }

    [Theory]
    [CustomInlineAutoData(true)]
    [CustomInlineAutoData(false)]
    public async Task Test_GetOAuthTokenAsync(
        bool withAccessToken,
        OAuthClientId clientId,
        OAuthClientSecret clientSecret,
        OAuthToken accessToken,
        DateTime expiresAt,
        OAuthRefreshToken refreshToken,
        DateTime refreshExpiresAt,
        AuthorizationCode authorizationCode)
    {
        var base64 = OAuthHelper.ClientIdAndSecretToBase64(clientId, clientSecret);

        var expectedResponse = new OAuthResponse(
            accessToken,
            expiresAt,
            refreshToken,
            refreshExpiresAt,
            clientId
        );

        var httpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        httpMessageHandler
            .SetupRequest(
                HttpMethod.Post,
                OAuthHelper.OAuthTokenUrl,
                async request =>
                {
                    if (!string.Equals(request.Headers.Authorization?.Parameter, base64, StringComparison.Ordinal))
                        return false;

                    if (request.Content is not FormUrlEncodedContent formUrlEncodedContent)
                        return false;

                    var queryString = await formUrlEncodedContent.ReadAsStringAsync().ConfigureAwait(false);
                    var query = HttpUtility.ParseQueryString(queryString);

                    if (withAccessToken)
                    {
                        return
                            string.Equals(query["grant_type"], "authorization_code", StringComparison.Ordinal) &&
                            string.Equals(query["code"], authorizationCode.Value, StringComparison.Ordinal);
                    }

                    return
                        string.Equals(query["grant_type"], "refresh_token", StringComparison.Ordinal) &&
                        string.Equals(query["refresh_token"], refreshToken.Value, StringComparison.Ordinal);
                })
            .ReturnsJsonResponse(expectedResponse);

        var oauthHelper = new OAuthHelper(httpMessageHandler.Object, clientId, clientSecret);

        var res = await oauthHelper.GetOAuthTokenAsync(withAccessToken ? authorizationCode : refreshToken);
        res.IsT0.Should().BeTrue(res.IsT1 ? res.AsT1.Value : string.Empty);

        var actualResponse = res.AsT0;
        actualResponse.Should().Be(expectedResponse);
    }
}
