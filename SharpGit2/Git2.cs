using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace SharpGit2;

public static unsafe partial class Git2
{
    internal const string LibraryName = "";

    public static int Initialize()
    {
        return NativeApi.git_libgit2_init();
    }

    public static int Shutdown()
    {
        return NativeApi.git_libgit2_shutdown();
    }

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

#pragma warning disable IDE1006
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

    internal struct git_strarray
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
    }

    internal struct Error
    {
        public byte* Message;
        public int @class;
    }

    internal struct Commit
    {
    }

    internal struct Reference
    {
    }

    internal struct Repository
    {
    }

    internal struct Worktree
    {
    }
#pragma warning restore IDE1006
}
