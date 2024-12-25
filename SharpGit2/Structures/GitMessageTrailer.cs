using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2.Native;

public unsafe struct GitMessageTrailer
{
    public byte* Key;
    public byte* Value;
}

public unsafe struct GitMessageTrailerArray
{
    public GitMessageTrailer* Trailers;
    public nuint Count;

#pragma warning disable IDE0044, IDE0051, CS0169 // Add readonly modifier
    // Relevent to the native library, DO NOT REMOVE.
    private byte* _trailer_block;
#pragma warning restore IDE0044, IDE0051, CS0169 // Add readonly modifier

    public readonly KeyValuePair<string, string>[] ToManaged()
    {
        ReadOnlySpan<GitMessageTrailer> span = new(Trailers, checked((int)Count));

        var array = new KeyValuePair<string, string>[span.Length];
        for (int i = 0; i < span.Length; ++i)
        {
            var key = Git2.GetPooledString(span[i].Key);
            var value = Utf8StringMarshaller.ConvertToManaged(span[i].Value);

            array[i] = new(key!, value!);
        }

        return array;
    }
}
