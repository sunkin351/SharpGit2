using System.Runtime.InteropServices.Marshalling;

using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitPathSpec(Git2.PathSpec* handle) : IDisposable
{
    public readonly Git2.PathSpec* NativeHandle = handle;

    public void Dispose()
    {
        git_pathspec_free(NativeHandle);
    }

    public bool IsMatch(string path, GitPathSpecFlags flags = GitPathSpecFlags.Default)
    {
        return git_pathspec_matchs_path(NativeHandle, flags, path) != 0;
    }

    public GitPathSpecMatchList Match(GitRepository repository, GitPathSpecFlags flags = GitPathSpecFlags.Default)
    {
        Git2.PathSpecMatchList* result;
        Git2.ThrowIfError(git_pathspec_match_workdir(&result, repository.NativeHandle, flags, NativeHandle));

        return new(result);
    }

    public GitPathSpecMatchList Match(GitIndex index, GitPathSpecFlags flags = GitPathSpecFlags.Default)
    {
        Git2.PathSpecMatchList* result;
        Git2.ThrowIfError(git_pathspec_match_index(&result, index.NativeHandle, flags, NativeHandle));

        return new(result);
    }

    public GitPathSpecMatchList Match(GitTree tree, GitPathSpecFlags flags = GitPathSpecFlags.Default)
    {
        Git2.PathSpecMatchList* result;
        Git2.ThrowIfError(git_pathspec_match_tree(&result, tree.NativeHandle, flags, NativeHandle));

        return new(result);
    }

    public GitPathSpecMatchList Match(GitDiff diff, GitPathSpecFlags flags = GitPathSpecFlags.Default)
    {
        Git2.PathSpecMatchList* result;
        Git2.ThrowIfError(git_pathspec_match_diff(&result, diff.NativeHandle, flags, NativeHandle));

        return new(result);
    }

}

public unsafe readonly struct GitPathSpecMatchList(Git2.PathSpecMatchList* handle) : IDisposable
{
    public readonly Git2.PathSpecMatchList* NativeHandle = handle;

    public nuint Count => git_pathspec_match_list_entrycount(NativeHandle);

    public nuint FailedCount => git_pathspec_match_list_failed_entrycount(NativeHandle);

    public void Dispose()
    {
        git_pathspec_match_list_free(NativeHandle);
    }

    public ref readonly Native.GitDiffDelta GetNativeDiffEntry(nuint position)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, this.Count);

        var ptr = git_pathspec_match_list_diff_entry(NativeHandle, position);

        if (ptr is null)
        {
            throw new InvalidOperationException("Pathspec match list WAS NOT created against a Git Diff!");
        }

        return ref *ptr;
    }

    public GitDiffDelta GetDiffEntry(nuint position)
    {
        return new GitDiffDelta(in GetNativeDiffEntry(position));
    }

    public string GetEntry(nuint position)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, this.Count);

        var ptr = git_pathspec_match_list_entry(NativeHandle, position);

        if (ptr is null)
        {
            throw new InvalidOperationException("Pathspec match list WAS created against a Git Diff!");
        }

        return Utf8StringMarshaller.ConvertToManaged(ptr)!;
    }

    public string GetFailedEntry(nuint position)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, this.FailedCount);

        return Utf8StringMarshaller.ConvertToManaged(git_pathspec_match_list_failed_entry(NativeHandle, position))!;
    }

    public static GitPathSpec Create(string[] pathspec)
    {
        Git2.PathSpec* result;

        Git2.ThrowIfError(git_pathspec_new(&result, pathspec));

        return new(result);
    }
}