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
}
