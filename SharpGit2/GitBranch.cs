using System.Buffers;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public unsafe readonly struct GitBranch : IDisposable, IGitHandle
{
    /// <summary>
    /// Underlying reference of this branch
    /// </summary>
    public GitReference Reference { get; }

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
        var handle = this.ThrowIfNull();

        Git2.Reference* result = null;
        Git2.ThrowIfError(git_branch_upstream(&result, handle.Reference.NativeHandle));

        return new(result);
    }

    public GitObjectID? GetTarget()
    {
        var handle = this.ThrowIfNull();

        return handle.Reference.GetTarget();
    }

    public GitBranch SetTarget(GitObjectID target, string? reflog_message)
    {
        var handle = this.ThrowIfNull();

        return new(handle.Reference.SetTarget(target, reflog_message));
    }

    public GitBranch SetTarget(in GitObjectID target, string? reflog_message)
    {
        var handle = this.ThrowIfNull();

        return new(handle.Reference.SetTarget(in target, reflog_message));
    }

    public ref readonly GitObjectID DangerousGetTarget()
    {
        var handle = this.ThrowIfNull();

        return ref handle.Reference.DangerousGetTarget();
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
