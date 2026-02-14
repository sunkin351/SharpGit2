using System.Collections;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SharpGit2.Managed.Config.Backend;

internal class GitConfigSnapshotBackend
    : IGitConfigBackend
{
    public bool IsReadOnly => true;
    
    public GitConfig Config { get; set; } = null!;

    private IGitConfigBackend Source;
    private readonly Lock _lock = new();
    private readonly Dictionary<string, ConfigMapEntryHead<GitConfigEntry>> _nameLookup = new();
    private ValueList<GitConfigEntry> _entries = new();

    private volatile bool _disposed = false;
    
    public GitConfigSnapshotBackend(IGitConfigBackend source)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
            return;
        
        lock (_lock)
        {
            _nameLookup.Clear();
            _entries.Clear();
        }
    }

    public IEnumerator<GitConfigEntry> GetEnumerator()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        lock (_lock)
        {
            // For now, make a copy of the data
            return (_entries.Count == 0 ? Enumerable.Empty<GitConfigEntry>() : _entries.GetSpan().ToArray()).GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public void Open(GitConfigLevel level, GitRepository? repo)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        lock (_lock)
        {
            foreach (var entry in this.Source)
            {
                ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(_nameLookup, entry.Name, out bool exists);

                value.Entry = entry;
                value.Multivar = exists;

                _entries.Add(entry);
            }
            
            _entries.Capacity = _entries.Count;
        }
    }

    public GitConfigEntry? Get(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        lock (_lock)
        {
            _nameLookup.TryGetValue(key, out var value);

            return value.Entry; // Null if not found
        }
    }

    public void Set(string key, string value)
    {
        throw new InvalidOperationException("This backend is Read-Only!");
    }

    public void SetMultiVar(string key, Regex regex, string value)
    {
        throw new InvalidOperationException("This backend is Read-Only!");
    }

    public bool Delete(string key)
    {
        throw new InvalidOperationException("This backend is Read-Only!");
    }

    public bool DeleteMultiVar(string key, Regex regex)
    {
        throw new InvalidOperationException("This backend is Read-Only!");
    }

    public IGitConfigBackend Snapshot()
    {
        return this;
    }

    public void Lock()
    {
        throw new InvalidOperationException("This backend is Read-Only!");
    }

    public void Unlock(bool success)
    {
        throw new InvalidOperationException("This backend is Read-Only!");
    }
}