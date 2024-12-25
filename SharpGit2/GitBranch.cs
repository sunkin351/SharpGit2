using System.Buffers;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public unsafe readonly struct GitBranch : IDisposable, IGitHandle
{
    /// <summary>
    /// Underlying reference of this branch
    /// </summary>
    public GitReference Reference { get; }

    /// <inheritdoc/>
    public bool IsNull => Reference.IsNull;

    internal GitBranch(GitReference reference)
    {
        Reference = reference;
    }

    // This constructor is for situations where we know the reference is a branch reference
    internal GitBranch(Git2.Reference* nativeHandle)
    {
        Reference = new GitReference(nativeHandle);
    }

    public void Dispose()
    {
        this.Reference.Dispose();
    }

    public bool IsCheckedOut
    {
        get
        {
            var handle = this.ThrowIfNull();

            return Git2.ErrorOrBoolean(git_branch_is_checked_out(handle.Reference.NativeHandle));
        }
    }

    public bool IsHead
    {
        get
        {
            var handle = this.ThrowIfNull();

            return Git2.ErrorOrBoolean(git_branch_is_head(handle.Reference.NativeHandle));
        }
    }

    public void Delete()
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_branch_delete(handle.Reference.NativeHandle));
    }

    /// <summary>
    /// Rename the branch
    /// </summary>
    /// <param name="new_branch_name"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    public GitBranch Rename(string new_branch_name, bool force)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* result = null;
        Git2.ThrowIfError(git_branch_move(&result, handle.Reference.NativeHandle, new_branch_name, force ? 1 : 0));

        return new(result);
    }

    public string GetBranchName()
    {
        var handle = this.ThrowIfNull();

        byte* _name = null;

        Git2.ThrowIfError(git_branch_name(&_name, handle.Reference.NativeHandle));

        return Git2.GetPooledString(_name);
    }

    public void SetUpstream(string branch_name)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_branch_set_upstream(handle.Reference.NativeHandle, branch_name));
    }

    public GitBranch GetUpstream()
    {
        var handle = this.Reference.ThrowIfNull();

        Git2.Reference* result = null;
        Git2.ThrowIfError(git_branch_upstream(&result, handle.NativeHandle));

        return new(result);
    }

    public bool TryGetUpstream(out GitBranch upstream)
    {
        var handle = this.Reference.ThrowIfNull();

        Git2.Reference* result = null;
        var error = git_branch_upstream(&result, handle.NativeHandle);

        switch (error)
        {
            case GitError.OK:
                upstream = new(result);
                return true;
            case GitError.NotFound:
                upstream = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public string? GetUpstreamName()
    {
        var referencePtr = this.ThrowIfNull().Reference.NativeHandle;

        var repoPtr = git_reference_owner(referencePtr);

        Native.GitBuffer buffer = default;
        var error = git_branch_upstream_name(&buffer, repoPtr, git_reference_name(referencePtr));

        switch (error)
        {
            case GitError.OK:
                try
                {
                    return Git2.GetPooledString(buffer.Span);
                }
                finally
                {
                    git_buf_dispose(&buffer);
                }
            case GitError.NotFound:
                return null;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    /// <summary>
    /// Retreive the upstream remote of a local branch
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public string? GetUpstreamRemote()
    {
        var referencePtr = this.Reference.ThrowIfNull().NativeHandle;

        var refName = git_reference_name(referencePtr);

        // Check this here so we can override the error given by libgit2 with our own
        if (!MemoryMarshal.CreateReadOnlySpanFromNullTerminated(refName).StartsWith("refs/heads/"u8)) // git_reference_is_branch() == 0
        {
            throw new InvalidOperationException("The given reference must be a local branch reference!");
        }

        var repoPtr = git_reference_owner(referencePtr);

        Native.GitBuffer buffer = default;
        var error = git_branch_upstream_remote(&buffer, repoPtr, refName);

        switch (error)
        {
            case GitError.OK:
                try
                {
                    return Git2.GetPooledString(buffer.Span);
                }
                finally
                {
                    git_buf_dispose(&buffer);
                }
            case GitError.NotFound:
                return null;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public string? GetUpstreamMerge()
    {
        var referencePtr = this.Reference.ThrowIfNull().NativeHandle;

        var refName = git_reference_name(referencePtr);

        // Check this here so we can override the error given by libgit2 with our own
        if (!MemoryMarshal.CreateReadOnlySpanFromNullTerminated(refName).StartsWith("refs/heads/"u8)) // git_reference_is_branch() == 0
        {
            throw new InvalidOperationException("The given reference must be a local branch reference!");
        }

        var repoPtr = git_reference_owner(referencePtr);

        Native.GitBuffer buffer = default;
        var error = git_branch_upstream_merge(&buffer, repoPtr, refName);

        switch (error)
        {
            case GitError.OK:
                try
                {
                    return Git2.GetPooledString(buffer.Span);
                }
                finally
                {
                    git_buf_dispose(&buffer);
                }
            case GitError.NotFound:
                return null;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public GitObjectID? GetTarget()
    {
        return this.Reference.GetTarget();
    }

    public GitBranch SetTarget(GitObjectID target, string? reflog_message)
    {
        return new(this.Reference.SetTarget(target, reflog_message));
    }

    public GitBranch SetTarget(in GitObjectID target, string? reflog_message)
    {
        return new(this.Reference.SetTarget(in target, reflog_message));
    }

    public ref readonly GitObjectID DangerousGetTarget()
    {
        return ref this.Reference.DangerousGetTarget();
    }

    public static explicit operator GitBranch(GitReference reference)
    {
        if (!reference.IsNull && !reference.IsBranch)
        {
            throw new InvalidCastException("GitReference isn't a branch reference!");
        }

        return new(reference);
    }

    /// <summary>
    /// Implicit conversion to a git reference
    /// </summary>
    /// <param name="branch">The branch to convert</param>
    public static implicit operator GitReference(GitBranch branch)
    {
        return branch.Reference;
    }

    public static bool IsValidBranchName(ReadOnlySpan<char> name)
    {
        if (name.IsEmpty)
        {
            return false;
        }

        if (name[0] == '-' || name.SequenceEqual("HEAD"))
        {
            return false;
        }

        const string RefsHeadsDirectory = "refs/heads/";

        var length = RefsHeadsDirectory.Length + name.Length;
        char[] array = ArrayPool<char>.Shared.Rent(length);
        try
        {
            Span<char> buffer = array;
            RefsHeadsDirectory.CopyTo(buffer);

            name.CopyTo(buffer[RefsHeadsDirectory.Length..]);

            return GitReference.IsValidReferenceName(buffer[..length]);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(array);
        }
    }
}
