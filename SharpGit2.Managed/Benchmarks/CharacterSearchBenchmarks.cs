using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace SharpGit2.Managed.Benchmarks;

[SimpleJob]
public class CharacterSearchBenchmarks
{
    private static readonly SearchValues<char> _allInOne;
    private static readonly SearchValues<char> _outOfRangeChars = SearchValues.Create("~^:\\?[");

    static CharacterSearchBenchmarks()
    {
        List<char> allValues = new(33);

        for (int i = 0; i <= 32; ++i)
        {
            allValues.Add((char)i);
        }
        
        allValues.AddRange("~^:\\?[");

        _allInOne = SearchValues.Create([..allValues]);
    }

    private string _data;

    [GlobalSetup]
    public void Setup()
    {
        _data = new string('a', 256);
    }

    [Benchmark]
    public bool AllInOne()
    {
        return _data.ContainsAny(_allInOne);
    }

    [Benchmark]
    public bool Separate()
    {
        return _data.ContainsAnyInRange('\0', ' ') || _data.ContainsAny(_outOfRangeChars);
    }
}