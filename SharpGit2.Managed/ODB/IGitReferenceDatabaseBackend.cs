namespace SharpGit2.Managed.ODB;

[Flags]
public enum ReferenceDatabaseBackendInitFlags
{
    IsWorktree = 1,
    ForceHead = 2
}

public interface IGitReferenceDatabaseBackend : IDisposable
{
    void Initialize(string? headTarget, UnixFileMode mode, ReferenceDatabaseBackendInitFlags flags);
    
    /// <summary>
    /// Queries the refdb backend for the existence of a reference.
    /// </summary>
    /// <param name="referenceName">The reference's name that should be checked for existance.</param>
    /// <returns>
    /// <see langword="true"/> if the reference exists, <see langword="false"/> otherwise.
    /// </returns>
    bool Exists(string referenceName);

    /// <summary>
    /// Queries the refdb backend for a given reference.
    /// </summary>
    /// <param name="referenceName">he reference's name that should be checked for existance.</param>
    /// <returns>
    /// An allocated reference, if it could be found, <see langword="null"/> otherwise.
    /// </returns>
    GitReference? Lookup(string referenceName);

    IEnumerable<GitReference> EnumerateReferences(string glob);

    IEnumerable<string> EnumerateReferenceNames(string glob);

    void Write(GitReference reference, bool force, GitSignature who, string? message);
    void Write(GitReference reference, bool force, GitSignature who, string? message, string old_target);
    void Write(GitReference reference, bool force, GitSignature who, string? message, in GitObjectID old_target);

    GitReference Rename(string old_name, string new_name, bool force, GitSignature who, string? message);

    void Delete(string referenceName);
    void Delete(string referenceName, string old_target);
    void Delete(string referenceName, in GitObjectID old_target);

    /// <summary>
    /// Suggests that the given refdb compress or optimize its references.
    /// <br/><br/>
    /// This mechanism is implementation specific. For on-disk reference databases, this may pack all loose references.
    /// <br/><br/>
    /// A refdb implementation may provide this function; if it is not provided, nothing will be done.
    /// </summary>
    void Compress();

    /// <summary>
    /// Query whether a particular reference has a log (may be empty)
    /// </summary>
    /// <param name="referenceName"></param>
    /// <returns>
    /// 
    /// </returns>
    bool HasLog(string referenceName);

    void EnsureLog(string referenceName);

    GitReferenceLog ReflogRead(string referenceName);

    void ReflogWrite(GitReferenceLog reflog);

    void ReflogRename(string old_name, string new_name);

    void ReflogDelete(string referenceName);

    object Lock(string referenceName);

    void Unlock(object payload, bool success, bool update_reflog, GitReference? reference, GitSignature? who, string? message);
    void Unlock(object payload, int success, bool update_reflog, GitReference? reference, GitSignature? who, string? message);
}
