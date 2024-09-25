using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitCommit : IDisposable
{
    internal readonly Git2.Commit* NativeHandle;

    internal GitCommit(Git2.Commit* handle)
    {
        NativeHandle = handle;
    }

    public ref readonly GitObjectID Id => ref *git_commit_id(NativeHandle);

    public GitRepository Owner => new(git_commit_owner(NativeHandle));

    public int ParentCount => checked((int)git_commit_parentcount(NativeHandle));

    public void Dispose()
    {
        git_commit_free(NativeHandle);
    }

    public GitCommit Duplicate()
    {
        Git2.Commit* result;
        Git2.ThrowIfError(git_commit_dup(&result, NativeHandle));

        return new(result);
    }

    public GitTree GetTree()
    {
        Git2.Tree* result;

        Git2.ThrowIfError(git_commit_tree(&result, NativeHandle));

        return new(result);
    }

    public ref readonly GitObjectID GetParentID(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, this.ParentCount);

        return ref *git_commit_parent_id(NativeHandle, (uint)index);
    }

    public GitCommit GetParent(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, this.ParentCount);

        Git2.Commit* result;
        Git2.ThrowIfError(git_commit_parent(&result, NativeHandle, (uint)index));

        return new(result);
    }

    public GitObject GetObjectByPath(string path, GitObjectType type)
    {
        Git2.Object* result;
        Git2.ThrowIfError(git_object_lookup_bypath(&result, (Git2.Object*)this.NativeHandle, path, type));

        return new(result);
    }

    public bool TryGetObjectByPath(string path, GitObjectType type, out GitObject obj)
    {
        Git2.Object* result;
        var error = git_object_lookup_bypath(&result, (Git2.Object*)this.NativeHandle, path, type);

        switch (error)
        {
            case GitError.OK:
                obj = new(result);
                return true;
            case GitError.NotFound:
                obj = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public static explicit operator GitCommit(GitObject obj)
    {
        return obj.Type == GitObjectType.Commit
            ? new GitCommit((Git2.Commit*)obj.NativeHandle)
            : throw new InvalidCastException("Git Object is not of type Commit!");
    }

    public static implicit operator GitObject(GitCommit commit)
    {
        return new((Git2.Object*)commit.NativeHandle);
    }
}
