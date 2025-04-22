using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Unicode;

using SharpGit2.Marshalling;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public unsafe readonly struct GitCommit(Git2.Commit* handle) : IDisposable, IGitObject<GitCommit>, IGitHandle
{
    public Git2.Commit* NativeHandle { get; } = handle;

    public bool IsNull => this.NativeHandle == null;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// WARNING: It is Undefined behavior to read this property from a <see langword="default"/> instance of GitCommit. Could return a null reference if you do!
    /// </remarks>
    public ref readonly GitObjectID Id
    {
        get
        {
            var handle = this.ThrowIfNull();

            return ref *git_commit_id(handle.NativeHandle);
        }
    }

    public GitRepository Owner
    {
        get
        {
            var handle = this.ThrowIfNull();

            return new(git_commit_owner(handle.NativeHandle));
        }
    }

    public int ParentCount
    {
        get
        {
            var handle = this.ThrowIfNull();

            return checked((int)git_commit_parentcount(handle.NativeHandle));
        }
    }

    public ref readonly GitObjectID TreeId
    {
        get
        {
            var handle = this.ThrowIfNull();

            return ref *git_commit_tree_id(handle.NativeHandle);
        }
    }

    public void Dispose()
    {
        git_commit_free(this.NativeHandle);
    }

    [SkipLocalsInit]
    public GitObjectID Amend(string? update_ref, GitSignature? author, GitSignature? committer, string? message, GitTree? tree)
    {
        var handle = this.ThrowIfNull();

        GitObjectID result = default;
        scoped Utf8StringMarshaller.ManagedToUnmanagedIn _message = default;
        Native.GitSignature* _author = null, _committer = null;

        GitError error;
        try
        {
            _author = GitSignatureMarshaller.ConvertToUnmanaged(author);
            _committer = GitSignatureMarshaller.ConvertToUnmanaged(committer);
            _message.FromManaged(message, stackalloc byte[256]);

            error = git_commit_amend(
                &result,
                handle.NativeHandle,
                update_ref,
                _author,
                _committer,
                (string?)null,
                _message.ToUnmanaged(),
                tree.GetValueOrDefault().NativeHandle);
        }
        finally
        {
            GitSignatureMarshaller.Free(_author);
            GitSignatureMarshaller.Free(_committer);
            _message.Free();
        }

        Git2.ThrowIfError(error);
        return result;
    }

    public GitObjectID Amend(string? update_ref, GitSignature? author, GitSignature? committer, Encoding? messageEncoding, string? message, GitTree? tree)
    {
        var handle = this.ThrowIfNull();

        GitObjectID result = default;
        Native.GitSignature* _author = null, _committer = null;
        GitError error;

        try
        {
            _author = GitSignatureMarshaller.ConvertToUnmanaged(author);
            _committer = GitSignatureMarshaller.ConvertToUnmanaged(committer);

            error = git_commit_amend(
                &result,
                handle.NativeHandle,
                update_ref,
                _author,
                _committer,
                messageEncoding,
                message,
                tree.GetValueOrDefault().NativeHandle);
        }
        finally
        {
            GitSignatureMarshaller.Free(_author);
            GitSignatureMarshaller.Free(_committer);
        }

        Git2.ThrowIfError(error);
        return result;
    }

    public GitSignature GetAuthor()
    {
        var handle = this.ThrowIfNull();

        return new GitSignature(in *git_commit_author(handle.NativeHandle));
    }

    public GitSignature GetAuthor(GitMailmap mailMap)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfNullArgument(mailMap);

        Native.GitSignature* result = null;
        Git2.ThrowIfError(git_commit_author_with_mailmap(&result, handle.NativeHandle, mailMap.NativeHandle));

        try
        {
            return new GitSignature(in *result);
        }
        finally
        {
            git_signature_free(result);
        }
    }

    public GitSignature GetCommitter()
    {
        var handle = this.ThrowIfNull();

        return new GitSignature(in *git_commit_committer(handle.NativeHandle));
    }

    public GitSignature GetCommitter(GitMailmap mailMap)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfNullArgument(mailMap);

        Native.GitSignature* result = null;
        Git2.ThrowIfError(git_commit_committer_with_mailmap(&result, handle.NativeHandle, mailMap.NativeHandle));

        try
        {
            return new GitSignature(in *result);
        }
        finally
        {
            git_signature_free(result);
        }
    }

    public GitCommit Duplicate()
    {
        var handle = this.ThrowIfNull();

        Git2.Commit* result = null;
        Git2.ThrowIfError(git_commit_dup(&result, handle.NativeHandle));

        return new(result);
    }

    public GitTree GetTree()
    {
        var handle = this.ThrowIfNull();

        Git2.Tree* result = null;

        Git2.ThrowIfError(git_commit_tree(&result, handle.NativeHandle));

        return new(result);
    }

    public ref readonly GitObjectID GetParentID(int index)
    {
        var handle = this.ThrowIfNull();

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, handle.ParentCount);

        return ref *git_commit_parent_id(handle.NativeHandle, (uint)index);
    }

    public GitCommit GetParent(int index)
    {
        var handle = this.ThrowIfNull();

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, handle.ParentCount);

        Git2.Commit* result = null;
        Git2.ThrowIfError(git_commit_parent(&result, handle.NativeHandle, (uint)index));

        return new(result);
    }

    public GitObject GetObjectByPath(string path, GitObjectType type)
    {
        var handle = this.ThrowIfNull();

        if (!Enum.IsDefined(type) || type == GitObjectType.Invalid)
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        Git2.Object* result = null;
        Git2.ThrowIfError(git_object_lookup_bypath(&result, (Git2.Object*)handle.NativeHandle, path, type));

        return new(result);
    }

    public TObject GetObjectByPath<TObject>(string path)
        where TObject : struct, IGitHandle, IGitObject<TObject>
    {
        var handle = this.ThrowIfNull();

        Git2.Object* result = null;
        Git2.ThrowIfError(git_object_lookup_bypath(&result, (Git2.Object*)handle.NativeHandle, path, TObject.ObjectType));

        return TObject.FromObjectPointer(result);
    }

    public bool TryGetObjectByPath(string path, GitObjectType type, out GitObject obj)
    {
        var handle = this.ThrowIfNull();

        if (!Enum.IsDefined(type) || type == GitObjectType.Invalid)
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        Git2.Object* result = null;
        var error = git_object_lookup_bypath(&result, (Git2.Object*)handle.NativeHandle, path, type);

        switch (error)
        {
            case GitError.OK:
                obj = new(result);
                return true;
            case GitError.NotFound:
                obj = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public bool TryGetObjectByPath<TObject>(string path, out TObject obj)
        where TObject : struct, IGitHandle, IGitObject<TObject>
    {
        var handle = this.ThrowIfNull();

        Git2.Object* result = null;
        var error = git_object_lookup_bypath(&result, (Git2.Object*)handle.NativeHandle, path, TObject.ObjectType);

        switch (error)
        {
            case GitError.OK:
                obj = TObject.FromObjectPointer(result);
                return true;
            case GitError.NotFound:
                obj = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public string GetMessage()
    {
        var handle = this.ThrowIfNull();

        var messageNative = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(git_commit_message_raw(handle.NativeHandle));

        return messageNative.IsEmpty ? "" : handle.GetMessageEncoding().GetString(messageNative).TrimStart('\n');
    }

    public string GetBody()
    {
        // This is a managed implementation of git_commit_body().
        // The only downside is that it doesn't cache the result, unlike the original.
        // Bypasses an issue where the original doesn't respect the character encoding of the message when parsing.

        string message = this.GetMessage();

        int lineStart = 0;
        int lineEnd;

        // The algorithm was modified from the original
        // in order to be consistent with git_commit_summary() and GetSummary()
        while (true)
        {
            lineEnd = message.IndexOf('\n', lineStart);
            if (lineEnd < 0)
            {
                lineEnd = message.Length;
            }

            ReadOnlySpan<char> line = message.AsSpan(lineStart, lineEnd - lineStart);

            if (line.IsWhiteSpace()) // equivalent to string.IsNullOrWhitespace()
                break;

            if (lineEnd == message.Length)
                break;

            lineStart = lineEnd + 1;
        }

        return message.AsSpan(lineEnd).Trim().ToString();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>
    /// The commit summary (the first paragraph of the commit message)
    /// </returns>
    public string GetSummary()
    {
        // This is a managed implementation of git_commit_summary().
        // The only downside is that it doesn't cache the result, unlike the original.
        // Bypasses an issue where the original doesn't respect the character encoding of the message when parsing.

        var message = this.GetMessage();
        if (string.IsNullOrEmpty(message))
            return "";

        var builder = new StringBuilder();

        int lineStart = 0;
        int lineEnd;

        while (true)
        {
            lineEnd = message.IndexOf('\n', lineStart);
            if (lineEnd < 0)
            {
                lineEnd = message.Length;
            }

            ReadOnlySpan<char> line = message.AsSpan(lineStart, lineEnd - lineStart).Trim();

            if (line.IsEmpty)
                break;

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(line);

            if (lineEnd == message.Length)
                break;

            lineStart = lineEnd + 1;
        }

        return builder.ToString();
    }

    public string GetHeaderField(string field)
    {
        var handle = this.ThrowIfNull();

        Native.GitBuffer headerBuffer = default;

        Git2.ThrowIfError(git_commit_header_field(&headerBuffer, handle.NativeHandle, field));

        try
        {
            return headerBuffer.AsString();
        }
        finally
        {
            git_buf_dispose(&headerBuffer);
        }
    }

    public GitCommit GetNthGenerationAncestor(uint n)
    {
        var handle = this.ThrowIfNull();

        Git2.Commit* result = null;
        Git2.ThrowIfError(git_commit_nth_gen_ancestor(&result, handle.NativeHandle, n));

        return new(result);
    }

    public string GetRawHeader()
    {
        var handle = this.ThrowIfNull();

        return Utf8StringMarshaller.ConvertToManaged(git_commit_raw_header(handle.NativeHandle))!;
    }

    public DateTimeOffset GetCommitTime()
    {
        var handle = this.ThrowIfNull();

        var time = git_commit_time(handle.NativeHandle);
        var offset = git_commit_time_offset(handle.NativeHandle);

        return (DateTimeOffset)new GitTime(time, offset);
    }

    private Encoding GetMessageEncoding()
    {
        var encodingName = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(git_commit_message_encoding(this.NativeHandle));

        return IsUtf8(encodingName) ? Encoding.UTF8 : Encoding.GetEncoding(Encoding.UTF8.GetString(encodingName));

        static bool IsUtf8(ReadOnlySpan<byte> encodingName)
        {
            const string Utf8Name = "UTF-8";

            if (encodingName.IsEmpty)
                return true;

            Span<char> span = stackalloc char[Utf8Name.Length];

            return Utf8.ToUtf16(encodingName, span, out _, out int written) == OperationStatus.Done
                && written == span.Length
                && ((ReadOnlySpan<char>)span).Equals(Utf8Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static explicit operator GitCommit(GitObject obj)
    {
        return obj.IsNull || obj.Type == GitObjectType.Commit
            ? new GitCommit((Git2.Commit*)obj.NativeHandle)
            : throw new InvalidCastException("Git Object is not of type Commit!");
    }

    public static implicit operator GitObject(GitCommit commit)
    {
        return new((Git2.Object*)commit.NativeHandle);
    }

    Git2.Object* IGitObject<GitCommit>.NativeHandle => (Git2.Object*)this.NativeHandle;

    static GitCommit IGitObject<GitCommit>.FromObjectPointer(Git2.Object* obj)
    {
        return new((Git2.Commit*)obj);
    }

    static GitObjectType IGitObject<GitCommit>.ObjectType => GitObjectType.Commit;
}
