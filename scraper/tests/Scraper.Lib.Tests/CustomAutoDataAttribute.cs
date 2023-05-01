namespace Scraper.Lib.Tests;

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
