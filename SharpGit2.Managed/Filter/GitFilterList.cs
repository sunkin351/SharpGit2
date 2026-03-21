using System.Buffers;
using System.Collections.Immutable;
using CommunityToolkit.HighPerformance;
using JetBrains.Annotations;
using SharpGit2.Managed.Attributes;
using SharpGit2.Managed.Internal;

namespace SharpGit2.Managed.Filter;

public enum GitFilterMode
{
    ToWorktree = 0,
    Smudge = ToWorktree,
    ToODB = 1,
    Clean = ToODB
}

[Flags]
public enum GitFilterFlags
{
    Default = 0,
    AllowUnsafe = 1,
    NoSystemAttributes = 1 << 1,
    AttributesFromHead = 1 << 2,
    AttributesFromCommit = 1 << 3,
}

public sealed class GitFilterSource
{
    internal GitRepository Repository;
    internal string? Path;
    /// <summary>
    /// Zero if unknown (which is likely).
    /// </summary>
    internal GitObjectID Oid;
    /// <summary>
    /// Zero if unknown
    /// </summary>
    internal UnixFileMode FileMode;

    internal GitFilterMode Mode;
    internal GitFilterOptions Options;
}

[PublicAPI]
public sealed class GitFilterList
{
    public static GitFilterList? Load(
        GitRepository repo,
        GitBlob? blob,
        string path,
        GitFilterMode mode,
        in GitFilterOptions options)
    {
        var session = new GitFilterSession()
        {
            Options = options,
        };

        return GitFilterRegistry.LoadFilterList(repo, blob, path, mode, session);
    }
    
    public static GitFilterList? Load(
        GitRepository repo,
        GitBlob? blob,
        string path,
        GitFilterMode mode,
        GitFilterFlags flags)
    {
        var session = new GitFilterSession();
        session.Options.Flags = flags;

        return GitFilterRegistry.LoadFilterList(repo, blob, path, mode, session);
    }

    internal static GitFilterList? Load(
        GitRepository repo,
        GitBlob? blob,
        string path,
        GitFilterMode mode,
        GitFilterSession session)
    {
        return GitFilterRegistry.LoadFilterList(repo, blob, path, mode, session);
    }
    
    private ValueList<GitFilterEntry> Filters = new();
    private GitFilterSource Source;
    private string? Path;

    internal GitFilterList(GitRepository repo, GitFilterMode mode, GitFilterFlags flags)
        : this(new GitFilterSource()
        {
            Repository = repo,
            Mode = mode,
            Options = new GitFilterOptions() { Flags = flags }
        })
    {
    }

    internal GitFilterList(GitFilterSource source)
    {
        Source = source;
        Path = source.Path;
    }

    public bool Contains(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        for (int i = 0; i < this.Filters.Count; ++i)
        {
            if (this.Filters[i].FilterName == name)
                return true;
        }

        return false;
    }

    public void ApplyToBuffer(ReadOnlySpan<byte> input, IBufferWriter<byte> output)
    {
        using var destination = this.InitStream(output.AsStream());
        
        destination.Write(input);
    }

    public void ApplyToFile(GitRepository? repo, string path, IBufferWriter<byte> output)
    {
        using var destination = this.InitStream(output.AsStream());

        string absPath = PathUtilities.JoinUnrooted(path, repo?.WorkingDirectory, out _);
        
        GitPath.ValidatePathLength(repo, absPath);
        
        using var source = new FileStream(absPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        
        source.CopyTo(destination);
    }

    public void ApplyToBlob(GitBlob blob, IBufferWriter<byte> output)
    {
        using var destination = this.InitStream(output.AsStream());

        using var sourceStream = blob.GetContentStream();
        
        sourceStream.CopyTo(destination);
    }

    public void StreamFromBuffer(ReadOnlySpan<byte> input, Stream target)
    {
        using var destination = this.InitStream(target);
        
        destination.Write(input);
    }
    
    public void StreamFromFile(GitRepository? repo, string file, Stream target)
    {
        using var destination = this.InitStream(target);

        string absPath = PathUtilities.JoinUnrooted(file, repo?.WorkingDirectory, out _);
        
        GitPath.ValidatePathLength(repo, absPath);
        
        using var source = new FileStream(absPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        
        source.CopyTo(destination);
    }
    
    public void StreamFromBlob(GitBlob blob, Stream target)
    {
        using var destination = this.InitStream(target);

        using var sourceStream = blob.GetContentStream();
        
        sourceStream.CopyTo(destination);
    }

    private struct GitFilterEntry
    {
        public string FilterName;
        public IGitFilter Filter;
        public object? Context;
    }

    internal void Add(string name, IGitFilter filter, object? context)
    {
        Filters.Add(new GitFilterEntry() { FilterName = name, Filter = filter, Context = context });
    }

    private Stream InitStream(Stream destination)
    {
        if (!destination.CanWrite)
            throw new ArgumentException("Destination stream given to GitFilterList was not writable!");
        
        Stream resultStream = destination;

        if (this.Filters.Count > 0)
        {
            try
            {
                var source = this.Source;
                if (source.Mode == GitFilterMode.ToWorktree)
                {
                    for (int i = this.Filters.Count - 1; i >= 0; --i)
                    {
                        ref var entry = ref this.Filters[i];
                        resultStream = entry.Filter.WriteStream(source, resultStream, ref entry.Context);
                    }
                }
                else
                {
                    int count = this.Filters.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        ref var entry = ref this.Filters[i];
                        resultStream = entry.Filter.WriteStream(source, resultStream, ref entry.Context);
                    }
                }
            }
            catch
            {
                resultStream.Dispose();
                throw;
            }
        }

        return resultStream;
    }
}