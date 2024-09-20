using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitBranch : IDisposable
{
    public GitReference Reference { get; }

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

    public bool IsCheckedOut => Git2.ErrorOrBoolean(git_branch_is_checked_out(this.Reference.NativeHandle));

    public bool IsHead => Git2.ErrorOrBoolean(git_branch_is_head(this.Reference.NativeHandle));

    public void Delete()
    {
        Git2.ThrowIfError(git_branch_delete(this.Reference.NativeHandle));
    }

    // rename
    public GitBranch Move(string new_branch_name, bool force)
    {
        Git2.Reference* result;
        Git2.ThrowIfError(git_branch_move(&result, this.Reference.NativeHandle, new_branch_name, force ? 1 : 0));

        return new(result);
    }

    public string GetBranchName()
    {
        byte* _name = null;

        Git2.ThrowIfError(git_branch_name(&_name, this.Reference.NativeHandle));

        return Utf8StringMarshaller.ConvertToManaged(_name)!;
    }

    public void SetUpstream(string branch_name)
    {
        Git2.ThrowIfError(git_branch_set_upstream(this.Reference.NativeHandle, branch_name));
    }

    public GitBranch GetUpstream()
    {
        Git2.Reference* result;
        Git2.ThrowIfError(git_branch_upstream(&result, this.Reference.NativeHandle));

        return new(result);
    }

    public static explicit operator GitBranch(GitReference reference)
    {
        if (!reference.IsBranch)
        {
            throw new InvalidCastException("GitReference isn't a branch reference!");
        }

        return new(reference);
    }
}
