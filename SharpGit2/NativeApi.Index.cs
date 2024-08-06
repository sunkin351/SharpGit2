using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

using SharpGit2.Marshalling;

namespace SharpGit2;

internal unsafe partial class NativeApi
{
    /// <summary>
    /// Add or update an index entry from an in-memory struct
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="sourceEntry">new entry object</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// If a previous index entry exists that has the same path and stage as the given 'source_entry', it will be replaced. Otherwise, the 'source_entry' will be added.
    /// <br/><br/>
    /// A full copy (including the 'path' string) of the given 'source_entry' will be inserted on the index.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_add(Git2.Index* index, GitIndexEntry.Unmanaged* sourceEntry);

    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_add(Git2.Index* index, GitIndexEntry sourceEntry);

    /// <summary>
    /// Add or update index entries matching files in the working directory.
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="pathSpec">array of path patterns</param>
    /// <param name="options">combination of git_index_add_option_t flags</param>
    /// <param name="callback">
    /// notification callback for each added/updated path
    /// (also gets index of matching pathspec entry);
    /// can be NULL; return 0 to add, greater than 0 to skip, and less than 0 to abort scan.
    /// </param>
    /// <param name="payload">
    /// payload passed through to callback function
    /// </param>
    /// <returns>
    /// 0 on success, negative callback return value, or error code
    /// </returns>
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
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_add_all(
        Git2.Index* index,
        [MarshalUsing(typeof(StringArrayMarshaller))] string[] pathSpec,
        GitIndexAddOptions options,
        delegate* unmanaged[Cdecl]<byte*, byte*, nint, int> callback,
        nint payload);

    /// <summary>
    /// Add or update an index entry from a file on disk
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="path">filename to add</param>
    /// <returns>0 or an error code</returns>
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
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_add_bypath(Git2.Index* index, string path);

    /// <summary>
    /// Add or update an index entry from a buffer in memory
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="entry">entry to add</param>
    /// <param name="buffer">data to be written into the blob</param>
    /// <param name="length">length of the data</param>
    /// <returns>0 or an error code</returns>
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
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_add_from_buffer(Git2.Index* index, GitIndexEntry entry, byte* buffer, nuint length);

    /// <summary>
    /// Read index capabilities flags.
    /// </summary>
    /// <param name="index">An existing index object</param>
    /// <returns>
    /// A combination of <see cref="GitIndexCapabilities"/> values.
    /// </returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitIndexCapabilities git_index_caps(Git2.Index* index);

    /// <summary>
    /// Get the checksum of the index
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <returns>a pointer to the checksum of the index</returns>
    /// <remarks>
    /// This checksum is the SHA-1 hash over the index file
    /// (except the last 20 bytes which are the checksum itself).
    /// In cases where the index does not exist on-disk,
    /// it will be zeroed out.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitObjectID* git_index_checksum(Git2.Index* index);

    /// <summary>
    /// Clear the contents (all the entries) of an index object.
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <returns>0 on success, error code less than 0 on failure</returns>
    /// <remarks>
    /// This clears the index object in memory; changes must be explicitly
    /// written to disk for them to take effect persistently.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_clear(Git2.Index* index);

    /// <summary>
    /// Add or update index entries to represent a conflict.
    /// Any staged entries that exist at the given paths will be removed.
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="ancestor_entry">the entry data for the ancestor of the conflict</param>
    /// <param name="our_entry">the entry data for our side of the merge conflict</param>
    /// <param name="their_entry">the entry data for their side of the merge conflict</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The entries are the entries from the tree included in the merge.
    /// Any entry may be null to indicate that that file was not present
    /// in the trees during the merge. For example, ancestor_entry may be
    /// <see langword="null"/> to indicate that a file was added in both
    /// branches and must be resolved.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_conflict_add(
        Git2.Index* index,
        GitIndexEntry ancestor_entry,
        GitIndexEntry our_entry,
        GitIndexEntry their_entry);

    /// <summary>
    /// Remove all conflicts in the index (entries with a stage greater than 0).
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_conflict_cleanup(Git2.Index* index);

    /// <summary>
    /// Get the index entries that represent a conflict of a single file.
    /// </summary>
    /// <param name="ancestor_out">Pointer to store the ancestor entry</param>
    /// <param name="our_out">Pointer to store the our entry</param>
    /// <param name="their_out">Pointer to store the their entry</param>
    /// <param name="index">an existing index object</param>
    /// <param name="path">path to search</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The entries are not modifiable and should not be freed.
    /// Because the <see cref="GitIndexEntry.Unmanaged"/> struct
    /// is a publicly defined struct, you should be able to make
    /// your own permanent copy of the data if necessary.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_conflict_get(
        GitIndexEntry.Unmanaged** ancestor_out,
        GitIndexEntry.Unmanaged** our_out,
        GitIndexEntry.Unmanaged** their_out,
        Git2.Index* index,
        string path);

    /// <summary>
    /// Frees a <see cref="Git2.IndexConflictIterator"/> object
    /// </summary>
    /// <param name="iterator">pointer to the iterator</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_index_conflict_iterator_free(Git2.IndexConflictIterator* iterator);

    /// <summary>
    /// Create an iterator for the conflicts in the index.
    /// </summary>
    /// <param name="iterator_out">The newly created conflict iterator</param>
    /// <param name="index">The index to scan</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The index must not be modified while iterating; the results are undefined.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_conflict_iterator_new(Git2.IndexConflictIterator** iterator_out, Git2.Index* index);

    /// <summary>
    /// Returns the current conflict (ancestor, ours and theirs entry)
    /// and advance the iterator internally to the next value.
    /// </summary>
    /// <param name="ancestor_out">Pointer to store the ancestor side of the conflict</param>
    /// <param name="our_out">Pointer to store our side of the conflict</param>
    /// <param name="their_out">Pointer to store their side of the conflict</param>
    /// <param name="iterator">The conflict iterator.</param>
    /// <returns>0 on success, <see cref="GitError.IterationOver"/> when iteration is complete, or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_conflict_next(
        GitIndexEntry.Unmanaged** ancestor_out,
        GitIndexEntry.Unmanaged** our_out,
        GitIndexEntry.Unmanaged** their_out, 
        Git2.IndexConflictIterator* iterator);

    /// <summary>
    /// Removes the index entries that represent a conflict of a single file.
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="path">path to remove conflicts for</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_conflict_remove(Git2.Index* index, string path);

    /// <summary>
    /// Get the count of entries currently in the index
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <returns>integer count of current entries</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial nuint git_index_entrycount(Git2.Index* index);

    /// <summary>
    /// Find the first position of any entries which point to given path in the Git index.
    /// </summary>
    /// <param name="position">the address to which the position of the index entry is written (optional)</param>
    /// <param name="index">an existing index object</param>
    /// <param name="path">path to search</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_find(nuint* position, Git2.Index* index, string path);

    /// <summary>
    /// Find the first position of any entries matching a prefix. To find the first position of a path inside a given folder, suffix the prefix with a '/'.
    /// </summary>
    /// <param name="position">the address to which the position of the index entry is written (optional)</param>
    /// <param name="index">an existing index object</param>
    /// <param name="path">the prefix to search for</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_find_prefix(nuint* position, Git2.Index* index, string path);

    /// <summary>
    /// Free an existing index object.
    /// </summary>
    /// <param name="index">an existing index object</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_index_free(Git2.Index* index);

    /// <summary>
    /// Get a pointer to one of the entries in the index
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="n">the position of the entry</param>
    /// <returns>a pointer to the entry; NULL if out of bounds</returns>
    /// <remarks>
    /// The entry is not modifiable and should not be freed.
    /// Because the <see cref="GitIndexEntry.Unmanaged"/> struct
    /// is a publicly defined struct, you should be able to make
    /// your own permanent copy of the data if necessary.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitIndexEntry.Unmanaged* git_index_get_byindex(Git2.Index* index, nuint n);

    /// <summary>
    /// Get a pointer to one of the entries in the index
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="path">path to search</param>
    /// <param name="stage">stage to search</param>
    /// <returns>a pointer to the entry; <see langword="null"/> if it was not found</returns>
    /// <remarks>
    /// The entry is not modifiable and should not be freed.
    /// Because the <see cref="GitIndexEntry.Unmanaged"/> struct
    /// is a publicly defined struct, you should be able to make
    /// your own permanent copy of the data if necessary.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitIndexEntry.Unmanaged* git_index_get_bypath(Git2.Index* index, string path, GitIndexStageFlags stage);

    /// <summary>
    /// Determine if the index contains entries representing file conflicts.
    /// </summary>
    /// <param name="index">An existing index object.</param>
    /// <returns><see langword="true"/> if at least one conflict is found, <see langword="false"/> otherwise.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static partial bool git_index_has_conflicts(Git2.Index* index);

    /// <summary>
    /// Free the index iterator
    /// </summary>
    /// <param name="iterator">The iterator to free</param>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_index_iterator_free(Git2.IndexIterator* iterator);

    /// <summary>
    /// Create an iterator that will return every entry contained
    /// in the index at the time of creation. Entries are returned
    /// in order, sorted by path. This iterator is backed by a snapshot
    /// that allows callers to modify the index while iterating without
    /// affecting the iterator.
    /// </summary>
    /// <param name="iterator_out">The newly created iterator</param>
    /// <param name="index">The index to iterate</param>
    /// <returns>0 or an error code.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_iterator_new(Git2.IndexIterator** iterator_out, Git2.Index* index);

    /// <summary>
    /// Return the next index entry in-order from the iterator.
    /// </summary>
    /// <param name="entry_out">Pointer to store the index entry in</param>
    /// <param name="iterator">The iterator</param>
    /// <returns>0 on success, <see cref="GitError.IterationOver"/> on iterator completion, or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_iterator_next(GitIndexEntry.Unmanaged** entry_out, Git2.IndexIterator* iterator);

    /// <summary>
    /// Get the repository this index relates to
    /// </summary>
    /// <param name="index">The index</param>
    /// <returns>A pointer to the repository</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial Git2.Repository* git_index_owner(Git2.Index* index);

    /// <summary>
    /// Get the full path to the index file on disk.
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <returns>path to index file or NULL for in-memory index</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial byte* git_index_path(Git2.Index* index);

    /// <summary>
    /// Update the contents of an existing index object in memory by reading from the hard disk.
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="force">if true, always reload, vs. only read if file has changed</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// If <paramref name="force"/> is true, this performs a "hard" read that discards
    /// in-memory changes and always reloads the on-disk index data. If there is no
    /// on-disk version, the index will be cleared.
    /// <br/><br/>
    /// If <paramref name="force"/> is false, this does a "soft" read that reloads the
    /// index data from disk only if it has changed since the last time it was loaded.
    /// Purely in-memory index data will be untouched. Be aware: if there are changes
    /// on disk, unwritten in-memory changes are discarded.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_read(Git2.Index* index, int force);

    /// <summary>
    /// Read a tree into the index file with stats
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="force">tree to read</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The current index contents will be replaced by the specified tree.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_read_tree(Git2.Index* index, Git2.Tree* tree);

    /// <summary>
    /// Remove an entry from the index
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="path">path to search</param>
    /// <param name="stage">stage to search</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_remove(Git2.Index* index, string path, GitIndexStageFlags stage);

    /// <summary>
    /// Remove all matching index entries.
    /// </summary>
    /// <param name="index">An existing index object</param>
    /// <param name="pathSpec">array of path patterns</param>
    /// <param name="callback">
    /// notification callback for each removed path (also gets index of matching pathspec entry); can be NULL; return 0 to add, greater than 0 to skip, less than 0 to abort scan.
    /// </param>
    /// <param name="payload">payload passed through to callback function</param>
    /// <returns>0 on success, negative callback return value, or error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_remove_all(
        Git2.Index* index,
        [MarshalUsing(typeof(StringArrayMarshaller))] string[] pathSpec,
        delegate* unmanaged[Cdecl]<byte*, byte*, nint, int> callback,
        nint payload);

    /// <summary>
    /// Remove an index entry corresponding to a file on disk
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="path">filename to remove</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The <paramref name="path"/> must be relative to the repository's
    /// working folder. It may exist.
    /// <br/><br/>
    /// If this file currently is the result of a merge conflict, this
    /// file will no longer be marked as conflicting. The data about the
    /// conflict will be moved to the "resolve undo" (REUC) section.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_remove_bypath(Git2.Index* index, string path);

    /// <summary>
    /// Remove all entries from the index under a given directory
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <param name="directory">container directory path</param>
    /// <param name="stage">stage to search</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_remove_directory(Git2.Index* index, string directory, GitIndexStageFlags stage);

    /// <summary>
    /// Set index capabilities flags.
    /// </summary>
    /// <param name="index">An existing index object</param>
    /// <param name="caps">A combination of GIT_INDEX_CAPABILITY values</param>
    /// <returns>0 on success, -1 on failure</returns>
    /// <remarks>
    /// If you pass <see cref="GitIndexCapabilities.FromOwner"/> for <paramref name="caps"/>,
    /// then capabilities will be read from the config of the owner object, looking at
    /// <c>core.ignorecase</c>, <c>core.filemode</c>, and <c>core.symlinks</c>
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_set_caps(Git2.Index* index, GitIndexCapabilities caps);

    /// <summary>
    /// Set index on-disk version.
    /// </summary>
    /// <param name="index">An existing index object</param>
    /// <param name="version">The new version number</param>
    /// <returns>0 on success, -1 on failure</returns>
    /// <remarks>
    /// Valid values are 2, 3, or 4. If 2 is given, git_index_write may write an index with version 3 instead, if necessary to accurately represent the index.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_set_version(Git2.Index* index, uint version);

    /// <summary>
    /// Update all index entries to match the working directory
    /// </summary>
    /// <param name="index">An existing index object</param>
    /// <param name="pathSpec">array of path patterns</param>
    /// <param name="callback">
    /// notification callback for each updated path (also gets index of matching pathspec entry);
    /// can be NULL; return 0 to add, greater than 0 to skip, less than 0 to abort scan.
    /// </param>
    /// <param name="payload">payload passed through to callback function</param>
    /// <returns>0 on success, negative callback return value, or error code</returns>
    /// <remarks>
    /// This method will fail in bare index instances.
    /// <br/><br/>
    /// This scans the existing index entries and synchronizes them with the working directory,
    /// deleting them if the corresponding working directory file no longer exists otherwise
    /// updating the information (including adding the latest version of file to the ODB if needed).
    /// <br/><br/>
    /// If you provide a callback function, it will be invoked on each matching item in the index
    /// immediately before it is updated (either refreshed or removed depending on working directory
    /// state). Return 0 to proceed with updating the item, greater than 0 to skip the item,
    /// and less than 0 to abort the scan.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_update_all(
        Git2.Index* index,
        [MarshalUsing(typeof(StringArrayMarshaller))] string[] pathSpec,
        delegate* unmanaged[Cdecl]<byte*, byte*, nint, int> callback,
        nint payload);

    /// <summary>
    /// Get index on-disk version.
    /// </summary>
    /// <param name="index">An existing index object</param>
    /// <returns>the index version</returns>
    /// <remarks>
    /// Valid return values are 2, 3, or 4. If 3 is returned,
    /// an index with version 2 may be written instead,
    /// if the extension data in version 3 is not necessary.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial uint git_index_version(Git2.Index* index);

    /// <summary>
    /// Write an existing index object from memory back to disk using an atomic file lock.
    /// </summary>
    /// <param name="index">an existing index object</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_write(Git2.Index* index);

    /// <summary>
    /// Write the index as a tree
    /// </summary>
    /// <param name="id_out">Pointer where to store the OID of the written tree</param>
    /// <param name="index">Index to write</param>
    /// <returns>0 on success, <see cref="GitError.Unmerged"/> when the index is not clean, or an error code</returns>
    /// <remarks>
    /// This method will scan the index and write a representation of its current state back to disk;
    /// it recursively creates tree objects for each of the subtrees stored in the index, but only returns
    /// the OID of the root tree. This is the OID that can be used e.g. to create a commit.
    /// <br/><br/>
    /// The index instance cannot be bare, and needs to be associated to an existing repository.
    /// <br/><br/>
    /// The index must not contain any file in conflict.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_write_tree(GitObjectID* id_out, Git2.Index* index);

    /// <summary>
    /// Write the index as a tree to the given repository
    /// </summary>
    /// <param name="id_out">Pointer where to store the OID of the written tree</param>
    /// <param name="index">Index to write</param>
    /// <param name="repo">Repository where to write the tree</param>
    /// <returns>0 on success, <see cref="GitError.Unmerged"/> when the index is not clean, or an error code</returns>
    /// <remarks>
    /// This method will do the same as <see cref="git_index_write_tree(GitObjectID*, Git2.Index*)"/>,
    /// but letting the user choose the repository where the tree will be written.
    /// <br/><br/>
    /// The index must not contain any file in conflict.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_index_write_tree_to(GitObjectID* id_out, Git2.Index* index, Git2.Repository* repo);
}
