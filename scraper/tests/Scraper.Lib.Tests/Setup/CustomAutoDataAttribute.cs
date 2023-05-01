namespace Scraper.Lib.Tests.Setup;

public class CustomAutoDataAttribute : AutoDataAttribute
{
    public CustomAutoDataAttribute() : base(() =>
    {
        var fixture = new Fixture();
        fixture.AddValueObjects();
        return fixture;
    })
    { }
}
