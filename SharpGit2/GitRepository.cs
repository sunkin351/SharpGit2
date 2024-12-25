using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

using SharpGit2.Marshalling;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

/// <summary>
/// Representation of an existing git repository, including all its object contents
/// </summary>
/// <param name="handle">The native object pointer</param>
public unsafe readonly partial struct GitRepository(Git2.Repository* handle) : IDisposable, IGitHandle
{
    /// <summary>
    /// The native object pointer
    /// </summary>
    public Git2.Repository* NativeHandle { get; } = handle;

    /// <summary>
    /// Is this instance null? e.g. <c>NativeHandle == null</c>
    /// </summary>
    public bool IsNull => this.NativeHandle == null;

    /// <summary>
    /// Is this a bare repository?
    /// </summary>
    public bool IsBare
    {
        get
        {
            var code = git_repository_is_bare(this.ThrowIfNull().NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    /// <summary>
    /// Is this repository empty?
    /// </summary>
    /// <remarks>
    /// An empty repository has just been initialized and contains no references
    /// apart from HEAD, which must be pointing to the unborn master branch,
    /// or the branch specified for the repository in the <see cref="GitRepositoryInitOptions.InitialHead"/> configuration variable.
    /// </remarks>
    public bool IsEmpty
    {
        get
        {
            return Git2.ErrorOrBoolean(git_repository_is_empty(this.ThrowIfNull().NativeHandle));
        }
    }

    /// <summary>
    /// Is this repository's HEAD detached?
    /// </summary>
    /// <remarks>
    /// A repository's HEAD is detached when it points directly to a commit instead of a branch.
    /// </remarks>
    public bool IsHeadDetached
    {
        get
        {
            return Git2.ErrorOrBoolean(git_repository_head_detached(this.ThrowIfNull().NativeHandle));
        }
    }

    /// <summary>
    /// Is the current branch unborn?
    /// </summary>
    /// <remarks>
    /// An unborn branch is one named from HEAD but which doesn't exist
    /// in the refs namespace, because it doesn't have any commit to point to.
    /// </remarks>
    public bool IsHeadUnborn
    {
        get
        {
            return Git2.ErrorOrBoolean(git_repository_head_unborn(this.ThrowIfNull().NativeHandle));
        }
    }

    /// <summary>
    /// Is this repository a shallow clone?
    /// </summary>
    public bool IsShallow
    {
        get
        {
            return git_repository_is_shallow(this.ThrowIfNull().NativeHandle) != 0;
        }
    }

    /// <summary>
    /// Is this repository a linked work tree?
    /// </summary>
    public bool IsWorktree
    {
        get
        {
            return git_repository_is_worktree(this.ThrowIfNull().NativeHandle) != 0;
        }
    }

    /// <summary>
    /// The current state of this repository.
    /// </summary>
    public GitRepositoryState State
    {
        get
        {
            return git_repository_state(this.ThrowIfNull().NativeHandle);
        }
    }

    /// <summary>
    /// Free's this Git Repository
    /// </summary>
    /// <remarks>
    /// Do not call this twice!
    /// </remarks>
    public void Dispose()
    {
        git_repository_free(this.NativeHandle);
    }

    #region Annotated Commits
    /// <summary>
    /// Create an annotated commit from the given fetch head data.
    /// </summary>
    /// <param name="branchName">name of the (remote) branch</param>
    /// <param name="remoteUrl">url of the remote</param>
    /// <param name="id">the commit object id of the remote branch</param>
    /// <returns>The resulting Annotated Commit</returns>
    /// <exception cref="Git2Exception"/>
    public GitAnnotatedCommit GetAnnotatedCommitFromFetchHead(string branchName, string remoteUrl, GitObjectID id)
    {
        var handle = this.ThrowIfNull();

        Git2.AnnotatedCommit* result = null;
        Git2.ThrowIfError(git_annotated_commit_from_fetchhead(&result, handle.NativeHandle, branchName, remoteUrl, &id));

        return new(result);
    }

    /// <inheritdoc cref="GetAnnotatedCommitFromFetchHead(string, string, GitObjectID)"/>
    public GitAnnotatedCommit GetAnnotatedCommitFromFetchHead(string branchName, string remoteUrl, in GitObjectID id)
    {
        var handle = this.ThrowIfNull();

        Git2.AnnotatedCommit* result = null;
        Git2.ThrowIfError(git_annotated_commit_from_fetchhead(&result, handle.NativeHandle, branchName, remoteUrl, in id));

        return new(result);
    }

    /// <summary>
    /// Creates an annotated commit from the given reference.
    /// </summary>
    /// <param name="reference">Reference to lookup the annotated commit</param>
    /// <returns>The resulting Annotated Commit</returns>
    /// <exception cref="Git2Exception"/>
    public GitAnnotatedCommit GetAnnotatedCommitFromReference(GitReference reference)
    {
        var handle = this.ThrowIfNull();

        Git2.AnnotatedCommit* result = null;
        Git2.ThrowIfError(git_annotated_commit_from_ref(&result, handle.NativeHandle, reference.NativeHandle));

        return new(result);
    }

    /// <summary>
    /// Creates an annotated commit from a revision string.
    /// </summary>
    /// <param name="revspec">The extended SHA syntax string to use to lookup the commit</param>
    /// <returns>The resulting Annotated Commit</returns>
    /// <exception cref="Git2Exception"/>
    public GitAnnotatedCommit GetAnnotatedCommitFromRevspec(string revspec)
    {
        var handle = this.ThrowIfNull();

        Git2.AnnotatedCommit* result = null;
        Git2.ThrowIfError(git_annotated_commit_from_revspec(&result, handle.NativeHandle, revspec: revspec));

        return new(result);
    }

    /// <summary>
    /// Creates an annotated commit from the given id.
    /// </summary>
    /// <param name="id">The commit object id to lookup</param>
    /// <returns>The resulting Annotated Commit</returns>
    /// <exception cref="Git2Exception"/>
    public GitAnnotatedCommit GetAnnotatedCommit(GitObjectID id)
    {
        var handle = this.ThrowIfNull();

        Git2.AnnotatedCommit* result = null;
        Git2.ThrowIfError(git_annotated_commit_lookup(&result, handle.NativeHandle, &id));

        return new(result);
    }

    /// <inheritdoc cref="GetAnnotatedCommit(GitObjectID)"/>
    public GitAnnotatedCommit GetAnnotatedCommit(in GitObjectID id)
    {
        var handle = this.ThrowIfNull();

        Git2.AnnotatedCommit* result = null;
        Git2.ThrowIfError(git_annotated_commit_lookup(&result, handle.NativeHandle, in id));

        return new(result);
    }

    /// <summary>
    /// Attempts to create an annotated commit from the given id. 
    /// </summary>
    /// <param name="id">The commit object id to lookup</param>
    /// <param name="commit">The resulting commit, or <see langword="default"/> if false is returned.</param>
    /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise.</returns>
    public bool TryGetAnnotatedCommit(GitObjectID id, out GitAnnotatedCommit commit)
    {
        var handle = this.ThrowIfNull();

        Git2.AnnotatedCommit* result = null;
        var error = git_annotated_commit_lookup(&result, handle.NativeHandle, &id);

        switch (error)
        {
            case GitError.OK:
                commit = new(result);
                return true;
            case GitError.NotFound:
                commit = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    /// <inheritdoc cref="TryGetAnnotatedCommit(GitObjectID, out GitAnnotatedCommit)"/>
    public bool TryGetAnnotatedCommit(in GitObjectID id, out GitAnnotatedCommit commit)
    {
        var handle = this.ThrowIfNull();

        Git2.AnnotatedCommit* result = null;
        var error = git_annotated_commit_lookup(&result, handle.NativeHandle, in id);

        switch (error)
        {
            case GitError.OK:
                commit = new(result);
                return true;
            case GitError.NotFound:
                commit = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }
    #endregion

    #region Apply
    /// <summary>
    /// Apply a <see cref="GitDiff"/> to the given repository, making changes directly in the working directory, the index, or both.
    /// </summary>
    /// <param name="diff">The diff to apply</param>
    /// <param name="location">The location to apply (workdir, index or both)</param>
    public void Apply(GitDiff diff, GitApplyLocationType location)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_apply(handle.NativeHandle, diff.NativeHandle, location, null));
    }

    /// <summary>
    /// Apply a <see cref="GitDiff"/> to the given repository, making changes directly in the working directory, the index, or both.
    /// </summary>
    /// <param name="diff">The diff to apply</param>
    /// <param name="location">The location to apply (workdir, index or both)</param>
    /// <param name="options">The options for the apply</param>
    public void Apply(GitDiff diff, GitApplyLocationType location, in GitApplyOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_apply(handle.NativeHandle, diff.NativeHandle, location, in options));
    }

    /// <summary>
    /// Apply a <see cref="GitDiff"/> to a <see cref="GitTree"/>, and return the resulting image as an Index.
    /// </summary>
    /// <param name="preimage">The tree to apply the diff to</param>
    /// <param name="diff">The diff to apply</param>
    /// <returns>The postimage of the application</returns>
    public GitIndex ApplyToTree(GitTree preimage, GitDiff diff)
    {
        var handle = this.ThrowIfNull();

        Git2.Index* result = null;

        Git2.ThrowIfError(git_apply_to_tree(&result, handle.NativeHandle, preimage.NativeHandle, diff.NativeHandle, null));

        return new(result);
    }

    /// <summary>
    /// Apply a <see cref="GitDiff"/> to a <see cref="GitTree"/>, and return the resulting image as an Index.
    /// </summary>
    /// <param name="preimage">The tree to apply the diff to</param>
    /// <param name="diff">The diff to apply</param>
    /// <param name="options">The options for the apply</param>
    /// <returns>The postimage of the application</returns>
    public GitIndex ApplyToTree(GitTree preimage, GitDiff diff, in GitApplyOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.Index* result = null;

        Git2.ThrowIfError(git_apply_to_tree(&result, handle.NativeHandle, preimage.NativeHandle, diff.NativeHandle, in options));

        return new(result);
    }
    #endregion

    #region Attributes
    /// <summary>
    /// Add a macro definition.
    /// </summary>
    /// <param name="name">The name of the macro.</param>
    /// <param name="values">The value for the macro.</param>
    /// <remarks>
    /// Macros will automatically be loaded from the top level .gitattributes file of the repository (plus the built-in "binary" macro).
    /// This function allows you to add others. For example, to add the default macro, you would call:
    /// <code>
    /// repository.AttributeAddMacro("binary", "-diff crlf");
    /// </code>
    /// </remarks>
    public void AttributeAddMacro(string name, string values)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_attr_add_macro(handle.NativeHandle, name, values));
    }

    /// <summary>
    /// Flush the gitattributes cache.
    /// </summary>
    /// <remarks>
    /// Call this if you have reason to believe that the attributes files on disk no longer match the cached contents of memory.
    /// This will cause the attributes files to be reloaded the next time that an attribute access function is called.
    /// </remarks>
    public void AttributeCacheFlush()
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_attr_cache_flush(handle.NativeHandle));
    }

    /// <summary>
    /// Loop over all the git attributes for a path.
    /// </summary>
    /// <param name="flags"></param>
    /// <param name="path">
    /// Path inside the repo to check attributes. This does not have to exist, but if it does not,
    /// then it will be treated as a plain file (i.e. not a directory).
    /// </param>
    /// <param name="callbacks">Function to invoke on each attribute name and value</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AttributeForEach(GitAttributeCheckFlags flags, string path, ForeachAttributeCallback callbacks)
    {
        var handle = this.ThrowIfNull();

        Git2.CallbackContext<ForeachAttributeCallback> context = new(callbacks);

        var error = git_attr_foreach(handle.NativeHandle, flags, path, &AttributeForeachCallback, (nint)(void*)&context);

        context.ExceptionInfo?.Throw();
        if (error < 0)
            Git2.ThrowError((GitError)error);
    }

    /// <summary>
    /// Loop over all the git attributes for a path with extended options.
    /// </summary>
    /// <param name="options">The options to use when querying these attributes.</param>
    /// <param name="path">
    /// Path inside the repo to check attributes. This does not have to exist, but if it does not,
    /// then it will be treated as a plain file (i.e. not a directory).
    /// </param>
    /// <param name="callbacks">Function to invoke on each attribute name and value</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AttributeForEach(in GitAttributeOptions options, string path, ForeachAttributeCallback callbacks)
    {
        var handle = this.ThrowIfNull();

        Git2.CallbackContext<ForeachAttributeCallback> context = new(callbacks);
        Native.GitAttributeOptions nOptions = default;
        GitError error;

        try
        {
            nOptions.FromManaged(in options);

            error = (GitError)git_attr_foreach_ext(handle.NativeHandle, &nOptions, path, &AttributeForeachCallback, (nint)(void*)&context);
        }
        finally
        {
            nOptions.Free();
        }

        context.ExceptionInfo?.Throw();
        Git2.ThrowIfError(error);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int AttributeForeachCallback(byte* name, byte* value, nint payload)
    {
        ref var context = ref *(Git2.CallbackContext<ForeachAttributeCallback>*)payload;

        try
        {
            string mName = Git2.GetPooledString(name);

            var type = git_attr_value(value);

            GitAttributeValue mValue = new(type, type is GitAttributeValueType.String ? Utf8StringMarshaller.ConvertToManaged(value) : null);

            bool breakLoop = false;
            context.Callback(mName, mValue, ref breakLoop);

            return breakLoop ? 1 : 0;
        }
        catch (Exception e)
        {
            context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);
            return -1;
        }
    }

    /// <summary>
    /// Lookup the value for one git attribute for <paramref name="path"/>.
    /// </summary>
    /// <param name="flags"></param>
    /// <param name="path">
    /// The path to check for attributes. Relative paths are
    /// interpreted relative to the repo root. The file does
    /// not have to exist, but if it does not, then it will
    /// be treated as a plain file (not a directory).
    /// </param>
    /// <param name="name">The name of the attribute to look up</param>
    /// <returns></returns>
    public GitAttributeValue GetAttribute(GitAttributeCheckFlags flags, string path, string name)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_attr_get(out GitAttributeValue value, handle.NativeHandle, flags, path, name));

        return value;
    }

    public GitAttributeValue GetAttribute(in GitAttributeOptions options, string path, string name)
    {
        var handle = this.ThrowIfNull();

        Native.GitAttributeOptions nOptions = default;
        try
        {
            nOptions.FromManaged(in options);

            Git2.ThrowIfError(git_attr_get_ext(out GitAttributeValue value, handle.NativeHandle, &nOptions, path, name));

            return value;
        }
        finally
        {
            nOptions.Free();
        }
    }

    public GitAttributeValue[] GetAttributes(GitAttributeCheckFlags flags, string path, IEnumerable<string> names)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_attr_get_many(out var values, handle.NativeHandle, flags, path, names));

        return values;
    }

    public GitAttributeValue[] GetAttributes(in GitAttributeOptions options, string path, IEnumerable<string> names)
    {
        var handle = this.ThrowIfNull();

        Native.GitAttributeOptions nOptions = default;
        try
        {
            nOptions.FromManaged(in options);

            Git2.ThrowIfError(git_attr_get_many_ext(out var values, handle.NativeHandle, &nOptions, path, names));

            return values;
        }
        finally
        {
            nOptions.Free();
        }
    }

    public delegate void ForeachAttributeCallback(string name, GitAttributeValue value, ref bool breakLoop);
    #endregion

    #region Blame
    public GitBlame GetBlame(string filePath)
    {
        var handle = this.ThrowIfNull();

        Git2.Blame* result = null;
        Git2.ThrowIfError(git_blame_file(&result, handle.NativeHandle, filePath, null));
        return new(result);
    }

    public GitBlame GetBlame(string filePath, in GitBlameOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.Blame* result = null;
        GitError error;

        Native.GitBlameOptions nOptions = default;
        List<GCHandle> gchandles = [];
        try
        {
            nOptions.FromManaged(in options, gchandles);

            error = git_blame_file(&result, handle.NativeHandle, filePath, &nOptions);
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            nOptions.Free();
        }

        Git2.ThrowIfError(error);
        return new(result);
    }
    #endregion

    #region Blob
    public GitObjectID CreateBlobFromBuffer(ReadOnlySpan<byte> buffer)
    {
        var handle = this.ThrowIfNull();

        GitObjectID result = default;
        GitError error;

        fixed (byte* _buffer = buffer)
        {
            error = git_blob_create_from_buffer(&result, handle.NativeHandle, _buffer, (nuint)buffer.Length);
        }

        Git2.ThrowIfError(error);
        return result;
    }

    public GitObjectID CreateBlobFromDisk(string path)
    {
        var handle = this.ThrowIfNull();

        GitObjectID result = default;

        Git2.ThrowIfError(git_blob_create_from_disk(&result, handle.NativeHandle, path));

        return result;
    }

    public GitObjectID CreateBlobFromWorkingDirectory(string relativePath)
    {
        var handle = this.ThrowIfNull();

        GitObjectID result = default;

        Git2.ThrowIfError(git_blob_create_from_workdir(&result, handle.NativeHandle, relativePath));

        return result;
    }

    public GitBlobWriteStream CreateBlobFromStream(string? hintpath = null)
    {
        var handle = this.ThrowIfNull();

        Native.GitWriteStream* resultStream = null;

        Git2.ThrowIfError(git_blob_create_from_stream(&resultStream, handle.NativeHandle, hintpath));

        return new GitBlobWriteStream(resultStream);
    }

    public GitBlob GetBlob(in GitObjectID id)
    {
        var handle = this.ThrowIfNull();

        Git2.Blob* result = null;
        GitError error;

        fixed (GitObjectID* pId = &id)
        {
            error = git_blob_lookup(&result, handle.NativeHandle, pId);
        }

        Git2.ThrowIfError(error);
        return new(result);
    }

    public GitBlob GetBlob(in GitObjectID id, uint length)
    {
        var handle = this.ThrowIfNull();

        Git2.Blob* result = null;
        GitError error;

        fixed (GitObjectID* pId = &id)
        {
            error = git_blob_lookup_prefix(&result, handle.NativeHandle, pId, length);
        }

        Git2.ThrowIfError(error);
        return new(result);
    }

    public bool TryGetBlob(in GitObjectID id, out GitBlob blob)
    {
        var handle = this.ThrowIfNull();

        Git2.Blob* result = null;
        GitError error;

        fixed (GitObjectID* pId = &id)
        {
            error = git_blob_lookup(&result, handle.NativeHandle, pId);
        }

        switch (error)
        {
            case GitError.OK:
                blob = new(result);
                return true;
            case GitError.NotFound:
                blob = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public bool TryGetBlob(in GitObjectID id, uint length, out GitBlob blob)
    {
        var handle = this.ThrowIfNull();

        Git2.Blob* result = null;
        GitError error;

        fixed (GitObjectID* pId = &id)
        {
            error = git_blob_lookup_prefix(&result, handle.NativeHandle, pId, length);
        }

        switch (error)
        {
            case GitError.OK:
                blob = new(result);
                return true;
            case GitError.NotFound:
            case GitError.Ambiguous:
                blob = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }
    #endregion

    #region Branch
    public GitBranch CreateBranch(string branchName, GitCommit target, bool force)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* result = null;
        Git2.ThrowIfError(git_branch_create(&result, handle.NativeHandle, branchName, target.NativeHandle, force ? 1 : 0));

        return new(result);
    }

    public GitBranch CreateBranch(string branchName, GitAnnotatedCommit target, bool force)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* result = null;
        Git2.ThrowIfError(git_branch_create_from_annotated(&result, handle.NativeHandle, branchName, target.NativeHandle, force ? 1 : 0));

        return new(result);
    }

    /// <summary>
    /// Remember to free all returned objects yourself!
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public BranchEnumerable EnumerateBranches(GitBranchType filter)
    {
        var handle = this.ThrowIfNull();

        return new BranchEnumerable(handle, filter);
    }

    public GitBranch GetBranch(string branchName, GitBranchType type)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* result = null;
        Git2.ThrowIfError(git_branch_lookup(&result, handle.NativeHandle, branchName, type));

        return new(result);
    }

    public bool TryGetBranch(string branchName, GitBranchType type, out GitBranch branch)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* result = null;
        var error = git_branch_lookup(&result, handle.NativeHandle, branchName, type);

        switch (error)
        {
            case GitError.OK:
                branch = new(result);
                return true;
            case GitError.NotFound:
                branch = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    /// <summary>
    /// Find the remote name of a remote-tracking branch
    /// </summary>
    /// <param name="refName"></param>
    /// <returns></returns>
    /// <remarks>
    /// This will return the name of the remote whose fetch refspec is matching the given branch.
    /// E.g. given a branch "refs/remotes/test/master", it will extract the "test" part.
    /// If refspecs from multiple remotes match, the method will throw an exception.
    /// </remarks>
    /// <exception cref="Git2Exception"/>
    public string? GetBranchRemoteName(string refName)
    {
        var handle = this.ThrowIfNull();

        GitReference.ThrowIfInvalidReferenceName(refName, GitReferenceFormat.Normal);

        Native.GitBuffer buffer = default;
        var error = git_branch_remote_name(&buffer, handle.NativeHandle, refName);

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

    public string? GetBranchUpstreamMerge(string refName)
    {
        var handle = this.ThrowIfNull();

        Native.GitBuffer buffer = default;

        Git2.ThrowIfError(git_branch_upstream_merge(&buffer, handle.NativeHandle, refName));

        try
        {
            return buffer.AsString();
        }
        finally
        {
            git_buf_dispose(&buffer);
        }
    }

    /// <summary>
    /// Get the upstream name of a branch.
    /// </summary>
    /// <param name="refName">The full name of the branch reference</param>
    /// <returns>The upstream name, or null if no remote tracking reference exists.</returns>
    public string? GetBranchUpstreamName(string refName)
    {
        var handle = this.ThrowIfNull();

        if (!refName.StartsWith("refs/heads/"))
        {
            throw new ArgumentException("Reference name must be a local branch!", nameof(refName));
        }

        Native.GitBuffer buffer = default;
        var error = git_branch_upstream_name(&buffer, handle.NativeHandle, refName);

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
    /// <param name="refName">The full name of the branch reference</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Git2Exception"></exception>
    public string? GetBranchUpstreamRemote(string refName)
    {
        var handle = this.ThrowIfNull();

        if (!refName.StartsWith("refs/heads/"))
        {
            throw new ArgumentException("Reference name must be a local branch!", nameof(refName));
        }

        Native.GitBuffer buffer = default;
        var error = git_branch_upstream_remote(&buffer, handle.NativeHandle, refName);

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

    public readonly struct BranchEnumerable(GitRepository repo, GitBranchType type) : IEnumerable<(GitBranch, GitBranchType)>
    {
        private readonly GitRepository _repository = repo;
        private readonly GitBranchType _filter = type;

        public BranchEnumerator GetEnumerator()
        {
            Git2.BranchIterator* iterator = null;
            Git2.ThrowIfError(git_branch_iterator_new(&iterator, _repository.NativeHandle, _filter));

            return new(iterator);
        }

        IEnumerator<(GitBranch, GitBranchType)> IEnumerable<(GitBranch, GitBranchType)>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct BranchEnumerator(Git2.BranchIterator* iterator) : IEnumerator<(GitBranch, GitBranchType)>
    {
        private Git2.BranchIterator* _iterator = iterator;

        public (GitBranch, GitBranchType) Current { get; private set; }

        public bool MoveNext()
        {
            Git2.Reference* branch = default;
            GitBranchType type = default;

            var error = git_branch_next(&branch, &type, _iterator);

            switch (error)
            {
                case GitError.OK:
                    Current = (new GitBranch(branch), type);
                    return true;
                case GitError.IterationOver:
                    Current = default;
                    return false;
                default:
                    throw Git2.ExceptionForError(error);
            }
        }

        public void Dispose()
        {
            git_branch_iterator_free(_iterator);
            _iterator = null;
        }

        readonly object IEnumerator.Current => this.Current;

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }
    }
    #endregion

    #region Checkout
    public void Checkout(GitCommit commit)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_checkout_tree(handle.NativeHandle, (Git2.Object*)commit.NativeHandle, null));
    }

    public void Checkout(GitCommit commit, in GitCheckoutOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_checkout_tree(handle.NativeHandle, (Git2.Object*)commit.NativeHandle, in options));
    }

    public void Checkout(GitTag tag)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_checkout_tree(handle.NativeHandle, (Git2.Object*)tag.NativeHandle, null));
    }

    public void Checkout(GitTag tag, in GitCheckoutOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_checkout_tree(handle.NativeHandle, (Git2.Object*)tag.NativeHandle, in options));
    }

    public void Checkout(GitTree tree)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_checkout_tree(handle.NativeHandle, (Git2.Object*)tree.NativeHandle, null));
    }

    public void Checkout(GitTree tree, in GitCheckoutOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_checkout_tree(handle.NativeHandle, (Git2.Object*)tree.NativeHandle, in options));
    }

    public void Checkout(GitObject treeish, in GitCheckoutOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_checkout_tree(handle.NativeHandle, treeish.NativeHandle, in options));
    }

    public void Checkout(GitIndex index, in GitCheckoutOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_checkout_index(handle.NativeHandle, index.NativeHandle, in options));
    }

    public void CheckoutHead()
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_checkout_head(handle.NativeHandle, null));
    }

    public void CheckoutHead(in GitCheckoutOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_checkout_head(handle.NativeHandle, in options));
    }
    #endregion

    #region Cherrypick
    public void Cherrypick(GitCommit commit, in GitCherrypickOptions options)
    {
        var handle = this.ThrowIfNull();

        Native.GitCherrypickOptions _options = default;
        List<GCHandle> gchandles = [];
        GitError error;

        try
        {
            _options.FromManaged(in options, gchandles);

            error = git_cherrypick(handle.NativeHandle, commit.NativeHandle, &_options);
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);
    }

    public GitIndex Cherrypick(GitCommit cherrypick_commit, GitCommit our_commit, uint mainline, in GitMergeOptions options)
    {
        var handle = this.ThrowIfNull();

        Native.GitMergeOptions _options = default;
        List<GCHandle> gchandles = [];
        Git2.Index* result = null;
        GitError error;

        try
        {
            _options.FromManaged(in options, gchandles);

            error = git_cherrypick_commit(&result, handle.NativeHandle, cherrypick_commit.NativeHandle, our_commit.NativeHandle, mainline, &_options);
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);
        return new(result);
    }
    #endregion

    #region Clone
    public static GitRepository Clone(string url, string localDirectory)
    {
        Git2.Repository* result = null;
        Git2.ThrowIfError(git_clone(&result, url, localDirectory, null));

        return new(result);
    }

    public static GitRepository Clone(string url, string localDirectory, in GitCloneOptions options)
    {
        Git2.Repository* result = null;
        GitError error;

        List<GCHandle> gchandles = [];
        Native.GitCloneOptions nativeOptions = default;
        try
        {
            nativeOptions.FromManaged(in options, gchandles);

            error = git_clone(&result, url, localDirectory, &nativeOptions);
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
            }

            nativeOptions.Free();
        }

        Git2.ThrowIfError(error);

        return new(result);
    }

    public static GitRepository Clone(string url, string localDirectory, in Native.GitCloneOptions options)
    {
        Git2.Repository* result = null;
        Git2.ThrowIfError(git_clone(&result, url, localDirectory, in options));

        return new(result);
    }

    #endregion

    #region Commit
    public GitCommit GetCommit(in GitObjectID id)
    {
        var handle = this.ThrowIfNull();

        Git2.Commit* result = null;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            Git2.ThrowIfError(git_commit_lookup(&result, handle.NativeHandle, ptr));
        }

        return new(result);
    }

    public GitCommit GetCommit(in GitObjectID id, ushort prefixLength)
    {
        var handle = this.ThrowIfNull();

        Git2.Commit* result = null;

        fixed (GitObjectID* ptr = &id)
        {
            Git2.ThrowIfError(git_commit_lookup_prefix(&result, handle.NativeHandle, ptr, prefixLength));
        }

        return new(result);
    }

    public bool TryGetCommit(in GitObjectID id, out GitCommit commit)
    {
        var handle = this.ThrowIfNull();

        GitError error;
        Git2.Commit* result = null;

        fixed (GitObjectID* ptr = &id)
        {
            error = git_commit_lookup(&result, handle.NativeHandle, ptr);
        }

        switch (error)
        {
            case GitError.OK:
                commit = new(result);
                return true;
            case GitError.NotFound:
                commit = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public bool TryGetCommit(in GitObjectID id, ushort prefixLength, out GitCommit commit)
    {
        var handle = this.ThrowIfNull();

        GitError error;
        Git2.Commit* result = null;

        fixed (GitObjectID* ptr = &id)
        {
            error = git_commit_lookup_prefix(&result, handle.NativeHandle, ptr, prefixLength);
        }

        switch (error)
        {
            case GitError.OK:
                commit = new(result);
                return true;
            case GitError.NotFound:
            case GitError.Ambiguous:
                commit = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public GitObjectID CreateCommit(
        string? updateRef,
        GitSignature author,
        GitSignature committer,
        ReadOnlySpan<char> message,
        GitTree tree,
        ReadOnlySpan<GitCommit> parents)
    {
        var handle = this.ThrowIfNull();

        Debug.Assert(sizeof(GitCommit) == sizeof(Git2.Commit*));

        GitObjectID result = default;
        Native.GitSignature* _author = null, _committer = null;
        GitError error;
        try
        {
            _author = GitSignatureMarshaller.ConvertToUnmanaged(author);
            _committer = GitSignatureMarshaller.ConvertToUnmanaged(committer);

            fixed (GitCommit* _parents = parents)
            {
                error = git_commit_create(
                    &result,
                    handle.NativeHandle,
                    updateRef,
                    _author,
                    _committer,
                    null,
                    message,
                    tree.NativeHandle,
                    (nuint)parents.Length,
                    (Git2.Commit**)_parents);
            }
        }
        finally
        {
            GitSignatureMarshaller.Free(_author);
            GitSignatureMarshaller.Free(_committer);
        }

        Git2.ThrowIfError(error);
        return result;
    }

    public void CreateCommitToBuffer(
        IBufferWriter<byte> writer,
        GitSignature author,
        GitSignature committer,
        string message,
        GitTree tree,
        ReadOnlySpan<GitCommit> parents)
    {
        var handle = this.ThrowIfNull();

        Debug.Assert(sizeof(GitCommit) == sizeof(Git2.Commit*));

        Native.GitBuffer result = default;
        Native.GitSignature* _author = null, _committer = null;
        GitError error;
        try
        {
            _author = GitSignatureMarshaller.ConvertToUnmanaged(author);
            _committer = GitSignatureMarshaller.ConvertToUnmanaged(committer);

            fixed (GitCommit* _parents = parents)
            {
                error = git_commit_create_buffer(
                    &result,
                    handle.NativeHandle,
                    _author,
                    _committer,
                    null,
                    message,
                    tree.NativeHandle,
                    (nuint)parents.Length,
                    (Git2.Commit**)_parents);
            }
        }
        finally
        {
            GitSignatureMarshaller.Free(_author);
            GitSignatureMarshaller.Free(_committer);
        }

        Git2.ThrowIfError(error);

        try
        {
            result.CopyToBufferWriter(writer);
        }
        finally
        {
            git_buf_dispose(&result);
        }
    }

    public GitObjectID CreateCommitWithSignature(byte* commitContent, byte* signature, string? signature_field)
    {
        var handle = this.ThrowIfNull();

        GitObjectID result = default;
        Git2.ThrowIfError(git_commit_create_with_signature(&result, handle.NativeHandle, commitContent, signature, signature_field));

        return result;
    }

    public (byte[] signature, byte[] signed_data) ExtractCommitSignature(in GitObjectID commitId, string? field)
    {
        var handle = this.ThrowIfNull();

        Native.GitBuffer signature = default, signed_data = default;

        fixed (GitObjectID* _id = &commitId)
        {
            Git2.ThrowIfError(git_commit_extract_signature(&signature, &signed_data, handle.NativeHandle, _id, field));
        }

        try
        {
            return (signature.Span.ToArray(), signed_data.Span.ToArray());
        }
        finally
        {
            git_buf_dispose(&signature);
            git_buf_dispose(&signed_data);
        }
    }

    #endregion

    #region Describe
    public GitDescribeResult DescribeWorkingDirectory(in GitDescribeOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.DescribeResult* result = null;
        GitError error;

        Native.GitDescribeOptions _options = default;
        List<GCHandle> gchandles = [];
        try
        {
            _options.FromManaged(in options, gchandles);

            error = git_describe_workdir(&result, handle.NativeHandle, &_options);
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);

        return new(result);
    }
    #endregion

    #region Diff
    public GitDiff GetDiff(GitTree oldTree, GitTree newTree, in GitDiffOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.Diff* result = null;
        GitError error;
        Native.GitDiffOptions nOptions = default;
        List<GCHandle> gchandles = [];

        try
        {
            nOptions.FromManaged(in options, gchandles);

            error = git_diff_tree_to_tree(&result, handle.NativeHandle, oldTree.NativeHandle, newTree.NativeHandle, &nOptions);
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            nOptions.Free();
        }

        Git2.ThrowIfError(error);

        return new(result);
    }

    public GitDiff GetDiff(GitTree oldTree, GitTree newTree, in Native.GitDiffOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.Diff* result = null;

        fixed (Native.GitDiffOptions* pOptions = &options)
            Git2.ThrowIfError(git_diff_tree_to_tree(&result, handle.NativeHandle, oldTree.NativeHandle, newTree.NativeHandle, pOptions));

        return new(result);
    }

    public GitDiff GetDiffToWorkDir(GitTree oldTree, in GitDiffOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.Diff* result = null;
        GitError error;
        Native.GitDiffOptions nOptions = default;
        List<GCHandle> gchandles = [];

        try
        {
            nOptions.FromManaged(in options, gchandles);

            error = git_diff_tree_to_workdir(&result, handle.NativeHandle, oldTree.NativeHandle, &nOptions);
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            nOptions.Free();
        }

        Git2.ThrowIfError(error);

        return new(result);
    }

    public GitDiff GetDiffToWorkDirWithIndex(GitTree oldTree, in GitDiffOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.Diff* result = null;
        GitError error;
        Native.GitDiffOptions nOptions = default;
        List<GCHandle> gchandles = [];

        try
        {
            nOptions.FromManaged(in options, gchandles);

            error = git_diff_tree_to_workdir_with_index(&result, handle.NativeHandle, oldTree.NativeHandle, &nOptions);
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            nOptions.Free();
        }

        Git2.ThrowIfError(error);

        return new(result);
    }

    #endregion

    #region Graph
    public (nuint ahead, nuint behind) GetGraphAheadBehind(in GitObjectID local, in GitObjectID upstream)
    {
        var handle = this.ThrowIfNull();

        nuint _ahead = 0, _behind = 0;
        GitError error;

        fixed (GitObjectID* pLocal = &local, pUpstream = &upstream)
            error = git_graph_ahead_behind(&_ahead, &_behind, handle.NativeHandle, pLocal, pUpstream);

        Git2.ThrowIfError(error);
        return (_ahead, _behind);
    }
    #endregion

    #region Merge
    public void Merge(
        ReadOnlySpan<GitAnnotatedCommit> commits,
        in GitMergeOptions mergeOptions,
        in GitCheckoutOptions checkoutOptions)
    {
        var handle = this.ThrowIfNull();

        Debug.Assert(sizeof(GitAnnotatedCommit) == sizeof(Git2.AnnotatedCommit*));
        Native.GitMergeOptions _merge = default;
        Native.GitCheckoutOptions _checkout = default;
        List<GCHandle> gchandles = [];
        GitError error;

        try
        {
            _merge.FromManaged(in mergeOptions, gchandles);
            _checkout.FromManaged(in checkoutOptions, gchandles);

            fixed (GitAnnotatedCommit* pCommits = commits)
            {
                error = git_merge(handle.NativeHandle, (Git2.AnnotatedCommit**)pCommits, (nuint)commits.Length, &_merge, &_checkout);
            }
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            _merge.Free();
            _checkout.Free();
        }

        Git2.ThrowIfError(error);
    }

    public (GitMergeAnalysisResult analysis, GitMergePreference preference) GetMergeAnalysis(ReadOnlySpan<GitAnnotatedCommit> commits)
    {
        var handle = this.ThrowIfNull();

        GitMergeAnalysisResult analysis = default;
        GitMergePreference preference = default;

        fixed (GitAnnotatedCommit* pCommits = commits)
        {
            Git2.ThrowIfError(git_merge_analysis(
                &analysis,
                &preference,
                handle.NativeHandle,
                (Git2.AnnotatedCommit**)pCommits,
                (nuint)commits.Length));
        }

        return (analysis, preference);
    }

    public (GitMergeAnalysisResult analysis, GitMergePreference preference) GetMergeAnalysisForReference(GitReference our_ref, ReadOnlySpan<GitAnnotatedCommit> commits)
    {
        var handle = this.ThrowIfNull();

        GitMergeAnalysisResult analysis = default;
        GitMergePreference preference = default;

        fixed (GitAnnotatedCommit* pCommits = commits)
        {
            Git2.ThrowIfError(git_merge_analysis_for_ref(
                &analysis,
                &preference,
                handle.NativeHandle,
                our_ref.NativeHandle,
                (Git2.AnnotatedCommit**)pCommits,
                (nuint)commits.Length));
        }

        return (analysis, preference);
    }

    public GitObjectID GetMergeBase(in GitObjectID one, in GitObjectID two)
    {
        var handle = this.ThrowIfNull();

        GitObjectID result;
        fixed (GitObjectID* pOne = &one, pTwo = &two)
            Git2.ThrowIfError(git_merge_base(&result, handle.NativeHandle, pOne, pTwo));

        return result;
    }

    public GitObjectID GetMergeBase(ReadOnlySpan<GitObjectID> ids)
    {
        var handle = this.ThrowIfNull();

        GitObjectID result;
        fixed (GitObjectID* pIds = ids)
            Git2.ThrowIfError(git_merge_base_many(&result, handle.NativeHandle, (nuint)ids.Length, pIds));

        return result;
    }

    public GitObjectID GetMergeBaseOctopus(ReadOnlySpan<GitObjectID> ids)
    {
        var handle = this.ThrowIfNull();

        GitObjectID result;
        fixed (GitObjectID* pIds = ids)
            Git2.ThrowIfError(git_merge_base_octopus(&result, handle.NativeHandle, (nuint)ids.Length, pIds));

        return result;
    }


    public GitObjectID[] GetMergeBases(in GitObjectID one, in GitObjectID two)
    {
        var handle = this.ThrowIfNull();

        Native.GitObjectIDArray array = default;

        fixed (GitObjectID* pOne = &one, pTwo = &two)
            Git2.ThrowIfError(git_merge_bases(&array, handle.NativeHandle, pOne, pTwo));

        try
        {
            return new ReadOnlySpan<GitObjectID>(array.Ids, checked((int)array.Count)).ToArray();
        }
        finally
        {
            git_oidarray_dispose(&array);
        }
    }

    public GitObjectID[] GetMergeBases(ReadOnlySpan<GitObjectID> ids)
    {
        var handle = this.ThrowIfNull();

        Native.GitObjectIDArray array = default;

        fixed (GitObjectID* pIds = ids)
            Git2.ThrowIfError(git_merge_bases_many(&array, handle.NativeHandle, (nuint)ids.Length, pIds));

        try
        {
            return new ReadOnlySpan<GitObjectID>(array.Ids, checked((int)array.Count)).ToArray();
        }
        finally
        {
            git_oidarray_dispose(&array);
        }
    }

    public GitIndex MergeCommits(GitCommit ours, GitCommit theirs, in GitMergeOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.Index* result = null;
        GitError error;

        Native.GitMergeOptions _options = default;
        List<GCHandle> gchandles = [];
        try
        {
            _options.FromManaged(in options, gchandles);

            error = git_merge_commits(&result, handle.NativeHandle, ours.NativeHandle, theirs.NativeHandle, &_options);
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);
        return new(result);
    }

    public GitMergeFileResult GetMergeFileFromIndex(GitIndexEntry ancestor, GitIndexEntry ours, GitIndexEntry theirs, in GitMergeFileOptions options)
    {
        var handle = this.ThrowIfNull();

        Native.GitIndexEntry _ancestor = default, _ours = default, _theirs = default;
        Native.GitMergeFileResult _result = default;
        Native.GitMergeFileOptions _options = default;
        GitError error;

        List<GCHandle> gchandles = [];
        try
        {
            _ancestor.FromManaged(ancestor);
            _ours.FromManaged(ours);
            _theirs.FromManaged(theirs);

            _options.FromManaged(in options, gchandles);

            error = git_merge_file_from_index(&_result, handle.NativeHandle, &_ancestor, &_ours, &_theirs, &_options);
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            _ancestor.Free();
            _ours.Free();
            _theirs.Free();
            _options.Free();
        }

        Git2.ThrowIfError(error);

        // Take ownership of the resulting data, and free the unmanaged memory
        try
        {
            GitMergeFileResult result = default;

            result.AutoMergeable = _result.AutoMergeable != 0;
            result.Path = Git2.GetPooledString(_result.Path);
            result.Mode = _result.Mode;

            var source = new ReadOnlySpan<byte>(_result.FileContent, checked((int)_result.ContentLength));
            var destination = GC.AllocateUninitializedArray<byte>(source.Length);
            source.CopyTo(destination);

            result.FileContent = destination;

            return result;
        }
        finally
        {
            git_merge_file_result_free(&_result);
        }
    }

    #endregion

    #region Object

    public GitObject GetObject(in GitObjectID id)
    {
        return GetObject<GitObject>(in id);
    }

    public GitObject GetObject(in GitObjectID id, GitObjectType type)
    {
        var handle = this.ThrowIfNull();

        Git2.Object* result = null;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            Git2.ThrowIfError(git_object_lookup(&result, handle.NativeHandle, ptr, type));
        }

        return new(result);
    }

    public TObject GetObject<TObject>(in GitObjectID id)
        where TObject: struct, IGitHandle, IGitObject<TObject>
    {
        var handle = this.ThrowIfNull();

        Git2.Object* result = null;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            Git2.ThrowIfError(git_object_lookup(&result, handle.NativeHandle, ptr, TObject.ObjectType));
        }

        return TObject.FromObjectPointer(result);
    }

    public bool TryGetObject(in GitObjectID id, out GitObject obj)
    {
        return TryGetObject<GitObject>(in id, out obj);
    }

    public bool TryGetObject(in GitObjectID id, GitObjectType type, out GitObject obj)
    {
        var handle = this.ThrowIfNull();

        GitError error;
        Git2.Object* result = null;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            error = git_object_lookup(&result, handle.NativeHandle, ptr, type);
        }

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

    public bool TryGetObject<TObject>(in GitObjectID id, out TObject obj)
        where TObject: struct, IGitHandle, IGitObject<TObject>
    {
        var handle = this.ThrowIfNull();

        GitError error;
        Git2.Object* result = null;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            error = git_object_lookup(&result, handle.NativeHandle, ptr, TObject.ObjectType);
        }

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

    public GitObject GetObjectWithPrefix(in GitObjectID id, ushort prefix_length, GitObjectType type)
    {
        var handle = this.ThrowIfNull();

        Git2.Object* result = null;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            Git2.ThrowIfError(git_object_lookup_prefix(&result, handle.NativeHandle, ptr, prefix_length, type));
        }

        return new(result);
    }

    public TObject GetObjectWithPrefix<TObject>(in GitObjectID id, ushort prefix_length)
        where TObject: struct, IGitHandle, IGitObject<TObject>
    {
        var handle = this.ThrowIfNull();

        Git2.Object* result = null;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            Git2.ThrowIfError(git_object_lookup_prefix(&result, handle.NativeHandle, ptr, prefix_length, TObject.ObjectType));
        }

        return TObject.FromObjectPointer(result);
    }

    public bool TryGetObjectWithPrefix(in GitObjectID id, ushort prefix_length, GitObjectType type, out GitObject obj)
    {
        var handle = this.ThrowIfNull();

        GitError error;
        Git2.Object* result = null;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            error = git_object_lookup_prefix(&result, handle.NativeHandle, ptr, prefix_length, type);
        }

        switch (error)
        {
            case GitError.OK:
                obj = new(result);
                return true;
            case GitError.NotFound:
            case GitError.Ambiguous:
                obj = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public bool TryGetObjectWithPrefix<TObject>(in GitObjectID id, ushort prefix_length, out TObject obj)
        where TObject: struct, IGitHandle, IGitObject<TObject>
    {
        var handle = this.ThrowIfNull();

        GitError error;
        Git2.Object* result = null;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            error = git_object_lookup_prefix(&result, handle.NativeHandle, ptr, prefix_length, TObject.ObjectType);
        }

        switch (error)
        {
            case GitError.OK:
                obj = TObject.FromObjectPointer(result);
                return true;
            case GitError.NotFound:
            case GitError.Ambiguous:
                obj = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    #endregion

    #region Rebase
    /// <summary>
    /// Creates a rebase operation, rebasing the changes in <paramref name="branch"/> relative to <paramref name="upstream"/> onto another branch.
    /// </summary>
    /// <param name="branch">The terminal commit to rebase, or <see langword="default"/> to rebase the current branch</param>
    /// <param name="upstream">The commit to begin rebasing from, or <see langword="default"/> to rebase all reachable commits</param>
    /// <param name="onto">The branch to rebase onto, or <see langword="default"/> to rebase onto the given upstream</param>
    /// <returns>The created rebase object</returns>
    public GitRebase CreateRebase(GitAnnotatedCommit branch, GitAnnotatedCommit upstream, GitAnnotatedCommit onto)
    {
        var handle = this.ThrowIfNull();

        ValidateRebaseParameters(branch, upstream, onto);

        Git2.Rebase* rebase = null;

        Git2.ThrowIfError(git_rebase_init(&rebase, handle.NativeHandle, branch.NativeHandle, upstream.NativeHandle, onto.NativeHandle, null));

        return new(rebase);
    }

    /// <summary>
    /// Creates a rebase operation, rebasing the changes in <paramref name="branch"/> relative to <paramref name="upstream"/> onto another branch.
    /// </summary>
    /// <param name="branch">The terminal commit to rebase, or <see langword="default"/> to rebase the current branch</param>
    /// <param name="upstream">The commit to begin rebasing from, or <see langword="default"/> to rebase all reachable commits</param>
    /// <param name="onto">The branch to rebase onto, or <see langword="default"/> to rebase onto the given upstream</param>
    /// <param name="options">Options to specify how rebase is performed</param>
    /// <returns>The created rebase object</returns>
    public GitRebase CreateRebase(GitAnnotatedCommit branch, GitAnnotatedCommit upstream, GitAnnotatedCommit onto, in GitRebaseOptions options)
    {
        var handle = this.ThrowIfNull();

        ValidateRebaseParameters(branch, upstream, onto);

        List<GCHandle> gchandles = [];
        Native.GitRebaseOptions nativeOptions = default;

        Git2.Rebase* rebase = null;

        try
        {
            nativeOptions.FromManaged(in options, gchandles);

            Git2.ThrowIfError(git_rebase_init(&rebase, handle.NativeHandle, branch.NativeHandle, upstream.NativeHandle, onto.NativeHandle, &nativeOptions));
        }
        finally
        {
            nativeOptions.Free();

            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }
        }

        return new(rebase);
    }

    private static void ValidateRebaseParameters(GitAnnotatedCommit branch, GitAnnotatedCommit upstream, GitAnnotatedCommit onto)
    {
        if (upstream.IsNull && onto.IsNull)
        {
            throw new ArgumentException("Rebase upstream or onto must be specified!");
        }
    }

    public GitRebase OpenRebase(in GitRebaseOptions options)
    {
        var handle = this.ThrowIfNull();

        List<GCHandle> gchandles = [];
        Native.GitRebaseOptions nativeOptions = default;

        Git2.Rebase* rebase = null;

        try
        {
            nativeOptions.FromManaged(in options, gchandles);

            Git2.ThrowIfError(git_rebase_open(&rebase, handle.NativeHandle, &nativeOptions));
        }
        finally
        {
            nativeOptions.Free();

            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }
        }

        return new(rebase);
    }

    #endregion

    #region Reference

    public GitReference CreateReference(string name, GitObjectID id, bool force, string? logMessage)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* result = null;
        Git2.ThrowIfError(git_reference_create(&result, handle.NativeHandle, name, &id, force ? 1 : 0, logMessage));

        return new(result);
    }

    public GitReference CreateReference(string name, in GitObjectID id, bool force, string? logMessage)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* result = null;
        GitError error;

        fixed (GitObjectID* ptr = &id)
        {
            error = git_reference_create(&result, handle.NativeHandle, name, ptr, force ? 1 : 0, logMessage);
        }

        Git2.ThrowIfError(error);

        return new(result);
    }

    public GitReference CreateMatchingReference(string name, GitObjectID id, bool force, GitObjectID currentId, string? logMessage)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* result = null;
        Git2.ThrowIfError(git_reference_create_matching(&result, handle.NativeHandle, name, &id, force ? 1 : 0, &currentId, logMessage));

        return new(result);
    }

    public GitReference CreateMatchingReference(string name, in GitObjectID id, bool force, in GitObjectID currentId, string? logMessage)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* result = null;
        fixed (GitObjectID* idPtr = &id, currentIdPtr = &currentId)
        {
            Git2.ThrowIfError(git_reference_create_matching(&result, handle.NativeHandle, name, idPtr, force ? 1 : 0, currentIdPtr, logMessage));
        }

        return new(result);
    }

    public GitReference CreateSymbolicReference(string name, string target, bool force, string? logMessage)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* result = null;
        Git2.ThrowIfError(git_reference_symbolic_create(&result, handle.NativeHandle, name, target, force ? 1 : 0, logMessage));

        return new(result);
    }

    public GitReference CreateSymbolicReferenceMatching(string name, string target, bool force, string? currentValue, string? logMessage)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* result = null;
        Git2.ThrowIfError(git_reference_symbolic_create_matching(&result, handle.NativeHandle, name, target, force ? 1 : 0, currentValue, logMessage));

        return new(result);
    }

    public GitReference GetReference(string name)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* result = null;
        Git2.ThrowIfError(git_reference_lookup(&result, handle.NativeHandle, name));

        return new(result);
    }

    /// <summary>
    /// Looks up a reference from the repository
    /// </summary>
    /// <param name="name">Name of the reference object</param>
    /// <param name="reference">The returned reference handle</param>
    /// <returns>true if successful, false if not or if the reference name is malformed</returns>
    /// <exception cref="Git2Exception"/>
    public bool TryGetReference(string name, out GitReference reference)
    {
        var handle = this.ThrowIfNull();

        if (string.IsNullOrEmpty(name))
        {
            reference = default;
            return false;
        }

        Git2.Reference* result = null;
        var error = git_reference_lookup(&result, handle.NativeHandle, name);

        switch (error)
        {
            case GitError.OK:
                reference = new(result);
                return true;
            case GitError.NotFound:
                reference = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public string[] GetReferenceNameList()
    {
        var handle = this.ThrowIfNull();

        Native.GitStringArray nativeArray = default;

        Git2.ThrowIfError(git_reference_list(&nativeArray, handle.NativeHandle));

        try
        {
            return nativeArray.ToManaged(true);
        }
        finally
        {
            git_strarray_dispose(&nativeArray);
        }
    }

    public void ReferenceEnsureLog(string referenceName)
    {
        var handle = this.ThrowIfNull();

        ArgumentException.ThrowIfNullOrEmpty(referenceName);

        Git2.ThrowIfError(git_reference_ensure_log(handle.NativeHandle, referenceName));
    }

    public bool ReferenceHasLog(string referenceName)
    {
        var handle = this.ThrowIfNull();

        var code = git_reference_has_log(handle.NativeHandle, referenceName);

        return Git2.ErrorOrBoolean(code);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ForEachReference(ForEachReferenceCallback callback, bool autoDispose = true)
    {
        var handle = this.ThrowIfNull();

        var context = new ForEachReferenceContext(callback) { AutoDispose = autoDispose };
        var error = git_reference_foreach(handle.NativeHandle, &_Callback, (nint)(void*)&context);

        context.ExceptionInfo?.Throw();
        Git2.ThrowIfError(error);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int _Callback(Git2.Reference* reference, nint payload)
        {
            var referenceHandle = new GitReference(reference);

            ref var context = ref *(ForEachReferenceContext*)payload;

            try
            {
                bool breakLoop = false;
                context.Callback(referenceHandle, ref breakLoop);

                return breakLoop ? 1 : 0;
            }
            catch (Exception e)
            {
                Debug.Assert(context.ExceptionInfo is null);
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);

                return (int)GitError.User;
            }
            finally
            {
                if (context.AutoDispose)
                    referenceHandle.Dispose();
            }
        }
    }

    public delegate void ForEachReferenceUTF8NameCallback(ReadOnlySpan<byte> utf8Name, ref bool breakLoop);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ForEachReferenceName(ForEachReferenceUTF8NameCallback callback, string? glob = null)
    {
        var handle = this.ThrowIfNull();

        var context = new Git2.CallbackContext<ForEachReferenceUTF8NameCallback>(callback);

        GitError error = glob is null || glob == "*"
            ? git_reference_foreach_name(handle.NativeHandle, &_Callback, (nint)(void*)&context)
            : git_reference_foreach_glob(handle.NativeHandle, glob, &_Callback, (nint)(void*)&context);

        context.ExceptionInfo?.Throw();
        Git2.ThrowIfError(error);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int _Callback(byte* name, nint payload)
        {
            ref var context = ref *(Git2.CallbackContext<ForEachReferenceUTF8NameCallback>*)payload;

            try
            {
                var span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(name);

                bool breakLoop = false;
                context.Callback(span, ref breakLoop);

                return breakLoop ? 1 : 0;
            }
            catch (Exception e)
            {
                Debug.Assert(context.ExceptionInfo is null);
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);

                return (int)GitError.User;
            }
        }
    }

    public delegate void ForEachReferenceNameCallback(string referenceName, ref bool breakLoop);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ForEachReferenceName(ForEachReferenceNameCallback callback, string? glob = null)
    {
        var handle = this.ThrowIfNull();

        var context = new Git2.CallbackContext<ForEachReferenceNameCallback>(callback);

        GitError error = glob is null || glob == "*"
            ? git_reference_foreach_name(handle.NativeHandle, &_Callback, (nint)(void*)&context)
            : git_reference_foreach_glob(handle.NativeHandle, glob, &_Callback, (nint)(void*)&context);

        context.ExceptionInfo?.Throw();
        Git2.ThrowIfError(error);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int _Callback(byte* name, nint payload)
        {
            ref var context = ref *(Git2.CallbackContext<ForEachReferenceNameCallback>*)payload;

            try
            {
                string _name = Git2.GetPooledString(name);

                bool breakLoop = false;
                context.Callback(_name, ref breakLoop);

                return !breakLoop ? 0 : 1;
            }
            catch (Exception e)
            {
                Debug.Assert(context.ExceptionInfo is null);
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);

                return (int)GitError.User;
            }
        }
    }

    public ReferenceEnumerable EnumerateReferences(string? glob = null)
    {
        var handle = this.ThrowIfNull();

        return new ReferenceEnumerable(handle, glob);
    }

    public ReferenceNameEnumerable EnumerateReferenceNames(string? glob = null)
    {
        var handle = this.ThrowIfNull();

        return new ReferenceNameEnumerable(handle, glob);
    }

    /// <summary>
    /// Remove a reference without generating a reference handle
    /// </summary>
    /// <param name="name">The reference to remove</param>
    /// <remarks>
    /// This method removes the named reference from the repository without creating a reference handle.
    /// </remarks>
    public void RemoveReference(string name)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_reference_remove(handle.NativeHandle, name));
    }

    public bool TryGetReferenceByShorthand(string shorthand, out GitReference reference)
    {
        var handle = this.ThrowIfNull();

        GitReference result;
        var error = git_reference_dwim((Git2.Reference**)&result, handle.NativeHandle, shorthand);

        switch (error)
        {
            case GitError.OK:
                reference = result;
                return true;
            case GitError.NotFound:
            case GitError.InvalidSpec:
                reference = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public bool TryGetId(string referenceName, out GitObjectID id)
    {
        var handle = this.ThrowIfNull();

        GitError error;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            error = git_reference_name_to_id(ptr, handle.NativeHandle, referenceName);
        }

        switch (error)
        {
            case GitError.OK:
                return true;
            case GitError.NotFound:
            case GitError.InvalidSpec:
                id = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public delegate void ForEachReferenceCallback(GitReference reference, ref bool breakLoop);

    [method: SetsRequiredMembers]
    private ref struct ForEachReferenceContext(ForEachReferenceCallback callback)
    {
        public required ForEachReferenceCallback Callback { get; init; } = callback;

        internal ExceptionDispatchInfo? ExceptionInfo { get; set; }

        public bool AutoDispose { get; init; }
    }

    public readonly struct ReferenceEnumerable(GitRepository repo, string? glob) : IEnumerable<GitReference>
    {
        private readonly GitRepository _repository = repo;
        private readonly string? _glob = glob;

        public ReferenceEnumerator GetEnumerator()
        {
            Git2.ReferenceIterator* handle = null;

            if (_glob is null)
            {
                Git2.ThrowIfError(git_reference_iterator_new(&handle, _repository.NativeHandle));
            }
            else
            {
                Git2.ThrowIfError(git_reference_iterator_glob_new(&handle, _repository.NativeHandle, _glob));
            }

            return new(handle);
        }

        IEnumerator<GitReference> IEnumerable<GitReference>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct ReferenceEnumerator : IEnumerator<GitReference>
    {
        private Git2.ReferenceIterator* _iteratorHandle;

        internal ReferenceEnumerator(Git2.ReferenceIterator* handle) => _iteratorHandle = handle;

        public GitReference Current { get; private set; }

        public bool MoveNext()
        {
            Git2.Reference* reference = null;
            var code = git_reference_next(&reference, _iteratorHandle);

            switch (code)
            {
                case GitError.OK:
                    Current = new(reference);
                    return true;
                case GitError.IterationOver:
                    Current = default;
                    return false;
                default:
                    throw Git2.ExceptionForError(code);
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        readonly object IEnumerator.Current => throw new NotImplementedException();

        public void Dispose()
        {
            if (_iteratorHandle is not null)
            {
                git_reference_iterator_free(_iteratorHandle);
                _iteratorHandle = null;
            }
        }
    }

    public readonly struct ReferenceNameEnumerable(GitRepository repo, string? glob) : IEnumerable<string>
    {
        private readonly GitRepository _repository = repo;
        private readonly string? _glob = glob;

        public ReferenceNameEnumerator GetEnumerator()
        {
            Git2.ReferenceIterator* handle = null;

            if (_glob is null)
            {
                Git2.ThrowIfError(git_reference_iterator_new(&handle, _repository.NativeHandle));
            }
            else
            {
                Git2.ThrowIfError(git_reference_iterator_glob_new(&handle, _repository.NativeHandle, _glob));
            }

            return new(handle);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct ReferenceNameEnumerator : IEnumerator<string>
    {
        private Git2.ReferenceIterator* _iteratorHandle;

        internal ReferenceNameEnumerator(Git2.ReferenceIterator* handle) => _iteratorHandle = handle;

        public string Current { readonly get; private set; } = null!;

        public bool MoveNext()
        {
            byte* name = null;
            var code = git_reference_next_name(&name, _iteratorHandle);

            switch (code)
            {
                case GitError.OK:
                    Current = Git2.GetPooledString(name);
                    return true;
                case GitError.IterationOver:
                    Current = default!;
                    return false;
                default:
                    throw Git2.ExceptionForError(code);
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        readonly object IEnumerator.Current => this.Current;

        public void Dispose()
        {
            git_reference_iterator_free(_iteratorHandle);
            _iteratorHandle = null;
        }
    }

    #endregion

    #region Remote
    public GitRemote CreateRemote(string name, string url)
    {
        var handle = this.ThrowIfNull();

        Git2.Remote* result = null;

        Git2.ThrowIfError(git_remote_create(&result, handle.NativeHandle, name, url));

        return new(result);
    }

    public GitRemote CreateAnonymousRemote(string url)
    {
        var handle = this.ThrowIfNull();

        Git2.Remote* result = null;

        Git2.ThrowIfError(git_remote_create_anonymous(&result, handle.NativeHandle, url));

        return new(result);
    }

    public GitRemote CreateRemote(string name, string url, string fetchspec)
    {
        var handle = this.ThrowIfNull();

        Git2.Remote* result = null;

        Git2.ThrowIfError(git_remote_create_with_fetchspec(&result, handle.NativeHandle, name, url, fetchspec));

        return new(result);
    }

    public void DeleteRemote(string name)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_remote_delete(handle.NativeHandle, name));
    }

    public string[] GetRemoteList()
    {
        var handle = this.ThrowIfNull();

        Native.GitStringArray array = default;

        Git2.ThrowIfError(git_remote_list(&array, handle.NativeHandle));

        try
        {
            return array.ToManaged();
        }
        finally
        {
            git_strarray_dispose(&array);
        }
    }

    public GitRemote GetRemote(string name)
    {
        var handle = this.ThrowIfNull();

        Git2.Remote* result = null;

        Git2.ThrowIfError(git_remote_lookup(&result, handle.NativeHandle, name));

        return new(result);
    }

    public bool TryGetRemote(string name, out GitRemote remote)
    {
        var handle = this.ThrowIfNull();

        Git2.Remote* result = null;

        var error = git_remote_lookup(&result, handle.NativeHandle, name);

        switch (error)
        {
            case GitError.OK:
                remote = new(result);
                return true;
            case GitError.NotFound:
                remote = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public void RenameRemote(string current_name, string new_name, out string[] problemRefspecs)
    {
        var handle = this.ThrowIfNull();

        Native.GitStringArray problems = default;

        Git2.ThrowIfError(git_remote_rename(&problems, handle.NativeHandle, current_name, new_name));

        try
        {
            problemRefspecs = problems.ToManaged();
        }
        finally
        {
            git_strarray_dispose(&problems);
        }
    }

    public void SetRemoteAutotag(string remote_name, GitRemoteAutoTagOption value)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_remote_set_autotag(handle.NativeHandle, remote_name, value));
    }

    public void SetRemotePushUrl(string remote_name, string remote_pushurl)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_remote_set_pushurl(handle.NativeHandle, remote_name, remote_pushurl));
    }

    public void SetRemoteUrl(string remote_name, string remote_url)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_remote_set_pushurl(handle.NativeHandle, remote_name, remote_url));
    }
    #endregion

    #region Reset
    public void Reset(GitTag tag, GitResetType type)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_reset(handle.NativeHandle, (Git2.Object*)tag.NativeHandle, type, null));
    }

    public void Reset(GitTag tag, GitResetType type, in GitCheckoutOptions checkoutOptions)
    {
        ResetInternal(tag, type, in checkoutOptions);
    }

    public void Reset(GitCommit commit, GitResetType type)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_reset(handle.NativeHandle, (Git2.Object*)commit.NativeHandle, type, null));
    }

    public void Reset(GitCommit commit, GitResetType type, in GitCheckoutOptions checkoutOptions)
    {
        ResetInternal(commit, type, in checkoutOptions);
    }

    private void ResetInternal(GitObject committish, GitResetType type, in GitCheckoutOptions checkoutOptions)
    {
        var handle = this.ThrowIfNull();

        Native.GitCheckoutOptions _options = default;
        List<GCHandle> gchandles = [];

        try
        {
            _options.FromManaged(in checkoutOptions, gchandles);

            Git2.ThrowIfError(git_reset(handle.NativeHandle, committish.NativeHandle, type, &_options), "Reset");
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            _options.Free();
        }
    }

    public void Reset(GitAnnotatedCommit commit, GitResetType type)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_reset_from_annotated(handle.NativeHandle, commit.NativeHandle, type, null));
    }

    public void Reset(GitAnnotatedCommit commit, GitResetType type, in GitCheckoutOptions checkoutOptions)
    {
        var handle = this.ThrowIfNull();

        Native.GitCheckoutOptions _options = default;
        List<GCHandle> gchandles = [];

        try
        {
            _options.FromManaged(in checkoutOptions, gchandles);

            Git2.ThrowIfError(git_reset_from_annotated(handle.NativeHandle, commit.NativeHandle, type, &_options));
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            _options.Free();
        }
    }

    public void ResetDefault(params ReadOnlySpan<string> pathspecs)
    {
        ResetDefaultInternal(default, pathspecs);
    }

    public void ResetDefault(GitTag tag, params ReadOnlySpan<string> pathspecs)
    {
        ResetDefaultInternal(tag, pathspecs);
    }

    public void ResetDefault(GitCommit commit, params ReadOnlySpan<string> pathspecs)
    {
        ResetDefaultInternal(commit, pathspecs);
    }

    private void ResetDefaultInternal(GitObject committish, ReadOnlySpan<string> pathspecs)
    {
        Git2.ThrowIfError(git_reset_default(this.NativeHandle, committish.NativeHandle, pathspecs), "ResetDefault");
    }
    #endregion

    #region Signature
    public GitSignature DefaultSignature()
    {
        var handle = this.ThrowIfNull();

        Native.GitSignature* result = null;
        Git2.ThrowIfError(git_signature_default(&result, handle.NativeHandle));

        try
        {
            return new(in *result);
        }
        finally
        {
            git_signature_free(result);
        }
    }

    public bool TryGetDefaultSignature(out GitSignature signature)
    {
        var handle = this.ThrowIfNull();

        Native.GitSignature* result = null;
        
        var error = git_signature_default(&result, handle.NativeHandle);

        switch (error)
        {
            case GitError.OK:
                try
                {
                    signature = new(in *result);
                    return true;
                }
                finally
                {
                    git_signature_free(result);
                }
            case GitError.NotFound:
                signature = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }
    #endregion

    #region Stash

    #endregion

    #region Status
    public GitStatusFlags GetFileStatus(string path)
    {
        var handle = this.ThrowIfNull();

        GitStatusFlags status = default;
        Git2.ThrowIfError(git_status_file(&status, handle.NativeHandle, path));
        return status;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ForEachStatus(Func<string, GitStatusFlags, int> callback)
    {
        var handle = this.ThrowIfNull();

        Git2.CallbackContext<Func<string, GitStatusFlags, int>> context = new(callback);
        var error = git_status_foreach(handle.NativeHandle, &_Callback, (nint)(void*)&context);

        context.ExceptionInfo?.Throw();
        Git2.ThrowIfError(error);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int _Callback(byte* path, GitStatusFlags status, nint payload)
        {
            ref var context = ref *(Git2.CallbackContext<Func<string, GitStatusFlags, int>>*)payload;

            try
            {
                return context.Callback(Git2.GetPooledString(path), status);
            }
            catch (Exception e)
            {
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);
                return -1;
            }
        }
    }

    public void ForEachStatus(in GitStatusOptions options, Func<string, GitStatusFlags, int> callback)
    {
        var handle = this.ThrowIfNull();

        Git2.CallbackContext<Func<string, GitStatusFlags, int>> context = new(callback);

        Native.GitStatusOptions _options = default;
        GitError error;
        try
        {
            _options.FromManaged(in options);

            error = git_status_foreach_ext(handle.NativeHandle, &_options, &_Callback, (nint)(void*)&context);
        }
        finally
        {
            _options.Free();
        }

        context.ExceptionInfo?.Throw();
        Git2.ThrowIfError(error);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int _Callback(byte* path, GitStatusFlags status, nint payload)
        {
            ref var context = ref *(Git2.CallbackContext<Func<string, GitStatusFlags, int>>*)payload;

            try
            {
                return context.Callback(Git2.GetPooledString(path), status);
            }
            catch (Exception e)
            {
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);
                return -1;
            }
        }
    }

    public bool IsDirty
    {
        get
        {
            var handle = this.ThrowIfNull();

            return Git2.ErrorOrBoolean((int)git_status_foreach(handle.NativeHandle, &_Callback, 0));

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
            static int _Callback(byte* path, GitStatusFlags status, nint payload)
            {
                if ((status & GitStatusFlags.Ignored) != 0)
                    return 0;

                return status != GitStatusFlags.Current ? 1 : 0;
            }
        }
    }

    public bool IsIndexDirty
    {
        get
        {
            var handle = this.ThrowIfNull();

            return Git2.ErrorOrBoolean((int)git_status_foreach(handle.NativeHandle, &_Callback, 0));

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
            static int _Callback(byte* path, GitStatusFlags status, nint payload)
            {
                if ((status & GitStatusFlags.Ignored) != 0)
                    return 0;

                const GitStatusFlags flags = GitStatusFlags.IndexNew
                    | GitStatusFlags.IndexDeleted
                    | GitStatusFlags.IndexModified
                    | GitStatusFlags.IndexTypeChange;

                return (status & flags) != 0 ? 1 : 0;
            }
        }
    }

    public bool IsWorkingTreeDirty
    {
        get
        {
            var handle = this.ThrowIfNull();

            return Git2.ErrorOrBoolean((int)git_status_foreach(handle.NativeHandle, &_Callback, 0));

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
            static int _Callback(byte* path, GitStatusFlags status, nint payload)
            {
                if ((status & GitStatusFlags.Ignored) != 0)
                    return 0;

                const GitStatusFlags flags = GitStatusFlags.WorkingTreeNew
                    | GitStatusFlags.WorkingTreeDeleted
                    | GitStatusFlags.WorkingTreeModified
                    | GitStatusFlags.WorkingTreeTypeChange;

                return (status & flags) != 0 ? 1 : 0;
            }
        }
    }

    public bool StatusShouldIgnore(string path)
    {
        var handle = this.ThrowIfNull();

        int ignored = 0;
        Git2.ThrowIfError(git_status_should_ignore(&ignored, handle.NativeHandle, path));

        return ignored != 0;
    }

    #endregion

    #region Tree
    public GitObjectID CreateUpdatedTree(GitTree baseline, ReadOnlySpan<GitTreeUpdate> updates)
    {
        var handle = this.ThrowIfNull();

        GitObjectID id;
        Git2.ThrowIfError(git_tree_create_updated(&id, handle.NativeHandle, baseline.NativeHandle, (nuint)updates.Length, updates));

        return id;
    }

    public GitTree GetTree(in GitObjectID id)
    {
        return GetObject<GitTree>(in id);
    }

    public bool TryGetTree(in GitObjectID id, out GitTree tree)
    {
        return TryGetObject(in id, out tree);
    }

    #endregion

    #region Worktree
    public GitWorkTree AddWorktree(string name, string path, in GitWorktreeAddOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.Worktree* result = null;
        GitError error;

        Native.GitWorktreeAddOptions _options = default;
        List<GCHandle> gchandles = [];
        try
        {
            _options.FromManaged(in options, gchandles);

            error = git_worktree_add(&result, handle.NativeHandle, name, path, &_options);
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);

        return new(result);
    }

    public string[] GetWorktreeList()
    {
        var handle = this.ThrowIfNull();

        Native.GitStringArray _strings = default;

        Git2.ThrowIfError(git_worktree_list(&_strings, handle.NativeHandle));

        try
        {
            return _strings.ToManaged();
        }
        finally
        {
            git_strarray_dispose(&_strings);
        }
    }

    public GitWorkTree GetWorktree(string name)
    {
        var handle = this.ThrowIfNull();

        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

        Git2.Worktree* result = null;

        Git2.ThrowIfError(git_worktree_lookup(&result, handle.NativeHandle, name));

        return new(result);
    }

    public bool TryGetWorktree(string name, out GitWorkTree worktree)
    {
        var handle = this.ThrowIfNull();

        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

        Git2.Worktree* result = null;

        var error = git_worktree_lookup(&result, handle.NativeHandle, name);

        switch (error)
        {
            case GitError.OK:
                worktree = new(result);
                return true;
            case GitError.NotFound:
                worktree = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public GitReference GetHeadForWorktree(string worktreeName)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* result = null;

        Git2.ThrowIfError(git_repository_head_for_worktree(&result, handle.NativeHandle, worktreeName));

        return new(result);
    }
    #endregion

    /// <summary>
    /// Remove all the metadata associated with an ongoing command like merge, revert, cherry-pick, etc. For example: MERGE_HEAD, MERGE_MSG, etc.
    /// </summary>
    public void CleanupState()
    {
        Git2.ThrowIfError(git_repository_state_cleanup(this.NativeHandle));
    }

    public void DetachHead(out bool unbornBranch)
    {
        var handle = this.ThrowIfNull();

        var error = git_repository_detach_head(handle.NativeHandle);

        if (error is not GitError.OK and not GitError.UnbornBranch)
        {
            Git2.ThrowError(error);
        }

        unbornBranch = error == GitError.UnbornBranch;
    }

    internal GitError ForEachFetchHead(delegate* unmanaged[Cdecl]<byte*, byte*, GitObjectID*, uint, nint, int> callback, nint payload)
    {
        var handle = this.ThrowIfNull();

        return git_repository_fetchhead_foreach(handle.NativeHandle, callback, payload);
    }

    public delegate void ForEachFetchHeadCallback(string referenceName, string remoteUrl, in GitObjectID objectId, bool isMerge, ref bool breakLoop);

    [MethodImpl(MethodImplOptions.NoInlining)] // preserve callstack for exceptions
    public void ForEachFetchHead(ForEachFetchHeadCallback callback)
    {
        var handle = this.ThrowIfNull();

        var context = new Git2.CallbackContext<ForEachFetchHeadCallback>(callback);

        var error = git_repository_fetchhead_foreach(handle.NativeHandle, &_Callback, (nint)(void*)&context);

        context.ExceptionInfo?.Throw();
        Git2.ThrowIfError(error);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int _Callback(byte* ref_name, byte* remote_url, GitObjectID* oid, uint is_merge, nint payload)
        {
            ref var context = ref *(Git2.CallbackContext<ForEachFetchHeadCallback>*)payload;

            try
            {
                string referenceName = Git2.GetPooledString(ref_name);
                string remoteUrl = Git2.GetPooledString(remote_url);

                bool breakLoop = false;
                context.Callback(referenceName, remoteUrl, in *oid, is_merge != 0, ref breakLoop);

                return breakLoop ? 1 : 0;
            }
            catch (Exception e)
            {
                Debug.Assert(context.ExceptionInfo is null);
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);
                return (int)GitError.User;
            }
        }
    }

    internal GitError ForEachMergeHead(delegate* unmanaged[Cdecl]<GitObjectID*, nint, int> callback, nint payload)
    {
        var handle = this.ThrowIfNull();

        return git_repository_mergehead_foreach(handle.NativeHandle, callback, payload);
    }

    /// <summary>
    /// Callback used to iterate over each MERGE_HEAD entry
    /// </summary>
    /// <param name="objectId">The merge OID</param>
    /// <returns><see langword="true"/> to continue iterating, <see langword="false"/> to break iteration</returns>
    public delegate void ForEachMergeHeadCallback(in GitObjectID objectId, ref bool breakLoop);

    /// <summary>
    /// If a merge is in progress, invoke <paramref name="callback"/> for each commit ID in the MERGE_HEAD file.
    /// </summary>
    /// <param name="callback"></param>
    public void ForEachMergeHead(ForEachMergeHeadCallback callback)
    {
        var handle = this.ThrowIfNull();

        var context = new Git2.CallbackContext<ForEachMergeHeadCallback>(callback);

        var error = git_repository_mergehead_foreach(handle.NativeHandle, &_Callback, (nint)(void*)&context);

        context.ExceptionInfo?.Throw();
        if (error < 0 && error != GitError.NotFound)
        {
            Git2.ThrowError(error);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int _Callback(GitObjectID* objectId, nint payload)
        {
            ref var context = ref *(Git2.CallbackContext<ForEachMergeHeadCallback>*)payload;

            try
            {
                bool breakLoop = false;
                context.Callback(in *objectId, ref breakLoop);
                return breakLoop ? 1 : 0;
            }
            catch (Exception e)
            {
                Debug.Assert(context.ExceptionInfo is null);
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);
                return (int)GitError.User;
            }
        }
    }

    /// <summary>
    /// Gets the parents of the next commit, given the current repository state.
    /// Generally, this is the HEAD commit, except when performing a merge,
    /// in which case it is two or more commits.
    /// </summary>
    /// <returns>
    /// A native commit array
    /// </returns>
    /// <remarks>
    /// The returned object needs to be disposed. (<see cref="Git2.CommitArray.Dispose"/>)<br/>
    /// The lifetime of all objects in the array are connected to the lifetime of the array.
    /// Disposal of the array disposes of every underlying object. Use <see cref="GitCommit.Duplicate()"/>
    /// to copy any objects you need to be outside the lifetime of the array.
    /// </remarks>
    public Git2.CommitArray GetCommitParents()
    {
        var handle = this.ThrowIfNull();

        if (Git2.NativeLibraryVersion < new Git2.Version(1, 8, 0))
            throw new NotSupportedException($"This function isn't part of libgit2 version {Git2.NativeLibraryVersion}!");

        Git2.CommitArray array = default;

        Git2.ThrowIfError(git_repository_commit_parents(&array, handle.NativeHandle));

        return array;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string? GetCommonDirectory()
    {
        var handle = this.ThrowIfNull();

        var nativeString = git_repository_commondir(handle.NativeHandle);

        return nativeString == null ? null : Git2.GetPooledString(nativeString);
    }

    /// <summary>
    /// Get the configuration file for this repository.
    /// Needs disposal when no longer needed.
    /// </summary>
    /// <returns>The git config file object.</returns>
    public GitConfig GetConfig()
    {
        var handle = this.ThrowIfNull();

        Git2.Config* config = null;
        Git2.ThrowIfError(git_repository_config(&config, handle.NativeHandle));

        return new(config);
    }

    public GitConfig GetConfigSnapshot()
    {
        var handle = this.ThrowIfNull();

        Git2.Config* config = null;
        Git2.ThrowIfError(git_repository_config_snapshot(&config, handle.NativeHandle));

        return new(config);
    }

    public GitReference GetHead(out bool unborn)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* result = null;
        var error = git_repository_head(&result, handle.NativeHandle);

        switch (error)
        {
            case GitError.OK:
                unborn = false;
                return new(result);
            case GitError.UnbornBranch:
                unborn = true;
                return new(result);
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    /// <summary>
    /// Gets the configured identity to use for reflogs
    /// </summary>
    /// <param name="name">Identity name</param>
    /// <param name="email">Identity email</param>
    public void GetIdentity(out string name, out string email)
    {
        var handle = this.ThrowIfNull();

        byte* __name = null, __email = null;

        Git2.ThrowIfError(git_repository_ident(&__name, &__email, handle.NativeHandle));

        name = Git2.GetPooledString(__name);
        email = Git2.GetPooledString(__email);
    }

    public GitIndex GetIndex()
    {
        var handle = this.ThrowIfNull();

        Git2.Index* index = null;
        Git2.ThrowIfError(git_repository_index(&index, handle.NativeHandle));

        return new(index);
    }

    public string GetItemPath(GitRepositoryItemType itemType)
    {
        var handle = this.ThrowIfNull();

        Native.GitBuffer buffer = default;
        Git2.ThrowIfError(git_repository_item_path(&buffer, handle.NativeHandle, itemType));

        try
        {
            return Git2.GetPooledString(new ReadOnlySpan<byte>(buffer.Pointer, checked((int)buffer.Size)));
        }
        finally
        {
            git_buf_dispose(&buffer);
        }
    }

    public string? GetMessage()
    {
        var handle = this.ThrowIfNull();

        Native.GitBuffer buffer = default;

        var error = git_repository_message(&buffer, handle.NativeHandle);

        if (error == GitError.OK)
        {
            try
            {
                return Encoding.UTF8.GetString(buffer.Pointer, checked((int)buffer.Size));
            }
            finally
            {
                git_buf_dispose(&buffer);
            }
        }
        else if (error == GitError.NotFound)
        {
            return null;
        }
        else
        {
            throw Git2.ExceptionForError(error);
        }
    }

    /// <summary>
    /// Gets the currently active namespace for this repository.
    /// </summary>
    /// <remarks>
    /// Always allocates a new string.
    /// </remarks>
    /// <returns>
    /// The namespace string, or <see langword="null"/> if there isn't one.
    /// </returns>
    public string? GetNamespace()
    {
        var handle = this.ThrowIfNull();

        var nativeString = git_repository_get_namespace(handle.NativeHandle);

        return nativeString is null ? null : Git2.GetPooledString(nativeString);
    }

    public GitObjectDatabase GetObjectDatabase()
    {
        var handle = this.ThrowIfNull();

        Git2.ObjectDatabase* result = null;
        Git2.ThrowIfError(git_repository_odb(&result, handle.NativeHandle));

        return new(result);
    }

    public string GetPath()
    {
        var handle = this.ThrowIfNull();

        var nativePath = git_repository_path(handle.NativeHandle);

        return Git2.GetPooledString(nativePath);
    }

    public GitReferenceDatabase GetRefDB()
    {
        var handle = this.ThrowIfNull();

        Git2.ReferenceDatabase* refDB = null;
        Git2.ThrowIfError(git_repository_refdb(&refDB, handle.NativeHandle));

        return new(refDB);
    }

    public string[] GetTagList()
    {
        var handle = this.ThrowIfNull();

        Native.GitStringArray array = default;
        Git2.ThrowIfError(git_tag_list(&array, handle.NativeHandle));

        try
        {
            return array.ToManaged(true);
        }
        finally
        {
            git_strarray_dispose(&array);
        }
    }

    public string? GetWorkDirectory()
    {
        var handle = this.ThrowIfNull();

        var nativePath = git_repository_workdir(handle.NativeHandle);

        return nativePath == null ? null : Git2.GetPooledString(nativePath);
    }

    public void HashFile(string path, GitObjectType type, string asPath, out GitObjectID @out)
    {
        var handle = this.ThrowIfNull();

        GitError error;

        @out = default;

        fixed (GitObjectID* outPtr = &@out)
        {
            error = git_repository_hashfile(outPtr, handle.NativeHandle, path, type, asPath);
        }

        Git2.ThrowIfError(error);
    }

    public bool IsHeadDetachedForWorktree(string worktreeName)
    {
        var handle = this.ThrowIfNull();

        var code = git_repository_head_detached_for_worktree(handle.NativeHandle, worktreeName);

        return Git2.ErrorOrBoolean(code);
    }

    public void RemoveMessage()
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_repository_message_remove(handle.NativeHandle));
    }

    public void SetHead(string refName)
    {
        var handle = this.ThrowIfNull();

        ArgumentException.ThrowIfNullOrEmpty(refName);

        Git2.ThrowIfError(git_repository_set_head(handle.NativeHandle, refName));
    }

    public void SetHead(GitReference reference)
    {
        var handle = this.ThrowIfNull();

        ArgumentNullException.ThrowIfNull(reference.NativeHandle);

        Git2.ThrowIfError(git_repository_set_head(handle.NativeHandle, reference.NativeName));
    }

    public void SetHeadDetached(GitObjectID objectId)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_repository_set_head_detached(handle.NativeHandle, &objectId));
    }

    public void SetIdentity(string? name, string? email)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_repository_set_ident(handle.NativeHandle, name, email));
    }

    public void SetNamespace(string @namespace)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_repository_set_namespace(handle.NativeHandle, @namespace));
    }

    public void SetWorkDirectory(string directory, bool updateGitlink)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_repository_set_workdir(handle.NativeHandle, directory, updateGitlink ? 1 : 0));
    }

    public static GitRepository Init(string path)
    {
        return Init(path, false);
    }

    public static GitRepository Init(string path, bool bare)
    {
        Git2.Repository* result = null;
        Git2.ThrowIfError(git_repository_init(&result, path, bare ? 1u : 0u));

        return new(result);
    }

    public static GitRepository Init(string path, in GitRepositoryInitOptions options)
    {
        Native.GitRepositoryInitOptions nOptions = default;
        Git2.Repository* result = null;
        GitError error;

        try
        {
            nOptions.FromManaged(in options);

            error = git_repository_init_ext(&result, path, &nOptions);
        }
        finally
        {
            nOptions.Free();
        }

        Git2.ThrowIfError(error);
        return new(result);
    }

    public static GitRepository Init(string path, in Native.GitRepositoryInitOptions options)
    {
        Git2.Repository* result = null;
        Git2.ThrowIfError(git_repository_init_ext(&result, path, in options));

        return new(result);
    }

    public static GitRepository Open(string path)
    {
        Git2.Repository* result = null;
        Git2.ThrowIfError(git_repository_open(&result, path));

        return new(result);
    }

    public static GitRepository OpenBare(string path)
    {
        Git2.Repository* result = null;
        Git2.ThrowIfError(git_repository_open_bare(&result, path));

        return new(result);
    }

    public static GitRepository Open(string path, GitRepositoryOpenFlags flags, string? ceiling_dirs = null)
    {
        Git2.Repository* result = null;
        Git2.ThrowIfError(git_repository_open_ext(&result, path, flags, ceiling_dirs));

        return new(result);
    }

    /// <summary>
    /// Look for a Git repository and return it's path as a string.
    /// Starts searching from <paramref name="startPath"/> and if
    /// it doesn't find a repository, searches each parent directory
    /// until it finds one.
    /// </summary>
    /// <param name="startPath">The base path where the lookup starts</param>
    /// <param name="acrossFileSystems">
    /// If true, then the lookup will not stop when a filesystem device change is detected while exploring parent directories.
    /// </param>
    /// <param name="ceilingDirs">
    /// A <see cref="Git2.PathListSeparator"/> separated list of absolute symbolic link free paths.
    /// The lookup will stop when any of this paths is reached. Note that the lookup
    /// always performs on <paramref name="startPath"/> no matter <paramref name="startPath"/>
    /// appears in <paramref name="ceilingDirs"/>. <paramref name="ceilingDirs"/> might be
    /// <see langword="null"/> (which is equivalent to an empty string).
    /// </param>
    /// <returns>
    /// Returns the found git repository path, or <see langword="null"/> is one isn't found.
    /// </returns>
    public static string? Discover(string startPath, bool acrossFileSystems = true, string? ceilingDirs = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(startPath);

        Native.GitBuffer buffer = default;

        var error = git_repository_discover(&buffer, startPath, acrossFileSystems ? 1 : 0, ceilingDirs);

        switch (error)
        {
            case GitError.OK:
                try
                {
                    return Encoding.UTF8.GetString(buffer.Pointer, checked((int)buffer.Size));
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
}

