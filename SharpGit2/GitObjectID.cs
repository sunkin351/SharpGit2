using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGit2;

#if SHA256_OBJECT_ID
public enum GitObjectIDType : byte
{
    SHA1 = 1,
    SHA256 = 2
}
#endif

[StructLayout(LayoutKind.Sequential)]
public struct GitObjectID
{
#if SHA256_OBJECT_ID
    public GitObjectIDType Type;
#endif
    public IdArray Id;

#if SHA256_OBJECT_ID
    [InlineArray(32)]
#else
    [InlineArray(20)]
#endif
    public struct IdArray
    {
        public byte Element;
    }
}
