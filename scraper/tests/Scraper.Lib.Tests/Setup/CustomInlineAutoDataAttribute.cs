namespace Scraper.Lib.Tests.Setup;

public class CustomInlineAutoDataAttribute : InlineAutoDataAttribute
{
    public CustomInlineAutoDataAttribute(params object[] values) : base(new CustomAutoDataAttribute(), values) { }
}
