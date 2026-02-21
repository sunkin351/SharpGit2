using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using SharpGit2.Managed.Attributes;
using SharpGit2.Managed.Config;
using SharpGit2.Managed.Internal;
using SharpGit2.Managed.ObjectDB;
using SharpGit2.Managed.ReferenceDB;
using SharpGit2.Managed.Submodule;
using SharpGit2.Managed.Worktree;

namespace SharpGit2.Managed;

[Flags]
public enum GitRepositoryOpenFlags : uint
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
    /// Unlike <seealso cref="GitRepository.Open(string, GitRepositoryOpenFlags, string?)"/>, this can follow gitlinks.
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
    /// In the future, this flag will also cause <see cref="GitRepository.Open(string?, GitRepositoryOpenFlags, string?)"/>
    /// to respect $GIT_WORK_TREE and $GIT_COMMON_DIR; currently,
    /// <see cref="GitRepository.Open(string?, GitRepositoryOpenFlags, string?)"/>
    /// with this flag will error out if either
    /// $GIT_WORK_TREE or $GIT_COMMON_DIR is set.
    /// </summary>
    FromEnvironment = 1 << 4
}


[Flags]
public enum GitRepositoryInitFlags : uint
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

public struct GitRepositoryInitOptions
{
    public GitRepositoryInitFlags Flags { get; set; }

    public UnixFileMode Mode { get; set; }

    public string? WorkingDirectoryPath { get; set; }

    public string? Description { get; set; }

    public string? TemplatePath { get; set; }

    public string? InitialHead { get; set; }

    public string? OriginUrl { get; set; }

    public GitObjectIDType ObjectIDType { get; set; }
}

public sealed partial class GitRepository : IDisposable
{
    public static GitRepository Open(string repoPath)
    {
        return Open(repoPath, GitRepositoryOpenFlags.NoSearch, null);
    }

    public static GitRepository Open(string repoPath, GitRepositoryOpenFlags flags, string? ceilingDirectories = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(repoPath);

        RepoPaths paths = default;

        FindRepository(ref paths, repoPath, ceilingDirectories, flags);

        var repo = new GitRepository()
        {
            UseEnv = (flags & GitRepositoryOpenFlags.FromEnvironment) != 0,
            GitDirectory = paths.GitDirectory!
        };

        if (!string.IsNullOrEmpty(paths.GitLink))
        {
            repo.GitLink = paths.GitLink;
        }

        if (!string.IsNullOrEmpty(paths.CommonDirectory))
        {
            repo.CommonDirectory = paths.CommonDirectory;
        }

        repo.IsWorktree = repo.RepositoryIsWorktree();

        var config = repo.ObtainConfigAndSetOIDType();

        repo.LoadGrafts();

        if ((flags & GitRepositoryOpenFlags.Bare) != 0)
            repo.IsBare = true;
        else if (config != null)
        {
            repo.LoadConfigData(config);
            repo.LoadWorkDirectory(config, repoPath);
        }

        repo.LoadNamespace();

        if (ValidateOwnership)
        {
            repo.ValidateOwnershipOperation();
        }

        return repo;
    }

    public static GitRepository OpenBare(string barePath)
    {
        var path = PrettifyPath_Directory(barePath, null);

        if (!IsValidRepositoryPath(path, default, out string commonPath))
        {
            throw new Git2Exception("Path is not a repository!");
        }

        var repo = new GitRepository()
        {
            GitDirectory = path,
            CommonDirectory = commonPath,
            IsBare = true
        };

        _ = repo.ObtainConfigAndSetOIDType();

        return repo;
    }

    public static GitRepository OpenFromWorktree(GitWorktree worktree)
    {
        string path = worktree.Gitlink_Path;

        if (!path.EndsWith(Constants.DotGit, StringComparison.OrdinalIgnoreCase))
        {
            throw new Git2Exception("Internal error!");
        }

        return Open(path[..^4]);
    }

    public static string? Discover(string startPath, bool acrossFS = true, string? ceilingDirectories = null)
    {
        var flags = acrossFS ? GitRepositoryOpenFlags.CrossFileSystem : default;
        RepoPaths paths = default;

        FindRepository(ref paths, startPath, ceilingDirectories, flags);

        return paths.GitDirectory;
    }

    public static GitRepository InitRepository(string path, bool isBare)
    {
        GitRepositoryInitOptions options = new()
        {
            Flags = GitRepositoryInitFlags.MakePath // Original project does not love this default
                | (isBare ? GitRepositoryInitFlags.Bare : 0)
        };

        return InitRepository_Internal(path, ref options);
    }

    public static GitRepository InitRepository(string path, in GitRepositoryInitOptions options)
    {
        var copy = options; // Internal methods are expected to modify this, so make a copy

        return InitRepository_Internal(path, ref copy);
    }

    private static GitRepository InitRepository_Internal(string path, ref GitRepositoryInitOptions options, bool useEnvironment = false)
    {
        var oid_type = GitObjectIDType.Default;

#if GIT_EXPERIMENTAL_SHA256
        if (options.ObjectIDType != 0)
            oid_type = options.ObjectIDType;
#endif

        InitDirectories(path, ref options, out string repoPath, out string? workdirpath);

        if ((options.Flags & GitRepositoryInitFlags.Bare) != 0)
            workdirpath = null;

        if (IsValidRepositoryPath(workdirpath, useEnvironment ? GitRepositoryOpenFlags.FromEnvironment : default, out string commonPath))
        {
            if ((options.Flags & GitRepositoryInitFlags.NoReinit) != 0)
            {
                throw new Git2Exception($"Attempted to reinitialize '{path}'");
            }

            options.Flags |= InitFlags_IsReinit;

            InitConfig(repoPath, workdirpath, options.Flags, useEnvironment, options.Mode, oid_type);
        }
        else
        {
            InitStructure(repoPath, workdirpath, ref options);
            InitConfig(repoPath, workdirpath, options.Flags, useEnvironment, options.Mode, oid_type);
            InitHead(repoPath, options.InitialHead);
        }

        var repo = Open(repoPath);

        if (options.OriginUrl != null)
        {
            InitCreateOrigin(repo, options.OriginUrl);
        }

        return repo;
    }

    public bool IsBare { get; private set; }
    public bool IsWorktree { get; private set; }

    private string GitLink { get; set; }

    public string? WorkingDirectory { get; private set; }

    private volatile bool _disposed = false;
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
            return;
        
        Interlocked.Exchange(ref _configField, null)?.Dispose();
        Interlocked.Exchange(ref _refdbField, null)?.Dispose();
        Interlocked.Exchange(ref _indexField, null)?.Dispose();
        Interlocked.Exchange(ref _odbField, null)?.Dispose();
    }

    public void SetWorkingDirectory(string workingDirectory, bool updateGitLink)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        
        var path = PrettifyPath_Directory(workingDirectory, null);

        if (this.WorkingDirectory == path)
            return;

        if (updateGitLink)
        {
            var config = this.Config;
            
            bool necessary = WriteGitlink(path, this.GitDirectory, false);

            if (necessary)
            {
                config.Set("core.worktree", path);
            }
            else
            {
                config.DeleteEntry("core.worktree");
            }
            
            config.Set("core.bare", false);
        }
        
        this.WorkingDirectory = path;
        this.IsBare = false;
    }
    
    private string GitDirectory;
    internal string? CommonDirectory;

    public string? Namespace { get; private set; }
    public string RepositoryPath => this.GitLink;
    
    internal string? IdentityName { get; private set; }
    internal string? IdentityEmail { get; private set; }

    internal GitObjectIDType ObjectIdType { get; private set; } = GitObjectIDType.Default;

    public void GetAttribute(GitAttributeCheckFlags flags, string pathname, string name, out Attributes.GitAttributeValue value_out)
    {
        GitAttributeOptions options = new() { Flags = flags };

        GetAttribute(in options, pathname, name, out value_out);
    }

    public void GetAttribute(in GitAttributeOptions options, string pathname, string name, out Attributes.GitAttributeValue value_out)
    {
        ArgumentNullException.ThrowIfNull(pathname);
        ArgumentNullException.ThrowIfNull(name);

        var path = new GitAttributePath(pathname, this.WorkingDirectory, this.IsBare ? false : null);

        var files = CollectAttributeFiles(null, in options, pathname);

        foreach (var file in files)
        {
            if (file.TryLookupOne(in path, name, out value_out))
            {
                return;
            }
        }

        value_out = default;
    }

    public void GetAttributes(GitAttributeCheckFlags flags, string pathname, ReadOnlySpan<string> names, Span<Attributes.GitAttributeValue> values_out)
    {
        GitAttributeOptions options = new() { Flags = flags };

        GetAttributes(in options, pathname, names, values_out);
    }

    public void GetAttributes(in GitAttributeOptions options, string pathname, ReadOnlySpan<string> names, Span<Attributes.GitAttributeValue> values_out)
    {
        GetAttributesWithSession(null, in options, pathname, names, values_out);
    }

    internal void GetAttributesWithSession(GitAttributeSession? session, in GitAttributeOptions options, string pathname, ReadOnlySpan<string> names, Span<Attributes.GitAttributeValue> values_out)
    {
        ArgumentNullException.ThrowIfNull(pathname);
        ArgumentOutOfRangeException.ThrowIfNotEqual(values_out.Length, names.Length);

        if (names.IsEmpty)
            return;

        values_out.Clear();

        var path = new GitAttributePath(pathname, this.WorkingDirectory, dirFlag: this.IsBare ? false : null);
        var files = CollectAttributeFiles(session, in options, pathname);

        Span<bool> foundAttributes = names.Length <= 32 ? stackalloc bool[names.Length] : new bool[names.Length];

        foreach (var file in files)
        {
            file.LookupMany(in path, names, values_out, foundAttributes);

            if (foundAttributes.Count(false) == 0)
                break;
        }
    }

    /// <summary>
    /// Retrieve and resolve the reference pointed at by HEAD.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> when HEAD points to a non-existent branch, or a valid <see cref="GitReference"/> object.
    /// </returns>
    public GitReference? GetHead()
    {
        var head = this.References.Lookup(Constants.GitHeadFile);
        
        Debug.Assert(head != null, "HEAD reference was null somehow!");

        if (head.ReferenceType == GitReferenceType.Direct)
            return head;

        return this.LookupReferenceResolved(head.SymbolicTarget!, -1);
    }

    public bool IsHeadUnborn => this.GetHead() == null; // TODO: Optimize this to be cheaper

    public GitReference? GetHeadForWorktree(string worktreeName)
    {
        throw new NotImplementedException();
    }

    public GitObject? LookupObject(in GitObjectID oid, GitObjectType type = GitObjectType.Any)
    {
        throw new NotImplementedException();
    }

    public GitBlob? LookupBlob(in GitObjectID oid)
    {
        return (GitBlob?)LookupObject(in oid, GitObjectType.Blob);
    }

    public GitCommit? LookupCommit(in GitObjectID oid)
    {
        return (GitCommit?)LookupObject(in oid, GitObjectType.Commit);
    }

    public GitReference? LookupReference(string referenceName)
    {
        throw new NotImplementedException();
    }

    public GitReference? LookupReferenceResolved(string referenceName, int maxNesting)
    {
        throw new NotImplementedException();
    }

    public GitIndex? GetIndex()
    {
        throw new NotImplementedException();
    }

    public string GetItemPath(GitRepositoryItemType item)
    {
        if (TryGetItemPath(item, out string? path))
            return path;

        throw new Git2Exception($"Could not find path for item {item}!");
    }

    public bool TryGetItemPath(GitRepositoryItemType item, [NotNullWhen(true)] out string? path)
    {
        var (parent, fallback, name, isDir) = Items[(int)item];

        string? parentPath = ResolvedParentPath(this, parent, fallback);

        if (parentPath == null)
        {
            path = null;
            return false;
        }
        
        Debug.Assert(parentPath.Length > 0);

        if (name != null || isDir)
        {
            // $"{parentPath}/{name}{(name != null && isDir ? "/" : "")}" but more efficient
            var handler = new DefaultInterpolatedStringHandler(0, 4, null, stackalloc char[256]);
            
            handler.AppendFormatted(parentPath);
            
            if (!GitPath.IsDirectorySeparator(parentPath[^1]))
                handler.AppendFormatted("/");

            if (name != null)
            {
                handler.AppendFormatted(name);
                
                if (isDir)
                    handler.AppendFormatted("/");
            }

            parentPath = handler.ToStringAndClear();
        }

        path = parentPath;
        return true;
        
        static string? ResolvedParentPath(GitRepository repo, GitRepositoryItemType item, GitRepositoryItemType fallback)
        {
            string? parent = item switch
            {
                GitRepositoryItemType.GitDir => repo.RepositoryPath,
                GitRepositoryItemType.WorkDir => repo.WorkingDirectory,
                GitRepositoryItemType.CommonDir => repo.CommonDirectory,
                _ => throw new ArgumentException("Invalid item directory!")
            };

            if (parent == null && fallback != GitRepositoryItemType._Last)
            {
                parent = fallback switch
                {
                    GitRepositoryItemType.GitDir => repo.RepositoryPath,
                    GitRepositoryItemType.WorkDir => repo.WorkingDirectory,
                    GitRepositoryItemType.CommonDir => repo.CommonDirectory,
                    _ => throw new ArgumentException("Invalid item directory!")
                };
            }

            return parent;
        }
    }

    public GitConfig? GetConfigSnapshot()
    {
        return this.Config.CreateSnapshot();
    }

    public void AddAttributeMacro(string name, string values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrEmpty(values);

        var cache = this.AttributeCache;

        var rule = new GitAttributeRule();
        rule.Match.Pattern = name;
        rule.Match.Flags = GitAttributeFNMatchFlags.Macro;

        if (GitAttributeRule.TryParseAssignments(values, this, ref rule))
        {
            cache.InsertMacro(in rule);
        }
    }

    public bool IsEmpty()
    {
        var head = this.References.Lookup(Constants.GitHeadFile);

        Debug.Assert(head != null, "HEAD reference was null somehow!");
        
        var initialBranch = this.GetInitialBranch();

        return head.ReferenceType == GitReferenceType.Symbolic
               && head.SymbolicTarget == initialBranch
               && !this.References.Any();
    }

    public string GetInitialBranch()
    {
        string branch;
        if (this.Config.TryGetEntry("init.defaultbranch", out var entry) && !string.IsNullOrEmpty(entry.Value))
        {
            branch = Constants.RefsHeadsDir + entry.Value;
            
            if (!GitReference.IsReferenceNameValid(branch))
            {
                throw new Git2ConfigException("The value of init.defaultbranch is not a valid branch name!");
            }
        }
        else
        {
            branch = Constants.RefsHeadsDir + Constants.GitDefaultBranch;
            Debug.Assert(GitReference.IsReferenceNameValid(branch), "Default branch name is not a valid branch name!");
        }

        return branch;
    }

    public ObjectCollection Objects => new(this);

    public readonly struct ObjectCollection(GitRepository parentRepository)
    {
        private readonly GitRepository  _parentRepository = parentRepository;
        private readonly GitObjectDatabase _database = parentRepository.ObjectDatabase;

        public GitObject Lookup(in GitObjectID id, GitObjectType type = GitObjectType.Any)
        {
            throw new NotImplementedException();
        }

        public GitBlob LookupBlob(in GitObjectID id) => (GitBlob)this.Lookup(in id, GitObjectType.Blob);
        
        public GitCommit LookupCommit(in GitObjectID id) => (GitCommit)this.Lookup(in id, GitObjectType.Commit);

        public GitTag LookupTag(in GitObjectID id) => (GitTag)this.Lookup(in id, GitObjectType.Tag);

        public GitTree LookupTree(in GitObjectID id) => (GitTree)this.Lookup(in id, GitObjectType.Tree);
    }

    public ReferenceCollection References => new(this);

    // This isn't decorated with anything like `IReadOnlyCollection<T>` because this is backed by the filesystem
    // instead of managed memory.
    [SuppressMessage("ReSharper", "ReplaceWithPrimaryConstructorParameter")]
    public readonly struct ReferenceCollection(GitRepository parentRepository) : IEnumerable<GitReference>
    {
        private readonly GitRepository _parentRepository = parentRepository;
        private readonly GitReferenceDatabase _database = parentRepository.ReferenceDatabase;

        /// <summary>
        /// Does not correspond with any libgit2 function, but is an optimization point for `GitRepository.IsEmpty()`.
        /// </summary>
        /// <returns><see langword="true"/> if any references exist in this repository, otherwise <see langword="false"/></returns>
        public bool Any()
        {
            return _database.EnumerateReferenceNames(null).Any();
        }

        public GitReference Create(
            string referenceName,
            in GitObjectID id,
            bool force,
            string? logMessage = null)
        {
            var who = GitReference.LogSignature(_parentRepository);

            return CreateReference(referenceName, in id, force, who, logMessage);
        }
        
        public GitReference Create(
            string referenceName,
            string symbolicTarget,
            bool force,
            string? logMessage = null)
        {
            var who = GitReference.LogSignature(_parentRepository);

            return CreateReference(referenceName, symbolicTarget, force, who, logMessage);
        }

        public GitReference CreateMatching(
            string referenceName,
            in GitObjectID id,
            bool force,
            in GitObjectID oldId,
            string? logMessage = null)
        {
            var who = GitReference.LogSignature(_parentRepository);

            return CreateReference(referenceName, in id, force, who, logMessage, in oldId);
        }

        public GitReference CreateMatching(
            string referenceName,
            string symbolicTarget,
            bool force,
            string oldTarget,
            string? logMessage = null)
        {
            var who = GitReference.LogSignature(_parentRepository);

            return CreateReference(referenceName, symbolicTarget, force, who, logMessage, oldTarget);
        }

        /// <summary>
        /// Synonymous with git_reference_lookup()
        /// </summary>
        /// <param name="referenceName"></param>
        /// <returns></returns>
        public GitReference? Lookup(string referenceName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Synonymous with git_reference_lookup_resolved()
        /// </summary>
        /// <param name="referenceName"></param>
        /// <param name="maxDereference"></param>
        /// <returns></returns>
        internal GitReference? LookupResolved(string referenceName, int maxDereference = -1)
        {
            referenceName = GitReference.ReferenceNormalizeForRepo(_parentRepository, referenceName, true);
            
            GitReference? reference = _database.Resolve(referenceName, maxDereference);
            
            return maxDereference != 0 && reference is { ReferenceType: GitReferenceType.Symbolic } ? null : reference;
        }

        private static readonly (string?, string?)[] _dwimformatters = [
            (null, null),
            (Constants.RefsDir, null),
            (Constants.RefsTagsDir, null),
            (Constants.RefsHeadsDir, null),
            (Constants.RefsRemotesDir, null),
            (Constants.RefsRemotesDir, "/" + Constants.GitHeadFile)
        ];
        
        public GitReference? LookupByDWIM(string shorthand)
        {
            string name = string.IsNullOrEmpty(shorthand) ? Constants.GitHeadFile : shorthand;

            Span<char> buffer = stackalloc char[128];

            bool foundValid = false;
            foreach (var (before, after) in _dwimformatters)
            {
                var interpolate = new DefaultInterpolatedStringHandler(0, 3, null, buffer);
                interpolate.AppendFormatted(before);
                interpolate.AppendFormatted(name);
                interpolate.AppendFormatted(after);

                if (!GitReference.IsReferenceNameValid(interpolate.Text))
                {
                    buffer.Clear();
                    continue;
                }
                
                foundValid = true;
                var reference = this.LookupResolved(interpolate.ToStringAndClear());

                if (reference != null)
                    return reference;
            }

            if (!foundValid)
                throw new ArgumentException($"Could not use '{name}' as a valid reference name!");

            return null;
        }

        public bool Exists(string referenceName)
        {
            throw new NotImplementedException();
        }

        internal void UpdateTerminal(string referenceName, in GitObjectID oid, GitSignature? signature, string logMessage)
        {
            var who = signature ?? GitReference.LogSignature(_parentRepository);

            var database = _database;

            var reference = database.Resolve(referenceName, -1);

            if (reference == null) // not found
            {
                this.CreateReference(referenceName, in oid, false, who, logMessage);
                return;
            }

            if (reference.ReferenceType == GitReferenceType.Symbolic)
            {
                this.CreateReference(reference.SymbolicTarget!, in oid, false, who, logMessage);
            }
            else
            {
                this.CreateReference(reference.ReferenceName, in oid, false, who, logMessage, in reference.DirectTarget);
            }
        }

        internal void UpdateForCommit(GitReference? reference, string referenceName, in GitObjectID id, string? operation)
        {
            var commit = _parentRepository.LookupCommit(in id);
            var reflogMessage =
                $"{operation ?? "commit"}{commit.Parents.Length switch { 0 => " (initial)", 1 => "", _ => " (merge)" }}: {commit.Summary}";

            var who = commit.Committer;

            if (reference != null)
            {
                if (reference.ReferenceType != GitReferenceType.Direct)
                    throw new InvalidOperationException("Cannot set OID on symbolic reference!");
                
                this.CreateReference(reference.ReferenceName, in id, true, who, reflogMessage, in reference.DirectTarget);
            }
            else
            {
                this.UpdateTerminal(referenceName, in id, who, reflogMessage);
            }
        }

        public void Remove(string referenceName)
        {
            _database.Delete(referenceName);
        }

        public bool TryNameToId(string referenceName, out GitObjectID id)
        {
            var reference = this.LookupResolved(referenceName);

            if (reference != null)
            {
                id = reference.DirectTarget;
                return true;
            }
            
            id = default;
            return false;
        }

        public GitObjectID NameToId(string referenceName)
        {
            var reference = this.LookupResolved(referenceName);
            
            return reference?.DirectTarget ?? throw new Git2Exception("Reference name not found!");
        }

        public IEnumerator<GitReference> GetEnumerator()
        {
            return _database.EnumerateReferences(null).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<string> EnumerateReferenceNames()
        {
            return _database.EnumerateReferenceNames(null);
        }

        public bool HasLog(string referenceName)
        {
            return _database.HasLog(referenceName);
        }

        public void EnsureLog(string referenceName)
        {
            _database.EnsureLog(referenceName);
        }

        private GitReference CreateReference(
            string referenceName,
            in GitObjectID oid,
            bool force,
            GitSignature signature,
            string? logMessage)
        {
            string normalized = GitReference.ReferenceNormalizeForRepo(_parentRepository, referenceName, true);
            
            this.ValidateObjectID(in oid);
            var reference = new GitReference(normalized, oid, null);

            _database.Write(reference, force, signature, logMessage);

            return reference;
        }
        
        private GitReference CreateReference(
            string referenceName,
            in GitObjectID oid,
            bool force,
            GitSignature signature,
            string? logMessage,
            in GitObjectID oldId)
        {
            string normalized = GitReference.ReferenceNormalizeForRepo(_parentRepository, referenceName, true);

            this.ValidateObjectID(in oid);
            var reference = new GitReference(normalized, oid, null);

            _database.Write(reference, force, signature, logMessage, oldId);

            return reference;
        }
        
        private GitReference CreateReference(
            string referenceName,
            string symbolicTarget,
            bool force,
            GitSignature signature,
            string? logMessage,
            string? oldTarget = null)
        {
            string normalized = GitReference.ReferenceNormalizeForRepo(_parentRepository, referenceName, true);
            string normalizedTarget = GitReference.ReferenceNormalizeForRepo(_parentRepository, symbolicTarget, true);
            
            var reference = new GitReference(normalized, normalizedTarget);

            if (oldTarget == null)
                _database.Write(reference, force, signature, logMessage);
            else
                _database.Write(reference, force, signature, logMessage, oldTarget);

            return reference;
        }
        
        private void ValidateObjectID(in GitObjectID oid)
        {
            if (!_parentRepository.IsValidObjectId(in oid, GitObjectType.Any))
            {
                throw new ArgumentException("Target OID for the reference doesn't exist in this repository!");
            }
        }
    }
    
    public SubmoduleCollection Submodules => new(this);

    public readonly struct SubmoduleCollection(GitRepository parentRepository) : IEnumerable<GitSubmodule>
    {
        private readonly GitRepository _parentRepository = parentRepository;

        public IEnumerator<GitSubmodule> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}