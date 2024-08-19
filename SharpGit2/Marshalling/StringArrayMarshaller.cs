using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace SharpGit2.Marshalling;

[CustomMarshaller(typeof(string[]), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
public unsafe static class StringArrayMarshaller
{
    public static Native.GitStringArray ConvertToUnmanaged(string[]? array)
    {
        if (array is null or { Length: 0 })
            return default;

        byte* strMemTmp = null;
        int countInitialized = 0;

        var arrayMemory = (byte**)NativeMemory.Alloc((nuint)array.Length * (nuint)sizeof(void*));
        
        try
        {
            for (int i = 0; i < array.Length; ++i, ++countInitialized)
            {
                string value = array[i];

                if (string.IsNullOrEmpty(value))
                {
                    arrayMemory[i] = null;
                    continue;
                }

                int exactCount = Encoding.UTF8.GetByteCount(value);

                byte* strMem = strMemTmp = (byte*)NativeMemory.Alloc((nuint)exactCount + 1);
                
                int written = Encoding.UTF8.GetBytes(value, new Span<byte>(strMem, exactCount));
                strMem[written] = 0;
                arrayMemory[i] = strMem;

                strMemTmp = null;
            }

            return new Native.GitStringArray(arrayMemory, (nuint)array.Length);
        }
        catch
        {
            if (strMemTmp is not null)
            {
                NativeMemory.Free(strMemTmp);
            }

            for (int j = 0; j < countInitialized; ++j)
            {
                NativeMemory.Free(arrayMemory[j]);
            }

            NativeMemory.Free(arrayMemory);
            throw;
        }
    }

    public static void Free(Native.GitStringArray array)
    {
        if (array.Strings is null)
            return;

        for (nuint i = 0; i < array.Count; ++i)
        {
            NativeMemory.Free(array.Strings[i]);
        }

        NativeMemory.Free(array.Strings);
    }

    public ref struct ManagedToUnmanagedIn
    {
        private Native.GitStringArray _array;

        public void FromManaged(string[] array)
        {
            _array = ConvertToUnmanaged(array);
        }

        public Native.GitStringArray* ToUnmanaged()
        {
            return (Native.GitStringArray*)Unsafe.AsPointer(ref _array);
        }

        public void Free()
        {
            StringArrayMarshaller.Free(_array);
            _array = default;
        }
    }
}
