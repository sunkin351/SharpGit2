using System.Diagnostics;
using SharpGit2.Managed.Internal;

namespace SharpGit2.Managed.Attributes;

internal class GitAttributeCache
{
    public const string AttributeConfig = "core.attributesfile";
    public const string IgnoreConfig = "core.excludesfile";

    public GitRepository Repository { get; }

    public string? ConfigAttributeFile;
    public string? ConfigExcludeFile;

    public readonly Dictionary<string, GitAttributeFile.Entry> Files = new(GitPath.PathEqualityComparer);
    public readonly Dictionary<string, GitAttributeRule> Macros = new(GitPath.PathEqualityComparer);

    internal readonly Lock _lock = new();

    public GitAttributeCache(GitRepository repo)
    {
        this.Repository = repo;
    }

    public string AttributesFile { get => throw new NotImplementedException(); }

    public string ExcludesFile { get => throw new NotImplementedException(); }

    public void InsertOrUpdate(GitAttributeFile file)
    {
        lock (_lock)
        {
            if (this.Files.TryGetValue(file.EntryInstance.Path, out var entry))
                Volatile.Write(ref entry.File[(int)file.SourceInstance.Type], file);
        }
    }

    public void Remove(GitAttributeFile file)
    {
        if (file is null)
            return;

        lock (_lock)
        {
            if (this.Files.TryGetValue(file.EntryInstance!.Path, out var entry))
            {
                ref var current = ref entry.File[(int)file.SourceInstance.Type];

                if (current == file)
                    current = null;
            }
        }
    }

    public (GitAttributeFile?, GitAttributeFile.Entry) Lookup(GitAttributeSession? session, in GitAttributeFile.Source source)
    {
        var repo = this.Repository;
        string? wd = repo.WorkingDirectory;
        ReadOnlySpan<char> fileName;

        if (source.Base is not null && !Path.IsPathRooted(source.FileName))
        {
            string p = GitPath.PosixJoin(source.Base, source.FileName);

            GitPath.ValidatePathLength(repo, p);

            if (session is not null)
                session.Tmp = p;

            fileName = p;
        }
        else
        {
            fileName = source.FileName!;
        }

        if (wd is not null && fileName.StartsWith(wd, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            fileName = fileName.Slice(wd.Length);
        }

        lock (_lock)
        {
            var spanLookup = Files.GetAlternateLookup<ReadOnlySpan<char>>();

            GitAttributeFile? file;
            if (!spanLookup.TryGetValue(fileName, out var entry))
            {
                entry = MakeEntry(repo, fileName.ToString());
                file = null;
            }
            else
            {
                file = entry.File[(int)source.Type];
            }

            return (file, entry);
        }
    }

    public GitAttributeFile? Get(
        GitAttributeSession? session,
        in GitAttributeFile.Source source,
        GitAttributeFile.Parser parser,
        bool allow_macros)
    {
        // git_attr_cache__get()
        
        var (file, entry) = Lookup(session, in source);
        GitAttributeFile? updated = null;

        if (file is null || file.OutOfDate(this.Repository, session, in source))
        {
            updated = GitAttributeFile.Load(this.Repository, session, entry, in source, parser, allow_macros);

            if (updated is not null)
            {
                this.InsertOrUpdate(updated);
                file = updated;
            }
            else if (file is not null)
            {
                this.Remove(file);
                file = null;
            }
        }

        return file;
    }

    public bool IsCached(GitAttributeFile.SourceType sourceType, string fileName)
    {
        // git_attr_cache__is_cached()

        if (!Files.TryGetValue(fileName, out var entry))
            return false;

        return entry.File[(int)sourceType] != null;
    }

    public static GitAttributeFile.Entry CreateFileEntry(GitRepository? repo, string? @base, string path)
    {
        // git_attr_cache__alloc_file_entry()
        string fullPath;
        if (@base is not null && !Path.IsPathRooted(path))
        {
            int strLen = @base.Length + path.Length;

            if (@base[^1] != '/')
                strLen += 1;

            fullPath = string.Create(strLen, (@base, path), static (span, input) =>
            {
                var (@base, path) = input;

                @base.CopyTo(span);

                int offset = @base.Length;
                if (@base[^1] != '/')
                {
                    span[offset++] = '/';
                }

                Debug.Assert(span.Length - offset == path.Length);

                path.CopyTo(span[offset..]);
            });
        }
        else
        {
            fullPath = path;
        }

        GitPath.ValidatePathLength(repo, fullPath);

        return new GitAttributeFile.Entry() { FullPath = fullPath, Path = path };
    }

    public void InsertMacro(in GitAttributeRule macro)
    {
        // git_attr_cache__insert_macro()

        if (macro.Assigns.Count == 0)
        {
            return;
        }

        lock (_lock)
        {
            this.Macros[macro.Match.Pattern] = macro;
        }
    }

    public GitAttributeRule? LookupMacro(string name)
    {
        // git_attr_cache__lookup_macro()

        lock (_lock) // original was not synchronized like this, but unguarded
        {
            if (Macros.TryGetValue(name, out var rule))
                return rule;
        }

        return null;
    }

    public static void Initialize(GitRepository repo)
    {
        _ = repo.AttributeCache; // trigger lazy initialization
    }

    private GitAttributeFile.Entry MakeEntry(GitRepository repo, string path)
    {
        var entry = CreateFileEntry(repo, repo.WorkingDirectory, path);

        Files.Add(entry.Path, entry);

        return entry;
    }
}
