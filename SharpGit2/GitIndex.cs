using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

/// <summary>
/// In-Memory representation of an index file.
/// </summary>
/// <param name="nativeHandle">Native Object Pointer</param>
public unsafe readonly struct GitIndex(Git2.Index* nativeHandle) : IDisposable, IGitHandle
{
    /// <summary>
    /// The native object pointer
    /// </summary>
    public Git2.Index* NativeHandle { get; } = nativeHandle;

    /// <inheritdoc/>
    public bool IsNull => this.NativeHandle == null;

    /// <summary>
    /// Index capability flags
    /// </summary>
    public GitIndexCapabilities Capabilities
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_index_caps(handle.NativeHandle);
        }
        set
        {
            var handle = this.ThrowIfNull();

            Git2.ThrowIfError(git_index_set_caps(handle.NativeHandle, value));
        }
    }

    /// <summary>
    /// The checksum of the Index
    /// </summary>
    /// <remarks>
    /// This checksum is the SHA-1 hash over the index file.
    /// In cases where the index does not exist on-disk,
    /// it will be zeroed out.
    /// </remarks>
    public ref readonly GitObjectID Checksum
    {
        get
        {
            var handle = this.ThrowIfNull();

            return ref *git_index_checksum(handle.NativeHandle);
        }
    }

    /// <summary>
    /// The number of entries in this Index
    /// </summary>
    public nuint EntryCount
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_index_entrycount(handle.NativeHandle);
        }
    }

    /// <summary>
    /// Determines if this Index has entries that represent file conflicts
    /// </summary>
    public bool HasConflicts
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_index_has_conflicts(handle.NativeHandle);
        }
    }

    /// <summary>
    /// Gets the repository that owns this Index
    /// </summary>
    public GitRepository Owner
    {
        get
        {
            var handle = this.ThrowIfNull();

            return new(git_index_owner(handle.NativeHandle));
        }
    }

    /// <summary>
    /// This Index's on-disk version
    /// </summary>
    /// <remarks>
    /// Valid values are 2, 3, or 4.
    /// <br/><br/>
    /// If 2 is given, <see cref="Write"/> may write an index with version 3 instead,
    /// if necessary to accurately represent the index.
    /// <br/><br/>
    /// If 3 is returned, an index with version 2 may be written instead,
    /// if the extension data in version 3 is not necessary.
    /// </remarks>
    public uint Version
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_index_version(handle.NativeHandle);
        }

        set
        {
            var handle = this.ThrowIfNull();

            Git2.ThrowIfError(git_index_set_version(handle.NativeHandle, value));
        }
    }

    /// <summary>
    /// Free's this Git Index
    /// </summary>
    /// <remarks>
    /// Do not call this more than once!
    /// </remarks>
    public void Dispose()
    {
        git_index_free(this.NativeHandle);
    }

    /// <summary>
    /// Add or update an Index entry.
    /// </summary>
    /// <param name="entry">The entry</param>
    /// <remarks>
    /// If a previous entry is found with the same path and stage as the given entry, it will be replaced.
    /// Otherwise, the given entry will be added.
    /// </remarks>
    public void AddOrUpdate(GitIndexEntry entry)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_index_add(handle.NativeHandle, entry));
    }

    /// <summary>
    /// Add or Update an Index entry from a file on disk
    /// </summary>
    /// <param name="path">Filename to add</param>
    /// <remarks>
    /// The <paramref name="path"/> must be relative to the repository's working folder
    /// and must be readable.
    /// <br/><br/>
    /// This method will fail in bare index instances.
    /// <br/><br/>
    /// This forces the file to be added to the index, not looking at gitignore rules.
    /// Those rules can be evaluated through the git_status APIs (in status.h) before
    /// calling this.
    /// <br/><br/>
    /// If this file currently is the result of a merge conflict, this file will no
    /// longer be marked as conflicting. The data about the conflict will be moved to
    /// the "resolve undo" (REUC) section.
    /// </remarks>
    /// <exception cref="Git2Exception"/>
    public void Add(string path)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_index_add_bypath(handle.NativeHandle, path));
    }

    /// <summary>
    /// Add or Update an Index entry from a buffer in memory
    /// </summary>
    /// <param name="entry">Entry to add</param>
    /// <param name="blobData">Data to be written into the blob</param>
    /// <remarks>
    /// This method will create a blob in the repository that owns
    /// the index and then add the index entry to the index. The <see cref="GitIndexEntry.Path"/>
    /// of the entry represents the position of the blob relative to
    /// the repository's root folder.
    /// <br/><br/>
    /// If a previous index entry exists that has the same path as
    /// the given <paramref name="entry"/>, it will be replaced.
    /// Otherwise, the <paramref name="entry"/> will be added.
    /// <br/><br/>
    /// This forces the file to be added to the index, not looking
    /// at gitignore rules. Those rules can be evaluated through the
    /// <c>git_status_*</c> APIs before calling this.
    /// <br/><br/>
    /// If this file currently is the result of a merge conflict,
    /// this file will no longer be marked as conflicting. The data
    /// about the conflict will be moved to the "resolve undo"
    /// (REUC) section.
    /// </remarks>
    public void Add(GitIndexEntry entry, ReadOnlySpan<byte> blobData)
    {
        var handle = this.ThrowIfNull();

        fixed (byte* pBlob = blobData)
        {
            Git2.ThrowIfError(git_index_add_from_buffer(handle.NativeHandle, entry, pBlob, (nuint)blobData.Length));
        }
    }

    /// <summary>
    /// Callback for APIs that add/remove/update files matching pathspec
    /// </summary>
    /// <returns>
    /// 0 to add, greater than 0 to skip, less than 0 to abort scan
    /// </returns>
    public delegate int MatchedPathCallback(string path, string matchedPathSpec);

    /// <summary>
    /// Add or update index entries matching files in the working directory.
    /// This method will fail if this Index is associated with a bare repository.
    /// </summary>
    /// <param name="pathSpec">List of path patterns</param>
    /// <param name="options">Combination of <see cref="GitIndexAddOptions"/> flags</param>
    /// <remarks>
    /// This method will fail in bare index instances.
    /// <br/><br/>
    /// The <paramref name="pathSpec"/> is a list of file names or shell glob patterns
    /// that will be matched against files in the repository's working directory.
    /// Each file that matches will be added to the index (either updating an existing
    /// entry or adding a new entry). You can disable glob expansion and force exact
    /// matching with the <see cref="GitIndexAddOptions.DisablePathspecMatch"/> flag.
    /// <br/><br/>
    /// Files that are ignored will be skipped (unlike <see cref="git_index_add_bypath"/>).
    /// If a file is already tracked in the index, then it will be updated even if it is
    /// ignored. Pass the <see cref="GitIndexAddOptions.Force"/> flag to skip the checking
    /// of ignore rules.
    /// <br/><br/>
    /// To emulate <c>git add -A</c> and generate an error if the pathspec contains the exact
    /// path of an ignored file (when not using FORCE), add the <see cref="GitIndexAddOptions.CheckPathspec"/>
    /// flag. This checks that each entry in the pathspec that is an exact match to a filename
    /// on disk is either not ignored or already in the index. If this check fails, the function
    /// will return <see cref="GitError.InvalidSpec"/>.
    /// <br/><br/>
    /// To emulate <c>git add -A</c> with the "dry-run" option, just use a callback function that
    /// always returns a positive value. See below for details.
    /// <br/><br/>
    /// If any files are currently the result of a merge conflict, those files will no longer
    /// be marked as conflicting. The data about the conflicts will be moved to the
    /// "resolve undo" (REUC) section.
    /// </remarks>
    public void AddOrUpdateAll(ReadOnlySpan<string> pathSpec, GitIndexAddOptions options)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_index_add_all(handle.NativeHandle, pathSpec, options, null, 0));
    }

    /// <summary>
    /// Add or update index entries matching files in the working directory.
    /// This method will fail if this Index is associated with a bare repository.
    /// </summary>
    /// <param name="pathSpec">List of path patterns</param>
    /// <param name="options">Combination of <see cref="GitIndexAddOptions"/> flags</param>
    /// <param name="callback">
    /// notification callback for each added/updated path
    /// (also gets index of matching pathspec entry);
    /// can be NULL; return 0 to add, greater than 0 to skip, and less than 0 to abort scan.
    /// </param>
    /// <remarks>
    /// This method will fail in bare index instances.
    /// <br/><br/>
    /// The <paramref name="pathSpec"/> is a list of file names or shell glob patterns
    /// that will be matched against files in the repository's working directory.
    /// Each file that matches will be added to the index (either updating an existing
    /// entry or adding a new entry). You can disable glob expansion and force exact
    /// matching with the <see cref="GitIndexAddOptions.DisablePathspecMatch"/> flag.
    /// <br/><br/>
    /// Files that are ignored will be skipped (unlike <see cref="git_index_add_bypath"/>).
    /// If a file is already tracked in the index, then it will be updated even if it is
    /// ignored. Pass the <see cref="GitIndexAddOptions.Force"/> flag to skip the checking
    /// of ignore rules.
    /// <br/><br/>
    /// To emulate <c>git add -A</c> and generate an error if the pathspec contains the exact
    /// path of an ignored file (when not using FORCE), add the <see cref="GitIndexAddOptions.CheckPathspec"/>
    /// flag. This checks that each entry in the pathspec that is an exact match to a filename
    /// on disk is either not ignored or already in the index. If this check fails, the function
    /// will return <see cref="GitError.InvalidSpec"/>.
    /// <br/><br/>
    /// To emulate <c>git add -A</c> with the "dry-run" option, just use a callback function that
    /// always returns a positive value. See below for details.
    /// <br/><br/>
    /// If any files are currently the result of a merge conflict, those files will no longer
    /// be marked as conflicting. The data about the conflicts will be moved to the
    /// "resolve undo" (REUC) section.
    /// <br/><br/>
    /// If you provide a callback function, it will be invoked on each matching item in the
    /// working directory immediately before it is added to / updated in the index. Returning
    /// zero will add the item to the index, greater than zero will skip the item, and less than
    /// zero will abort the scan and return that value to the caller.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AddOrUpdateAll(ReadOnlySpan<string> pathSpec, GitIndexAddOptions options, MatchedPathCallback? callback)
    {
        var handle = this.ThrowIfNull();

        if (callback is null)
        {
            Git2.ThrowIfError(git_index_add_all(handle.NativeHandle, pathSpec, options, null, 0));
        }
        else
        {
            Git2.CallbackContext<MatchedPathCallback> context = new(callback);

            var error = git_index_add_all(handle.NativeHandle, pathSpec, options, &MatchedPathNativeCallback, (nint)(void*)&context);

            context.ExceptionInfo?.Throw();

            if (error is < 0 and not GitError.User)
                Git2.ThrowError(error);
        }
    }

    /// <summary>
    /// Add or update index entries to represent a conflict.
    /// Any staged entries that exist at the given paths will be removed.
    /// </summary>
    /// <param name="ancestor">The entry data for the ancestor of the conflict</param>
    /// <param name="our">The entry data for our side of the merge conflict</param>
    /// <param name="their">The entry data for their side of the merge conflict</param>
    /// <remarks>
    /// The entries are the entries from the tree included in the merge.
    /// Any entry may be null to indicate that that file was not present
    /// in the trees during the merge. For example, ancestor_entry may be
    /// <see langword="null"/> to indicate that a file was added in both
    /// branches and must be resolved.
    /// </remarks>
    public void AddConflict(GitIndexEntry ancestor, GitIndexEntry our, GitIndexEntry their)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_index_conflict_add(handle.NativeHandle, ancestor, our, their));
    }

    /// <summary>
    /// Clear the contents (all the entries) of an index object.
    /// </summary>
    /// <remarks>
    /// This clears the index object in memory; changes must be explicitly
    /// written to disk for them to take effect persistently.
    /// </remarks>
    public void Clear()
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_index_clear(handle.NativeHandle));
    }

    /// <summary>
    /// Remove all conflicts in the index (entries with a stage greater than 0).
    /// </summary>
    public void ConflictCleanup()
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_index_conflict_cleanup(handle.NativeHandle));
    }

    /// <summary>
    /// Enumerate the conflicts in the index.
    /// </summary>
    /// <returns>An enumerable </returns>
    /// <remarks>
    /// The index must not be modified while iterating; the results are undefined.
    /// <br/><br/>
    /// <see cref="IDisposable.Dispose()"/> MUST be called on the resulting enumerator. (<see langword="foreach"/> does this automatically)
    /// </remarks>
    public ConflictEnumerable EnumerateConflicts()
    {
        var handle = this.ThrowIfNull();

        return new ConflictEnumerable(handle);
    }

    public Enumerable EnumerateEntries()
    {
        var handle = this.ThrowIfNull();

        return new Enumerable(handle);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public nuint? Find(string path)
    {
        var handle = this.ThrowIfNull();

        nuint idx = 0;
        var error = git_index_find(&idx, handle.NativeHandle, path);

        return error switch
        {
            GitError.OK => idx,
            GitError.NotFound => null,
            _ => throw Git2.ExceptionForError(error)
        };
    }

    public nuint? FindPrefix(string pathPrefix)
    {
        var handle = this.ThrowIfNull();

        nuint idx = 0;
        var error = git_index_find_prefix(&idx, handle.NativeHandle, pathPrefix);

        return error switch
        {
            GitError.OK => idx,
            GitError.NotFound => null,
            _ => throw Git2.ExceptionForError(error)
        };
    }

    public GitIndexEntry? GetEntry(int index)
    {
        var handle = this.ThrowIfNull();

        ArgumentOutOfRangeException.ThrowIfNegative(index);

        return GetEntry((nuint)index);
    }
    
    public GitIndexEntry? GetEntry(nuint index)
    {
        var handle = this.ThrowIfNull();

        var ptr = git_index_get_byindex(handle.NativeHandle, index);

        return ptr is null ? null : new GitIndexEntry(ptr);
    }

    public GitIndexEntry? GetEntry(string path, GitIndexStageFlags stage)
    {
        var handle = this.ThrowIfNull();

        var ptr = git_index_get_bypath(handle.NativeHandle, path, stage);

        return ptr is null ? null : new GitIndexEntry(ptr);
    }

    public string? GetPath()
    {
        var handle = this.ThrowIfNull();

        var nativePath = git_index_path(handle.NativeHandle);

        if (nativePath is null)
            return null;

        return Git2.GetPooledString(nativePath);
    }

    public void Read(bool force)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_index_read(handle.NativeHandle, force ? 1 : 0));
    }

    public void ReadTree(GitTree tree)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_index_read_tree(handle.NativeHandle, tree.NativeHandle));
    }

    public void RemoveConflict(string path)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_index_conflict_remove(handle.NativeHandle, path));
    }

    public void Remove(string path, GitIndexStageFlags stage)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_index_remove(handle.NativeHandle, path, stage));
    }

    public void RemoveAll(ReadOnlySpan<string> pathSpec)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_index_remove_all(handle.NativeHandle, pathSpec, null, 0));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RemoveAll(ReadOnlySpan<string> pathSpec, MatchedPathCallback? callback)
    {
        var handle = this.ThrowIfNull();

        if (callback is null)
        {
            Git2.ThrowIfError(git_index_remove_all(handle.NativeHandle, pathSpec, null, 0));
        }
        else
        {
            Git2.CallbackContext<MatchedPathCallback> context = new(callback);

            var error = git_index_remove_all(handle.NativeHandle, pathSpec, &MatchedPathNativeCallback, (nint)(void*)&context);

            context.ExceptionInfo?.Throw();

            if (error is < 0 and not GitError.User)
                Git2.ThrowError(error);
        }
    }

    public void RemoveByPath(string path)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_index_remove_bypath(handle.NativeHandle, path));
    }

    public void RemoveDirectory(string directory, GitIndexStageFlags stage)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_index_remove_directory(handle.NativeHandle, directory, stage));
    }

    public bool TryGetConflict(
        string path,
        [NotNullWhen(true)] out GitIndexEntry? ancestor,
        [NotNullWhen(true)] out GitIndexEntry? ours,
        [NotNullWhen(true)] out GitIndexEntry? theirs)
    {
        var handle = this.ThrowIfNull();

        Native.GitIndexEntry* pAncestor = null, pOurs = null, pTheirs = null;

        var error = git_index_conflict_get(&pAncestor, &pOurs, &pTheirs, NativeHandle, path);

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

    public void UpdateAll(ReadOnlySpan<string> pathSpec)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_index_update_all(handle.NativeHandle, pathSpec, null, 0));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UpdateAll(ReadOnlySpan<string> pathSpec, MatchedPathCallback? callback)
    {
        var handle = this.ThrowIfNull();

        if (callback is null)
        {
            Git2.ThrowIfError(git_index_update_all(handle.NativeHandle, pathSpec, null, 0));
        }
        else
        {
            Git2.CallbackContext<MatchedPathCallback> context = new(callback);

            var error = git_index_update_all(handle.NativeHandle, pathSpec, &MatchedPathNativeCallback, (nint)(void*)&context);

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
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_index_write(handle.NativeHandle));
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
        var handle = this.ThrowIfNull();

        GitObjectID id;
        Git2.ThrowIfError(git_index_write_tree(&id, NativeHandle));

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
        var handle = this.ThrowIfNull();

        ArgumentNullException.ThrowIfNull(repository.NativeHandle);

        GitObjectID id;
        Git2.ThrowIfError(git_index_write_tree_to(&id, NativeHandle, repository.NativeHandle));

        return id;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MatchedPathNativeCallback(byte* path, byte* matched_pathspec, nint payload)
    {
        ref var context = ref *(Git2.CallbackContext<MatchedPathCallback>*)payload;

        try
        {
            string _path = Git2.GetPooledString(path);
            string _pathSpec = Git2.GetPooledString(matched_pathspec);

            int ret = context.Callback(_path, _pathSpec);

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

        /// <summary>
        /// Create an Enumerator that will return every entry contained
        /// in the index at the time of creation. Entries are returned
        /// in order, sorted by path. This iterator is backed by a snapshot
        /// that allows callers to modify the index while iterating without
        /// affecting the iterator.
        /// </summary>
        /// <returns>The iterator</returns>
        /// <remarks>
        /// <see cref="IDisposable.Dispose()"/> MUST be called on the resulting enumerator. (Using <see langword="foreach"/> on the Enumerable does this automatically)
        /// </remarks>
        public Enumerator GetEnumerator()
        {
            Git2.IndexIterator* iterator = null;
            Git2.ThrowIfError(git_index_iterator_new(&iterator, _index.NativeHandle));

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
            Native.GitIndexEntry* entry = null;

            var error = git_index_iterator_next(&entry, _iterator);

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

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        readonly object IEnumerator.Current => Current;

        public void Dispose()
        {
            if (_iterator != null)
            {
                git_index_iterator_free(_iterator);
                _iterator = null;
            }
        }
    }

    public readonly struct ConflictEnumerable(GitIndex handle) : IEnumerable<(GitIndexEntry ancestor, GitIndexEntry ours, GitIndexEntry theirs)>
    {
        private readonly GitIndex _handle = handle;

        public ConflictEnumerator GetEnumerator()
        {
            Git2.IndexConflictIterator* iterator = null;
            Git2.ThrowIfError(git_index_conflict_iterator_new(&iterator, _handle.NativeHandle));

            return new ConflictEnumerator(iterator);
        }

        IEnumerator<(GitIndexEntry ancestor, GitIndexEntry ours, GitIndexEntry theirs)> IEnumerable<(GitIndexEntry ancestor, GitIndexEntry ours, GitIndexEntry theirs)>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// The conflict iterator
    /// </summary>
    public struct ConflictEnumerator : IEnumerator<(GitIndexEntry ancestor, GitIndexEntry ours, GitIndexEntry theirs)>
    {
        private Git2.IndexConflictIterator* _iterator;

        internal ConflictEnumerator(Git2.IndexConflictIterator* iterator)
        {
            _iterator = iterator;
        }

        /// <inheritdoc/>
        public (GitIndexEntry ancestor, GitIndexEntry ours, GitIndexEntry theirs) Current { get; private set; }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            Native.GitIndexEntry* ancestor = null, ours = null, theirs = null;

            var error = git_index_conflict_next(&ancestor, &ours, &theirs, _iterator);

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

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        readonly object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_iterator != null)
            {
                git_index_conflict_iterator_free(_iterator);
                _iterator = null;
            }
        }
    }
}
