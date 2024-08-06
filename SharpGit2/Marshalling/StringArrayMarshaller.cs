using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace SharpGit2.Marshalling;

[CustomMarshaller(typeof(string[]), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
internal unsafe static class StringArrayMarshaller
{
    public ref struct ManagedToUnmanagedIn
    {
        private Git2.StringArray _array;

        public void FromManaged(string[] array)
        {
            if (array is null or { Length: 0 })
                return;

            var memory = NativeMemory.AllocZeroed((nuint)array.Length * (nuint)sizeof(void*));

            try
            {
                Span<nint> span = new(memory, array.Length);

                int i = 0;

                try
                {
                    for (; i < array.Length; ++i)
                    {
                        string value = array[i];

                        if (value is null)
                            continue; // span element should already be zero

                        int exactCount = Encoding.UTF8.GetByteCount(value);

                        byte* strMem = (byte*)NativeMemory.Alloc((nuint)exactCount + 1);

                        try
                        {
                            int written = Encoding.UTF8.GetBytes(value, new Span<byte>(strMem, exactCount));
                            strMem[written] = 0;

                            span[i] = (nint)strMem;
                        }
                        catch
                        {
                            NativeMemory.Free(strMem);
                            throw;
                        }
                    }
                }
                catch
                {
                    for (int j = 0; j < i; ++j)
                    {
                        NativeMemory.Free((byte*)span[j]);
                    }

                    throw;
                }
            }
            catch
            {
                NativeMemory.Free(memory);
                throw;
            }

            _array = new Git2.StringArray((byte**)memory, (nuint)array.Length);
        }

        public Git2.StringArray* ToUnmanaged()
        {
            return (Git2.StringArray*)Unsafe.AsPointer(ref _array);
        }

        public void Free()
        {
            if (_array.Strings is null)
                return;

            var span = new ReadOnlySpan<nint>(_array.Strings, (int)_array.Count);

            for (int i = 0; i < span.Length; ++i)
            {
                NativeMemory.Free((void*)span[i]); // null is handled into being a no-op
            }

            NativeMemory.Free(_array.Strings);

            _array = default;
        }
    }
}
