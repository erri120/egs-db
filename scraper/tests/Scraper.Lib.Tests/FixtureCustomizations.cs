using Scraper.Lib.ValueObjects;

namespace Scraper.Lib.Tests;

public static class FixtureCustomizations
{
    public static Fixture AddValueObjects(this Fixture fixture)
    {
        fixture.Customize<ApiToken>(composer =>
            composer.FromFactory<Guid>(guid => ApiToken.From(guid.ToString("N"))));

        fixture.Customize<CatalogId>(composer =>
            composer.FromFactory<Guid>(guid => CatalogId.From(guid.ToString("N"))));

        fixture.Customize<CatalogNamespace>(composer =>
            composer.FromFactory<Guid>(guid => CatalogNamespace.From(guid.ToString("N"))));

        return fixture;
    }
}
