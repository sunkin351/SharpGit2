using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;

namespace SharpGit2.Managed;

public enum GitObjectIDType : byte
{
    SHA1 = 1,
#if GIT_EXPERIMENTAL_SHA256
    SHA256 = 2,
#endif
    Default = SHA1
}

#if !GIT_EXPERIMENTAL_SHA256

[StructLayout(LayoutKind.Sequential)]
public unsafe record struct GitObjectID : IComparable<GitObjectID>, ISpanFormattable
{
    public static int MaxHexSize => SHA1.HashSizeInBytes * 2;
    public static int MaxByteSize => SHA1.HashSizeInBytes;

    public static GitObjectID Zero => default;
    
    public IdByteArray Id;

    public GitObjectID(GitObjectIDType type, ReadOnlySpan<byte> idBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(type, GitObjectIDType.SHA1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(idBytes.Length, SHA1.HashSizeInBytes);

        idBytes.CopyTo(this.Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(GitObjectID other)
    {
        return ((ReadOnlySpan<byte>)this.Id).SequenceEqual(other.Id);
    }
    
    public static bool Equals(in GitObjectID first, in GitObjectID second)
    {
        ref byte f = ref Unsafe.As<IdByteArray, byte>(ref Unsafe.AsRef(in first.Id));
        ref byte s = ref Unsafe.As<IdByteArray, byte>(ref Unsafe.AsRef(in second.Id));

        if (Vector128.IsHardwareAccelerated)
        {
            var cmp = Vector128.LoadUnsafe(ref f, 0) ^ Vector128.LoadUnsafe(ref s, 0);
            cmp |= Vector128.LoadUnsafe(ref f, 4u) ^ Vector128.LoadUnsafe(ref s, 4u);
            return Vector128.EqualsAll(cmp, Vector128<byte>.Zero);
        }
        
        return Unsafe.ReadUnaligned<ulong>(ref f) == Unsafe.ReadUnaligned<ulong>(ref s)
            && Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref f, 8)) == Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref s, 8))
            && Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref f, 16)) == Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 16));
    }
    
    public readonly override int GetHashCode()
    {
        return CommunityToolkit.HighPerformance.Helpers.HashCode<byte>.Combine(this.Id);
    }

    /// <summary>
    /// Create a hexadecimal string that represents the current ID
    /// </summary>
    public readonly override string ToString()
    {
        return Convert.ToHexStringLower(this.Id);
    }

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return this.ToString();
    }

    public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
    {
        return Convert.TryToHexStringLower(this.Id, destination, out charsWritten);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int CompareTo(GitObjectID other)
    {
        return ((ReadOnlySpan<byte>)this.Id).SequenceCompareTo(other.Id);
    }

    public readonly bool IsZero
    {
        get
        {
            Debug.Assert(sizeof(IdByteArray) == 20);
            
            ref byte reference = ref Unsafe.As<IdByteArray, byte>(ref Unsafe.AsRef(in this.Id));

            if (Vector128.IsHardwareAccelerated)
            {
                // The area to check is 20 bytes long, which is longer than a single vector, but less than 2.
                // Benchmarking proved this method was the fastest method on modern/semi-modern CPUs to check
                // if all 20 bytes are zero. (sub-nanosecond time, on my AMD Ryzen 2700)
                var value = Vector128.LoadUnsafe(ref reference, 0);
                value |= Vector128.LoadUnsafe(ref reference, 4u);
            
                return Vector128.EqualsAll(value, Vector128<byte>.Zero);
            }

            // Always supply a fallback for vectorized code paths.
            return Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref reference, 0)) == 0
                && Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref reference, 8)) == 0
                && Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref reference, 16)) == 0;
        }
    }

    public static GitObjectID Parse(ReadOnlySpan<char> hexString, GitObjectIDType type = GitObjectIDType.Default)
    {
        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(nameof(type));
        
        if (hexString.Length != MaxHexSize)
        {
            throw new ArgumentException($"Invalid Hex String Length! (Must be {MaxHexSize} characters)");
        }

        GitObjectID result = default;
        var status = Convert.FromHexString(hexString, result.Id, out var consumed, out var written);

        if (status == OperationStatus.InvalidData)
        {
            throw new ArgumentException("Invalid Hex String Content!");
        }

        Debug.Assert(status == OperationStatus.Done && consumed == hexString.Length && written == SHA1.HashSizeInBytes);

        return result;
    }

    public static bool TryParse(ReadOnlySpan<char> hexString, out GitObjectID id, GitObjectIDType type = GitObjectIDType.Default)
    {
        id = default;
        
        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(nameof(type));
        
        if (hexString.Length != MaxHexSize)
        {
            return false;
        }

        var status = Convert.FromHexString(hexString, id.Id, out var consumed, out var written);

        if (status == OperationStatus.InvalidData)
        {
            id = default;
            return false;
        }

        Debug.Assert(status == OperationStatus.Done
                     && consumed == hexString.Length
                     && written == SHA1.HashSizeInBytes);
        return true;
    }

    public static bool TryParsePrefix(ReadOnlySpan<char> hexString, out GitObjectID id, out ushort nibblePrefixLength)
    {
        if (hexString.Length > 40)
        {
            throw new ArgumentException("Invalid Hex String Length!");
        }

        id = default;
        nibblePrefixLength = 0;

        if (hexString.IsEmpty)
        {
            return false;
        }

        Span<byte> output = id.Id;

        var status = Convert.FromHexString(hexString, output, out int consumed, out int written);

        switch (status)
        {
            case OperationStatus.Done:
                nibblePrefixLength = (ushort)hexString.Length;
                return true;
            case OperationStatus.NeedMoreData:

                Debug.Assert(consumed == hexString.Length - 1);

                status = Convert.FromHexString([hexString[^1], '0'], output.Slice(written), out _, out _);

                Debug.Assert(status == OperationStatus.Done); // assume all the data was verified on the first call
                goto case OperationStatus.Done;

            default:
                return false;
        }
    }

    [InlineArray(SHA1.HashSizeInBytes)]
    public struct IdByteArray
    {
        public byte Element;
    }
}

#else

[StructLayout(LayoutKind.Sequential)]
public unsafe record struct GitObjectID : IComparable<GitObjectID>, ISpanFormattable
{
    internal const int SHA1HexSize = SHA1.HashSizeInBytes * 2;
    internal const int SHA256HexSize = SHA256.HashSizeInBytes * 2;

    public static int MaxHexSize => SHA256HexSize;
    public static int MaxByteSize => SHA256.HashSizeInBytes;

    public static GitObjectID Zero => default;

    public GitObjectIDType Type;
    public IdByteArray Id;

    public GitObjectID(GitObjectIDType type, ReadOnlySpan<byte> idBytes)
    {
        switch (type)
        {
            case GitObjectIDType.SHA1:
                ArgumentOutOfRangeException.ThrowIfNotEqual(idBytes.Length, SHA1.HashSizeInBytes);
                break;
            case GitObjectIDType.SHA256:
                ArgumentOutOfRangeException.ThrowIfNotEqual(idBytes.Length, SHA256.HashSizeInBytes);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }

        this = default;
        this.Type = type;
        idBytes.CopyTo(this.Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(GitObjectID other)
    {
        return Equals(in this, in other);
    }

    public static bool Equals(in GitObjectID first, in GitObjectID second)
    {
        if (first.Type != second.Type)
            return false;
        
        ref byte firstRef = ref Unsafe.As<IdByteArray, byte>(ref Unsafe.AsRef(in first.Id));
        ref byte secondRef = ref Unsafe.As<IdByteArray, byte>(ref Unsafe.AsRef(in second.Id));

        switch (first.Type)
        {
            case GitObjectIDType.SHA1:
                if (Vector128.IsHardwareAccelerated)
                {
                    var cmp = Vector128.LoadUnsafe(ref firstRef, 0) ^ Vector128.LoadUnsafe(ref secondRef, 0);
                    cmp |= Vector128.LoadUnsafe(ref firstRef, 4u) ^ Vector128.LoadUnsafe(ref secondRef, 4u);
                    return Vector128.EqualsAll(cmp, Vector128<byte>.Zero);
                }
                
                return Unsafe.ReadUnaligned<ulong>(ref firstRef) == Unsafe.ReadUnaligned<ulong>(ref secondRef)
                   && Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref firstRef, 8)) == Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref secondRef, 8))
                   && Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref firstRef, 16)) == Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref secondRef, 16));
            case GitObjectIDType.SHA256:
                if (Vector256.IsHardwareAccelerated)
                {
                    return Vector256.LoadUnsafe(ref firstRef) == Vector256.LoadUnsafe(ref secondRef);
                }
                
                if (Vector128.IsHardwareAccelerated)
                {
                    var cmp = Vector128.LoadUnsafe(ref firstRef, 0) ^ Vector128.LoadUnsafe(ref secondRef, 0);
                    cmp |= Vector128.LoadUnsafe(ref firstRef, 16u) ^ Vector128.LoadUnsafe(ref secondRef, 16u);
                    return Vector128.EqualsAll(cmp, Vector128<byte>.Zero);
                }
                
                return Unsafe.ReadUnaligned<ulong>(ref firstRef) == Unsafe.ReadUnaligned<ulong>(ref secondRef)
                   && Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref firstRef, 8)) == Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref secondRef, 8))
                   && Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref firstRef, 16)) == Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref secondRef, 16))
                   && Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref firstRef, 24)) == Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref secondRef, 24));
        }

        return false;
    }

    public readonly override string ToString()
    {
        ReadOnlySpan<byte> span = this.Id;
        
        Debug.Assert(this.Type is GitObjectIDType.SHA1 or GitObjectIDType.SHA256);

        if (this.Type != GitObjectIDType.SHA256)
            span = span.Slice(0, SHA1.HashSizeInBytes);

        return Convert.ToHexStringLower(span);
    }

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return this.ToString();
    }

    public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
    {
        ReadOnlySpan<byte> idSpan = this.Id;

        Debug.Assert(this.Type is GitObjectIDType.SHA1 or GitObjectIDType.SHA256);

        if (this.Type != GitObjectIDType.SHA256)
            idSpan = idSpan.Slice(0, SHA1.HashSizeInBytes);

        return Convert.TryToHexStringLower(idSpan, destination, out charsWritten);
    }

    public readonly override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(this.Type);
        
        Debug.Assert(this.Type is GitObjectIDType.SHA1 or GitObjectIDType.SHA256);

        ReadOnlySpan<byte> idSpan = this.Id;

        if (this.Type != GitObjectIDType.SHA256)
            idSpan = idSpan.Slice(0, SHA1.HashSizeInBytes);
        
        hash.AddBytes(idSpan);

        return hash.ToHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int CompareTo(GitObjectID other)
    {
        if (this.Type < other.Type)
            return -1;

        if (this.Type > other.Type)
            return 1;

        // Let's assume any bytes beyond the end of an SHA1 hash are garbage, and ignore them
        int size = this.Type.HashSize;

        var left = ((ReadOnlySpan<byte>)this.Id).Slice(0, size);
        var right = ((ReadOnlySpan<byte>)other.Id).Slice(0, size);

        return left.SequenceCompareTo(right);
    }

    public readonly bool IsZero
    {
        get
        {
            if (this.Type == 0)
                return true;

            ref byte reference = ref Unsafe.As<IdByteArray, byte>(ref Unsafe.AsRef(in this.Id));
            
            switch (this.Type)
            {
                case GitObjectIDType.SHA1:
                {
                    if (Vector128.IsHardwareAccelerated)
                    {
                        Vector128<byte> value = Vector128.LoadUnsafe(ref reference, 0);
                        value |= Vector128.LoadUnsafe(ref reference, 4u);

                        return Vector128.EqualsAll(value, Vector128<byte>.Zero);
                    }
                    
                    // Always supply a fallback for vectorized code paths.
                    return Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref reference, 0)) == 0
                        && Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref reference, 8)) == 0
                        && Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref reference, 16)) == 0;
                }
                case GitObjectIDType.SHA256:
                {
                    if (Vector256.IsHardwareAccelerated)
                    {
                        Vector256<byte> value = Vector256.LoadUnsafe(ref reference, 0);
                        return Vector256.EqualsAll(value, Vector256<byte>.Zero);
                    }
                    else if (Vector128.IsHardwareAccelerated)
                    {
                        Vector128<byte> value = Vector128.LoadUnsafe(ref reference, 0);
                        value |= Vector128.LoadUnsafe(ref reference, 16u);
                        return Vector128.EqualsAll(value, Vector128<byte>.Zero);
                    }
                    else
                    {
                        // Always supply a fallback for vectorized code paths.
                        return Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref reference, 0)) == 0
                            && Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref reference, 8)) == 0
                            && Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref reference, 16)) == 0
                            && Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref reference, 24)) == 0;
                    }
                }
                default:
                    throw new InvalidOperationException("Invalid type value for GitObjectID");
            }
        }
    }

    public static GitObjectID Parse(ReadOnlySpan<char> hexString, GitObjectIDType type = GitObjectIDType.Default)
    {
        int hexSize = type.HexSize;
        if (hexString.Length != hexSize)
        {
            throw new ArgumentException("Invalid Hex String Length!");
        }

        GitObjectID result = default;
        var status = Convert.FromHexString(hexString, result.Id, out var consumed, out var written);

        if (status == OperationStatus.InvalidData)
        {
            throw new ArgumentException("Invalid Hex String Content!");
        }
        
        Debug.Assert(status == OperationStatus.Done && consumed == hexString.Length && written is SHA1.HashSizeInBytes or SHA256.HashSizeInBytes);
        
        result.Type = written == SHA256.HashSizeInBytes ? GitObjectIDType.SHA256 : GitObjectIDType.SHA1;

        return result;
    }

    public static bool TryParse(
        ReadOnlySpan<char> hexString,
        out GitObjectID id,
        GitObjectIDType type = GitObjectIDType.Default)
    {
        id = default;
        
        int hexSize = type.HexSize;
        if (hexString.Length != hexSize)
        {
            id = default;
            return false;
        }

        id.Type = type;
        var status = Convert.FromHexString(hexString, id.Id, out var consumed, out var written);

        if (status == OperationStatus.InvalidData)
        {
            id = default;
            return false;
        }
        
        Debug.Assert(status == OperationStatus.Done && consumed == hexString.Length && written is SHA1.HashSizeInBytes or SHA256.HashSizeInBytes);

        return true;
    }

    public static bool TryParsePrefix(ReadOnlySpan<char> hexString, out GitObjectID id, out ushort nibblePrefixLength)
    {
        id = default;
        nibblePrefixLength = 0;

        if (hexString.IsEmpty || hexString.Length > MaxHexSize)
        {
            return false;
        }

        Span<byte> output = id.Id;

        var status = Convert.FromHexString(hexString, output, out int consumed, out int written);

        switch (status)
        {
            case OperationStatus.Done:
                nibblePrefixLength = (ushort)hexString.Length;
                return true;
            case OperationStatus.NeedMoreData:

                Debug.Assert(consumed == hexString.Length - 1);

                status = Convert.FromHexString([hexString[^1], '0'], output.Slice(written), out _, out _);

                Debug.Assert(status == OperationStatus.Done); // assume all the data was verified on the first call
                goto case OperationStatus.Done;

            default:
                return false;
        }
    }

    [InlineArray(SHA256.HashSizeInBytes)]
    public struct IdByteArray
    {
        public byte Element;
    }
}

#endif