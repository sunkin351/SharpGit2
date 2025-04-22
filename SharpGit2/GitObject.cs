using SharpGit2.Native;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public enum GitObjectType
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

/// <summary>
/// Representation of a generic object in a repository
/// </summary>
/// <param name="nativeHandle">The native object pointer</param>
public unsafe readonly struct GitObject(Git2.Object* nativeHandle) : IDisposable, IGitObject<GitObject>, IGitHandle
{
    /// <summary>
    /// The native object pointer
    /// </summary>
    public Git2.Object* NativeHandle { get; } = nativeHandle;

    /// <inheritdoc/>
    public bool IsNull => NativeHandle == null;

    /// <summary>
    /// Frees the native object
    /// </summary>
    /// <remarks>
    /// Do not call more than once!!!
    /// </remarks>
    public void Dispose()
    {
        git_object_free(this.NativeHandle);
    }

    /// <summary>
    /// The type of this object
    /// </summary>
    public GitObjectType Type
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_object_type(handle.NativeHandle);
        }
    }

    public ref readonly GitObjectID Id
    {
        get
        {
            var handle = this.ThrowIfNull();

            return ref *git_object_id(handle.NativeHandle);
        }
    }

    public GitRepository Owner
    {
        get
        {
            var handle = this.ThrowIfNull();

            return new(git_object_owner(handle.NativeHandle));
        }
    }

    public GitObject Duplicate()
    {
        var handle = this.ThrowIfNull();

        Git2.Object* result = null;
        Git2.ThrowIfError(git_object_dup(&result, handle.NativeHandle));

        return new(result);
    }

    public GitObject Peel(GitObjectType type)
    {
        var handle = this.ThrowIfNull();

        if (!Enum.IsDefined(type) || type == GitObjectType.Invalid)
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        Git2.Object* result = null;
        Git2.ThrowIfError(git_object_peel(&result, handle.NativeHandle, type));

        return new(result);
    }

    public string GetShortID()
    {
        var handle = this.ThrowIfNull();

        GitBuffer result;
        Git2.ThrowIfError(git_object_short_id(&result, handle.NativeHandle));

        try
        {
            return result.AsString();
        }
        finally
        {
            git_buf_dispose(&result);
        }
    }

    public static bool IsRawContentValid(ReadOnlySpan<byte> buffer, GitObjectType type)
    {
        if (!Enum.IsDefined(type) || type == GitObjectType.Invalid)
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        if (buffer.IsEmpty)
        {
            return false;
        }

        int result;

        fixed (byte* _buffer = buffer)
        {
            Git2.ThrowIfError(git_object_rawcontent_is_valid(&result, _buffer, (nuint)buffer.Length, type));
        }

        return result != 0;
    }

    Git2.Object* IGitObject<GitObject>.NativeHandle => this.NativeHandle;

    static GitObject IGitObject<GitObject>.FromObjectPointer(Git2.Object* obj)
    {
        return new(obj);
    }

    static GitObjectType IGitObject<GitObject>.ObjectType => GitObjectType.Any;
}

public static class GitObjectTypeExtensions
{
    public static bool IsLooseType(this GitObjectType type)
    {
        if (!Enum.IsDefined(type) || type == GitObjectType.Invalid)
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        return git_object_typeisloose(type);
    }
}