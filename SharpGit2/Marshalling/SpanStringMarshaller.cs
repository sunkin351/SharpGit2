﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace SharpGit2.Marshalling;

[CustomMarshaller(typeof(ReadOnlySpan<char>), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
internal static unsafe class SpanStringMarshaller
{
    public ref struct ManagedToUnmanagedIn
    {
        /// <summary>
        /// Gets the requested buffer size for optimized marshalling.
        /// </summary>
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
}
