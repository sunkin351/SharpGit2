using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

/// <summary>
/// A compiled pathspec
/// </summary>
/// <remarks>
/// Specifiers for path matching
/// </remarks>
/// <param name="nativeHandle">The native object pointer</param>
public unsafe readonly struct GitPathSpec(Git2.PathSpec* nativeHandle) : IGitHandle
{
    /// <summary>
    /// The native object pointer
    /// </summary>
    public Git2.PathSpec* NativeHandle { get; } = nativeHandle;

    /// <inheritdoc/>
    public bool IsNull => this.NativeHandle == null;

    public void Dispose()
    {
        git_pathspec_free(this.NativeHandle);
    }

    public bool IsMatch(string path, GitPathSpecFlags flags = GitPathSpecFlags.Default)
    {
        var handle = this.ThrowIfNull();

        return git_pathspec_matchs_path(handle.NativeHandle, flags, path) != 0;
    }

    public GitPathSpecMatchList Match(GitRepository repository, GitPathSpecFlags flags = GitPathSpecFlags.Default)
    {
        var handle = this.ThrowIfNull();

        Git2.PathSpecMatchList* result = null;
        Git2.ThrowIfError(git_pathspec_match_workdir(&result, repository.NativeHandle, flags, handle.NativeHandle));

        return new(result);
    }

    public GitPathSpecMatchList Match(GitIndex index, GitPathSpecFlags flags = GitPathSpecFlags.Default)
    {
        var handle = this.ThrowIfNull();

        Git2.PathSpecMatchList* result = null;
        Git2.ThrowIfError(git_pathspec_match_index(&result, index.NativeHandle, flags, handle.NativeHandle));

        return new(result);
    }

    public GitPathSpecMatchList Match(GitTree tree, GitPathSpecFlags flags = GitPathSpecFlags.Default)
    {
        var handle = this.ThrowIfNull();

        Git2.PathSpecMatchList* result = null;
        Git2.ThrowIfError(git_pathspec_match_tree(&result, tree.NativeHandle, flags, handle.NativeHandle));

        return new(result);
    }

    public GitPathSpecMatchList Match(GitDiff diff, GitPathSpecFlags flags = GitPathSpecFlags.Default)
    {
        var handle = this.ThrowIfNull();

        Git2.PathSpecMatchList* result = null;
        Git2.ThrowIfError(git_pathspec_match_diff(&result, diff.NativeHandle, flags, handle.NativeHandle));

        return new(result);
    }

}

/// <summary>
/// List of filenames matching a pathspec
/// </summary>
/// <param name="nativeHandle">The native object pointer</param>
public unsafe readonly struct GitPathSpecMatchList(Git2.PathSpecMatchList* nativeHandle) : IGitHandle
{
    /// <summary>
    /// The native object pointer
    /// </summary>
    public Git2.PathSpecMatchList* NativeHandle { get; } = nativeHandle;

    /// <inheritdoc/>
    public bool IsNull => this.NativeHandle == null;

    public nuint Count
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_pathspec_match_list_entrycount(handle.NativeHandle);
        }
    }

    public nuint FailedCount
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_pathspec_match_list_failed_entrycount(handle.NativeHandle);
        }
    }

    public void Dispose()
    {
        git_pathspec_match_list_free(this.NativeHandle);
    }

    public ref readonly Native.GitDiffDelta GetNativeDiffEntry(nuint position)
    {
        var handle = this.ThrowIfNull();

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, git_pathspec_match_list_entrycount(handle.NativeHandle));

        var ptr = git_pathspec_match_list_diff_entry(handle.NativeHandle, position);

        if (ptr is null)
        {
            throw new InvalidOperationException("Pathspec match list WAS NOT created against a Git Diff!");
        }

        return ref *ptr;
    }

    public GitDiffDelta GetDiffEntry(nuint position)
    {
        return new GitDiffDelta(in this.GetNativeDiffEntry(position));
    }

    public string GetEntry(nuint position)
    {
        var handle = this.ThrowIfNull();

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, git_pathspec_match_list_entrycount(handle.NativeHandle));

        var ptr = git_pathspec_match_list_entry(handle.NativeHandle, position);

        if (ptr is null)
        {
            throw new InvalidOperationException("Pathspec match list WAS created against a Git Diff!");
        }

        return Git2.GetPooledString(ptr);
    }

    public string GetFailedEntry(nuint position)
    {
        var handle = this.ThrowIfNull();

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, git_pathspec_match_list_failed_entrycount(handle.NativeHandle));

        var nativeEntry = git_pathspec_match_list_failed_entry(handle.NativeHandle, position);

        return Git2.GetPooledString(nativeEntry);
    }

    public static GitPathSpec Create(string[] pathspec)
    {
        Git2.PathSpec* result = null;

        Git2.ThrowIfError(git_pathspec_new(&result, pathspec));

        return new(result);
    }
}