using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2;

public unsafe readonly struct GitIndex : IDisposable
{
    internal readonly Git2.Index* NativeHandle;

    internal GitIndex(Git2.Index* nativeHandle)
    {
        NativeHandle = nativeHandle;
    }

    public GitIndexCapabilities Capabilities
    {
        get => NativeApi.git_index_caps(NativeHandle);
        set
        {
            Git2.ThrowIfError(NativeApi.git_index_set_caps(NativeHandle, value));
        }
    }

    public ref readonly GitObjectID Checksum => ref *NativeApi.git_index_checksum(NativeHandle);

    public nuint EntryCount => NativeApi.git_index_entrycount(NativeHandle);

    public bool HasConflicts => NativeApi.git_index_has_conflicts(NativeHandle);

    public GitRepository Owner => new(NativeApi.git_index_owner(NativeHandle));

    /// <summary>
    /// This Index's on-disk version
    /// </summary>
    /// <remarks>
    /// Valid values are 2, 3, or 4.
    /// 
    /// If 2 is given, <see cref="Write"/> may write an index with version 3 instead,
    /// if necessary to accurately represent the index.
    /// 
    /// If 3 is returned, an index with version 2 may be written instead,
    /// if the extension data in version 3 is not necessary.
    /// </remarks>
    public uint Version
    {
        get => NativeApi.git_index_version(NativeHandle);
        set => Git2.ThrowIfError(NativeApi.git_index_set_version(NativeHandle, value));
    }

    public void Dispose()
    {
        NativeApi.git_index_free(NativeHandle);
    }

    public void Add(GitIndexEntry entry)
    {
        Git2.ThrowIfError(NativeApi.git_index_add(NativeHandle, entry));
    }

    public void Add(string path)
    {
        Git2.ThrowIfError(NativeApi.git_index_add_bypath(NativeHandle, path));
    }

    public void Add(GitIndexEntry entry, ReadOnlySpan<byte> blobData)
    {
        fixed (byte* pBlob = blobData)
        {
            Git2.ThrowIfError(NativeApi.git_index_add_from_buffer(NativeHandle, entry, pBlob, (nuint)blobData.Length));
        }
    }

    /// <summary>
    /// Callback for APIs that add/remove/update files matching pathspec
    /// </summary>
    /// <returns>
    /// 0 to add, greater than 0 to skip, less than 0 to abort scan
    /// </returns>
    public delegate int MatchedPathCallback(string path, string matchedPathSpec);

    public void AddAll(string[] pathSpec, GitIndexAddOptions options)
    {
        Git2.ThrowIfError(NativeApi.git_index_add_all(NativeHandle, pathSpec, options, null, 0));
    }

    public void AddAll(string[] pathSpec, GitIndexAddOptions options, MatchedPathCallback? callback)
    {
        if (callback is null)
        {
            AddAll(pathSpec, options);
        }
        else
        {
            Git2.CallbackContext<MatchedPathCallback> context = new() { Callback = callback };

            GitError error;
            var gcHandle = GCHandle.Alloc(context, GCHandleType.Normal);
            try
            {
                error = NativeApi.git_index_add_all(NativeHandle, pathSpec, options, &MatchedPathNativeCallback, (nint)gcHandle);
            }
            finally
            {
                gcHandle.Free();
            }

            context.ExceptionInfo?.Throw();

            if (error is < 0 and not GitError.User)
                Git2.ThrowError(error);
        }
    }

    public void AddConflict(GitIndexEntry ancestor, GitIndexEntry our, GitIndexEntry their)
    {
        Git2.ThrowIfError(NativeApi.git_index_conflict_add(NativeHandle, ancestor, our, their));
    }

    public void Clear()
    {
        Git2.ThrowIfError(NativeApi.git_index_clear(NativeHandle));
    }

    public void ConflictCleanup()
    {
        Git2.ThrowIfError(NativeApi.git_index_conflict_cleanup(NativeHandle));
    }

    public ConflictEnumerable EnumerateConflicts()
    {
        return new ConflictEnumerable(this);
    }

    public Enumerable EnumerateEntries()
    {
        return new Enumerable(this);
    }

    public GitIndexEntry? GetEntry(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        return GetEntry((nuint)index);
    }
    
    public GitIndexEntry? GetEntry(nuint index)
    {
        var ptr = NativeApi.git_index_get_byindex(NativeHandle, index);

        return ptr is null ? null : new GitIndexEntry(ptr);
    }

    public GitIndexEntry? GetEntry(string path, GitIndexStageFlags stage)
    {
        var ptr = NativeApi.git_index_get_bypath(NativeHandle, path, stage);

        return ptr is null ? null : new GitIndexEntry(ptr);
    }

    ///<inheritdoc cref="NativeApi.git_index_path(Git2.Index*)"/>
    public string? GetPath()
    {
        return Utf8StringMarshaller.ConvertToManaged(NativeApi.git_index_path(NativeHandle));
    }

    public void Read(bool force)
    {
        Git2.ThrowIfError(NativeApi.git_index_read(NativeHandle, force ? 1 : 0));
    }

    public void ReadTree(GitTree tree)
    {
        Git2.ThrowIfError(NativeApi.git_index_read_tree(NativeHandle, tree.NativeHandle));
    }

    public void RemoveConflict(string path)
    {
        Git2.ThrowIfError(NativeApi.git_index_conflict_remove(NativeHandle, path));
    }

    public void Remove(string path, GitIndexStageFlags stage)
    {
        Git2.ThrowIfError(NativeApi.git_index_remove(NativeHandle, path, stage));
    }

    public void RemoveAll(string[] pathSpec)
    {
        Git2.ThrowIfError(NativeApi.git_index_remove_all(NativeHandle, pathSpec, null, 0));
    }

    public void RemoveAll(string[] pathSpec, MatchedPathCallback? callback)
    {
        if (callback is null)
        {
            RemoveAll(pathSpec);
        }
        else
        {
            var context = new Git2.CallbackContext<MatchedPathCallback>() { Callback = callback };

            var gcHandle = GCHandle.Alloc(context, GCHandleType.Normal);

            GitError error;
            try
            {
                error = NativeApi.git_index_remove_all(NativeHandle, pathSpec, &MatchedPathNativeCallback, (nint)gcHandle);
            }
            finally
            {
                gcHandle.Free();
            }

            context.ExceptionInfo?.Throw();
            Git2.ThrowIfError(error);
        }
    }

    public void RemoveByPath(string path)
    {
        Git2.ThrowIfError(NativeApi.git_index_remove_bypath(NativeHandle, path));
    }

    public void RemoveDirectory(string directory, GitIndexStageFlags stage)
    {
        Git2.ThrowIfError(NativeApi.git_index_remove_directory(NativeHandle, directory, stage));
    }

    public bool TryGetConflict(
        string path,
        [NotNullWhen(true)] out GitIndexEntry? ancestor,
        [NotNullWhen(true)] out GitIndexEntry? ours,
        [NotNullWhen(true)] out GitIndexEntry? theirs)
    {
        Native.GitIndexEntry* pAncestor, pOurs, pTheirs;

        var error = NativeApi.git_index_conflict_get(&pAncestor, &pOurs, &pTheirs, NativeHandle, path);

        switch (error)
        {
            case GitError.OK:
                ancestor = new GitIndexEntry(pAncestor);
                ours = new GitIndexEntry(pOurs);
                theirs = new GitIndexEntry(pTheirs);
                return true;
            case GitError.NotFound:
                ancestor = null;
                ours = null;
                theirs = null;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public void UpdateAll(string[] pathSpec)
    {
        Git2.ThrowIfError(NativeApi.git_index_update_all(NativeHandle, pathSpec, null, 0));
    }

    public void UpdateAll(string[] pathSpec, MatchedPathCallback? callback)
    {
        if (callback is null)
        {
            UpdateAll(pathSpec);
        }
        else
        {
            var context = new Git2.CallbackContext<MatchedPathCallback>() { Callback = callback };

            var gcHandle = GCHandle.Alloc(context, GCHandleType.Normal);

            GitError error;
            try
            {
                error = NativeApi.git_index_update_all(NativeHandle, pathSpec, &MatchedPathNativeCallback, (nint)gcHandle);
            }
            finally
            {
                gcHandle.Free();
            }

            context.ExceptionInfo?.Throw();

            if (error is < 0 and not GitError.User)
                Git2.ThrowError(error);
        }
    }

    /// <summary>
    /// Write an existing index object from memory back to disk using an atomic file lock.
    /// </summary>
    public void Write()
    {
        Git2.ThrowIfError(NativeApi.git_index_write(NativeHandle));
    }

    /// <summary>
    /// Write the index as a tree
    /// </summary>
    /// <returns>The OID of the written tree</returns>
    /// <remarks>
    /// This method will scan the index and write a representation of its current state back to disk;
    /// it recursively creates tree objects for each of the subtrees stored in the index, but only returns
    /// the OID of the root tree. This is the OID that can be used e.g. to create a commit.
    /// <br/><br/>
    /// The index instance cannot be bare, and needs to be associated to an existing repository.
    /// <br/><br/>
    /// The index must not contain any file in conflict.
    /// </remarks>
    public GitObjectID WriteTree()
    {
        GitObjectID id;
        Git2.ThrowIfError(NativeApi.git_index_write_tree(&id, NativeHandle));

        return id;
    }

    /// <summary>
    /// Write the index as a tree to the given repository
    /// </summary>
    /// <param name="repository">Repository where to write the tree</param>
    /// <returns>The OID of the written tree</returns>
    /// <remarks>
    /// This method will do the same as <see cref="WriteTree()"/>
    /// </remarks>
    public GitObjectID WriteTreeTo(GitRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository.NativeHandle);

        GitObjectID id;
        Git2.ThrowIfError(NativeApi.git_index_write_tree_to(&id, NativeHandle, repository.NativeHandle));

        return id;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MatchedPathNativeCallback(byte* path, byte* matched_pathspec, nint payload)
    {
        var context = (Git2.CallbackContext<MatchedPathCallback>)((GCHandle)payload).Target!;

        try
        {
            int ret = context.Callback(Utf8StringMarshaller.ConvertToManaged(path)!, Utf8StringMarshaller.ConvertToManaged(matched_pathspec)!);

            return ret < 0 ? (int)GitError.User : ret;
        }
        catch (Exception e)
        {
            Debug.Assert(context.ExceptionInfo is null);
            context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);
            return -1;
        }
    }

    public readonly struct Enumerable(GitIndex index) : IEnumerable<GitIndexEntry>
    {
        private readonly GitIndex _index = index;

        public Enumerator GetEnumerator()
        {
            Git2.IndexIterator* iterator;
            Git2.ThrowIfError(NativeApi.git_index_iterator_new(&iterator, _index.NativeHandle));

            return new Enumerator(iterator);
        }

        IEnumerator<GitIndexEntry> IEnumerable<GitIndexEntry>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct Enumerator : IEnumerator<GitIndexEntry>
    {
        private Git2.IndexIterator* _iterator;

        internal Enumerator(Git2.IndexIterator* iterator)
        {
            _iterator = iterator;
        }

        public GitIndexEntry Current { get; private set; } = default!;

        public bool MoveNext()
        {
            Native.GitIndexEntry* entry;

            var error = NativeApi.git_index_iterator_next(&entry, _iterator);

            switch (error)
            {
                case GitError.OK:
                    Current = new GitIndexEntry(entry);
                    return true;

                case GitError.IterationOver:
                    Current = default!;
                    return false;

                default:
                    throw Git2.ExceptionForError(error);
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            if (_iterator != null)
            {
                NativeApi.git_index_iterator_free(_iterator);
                _iterator = null;
            }
        }
    }

    public readonly struct ConflictEnumerable(GitIndex handle) : IEnumerable<(GitIndexEntry ancestor, GitIndexEntry ours, GitIndexEntry theirs)>
    {
        private readonly GitIndex _handle = handle;

        public ConflictEnumerator GetEnumerator()
        {
            Git2.IndexConflictIterator* iterator;
            Git2.ThrowIfError(NativeApi.git_index_conflict_iterator_new(&iterator, _handle.NativeHandle));

            return new ConflictEnumerator(iterator);
        }

        IEnumerator<(GitIndexEntry ancestor, GitIndexEntry ours, GitIndexEntry theirs)> IEnumerable<(GitIndexEntry ancestor, GitIndexEntry ours, GitIndexEntry theirs)>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct ConflictEnumerator : IEnumerator<(GitIndexEntry ancestor, GitIndexEntry ours, GitIndexEntry theirs)>
    {
        private Git2.IndexConflictIterator* _iterator;

        internal ConflictEnumerator(Git2.IndexConflictIterator* iterator)
        {
            _iterator = iterator;
        }

        public (GitIndexEntry ancestor, GitIndexEntry ours, GitIndexEntry theirs) Current { get; private set; }

        public bool MoveNext()
        {
            Native.GitIndexEntry* ancestor, ours, theirs;

            var error = NativeApi.git_index_conflict_next(&ancestor, &ours, &theirs, _iterator);

            switch (error)
            {
                case GitError.OK:
                    Current = (new GitIndexEntry(ancestor), new GitIndexEntry(ours), new GitIndexEntry(theirs));
                    return true;

                case GitError.IterationOver:
                    Current = default;
                    return false;

                default:
                    throw Git2.ExceptionForError(error);
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        readonly object IEnumerator.Current => Current;

        public void Dispose()
        {
            if (_iterator != null)
            {
                NativeApi.git_index_conflict_iterator_free(_iterator);
                _iterator = null;
            }
        }
    }
}
