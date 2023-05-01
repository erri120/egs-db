using System;
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
    /// <seealso cref="ToListSafeAsync{T}"/>
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

    /// <summary>
    /// Asynchronously enumerates the entire <see cref="IAsyncEnumerable{T}"/>
    /// and puts the elements into a <see cref="IList{T}"/>. This function
    /// safely enumerates and returns the list when an exception occurs, to not
    /// loose any data.
    /// </summary>
    /// <param name="asyncEnumerable"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <seealso cref="ToListAsync{T}"/>
    public static async Task<IList<T>> ToListSafeAsync<T>(
        [InstantHandle(RequireAwait = true)] this IAsyncEnumerable<T> asyncEnumerable,
        CancellationToken cancellationToken = default)
    {
        var list = new List<T>();

        try
        {
            await foreach (var item in asyncEnumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                list.Add(item);
            }
        }
        catch (Exception)
        {
            return list;
        }

        return list;
    }
}
