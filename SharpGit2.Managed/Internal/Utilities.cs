using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using BitFaster.Caching.Lfu;
using CommunityToolkit.HighPerformance.Buffers;
using SharpGit2.Managed.Internal;

using TerraFX.Interop.Windows;

namespace SharpGit2.Managed;

internal static partial class Utilities
{
    #region Percent Decode
    private static readonly Regex _percentRegex = PercentRegex();

    [return: NotNullIfNotNull(nameof(input))]
    public static string? PercentDecode(string? input)
    {
        if (string.IsNullOrEmpty(input) || !_percentRegex.IsMatch(input))
            return input;

        return PercentDecode(input.AsSpan());
    }

    public static string PercentDecode(ReadOnlySpan<char> input)
    {
        var builder = new StringBuilder(input.Length);

        int lastEnd = 0;
        foreach (var match in _percentRegex.EnumerateMatches(input))
        {
            builder.Append(input.Slice(lastEnd, match.Index - lastEnd));
            lastEnd = match.Index + match.Length;

            int character = Decode(input[match.Index + 1]) << 4;
            character |= Decode(input[match.Index + 2]);

            if ((uint)character >= 128)
                throw new InvalidDataException("Unable to handle non-ascii percent characters!");

            builder.Append((char)character);
        }

        if ((uint)lastEnd < (uint)input.Length)
            builder.Append(input.Slice(lastEnd));

        return builder.ToString();

        static int Decode(char c)
        {
            return char.IsBetween(c, '0', '9') ? c - '0' : 10 + ((c | 0x20) - 'a'); // regex guarentees the character will be within these ranges
        }
    }

    [GeneratedRegex("\\%[0-9a-fA-F]{2}", RegexOptions.None)]
    private static partial Regex PercentRegex();
    #endregion

    /// <summary>
    /// Mimics the behavior of git__strntol64() for compatibility.
    /// 
    /// Biggest difference to what we have in long.TryParse() is that this doesn't error out
    /// with trying to consume the whole input, and returns how much of the input it consumed.
    /// 
    /// Another key difference is that this takes a number base for the expected input.
    /// </summary>
    /// <returns></returns>
    public static bool TryParseLong(ReadOnlySpan<char> input, out long value, out int consumed, int numberBase = 0)
    {
        int p = 0;
        long n = 0, nn;

        for (; (uint)p < (uint)input.Length; p += 1)
        {
            if (!char.IsWhiteSpace(input[p]))
                break;
        }

        if ((uint)p >= (uint)input.Length)
        {
            goto fail;
        }

        bool neg = false;
        if (input[p] is '+' or '-')
        {
            if (input[p] == '-')
                neg = true;

            p += 1;

            if ((uint)p >= (uint)input.Length)
            {
                goto fail;
            }
        }

        if ((uint)numberBase > 36)
            goto fail;

        if (numberBase == 0)
        {
            numberBase = input.Slice(p) switch
            {
                not ['0', ..] => 10,
                ['0', 'x' or 'X', ..] => 16,
                _ => 8,
            };
        }

        if (numberBase == 16 && input.Slice(p).StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        { 
            p += 2;

            if ((uint)p >= (uint)input.Length)
                goto fail;
        }

        long result = 0;
        int digits = 0;

        for (; (uint)p < (uint)input.Length; ++p)
        {
            char c = input[p];

            int intermediate = -1;
            if (char.IsBetween(c, '0', '9'))
            {
                intermediate = c - '0';
            }
            else if (char.IsBetween(c, 'a', 'z'))
            {
                intermediate = c - 'a' + 10;
            }
            else if (char.IsBetween(c, 'A', 'Z'))
            {
                intermediate = c - 'A' + 10;
            }

            if ((uint)intermediate >= (uint)numberBase)
                break;

            result = checked(result * numberBase + intermediate);
            digits += 1;
        }

        if (digits == 0)
            goto fail;

        consumed = p;
        value = neg ? -result : result;
        return true;

    fail:
        value = default;
        consumed = 0;
        return false;
    }

    extension(File)
    {
        public static void ReadAllText(string filePath, IBufferWriter<char> buffer)
        {
            ArgumentNullException.ThrowIfNull(filePath);
            ArgumentNullException.ThrowIfNull(buffer);

            using var reader = new StreamReader(filePath);

            while (true)
            {
                var span = buffer.GetSpan();
                int read = reader.Read(span);

                if (read == 0)
                    break;

                buffer.Advance(read);
            }
        }

        public static void ReadAllText(string filePath, Encoding encoding, IBufferWriter<char> buffer)
        {
            ArgumentNullException.ThrowIfNull(filePath);
            ArgumentNullException.ThrowIfNull(buffer);

            using var reader = new StreamReader(filePath, encoding);

            while (true)
            {
                var span = buffer.GetSpan();
                int read = reader.Read(span);

                if (read == 0)
                    break;

                buffer.Advance(read);
            }
        }
    }

    //extension(Path)
    //{
    //    public static void GetFullPath(ReadOnlySpan<char> path, IBufferWriter<char> buffer)
    //    {
    //        if (path.IsEmpty)
    //            throw new ArgumentException("Cannot normalize empty path!", nameof(path));

    //        if (path.Contains('\0'))
    //            throw new ArgumentException("Null character in path!", nameof(path));


    //    }

    //    public static void GetFullPath(ReadOnlySpan<char> path, ReadOnlySpan<char> basePath, IBufferWriter<char> buffer)
    //    {
    //        if (!Path.IsPathFullyQualified(basePath))
    //            throw new ArgumentException("Base path is not fully qualified!", nameof(basePath));

    //        if (path.Contains('\0') || basePath.Contains('\0'))
    //            throw new ArgumentException("Null character in path!");

    //        if (Path.IsPathFullyQualified(path))
    //        {
    //            GetFullPath(path, buffer);
    //        }
    //        else
    //        {
    //            //Path.GetFullPath()
    //        }
    //    }
    //}

    extension(Interlocked)
    {
        public static void Write<T>(ref T location1, T value)
            where T : class
        {
            // Attempt to guarentee the write is immediately visible to all observers.
            // A guarentee of Interlocked.Exchange()
            _ = Interlocked.Exchange(ref location1, value);
        }
    }

    extension(GitObjectIDType type)
    {
        public int HashSize
        {
            get => type switch
            {
                GitObjectIDType.SHA1 => SHA1.HashSizeInBytes,
#if GIT_EXPERIMENTAL_SHA256
                GitObjectIDType.SHA256 => SHA256.HashSizeInBytes,
#endif
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        public int HexSize => type.HashSize * 2;
    }

    private sealed class StringBuilderSegment : ReadOnlySequenceSegment<char>
    {
        public StringBuilderSegment(ReadOnlyMemory<char> block)
        {
            this.Memory = block;
        }

        public StringBuilderSegment Append(ReadOnlyMemory<char> block)
        {
            var segment = new StringBuilderSegment(block)
            {
                RunningIndex = this.RunningIndex + this.Memory.Length
            };

            this.Next = segment;

            return segment;
        }
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "m_ChunkChars")]
    private static extern ref char[] GetChunkBuffer(StringBuilder builder);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "m_ChunkLength")]
    private static extern ref int GetChunkLength(StringBuilder builder);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "m_ChunkPrevious")]
    private static extern ref StringBuilder? GetPreviousChunk(StringBuilder builder);

    extension(StringBuilder builder)
    {
        public bool TryGetSpan(out ReadOnlySpan<char> span)
        {
            if (GetPreviousChunk(builder) == null)
            {
                // builder data is in a contiguous state
                int length = GetChunkLength(builder);
                span = GetChunkBuffer(builder).AsSpan(0, length);
                return true;
            }

            span = default;
            return false;
        }

        public ReadOnlySequence<char> ToReadOnlySequence()
        {
            var enumerator = builder.GetChunks();

            if (!enumerator.MoveNext())
                return default;

            ReadOnlyMemory<char> firstBlock = enumerator.Current;

            if (!enumerator.MoveNext())
                return new ReadOnlySequence<char>(firstBlock);

            StringBuilderSegment? first = new(firstBlock), last = first.Append(enumerator.Current);

            while (enumerator.MoveNext())
            {
                last = last.Append(enumerator.Current);
            }

            return new ReadOnlySequence<char>(first, 0, last, last.Memory.Length);
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct SymLinkData
    {
        public ushort SubstituteNameOffset;
        public ushort SubstituteNameLength;
        public ushort PrintNameOffset;
        public ushort PrintNameLength;
        public uint Flags;
        public char PathBufferStart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct MountPointData
    {
        public ushort SubstituteNameOffset;
        public ushort SubstituteNameLength;
        public ushort PrintNameOffset;
        public ushort PrintNameLength;
        public char PathBufferStart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct ReparseDataHeader
    {
        public uint ReparseTag;
        public ushort ReparseDataLength;
        public ushort Reserved;
        public byte DataBufferStart;
    }

    [SupportedOSPlatform("windows6.1")]
    internal static unsafe bool TryWin32ReadLink(string path, [NotNullWhen(true)] out string? target)
    {
        HANDLE fileHandle;
        fixed (char* pPath = path)
            fileHandle = Windows.CreateFileW(pPath,
                Windows.GENERIC_READ,
                FILE.FILE_SHARE_READ | FILE.FILE_SHARE_DELETE,
                null,
                OPEN.OPEN_EXISTING,
                FILE.FILE_FLAG_OPEN_REPARSE_POINT | FILE.FILE_FLAG_BACKUP_SEMANTICS,
                default);

        if (fileHandle == Windows.INVALID_HANDLE_VALUE)
        {
            goto Fail;
        }

        try
        {
            const int reparseBufferSize = MAXIMUM.MAXIMUM_REPARSE_DATA_BUFFER_SIZE;
            byte* reparseBuffer = stackalloc byte[reparseBufferSize];

            uint written = 0;
            if (!Windows.DeviceIoControl(fileHandle, FSCTL.FSCTL_GET_REPARSE_POINT, null, 0, reparseBuffer, reparseBufferSize, &written, null))
            {
                goto Fail;
            }

            var reparsePtr = (ReparseDataHeader*)reparseBuffer;
            ReadOnlySpan<char> targetSpan;

            switch (reparsePtr->ReparseTag)
            {
                case IO.IO_REPARSE_TAG_SYMLINK:
                    var symlinkData = (SymLinkData*)&reparsePtr->DataBufferStart;

                    targetSpan = MemoryMarshal.CreateReadOnlySpan(
                        ref Unsafe.Add(ref symlinkData->PathBufferStart, symlinkData->SubstituteNameOffset / sizeof(char)),
                        symlinkData->SubstituteNameLength / sizeof(char));
                    break;
                case IO.IO_REPARSE_TAG_MOUNT_POINT:
                    var mountPointData = (MountPointData*)&reparsePtr->DataBufferStart;

                    targetSpan = MemoryMarshal.CreateReadOnlySpan(
                        ref Unsafe.Add(ref mountPointData->PathBufferStart, mountPointData->SubstituteNameOffset / sizeof(char)),
                        mountPointData->SubstituteNameLength / sizeof(char));
                    break;
                default:
                    goto Fail;
            }

            if (targetSpan.StartsWith("\\??\\Volume{")) // if path is volume
            {
                goto Fail;
            }

            target = GitPath.TrimNamespace(targetSpan).ToString();
            return true;
        }
        finally
        {
            Windows.CloseHandle(fileHandle);
        }

    Fail:
        target = null;
        return false;
    }

    [SupportedOSPlatform("windows6.1")]
    internal static unsafe string Win32ReadLink(string path)
    {
        HANDLE fileHandle;
        fixed (char* pPath = path)
            fileHandle = Windows.CreateFileW(pPath,
                Windows.GENERIC_READ,
                FILE.FILE_SHARE_READ | FILE.FILE_SHARE_DELETE,
                null,
                OPEN.OPEN_EXISTING,
                FILE.FILE_FLAG_OPEN_REPARSE_POINT | FILE.FILE_FLAG_BACKUP_SEMANTICS,
                default);

        if (fileHandle == Windows.INVALID_HANDLE_VALUE)
        {
            throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())!;
        }

        try
        {
            const int reparseBufferSize = MAXIMUM.MAXIMUM_REPARSE_DATA_BUFFER_SIZE;
            byte* reparseBuffer = stackalloc byte[reparseBufferSize];

            uint written = 0;
            if (!Windows.DeviceIoControl(fileHandle, FSCTL.FSCTL_GET_REPARSE_POINT, null, 0, reparseBuffer, reparseBufferSize, &written, null))
            {
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())!;
            }

            var reparsePtr = (ReparseDataHeader*)reparseBuffer;
            ReadOnlySpan<char> target;

            switch (reparsePtr->ReparseTag)
            {
                case IO.IO_REPARSE_TAG_SYMLINK:
                    var symlinkData = (SymLinkData*)&reparsePtr->DataBufferStart;

                    target = MemoryMarshal.CreateReadOnlySpan(
                        ref Unsafe.Add(ref symlinkData->PathBufferStart, symlinkData->SubstituteNameOffset / sizeof(char)),
                        symlinkData->SubstituteNameLength / sizeof(char));
                    break;
                case IO.IO_REPARSE_TAG_MOUNT_POINT:
                    var mountPointData = (MountPointData*)&reparsePtr->DataBufferStart;

                    target = MemoryMarshal.CreateReadOnlySpan(
                        ref Unsafe.Add(ref mountPointData->PathBufferStart, mountPointData->SubstituteNameOffset / sizeof(char)),
                        mountPointData->SubstituteNameLength / sizeof(char));
                    break;
                default:
                    throw new IOException("Reparse data is not a link!");
            }

            if (target.StartsWith("\\??\\Volume{")) // if path is volume
            {
                throw new IOException("Reparse data is not a link!");
            }

            target = GitPath.TrimNamespace(target);

            return target.ToString();
        }
        finally
        {
            Windows.CloseHandle(fileHandle);
        }
    }

    internal static void InitToIndexes(this Span<int> span)
    {
        if (Vector128.IsHardwareAccelerated && span.Length >= Vector128<int>.Count)
        {
            ref int @ref = ref MemoryMarshal.GetReference(span);

            if (Vector.IsHardwareAccelerated && Vector<int>.Count > Vector128<int>.Count && span.Length >= Vector<int>.Count)
            {
                ref int lenMinusOne = ref Unsafe.Add(ref @ref, span.Length - Vector<int>.Count);
                Vector<int> value = Vector<int>.Indices;

                while (Unsafe.IsAddressLessThan(ref @ref, ref lenMinusOne))
                {
                    value.StoreUnsafe(ref @ref);

                    value += Vector.Create(Vector<int>.Count);
                    @ref = ref Unsafe.Add(ref @ref, Vector<int>.Count);
                }

                value = Vector.Create(span.Length - Vector<int>.Count) + Vector<int>.Indices;
                value.StoreUnsafe(ref lenMinusOne);
            }
            else
            {
                ref int lenMinusOne = ref Unsafe.Add(ref @ref, span.Length - Vector128<int>.Count);
                Vector128<int> value = Vector128<int>.Indices;

                while (Unsafe.IsAddressLessThan(ref @ref, ref lenMinusOne))
                {
                    value.StoreUnsafe(ref @ref);

                    value += Vector128.Create(Vector128<int>.Count);
                    @ref = ref Unsafe.Add(ref @ref, Vector128<int>.Count);
                }

                value = Vector128.Create(span.Length - Vector128<int>.Count) + Vector128<int>.Indices;
                value.StoreUnsafe(ref lenMinusOne);
            }
        }
        else
        {
            for (int i = 0; i < span.Length; ++i)
            {
                span[i] = i;
            }
        }
    }

    internal static int BinarySearch<T, TComparable>(this List<T> list, TComparable value)
        where TComparable: IComparable<T>
    {
        return CollectionsMarshal.AsSpan(list).BinarySearch(value);
    }

    internal static int BinarySearch<T>(this List<T> list, T value, Comparison<T> comparison)
    {
        return list.BinarySearch(new BinarySearchComparisonComparable<T>(value, comparison));
    }

    private struct BinarySearchComparisonComparable<T>(T value, Comparison<T> comparison) : IComparable<T>
    {
        public readonly int CompareTo(T other)
        {
            return comparison(value, other);
        }
    }

    [InlineArray(SHA256.HashSizeInBytes)]
    internal struct SHA256HashField
    {
        public byte _element;
    }

    internal static readonly ConcurrentLfu<string, Regex> RegexCache = new(256);

    internal static Regex GetCachedRegex(string regexPattern)
    {
        return RegexCache.GetOrAdd(regexPattern, x => new Regex(x));
    }
    
    private static readonly StringPool _stringPool = new StringPool();

    internal static string GetPooledString(ReadOnlySpan<char> text) => _stringPool.GetOrAdd(text);
    internal static string GetPooledString(string text) => _stringPool.GetOrAdd(text);
}
