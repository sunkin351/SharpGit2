using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;

namespace SharpGit2.Native;

public unsafe struct GitBuffer
{
    /// <summary>
    /// The buffer contents.  `ptr` points to the start of the buffer
    /// being returned.The buffer's length (in bytes) is specified
    /// by the `size` member of the structure, and contains a NUL
    /// terminator at position `(size + 1)`.
    /// </summary>
    public byte* Pointer;
    /// <summary>
    /// This field is reserved and unused.
    /// </summary> 
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "This is a native padding field, reserved for future use")]
    [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
    private nuint Reserved;
    /// <summary>
    /// The length (in bytes) of the buffer pointed to by `ptr`,
    /// not including a NUL terminator.
    /// </summary>
    public nuint Size;

    public readonly ReadOnlySpan<byte> Span => new(Pointer, checked((int)Size));

    /// <summary>
    /// Interprets the buffer as a UTF8 string and converts it to a managed string
    /// </summary>
    /// <returns>The managed string</returns>
    public readonly string AsString()
    {
        return Encoding.UTF8.GetString(Pointer, checked((int)Size));
    }
    
    public readonly bool ContainsNul()
    {
        if (Size <= int.MaxValue)
        {
            return new ReadOnlySpan<byte>(Pointer, (int)Size).Contains((byte)0);
        }

        return NonTrivialPath(Pointer, Size);

        static bool NonTrivialPath(byte* ptr, nuint length)
        {
            if (Vector.IsHardwareAccelerated)
            {
                byte* ptrMinusOneVec = ptr + length - Vector<byte>.Count;

                if (Vector.EqualsAny(Vector<byte>.Zero, Vector.Load(ptr)))
                {
                    return true;
                }

                // align the pointer
                nuint alignment = (nuint)Vector<byte>.Count;
                ptr = (byte*)(((nuint)ptr + alignment - 1) / alignment * alignment);
                Debug.Assert((nuint)ptr % alignment == 0);

                while (ptr < ptrMinusOneVec)
                {
                    // non temporal loads to prevent thrashing the cpu cache
                    // with accessing so much memory
                    if (Vector.EqualsAny(Vector<byte>.Zero, Vector.LoadAlignedNonTemporal(ptr)))
                    {
                        return true;
                    }

                    ptr += (nuint)Vector<byte>.Count;
                }

                if (Vector.EqualsAny(Vector<byte>.Zero, Vector.Load(ptrMinusOneVec)))
                {
                    return true;
                }

                return false;
            }

            var (loopCount, remainder) = Math.DivRem(length, int.MaxValue);

            for (nuint i = 0; i < loopCount; ++i, ptr += int.MaxValue)
            {
                if (new ReadOnlySpan<byte>(ptr, int.MaxValue).Contains((byte)0))
                {
                    return true;
                }
            }

            if (new ReadOnlySpan<byte>(ptr, (int)remainder).Contains((byte)0))
            {
                return true;
            }

            return false;
        }
    }
}
