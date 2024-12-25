using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace SharpGit2.Native;

public unsafe struct GitStringArray(byte** strings, nuint count)
{
    public byte** Strings = strings;
    public nuint Count = count;

    public readonly string[] ToManaged(bool poolValues = false)
    {
        int count = checked((int)Count);
        byte** ptr = Strings;

        if (count == 0 || ptr == null)
            return [];

        var managedArray = new string[count];

        if (poolValues)
        {
            for (int i = 0; i < count; ++i)
            {
                var nativeValue = ptr[i];

                if (nativeValue != null)
                {
                    managedArray[i] = Git2.GetPooledString(nativeValue);
                }
            }
        }
        else
        {
            for (int i = 0; i < count; ++i)
            {
                managedArray[i] = Utf8StringMarshaller.ConvertToManaged(ptr[i])!;
            }
        }

        return managedArray;
    }
}
