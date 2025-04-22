using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

using SharpGit2.Marshalling;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public unsafe readonly struct GitRebase(Git2.Rebase* nativeHandle) : IGitHandle
{
    public Git2.Rebase* NativeHandle { get; } = nativeHandle;

    public bool IsNull => NativeHandle == null;

    public void Dispose()
    {
        git_rebase_free(this.NativeHandle);
    }

    public ref readonly GitObjectID OriginalHeadID
    {
        get
        {
            var handle = this.ThrowIfNull();

            return ref *git_rebase_orig_head_id(handle.NativeHandle);
        }
    }

    public ref readonly GitObjectID OntoID
    {
        get
        {
            var handle = this.ThrowIfNull();

            return ref *git_rebase_onto_id(handle.NativeHandle);
        }
    }

    public string OriginalHeadName
    {
        get
        {
            var handle = this.ThrowIfNull();

            return Git2.GetPooledString(git_rebase_orig_head_name(handle.NativeHandle));
        }
    }

    public string OntoName
    {
        get
        {
            var handle = this.ThrowIfNull();

            return Git2.GetPooledString(git_rebase_onto_name(handle.NativeHandle));
        }
    }

    public nuint OperationCount
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_rebase_operation_entrycount(handle.NativeHandle);
        }
    }

    public nuint CurrentOperation
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_rebase_operation_current(handle.NativeHandle);
        }
    }

    public GitRebaseOperation this[nuint idx]
    {
        get
        {
            var handle = this.ThrowIfNull().NativeHandle;

            var count = git_rebase_operation_entrycount(handle);

            if (idx >= count)
            {
                throw new IndexOutOfRangeException();
            }

            var op = git_rebase_operation_byindex(handle, idx);

            return new(in *op);
        }
    }

    /// <summary>
    /// Move to the next operation to apply.
    /// </summary>
    /// <param name="operation">The details of the operation</param>
    /// <returns><see langword="true"/> if there's another operation to process, <see langword="false"/> otherwise.</returns>
    public bool Next(out GitRebaseOperation operation)
    {
        var handle = this.ThrowIfNull();

        Native.GitRebaseOperation* nextOp = null;
        var error = git_rebase_next(&nextOp, handle.NativeHandle);

        if (error == GitError.OK)
        {
            operation = new(in *nextOp);
            return true;
        }
        else if (error == GitError.IterationOver)
        {
            operation = default;
            return false;
        }
        else
        {
            throw Git2.ExceptionForError(error);
        }
    }

    public GitIndex GetInMemoryIndex()
    {
        var handle = this.ThrowIfNull();

        Git2.Index* index = null;
        Git2.ThrowIfError(git_rebase_inmemory_index(&index, handle.NativeHandle));

        return new(index);
    }

    public GitObjectID Commit(GitSignature committer, GitSignature? author = null, string? message = null)
    {
        var handle = this.ThrowIfNull();

        Native.GitSignature* committerSig = null, authorSig = null;
        GitObjectID commitId = default;
        scoped Utf8StringMarshaller.ManagedToUnmanagedIn _message = default;

        try
        {
            committerSig = GitSignatureMarshaller.ConvertToUnmanaged(committer);
            authorSig = GitSignatureMarshaller.ConvertToUnmanaged(author);
            _message.FromManaged(message, stackalloc byte[Utf8StringMarshaller.ManagedToUnmanagedIn.BufferSize]);

            Git2.ThrowIfError(git_rebase_commit(&commitId, handle.NativeHandle, authorSig, committerSig, null, _message.ToUnmanaged()));
        }
        finally
        {
            GitSignatureMarshaller.Free(committerSig);
            GitSignatureMarshaller.Free(authorSig);
            _message.Free();
        }

        return commitId;
    }

    public void Abort()
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_rebase_abort(handle.NativeHandle));
    }

    public void Finish(GitSignature? committer = null)
    {
        var handle = this.ThrowIfNull();

        Native.GitSignature* committerSig = null;
        try
        {
            committerSig = GitSignatureMarshaller.ConvertToUnmanaged(committer);

            Git2.ThrowIfError(git_rebase_finish(handle.NativeHandle, committerSig));
        }
        finally
        {
            GitSignatureMarshaller.Free(committerSig);
        }
    }
}
