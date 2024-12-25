using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public unsafe readonly struct GitWorkTree(Git2.Worktree* handle) : IDisposable, IGitHandle
{
    public Git2.Worktree* NativeHandle { get; } = handle;

    public bool IsNull => NativeHandle == null;

    public void Dispose()
    {
        git_worktree_free(NativeHandle);
    }

    public static GitWorkTree OpenFromRepository(GitRepository repository)
    {
        Git2.ThrowIfNull(repository);

        Git2.Worktree* result = null;
        Git2.ThrowIfError(git_worktree_open_from_repository(&result, repository.NativeHandle));

        return new(result);
    }

    public bool IsLocked()
    {
        var handle = this.ThrowIfNull();

        var result = git_worktree_is_locked(null, handle.NativeHandle);

        if (result < 0)
            Git2.ThrowError((GitError)result);

        return result != 0;
    }

    public bool IsLocked(out string? reason)
    {
        var handle = this.ThrowIfNull();

        Native.GitBuffer _reason = default;

        var result = git_worktree_is_locked(&_reason, handle.NativeHandle);

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
        var handle = this.ThrowIfNull();

        int result;

        Native.GitWorktreePruneOptions _options = default;
        try
        {
            _options.FromManaged(in options);

            result = git_worktree_is_prunable(handle.NativeHandle, &_options);
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
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_worktree_lock(handle.NativeHandle, reason));
    }

    /// <summary>
    /// Unlock a locked worktree
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the tree was unlocked, <see langword="false"/> otherwise.
    /// </returns>
    public bool Unlock()
    {
        var handle = this.ThrowIfNull();

        int result = git_worktree_unlock(handle.NativeHandle);

        if (result < 0)
            Git2.ThrowIfError((GitError)result);

        return result == 0;
    }

    public string GetName()
    {
        var handle = this.ThrowIfNull();

        var nativeName = git_worktree_name(handle.NativeHandle);

        return Git2.GetPooledString(nativeName);
    }

    public string GetPath()
    {
        var handle = this.ThrowIfNull();

        var nativePath = git_worktree_path(handle.NativeHandle);

        return Git2.GetPooledString(nativePath);
    }

    public void Prune(in GitWorktreePruneOptions options)
    {
        var handle = this.ThrowIfNull();

        GitError error;

        Native.GitWorktreePruneOptions _options = default;
        try
        {
            _options.FromManaged(in options);

            error = git_worktree_prune(handle.NativeHandle, &_options);
        }
        finally
        {
            _options.Free();
        }

        Git2.ThrowIfError(error);
    }

    public void Validate()
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_worktree_validate(handle.NativeHandle));
    }

    public GitRepository OpenAsRepository()
    {
        var handle = this.ThrowIfNull();

        Git2.Repository* result = null;

        Git2.ThrowIfError(git_repository_open_from_worktree(&result, handle.NativeHandle));

        return new(result);
    }
}
