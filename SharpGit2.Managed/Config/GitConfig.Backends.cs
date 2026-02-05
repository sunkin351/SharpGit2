using System.Diagnostics;
using SharpGit2.Managed.Config.Backend;

namespace SharpGit2.Managed.Config;

public partial class GitConfig
{
    internal static IGitConfigBackend GetBackendFromFile(string path)
    {
        return new GitConfigFileBackend(path);
    }

    public static IGitConfigBackend GetBackendFromString(string config, string? backendType, string? originPath)
    {
        return new GitConfigMemoryBackend(config, backendType, originPath);
    }

    public static IGitConfigBackend GetBackendFromValues(IEnumerable<KeyValuePair<string, string>> values, string? backendType, string? originPath)
    {
        return new GitConfigMemoryBackend(values, backendType, originPath);
    }

    public void AddBackend(IGitConfigBackend file, GitConfigLevel level, GitRepository? repo, bool force)
    {
        ArgumentNullException.ThrowIfNull(file);
        ValidateConfigLevel(level, true);

        file.Open(level, repo);
        file.Config = this;

        this.AddInstance(file, level, force);
    }

    private IGitConfigBackend? FindBackendByLevel(GitConfigLevel level, bool throwIfNotFound)
    {
        IGitConfigBackend? found = null;

        var readers = this._readers;
        if (level == GitConfigLevel.HighestLevel)
        {
            if (readers.Count > 0)
                found = _backendArray[readers[0]].Backend;
        }
        else
        {
            ref var entry = ref _backendArray[GetConfigLevelIndex(level)];

            if (entry.EntryInUse)
            {
                Debug.Assert(entry.Backend != null);
                found = entry.Backend;
            }
        }
        
        if (throwIfNotFound && found == null)
            throw new Git2Exception($"No configuration exists for the given level '{level}'");

        return found;
    }

    private void TryRemoveExistingBackend(GitConfigLevel level)
    {
#if DEBUG
        ValidateConfigLevel(level, true);
#endif

        var backends = _backendArray;
        int entryIndex = GetConfigLevelIndex(level);

        ref var entry = ref backends[entryIndex];
        
        if (entry.EntryInUse)
        {
            Debug.Assert(entry.Backend != null);
            Debug.Assert(entry.Level == level);

            this._readers.Remove(entryIndex);
            this._writers.Remove(entryIndex);

            // in case the dispose implementation decides to access the parent config for whatever reason,
            // ensure the disposed backend is removed before disposal.
            var backend = entry.Backend;
            entry = default;

            backend.Dispose();
        }

        Debug.Assert(entry.Backend == null);
        Debug.Assert(entry.EntryInUse == false);
        Debug.Assert(!backends.Any(x => x.Level == level));
    }

    private void AddInstance(IGitConfigBackend instance, GitConfigLevel level, bool force)
    {
        if (force)
            TryRemoveExistingBackend(level);

        int backendEntryIndex = GetConfigLevelIndex(level);
        ref var entry = ref _backendArray[backendEntryIndex];

        if (entry.EntryInUse)
            throw new InvalidOperationException($"Configuration at level '{level}' already exists!");

        entry = new BackendEntry(instance, level, (int)level); // automatically sets the `EntryInUse` field to true
        Debug.Assert(entry.EntryInUse == true);

        var readers = this._readers;
        var writers = this._writers;

        int idx = readers.BinarySearch(backendEntryIndex, _readerCompare);
        Debug.Assert(idx < 0);

        readers.Insert(~idx, backendEntryIndex);

        idx = writers.BinarySearch(backendEntryIndex, _writerCompare);

        if (idx >= 0)
        {
            for (int i = idx + 1; i < writers.Count; ++i)
            {
                if (_backendArray[writers[i]].WriteOrder != entry.WriteOrder)
                {
                    break;
                }
            }

            writers.Insert(idx, backendEntryIndex);
        }
        else
        {
            writers.Insert(~idx, backendEntryIndex);
        }
    }

    private IGitConfigBackend? GetWriter()
    {
        foreach (var index in this._writers)
        {
            ref var entry = ref _backendArray[index];

            Debug.Assert(entry.EntryInUse);

            if (entry.Backend.IsReadOnly || entry.WriteOrder < 0)
                continue;

            return entry.Backend;
        }

        return null;
    }

    private struct BackendEntry(IGitConfigBackend backend, GitConfigLevel level, int writeOrder)
    {
        public IGitConfigBackend Backend = backend;
        public GitConfigLevel Level = level;
        public int WriteOrder = writeOrder;
        public bool EntryInUse = true;
    }
}
