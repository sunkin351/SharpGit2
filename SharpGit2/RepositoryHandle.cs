using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Xml.Linq;

namespace SharpGit2;

public unsafe readonly partial struct RepositoryHandle(nint handle) : IDisposable
{
    internal readonly nint NativeHandle = handle;

    public static RepositoryHandle Open(string path)
    {
        RepositoryHandle repository;
        Git2.ThrowIfError(git_repository_open(&repository, path));

        return repository;
    }

    public static RepositoryHandle Init(string path, bool isBare = false)
    {
        RepositoryHandle handle;
        Git2.ThrowIfError(git_repository_init(&handle, path, isBare ? 1u : 0u));

        return handle;
    }

    public bool IsHeadDetached
    {
        get
        {
            var code = git_repository_head_detached(NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsBare
    {
        get
        {
            var code = git_repository_is_bare(NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public int State
    {
        get
        {
            return git_repository_state(NativeHandle);
        }
    }

    public void Dispose()
    {
        git_repository_free(NativeHandle);
    }

    public void CleanupState()
    {
        Git2.ThrowIfError(git_repository_state_cleanup(NativeHandle));
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

    public ReferenceEnumerable EnumerateReferences(string? glob = null)
    {
        return new ReferenceEnumerable(this, glob);
    }

    public ReferenceNameEnumerable EnumerateReferenceNames(string? glob = null)
    {
        return new ReferenceNameEnumerable(this, glob);
    }

    private const GitError ForEachBreak = (GitError)1;
    private const GitError ForEachException = GitError.User;

    internal GitError ForEachReference(delegate* unmanaged[Cdecl]<nint, nint, GitError> callback, nint payload)
    {
        return NativeApi.git_reference_foreach(NativeHandle, callback, payload);
    }

    public void ForEachReference(Func<ReferenceHandle, bool> callback, bool autoDispose = true)
    {
        var context = new ForEachContext<Func<ReferenceHandle, bool>>() { Callback = callback, AutoDispose = autoDispose };

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

        if (error == ForEachException)
        {
            context.ExceptionInfo!.Throw();
        }
        else if (error < 0)
        {
            Git2.ThrowError(error);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static GitError _Callback(nint reference, nint payload)
        {
            var referenceHandle = new ReferenceHandle(reference);

            var context = (ForEachContext<Func<ReferenceHandle, bool>>)GCHandle.FromIntPtr(payload).Target!;

            try
            {
                return context.Callback(referenceHandle) ? GitError.OK : ForEachBreak;
            }
            catch (Exception e)
            {
                Debug.Assert(context.ExceptionInfo is null);
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);

                return ForEachException;
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
        var context = new ForEachContext<ForEachReferenceUTF8NameCallback>() { Callback = callback };

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

        if (error == ForEachException)
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
            var context = (ForEachContext<ForEachReferenceUTF8NameCallback>)GCHandle.FromIntPtr(payload).Target!;

            try
            {
                var span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(name);

                return context.Callback(span) ? GitError.OK : ForEachBreak;
            }
            catch (Exception e)
            {
                Debug.Assert(context.ExceptionInfo is null);
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);

                return ForEachException;
            }
        }
    }

    public void ForEachReferenceName(Func<string, bool> callback, string? glob = null)
    {
        var context = new ForEachContext<Func<string, bool>>() { Callback = callback };

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

        if (error == ForEachException)
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
            var context = (ForEachContext<Func<string, bool>>)GCHandle.FromIntPtr(payload).Target!;

            try
            {
                string str = Utf8StringMarshaller.ConvertToManaged(name)!;

                return context.Callback(str) ? GitError.OK : ForEachBreak;
            }
            catch (Exception e)
            {
                Debug.Assert(context.ExceptionInfo is null);
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);

                return ForEachException;
            }
        }
    }

    /// <summary>
    /// Get the configuration file for this repository.
    /// Needs disposal when no longer needed.
    /// </summary>
    /// <returns>The git config file object.</returns>
    public ConfigHandle GetConfig()
    {
        ConfigHandle config;
        Git2.ThrowIfError(git_repository_config(&config, NativeHandle));

        return config;
    }

    public GitError GetHead(out ReferenceHandle head)
    {
        ReferenceHandle head_loc;
        var error = git_repository_head(&head_loc, NativeHandle);

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

    public IndexHandle GetIndex()
    {
        IndexHandle index;
        Git2.ThrowIfError(git_repository_index(&index, NativeHandle));

        return index;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Always allocates a new string.
    /// </remarks>
    /// <returns></returns>
    public string GetNamespace()
    {
        // returned pointer does not need to be freed by the user
        return Utf8StringMarshaller.ConvertToManaged(git_repository_get_namespace(NativeHandle))!;
    }

    public string GetPath()
    {
        // returned pointer does not need to be freed by the user
        return Utf8StringMarshaller.ConvertToManaged(git_repository_path(NativeHandle))!;
    }

    public RefDBHandle GetRefDB()
    {
        RefDBHandle refDB;
        Git2.ThrowIfError(git_repository_refdb(&refDB, NativeHandle));

        return refDB;
    }

    public string[] GetReferenceNameList()
    {
        Git2.git_strarray nativeArray = default;

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

    public bool HasLog(string referenceName)
    {
        var code = NativeApi.git_reference_has_log(NativeHandle, referenceName);

        return Git2.ErrorOrBoolean(code);
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
        Git2.ThrowIfError(git_repository_set_head(NativeHandle, refName));
    }

    public void SetHead(ReferenceHandle reference)
    {
        if (reference.NativeHandle == 0)
            throw new ArgumentNullException(nameof(reference));

        Git2.ThrowIfError(git_repository_set_head(NativeHandle, reference.NativeName));
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
        if (name is null)
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

    private sealed class ForEachContext<TDelegate> where TDelegate : Delegate
    {
        public required TDelegate Callback { get; init; }

        public bool AutoDispose { get; init; }

        internal ExceptionDispatchInfo? ExceptionInfo { get; set; }
    }

    public readonly struct ReferenceEnumerable(RepositoryHandle repo, string? glob) : IEnumerable<ReferenceHandle>
    {
        private readonly RepositoryHandle _repository = repo;
        private readonly string? _glob = glob;

        public ReferenceEnumerator GetEnumerator()
        {
            nint handle;

            if (_glob is null)
            {
                Git2.ThrowIfError(NativeApi.git_reference_iterator_new(&handle, _repository.NativeHandle));
            }
            else
            {
                Git2.ThrowIfError(NativeApi.git_reference_iterator_glob_new(&handle, _repository.NativeHandle, _glob));
            }

            return new ReferenceEnumerator(handle);
        }

        IEnumerator<ReferenceHandle> IEnumerable<ReferenceHandle>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct ReferenceEnumerator : IEnumerator<ReferenceHandle>
    {
        private nint _iteratorHandle;

        internal ReferenceEnumerator(nint handle) => _iteratorHandle = handle;

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
            if (_iteratorHandle != 0)
            {
                NativeApi.git_reference_iterator_free(_iteratorHandle);
                _iteratorHandle = 0;
            }
        }
    }
    public readonly struct ReferenceNameEnumerable(RepositoryHandle repo, string? glob) : IEnumerable<string>
    {
        private readonly RepositoryHandle _repository = repo;
        private readonly string? _glob = glob;

        public ReferenceNameEnumerator GetEnumerator()
        {
            nint handle;

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
        private nint _iteratorHandle;

        internal ReferenceNameEnumerator(nint handle) => _iteratorHandle = handle;

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
            if (_iteratorHandle != 0)
            {
                NativeApi.git_reference_iterator_free(_iteratorHandle);
                _iteratorHandle = 0;
            }
        }
    }
}

