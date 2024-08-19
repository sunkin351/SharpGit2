using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SharpGit2.Native;

namespace SharpGit2;

internal unsafe static partial class NativeApi
{
    /// <summary>
    /// Create a tree based on another one with the specified modifications
    /// </summary>
    /// <param name="id_out">id of the new tree</param>
    /// <param name="repo">
    /// the repository in which to create the tree, must be the same as for <paramref name="baseline"/>
    /// </param>
    /// <param name="baseline">the tree to base these changes on</param>
    /// <param name="updateCount">the number of elements in the update list</param>
    /// <param name="updates">the list of updates to perform</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// Given the <paramref name="baseline"/>, perform the changes described
    /// in the list of updates and create a new tree.
    /// 
    /// This function is optimized for common file/directory addition,
    /// removal and replacement in trees. It is much more efficient than
    /// reading the tree into a git_index and modifying that, but in
    /// exchange it is not as flexible.
    /// 
    /// Deleting and adding the same entry is undefined behaviour,
    /// changing a tree to a blob or viceversa is not supported.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_tree_create_updated(
        GitObjectID* id_out,
        Git2.Repository* repo,
        Git2.Tree* baseline,
        nuint updateCount,
        ReadOnlySpan<GitTreeUpdate> updates);

    /// <summary>
    /// Create an in-memory copy of a tree. The copy must be explicitly free'd
    /// or it will result in a memory leak.
    /// </summary>
    /// <param name="tree_out">Pointer to store the copy of the tree</param>
    /// <param name="tree">Original tree to copy</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_tree_dup(Git2.Tree** tree_out, Git2.Tree* tree);

    /// <summary>
    /// Lookup a tree entry by SHA value
    /// </summary>
    /// <param name="tree">a previously loaded tree.</param>
    /// <param name="id">the sha being looked for</param>
    /// <returns>the tree entry; NULL if not found</returns>
    /// <remarks>
    /// This returns a git_tree_entry that is owned by the git_tree.
    /// You don't have to free it, but you must not use it after the git_tree is released.
    /// 
    /// Warning: this must examine every entry in the tree, so it is not fast.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial Git2.TreeEntry* git_tree_entry_byid(Git2.Tree* tree, GitObjectID* id);

    /// <summary>
    /// Lookup a tree entry by its position in the tree
    /// </summary>
    /// <param name="tree">a previously loaded tree</param>
    /// <param name="index">the position in the entry list</param>
    /// <returns>the tree entry; NULL if not found</returns>
    /// <remarks>
    /// This returns a git_tree_entry that is owned by the git_tree.
    /// You don't have to free it, but you must not use it after the git_tree is released.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial Git2.TreeEntry* git_tree_entry_byindex(Git2.Tree* tree, nuint index);

    /// <summary>
    /// Lookup a tree entry by its filename
    /// </summary>
    /// <param name="tree">a previously loaded tree.</param>
    /// <param name="filename">the filename of the desired entry</param>
    /// <returns>he tree entry; NULL if not found</returns>
    /// <remarks>
    /// This returns a git_tree_entry that is owned by the git_tree.
    /// You don't have to free it, but you must not use it after the git_tree is released.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial Git2.TreeEntry* git_tree_entry_byname(Git2.Tree* tree, string filename);

    /// <summary>
    /// Retrieve a tree entry contained in a tree or in any of its subtrees, given its relative path.
    /// </summary>
    /// <param name="entry_out">Pointer where to store the tree entry</param>
    /// <param name="tree">Previously loaded tree which is the root of the relative path</param>
    /// <param name="path">Path to the contained entry</param>
    /// <returns>0 on success; <see cref="GitError.NotFound"/> if the path does not exist</returns>
    /// <remarks>
    /// Unlike the other lookup functions, the returned tree entry
    /// is owned by the user and must be freed explicitly with
    /// <see cref="git_tree_entry_free"/>
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_tree_entry_bypath(Git2.TreeEntry** entry_out, Git2.Tree* tree, string path);

    /// <summary>
    /// Compare two tree entries
    /// </summary>
    /// <param name="entry1">first tree entry</param>
    /// <param name="entry2">second tree entry</param>
    /// <returns>Standard <see cref="IComparable{T}"/> behavior</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_tree_entry_cmp(Git2.TreeEntry* entry1, Git2.TreeEntry* entry2);

    /// <summary>
    /// Duplicate a tree entry
    /// </summary>
    /// <param name="entry_out">pointer where to store the copy</param>
    /// <param name="entry">tree entry to duplicate</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// Create a copy of a tree entry. The returned copy is owned by the user, and must be freed explicitly with
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_tree_entry_dup(Git2.TreeEntry** entry_out, Git2.TreeEntry* entry);

    /// <summary>
    /// Get the UNIX file attributes of a tree entry
    /// </summary>
    /// <param name="entry">a tree entry</param>
    /// <returns>filemode as an integer</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitFileMode git_tree_entry_filemode(Git2.TreeEntry* entry);

    /// <summary>
    /// Get the UNIX file attributes of a tree entry
    /// </summary>
    /// <param name="entry">a tree entry</param>
    /// <returns>filemode as an integer</returns>
    /// <remarks>
    /// This function does not perform any normalization and is only useful
    /// if you need to be able to recreate the original tree object.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitFileMode git_tree_entry_filemode_raw(Git2.TreeEntry* entry);

    /// <summary>
    /// Free a user-owned tree entry
    /// </summary>
    /// <param name="entry">The entry to free</param>
    /// <remarks>
    /// IMPORTANT: This function is only needed for tree entries owned by the user,
    /// such as the ones returned by <see cref="git_tree_entry_dup(Git2.TreeEntry**, Git2.TreeEntry*)"/>
    /// or <see cref="git_tree_entry_bypath(Git2.TreeEntry**, Git2.Tree*, string)"/>
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_tree_entry_free(Git2.TreeEntry* entry);

    /// <summary>
    /// Get the id of the object pointed by the entry
    /// </summary>
    /// <param name="entry">a tree entry</param>
    /// <returns>the oid of the object</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    internal static partial GitObjectID* git_tree_entry_id(Git2.TreeEntry* entry);

    /// <summary>
    /// Get the filename of a tree entry
    /// </summary>
    /// <param name="entry">a tree entry</param>
    /// <returns>the name of the file</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    internal static partial byte* git_tree_entry_name(Git2.TreeEntry* entry);

    /// <summary>
    /// Convert a tree entry to the git_object it points to
    /// </summary>
    /// <param name="object_out">pointer to the converted object</param>
    /// <param name="repo">repository where to lookup the pointed object</param>
    /// <param name="entry">a tree entry</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// You must call <see cref="git_object_free"/> on the object when you are done with it.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_tree_entry_to_object(Git2.Object** object_out, Git2.Repository* repo, Git2.TreeEntry* entry);

    /// <summary>
    /// Get the type of the object pointed by the entry
    /// </summary>
    /// <param name="entry">a tree entry</param>
    /// <returns>the type of the pointed object</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    internal static partial GitObjectType git_tree_entry_type(Git2.TreeEntry* entry);

    /// <summary>
    /// Get the number of entries listed in a tree
    /// </summary>
    /// <param name="tree">a previously loaded tree</param>
    /// <returns>the number of entries in the tree</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    internal static partial nuint git_tree_entrycount(Git2.Tree* tree);

    /// <summary>
    /// Close an open tree
    /// </summary>
    /// <param name="tree">The tree to close</param>
    /// <remarks>
    /// You can no longer use the git_tree pointer after this call.
    /// 
    /// IMPORTANT: You MUST call this method when you stop using a tree to release memory. Failure to do so will cause a memory leak.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_tree_free(Git2.Tree* tree);

    /// <summary>
    /// Get the id of a tree
    /// </summary>
    /// <param name="tree">a previously loaded tree</param>
    /// <returns>object identity for the tree</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    internal static partial GitObjectID* git_tree_id(Git2.Tree* tree);

    /// <summary>
    /// Lookup a tree object from the repository
    /// </summary>
    /// <param name="tree_out">pointer to the looked up tree</param>
    /// <param name="repository">the repo to use when locating the tree</param>
    /// <param name="id">identity of the tree to locate</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_tree_lookup(Git2.Tree** tree_out, Git2.Repository* repository, GitObjectID* id);

    /// <summary>
    /// Lookup a tree object from the repository, given a prefix of its identifier (short id).
    /// </summary>
    /// <param name="tree_out">pointer to the looked up tree</param>
    /// <param name="repository">the repo to use when locating the tree</param>
    /// <param name="id">identity of the tree to locate</param>
    /// <param name="len">the length of the short identifier</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_tree_lookup_prefix(Git2.Tree** tree_out, Git2.Repository* repository, GitObjectID* id, nuint len);

    /// <summary>
    /// Get the repository that contains the tree
    /// </summary>
    /// <param name="tree">A previously loaded tree</param>
    /// <returns>Repository that contains this tree</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition)])]
    internal static partial Git2.Repository* git_tree_owner(Git2.Tree* tree);

    /// <summary>
    /// Traverse the entries in a tree and its subtrees in post or pre order.
    /// </summary>
    /// <param name="tree">The tree to walk</param>
    /// <param name="mode">Traversal mode (pre or post-order)</param>
    /// <param name="callback">Function to call on each tree entry</param>
    /// <param name="payload">Opaque pointer to be passed on each callback</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The entries will be traversed in the specified order, children subtrees will be automatically loaded as required,
    /// and the <paramref name="callback"/> will be called once per entry with the current (relative) root for the entry and the entry data itself.
    /// 
    /// If the callback returns a positive value, the passed entry will be skipped on the traversal (in pre mode). A negative value stops the walk.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_tree_walk(Git2.Tree* tree, GitTreeWalkMode mode, delegate* unmanaged[Cdecl]<byte*, Git2.TreeEntry*, nint, int> callback, nint payload);
}
