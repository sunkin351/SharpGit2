using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

/// <summary>
/// The diff patch is used to store all the text diffs for a delta
/// </summary>
/// <param name="nativeHandle">The native object pointer</param>
public unsafe readonly struct GitPatch(Git2.Patch* nativeHandle) : IGitHandle
{
    /// <summary>
    /// The native object pointer
    /// </summary>
    public Git2.Patch* NativeHandle { get; } = nativeHandle;

    /// <inheritdoc/>
    public bool IsNull => this.NativeHandle == null;

    public void Dispose()
    {
        git_patch_free(NativeHandle);
    }

    public GitRepository? Owner
    {
        get
        {
            var handle = this.ThrowIfNull();

            Git2.Repository* owner = git_patch_owner(handle.NativeHandle);

            return owner is null ? null : new(owner);
        }
    }

    public ref readonly Native.GitDiffDelta NativeDelta
    {
        get
        {
            var handle = this.ThrowIfNull();

            return ref *git_patch_get_delta(handle.NativeHandle);
        }
    }

    public nuint HunkCount
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_patch_num_hunks(handle.NativeHandle);
        }
    }

    public GitDiffDelta GetDelta()
    {
        return new GitDiffDelta(in this.NativeDelta);
    }

    public ref readonly Native.GitDiffHunk GetNativeHunk(nuint hunk_idx, out nuint linesInHunk)
    {
        var handle = this.ThrowIfNull();

        Native.GitDiffHunk* result = null;
        nuint resultLines = 0;

        Git2.ThrowIfError(git_patch_get_hunk(&result, &resultLines, handle.NativeHandle, hunk_idx));

        Debug.Assert(result != null);
        linesInHunk = resultLines;
        return ref *result;
    }

    public (GitDiffHunk hunk, nuint linesInHunk) GetHunk(nuint hunk_idx)
    {
        var handle = this.ThrowIfNull();

        Native.GitDiffHunk* result = null;
        nuint resultLines = 0;

        Git2.ThrowIfError(git_patch_get_hunk(&result, &resultLines, handle.NativeHandle, hunk_idx));

        Debug.Assert(result != null);
        return (new GitDiffHunk(in *result), resultLines);
    }

    public ref readonly Native.GitDiffLine GetNativeHunkLine(nuint hunk_idx, nuint line_idx)
    {
        var handle = this.ThrowIfNull();

        Native.GitDiffLine* result = null;
        Git2.ThrowIfError(git_patch_get_line_in_hunk(&result, handle.NativeHandle, hunk_idx, line_idx));

        return ref *result;
    }

    public GitDiffLine GetHunkLine(nuint hunk_idx, nuint line_idx)
    {
        var handle = this.ThrowIfNull();

        Native.GitDiffLine* result = null;
        Git2.ThrowIfError(git_patch_get_line_in_hunk(&result, handle.NativeHandle, hunk_idx, line_idx));

        return new GitDiffLine(in *result);
    }

    public nuint GetSize(bool include_context, bool include_hunk_headers, bool include_file_headers)
    {
        var handle = this.ThrowIfNull();

        return git_patch_size(handle.NativeHandle, include_context ? 1 : 0, include_hunk_headers ? 1 : 0, include_file_headers ? 1 : 0);
    }

    public (nuint total_context, nuint total_additions, nuint total_deletions) GetLineStats()
    {
        var handle = this.ThrowIfNull();

        nuint context, additions, deletions;
        Git2.ThrowIfError(git_patch_line_stats(&context, &additions, &deletions, handle.NativeHandle));

        return (context, additions, deletions);
    }

    public override string ToString()
    {
        var handle = this.ThrowIfNull();

        Native.GitBuffer buffer = default;
        Git2.ThrowIfError(git_patch_to_buf(&buffer, handle.NativeHandle));

        try
        {
            return buffer.AsString();
        }
        finally
        {
            git_buf_dispose(&buffer);
        }
    }

    public static GitPatch FromBuffers(ReadOnlySpan<char> old_text, string? old_as_path, ReadOnlySpan<char> new_text, string? new_as_path, in GitDiffOptions options)
    {
        byte[] old_array = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetByteCount(old_text));
        try
        {
            var oldWritten = Encoding.UTF8.GetBytes(old_text, old_array);

            byte[] new_array = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetByteCount(new_text));
            try
            {
                var newWritten = Encoding.UTF8.GetBytes(new_text, new_array);

                return FromBuffers(
                    new ReadOnlySpan<byte>(old_array, 0, oldWritten), old_as_path,
                    new ReadOnlySpan<byte>(new_array, 0, newWritten), new_as_path,
                    in options);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(new_array);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(old_array);
        }
    }

    public static GitPatch FromBuffers(ReadOnlySpan<byte> old_text, string? old_as_path, ReadOnlySpan<byte> new_text, string? new_as_path, in GitDiffOptions options)
    {
        Git2.Patch* result = null;
        GitError error;

        Native.GitDiffOptions _options = default;
        List<GCHandle> gchandles = [];
        try
        {
            _options.FromManaged(in options, gchandles);

            fixed (byte* pOld = old_text, pNew = new_text)
            {
                error = git_patch_from_buffers(
                    &result,
                    pOld,
                    (nuint)old_text.Length,
                    old_as_path,
                    pNew,
                    (nuint)new_text.Length,
                    new_as_path,
                    &_options);
            }
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);

        return new(result);
    }

    public static GitPatch FromBlobs(GitBlob old_blob, string? old_as_path, GitBlob new_blob, string? new_as_path, in GitDiffOptions options)
    {
        Git2.Patch* result = null;
        GitError error;

        Native.GitDiffOptions _options = default;
        List<GCHandle> gchandles = [];
        try
        {
            _options.FromManaged(in options, gchandles);

            error = git_patch_from_blobs(
                &result,
                old_blob.NativeHandle,
                old_as_path,
                new_blob.NativeHandle,
                new_as_path,
                &_options);
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);

        return new(result);
    }

    public static GitPatch FromBlobAndBuffer(GitBlob old_blob, string? old_as_path, ReadOnlySpan<byte> new_buffer, string? new_as_path, in GitDiffOptions options)
    {
        Git2.Patch* result = null;
        GitError error;

        Native.GitDiffOptions _options = default;
        List<GCHandle> gchandles = [];
        try
        {
            _options.FromManaged(in options, gchandles);

            fixed (byte* pNew = new_buffer)
            {
                error = git_patch_from_blob_and_buffer(
                    &result,
                    old_blob.NativeHandle,
                    old_as_path,
                    pNew,
                    (nuint)new_buffer.Length,
                    new_as_path,
                    &_options);
            }
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);

        return new(result);
    }
}
