using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Mono.Unix.Native;

using SharpGit2.Managed.Internal;

namespace SharpGit2.Managed;

internal static class SystemDirectory
{
    /// <summary>
    /// Find a "system" file (i.e. one shared for all users of the system).
    /// </summary>
    /// <param name="filename">Name of file to find in the directory</param>
    /// <returns>The full path of the file, or null if not found.</returns>
    public static string? FindSystemFile(string? filename, bool throwIfNotFound)
    {
        Debug.Assert(SystemDirectories != null);
        return FindInDirectoryList(SystemDirectories, filename, throwIfNotFound, "system");
    }

    /// <summary>
    /// Find a "global" file (i.e. one in a user's home directory).
    /// </summary>
    /// <param name="filename">Name of file to find in the directory</param>
    /// <returns>The full path of the file, or null if not found.</returns>
    public static string? FindGlobalFile(string? filename, bool throwIfNotFound)
    {
        Debug.Assert(GlobalDirectories != null);
        return FindInDirectoryList(GlobalDirectories, filename, throwIfNotFound, "global");
    }

    /// <summary>
    /// Find an "XDG" file (i.e. one in user's XDG config paths).
    /// </summary>
    /// <param name="filename">Name of file to find in the directory</param>
    /// <returns>The full path of the file, or null if not found.</returns>
    public static string? FindXDGFile(string? filename, bool throwIfNotFound)
    {
        Debug.Assert(XDGDirectories != null);
        return FindInDirectoryList(XDGDirectories, filename, throwIfNotFound, "global/xdg");
    }

    /// <summary>
    /// Find a "ProgramData" file (i.e. one in %PROGRAMDATA%)
    /// </summary>
    /// <param name="filename">Name of file to find in the directory</param>
    /// <returns>The full path of the file, or null if not found.</returns>
    public static string? FindProgramDataFile(string? filename, bool throwIfNotFound)
    {
        Debug.Assert(ProgramDataDirectories != null);
        return FindInDirectoryList(ProgramDataDirectories, filename, throwIfNotFound, "ProgramData");
    }

    /// <summary>
    /// Find the template directory.
    /// </summary>
    /// <returns>The full path of the directory, or null if not found.</returns>
    public static string? FindTemplateDirectory(bool throwIfNotFound)
    {
        Debug.Assert(TemplateDirectories != null);
        return FindInDirectoryList(TemplateDirectories, null, throwIfNotFound, "template");
    }

    /// <summary>
    /// Find the home directory. On Windows, this will look at the `HOME`,
    /// `HOMEPATH`, and `USERPROFILE` environment variables (in that order)
    /// and return the first path that is set and exists. On other systems,
    /// this will simply return the contents of the `HOME` environment variable.
    /// </summary>
    /// <returns>The full path of the directory, or null if not found.</returns>
    /// <exception cref="NotImplementedException"></exception>
    public static string? FindHomeDirectory(bool throwIfNotFound)
    {
        Debug.Assert(HomeDirectories != null);
        return FindInDirectoryList(HomeDirectories, null, throwIfNotFound, "home");
    }

    /// <summary>
    /// Expand the name of a "global" file -- by default inside the user's
    /// home directory, but can be overridden by the user configuration.<br/>
    /// Unlike `find_global_file` (above), this makes no attempt to check
    /// for the existence of the file, and is useful if you want the full
    /// path regardless of existence.
    /// </summary>
    /// <param name="filename">Name of file in the directory</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static string? ExpandGlobalFile(string filename)
    {
        string? result = FindGlobalFile(null, false);

        if (result is not null)
        {
            result = $"{result}/{filename}";
        }

        return result;
    }

    /// <summary>
    /// Expand the name of a file in the user's home directory. This
    /// function makes no attempt to check for the existence of the file,
    /// and is useful if you want the full path regardless of existence.
    /// </summary>
    /// <param name="filename">Name of file in the directory</param>
    /// <returns></returns>
    public static string? ExpandHomeDirectoryFile(string filename)
    {
        string? result = FindHomeDirectory(false);

        if (result is not null)
        {
            result = GitPath.PosixJoin(result, filename);
        }

        return result;
    }

    public static string? ExpandHomeDirectoryFile(ReadOnlySpan<char> filename)
    {
        string? result = FindHomeDirectory(false);

        if (result is not null)
        {
            result = GitPath.PosixJoin(result, filename);
        }

        return result;
    }

    private static readonly ImmutableHashSet<string> SysDirs_InitValue;
    private static readonly ImmutableHashSet<string> GlobalDirs_InitValue;
    private static readonly ImmutableHashSet<string> XDGDirs_InitValue;
    private static readonly ImmutableHashSet<string> ProgramDataDirs_InitValue;
    private static readonly ImmutableHashSet<string> TemplateDirs_InitValue;
    private static readonly ImmutableHashSet<string> HomeDirs_InitValue;

    private static IEqualityComparer<string> PathStringComparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static volatile ImmutableHashSet<string> SystemDirectories;
    private static volatile ImmutableHashSet<string> GlobalDirectories;
    private static volatile ImmutableHashSet<string> XDGDirectories;
    private static volatile ImmutableHashSet<string> ProgramDataDirectories;
    private static volatile ImmutableHashSet<string> TemplateDirectories;
    private static volatile ImmutableHashSet<string> HomeDirectories;

    static SystemDirectory()
    {
        SysDirs_InitValue = !OperatingSystem.IsWindows() ? ["/etc"] : FindSystemDirsWin32("etc");
        SystemDirectories = SysDirs_InitValue;

        HomeDirs_InitValue = GuessHomeDirs();
        HomeDirectories = HomeDirs_InitValue;

        GlobalDirs_InitValue = HomeDirs_InitValue;
        GlobalDirectories = GlobalDirs_InitValue;
        
        XDGDirs_InitValue = GuessXDGDirs();
        XDGDirectories = XDGDirs_InitValue;
        
        ProgramDataDirs_InitValue = !OperatingSystem.IsWindows() ? [] : FindWin32Dirs(["%PROGRAMDATA%\\Git"]);
        ProgramDataDirectories = ProgramDataDirs_InitValue;
        
        TemplateDirs_InitValue = !OperatingSystem.IsWindows() ? ["/usr/share/git-core/templates"] : FindSystemDirsWin32("share/git-core/templates");
        TemplateDirectories = TemplateDirs_InitValue;
    }

    public static IEnumerable<string> GetDirectories(Type which)
    {
        return which switch
        {
            Type.System => SystemDirectories,
            Type.Global => GlobalDirectories,
            Type.XDG => XDGDirectories,
            Type.ProgramData => ProgramDataDirectories,
            Type.Template => TemplateDirectories,
            Type.Home => HomeDirectories,
            _ => throw new ArgumentOutOfRangeException(nameof(which)),
        };
    }

    public static void AddDirectory(Type which, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile
        switch (which)
        {
            case Type.System:
                Add(ref SystemDirectories, path);
                break;
            case Type.Global:
                Add(ref GlobalDirectories, path);
                break;
            case Type.ProgramData:
                Add(ref ProgramDataDirectories, path);
                break;
            case Type.Template:
                Add(ref TemplateDirectories, path);
                break;
            case Type.Home:
                Add(ref HomeDirectories, path);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(which));
        }
#pragma warning restore CS0420 // A reference to a volatile field will not be treated as volatile

        //IsVolatile

        static void Add(ref ImmutableHashSet<string> set, string path)
        {
            ImmutableInterlocked.Update(ref set, (set, path) => set.Add(path), path);
        }
    }

    public static void SetDirectories(Type which, IEnumerable<string> search_paths)
    {
        ArgumentNullException.ThrowIfNull(search_paths);

        var set = search_paths.Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(PathUtilities.NormalizePath)
            .ToImmutableHashSet(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        switch (which)
        {
            case Type.System:
                SystemDirectories = set;
                break;
            case Type.Global:
                GlobalDirectories = set;
                break;
            case Type.XDG:
                XDGDirectories = set;
                break;
            case Type.ProgramData:
                ProgramDataDirectories = set;
                break;
            case Type.Template:
                TemplateDirectories = set;
                break;
            case Type.Home:
                HomeDirectories = set;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(which));
        }
    }

    public static void Reset()
    {
        SystemDirectories = SysDirs_InitValue;
        GlobalDirectories = GlobalDirs_InitValue;
        XDGDirectories = XDGDirs_InitValue;
        ProgramDataDirectories = ProgramDataDirs_InitValue;
        TemplateDirectories = TemplateDirs_InitValue;
        HomeDirectories = HomeDirs_InitValue;
    }

    private static ImmutableHashSet<string> GuessXDGDirs()
    {
        if (OperatingSystem.IsWindows())
        {
            return FindWin32Dirs(["%XDG_CONFIG_HOME%\\git", "%APPDATA%\\git", "%LOCALAPPDATA%\\git", "%HOME%\\.config\\git", "%HOMEDRIVE%%HOMEPATH%\\.config\\git", "%USERPROFILE%\\.config\\git"]);
        }
        else
        {
            uint uid = Syscall.getuid(),
                euid = Syscall.geteuid();

            string? env;
            if (uid == euid)
            {
                env = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (env is not null)
                {
                    env = Path.Combine(env, "git");
                }
                else
                {
                    env = Environment.GetEnvironmentVariable("HOME");

                    if (env is not null)
                    {
                        env = Path.Combine(env, ".config/git");
                    }
                }
            }
            else
            {
                env = Path.Combine(GetPasswdHome(euid), ".config/git");
            }

            return env is null ? [] : [env];
        }
    }

    private static ImmutableHashSet<string> GuessHomeDirs()
    {
        if (OperatingSystem.IsWindows())
        {
            return FindWin32Dirs(["%HOME%\\", "%HOMEDRIVE%%HOMEPATH%\\", "%USERPROFILE%\\"]);
        }
        else
        {
            string? sandbox_id = Environment.GetEnvironmentVariable("APP_SANDBOX_CONTAINER_ID");
            uint uid = Syscall.getuid(),
                euid = Syscall.geteuid();

            string? result = (sandbox_id is null && uid == euid ? Environment.GetEnvironmentVariable("HOME") : GetPasswdHome(euid))?.TrimEnd('/');

            return result is null ? [] : [result];
        }
    }

    private static string? FindInDirectoryList(ImmutableHashSet<string> sysPaths, string? fileName, bool throwIfNotFound, string label)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            foreach (var path in sysPaths)
            {
                if (Directory.Exists(path))
                    return path;
            }

            if (throwIfNotFound)
                throw new DirectoryNotFoundException($"The {label} directory does not exist!");
        }
        else
        {
            char[]? buffer = null;

            try
            {
                foreach (var path in sysPaths)
                {
                    int length = path.Length + 1 + fileName.Length;

                    if (buffer is null)
                    {
                        buffer = ArrayPool<char>.Shared.Rent(length);
                    }
                    else if (buffer.Length < length)
                    {
                        ArrayPool<char>.Shared.Return(buffer);
                        buffer = ArrayPool<char>.Shared.Rent(length);
                    }

                    bool success = Path.TryJoin(path, fileName, buffer, out int written);
                    Debug.Assert(success);

                    var span = buffer.AsSpan(0, written);

                    if (OperatingSystem.IsWindows())
                        span.Replace('\\', '/');

                    string finalPath = span.ToString();

                    if (Path.Exists(finalPath))
                        return finalPath;
                }
            }
            finally
            {
                if (buffer is not null)
                    ArrayPool<char>.Shared.Return(buffer);
            }

            if (throwIfNotFound)
                throw new FileNotFoundException($"The {label} file '{fileName}' does not exist!");
        }

        return null;
    }

    #region Windows-Specific
    [SupportedOSPlatform("windows")]
    private static ImmutableHashSet<string> FindWin32Dirs(ReadOnlySpan<string> templates)
    {
        var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var template in templates)
        {
            var path = Environment.ExpandEnvironmentVariables(template);

            if (!path.StartsWith('%') && Directory.Exists(path))
            {
                builder.Add(PathUtilities.NormalizePath(path));
            }
        }

        return builder.ToImmutable();
    }

    [SupportedOSPlatform("windows")]
    private static ImmutableHashSet<string> FindSystemDirsWin32(string subdir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subdir);

        Debug.Assert(subdir[0] is not '/' and not '\\');

        string? pathDir = FindSysDirInPath();
        string? registryDir = FindSysDirInRegistry();

        if (pathDir is null && registryDir is null)
            return [];

        if (pathDir == registryDir)
            registryDir = null;

        char[] array = ArrayPool<char>.Shared.Rent(Math.Max(pathDir?.Length ?? 0, registryDir?.Length ?? 0) + subdir.Length + 9);

        var paths = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in (ReadOnlySpan<string?>)[pathDir, registryDir])
        {
            if (path is null)
                continue;

            Span<char> buffer = array;

            var written = SanitizePath(path, buffer);

            bool success = Path.TryJoin(buffer.Slice(0, written), subdir, buffer, out int pathLen);
            Debug.Assert(success);

            buffer[written..pathLen].Replace('\\', '/');
            paths.Add(buffer[..pathLen].ToString());

            success = Path.TryJoin(buffer.Slice(0, written), "mingw64", subdir, buffer, out pathLen);
            Debug.Assert(success);

            buffer[written..pathLen].Replace('\\', '/');
            paths.Add(buffer[..pathLen].ToString());

            success = Path.TryJoin(buffer.Slice(0, written), "mingw32", subdir, buffer, out pathLen);
            Debug.Assert(success);

            buffer[written..pathLen].Replace('\\', '/');
            paths.Add(buffer[..pathLen].ToString());
        }

        return paths.ToImmutable();

        static int SanitizePath(ReadOnlySpan<char> inputPath, Span<char> buffer)
        {
            int written = 0;

            if (inputPath.StartsWith("//?/") || inputPath.StartsWith("\\\\?\\")) // is NT Namespace
            {
                inputPath = inputPath.Slice(4);

                if (inputPath.StartsWith("UNC\\"))
                {
                    "//".CopyTo(buffer);
                    written += 2;
                    inputPath = inputPath.Slice(4);
                }
            }

            inputPath.Replace(buffer.Slice(written), '\\', '/');

            return inputPath.Length + written;
        }

        throw new NotImplementedException();
    }

    [SupportedOSPlatform("windows")]
    private static string? FindSysDirInPath()
    {
        ReadOnlySpan<char> dir = default;
        foreach (var path in new Win32PathEnumerator(Environment.GetEnvironmentVariable("PATH")))
        {
            if (File.Exists(Path.Join(path, "git.exe"))
                || File.Exists(Path.Join(path, "git.cmd")))
            {
                dir = Path.TrimEndingDirectorySeparator(path);
                break;
            }
        }

        if (dir.IsEmpty)
            return null;

        if (Path.GetFileName(dir) is "bin" or "cmd")
            dir = Path.GetDirectoryName(dir);

        return dir.IsEmpty ? null : dir.ToString();
    }

    [SupportedOSPlatform("windows")]
    private static string? FindSysDirInRegistry()
    {
        const string REG_GITFORWINDOWS_KEY = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Git_is1";
        const string REG_GITFORWINDOWS_KEY_WOW64 = "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Git_is1";

        if (TryGet(Registry.CurrentUser.OpenSubKey(REG_GITFORWINDOWS_KEY), out string? value)
            || TryGet(Registry.CurrentUser.OpenSubKey(REG_GITFORWINDOWS_KEY_WOW64), out value)
            || TryGet(Registry.LocalMachine.OpenSubKey(REG_GITFORWINDOWS_KEY), out value)
            || TryGet(Registry.LocalMachine.OpenSubKey(REG_GITFORWINDOWS_KEY_WOW64), out value))
        {
            return Path.TrimEndingDirectorySeparator(value!);
        }

        return null;

        static bool TryGet(RegistryKey? key, out string? value)
        {
            value = null;

            if (key is null)
            {
                return false;
            }

            const string InstallLocationValueName = "InstallLocation";

            try
            {
                if (key.GetValueKind(InstallLocationValueName) == RegistryValueKind.String)
                {
                    value = (string?)key.GetValue(InstallLocationValueName);
                }
            }
            finally
            {
                key.Close();
            }

            return value is { Length: > 0 };
        }
    }

    private ref struct Win32PathEnumerator
    {
        private ReadOnlySpan<char> _path;
        private ReadOnlySpan<char> _current;

        public readonly ReadOnlySpan<char> Current => _current;

        public Win32PathEnumerator(ReadOnlySpan<char> path)
        {
            _path = path;
        }

        public bool MoveNext()
        {
            if (_path.IsEmpty)
            {
                _current = default;
                return false;
            }

            var path = _path;
            char term;

            if (path.StartsWith('"'))
            {
                term = '"';
                path = path.Slice(1);
            }
            else
            {
                term = ';';
            }

            int idx = path.IndexOf(term);

            if (idx >= 0)
            {
                _current = path.Slice(0, idx);
                path = path.Slice(idx + 1);

                _path = term == '"' && path.StartsWith(';') ? path.Slice(1) : path;
            }
            else
            {
                _current = path;
                _path = default;
            }

            return true;
        }

        public Win32PathEnumerator GetEnumerator()
        {
            return this;
        }
    }
    #endregion

    #region Linux-Specific
    [UnsupportedOSPlatform("windows")]
    private static unsafe string GetPasswdHome(uint uid)
    {
        // get_passwd_home() -> sysdir.c:287
        // ported to use Mono.Posix.NETSTANDARD package
        var x = new Passwd();

        if (Syscall.getpwuid_r(uid, x, out var result) != 0)
            throw new Git2OSException("Failed to get passwd entry!");

        if (result == null)
        {
            throw new Git2OSException("No passwd entry found for user!");
        }

        return x.pw_dir;
    }

    [DllImport("libc", EntryPoint = "getuid")]
    private static extern uint GetRealUserID();

    [DllImport("libc", EntryPoint = "geteuid")]
    private static extern uint GetEffectiveUserID();

    [DllImport("libc")]
    private static unsafe extern int getpwuid_r(uint uid, passwd* pwd, void* buffer, nuint buffer_length, passwd** result);

    private unsafe struct passwd
    {
        public byte* pw_name;
        public byte* pw_passwd;
        public uint pw_uid;
        public uint pw_gid;
        public byte* pw_gecos;
        public byte* pw_dir;
        public byte* pw_shell;
    }
    #endregion

    public enum Type
    {
        System = 0,
        Global,
        XDG,
        ProgramData,
        Template,
        Home
    }
}
