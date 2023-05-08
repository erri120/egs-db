using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Scraper.Benchmarks;

[SimpleJob]
public class SomethingAboutSpeed
{
    // private static readonly Consumer Consumer = new();

    private const string OutputDirectory = "output";

    private readonly ParallelOptions _parallelOptions = new()
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount,
    };

    private Guid[] _data = Array.Empty<Guid>();

    [Params(2048)]
    public int N { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _data = Enumerable
            .Range(0, N)
            .Select(_ => Guid.NewGuid())
            .ToArray();

        Directory.CreateDirectory(OutputDirectory);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        Directory.Delete(OutputDirectory, recursive: true);
    }

    [Benchmark(Description = "AsParallel")]
    public void UsingAsParallel()
    {
        _data
            .AsParallel()
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
            .WithMergeOptions(ParallelMergeOptions.AutoBuffered)
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .ForAll(guid => DoSomethingAsync(guid, default).GetAwaiter().GetResult());
    }

    [Benchmark(Description = "Parallel.For")]
    public void UsingParallelFor()
    {
        Parallel
            .For(0, _data.Length, _parallelOptions, i =>
            {
                DoSomethingAsync(_data[i], default).GetAwaiter().GetResult();
            });
    }

    [Benchmark(Description = "Parallel.ForEach")]
    public void UsingParallelForEach()
    {
        Parallel.ForEach(_data, _parallelOptions, guid => DoSomethingAsync(guid, default).GetAwaiter().GetResult());
    }

    [Benchmark(Description = "Parallel.ForEach with Partitioner")]
    public void UsingParallelForEach_WithPartitioner_WithLoadBalance()
    {
        var partitioner = Partitioner.Create(_data, loadBalance: true);

        Parallel.ForEach(partitioner, _parallelOptions, guid => DoSomethingAsync(guid, default).GetAwaiter().GetResult());
    }

    [Benchmark(Description = "Parallel.ForEachAsync")]
    public async Task UsingParallelForEachAsync()
    {
        await Parallel
            .ForEachAsync(_data, _parallelOptions, DoSomethingAsync)
            .ConfigureAwait(false);
    }

    [Benchmark(Description = "Custom Parallel.ForEachAsync")]
    public async Task UsingCustomParallelForEachAsync()
    {
        await CustomParallelForEachAsync(_data, DoSomethingAsync, default).ConfigureAwait(false);
    }

    private static Task CustomParallelForEachAsync<T>(
        IEnumerable<T> source,
        Func<T, CancellationToken, ValueTask> body,
        CancellationToken cancellationToken)
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

        return Task.WhenAll(
            Partitioner
                .Create(source)
                .GetPartitions(Environment.ProcessorCount)
                .AsParallel()
                .Select(AwaitPartition)
        );
    }

    private static async ValueTask DoSomethingAsync(Guid input, CancellationToken cancellationToken)
    {
        var path = Path.Combine(OutputDirectory, $"{input.ToString("N", CultureInfo.InvariantCulture)}.txt");

        var stream = File.Open(
            path,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None
        );

        await using (stream.ConfigureAwait(false))
        {
            var bytes = input.ToByteArray();
            await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        }
    }
}
