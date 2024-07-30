using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2;

[Flags]
public enum RepositoryInitFlags : uint
{
    /// <summary>
    /// Create a bare repository with no working directory.
    /// </summary>
    Bare = 1,
    /// <summary>
    /// Return <see cref="GitError.Exists"/> if the repo_path appears to already be a git repository.
    /// </summary>
    NoReinit = 1 << 1,
    /// <summary>
    /// Normally a "/.git/" will be appended to the repo path for
    /// non-bare repos (if it is not already there), but passing this flag
    /// prevents that behavior.
    /// </summary>
    NoDotGitFolder = 1 << 2,
    /// <summary>
    /// Make the repo_path (and workdir_path) as needed. Init is always willing
    /// to create the ".git" directory even without this flag. This flag tells
    /// init to create the trailing component of the repo and workdir paths
    /// as needed.
    /// </summary>
    MakeDirectory = 1 << 3,
    /// <summary>
    /// Recursively make all components of the repo
    /// and workdir paths as necessary.
    /// </summary>
    MakePath = 1 << 4,
    /// <summary>
    /// libgit2 normally uses internal templates to initialize a new repo.
    /// This flags enables external templates, looking the "template_path" from
    /// the options if set, or the `init.templatedir` global config if not,
    /// or falling back on "/usr/share/git-core/templates" if it exists.
    /// </summary>
    ExternalTemplate = 1 << 5,
    /// <summary>
    /// If an alternate workdir is specified, use relative paths for the gitdir
    /// and core.worktree.
    /// </summary>
    RelativeGitlink = 1 << 6
}

public enum RepositoryInitMode : uint
{
    /// <summary>
    /// Use permissions configured by umask - the default.
    /// </summary>
    SharedUmask = 0,
    /// <summary>
    /// Use "--shared=group" behavior, chmod'ing the new repo to be group
    /// writable and "g+sx" for sticky group assignment.
    /// </summary>
    SharedGroup = 2775,
    /// <summary>
    /// Use "--shared=all" behavior, adding world readability.
    /// </summary>
    SharedAll = 2777
}

[Flags]
public enum RepositoryOpenFlags : uint
{
    /// <summary>
    /// Only open the repository if it can be immediately found in the
    /// start_path. Do not walk up from the start_path looking at parent
    /// directories.
    /// </summary>
    NoSearch = 1,

    /// <summary>
    /// Unless this flag is set, open will not continue searching across
    /// filesystem boundaries (i.e. when `st_dev` changes from the `stat`
    /// system call).  For example, searching in a user's home directory at
    /// "/home/user/source/" will not return "/.git/" as the found repo if
    /// "/" is a different filesystem than "/home".
    /// </summary>
    CrossFileSystem = 1 << 1,

    /// <summary>
    /// Open repository as a bare repo regardless of core.bare config, and
    /// defer loading config file for faster setup.
    /// Unlike <seealso cref="RepositoryHandle.Open(string, RepositoryOpenFlags, string?)"/>, this can follow gitlinks.
    /// </summary>
    Bare = 1 << 2,

    /// <summary>
    /// Do not check for a repository by appending /.git to the start_path;
    /// only open the repository if start_path itself points to the git
    /// directory.
    /// </summary>
    NoDotGit = 1 << 3,

    /// <summary>
    /// Find and open a git repository, respecting the environment variables
    /// used by the git command-line tools.
    /// 
    /// If set, `git_repository_open_ext` will ignore the other flags and
    /// the `ceiling_dirs` argument, and will allow a NULL `path` to use
    /// `GIT_DIR` or search from the current directory.
    /// 
    /// The search for a repository will respect $GIT_CEILING_DIRECTORIES and
    /// $GIT_DISCOVERY_ACROSS_FILESYSTEM.  The opened repository will
    /// respect $GIT_INDEX_FILE, $GIT_NAMESPACE, $GIT_OBJECT_DIRECTORY, and
    /// $GIT_ALTERNATE_OBJECT_DIRECTORIES.
    /// 
    /// In the future, this flag will also cause <see cref="RepositoryHandle.Open(string?, RepositoryOpenFlags, string?)"/>
    /// to respect $GIT_WORK_TREE and $GIT_COMMON_DIR; currently,
    /// <see cref="RepositoryHandle.Open(string?, RepositoryOpenFlags, string?)"/>
    /// with this flag will error out if either
    /// $GIT_WORK_TREE or $GIT_COMMON_DIR is set.
    /// </summary>
    FromEnvironment = 1 << 4
}

public enum GitRepositoryState
{
    None,
    Merge,
    Revert,
    RevertSequence,
    CherryPick,
    CherryPickSequence,
    Bisect,
    Rebase,
    RebaseInteractive,
    RebaseMerge,
    ApplyMailbox,
    ApplyMailboxOrRebase
}

public enum GitRepositoryItemType
{
    GitDir,
    WorkDir,
    CommonDir,
    Index,
    Objects,
    Refs,
    PackedRefs,
    Remotes,
    Config,
    Info,
    Hooks,
    Logs,
    Modules,
    WorkTrees,
    WorkTreeConfig,
    _Last
}