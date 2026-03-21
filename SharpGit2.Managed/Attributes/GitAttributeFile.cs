using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SharpGit2.Managed.Attributes;

[Flags]
public enum GitAttributeCheckFlags : uint
{
    /// <summary>
    /// Check attribute flags: Reading values from index and working directory.
    /// <br/><br/>
    /// When checking attributes, it is possible to check attribute files
    /// in both the working directory (if there is one) and the index (if
    /// there is one).  You can explicitly choose where to check and in
    /// which order using the following flags.
    /// <br/><br/>
    /// Core git usually checks the working directory then the index,
    /// except during a checkout when it checks the index first.  It will
    /// use index only for creating archives or for a bare repo (if an
    /// index has been specified for the bare repo).
    /// </summary>
    FileThenIndex = 0,
    ///<inheritdoc cref="FileThenIndex"/>
    IndexThenFile = 1,
    ///<inheritdoc cref="FileThenIndex"/>
    IndexOnly = 1 << 1,

    /// <summary>
    /// Check attribute flags: controlling extended attribute behavior.
    /// 
    /// Normally, attribute checks include looking in the /etc (or system
    /// equivalent) directory for a `gitattributes` file.  Passing this
    /// flag will cause attribute checks to ignore that file.
    /// equivalent) directory for a `gitattributes` file.  Passing the
    /// `GIT_ATTR_CHECK_NO_SYSTEM` flag will cause attribute checks to
    /// ignore that file.
    /// 
    /// Passing the `GIT_ATTR_CHECK_INCLUDE_HEAD` flag will use attributes
    /// from a `.gitattributes` file in the repository at the HEAD revision.
    /// 
    /// Passing the `GIT_ATTR_CHECK_INCLUDE_COMMIT` flag will use attributes
    /// from a `.gitattributes` file in a specific commit.
    /// </summary>
    NoSystem = 1 << 2,
    ///<inheritdoc cref="NoSystem"/>
    IncludeHead = 1 << 3,
    ///<inheritdoc cref="NoSystem"/>
    IncludeCommit = 1 << 4,
}

internal sealed class GitAttributeFile
{
    public readonly Lock Lock = new();
    public Entry? EntryInstance;
    public Source SourceInstance;
    public ValueList<GitAttributeRule> Rules = new();
    public bool NonExistant;
    public int SessionKey;

    // these two fields are part of a union in the original native implementation
    public GitObjectID cache_data_oid;
    public DateTime cache_data_stamp;

    public GitAttributeFile(Entry? entry, in Source source)
    {
        EntryInstance = entry;
        SourceInstance = source;
    }

    public void ClearRules(bool need_lock)
    {
        // git_attr_file__clear_rules()

        if (need_lock)
        {
            lock (this.Lock)
            {
                Rules.Clear();
            }
        }
        else
        {
            Rules.Clear();
        }
    }

    public bool OutOfDate(GitRepository repo, GitAttributeSession? attributeSession, in Source source)
    {
        // git_attr_file__out_of_date()

        if (attributeSession?.Key == this.SessionKey)
        {
            return false;
        }
        else if (this.NonExistant)
        {
            return true;
        }

        switch (this.SourceInstance.Type)
        {
            case SourceType.Memory:
                return false;

            case SourceType.File:
                Debug.Assert(this.cache_data_stamp.Kind == DateTimeKind.Utc);
                return this.cache_data_stamp != File.GetLastWriteTimeUtc(this.EntryInstance!.FullPath);

            case SourceType.Index:
            {
                var id = AttributeFileOidFromIndex(repo, this.EntryInstance!.Path);

                return !GitObjectID.Equals(in this.cache_data_oid, in id);
            }    

            case SourceType.Head:
                return !GitObjectID.Equals(in this.cache_data_oid, in repo.GetHeadTree().ObjectID);

            case SourceType.Commit:
                return !GitObjectID.Equals(in this.cache_data_oid, in repo.LookupCommit(in source.CommitId).GetTree().ObjectID);

            default:
                throw new InvalidOperationException($"Invalid file source {this.SourceInstance.Type}");
        }
    }

    public bool TryLookupOne(in GitAttributePath path, string attribute, out GitAttributeValue value)
    {
        for (int i = this.Rules.Count - 1; i >= 0; --i)
        {
            ref var rule = ref this.Rules[i];

            if (rule.IsMatch(in path) && rule.Assigns.TryGetValue(attribute, out var str_value))
            {
                value = CreateValueFromString(str_value);
                return true;
            }
        }

        value = default;
        return false;
    }

    public void LookupMany(in GitAttributePath path, ReadOnlySpan<string> attributes, Span<GitAttributeValue> values, Span<bool> found)
    {
        Debug.Assert(attributes.Length == values.Length && attributes.Length == found.Length);
        Debug.Assert(found.Count(false) > 0);
        
        for (int ruleIdx = this.Rules.Count - 1; ruleIdx >= 0; --ruleIdx)
        {
            ref var rule = ref this.Rules[ruleIdx];

            if (!rule.IsMatch(in path))
                continue;

            var assigns = rule.Assigns;

            for (int attrIdx = 0; attrIdx < attributes.Length; ++attrIdx)
            {
                if (found[attrIdx])
                    continue;

                if (assigns.TryGetValue(attributes[attrIdx], out var value))
                {
                    values[attrIdx] = CreateValueFromString(value);
                    found[attrIdx] = true;
                }
            }
        }
    }

    private static GitAttributeValue CreateValueFromString(string value)
    {
        if (ReferenceEquals(value, Constants.attribute_internal_unset))
            return new GitAttributeValue(GitAttributeValue.ValueType.Unspecified, null);

        if (ReferenceEquals(value, Constants.attribute_internal_true))
            return new GitAttributeValue(GitAttributeValue.ValueType.True, null);

        if (ReferenceEquals(value, Constants.attribute_internal_false))
            return new GitAttributeValue(GitAttributeValue.ValueType.False, null);

        return new GitAttributeValue(GitAttributeValue.ValueType.String, value);
    }

    private static GitObjectID AttributeFileOidFromIndex(GitRepository repo, string path)
    {
        var index = repo.GetIndex();

        int pos = index.FindPosition(path, 0);

        return index[pos].Id;
    }

    internal static unsafe GitAttributeFile? Load(GitRepository repo, GitAttributeSession? attributeSession, Entry entry, in Source source, Parser? parser, bool allow_macros)
    {
        // git_attr_file__load()

        GitObjectID id = default;
        DateTime fileWriteTime = default;
        bool nonexistent = false;

        ReadOnlySpan<char> content = default;

        char[]? pooledArray = null;

        GitBlob? blob = null;
        GitTree? tree = null;

        switch (source.Type)
        {
            case SourceType.Memory:
                // in memory attribute file does not need data
                break;

            case SourceType.Index:
                id = AttributeFileOidFromIndex(repo, entry.Path);
                blob = repo.LookupBlob(in id);

                goto from_blob;

            case SourceType.File:
            {
                try
                {
                    using var fileHandle = File.OpenHandle(entry.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);
                    long length = RandomAccess.GetLength(fileHandle);

                    if (length > Constants.MaxAttributeFileSize)
                    {
                        nonexistent = true;
                        break;
                    }

                    fileWriteTime = File.GetLastWriteTimeUtc(fileHandle);

                    using var stream = new FileStream(fileHandle, FileAccess.Read);
                    using var reader = new StreamReader(stream); // automatically handles all unicode formats, assuming the bom is present (defaults to UTF8)

                    // Assume utf8, which can be one character per byte. (Ascii)
                    // reader.CurrentEncoding is only set properly on first read from the stream,
                    // so we can't know ahead of time what the encoding of the stream is.
                    pooledArray = ArrayPool<char>.Shared.Rent((int)length);

                    int read = reader.Read(pooledArray);

                    content = pooledArray.AsSpan(0, read);

                    Debug.Assert(reader.EndOfStream);
                }
                catch (IOException)
                {
                    nonexistent = true;
                }

                break;
            }

            case SourceType.Commit:
                tree = repo.LookupCommit(in source.CommitId).GetTree();
                goto from_tree;

            case SourceType.Head:
                tree = repo.GetHeadTree();

            from_tree:
                id = tree.Oid;
                blob = repo.LookupBlob(tree.GetEntryByPath(entry.Path).Oid);

            from_blob:
                if (blob.RawSize > Constants.MaxAttributeFileSize)
                {
                    return null;
                }

                using (var reader = new StreamReader(blob.GetContentStream()))
                {
                    // Assume utf8, which can be one character per byte. (Ascii)
                    // reader.CurrentEncoding is only set properly on first read from the stream,
                    // so we can't know ahead of time what the encoding of the stream is.
                    pooledArray = ArrayPool<char>.Shared.Rent((int)blob.RawSize);

                    int read = reader.Read(pooledArray);

                    content = pooledArray.AsSpan(0, read);

                    Debug.Assert(reader.EndOfStream);
                }

                break;

            default:
                throw new ArgumentException($"Unknown file source '{source.Type}'!");
        }

        var file = new GitAttributeFile(entry, in source);

        if (attributeSession is not null)
        {
            file.SessionKey = attributeSession.Key;
        }

        parser?.Invoke(repo, file, content, allow_macros);

        if (nonexistent)
            file.NonExistant = true;
        else if (source.Type is SourceType.Index or SourceType.Head or SourceType.Commit)
            file.cache_data_oid = id;
        else if (source.Type == SourceType.File)
            file.cache_data_stamp = fileWriteTime;

        if (pooledArray is not null)
            ArrayPool<char>.Shared.Return(pooledArray);

        return file;
    }

    public static GitAttributeFile LoadStandalone(string path)
    {
        path = Path.GetFullPath(path);
        
        var text = File.ReadAllText(path);

        var file = new GitAttributeFile(null, new Source(SourceType.File, path));

        ParseBuffer(null, file, text, true);

        file.EntryInstance = GitAttributeCache.CreateFileEntry(null, null, path);

        return file;
    }

    public static void ParseBuffer(GitRepository? repo, GitAttributeFile attributes, ReadOnlySpan<char> data, bool allow_macros)
    {
        string? context = null;

        if (attributes.EntryInstance is not null
            && !Path.IsPathRooted(attributes.EntryInstance.Path)
            && attributes.EntryInstance.Path.EndsWith("/" + FileName))
        {
            context = attributes.EntryInstance.Path;
        }

        lock (attributes.Lock)
        {
            while (data.Length > 0)
            {
                if (GitAttributeRule.TryParse(
                    data,
                    GitAttributeFNMatchFlags.AllowNegation | GitAttributeFNMatchFlags.AllowMacro,
                    context,
                    repo,
                    out int consumed,
                    out GitAttributeRule rule))
                {
                    // TODO: Lookup repository configuration for ignoring case
                    bool ignoreCase = false;

                    if (ignoreCase)
                        rule.Match.Flags |= GitAttributeFNMatchFlags.IgnoreCase;

                    if ((rule.Match.Flags & GitAttributeFNMatchFlags.Macro) != 0)
                    {
                        // [Inherited] TODO: warning if macro found in file below repo root
                        if (!allow_macros)
                            continue;

                        // TODO: Add Macro to repository cache
                    }
                    else
                    {
                        attributes.Rules.Add(rule);
                    }
                }

                Debug.Assert(consumed > 0);

                data = data.Slice(consumed);
            }
        }
    }

    public const string FileName = ".gitattributes";
    public const string FileNameInRepo = "attributes";
    public const string FileNameSystem = "gitattributes";
    public const string FileNameXDG = "attributes";

    public const int MaxFileSize = 100 * 1024 * 1024;

    public enum SourceType
    {
        Memory = 0,
        File = 1,
        Index = 2,
        Head = 3,
        Commit = 4
    }

    public struct Source(SourceType type, string filename, string? @base = null, GitObjectID commitId = default)
    {
        public SourceType Type = type;
        public string? Base = @base;
        public string? FileName = filename;
        public GitObjectID CommitId = commitId;
    }

    public sealed class Entry
    {
        public FileEntries File;
        public required string Path;
        public required string FullPath;

        [InlineArray(5)]
        public struct FileEntries
        {
            public GitAttributeFile? _element;
        }
    }

    public delegate void Parser(GitRepository repo, GitAttributeFile file, ReadOnlySpan<char> data, bool allow_macros);
}
