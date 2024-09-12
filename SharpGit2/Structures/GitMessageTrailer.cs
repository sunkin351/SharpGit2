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

    private byte* _trailer_block;

    public KeyValuePair<string, string>[] ToManaged()
    {
        ReadOnlySpan<GitMessageTrailer> span = new(Trailers, checked((int)Count));

        var array = new KeyValuePair<string, string>[span.Length];
        for (int i = 0; i < span.Length; ++i)
        {
            var key = Utf8StringMarshaller.ConvertToManaged(span[i].Key);
            var value = Utf8StringMarshaller.ConvertToManaged(span[i].Value);

            array[i] = new(key!, value!);
        }

        return array;
    }
}
