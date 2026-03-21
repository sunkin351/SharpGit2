using JetBrains.Annotations;
using SharpGit2.Managed.Cache;

namespace SharpGit2.Managed;

[PublicAPI]
public enum GitObjectType : short
{
    /// <summary>
    /// Object can be any of the following
    /// </summary>
    Any = -2,
    /// <summary>
    /// Object is invalid
    /// </summary>
    Invalid = -1,
    /// <summary>
    /// A commit object
    /// </summary>
    Commit = 1,
    /// <summary>
    /// A tree (directory listing) object
    /// </summary>
    Tree = 2,
    /// <summary>
    /// A file revision object.
    /// </summary>
    Blob = 3,
    /// <summary>
    /// An annotated tag object
    /// </summary>
    Tag = 4,
    /// <summary>
    /// A delta, base is given by an offset
    /// </summary>
    Offset_Delta = 6,
    /// <summary>
    /// A delta, base is given by object id
    /// </summary>
    REF_Delta = 7
}

internal interface ICacheableObject
{
    ref readonly GitObjectID ObjectID { get; }

    GitObjectType ObjectType { get; }
}

[PublicAPI]
public abstract class GitObject : IEquatable<GitObject>, ICacheableObject
{
    internal GitObjectID Oid;
    internal GitCacheStore Store;

    public GitRepository Repository { get; }

    public ref readonly GitObjectID ObjectID { get => ref Oid; }

    GitObjectType ICacheableObject.ObjectType => this.ObjectType;

    internal abstract GitObjectType ObjectType { get; }

    internal abstract int ObjectSize { get; }

    internal GitObject(in GitObjectID oid, GitRepository repository)
    {
        Oid = oid;
        Repository = repository;
    }

    public override bool Equals(object? obj)
    {
        return obj is GitObject @object &&
               this.Equals(@object);
    }

    public override int GetHashCode()
    {
        return Oid.GetHashCode();
    }

    public bool Equals(GitObject? other)
    {
        return this.GetType() == other?.GetType()
            && this.Oid.Equals(other.Oid);
    }

    public GitObject Peel(GitObjectType targetType = GitObjectType.Any)
    {
        throw new NotImplementedException();
    }
}
