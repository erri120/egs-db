using System.Text;
using System.Text.Json;
using Scraper.Lib.Services;
using Scraper.Lib.Tests.Setup;
using Scraper.Lib.ValueObjects;

namespace Scraper.Lib.Tests;

public class NamespaceScraperTests
{
    [Theory, CustomAutoData]
    public void Test_GetNamespacesFromHtmlText(IDictionary<CatalogNamespace, UrlSlug> expectedMappings)
    {
        var json = JsonSerializer.Serialize(expectedMappings);
        var sb = new StringBuilder();

        sb.Append("window.__epic_client_state = {");
        sb.Append("productInstall = {");
        sb.Append("latestValue = \n");
        sb.Append(json);
        sb.Append('}');
        sb.Append("};");

        var input = sb.ToString();

        var res = NamespaceScraper.GetNamespacesFromHtmlText(input);
        res.IsT0.Should().BeTrue(res.IsT1 ? res.AsT1.Value : string.Empty);

        var mappings = res.AsT0;
        mappings.Should().Equal(expectedMappings);
    }
}

