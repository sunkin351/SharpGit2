using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly partial struct GitRepository(Git2.Repository* handle) : IDisposable
{
    public readonly Git2.Repository* NativeHandle = handle;

    public bool IsBare
    {
        get
        {
            var code = git_repository_is_bare(this.NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsEmpty
    {
        get
        {
            var code = git_repository_is_empty(this.NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsHeadDetached
    {
        get
        {
            var code = git_repository_head_detached(this.NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsHeadUnborn
    {
        get
        {
            var code = git_repository_head_unborn(this.NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsShallow
    {
        get
        {
            var code = git_repository_is_shallow(this.NativeHandle);

            return code != 0;
        }
    }

    public bool IsWorktree
    {
        get
        {
            var code = git_repository_is_worktree(this.NativeHandle);

            return code != 0;
        }
    }

    /// <summary>
    /// Gets the currently active namespace for this repository, or <see langword="null"/> if there isn't one.
    /// </summary>
    internal byte* NativeNamespace => git_repository_get_namespace(this.NativeHandle);

    internal byte* NativeWorkDirectory => git_repository_workdir(this.NativeHandle);

    public GitRepositoryState State => git_repository_state(this.NativeHandle);

    public void Dispose()
    {
        git_repository_free(this.NativeHandle);
    }

    #region Attributes

    public void AttributeAddMacro(string name, string values)
    {
        Git2.ThrowIfError(git_attr_add_macro(this.NativeHandle, name, values));
    }

    public void AttributeCacheFlush()
    {
        Git2.ThrowIfError(git_attr_cache_flush(this.NativeHandle));
    }

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AttributeForEach(GitAttributeCheckFlags flags, string path, ForeachAttributeCallback callbacks)
    {
        Git2.CallbackContext<ForeachAttributeCallback> context = new(callbacks);

        var error = git_attr_foreach(this.NativeHandle, flags, path, &AttributeForeachCallback, (nint)(void*)&context);

        context.ExceptionInfo?.Throw();
        if (error < 0)
            Git2.ThrowError((GitError)error);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AttributeForEach(in GitAttributeOptions options, string path, ForeachAttributeCallback callbacks)
    {
        Git2.CallbackContext<ForeachAttributeCallback> context = new(callbacks);
        Native.GitAttributeOptions nOptions = default;
        GitError error;

        try
        {
            nOptions.FromManaged(in options);

            error = (GitError)git_attr_foreach_ext(this.NativeHandle, &nOptions, path, &AttributeForeachCallback, (nint)(void*)&context);
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
            string mName = Utf8StringMarshaller.ConvertToManaged(name)!;

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
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

    public GitAttributeValue GetAttribute(GitAttributeCheckFlags flags, string path, string name)
    {
        Git2.ThrowIfError(git_attr_get(out GitAttributeValue value, this.NativeHandle, flags, path, name));

        return value;
    }

    public GitAttributeValue GetAttribute(in GitAttributeOptions options, string path, string name)
    {
        Native.GitAttributeOptions nOptions = default;
        try
        {
            nOptions.FromManaged(in options);

            Git2.ThrowIfError(git_attr_get_ext(out GitAttributeValue value, this.NativeHandle, &nOptions, path, name));

            return value;
        }
        finally
        {
            nOptions.Free();
        }
    }

    public GitAttributeValue[] GetAttributes(GitAttributeCheckFlags flags, string path, IEnumerable<string> names)
    {
        Git2.ThrowIfError(git_attr_get_many(out var values, this.NativeHandle, flags, path, names));

        return values;
    }

    public GitAttributeValue[] GetAttributes(in GitAttributeOptions options, string path, IEnumerable<string> names)
    {
        Native.GitAttributeOptions nOptions = default;
        try
        {
            nOptions.FromManaged(in options);

            Git2.ThrowIfError(git_attr_get_many_ext(out var values, this.NativeHandle, &nOptions, path, names));

            return values;
        }
        finally
        {
            nOptions.Free();
        }
    }
    #endregion

    #region Blame
    public GitBlame GetBlame(string filePath)
    {
        Git2.Blame* result;
        Git2.ThrowIfError(git_blame_file(&result, this.NativeHandle, filePath, null));
        return new(result);
    }

    public GitBlame GetBlame(string filePath, in GitBlameOptions options)
    {
        Git2.Blame* result;
        GitError error;

        Native.GitBlameOptions nOptions = default;
        List<GCHandle> gchandles = [];
        try
        {
            nOptions.FromManaged(in options, gchandles);

            error = git_blame_file(&result, this.NativeHandle, filePath, &nOptions);
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
            }

            nOptions.Free();
        }

        Git2.ThrowIfError(error);
        return new(result);
    }
    #endregion

    #region Branch
    public GitBranch CreateBranch(string branchName, GitCommit target, bool force)
    {
        Git2.Reference* result = null;
        Git2.ThrowIfError(git_branch_create(&result, this.NativeHandle, branchName, target.NativeHandle, force ? 1 : 0));

        return new(result);
    }

    public GitBranch CreateBranch(string branchName, GitAnnotatedCommit target, bool force)
    {
        Git2.Reference* result = null;
        Git2.ThrowIfError(git_branch_create_from_annotated(&result, this.NativeHandle, branchName, target.NativeHandle, force ? 1 : 0));

        return new(result);
    }

    /// <summary>
    /// Remember to free all returned objects yourself!
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public BranchEnumerable EnumerateBranches(GitBranchType filter)
    {
        return new BranchEnumerable(this, filter);
    }

    public GitBranch GetBranch(string branchName, GitBranchType type)
    {
        Git2.Reference* result;
        Git2.ThrowIfError(git_branch_lookup(&result, this.NativeHandle, branchName, type));

        return new(result);
    }

    public bool TryGetBranch(string branchName, GitBranchType type, out GitBranch branch)
    {
        Git2.Reference* result;
        var error = git_branch_lookup(&result, this.NativeHandle, branchName, type);

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

    public string? GetBranchRemoteName(string refName)
    {
        Native.GitBuffer buffer = default;

        Git2.ThrowIfError(git_branch_remote_name(&buffer, this.NativeHandle, refName));

        try
        {
            return buffer.AsString();
        }
        finally
        {
            git_buf_dispose(&buffer);
        }
    }

    public string? GetBranchUpstreamMerge(string refName)
    {
        Native.GitBuffer buffer = default;

        Git2.ThrowIfError(git_branch_upstream_merge(&buffer, this.NativeHandle, refName));

        try
        {
            return buffer.AsString();
        }
        finally
        {
            git_buf_dispose(&buffer);
        }
    }

    public string? GetBranchUpstreamName(string refName)
    {
        Native.GitBuffer buffer = default;

        Git2.ThrowIfError(git_branch_upstream_name(&buffer, this.NativeHandle, refName));

        try
        {
            return buffer.AsString();
        }
        finally
        {
            git_buf_dispose(&buffer);
        }
    }

    public string? GetBranchUpstreamRemote(string refName)
    {
        Native.GitBuffer buffer = default;

        Git2.ThrowIfError(git_branch_upstream_remote(&buffer, this.NativeHandle, refName));

        try
        {
            return buffer.AsString();
        }
        finally
        {
            git_buf_dispose(&buffer);
        }
    }

    public readonly struct BranchEnumerable(GitRepository repo, GitBranchType type) : IEnumerable<(GitBranch, GitBranchType)>
    {
        private readonly GitRepository _repository = repo;
        private readonly GitBranchType _filter = type;

        public BranchEnumerator GetEnumerator()
        {
            Git2.BranchIterator* iterator;
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
        Git2.ThrowIfError(git_checkout_tree(this.NativeHandle, (Git2.Object*)commit.NativeHandle, null));
    }

    public void Checkout(GitCommit commit, in GitCheckoutOptions options)
    {
        Git2.ThrowIfError(git_checkout_tree(this.NativeHandle, (Git2.Object*)commit.NativeHandle, in options));
    }

    public void Checkout(GitTag tag)
    {
        Git2.ThrowIfError(git_checkout_tree(this.NativeHandle, (Git2.Object*)tag.NativeHandle, null));
    }

    public void Checkout(GitTag tag, in GitCheckoutOptions options)
    {
        Git2.ThrowIfError(git_checkout_tree(this.NativeHandle, (Git2.Object*)tag.NativeHandle, in options));
    }

    public void Checkout(GitTree tree)
    {
        Git2.ThrowIfError(git_checkout_tree(this.NativeHandle, (Git2.Object*)tree.NativeHandle, null));
    }

    public void Checkout(GitTree tree, in GitCheckoutOptions options)
    {
        Git2.ThrowIfError(git_checkout_tree(this.NativeHandle, (Git2.Object*)tree.NativeHandle, in options));
    }

    public void Checkout(GitObject treeish, in GitCheckoutOptions options)
    {
        Git2.ThrowIfError(git_checkout_tree(this.NativeHandle, treeish.NativeHandle, in options));
    }

    public void Checkout(GitIndex index, in GitCheckoutOptions options)
    {
        Git2.ThrowIfError(git_checkout_index(this.NativeHandle, index.NativeHandle, in options));
    }

    public void CheckoutHead()
    {
        Git2.ThrowIfError(git_checkout_head(this.NativeHandle, null));
    }

    public void CheckoutHead(in GitCheckoutOptions options)
    {
        Git2.ThrowIfError(git_checkout_head(this.NativeHandle, in options));
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

    #region Graph
    public (nuint ahead, nuint behind) GetGraphAheadBehind(in GitObjectID local, in GitObjectID upstream)
    {
        nuint _ahead = 0, _behind = 0;
        GitError error;

        fixed (GitObjectID* pLocal = &local, pUpstream = &upstream)
            error = git_graph_ahead_behind(&_ahead, &_behind, this.NativeHandle, pLocal, pUpstream);

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
                error = git_merge(this.NativeHandle, (Git2.AnnotatedCommit**)pCommits, (nuint)commits.Length, &_merge, &_checkout);
            }
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
            }

            _merge.Free();
            _checkout.Free();
        }

        Git2.ThrowIfError(error);
    }

    public (GitMergeAnalysisResult analysis, GitMergePreference preference) GetMergeAnalysis(ReadOnlySpan<GitAnnotatedCommit> commits)
    {
        GitMergeAnalysisResult analysis = default;
        GitMergePreference preference = default;

        fixed (GitAnnotatedCommit* pCommits = commits)
        {
            Git2.ThrowIfError(git_merge_analysis(
                &analysis,
                &preference,
                this.NativeHandle,
                (Git2.AnnotatedCommit**)pCommits,
                (nuint)commits.Length));
        }

        return (analysis, preference);
    }

    public (GitMergeAnalysisResult analysis, GitMergePreference preference) GetMergeAnalysisForReference(GitReference our_ref, ReadOnlySpan<GitAnnotatedCommit> commits)
    {
        GitMergeAnalysisResult analysis = default;
        GitMergePreference preference = default;

        fixed (GitAnnotatedCommit* pCommits = commits)
        {
            Git2.ThrowIfError(git_merge_analysis_for_ref(
                &analysis,
                &preference,
                this.NativeHandle,
                our_ref.NativeHandle,
                (Git2.AnnotatedCommit**)pCommits,
                (nuint)commits.Length));
        }

        return (analysis, preference);
    }

    public GitObjectID GetMergeBase(in GitObjectID one, in GitObjectID two)
    {
        GitObjectID result;
        fixed (GitObjectID* pOne = &one, pTwo = &two)
            Git2.ThrowIfError(git_merge_base(&result, this.NativeHandle, pOne, pTwo));

        return result;
    }

    public GitObjectID GetMergeBase(ReadOnlySpan<GitObjectID> ids)
    {
        GitObjectID result;
        fixed (GitObjectID* pIds = ids)
            Git2.ThrowIfError(git_merge_base_many(&result, this.NativeHandle, (nuint)ids.Length, pIds));

        return result;
    }

    public GitObjectID GetMergeBaseOctopus(ReadOnlySpan<GitObjectID> ids)
    {
        GitObjectID result;
        fixed (GitObjectID* pIds = ids)
            Git2.ThrowIfError(git_merge_base_octopus(&result, this.NativeHandle, (nuint)ids.Length, pIds));

        return result;
    }


    public GitObjectID[] GetMergeBases(in GitObjectID one, in GitObjectID two)
    {
        Native.GitObjectIDArray array = default;

        fixed (GitObjectID* pOne = &one, pTwo = &two)
            Git2.ThrowIfError(git_merge_bases(&array, this.NativeHandle, pOne, pTwo));

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
        Native.GitObjectIDArray array = default;

        fixed (GitObjectID* pIds = ids)
            Git2.ThrowIfError(git_merge_bases_many(&array, this.NativeHandle, (nuint)ids.Length, pIds));

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
        Git2.Index* result;
        GitError error;

        Native.GitMergeOptions _options = default;
        List<GCHandle> gchandles = [];
        try
        {
            _options.FromManaged(in options, gchandles);

            error = git_merge_commits(&result, this.NativeHandle, ours.NativeHandle, theirs.NativeHandle, &_options);
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

    public GitMergeFileResult GetMergeFileFromIndex(GitIndexEntry ancestor, GitIndexEntry ours, GitIndexEntry theirs, in GitMergeFileOptions options)
    {
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

            error = git_merge_file_from_index(&_result, this.NativeHandle, &_ancestor, &_ours, &_theirs, &_options);
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
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
            result.Path = Utf8StringMarshaller.ConvertToManaged(_result.Path)!;
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

    /// <summary>
    /// Remove all the metadata associated with an ongoing command like merge, revert, cherry-pick, etc. For example: MERGE_HEAD, MERGE_MSG, etc.
    /// </summary>
    public void CleanupState()
    {
        Git2.ThrowIfError(git_repository_state_cleanup(this.NativeHandle));
    }

    public GitReference CreateReference(string name, GitObjectID id, bool force, string? logMessage)
    {
        GitReference result;
        Git2.ThrowIfError(git_reference_create((Git2.Reference**)&result, this.NativeHandle, name, &id, force ? 1 : 0, logMessage));

        return result;
    }

    public GitReference CreateReference(string name, in GitObjectID id, bool force, string? logMessage)
    {
        GitReference result;
        GitError error;

        fixed (GitObjectID* ptr = &id)
        {
            error = git_reference_create((Git2.Reference**)&result, this.NativeHandle, name, ptr, force ? 1 : 0, logMessage);
        }

        Git2.ThrowIfError(error);

        return result;
    }

    public GitReference CreateMatchingReference(string name, GitObjectID id, bool force, GitObjectID currentId, string? logMessage)
    {
        GitReference result;
        Git2.ThrowIfError(git_reference_create_matching((Git2.Reference**)&result, this.NativeHandle, name, &id, force ? 1 : 0, &currentId, logMessage));

        return result;
    }

    public GitReference CreateMatchingReference(string name, in GitObjectID id, bool force, in GitObjectID currentId, string? logMessage)
    {
        GitReference result;
        fixed (GitObjectID* idPtr = &id, currentIdPtr = &currentId)
        {
            Git2.ThrowIfError(git_reference_create_matching((Git2.Reference**)&result, this.NativeHandle, name, idPtr, force ? 1 : 0, currentIdPtr, logMessage));
        }

        return result;
    }

    public GitReference CreateSymbolicReference(string name, string target, bool force, string? logMessage)
    {
        GitReference result;
        Git2.ThrowIfError(git_reference_symbolic_create((Git2.Reference**)&result, this.NativeHandle, name, target, force ? 1 : 0, logMessage));

        return result;
    }

    public GitReference CreateSymbolicReferenceMatching(string name, string target, bool force, string? currentValue, string? logMessage)
    {
        GitReference result;
        Git2.ThrowIfError(git_reference_symbolic_create_matching((Git2.Reference**)&result, this.NativeHandle, name, target, force ? 1 : 0, currentValue, logMessage));

        return result;
    }

    public GitObjectID CreateUpdatedTree(GitTree baseline, ReadOnlySpan<GitTreeUpdate> updates)
    {
        GitObjectID id;
        Git2.ThrowIfError(git_tree_create_updated(&id, this.NativeHandle, baseline.NativeHandle, (nuint)updates.Length, updates));

        return id;
    }

    public GitError DetachHead()
    {
        var error = git_repository_detach_head(this.NativeHandle);

        if (error is not GitError.OK and not GitError.UnbornBranch)
        {
            Git2.ThrowError(error);
        }

        return error;
    }

    public ReferenceEnumerable EnumerateReferences(string? glob = null)
    {
        return new ReferenceEnumerable(this, glob);
    }

    public ReferenceNameEnumerable EnumerateReferenceNames(string? glob = null)
    {
        return new ReferenceNameEnumerable(this, glob);
    }

    public void EnsureLog(string referenceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(referenceName);

        Git2.ThrowIfError(git_reference_ensure_log(this.NativeHandle, referenceName));
    }

    internal GitError ForEachFetchHead(delegate* unmanaged[Cdecl]<byte*, byte*, GitObjectID*, uint, nint, int> callback, nint payload)
    {
        return git_repository_fetchhead_foreach(this.NativeHandle, callback, payload);
    }

    public delegate void ForEachFetchHeadCallback(string referenceName, string remoteUrl, in GitObjectID objectId, bool isMerge, ref bool breakLoop);

    [MethodImpl(MethodImplOptions.NoInlining)] // preserve callstack for exceptions
    public void ForEachFetchHead(ForEachFetchHeadCallback callback)
    {
        var context = new Git2.CallbackContext<ForEachFetchHeadCallback>(callback);

        var error = git_repository_fetchhead_foreach(this.NativeHandle, &_Callback, (nint)(void*)&context);

        context.ExceptionInfo?.Throw();
        Git2.ThrowIfError(error);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int _Callback(byte* ref_name, byte* remote_url, GitObjectID* oid, uint is_merge, nint payload)
        {
            ref var context = ref *(Git2.CallbackContext<ForEachFetchHeadCallback>*)payload;

            try
            {
                string referenceName = Utf8StringMarshaller.ConvertToManaged(ref_name)!;
                string remoteUrl = Utf8StringMarshaller.ConvertToManaged(remote_url)!;

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
        return git_repository_mergehead_foreach(this.NativeHandle, callback, payload);
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
        var context = new Git2.CallbackContext<ForEachMergeHeadCallback>() { Callback = callback };

        var error = git_repository_mergehead_foreach(this.NativeHandle, &_Callback, (nint)(void*)&context);

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

    internal GitError ForEachReference(delegate* unmanaged[Cdecl]<Git2.Reference*, nint, int> callback, nint payload)
    {
        return git_reference_foreach(this.NativeHandle, callback, payload);
    }

    public void ForEachReference(ForEachReferenceCallback callback, bool autoDispose = true)
    {
        var context = new ForEachReferenceContext(callback) { AutoDispose = autoDispose };
        var error = git_reference_foreach(this.NativeHandle, &_Callback, (nint)(void*)&context);

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

    internal GitError ForEachReferenceName(string? glob, delegate* unmanaged[Cdecl]<byte*, nint, int> callback, nint payload)
    {
        GitError error;
        if (glob is null)
            error = git_reference_foreach_name(this.NativeHandle, callback, payload);
        else
            error = git_reference_foreach_glob(this.NativeHandle, glob, callback, payload);

        return error;
    }

    public void ForEachReferenceName(ForEachReferenceUTF8NameCallback callback, string? glob = null)
    {
        var context = new Git2.CallbackContext<ForEachReferenceUTF8NameCallback>(callback);

        GitError error;

        if (glob is null)
            error = git_reference_foreach_name(this.NativeHandle, &_Callback, (nint)(void*)&context);
        else
            error = git_reference_foreach_glob(this.NativeHandle, glob, &_Callback, (nint)(void*)&context);

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

    public void ForEachReferenceName(ForEachReferenceNameCallback callback, string? glob = null)
    {
        var context = new Git2.CallbackContext<ForEachReferenceNameCallback>(callback);

        GitError error;
        if (glob is null)
            error = git_reference_foreach_name(this.NativeHandle, &_Callback, (nint)(void*)&context);
        else
            error = git_reference_foreach_glob(this.NativeHandle, glob, &_Callback, (nint)(void*)&context);

        context.ExceptionInfo?.Throw();
        Git2.ThrowIfError(error);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int _Callback(byte* name, nint payload)
        {
            ref var context = ref *(Git2.CallbackContext<ForEachReferenceNameCallback>*)payload;

            try
            {
                string str = Utf8StringMarshaller.ConvertToManaged(name)!;

                bool breakLoop = false;
                context.Callback(str, ref breakLoop);

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

    #region Annotated Commits
    public GitAnnotatedCommit GetAnnotatedCommitFromFetchHead(string branchName, string remoteUrl, GitObjectID id)
    {
        Git2.AnnotatedCommit* result;
        Git2.ThrowIfError(git_annotated_commit_from_fetchhead(&result, this.NativeHandle, branchName, remoteUrl, &id));

        return new(result);
    }

    public GitAnnotatedCommit GetAnnotatedCommitFromFetchHead(string branchName, string remoteUrl, in GitObjectID id)
    {
        Git2.AnnotatedCommit* result;
        Git2.ThrowIfError(git_annotated_commit_from_fetchhead(&result, this.NativeHandle, branchName, remoteUrl, in id));

        return new(result);
    }

    public GitAnnotatedCommit GetAnnotatedCommitFromReference(GitReference reference)
    {
        Git2.AnnotatedCommit* result;
        Git2.ThrowIfError(git_annotated_commit_from_ref(&result, this.NativeHandle, reference.NativeHandle));

        return new(result);
    }

    public GitAnnotatedCommit GetAnnotatedCommitFromRevspec(string revspec)
    {
        Git2.AnnotatedCommit* result;
        Git2.ThrowIfError(git_annotated_commit_from_revspec(&result, this.NativeHandle, revspec));

        return new(result);
    }

    public GitAnnotatedCommit GetAnnotatedCommit(GitObjectID id)
    {
        Git2.AnnotatedCommit* result;
        Git2.ThrowIfError(git_annotated_commit_lookup(&result, this.NativeHandle, &id));

        return new(result);
    }

    public GitAnnotatedCommit GetAnnotatedCommit(in GitObjectID id)
    {
        Git2.AnnotatedCommit* result;
        Git2.ThrowIfError(git_annotated_commit_lookup(&result, this.NativeHandle, in id));

        return new(result);
    }

    public bool TryGetAnnotatedCommit(GitObjectID id, out GitAnnotatedCommit commit)
    {
        Git2.AnnotatedCommit* result;
        var error = git_annotated_commit_lookup(&result, this.NativeHandle, &id);

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

    public bool TryGetAnnotatedCommit(in GitObjectID id, out GitAnnotatedCommit commit)
    {
        Git2.AnnotatedCommit* result;
        var error = git_annotated_commit_lookup(&result, this.NativeHandle, in id);

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

    public void Apply(GitDiff diff, GitApplyLocationType location, in GitApplyOptions options)
    {
        Git2.ThrowIfError(git_apply(this.NativeHandle, diff.NativeHandle, location, in options));
    }

    public GitIndex ApplyToTree(GitTree preimage, GitDiff diff, in GitApplyOptions options)
    {
        Git2.Index* result;

        Git2.ThrowIfError(git_apply_to_tree(&result, this.NativeHandle, preimage.NativeHandle, diff.NativeHandle, in options));

        return new(result);
    }

    #endregion

    #region Macros

    public void AddAttributeMacro(string name, string values)
    {

    }

    #endregion

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
    /// The lifetime of all objects in the array are connected to the lifetime of the array. Disposal of the array disposes of every underlying object.
    /// </remarks>
    //public Git2.CommitArray GetCommitParents()
    //{
    //    Git2.CommitArray array = default;

    //    Git2.ThrowIfError(git_repository_commit_parents(&array, this.NativeHandle));

    //    return array;
    //}

    public string? GetCommonDirectory()
    {
        return Utf8StringMarshaller.ConvertToManaged(git_repository_commondir(this.NativeHandle));
    }

    /// <summary>
    /// Get the configuration file for this repository.
    /// Needs disposal when no longer needed.
    /// </summary>
    /// <returns>The git config file object.</returns>
    public GitConfig GetConfig()
    {
        Git2.Config* config;
        Git2.ThrowIfError(git_repository_config(&config, this.NativeHandle));

        return new(config);
    }

    public GitConfig GetConfigSnapshot()
    {
        Git2.Config* config;
        Git2.ThrowIfError(git_repository_config_snapshot(&config, this.NativeHandle));

        return new(config);
    }

    public GitDiff GetDiff(GitTree oldTree, GitTree newTree, in GitDiffOptions options)
    {
        Git2.Diff* result;
        GitError error;
        Native.GitDiffOptions nOptions = default;
        List<GCHandle> gchandles = [];

        try
        {
            nOptions.FromManaged(in options, gchandles);

            error = git_diff_tree_to_tree(&result, this.NativeHandle, oldTree.NativeHandle, newTree.NativeHandle, &nOptions);
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
            }

            nOptions.Free();
        }

        Git2.ThrowIfError(error);

        return new(result);
    }

    public GitDiff GetDiff(GitTree oldTree, GitTree newTree, in Native.GitDiffOptions options)
    {
        Git2.Diff* result;

        fixed (Native.GitDiffOptions* pOptions = &options)
            Git2.ThrowIfError(git_diff_tree_to_tree(&result, this.NativeHandle, oldTree.NativeHandle, newTree.NativeHandle, pOptions));

        return new(result);
    }

    public GitDiff GetDiffToWorkDir(GitTree oldTree, in GitDiffOptions options)
    {
        Git2.Diff* result;
        GitError error;
        Native.GitDiffOptions nOptions = default;
        List<GCHandle> gchandles = [];

        try
        {
            nOptions.FromManaged(in options, gchandles);

            error = git_diff_tree_to_workdir(&result, this.NativeHandle, oldTree.NativeHandle, &nOptions);
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
            }

            nOptions.Free();
        }

        Git2.ThrowIfError(error);

        return new(result);
    }

    public GitDiff GetDiffToWorkDirWithIndex(GitTree oldTree, in GitDiffOptions options)
    {
        Git2.Diff* result;
        GitError error;
        Native.GitDiffOptions nOptions = default;
        List<GCHandle> gchandles = [];

        try
        {
            nOptions.FromManaged(in options, gchandles);

            error = git_diff_tree_to_workdir_with_index(&result, this.NativeHandle, oldTree.NativeHandle, &nOptions);
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
            }

            nOptions.Free();
        }

        Git2.ThrowIfError(error);

        return new(result);
    }

    public GitError GetHead(out GitReference head)
    {
        Git2.Reference* result;
        var error = git_repository_head(&result, this.NativeHandle);

        switch (error)
        {
            case GitError.OK:
            case GitError.UnbornBranch:
                head = new(result);
                return error;
            case GitError.NotFound:
                head = default;
                return error;
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
        byte* __name, __email;

        Git2.ThrowIfError(git_repository_ident(&__name, &__email, this.NativeHandle));

        name = Utf8StringMarshaller.ConvertToManaged(__name)!;
        email = Utf8StringMarshaller.ConvertToManaged(__email)!;
    }

    public GitIndex GetIndex()
    {
        Git2.Index* index;
        Git2.ThrowIfError(git_repository_index(&index, this.NativeHandle));

        return new(index);
    }

    public string GetItemPath(GitRepositoryItemType itemType)
    {
        Native.GitBuffer buffer = default;
        Git2.ThrowIfError(git_repository_item_path(&buffer, this.NativeHandle, itemType));

        try
        {
            return Encoding.UTF8.GetString(buffer.Pointer, checked((int)buffer.Size));
        }
        finally
        {
            git_buf_dispose(&buffer);
        }
    }

    public string? GetMessage()
    {
        Native.GitBuffer buffer = default;

        var error = git_repository_message(&buffer, this.NativeHandle);

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
        // returned pointer does not need to be freed by the user
        return Utf8StringMarshaller.ConvertToManaged(this.NativeNamespace);
    }

    public GitObjectDatabase GetObjectDatabase()
    {
        Git2.ObjectDatabase* result;
        Git2.ThrowIfError(git_repository_odb(&result, this.NativeHandle));

        return new(result);
    }

    public string GetPath()
    {
        // returned pointer does not need to be freed by the user
        return Utf8StringMarshaller.ConvertToManaged(git_repository_path(this.NativeHandle))!;
    }

    public GitReferenceDatabase GetRefDB()
    {
        Git2.ReferenceDatabase* refDB;
        Git2.ThrowIfError(git_repository_refdb(&refDB, this.NativeHandle));

        return new(refDB);
    }

    public GitReference GetReference(string name)
    {
        Git2.Reference* result;
        Git2.ThrowIfError(git_reference_lookup(&result, this.NativeHandle, name));

        return new(result);
    }

    public string[] GetReferenceNameList()
    {
        Native.GitStringArray nativeArray = default;

        Git2.ThrowIfError(git_reference_list(&nativeArray, this.NativeHandle));

        try
        {
            return nativeArray.ToManaged();
        }
        finally
        {
            git_strarray_dispose(&nativeArray);
        }
    }

    public string[] GetTagList()
    {
        Native.GitStringArray array = default;
        Git2.ThrowIfError(git_tag_list(&array, this.NativeHandle));

        try
        {
            return array.ToManaged();
        }
        finally
        {
            git_strarray_dispose(&array);
        }
    }

    public string? GetWorkDirectory()
    {
        return Utf8StringMarshaller.ConvertToManaged(this.NativeWorkDirectory);
    }

    public void HashFile(string path, GitObjectType type, string asPath, out GitObjectID @out)
    {
        GitError error;

        @out = default;

        fixed (GitObjectID* outPtr = &@out)
        {
            error = git_repository_hashfile(outPtr, this.NativeHandle, path, type, asPath);
        }

        Git2.ThrowIfError(error);
    }

    public bool HasLog(string referenceName)
    {
        var code = git_reference_has_log(this.NativeHandle, referenceName);

        return Git2.ErrorOrBoolean(code);
    }

    public bool IsHeadDetachedForWorktree(string worktreeName)
    {
        var code = git_repository_head_detached_for_worktree(this.NativeHandle, worktreeName);

        return Git2.ErrorOrBoolean(code);
    }

    public void RemoveMessage()
    {
        Git2.ThrowIfError(git_repository_message_remove(this.NativeHandle));
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
        Git2.ThrowIfError(git_reference_remove(this.NativeHandle, name));
    }

    public void SetHead(string refName)
    {
        Git2.ThrowIfError(git_repository_set_head(this.NativeHandle, refName));
    }

    public void SetHead(GitReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference.NativeHandle);

        Git2.ThrowIfError(git_repository_set_head(this.NativeHandle, reference.NativeName));
    }

    public void SetHeadDetached(GitObjectID objectId)
    {
        Git2.ThrowIfError(git_repository_set_head_detached(this.NativeHandle, &objectId));
    }

    public void SetIdentity(string? name, string? email)
    {
        Git2.ThrowIfError(git_repository_set_ident(this.NativeHandle, name, email));
    }

    public void SetNamespace(string @namespace)
    {
        Git2.ThrowIfError(git_repository_set_namespace(this.NativeHandle, @namespace));
    }

    public void SetWorkDirectory(string directory, bool updateGitlink)
    {
        Git2.ThrowIfError(git_repository_set_workdir(this.NativeHandle, directory, updateGitlink ? 1 : 0));
    }

    /// <summary>
    /// Looks up a reference from the repository
    /// </summary>
    /// <param name="name">Name of the reference object</param>
    /// <param name="reference">The returned reference handle</param>
    /// <returns>true if successful, false if not or if the reference name is malformed</returns>
    /// <exception cref="Git2Exception"/>
    /// <exception cref="ArgumentNullException"/>
    public bool TryLookupReference(string name, out GitReference reference)
    {
        if (string.IsNullOrEmpty(name))
        {
            reference = default;
            return false;
        }

        Git2.Reference* refPtr;
        var result = git_reference_lookup(&refPtr, this.NativeHandle, name);

        switch (result)
        {
            case GitError.OK:
                reference = new(refPtr);
                return true;
            case GitError.NotFound:
            case GitError.InvalidSpec:
                reference = default;
                return false;
            default:
                throw Git2.ExceptionForError(result);
        }
    }

    public bool TryGetReferenceByShorthand(string shorthand, out GitReference reference)
    {
        GitReference result;
        var error = git_reference_dwim((Git2.Reference**)&result, this.NativeHandle, shorthand);

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
        GitError error;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            error = git_reference_name_to_id(ptr, this.NativeHandle, referenceName);
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

    public GitCommit GetCommitForID(in GitObjectID id)
    {
        Git2.Commit* result;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            Git2.ThrowIfError(git_commit_lookup(&result, this.NativeHandle, ptr));
        }

        return new(result);
    }

    public bool TryLookupCommit(in GitObjectID id, out GitCommit commit)
    {
        GitError error;
        Git2.Commit* result;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            error = git_commit_lookup(&result, this.NativeHandle, ptr);
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

    public GitObject GetObject(in GitObjectID id, GitObjectType type)
    {
        Git2.Object* result;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            Git2.ThrowIfError(git_object_lookup(&result, this.NativeHandle, ptr, type));
        }

        return new(result);
    }

    public bool TryGetObject(in GitObjectID id, GitObjectType type, out GitObject obj)
    {
        GitError error;
        Git2.Object* result;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            error = git_object_lookup(&result, this.NativeHandle, ptr, type);
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

    public GitObject GetObjectWithPrefix(in GitObjectID id, ushort prefix_length, GitObjectType type)
    {
        Git2.Object* result;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            Git2.ThrowIfError(git_object_lookup_prefix(&result, this.NativeHandle, ptr, prefix_length, type));
        }

        return new(result);
    }

    public bool TryGetObjectWithPrefix(in GitObjectID id, ushort prefix_length, GitObjectType type, out GitObject obj)
    {
        GitError error;
        Git2.Object* result;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            error = git_object_lookup_prefix(&result, this.NativeHandle, ptr, prefix_length, type);
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

    public static GitRepository Init(string path)
    {
        return Init(path, false);
    }

    public static GitRepository Init(string path, bool bare)
    {
        Git2.Repository* result;
        Git2.ThrowIfError(git_repository_init(&result, path, bare ? 1u : 0u));

        return new(result);
    }

    public static GitRepository Init(string path, in GitRepositoryInitOptions options)
    {
        Native.GitRepositoryInitOptions nOptions = default;
        Git2.Repository* result;
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
        Git2.Repository* result;
        Git2.ThrowIfError(git_repository_init_ext(&result, path, in options));

        return new(result);
    }

    public static GitRepository Open(string path)
    {
        Git2.Repository* result;
        Git2.ThrowIfError(git_repository_open(&result, path));

        return new(result);
    }

    public static GitRepository OpenBare(string path)
    {
        Git2.Repository* result;
        Git2.ThrowIfError(git_repository_open_bare(&result, path));

        return new(result);
    }

    public static GitRepository Open(string path, GitRepositoryOpenFlags flags, string? ceiling_dirs = null)
    {
        Git2.Repository* result;
        Git2.ThrowIfError(git_repository_open_ext(&result, path, flags, ceiling_dirs));

        return new(result);
    }

    public delegate void ForEachReferenceCallback(GitReference reference, ref bool breakLoop);

    private ref struct ForEachReferenceContext
    {
        public required ForEachReferenceCallback Callback { get; init; }

        internal ExceptionDispatchInfo? ExceptionInfo { get; set; }

        [SetsRequiredMembers]
        public ForEachReferenceContext(ForEachReferenceCallback callback)
        {
            Callback = callback;
        }

        public bool AutoDispose { get; init; }
    }

    public readonly struct ReferenceEnumerable(GitRepository repo, string? glob) : IEnumerable<GitReference>
    {
        private readonly GitRepository _repository = repo;
        private readonly string? _glob = glob;

        public ReferenceEnumerator GetEnumerator()
        {
            Git2.ReferenceIterator* handle;

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
            Git2.Reference* reference;
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

        object IEnumerator.Current => throw new NotImplementedException();

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
            Git2.ReferenceIterator* handle;

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
            byte* name;
            var code = git_reference_next_name(&name, _iteratorHandle);

            switch (code)
            {
                case GitError.OK:
                    Current = Utf8StringMarshaller.ConvertToManaged(name)!;
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

        object IEnumerator.Current => this.Current;

        public void Dispose()
        {
            git_reference_iterator_free(_iteratorHandle);
            _iteratorHandle = null;
        }
    }

    public delegate void ForeachAttributeCallback(string name, GitAttributeValue value, ref bool breakLoop);

    public readonly struct AttributeCollection
    {
        private readonly GitRepository _repository;

        internal AttributeCollection(GitRepository repo) => _repository = repo;

    }
}

