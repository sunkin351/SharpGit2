using System;
using System.Security.Cryptography;
using System.Text.Unicode;
using BenchmarkDotNet.Attributes;

using SharpGit2.Managed;

namespace SharpGit2Benchmarks;

[SimpleJob]
public class GitObjectIDBenchmarks
{
    private GitObjectID _id = default;
    private char[] _hexString = new char[SHA1.HashSizeInBytes * 2];
    private byte[] _hexByteString = new byte[SHA1.HashSizeInBytes * 2 + 1];

    [GlobalSetup]
    public void Setup()
    {
        Random.Shared.GetHexString(_hexString, true);
        Utf8.FromUtf16(_hexString, _hexByteString, out _, out _);
    }

    [Benchmark, BenchmarkCategory("Misc")]
    public bool IsZero()
    {
        return _id.IsZero;
    }

    [Benchmark, BenchmarkCategory("Parsing")]
    public GitObjectID Parse()
    {
        return GitObjectID.Parse(_hexString);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Parsing")]
    public unsafe SharpGit2.GitObjectID ParseNative()
    {
        SharpGit2.GitObjectID result = default;
        fixed (byte* str = _hexByteString)
        {
            SharpGit2.Git2.ThrowIfError(SharpGit2.GitNativeApi.git_oid_fromstrp(&result, str));
        }

        return result;
    }
}