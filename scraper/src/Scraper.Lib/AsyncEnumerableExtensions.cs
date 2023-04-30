using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Scraper.Lib;

/// <summary>
/// Extension methods for <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
[PublicAPI]
public static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Asynchronously enumerates the entire <see cref="IAsyncEnumerable{T}"/>
    /// and puts the elements into a <see cref="IList{T}"/>.
    /// </summary>
    /// <param name="asyncEnumerable"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<IList<T>> ToListAsync<T>(
        [InstantHandle(RequireAwait = true)] this IAsyncEnumerable<T> asyncEnumerable,
        CancellationToken cancellationToken = default)
    {
        var list = new List<T>();

        await foreach (var item in asyncEnumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            list.Add(item);
        }

        return list;
    }
}
