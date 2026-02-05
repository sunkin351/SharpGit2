using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Text;

using Mono.Unix;
using Mono.Unix.Native;

using TerraFX.Interop.Windows;

namespace SharpGit2.Managed.Internal;

/// <summary>
/// These are OS helpers that will be optimized in the future.
/// </summary>
internal static partial class FileSystemHelpers
{
    public const UnixFileMode AllPerms = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
        | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
        | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

    public static void CreateDirectory(string path, UnixFileMode mode)
    {
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(path); // Ignore file mode on windows, instead of throwing an exception
        }
        else
        {
            Directory.CreateDirectory(path, mode);
        }
    }

    public static unsafe bool PathExists(ReadOnlySpan<char> path)
    {
        return Path.Exists(GitPath.PathPool.GetOrAdd(path));

        //if (OperatingSystem.IsWindows())
        //{
        //    int bufferLen = checked(path.Length + 1);

        //    char[]? array = null;
        //    try
        //    {
        //        Span<char> buffer = bufferLen <= 256 ? stackalloc char[256] : (array = ArrayPool<char>.Shared.Rent(bufferLen));

        //        path.CopyTo(buffer);
        //        buffer[path.Length] = '\0';

        //        bool success;
        //        fixed (char* pBuffer = buffer)
        //            success = Windows.PathFileExistsW(pBuffer);

        //        if (success)
        //            return true;

        //        uint error = Windows.GetLastError();

        //        if (error is ERROR.ERROR_PATH_NOT_FOUND or ERROR.ERROR_FILE_NOT_FOUND)
        //            return false;

        //        Marshal.SetLastPInvokeError((int)error);

        //        throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())!;
        //    }
        //    finally
        //    {
        //        if (array != null)
        //            ArrayPool<char>.Shared.Return(array);
        //    }
        //}
        //else
        //{

        //}
    }

    private const bool falseOnError = false;

    public static unsafe bool PathExists(ReadOnlySpan<char> path, out FileAttributes attributes, out UnixFileMode mode)
    {
        if (OperatingSystem.IsWindows())
        {
            fixed (char* pPath = path)
            {
                uint result = Windows.GetFileAttributesW(pPath);

                if (result != Windows.INVALID_FILE_ATTRIBUTES)
                {
                    attributes = (FileAttributes)result;
                    mode = ((FileAttributes)result & FileAttributes.ReadOnly) != 0 ? (UnixFileMode)0x16D /*All perms RX*/ : (UnixFileMode)0x1ff /*All perms RWX*/;
                    return true;
                }
            }

            if (falseOnError || Marshal.GetLastWin32Error() is ERROR.ERROR_FILE_NOT_FOUND or ERROR.ERROR_PATH_NOT_FOUND)
            {
                mode = default;
                attributes = default;
                return false;
            }

            throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())!;
        }
        else
        {
            Stat x = default;

            int result = Unix.GetFileStatus_Link(path, &x);

            if (result == 0)
            {
                FileAttributes tempAttr = default;
                var unixAttrs = x.st_mode & FilePermissions.S_IFMT;

                if (unixAttrs == FilePermissions.S_IFLNK)
                {
                    tempAttr |= FileAttributes.ReparsePoint;

                    Stat y = default;
                    int err = Unix.GetFileStatus(path, &y);

                    UnixMarshal.ThrowExceptionForLastErrorIf(err);

                    unixAttrs = y.st_mode & FilePermissions.S_IFMT;
                    mode = (UnixFileMode)(y.st_mode & FilePermissions.ACCESSPERMS);
                }
                else
                {
                    mode = (UnixFileMode)(x.st_mode & FilePermissions.ACCESSPERMS);
                }

                if (unixAttrs == FilePermissions.S_IFDIR)
                {
                    tempAttr |= FileAttributes.Directory;
                }
                else
                {

                }

                attributes = tempAttr;
                return true;
            }
            else
            {
                if (!falseOnError && Syscall.GetLastError() is not Errno.ENOENT and not Errno.ENOTDIR)
                {
                    UnixMarshal.ThrowExceptionForLastError();
                }

                attributes = default;
                mode = default;
                return false;
            }
        }
    }

    [ThreadStatic]
    private static FileStreamOptions? _temporaryFileOptions;

    public static FileStream CreateTemporary(string filename, UnixFileMode mode, out string path, FileShare share = FileShare.None, FileOptions options = FileOptions.None)
    {
        var streamOptions = _temporaryFileOptions ??= new FileStreamOptions()
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.ReadWrite
        };

        streamOptions.Share = share;
        streamOptions.Options = options;

        if (!OperatingSystem.IsWindows())
            streamOptions.UnixCreateMode = mode;

        for (int tries = 32; tries > 0; --tries)
        {
            var rand = (ulong)Random.Shared.NextInt64();

            var pathtmp = $"{filename}_git2_{rand:X}";
            path = pathtmp;

            try
            {
                return new FileStream(pathtmp, streamOptions);
            }
            catch (IOException e)
            {
                if (e is DirectoryNotFoundException)
                    throw;
            }
        }

        throw new Git2OSException("Failed to create temporary file!");
    }

    private static unsafe class Win32
    {
        internal static void CreateDirectoryW(ReadOnlySpan<char> path)
        {
            int bufferLen = checked(path.Length + 1);

            char[]? array = null;
            try
            {
                Span<char> buffer = bufferLen <= 256 ? stackalloc char[256] : (array = ArrayPool<char>.Shared.Rent(bufferLen));

                path.CopyTo(buffer);
                buffer[path.Length] = '\0';

                bool success;
                fixed (char* pBuffer = buffer)
                    success = Windows.CreateDirectoryW(pBuffer, null);

                if (!success)
                {
                    throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())!;
                }
            }
            finally
            {
                if (array != null)
                    ArrayPool<char>.Shared.Return(array);
            }
        }
    }

    [UnsupportedOSPlatform("windows")]
    private static unsafe partial class Unix
    {
        [LibraryImport("libc", EntryPoint = "mkdir", SetLastError = true)]
        internal static partial int MakeDirectory([MarshalUsing(typeof(UnixPathMarshaller))] ReadOnlySpan<char> path, FilePermissions mode);

        [LibraryImport("libc", EntryPoint = "stat", SetLastError = true)]
        internal static partial int GetFileStatus([MarshalUsing(typeof(UnixPathMarshaller))] ReadOnlySpan<char> path, Stat* stat);

        [LibraryImport("libc", EntryPoint = "lstat", SetLastError = true)]
        internal static partial int GetFileStatus_Link([MarshalUsing(typeof(UnixPathMarshaller))] ReadOnlySpan<char> path, Stat* stat);

        [LibraryImport("libc", EntryPoint = "geteuid", SetLastError = true)]
        internal static partial uint GetEffectiveUserID();

        [CustomMarshaller(typeof(ReadOnlySpan<char>), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
        private static unsafe class UnixPathMarshaller
        {
            public ref struct ManagedToUnmanagedIn
            {
                public static int BufferSize => 0x100;

                private byte* _unmanagedValue;
                private bool _allocated;

                /// <summary>
                /// Initializes the marshaller with a managed string and requested buffer.
                /// </summary>
                /// <param name="managed">The managed string with which to initialize the marshaller.</param>
                /// <param name="buffer">The request buffer whose size is at least <see cref="BufferSize"/>.</param>
                public void FromManaged(ReadOnlySpan<char> managed, Span<byte> buffer)
                {
                    _allocated = false;

                    if (managed.IsEmpty)
                    {
                        _unmanagedValue = null;
                        return;
                    }

                    const int MaxUtf8BytesPerChar = 3;

                    // >= for null terminator
                    // Use the cast to long to avoid the checked operation
                    if ((long)MaxUtf8BytesPerChar * managed.Length >= buffer.Length)
                    {
                        // Calculate accurate byte count when the provided stack-allocated buffer is not sufficient
                        int exactByteCount = checked(Encoding.UTF8.GetByteCount(managed) + 1); // + 1 for null terminator
                        if (exactByteCount > buffer.Length)
                        {
                            buffer = new Span<byte>((byte*)NativeMemory.Alloc((nuint)exactByteCount), exactByteCount);
                            _allocated = true;
                        }
                    }

                    // Unsafe.AsPointer is safe since buffer must be pinned
                    _unmanagedValue = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));

                    int byteCount = Encoding.UTF8.GetBytes(managed, buffer);
                    buffer[byteCount] = 0; // null-terminate
                }

                /// <summary>
                /// Converts the current managed string to an unmanaged string.
                /// </summary>
                /// <returns>An unmanaged string.</returns>
                public byte* ToUnmanaged() => _unmanagedValue;

                /// <summary>
                /// Frees any allocated unmanaged memory.
                /// </summary>
                public void Free()
                {
                    if (_allocated)
                        NativeMemory.Free(_unmanagedValue);
                }
            }

            public static byte* ConvertToUnmanaged(ReadOnlySpan<char> managed)
            {
                if (managed.IsEmpty)
                    return null;

                int exactByteCount = checked(Encoding.UTF8.GetByteCount(managed) + 1); // + 1 for null terminator
                byte* mem = (byte*)Marshal.AllocCoTaskMem(exactByteCount);
                Span<byte> buffer = new(mem, exactByteCount);

                int byteCount = Encoding.UTF8.GetBytes(managed, buffer);
                buffer[byteCount] = 0; // null-terminate
                return mem;
            }

            public static void Free(byte* unmanaged)
            {
                Marshal.FreeCoTaskMem((nint)unmanaged);
            }
        }
    }
}
