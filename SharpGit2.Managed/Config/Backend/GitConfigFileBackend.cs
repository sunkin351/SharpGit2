using System.Buffers;
using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.HighPerformance.Buffers;

using SharpGit2.Managed.Internal;

namespace SharpGit2.Managed.Config.Backend;

internal sealed class GitConfigFileBackend : IGitConfigBackend
{
    private const int MaxIncludeDepth = 10;

    private readonly Lock _valuesLock = new();

    private readonly Dictionary<string, ConfigMapEntryHead> _nameLookup = new();
    private readonly LinkedList<ConfigListEntry> _entries = new();

    private GitRepository? _repository;
    private GitConfigLevel _level;

    private ConfigFile _file;
    private volatile bool _locked;
    private volatile bool _disposed = false;

    public bool IsReadOnly { get; private set; }

    public GitConfig Config { get; set; } = null!;

    private GitFileBuffer? _lockedBuffer;
    private char[]? _lockedContent;
    private int _lockedContentLength;

    public GitConfigFileBackend(string path)
    {
        _file = new(path);
    }

    public void Dispose()
    {
        // If the original value was already true, return.
        // Dispose() is allowed to be called multiple times without error.
        if (Interlocked.Exchange(ref _disposed, true))
            return;

        lock (_valuesLock) // synchronize with any threads still using this object
        {
            _nameLookup.Clear();
            _entries.Clear();
            _repository = null;
            _file = default;
        }
    }

    public bool Delete(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string key = GitConfig.NormalizeName(name);

        lock (_valuesLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var entry = this.GetUnique(key);

            if (entry == null)
                return false;

            this.ConfigFileWrite(name, key, null, null);
            return true;
        }
    }

    public bool DeleteMultiVar(string key, Regex regex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        throw new NotImplementedException();
    }

    public GitConfigEntry? Get(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var key = GitConfig.NormalizeName(name);

        return this.GetInternal(key);
    }

    private GitConfigEntry? GetInternal(string key)
    {
        if (!this.IsReadOnly)
        {
            lock (_valuesLock)
            {
                this.RefreshNoLock();

                _ = _nameLookup.TryGetValue(key, out var value);

                return value.Entry; // may be null if not found
            }
        }
        else
        {
            _ = _nameLookup.TryGetValue(key, out var value);

            return value.Entry;
        }
    }

    public void Lock()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_locked)
            throw new InvalidOperationException();

        _lockedBuffer = new GitFileBuffer(_file.Path, default, GitConfig.FileMode);

        char[]? content = null;
        try
        {
            using var stream = File.OpenRead(_file.Path); // open/readonly

            int length = Encoding.UTF8.GetMaxCharCount(checked((int)stream.Length));

            using var reader = new StreamReader(stream);

            content = ArrayPool<char>.Shared.Rent(length);

            _lockedContentLength = reader.Read(content, 0, length);
            Debug.Assert(reader.EndOfStream);

            _lockedContent = content;
        }
        catch
        {
            // Ensure a consistent state
            _lockedBuffer.Dispose();
            _lockedBuffer = null;

            if (content != null)
                ArrayPool<char>.Shared.Return(content);

            _lockedContent = null;
            _locked = false;

            throw;
        }

        _locked = true;
    }

    public void Unlock(bool success)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_locked)
            throw new InvalidOperationException();

        Debug.Assert(_lockedBuffer != null && _lockedContent != null);

        if (success)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(
                Encoding.UTF8.GetMaxByteCount(_lockedContentLength));

            try
            {
                int written = Encoding.UTF8.GetBytes(_lockedContent.AsSpan(0, _lockedContentLength), buffer);

                _lockedBuffer.Write(buffer.AsSpan(0, written));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            _lockedBuffer.Commit();
        }

        _lockedBuffer.Dispose();
        _lockedBuffer = null;

        var tmp = _lockedContent;
        _lockedContent = null;
        ArrayPool<char>.Shared.Return(tmp);
        _lockedContentLength = 0;

        _locked = false;
    }

    public void Open(GitConfigLevel level, GitRepository? repo)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _level = level;
        _repository = repo;

        /*
         * git silently ignores configuration files that are not
         * readable.  We emulate that behavior.  This is particularly
         * important for sandboxed applications on macOS where the
         * git configuration files may not be readable.
         */
        // This returns false for unauthorized access exceptions
        if (!File.Exists(this._file.Path))
        {
            return;
        }

        try
        {
            // This object is not expected to be accessed from multiple threads during this phase.
            // Forego the lock.
            ConfigFileRead(repo, ref this._file, level, 0);
        }
        catch
        {
            // Ensure consistent state if an exception is thrown
            _nameLookup.Clear();
            _entries.Clear();
            _file.Clear();
            throw;
        }
    }

    public void Set(string name, string? value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        this.ThrowIfReadOnly();

        var key = GitConfig.NormalizeName(name);

        if (value != null)
        {
            value = EscapeValue(value);
        }

        lock (_valuesLock)
        {
            var existing = this.GetUnique(key);

            if (existing != null && existing.Value == value)
            {
                return;
            }

            this.ConfigFileWrite(name, key, null, value);
        }
    }

    public void SetMultiVar(string key, Regex regex, string? value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(regex);

        lock (_valuesLock)
        {
            ConfigFileWrite(key, GitConfig.NormalizeName(key), regex, value);
        }
    }

    public IGitConfigBackend Snapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        throw new NotImplementedException();
    }

    public IEnumerator<GitConfigEntry> GetEnumerator()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        List<ConfigListEntry> entries;

        lock (_valuesLock)
        {
            this.RefreshNoLock();

            entries = [.. _entries]; // make a copy of all current entries
        }

        return entries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private void RefreshFromReader(TextReader reader)
    {
        this.ThrowIfReadOnly();

        lock (_valuesLock)
        {
            _nameLookup.Clear();
            _entries.Clear();

            ConfigFileRead(_repository, ref _file, _level, 0, reader);
        }
    }

    private void RefreshNoLock()
    {
        if (this.IsReadOnly)
            return;

        if (!_file.IsModified())
        {
            return;
        }

        _file.ClearIncludes();

        _nameLookup.Clear();
        _entries.Clear();

        ConfigFileRead(_repository, ref _file, _level, 0);
    }

    private void Refresh()
    {
        lock (_valuesLock)
        {
            this.RefreshNoLock();
        }
    }

    private void ConfigFileRead(GitRepository? repo, ref ConfigFile file, GitConfigLevel level, int depth)
    {
        using var stream = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read);

        Debug.Assert(((ReadOnlySpan<byte>)file.Checksum).Length == SHA256.HashSizeInBytes);
        int written = SHA256.HashData(stream, file.Checksum);
        Debug.Assert(written == SHA256.HashSizeInBytes);

        stream.Position = 0; // reset stream position to the beginning

        using var reader = new StreamReader(stream);

        ConfigFileRead(repo, ref file, level, depth, reader);
    }

    private void ConfigFileRead(GitRepository? repo, ref ConfigFile file, GitConfigLevel level, int depth, TextReader reader)
    {
        Debug.Assert(depth >= 0);

        if (depth >= MaxIncludeDepth)
        {
            throw new Git2Exception("Maximum config include depth reached!");
        }

        var parser = new GitConfigParser(reader, file.Path);

        var callbacks = new ConfigFileParseCallbacks()
        {
            Config = this,
            ConfigFile = file,
            Depth = (uint)depth,
            Level = level,
            Repo = repo
        };

        parser.Parse(callbacks);

        file = callbacks.ConfigFile; // copy back, expected to be modified
    }

    private void ConfigFileWrite(string originalKey, string key, Regex? preparedRegex, string? value)
    {
        using var content = new ArrayPoolBufferWriter<char>(1024);
        GitFileBuffer? file = null;

        try
        {
            if (_locked)
            {
                if (this._lockedContent != null)
                {
                    content.Write(this._lockedContent.AsSpan(0, this._lockedContentLength));
                }
            }
            else
            {
                file = new GitFileBuffer(_file.Path, GitFileBuffer.Flags.HashSHA256, GitConfig.FileMode);

                using var stream = new StreamReader(_file.Path);

                int read;
                while ((read = stream.Read(content.GetSpan())) > 0)
                {
                    content.Advance(read);
                }
            }

            var parser = new GitConfigParser(new MemoryTextReader(content.WrittenMemory), _file.Path);

            int dot = key.LastIndexOf('.');
            Debug.Assert(dot >= 0);

            string name = key[(dot + 1)..], section = key[..dot];

            dot = originalKey.LastIndexOf('.');
            Debug.Assert(dot >= 0);

            string originalName = originalKey[(dot + 1)..], originalSection = originalKey[..dot];

            var callbacks = new ConfigFileModifyParseCallbacks()
            {
                OriginalSection = originalSection,
                OriginalName = originalName,
                Section = section,
                Name = name,
                PreparedRegex = preparedRegex,
                Value = value
            };

            parser.Parse(callbacks);

            if (_locked)
            {
                var builder = callbacks.TextBuilder;
                int length = builder.Length;

                if (length > _lockedContent!.Length)
                {
                    ArrayPool<char>.Shared.Return(_lockedContent);
                    _lockedContent = ArrayPool<char>.Shared.Rent(length);
                }

                Debug.Assert((uint)_lockedContent.Length <= (uint)length);

                builder.CopyTo(0, _lockedContent, 0, length);
                _lockedContentLength = length;
            }
            else
            {
                using (var writer = new StreamWriter(file!.GetWriteStream(), leaveOpen: true))
                    writer.Write(callbacks.TextBuilder);

                file.Commit(_file.Checksum);
                _file.TimeStamp = DateTime.UtcNow;

                // Refresh from written data
                this.ConfigFileRead(
                    _repository, ref _file, _level, 0,
                    new SequenceTextReader(callbacks.TextBuilder.ToReadOnlySequence()));
            }
        }
        finally
        {
            file?.Dispose();
        }
    }

    private ConfigListEntry? GetUnique(string key)
    {
        if (_nameLookup.TryGetValue(key, out var value))
        {
            if (value.Multivar)
                throw new Git2Exception("Entry is not unique due to being a multivar!");

            if (value.Entry.IncludeDepth > 0)
                throw new Git2Exception("Entry is not unique due to being included!");

            return value.Entry;
        }

        return null;
    }

    private static readonly FrozenDictionary<char, char> _escapeCharLookup = InitEscapeChars();
    private static readonly SearchValues<char> _escapeSearch = SearchValues.Create(GitConfigParser.ConfigEscaped);

    private static FrozenDictionary<char, char> InitEscapeChars()
    {
        var dict = new Dictionary<char, char>();

        for (int i = 0; i < GitConfigParser.ConfigEscaped.Length; ++i)
        {
            dict.Add(GitConfigParser.ConfigEscaped[i], GitConfigParser.ConfigEscapes[i]);
        }

        return dict.ToFrozenDictionary();
    }

    internal static void EscapeValue(ReadOnlySpan<char> value, StringBuilder builder)
    {
        var searchValues = _escapeSearch;

        int idx = value.IndexOfAny(searchValues);
        if (idx < 0)
        {
            builder.Append(value);
            return;
        }

        int position = 0;

        do
        {
            if (idx > 0)
            {
                builder.Append(value[position..(position + idx)]);
                position += idx;
            }

            builder.Append('\\').Append(_escapeCharLookup[value[position]]);
            position += 1;

            idx = value.Slice(position).IndexOfAny(searchValues);
        }
        while (idx >= 0);

        if (position < value.Length)
        {
            builder.Append(value.Slice(position));
        }
    }

    internal static string EscapeValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        ReadOnlySpan<char> input = value;

        int idx = input.IndexOfAny(_escapeSearch);

        if (idx < 0)
            return value;

        var builder = new StringBuilder();
        int position = 0;

        do
        {
            if (idx > 0)
            {
                builder.Append(input[position..(position + idx)]);
                position += idx;
            }

            builder.Append('\\').Append(_escapeCharLookup[input[position]]);
            position += 1;

            idx = input.Slice(position).IndexOfAny(_escapeSearch);
        }
        while (idx >= 0);

        if (position < input.Length)
        {
            builder.Append(input.Slice(position));
        }

        return builder.ToString();
    }

    private void ThrowIfReadOnly()
    {
        if (this.IsReadOnly)
            Throw();

        static void Throw()
        {
            throw new InvalidOperationException("This backend is Read-Only!");
        }
    }

    private void AppendInternal(ConfigListEntry entry)
    {
        Debug.Assert(_valuesLock.IsHeldByCurrentThread);

        ref var map_head = ref CollectionsMarshal.GetValueRefOrAddDefault(_nameLookup, entry.Name, out bool exists);

        map_head.Entry = entry;
        map_head.Multivar = exists;

        _entries.AddLast(entry);
    }

    private struct ConfigFile
    {
        public DateTime TimeStamp;
        public Utilities.SHA256HashField Checksum;
        public string Path;
        private ConfigFile[] IncludesArray;
        private int IncludeCount;

        public readonly Span<ConfigFile> Includes => IncludesArray.AsSpan(0, IncludeCount);

        public ConfigFile(string path)
        {
            Path = path;
            IncludesArray = [];
        }

        public void Clear()
        {
            this.TimeStamp = default;
            this.Checksum = default;
            this.Path = null!;

            this.ClearIncludes();
        }

        public void ClearIncludes()
        {
            Array.Clear(IncludesArray, 0, IncludeCount);
            IncludeCount = 0;
        }

        public bool IsModified()
        {
            if (File.GetLastWriteTimeUtc(this.Path) != this.TimeStamp)
            {
                using var stream = new FileStream(this.Path, FileMode.Open, FileAccess.Read);

                Span<byte> checksum = stackalloc byte[SHA256.HashSizeInBytes];

                int result = SHA256.HashData(stream, checksum);

                Debug.Assert(result == SHA256.HashSizeInBytes);

                if (!checksum.SequenceEqual(this.Checksum))
                {
                    return true;
                }
            }

            var array = this.IncludesArray;
            int len = this.IncludeCount;
            Debug.Assert((uint)len <= (uint)array.Length);

            for (int i = 0; i < len; ++i)
            {
                if (array[i].IsModified())
                    return true;
            }

            return false;
        }

        public ref ConfigFile AllocateFile()
        {
            throw new NotImplementedException();
        }
    }

    private sealed class ConfigFileParseCallbacks : GitConfigParser.ICallbacks
    {
        public GitRepository? Repo;
        public ConfigFile ConfigFile;
        public required GitConfigFileBackend Config;
        public GitConfigLevel Level;
        public uint Depth;

        public void OnComment(GitConfigParser parser, ReadOnlySpan<char> line)
        {
        }

        public void OnEndOfFile(GitConfigParser parser, string? currentSection)
        {
        }

        public void OnSection(GitConfigParser parser, string currentSection, ReadOnlySpan<char> line)
        {
        }

        public void OnVariable(GitConfigParser parser, string? currentSection, string variableName, string? variableValue, ReadOnlySpan<char> line)
        {
            string name = currentSection == null ? variableName : string.Create(currentSection.Length + 1 + variableName.Length, (currentSection, variableName), (output, args) =>
            {
                var (section, varName) = args;

                section.CopyTo(output);
                output[section.Length] = '.';
                varName.ToLowerInvariant(output.Slice(section.Length + 1));
            });

            var entry = new ConfigListEntry(name, variableValue, "file", this.ConfigFile.Path, this.Depth, this.Level)
            {
                Backend = this.Config
            };

            this.Config.AppendInternal(entry);

            if (name == "include.path")
            {
                this.ParseInclude(variableValue!);
            }
            else if (name.Length > includeIfText.Length + DotPathText.Length
                && name.StartsWith(includeIfText)
                && name.EndsWith(DotPathText))
            {
                ParseConditionalInclude(name, variableValue);
            }
        }

        private void ParseInclude(string file)
        {
            var directory = Path.GetDirectoryName(this.ConfigFile.Path)!;
            var path = IncludedPath(directory, file);

            ref var include = ref this.ConfigFile.AllocateFile();

            include = new ConfigFile(path);

            this.Config.ConfigFileRead(this.Repo, ref include, this.Level, (int)this.Depth + 1);
        }


        private static string IncludedPath(string dir, string path)
        {
            if (path.StartsWith("~/"))
            {
                // User's home directory
                return SystemDirectory.ExpandHomeDirectoryFile(path.AsSpan(2))!;
            }
            else
            {
                return PathUtilities.JoinUnrooted(path, dir, out _);
            }
        }

        internal static bool DoMatchGitdir(GitRepository repo, string configFile, string condition, bool caseInsensitive)
        {
            string pattern;

            if (condition.Length >= 2 && condition[0] is '.' or '~' && GitPath.IsDirectorySeparator(condition[1]))
            {
                if (condition[0] == '.')
                {
                    pattern = GitPath.PosixJoin(Path.GetDirectoryName(configFile), condition.AsSpan(2));
                }
                else
                {
                    pattern = SystemDirectory.ExpandHomeDirectoryFile(condition.AsSpan(2))!;
                }
            }
            else if (!Path.IsPathRooted(condition))
            {
                pattern = GitPath.PosixJoin("**", condition);
            }
            else
            {
                pattern = condition;
            }

            if (condition.Length > 0 && GitPath.IsDirectorySeparator(condition[^1]))
            {
                pattern = GitPath.PosixJoin(pattern, "**");
            }

            ReadOnlySpan<char> gitdir = repo.GetItemPath(GitRepositoryItemType.GitDir);

            if (gitdir.Length > 0 && GitPath.IsDirectorySeparator(gitdir[^1]))
            {
                gitdir = gitdir[..^1];
            }

            return WildMatch.Match(
                pattern,
                gitdir,
                WildMatch.Flags.PathName | (caseInsensitive ? WildMatch.Flags.CaseFold : 0)) == WildMatch.Result.Match;
        }

        private static bool ConditionalMatchOnBranch(GitRepository repo, string configFile, string condition)
        {
            /*
             * NOTE: you cannot use `GitRepository.GetHead()` here. Looking up the
             * HEAD reference will create the ODB, which causes us to read the
             * repo's config for keys like core.precomposeUnicode. As we're
             * just parsing the config right now, though, this would result in
             * an endless recursion.
             */
            var headPath = Path.Join(repo.RepositoryPath, Constants.GitHeadFile);
            var reference = File.ReadAllText(headPath).AsSpan().TrimEnd();

            const string expectedPrefix = Constants.GitSymRef + Constants.RefsHeadsDir;

            if (!reference.StartsWith(expectedPrefix))
            {
                return false;
            }

            reference = reference.Slice(expectedPrefix.Length);

            if (condition.Length > 0 && GitPath.IsDirectorySeparator(condition[^1]))
            {
                condition = GitPath.PosixJoin(condition, "**");
            }

            return WildMatch.Match(condition, reference, WildMatch.Flags.PathName) == WildMatch.Result.Match;
        }

        private static readonly (string, Func<GitRepository, string, string, bool>)[] Conditions = [
            ("gitdir:", (repo, config, value) => DoMatchGitdir(repo, config, value, false)),
            ("gitdir/i:", (repo, config, value) => DoMatchGitdir(repo, config, value, true)),
            ("onbranch:", ConditionalMatchOnBranch)
        ];

        private const string includeIfText = "includeIf.";
        private const string DotPathText = ".path";

        private void ParseConditionalInclude(string section, string? file)
        {
            if (this.Repo == null || file == null)
                return;

            Debug.Assert(section.Length > includeIfText.Length + DotPathText.Length
                && section.StartsWith(includeIfText)
                && section.EndsWith(DotPathText));

            ReadOnlySpan<char> condition = section.AsSpan()[includeIfText.Length..^DotPathText.Length];

            foreach (var (prefix, match) in Conditions)
            {
                if (condition.StartsWith(prefix))
                {
                    if (match(this.Repo, this.ConfigFile.Path, condition.Slice(prefix.Length).ToString()))
                    {
                        this.ParseInclude(file);
                    }

                    break;
                }
            }
        }
    }

    private sealed class ConfigFileModifyParseCallbacks : GitConfigParser.ICallbacks
    {
        public required string OriginalSection, Section, OriginalName, Name;

        public required Regex? PreparedRegex;
        public required string? Value;

        public readonly StringBuilder TextBuilder;
        private readonly StringBuilder _bufferedComments;

        private bool _preparedRegexReplaced;
        private bool _inSection;

        public ConfigFileModifyParseCallbacks(int builderCapacity = 1024)
        {
            TextBuilder = new StringBuilder(builderCapacity);
            _bufferedComments = new();
        }

        public void OnComment(GitConfigParser parser, ReadOnlySpan<char> line)
        {
            _bufferedComments.Append(line);
            if (!line.EndsWith('\n'))
                _bufferedComments.Append('\n');
        }

        public void OnEndOfFile(GitConfigParser parser, string? currentSection)
        {
            this.AppendAndClearComments();

            if ((this.PreparedRegex == null || !_preparedRegexReplaced) && this.Value != null)
            {
                if (currentSection == null || currentSection != this.Section)
                {
                    this.WriteSection(this.OriginalSection);
                }

                this.WriteValue();
            }
        }

        public void OnSection(GitConfigParser parser, string currentSection, ReadOnlySpan<char> line)
        {
            if (_inSection && this.PreparedRegex == null && this.Value != null)
            {
                this.WriteValue();
            }

            _inSection = this.Section == currentSection;

            this.AppendAndClearComments();

            this.TextBuilder.Append(line);
            if (!line.EndsWith('\n'))
                this.TextBuilder.Append('\n');
        }

        public void OnVariable(GitConfigParser parser, string? currentSection, string variableName, string? variableValue, ReadOnlySpan<char> line)
        {
            this.AppendAndClearComments();

            bool has_matched = _inSection && this.Name == variableName;

            if (has_matched && this.PreparedRegex != null)
            {
                has_matched = this.PreparedRegex.IsMatch(variableValue ?? "");
            }

            if (!has_matched)
            {
                this.TextBuilder.Append(line);

                if (!line.EndsWith('\n'))
                    this.TextBuilder.Append('\n');

                return;
            }

            _preparedRegexReplaced = true;

            if (this.Value == null)
                return;

            this.WriteValue();
        }

        private void WriteSection(string section)
        {
            var builder = this.TextBuilder;
            int dot = section.IndexOf('.');

            builder.Append('[');

            if (dot < 0)
            {
                builder.Append(section);
            }
            else
            {
                builder.Append(section.AsSpan(0, dot));
                builder.Append(" \"");

                EscapeValue(section.AsSpan(dot + 1), builder);

                builder.Append('"');
            }

            builder.Append("]\n");
        }

        private void WriteValue()
        {
            var value = this.Value;
            bool shouldQuote = ShouldQuoteValue(value);

            var builder = this.TextBuilder;
            builder.Append($"\t{this.OriginalName} = ");

            if (shouldQuote)
            {
                builder.Append('"').Append(value).Append('"');
            }
            else
            {
                builder.Append(value);
            }

            builder.Append('\n');
        }

        private static bool ShouldQuoteValue(string? value)
        {
            if (value == null
                || value.Length == 0
                || char.IsWhiteSpace(value[0])
                || char.IsWhiteSpace(value[^1]))
                return true;

            return value.ContainsAny(';', '#');
        }

        private void AppendAndClearComments()
        {
            var comments = _bufferedComments;

            this.TextBuilder.Append(comments);
            comments.Clear();
        }
    }
}
