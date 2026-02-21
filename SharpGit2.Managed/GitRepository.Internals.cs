using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using CommunityToolkit.HighPerformance.Buffers;

using Mono.Unix.Native;

using SharpGit2.Managed.Config;
using SharpGit2.Managed.Internal;
using SharpGit2.Managed.ObjectDB;
using SharpGit2.Managed.ReferenceDB;
using SharpGit2.Managed.Submodule;
using SharpGit2.Managed.Worktree;

using TerraFX.Interop.Windows;

namespace SharpGit2.Managed;

public sealed partial class GitRepository
{
    // internal flags
    internal const GitRepositoryInitFlags InitFlags_HasDotGit = (GitRepositoryInitFlags)(1 << 16);
    internal const GitRepositoryInitFlags InitFlags_NaturalWD = (GitRepositoryInitFlags)(1 << 17);
    internal const GitRepositoryInitFlags InitFlags_IsReinit = (GitRepositoryInitFlags)(1 << 18);

    internal const UnixFileMode InitSharedGroup = UnixFileMode.OtherRead
        | UnixFileMode.OtherExecute
        | UnixFileMode.GroupExecute
        | UnixFileMode.GroupRead
        | UnixFileMode.GroupWrite
        | UnixFileMode.UserExecute
        | UnixFileMode.UserRead
        | UnixFileMode.UserWrite
        | UnixFileMode.SetGroup;

    internal const UnixFileMode InitSharedAll = InitSharedGroup | UnixFileMode.OtherWrite;

    internal static readonly ImmutableArray<string> ReservedNamesWin32 = [".git", "GIT~1"];
    internal static readonly ImmutableArray<string> ReservedNamesPosix = [".git"];

    internal static bool FSyncGitDir { get; set; }
    internal static bool ValidateOwnership { get; set; }

    internal static bool EnableObjectCaching { get; set; }

    private static readonly (GitRepositoryItemType Parent, GitRepositoryItemType Fallback, string? Name, bool Directory)[] Items = [
        (GitRepositoryItemType.GitDir, GitRepositoryItemType._Last, null, true),
        (GitRepositoryItemType.WorkDir, GitRepositoryItemType._Last, null, true),
        (GitRepositoryItemType.CommonDir, GitRepositoryItemType._Last, null, true),
        (GitRepositoryItemType.GitDir, GitRepositoryItemType._Last, "index", false),
        (GitRepositoryItemType.CommonDir, GitRepositoryItemType.GitDir, "objects", true),
        (GitRepositoryItemType.CommonDir, GitRepositoryItemType.GitDir, "refs", true),
        (GitRepositoryItemType.CommonDir, GitRepositoryItemType.GitDir, "packed-refs", false),
        (GitRepositoryItemType.CommonDir, GitRepositoryItemType.GitDir, "remotes", true),
        (GitRepositoryItemType.CommonDir, GitRepositoryItemType.GitDir, "config", false),
        (GitRepositoryItemType.CommonDir, GitRepositoryItemType.GitDir, "info", true),
        (GitRepositoryItemType.CommonDir, GitRepositoryItemType.GitDir, "hooks", true),
        (GitRepositoryItemType.CommonDir, GitRepositoryItemType.GitDir, "logs", true),
        (GitRepositoryItemType.GitDir, GitRepositoryItemType._Last, "modules", true),
        (GitRepositoryItemType.CommonDir, GitRepositoryItemType.GitDir, "worktrees", true),
        (GitRepositoryItemType.GitDir, GitRepositoryItemType.GitDir, "config.worktree", false)
    ];

    private const string CommonDirFile = "commondir";
    private const string GitDirFile = "gitdir";

    private const string FileContentPrefix = "gitdir:";

    private const int RepoVersionDefault = 0;
    private const int RepoVersionMax = 1;

    internal static string PrettifyPath(string path, string? @base)
    {
        string result = @base != null && !Path.IsPathRooted(path) ? Path.GetFullPath(path, @base) : Path.GetFullPath(path);

        return OperatingSystem.IsWindows() ? result.Replace('\\', '/') : result;
    }

    internal static string PrettifyPath_Directory(string path, string? @base)
    {
        string result = @base != null && !Path.IsPathRooted(path) ? Path.GetFullPath(path, @base) : Path.GetFullPath(path);

        if (OperatingSystem.IsWindows())
        {
            if (!Path.EndsInDirectorySeparator(result))
            {
                result = string.Create(result.Length + 1, result, (output, input) =>
                {
                    input.Replace(output, '\\', '/');
                    output[input.Length] = '/';
                });
            }
            else
            {
                result = result.Replace('\\', '/');
            }
        }
        else if (!Path.EndsInDirectorySeparator(result))
        {
            result += "/";
        }

        return result;
    }

    private static string LookupCommonDir(ref bool separate, string repositoryPath, GitRepositoryOpenFlags flags)
    {
        string? commondir;

        if ((flags & GitRepositoryOpenFlags.FromEnvironment) != 0)
        {
            if ((commondir = Environment.GetEnvironmentVariable("GIT_COMMON_DIR")) != null)
            {
                separate = true; 
                return commondir;
            }
        }

        string commonDirFile = Path.Combine(repositoryPath, CommonDirFile);
        if (!File.Exists(commonDirFile))
        {
            separate = false;
            return Path.EndsInDirectorySeparator(repositoryPath) ? repositoryPath : repositoryPath + "/";
        }

        separate = true;

        using (var common_link_buffer = new ArrayPoolBufferWriter<char>())
        {
            File.ReadAllText(commonDirFile, common_link_buffer);

            ReadOnlySpan<char> common_link = common_link_buffer.WrittenSpan.TrimEnd();

            commondir = Path.IsPathFullyQualified(common_link) ? common_link.ToString() : Path.Join(repositoryPath, common_link);
        }

        return PrettifyPath_Directory(commondir, null);
    }

    private static bool ValidateRepoPath(ReadOnlySpan<char> path) => GitPath.ValidateStringLengthWithSuffix(path, "objects/pack/pack-.pack.lock".Length + SHA1.HashSizeInBytes * 2);

    private static bool IsValidRepositoryPath(string repoPath, GitRepositoryOpenFlags flags, out string commonPath)
    {
        bool separateCommonDir = false;
        commonPath = LookupCommonDir(ref separateCommonDir, repoPath, flags);

        if (!File.Exists(Path.Combine(repoPath, Constants.GitHeadFile))
            || !Directory.Exists(Path.Combine(repoPath, "objects"))
            || !Directory.Exists(Path.Combine(repoPath, "refs")))
        {
            return false;
        }

        if (!ValidateRepoPath(commonPath) || (separateCommonDir && !ValidateRepoPath(repoPath)))
        {
            return false;
        }

        if (OperatingSystem.IsWindows())
        {
            commonPath = commonPath.Replace('\\', '/');
        }

        return true;
    }

    private volatile GitObjectDatabase? _odbField;
    public GitObjectDatabase ObjectDatabase
    {
        get => _odbField ?? ObjectDatabaseLazyInit();
        private set
        {
            Debug.Assert(value != null);

            _odbField = value;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // This is the code path that shouldn't be executed often, don't inline it.
    private GitObjectDatabase ObjectDatabaseLazyInit()
    {
        var odb_path = GetObjectDatabasePath();
        var odb = new GitObjectDatabase(this.ObjectIdType);

        this.GetObjectDatabaseAlternates(odb);

        odb.SetCaps(GitObjectDatabaseCapabilities.FromOwner);
        odb.AddDefaultBackends(odb_path, false, 0);

        return Interlocked.CompareExchange(ref _odbField, odb, null) ?? odb;
    }

    private volatile GitReferenceDatabase? _refdbField;
    public GitReferenceDatabase ReferenceDatabase
    {
        get => _refdbField ?? ReferenceDatabaseLazyInit();
        private set
        {
            Debug.Assert(value != null);

            _refdbField = value;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // This is the code path that shouldn't be executed often, don't inline it.
    private GitReferenceDatabase ReferenceDatabaseLazyInit()
    {
        var db = GitReferenceDatabase.Open(this);

        return Interlocked.CompareExchange(ref _refdbField, db, null) ?? db;
    }

    private volatile GitConfig? _configField;
    public GitConfig Config
    {
        get => _configField ?? ConfigLazyInit();
        private set
        {
            Debug.Assert(value != null);

            _configField = value;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // This is the code path that shouldn't be executed often, don't inline it.
    private GitConfig ConfigLazyInit()
    {
        string? system = ConfigPathSystem(this.UseEnv);
        string? global = ConfigPathGlobal(this.UseEnv);
        string? xdg = GitConfig.FindXDG();
        string? programData = GitConfig.FindProgramData();

        if (string.IsNullOrEmpty(global))
            global = GitConfig.GlobalLocation;

        var config = LoadConfig(this,
            string.IsNullOrEmpty(global) ? null : global,
            string.IsNullOrEmpty(xdg) ? null : xdg,
            string.IsNullOrEmpty(system) ? null : system,
            string.IsNullOrEmpty(programData) ? null : programData);

        return Interlocked.CompareExchange(ref _configField, config, null) ?? config;
    }

    private volatile GitIndex? _indexField;
    public GitIndex Index
    {
        get => _indexField ?? IndexLazyInit();
        private set
        {
            Debug.Assert(value != null);

            _indexField = value;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // This is the code path that shouldn't be executed often, don't inline it.
    private GitIndex IndexLazyInit()
    {
        string? indexPath = this.UseEnv ? Environment.GetEnvironmentVariable("GIT_INDEX_FILE") : null;
        indexPath ??= this.GetItemPath(GitRepositoryItemType.Index);

        var index = new GitIndex(indexPath, this.ObjectIdType);

        index.SetCapabilities(GitIndexCapabilities.FromOwner);

        return Interlocked.CompareExchange(ref _indexField, index, null) ?? index;
    }

    private bool UseEnv;

    private string? Identity_Name;
    private string? Identity_Email;

    internal GitGrafts? _grafts;
    internal GitGrafts? _shallow_grafts;

    private readonly object?[] configmap_cache = new object?[15];
    private GitSubmoduleCache? _submoduleCache;

    private uint lruCounter;

    internal GitRepository() : this(GitObjectIDType.Default)
    {
    }

    internal GitRepository(GitObjectIDType type)
    {
        if (!Enum.IsDefined(type) && type != default)
            throw new ArgumentOutOfRangeException(nameof(type));

        IsBare = true;
        IsWorktree = false;
        ObjectIdType = type == default ? GitObjectIDType.SHA1 : type;
    }

    internal GitTree GetHeadTree()
    {
        throw new NotImplementedException();
    }

    internal GitIndex? TryGetIndex()
    {
        throw new NotImplementedException();
    }

    internal GitIndex? GetIndexInternal(bool throwIfNonexistant)
    {
        throw new NotImplementedException();
    }

    internal bool ValidatePathStringLength(ReadOnlySpan<char> path)
    {
        throw new NotImplementedException();
    }

    internal bool IsPathStringValid(ReadOnlySpan<char> path, UnixFileMode file_mode, GitPath.ValidationFlags flags)
    {
        throw new NotImplementedException();
    }

    private ImmutableArray<string> _reservedNames;

    internal ImmutableArray<string> GetReservedNames(bool include_ntfs)
    {
        // git_repository__reserved_names()
        if (OperatingSystem.IsWindows())
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                Constants.ThrowWindowsPlatformNotSupported();

            if (!_reservedNames.IsDefault)
                return _reservedNames;

            if (!this.IsBare)
            {
                HashSet<string> reservedNames = [.. ReservedNamesWin32];

                if (this.GitLink is { } gitlink)
                {
                    add_path_8dot3_name(reservedNames, gitlink);
                }

                var comparison = this.ConfigMapLookup(GitConfigMapItem.IgnoreCase) != 0
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

                if (this.GitDirectory is { } gitdir
                    && gitdir.StartsWith(this.WorkingDirectory!, comparison))
                {
                    add_path_8dot3_name(reservedNames, gitdir);
                }

                ImmutableInterlocked.InterlockedInitialize(ref _reservedNames, [.. reservedNames]);
            }
            else
            {
                _reservedNames = ReservedNamesWin32;
            }

            return _reservedNames;

            [SupportedOSPlatform("windows6.1")]
            static unsafe void add_path_8dot3_name(HashSet<string> reserved, string path)
            {
                const int bufferLen = 266;
                char* buffer = stackalloc char[bufferLen];
                uint len;

                fixed (char* pPath = path)
                    len = Windows.GetShortPathNameW(pPath, buffer, bufferLen);

                if (len >= bufferLen)
                    return;

                var span = new ReadOnlySpan<char>(buffer, (int)len).TrimEnd('\\');

                if (span.IsEmpty)
                    return;

                var name = Path.GetFileName(span);

                if (name.Length > 12)
                    return;

                reserved.GetAlternateLookup<ReadOnlySpan<char>>().Add(name);
            }
        }
        else
        {
            return include_ntfs ? ReservedNamesWin32 : ReservedNamesPosix;
        }
    }

    internal bool TryConfigMapLookup(GitConfigMapItem map, out bool value)
    {
        throw new NotImplementedException();
    }

    internal int ConfigMapLookup(GitConfigMapItem item) => this.Config.ConfigMapLookup(item);

    internal void FlushAttributeCache()
    {
        // Potential for doing this in a less expensive mannor. (Complete Reinitialization is expensive)
        _attributeCache = null;
    }

    private void LoadConfigData(GitConfig config)
    {
        this.IsBare = config.GetBoolean("core.bare") is bool isBare && isBare && !this.IsWorktree;
    }

    private void LoadWorkDirectory(GitConfig config, string parentPath)
    {
        if (this.IsBare)
            return;

        string? value = null;
        if (this.UseEnv)
        {
            value = Environment.GetEnvironmentVariable("GIT_WORK_TREE");
        }

        if (value is null)
        {
            var ce = config.LookupEntry("core.worktree", false);

            if (ce?.Value != null)
            {
                value = ce.Value;
            }
        }

        if (this.IsWorktree)
        {
            string gitlink = GitWorktree.ReadLink(this.GitDirectory, Constants.GitDirFile)!;

            this.WorkingDirectory = string.Concat(Path.GetDirectoryName(gitlink.AsSpan()), "/");
        }
        else if (value is not null)
        {
            if (value.Length == 0)
            {
                throw new Git2Exception("Working Directory cannot be set to an empty path!");
            }

            this.WorkingDirectory = PrettifyPath_Directory(value, this.GitDirectory);
        }
        else if (parentPath is not null && Directory.Exists(parentPath))
        {
            this.WorkingDirectory = parentPath;
        }
        else
        {
            var worktree = Path.GetDirectoryName(this.GitDirectory.AsSpan().TrimEnd('/'));
            if (worktree.IsEmpty)
            {
                throw new Git2Exception("Invalid worktree path!");
            }

            this.WorkingDirectory = string.Concat(worktree, "/");
        }
    }

    private static void CreateHEAD(string gitDir, string refname)
    {
        ArgumentNullException.ThrowIfNull(gitDir);
        ArgumentNullException.ThrowIfNull(refname);

        using var stream = new FileStream(Path.Combine(gitDir, Constants.GitHeadFile), FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, Constants.UTF8NoBOM);

        writer.Write("ref: ");

        if (!refname.StartsWith("refs/"))
            writer.Write("heads/");

        writer.Write(refname);
        writer.Write('\n');
    }

    private static int FindCeilingDirOffset(string path, ReadOnlySpan<char> ceilingDirectories)
    {
        var minLen = Path.GetPathRoot(path.AsSpan()).Length;

        if (ceilingDirectories.IsEmpty || minLen == 0)
            return minLen;

        int maxLen = 0;

        foreach (var range in ceilingDirectories.Split(Constants.PathListSeparator))
        {
            var dirPath = ceilingDirectories[range];

            if (dirPath.Length == 0 || Path.GetPathRoot(dirPath).IsEmpty)
            {
                continue;
            }

            string ceilingDirString = dirPath.ToString();
            ReadOnlySpan<char> realPath = GitPath.RealPath(ceilingDirString);

            if (realPath.EndsWith('/'))
            {
                realPath = realPath[..^1];
            }

            if (path.AsSpan().StartsWith(realPath) && ((uint)realPath.Length >= (uint)path.Length || path[realPath.Length] == '/') && realPath.Length > maxLen)
            {
                maxLen = realPath.Length;
            }
        }

        return Math.Max(minLen, maxLen);
    }

    private static string ReadGitFile(string filePath)
    {
        using var buffer = new ArrayPoolBufferWriter<char>();

        File.ReadAllText(filePath, buffer); // Extension static method

        var file = buffer.WrittenSpan;

        if (!file.StartsWith(Constants.GitFileContentPrefix))
        {
            throw new Git2Exception($"The `.git` file at {filePath} is malformed!");
        }

        file = file.Slice(Constants.GitFileContentPrefix.Length).Trim();

        var result = string.Create(file.Length, file, (output, input) =>
        {
            // Because apparently some people on Windows save backslashed paths.
            input.Replace(output, '\\', '/');
        });

        return PrettifyPath_Directory(result, null);
    }

    private class ValidateOwnershipData
    {
        public required string RepoPath;
        public string? tmp;
        public bool IsSafe;

        public void ValidateOwnershipCallback(GitConfigEntry entry)
        {
            if (entry.Value == "")
            {
                this.IsSafe = false;
            }
            else if (entry.Value == "*")
            {
                this.IsSafe = true;
            }
            else
            {
                var tmp = this.tmp = entry.Value!;

                if (!Path.IsPathRooted(tmp))
                {
                    if (tmp.Length == 0 || tmp[^1] == '/')
                        return;

                    this.tmp = (tmp += "/");
                }

                string test_path = tmp;

                /*
                 * Git - and especially, Git for Windows - does some
                 * truly bizarre things with paths that start with a
                 * forward slash; and expects you to escape that with
                 * `%(prefix)`. This syntax generally means to add the
                 * prefix that Git was installed to (eg `/usr/local`)
                 * unless it's an absolute path, in which case the
                 * leading `%(prefix)/` is just removed. And Git for
                 * Windows expects you to use this syntax for absolute
                 * Unix-style paths (in "Git Bash" or Windows Subsystem
                 * for Linux).
                 *
                 * Worse, the behavior used to be that a leading `/` was
                 * not absolute. It would indicate that Git for Windows
                 * should add the prefix. So `//` is required for absolute
                 * Unix-style paths. Yes, this is truly horrifying.
                 *
                 * Emulate that behavior, I guess, but only for absolute
                 * paths. We won't deal with the Git install prefix. Also,
                 * give WSL users an escape hatch where they don't have to
                 * think about this and can use the literal path that the
                 * filesystem APIs provide (`//wsl.localhost/...`).
                 */

                bool result = (test_path.StartsWith("%(prefix)//") ? test_path.AsSpan(10) : test_path).SequenceEqual(this.RepoPath);

                if (result)
                    this.IsSafe = true;
            }
        }
    }

    private static void ValidateOwndershipConfig(ref bool isSafe, string path, bool useEnv)
    {
        GitConfig config;
        try
        {
            config = LoadGlobalConfig(useEnv);
        }
        catch
        {
            return;
        }

        var ownershipData = new ValidateOwnershipData() { RepoPath = path, IsSafe = isSafe };

        try
        {
            config.ForEachMultiVariable("safe.directory", null, ownershipData.ValidateOwnershipCallback);

            isSafe = ownershipData.IsSafe;
        }
        catch (KeyNotFoundException)
        {
        }
    }

    private static unsafe void ValidateOwnershipPath(ref bool isSafe, string path)
    {
        var ownerLevel = GitPath.PathOwnerType.CurrentUser | GitPath.PathOwnerType.UserIsAdministrator | GitPath.PathOwnerType.RunningSudo;

        if (path is not null)
        {
            try
            {
                isSafe = GitPath.OwnerIs(path, ownerLevel);
            }
            catch (FileNotFoundException)
            {
                isSafe = true;
            }
        }
    }

    private void ValidateOwnershipOperation()
    {
        Span<string?> validation_paths = [null, null, null];
        int validation_len = 0;

        bool isSafe = false;

        if (this.WorkingDirectory != null)
        {
            validation_paths[validation_len++] = this.WorkingDirectory;
        }

        if (this.GitLink != null)
        {
            validation_paths[validation_len++] = this.GitLink;
        }

        validation_paths[validation_len++] = this.GitDirectory;

        string? path = null;

        for (int i = 0; i < validation_len; ++i)
        {
            path = validation_paths[i];

            ValidateOwnershipPath(ref isSafe, path);

            if (!isSafe)
                break;
        }

        if (isSafe)
        {
            ValidateOwndershipConfig(ref isSafe, validation_paths[0], this.UseEnv);
        }

        if (!isSafe)
        {
            throw new Git2Exception($"Repository path '{path}' is not owned by the current user.");
        }
    }

    public struct RepoPaths
    {
        public string? GitDirectory;
        public string? WorkDirectory;
        public string? GitLink;
        public string? CommonDirectory;
    }

    public static unsafe void FindRepositoryTraverse(ref RepoPaths @out, string startPath, string? ceilingDirectories, GitRepositoryOpenFlags flags)
    {
        @out.GitDirectory = null;

        var path = Path.GetFullPath(startPath);

        /*
	     * In each loop we look first for a `.git` dir within the
	     * directory, then to see if the directory itself is a repo.
	     *
	     * In other words: if we start in /a/b/c, then we look at:
	     * /a/b/c/.git, /a/b/c, /a/b/.git, /a/b, /a/.git, /a
	     *
	     * With GIT_REPOSITORY_OPEN_BARE or GIT_REPOSITORY_OPEN_NO_DOTGIT,
	     * we assume we started with /a/b/c.git and don't append .git the
	     * first time through.  min_iterations indicates the number of
	     * iterations left before going further counts as a search.
	     */
        bool indotgit = false;
        int minIterations = 2;
        int ceilingOffset = 0;

        if ((flags & (GitRepositoryOpenFlags.Bare | GitRepositoryOpenFlags.NoDotGit)) != 0)
        {
            indotgit = true;
            minIterations = 1;
        }

        ulong initial_device = 0;

        while (true)
        {
            if ((flags & GitRepositoryOpenFlags.NoDotGit) == 0)
            {
                if (!indotgit)
                {
                    path = Path.Join(path, Constants.DotGit);
                }

                indotgit = !indotgit;
            }

            if (OperatingSystem.IsWindows())
            {
                if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                    throw new PlatformNotSupportedException("Windows versions below NT 6.1 are unsupported!");

                FileAttributes attributes;
                fixed (char* pPath = path)
                {
                    attributes = (FileAttributes)Windows.GetFileAttributesW(pPath);
                }

                if ((uint)attributes != Windows.INVALID_FILE_ATTRIBUTES)
                {
                    // Value returned is identical to the System.IO.FileAttributes enumeration
                    if ((attributes & FileAttributes.Directory) != 0)
                    {
                        if (IsValidRepositoryPath(path, flags, out string common_link))
                        {
                            @out.GitDirectory = path.Replace('\\', '/') + "/";
                            @out.GitLink = GitWorktree.ReadLink(path, Constants.GitDirFile);
                            @out.CommonDirectory = common_link;
                            break;
                        }
                    }
                    else if ((attributes & FileAttributes.Normal) != 0 && Path.GetFileName(path.AsSpan()).SequenceEqual(Constants.DotGit)) // GetFullPath on windows uses \ for all directory separators
                    {
                        var repoLink = ReadGitFile(path);

                        if (IsValidRepositoryPath(repoLink, flags, out string common_link))
                        {
                            @out.GitDirectory = repoLink;
                            @out.GitLink = path;
                            @out.CommonDirectory = common_link;
                        }

                        break;
                    }
                }
            }
            else
            {
                if (Syscall.lstat(path, out var st) == 0)
                {
                    if (initial_device == 0)
                        initial_device = st.st_dev;
                    else if (st.st_dev != initial_device &&
                         (flags & GitRepositoryOpenFlags.CrossFileSystem) == 0)
                        break;

                    var type = st.st_mode & FilePermissions.S_IFMT;
                    if (type == FilePermissions.S_IFDIR) // directory
                    {
                        if (IsValidRepositoryPath(path, flags, out string common_link))
                        {
                            @out.GitDirectory = path + "/";
                            @out.GitLink = GitWorktree.ReadLink(path, Constants.GitDirFile);
                            @out.CommonDirectory = common_link;
                            break;
                        }
                    }
                    else if (type == FilePermissions.S_IFREG) // regular file
                    {
                        var repoLink = ReadGitFile(path);

                        if (IsValidRepositoryPath(repoLink, flags, out string common_link))
                        {
                            @out.GitDirectory = repoLink;
                            @out.GitLink = path;
                            @out.CommonDirectory = common_link;
                        }

                        break;
                    }
                }
            }

            /*
             * Move up one directory. If we're in_dot_git, we'll
             * search the parent itself next. If we're !in_dot_git,
             * we'll search .git in the parent directory next (added
             * at the top of the loop).
             */
            path = Path.GetDirectoryName(path)!;

            /*
             * Once we've checked the directory (and .git if
             * applicable), find the ceiling for a search.
             */
            if (minIterations > 0 && (--minIterations == 0))
            {
                ceilingOffset = FindCeilingDirOffset(path, ceilingDirectories);
            }

            if (minIterations == 0 && ((uint)ceilingOffset >= (uint)path.Length || (flags & GitRepositoryOpenFlags.NoSearch) != 0))
                break;
        }

        if ((flags & GitRepositoryOpenFlags.Bare) == 0)
        {
            if (!string.IsNullOrEmpty(@out.GitDirectory))
            {
                @out.WorkDirectory = null;
            }
            else
            {
                ReadOnlySpan<char> pathSpan = path;
                pathSpan = Path.GetDirectoryName(pathSpan);

                @out.WorkDirectory = string.Create(pathSpan.Length + 1, pathSpan, (output, input) =>
                {
                    if (OperatingSystem.IsWindows())
                    {
                        input.Replace(output, '\\', '/');
                    }
                    else
                    {
                        input.CopyTo(output);
                    }

                    output[input.Length] = '/';
                });
            }
        }

        if (string.IsNullOrEmpty(@out.GitDirectory))
        {
            throw new Git2Exception($"Could not find repository at '{startPath}'!");
        }
    }

    private void LoadGrafts()
    {
        if (_grafts != null && _shallow_grafts != null)
        {
            _grafts.Refresh();
            _shallow_grafts.Refresh();
            return;
        }

        if (!this.TryGetItemPath(GitRepositoryItemType.Info, out string? path))
        {
            _grafts ??= new GitGrafts(this.ObjectIdType, null);
            _shallow_grafts ??= new GitGrafts(this.ObjectIdType, null);
            return;
        }

        if (_grafts == null)
            _grafts = GitGrafts.Open(GitPath.PosixJoin(path, "grafts"), this.ObjectIdType);
        else
            _grafts.Refresh();


        if (_shallow_grafts == null)
            _shallow_grafts = GitGrafts.Open(GitPath.PosixJoin(this.GitDirectory, "shallow"), this.ObjectIdType);
        else
            _shallow_grafts.Refresh();
    }

    private static void FindRepository(ref RepoPaths @out, string startPath, string? ceilingDirectories, GitRepositoryOpenFlags flags)
    {
        if ((flags & GitRepositoryOpenFlags.FromEnvironment) != 0)
        {
            if (string.IsNullOrEmpty(startPath))
            {
                var tmpPath = Environment.GetEnvironmentVariable("GIT_DIR");

                if (tmpPath != null)
                {
                    startPath = tmpPath;
                    flags |= GitRepositoryOpenFlags.NoSearch | GitRepositoryOpenFlags.NoDotGit;
                }
                else
                {
                    startPath = ".";
                }
            }

            if (string.IsNullOrEmpty(ceilingDirectories))
            {
                ceilingDirectories = Environment.GetEnvironmentVariable("GIT_CEILING_DIRECTORIES");
            }

            var acrossFSStr = Environment.GetEnvironmentVariable("GIT_DISCOVERY_ACROSS_FILESYSTEM");

            if (bool.TryParse(acrossFSStr, out bool acrossFs) && acrossFs)
            {
                flags |= GitRepositoryOpenFlags.CrossFileSystem;
            }
        }

        FindRepositoryTraverse(ref @out, startPath, ceilingDirectories, flags);
    }

    private GitConfig? ObtainConfigAndSetOIDType()
    {
        var config = this.GetConfigSnapshot();
        int version = 0;

        if (config != null)
        {
            version = CheckRepositoryFormatVersion(config) ?? 0;

            CheckExtensions(config, version);
        }

        if (version > 0)
        {
            this.LoadObjectFormat(config!);
        }
        else
        {
            this.ObjectIdType = GitObjectIDType.Default;
        }

        return config;
    }

    private static bool HasConfigWorktree(GitConfig config)
    {
        return config.GetBoolean("extensions.worktreeconfig") ?? false;
    }

    private static GitConfig LoadConfig(GitRepository? repo, string? globalConfigPath, string? xdgConfigPath, string? systemConfigPath, string? programdataPath)
    {
        var config = new GitConfig();

        if (repo != null)
        {
            if (repo.TryGetItemPath(GitRepositoryItemType.Config, out string? configPath))
            {
                config.AddFileOnDisk(configPath, GitConfigLevel.Local, repo, false);
            }

            if (HasConfigWorktree(config)
                && repo.TryGetItemPath(GitRepositoryItemType.WorkTreeConfig, out configPath))
            {
                config.AddFileOnDisk(configPath, GitConfigLevel.Worktree, repo, false);
            }
        }

        if (globalConfigPath != null)
        {
            config.AddFileOnDisk(globalConfigPath, GitConfigLevel.Global, repo, false);
        }

        if (xdgConfigPath != null)
        {
            config.AddFileOnDisk(xdgConfigPath, GitConfigLevel.XDG, repo, false);
        }

        if (systemConfigPath != null)
        {
            config.AddFileOnDisk(systemConfigPath, GitConfigLevel.System, repo, false);
        }

        if (programdataPath != null)
        {
            config.AddFileOnDisk(programdataPath, GitConfigLevel.ProgramData, repo, false);
        }

        config.SetWriteOrder([GitConfigLevel.Local]);

        return config;
    }

    private static string? ConfigPathSystem(bool useEnv)
    {
        if (useEnv)
        {
            if (bool.TryParse(Environment.GetEnvironmentVariable("GIT_CONFIG_NOSYSTEM"), out bool no_system) && no_system)
                return null;

            string? system = Environment.GetEnvironmentVariable("GIT_CONFIG_SYSTEM");
            if (system != null)
            {
                return system;
            }
        }

        return GitConfig.FindSystem();
    }

    private static string? ConfigPathGlobal(bool useEnv)
    {
        if (useEnv)
        {
            string? global = Environment.GetEnvironmentVariable("GIT_CONFIG_GLOBAL");

            if (global != null)
                return global;
        }

        return GitConfig.FindGlobal();
    }

    private void LoadNamespace()
    {
        if (!this.UseEnv)
            return;

        this.Namespace = Environment.GetEnvironmentVariable("GIT_NAMESPACE");
    }

    private bool RepositoryIsWorktree()
    {
        if (this.CommonDirectory != null && this.GitDirectory != null && this.CommonDirectory != this.GitDirectory)
        {
            return false;
        }

        string gitdir_link = Path.Combine(this.GitDirectory!, "gitdir");

        return Path.Exists(gitdir_link);
    }

    internal static GitRepository WrapObjectDatabase(GitObjectDatabase odb)
    {
        Debug.Assert(Enum.IsDefined(odb.Options_OID_Type));

        return new GitRepository() { ObjectIdType = odb.Options_OID_Type, ObjectDatabase = odb };
    }

    private static int? CheckRepositoryFormatVersion(GitConfig config)
    {
        int? version = config.GetInt32("core.repositoryformatversion");

        if (version == null)
            return null;

        // assume success from here forward
        if (version < 0)
            throw new Git2Exception("Invalid repository version!");

        if (version > Constants.GitRepositoryMaxVersion)
        {
            throw new Git2Exception($"Unsupported repository version {version}; Only versions up to {Constants.GitRepositoryMaxVersion} are supported.");
        }

        return version;
    }

    private static readonly ImmutableArray<string> BuiltinExtensions = [
        "noop",
        "objectformat",
        "worktreeconfig",
        "preciousobjects"
    ];

    private static readonly HashSet<string> UserExtensions = [];

    private static void CheckExtensions(GitConfig config, int version)
    {
        if (version < 1)
            return;

        config.ForEachMatch("^extensions\\\\.", entry =>
        {
            // TODO: optimize string allocations
            bool reject = false;

            foreach (var extension in UserExtensions)
            {
                var extensionSpan = extension.AsSpan();
                if (reject = extensionSpan.StartsWith('!'))
                    extensionSpan = extensionSpan[1..];

                if (entry.Name == $"extensions.{extensionSpan}")
                {
                    if (reject)
                        goto Fail;

                    return;
                }
            }

            foreach (var extension in BuiltinExtensions)
            {
                if (entry.Name == $"extensions.{extension}")
                {
                    return;
                }
            }

        Fail:
            throw new Git2Exception($"Unsupported extension name {entry.Name}");
        });
    }

    private void LoadObjectFormat(GitConfig config)
    {
        if (!config.TryGetEntry("extensions.objectformat", out var entry))
        {
            this.ObjectIdType = GitObjectIDType.Default;
            return;
        }

        if (!Enum.TryParse<GitObjectIDType>(entry.Value, true, out var type))
        {
            throw new Git2Exception($"Unknown object format '{entry.Value}'");
        }

        this.ObjectIdType = type;
    }

    private static GitConfig LoadGlobalConfig(bool useEnv)
    {
        string? system = ConfigPathSystem(useEnv),
            global = ConfigPathGlobal(useEnv),
            xdg = GitConfig.FindXDG(),
            programdata = GitConfig.FindProgramData();

        return LoadConfig(null, global, xdg, system, programdata);
    }

    private string GetObjectDatabasePath()
    {
        string? path = this.UseEnv ? Environment.GetEnvironmentVariable("GIT_OBJECT_DIRECTORY") : null;

        path ??= GetItemPath(GitRepositoryItemType.Objects);

        return path;
    }

    private void GetObjectDatabaseAlternates(GitObjectDatabase odb)
    {
        if (!this.UseEnv)
            return;

        ReadOnlySpan<char> alternates = Environment.GetEnvironmentVariable("GIT_ALTERNATE_OBJECT_DIRECTORIES");

        if (alternates.IsEmpty)
            return;

        foreach (var altRange in alternates.Split(Constants.PathListSeparator))
        {
            var alt = PathUtilities.PathStringPool.GetOrAdd(alternates[altRange]);

            odb.AddDiskAlternate(alt);
        }
    }

    internal void SetObjectFormat(GitObjectIDType type)
    {
        /*
         * Older clients do not necessarily understand the
         * `objectformat` extension, even when it's set to an
         * object format that they understand (SHA1). Do not set
         * the objectformat extension unless we're not using the
         * default object format.
         */
        if (type == GitObjectIDType.Default)
            return;

        if (!this.IsEmpty() && this.ObjectIdType != type)
            throw new InvalidOperationException("Cannot change Object ID Type of existing Repository.");

        var config = this.Config;

        config.Set("core.repositoryformatversion", 1);
        config.Set("extensions.objectformat", type.ToString().ToLowerInvariant());

        /*
         * During repo init, we may create some backends with the
         * default oid type. Clear them so that we create them with
         * the proper oid type.
         */
        if (this.ObjectIdType != type)
        {
            this.ObjectIdType = type;
            _indexField = null;
            _odbField = null;
            _refdbField = null;
        }
    }

    internal static IEnumerable<string> Extensions
    {
        get
        {
            return BuiltinExtensions.Concat(UserExtensions).Distinct().Order();
        }
    }

    internal static void SetExtensions(IEnumerable<string> extensions)
    {
        var userExtensions = UserExtensions;
        userExtensions.Clear();

        foreach (var ext in extensions)
        {
            if (BuiltinExtensions.Contains(ext))
                continue;

            userExtensions.Add(ext);
        }
    }

    private static bool IsChangeModeSupported(string path)
    {
        if (OperatingSystem.IsWindows())
            return false;

        if (Syscall.lstat(path, out var st1) < 0)
            return false;

        if (Syscall.chmod(path, st1.st_mode ^ FilePermissions.S_IXUSR) < 0)
            return false;

        if (Syscall.lstat(path, out var st2) < 0)
            return false;

        return st1.st_mode != st2.st_mode;
    }

    private static bool IsFilesystemCaseInsensitive(string gitdirPath)
    {
        return Path.Exists(Path.Combine(gitdirPath, "CoNfIg"));
    }

    private static bool AreSymlinksSupported(string workDirectory, bool useEnv)
    {
        /*
         * To emulate Git for Windows, symlinks on Windows must be explicitly
         * opted-in.  We examine the system configuration for a core.symlinks
         * set to true.  If found, we then examine the filesystem to see if
         * symlinks are _actually_ supported by the current user.  If that is
         * _not_ set, then we do not test or enable symlink support.
         */
        if (OperatingSystem.IsWindows())
        {
            var config = LoadGlobalConfig(useEnv);

            if (config.GetBoolean("core.symlinks") != true)
            {
                return false;
            }
        }

        return GitPath.SupportsSymlinks(workDirectory);
    }

    private static void CreateEmptyFile(string filename, UnixFileMode mode)
    {
        using var handle = File.OpenHandle(filename, FileMode.Create, FileAccess.Write, FileShare.None);

        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(handle, mode);
    }

    private static GitConfig LocalConfig(GitRepository? repo, string repoDir, out string configDir)
    {
        string configPath = GitPath.PosixJoin(repoDir, GitConfig.FileNameInRepo);
        configDir = configPath;

        if (!File.Exists(configPath))
        {
            CreateEmptyFile(configPath, GitConfig.FileMode);
        }

        if (repo == null)
            return GitConfig.OpenOnDisk(configPath);

        var parent = repo.Config;

        if (!parent.TryOpenLevel(GitConfigLevel.Local, out GitConfig result))
        {
            parent.AddFileOnDisk(configPath, GitConfigLevel.Local, repo, false);

            result = parent.OpenLevel(GitConfigLevel.Local);
        }

        return result;
    }

    private static void InitFileSystemConfigs(GitConfig config, string configPath, string repoDir, string? workDir, bool updateIgnoreCase, bool useEnv)
    {
        workDir ??= repoDir;

        config.Set("core.filemode", IsChangeModeSupported(configPath));

        if (!AreSymlinksSupported(workDir, useEnv))
        {
            config.Set("core.symlinks", false);
        }
        else
        {
            config.DeleteEntry("core.symlinks");
        }

        if (updateIgnoreCase)
        {
            if (IsFilesystemCaseInsensitive(repoDir))
            {
                config.Set("core.ignorecase", true);
            }
            else
            {
                config.DeleteEntry("core.ignorecase");
            }
        }

#if GIT_I18N_ICONV
        config.Set("core.precomposeunicode", GitPath.DoesDecomposeUnicode(workDir));
#endif
    }

    private static void InitConfig(string repoDir, string workDir, GitRepositoryInitFlags flags, bool useEnv, UnixFileMode mode, GitObjectIDType type)
    {
        bool isBare = (flags & GitRepositoryInitFlags.Bare) != 0;
        int version = Constants.RepositoryVersionDefault;

        var config = LocalConfig(null, repoDir, out string configPath);

        if ((flags & InitFlags_IsReinit) == InitFlags_IsReinit)
        {
            version = CheckRepositoryFormatVersion(config) ?? Constants.RepositoryVersionDefault;
        }

        CheckExtensions(config, version);

        config.Set("core.bare", isBare);
        config.Set("core.repositoryformatversion", version);

        InitFileSystemConfigs(config, configPath, repoDir, workDir, (flags & InitFlags_IsReinit) == 0, useEnv);
        if (!isBare)
        {
            config.Set("core.logallrefupdates", true);

            if ((flags & InitFlags_NaturalWD) == 0)
            {
                string worktreePath = workDir;

                if ((flags & GitRepositoryInitFlags.RelativeGitlink) != 0)
                    worktreePath = Path.GetRelativePath(repoDir, worktreePath);

                if (OperatingSystem.IsWindows())
                    worktreePath = worktreePath.Replace('\\', '/');

                config.Set("core.worktree", worktreePath);
            }
            else if ((flags & InitFlags_IsReinit) != 0)
            {
                config.DeleteEntry("core.worktree");
            }
        }

        if (mode == InitSharedGroup)
        {
            config.Set("core.sharedrepository", 1);
            config.Set("receive.denyNonFastforwards", true);
        }
        else if (mode == InitSharedAll)
        {
            config.Set("core.sharedrepository", 2);
            config.Set("receive.denyNonFastforwards", true);
        }

        if (type != GitObjectIDType.Default)
        {
            config.Set("core.repositoryformatversion", 1);
            config.Set("extensions.objectformat", type.ToString().ToLowerInvariant());
        }
    }

    internal void ReinitFilesystem(bool recurse)
    {
        string repoDir = this.GitDirectory;

        var config = LocalConfig(this, repoDir, out string path);

        InitFileSystemConfigs(config, path, repoDir, this.WorkingDirectory, true, this.UseEnv);

        Array.Clear(this.configmap_cache);

        if (!this.IsBare && recurse)
        {
            foreach (var submodule in this.Submodules)
            {
                try
                {
                    GitRepository smrepo = submodule.Open();

                    smrepo.ReinitFilesystem(true);
                }
                catch
                {
                }
            }
        }
    }

    private static void WriteTemplate(string gitdir, bool allow_overwrite, string file, UnixFileMode mode, bool hidden, string content)
    {
        using var handle = File.OpenHandle(Path.Combine(gitdir, file), allow_overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None);

        if (OperatingSystem.IsWindows())
        {
            var attributes = File.GetAttributes(handle);

            var newAttributes = hidden ? attributes | FileAttributes.Hidden : attributes & ~FileAttributes.Hidden;

            if (attributes != newAttributes)
                File.SetAttributes(handle, newAttributes);
        }
        else
        {
            File.SetUnixFileMode(handle, mode);
        }

        using var writer = new StreamWriter(new FileStream(handle, FileAccess.Write), Constants.UTF8NoBOM);

        writer.Write(content);
    }

    private static bool WriteGitlink(string inDir, string toRepo, bool useRelativePath)
    {
        var span = Path.TrimEndingDirectorySeparator(toRepo.AsSpan());
        if (Path.GetFileName(span).SequenceEqual(Constants.DotGit)
            && Path.GetDirectoryName(span).SequenceEqual(inDir))
        {
            // don't write gitlink to natural workdir
            return false;
        }

        var dirpath = Path.Combine(inDir, Constants.DotGit);

        if (OperatingSystem.IsWindows())
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                Constants.ThrowWindowsPlatformNotSupported();

            var attributes = GitPath.Win32GetFileAttributes(dirpath);

            if (attributes == null || (attributes & FileAttributes.Normal) == 0)
                throw new Git2Exception($"Cannot overwrite gitlink file into path '{inDir}'");
        }
        else
        {
            if (Syscall.lstat(dirpath, out var stat) != 0 || (stat.st_mode & FilePermissions.S_IFREG) == 0)
            {
                throw new Git2Exception($"Cannot overwrite gitlink file into path '{inDir}'");
            }
        }

        string pathToRepo = toRepo;

        if (useRelativePath)
            pathToRepo = Path.GetRelativePath(inDir, pathToRepo);

        WriteTemplate(inDir, true, Constants.DotGit, Constants.DefaultFileMode, true, $"{Constants.GitFileContentPrefix} {pathToRepo}");
        return true;
    }

    private static UnixFileMode PickDirMode(GitRepositoryInitOptions options)
    {
        const UnixFileMode AllReadWriteExecutePerms = UnixFileMode.UserRead
                    | UnixFileMode.UserWrite
                    | UnixFileMode.UserExecute
                    | UnixFileMode.GroupRead
                    | UnixFileMode.GroupWrite
                    | UnixFileMode.GroupExecute
                    | UnixFileMode.OtherRead
                    | UnixFileMode.OtherWrite
                    | UnixFileMode.OtherExecute;

        return options.Mode switch
        {
            0 => AllReadWriteExecutePerms,
            InitSharedGroup => AllReadWriteExecutePerms & ~UnixFileMode.OtherWrite,
            InitSharedAll => AllReadWriteExecutePerms,
            _ => options.Mode,
        };
    }

    private static void InitStructure(string repoDir, string workDir, ref GitRepositoryInitOptions options)
    {
        var mode = PickDirMode(options);
        bool chmod = options.Mode != 0;

        if (OperatingSystem.IsWindows() && (options.Flags & InitFlags_HasDotGit) != 0)
        {
            try
            {
                var attributes = File.GetAttributes(repoDir);

                if ((attributes & FileAttributes.Hidden) == 0)
                    File.SetAttributes(repoDir, attributes | FileAttributes.Hidden);
            }
            catch (Exception e)
            {
                throw new Git2OSException("Failed to mark Git repository folder as hidden!", e);
            }
        }

        if ((options.Flags & GitRepositoryInitFlags.Bare) == 0 && (options.Flags & InitFlags_NaturalWD) == 0)
        {
            WriteGitlink(workDir, repoDir, (options.Flags & GitRepositoryInitFlags.RelativeGitlink) != 0);
        }

        bool externalTemplate = options.TemplatePath != null || (options.Flags & GitRepositoryInitFlags.ExternalTemplate) != 0;

        if (externalTemplate)
        {
            string? tdir;
            bool default_template = false;

            try
            {
                if ((tdir = options.TemplatePath) == null)
                {
                    try
                    {
                        GitConfig config = GitConfig.OpenDefault();

                        tdir = config.GetString("init.templatedir");
                    }
                    catch
                    {
                        // ignore all errors and continue
                    }

                    if (tdir == null)
                    {
                        tdir = SystemDirectory.FindTemplateDirectory(false);
                        default_template = true;
                    }
                }

                if (!string.IsNullOrEmpty(tdir))
                {
                    var copyFlags = GitPath.CopyDirectoryFlags.CopySymlinks
                        | GitPath.CopyDirectoryFlags.SimpleTargetMode
                        | GitPath.CopyDirectoryFlags.CopyDotFiles;

                    if (options.Mode != 0)
                        copyFlags |= GitPath.CopyDirectoryFlags.ChangeModeDirectories;

                    GitPath.CopyRecursive(tdir, repoDir, copyFlags, mode);
                }
            }
            catch (Exception e)
            {
                if (!default_template && e is not FileNotFoundException)
                    throw;

                externalTemplate = false;
            }
        }

        foreach (var (path, templateMode, content) in RepoTemplates)
        {
            if (content == null)
            {
                var tpath = Path.Combine(repoDir, path);
                FileSystemHelpers.CreateDirectory(tpath, mode);
            }
            else if (!externalTemplate)
            {
                string? desc = path == DescFile ? options.Description : null;

                WriteTemplate(repoDir, false, path, templateMode, false, desc ?? content);
            }
        }
    }

    private static void MakeDirParent(string path, UnixFileMode mode, bool skip2)
    {
        ReadOnlySpan<char> parent = Path.GetDirectoryName(path.AsSpan());

        if (skip2)
            parent = Path.GetDirectoryName(parent);

        string tmpPath = GitPath.PathPool.GetOrAdd(parent);

        _ = OperatingSystem.IsWindows()
            ? Directory.CreateDirectory(tmpPath)
            : Directory.CreateDirectory(tmpPath, mode & ~UnixFileMode.OtherWrite);
    }

    private static void InitDirectories(string givenPath, ref GitRepositoryInitOptions options, out string repoPath, out string? workDirPath)
    {
        givenPath = GitPath.PathPool.GetOrAdd(Path.GetFullPath(givenPath).Replace('\\', '/'));

        Debug.Assert(GitPath.PathStringIsValid(null, givenPath, default, GitPath.DefaultValidation));

        bool isBare = (options.Flags & GitRepositoryInitFlags.Bare) != 0;

        bool has_dotgit = Path.GetFileName(givenPath.AsSpan().TrimEnd('/')).SequenceEqual(Constants.DotGit);

        if (!isBare && options.WorkingDirectoryPath == null && !has_dotgit)
        {
            repoPath = GitPath.PathPool.GetOrAdd(GitPath.PosixJoin(givenPath, Constants.DotGit));

            has_dotgit = true;
        }
        else
        {
            repoPath = givenPath;
        }

        if (has_dotgit)
        {
            options.Flags |= InitFlags_HasDotGit;
        }

        if (!isBare)
        {
            if (options.WorkingDirectoryPath != null)
            {
                workDirPath = Path.Combine(repoPath, options.WorkingDirectoryPath);
            }
            else if (has_dotgit)
            {
                workDirPath = GitPath.PathPool.GetOrAdd(Path.GetDirectoryName(repoPath.AsSpan()));
            }
            else
            {
                throw new Git2Exception("Cannot pick working directory for non-bare repository that isn't a '.git' directory!");
            }
        }
        else
        {
            workDirPath = null;
        }

        bool natural_wd;
        {
            ReadOnlySpan<char> rpath = repoPath, wpath = workDirPath;

            natural_wd = has_dotgit
                && !wpath.IsEmpty
                && rpath.StartsWith(wpath)
                && rpath.Slice(wpath.Length).SequenceEqual("/" + Constants.DotGit);
        }


        if (natural_wd)
            options.Flags |= InitFlags_NaturalWD;

        var dirmode = PickDirMode(options);

        if ((options.Flags & GitRepositoryInitFlags.MakePath) != 0)
        {
            /* create path #4 */
            if (!string.IsNullOrEmpty(workDirPath))
            {
                FileSystemHelpers.CreateDirectory(workDirPath, dirmode);
            }

            if (!natural_wd)
            {
                FileSystemHelpers.CreateDirectory(repoPath, dirmode);
            }
        }
        else if ((options.Flags & GitRepositoryInitFlags.MakeDirectory) != 0)
        {
            if (!string.IsNullOrEmpty(workDirPath))
            {
                string parent = Path.GetDirectoryName(workDirPath)!;
                if (!Directory.Exists(parent))
                {
                    throw new DirectoryNotFoundException($"The directory '{parent}' does not exist!");
                }

                FileSystemHelpers.CreateDirectory(workDirPath, dirmode);
            }

            if (!natural_wd)
            {
                string parent = Path.GetDirectoryName(repoPath)!;
                if (!Directory.Exists(parent))
                {
                    throw new DirectoryNotFoundException($"The directory '{parent}' does not exist!");
                }

                FileSystemHelpers.CreateDirectory(repoPath, dirmode);
            }
        }

        if (has_dotgit)
        {
            if ((options.Flags & (GitRepositoryInitFlags.MakeDirectory | GitRepositoryInitFlags.MakePath)) == 0)
            {
                string parent = Path.GetDirectoryName(repoPath)!;

                if (!Directory.Exists(parent))
                    throw new DirectoryNotFoundException($"The directory '{parent}' does not exist!");
            }

            FileSystemHelpers.CreateDirectory(repoPath, dirmode);
        }

        repoPath = PrettifyPath_Directory(repoPath, null);

        if (!string.IsNullOrEmpty(workDirPath))
        {
            workDirPath = PrettifyPath_Directory(workDirPath, null);
        }
    }

    private static void InitHead(string repoDir, string? given)
    {
        var headPath = Path.Combine(repoDir, Constants.GitHeadFile);
        if (File.Exists(headPath) && given == null)
            return;

        string? initialHead = given;

        if (initialHead == null)
        {
            var config = GitConfig.OpenDefault();
            var configBranch = config.GetString("init.defaultbranch");

            if (!string.IsNullOrEmpty(configBranch))
                initialHead = configBranch;
        }

        initialHead ??= Constants.GitDefaultBranch;

        CreateHEAD(repoDir, initialHead);
    }

    private static void InitCreateOrigin(GitRepository repository, string url)
    {
        GitRemote.Create(repository, Constants.GitRemoteOrigin, url);
    }

    internal void ConfigMapLookupCacheClear()
    {
        throw new NotImplementedException();
    }

    internal bool IsValidObjectId(in GitObjectID oid, GitObjectType type)
    {
        throw new NotImplementedException();
    }

    internal IEnumerable<GitRepository> EnumerateWorktreesInternal()
    {
        if (this.CommonDirectory == null)
        {
            yield return this;
            yield break;
        }

        yield return GitRepository.Open(this.CommonDirectory);

        throw new NotImplementedException();
    }
}
