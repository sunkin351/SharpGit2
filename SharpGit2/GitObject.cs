using SharpGit2.Native;

using static SharpGit2.NativeApi;

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

public unsafe readonly struct GitObject : IDisposable
{
    internal readonly Git2.Object* NativeHandle;

    internal GitObject(Git2.Object* nativeHandle)
    {
        NativeHandle = nativeHandle;
    }

    public void Dispose()
    {
        git_object_free(this.NativeHandle);
    }

    public GitObjectType Type => git_object_type(this.NativeHandle);

    public ref readonly GitObjectID Id => ref *git_object_id(this.NativeHandle);

    public GitRepository Owner => new(git_object_owner(this.NativeHandle));

    public GitObject Duplicate()
    {
        Git2.Object* result;
        Git2.ThrowIfError(git_object_dup(&result, this.NativeHandle));

        return new(result);
    }

    public GitObject Peel(GitObjectType type)
    {
        Git2.Object* result;
        Git2.ThrowIfError(git_object_peel(&result, this.NativeHandle, type));

        return new(result);
    }

    public static bool IsRawContentValid(ReadOnlySpan<byte> buffer, GitObjectType type)
    {
        int result;

        fixed (byte* _buffer = buffer)
        {
            Git2.ThrowIfError(git_object_rawcontent_is_valid(&result, _buffer, (nuint)buffer.Length, type));
        }

        return result != 0;
    }

    public string GetShortID()
    {
        GitBuffer result;
        Git2.ThrowIfError(git_object_short_id(&result, this.NativeHandle));

        try
        {
            return result.AsString();
        }
        finally
        {
            git_buf_dispose(&result);
        }
    }
}

public static class GitObjectTypeExtensions
{
    public static bool IsLooseType(this GitObjectType type)
    {
        return git_object_typeisloose(type);
    }
}