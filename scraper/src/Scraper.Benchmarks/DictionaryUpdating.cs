using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Scraper.Benchmarks;

[MemoryDiagnoser]
public class DictionaryUpdating
{
    private IDictionary<string, string> _newValues = new Dictionary<string, string>(StringComparer.Ordinal);
    private IDictionary<string, string> _existingValues = new Dictionary<string, string>(StringComparer.Ordinal);

    [Params(100, 1_000)]
    public int NumItems { get; set; }

    [Params(0.25, 0.5, 1.0)]
    public double SharedItemsRatio { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _newValues = Enumerable
            .Range(0, NumItems)
            .Select(_ => (key: Guid.NewGuid().ToString("N"), value: Guid.NewGuid().ToString("N")))
            .ToDictionary(x => x.key, x => x.value, StringComparer.Ordinal);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _existingValues = _newValues
            .Take((int)(NumItems * SharedItemsRatio))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
    }

    [Benchmark(Baseline = true)]
    public IDictionary<string, string> UpdateExistingInPlace()
    {
        foreach (var kv in _newValues)
        {
            _existingValues[kv.Key] = kv.Value;
        }

        return _existingValues;
    }

    [Benchmark]
    public IDictionary<string, string> CreateNewDictionary_WithMaxCapacity()
    {
        var result = new Dictionary<string, string>(_newValues.Count + _existingValues.Count, StringComparer.Ordinal);

        foreach (var kv in _existingValues)
        {
            result[kv.Key] = kv.Value;
        }

        foreach (var kv in _newValues)
        {
            result[kv.Key] = kv.Value;
        }

        return result;
    }

    [Benchmark]
    public IDictionary<string, string> CreateNewDictionary_WithMinCapacity()
    {
        var capacity = GetMinCapacity(_newValues, _existingValues);
        var result = new Dictionary<string, string>(capacity, StringComparer.Ordinal);

        foreach (var kv in _existingValues)
        {
            result[kv.Key] = kv.Value;
        }

        foreach (var kv in _newValues)
        {
            result[kv.Key] = kv.Value;
        }

        return result;
    }

    private static int GetMinCapacity(IDictionary<string, string> left, IDictionary<string, string> right)
    {
        var count = left.Count;
        foreach (var kv in right)
        {
            if (!left.ContainsKey(kv.Key)) count++;
        }

        return count;
    }
}
