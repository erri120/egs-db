using Scraper.Lib.ValueObjects;

namespace Scraper.Lib.Tests;

public static class FixtureCustomizations
{
    public static Fixture AddValueObjects(this Fixture fixture)
    {
        fixture.GuidCustomization<AuthorizationCode>(AuthorizationCode.From);
        fixture.GuidCustomization<CatalogId>(CatalogId.From);
        fixture.GuidCustomization<CatalogNamespace>(CatalogNamespace.From);
        fixture.GuidCustomization<OAuthClientId>(OAuthClientId.From);
        fixture.GuidCustomization<OAuthClientSecret>(OAuthClientSecret.From);
        fixture.GuidCustomization<OAuthRefreshToken>(OAuthRefreshToken.From);
        fixture.GuidCustomization<OAuthToken>(OAuthToken.From);

        return fixture;
    }

    private static void GuidCustomization<T>(this IFixture fixture, Func<string, T> factory)
    {
        fixture.Customize<T>(composer =>
            composer.FromFactory<Guid>(guid => factory(guid.ToString("N"))));
    }
}
