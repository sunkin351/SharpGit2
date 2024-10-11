using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Unicode;

using SharpGit2.Marshalling;

using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitCommit : IDisposable
{
    internal readonly Git2.Commit* NativeHandle;

    internal GitCommit(Git2.Commit* handle)
    {
        NativeHandle = handle;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// WARNING: It is Undefined behavior to read this property from a <see langword="default"/> instance of GitCommit. Could return a null reference if you do!
    /// </remarks>
    public ref readonly GitObjectID Id => ref *git_commit_id(this.NativeHandle);

    public GitRepository Owner => new(git_commit_owner(this.NativeHandle));

    public int ParentCount => checked((int)git_commit_parentcount(this.NativeHandle));

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// WARNING: It is Undefined behavior to read this property from a <see langword="default"/> instance of GitCommit. Could return a null reference if you do!
    /// </remarks>
    public ref readonly GitObjectID TreeId => ref *git_commit_tree_id(this.NativeHandle);

    public void Dispose()
    {
        git_commit_free(this.NativeHandle);
    }

    [SkipLocalsInit]
    public GitObjectID Amend(string? update_ref, GitSignature? author, GitSignature? committer, string? message, GitTree? tree)
    {
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
                this.NativeHandle,
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
        GitObjectID result = default;
        Native.GitSignature* _author = null, _committer = null;
        GitError error;

        try
        {
            _author = GitSignatureMarshaller.ConvertToUnmanaged(author);
            _committer = GitSignatureMarshaller.ConvertToUnmanaged(committer);

            error = git_commit_amend(
                &result,
                this.NativeHandle,
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
        return new GitSignature(in *git_commit_author(this.NativeHandle));
    }

    public GitSignature GetAuthor(GitMailmap mailMap)
    {
        Native.GitSignature* result = null;
        Git2.ThrowIfError(git_commit_author_with_mailmap(&result, this.NativeHandle, mailMap.NativeHandle));

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
        Git2.Commit* result;
        Git2.ThrowIfError(git_commit_dup(&result, this.NativeHandle));

        return new(result);
    }

    public GitTree GetTree()
    {
        Git2.Tree* result;

        Git2.ThrowIfError(git_commit_tree(&result, this.NativeHandle));

        return new(result);
    }

    public ref readonly GitObjectID GetParentID(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, this.ParentCount);

        return ref *git_commit_parent_id(this.NativeHandle, (uint)index);
    }

    public GitCommit GetParent(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, this.ParentCount);

        Git2.Commit* result;
        Git2.ThrowIfError(git_commit_parent(&result, this.NativeHandle, (uint)index));

        return new(result);
    }

    public GitObject GetObjectByPath(string path, GitObjectType type)
    {
        Git2.Object* result;
        Git2.ThrowIfError(git_object_lookup_bypath(&result, (Git2.Object*)this.NativeHandle, path, type));

        return new(result);
    }

    public bool TryGetObjectByPath(string path, GitObjectType type, out GitObject obj)
    {
        Git2.Object* result;
        var error = git_object_lookup_bypath(&result, (Git2.Object*)this.NativeHandle, path, type);

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

    public string GetMessage()
    {
        var messageNative = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(git_commit_message_raw(this.NativeHandle));

        return messageNative.IsEmpty ? "" : this.GetMessageEncoding().GetString(messageNative).TrimStart('\n');
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
        Native.GitBuffer headerBuffer = default;

        Git2.ThrowIfError(git_commit_header_field(&headerBuffer, this.NativeHandle, field));

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
        Git2.Commit* result = null;
        Git2.ThrowIfError(git_commit_nth_gen_ancestor(&result, this.NativeHandle, n));

        return new(result);
    }

    public string GetRawHeader()
    {
        return Utf8StringMarshaller.ConvertToManaged(git_commit_raw_header(this.NativeHandle))!;
    }

    public DateTimeOffset GetCommitTime()
    {
        var time = git_commit_time(this.NativeHandle);
        var offset = git_commit_time_offset(this.NativeHandle);

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
        return obj.Type == GitObjectType.Commit
            ? new GitCommit((Git2.Commit*)obj.NativeHandle)
            : throw new InvalidCastException("Git Object is not of type Commit!");
    }

    public static implicit operator GitObject(GitCommit commit)
    {
        return new((Git2.Object*)commit.NativeHandle);
    }
}
