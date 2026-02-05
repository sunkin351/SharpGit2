using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace SharpGit2.Managed.Benchmarks;

[SimpleJob, MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
//[EventPipeProfiler(BenchmarkDotNet.Diagnosers.EventPipeProfile.Jit)]
public class DiffAlgorithmBenchmarks
{
    internal static readonly XDiff.EmitConfiguration<char> _config = new() { ContextLength = 11, Flags = default, InterHunkContextLength = 2 };
    private Emitter _emitter = new Emitter(TextWriter.Null);

    //private byte[] _file1utf8 = Encoding.UTF8.GetBytes(file1);
    //private byte[] _file2utf8 = Encoding.UTF8.GetBytes(file2);

    //[Benchmark, BenchmarkCategory("meyers")]
    //public unsafe void MeyersAlgorithm_Native()
    //{
    //    var options = new Native.GitDiffOptions() { Flags = GitDiffOptionFlags.Normal, ContextLines = 11, InterhunkLines = 2 };

    //    GitNativeApi.git_diff_buffers(_file1utf8, null, _file2utf8, null, &options, null, null, null, &_onLineStub, 0);
    //}

    [Benchmark, BenchmarkCategory("meyers")]
    public void MeyersAlgorithm()
    {
        XDiff.RunDiff(file1, file2, XDiffFlags.None, _config, _emitter);
    }

    //[Benchmark, BenchmarkCategory("patience")]
    //public unsafe void PatienceAlgorithm_Native()
    //{
    //    var options = new Native.GitDiffOptions() { Flags = GitDiffOptionFlags.Patience, ContextLines = 11, InterhunkLines = 2 };

    //    GitNativeApi.git_diff_buffers(_file1utf8, null, _file2utf8, null, &options, null, null, null, &_onLineStub, 0);
    //}

    [Benchmark, BenchmarkCategory("patience")]
    public void PatienceAlgorithm()
    {
        XDiff.RunDiff(file1, file2, XDiffFlags.PatienceDiff, _config, _emitter);
    }

    [Benchmark, BenchmarkCategory("histogram")]
    public void HistogramAlgorithm()
    {
        XDiff.RunDiff(file1, file2, XDiffFlags.HistogramDiff, _config, _emitter);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static unsafe int _onLineStub(void* delta, void* hunk, void* line, nint payload)
    {
        return 0;
    }

    internal sealed class Emitter : XDiff.IEmitCallbacks<char>
    {
        private readonly TextWriter _writer;

        public Emitter(TextWriter writer)
        {
            _writer = writer;
        }

        public void EmitLine(XDiff.EmitLineType lineType, ReadOnlySpan<char> line)
        {
            switch (lineType)
            {
                case XDiff.EmitLineType.Added:
                    _writer.Write("+ ");
                    _writer.Write(line);
                    break;
                case XDiff.EmitLineType.Removed:
                    _writer.Write("- ");
                    _writer.Write(line);
                    break;
                case XDiff.EmitLineType.Context:
                    _writer.Write("  ");
                    _writer.Write(line);
                    break;
                case XDiff.EmitLineType.HunkDescription:
                    _writer.Write(line);
                    break;
            }
        }
    }


    internal const string file1 = """
            <Project Sdk="Microsoft.NET.Sdk">

                <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net8.0</TargetFramework>
                    <RootNamespace>Server</RootNamespace>
                    <AssemblyName>Server</AssemblyName>

                </PropertyGroup>

                <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
                    <DefineConstants>DEBUG;TRACE</DefineConstants>
                    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                </PropertyGroup>

                <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|AnyCPU'">
                    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="CommunityToolkit.HighPerformance" Version="8.2.2" />
                    <PackageReference Include="Dapper" Version="2.1.35" />
                    <PackageReference Include="EmailValidation" Version="1.2.0" />
                    <PackageReference Include="FluentMigrator" Version="5.2.0" />
                    <PackageReference Include="FluentMigrator.Runner.MySql" Version="5.2.0" />
                    <PackageReference Include="log4net" Version="2.0.17" />
                    <PackageReference Include="MailKit" Version="4.7.1" />
                    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.9.2" />
                    <PackageReference Include="Microsoft.IO.RecyclableMemoryStream" Version="3.0.1" />
                    <PackageReference Include="MimeKit" Version="4.7.1" />
                    <PackageReference Include="MySqlConnector" Version="2.3.7" />
                    <PackageReference Include="System.IO.Pipelines" Version="8.0.0" />
                    <PackageReference Include="System.Reactive" Version="6.0.1" />
                    <PackageReference Include="System.Threading.Tasks.Extensions" Version="4.5.4" />
                </ItemGroup>

                <ItemGroup>
                    <ProjectReference Include="..\Framework\Core\Core\PMU.Core.csproj" />
                    <ProjectReference Include="..\Framework\Sockets\Sockets\Sockets.csproj" />
                </ItemGroup>

            </Project>
            """;

    internal const string file2 = """
            <Project Sdk="Microsoft.NET.Sdk">

                <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net9.0</TargetFramework>
                    <RootNamespace>Server</RootNamespace>
                    <AssemblyName>Server</AssemblyName>

                </PropertyGroup>

                <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
                    <DefineConstants>DEBUG;TRACE</DefineConstants>
                    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                </PropertyGroup>

                <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|AnyCPU'">
                    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                </PropertyGroup>

                <ItemGroup>
                    <PackageReference Include="CommunityToolkit.HighPerformance" Version="8.3.2" />
                    <PackageReference Include="Dapper" Version="2.1.35" />
                    <PackageReference Include="EmailValidation" Version="1.2.0" />
                    <PackageReference Include="FluentMigrator" Version="5.2.0" />
                    <PackageReference Include="FluentMigrator.Runner.MySql" Version="5.2.0" />
                    <PackageReference Include="LibGit2Sharp.NativeBinaries" Version="2.0.322" />
                    <PackageReference Include="log4net" Version="3.0.3" />
                    <PackageReference Include="MailKit" Version="4.8.0" />
                    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.11.0" />
                    <PackageReference Include="Microsoft.IO.RecyclableMemoryStream" Version="3.0.1" />
                    <PackageReference Include="MimeKit" Version="4.8.0" />
                    <PackageReference Include="MySqlConnector" Version="2.4.0" />
                    <PackageReference Include="System.IO.Pipelines" Version="9.0.0" />
                    <PackageReference Include="System.Reactive" Version="6.0.1" />
                    <PackageReference Include="System.Threading.Tasks.Extensions" Version="4.6.0" />
            		<PackageReference Include="SharpGit2" Version="0.5.1" />
                </ItemGroup>

                <ItemGroup>
                    <ProjectReference Include="..\Framework\Core\Core\PMU.Core.csproj" />
                    <ProjectReference Include="..\Framework\Sockets\Sockets\Sockets.csproj" />
                </ItemGroup>

            </Project>
            """;
}
