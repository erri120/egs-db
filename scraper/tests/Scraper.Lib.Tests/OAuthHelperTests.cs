using System.Text;
using Moq.Contrib.HttpClient;
using Scraper.Lib.Models;
using Scraper.Lib.ValueObjects;

namespace Scraper.Lib.Tests;

public class OAuthHelperTests
{
    [Theory, AutoData]
    public void Test_ToBase64(Guid guid1, Guid guid2)
    {
        var clientId = OAuthClientId.From(guid1.ToString("N"));
        var clientSecret = OAuthClientSecret.From(guid2.ToString("N"));

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

    [Theory, AutoData]
    public async Task Test_GetOAuthTokenAsync(Guid guid1, Guid guid2)
    {
        var clientId = OAuthClientId.From(guid1.ToString("N"));
        var clientSecret = OAuthClientSecret.From(guid2.ToString("N"));

        var base64 = OAuthHelper.ClientIdAndSecretToBase64(clientId, clientSecret);

        var expectedResponse = new OAuthResponse(
            OAuthToken.From("foo"),
            DateTime.Now,
            OAuthRefreshToken.From("foo"),
            DateTime.Now,
            clientId
        );

        var httpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        httpMessageHandler
            .SetupRequest(
                HttpMethod.Post,
                OAuthHelper.OAuthTokenUrl,
                async request => string.Equals(request.Headers.Authorization?.Parameter, base64, StringComparison.Ordinal))
            .ReturnsJsonResponse(expectedResponse);

        var oauthHelper = new OAuthHelper(httpMessageHandler.Object, clientId, clientSecret);

        var res = await oauthHelper.GetOAuthTokenAsync("foo");
        res.IsT0.Should().BeTrue(res.IsT1 ? res.AsT1.Value : string.Empty);

        var actualResponse = res.AsT0;
        actualResponse.Should().Be(expectedResponse);
    }
}
