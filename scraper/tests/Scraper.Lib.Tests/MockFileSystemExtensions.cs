using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;

namespace Scraper.Lib.Tests;

public static class MockFileSystemExtensions
{
    public static async ValueTask AddJsonFileAsync<TData>(
        this MockFileSystem fileSystem,
        TData data,
        string path)
    {
        var stream = fileSystem.File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        await using (stream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(stream, data).ConfigureAwait(false);
        }
    }
}
