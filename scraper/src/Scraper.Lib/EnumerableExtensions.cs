using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Scraper.Lib;

public static class EnumerableExtensions
{
    public static Task AsParallelAsync<T>(this IEnumerable<T> enumerable, Func<T, CancellationToken, Task> body, CancellationToken cancellationToken)
    {
        async Task AwaitPartition(IEnumerator<T> partition)
        {
            using (partition)
            {
                while (partition.MoveNext())
                {
                    await body(partition.Current, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        var tasks = Partitioner
            .Create(enumerable)
            .GetPartitions(Environment.ProcessorCount)
            .AsParallel()
            .Select(AwaitPartition);

        return Task.WhenAll(tasks);
    }
}
