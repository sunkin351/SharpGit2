using BenchmarkDotNet.Attributes;

namespace SharpGit2.Managed.Benchmarks;

[SimpleJob]
public class HashLineBenchmarks
{
    private string _data = DiffAlgorithmBenchmarks.file1;
    private ValueList<XDiff.XRecord> _records = new(48);

    [Benchmark]
    public void NoOptions()
    {
        XDiff.XDL.HashLines<char>(_data, ref _records, default);
        _records.Clear();
    }

    [Benchmark]
    public void IgnoreCRAtEOL()
    {
        XDiff.XDL.HashLines<char>(_data, ref _records, XDiffFlags.IgnoreCRAtEOL);
        _records.Clear();
    }

    [Benchmark]
    public void IgnoreWhitespace()
    {
        XDiff.XDL.HashLines<char>(_data, ref _records, XDiffFlags.IgnoreWhitespace);
        _records.Clear();
    }

    [Benchmark]
    public void IgnoreWhitespaceChange()
    {
        XDiff.XDL.HashLines<char>(_data, ref _records, XDiffFlags.IgnoreWhitespaceChange);
        _records.Clear();
    }

    [Benchmark]
    public void IgnoreWhitespaceAtEOL()
    {
        XDiff.XDL.HashLines<char>(_data, ref _records, XDiffFlags.IgnoreWhitespaceAtEOL);
        _records.Clear();
    }
}
