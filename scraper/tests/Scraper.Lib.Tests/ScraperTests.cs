using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq.Contrib.HttpClient;
using Scraper.Lib.Models;
using Scraper.Lib.Services;
using Scraper.Lib.Tests.Setup;
using Scraper.Lib.ValueObjects;

namespace Scraper.Lib.Tests;

public class ScraperTests
{
    [Theory, CustomAutoData]
    public async Task Test_ImportState(
        MockFileSystem fs,
        OAuthClientId clientId,
        OAuthClientSecret clientSecret,
        OAuthResponse lastOAuthResponse)
    {
        var expectedState = new ScraperState
        {
            OAuthClientId = clientId,
            OAuthClientSecret = clientSecret,
            LastOAuthResponse = lastOAuthResponse,
        };

        await fs.AddJsonFileAsync(expectedState, Scraper.StateFileName);

        var actualState = await Scraper.ImportState(
            new NullLogger<Scraper>(),
            fs,
            default
        );

        actualState.Should().Be(expectedState);
    }

    [Theory, CustomAutoData]
    public async Task Test_ExportState(
        MockFileSystem fs,
        OAuthClientId clientId,
        OAuthClientSecret clientSecret,
        OAuthResponse lastOAuthResponse)
    {
        var expectedState = new ScraperState
        {
            OAuthClientId = clientId,
            OAuthClientSecret = clientSecret,
            LastOAuthResponse = lastOAuthResponse,
        };

        var scraper = new Scraper(
            new NullLogger<Scraper>(),
            fs,
            Mock.Of<HttpMessageHandler>(),
            Mock.Of<IScraperDelegates>(),
            expectedState
        );

        await scraper.ExportState(default);

        fs.File.Exists(Scraper.StateFileName).Should().BeTrue();

        var actualState = await Scraper.ImportState(
            new NullLogger<Scraper>(),
            fs,
            default
        );

        actualState.Should().Be(expectedState);
    }

    [Theory, CustomAutoData]
    public async Task GetOrRefreshToken_NotExpired(
        MockFileSystem fs,
        OAuthClientId clientId,
        OAuthClientSecret clientSecret,
        OAuthToken expectedToken,
        OAuthRefreshToken refreshToken)
    {
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        var startState = new ScraperState
        {
            LastOAuthResponse = new OAuthResponse(
                expectedToken,
                DateTimeOffset.Now + TimeSpan.FromDays(1),
                refreshToken,
                DateTimeOffset.Now + TimeSpan.FromDays(5)
            ),
            OAuthClientId = clientId,
            OAuthClientSecret = clientSecret,
        };

        var scraper = new Scraper(
            new NullLogger<Scraper>(),
            fs,
            httpMessageHandlerMock.Object,
            Mock.Of<IScraperDelegates>(),
            startState
        );

        var actualToken = await scraper.GetOrRefreshToken(default);
        actualToken.Should().Be(expectedToken);
    }

    [Theory, CustomAutoData]
    public async Task GetOrRefreshToken_Expired(
        MockFileSystem fs,
        OAuthClientId clientId,
        OAuthClientSecret clientSecret,
        OAuthToken firstToken,
        OAuthToken secondToken,
        OAuthRefreshToken refreshToken)
    {
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        var firstResponse = new OAuthResponse(
            firstToken,
            DateTimeOffset.Now - TimeSpan.FromDays(1),
            refreshToken,
            DateTimeOffset.Now + TimeSpan.FromDays(5)
        );

        var secondResponse = new OAuthResponse(
            secondToken,
            DateTimeOffset.Now + TimeSpan.FromDays(1),
            refreshToken,
            DateTimeOffset.Now + TimeSpan.FromDays(5)
        );

        httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, OAuthHelper.OAuthTokenUrl)
            .ReturnsJsonResponse(secondResponse);

        var startState = new ScraperState
        {
            LastOAuthResponse = firstResponse,
            OAuthClientId = clientId,
            OAuthClientSecret = clientSecret,
        };

        var scraper = new Scraper(
            new NullLogger<Scraper>(),
            fs,
            httpMessageHandlerMock.Object,
            Mock.Of<IScraperDelegates>(),
            startState
        );

        var newToken = await scraper.GetOrRefreshToken(default);
        newToken.Should().Be(secondToken);

        var expectedState = new ScraperState
        {
            LastOAuthResponse = secondResponse,
            OAuthClientId = clientId,
            OAuthClientSecret = clientSecret,
        };

        var actualState = await Scraper.ImportState(
            new NullLogger<Scraper>(),
            fs,
            default
        );

        actualState.Should().Be(expectedState);
    }

    [Theory, CustomAutoData]
    public async Task Test_ScrapNamespaces(
        MockFileSystem fs,
        ScraperState scraperState,
        IDictionary<CatalogNamespace, UrlSlug> expectedMappings)
    {
        var delegatesMock = new Mock<IScraperDelegates>(MockBehavior.Strict);

        var json = JsonSerializer.Serialize(expectedMappings);
        var sb = new StringBuilder();

        sb.Append("window.__epic_client_state = {");
        sb.Append("productInstall = {");
        sb.Append("latestValue = \n");
        sb.Append(json);
        sb.Append('}');
        sb.Append("};");

        delegatesMock
            .Setup(x => x.RenderHtmlPage(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, CancellationToken _) => sb.ToString());

        var scraper = new Scraper(
            new NullLogger<Scraper>(),
            fs,
            Mock.Of<HttpMessageHandler>(),
            delegatesMock.Object,
            scraperState
        );

        await scraper.ScrapNamespaces(default);

        var outputPath = fs.Path.Combine(scraperState.OutputFolder, Scraper.NamespacesFileName);
        fs.File.Exists(outputPath).Should().BeTrue();

        var res = await fs.ReadFromJsonAsync<IDictionary<CatalogNamespace, UrlSlug>>(outputPath);
        res.IsT0.Should().BeTrue(res.IsT1 ? res.AsT1.Value : string.Empty);

        var actualMappings = res.AsT0;
        actualMappings.Should().Equal(expectedMappings);
    }
}
