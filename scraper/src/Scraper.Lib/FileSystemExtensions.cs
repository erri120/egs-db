using System;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OneOf;
using Scraper.Lib.ValueObjects;

namespace Scraper.Lib;

[PublicAPI]
public static class FileSystemExtensions
{
    public static async ValueTask<OneOf<TData, GenericError>> ReadFromJsonAsync<TData>(
        this IFileSystem fileSystem,
        string path,
        JsonSerializerOptions? options = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stream = fileSystem.File.Open(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );

            await using (stream.ConfigureAwait(false))
            {
                var res = await JsonSerializer
                    .DeserializeAsync<TData>(stream, options, cancellationToken)
                    .ConfigureAwait(false);

                if (res is not null) return res;

                logger?.LogError("Unable to deserialize file \"{Path}\", result is null", path);
                return GenericError.From($"Unable to deserialize file \"{path}\", result is null!");

            }
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Unable to deserialize file \"{Path}\"", path);
            return GenericError.From($"Unable to deserialize file \"{path}\":\n{e}");
        }
    }

    public static async ValueTask<GenericError?> WriteToJsonAsync<TData>(
        this IFileSystem fileSystem,
        TData data,
        string path,
        JsonSerializerOptions? options = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stream = fileSystem.File.Open(
                path,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read
            );

            await using (stream.ConfigureAwait(false))
            {
                await JsonSerializer
                    .SerializeAsync(stream, data, options, cancellationToken)
                    .ConfigureAwait(false);

                return null;
            }
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Unable to serialize data to file \"{Path}\"", path);
            return GenericError.From($"Unable to serialize data to file \"{path}\":\n{e}");
        }
    }
}
