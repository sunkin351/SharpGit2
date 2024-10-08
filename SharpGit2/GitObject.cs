using SharpGit2.Native;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static SharpGit2.NativeApi;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

    public GitObjectType Type => git_object_type(NativeHandle);

    //public static partial GitObjectID* git_object_id(Git2.Object* instance);
    public ref readonly GitObjectID Id => ref *git_object_id(NativeHandle);

    //public static partial GitError git_object_dup(Git2.Object** obj_out, Git2.Object* obj);
    public GitObject Duplicate()
    {
        Git2.Object* result;
        Git2.ThrowIfError(git_object_dup(&result, this.NativeHandle));

        return new(result);
    }

    //public static partial Git2.Repository* git_object_owner(Git2.Object* obj);
    public GitRepository Owner()
    {
        Git2.Repository* result = git_object_owner(this.NativeHandle);

        return new(result);
    }
    //public static partial GitError git_object_peel(Git2.Object** obj_out, Git2.Object* obj, GitObjectType type);
    public GitObject Peel(GitObjectType type)
    {
        Git2.Object* result;
        Git2.ThrowIfError(git_object_peel(&result, this.NativeHandle, type));

        return new(result);
    }

    //public static partial GitError git_object_rawcontent_is_valid(int* valid, byte* buffer, nuint length, GitObjectType type);
    static public int RawcontentIsValid(ReadOnlySpan<byte> buffer, GitObjectType type)
    {
        int result;

        fixed (byte* _buffer = buffer)
        {
            Git2.ThrowIfError(git_object_rawcontent_is_valid(&result, _buffer, (nuint)buffer.Length, type));
        }

        return result;
    }

    //TODO: We don't like this
    //public static partial GitError git_object_short_id(Native.GitBuffer* id_out, Git2.Object* obj);
    public GitBuffer ShortID()
    {
        GitBuffer result;
        Git2.ThrowIfError(git_object_short_id(&result, this.NativeHandle));

        return result;
    }

    public void Dispose()
    {
        git_object_free(NativeHandle);
    }
}

public static class GitObjectTypeExtensions
{
    public static bool IsLooseType(this GitObjectType type)
    {
        return git_object_typeisloose(type);
    }
}