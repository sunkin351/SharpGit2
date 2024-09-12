using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

[assembly: InternalsVisibleTo("SharpGit2.Tests")]
[assembly: DisableRuntimeMarshalling]

namespace SharpGit2;

public static unsafe partial class Git2
{
    internal const string LibraryName = "git2";

    internal const GitError ForEachBreak = (GitError)1;
    internal const GitError ForEachException = GitError.User;

    public static Version NativeLibraryVersion { get; } = GetVersion();

    private static Version GetVersion()
    {
        int major, minor, rev;
        Git2.ThrowIfError(NativeApi.git_libgit2_version(&major, &minor, &rev));

        return new Version(major, minor, rev);
    }

    public static GitFeatures NativeLibraryFeatures { get; } = NativeApi.git_libgit2_features();

    public static string PathListSeparator
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return ";";
            }
            else
            {
                return ":";
            }
        }
    }

    ///<inheritdoc cref="Discover(string, bool, string?)"/>
    public static string? Discover(string startPath)
    {
        return Discover(startPath, true, null);
    }

    ///<inheritdoc cref="Discover(string, bool, string?)"/>
    public static string? Discover(string startPath, bool acrossFileSystems)
    {
        return Discover(startPath, acrossFileSystems, null);
    }

    /// <summary>
    /// Look for a Git repository and return it's path as a string.
    /// Starts searching from <paramref name="startPath"/> and if
    /// it doesn't find a repository, searches each parent directory
    /// until it finds one.
    /// </summary>
    /// <param name="startPath">The base path where the lookup starts</param>
    /// <param name="acrossFileSystems">
    /// If true, then the lookup will not stop when a filesystem device change is detected while exploring parent directories.
    /// </param>
    /// <param name="ceilingDirs">
    /// A <see cref="Git2.PathListSeparator"/> separated list of absolute symbolic link free paths.
    /// The lookup will stop when any of this paths is reached. Note that the lookup
    /// always performs on <paramref name="startPath"/> no matter <paramref name="startPath"/>
    /// appears in <paramref name="ceilingDirs"/>. <paramref name="ceilingDirs"/> might be
    /// <see langword="null"/> (which is equivalent to an empty string).
    /// </param>
    /// <returns>
    /// Returns the found git repository path, or <see langword="null"/> is one isn't found.
    /// </returns>
    public static string? Discover(string startPath, bool acrossFileSystems, string? ceilingDirs)
    {
        ArgumentException.ThrowIfNullOrEmpty(startPath);

        Native.GitBuffer buffer = default;

        var error = NativeApi.git_repository_discover(&buffer, startPath, acrossFileSystems ? 1 : 0, ceilingDirs);

        switch (error)
        {
            case GitError.OK:
                try
                {
                    return Encoding.UTF8.GetString(buffer.Pointer, checked((int)buffer.Size));
                }
                finally
                {
                    NativeApi.git_buf_dispose(&buffer);
                }
            case GitError.NotFound:
                return null;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public static GitRepository InitRepository(string path)
    {
        return InitRepository(path, false);
    }

    public static GitRepository InitRepository(string path, bool isBare)
    {
        GitRepository handle;
        Git2.ThrowIfError(NativeApi.git_repository_init((Git2.Repository**)&handle, path, isBare ? 1u : 0u));

        return handle;
    }

    public static GitRepository InitRepository(string path, in GitRepositoryInitOptions options)
    {
        Git2.Repository* result;
        GitError error;

        Native.GitRepositoryInitOptions nOptions = default;
        try
        {
            nOptions.FromManaged(in options);

            error = NativeApi.git_repository_init_ext(&result, path, &nOptions);
        }
        finally
        {
            nOptions.Free();
        }

        Git2.ThrowIfError(error);

        return new(result);
    }

    public static GitRepository InitRepository(string path, in Native.GitRepositoryInitOptions options)
    {
        Git2.Repository* result;
        GitError error;

        fixed (Native.GitRepositoryInitOptions* pOptions = &options)
            error = NativeApi.git_repository_init_ext(&result, path, pOptions);

        Git2.ThrowIfError(error);

        return new(result);
    }

    public static GitRepository OpenRepository(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        GitRepository repository;
        Git2.ThrowIfError(NativeApi.git_repository_open((Git2.Repository**)&repository, path));

        return repository;
    }

    public static GitRepository OpenRepository(string? path, GitRepositoryOpenFlags flags, string? ceiling_dirs)
    {
        if ((flags & GitRepositoryOpenFlags.FromEnvironment) == 0)
            ArgumentException.ThrowIfNullOrEmpty(path);

        GitRepository repository;
        Git2.ThrowIfError(NativeApi.git_repository_open_ext((Git2.Repository**)&repository, path, flags, ceiling_dirs));

        return repository;
    }

    public static GitRepository OpenBareRepository(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        GitRepository repository;
        Git2.ThrowIfError(NativeApi.git_repository_open_bare((Git2.Repository**)&repository, path));

        return repository;
    }

    public static GitRepository OpenRepositoryFromWorktree(GitWorkTree worktree)
    {
        ArgumentNullException.ThrowIfNull(worktree.NativeHandle);

        GitRepository repository;
        Git2.ThrowIfError(NativeApi.git_repository_open_from_worktree((Git2.Repository**)&repository, worktree.NativeHandle));

        return repository;
    }

    public static string? GlobalConfigFile { get; } = GetPathFromFunction(&NativeApi.git_config_find_global);
    public static string? ProgramDataConfigFile { get; } = GetPathFromFunction(&NativeApi.git_config_find_programdata);
    public static string? SystemConfigFile { get; } = GetPathFromFunction(&NativeApi.git_config_find_system);
    public static string? XDGConfigFile { get; } = GetPathFromFunction(&NativeApi.git_config_find_xdg);

    private static string? GetPathFromFunction(delegate* managed<Native.GitBuffer*, GitError> func)
    {
        Native.GitBuffer buffer = default;
        var error = func(&buffer);

        switch (error)
        {
            case GitError.OK:
                try
                {
                    return Encoding.UTF8.GetString(buffer.Pointer, checked((int)buffer.Size));
                }
                finally
                {
                    NativeApi.git_buf_dispose(&buffer);
                }
            case GitError.NotFound:
                return null;
            default:
                throw ExceptionForError(error);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Exception ExceptionForError(GitError error, string? message = null)
    {
        var err = NativeApi.git_error_last();

        Debug.Assert(err != null);

        GitErrorClass @class = err->Class;
        message = Utf8StringMarshaller.ConvertToManaged(err->Message)!;

        return error switch
        {
            GitError.NotSupported => new NotSupportedException(message ?? "LibGit2 has returned that it does not support this operation!"),
            _ => new Git2Exception(error, @class, message ?? "LibGit2 has returned an error!"),
        };
    }

    internal static void ThrowError(GitError code)
    {
        throw ExceptionForError(code);
    }

    internal static void ThrowError(GitError code, string message)
    {
        throw ExceptionForError(code, message);
    }

    internal static void ThrowIfError(GitError code)
    {
        if (code < 0)
            ThrowError(code);
    }

    internal static void ThrowIfError(GitError code, string message)
    {
        if (code < 0)
            ThrowError(code, message);
    }

    internal static bool ErrorOrBoolean(int code)
    {
        if (code < 0)
            ThrowError((GitError)code);

        return code != 0;
    }

    /// <summary>
    /// An array of commit objects returned by libgit2.
    /// </summary>
    /// <remarks>
    /// Lifetime of containing Commit objects is tied to the lifetime of the array.
    /// <br/>
    /// Do not dispose of the underlying objects, and only dispose of the array when you are done with all objects in the array.
    /// </remarks>
    public struct CommitArray : IDisposable
    {
        internal Git2.Commit** commits;
        internal nuint count;

        public readonly ReadOnlySpan<GitCommit> Span => new(commits, checked((int)count));

        public void Dispose()
        {
            if (commits == null)
                return;

            var copy = this;

            NativeApi.git_commitarray_dispose(&copy);

            this = default;
        }
    }

    #region Opaque Handles
    public struct AnnotatedCommit { }
    public struct Blame { }
    public struct Blob { }
    public struct BranchIterator { }
    public struct Certificate { }
    public struct Commit { }
    public struct CommitGraph { }
    public struct Config { }
    public struct ConfigBackend { }
    public struct ConfigIterator { }
    public struct Credential { }
    public struct DescribeResult { }
    public struct Diff { }
    public struct DiffStats { }
    public struct FilterList { }
    public struct Index { }
    public struct Indexer { }
    public struct IndexConflictIterator { }
    public struct IndexIterator { }
    public struct MailMap { }
    public struct Note { }
    public struct NoteIterator { }
    public struct Object { }
    public struct ObjectDatabase { }
    public struct ObjectDatabaseBackend { }
    public struct ObjectDatabaseObject { }
    public struct PackBuilder { }
    public struct Patch { }
    public struct PathSpec { }
    public struct PathSpecMatchList { }
    public struct Rebase { }
    public struct RefDB { }
    public struct Reference { }
    public struct ReferenceIterator { }
    public struct ReferenceDatabase { }
    public struct RefLog { }
    public struct RefLogEntry { }
    public struct RefSpec { }
    public struct Remote { }
    public struct Repository { }
    public struct RevWalk { }
    public struct StatusList { }
    public struct Submodule { }
    public struct Tag { }
    public struct Transaction { }
    public struct Transport { }
    public struct Tree { }
    public struct TreeBuilder { }
    public struct TreeEntry { }
    public struct Worktree { }

    #endregion

    internal class CallbackContext<TDelegate> where TDelegate : Delegate
    {
        public required TDelegate Callback { get; init; }

        internal ExceptionDispatchInfo? ExceptionInfo { get; set; }
    }

    public readonly record struct Version : IComparable<Version>, ISpanFormattable
    {
        public int Major { get; }
        public int Minor { get; }
        public int Revision { get; }

        public Version(int major, int minor, int revision)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(major);
            ArgumentOutOfRangeException.ThrowIfNegative(minor);
            ArgumentOutOfRangeException.ThrowIfNegative(revision);

            Major = major;
            Minor = minor;
            Revision = revision;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Version other)
        {
            if (Major != other.Major)
                return Major.CompareTo(other.Major);

            if (Minor != other.Minor)
                return Minor.CompareTo(other.Minor);

            return Revision.CompareTo(other.Revision);
        }

        public static bool operator <(Version a, Version b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator >(Version a, Version b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator <=(Version a, Version b)
        {
            return a.CompareTo(b) <= 0;
        }

        public static bool operator >=(Version a, Version b)
        {
            return a.CompareTo(b) >= 0;
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return destination.TryWrite($"v{Major}.{Minor}.{Revision}", out charsWritten);
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return $"v{Major}.{Minor}.{Revision}";
        }

        public override string ToString()
        {
            return ToString(null, null);
        }
    }

    /// <summary>
    /// Unmanaged string allocated on the pinned object heap
    /// </summary>
    internal struct UnmanagedString
    {
        public string? Value { get; private set; }
        private byte[]? _memory;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="initialCapacity">UTF8 byte count</param>
        public UnmanagedString(int initialCapacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCapacity);

            _memory = GC.AllocateUninitializedArray<byte>(initialCapacity, pinned: true);
            _memory[0] = 0;
        }

        public readonly byte* NativePointer
        {
            get
            {
                if (Value is null)
                    return null;

                return (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(_memory!));
            }
        }

        /// <summary>
        /// Sets the value of the unmanaged string.
        /// </summary>
        /// <param name="value">The new value</param>
        /// <returns><see langword="true"/> if the NativePointer property changed, <see langword="false"/> if not.</returns>
        public bool SetValue(string? value, bool reusableAllocation)
        {
            string? currentValue = this.Value;
            if (currentValue == value)
                return false;

            if (string.IsNullOrEmpty(value))
            {
                Value = null;
                return true;
            }

            const int MaxUtf8BytesPerChar = 3;

            Span<byte> buffer = _memory;
            bool allocationChanged = currentValue is null;

            if (value.Length * MaxUtf8BytesPerChar >= buffer.Length)
            {
                int byteCount = checked(Encoding.UTF8.GetByteCount(value) + 1);

                if (byteCount > buffer.Length)
                {
                    if (reusableAllocation)
                    {
                        // Saturates down to int.MaxValue if ever exceeded
                        byteCount = int.CreateSaturating(BitOperations.RoundUpToPowerOf2((uint)byteCount));
                    }

                    buffer = (_memory = GC.AllocateUninitializedArray<byte>(byteCount, pinned: true));
                    allocationChanged = true;
                }
            }

            int written = Encoding.UTF8.GetBytes(value, buffer[..^1]);
            buffer[written] = 0;

            Value = value;
            return allocationChanged;
        }
    }
}
