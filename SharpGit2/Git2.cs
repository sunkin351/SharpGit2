﻿using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace SharpGit2;

public static unsafe partial class Git2
{
    internal const string LibraryName = "git2";

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

        Git2.Buffer buffer = default;

        var error = NativeApi.git_repository_discover(&buffer, startPath, acrossFileSystems ? 1 : 0, ceilingDirs);

        switch (error)
        {
            case GitError.OK:
                try
                {
                    return Encoding.UTF8.GetString(buffer.Ptr, checked((int)buffer.Size));
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

    public static RepositoryHandle InitRepository(string path)
    {
        return InitRepository(path, false);
    }

    public static RepositoryHandle InitRepository(string path, bool isBare)
    {
        RepositoryHandle handle;
        Git2.ThrowIfError(NativeApi.git_repository_init(&handle, path, isBare ? 1u : 0u));

        return handle;
    }

    public static RepositoryHandle InitRepository(string path, RepositoryInitOptions options)
    {
        RepositoryHandle handle;
        GitError error;

        fixed (RepositoryInitOptions.Unmanaged* pOptions = &options._structure)
        {
            error = NativeApi.git_repository_init_ext(&handle, path, pOptions);
        }

        Git2.ThrowIfError(error);

        return handle;
    }

    public static RepositoryHandle OpenRepository(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        RepositoryHandle repository;
        Git2.ThrowIfError(NativeApi.git_repository_open(&repository, path));

        return repository;
    }

    public static RepositoryHandle OpenRepository(string? path, RepositoryOpenFlags flags, string? ceiling_dirs)
    {
        if ((flags & RepositoryOpenFlags.FromEnvironment) == 0)
            ArgumentException.ThrowIfNullOrEmpty(path);

        RepositoryHandle repository;
        Git2.ThrowIfError(NativeApi.git_repository_open_ext(&repository, path, flags, ceiling_dirs));

        return repository;
    }

    public static RepositoryHandle OpenBareRepository(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        RepositoryHandle repository;
        Git2.ThrowIfError(NativeApi.git_repository_open_bare(&repository, path));

        return repository;
    }

    public static RepositoryHandle OpenRepositoryFromWorktree(WorkTreeHandle worktree)
    {
        ArgumentNullException.ThrowIfNull(worktree.NativeHandle);

        RepositoryHandle repository;
        Git2.ThrowIfError(NativeApi.git_repository_open_from_worktree(&repository, worktree.NativeHandle));

        return repository;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Exception ExceptionForError(GitError error, string? message = null)
    {
        if (message is null)
        {
            var err = NativeApi.git_error_last();

            Debug.Assert(err != null);
            Debug.Assert(err->@class == (int)error);

            message = Utf8StringMarshaller.ConvertToManaged(err->Message)!;
        }

        return new Git2Exception(error, message);
    }

    internal static void ThrowError(GitError code)
    {
        throw ExceptionForError(code);
    }

    internal static void ThrowIfError(GitError code)
    {
        if (code != GitError.OK)
            ThrowError(code);
    }

    internal static bool ErrorOrBoolean(int code)
    {
        if (code < 0)
            ThrowError((GitError)code);

        return code != 0;
    }

    internal static void MarshalToStructure(ref byte* pointer, ref byte[]? pinnedMemory, ref string? storedValue, string? value)
    {
        Debug.Assert((pointer is null) == (pinnedMemory is null));
        Debug.Assert((pointer is null) == (storedValue is null));

        if (storedValue == value)
            return;

        if (value is null)
        {
            pointer = null;
            pinnedMemory = null;
            storedValue = null;
            return;
        }

        Span<byte> buffer = pinnedMemory;
        if (!buffer.IsEmpty)
        {
            Debug.Assert(pointer != null);
            Debug.Assert(Unsafe.AreSame(ref Unsafe.AsRef<byte>(pointer), ref MemoryMarshal.GetReference(buffer)), "pointer does not reference the given pinned array!");

            if (Encoding.UTF8.TryGetBytes(value, buffer[..^1], out int written))
            {
                buffer[written] = 0;
                storedValue = value;
                return;
            }
        }

        int byteCount = Encoding.UTF8.GetByteCount(value) + 1;

        byte[] memory = GC.AllocateArray<byte>(byteCount, pinned: true);
        
        Encoding.UTF8.GetBytes(value, memory.AsSpan(0, byteCount - 1));

        storedValue = value;
        pinnedMemory = memory;
        // Array is allocated on the pinned object heap, so this is safe (because the array is guarenteed to never move)
        pointer = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(memory));
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

        public readonly ReadOnlySpan<CommitHandle> Span => new(commits, checked((int)count));

        public void Dispose()
        {
            if (commits == null)
                return;

            var copy = this;

            NativeApi.git_commitarray_dispose(&copy);

            this = default;
        }
    }

    internal struct StringArray
    {
        public byte** strings;
        public nuint count;

        public readonly string[] ToManaged()
        {
            if (count > int.MaxValue)
                throw new InvalidOperationException("Too many strings in native string array!");

            var span = new ReadOnlySpan<nint>(strings, (int)count);

            var managedArray = new string[span.Length];

            for (int i = 0; i < span.Length; ++i)
            {
                managedArray[i] = Utf8StringMarshaller.ConvertToManaged((byte*)span[i])!;
            }

            return managedArray;
        }
    }

    internal struct Buffer
    {
        /// <summary>
        /// The buffer contents.  `ptr` points to the start of the buffer
	    /// being returned.The buffer's length (in bytes) is specified
	    /// by the `size` member of the structure, and contains a NUL
	    /// terminator at position `(size + 1)`.
        /// </summary>
        public byte* Ptr;
        /// <summary>
        /// This field is reserved and unused.
        /// </summary>
        private nuint Reserved;
        /// <summary>
        /// The length (in bytes) of the buffer pointed to by `ptr`,
	    /// not including a NUL terminator.
        /// </summary>
        public nuint Size;

        public readonly ReadOnlySpan<byte> Span => new(Ptr, checked((int)Size));
    }

    internal struct Error
    {
        public byte* Message;
        public int @class;
    }

    internal struct Commit
    {
    }

    internal struct Config
    {
    }

    internal struct Index
    {
    }

    internal struct Iterator
    {
    }

    internal struct Object
    {
    }

    internal struct ObjectDatabase
    {
    }

    internal struct Reference
    {
    }

    internal struct ReferenceDatabase
    {
    }

    internal struct Repository
    {
    }

    internal struct Worktree
    {
    }
}
