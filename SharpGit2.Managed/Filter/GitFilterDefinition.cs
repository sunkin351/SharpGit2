using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using SharpGit2.Managed.Attributes;

namespace SharpGit2.Managed.Filter;

internal sealed class GitFilterDefinition(
    string filterName,
    IGitFilter filter,
    int priority,
    ImmutableArray<(string name, GitAttributeValue value)> attributes) : IComparable<GitFilterDefinition>
{
    public string FilterName { get; } = filterName;
        
    public IGitFilter Filter { get; } = filter;
        
    public int Priority { get; } = priority;
        
    public ImmutableArray<(string name, GitAttributeValue value)> Attributes { get; } = attributes;

    /// <summary>
    /// The flag that declares whether the filter's Initialize() method has been called yet.
    /// </summary>
    private volatile bool _initialized;
    private SpinLock _initLock = new();

    public bool Initialized => _initialized;

    public void EnsureInitialized()
    {
        if (_initialized)
            return;
            
        this.Initialize();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Initialize()
    {
        bool taken = false;
        try
        {
            // Ensure IGitFilter.Initialize() is called only once,
            // regardless of how many threads may be moving to call it.
            _initLock.Enter(ref taken);

            // If another thread got here first, it will have been initialized already.
            if (_initialized)
                return;
                
            this.Filter.Initialize();
            _initialized = true;
        }
        finally
        {
            if (taken)
                _initLock.Exit(true);
        }
    }

    public int CompareTo(GitFilterDefinition? other)
    {
        if (ReferenceEquals(this, other))
            return 0;
        if (other is null)
            return 1;
        return this.Priority.CompareTo(other.Priority);
    }
}