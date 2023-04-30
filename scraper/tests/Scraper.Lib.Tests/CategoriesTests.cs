using System.Text.Json;
using System.Text.Json.Serialization;
using Scraper.Lib.Models;

namespace Scraper.Lib.Tests;

public class CategoriesTests
{
    public record DummyObject([property: JsonPropertyName("categories")] Categories Categories);

    [Fact]
    public void Test_Converter_ReadOriginal()
    {
        const string input = @"
{
    ""categories"": [
        {
            ""path"": ""foo""
        },
        {
            ""path"": ""bar""
        },
        {
            ""path"": ""baz""
        }
    ]
}
";

        var output = JsonSerializer.Deserialize<DummyObject>(input);
        output.Should().NotBeNull();
        output!.Categories.Parts.Should().SatisfyRespectively(
            path => path.Should().Be("foo"),
            path => path.Should().Be("bar"),
            path => path.Should().Be("baz")
        );
        output.Categories.StringRepresentation.Should().Be("foo/bar/baz");
    }

    [Fact]
    public void Test_Converter_ReadOriginal_WithExtraProperties()
    {
        const string input = @"
{
    ""categories"": [
        {
            ""something"": ""else"",
            ""path"": ""foo""
        },
        {
            ""another"": 1,
            ""path"": ""bar""
        },
        {
            ""what"": [1, 2, 3, 4],
            ""path"": ""baz"",
            ""is"": false,
            ""this"": {
                ""a"": ""b"",
                ""b"": 0,
                ""c"": [1, false, ""hi!""]
            }
        }
    ]
}
";

        var output = JsonSerializer.Deserialize<DummyObject>(input);
        output.Should().NotBeNull();
        output!.Categories.Parts.Should().SatisfyRespectively(
            path => path.Should().Be("foo"),
            path => path.Should().Be("bar"),
            path => path.Should().Be("baz")
        );
        output.Categories.StringRepresentation.Should().Be("foo/bar/baz");
    }

    [Fact]
    public void Test_Converter_ReadCompact()
    {
        const string input = @"
{
    ""categories"": ""foo/bar/baz""
}
";

        var output = JsonSerializer.Deserialize<DummyObject>(input);
        output.Should().NotBeNull();
        output!.Categories.Parts.Should().SatisfyRespectively(
            path => path.Should().Be("foo"),
            path => path.Should().Be("bar"),
            path => path.Should().Be("baz")
        );
        output.Categories.StringRepresentation.Should().Be("foo/bar/baz");
    }

    [Fact]
    public void Test_Converter_ReadCompact_Null()
    {
        const string input = @"
{
    ""categories"": null
}
";

        var output = JsonSerializer.Deserialize<DummyObject>(input);
        output.Should().NotBeNull();
        output!.Categories.Should().BeNull();
    }

    [Fact]
    public void Test_Converter_ReadCompact_Empty()
    {
        const string input = @"
{
    ""categories"": """"
}
";

        var output = JsonSerializer.Deserialize<DummyObject>(input);
        output.Should().NotBeNull();
        output!.Categories.Parts.Should().BeEmpty();
        output.Categories.StringRepresentation.Should().Be(string.Empty);
    }

    [Fact]
    public void Test_Converter_ReadCompact_Single()
    {
        const string input = @"
{
    ""categories"": ""foo""
}
";

        var output = JsonSerializer.Deserialize<DummyObject>(input);
        output.Should().NotBeNull();
        output!.Categories.Parts.Should().SatisfyRespectively(
            path => path.Should().Be("foo")
        );
        output.Categories.StringRepresentation.Should().Be("foo");
    }

    [Fact]
    public void Test_Converter_Write()
    {
        var input = new DummyObject(new Categories(new[] { "foo", "bar" }));
        var json = JsonSerializer.Serialize(input);
        var output = JsonSerializer.Deserialize<DummyObject>(json);
        output.Should().Be(input);
    }
}
