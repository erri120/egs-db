using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Scraper.Lib.ValueObjects;

namespace Scraper.Lib.Tests.Setup;

public static class FixtureCustomizations
{
    public static Fixture AddValueObjects(this Fixture fixture)
    {
        fixture.GuidCustomization(AuthorizationCode.From);
        fixture.GuidCustomization(CatalogId.From);
        fixture.GuidCustomization(CatalogNamespace.From);
        fixture.GuidCustomization(OAuthClientId.From);
        fixture.GuidCustomization(OAuthClientSecret.From);
        fixture.GuidCustomization(OAuthRefreshToken.From);
        fixture.GuidCustomization(OAuthToken.From);
        fixture.GuidCustomization(UrlSlug.From);

        return fixture;
    }

    public static Fixture AddFileSystem(this Fixture fixture)
    {
        fixture.Customize<MockFileSystem>(composer =>
            composer.FromFactory(() => new MockFileSystem()));
        fixture.Customize<IFileSystem>(composer =>
            composer.FromFactory(() => new MockFileSystem()));
        return fixture;
    }

    private static void GuidCustomization<T>(this IFixture fixture, Func<string, T> factory)
    {
        fixture.Customize<T>(composer =>
            composer.FromFactory<Guid>(guid => factory(guid.ToString("N"))));
    }
}
