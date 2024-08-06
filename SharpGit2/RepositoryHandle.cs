using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace SharpGit2;

public unsafe readonly partial struct RepositoryHandle : IDisposable
{
    internal readonly Git2.Repository* NativeHandle;

    internal RepositoryHandle(Git2.Repository* handle)
    {
        NativeHandle = handle;
    }

    public bool IsBare
    {
        get
        {
            var code = NativeApi.git_repository_is_bare(NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsEmpty
    {
        get
        {
            var code = NativeApi.git_repository_is_empty(NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsHeadDetached
    {
        get
        {
            var code = NativeApi.git_repository_head_detached(NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsHeadUnborn
    {
        get
        {
            var code = NativeApi.git_repository_head_unborn(NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsShallow
    {
        get
        {
            var code = NativeApi.git_repository_is_shallow(NativeHandle);

            return code != 0;
        }
    }

    public bool IsWorktree
    {
        get
        {
            var code = NativeApi.git_repository_is_worktree(NativeHandle);

            return code != 0;
        }
    }

    internal byte* NativeNamespace => NativeApi.git_repository_get_namespace(NativeHandle);

    internal byte* NativeWorkDirectory => NativeApi.git_repository_workdir(NativeHandle);

    public GitRepositoryState State => NativeApi.git_repository_state(NativeHandle);

    public void Dispose()
    {
        NativeApi.git_repository_free(NativeHandle);
    }

    /// <summary>
    /// Remove all the metadata associated with an ongoing command like merge, revert, cherry-pick, etc. For example: MERGE_HEAD, MERGE_MSG, etc.
    /// </summary>
    public void CleanupState()
    {
        Git2.ThrowIfError(NativeApi.git_repository_state_cleanup(NativeHandle));
    }

    public ReferenceHandle CreateReference(string name, GitObjectID id, bool force, string? logMessage)
    {
        ReferenceHandle result;
        Git2.ThrowIfError(NativeApi.git_reference_create(&result, NativeHandle, name, &id, force ? 1 : 0, logMessage));

        return result;
    }

    public ReferenceHandle CreateReference(string name, in GitObjectID id, bool force, string? logMessage)
    {
        ReferenceHandle result;
        GitError error;

        fixed (GitObjectID* ptr = &id)
        {
            error = NativeApi.git_reference_create(&result, NativeHandle, name, ptr, force ? 1 : 0, logMessage);
        }

        Git2.ThrowIfError(error);

        return result;
    }

    public ReferenceHandle CreateMatchingReference(string name, GitObjectID id, bool force, GitObjectID currendId, string? logMessage)
    {
        ReferenceHandle result;
        Git2.ThrowIfError(NativeApi.git_reference_create_matching(&result, NativeHandle, name, &id, force ? 1 : 0, &currendId, logMessage));

        return result;
    }

    public ReferenceHandle CreateMatchingReference(string name, in GitObjectID id, bool force, in GitObjectID currentId, string? logMessage)
    {
        ReferenceHandle result;
        fixed (GitObjectID* idPtr = &id, currentIdPtr = &currentId)
        {
            Git2.ThrowIfError(NativeApi.git_reference_create_matching(&result, NativeHandle, name, idPtr, force ? 1 : 0, currentIdPtr, logMessage));
        }

        return result;
    }

    public ReferenceHandle CreateSymbolicReference(string name, string target, bool force, string? logMessage)
    {
        ReferenceHandle result;
        Git2.ThrowIfError(NativeApi.git_reference_symbolic_create(&result, NativeHandle, name, target, force ? 1 : 0, logMessage));

        return result;
    }

    public ReferenceHandle CreateSymbolicReferenceMatching(string name, string target, bool force, string? currentValue, string? logMessage)
    {
        ReferenceHandle result;
        Git2.ThrowIfError(NativeApi.git_reference_symbolic_create_matching(&result, NativeHandle, name, target, force ? 1 : 0, currentValue, logMessage));

        return result;
    }

    public GitError DetachHead()
    {
        var error = NativeApi.git_repository_detach_head(NativeHandle);

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

        Git2.ThrowIfError(NativeApi.git_reference_ensure_log(NativeHandle, referenceName));
    }

    internal GitError ForEachFetchHead(delegate* unmanaged[Cdecl]<byte*, byte*, GitObjectID*, uint, nint, GitError> callback, nint payload)
    {
        return NativeApi.git_repository_fetchhead_foreach(NativeHandle, callback, payload);
    }

    public delegate bool ForEachFetchHeadCallback(string referenceName, string remoteUrl, in GitObjectID objectId, bool isMerge);

    public void ForEachFetchHead(ForEachFetchHeadCallback callback)
    {
        var context = new Git2.ForEachContext<ForEachFetchHeadCallback>() { Callback = callback };

        var gcHandle = GCHandle.Alloc(context, GCHandleType.Normal);
        GitError error;

        try
        {
            error = NativeApi.git_repository_fetchhead_foreach(NativeHandle, &_Callback, GCHandle.ToIntPtr(gcHandle));
        }
        finally
        {
            gcHandle.Free();
        }

        if (error == Git2.ForEachException)
        {
            context.ExceptionInfo!.Throw();
        }
        else if (error < 0)
        {
            Git2.ThrowError(error);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static GitError _Callback(byte* ref_name, byte* remote_url, GitObjectID* oid, uint is_merge, nint payload)
        {
            var context = (Git2.ForEachContext<ForEachFetchHeadCallback>)((GCHandle)payload).Target!;

            try
            {
                string referenceName = Utf8StringMarshaller.ConvertToManaged(ref_name)!;
                string remoteUrl = Utf8StringMarshaller.ConvertToManaged(remote_url)!;

                return context.Callback(referenceName, remoteUrl, in *oid, is_merge != 0) ? GitError.OK : Git2.ForEachBreak;
            }
            catch (Exception e)
            {
                Debug.Assert(context.ExceptionInfo is null);
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);
                return Git2.ForEachException;
            }
        }
    }

    internal GitError ForEachMergeHead(delegate* unmanaged[Cdecl]<GitObjectID*, nint, GitError> callback, nint payload)
    {
        return NativeApi.git_repository_mergehead_foreach(NativeHandle, callback, payload);
    }

    /// <summary>
    /// Callback used to iterate over each MERGE_HEAD entry
    /// </summary>
    /// <param name="objectId">The merge OID</param>
    /// <returns><see langword="true"/> to continue iterating, <see langword="false"/> to break iteration</returns>
    public delegate bool ForEachMergeHeadCallback(in GitObjectID objectId);

    /// <summary>
    /// If a merge is in progress, invoke <paramref name="callback"/> for each commit ID in the MERGE_HEAD file.
    /// </summary>
    /// <param name="callback"></param>
    public void ForEachMergeHead(ForEachMergeHeadCallback callback)
    {
        var context = new Git2.ForEachContext<ForEachMergeHeadCallback>() { Callback = callback };

        var gcHandle = GCHandle.Alloc(callback, GCHandleType.Normal);
        GitError error;

        try
        {
            error = NativeApi.git_repository_mergehead_foreach(NativeHandle, &_Callback, (nint)gcHandle);
        }
        finally
        {
            gcHandle.Free();
        }

        if (error == Git2.ForEachException)
        {
            context.ExceptionInfo!.Throw();
        }
        else if (error < 0 && error != GitError.NotFound)
        {
            Git2.ThrowError(error);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static GitError _Callback(GitObjectID* objectId, nint payload)
        {
            var context = (Git2.ForEachContext<ForEachMergeHeadCallback>)((GCHandle)payload).Target!;

            try
            {
                return context.Callback(in *objectId) ? GitError.OK : Git2.ForEachBreak;
            }
            catch (Exception e)
            {
                Debug.Assert(context.ExceptionInfo is null);
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);
                return Git2.ForEachException;
            }
        }
    }

    internal GitError ForEachReference(delegate* unmanaged[Cdecl]<Git2.Reference*, nint, GitError> callback, nint payload)
    {
        return NativeApi.git_reference_foreach(NativeHandle, callback, payload);
    }

    public void ForEachReference(Func<ReferenceHandle, bool> callback, bool autoDispose = true)
    {
        var context = new ForEachReferenceContext() { Callback = callback, AutoDispose = autoDispose };

        var gcHandle = GCHandle.Alloc(context, GCHandleType.Normal);
        GitError error;

        try
        {
            error = NativeApi.git_reference_foreach(NativeHandle, &_Callback, GCHandle.ToIntPtr(gcHandle));
        }
        finally
        {
            gcHandle.Free();
        }

        if (error == Git2.ForEachException)
        {
            context.ExceptionInfo!.Throw();
        }
        else if (error < 0)
        {
            Git2.ThrowError(error);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static GitError _Callback(Git2.Reference* reference, nint payload)
        {
            var referenceHandle = new ReferenceHandle(reference);

            var context = (ForEachReferenceContext)GCHandle.FromIntPtr(payload).Target!;

            try
            {
                return context.Callback(referenceHandle) ? GitError.OK : Git2.ForEachBreak;
            }
            catch (Exception e)
            {
                Debug.Assert(context.ExceptionInfo is null);
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);

                return Git2.ForEachException;
            }
            finally
            {
                if (context.AutoDispose)
                    referenceHandle.Dispose();
            }
        }
    }

    public delegate bool ForEachReferenceUTF8NameCallback(ReadOnlySpan<byte> utf8Name);

    internal GitError ForEachReferenceName(string? glob, delegate* unmanaged[Cdecl]<byte*, nint, GitError> callback, nint payload)
    {
        GitError error;
        if (glob is null)
            error = NativeApi.git_reference_foreach_name(NativeHandle, callback, payload);
        else
            error = NativeApi.git_reference_foreach_glob(NativeHandle, glob, callback, payload);

        return error;
    }

    public void ForEachReferenceName(ForEachReferenceUTF8NameCallback callback, string? glob = null)
    {
        var context = new Git2.ForEachContext<ForEachReferenceUTF8NameCallback>() { Callback = callback };

        var gcHandle = GCHandle.Alloc(context, GCHandleType.Normal);
        GitError error;

        try
        {
            if (glob is null)
                error = NativeApi.git_reference_foreach_name(NativeHandle, &_Callback, GCHandle.ToIntPtr(gcHandle));
            else
                error = NativeApi.git_reference_foreach_glob(NativeHandle, glob, &_Callback, GCHandle.ToIntPtr(gcHandle));
        }
        finally
        {
            gcHandle.Free();
        }

        if (error == Git2.ForEachException)
        {
            context.ExceptionInfo!.Throw();
        }
        else if (error < 0)
        {
            Git2.ThrowError(error);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static GitError _Callback(byte* name, nint payload)
        {
            var context = (Git2.ForEachContext<ForEachReferenceUTF8NameCallback>)GCHandle.FromIntPtr(payload).Target!;

            try
            {
                var span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(name);

                return context.Callback(span) ? GitError.OK : Git2.ForEachBreak;
            }
            catch (Exception e)
            {
                Debug.Assert(context.ExceptionInfo is null);
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);

                return Git2.ForEachException;
            }
        }
    }

    public void ForEachReferenceName(Func<string, bool> callback, string? glob = null)
    {
        var context = new Git2.ForEachContext<Func<string, bool>>() { Callback = callback };

        var gcHandle = GCHandle.Alloc(context, GCHandleType.Normal);
        GitError error;

        try
        {
            if (glob is null)
                error = NativeApi.git_reference_foreach_name(NativeHandle, &_Callback, GCHandle.ToIntPtr(gcHandle));
            else
                error = NativeApi.git_reference_foreach_glob(NativeHandle, glob, &_Callback, GCHandle.ToIntPtr(gcHandle));
        }
        finally
        {
            gcHandle.Free();
        }

        if (error == Git2.ForEachException)
        {
            context.ExceptionInfo!.Throw();
        }
        else if (error < 0)
        {
            Git2.ThrowError(error);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static GitError _Callback(byte* name, nint payload)
        {
            var context = (Git2.ForEachContext<Func<string, bool>>)GCHandle.FromIntPtr(payload).Target!;

            try
            {
                string str = Utf8StringMarshaller.ConvertToManaged(name)!;

                return context.Callback(str) ? GitError.OK : Git2.ForEachBreak;
            }
            catch (Exception e)
            {
                Debug.Assert(context.ExceptionInfo is null);
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);

                return Git2.ForEachException;
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
    /// The lifetime of all objects in the array are connected to the lifetime of the array. Disposal of the array disposes of every underlying object.
    /// </remarks>
    public Git2.CommitArray GetCommitParents()
    {
        Git2.CommitArray array = default;

        Git2.ThrowIfError(NativeApi.git_repository_commit_parents(&array, NativeHandle));

        return array;
    }

    public string? GetCommonDirectory()
    {
        return Utf8StringMarshaller.ConvertToManaged(NativeApi.git_repository_commondir(NativeHandle));
    }

    /// <summary>
    /// Get the configuration file for this repository.
    /// Needs disposal when no longer needed.
    /// </summary>
    /// <returns>The git config file object.</returns>
    public ConfigHandle GetConfig()
    {
        ConfigHandle config;
        Git2.ThrowIfError(NativeApi.git_repository_config(&config, NativeHandle));

        return config;
    }

    public ConfigHandle GetConfigSnapshot()
    {
        ConfigHandle config;
        Git2.ThrowIfError(NativeApi.git_repository_config_snapshot(&config, NativeHandle));

        return config;
    }

    public GitError GetHead(out ReferenceHandle head)
    {
        ReferenceHandle head_loc;
        var error = NativeApi.git_repository_head(&head_loc, NativeHandle);

        switch (error)
        {
            case GitError.OK:
            case GitError.UnbornBranch:
                head = head_loc;
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

        Git2.ThrowIfError(NativeApi.git_repository_ident(&__name, &__email, NativeHandle));

        name = Utf8StringMarshaller.ConvertToManaged(__name)!;
        email = Utf8StringMarshaller.ConvertToManaged(__email)!;
    }

    public IndexHandle GetIndex()
    {
        IndexHandle index;
        Git2.ThrowIfError(NativeApi.git_repository_index(&index, NativeHandle));

        return index;
    }

    public string GetItemPath(GitRepositoryItemType itemType)
    {
        Git2.Buffer buffer = default;
        Git2.ThrowIfError(NativeApi.git_repository_item_path(&buffer, NativeHandle, itemType));

        try
        {
            return Encoding.UTF8.GetString(buffer.Ptr, checked((int)buffer.Size));
        }
        finally
        {
            NativeApi.git_buf_dispose(&buffer);
        }
    }

    public string? GetMessage()
    {
        Git2.Buffer buffer = default;

        var error = NativeApi.git_repository_message(&buffer, NativeHandle);

        if (error == GitError.OK)
        {
            try
            {
                return Encoding.UTF8.GetString(buffer.Ptr, checked((int)buffer.Size));
            }
            finally
            {
                NativeApi.git_buf_dispose(&buffer);
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

    public ObjectDatabaseHandle GetObjectDatabase()
    {
        ObjectDatabaseHandle result;
        Git2.ThrowIfError(NativeApi.git_repository_odb(&result, NativeHandle));

        return result;
    }

    public string GetPath()
    {
        // returned pointer does not need to be freed by the user
        return Utf8StringMarshaller.ConvertToManaged(NativeApi.git_repository_path(NativeHandle))!;
    }

    public ReferenceDatabaseHandle GetRefDB()
    {
        ReferenceDatabaseHandle refDB;
        Git2.ThrowIfError(NativeApi.git_repository_refdb(&refDB, NativeHandle));

        return refDB;
    }

    public string[] GetReferenceNameList()
    {
        Git2.StringArray nativeArray = default;

        Git2.ThrowIfError(NativeApi.git_reference_list(&nativeArray, NativeHandle));

        try
        {
            return nativeArray.ToManaged();
        }
        finally
        {
            NativeApi.git_strarray_dispose(&nativeArray);
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
            error = NativeApi.git_repository_hashfile(outPtr, NativeHandle, path, type, asPath);
        }

        Git2.ThrowIfError(error);
    }

    public bool HasLog(string referenceName)
    {
        var code = NativeApi.git_reference_has_log(NativeHandle, referenceName);

        return Git2.ErrorOrBoolean(code);
    }

    public bool IsHeadDetachedForWorktree(string worktreeName)
    {
        var code = NativeApi.git_repository_head_detached_for_worktree(NativeHandle, worktreeName);

        return Git2.ErrorOrBoolean(code);
    }

    public void RemoveMessage()
    {
        Git2.ThrowIfError(NativeApi.git_repository_message_remove(NativeHandle));
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
        Git2.ThrowIfError(NativeApi.git_reference_remove(NativeHandle, name));
    }

    public void SetHead(string refName)
    {
        Git2.ThrowIfError(NativeApi.git_repository_set_head(NativeHandle, refName));
    }

    public void SetHead(ReferenceHandle reference)
    {
        ArgumentNullException.ThrowIfNull((Git2.Reference*)reference.NativeHandle);

        Git2.ThrowIfError(NativeApi.git_repository_set_head(NativeHandle, reference.NativeName));
    }

    public void SetHeadDetached(GitObjectID objectId)
    {
        Git2.ThrowIfError(NativeApi.git_repository_set_head_detached(NativeHandle, &objectId));
    }

    public void SetIdentity(string? name, string? email)
    {
        Git2.ThrowIfError(NativeApi.git_repository_set_ident(NativeHandle, name, email));
    }

    public void SetNamespace(string @namespace)
    {
        Git2.ThrowIfError(NativeApi.git_repository_set_namespace(NativeHandle, @namespace));
    }

    public void SetWorkDirectory(string directory, bool updateGitlink)
    {
        Git2.ThrowIfError(NativeApi.git_repository_set_workdir(NativeHandle, directory, updateGitlink ? 1 : 0));
    }

    /// <summary>
    /// Looks up a reference from the repository
    /// </summary>
    /// <param name="name">Name of the reference object</param>
    /// <param name="reference">The returned reference handle</param>
    /// <returns>true if successful, false if not or if the reference name is malformed</returns>
    /// <exception cref="Git2Exception"/>
    /// <exception cref="ArgumentNullException"/>
    public bool TryLookupReference(string name, out ReferenceHandle reference)
    {
        if (string.IsNullOrEmpty(name))
        {
            reference = default;
            return false;
        }

        ReferenceHandle refLoc;
        var result = NativeApi.git_reference_lookup(&refLoc, NativeHandle, name);

        switch (result)
        {
            case GitError.OK:
                reference = refLoc;
                return true;
            case GitError.NotFound:
            case GitError.InvalidSpec:
                reference = default;
                return false;
            default:
                throw Git2.ExceptionForError(result);
        }
    }

    public bool TryGetReferenceByShorthand(string shorthand, out ReferenceHandle reference)
    {
        ReferenceHandle result;
        var error = NativeApi.git_reference_dwim(&result, NativeHandle, shorthand);

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
        GitError result;

        // Relatively heavy struct, pin the reference instead of copying for performance
        fixed (GitObjectID* ptr = &id)
        {
            result = NativeApi.git_reference_name_to_id(ptr, NativeHandle, referenceName);
        }

        switch (result)
        {
            case GitError.OK:
                return true;
            case GitError.NotFound:
            case GitError.InvalidSpec:
                id = default;
                return false;
            default:
                throw Git2.ExceptionForError(result);
        }
    }

    private sealed class ForEachReferenceContext : Git2.ForEachContext<Func<ReferenceHandle, bool>>
    {
        public bool AutoDispose { get; init; }
    }

    public readonly struct ReferenceEnumerable(RepositoryHandle repo, string? glob) : IEnumerable<ReferenceHandle>
    {
        private readonly RepositoryHandle _repository = repo;
        private readonly string? _glob = glob;

        public ReferenceEnumerator GetEnumerator()
        {
            Git2.ReferenceIterator* handle;

            if (_glob is null)
            {
                Git2.ThrowIfError(NativeApi.git_reference_iterator_new(&handle, _repository.NativeHandle));
            }
            else
            {
                Git2.ThrowIfError(NativeApi.git_reference_iterator_glob_new(&handle, _repository.NativeHandle, _glob));
            }

            return new(handle);
        }

        IEnumerator<ReferenceHandle> IEnumerable<ReferenceHandle>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct ReferenceEnumerator : IEnumerator<ReferenceHandle>
    {
        private Git2.ReferenceIterator* _iteratorHandle;

        internal ReferenceEnumerator(Git2.ReferenceIterator* handle) => _iteratorHandle = handle;

        public ReferenceHandle Current { get; private set; }

        public bool MoveNext()
        {
            ReferenceHandle handle;
            var code = NativeApi.git_reference_next(&handle, _iteratorHandle);

            switch (code)
            {
                case GitError.OK:
                    Current = handle;
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
                NativeApi.git_reference_iterator_free(_iteratorHandle);
                _iteratorHandle = null;
            }
        }
    }

    public readonly struct ReferenceNameEnumerable(RepositoryHandle repo, string? glob) : IEnumerable<string>
    {
        private readonly RepositoryHandle _repository = repo;
        private readonly string? _glob = glob;

        public ReferenceNameEnumerator GetEnumerator()
        {
            Git2.ReferenceIterator* handle;

            if (_glob is null)
            {
                Git2.ThrowIfError(NativeApi.git_reference_iterator_new(&handle, _repository.NativeHandle));
            }
            else
            {
                Git2.ThrowIfError(NativeApi.git_reference_iterator_glob_new(&handle, _repository.NativeHandle, _glob));
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
            var code = NativeApi.git_reference_next_name(&name, _iteratorHandle);

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
            if (_iteratorHandle is not null)
            {
                NativeApi.git_reference_iterator_free(_iteratorHandle);
                _iteratorHandle = null;
            }
        }
    }
}

