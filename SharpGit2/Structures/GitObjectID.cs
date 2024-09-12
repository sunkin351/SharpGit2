using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGit2;

public enum GitObjectIDType : byte
{
    SHA1 = 1,
#if GIT_EXPERIMENTAL_SHA256
    SHA256 = 2,
#endif
}

#if !GIT_EXPERIMENTAL_SHA256

[StructLayout(LayoutKind.Sequential)]
public unsafe struct GitObjectID : IEquatable<GitObjectID>, IComparable<GitObjectID>
{
    public IdArray Id;

    public GitObjectID(ReadOnlySpan<byte> idBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(idBytes.Length, 20);

        idBytes.CopyTo(Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(GitObjectID other)
    {
        return ((ReadOnlySpan<byte>)this.Id).SequenceEqual(other.Id);
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is GitObjectID other && this.Equals(other);
    }

    public readonly override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddBytes(Id);

        return hash.ToHashCode();
    }

    /// <summary>
    /// Create a hexidecimal string that represents the current ID
    /// </summary>
    public readonly override string ToString()
    {
        return Convert.ToHexString(this.Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int CompareTo(GitObjectID other)
    {
        return ((ReadOnlySpan<byte>)this.Id).SequenceCompareTo(other.Id);
    }

    public static GitObjectID FromHexString(string hexString)
    {
        ArgumentNullException.ThrowIfNull(hexString);

        if (hexString.Length != 40)
        {
            throw new ArgumentException("Invalid Hex String Length!");
        }

        ReadOnlySpan<byte> idBytes = Convert.FromHexString(hexString);

        GitObjectID result = default;
        idBytes.CopyTo(result.Id);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(GitObjectID left, GitObjectID right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(GitObjectID left, GitObjectID right)
    {
        return !left.Equals(right);
    }

    [InlineArray(20)]
    public struct IdArray
    {
        public byte Element;
    }
}

#else

[StructLayout(LayoutKind.Sequential)]
public unsafe struct GitObjectID : IEquatable<GitObjectID>, IComparable<GitObjectID>
{
    public GitObjectIDType Type;
    public IdArray Id;

    public GitObjectID(GitObjectIDType type, ReadOnlySpan<byte> idBytes)
    {
        switch (type)
        {
            case GitObjectIDType.SHA1:
                ArgumentOutOfRangeException.ThrowIfNotEqual(idBytes.Length, 20);
                break;
            case GitObjectIDType.SHA256:
                ArgumentOutOfRangeException.ThrowIfNotEqual(idBytes.Length, 32);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }

        this = default;
        Type = type;
        idBytes.CopyTo(Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(GitObjectID other)
    {
        return this.Type == other.Type && ((ReadOnlySpan<byte>)this.Id).SequenceEqual(other.Id);
    }

    public readonly override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is GitObjectID other && this.Equals(other);
    }

    public readonly override string ToString()
    {
        ReadOnlySpan<byte> span = this.Id;

        return Convert.ToHexString(this.Type switch { GitObjectIDType.SHA1 => span.Slice(0, 20), _ => span });
    }

    public readonly override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Type);
        hash.AddBytes(Id);

        return hash.ToHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int CompareTo(GitObjectID other)
    {
        if (this.Type != other.Type)
            return ((int)this.Type).CompareTo((int)other.Type);

        return ((ReadOnlySpan<byte>)this.Id).SequenceCompareTo(other.Id);
    }

    public static GitObjectID FromHexString(string hexString)
    {
        ArgumentNullException.ThrowIfNull(hexString);

        if (hexString.Length is not 40 and not 64)
        {
            throw new ArgumentException("Invalid Hex String Length!");
        }

        ReadOnlySpan<byte> byteArr = Convert.FromHexString(hexString);

        GitObjectID result = default;
        result.Type = byteArr.Length == 20 ? GitObjectIDType.SHA1 : GitObjectIDType.SHA256;
        byteArr.CopyTo(result.Id);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(GitObjectID left, GitObjectID right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(GitObjectID left, GitObjectID right)
    {
        return !left.Equals(right);
    }

    [InlineArray(32)]
    public struct IdArray
    {
        public byte Element;
    }
}

#endif