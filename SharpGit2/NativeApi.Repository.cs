using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace SharpGit2;

internal static unsafe partial class NativeApi
{
    /// <summary>
    /// Gets the parents of the next commit, given the current repository state.
    /// Generally, this is the HEAD commit, except when performing a merge,
    /// in which case it is two or more commits.
    /// </summary>
    /// <param name="commits">a `git_commitarray` that will contain the commit parents</param>
    /// <param name="repository">the repository</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_commit_parents(Git2.CommitArray* commits, nint repository);

    /// <summary>
    /// Get the path of the shared common directory for this repository.
    /// </summary>
    /// <param name="repository">A repository object</param>
    /// <returns>the path to the common dir</returns>
    /// <remarks>
    /// If the repository is bare, it is the root directory for the repository.
    /// If the repository is a worktree, it is the parent repo's gitdir.
    /// Otherwise, it is the gitdir.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial byte* git_repository_commondir(nint repository);

    /// <summary>
    /// Get the configuration file for this repository.
    /// </summary>
    /// <param name="config">Pointer to store the loaded configuration</param>
    /// <param name="repository">A repository object</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// If a configuration file has not been set, the default config set for the repository will be returned,
    /// including global and system configurations (if they are available).
    /// 
    /// The configuration file must be freed once it's no longer being used by the user.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_config(ConfigHandle* config, nint repository);

    /// <summary>
    /// Get a snapshot of the repository's configuration
    /// </summary>
    /// <param name="config">Pointer to store the loaded configuration</param>
    /// <param name="repository">the repository</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// Convenience function to take a snapshot from the repository's configuration.
    /// The contents of this snapshot will not change, even if the underlying config files are modified.
    /// The configuration file must be freed once it's no longer being used by the user.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_config_snapshot(ConfigHandle* config, nint repository);

    /// <summary>
    /// Detach the HEAD.
    /// </summary>
    /// <param name="repository">Repository pointer</param>
    /// <returns>0 on success, <see cref="GitError.UnbornBranch"/> when HEAD points to a non existing branch or an error code</returns>
    /// <remarks>
    /// If the HEAD is already detached and points to a Commit, 0 is returned.
    /// <br/><br/>
    /// If the HEAD is already detached and points to a Tag, the HEAD is updated into making it point to the peeled Commit, and 0 is returned.
    ///<br/><br/>
    /// If the HEAD is already detached and points to a non committish, the HEAD is unaltered, and -1 is returned.
    ///<br/><br/>
    /// Otherwise, the HEAD will be detached and point to the peeled Commit.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_detach_head(nint repository);

    /// <summary>
    /// Look for a git repository and copy its path in the given buffer.
    /// The lookup start from base_path and walk across parent directories
    /// if nothing has been found. The lookup ends when the first repository
    /// is found, or when reaching a directory referenced in <paramref name="ceilingDirs"/>
    /// or when the filesystem changes (in case <paramref name="acrossFS"/> is true).
    /// </summary>
    /// <param name="out">A pointer to a user-allocated <see cref="Git2.Buffer"/> which will contain the found path.</param>
    /// <param name="startPath">The base path where the lookup starts.</param>
    /// <param name="acrossFS">
    /// If true, then the lookup will not stop when a filesystem device change is detected while exploring parent directories.
    /// </param>
    /// <param name="ceilingDirs">
    /// A GIT_PATH_LIST_SEPARATOR separated list of absolute symbolic link free paths.
    /// The lookup will stop when any of this paths is reached. Note that the lookup
    /// always performs on <paramref name="startPath"/> no matter <paramref name="startPath"/>
    /// appears in <paramref name="ceilingDirs"/>. <paramref name="ceilingDirs"/> might be
    /// <see langword="null"/> (which is equivalent to an empty string).
    /// </param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The method will automatically detect if the repository is bare (if there is a repository).
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_discover(Git2.Buffer* @out, string startPath, int acrossFS, string? ceilingDirs);

    /// <summary>
    /// Invoke <paramref name="callback"/> for each entry in the given FETCH_HEAD file.
    /// </summary>
    /// <param name="repository">A repository object</param>
    /// <param name="callback">Callback function</param>
    /// <param name="payload">Pointer to callback data (optional)</param>
    /// <returns>
    /// 0 on success, non-zero callback return value,
    /// <see cref="GitError.NotFound"/> if there is no FETCH_HEAD file,
    /// or other error code.
    /// </returns>
    /// <remarks>
    /// Return a non-zero value from the callback to stop the loop.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_fetchhead_foreach(nint repository, delegate* unmanaged[Cdecl]<byte*, byte*, GitObjectID*, uint, nint, GitError> callback, nint payload);

    /// <summary>
    /// Free a previously allocated repository
    /// </summary>
    /// <param name="repository">repository handle to close. If NULL nothing occurs.</param>
    /// <remarks>
    /// Note that after a repository is free'd, all the objects it has spawned will still exist
    /// until they are manually closed by the user with <see cref="git_object_free"/>, but accessing
    /// any of the attributes of an object without a backing repository will result in undefined behavior
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void git_repository_free(nint repository);

    /// <summary>
    /// Get the currently active namespace for this repository
    /// </summary>
    /// <param name="repository">The repo</param>
    /// <returns>the active namespace, or NULL if there isn't one</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial byte* git_repository_get_namespace(nint repository);

    /// <summary>
    /// Calculate hash of file using repository filtering rules.
    /// </summary>
    /// <param name="out">Output value of calculated SHA</param>
    /// <param name="repository">Repository pointer</param>
    /// <param name="path">
    /// Path to file on disk whose contents should be hashed.
    /// This may be an absolute path or a relative path,
    /// in which case it will be treated as a path within
    /// the working directory.
    /// </param>
    /// <param name="type">
    /// The object type to hash as (e.g. <see cref="GitObjectType.Blob"/>)
    /// </param>
    /// <param name="asPath">
    /// The path to use to look up filtering rules.
    /// If this is an empty string then no filters will be applied
    /// when calculating the hash. If this is `NULL` and the `path`
    /// parameter is a file within the repository's working directory,
    /// then the `path` will be used.
    /// </param>
    /// <returns>0 on success, or an error code</returns>
    /// <remarks>
    /// If you simply want to calculate the hash of a file on disk with no filters,
    /// you can just use the <see cref="git_odb_hashfile"/> API. However, if you want
    /// to hash a file in the repository and you want to apply filtering rules
    /// (e.g. crlf filters) before generating the SHA, then use this function.
    /// <br/><br/>
    /// Note: If the repository has core.safecrlf set to fail and the filtering
    /// triggers that failure, then this function will return an error and not
    /// calculate the hash of the file.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_hashfile(GitObjectID* @out, nint repository, string path, GitObjectType type, string asPath);

    /// <summary>
    /// Retrieve and resolve the reference pointed at by HEAD.
    /// </summary>
    /// <param name="head">pointer to the reference which will be retrieved</param>
    /// <param name="repository">a repository object</param>
    /// <returns>
    /// 0 on success, <see cref="GitError.UnbornBranch"/> when HEAD points to a non existing branch,
    /// <see cref="GitError.NotFound"/> when HEAD is missing, or an error code otherwise
    /// </returns>
    /// <remarks>
    /// The returned <see cref="ReferenceHandle"/> will be owned by caller and
    /// <see cref="ReferenceHandle.Dispose"/> must be called when done with it
    /// to release the allocated memory and prevent a leak.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_head(ReferenceHandle* head, nint repository);

    /// <summary>
    /// Check if a repository's HEAD is detached
    /// </summary>
    /// <param name="repository">Repo to test</param>
    /// <returns>
    /// 1 if HEAD is detached, 0 if it's not; error code if there was an error.
    /// </returns>
    /// <remarks>
    /// A repository's HEAD is detached when it points directly to a commit instead of a branch.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_repository_head_detached(nint repository);

    /// <summary>
    /// Check if a worktree's HEAD is detached
    /// </summary>
    /// <param name="repository">a repository object</param>
    /// <param name="worktreeName">name of the worktree to retrieve HEAD for</param>
    /// <returns>1 if HEAD is detached, 0 if its not; error code if there was an error</returns>
    /// <remarks>
    /// A worktree's HEAD is detached when it points directly to a commit instead of a branch.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_repository_head_detached_for_worktree(nint repository, string worktreeName);

    /// <summary>
    /// Check if the current branch is unborn
    /// </summary>
    /// <param name="repository">Repo to test</param>
    /// <returns>
    /// 1 if the current branch is unborn, 0 if it's not,
    /// or an error code if there was an error
    /// </returns>
    /// <remarks>
    /// An unborn branch is one named from HEAD but which doesn't exist
    /// in the refs namespace, because it doesn't have any commit to point to.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_repository_head_unborn(nint repository);

    /// <summary>
    /// Retrieve the configured identity to use for reflogs
    /// </summary>
    /// <param name="name">where to store the pointer to the name</param>
    /// <param name="email">where to store the pointer to the email</param>
    /// <param name="repository">the repository</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The memory is owned by the repository and must not be freed by the user.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_ident(byte** name, byte** email, nint repository);

    /// <summary>
    /// Get the Index file for this repository.
    /// </summary>
    /// <param name="index">Pointer to store the loaded index</param>
    /// <param name="repository">A repository object</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// If a custom index has not been set, the default index for the repository will be returned (the one located in .git/index).
    /// <br/><br/>
    /// The index must be freed with <see cref="IndexHandle.Dispose"/> once it's no longer being used by the user.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_index(IndexHandle* index, nint repository);

    /// <summary>
    /// Creates a new Git repository in the given folder.
    /// </summary>
    /// <param name="repository">
    /// Pointer to the repo which will be created or reinitialized
    /// </param>
    /// <param name="path">
    /// the path to the repository
    /// </param>
    /// <param name="isBare">
    /// If true, a Git repository without a working directory is created at the pointed path.
    /// If false, provided path will be considered as the working directory into which the .git directory will be created.
    /// </param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_init(RepositoryHandle* repository, string path, uint isBare);

    /// <summary>
    /// Create a new Git repository in the given folder with extended controls.
    /// </summary>
    /// <param name="repository">Pointer to the repo which will be created or reinitialized.</param>
    /// <param name="path">The path to the repository.</param>
    /// <param name="options">Pointer to git_repository_init_options struct.</param>
    /// <returns>0 or an error code on failure.</returns>
    /// <remarks>
    /// This will initialize a new git repository (creating the repo_path if requested by flags)
    /// and working directory as needed. It will auto-detect the case sensitivity of the file system
    /// and if the file system supports file mode bits correctly.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_init_ext(RepositoryHandle* repository, string path, RepositoryInitOptions.Unmanaged* options);

    /// <summary>
    /// Check if a repository is bare
    /// </summary>
    /// <param name="repository">Repo to test</param>
    /// <returns>1 if the repository is bare, 0 otherwise.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_repository_is_bare(nint repository);

    /// <summary>
    /// Check if a repository is empty
    /// </summary>
    /// <param name="repository">Repo to test</param>
    /// <returns>1 if the repository is empty, 0 if it isn't, error code if the repository is corrupted</returns>
    /// <remarks>
    /// An empty repository has just been initialized and contains no references
    /// apart from HEAD, which must be pointing to the unborn master branch,
    /// or the branch specified for the repository in the <see cref="RepositoryInitOptions.InitialHead"/> configuration variable.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_repository_is_empty(nint repository);

    /// <summary>
    /// Determine if the repository was a shallow clone
    /// </summary>
    /// <param name="repository">The repository</param>
    /// <returns>1 if shallow, zero if not</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_repository_is_shallow(nint repository);

    /// <summary>
    /// Check if a repository is a linked work tree
    /// </summary>
    /// <param name="repository">Repo to test</param>
    /// <returns>1 if the repository is a linked work tree, 0 otherwise.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int git_repository_is_worktree(nint repository);

    /// <summary>
    /// Get the location of a specific repository file or directory
    /// </summary>
    /// <param name="out">Buffer to store the path at</param>
    /// <param name="repository">Repository to get path for</param>
    /// <param name="itemType">The repository item for which to retrieve the path</param>
    /// <returns>0, <see cref="GitError.NotFound"/> if the path cannot exist or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_item_path(Git2.Buffer* @out, nint repository, GitRepositoryItemType itemType);

    /// <summary>
    /// If a merge is in progress, invoke 'callback' for each commit ID in the MERGE_HEAD file.
    /// </summary>
    /// <param name="repository">A repository object</param>
    /// <param name="callback">Callback function</param>
    /// <param name="payload">Pointer to callback data (optional)</param>
    /// <returns>0 on success, non-zero callback return value, <see cref="GitError.NotFound"/> if there is no MERGE_HEAD file, or other error code.</returns>
    /// <remarks>
    /// Return a non-zero value from the callback to stop the loop.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_mergehead_foreach(nint repository, delegate* unmanaged[Cdecl]<GitObjectID*, nint, GitError> callback, nint payload);

    /// <summary>
    /// Retrieve git's prepared message
    /// </summary>
    /// <param name="out"><see cref="Git2.Buffer"/> to write data into</param>
    /// <param name="repository">Repository to read prepared message from</param>
    /// <returns>0, <see cref="GitError.NotFound"/> if no message exists, or an error code</returns>
    /// <remarks>
    /// Operations such as git revert/cherry-pick/merge with the -n option stop just short of
    /// creating a commit with the changes and save their prepared message in .git/MERGE_MSG
    /// so the next git-commit execution can present it to the user for them to amend if they wish.
    /// <br/><br/>
    /// Use this function to get the contents of this file.
    /// Don't forget to remove the file after you create the commit.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_message(Git2.Buffer* @out, nint repository);

    /// <summary>
    /// Remove git's prepared message.
    /// </summary>
    /// <param name="repository">Repository to remove prepared message from.</param>
    /// <returns>0 or an error code.</returns>
    /// <remarks>
    /// Remove the message that <seealso cref="git_repository_message(Git2.Buffer*, nint)"/> retrieves.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_message_remove(nint repository);

    /// <summary>
    /// Get the Object Database for this repository.
    /// </summary>
    /// <param name="out">Pointer to store the loaded ODB</param>
    /// <param name="repository">A repository object</param>
    /// <returns>0, or an error code</returns>
    /// <remarks>
    /// If a custom ODB has not been set, the default database for the repository will be returned (the one located in .git/objects).
    /// <br/><br/>
    /// The ODB must be freed once it's no longer being used by the user.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_odb(ObjectDatabaseHandle* @out, nint repository);

#if SHA256_OBJECT_ID
    /// <summary>
    /// Gets the object type used by this repository.
    /// </summary>
    /// <param name="repository">the repository</param>
    /// <returns>the object id type</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitObjectType git_repository_oid_type(nint repository);
#endif

    /// <summary>
    /// Open a git repository.
    /// </summary>
    /// <param name="repository">pointer to the repo which will be opened</param>
    /// <param name="path">the path to the repository</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The <paramref name="path"/> argument must point to either a git repository folder, or an existing work dir.
    /// <br/><br/>
    /// The method will automatically detect if <paramref name="path"/> is a normal or bare repository or fail if 'path' is neither.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_open(RepositoryHandle* repository, string path);

    /// <summary>
    /// Open a bare repository on the serverside.
    /// </summary>
    /// <param name="repository">Pointer to the repo which will be opened.</param>
    /// <param name="path">Direct path to the bare repository</param>
    /// <returns>0 on success, or an error code</returns>
    /// <remarks>
    /// This is a fast open for bare repositories that will come in handy if you're e.g. hosting git repositories and need to access them efficiently
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_open_bare(RepositoryHandle* repository, string path);

    /// <summary>
    /// Find and open a repository with extended controls.
    /// </summary>
    /// <param name="repository">
    /// Pointer to the repo which will be opened.
    /// This can actually be <see langword="null"/> if you only want
    /// to use the error code to see if a repo at
    /// this path could be opened.
    /// </param>
    /// <param name="path">
    /// Path to open as git repository. If the flags permit "searching",
    /// then this can be a path to a subdirectory inside the working directory
    /// of the repository. May be <see langword="null"/> if flags contains <see cref="RepositoryOpenFlags.FromEnvironment"/>.
    /// </param>
    /// <param name="flags"></param>
    /// <param name="ceiling_dirs">
    /// A <see cref="Git2.PathListSeparator"/> delimited list of path
    /// prefixes at which the search for a containing repository should terminate.
    /// </param>
    /// <returns>
    /// 0 on success, <see cref="GitError.NotFound"/> if no repository could be found,
    /// or -1 if there was a repository but open failed for some reason (such as repo corruption or system errors).
    /// </returns>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_open_ext(RepositoryHandle* repository, string? path, RepositoryOpenFlags flags, string? ceiling_dirs);

    /// <summary>
    /// Open working tree as a repository
    /// </summary>
    /// <param name="repository">Output pointer containing opened repository</param>
    /// <param name="worktree">Working tree to open</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// Open the working directory of the working tree as a normal repository that can then be worked on.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_open_from_worktree(RepositoryHandle* repository, Git2.Worktree* worktree);

    /// <summary>
    /// Get the path of this repository
    /// </summary>
    /// <param name="repository">A repository object</param>
    /// <returns>the path to the repository</returns>
    /// <remarks>
    /// This is the path of the ".git" folder for normal repositories,
    /// or of the repository itself for bare repositories.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial byte* git_repository_path(nint repository);

    /// <summary>
    /// Get the Reference Database Backend for this repository.
    /// </summary>
    /// <param name="refDB">Pointer to store the loaded refdb</param>
    /// <param name="repository">A repository object</param>
    /// <returns>0 or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_refdb(RefDBHandle* refDB, nint repository);

    /// <summary>
    /// Make the repository HEAD point to the specified reference.
    /// </summary>
    /// <param name="repository">Repository pointer</param>
    /// <param name="refname">Canonical name of the reference the HEAD should point at</param>
    /// <returns>0 on success, or an error code</returns>
    /// <remarks>
    /// If the provided reference points to a Tree or a Blob, the HEAD is unaltered and -1 is returned.
    /// <br/><br/>
    /// If the provided reference points to a branch, the HEAD will point to that branch,
    /// staying attached, or become attached if it isn't yet. If the branch doesn't exist yet,
    /// no error will be return. The HEAD will then be attached to an unborn branch.
    /// <br/><br/>
    /// Otherwise, the HEAD will be detached and will directly point to the Commit.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_set_head(nint repository, byte* refname);

    ///<inheritdoc cref="git_repository_set_head(nint, byte*)"/>
    internal static GitError git_repository_set_head(nint repository, string refname)
    {
        scoped Utf8StringMarshaller.ManagedToUnmanagedIn refNameIn = new();
        try
        {
            refNameIn.FromManaged(refname, stackalloc byte[Utf8StringMarshaller.ManagedToUnmanagedIn.BufferSize]);

            return git_repository_set_head(repository, refNameIn.ToUnmanaged());
        }
        finally
        {
            refNameIn.Free();
        }
    }

    /// <summary>
    /// Make the repository HEAD directly point to the Commit.
    /// </summary>
    /// <param name="repository">Repository pointer</param>
    /// <param name="committish">Object id of the Commit the HEAD should point to</param>
    /// <returns>0 on success, or an error code</returns>
    /// <remarks>
    /// If the provided committish cannot be found in the repository, the HEAD is unaltered and <see cref="GitError.NotFound"/> is returned.
    /// <br/><br/>
    /// If the provided committish cannot be peeled into a commit, the HEAD is unaltered and -1 (<see cref="GitError.Error"/>) is returned.
    /// <br/><br/>
    /// Otherwise, the HEAD will eventually be detached and will directly point to the peeled Commit.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_set_head_detached(nint repository, GitObjectID* committish);

    /// <summary>
    /// Make the repository HEAD directly point to the Commit.
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="committish"></param>
    /// <returns></returns>
    /// <remarks>
    /// This behaves like <see cref="git_repository_set_head_detached(nint, GitObjectID*)"/>
    /// but takes an annotated commit, which lets you specify which extended sha syntax string
    /// was specified by a user, allowing for more exact reflog messages.
    /// </remarks>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_set_head_detached_from_annotated(nint repository, nint committish); // TODO: Needs to be exposed

    /// <summary>
    /// Set the identity to be used for writing reflogs
    /// </summary>
    /// <param name="repository">the repository to configure</param>
    /// <param name="name">the name to use for the reflog entries</param>
    /// <param name="email">the email to use for the reflog entries</param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// If both are set, this name and email will be used to write to the reflog.
    /// Pass NULL to unset. When unset, the identity will be taken from the repository's configuration.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_set_ident(nint repository, string? name, string? email);

    /// <summary>
    /// Sets the active namespace for this Git Repository
    /// </summary>
    /// <param name="repository">The repo</param>
    /// <param name="namespace">
    /// The namespace. This should not include the refs folder, e.g. to namespace all references under `refs/namespaces/foo/`, use `foo` as the namespace.
    /// </param>
    /// <returns>0 on success, -1 on error</returns>
    /// <remarks>
    /// This namespace affects all reference operations for the repo.
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_set_namespace(nint repository, string @namespace);

    /// <summary>
    /// Set the path to the working directory for this repository
    /// </summary>
    /// <param name="repository">A repository object</param>
    /// <param name="workdir">The path to a working directory</param>
    /// <param name="updateGitlink">
    /// Create/update gitlink in workdir and set config "core.worktree" (if workdir is not the parent of the .git directory)
    /// </param>
    /// <returns>0 or an error code</returns>
    /// <remarks>
    /// The working directory doesn't need to be the same one that
    /// contains the ".git" folder for this repository.
    /// <br/><br/>
    /// If this repository is bare, setting its working directory
    /// will turn it into a normal repository, capable of performing
    /// all the common workdir operations (checkout, status, index manipulation, etc).
    /// </remarks>
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_set_workdir(nint repository, string workdir, int updateGitlink);

    /// <summary>
    /// Determines the status of a git repository - ie, whether an operation (merge, cherry-pick, etc) is in progress.
    /// </summary>
    /// <param name="repository">Repository pointer</param>
    /// <returns>The state of the repository</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitRepositoryState git_repository_state(nint repository);

    /// <summary>
    /// Remove all the metadata associated with an ongoing command like merge, revert, cherry-pick, etc. For example: MERGE_HEAD, MERGE_MSG, etc.
    /// </summary>
    /// <param name="repository">A repository object</param>
    /// <returns>0 on success, or an error code</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial GitError git_repository_state_cleanup(nint repository);

    /// <summary>
    /// Get the path of the working directory for this repository
    /// </summary>
    /// <param name="repository">A repository object</param>
    /// <returns>The path to the working directory if it exists, or <see langword="null"/> if it does not.</returns>
    [LibraryImport(Git2.LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial byte* git_repository_workdir(nint repository);

}
