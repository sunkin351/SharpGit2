using SharpGit2.Managed.Config;

namespace SharpGit2.Managed.ReferenceDB;

public enum GitReferenceDatabaseType
{
    Files = 1,
}

public sealed class GitReferenceDatabase : IDisposable
{
    internal const string InvalidHead = Constants.RefsHeadsDir + ".invalid";
    
    /// <summary>
    /// Creates a new reference database with no backends.
    /// </summary>
    /// <remarks>
    /// Before the Ref DB can be used for read/writing,
    /// a custom database backend must be manually set
    /// by setting <see cref="GitReferenceDatabase.Backend"/>
    /// </remarks>
    /// <param name="repository"></param>
    public GitReferenceDatabase(GitRepository repository)
    {
        this.Repository = repository;
    }
    
    public GitRepository Repository { get; }

    private volatile IGitReferenceDatabaseBackend? _backend;

    public IGitReferenceDatabaseBackend? Backend
    {
        get => Volatile.Read(ref field);
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            
            Interlocked.Exchange(ref field, value)?.Dispose();
        }
    }

    private volatile bool _disposed = false;

    /// <summary>
    /// Suggests that the <see cref="GitReferenceDatabase"/> compress or optimize its references.
    /// This mechanism is implementation specific. For on-disk reference databases, for example,
    /// this may pack all loose references. 
    /// </summary>
    public void Compress()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        this.ThrowIfBackendNotSet().Compress();
    }
    
    internal void Initialize(string headTarget, UnixFileMode mode, ReferenceDatabaseBackendInitFlags flags)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        this.ThrowIfBackendNotSet().Initialize(headTarget, mode, flags);
    }

    internal bool Exists(string referenceName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        return this.ThrowIfBackendNotSet().Exists(referenceName);
    }

    internal GitReference? Lookup(string referenceName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var reference = this.ThrowIfBackendNotSet().Lookup(referenceName);

        reference?.DB = this;

        return reference;
    }

    internal GitReference? Resolve(string referenceName, int maxDereference)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var reference = this.Lookup(referenceName);

        if (reference == null)
            return null;

        maxDereference = maxDereference < 0
            ? Constants.DefaultReferenceNestingLevel
            : Math.Min(maxDereference, Constants.MaxReferenceNestingLevel);

        for (int nesting = 0; nesting < maxDereference; ++nesting)
        {
            if (reference.ReferenceType == GitReferenceType.Direct)
                return reference;

            var resolved = this.Lookup(reference.SymbolicTarget!);

            if (resolved == null)
            {
                // If we find a symbolic reference with a nonexistent target, return it.
                return reference;
            }

            reference = resolved;
        }

        if (reference.ReferenceType != GitReferenceType.Direct && maxDereference != 0)
        {
            return null;
        }

        return reference;
    }
    
    internal GitReference Rename(string referenceName, string newName, bool force, GitSignature signature, string? logMessage)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        
        if (string.IsNullOrWhiteSpace(signature.Name) || string.IsNullOrWhiteSpace(signature.Email)) // was `default` used?
            throw new ArgumentException("Invalid signature!");

        var reference = this.ThrowIfBackendNotSet().Rename(referenceName, newName, force, signature, logMessage);

        reference.DB = this;

        return reference;
    }
    
    internal IEnumerable<GitReference> EnumerateReferences(string? glob)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return this.ThrowIfBackendNotSet().EnumerateReferences(glob);
    }

    internal IEnumerable<string> EnumerateReferenceNames(string? glob)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return this.ThrowIfBackendNotSet().EnumerateReferenceNames(glob);
    }
    
    internal void Write(GitReference reference, bool force, GitSignature signature, string? logMessage)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(reference);
        
        if (string.IsNullOrWhiteSpace(signature.Name) || string.IsNullOrWhiteSpace(signature.Email)) // was `default` used?
            throw new ArgumentException("Invalid signature!");

        reference.DB = this;
        
        this.ThrowIfBackendNotSet().Write(reference, force, signature, logMessage);
    }
    
    internal void Write(GitReference reference, bool force, GitSignature signature, string? logMessage, in GitObjectID oldId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(reference);
        
        if (string.IsNullOrWhiteSpace(signature.Name) || string.IsNullOrWhiteSpace(signature.Email)) // was `default` used?
            throw new ArgumentException("Invalid signature!");

        reference.DB = this;
        
        this.ThrowIfBackendNotSet().Write(reference, force, signature, logMessage, in oldId);
    }
    
    internal void Write(GitReference reference, bool force, GitSignature signature, string? logMessage, string oldTarget)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(reference);
        
        if (string.IsNullOrWhiteSpace(signature.Name) || string.IsNullOrWhiteSpace(signature.Email)) // was `default` used?
            throw new ArgumentException("Invalid signature!");

        reference.DB = this;

        this.ThrowIfBackendNotSet().Write(reference, force, signature, logMessage, oldTarget);
    }

    internal void Delete(string referenceName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceName);

        this.ThrowIfBackendNotSet().Delete(referenceName);
    }
    
    internal void Delete(string referenceName, in GitObjectID directTarget)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceName);

        this.ThrowIfBackendNotSet().Delete(referenceName, in directTarget);
    }
    
    internal void Delete(string referenceName, string symbolicTarget)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceName);

        this.ThrowIfBackendNotSet().Delete(referenceName, symbolicTarget);
    }

    internal GitReferenceLog ReadReferenceLog(string referenceName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceName);

        return this.ThrowIfBackendNotSet().ReflogRead(referenceName);
    }

    internal void WriteReferenceLog(GitReferenceLog reflog)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        ArgumentNullException.ThrowIfNull(reflog);

        this.ThrowIfBackendNotSet().ReflogWrite(reflog);
    }

    internal bool ShouldWriteReferenceLog(GitReference reference)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(reference);
        
        int value = this.Repository.Config.ConfigMapLookup(GitConfigMapItem.LogalLRefUpdates);

        switch (value)
        {
            case 1: // true
                string refName = reference.ReferenceName;
                return this.HasLog(refName)
                       || refName == Constants.GitHeadFile
                       || refName.StartsWith(Constants.RefsHeadsDir)
                       || refName.StartsWith(Constants.RefsRemotesDir)
                       || refName.StartsWith(Constants.RefsNotesDir);
            
            case 3: // always
                return true;
            case 0: // false
            case 2: // unset
            default:
                return false;
        }
    }
    
    internal bool ShouldWriteHeadReferenceLog(GitReference reference)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        ArgumentNullException.ThrowIfNull(reference);

        var head = this.Lookup(Constants.GitHeadFile);

        if (head!.ReferenceType == GitReferenceType.Direct)
            return false;

        GitReference? resolved = this.Resolve(head.SymbolicTarget!, -1);

        string name;
        if (resolved == null)
            name = head.SymbolicTarget!;
        else if (resolved.ReferenceType == GitReferenceType.Symbolic)
            name = resolved.SymbolicTarget!;
        else
            name = resolved.ReferenceName;

        return reference.ReferenceName == name;
    }
    
    public bool HasLog(string referenceName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return this.ThrowIfBackendNotSet().HasLog(referenceName);
    }

    public void EnsureLog(string referenceName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        this.ThrowIfBackendNotSet().EnsureLog(referenceName);
    }
    
    internal object Lock(string referenceName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return this.ThrowIfBackendNotSet().Lock(referenceName);
    }

    internal void Unlock(object payload, bool success, bool updateReflog, GitReference? reference, GitSignature signature, string? message)
    {
        this.Unlock(payload, success ? 1 : 0, updateReflog, reference, signature, message);
    }

    internal void Unlock(object payload, int success, bool updateReflog, GitReference? reference, GitSignature signature, string? message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        this.ThrowIfBackendNotSet().Unlock(payload, success, updateReflog, reference, signature, message);
    }


    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
            return;

        this.Backend = null;
    }

    private IGitReferenceDatabaseBackend ThrowIfBackendNotSet()
    {
        return this.Backend ?? throw new InvalidOperationException("Reference database backend has not been set!");
    }
    
    /// <summary>
    /// Create a new reference database and automatically add the default backends:
    /// <br/>
    /// - git_refdb_dir: read and write loose and packed refs from disk, assuming the repository dir as the folder.
    /// </summary>
    /// <param name="repo">The repository</param>
    /// <returns>The created reference database</returns>
    public static GitReferenceDatabase Open(GitRepository repo)
    {
        return new GitReferenceDatabase(repo)
        {
            Backend = new GitReferenceDatabaseFileSystemBackend(repo)
        };
    }
}
