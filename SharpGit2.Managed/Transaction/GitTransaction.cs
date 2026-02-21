using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using SharpGit2.Managed.Config;
using SharpGit2.Managed.ReferenceDB;

namespace SharpGit2.Managed.Transaction;

internal enum GitTransationType
{
    None = 0,
    Refs,
    Config
}

internal sealed class GitTransactionNode
{
    internal string Name;
    internal object Payload;
    internal GitReferenceType RefType;
    internal GitObjectID Target_ID;
    internal string? Target_Symbolic;
    internal GitReferenceLog? RefLog;
    internal string? Message;
    internal GitSignature? Signature;
    internal bool Committed, Remove;
}

public sealed class GitTransaction : IDisposable
{
    internal readonly GitTransationType Type;
    internal readonly GitRepository? Repo;
    internal readonly GitReferenceDatabase? RefDB;
    internal readonly GitConfig? Config;
    internal readonly object? ConfigData;

    internal readonly Dictionary<string, GitTransactionNode>? Locks;
    private bool _committed = false;

    internal GitTransaction(GitConfig config, object data)
    {
        Type = GitTransationType.Config;
        Config = config;
        ConfigData = data;
    }

    internal GitTransaction(GitRepository repo)
    {
        Type = GitTransationType.Refs;
        RefDB = repo.ReferenceDatabase;
        Locks = new();
    }

    internal void LockRef(string refname)
    {
        this.ThrowIfCommitted();

        ArgumentException.ThrowIfNullOrEmpty(refname);

        if (this.RefDB == null || this.Locks == null)
            throw new InvalidOperationException();

        var node = new GitTransactionNode
        {
            Name = refname,
            Payload = this.RefDB.Lock(refname)
        };

        try
        {
            this.Locks.Add(refname, node);
        }
        catch
        {
            this.RefDB.Unlock(node.Payload, false, false, null, default, null);
            throw;
        }
    }

    public void Dispose()
    {
        Commit();
    }

    internal void SetTarget(string refname, in GitObjectID target, GitSignature? signature, string? message)
    {
        this.ThrowIfCommitted();

        if (this.Locks == null || this.Repo == null)
            throw new InvalidOperationException();

        ArgumentException.ThrowIfNullOrEmpty(refname);

        if (!this.Locks.TryGetValue(refname, out var node))
            ThrowRefnameNotLocked();

        if (signature != null)
        {
            node.Signature = signature;
        }

        node.Signature ??= GitReference.LogSignature(this.Repo);

        if (message != null)
            node.Message = message;

        node.Target_ID = target;
        node.Target_Symbolic = null;
        node.RefType = GitReferenceType.Direct;
    }

    internal void SetSymbolicTarget(string refname, string target, GitSignature? signature, string? message)
    {
        this.ThrowIfCommitted();

        if (this.Locks == null || this.Repo == null)
            throw new InvalidOperationException();

        ArgumentException.ThrowIfNullOrEmpty(refname);
        ArgumentException.ThrowIfNullOrEmpty(target);

        if (!this.Locks.TryGetValue(refname, out var node))
            ThrowRefnameNotLocked();

        if (signature != null)
        {
            node.Signature = signature;
        }

        node.Signature ??= GitReference.LogSignature(this.Repo);

        if (message != null)
            node.Message = message;

        node.Target_ID = default;
        node.Target_Symbolic = target;
        node.RefType = GitReferenceType.Symbolic;
    }

    internal void Remove(string refname)
    {
        this.ThrowIfCommitted();

        if (this.Locks == null)
            throw new InvalidOperationException();

        if (!this.Locks.TryGetValue(refname, out var node))
            ThrowRefnameNotLocked();

        node.Remove = true;
        node.RefType = GitReferenceType.Direct; // The id will be ignored
    }

    internal void SetReflog(string refname, GitReferenceLog reflog)
    {
        this.ThrowIfCommitted();

        if (this.Locks == null)
            throw new InvalidOperationException();

        ArgumentException.ThrowIfNullOrEmpty(refname);
        ArgumentNullException.ThrowIfNull(reflog);

        if (!this.Locks.TryGetValue(refname, out var node))
            ThrowRefnameNotLocked();

        node.RefLog = reflog.Duplicate();
    }

    private static void UpdateTarget(GitReferenceDatabase db, GitTransactionNode node)
    {
        Debug.Assert(db != null);
        Debug.Assert(node != null);

        GitReference reference = node.RefType switch
        {
            GitReferenceType.Direct => new GitReference(node.Name, node.Target_ID, null),
            GitReferenceType.Symbolic => new GitReference(node.Name, node.Target_Symbolic!),
            _ => throw new InvalidOperationException()
        };

        switch (node)
        {
            case { Remove: true }:
                db.Unlock(node.Payload, 2, false, reference, node.Signature.Value, node.Message);
                break;
            case { RefType: GitReferenceType.Direct or GitReferenceType.Symbolic }:
                db.Unlock(node.Payload, true, node.RefLog == null, reference, node.Signature.Value, node.Message);
                break;
            default:
                throw new InvalidOperationException();
        }

        node.Committed = true;
    }

    public void Commit()
    {
        if (_committed)
            return;

        _committed = true;

        if (this.Type == GitTransationType.Config)
        {
            Debug.Assert(this.Config != null);
            Debug.Assert(this.ConfigData != null);

            this.Config.Unlock(this.ConfigData, true);
            return;
        }

        Debug.Assert(this.Type == GitTransationType.Refs);
        Debug.Assert(this.Locks != null);
        Debug.Assert(this.RefDB != null);

        foreach (var node in this.Locks.Values)
        {
            if (node.RefLog != null)
            {
                Debug.Assert(this.RefDB.Backend != null);
                this.RefDB.Backend.ReflogWrite(node.RefLog);
            }

            if (node.RefType == GitReferenceType.Invalid)
            {
                // The ref was locked but not modified
                this.RefDB.Unlock(node.Payload, false, false, null, default, null);
                node.Committed = true;
            }
            else
            {
                UpdateTarget(this.RefDB, node);
            }
        }
    }

    [DoesNotReturn]
    private static void ThrowRefnameNotLocked()
    {
        throw new ArgumentException("The specified reference was not locked!");
    }

    private void ThrowIfCommitted()
    {
        if (_committed)
            Throw();

        static void Throw()
        {
            throw new InvalidOperationException("This transaction has been committed and shouldn't be used further.");
        }
    }
}
