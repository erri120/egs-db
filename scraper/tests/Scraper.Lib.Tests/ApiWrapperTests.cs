using System.Globalization;
using System.Net;
using System.Web;
using Moq.Contrib.HttpClient;
using Scraper.Lib.Models;
using Scraper.Lib.Services;
using Scraper.Lib.Tests.Setup;
using Scraper.Lib.ValueObjects;

namespace Scraper.Lib.Tests;

public class ApiWrapperTests
{
    // alien isolation namespace
    private const string DummyNamespace = "df37f065c3f14eadbf011177396e2966";

    [Theory, CustomAutoData]
    public async Task Test_GetCatalogNamespaceItems(Fixture fixture, OAuthToken oAuthToken)
    {
        var url = string.Format(
            CultureInfo.InvariantCulture,
            ApiWrapper.CatalogFormatUrl,
            DummyNamespace
        );

        var expectedElements = fixture
            .CreateMany<CatalogNamespaceEnumerationResult.Element>()
            .ToList();

        var expectedResult = new CatalogNamespaceEnumerationResult(
            new CatalogNamespaceEnumerationResult.PagingDetails(
                expectedElements.Count,
                0,
                expectedElements.Count),
            expectedElements);

        var httpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        httpMessageHandler
            .SetupRequest(HttpMethod.Get, url)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResult);

        var wrapper = new ApiWrapper(httpMessageHandler.Object, new EmptyRateLimiter());

        var res = await wrapper.GetCatalogNamespaceItems(
            url,
            oAuthToken,
            0,
            expectedElements.Count,
            "US",
            "en-US",
            includeDLCDetails: true,
            includeMainGameDetails: true
        );

        res.IsT0.Should().BeTrue(res.IsT1 ? res.AsT1.Value : string.Empty);

        var actualResult = res.AsT0;
        actualResult.Paging.Should().Be(expectedResult.Paging);
        actualResult.Elements.Should().Equal(expectedElements);
    }

    [Theory]
    [InlineData(30, 10)]
    [InlineData(5, 2)]
    public async Task Test_EnumerateCatalogNamespaceAsync(int totalElements, int itemsPerPage)
    {
        var url = string.Format(
            CultureInfo.InvariantCulture,
            ApiWrapper.CatalogFormatUrl,
            DummyNamespace
        );

        var fixture = new Fixture().AddValueObjects();
        var expectedElements = fixture
            .CreateMany<CatalogNamespaceEnumerationResult.Element>(count: totalElements)
            .ToList();

        var httpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        CatalogNamespaceEnumerationResult CreateResult(int start)
        {
            var res = new CatalogNamespaceEnumerationResult(
                new CatalogNamespaceEnumerationResult.PagingDetails(
                    itemsPerPage,
                    start,
                    expectedElements!.Count),
                expectedElements.Skip(start).Take(itemsPerPage).ToArray());

            return res;
        }

        Func<HttpRequestMessage, Task<bool>> MatchRequestWithStart(int expectedStart)
        {
            return async request =>
            {
                if (request.Content is not FormUrlEncodedContent formUrlEncodedContent) return false;
                var queryString = await formUrlEncodedContent.ReadAsStringAsync().ConfigureAwait(false);
                var query = HttpUtility.ParseQueryString(queryString);

                var sStart = query["start"];
                if (!int.TryParse(sStart, CultureInfo.InvariantCulture, out var start))
                    return false;

                return start == expectedStart;
            };
        }

        for (var i = 0; i < totalElements; i += itemsPerPage)
        {
            httpMessageHandler
                .SetupRequest(HttpMethod.Get, url, MatchRequestWithStart(i))
                .ReturnsJsonResponse(CreateResult(i));
        }

        var wrapper = new ApiWrapper(httpMessageHandler.Object, new EmptyRateLimiter());

        var res = wrapper.EnumerateCatalogNamespaceAsync(
            fixture.Create<OAuthToken>(),
            CatalogNamespace.From(DummyNamespace),
            itemsPerPage: itemsPerPage
        );

        var results = await res.ToListAsync();
        results.Should().AllSatisfy(x => x.IsT0.Should().BeTrue(x.IsT1 ? x.AsT1.Value : string.Empty));

        var actualElements = results.Select(x => x.AsT0).ToList();
        actualElements.Should().Equal(expectedElements);
        actualElements.Should().HaveCount(totalElements);
    }
}
