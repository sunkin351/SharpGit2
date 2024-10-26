using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2.Native;

public unsafe struct GitStringArray(byte** strings, nuint count)
{
    public byte** Strings = strings;
    public nuint Count = count;

    public readonly string[] ToManaged()
    {
        int count = checked((int)Count);
        byte** ptr = Strings;

        if (count == 0 || ptr == null)
            return [];

        var managedArray = new string[count];

        for (int i = 0; i < count; ++i)
        {
            managedArray[i] = Utf8StringMarshaller.ConvertToManaged(ptr[i])!;
        }

        return managedArray;
    }
}
