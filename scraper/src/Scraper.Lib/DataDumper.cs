using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Scraper.Lib;

public static class DataDumper
{
    public static async ValueTask CreateOrUpdateDump<TKey, TData>(
        this IDictionary<TKey, TData> data,
        IFileInfo fileInfo,
        CancellationToken cancellationToken = default)
    {
        if (!fileInfo.Exists)
        {
            await WriteJson(data, fileInfo, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var existingData = await ReadJson<IDictionary<TKey, TData>>(fileInfo, cancellationToken).ConfigureAwait(false);

            if (existingData is null)
            {
                fileInfo.Delete();
                await WriteJson(data, fileInfo, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                foreach (var kv in data)
                {
                    existingData[kv.Key] = kv.Value;
                }

                await WriteJson(existingData, fileInfo, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async ValueTask WriteJson<TData>(
        TData data,
        IFileInfo fileInfo,
        CancellationToken cancellationToken)
    {
        var stream = fileInfo.OpenWrite();
        await using (stream.ConfigureAwait(false))
        {
            await JsonSerializer
                .SerializeAsync(stream, data, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async ValueTask<TData?> ReadJson<TData>(
        IFileInfo fileInfo,
        CancellationToken cancellationToken)
        where TData : class
    {
        var stream = fileInfo.OpenRead();
        await using (stream.ConfigureAwait(false))
        {
            var existingData = await JsonSerializer
                .DeserializeAsync<TData>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return existingData;
        }
    }
}
