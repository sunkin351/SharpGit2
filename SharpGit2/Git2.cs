using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

    /// <summary>
    /// Minimum length (in number of hex characters, i.e. packets of 4 bits) of an oid prefix
    /// </summary>
    public static int ObjectIDMinPrefixLength => 4; // This is a property to allow future updates without having consumers need to recompile their code.

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

    public static string? GlobalConfigFile { get; } = GetPathFromFunction(&NativeApi.git_config_find_global, nameof(GlobalConfigFile));
    public static string? ProgramDataConfigFile { get; } = GetPathFromFunction(&NativeApi.git_config_find_programdata, nameof(ProgramDataConfigFile));
    public static string? SystemConfigFile { get; } = GetPathFromFunction(&NativeApi.git_config_find_system, nameof(SystemConfigFile));
    public static string? XDGConfigFile { get; } = GetPathFromFunction(&NativeApi.git_config_find_xdg, nameof(XDGConfigFile));

    private static string? GetPathFromFunction(delegate* managed<Native.GitBuffer*, GitError> func, string propertyName)
    {
        Native.GitBuffer buffer = default;
        var error = func(&buffer);

        switch (error)
        {
            case GitError.OK:
                try
                {
                    return buffer.AsString();
                }
                finally
                {
                    NativeApi.git_buf_dispose(&buffer);
                }
            case GitError.NotFound:
                return null;
            default:
                throw ExceptionForError(error, propertyName);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Exception ExceptionForError(GitError error, [CallerMemberName] string? callerName = null)
    {
        var err = NativeApi.git_error_last();

        Debug.Assert(err != null);

        GitErrorClass @class = err->Class;
        var message = Utf8StringMarshaller.ConvertToManaged(err->Message)!;

        return error switch
        {
            GitError.NotSupported => new NotSupportedException($"{callerName}: {message ?? "LibGit2 has returned that it does not support this operation!"}"),
            _ => new Git2Exception(error, @class, $"{callerName}: {message ?? "LibGit2 has returned an error!"}"),
        };
    }

    internal static void ThrowError(GitError code, [CallerMemberName] string? callerName = null)
    {
        throw ExceptionForError(code, callerName);
    }

    internal static void ThrowIfError(GitError code, [CallerMemberName] string? callerName = null)
    {
        if (code < 0)
            ThrowError(code, callerName);
    }

    internal static bool ErrorOrBoolean(int code, [CallerMemberName] string? callerName = null)
    {
        if (code < 0)
            ThrowError((GitError)code, callerName);

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

        public readonly ReadOnlySpan<GitCommit> Span
        {
            get
            {
                Debug.Assert(sizeof(GitCommit) == sizeof(Git2.Commit*));

                return new(commits, checked((int)count));
            }
        }

        public readonly nuint Count => count;

        public readonly GitCommit this[nuint i]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(i, this.Count);

                return new(commits[i]);
            }
        }

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

    internal ref struct CallbackContext<TCallback> where TCallback : class
    {
        public required TCallback Callback { get; init; }

        internal ExceptionDispatchInfo? ExceptionInfo { get; set; }

        [SetsRequiredMembers]
        public CallbackContext(TCallback callback)
        {
            Callback = callback;
        }
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
