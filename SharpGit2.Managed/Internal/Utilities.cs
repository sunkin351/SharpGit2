using System.Buffers;
using System.Collections.Immutable;
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

    extension(IOException e)
    {
        public bool FileAlreadyExists
        {
            get
            {
                if (OperatingSystem.IsWindows())
                {
                    return (uint)e.HResult is 0x80070050 or 0x800700B7;
                }

                return e.HResult == 17;
            }
        }

        public bool FileInUse
        {
            get
            {
                if (OperatingSystem.IsWindows())
                {
                    return (uint)e.HResult is 0x80070020 or 0x80070021;
                }

                return e.HResult == 11;
            }
        }
    }

    extension(Interlocked)
    {
        public static void Write<T>(ref T location1, T value)
            where T : class
        {
            // Attempt to guarantee this write is immediately visible to all observers.
            // A guarantee of Interlocked.Exchange()
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

    internal static bool IsValidObjectType(GitObjectType type) =>
        type is GitObjectType.Commit or GitObjectType.Tree or GitObjectType.Blob or GitObjectType.Tag;

    public enum ByteOrderMark
    {
        None = 0,
        Utf8,
        Utf16LE,
        Utf16BE,
        Utf32LE,
        Utf32BE
    }
    
    public static ByteOrderMark DetectByteOrderMark(ReadOnlySpan<byte> data, out int byteCount)
    {
        if (data.Length < 2)
        {
            byteCount = 0;
            return ByteOrderMark.None;
        }

        switch (data[0])
        {
            case 0:
                if (data.Length >= 4 && data[1] == 0 && data[2] == 0xFE && data[3] == 0xFF)
                {
                    byteCount = 4;
                    return ByteOrderMark.Utf32BE;
                }

                break;
            case 0xEF:
                if (data.Length >= 3 && data[1] == 0xBB && data[2] == 0xBF)
                {
                    byteCount = 3;
                    return ByteOrderMark.Utf8;
                }

                break;
            case 0xFE:
                if (data[1] == 0xFF)
                {
                    byteCount = 2;
                    return ByteOrderMark.Utf16BE;
                }

                break;
            case 0xFF:
                if (data[1] == 0xFE)
                {
                    if (data.Length >= 4 && data[2] == 0 && data[3] == 0)
                    {
                        byteCount = 4;
                        return ByteOrderMark.Utf32LE;
                    }
                    else
                    {
                        byteCount = 2;
                        return ByteOrderMark.Utf16LE;
                    }
                }

                break;
        }

        byteCount = 0;
        return ByteOrderMark.None;
    }

    public struct TextStats
    {
        public ByteOrderMark Bom;
        public int Printable;
        public int Nul;
        public int Nonprintable;
        public int LF;
        public int CR;
        public int CRLF;

        public TextStats(ByteOrderMark bom, int printable, int nul, int nonprintable, int lf, int cr, int crlf)
        {
            Bom = bom;
            Printable = printable;
            Nul = nul;
            Nonprintable = nonprintable;
            LF = lf;
            CR = cr;
            CRLF = crlf;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <param name="skip_bom"></param>
    /// <param name="stats"></param>
    /// <returns>True if the data should be interpreted as binary, false if it should be treated as text.</returns>
    public static bool GatherTextStats(ReadOnlySpan<byte> data, bool skip_bom, out TextStats stats)
    {
        stats = default;

        var bom = DetectByteOrderMark(data, out int toSkip);
        if (skip_bom)
            data = data.Slice(toSkip);
        
        int printable = 0, nonprintable = 0;
        int nul = 0;
        int lf = 0, cr = 0, crlf = 0;

        if (Vector128.IsHardwareAccelerated && data.Length >= Vector128<byte>.Count)
        {
            ref byte reference = ref MemoryMarshal.GetReference(data);

            if (Vector512.IsHardwareAccelerated && data.Length >= Vector512<byte>.Count)
            {
                ref byte minusOneVec = ref Unsafe.Add(ref reference, data.Length - Vector512<byte>.Count);

                Vector512<byte> c0 = Vector512.Create((byte)0x1f),
                    c1 = Vector512.Create((byte)0x7f),
                    c2 = Vector512.Create((byte)8),
                    c3 = Vector512.Create((byte)(13 - 8)),
                    escapeConst = Vector512.Create((byte)'\e'),
                    crConst = Vector512.Create((byte)'\r'),
                    lfConst = Vector512.Create((byte)'\n');
            
                Vector512<byte> charData, printableVec, crVec, lfVec, crlfVec, zeroVec;

                while (Unsafe.IsAddressLessThan(ref reference, ref minusOneVec))
                {
                    charData = Vector512.LoadUnsafe(ref reference);

                    printableVec = Vector512.GreaterThan(charData, c0);
                    printableVec = Vector512.AndNot(printableVec, Vector512.Equals(charData, c1));

                    crVec = Vector512.Equals(charData, crConst);
                    cr += Vector512.CountWhereAllBitsSet(crVec);
                    
                    lfVec = Vector512.Equals(charData, lfConst);
                    lf += Vector512.CountWhereAllBitsSet(lfVec);

                    printableVec |= Vector512.AndNot(
                        Vector512.LessThanOrEqual(charData - c2, c3),
                        crVec | lfVec)
                        | Vector512.Equals(charData, escapeConst);
                    
                    printable += Vector512.CountWhereAllBitsSet(printableVec);

                    printableVec |= crVec | lfVec;

                    // The way the condition of this loop is coded, there will always be at least one additional byte
                    // ahead of the current read. So this should always be safe.
                    crlfVec = crVec & Vector512.Equals(lfConst, Vector512.LoadUnsafe(ref reference, 1u));
                    crlf += Vector512.CountWhereAllBitsSet(crlfVec);
                
                    zeroVec = Vector512.Equals(charData, Vector512<byte>.Zero);
                    nul += Vector512.CountWhereAllBitsSet(zeroVec);

                    nonprintable += Vector512.CountWhereAllBitsSet(~printableVec);
                
                    reference = ref Unsafe.Add(ref reference, Vector512<byte>.Count);
                }

                int remaining = data.Length % Vector512<byte>.Count;
                var resultMask = remaining == 0
                    ? Vector512<byte>.AllBitsSet
                    : Vector512.GreaterThanOrEqual(Vector512<byte>.Indices, Vector512.Create((byte)(Vector512<byte>.Count - remaining)));

                charData = Vector512.LoadUnsafe(ref minusOneVec);
            
                printableVec = Vector512.GreaterThan(charData, c0);
                printableVec = Vector512.AndNot(printableVec, Vector512.Equals(charData, c1));

                crVec = Vector512.Equals(charData, crConst);
                cr += Vector512.CountWhereAllBitsSet(crVec & resultMask);
                    
                lfVec = Vector512.Equals(charData, lfConst);
                lf += Vector512.CountWhereAllBitsSet(lfVec & resultMask);

                printableVec |= Vector512.AndNot(
                             Vector512.LessThanOrEqual(charData - c2, c3),
                             crVec | lfVec)
                         | Vector512.Equals(charData, escapeConst);
                    
                printable += Vector512.CountWhereAllBitsSet(printableVec & resultMask);

                printableVec |= crVec | lfVec;

                // shuffle the entire vector over by 1 byte, zeroing the last bytes
                crlfVec = crVec & Vector512.Shuffle(lfVec,
                        Vector512.Create(1,  2,  3,  4,  5,  6,  7,  8,  9,  10, 11, 12, 13, 14, 15, 16,
                            17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32,
                            33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
                            49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, byte.MaxValue));
                crlf += Vector512.CountWhereAllBitsSet(crlfVec & resultMask);

                zeroVec = Vector512.Equals(charData, Vector512<byte>.Zero);
                nul += Vector512.CountWhereAllBitsSet(zeroVec & resultMask);

                nonprintable += Vector512.CountWhereAllBitsSet(~printableVec & resultMask);
            }
            else if (Vector256.IsHardwareAccelerated && data.Length >= Vector256<byte>.Count)
            {
                ref byte minusOneVec = ref Unsafe.Add(ref reference, data.Length - Vector256<byte>.Count);

                Vector256<byte> c0 = Vector256.Create((byte)0x1f),
                    c1 = Vector256.Create((byte)0x7f),
                    c2 = Vector256.Create((byte)8),
                    c3 = Vector256.Create((byte)(13 - 8)),
                    escapeConst = Vector256.Create((byte)'\e'),
                    crConst = Vector256.Create((byte)'\r'),
                    lfConst = Vector256.Create((byte)'\n');
            
                Vector256<byte> charData, printableVec, crVec, lfVec, crlfVec, zeroVec;

                while (Unsafe.IsAddressLessThan(ref reference, ref minusOneVec))
                {
                    charData = Vector256.LoadUnsafe(ref reference);

                    printableVec = Vector256.GreaterThan(charData, c0);
                    printableVec = Vector256.AndNot(printableVec, Vector256.Equals(charData, c1));

                    crVec = Vector256.Equals(charData, crConst);
                    cr += Vector256.CountWhereAllBitsSet(crVec);
                    
                    lfVec = Vector256.Equals(charData, lfConst);
                    lf += Vector256.CountWhereAllBitsSet(lfVec);

                    printableVec |= Vector256.AndNot(
                        Vector256.LessThanOrEqual(charData - c2, c3),
                        crVec | lfVec)
                        | Vector256.Equals(charData, escapeConst);
                    
                    printable += Vector256.CountWhereAllBitsSet(printableVec);

                    printableVec |= crVec | lfVec;

                    // The way the condition of this loop is coded, there will always be at least one additional byte
                    // ahead of the current read. So this should always be safe.
                    crlfVec = crVec & Vector256.Equals(lfConst, Vector256.LoadUnsafe(ref reference, 1u));
                    crlf += Vector256.CountWhereAllBitsSet(crlfVec);
                
                    zeroVec = Vector256.Equals(charData, Vector256<byte>.Zero);
                    nul += Vector256.CountWhereAllBitsSet(zeroVec);

                    nonprintable += Vector256.CountWhereAllBitsSet(~printableVec);
                
                    reference = ref Unsafe.Add(ref reference, Vector256<byte>.Count);
                }

                int remaining = data.Length % Vector256<byte>.Count;
                var resultMask = remaining == 0
                    ? Vector256<byte>.AllBitsSet
                    : Vector256.GreaterThanOrEqual(Vector256<byte>.Indices, Vector256.Create((byte)(Vector256<byte>.Count - remaining)));

                charData = Vector256.LoadUnsafe(ref minusOneVec);
            
                printableVec = Vector256.GreaterThan(charData, c0);
                printableVec = Vector256.AndNot(printableVec, Vector256.Equals(charData, c1));

                crVec = Vector256.Equals(charData, crConst);
                cr += Vector256.CountWhereAllBitsSet(crVec & resultMask);
                    
                lfVec = Vector256.Equals(charData, lfConst);
                lf += Vector256.CountWhereAllBitsSet(lfVec & resultMask);

                printableVec |= Vector256.AndNot(
                             Vector256.LessThanOrEqual(charData - c2, c3),
                             crVec | lfVec)
                         | Vector256.Equals(charData, escapeConst);
                    
                printable += Vector256.CountWhereAllBitsSet(printableVec & resultMask);

                printableVec |= crVec | lfVec;

                // shuffle the entire vector over by 1 byte, zeroing the last bytes
                crlfVec = crVec & Vector256.Shuffle(lfVec,
                        Vector256.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
                            17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, byte.MaxValue));
                crlf += Vector256.CountWhereAllBitsSet(crlfVec & resultMask);

                zeroVec = Vector256.Equals(charData, Vector256<byte>.Zero);
                nul += Vector256.CountWhereAllBitsSet(zeroVec & resultMask);

                nonprintable += Vector256.CountWhereAllBitsSet(~printableVec & resultMask);
            }
            else
            {
                ref byte minusOneVec = ref Unsafe.Add(ref reference, data.Length - Vector128<byte>.Count);

                Vector128<byte> c0 = Vector128.Create((byte)0x1f),
                    c1 = Vector128.Create((byte)0x7f),
                    c2 = Vector128.Create((byte)8),
                    c3 = Vector128.Create((byte)(13 - 8)),
                    escapeConst = Vector128.Create((byte)'\e'),
                    crConst = Vector128.Create((byte)'\r'),
                    lfConst = Vector128.Create((byte)'\n');
            
                Vector128<byte> charData, printableVec, crVec, lfVec, crlfVec, zeroVec;

                while (Unsafe.IsAddressLessThan(ref reference, ref minusOneVec))
                {
                    charData = Vector128.LoadUnsafe(ref reference);

                    printableVec = Vector128.GreaterThan(charData, c0);
                    printableVec = Vector128.AndNot(printableVec, Vector128.Equals(charData, c1));

                    crVec = Vector128.Equals(charData, crConst);
                    cr += Vector128.CountWhereAllBitsSet(crVec);
                    
                    lfVec = Vector128.Equals(charData, lfConst);
                    lf += Vector128.CountWhereAllBitsSet(lfVec);

                    printableVec |= Vector128.AndNot(
                        Vector128.LessThanOrEqual(charData - c2, c3),
                        crVec | lfVec)
                        | Vector128.Equals(charData, escapeConst);
                    
                    printable += Vector128.CountWhereAllBitsSet(printableVec);

                    printableVec |= crVec | lfVec;

                    // The way the condition of this loop is coded, there will always be at least one additional byte
                    // ahead of the current read. So this should always be safe.
                    crlfVec = crVec & Vector128.Equals(lfConst, Vector128.LoadUnsafe(ref reference, 1u));
                    crlf += Vector128.CountWhereAllBitsSet(crlfVec);
                
                    zeroVec = Vector128.Equals(charData, Vector128<byte>.Zero);
                    nul += Vector128.CountWhereAllBitsSet(zeroVec);

                    nonprintable += Vector128.CountWhereAllBitsSet(~printableVec);
                
                    reference = ref Unsafe.Add(ref reference, Vector128<byte>.Count);
                }

                int remaining = data.Length % Vector128<byte>.Count;
                var resultMask = remaining == 0
                    ? Vector128<byte>.AllBitsSet
                    : Vector128.GreaterThanOrEqual(Vector128<byte>.Indices, Vector128.Create((byte)(Vector128<byte>.Count - remaining)));

                charData = Vector128.LoadUnsafe(ref minusOneVec);
            
                printableVec = Vector128.GreaterThan(charData, c0);
                printableVec = Vector128.AndNot(printableVec, Vector128.Equals(charData, c1));

                crVec = Vector128.Equals(charData, crConst);
                cr += Vector128.CountWhereAllBitsSet(crVec & resultMask);
                    
                lfVec = Vector128.Equals(charData, lfConst);
                lf += Vector128.CountWhereAllBitsSet(lfVec & resultMask);

                printableVec |= Vector128.AndNot(
                     Vector128.LessThanOrEqual(charData - c2, c3),
                     crVec | lfVec)
                    | Vector128.Equals(charData, escapeConst);
                    
                printable += Vector128.CountWhereAllBitsSet(printableVec & resultMask);

                printableVec |= crVec | lfVec;

                // shuffle the entire vector over by 1 byte, zeroing the last bytes
                crlfVec = crVec & Vector128.Shuffle(lfVec,
                        Vector128.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, byte.MaxValue));
                crlf += Vector128.CountWhereAllBitsSet(crlfVec & resultMask);

                zeroVec = Vector128.Equals(charData, Vector128<byte>.Zero);
                nul += Vector128.CountWhereAllBitsSet(zeroVec & resultMask);

                nonprintable += Vector128.CountWhereAllBitsSet(~printableVec & resultMask);
            }
        }
        else
        {
            // Always have a scalar fallback
            for (int i = 0; i < data.Length; ++i)
            {
                byte c = data[i];

                if (c > 0x1f && c != 0x7f)
                    printable += 1;
                else
                {
                    switch (c)
                    {
                        case 0:
                            nul += 1;
                            nonprintable += 1;
                            break;
                        case (byte)'\n':
                            lf += 1;
                            break;
                        case (byte)'\r':
                            cr += 1;
                            if ((uint)i + 1 < data.Length && data[i + 1] == (byte)'\n')
                                crlf += 1;

                            break;
                        case (byte)'\t' or (byte)'\f' or (byte)'\v' or (byte)'\b' or 0x1b:
                            printable += 1;
                            break;
                        default:
                            nonprintable += 1;
                            break;
                    }
                }
            }
        }

        stats.Bom = bom;
        stats.Printable = printable;
        stats.Nul = nul;
        stats.Nonprintable = nonprintable;
        stats.LF = lf;
        stats.CR = cr;
        stats.CRLF = crlf;

        return cr != crlf || nul > 0 || (printable >> 7) < nonprintable;
    }

    public static bool IsBinaryData(ReadOnlySpan<byte> data)
    {
        DetectByteOrderMark(data, out int bomBytes);
        data = data.Slice(bomBytes);

        int printable = 0, nonprintable = 0;

        if (Vector128.IsHardwareAccelerated && data.Length >= Vector128<byte>.Count)
        {
            ref byte reference = ref MemoryMarshal.GetReference(data);

            if (Vector512.IsHardwareAccelerated && data.Length >= Vector512<byte>.Count)
            {
                ref byte minusOneVec = ref Unsafe.Add(ref reference, data.Length - Vector512<byte>.Count);

                Vector512<byte> c0 = Vector512.Create((byte)0x1f),
                    c1 = Vector512.Create((byte)0x7f),
                    c2 = Vector512.Create((byte)'\b'),
                    c3 = Vector512.Create((byte)'\f'),
                    c4 = Vector512.Create((byte)'\e'),
                    c5 = Vector512.Create((byte)9),
                    c6 = Vector512.Create((byte)(13 - 9));
                
                Vector512<byte> charVec, printableMask;
                
                while (Unsafe.IsAddressLessThan(ref reference, ref minusOneVec))
                {
                    charVec = Vector512.LoadUnsafe(ref reference);

                    if (Vector512.Any(charVec, (byte)0))
                        return true;

                    printableMask = Vector512.GreaterThan(charVec, c0);
                    printableMask = Vector512.AndNot(printableMask, Vector512.Equals(charVec, c1));
                    
                    printableMask |= Vector512.Equals(charVec, c2)
                        | Vector512.Equals(charVec, c3)
                        | Vector512.Equals(charVec, c4);

                    printable += Vector512.CountWhereAllBitsSet(printableMask);

                    printableMask |= Vector512.LessThanOrEqual(charVec - c5, c6);
                    
                    nonprintable += Vector512.CountWhereAllBitsSet(~printableMask);

                    reference = ref Unsafe.Add(ref reference, Vector512<byte>.Count);
                }
                
                int remaining = data.Length % Vector512<byte>.Count;
                var resultMask = remaining == 0
                    ? Vector512<byte>.AllBitsSet
                    : Vector512.GreaterThanOrEqual(Vector512<byte>.Indices, Vector512.Create((byte)(Vector512<byte>.Count - remaining)));
                
                charVec = Vector512.LoadUnsafe(ref minusOneVec);

                if (Vector512.Any(charVec, (byte)0))
                    return true;

                printableMask = Vector512.GreaterThan(charVec, c0);
                printableMask = Vector512.AndNot(printableMask, Vector512.Equals(charVec, c1));
                    
                printableMask |= Vector512.Equals(charVec, c2)
                               | Vector512.Equals(charVec, c3)
                               | Vector512.Equals(charVec, c4);
                printable += Vector512.CountWhereAllBitsSet(printableMask & resultMask);

                printableMask |= Vector512.LessThanOrEqual(charVec - c5, c6);
                    
                nonprintable += Vector512.CountWhereAllBitsSet(~printableMask & resultMask);
            }
            else if (Vector256.IsHardwareAccelerated && data.Length >= Vector256<byte>.Count)
            {
                ref byte minusOneVec = ref Unsafe.Add(ref reference, data.Length - Vector256<byte>.Count);

                Vector256<byte> c0 = Vector256.Create((byte)0x1f),
                    c1 = Vector256.Create((byte)0x7f),
                    c2 = Vector256.Create((byte)'\b'),
                    c3 = Vector256.Create((byte)'\f'),
                    c4 = Vector256.Create((byte)'\e'),
                    c5 = Vector256.Create((byte)9),
                    c6 = Vector256.Create((byte)(13 - 9));
                
                Vector256<byte> charVec, printableMask;
                
                while (Unsafe.IsAddressLessThan(ref reference, ref minusOneVec))
                {
                    charVec = Vector256.LoadUnsafe(ref reference);

                    if (Vector256.Any(charVec, (byte)0))
                        return true;

                    printableMask = Vector256.GreaterThan(charVec, c0);
                    printableMask = Vector256.AndNot(printableMask, Vector256.Equals(charVec, c1));
                    
                    printableMask |= Vector256.Equals(charVec, c2)
                        | Vector256.Equals(charVec, c3)
                        | Vector256.Equals(charVec, c4);

                    printable += Vector256.CountWhereAllBitsSet(printableMask);

                    printableMask |= Vector256.LessThanOrEqual(charVec - c5, c6);
                    
                    nonprintable += Vector256.CountWhereAllBitsSet(~printableMask);

                    reference = ref Unsafe.Add(ref reference, Vector256<byte>.Count);
                }
                
                int remaining = data.Length % Vector256<byte>.Count;
                var resultMask = remaining == 0
                    ? Vector256<byte>.AllBitsSet
                    : Vector256.GreaterThanOrEqual(Vector256<byte>.Indices, Vector256.Create((byte)(Vector256<byte>.Count - remaining)));
                
                charVec = Vector256.LoadUnsafe(ref minusOneVec);

                if (Vector256.Any(charVec, (byte)0))
                    return true;

                printableMask = Vector256.GreaterThan(charVec, c0);
                printableMask = Vector256.AndNot(printableMask, Vector256.Equals(charVec, c1));
                    
                printableMask |= Vector256.Equals(charVec, c2)
                                 | Vector256.Equals(charVec, c3)
                                 | Vector256.Equals(charVec, c4);
                printable += Vector256.CountWhereAllBitsSet(printableMask & resultMask);

                printableMask |= Vector256.LessThanOrEqual(charVec - c5, c6);
                    
                nonprintable += Vector256.CountWhereAllBitsSet(~printableMask & resultMask);
            }
            else
            {
                ref byte minusOneVec = ref Unsafe.Add(ref reference, data.Length - Vector128<byte>.Count);

                Vector128<byte> c0 = Vector128.Create((byte)0x1f),
                    c1 = Vector128.Create((byte)0x7f),
                    c2 = Vector128.Create((byte)'\b'),
                    c3 = Vector128.Create((byte)'\f'),
                    c4 = Vector128.Create((byte)'\e'),
                    c5 = Vector128.Create((byte)9),
                    c6 = Vector128.Create((byte)(13 - 9));
                
                Vector128<byte> charVec, printableMask;
                
                while (Unsafe.IsAddressLessThan(ref reference, ref minusOneVec))
                {
                    charVec = Vector128.LoadUnsafe(ref reference);

                    if (Vector128.Any(charVec, (byte)0))
                        return true;

                    printableMask = Vector128.GreaterThan(charVec, c0);
                    printableMask = Vector128.AndNot(printableMask, Vector128.Equals(charVec, c1));
                    
                    printableMask |= Vector128.Equals(charVec, c2)
                        | Vector128.Equals(charVec, c3)
                        | Vector128.Equals(charVec, c4);

                    printable += Vector128.CountWhereAllBitsSet(printableMask);

                    printableMask |= Vector128.LessThanOrEqual(charVec - c5, c6);
                    
                    nonprintable += Vector128.CountWhereAllBitsSet(~printableMask);

                    reference = ref Unsafe.Add(ref reference, Vector128<byte>.Count);
                }
                
                int remaining = data.Length % Vector128<byte>.Count;
                var resultMask = remaining == 0
                    ? Vector128<byte>.AllBitsSet
                    : Vector128.GreaterThanOrEqual(Vector128<byte>.Indices, Vector128.Create((byte)(Vector128<byte>.Count - remaining)));
                
                charVec = Vector128.LoadUnsafe(ref minusOneVec);

                if (Vector128.Any(charVec, (byte)0))
                    return true;

                printableMask = Vector128.GreaterThan(charVec, c0);
                printableMask = Vector128.AndNot(printableMask, Vector128.Equals(charVec, c1));
                    
                printableMask |= Vector128.Equals(charVec, c2)
                               | Vector128.Equals(charVec, c3)
                               | Vector128.Equals(charVec, c4);
                
                printable += Vector128.CountWhereAllBitsSet(printableMask & resultMask);

                printableMask |= Vector128.LessThanOrEqual(charVec - c5, c6);
                    
                nonprintable += Vector128.CountWhereAllBitsSet(~printableMask & resultMask);
            }
        }
        else
        {
            for (int i = 0; i < data.Length; ++i)
            {
                byte c = data[i];

                if (c == 0)
                    return true;

                if ((c > 0x1f && c != 0x7f) || c == '\b' || c == '\e' || c == '\f')
                    printable += 1;
                else if (c is not (byte)'\t' and not (byte)'\n' and not (byte)'\f' and not (byte)'\r' and not (byte)'\v')
                    nonprintable += 1;
            }
        }

        return (printable >> 7) < nonprintable;
    }

    public static bool TryFromCrlfToLf(ReadOnlySpan<byte> input, Span<byte> output, out int written)
    {
        if (input.Length / 2 > output.Length) // If we assume every pair is a crlf pair...
        {
            written = 0;
            return false;
        }
        
        // TODO: Combine the IndexOf and CopyTo operations in a manual vectorized loop.
        // As it stands, this will likely only be a little faster than the original
        // because it's able to skip over CR that isn't followed by LF
        int idx = input.IndexOf("\r\n"u8);
        int written0 = 0;

        while (idx >= 0)
        {
            if (idx + 1 > output.Length)
            {
                written = 0;
                return false;
            }

            if (idx > 0)
                input.Slice(0, idx).CopyTo(output);
            
            output[idx] = (byte)'\n';

            written0 += idx + 1;

            output = output.Slice(idx + 1);
            input = input.Slice(idx + 2);
            idx = input.IndexOf("\r\n"u8);
        }

        if (input.IsEmpty || input.TryCopyTo(output))
        {
            written = written0 + input.Length;
            return true;
        }

        written = 0;
        return false;
    }

    public static void FromLfToCrlf(ReadOnlySpan<byte> input, IBufferWriter<byte> writer)
    {
        int idx = IndexOfLFWithoutCR(input);

        while (idx >= 0)
        {
            if (idx > 0)
                writer.Write(input[..idx]);

            var output = writer.GetSpan(2);
            output[0] = (byte)'\r';
            output[1] = (byte)'\n';
            writer.Advance(2);

            input = input[(idx + 1)..];
            idx = IndexOfLFWithoutCR(input);
        }

        if (input.IsEmpty)
        {
            return;
        }

        writer.Write(input);
        return;
        
        static int IndexOfLFWithoutCR(ReadOnlySpan<byte> input)
        {
            if (input.IsEmpty)
                return -1;
            
            if (input.StartsWith((byte)'\n'))
                return 0;

            if (Vector128.IsHardwareAccelerated && input.Length >= Vector128<byte>.Count)
            {
                ref byte reference = ref MemoryMarshal.GetReference(input);
                nuint length = (nuint)input.Length, position = 0, minusOneVec;

                if (Vector512.IsHardwareAccelerated && length >= (nuint)Vector512<byte>.Count)
                {
                    minusOneVec = length - (nuint)Vector512<byte>.Count;

                    Vector512<byte> cr = Vector512.Create((byte)'\r'),
                        lf = Vector512.Create((byte)'\n'),
                        v0, v1;
                    
                    while (position < minusOneVec)
                    {
                        v0 = Vector512.Equals(cr, Vector512.LoadUnsafe(ref reference, position));
                        v1 = Vector512.Equals(lf, Vector512.LoadUnsafe(ref reference, position + 1u));

                        v0 = Vector512.AndNot(v1, v0);

                        if (Vector512.AnyWhereAllBitsSet(v0))
                        {
                            return (int)position + Vector512.IndexOfWhereAllBitsSet(v0) + 1;
                        }

                        position += (nuint)Vector512<byte>.Count;
                    }
                    
                    v0 = Vector512.LoadUnsafe(ref reference, minusOneVec);
                    v1 = Vector512.Shuffle(v0,
                        Vector512.Create(1,  2,  3,  4,  5,  6,  7,  8,  9,  10, 11, 12, 13, 14, 15, 16,
                            17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32,
                            33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
                            49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, byte.MaxValue));

                    v0 = Vector512.Equals(v0, cr);
                    v1 = Vector512.Equals(v1, lf);

                    v0 = Vector512.AndNot(v1, v0);

                    if (Vector512.AnyWhereAllBitsSet(v0))
                    {
                        return (int)minusOneVec + Vector512.IndexOfWhereAllBitsSet(v0) + 1;
                    }
                }
                else if (Vector256.IsHardwareAccelerated && length >= (nuint)Vector256<byte>.Count)
                {
                    minusOneVec = length - (nuint)Vector256<byte>.Count;

                    Vector256<byte> cr = Vector256.Create((byte)'\r'),
                        lf = Vector256.Create((byte)'\n'),
                        v0, v1;
                    
                    while (position < minusOneVec)
                    {
                        v0 = Vector256.Equals(cr, Vector256.LoadUnsafe(ref reference, position));
                        v1 = Vector256.Equals(lf, Vector256.LoadUnsafe(ref reference, position + 1u));

                        v0 = Vector256.AndNot(v1, v0);

                        if (Vector256.AnyWhereAllBitsSet(v0))
                        {
                            return (int)position + Vector256.IndexOfWhereAllBitsSet(v0) + 1;
                        }

                        position += (nuint)Vector256<byte>.Count;
                    }
                    
                    v0 = Vector256.LoadUnsafe(ref reference, minusOneVec);
                    v1 = Vector256.Shuffle(v0,
                        Vector256.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
                            17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, byte.MaxValue));

                    v0 = Vector256.Equals(v0, cr);
                    v1 = Vector256.Equals(v1, lf);

                    v0 = Vector256.AndNot(v1, v0);

                    if (Vector256.AnyWhereAllBitsSet(v0))
                    {
                        return (int)minusOneVec + Vector256.IndexOfWhereAllBitsSet(v0) + 1;
                    }
                }
                else
                {
                    minusOneVec = (nuint)(input.Length - Vector128<byte>.Count);

                    Vector128<byte> cr = Vector128.Create((byte)'\r'),
                        lf = Vector128.Create((byte)'\n'),
                        v0, v1;
                    
                    while (position < minusOneVec)
                    {
                        v0 = Vector128.Equals(cr, Vector128.LoadUnsafe(ref reference, position));
                        v1 = Vector128.Equals(lf, Vector128.LoadUnsafe(ref reference, position + 1u));

                        v0 = Vector128.AndNot(v1, v0);

                        if (Vector128.AnyWhereAllBitsSet(v0))
                        {
                            return (int)position + Vector128.IndexOfWhereAllBitsSet(v0) + 1;
                        }

                        position += (nuint)Vector128<byte>.Count;
                    }
                    
                    v0 = Vector128.LoadUnsafe(ref reference, minusOneVec);
                    v1 = Vector128.Shuffle(v0,
                        Vector128.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, byte.MaxValue));
                    
                    v0 = Vector128.Equals(v0, cr);
                    v1 = Vector128.Equals(v1, lf);

                    v0 = Vector128.AndNot(v1, v0);

                    if (Vector128.AnyWhereAllBitsSet(v0))
                    {
                        return (int)minusOneVec + Vector128.IndexOfWhereAllBitsSet(v0) + 1;
                    }
                }
            }
            else
            {
                int nextStart = 1, idx;

                while ((idx = input.Slice(nextStart).IndexOf((byte)'\n')) >= 0)
                {
                    nextStart += idx;

                    if (input[nextStart - 1] != '\r')
                        return nextStart;
                }
            }
            
            return -1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TTo[] SelectToArray<TFrom, TTo>(this ImmutableArray<TFrom> collection, Func<TFrom, TTo> func)
    {
        if (collection.IsDefaultOrEmpty)
            return [];
        
        var resultArray = new TTo[collection.Length];

        for (int i = 0; i < collection.Length; ++i)
        {
            resultArray[i] = func(collection[i]);
        }

        return resultArray;
    }
}
