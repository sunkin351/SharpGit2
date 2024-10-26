using System.Runtime.InteropServices.Marshalling;

using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitWorkTree(Git2.Worktree* handle) : IDisposable
{
    internal readonly Git2.Worktree* NativeHandle = handle;

    public void Dispose()
    {
        git_worktree_free(NativeHandle);
    }

    public static GitWorkTree OpenFromRepository(GitRepository repository)
    {
        Git2.Worktree* result;
        Git2.ThrowIfError(git_worktree_open_from_repository(&result, repository.NativeHandle));

        return new(result);
    }

    public bool IsLocked()
    {
        var result = git_worktree_is_locked(null, this.NativeHandle);

        if (result < 0)
            Git2.ThrowError((GitError)result);

        return result != 0;
    }

    public bool IsLocked(out string? reason)
    {
        Native.GitBuffer _reason = default;

        var result = git_worktree_is_locked(&_reason, this.NativeHandle);

        if (result < 0)
            Git2.ThrowError((GitError)result);

        try
        {
            reason = _reason.AsString();
        }
        finally
        {
            git_buf_dispose(&_reason);
        }

        return result != 0;
    }

    public bool IsPrunable(in GitWorktreePruneOptions options)
    {
        int result;

        Native.GitWorktreePruneOptions _options = default;
        try
        {
            _options.FromManaged(in options);

            result = git_worktree_is_prunable(this.NativeHandle, &_options);
        }
        finally
        {
            _options.Free();
        }

        if (result < 0)
            Git2.ThrowError((GitError)result);

        return result != 0;
    }

    public void Lock(string reason)
    {
        Git2.ThrowIfError(git_worktree_lock(this.NativeHandle, reason));
    }

    /// <summary>
    /// Unlock a locked worktree
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the tree was unlocked, <see langword="false"/> otherwise.
    /// </returns>
    public bool Unlock()
    {
        int result = git_worktree_unlock(this.NativeHandle);

        if (result < 0)
            Git2.ThrowIfError((GitError)result);

        return result == 0;
    }

    public string GetName()
    {
        return Utf8StringMarshaller.ConvertToManaged(git_worktree_name(this.NativeHandle))!;
    }

    public string GetPath()
    {
        return Utf8StringMarshaller.ConvertToManaged(git_worktree_path(this.NativeHandle))!;
    }

    public void Prune(in GitWorktreePruneOptions options)
    {
        GitError error;

        Native.GitWorktreePruneOptions _options = default;
        try
        {
            _options.FromManaged(in options);

            error = git_worktree_prune(this.NativeHandle, &_options);
        }
        finally
        {
            _options.Free();
        }

        Git2.ThrowIfError(error);
    }

    public void Validate()
    {
        Git2.ThrowIfError(git_worktree_validate(this.NativeHandle));
    }

    public GitRepository OpenAsRepository()
    {
        Git2.Repository* result = null;

        Git2.ThrowIfError(git_repository_open_from_worktree(&result, this.NativeHandle));

        return new(result);
    }
}
