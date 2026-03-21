using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using SharpGit2.Managed.Config.Backend;
using SharpGit2.Managed.Internal;
using SharpGit2.Managed.Transaction;

namespace SharpGit2.Managed.Config;

public sealed partial class GitConfig : IEnumerable<GitConfigEntry>, IDisposable
{
    public enum Result
    {
        Found,
        NotFound,
        CouldNotParse,
    }

    internal const string FileNameProgramData = "config";
    internal const string FileNameSystem = "gitconfig";
    internal const string FileNameGlobal = ".gitconfig";
    internal const string FileNameXDG = "config";
    internal const string FileNameInRepo = "config";

    internal const UnixFileMode FileMode = UnixFileMode.UserRead
        | UnixFileMode.UserWrite
        | UnixFileMode.GroupRead
        | UnixFileMode.GroupWrite
        | UnixFileMode.OtherRead
        | UnixFileMode.OtherWrite;

    internal static string GlobalLocation { get; }

    public static string? FindGlobal()
    {
        return SystemDirectory.FindGlobalFile(FileNameGlobal, false);
    }

    public static string? FindXDG()
    {
        return SystemDirectory.FindXDGFile(FileNameXDG, false);
    }

    public static string? FindSystem()
    {
        return SystemDirectory.FindSystemFile(FileNameSystem, false);
    }

    public static string? FindProgramData()
    {
        var path = SystemDirectory.FindProgramDataFile(FileNameProgramData, false);

        if (path != null && !GitPath.OwnerIs(path, GitPath.PathOwnerType.CurrentUser | GitPath.PathOwnerType.Administrator))
        {
            throw new Git2ConfigException("programdata path has invalid ownership!");
        }

        return path;
    }

    public static GitConfig OpenOnDisk(string configPath)
    {
        var config = new GitConfig();

        config.AddFileOnDisk(configPath, GitConfigLevel.Local, null, false);

        return config;
    }

    internal static void RenameSection(GitRepository repo, string oldSectionName, string? newSectionName)
    {
        ArgumentNullException.ThrowIfNull(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(oldSectionName);

        var regex = new Regex($"^{Regex.Escape(oldSectionName)}\\..+");

        var config = repo.Config;

        if (newSectionName != null && (newSectionName.ContainsAnyExcept(_allowedCharacters) || newSectionName.StartsWith('-')))
        {
            throw new ArgumentException($"Invalid config section '{newSectionName}'");
        }

        string? replacement = newSectionName == null ? null : string.Create(newSectionName.Length + 1, newSectionName, (output, input) =>
        {
            input.ToLowerInvariant(output);
            output[input.Length] = '.';
        });

        foreach (var entry in config)
        {
            if (!regex.IsMatch(entry.Name))
                continue;

            if (replacement != null)
            {
                config.SetMultiVar(string.Concat(replacement, entry.Name.AsSpan(oldSectionName.Length + 1)), "^$", entry.Value!);
            }

            config.DeleteMultiVar(entry.Name, $"^{Regex.Escape(entry.Value ?? "")}$");
        }
    }

    // ReSharper disable once StringLiteralTypo
    private static readonly SearchValues<char> _allowedCharacters = SearchValues.Create("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-");

    internal static string NormalizeName(ReadOnlySpan<char> name)
    {
        Debug.Assert(!name.IsWhiteSpace());

        int fdot = name.IndexOf('.');
        int ldot = name.LastIndexOf('.');

        Debug.Assert(fdot <= ldot);

        if (fdot <= 0 || ldot == name.Length - 1 || (fdot != ldot && name[(fdot + 1)..ldot].Contains('\n')))
        {
            goto InvalidName;
        }

        char[] buffer = ArrayPool<char>.Shared.Rent(name.Length);

        try
        {
            for (int i = 0; i < name.Length; ++i)
            {
                if (i < fdot || i > ldot)
                {
                    char c = name[i];

                    if (char.IsAsciiLetterOrDigit(c))
                    {
                        c = char.ToLowerInvariant(c);
                    }
                    else if (c != '-' || i == 0 || i == ldot + 1)
                        goto InvalidName;

                    buffer[i] = c;
                }
                else
                {
                    buffer[i] = name[i];
                }
            }

            return new string(buffer, 0, name.Length);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }

    InvalidName:
        throw new ArgumentException($"Invalid config item name '{name}'!");
    }

    internal static GitConfig OpenDefault()
    {
        var config = new GitConfig();

        if ((FindGlobal() ?? GlobalLocation) is { } global)
        {
            config.AddFileOnDisk(global, GitConfigLevel.Global, null, false);
        }

        if (FindXDG() is { } xdgFile)
        {
            config.AddFileOnDisk(xdgFile, GitConfigLevel.XDG, null, false);
        }

        if (FindSystem() is { } systemFile)
        {
            config.AddFileOnDisk(systemFile, GitConfigLevel.System, null, false);
        }

        if (FindProgramData() is { } programDataFile)
        {
            config.AddFileOnDisk(programDataFile, GitConfigLevel.ProgramData, null, false);
        }

        return config;
    }

    public static bool TryParseBoolean(string? value, out bool result)
    {
        if (value == null || value.Length is 0 or > 5)
        {
            result = default;
            return false;
        }

        Span<char> lowerValue = stackalloc char[5];
        value.ToLowerInvariant(lowerValue);

        switch (lowerValue[..value.Length])
        {
            case "true" or "yes" or "on" or "1":
                result = true;
                return true;
            case "false" or "no" or "off" or "0":
                result = false;
                return true;
            default:
                result = default;
                return false;
        }
    }

    public static bool TryParseInt32(string? value, out int result)
    {
        if (TryParseInt64(value, out long result64))
        {
            var tres = unchecked((int)result64);

            if (tres == result64)
            {
                result = tres;
                return true;
            }
        }

        result = default;
        return false;
    }

    public static bool TryParseInt64(string? value, out long result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            goto FailParse;
        }

        if (Utilities.TryParseLong(value, out result, out int consumed))
        {
            if ((uint)consumed < (uint)value.Length)
            {
                long tresult = result;

                switch (value[consumed])
                {
                    case 'g' or 'G':
                        tresult *= 1024 * 1024 * 1024;
                        break;

                    case 'm' or 'M':
                        tresult *= 1024 * 1024;
                        break;

                    case 'k' or 'K':
                        tresult *= 1024;
                        break;

                    default:
                        goto FailParse;
                }

                consumed += 1;
                if ((uint)consumed < (uint)value.Length)
                    goto FailParse;

                result = tresult;
            }

            return true;
        }

    FailParse:
        result = default;
        return false;
    }

    public static bool TryParsePath(string? value, [NotNullWhen(true)] out string? result)
    {
        ReadOnlySpan<char> valueSpan = value;
        if (valueSpan.StartsWith('~'))
        {
            if (valueSpan.Length > 1 && valueSpan[1] != '/')
            {
                // Retrieving a homedir by name is not supported
                result = null;
                return false;
            }

            return (result = SystemDirectory.ExpandHomeDirectoryFile(
                valueSpan.SequenceEqual("~") ? default : valueSpan[2..])) != null;
        }

        result = value;
        return !string.IsNullOrWhiteSpace(value);
    }

    public GitRepository? ParentRepository { get; internal set; }
    private readonly BackendEntry[] _backendArray = new BackendEntry[8];
    
    // These are indexes into the `_backingArray` of entries
    private readonly List<int> _readers = new(8);
    private readonly List<int> _writers = new(8);
    
    private readonly Comparison<int> _readerCompare;
    private readonly Comparison<int> _writerCompare;
    private bool _disposed = false;

    public GitConfig()
    {
        _readerCompare = (left, right) =>
        {
            ref var leftEntry = ref _backendArray[left];
            ref var rightEntry = ref _backendArray[right];

            Debug.Assert(leftEntry.EntryInUse && rightEntry.EntryInUse);

            return rightEntry.Level.CompareTo(leftEntry.Level);
        };
        _writerCompare = (left, right) =>
        {
            ref var leftEntry = ref _backendArray[left];
            ref var rightEntry = ref _backendArray[right];

            Debug.Assert(leftEntry.EntryInUse && rightEntry.EntryInUse);

            return rightEntry.WriteOrder.CompareTo(leftEntry.WriteOrder);
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        foreach (ref var backend in _backendArray.AsSpan())
        {
            if (backend.EntryInUse)
            {
                Debug.Assert(backend.Backend != null);
                
                backend.Backend.Dispose();

                backend = default;
            }
        }
        
        _readers.Clear();
        _writers.Clear();
        _disposed = true;
    }

    public void AddFileOnDisk(string path, GitConfigLevel level, GitRepository? repo, bool force)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrEmpty(path);
        
        IGitConfigBackend? file = GetBackendFromFile(path);

        this.AddBackend(file, level, repo, force);
    }

    public bool? GetBoolean(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var entry = GetEntry(name, true, false);

        if (entry?.Value == null)
        {
            return null;
        }

        if (TryParseBoolean(entry.Value, out bool value))
        {
            return value;
        }

        if (int.TryParse(entry.Value, CultureInfo.InvariantCulture, out int numericValue))
        {
            return numericValue != 0;
        }

        throw new Git2Exception($"Failed to parse '{name}' as a boolean value!");
    }

    public void Set(string name, bool value)
    {
        this.Set(name, value ? "true" : "false");
    }

    public int? GetInt32(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        var entry = this.GetEntry(name, true, false);

        if (entry?.Value == null)
            return null;

        if (GitConfig.TryParseInt32(entry.Value, out int result))
        {
            return result;
        }

        throw new Git2Exception($"Failed to parse '{name}' as an Int32 value!");
    }

    public void Set(string name, int value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        this.Set(name, (long)value);
    }

    public long? GetInt64(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var entry = this.GetEntry(name, true, false);

        if (entry?.Value == null)
            return null;

        if (GitConfig.TryParseInt64(entry.Value, out long result))
            return result;
        
        throw new Git2Exception($"Failed to parse '{name}' as an Int64 value!");
    }

    internal void Set(string name, long value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        this.Set(name, value.ToString(CultureInfo.InvariantCulture));
    }

    public string? GetString(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        

        return this.GetEntry(name, true, false)?.Value;
    }

    public void Set(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        var backend = this.GetWriter() ?? throw new InvalidOperationException($"Cannot set '{name}'; The configuration is read-only!");

        backend.Set(name, value);

        if (this.ParentRepository is { } repo)
        {
            repo.ConfigMapLookupCacheClear();
        }
    }

    public string? GetPath(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var value = this.GetEntry(name, true, false)?.Value;

        if (value != null && TryParsePath(value, out string? path))
            return path;

        return null;
    }

    public GitConfigEntry GetEntry(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return this.GetEntry(name, true, true)!;
    }

    public bool TryGetEntry(string name, [NotNullWhen(true)] out GitConfigEntry? entry)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            entry = null;
            return false;
        }

        return (entry = this.GetEntry(name, true, false)) != null;
    }

    private GitConfigEntry? GetEntry(string name, bool normalizeName, bool throwIfMissing)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(name));

        if (normalizeName)
            name = NormalizeName(name);

        foreach (int readerIdx in this._readers)
        {
            ref var entry = ref _backendArray[readerIdx];
            Debug.Assert(entry.EntryInUse);

            var backend = entry.Backend;

            var backendEntry = backend.Get(name);

            if (backendEntry != null)
            {
                return backendEntry;
            }
        }

        if (throwIfMissing)
            throw new KeyNotFoundException();

        return null;
    }

    internal GitConfigEntry? LookupEntry(string key, bool no_errors)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            return this.GetEntry(key, false, false);
        }
        catch
        {
            if (no_errors)
                return null;

            throw;
        }
    }

    internal void UpdateEntry(string key, string? value, bool overwrite_existing, bool only_if_existing)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var entry = LookupEntry(key, false);

        if (entry == null && only_if_existing)
            return;

        if (entry != null && !overwrite_existing)
            return;

        if (value == entry?.Value)
            return;

        if (value == null)
            this.DeleteEntry(key);
        else
            this.Set(key, value);
    }

    [return: NotNullIfNotNull(nameof(fallbackValue))]
    internal string? GetStringForce(string key, string? fallbackValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return this.GetEntry(key, false, false)?.Value ?? fallbackValue;
    }

    internal bool GetBoolForce(string key, bool fallbackValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        string? value = this.GetEntry(key, false, false)?.Value;

        if (value != null)
        {
            if (TryParseBoolean(value, out bool result))
            {
                return result;
            }
            else if (TryParseInt32(value, out int intVal))
            {
                return intVal != 0;
            }
        }
        
        return fallbackValue;
    }

    internal int GetInt32Force(string key, int fallbackValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        
        string? value = this.GetEntry(key, false, false)?.Value;

        if (TryParseInt32(value, out int result))
        {
            return result;
        }
        
        return fallbackValue;
    }

    internal int ConfigMapLookup(GitConfigMapItem item)
    {
        switch (item)
        {
            case GitConfigMapItem.AutoCRLF:
            {
                string? value = this.GetString("core.autocrlf");

                if (value == null)
                    return Constants.GitAutoCRLFDefault;
                
                if (TryParseBoolean(value, out bool x))
                {
                    return x ? 1 : 0;
                }
                else if (value == "input")
                {
                    return 2;
                }
                else
                {
                    throw new Git2ConfigException($"Failed to map `core.autocrlf` value! Found Value: '{value}'");
                }
            }

            case GitConfigMapItem.EOL:
            {
                string? value = this.GetString("core.eol");

                return value switch
                {
                    null => 0,
                    "crlf" => 1,
                    "lf" => 2,
                    "native" => OperatingSystem.IsWindows() ? 1 : 2,
                    _ => throw new Git2ConfigException($"Failed to map `core.eol` value! Found value: '{value}'")
                };
            }

            case GitConfigMapItem.Symlinks:
            {
                return (this.GetBoolean("core.symlinks") ?? true) ? 1 : 0;
            }

            case GitConfigMapItem.FileMode:
            {
                return this.GetInt32("core.filemode") ?? (int)Constants.DefaultFileMode;
            }

            case GitConfigMapItem.IgnoreCase:
            {
                return (this.GetBoolean("core.ignorecase") ?? false) ? 1 : 0;
            }

            case GitConfigMapItem.IgnoreStat:
            {
                return (this.GetBoolean("core.ignorestat") ?? false) ? 1 : 0;
            }

            case GitConfigMapItem.TrustCTime:
            {
                return (this.GetBoolean("core.trustctime") ?? true) ? 1 : 0;
            }

            case GitConfigMapItem.Abbrev:
            {
                string? value = this.GetString("core.abbrev");

                if (value is null or "auto")
                    return Constants.GitAbbrevDefault;
                
                if (TryParseInt32(value, out int result))
                    return result;

                if (TryParseBoolean(value, out bool x) && !x) // GIT_CONFIGMAP_FALSE
                    return GitObjectID.MaxHexSize;

                throw new Git2ConfigException($"Failed to map `core.abbrev` value! Found value: '{value}'");
            }

            case GitConfigMapItem.Precompose:
            {
                return (this.GetBoolean("core.precomposeunicode") ?? false) ? 1 : 0;
            }

            case GitConfigMapItem.SafeCRLF:
            {
                string? value = this.GetString("core.safecrlf");

                if (value == null)
                    return 0;

                if (TryParseBoolean(value, out bool x))
                    return x ? 1 : 0;

                if (value == "warn")
                    return 2;
                
                throw new Git2ConfigException($"Failed to map `core.safecrlf` value! Found value: '{value}'");
            }

            case GitConfigMapItem.LogalLRefUpdates:
            {
                string? value = this.GetString("core.logallrefupdates");

                if (value == null)
                    return 2; // unset
                
                if (TryParseBoolean(value, out bool x))
                    return x ? 1 : 0;

                if (value == "always")
                    return 3;
                
                throw new Git2ConfigException($"Failed to map `core.logallrefupdates` value! Found value: '{value}'");
            }

            case GitConfigMapItem.ProtectHFS:
            {
                return (this.GetBoolean("core.protecthfs") ?? false) ? 1 : 0;
            }

            case GitConfigMapItem.ProtectNTFS:
            {
                return (this.GetBoolean("core.protectntfs") ?? true) ? 1 : 0;
            }

            case GitConfigMapItem.FSyncObjectFiles:
            {
                return (this.GetBoolean("core.fsyncobjectfiles") ?? false) ? 1 : 0;
            }

            case GitConfigMapItem.LongPaths:
            {
                return (this.GetBoolean("core.longpaths") ?? false) ? 1 : 0;
            }
            
            default:
                throw new ArgumentOutOfRangeException(nameof(item), item, "Invalid value for item!");
        }
    }

    public void ForEachMatch([StringSyntax("regex")] string regex, Action<GitConfigEntry> callback)
    {
        foreach (var entry in this.EnumerateGlob(regex))
        {
            callback(entry);
        }
    }

    public void ForEachMultiVariable(string name, [StringSyntax("regex")] string regex, Action<GitConfigEntry> callback)
    {
        foreach (var entry in this.EnumerateMultiVar(name, regex))
        {
            callback(entry);
        }
    }

    public IEnumerable<GitConfigEntry> EnumerateMultiVar(string name) => this.EnumerateMultiVar(name, (Regex?)null);
    
    public IEnumerable<GitConfigEntry> EnumerateMultiVar(string name, [StringSyntax("regex")] string? regex)
    {
        return this.EnumerateMultiVar(name, regex == null ? null : Utilities.GetCachedRegex(regex));
    }

    public IEnumerable<GitConfigEntry> EnumerateMultiVar(string name, Regex? regex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return Enumerate(this, NormalizeName(name), regex);
        
        static IEnumerable<GitConfigEntry> Enumerate(GitConfig config, string name, Regex? regex)
        {
            foreach (var entry in config)
            {
                if (entry.Name != name)
                    continue;

                if (regex == null || regex.IsMatch(entry.Value ?? ""))
                    yield return entry;
            }
        }
    }

    public void SetMultiVar(string name, [StringSyntax("regex")] string regex, string value)
    {
        this.SetMultiVar(name, Utilities.GetCachedRegex(regex), value);
    }

    public void SetMultiVar(string name, Regex regex, string value)
    {
        var backend = this.GetWriter() ?? throw new Git2ConfigException($"Cannot set '{name}': The configuration is read-only!");
        
        backend.SetMultiVar(name, regex, value);
    }

    public void SetWriteOrder(ReadOnlySpan<GitConfigLevel> levels)
    {
        if (levels.IsEmpty)
        {
            var readerSpan = CollectionsMarshal.AsSpan(this._readers);
            for (int i = 0; i < readerSpan.Length; ++i)
            {
                ref var entry = ref _backendArray[readerSpan[i]];
                Debug.Assert(entry.EntryInUse);
                entry.WriteOrder = -1;
            }
        }
        else
        {
            foreach (var level in levels)
            {
                ValidateConfigLevel(level, true);
            }

            Span<GitConfigLevel> orderLevels = levels.ToArray();

            var orderValues = new int[orderLevels.Length];
            orderValues.InitToIndexes(); // extension method

            orderLevels.Sort(orderValues);

            var readerSpan = CollectionsMarshal.AsSpan(this._readers);
            for (int i = 0; i < readerSpan.Length; ++i)
            {
                ref var entry = ref _backendArray[readerSpan[i]];
                Debug.Assert(entry.EntryInUse);

                int levelIndex = orderLevels.BinarySearch(new ConfigLevelComparable(entry.Level)); // performant workaround for enums not implementing IComparable<T>

                entry.WriteOrder = levelIndex >= 0 ? orderValues[levelIndex] : -1;
            }

            this._writers.Sort(_writerCompare);
        }
    }

    public GitConfig CreateSnapshot()
    {
        var config = new GitConfig();

        foreach (int readerIdx in _readers)
        {
            ref var entry = ref _backendArray[readerIdx];
            Debug.Assert(entry.EntryInUse);

            var snap = entry.Backend.Snapshot();

            config.AddBackend(snap, entry.Level, null, false);
        }

        config.SetWriteOrder(default);

        return config;
    }

    public GitConfig OpenGlobal()
    {
        if (TryOpenLevel(GitConfigLevel.XDG, out var config))
            return config;

        return OpenLevel(GitConfigLevel.Global);
    }

    public bool TryOpenGlobal([NotNullWhen(true)] out GitConfig? config)
    {
        if (TryOpenLevel(GitConfigLevel.XDG, out config))
            return true;

        return TryOpenLevel(GitConfigLevel.Global, out config);
    }

    public GitConfig OpenLevel(GitConfigLevel level)
    {
        ValidateConfigLevel(level, false);

        var instance = this.FindBackendByLevel(level, true)!;

        var config = new GitConfig();

        config.AddInstance(instance, level, true);

        return config;
    }

    public bool TryOpenLevel(GitConfigLevel level, [NotNullWhen(true)] out GitConfig? result)
    {
        ValidateConfigLevel(level, false);

        var instance = this.FindBackendByLevel(level, false);

        if (instance == null)
        {
            result = null;
            return false;
        }

        var config = new GitConfig();

        config.AddInstance(instance, level, true);

        result = config;
        return true;
    }

    public bool DeleteEntry(string name, bool throwIfReadOnly = true)
    {
        var backend = this.GetWriter();

        if (backend == null)
        {
            if (throwIfReadOnly)
                throw new InvalidOperationException("Git Config is readonly!");

            return false;
        }

        return backend.Delete(name);
    }

    public bool DeleteMultiVar(string name, string regex, bool throwIfReadOnly = true)
    {
        var backend = this.GetWriter();

        if (backend == null)
        {
            if (throwIfReadOnly)
                throw new InvalidOperationException("Git Config is readonly!");

            return false;
        }

        return backend.DeleteMultiVar(name, Utilities.GetCachedRegex(regex));
    }

    public bool DeleteMultiVar(string name, Regex regex, bool throwIfReadOnly = true)
    {
        var backend = this.GetWriter();

        if (backend == null)
        {
            if (throwIfReadOnly)
                throw new InvalidOperationException("Git Config is readonly!");

            return false;
        }

        return backend.DeleteMultiVar(name, regex);
    }

    public GitTransaction Lock()
    {
        var instance = this.GetWriter() ?? throw new Git2ConfigException("Cannot lock: the configuration is read-only!");
        
        instance.Lock();

        return new GitTransaction(this, instance);
    }

    internal void Unlock(object data, bool commit)
    {
        ArgumentNullException.ThrowIfNull(data);
        
        var instance = (IGitConfigBackend)data;
        
        Debug.Assert(instance == this.GetWriter());
        
        instance.Unlock(commit);
    }

    public IEnumerable<GitConfigEntry> EnumerateGlob(Regex regex)
    {
        return this.Where(x => regex.IsMatch(x.Name));
    }

    public IEnumerable<GitConfigEntry> EnumerateGlob(string regularExpression)
    {
        return this.Where(x => Regex.IsMatch(x.Name, regularExpression));
    }

    public IEnumerator<GitConfigEntry> GetEnumerator()
    {
        return new AllEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    private static void ValidateConfigLevel(GitConfigLevel level, bool disallowHighestLevel)
    {
        if (!Enum.IsDefined(level) || (disallowHighestLevel && level == GitConfigLevel.HighestLevel))
        {
            Throw(level);
        }

        static void Throw(GitConfigLevel level)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "Invalid config level value!");
        }
    }

    private static int GetConfigLevelIndex(GitConfigLevel level)
    {
#if DEBUG
        ValidateConfigLevel(level, true);
#endif
        // 0 is an unused value of config level
        return (int)level - 1;
    }

    private struct ConfigLevelComparable(GitConfigLevel level) : IComparable<GitConfigLevel>
    {
        public readonly int CompareTo(GitConfigLevel other)
        {
            return ((int)level).CompareTo((int)other);
        }
    }

    private sealed class AllEnumerator : IEnumerator<GitConfigEntry>
    {
        private readonly GitConfig _parent;
        private List<int>.Enumerator _readerEnumerator;
        private IEnumerator<GitConfigEntry>? _entryEnumerator;
        private bool _disposed = false;

        public GitConfigEntry Current { get; private set; } = null!;

        public AllEnumerator(GitConfig parent)
        {
            _parent = parent;
            _readerEnumerator = parent._readers.GetEnumerator();
        }

        public bool MoveNext()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_entryEnumerator is not null && _entryEnumerator.MoveNext())
            {
                this.Current = _entryEnumerator.Current;
                return true;
            }

            while (true)
            {
                if (!_readerEnumerator.MoveNext())
                {
                    this.Current = null!;
                    return false;
                }

                _entryEnumerator?.Dispose();

                _entryEnumerator = _parent._backendArray[_readerEnumerator.Current].Backend.GetEnumerator();

                if (_entryEnumerator.MoveNext())
                {
                    this.Current = _entryEnumerator.Current;
                    return true;
                }
            }
        }

        object IEnumerator.Current => this.Current;

        public void Dispose()
        {
            // List<T>.Enumerator has an empty Dispose method, don't even call it.
            var tmp = _entryEnumerator;
            if (tmp != null)
            {
                tmp.Dispose();
                _entryEnumerator = null;
            }

            this.Current = null!;
            _disposed = true;
        }

        public void Reset() => throw new NotSupportedException();
    }
}
