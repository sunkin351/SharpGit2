using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SharpGit2.Managed.Config.Backend;

internal sealed class GitConfigMemoryBackend : IGitConfigBackend
{
    private readonly string? BackendType;
    private readonly string? OriginPath;

    private readonly Dictionary<string, ConfigMapEntryHead> _nameLookup = new();
    private readonly LinkedList<ConfigListEntry> _entries = new();

    private string? _configData;
    private IEnumerable<KeyValuePair<string, string>>? _keyValuePairs;

    public GitConfigMemoryBackend(string configData, string? backendType, string? originPath)
    {
        ArgumentNullException.ThrowIfNull(configData);

        this.BackendType = backendType;
        this.OriginPath = originPath;

        _configData = configData;
    }

    public GitConfigMemoryBackend(IEnumerable<KeyValuePair<string, string>> keyValuePairs, string? backendType, string? originPath)
    {
        ArgumentNullException.ThrowIfNull(keyValuePairs);

        this.BackendType = backendType;
        this.OriginPath = originPath;

        _keyValuePairs = keyValuePairs;
    }

    public bool IsReadOnly => true;

    public GitConfig? Config { get; set; }

    public void Open(GitConfigLevel level, GitRepository? repo)
    {
        _nameLookup.Clear();
        _entries.Clear();

        if (_configData != null)
        {
            var parser = new GitConfigParser(new StringReader(_configData), "in-memory");

            var callbacks = new GitConfigMemoryParseCallbacks(this.BackendType, this.OriginPath, this, level);

            parser.Parse(callbacks);
        }
        else
        {
            Debug.Assert(_keyValuePairs != null);

            foreach (var (key, value) in _keyValuePairs)
            {
                if (string.IsNullOrEmpty(key))
                    throw new InvalidDataException("Empty config key");

                var entry = new ConfigListEntry(key, value, this.BackendType, this.OriginPath, 0, level)
                {
                    Backend = this
                };

                this.Append(entry);
            }
        }
    }

    private void Append(ConfigListEntry entry)
    {
        ref var t = ref CollectionsMarshal.GetValueRefOrAddDefault(_nameLookup, entry.Name, out bool exists);
        t.Entry = entry;
        t.Multivar = exists;

        _entries.AddLast(entry);
    }

    public bool Delete(string key)
    {
        throw new InvalidOperationException("This backend is read-only!");
    }

    public bool DeleteMultiVar(string key, Regex regex)
    {
        throw new InvalidOperationException("This backend is read-only!");
    }

    public void Dispose()
    {
    }

    public GitConfigEntry? Get(string key)
    {
        _ = _nameLookup.TryGetValue(key, out var tmp);

        return tmp.Entry!;
    }

    public void Lock()
    {
        throw new InvalidOperationException("This backend is read-only!");
    }

    public void Unlock(bool success)
    {
        throw new InvalidOperationException("This backend is read-only!");
    }

    public void Set(string key, string value)
    {
        throw new InvalidOperationException("This backend is read-only!");
    }

    public void SetMultiVar(string key, Regex regex, string value)
    {
        throw new InvalidOperationException("This backend is read-only!");
    }

    public IGitConfigBackend Snapshot()
    {
        return this; // This backend is read-only, so there's no need to create a separate object.
    }

    public IEnumerator<GitConfigEntry> GetEnumerator()
    {
        return _entries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }


    internal class GitConfigMemoryParseCallbacks : GitConfigParser.ICallbacks
    {
        public string BackendType;
        public string OriginPath;

        public GitConfigMemoryBackend Backend;

        public GitConfigLevel Level;

        public GitConfigMemoryParseCallbacks(string backendType, string originPath, GitConfigMemoryBackend backend, GitConfigLevel level)
        {
            BackendType = backendType;
            OriginPath = originPath;
            Backend = backend;
            Level = level;
        }

        public void OnComment(GitConfigParser parser, ReadOnlySpan<char> line)
        {
        }

        public void OnEndOfFile(GitConfigParser parser, string? currentSection)
        {
        }

        public void OnSection(GitConfigParser parser, string currentSection, ReadOnlySpan<char> line)
        {
        }

        public void OnVariable(
            GitConfigParser parser,
            string? currentSection,
            string variableName,
            string? variableValue,
            ReadOnlySpan<char> line)
        {
            string name = currentSection is null ? variableName : string.Create(checked(currentSection.Length + 1 + variableName.Length), (currentSection, variableName), (output, input) =>
            {
                input.currentSection.CopyTo(output);
                output[input.currentSection.Length] = '.';
                input.variableName.ToLowerInvariant(output.Slice(input.currentSection.Length + 1));
            });

            var entry = new ConfigListEntry(name, variableValue, this.BackendType, this.OriginPath, 0, this.Level)
            {
                Backend = this.Backend
            };

            this.Backend.Append(entry);
        }
    }
}
