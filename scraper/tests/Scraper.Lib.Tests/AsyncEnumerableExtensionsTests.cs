using Scraper.Lib.Tests.Setup;

namespace Scraper.Lib.Tests;

public class AsyncEnumerableExtensionsTests
{
    [Theory, CustomAutoData]
    public async Task Test_ToListAsync(IList<int> input)
    {
        var output = await DummyAsyncEnumerable(input).ToListAsync();
        output.Should().Equal(input);
    }

    [Theory, CustomAutoData]
    public async Task Test_ToListAsync_WithException(IList<int> input)
    {
        IList<int>? output = null;

        var act = async () => output = await DummyAsyncEnumerable(input, throwAfterFirst: true)
            .ToListAsync()
            .ConfigureAwait(false);

        await act.Should().ThrowAsync<Exception>();
        output.Should().BeNull();
    }

    [Theory, CustomAutoData]
    public async Task Test_ToListSafeAsync(IList<int> input)
    {
        var output = await DummyAsyncEnumerable(input, throwAfterFirst: true).ToListSafeAsync();
        output.Should().ContainSingle(i => i == input.First());
    }

    [Theory, CustomAutoData]
    public async Task Test_AddToListAsync(IList<int> input)
    {
        var output = new List<int>();
        await DummyAsyncEnumerable(input).AddToListAsync(output);
        output.Should().Equal(input);
    }

    [Theory, CustomAutoData]
    public async Task Test_AddToListSafeAsync(IList<int> input)
    {
        var output = new List<int>();
        await DummyAsyncEnumerable(input, throwAfterFirst: true).AddToListSafeAsync(output);
        output.Should().ContainSingle(i => i == input.First());
    }

    private static async IAsyncEnumerable<int> DummyAsyncEnumerable(IEnumerable<int> input, bool throwAfterFirst = false)
    {
        foreach (var i in input)
        {
            yield return i;
            if (throwAfterFirst) throw new Exception();
        }
    }
}
