namespace Scraper.Lib.Tests.Setup;

public class CustomAutoDataAttribute : AutoDataAttribute
{
    public CustomAutoDataAttribute() : base(() => new Fixture()
        .AddValueObjects()
        .AddFileSystem())
    { }
}
